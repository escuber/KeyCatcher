using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace KeyCatcher.services
{
    public sealed class KeyCatcherWiFiService : IDisposable //IKeyCatcherCommService, 
    {
        private const int DevicePort = 4210;
        private const string ApIp = "192.168.4.1";

        private string? _ipAddress;
        private readonly CancellationTokenSource _cts = new();

        public event EventHandler<bool>? ConnectedChanged;

        public bool IsConnected { get; set; }
        public bool IsApMode => _ipAddress == ApIp;

        public KeyCatcherWiFiService()
        {
            _ipAddress = IpCache.Load()?.ToString();
        }

        public async Task<bool> ConnectAsync()
        {
            if (IsConnected && !string.IsNullOrEmpty(_ipAddress))
                return true;

            // 1) try cached
            if (!string.IsNullOrWhiteSpace(_ipAddress))
            {
                if (await ProbeAsync(_ipAddress))
                {
                    MarkConnected(_ipAddress);
                    return true;
                }
            }

            // 2) discovery
            var found = await DiscoverDeviceIpAsync(DevicePort, 2000, _cts.Token);
            if (!string.IsNullOrEmpty(found))
            {
                MarkConnected(found);
                return true;
            }

            // 3) AP fallback
            if (await ProbeAsync(ApIp))
            {
                MarkConnected(ApIp);
                return true;
            }

            return false;
        }

        public async Task<bool> PingAsync(int timeoutMs = 2000)
        {
            if (!await ConnectAsync())
                return false;

            var reply = await SendAndWaitAsync("ping", _ipAddress!, DevicePort, timeoutMs);
            return reply != null && reply.Contains("pong", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> SendTextAsync(string text)
        {
            if (!await ConnectAsync())
                return false;

            var payload = text.EndsWith("<<END>>") ? text : text + "<<END>>";
            //var reply = await SendAndWaitAsync(payload, _ipAddress!, DevicePort, 5000);

            await SendWithAckAsync(payload, _ipAddress!, DevicePort, 8000);
            //    .ContinueWith(t =>
            //{
            //    if (t.IsFaulted)
            //        Log($"[WiFi] SendWithAck failed: {t.Exception?.GetBaseException().Message}");
            //});
            return true;// !string.IsNullOrEmpty(reply);
        }

        public async Task<string?> GetConfigAsync()
        {
            // if (!await ConnectAsync())
            //    return null;

            return await SendAndWaitAsync("get_config", _ipAddress!, DevicePort, 3000);
        }

        public void Disconnect()
        {
            IsConnected = false;
            _ipAddress = null;
            ConnectedChanged?.Invoke(this, false);
        }

        public void Dispose() => _cts.Cancel();

        // ---------------- internal helpers ----------------

        private void MarkConnected(string ip)
        {
            _ipAddress = ip;
            IsConnected = true;
            IpCache.Store(IPAddress.Parse(ip));
            ConnectedChanged?.Invoke(this, true);
            System.Diagnostics.Debug.WriteLine($"[WiFi] ✔ Connected @ {ip}");
        }
        public async Task<bool> ProbeAsync()
        {
            var reply = await SendAndWaitAsync("ping", _ipAddress, DevicePort, 1200);
            return !string.IsNullOrEmpty(reply);
        }
        private static async Task<bool> ProbeAsync(string host)
        {
            var reply = await SendAndWaitAsync("ping", host, DevicePort, 1200);
            return !string.IsNullOrEmpty(reply);
        }

        private static async Task<string?> SendAndWaitAsync(string msg, string host, int port, int timeoutMs)
        {
            try
            {
                using var sock = new UdpClient(AddressFamily.InterNetwork);
                if (!IPAddress.TryParse(host, out var ip))
                    ip = (await Dns.GetHostAddressesAsync(host))
                        .First(a => a.AddressFamily == AddressFamily.InterNetwork);

                sock.Connect(new IPEndPoint(ip, port));
                var bytes = Encoding.UTF8.GetBytes(msg);
                await sock.SendAsync(bytes, bytes.Length);

                var task = sock.ReceiveAsync();
                if (await Task.WhenAny(task, Task.Delay(timeoutMs)) == task)
                {
                    var res = await task;
                    return Encoding.UTF8.GetString(res.Buffer);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        public static async Task SendWithAckAsync(string msg, string host, int port, int finalTimeoutMs)
        {
            using var sock = new UdpClient(AddressFamily.InterNetwork);
            sock.Connect(host, port);

            const int ChunkBudget = 200;
            const int AckTimeoutPerTryMs = 800;
            const int MaxRetries = 4;

            var payload = Encoding.UTF8.GetBytes(msg);
            int id = Random.Shared.Next(1, int.MaxValue);
            int total = (payload.Length + ChunkBudget - 1) / ChunkBudget;

            for (int index = 0, offset = 0; offset < payload.Length; index++)
            {
                int len = Math.Min(ChunkBudget, payload.Length - offset);
                string b64 = Convert.ToBase64String(payload, offset, len);
                string env = $"{{\"t\":\"kc_chunk\",\"v\":1,\"id\":{id},\"n\":{total},\"i\":{index},\"pl\":\"{b64}\"}}";
                byte[] envBytes = Encoding.UTF8.GetBytes(env);

                bool acked = false;
                for (int attempt = 0; attempt < MaxRetries && !acked; attempt++)
                {
                    await sock.SendAsync(envBytes, envBytes.Length);
                    acked = await WaitAckAsync(sock, id, index, AckTimeoutPerTryMs);
                    if (!acked) Log($"retry {index} attempt {attempt + 1}");
                }
                if (!acked) throw new TimeoutException($"No ACK for chunk {index}");

                Log($"ACK {index + 1}/{total}");
                offset += len;
            }

            using var cts = new CancellationTokenSource(finalTimeoutMs);
            while (true)
            {
                var res = await sock.ReceiveAsync(cts.Token);
                if (!LooksLikeAck(res.Buffer))
                {
                    string reply = Encoding.UTF8.GetString(res.Buffer);
                    Log("Final reply: " + reply);
                    return;
                }
            }
        }
        public static void Log(string s)
        {
            //MainThread.BeginInvokeOnMainThread(() =>
            //{
            // LogLabel.Text = $"{DateTime.Now:T} {s}\n{LogLabel.Text}";
            //StatusLabel.Text = s;
            //});
            Debug.WriteLine(s);
        }

        static bool IsAckFor(byte[] buf, int id, int index)
        {
            try
            {
                using var doc = JsonDocument.Parse(buf);
                var root = doc.RootElement;
                if (root.GetProperty("t").GetString() != "kc_ack") return false;
                return root.GetProperty("id").GetInt32() == id
                    && root.GetProperty("i").GetInt32() == index;
            }
            catch { return false; }
        }

        static bool LooksLikeAck(byte[] buf)
        {
            try
            {
                using var doc = JsonDocument.Parse(buf);
                return doc.RootElement.TryGetProperty("t", out var t) && t.GetString() == "kc_ack";
            }
            catch { return false; }
        }
        static async Task<bool> WaitAckAsync(UdpClient sock, int id, int index, int timeoutMs)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                while (true)
                {
                    var res = await sock.ReceiveAsync(cts.Token);
                    if (IsAckFor(res.Buffer, id, index)) return true;
                }
            }
            catch (OperationCanceledException) { return false; }
        }

        private static async Task<string?> DiscoverDeviceIpAsync(int port, int timeoutMs, CancellationToken ct)
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                    var local = ua.Address;
                    var mask = ua.IPv4Mask;
                    if (mask == null) continue;

                    var bcast = GetBroadcast(local, mask);
                    var ip = await BroadcastProbeAsync(local, bcast, port, timeoutMs, ct);
                    if (!string.IsNullOrEmpty(ip)) return ip;
                }
            }
            return null;
        }

        private static async Task<string?> BroadcastProbeAsync(IPAddress localIp, IPAddress targetBroadcast, int port, int timeoutMs, CancellationToken ct)
        {
            try
            {
                using var sock = new UdpClient(AddressFamily.InterNetwork)
                {
                    EnableBroadcast = true
                };
                sock.Client.Bind(new IPEndPoint(localIp, 0));

                var probe = Encoding.UTF8.GetBytes("KC:DISCOVER?");
                await sock.SendAsync(probe, probe.Length, new IPEndPoint(targetBroadcast, port));

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                var res = await sock.ReceiveAsync(cts.Token);
                return res.RemoteEndPoint.Address.ToString();
            }
            catch { return null; }
        }

        private static IPAddress GetBroadcast(IPAddress addr, IPAddress mask)
        {
            var a = addr.GetAddressBytes();
            var m = mask.GetAddressBytes();
            var b = new byte[4];
            for (int i = 0; i < 4; i++) b[i] = (byte)(a[i] | ~m[i]);
            return new IPAddress(b);
        }
    }

    public static class IpCache
    {
        private const string Key = "LastBoardIp";
        public static void Store(IPAddress ip) => Preferences.Set(Key, ip.ToString());
        public static IPAddress? Load() => IPAddress.TryParse(Preferences.Get(Key, ""), out var ip) ? ip : null;
    }
}
