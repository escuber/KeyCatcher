using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCatcher_acc.services
{
    public static class perms
    {


        public static bool _permissionsChecked = false;
        public async static Task<bool> HasLocationPermission()
        {
            var status = Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>().Result;
            return status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;

            // _ = _cntl.ConnectAsync();

            if (_permissionsChecked) return true; ;
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
                return false;
            }

            // Pick up any PendingShareText saved earlier
            if (Preferences.ContainsKey("PendingShareText"))
            {
                var text = Preferences.Get("PendingShareText", "");
                if (!string.IsNullOrWhiteSpace(text))
                {
                   // vm.MessageText = text;
                    Preferences.Remove("PendingShareText");
                }
            }

            // Optionally kick off an initial scan here (Android only)
            // await vm.ScanForDevicesAsync();


            if (!allGranted)
            {
                await SafeAlert("Permission needed",
                    "KeyCatcher needs Location and Bluetooth permissions to scan for devices.");
                return true;
            }

            // 3️⃣ kick off Wi-Fi + BLE race exactly once
            //  _ = Task.Run(() => cntlr .ConnectAsync());

            // Optional: restore any pending share text
            if (Preferences.ContainsKey("PendingShareText"))
            {
                var text = Preferences.Get("PendingShareText", "");
                if (!string.IsNullOrWhiteSpace(text))
                {
                   // vm.MessageText = text;
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
}
