using System.Net;




public static class IpCache
{
    private static readonly object _lock = new();
    private static IPAddress? _ip;

    public static IPAddress? Load() { lock (_lock) return _ip; }
    public static void Store(IPAddress ip) { lock (_lock) _ip = ip; }
    public static void Clear() { lock (_lock) _ip = null; }
}


namespace KeyCatcher_acc.services
{
    // File: services/UdpHelpers.cs
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;

    public static class UdpHelpers
    {
        // ---------------------------------------------------------------------
        // 1) Simple send + one reply (used for "ping" check)
        // ---------------------------------------------------------------------
        public static async Task<string?> SendAndWaitPinnedAsync(
            string msg, string host, int port, int timeoutMs, CancellationToken ct = default)
        {
            try
            {
                if (!IPAddress.TryParse(host, out var ip))
                {
                    var addrs = await Dns.GetHostAddressesAsync(host, ct);
                    ip = Array.Find(addrs, a => a.AddressFamily == AddressFamily.InterNetwork) ?? addrs[0];
                }

                using var sock = new UdpClient(AddressFamily.InterNetwork);
                sock.Connect(new IPEndPoint(ip, port));

                var payload = Encoding.UTF8.GetBytes(msg);
                await sock.SendAsync(payload, payload.Length);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                var res = await sock.ReceiveAsync(cts.Token);
                return Encoding.UTF8.GetString(res.Buffer);
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------------------
        // 2) Robust broadcast discovery from EVERY IPv4 NIC
        //    Returns the first responder's IP (KC:HELLO or PONG)
        // ---------------------------------------------------------------------
        public static async Task<string?> DiscoverDeviceIpAsync(
  int port, int timeoutMs, CancellationToken ct = default)
        {
            var sockets = new List<Socket>();
            var tasks = new List<Task<(bool ok, IPEndPoint? from, byte[] data)>>();

            static IPAddress BroadcastOf(IPAddress ip, IPAddress mask)
            {
                var a = ip.GetAddressBytes();
                var m = mask.GetAddressBytes();
                var b = new byte[4];
                for (int i = 0; i < 4; i++)
                    b[i] = (byte)((a[i] & m[i]) | (~m[i] & 0xFF));   // byte-wise broadcast (endian-safe)
                return new IPAddress(b);
            }

            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                    var props = ni.GetIPProperties();
                    foreach (var ua in props.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;   // IPv4 only
                        if (ua.IPv4Mask is null) continue;

                        var localIp = ua.Address;
                        var directedB = BroadcastOf(localIp, ua.IPv4Mask);

                        var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                        s.Bind(new IPEndPoint(localIp, 0));
                        sockets.Add(s);

                        var probe = Encoding.ASCII.GetBytes("KC:DISCOVER?");
                        // Send on this NIC’s directed broadcast…
                        _ = s.SendToAsync(new ArraySegment<byte>(probe), SocketFlags.None, new IPEndPoint(directedB, port));
                        // …and also limited broadcast (some APs require this)
                        _ = s.SendToAsync(new ArraySegment<byte>(probe), SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));

                        tasks.Add(ReceiveOneAsync(s, timeoutMs, ct));
                    }
                }

                if (tasks.Count == 0) return null;

                while (tasks.Count > 0)
                {
                    var done = await Task.WhenAny(tasks);
                    tasks.Remove(done);

                    var (ok, from, data) = await done.ConfigureAwait(false);
                    if (!ok || from is null || data.Length == 0) continue;

                    var txt = Encoding.UTF8.GetString(data);
                    if (txt.StartsWith("KC:HELLO", StringComparison.OrdinalIgnoreCase) ||
                        txt.StartsWith("PONG", StringComparison.OrdinalIgnoreCase))
                        return from.Address.ToString(); // <- device IP
                }
                return null;
            }
            catch { return null; }
            finally { foreach (var s in sockets) try { s.Dispose(); } catch { } }
        }

        //private static async Task<(bool ok, IPEndPoint? from, byte[] data)> ReceiveOneAsync(


        private static async Task<(bool ok, IPEndPoint? from, byte[] data)> ReceiveOneAsync(
            Socket s, int timeoutMs, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            try
            {
                var buffer = new byte[512];
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
#if NET8_0_OR_GREATER
                var res = await s.ReceiveFromAsync(buffer, SocketFlags.None, from, cts.Token);
                var taken = res.ReceivedBytes;
                from = res.RemoteEndPoint;
#else
        var t = Task.Factory.FromAsync(
            (cb, state) => s.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref from, cb, state),
            ar => s.EndReceiveFrom(ar, ref from), null);
        var done = await Task.WhenAny(t, Task.Delay(timeoutMs, cts.Token));
        if (done != t) return (false, null, Array.Empty<byte>());
        var taken = t.Result;
#endif
                var data = new byte[taken];
                Array.Copy(buffer, data, taken);
                return (true, (IPEndPoint)from, data);
            }
            catch { return (false, null, Array.Empty<byte>()); }
        }
        // ---------------------------------------------------------------------
        // 3) Reliable chunked sender (kc_chunk + kc_ack + final reply)
        // ---------------------------------------------------------------------
        public static async Task<string?> SendWithAckAsync(
                string msg, string host, int port, int finalTimeoutMs, CancellationToken ct = default)
        {
            try
            {
                if (!IPAddress.TryParse(host, out var ip))
                {
                    var addrs = await Dns.GetHostAddressesAsync(host, ct);
                    ip = Array.Find(addrs, a => a.AddressFamily == AddressFamily.InterNetwork) ?? addrs[0];
                }

                using var sock = new UdpClient(AddressFamily.InterNetwork);
                sock.Connect(new IPEndPoint(ip, port));

                const int ChunkBudget = 200;        // raw bytes per chunk (before base64)
                const int AckTimeoutPerTryMs = 800; // wait for kc_ack per chunk
                const int MaxRetries = 4;

                var payload = Encoding.UTF8.GetBytes(msg);
                int id = Random.Shared.Next(1, int.MaxValue);
                int total = (payload.Length + ChunkBudget - 1) / ChunkBudget;

                byte[]? capturedFinal = null;

                for (int index = 0, offset = 0; offset < payload.Length; index++)
                {
                    int len = Math.Min(ChunkBudget, payload.Length - offset);
                    string b64 = Convert.ToBase64String(payload, offset, len);
                    string env = $"{{\"t\":\"kc_chunk\",\"v\":1,\"id\":{id},\"n\":{total},\"i\":{index},\"pl\":\"{b64}\"}}";
                    var envBytes = Encoding.UTF8.GetBytes(env);

                    bool acked = false;
                    for (int attempt = 0; attempt < MaxRetries && !acked; attempt++)
                    {
                        await sock.SendAsync(envBytes, envBytes.Length);

                        var (gotAck, nonAck) = await WaitAckAsync(sock, id, index, AckTimeoutPerTryMs, ct);
                        if (nonAck is not null) capturedFinal = nonAck; // final might come early
                        acked = gotAck;

                        if (!acked && attempt + 1 < MaxRetries)
                            await Task.Delay(20, ct); // gentle pacing
                    }

                    if (!acked) return null;
                    offset += len;
                    await Task.Delay(15, ct);         // keep ESP happy
                }

                // If final reply already captured, use it
                if (capturedFinal is not null && !LooksLikeAck(capturedFinal))
                    return Encoding.UTF8.GetString(capturedFinal);

                // Otherwise, wait for it now (skip stray acks)
                var done = await Task.WhenAny(sock.ReceiveAsync(), Task.Delay(finalTimeoutMs, ct));
                if (done is not Task<UdpReceiveResult> t1) return null;
                var res = await t1;
                if (!LooksLikeAck(res.Buffer))
                    return Encoding.UTF8.GetString(res.Buffer);

                // If we still got an ACK, give a short second chance
                var done2 = await Task.WhenAny(sock.ReceiveAsync(), Task.Delay(1000, ct));
                if (done2 is Task<UdpReceiveResult> t2)
                {
                    var res2 = await t2;
                    if (!LooksLikeAck(res2.Buffer))
                        return Encoding.UTF8.GetString(res2.Buffer);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ------------------- ACK internals -------------------
        private static async Task<(bool acked, byte[]? nonAck)> WaitAckAsync(
            UdpClient sock, int id, int index, int timeoutMs, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            try
            {
                while (true)
                {
                    var res = await sock.ReceiveAsync(cts.Token);
                    var buf = res.Buffer;

                    if (IsAckFor(buf, id, index)) return (true, null);
                    if (!LooksLikeAck(buf)) return (false, buf); // probably final reply
                }
            }
            catch (OperationCanceledException)
            {
                return (false, null);
            }
        }

        private static bool LooksLikeAck(byte[] jsonBytes)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonBytes);
                return doc.RootElement.TryGetProperty("t", out var t) &&
                       t.GetString() == "kc_ack";
            }
            catch { return false; }
        }

        private static bool IsAckFor(byte[] jsonBytes, int id, int index)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonBytes);
                var root = doc.RootElement;
                if (root.GetProperty("t").GetString() != "kc_ack") return false;
                return root.GetProperty("id").GetInt32() == id &&
                       root.GetProperty("i").GetInt32() == index;
            }
            catch { return false; }
        }
    }
}


//}

