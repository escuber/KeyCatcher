using CommunityToolkit.Maui.Views;
using KeyCatcher.models; // WifiCredential
using KeyCatcher.services;
using KeyCatcher.ViewModels;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Input;
namespace KeyCatcher.Popups
{
    public partial class WifiCreds : Popup
    {
        private readonly KeyCatcherSettingsService _settings;
        private readonly CommHub? _hub;
        private bool _isClosed;
        public WifiCredsViewModel VM { get; }
        
        public WifiCreds(KeyCatcherSettingsService settings, CommHub? hub = null)
        {
            InitializeComponent(); // let XAML load first

            _settings = settings;
            _hub = hub;
            CanBeDismissedByTappingOutsideOfPopup = false;
            // ViewModel
            VM = new WifiCredsViewModel(_settings,hub);
            VM.InitFromService(_settings);

            // If your XAML binds to VM properties, set BindingContext to VM.
            // If you use x:Reference RootPopup for commands, that still works.
            BindingContext = VM;
            SaveAndCloseCommand = new Command(async () =>
            {
                VM.ApplyToService(_settings);
                VM.IsEditing = false;

                if (_hub is not null)
                {
                    var payload = _settings.MakeMessage();
                    //try { await _hub.ApplySetupAsync(payload, 20000); }
                    //catch { /* ignore transport error here */ }
                }

               await CloseAsync();
            });

            SaveAndCloseCommand = new Command(async () =>
            {
                try
                {
                    VM.IsBusy = true;   // show spinner
                    //VM.ApplyToService(_settings);
                    VM.IsEditing = false;

                    //if (_hub is not null)
                    //{
                    //    var payload = _settings.MakeMessage();
                    //    //try { 
                    //        await _hub.ApplySetupAsync(payload, 20000); 
                    //    //}
                    //    //catch { /* ignore transport error here */ }
                    //}
                }
                finally
                {
                    VM.IsBusy = false;  // hide spinner
                    await CloseAsync();
                }
            });

            // Commands: never put logic in a getter
            //CloseCommand = new Command(async () => await CloseAsync());
            //CloseCommand = new Command(async () =>
            //{
            //    System.Diagnostics.Debug.WriteLine("CloseCommand running");
            //    await CloseAsync();
            //    System.Diagnostics.Debug.WriteLine("CloseAsync returned");
            //});

            //CloseCommand = new Command(async () => await CloseAsync());
         //   BtnEditorCancel.Command = new Command(() =>
        //    {
        //        VM.IsEditing = false;
       //         VM.EditingNetwork = new WifiCredential();
        //    });

            // save the editing network and close the overlay
            //BtnEditorSave.Command = new Command(() =>
         //   {
                // use your VM logic
          //      VM.SaveNetwork(); // calls IsEditing = false inside
                                  // if you prefer your other flow:
                                  // VM.ApplyToService(_settings);
                                  // VM.IsEditing = false;
         //   });


            CloseCommand = new Command(async () =>
            {
                System.Diagnostics.Debug.WriteLine("CloseCommand firing");
                await CloseAsync();
            });



          
            //BtnSaveAndClose.Command = SaveAndCloseCommand;

            //CloseCommand = new Command(async () =>
            //  await MainThread.InvokeOnMainThreadAsync(async () => await CloseAsync()));
            // CloseCommand = new Command(() =>
            //{
            //    System.Diagnostics.Debug.WriteLine("CloseCommand running");

            //    Close(); // not CloseAsync
            //    System.Diagnostics.Debug.WriteLine("Close() returned");
            //});

            CancelEditCommand = new Command(() =>
            {
                VM.IsEditing = false;
                VM.EditingNetwork = new WifiCredential();
            });
           // BtnClose.Command = CloseCommand;
            Closed += (_, __) => _isClosed = true;
        }

        // optional: trace the lifecycle
        // this.Opened += (_, __) => System.Diagnostics.Debug.WriteLine("WifiCreds opened");
        //this.Closed += (_, __) => System.Diagnostics.Debug.WriteLine("WifiCreds closed");
        private async Task SafeCloseAsync()
        {
            if (_isClosed) return;                 // already closed
            try { await CloseAsync(); }            // only close if still open
            catch (Exception ex) { }     // swallow race condition
            //PopupNotFoundException
        }
        [RelayCommand]
        public void closeit() {

            CloseAsync().Wait();
        }
        void OnSaveAndClose2(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnSaveAndClose fired");
            // do your save/apply logic here (or delegate to VM)
            // Close with a result if you want:
            CloseAsync().Wait                ();
            // or: await CloseAsync(true);  <-- requires `async void OnSaveAndClose(...)`
        }



        public async Task SaveAndCloseAsync()
        {
            // 1. Write VM changes to settings service
            VM.ApplyToService(_settings);

            // 2. Save to local preferences
            _settings.Save();

            // 3. Push config to device if hub is connected (WiFi or BLE)
            if (_hub != null && _hub.IsAnyUp)
            {
                var payload = _settings.MakeMessage();
                try
                {
                    // 20s timeout is typical, adjust as needed
                    var ok = await _hub.SendAsync(payload);
                    if (!ok)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Device not updated",
                            "Saved locally, but the device did not respond. Reconnect and retry.",
                            "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Update failed", ex.Message, "OK");
                }
            }
            else
            {
                // Optionally alert: Saved locally, device not updated yet
                // await Application.Current.MainPage.DisplayAlert("Not connected", "Config saved. Connect and retry.", "OK");
            }

            // 4. Close the popup
            await CloseAsync();
        }
        private async Task DoSaveAndCloseAsync()
        {
            try
            {
                VM.IsBusy = true;                  // spinner + disable buttons

                // persist locally
                //VM.ApplyToService(_settings);

                // push to device with timeout (don’t hang forever)
                if (_hub is not null)
                {
                    _settings.Save();            // ensure saved before push
                    var payload = _settings.MakeMessage();
                    //todo fix this
                    //var push = _hub.ApplySetupAsync(payload, 20_000);   // your method
                    //var done = await Task.WhenAny(push, Task.Delay(20_000));
                    // (optional) notify if it didn’t complete
                }
            }
            finally
            {
                VM.IsBusy = false;
                await SafeCloseAsync();            // <- guarded close
            }
        }
        //credSaveAndCloseCommand = new Command(() =>
        //{
        //    // local save inside popup list only
        //    VM.ApplyToService(_settings);
        //    VM.Networks.Add(VM.EditingNetwork);
        //    VM.EditingNetwork = new WifiCredential();
        //    VM.IsEditing = false;
        //});
        // Wire this to your “Save & Close” button

        private async void OnSaveAndClose(object? sender, EventArgs e)
        {
            try
            {
                if (BindingContext is WifiCredsViewModel vm)
                    vm.ApplyToService(_settings);

                if (_hub is not null)
                {
                    var payload = _settings.MakeMessage();
                   // var ok = await _hub.ApplySetupAsync(payload, 20_000);
                    // optionally show a toast if !ok
                }

                await CloseAsync();// ("updated");   // <-- MAUI Controls Popup
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert("Save failed", ex.Message, "OK"));
                await CloseAsync();// ("error");
            }
        }

        private async void OnCancel(object? sender, EventArgs e)
        {
            await CloseAsync();// ("cancel");        // <-- MAUI Controls Popup
        }
        //SaveAndCloseCommand = new Command(async () =>
        //    {
        //        // Persist and optionally push to device
        //        VM.ApplyToService(_settings);
        //        VM.IsEditing = false;

        //        if (_hub is not null)
        //        {
        //            var payload = _settings.MakeMessage(); // emits <setup>...<<END>>
        //            try { await _hub.ApplySetupAsync(payload, 20000); }
        //            catch { /* ignore transport errors here */ }
        //        }

        //        await CloseAsync();
        //    });
        //    // after InitializeComponent and BindingContext
        //    BtnSaveAndClose.Command = new Command(async () =>
        //    {
        //        VM.ApplyToService(_settings);

        //        if (!(_hub?.IsAnyUp ?? false))
        //        {
        //            await Application.Current.MainPage.DisplayAlert(
        //                "Not connected",
        //                "Saved to app settings. Device update will run after a link is up.",
        //                "OK");
        //            await CloseAsync();
        //            return;
        //        }

        //        try
        //        {
        //            var payload = _settings.MakeMessage();
        //            var ok = await _hub!.ApplySetupAsync(payload, 20_000);
        //            if (!ok)
        //                await Application.Current.MainPage.DisplayAlert("Device not ready", "It did not come back in time. You can retry from Settings.", "OK");
        //        }
        //        catch (Exception ex)
        //        {
        //            await Application.Current.MainPage.DisplayAlert("Update failed", ex.Message, "OK");
        //        }
        //        finally
        //        {
        //            await CloseAsync();
        //        }
        //    });

        //    // Sizing
        //    ResizeToViewport();
        //    DeviceDisplay.MainDisplayInfoChanged += (_, __) =>
        //        MainThread.BeginInvokeOnMainThread(ResizeToViewport);
        //}

        void ResizeToViewport()
        {
            var di = DeviceDisplay.MainDisplayInfo;
            var w = di.Width / di.Density;
            var h = di.Height / di.Density;
            // adjust your layout element sizes here
        }

        // Exposed to XAML
        public ICommand CancelEditCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SaveAndCloseCommand { get; }
        public ICommand credSaveAndCloseCommand { get; }


        private void BtnClose_Clicked(object sender, EventArgs e)
        {
            _settings.Save();
            CloseAsync().Wait();
        }


        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is WifiCredsViewModel VM)
                    VM.ApplyToService(_settings);

                if (this.VM.Hub is not null)
                {
                    var payload = _settings.MakeMessage();

                    // await instead of .Wait()
                    //var ok = await _hub.ApplySetupAsync(payload, 20_000);

                    //if (!ok)
                    //    await Application.Current.MainPage.DisplayAlert(
                    //        "Device not ready",
                    //        "It didn’t respond in time. You can retry from Settings.",
                    //        "OK");
                }

                // Close popup on UI thread
                await CloseAsync();
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert("Save failed", ex.Message, "OK"));

                await CloseAsync();
            }
        }
        //private void Button_Clicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (BindingContext is WifiCredsViewModel VM)
        //            VM.ApplyToService(_settings);

        //        if (this.VM.Hub is not null)
        //        {
        //            var payload = _settings.MakeMessage();
        //            _hub.ApplySetupAsync(payload, 20_000).Wait();
        //            // optionally show a toast if !ok
        //        }

        //        CloseAsync().Wait();// ("updated");   // <-- MAUI Controls Popup
        //    }
        //    catch (Exception ex)
        //    {
        //        MainThread.InvokeOnMainThreadAsync(() =>                    Application.Current.MainPage.DisplayAlert("Save failed", ex.Message, "OK"));
        //        CloseAsync().Wait();// ("error");
        //    }
        //}
    }
}


//using CommunityToolkit.Maui.Views;
//using KeyCatcher_acc.models; // WifiCredential
//using KeyCatcher_acc.services;
//using KeyCatcher_acc.ViewModels;
//using System.Runtime.InteropServices;
//using System.Windows.Input;

//namespace KeyCatcher_acc.Popups
//{
//    public partial class WifiCreds : Popup
//    {
//        private readonly KeyCatcherSettingsService _settings;
//        private readonly CommHub? _hub;
//        public WifiCredsViewModel VM { get; }

//        // You can inject CommHub if you want to push <setup> after save
//        public WifiCreds(KeyCatcherSettingsService settings, CommHub? hub = null)
//        {
//            //  VM= vm;

//           // CloseCommand = new Command(Close);

//            //CloseCommand = new Command(() =>
//            //{
//            //    VM.IsEditing = false;
//            //    VM.EditingNetwork = new WifiCredential();
//            //});
//            CancelEditCommand = new Command(() =>
//            {
//                VM.IsEditing = false;
//                VM.EditingNetwork = new WifiCredential();
//            });

//            credSaveAndCloseCommand = new Command(async () => {

//                VM.ApplyToService(_settings);




//                VM.Networks.Add(VM.EditingNetwork);
//                VM.EditingNetwork = new WifiCredential();


//                // Optional: push to device as <setup> and wait for the link to come back
//                // If you prefer to do this elsewhere, remove this block.
//                VM.IsEditing = false;
//                //if (_hub is not null)
//                //{
//                //    var payload = _settings.MakeMessage(); // emits <setup>...<<END>>
//                //    try { await _hub.ApplySetupAsync(payload, 20000); }
//                //    catch { /* swallow apply errors, the UI save already persisted */ }
//                //}
//                //VM.IsEditing = false;
//            });

//            SaveAndCloseCommand = new Command(async () =>
//            {
//                // Persist to settings
//                VM.ApplyToService(_settings);

//                // Optional: push to device as <setup> and wait for the link to come back
//                // If you prefer to do this elsewhere, remove this block.
//                VM.IsEditing = false;
//                if (_hub is not null)
//                {
//                    var payload = _settings.MakeMessage(); // emits <setup>...<<END>>
//                    try { await _hub.ApplySetupAsync(payload, 20000); }
//                    catch { /* swallow apply errors, the UI save already persisted */ }
//                }
//                VM.IsEditing = false;
//                // await CloseAsync();

//            });
//            //SaveAndCloseCommand = new AsyncRelayCommand(async () =>
//            //{
//            //    VM.ApplyToService(settings);        // persist
//            //    await hub.SendSetupAsync();         // optional push
//            //    Close();                            // dismiss popup
//            //});

//            // CancelEditCommand = new Command(() => VM.CancelEdit());

//            InitializeComponent();
//            _settings = settings;
//            _hub = hub;

//            VM = new WifiCredsViewModel(_settings);
//            VM.InitFromService(_settings); // load current SSID, pss, and backups
//            BindingContext = VM;

//            // lightweight commands handled by the popup itself

//            //CloseCommand = new Command(() => CloseAsync());


//            ResizeToViewport();
//            DeviceDisplay.MainDisplayInfoChanged += (_, __) => MainThread.BeginInvokeOnMainThread(ResizeToViewport);
//        }
//        void ResizeToViewport()
//        {
//            var di = DeviceDisplay.MainDisplayInfo;
//            var w = di.Width / di.Density;
//            var h = di.Height / di.Density;

//            //Card.WidthRequest = Math.Min(w * 0.98, 720);
//            //Card.MaximumHeightRequest = h * 0.90;

//            //// Ensure the editor never exceeds the card
//            //EditorCard.MaximumHeightRequest = Card.MaximumHeightRequest - 24;
//        }

//        // Exposed to XAML
//        public ICommand CancelEditCommand { get; }
//        public ICommand CloseCommand { 
//                    get {
//                CloseAsync();

//                return null;
//            }
//}
//public ICommand SaveAndCloseCommand { get; }
//        public ICommand credSaveAndCloseCommand { get; }
//        public async Task SaveAndCloseAsync()
//        {
//            // Persist data held in the popup
//            _settings.Save();   // replace with your call

//            // Close the popup and return
//            CloseAsync();
//        }
//    }
//}