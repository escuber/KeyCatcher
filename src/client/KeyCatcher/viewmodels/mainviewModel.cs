//using CoreBluetooth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher_acc.converters;
using KeyCatcher_acc.services;
//using KeyCatcher_acc.Services;

using System.Net;
using System.Net.Sockets;
using System.Runtime;
using static System.Net.Mime.MediaTypeNames;
using Encoding = System.Text.Encoding;

namespace KeyCatcher_acc.ViewModels;
public partial class mainviewModel : ObservableObject
{
    [ObservableProperty] private CommHub hub;
    //private readonly KeyCatcherMuxService _mux;
    public KeyCatcherSettingsService _settings;
    private readonly KeyCatcherBleService _ble;
    private readonly KeyCatcherWiFiService _wifi;

    [ObservableProperty] private bool bleSta => hub.IsBleUp;

    [ObservableProperty] private bool wifiSta => hub.IsWifiUp;
    [ObservableProperty] private string status = "Idle";
    [ObservableProperty] private string messageText = string.Empty;
    [ObservableProperty] private string rsltText = string.Empty;
    
    [ObservableProperty] private int pauseSeconds = 0;
    public IReadOnlyList<int> PauseOptions { get; } = new[] { 0, 5, 10 };

    // EXPLICIT commands (no source generator required)
    public IRelayCommand PingCommand { get; }
    public IAsyncRelayCommand SendCommand { get; }
    public IAsyncRelayCommand SetWifiCommand { get; }
    public IAsyncRelayCommand SetBleCommand { get; }
    public IAsyncRelayCommand SetAutoCommand { get; }
    public IAsyncRelayCommand ConnectNowCommand { get; }




    //partial void OnPauseSecondsChanged(int value) => Preferences.Set("pauseSeconds", value);
    [RelayCommand]
    private async Task NavigateSettingsAsync()
    {
        await Shell.Current.GoToAsync("Settings");
    } 
    public mainviewModel(
        //KeyCatcherMuxService mux,
        CommHub cntlr,
        KeyCatcherSettingsService settings,
        KeyCatcherBleService ble,
        KeyCatcherWiFiService wifi)
    {
        //_mux = mux;
        _ble = ble; _wifi = wifi; _settings = settings;
        hub = cntlr;
       // _hub.PauseSeconds = Preferences.Get("pauseSeconds", 0);

        // wire status bubbling
        //_hub.PropertyChanged += (_, e) =>
        //{
        //    if (e.PropertyName is nameof(_hub.WifiStatus) or nameof(_hub.BleStatus)
        //        or nameof(_hub.ActiveTransport) or nameof(_hub.LastError))
        //    {
        //        OnPropertyChanged(nameof(WifiStatus));
        //        OnPropertyChanged(nameof(BleStatus));
        //        OnPropertyChanged(nameof(ActiveTransport));
        //        OnPropertyChanged(nameof(LastError));
        //    }
        //};

        // initialize commands explicitly
        PingCommand = new RelayCommand(Ping);
        SendCommand = new AsyncRelayCommand(SendAsync);
        SetWifiCommand = new AsyncRelayCommand(SetWifiAsync);
        SetBleCommand = new AsyncRelayCommand(SetBleAsync);
        SetAutoCommand = new AsyncRelayCommand(SetAutoAsync);
       // ConnectNowCommand = new AsyncRelayCommand(ConnectNowAsync);
    }

    /// <summary>
    /// public LinkState WifiStatus => _hub.WifiStatus;
    /// </summary>
    //public LinkState BleStatus => _hub.BleStatus;
    //public string ActiveTransport => _hub.ActiveTransport ?? "";
    //public string LastError => _hub.LastError ?? "";

    // ===== command handlers =====

    private void Ping() =>
        System.Diagnostics.Debug.WriteLine("PING CLICKED");

    private async Task SendAsync()
    {


        if (!await hub.SendAsync(messageText))
        {
            await Shell.Current.DisplayAlert("Error", "No link is up", "OK");
        }
        else { messageText = ""; }

        //if (string.IsNullOrWhiteSpace(MessageText)) return;
        ///await Task.Delay(PauseSeconds * 1000);

          //await _ble.SendTextAsync("wtf junour");
                //var c = hub.ActiveTransport;
        //      await _hub.SendLongMessageAsync(MessageText);
         
        //string? json = await hub.GetConfigAsync();
        //if (json == null) ShowToast("no link");
        //else              ProcessConfig(json);
    }

    private async Task SetWifiAsync()
    {
        _settings.InputType = "WIFI"; 
        _settings.Save();

        var rslt =await _wifi.PingAsync();

       // var isconn =  await _wifi.ConnectAsync();

       var rs=await _wifi.SendTextAsync(messageText);


        //r msg = _settings.MakeMessage();
       // var rslt2 = await _hub.SendTextAsync(msg);

      //  await _hub.ConnectAsync();
    }

    private async Task SetBleAsync()
    {

       // var rslt =hub.SendAsync(messageText);


        var isconn = await _ble.ConnectAsync();        
        var rs = await _ble.SendTextAsync(messageText);


        // _settings.InputType = "BLE";
        //  _settings.Save();
        //  _hub.LoadFromXaml

        //var msg = _settings.MakeMessage();
        // var rslt2 = await _hub.SendTextAsync(msg);

    }

    private async Task SetAutoAsync()
    {
        var rslt = await hub.SendAsync("wrkrwrkr<enter>");
        if (rslt) {
            messageText = rslt ? "sent" : "not sent";
        }

        //_settings.InputType = "BOTH";
        //_settings.Save();


        //var msg = _settings.MakeMessage();
       // var rslt2 = await _hub.SendTextAsync(msg);

    }

    //private Task ConnectNowAsync() => _hub.SendAsync();
}

