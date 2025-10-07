using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyCatcher.services
{
    public enum LinkState
    {
        Off,     // Not connected or user disabled
        Trying,  // Attempting to connect
        On,      // Connected
        Error    // Failed
    }

    public enum Transport { None, Wifi, Ble }
    public enum LinkMode { Auto, WifiOnly, BleOnly }
    public class SendHealthGate
    {
        private readonly SemaphoreSlim _sem = new(1, 1);

        // For real messages: must wait
        public async Task<T> RunSendAsync<T>(Func<Task<T>> action)
        {
            await _sem.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                _sem.Release();
            }
        }

        // For health checks: skip if busy
        public async Task<T?> TryRunHealthCheckAsync<T>(Func<Task<T>> action)
        {
            if (!await _sem.WaitAsync(0)) // immediate try
                return default;           // skip gracefully

            try
            {
                return await action();
            }
            finally
            {
                _sem.Release();
            }
        }
    }
    public partial class CommHub : ObservableObject, INotifyPropertyChanged
    {

        private bool readConfig = false;
        private readonly KeyCatcherBleService _ble;
        private readonly KeyCatcherWiFiService _wifi;
        private readonly KeyCatcherSettingsService settings;
        [ObservableProperty] private int pauseSeconds = 0;
        private bool _initialized;
        //private readonly SendHealthGate _gate = new();
        public CommHub(KeyCatcherBleService ble, KeyCatcherWiFiService wifi, KeyCatcherSettingsService ssettings)
        {
            settings = ssettings;
            _ble = ble;
            _wifi = wifi;
            PauseSeconds = Preferences.Get("pauseSeconds", 0);
        }
        partial void OnPauseSecondsChanged(int value) => Preferences.Set("pauseSeconds", value);
        public async Task InitializeAsync()
        {
            await _wifi.ConnectAsync();
            if (_initialized) return;
            _initialized = true;

            _ = MaintainWifiAsync();
            _ = MaintainBleAsync();
        }
        public async Task<string?> GetConfigAsync()
        {

            var config = "";

            // Use WiFi if connected, else BLE
            if (_wifi.IsConnected)
                config = await _wifi.GetConfigAsync();
            if (_ble.IsConnected)
                config = await _ble.GetConfigAsync();
            Log(config);
            settings.ApplyDeviceJson(config);

            return config;
        }
        public LinkMode Mode { get; private set; } = LinkMode.Auto;
        public bool WifiEnabled { get; private set; } = true;
        public bool BleEnabled { get; private set; } = true;

        private bool _isWifiUp;
        public bool IsWifiUp
        {
            get => _isWifiUp;
            set
            {

                Debug.WriteLine($"[Hub] IsWifiUp set to {value}");
                if (Set(ref _isWifiUp, value)) RecomputeBest();

            }
        }

        private bool _isBleUp;
        public bool IsBleUp
        {
            get => _isBleUp;
            set { if (Set(ref _isBleUp, value)) RecomputeBest(); }
        }

        public bool IsAnyUp => Best != Transport.None;

        private Transport _best = Transport.None;
        public Transport Best
        {
            get => _best;
            private set => Set(ref _best, value);
        }

        public event Action<Transport>? TransportChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        // --------------------------------------------------------------------
        // Mode switching
        // --------------------------------------------------------------------
        public Task SetModeAsync(LinkMode mode)
        {
            Mode = mode;
            WifiEnabled = mode != LinkMode.BleOnly;
            BleEnabled = mode != LinkMode.WifiOnly;

            // Clear status when disabled
            if (!WifiEnabled) IsWifiUp = false;
            if (!BleEnabled) IsBleUp = false;

            return Task.CompletedTask;
        }

        // --------------------------------------------------------------------
        // Sending
        // --------------------------------------------------------------------
        private volatile bool _busy;
        public bool IsBusy => _busy;

        private static string Finish(string s) =>
            s.EndsWith("<<END>>", StringComparison.Ordinal) ? s : s + "<<END>>";

        private readonly SendHealthGate _sendGate = new();   // Used for real sends (SendAsync)
        private readonly SendHealthGate _probeGate = new();
        void Log(string s)
        {
            //MainThread.BeginInvokeOnMainThread(() =>
            //{
            // LogLabel.Text = $"{DateTime.Now:T} {s}\n{LogLabel.Text}";
            //StatusLabel.Text = s;
            //});
            Debug.WriteLine("[CommHub] "+s);
        }

        bool pendingReconnect = false;
        public async Task<bool> SendConfigAsync(string configText, bool touchesWifi)
        {
            bool sent = await SendAsync(configText);

            if (!sent) return false;

            if (touchesWifi)
            {
                pendingReconnect = true;
                // Notify UI: waiting for reconnect
                for (int i = 0; i < 10; i++) // Try 10x, 1s apart
                {
                    await Task.Delay(1000);
                    var ip = await _wifi.DiscoverDeviceIpAsync(4210, 1500);
                    if (ip != null)
                    {
                        _wifi.MarkConnected(ip); // reacquire connection
                        pendingReconnect = false;
                        IsWifiUp = true;
                        // Notify UI: success!
                        return true;
                    }
                }
                pendingReconnect = false;
                IsWifiUp = false;
                // UI: after retries, show "failed"
                return false;
            }
            return true;
        }
        public async Task<bool> SendAsync(string text)
        {

            return await _sendGate.RunSendAsync(async () =>
            {
                //if (PauseSeconds > 0)
                //    await Task.Delay(PauseSeconds * 1000);

                // Use WiFi if connected, else BLE
                if (_wifi.IsConnected)
                {
                    Log("sending wifi");
                    return await _wifi.SendTextAsync(text);
                }
                if (IsBleUp)
                //_ble.IsConnected)
                {
                    Log("sending BLE");
                    return await _ble.SendAsync(text);
                }

                return false;
            });
        }

            //return await _gate.RunSendAsync(async () =>
            //{
            //    if (PauseSeconds > 0)
            //        await Task.Delay(PauseSeconds * 1000);

            //    // Use WiFi if connected, else BLE
            //    if (_wifi.IsConnected)
            //    {
            //        Log("sending wifi");
            //        return await _wifi.SendTextAsync(text);
            //    }
            //    if (_ble.IsConnected)
            //    {
            //        Log("sending BLE");
            //        return await _ble.SendAsync(text);
            //    }

            //    return false;
            //});
        //}
        //public async Task<bool> ProbeAsync()
        //{

        //    bool? result = await _probeGate.TryRunHealthCheckAsync(async () =>
        //    {
        //        return await _wifi.PingAsync();
        //    });

        //    //bool? result = await _gate.TryRunHealthCheckAsync(async () =>
        //    //{
        //    //    return await _wifi.PingAsync();
        //    //});

        //    return result ?? false; // if skipped, treat as failed probe
        //}

        // --------------------------------------------------------------------
        // Keepalive probes
        // --------------------------------------------------------------------
        private async Task MaintainWifiAsync()
        {
            while (true)
            {
                try
                {
                    if (!WifiEnabled) { IsWifiUp = false; }
                    else IsWifiUp = await _wifi.ProbeAsync();
                }
                catch { IsWifiUp = false; }

                await Task.Delay(5000);
            }
        }
        private CancellationTokenSource? _bleCts;

        private async Task MaintainBleAsync()
        {


            return;
            while (true)
            {
                try
                {
                    if (!BleEnabled) { IsBleUp = false; }
                    else IsBleUp = await _ble.ProbeAsync();
                }
                catch { IsBleUp = false; }

                await Task.Delay(5000);
            }
        }
        private bool _wasDisconnected = true;
        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        private async void RecomputeBest()
        {
            var newBest =
                (IsWifiUp && WifiEnabled) ? Transport.Wifi :
                (IsBleUp && BleEnabled) ? Transport.Ble :
                Transport.None;

            if (newBest != Best)
            {
                Best = newBest;
                TransportChanged?.Invoke(newBest);
            }

            bool nowConnected = newBest != Transport.None;

            // Only trigger GetConfig once both layer 1 and 2 are confirmed ready
            if (_wasDisconnected && nowConnected)
            {
                await WaitForTransportReadyAsync(newBest);
                _ = GetConfigAsync();
            }

            _wasDisconnected = !nowConnected;
        }

        private async Task WaitForTransportReadyAsync(Transport t)
        {
            const int maxWait = 1000;
            const int step = 100;
            int waited = 0;

            while (waited < maxWait)
            {
                bool ready = t switch
                {
                    Transport.Wifi => _wifi?.IsConnected == true,
                    Transport.Ble => _ble?.IsConnected == true,
                    _ => false
                };
                if (ready) return;

                await Task.Delay(step);
                waited += step;
            }

            Debug.WriteLine($"[CommHub] WaitForTransportReady timed out after {waited}ms");
        }
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged(string? name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
