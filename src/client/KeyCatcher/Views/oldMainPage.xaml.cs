using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;

namespace BleTestingAndDev.Views;


public partial class oldMainPage : ContentPage
{
    static readonly Guid SVC_UUID = Guid.Parse("0000AAAA-0000-1000-8000-00805F9B34FB");
    static readonly Guid RX_UUID = Guid.Parse("0000BBBB-0000-1000-8000-00805F9B34FB");
    private  readonly Guid TX_UUID = Guid.Parse("0000BBBC-0000-1000-8000-00805F9B34FB");

    IBluetoothLE _ble;
    IAdapter _adapter;
    private bool _permissionsChecked = false;

    
    private IDevice? _dev;                // The connected BLE device (null if not connected)
    private ICharacteristic? _rx;         // Characteristic for "Write" (used to send data to device)
    private ICharacteristic? _tx;         // Characteristic for "Notify" (used to receive data from device)
    private bool _subscribed;             // Whether notifications are set up
    private readonly object _subLock = new(); //
    public bool IsConnected { get; private set; }
    public event EventHandler<string>? Notified;
    private void OnTxValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        var s = System.Text.Encoding.UTF8.GetString(e.Characteristic.Value ?? Array.Empty<byte>());
        Notified?.Invoke(this, s); // raise your event for outside code
    }
    public oldMainPage()
    {
        InitializeComponent();
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        Log("App started. Ready.");
    }


    
    BleProgressPopup? _popup;

    void ShowBlePopup(string status)
    {
        DismissBlePopup();
        _popup = new BleProgressPopup();
        _popup.StatusText = status; // Adjust if your property is "Status"
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current?.MainPage?.ShowPopup(_popup);
        });
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // _ = _cntl.ConnectAsync();

        if (_permissionsChecked) return;
        _permissionsChecked = true;
        var allGranted = true;


        var win = Application.Current.Windows.First();
        double width = win.Width;   // logical units (DIP)
        double height = win.Height;
        Debug.WriteLine($"Page sees {width} × {height}");


#if ANDROID
        try
        {
            var statusScan = await Permissions.RequestAsync<BluetoothScanPermission>();
            var statusConn = await Permissions.RequestAsync<BluetoothConnectPermission>();
            var statusLoc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            allGranted = statusScan == PermissionStatus.Granted
                         && statusConn == PermissionStatus.Granted
                         && statusLoc == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            await SafeAlert("Error", $"Failed to request Android BLE permissions: {ex.Message}");
            allGranted = false;
        }
#elif WINDOWS
        // Do not request runtime permissions on Windows.
        // Capabilities are declared in Platforms/Windows/Package.appxmanifest.
        allGranted = true;
#else
        // Other platforms as needed...

#endif

        if (!allGranted)
        {
            await SafeAlert("Permission needed",
                "KeyCatcher needs Location and Bluetooth permissions to scan for devices.");
            return;
        }

        //// Pick up any PendingShareText saved earlier
        //if (Preferences.ContainsKey("PendingShareText"))
        //{
        //    var text = Preferences.Get("PendingShareText", "");
        //    if (!string.IsNullOrWhiteSpace(text))
        //    {
        //        vm.MessageText = text;
        //        Preferences.Remove("PendingShareText");
        //    }
        //}

        // Optionally kick off an initial scan here (Android only)
        // await vm.ScanForDevicesAsync();


        if (!allGranted)
        {
            await SafeAlert("Permission needed",
                "KeyCatcher needs Location and Bluetooth permissions to scan for devices.");
            return;
        }

        // 3️⃣ kick off Wi-Fi + BLE race exactly once
        //  _ = Task.Run(() => cntlr .ConnectAsync());

        //// Optional: restore any pending share text
        //if (Preferences.ContainsKey("PendingShareText"))
        //{
        //    var text = Preferences.Get("PendingShareText", "");
        //    if (!string.IsNullOrWhiteSpace(text))
        //    {
        //        vm.MessageText = text;
        //        Preferences.Remove("PendingShareText");
        //    }
        //}
    }

    async void OnSendClicked(object sender, EventArgs e)
    {
        ///string text = TextEntry.Text ?? "";
      ////  await Task.Delay(5 * 1000);
        Console.WriteLine("Chunked long...");
        string text = new string('A', 5000);// + "<<END>>";
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
        _popup?.Close();
        _popup = null;
    }
    private static Task SafeAlert(string title, string message, string cancel = "OK")
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
            if (page != null)
                await page.DisplayAlert(title, message, cancel);
        });
    }

    void Log(string s)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogLabel.Text = $"{DateTime.Now:T} {s}\n{LogLabel.Text}";
            StatusLabel.Text = s;
        });
        Debug.WriteLine(s);
    }
    

    private async Task CleanupAsync()
    {
        try
        {
            if (_tx != null && _subscribed)
            {
                try { await _tx.StopUpdatesAsync(); } catch { }
                try { _tx.ValueUpdated -= OnTxValueUpdated; } catch { }
            }
            if (_dev != null)
            {
                try { await _adapter.DisconnectDeviceAsync(_dev); } catch { }
            }
        }
        finally
        {
            _subscribed = false;
            _tx = null;
            _rx = null;
            _dev = null;
            IsConnected = false;
        }
    }
}


