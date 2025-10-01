

using KeyCatcher_acc.services;
using KeyCatcher_acc.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.BLE.Abstractions.Contracts;

namespace KeyCatcher_acc.Views;

public partial class mainpage : ContentPage
{
    private bool _permissionsChecked = false;
    
    
    
    private MainPageViewModel vm;
    
    public CommHub _cntl;
    private readonly KeyCatcherWiFiService _wifi;
    
  
    public mainpage(MainPageViewModel viewModel, CommHub cntl,  KeyCatcherWiFiService wifi)
    {
        _cntl = cntl;
        _wifi = wifi;
        InitializeComponent();
       
        BindingContext = vm = viewModel;
        //(BindingContext is HomePageViewModel vm)
        //_ = vm.ConnectNowCommand; // do not await

#if DEBUG
        var vmType = BindingContext?.GetType();
        foreach (var p in vmType?.GetProperties() ?? Array.Empty<System.Reflection.PropertyInfo>())
            System.Diagnostics.Debug.WriteLine($"[VM] {p.Name} : {p.PropertyType.Name}");
#endif
    }
    //    private async void TestSend_Clicked(object sender, EventArgs e)
    //    {
    ////        var ok = await cntlr.ConnectAsync();
    //        //Debug.WriteLine($"Connected? {ok} via {(cntlr.IsConnected ? "active transport" : "none")}");
    //        var sent = await _cntl.SendAsync("piaaang");
    //        //ery Debug.WriteLine($"SendText result: {sent}");
    //    }
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

        // Pick up any PendingShareText saved earlier
        if (Preferences.ContainsKey("PendingShareText"))
        {
            var text = Preferences.Get("PendingShareText", "");
            if (!string.IsNullOrWhiteSpace(text))
            {
                vm.MessageText = text;
                Preferences.Remove("PendingShareText");
            }
        }

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

        // Optional: restore any pending share text
        if (Preferences.ContainsKey("PendingShareText"))
        {
            var text = Preferences.Get("PendingShareText", "");
            if (!string.IsNullOrWhiteSpace(text))
            {
                vm.MessageText = text;
                Preferences.Remove("PendingShareText");
            }
        }

    await _cntl.InitializeAsync();
        ///await _wifi.SendTextAsync("fucker you");
        //Task.Run(async () => await _cntl.InitializeAsync());
        //  _ = vm.ConnectNowCommand; // do not await
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


    private async void Settings_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Tapped!");
    }
    
}

