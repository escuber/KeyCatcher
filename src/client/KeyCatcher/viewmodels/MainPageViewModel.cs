//KeyCatcher_acc.services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher.services;
using KeyCatcher.models;
using KeyCatcher.services;
using KeyCatcher.services;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace KeyCatcher.ViewModels;

public partial class MainPageViewModel : ObservableObject
{


    
    readonly KeyCatcherSettingsService _settings;

    [ObservableProperty] bool wifiUp;
    [ObservableProperty] bool bleUp;
    private readonly CommHub _hub;
    private readonly SendGate _sendGate;
    public KeyCatcherWiFiService wifi;
    public KeyCatcherBleService ble;

    public CommHub Hub => _hub;
    [ObservableProperty] private string messageText = string.Empty;



    public MainPageViewModel(CommHub hub, SendGate sendGate, KeyCatcherSettingsService setting, KeyCatcherWiFiService wwifi, KeyCatcherBleService bble)
    {
        ble = bble;
        wifi = wwifi;
        _hub = hub;
        _sendGate = sendGate;
        
        _settings = setting;

        _hub.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_hub.IsWifiUp)) WifiUp = _hub.IsWifiUp;
            if (e.PropertyName == nameof(_hub.IsBleUp)) BleUp = _hub.IsBleUp;
        };


    }
    public LinkState WifiLinkState =>
    !_hub.WifiEnabled ? LinkState.Off :
    _hub.IsWifiUp ? LinkState.On :
    _hub.IsBusy ? LinkState.Trying : LinkState.Error;

    public LinkState BleLinkState =>
        !_hub.BleEnabled ? LinkState.Off :
        _hub.IsBleUp ? LinkState.On :
        _hub.IsBusy ? LinkState.Trying : LinkState.Error;
    //public LinkState WifiLinkState =>
    //    !_hub.WifiEnabled ? LinkState.Off :
    //    _hub.IsWifiUp ? LinkState.On :
    //    _hub.IsBusy ? LinkState.Trying : LinkState.Error;

    //public LinkState BleLinkState =>
    //    !_hub.BleEnabled ? LinkState.Off :
    //    _hub.IsBleUp ? LinkState.On :
    //    _hub.IsBusy ? LinkState.Trying : LinkState.Error;

    [RelayCommand]
    private async Task Send()
    {
        var conf = "";
     //   while (true)
       // {
            //conf = await _hub.g
            //await KeyCatcherBleService.FindAndGetConfigAsync(CrossBluetoothLE.Current, CrossBluetoothLE.Current.Adapter);
        //ble.GetConfigAsync

        _settings.ApplyDeviceJson(conf);
        var msg =_settings.MakeMessage();

        _settings.SSID="xDadsCar";
        _settings.Password="4c4c4c4c";
        //_settings.Creds = "[\"DadsCar\":\"4c4c4c4c\"]";

        _settings.Creds =
               new List<WifiCredential> { new WifiCredential { SSID = "DADNET", Password = "4c4c4c4c" } };

        var msg2 = _settings.MakeMessage();

        ble.SendAsync(msg2);

        var x = 100;





        var aaa=conf;

       // i//f (conf != null)
         //       break;
            Thread.Sleep(500);  
       // }
        


        _settings.ApplyDeviceJson(conf);
        var ssid = _settings.SSID;
        var password = _settings.Password;
        var amsg = MessageText + "<<END>>";




        //string longMsg = new string('A', 3000) + "<<END>>";
        // var rslt=await ble.SendAsync(longMsg);

        //var ok = await _hub.SendAsync(longMsg);
        //  if (!ok)
        //     await App.Current.MainPage.DisplayAlert("Blocked", "Send failed", "OK");



        ///var ok = await _sendGate.TrySendAsync(() => _hub.SendAsync("Hello from MainPage"));
        //if (!ok)
        //  await App.Current.MainPage.DisplayAlert("Blocked", "Sends are paused", "OK");
    }


 
    [RelayCommand]
    private async Task ShowCountdown()
    {
        var popup = new Popups.CountdownPopup(10, _sendGate);
        await App.Current.MainPage.ShowPopupAsync(popup);
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        // TODO: Navigate to settings page
    }


    [RelayCommand]
    private async Task ConnectNow()
    {

        await _hub.SetModeAsync(LinkMode.Auto);
        //// Try to connect WiFi if enabled
        //if (_hub.WifiEnabled)
        //    _ = _hub.SetModeAsync(CommHub.LinkMode.WifiOnly);

        //// Try to connect BLE if enabled
        //if (_hub.BleEnabled)
        //    _ = _hub.SetModeAsync(CommHub.LinkMode.BleOnly);

        //// If you want both, kick Auto mode
        //if (_hub.WifiEnabled && _hub.BleEnabled)
        //    _ = _hub.SetModeAsync(CommHub.LinkMode.Auto);

        //await App.Current.MainPage.DisplayAlert("Connecting", "Attempting to connect…", "OK");
    }

}