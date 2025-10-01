using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions;                 // ← contains GattStatus enum
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Extensions;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Encoding = System.Text.Encoding;
public static class KCBle
{
    //public static async Task<string?> SendWithAckBleAsync(
    //    string msg,
    //    ICharacteristic rxWrite,   // RX: write or write-without-response
    //    ICharacteristic txNotify,  // TX: notify
    //    int finalTimeoutMs,
    //    CancellationToken ct = default)
    //{
    //    // Try to raise MTU (supported on Android; harmless elsewhere)
    //    try
    //    {
    //        var dev = rxWrite.Service?.Device;
    //        if (dev != null)
    //            await dev.RequestMtuAsync(185);
    //    }
    //    catch { /* ignore */ }

    //    // Ensure notifications are on before sending
    //    try { await txNotify.StartUpdatesAsync(); } catch { /* already on or not supported */ }

    //    const int ChunkBudget = 120;         // raw bytes/chunk before base64 (BLE)
    //    const int AckTimeoutPerTryMs = 1200; // per-chunk ACK timeout
    //    const int MaxRetries = 4;

    //    var payload = Encoding.UTF8.GetBytes(msg);
    //    int id = Random.Shared.Next(1, int.MaxValue);
    //    int total = (payload.Length + ChunkBudget - 1) / ChunkBudget;

    //    var finalTcs = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
    //    var ackTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    //    int expectIdx = 0;
    //    object gate = new();

    //    void OnNotify(object? s, CharacteristicUpdatedEventArgs e)
    //    {
    //        var buf = e.Characteristic.Value;
    //        if (buf == null || buf.Length == 0) return;

    //        if (LooksLikeAck(buf))
    //        {
    //            if (IsAckFor(buf, id, expectIdx))
    //            {
    //                lock (gate) ackTcs.TrySetResult(true);
    //            }
    //            return;
    //        }

    //        // Non-ACK (final reply), capture it
    //        lock (gate) finalTcs.TrySetResult(buf);
    //    }

    //    txNotify.ValueUpdated += OnNotify;

    //    try
    //    {

    //        for (int index = 0, offset = 0; offset < payload.Length; index++)
    //        {
    //            int len = Math.Min(ChunkBudget, payload.Length - offset);
    //            string b64 = Convert.ToBase64String(payload, offset, len);
    //            string env = $"{{\"t\":\"kc_chunk\",\"v\":1,\"id\":{id},\"n\":{total},\"i\":{index},\"pl\":\"{b64}\"}}";
    //            byte[] envBytes = Encoding.UTF8.GetBytes(env);

    //            // new ACK waiter per chunk
    //            lock (gate)
    //            {
    //                expectIdx = index;
    //                ackTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    //            }
    //            rxWrite.WriteType = CharacteristicWriteType.WithoutResponse;

    //            // 2) make sure the characteristic is really writable
    //            if (!rxWrite.CanWrite)
    //                throw new InvalidOperationException("RX characteristic not writable yet.");

    //            // 3) retry on GATT 133/128 once per chunk
    //            bool okWrite = false;
    //            bool acked = false;
    //            //bool okWrite = false;
    //            rxWrite.WriteType = CharacteristicWriteType.WithoutResponse;

    //            var wrote = false;
    //            for (int attempt = 0; attempt < 2 && !wrote; attempt++)
    //            {
    //                try
    //                {
    //                    await rxWrite.WriteAsync(envBytes, ct);     // 1-arg + CT works in v3.x
    //                    wrote = true;
    //                }
    //                catch (Exception ex) when (attempt == 0)
    //                {
    //                    // first failure: wait briefly and retry once
    //                    Debug.WriteLine($"[BLE] write failed ({ex.Message}) – retrying…");
    //                    await Task.Delay(120, ct);
    //                }
    //            }

    //            if (!wrote) throw new IOException("BLE write failed twice.");
    //            //for (int attempt = 0; attempt < MaxRetries && !acked; attempt++)
    //            //{
    //            //    ///await rxWrite.wri(envBytes); // works on all platforms
    //            //    ///

    //            //    //await rx.WriteWithoutResponseAsync(envBytes);

    //            //    //await rxWrite.WriteAsync(  envBytes//,   //CharacteristicWriteType.WithoutResponse,                                                     ,ct);

    //            //    //await rxWrite.WriteAsync(        envBytes,        CharacteristicWriteType.WithoutResponse,   // ← **exact enum value**        ct);

    //            //    rxWrite!.WriteType = CharacteristicWriteType.WithoutResponse;

    //            //    // … then the plain call succeeds on every platform
    //            //    await rxWrite.WriteAsync(envBytes, ct);   // ct =
    //            //    var done = await Task.WhenAny(ackTcs.Task, Task.Delay(AckTimeoutPerTryMs, ct));
    //            //    acked = (done == ackTcs.Task) && ackTcs.Task.Result;

    //            //    if (!acked && attempt + 1 < MaxRetries)
    //            //        await Task.Delay(40, ct); // gentle retry pacing
    //            //}

    //            if (!acked) throw new TimeoutException($"No ACK for chunk {index}");

    //            offset += len;
    //            await Task.Delay(15, ct); // small pacing between chunks
    //        }

    //        // If final already captured during ACK waits, return it
    //        if (finalTcs.Task.IsCompleted)
    //        {
    //            var buf = await finalTcs.Task;
    //            return buf == null ? null : Encoding.UTF8.GetString(buf);
    //        }

    //        // Otherwise wait now (drop late ACKs)
    //        var doneFinal = await Task.WhenAny(finalTcs.Task, Task.Delay(finalTimeoutMs, ct));
    //        if (doneFinal != finalTcs.Task) return null;

    //        var fin = await finalTcs.Task;
    //        return fin == null ? null : Encoding.UTF8.GetString(fin);
    //    }
    //    finally
    //    {
    //        txNotify.ValueUpdated -= OnNotify;
    //        // You can keep notifications on for reuse; or stop:
    //        // try { await txNotify.StopUpdatesAsync(); } catch { }
    //    }
    //}
    public static Task<string?> SendWithAckBleAsync(
    string msg,
    ICharacteristic rxWrite,      // BLE RX characteristic (write)
    ICharacteristic txNotify,     // BLE TX characteristic (notify)
    int finalTimeoutMs,
    CancellationToken ct = default)
    {
        // Run the heavy work off the UI thread
        return Task.Run(async () =>
        {
            // ---- MTU and notifications -----------------------------------------
            try
            {
                var dev = rxWrite.Service?.Device;
                if (dev != null) await dev.RequestMtuAsync(185);
            }
            catch { /* ignore */ }

            try { await txNotify.StartUpdatesAsync(); } catch { /* already on */ }

            // ---- constants ------------------------------------------------------
            const int ChunkBudget = 35;   // raw bytes/chunk before b64
            const int AckTimeoutPerTryMs = 1200;  // wait for kc_ack
            const int MaxRetriesPerChunk = 2;     // 1 try + 1 retry

            // ---- prepare --------------------------------------------------------
            var payload = Encoding.UTF8.GetBytes(msg);
            int id = Random.Shared.Next(1, int.MaxValue);
            int total = (payload.Length + ChunkBudget - 1) / ChunkBudget;

            var finalTcs = new TaskCompletionSource<byte[]?>(
                               TaskCreationOptions.RunContinuationsAsynchronously);
            var ackTcs = new TaskCompletionSource<bool>(
                               TaskCreationOptions.RunContinuationsAsynchronously);
            int expectIdx = 0;
            object gate = new();

            void OnNotify(object? s, CharacteristicUpdatedEventArgs e)
            {
                var buf = e.Characteristic.Value;
                if (buf is null || buf.Length == 0) return;

                if (LooksLikeAck(buf) && IsAckFor(buf, id, expectIdx))
                {
                    lock (gate) ackTcs.TrySetResult(true);
                    return;
                }

                // final reply
                lock (gate) finalTcs.TrySetResult(buf);
            }
            txNotify.ValueUpdated += OnNotify;

            try
            {
                // -----------------------------------------------------------------
                //                       send every chunk
                // -----------------------------------------------------------------
                rxWrite.WriteType = CharacteristicWriteType.WithoutResponse;
                if (!rxWrite.CanWrite)
                    throw new InvalidOperationException("RX characteristic not writable.");

                for (int index = 0, offset = 0; offset < payload.Length; index++)
                {
                    int len = Math.Min(ChunkBudget, payload.Length - offset);
                    string b64 =Convert.ToBase64String(payload, offset, len);
                    //string env = $"{{\"t\":\"kc_chunk\",\"v\":1,\"id\":{id},\"n\":{total},\"i\":{index},\"pl\":\"{payload}\"}}";
                    string env = $"{{\"t\":\"kc_chunk\",\"v\":1,\"id\":{id},\"n\":{total},\"i\":{index},\"pl\":\"{b64}\"}}";
                    byte[] bytes = Encoding.UTF8.GetBytes(env);

                    // new ACK waiter
                    lock (gate)
                    {
                        expectIdx = index;
                        ackTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    }

                    bool wrote = false;
                    for (int at = 0; at < MaxRetriesPerChunk && !wrote; at++)
                    {
                        try
                        {
                            rxWrite.WriteType = CharacteristicWriteType.WithoutResponse;   // set once
                            await rxWrite.WriteAsync(bytes, ct);
                            //await rxWrite.WriteAsync(bytes, ct);   // 1-arg overload in v3.x
                            wrote = true;
                        }
                        catch when (at == 0)         // first failure → short retry once
                        {
                            await Task.Delay(120, ct);
                        }
                    }
                    if (!wrote) throw new IOException("BLE write failed twice.");

                    // wait ACK
                    var done = await Task.WhenAny(ackTcs.Task,
                                                  Task.Delay(AckTimeoutPerTryMs, ct));
                    if (done != ackTcs.Task || !ackTcs.Task.Result)
                        throw new TimeoutException($"No ACK for chunk {index}");

                    offset += len;
                    await Task.Delay(15, ct);       // gentle pacing
                }

                // ------------------ wait for final reply -------------------------
                if (!finalTcs.Task.IsCompleted)
                {
                    var done = await Task.WhenAny(finalTcs.Task,
                                                  Task.Delay(finalTimeoutMs, ct));
                    if (done != finalTcs.Task) return null;
                }

                var fin = await finalTcs.Task.ConfigureAwait(false);
                return fin is null ? null : Encoding.UTF8.GetString(fin);
            }
            finally
            {
                txNotify.ValueUpdated -= OnNotify;
            }
        });
    }

    static bool LooksLikeAck(byte[] jsonBytes)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonBytes);
            return doc.RootElement.TryGetProperty("t", out var t) && t.GetString() == "kc_ack";
        }
        catch { return false; }
    }

    static bool IsAckFor(byte[] jsonBytes, int id, int index)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;
            if (root.GetProperty("t").GetString() != "kc_ack") return false;
            return root.GetProperty("id").GetInt32() == id
                && root.GetProperty("i").GetInt32() == index;
        }
        catch { return false; }
    }
}