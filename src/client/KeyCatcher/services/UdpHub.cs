using KeyCatcher_acc.services;
using System.Net;
using System.Net.Sockets;
using Encoding = System.Text.Encoding;

public sealed class UdpHub : IDisposable
{
    private const int DevicePort = 4210;

    private readonly object _lock = new();
    private IPEndPoint? _lastGood;

    public IPEndPoint? LastEndpoint { get { lock (_lock) return _lastGood; } }
    public void SetLastEndpoint(IPEndPoint ep)
    {
        if (ep.Port != DevicePort) ep = new IPEndPoint(ep.Address, DevicePort);
        lock (_lock) _lastGood = ep;
    }

    public void Dispose() { }

    public async Task<bool> SendTextReliableAsync(string text, CancellationToken ct = default)
    {
        // 1) Make sure we have a live endpoint (ping/discover)
        var ep = await EnsureEndpointAsync(ct).ConfigureAwait(false);
        if (ep is null) return false;

        // 2) Chunk + send (safe UDP payload size)
        foreach (var chunk in ChunkUtf8(text, 200))
        {
            var ok = await SendAsync(ep, chunk.ToArray(), ct).ConfigureAwait(false);
            if (!ok)
            {
                // one quick rediscovery then retry once
                ep = await EnsureEndpointAsync(ct, preferQuick: true).ConfigureAwait(false);
                if (ep is null) return false;
                ok = await SendAsync(ep, chunk.ToArray(), ct).ConfigureAwait(false);
                if (!ok) return false;
            }
            await Task.Delay(8, ct).ConfigureAwait(false); // be gentle
        }
        return true;
    }

    // Finds a working device IP. Order: last-known → LAN broadcast → AP fallback.
    private async Task<IPEndPoint?> EnsureEndpointAsync(CancellationToken ct, bool preferQuick = false)
    {
        // 0) Fast path: does our cached endpoint still answer a ping?
        var cached = LastEndpoint;
        if (cached != null && await PingAsync(cached, ct, quick: true).ConfigureAwait(false))
            return cached;

        // 1) LAN broadcast discovery
        var found = await UdpHelpers.DiscoverDeviceIpAsync(DevicePort, preferQuick ? 700 : 1300, ct)
                                    .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(found))
        {
            var ep = new IPEndPoint(IPAddress.Parse(found!), DevicePort);
            SetLastEndpoint(ep);
            return ep;
        }

        // 2) Soft-AP fallback (192.168.4.1): ping first
        var ap = new IPEndPoint(IPAddress.Parse("192.168.4.1"), DevicePort);
        if (await PingAsync(ap, ct, quick: true).ConfigureAwait(false))
        {
            SetLastEndpoint(ap);
            return ap;
        }

        return null;
    }

    // --- helpers ---

    private static async Task<bool> PingAsync(IPEndPoint target, CancellationToken ct, bool quick = false)
    {
        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.EnableBroadcast = true;

        var recvTask = udp.ReceiveAsync(ct);
        var ping = Encoding.ASCII.GetBytes("ping");
        await udp.SendAsync(ping, ping.Length, target).ConfigureAwait(false);

        var timeout = Task.Delay(quick ? 300 : 600, ct);
        var first = await Task.WhenAny(recvTask.AsTask(), timeout).ConfigureAwait(false);
        if (first != recvTask.AsTask()) return false;

        try
        {
            var res = await recvTask.ConfigureAwait(false);
            return res.Buffer.Length == 4 && res.Buffer[0] == (byte)'p' && res.Buffer[1] == (byte)'o'
                   && res.Buffer[2] == (byte)'n' && res.Buffer[3] == (byte)'g';
        }
        catch { return false; }
    }

    private static async Task<bool> SendAsync(IPEndPoint to, byte[] buffer, CancellationToken ct)
    {
        try
        {
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            await udp.SendAsync(buffer, buffer.Length, to).ConfigureAwait(false);
            return true;
        }
        catch { return false; }
    }

    private static ReadOnlyMemory<byte>[] ChunkUtf8(string text, int size)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<ReadOnlyMemory<byte>>();
        var bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length <= size) return new[] { (ReadOnlyMemory<byte>)bytes };
        int count = (bytes.Length + size - 1) / size, off = 0;
        var result = new ReadOnlyMemory<byte>[count];
        for (int i = 0; i < count; i++)
        {
            int len = Math.Min(size, bytes.Length - off);
            result[i] = new ReadOnlyMemory<byte>(bytes, off, len);
            off += len;
        }
        return result;
    }
}