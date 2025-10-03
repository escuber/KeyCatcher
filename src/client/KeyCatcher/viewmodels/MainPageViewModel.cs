//KeyCatcher_acc.services;
///using Android.Provider;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher.models;
using KeyCatcher.Popups;
using KeyCatcher.services;

namespace KeyCatcher.ViewModels;

public partial class MainPageViewModel : ObservableObject
{



    //readonly KeyCatcherSettingsService _settings;

    //[ObservableProperty] public  bool wifiUp;
    //[ObservableProperty] bool bleUp;
    //private readonly CommHub _hub;
    //private readonly SendGate _sendGate;
    //public KeyCatcherWiFiService wifi;
    //public KeyCatcherBleService ble;
    ///// <summary>
    ///// public string BleIconColor => IsBleUp ? "DodgerBlue" : "Grey";
    ///// </summary>
    ////Colors.DodgerBlue : Colors.Gray;
    //public string WifiIconColor => WifiUp ? "LimeGreen" : "Red";
    //public CommHub Hub => _hub;
    //[ObservableProperty] private string messageText = string.Empty;

    //[ObservableProperty] private int pauseSeconds = 0;
    //public IReadOnlyList<int> PauseOptions { get; } = new[] { 0, 5, 10 };
    //public bool IsWifiUp
    //{
    //    get => (bool)GetValue(IsWifiUpProperty);
    //    set
    //    {
    //        SetValue(IsWifiUpProperty, value);
    //        Debug.WriteLine($"[Header] IsWifiUp set to {value}");
    //        OnPropertyChanged(nameof(WifiIconColor)); // <---- THIS TRIGGERS UI UPDATE
    //    }
    ////}
    //public MainPageViewModel(
    //    CommHub hub, 
    //    SendGate sendGate, 
    //    KeyCatcherSettingsService setting, 
    //    KeyCatcherWiFiService wwifi, 
    //    KeyCatcherBleService bble)
    //{
    //    ble = bble;        wifi = wwifi;        _hub = hub;        _sendGate = sendGate;

    //    _settings = setting;

    //    _hub.PropertyChanged += (s, e) =>
    //    {
    //        if (e.PropertyName == nameof(_hub.IsWifiUp)) WifiUp = _hub.IsWifiUp;
    //        if (e.PropertyName == nameof(_hub.IsBleUp)) BleUp = _hub.IsBleUp;
    //    };
    //    hub.PauseSeconds= Preferences.Get("pauseSeconds", 0);
    //    PauseSeconds = hub.PauseSeconds;
    //    _ = InitializeAsync(hub, setting);
    //}



    [ObservableProperty] private int pauseSeconds = 0;
    public IReadOnlyList<int> PauseOptions { get; } = new[] { 0, 5, 10 };


    [ObservableProperty] private string messageText = string.Empty;

    readonly KeyCatcherSettingsService _settings;


    private readonly CommHub _hub;
    private readonly SendGate _sendGate;

    public KeyCatcherWiFiService wifi;
    public KeyCatcherBleService ble;

    public CommHub Hub => _hub;

    [ObservableProperty] public bool wifiUp;
    [ObservableProperty] public bool bleUp;

    public string WifiIconColor => WifiUp ? "LimeGreen" : "Grey";
    public string BleIconColor => BleUp ? "DodgerBlue" : "Grey"; // <--- Fix spelling here!

    partial void OnWifiUpChanged(bool value) => OnPropertyChanged(nameof(WifiIconColor));
    partial void OnBleUpChanged(bool value) => OnPropertyChanged(nameof(BleIconColor));

    // ... other properties/ctor ...

    public MainPageViewModel(
        CommHub hub,
        SendGate sendGate,
        KeyCatcherSettingsService setting,
        KeyCatcherWiFiService wwifi,
        KeyCatcherBleService bble)
    {
        ble = bble; wifi = wwifi; _hub = hub; _sendGate = sendGate; _settings = setting;

        _hub.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_hub.IsWifiUp)) WifiUp = _hub.IsWifiUp;
            if (e.PropertyName == nameof(_hub.IsBleUp)) BleUp = _hub.IsBleUp;
        };

        hub.PauseSeconds = Preferences.Get("pauseSeconds", 0);
        PauseSeconds = hub.PauseSeconds;
        // ... other ctor code ...
    }

    //partial void OnWifiUpChanged(bool value)
    //{
    //    OnPropertyChanged(nameof(WifiIconColor)); // <-- This triggers XAML updates
    //}
    //partial void OnBleUpChanged(bool value)
    //{
    //    OnPropertyChanged(nameof(BleIconColor)); // <-- This triggers XAML updates
    //}


    //public bool IsWifiUp
    //{
    //    get => (bool)GetValue(IsWifiUpProperty);
    //    set
    //    {
    //        SetValue(IsWifiUpProperty, value);
    //        Debug.WriteLine($"[Header] IsWifiUp set to {value}");
    //        OnPropertyChanged(nameof(WifiIconColor)); // <---- THIS TRIGGERS UI UPDATE
    //    }
    ////}















    [RelayCommand]
    private async Task Clear()
    {
        MessageText = "";

        //await Shell.Current.GoToAsync("Settings");
    }
    private async Task SendAsync()
    {
        //  await Task.Delay(PauseSeconds * 1000);

        // await _ble.SendTextAsync(messageText);
        //return;

        if (!await _hub.SendAsync(messageText))
        {
            await Shell.Current.DisplayAlert("Error", "No link is up", "OK");
        }
        else { MessageText = ""; }
    }
    partial void OnPauseSecondsChanged(int value) => Preferences.Set("pauseSeconds", value);
    private async Task InitializeAsync(CommHub commman, KeyCatcherSettingsService settings)
    {
        await commman.InitializeAsync();

        var configJson = await commman.GetConfigAsync();
        if (configJson != null)
        {
            settings.ApplyDeviceJson(configJson);
            // update UI as needed, e.g., set observable props, etc.
        }
        else
        {
            // Show UI for "no connection" or onboarding/help, etc.
        }
    }
    private async Task ShowSsidsAsync()
    {
        var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        if (page is null) return;

        // construct popup and pass services
        var popup = new WifiCreds(_settings, Hub);

        // await the popup result
        var result = await page.ShowPopupAsync(popup);



        //var popup = new KeyCatcher.Popups.WifiCreds(_settings, Hub); 


        // if saved/updated, refresh any UI that mirrors settings
        //   if (result.ToString() as string == "updated")
        /// {
        // pull in new settings if your VM mirrors them
        // e.g., if you show a list of creds on this page:
        // Creds = _settings.GetNetworks();  OnPropertyChanged(nameof(Creds));
        // or update any indicator text, etc.
        //}
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
    private async Task showNetwork()
    {
        //var popup = new CommunityToolkit.Maui.Views.Popup
        //{
        //    Content = new Label
        //    {
        //        Text = "This is a test popup.",
        //        BackgroundColor = Colors.White,
        //        Padding = 40
        //    }
        //};


        //await Shell.Current.CurrentPage.ShowPopupAsync(popup);

        var conf = "";
        //   while (true)
        // {
        //conf = await _hub.g
        //await KeyCatcherBleService.FindAndGetConfigAsync(CrossBluetoothLE.Current, CrossBluetoothLE.Current.Adapter);
        var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        if (page is null) return;

        // construct popup and pass services
        var popup = new KeyCatcher.Popups.WifiCreds(_settings, Hub);//(_settings, Hub);

        // await the popup result
        var result = await page.ShowPopupAsync(popup);
        //var msg =_settings.MakeMessage();
        //await Hub.SendAsync(msg);


        //await showNetwork();
        return;

        //return;
        //var popup = new KeyCatcher.Popups.WifiCreds(_settings, Hub);

        ////var popup = new CommunityToolkit.Maui.Views.Popup
        ////{
        ////    Content = new Label { Text = "Hello, world!", Padding = 40, BackgroundColor = Colors.White }
        ////};
        //await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        //return;
        //MainThread.BeginInvokeOnMainThread(() =>
        //{
        //    //  if (_popup == null)
        //    //{
        //    //_popup = new BleProgressPopup { StatusText = status };
        //    var popup = new BleProgressPopup();
        //    //KeyCatcher.Popups.WifiCreds(_settings, Hub);
        //        Application.Current?.MainPage?.ShowPopup(popup);
        //    //}
        //    //else
        //    //{
        //        //popup.StatusText = status;
        //    //}
        //});


        //var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        //    if (page is null) return;

        //    // construct popup and pass services
        //    var popup = new KeyCatcher.Popups.WifiCreds(_settings, Hub);


        //    // await the popup result
        //    var result = await page.ShowPopupAsync(popup);




    }
    [RelayCommand]
    void ToggleWifi()
    {
        Hub.IsBleUp = !Hub.IsBleUp;
        // Hub.IsWifiUp = !Hub.IsWifiUp;
    }

    [RelayCommand]
    private async Task Send()
    {

        //wait ble.SendAsync("hellp  ");
        //string text = new string('A', 3000);// + "<<END>>";
        //var connn = wifi.IsConnected;
        //wifi.IsConnected = false;

        //wifi.IsConnected = true;

        //Hub.IsWifiUp = true;

        //SendTextAsync(text);


        //return;
        // messageText = text;
        var ok = await _sendGate.TrySendAsync(() => _hub.SendAsync(messageText));
        if (!ok)
            await App.Current.MainPage.DisplayAlert("Blocked", "Sends are paused", "OK");

        return;



        //var bconf = await ble.GetConfigAsync();
        //while(bconf == null)
        //{
        //    await Task.Delay(500);
        //    bconf = await ble.GetConfigAsync();
        //}   






        var cconf = await wifi.GetConfigAsync();

        //_settings.ApplyDeviceJson(conf);
        var msg = _settings.MakeMessage();

        _settings.SSID = "mxxyDadsCar";
        _settings.Password = "4c4c4c4c";
        //_settings.Creds = "[\"DadsCar\":\"4c4c4c4c\"]";

        _settings.creds =
               new List<WifiCredential> { new WifiCredential { SSID = "DADNET", Password = "4c4c4c4c" } };

        var msg2 = _settings.MakeMessage();

        await ble.SendAsync(msg2);


        await wifi.SendTextAsync(msg2);


        var blmyconf = await ble.GetConfigAsync();
        var myconf = await wifi.GetConfigAsync();
        //Hub.GetConfigAsync();

        //_settings.ApplyDeviceJson(myconf);
        // var msg =_settings.MakeMessage();

        // _settings.SSID="xDadsCar";
        // _settings.Password="4c4c4c4c";
        // //_settings.Creds = "[\"DadsCar\":\"4c4c4c4c\"]";

        // _settings.creds =
        //        new List<WifiCredential> { new WifiCredential { SSID = "DADNET", Password = "4c4c4c4c" } };

        msg2 = _settings.MakeMessage();
        await Hub.SendAsync(msg2);
        //Hub.SendAsync(msg2);
        //var popup = new CountdownPopup(pauseSeconds, _sendGate);
        //await Application.Current.MainPage.ShowPopupAsync(popup);

        var mmyconf = await //wifi.GetConfigAsync();
        Hub.GetConfigAsync();


        //// After the countdown is finished (popup auto-closes), send the message
        //// Optionally check gate if user can stop/abort
        //var ok = await _sendGate.TrySendAsync(() => _hub.SendAsync(messageText));

        //await Task.Delay(PauseSeconds * 1000);

        //var ok = await _sendGate.TrySendAsync(() => _hub.SendAsync(messageText));
        //if (!ok)
        //  await App.Current.MainPage.DisplayAlert("Blocked", "Sends are paused", "OK");

        // await _ble.SendTextAsync(messageText);
        //return;

        //if (!await _hub.SendAsync(messageText))
        //{
        //    await Shell.Current.DisplayAlert("Error", "No link is up", "OK");
        //}
        //else {
        ////    MessageText = ""; 
        //}

        //return;
        // var conf = "";
        // //   while (true)
        // // {
        // //conf = await _hub.g
        // //await KeyCatcherBleService.FindAndGetConfigAsync(CrossBluetoothLE.Current, CrossBluetoothLE.Current.Adapter);
        // //var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        // //if (page is null) return;

        // // construct popup and pass services
        // var popup = new KeyCatcher.Popups.WifiCreds(_settings, Hub);

        // // await the popup result
        // var result = await page.ShowPopupAsync(popup);
        // //page.ShowPopup(popup);



        // //await showNetwork();
        // return;



        // var any=Hub.IsAnyUp.ToString();

        // var myconf = await ble.GetConfigAsync();

        // _settings.ApplyDeviceJson(myconf);
        // var msg =_settings.MakeMessage();

        // _settings.SSID="xDadsCar";
        // _settings.Password="4c4c4c4c";
        // //_settings.Creds = "[\"DadsCar\":\"4c4c4c4c\"]";

        // _settings.creds =
        //        new List<WifiCredential> { new WifiCredential { SSID = "DADNET", Password = "4c4c4c4c" } };

        // var msg2 = _settings.MakeMessage();

        // ble.SendAsync(msg2);

        // var x = 100;





        // var aaa=conf;

        //// i//f (conf != null)
        //  //       break;
        //     Thread.Sleep(500);  
        //// }



        // _settings.ApplyDeviceJson(conf);
        // var ssid = _settings.SSID;
        // var password = _settings.Password;
        // var amsg = MessageText + "<<END>>";




        //string longMsg = new string('A', 3000) + "<<END>>";
        // var rslt=await ble.SendAsync(longMsg);

        //var ok = await _hub.SendAsync(longMsg);
        //  if (!ok)
        //     await App.Current.MainPage.DisplayAlert("Blocked", "Send failed", "OK");



    }

    [ObservableProperty]
    public string wifisUp => Hub.IsWifiUp ? "Wi-Fi: Up" : "Wi-Fi: Down";

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