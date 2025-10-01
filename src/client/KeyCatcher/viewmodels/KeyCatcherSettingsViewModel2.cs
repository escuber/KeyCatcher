using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

//using Java.Util;

//using Java.Util;

//using Java.Util;
using KeyCatcher_acc.models;
using KeyCatcher_acc.Popups;
using KeyCatcher_acc.services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace KeyCatcher_acc.ViewModels
{
    public partial  class KeyCatcherSettingsViewModel2 : ContentPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public CommHub hub;
        private readonly KeyCatcherBleService _ble;
        private readonly KeyCatcherWiFiService _wifi;
        //private readonly KeyCatcherSettingsService _settings;
        public  KeyCatcherSettingsService settingsService;

        public KeyCatcherSettingsService Settings => settingsService; // handy for XAML bindings

        public KeyCatcherSettingsViewModel2(
                                            CommHub cntlr,
                                            KeyCatcherSettingsService settings,
                                            KeyCatcherBleService ble,
                                            KeyCatcherWiFiService wifi)
        {
            _ble = ble;
            _wifi= wifi;

            hub= cntlr;
            settingsService  = settings;
            settingsService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(settingsService.InputType))
                    OnPropertyChanged(nameof(InputType));
            };
            settingsService.PropertyChanged += (_, __) => SendSetupCommand.NotifyCanExecuteChanged();


        }

        private bool CanSendSetup() => settingsService.IsDirty;
        [RelayCommand(CanExecute = nameof(CanSendSetup))]
        private async Task SendSetup()
        {
            // Build and send
            var payload = settingsService.MakeMessage();
            try
            {
                await hub.ApplySetupAsync(payload, 20000); // or your send pathway
            }
            finally
            {
                settingsService.Save(); // persist & resets IsDirty=false
                SendSetupCommand.NotifyCanExecuteChanged();
            }
        }
        
        public string InputType
        {
            get => settingsService.InputType;
            set
            {
                if (settingsService.InputType == value) return;
                settingsService.InputType = value;
                OnPropertyChanged();                 // notifies XAML
                settingsService.Save();              // persist immediately (optional)
            }
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await init();
        }
        public async Task init() {
            await getSettings();
            OnPropertyChanged();
        }
        public enum HostLinkType { USB, Bluetooth }

        private HostLinkType _selectedHostLinkType = HostLinkType.USB;
        public HostLinkType SelectedHostLinkType
        {
            get => _selectedHostLinkType;
            set { if (_selectedHostLinkType == value) return; _selectedHostLinkType = value; OnPropertyChanged(); }
        }

        [RelayCommand]
        public async Task sendUpdateCatcherSettings()
        {
         
            var msg = settingsService.MakeMessage();
            await hub.ApplySetupAsync(msg);

        }
        public async Task UpdateFromKeycatrcher()
        {

            var json = await hub.GetConfigAsync();
            if (String.IsNullOrEmpty(json))
            {
                settingsService.ApplyDeviceJson(json);
            }
            //var msg = settingsService.MakeMessage();
            settingsService.Save();

        }
        
            [RelayCommand]
        private async Task getSettings()
        {
            //await hub.ConnectAsync();


            // //if (settingsService.inputType == "BOTH") settingsService.InputType = "BLE";
            //  else if (settingsService.inputType != "BOTH") settingsService.InputType = "BOTH";
            //while ( _ble.IsConnecting)
            //{
            //    Thread.Sleep(1000);

            
            //}
            var config = await hub.GetConfigAsync();
            
            //string? json = await hub();
            //if (json == null) ShowToast("no link");
            //else ProcessConfig(json);

            var json = await hub.GetConfigAsync();
            //_wifi.GetConfigAsync();


            settingsService.ApplyDeviceJson(json);
            var msg = settingsService.MakeMessage();
            settingsService.Save();

            //await _ble.SendTextAsync(msg);
            //var ok = await _ble.SendLongMessageAsync(msg, mtuHint: null, finalTimeoutMs: 8000);
            ///SendLongMessageAsync(msg);  
            //OnPropertyChanged();
            //settingsService.PropertyChanged();
            //settingsService.Save();
            //var msg = settingsService.MakeMessage();
            //await _controller.SendLongMessageAsync(msg);  


            // var after = settingsService.MakeMessage();

            // var x = after;


            //wifivm = new WifiCredsViewModel();
            // popup = new KeyCatcher_acc.Popups.WifiCreds(wifivm, Settings);
        }
        [RelayCommand]
        private async Task misc()
        {
            // //if (settingsService.inputType == "BOTH") settingsService.InputType = "BLE";
            //  else if (settingsService.inputType != "BOTH") settingsService.InputType = "BOTH";
            //var rslt = await _controller.ConnectAsync();
            //var config = await _controller.GetConfigAsync();// ("hello");

            // settingsService.ApplyDeviceJson(config);
            //WifiCredential wif = new WifiCredential();
            //wif.SSID = settingsService.SSID;
            ////wif.Password= settingsService.Password;
            ////settingsService.Creds
            ////ObservableCollection<WifiCredential> nets= new();
            ////nets.Add(wif);

           
           // try
           // {

                //var config = await hub.GetConfigAsync();

                //settingsService.ApplyDeviceJson(config);

                settingsService.outputType = "USBHID";
                //settingsService.SSID = "DADNET";
                var msg = settingsService.MakeMessage();
            await hub.ApplySetupAsync(msg);
           //await hub.SendAsync(msg); 
            
            
            //    bool ok = await hub.ApplySetupAsync(msg);

                //await _ble.SendLongMessageAsync(msg);
                //var ok = await _ble.SendLongMessageAsync(msg, mtuHint: null, finalTimeoutMs: 8000);

                //var ok = await _ble.SendLongMessageAsync(msg, mtuHint: 185);
                //var rslt=await _ble
                //  .SendLongMessageAsync
                //    ///.SendTextAsync

                //    (msg);
          //  }
           // catch (Exception ex) {
            
         //   }
            ////if (string.IsNullOrWhiteSpace(MessageText)) return;
            //await Task.Delay(3 * 1000);
            //await _controller.reset();
           

            //var crea = settingsService.creds;


            //
            //            settingsService.inputType= "BLE";
            ////settingsService.Creds=JsonSerializer.Serialize(nets);
            ///       settingsService.Save();

            //    var msg =  settingsService.MakeMessage();
            //var rslt3 =await _controller.ConnectAsync();
            //var sent = await _controller.SendTextAsync(messageText);
            //  var rslt2 = await _controller.SendTextAsync(msg);
            //    //SendLongMessageAsync(msg);






            //var config = await _controller.GetConfigAsync();

            //settingsService.ApplyDeviceJson(config);
            //OnPropertyChanged();
            //settingsService.PropertyChanged();

            // var after = settingsService.MakeMessage();

            // var x = after;


            //wifivm = new WifiCredsViewModel();
            // popup = new KeyCatcher_acc.Popups.WifiCreds(wifivm, Settings);
        }
        //    set { outputType = value; OnPropertyChanged(); }
        //}
        //partial void OnOutputTypeChanged(string? oldValue, string newValue)       
        //{
        //    // This fires on every OutputType update!
        //    Console.WriteLine($"OutputType changed to: {newValue}");
        //    this.Settings.Save();
        //    // Put your logic here (send message, update config, etc.)
        //}
        public ICommand SaveCommand { get; }
        public event Action ShowNetworkPopupRequested;

        public ICommand ShowNetworkPopupCommand => new Command(() =>
        {
            ShowNetworkPopupRequested?.Invoke();
        });

        [RelayCommand]
        public async void closePopup() {

await            popup.CloseAsync();
            int x = 0;
        
        
        }
        //NetworkSettingsPopup popup;
        WifiCreds popup;
        WifiCredsViewModel wifivm;
        [RelayCommand]
        private async void showsetworkPopup()
        {
            wifivm = new WifiCredsViewModel();
            popup = new KeyCatcher_acc.Popups.WifiCreds( settingsService);
                //new networkInfoPopup(this)

            
            //NetworkSettingsPopup(this); popup.OnSave += (ssid, password) =>

            {
                // Save the details, update settings, etc.
                //Console.WriteLine($"SSID: {ssid}, Password: {password}");
            };
           // await this.ShowPopupAsync(popup);
        }

        public List<string> InputSources { get; set; } = new() { "WIFI", "BLE" };
        public List<string> OutputTypes { get; set; } = new() { "BLEHID", "USBHID" };
        public  void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        //public void SendUpdatedConfig()
        //{
        //    var config = new
        //    {
        //        wifi_ssid = SSID,
        //        wifi_password = Password,
        //        ap_mode = ApMode ? "true" : "false",
        //        inputType = InputType,
        //        outputType = OutputType
        //    };
        //    var json = JsonSerializer.Serialize(config);
        //    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        //    // Send the configuration to the device via the appropriate method
        //    // This is a placeholder for actual sending logic
        //    // e.g., via Bluetooth, USB, etc.
        //}
    }
}
