

using KeyCatcher_acc.services;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.BLE.Abstractions.Contracts;

namespace KeyCatcher_acc.Views;

public partial class tstpag : ContentPage
{
    private bool _permissionsChecked = false;
    private tstpagViewModel vm;
    //   KeyCatcherServiceController cntlr;
    public CommHub _cntl;
    private async void Settings_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Tapped!");
    }
    public tstpag(tstpagViewModel  viewModel, CommHub cntl)
    {
        _cntl = cntl;
        InitializeComponent();
        //vm=cv
        BindingContext = vm = viewModel;
        //(BindingContext is HomePageViewModel vm)
        _ = vm.ConnectNowCommand; // do not await

#if DEBUG
        var vmType = BindingContext?.GetType();
        foreach (var p in vmType?.GetProperties() ?? Array.Empty<System.Reflection.PropertyInfo>())
            System.Diagnostics.Debug.WriteLine($"[VM] {p.Name} : {p.PropertyType.Name}");
#endif
    }
    private async void TestSend_Clicked(object sender, EventArgs e)
    {
//        var ok = await cntlr.ConnectAsync();
        //Debug.WriteLine($"Connected? {ok} via {(cntlr.IsConnected ? "active transport" : "none")}");
        var sent = await _cntl.SendAsync("piaaang");
        //ery Debug.WriteLine($"SendText result: {sent}");
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
}

//using Plugin.BLE.Abstractions.Contracts;

//namespace KeyCatcher_acc.Views;

//using Microsoft.Maui;
//using Microsoft.Maui.ApplicationModel; // For Permissions
//using Microsoft.Maui.Devices;          // For DeviceInfo

//public partial class HomePage : ContentPage
//{
//    private bool _permissionsChecked = false;
//    private HomePageViewModel vm;

//    public HomePage(HomePageViewModel viewModel)
//    {
//        InitializeComponent();
//        BindingContext = viewModel;
//        vm = viewModel;
//    }

//    protected override async void OnAppearing()
//    {
//        base.OnAppearing();

//        if (_permissionsChecked)
//            return;

//        _permissionsChecked = true;

//        bool allGranted = true;

//#if ANDROID
//        try
//        {
//            var statusScan = await Permissions.RequestAsync<BluetoothScanPermission>();
//            var statusConn = await Permissions.RequestAsync<BluetoothConnectPermission>();
//            var statusLoc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

//            allGranted = (statusScan == PermissionStatus.Granted) &&
//                         (statusConn == PermissionStatus.Granted) &&
//                         (statusLoc == PermissionStatus.Granted);
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlert("Error", $"Failed to request Android BLE permissions: {ex.Message}", "OK");
//            allGranted = false;
//        }
//#else
//        try
//        {
//            var statusLoc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
//            allGranted = (statusLoc == PermissionStatus.Granted);
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlert("Error", $"Failed to request permissions: {ex.Message}", "OK");
//            allGranted = false;
//        }
//#endif

//        if (!allGranted)
//        {
//            await MainThread.InvokeOnMainThreadAsync(async () =>
//            {
//                if (Shell.Current != null)
//                    await Shell.Current.DisplayAlert(
//                        "Permission needed",
//                        "KeyCatcher needs Location and Bluetooth permissions to scan for devices.",
//                        "OK");
//            });

//            //await DisplayAlert("Permission needed",
//              //  "KeyCatcher needs Location and Bluetooth permissions to scan for devices.", "OK");
//            return;
//        }

//        if (Microsoft.Maui.Storage.Preferences.ContainsKey("PendingShareText"))
//        {
//            var text = Microsoft.Maui.Storage.Preferences.Get("PendingShareText", "");
//            if (!string.IsNullOrWhiteSpace(text))
//            {
//                vm = this.BindingContext as HomePageViewModel;
//                if (vm != null)
//                    vm.MessageText = text;

//                Microsoft.Maui.Storage.Preferences.Remove("PendingShareText");
//            }
//        }
//      //  await vm.ScanForDevicesAsync();
//        // Permissions granted!
//        // You can scan for BLE here, or just enable your Scan button.
//    }
//    //private void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
//    //{
//    //    if (e.CurrentSelection.Count > 0 && BindingContext is HomePageViewModel vm)
//    //    {
//    //        var device = e.CurrentSelection[0] as IDevice;
//    //        if (device != null)
//    //        {
//    //            vm.ConnectToDeviceCommand.Execute(device);
//    //        }
//    //    }
//    //}
//}
