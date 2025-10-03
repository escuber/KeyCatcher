using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using KeyCatcher.Popups;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;
public sealed class KeyCatcherBleService //: IKeyCatcherCommService
{
    static readonly Guid SVC_UUID = Guid.Parse("0000AAAA-0000-1000-8000-00805F9B34FB");
    static readonly Guid RX_UUID = Guid.Parse("0000BBBB-0000-1000-8000-00805F9B34FB");
    private readonly Guid TX_UUID = Guid.Parse("0000BBBC-0000-1000-8000-00805F9B34FB");
    private readonly IAdapter _adapter;
    private IDevice? _device;
    private ICharacteristic? _characteristic;
    private CancellationTokenSource? _healthCts;
    private IDevice? _dev;
    private ICharacteristic? _rx;         // Characteristic for "Write" (used to send data to device)
    private ICharacteristic? _tx;         // Characteristic for "Notify" (used to receive data from device)

    public bool IsConnected => _device?.State == DeviceState.Connected;
    public event EventHandler<bool>? ConnectedChanged;
    BleProgressPopup? _popup;

    public KeyCatcherBleService(IAdapter adapter)
    {
        _adapter = adapter;
    }
    private async Task<ICharacteristic?> FindCharacteristicAsync(IDevice device)
    {
        try
        {
            // Discover all services
            var services = await device.GetServicesAsync();
            foreach (var svc in services)
            {
                // You may want to check for a specific Service UUID here
                // if your firmware advertises one, e.g.:
                // if (svc.Id == Guid.Parse("YOUR_SERVICE_GUID")) { ... }

                var characteristics = await svc.GetCharacteristicsAsync();
                foreach (var ch in characteristics)
                {
                    // Look for one that supports write
                    if (ch.CanWrite)
                    {
                        Debug.WriteLine($"[BLE] Found writable characteristic {ch.Id}");
                        return ch;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BLE] FindCharacteristicAsync error: {ex.Message}");
        }

        Debug.WriteLine("[BLE] No writable characteristic found!");
        return null;
    }

    public async Task<bool> ProbeAsync()
    {
        try
        {
            if (_device == null || !_adapter.ConnectedDevices.Contains(_device))
            {
                return false; // Not connected
            }

            // Optionally, do a lightweight write/read to verify it’s still alive
            var ch = await FindCharacteristicAsync(_device);
            if (ch == null) return false;

            var payload = Encoding.UTF8.GetBytes("ping");
            await ch.WriteAsync(payload);

            // If you want, wait for notification or ignore (depends on firmware)
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BLE] Probe failed: {ex.Message}");
            return false;
        }
    }
    public async Task<bool> ConnectAsync()
    {
        if (IsConnected) return true;

        IDevice? found = null;

        void Handler(object? s, DeviceEventArgs ev)
        {
            if (!string.IsNullOrEmpty(ev.Device.Name) &&
                ev.Device.Name.Contains("KeyCatcher", StringComparison.OrdinalIgnoreCase))
            {
                found ??= ev.Device;
            }
        }

        _adapter.DeviceDiscovered += Handler;
        await _adapter.StartScanningForDevicesAsync();
        _adapter.DeviceDiscovered -= Handler;

        if (found == null)
        {
            Debug.WriteLine("[BLE] No KeyCatcher found.");
            return false;
        }

        try
        {
            await _adapter.ConnectToDeviceAsync(found);
            if (found.State == DeviceState.Connected)
            {
                _device = found;

                // Discover service + chars
                var services = await _device.GetServicesAsync();
                var svc = services.FirstOrDefault(s => s.Id == SVC_UUID);
                if (svc == null)
                {
                    Debug.WriteLine("[BLE] Service not found");
                    return false;
                }

                var chars = await svc.GetCharacteristicsAsync();
                _rx = chars.FirstOrDefault(c => c.Id == RX_UUID);
                _tx = chars.FirstOrDefault(c => c.Id == TX_UUID);

                if (_rx == null || _tx == null)
                {
                    Debug.WriteLine("[BLE] RX/TX characteristics not found");
                    return false;
                }

                ConnectedChanged?.Invoke(this, true);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BLE] ConnectAsync error: {ex.Message}");
        }

        return false;
    }
    public async Task DisconnectAsync()
    {
        StopHealthLoop();

        if (_device != null)
        {
            try
            {
                await _adapter.DisconnectDeviceAsync(_device);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BLE] Disconnect error: {ex.Message}");
            }
            finally
            {
                _device = null;
                _characteristic = null;
                ConnectedChanged?.Invoke(this, false);
            }
        }
    }
    void Log(string s)
    {
        //MainThread.BeginInvokeOnMainThread(() =>
        //{
           // LogLabel.Text = $"{DateTime.Now:T} {s}\n{LogLabel.Text}";
            //StatusLabel.Text = s;
        //});
        Debug.WriteLine(s);
    }

    //void ShowBlePopup(string status)
    //{
    //    DismissBlePopup();
    //    _popup = new BleProgressPopup();
    //    _popup.StatusText = status; // Adjust if your property is "Status"
    //    MainThread.BeginInvokeOnMainThread(() =>
    //    {
    //        Application.Current?.MainPage?.ShowPopup(_popup);
    //    });
    //}
    
    public event EventHandler<IDevice>? DeviceDiscovered;
    public event EventHandler<string>? Notified;
    public event EventHandler<string>? ErrorOccurred;

    private bool _isConfigReceiving;
    private readonly StringBuilder _configBuffer = new();
    public async Task<string?> GetConfigAsync()
    {



        var jcoonfig =await KeyCatcherBleService.FindAndGetConfigAsync(CrossBluetoothLE.Current, CrossBluetoothLE.Current.Adapter);
       // if (jcoonfig != null)
            return jcoonfig;        






        ShowBlePopup("Scanning for device...");
        try
        {
            // === 1. Scan and Connect ===
            IDevice? device = null;
            void Handler(object? s, DeviceEventArgs ev)
            {
                if (!string.IsNullOrEmpty(ev.Device.Name) && ev.Device.Name.Contains("KeyCatcher"))
                    device ??= ev.Device;
            }
            _adapter.DeviceDiscovered += Handler;
            await _adapter.StartScanningForDevicesAsync();
            _adapter.DeviceDiscovered -= Handler;
            if (device == null)
            {
                ShowBlePopup("Device not found.");
                await Task.Delay(1000);
                return null;
            }

            ShowBlePopup($"Found {device.Name}, connecting...");
            await _adapter.ConnectToDeviceAsync(device);
            if (device.State != DeviceState.Connected)
            {
                ShowBlePopup("Device not connected!");
                await Task.Delay(800);
                return null;
            }

            // === 2. Discover Services/Chars ===
            ShowBlePopup("Discovering services...");
            var svc = (await device.GetServicesAsync()).FirstOrDefault(s => s.Id == SVC_UUID);
            if (svc == null)
            {
                ShowBlePopup("Service not found.");
                await Task.Delay(800);
                return null;
            }
            var chars = await svc.GetCharacteristicsAsync();
            var rx = chars.FirstOrDefault(c => c.Id == RX_UUID);
            var tx = chars.FirstOrDefault(c => c.Id == TX_UUID);


            if (rx == null || tx == null)
            {
                var ok = await ConnectAsync();
                if (!ok) return null;
            }

            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            void myHandler(object? s, string msg)
            {
                Console.WriteLine($"[DOTNET] Chunk: [{msg}]");
                if (msg.StartsWith("CONFIG_START:"))
                {
                    _configBuffer.Clear();
                    _isConfigReceiving = true;
                }
                else if (_isConfigReceiving && msg.StartsWith("C_P:"))
                {
                    _configBuffer.Append(msg.Substring("C_P:".Length));
                    Console.WriteLine($"[DOTNET] config buffer:{_configBuffer}");
                }
                else if (_isConfigReceiving && msg.StartsWith("CONFIG_END"))
                {
                    _isConfigReceiving = false;
                    tcs.TrySetResult(_configBuffer.ToString());
                }
            }

            try
            {
                Notified -= myHandler; // Remove, just in case
                Notified += myHandler;
                Console.WriteLine("Subscribed Handler");

                var bytes = System.Text.Encoding.UTF8.GetBytes("get_config");
                await rx.WriteAsync(bytes);

                using var cts = new CancellationTokenSource(5000);
                await using var reg = cts.Token.Register(() => tcs.TrySetResult(null));
                var rslt =   await tcs.Task.ConfigureAwait(false);

                var v2 = rslt;
                return v2;

            }
            finally
            {
                Notified -= myHandler;
                Console.WriteLine("Unsubscribed Handler");
                DismissBlePopup();
            }

        }

        catch (Exception ex)
        {
            ShowBlePopup("BLE error: " + ex.Message);
            await Task.Delay(1600);
            return "false";
        }
        finally
        {
            DismissBlePopup();
        }
    }


    public static async Task<string?> FindAndGetConfigAsync(
    IBluetoothLE ble,
    IAdapter adapter,
    int scanTimeoutMs = 12000,
    int opTimeoutMs = 12000)
    {
        // UUIDs
        
        var svcGuid = Guid.Parse("0000AAAA-0000-1000-8000-00805F9B34FB");
        var rxGuid = Guid.Parse("0000BBBB-0000-1000-8000-00805F9B34FB");
        var txGuid = Guid.Parse("0000BBBC-0000-1000-8000-00805F9B34FB");
        IDevice? device = null;
        ICharacteristic? rx = null, tx = null;
        var configBuffer = new StringBuilder();
        var isConfigReceiving = false;
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Handler to collect chunked config
        void OnConfig(object? s, CharacteristicUpdatedEventArgs e)
        {
            var msg = Encoding.UTF8.GetString(e.Characteristic.Value ?? Array.Empty<byte>());
            if (msg.StartsWith("CONFIG_START:"))
            {
                configBuffer.Clear();
                isConfigReceiving = true;
            }
            else if (isConfigReceiving && msg.StartsWith("C_P:"))
            {
                configBuffer.Append(msg.Substring("C_P:".Length));
            }
            else if (isConfigReceiving && msg.StartsWith("CONFIG_END"))
            {
                isConfigReceiving = false;
                tcs.TrySetResult(configBuffer.ToString());
            }
        }

        try
        {
            // 1. Scan for KeyCatcher
            device = await ScanForDeviceAsync(adapter, svcGuid, scanTimeoutMs);
            if (device == null)
                return null;

            // 2. Connect & discover services
            await adapter.ConnectToDeviceAsync(device, new ConnectParameters(autoConnect: false, forceBleTransport: true));
            var svc = await device.GetServiceAsync(svcGuid);
            if (svc == null) return null;

            rx = await svc.GetCharacteristicAsync(rxGuid);
            tx = await svc.GetCharacteristicAsync(txGuid);
            if (rx == null || tx == null) return null;

            tx.ValueUpdated += OnConfig;
            await tx.StartUpdatesAsync();

            // 3. Request config
            await rx.WriteAsync(Encoding.UTF8.GetBytes("get_config"));

            // 4. Wait for all chunks, or timeout
            using var cts = new CancellationTokenSource(opTimeoutMs);
            await using var _ = cts.Token.Register(() => tcs.TrySetResult(null));
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            if (tx != null)
            {
                tx.ValueUpdated -= OnConfig;
                await tx.StopUpdatesAsync();
            }
            if (device != null)
            {
                try { await adapter.DisconnectDeviceAsync(device); } catch { }
            }
        }
    }

    // Helper to scan for device by service or name
    private static async Task<IDevice?> ScanForDeviceAsync(IAdapter adapter, Guid svcGuid, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<IDevice?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? s, DeviceEventArgs e)
        {
            if (e.Device.Name?.Contains("KeyCatcher", StringComparison.OrdinalIgnoreCase) == true)
                tcs.TrySetResult(e.Device);
        }
        adapter.DeviceDiscovered += Handler;
        try
        {
            await adapter.StartScanningForDevicesAsync(new[] { svcGuid });
            var done = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            await adapter.StopScanningForDevicesAsync();
            return (done == tcs.Task) ? tcs.Task.Result : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            adapter.DeviceDiscovered -= Handler;
            try { await adapter.StopScanningForDevicesAsync(); } catch { }
        }
    }





    public async Task<bool> SendConfigAsync(string configJson)
    {
        ShowBlePopup("Sending config...");
        try
        {
            SendAsync(configJson);
            return true;
        }
        catch (Exception ex)
        {
            ShowBlePopup("BLE error: " + ex.Message);
            await Task.Delay(1600);
            return false;
        }
        finally
        {
            DismissBlePopup();
        }
    }

    private void ShowBlePopup(string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_popup == null)
            {
                _popup = new BleProgressPopup { StatusText = status };
                Application.Current?.MainPage?.ShowPopupAsync(_popup);
            }
            else
            {
                _popup.StatusText = status;
            }
        });
    }
    public event Action<string>? BleStatusChanged;
    async Task DoFullSend(string txtMsg)
    {
        ///string text = TextEntry.Text ?? "";
      ////  await Task.Delay(5 * 1000);
        Console.WriteLine("Chunked long...");
        string text = txtMsg;// new string('A', 50);// + "<<END>>";
        //await SendWithAckAsync(longMsg, ip, port, 8000);
        const int maxRounds = 3;
        const int maxAttempts = 5;

        for (int round = 0; round < maxRounds; round++)
        {
            try
            {
                ShowBlePopup(round == 0 ? "Scanning for device…" : $"Retrying BLE round {round + 1}…");
                Log(round == 0 ? "Scanning for device..." : $"Retrying BLE round {round + 1}...");
                _dev = null;

                void Handler(object? s, DeviceEventArgs ev)
                {
                    if (ev.Device.Name != null && ev.Device.Name.Contains("KeyCatcher", StringComparison.OrdinalIgnoreCase))
                        _dev ??= ev.Device;
                }
                _adapter.DeviceDiscovered += Handler;
                await _adapter.StartScanningForDevicesAsync();
                _adapter.DeviceDiscovered -= Handler;
                if (_dev == null)
                {
                    ShowBlePopup("Device not found.");
                    Log("Device not found.");
                    await Task.Delay(1200);
                    break;
                }
                ShowBlePopup($"Found device: {_dev.Name}\nConnecting…");
                Log($"Found device: {_dev.Name}");

                await _adapter.ConnectToDeviceAsync(_dev);
                Log("Connected.");
                if (_dev.State != DeviceState.Connected)
                {
                    ShowBlePopup("Device not actually connected!");
                    Log("Device not actually connected!");
                    await Task.Delay(900);
                    continue; // Try again
                }

                // Service and char discovery with retries
                ShowBlePopup("Locating service/characteristics…");
                IService? svc = null;
                ICharacteristic? rx = null;
                ICharacteristic? tx = null;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        var services = (await _dev.GetServicesAsync()).ToList();
                        svc = services.FirstOrDefault(s => s.Id == SVC_UUID);
                        if (svc == null)
                        {
                            ShowBlePopup($"Attempt {attempt + 1}: Service not found, retrying…");
                            Log($"Attempt {attempt + 1}: Service not found, retrying...");
                            await Task.Delay(350);
                            continue;
                        }
                        var chars = (await svc.GetCharacteristicsAsync()).ToList();
                        rx = chars.FirstOrDefault(c => c.Id == RX_UUID);
                        tx = chars.FirstOrDefault(c => c.Id == TX_UUID);
                        if (rx == null || tx == null)
                        {
                            ShowBlePopup($"Attempt {attempt + 1}: {(rx == null ? "RX" : "TX")} char not found, retrying…");
                            Log($"Attempt {attempt + 1}: {(rx == null ? "RX" : "TX")} char not found, retrying...");
                            await Task.Delay(250);
                            continue;
                        }
                        break; // Success! BOTH found
                    }
                    catch (ObjectDisposedException ex)
                    {
                        ShowBlePopup($"Attempt {attempt + 1}: Object disposed. Will restart round.");
                        Log($"Attempt {attempt + 1}: Object disposed: {ex.Message}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ShowBlePopup($"Attempt {attempt + 1}: Discovery error, retrying…");
                        Log($"Attempt {attempt + 1}: Discovery error: {ex.Message}");
                        await Task.Delay(250);
                    }
                }
                if (svc == null || rx == null || tx == null)
                {
                    ShowBlePopup("Service/Char not found after retries. Restarting round…");
                    Log("Service/Char not found after retries in this round, restarting round...");
                    throw new ObjectDisposedException("BLE session invalid, need full reset.");
                }

                _rx = rx;
                _tx = tx;
                // Write (can retry once here if you want, but usually the above is enough)
                ShowBlePopup("Sending message…");
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(text + "<<END>>");


                    await KCBle.SendWithAckBleAsync(text + "<<END>>", rx, _tx, 15000);
                    // await _rx.WriteAsync(bytes);
                    Log("Sent message!");
                }
                catch (Exception ex)
                {
                    ShowBlePopup("Write error, will retry.");
                    Log($"Write error: {ex.Message}");
                    throw;
                }

                ShowBlePopup("Success! Disconnecting…");
                await Task.Delay(500);
                await _adapter.DisconnectDeviceAsync(_dev);
                Log("Disconnected. Done.");
                break; // Success!
            }
            catch (ObjectDisposedException ex)
            {
                ShowBlePopup("BLE session disposed. Cleaning up and retrying…");
                Log($"BLE round failed (disposed): {ex.Message}. Cleaning up and retrying from top...");
                try { if (_dev != null) await _adapter.DisconnectDeviceAsync(_dev); } catch { }
                _dev = null; _rx = null;
                await Task.Delay(800);
            }
            catch (Exception ex)
            {
                ShowBlePopup($"BLE round failed: {ex.Message}");
                Log($"BLE round failed (other): {ex.Message}");
                try { if (_dev != null) await _adapter.DisconnectDeviceAsync(_dev); } catch { }
                _dev = null; _rx = null;
                await Task.Delay(800);
            }
        }

        DismissBlePopup();
        Log("BLE send sequence complete.");
    }
    void DismissBlePopup()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _popup?.CloseAsync();
            _popup = null;
        });
    }
    public async Task<bool> SendAsync(string text)
    {

        await DoFullSend(text);
        return true;
        
    }

    private void StartHealthLoop()
    {
        StopHealthLoop();
        _healthCts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!_healthCts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(45), _healthCts.Token);

                if (_healthCts.Token.IsCancellationRequested) break;

                // skip if a message is mid-send (optional: add a flag for that)
                if (!IsConnected) continue;

                try
                {
                    // deep check: simple read/write
                    var ok = await PingAsync();
                    if (!ok)
                    {
                        Debug.WriteLine("[BLE] Health check failed, disconnecting...");
                        await DisconnectAsync();
                    }
                }
                catch
                {
                    await DisconnectAsync();
                }
            }
        });
    }

    private void StopHealthLoop()
    {
        try { _healthCts?.Cancel(); } catch { }
        _healthCts = null;
    }

    private async Task<bool> PingAsync()
    {
        if (_characteristic == null) return false;

        try
        {
            var payload = Encoding.UTF8.GetBytes("ping");
            await _characteristic.WriteAsync(payload);
            return true; // you can also wait for "pong" if firmware replies
        }
        catch
        {
            return false;
        }
    }
}
    