using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KeyCatcher_acc.services
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
        private readonly KeyCatcherBleService _ble;
        private readonly KeyCatcherWiFiService _wifi;
        [ObservableProperty] private int pauseSeconds = 0;
        private bool _initialized;
        //private readonly SendHealthGate _gate = new();
        public CommHub(KeyCatcherBleService ble, KeyCatcherWiFiService wifi)
        {
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

        public LinkMode Mode { get; private set; } = LinkMode.Auto;
        public bool WifiEnabled { get; private set; } = true;
        public bool BleEnabled { get; private set; } = true;

        private bool _isWifiUp;
        public bool IsWifiUp
        {
            get => _isWifiUp;
            private set { if (Set(ref _isWifiUp, value)) RecomputeBest(); }
        }

        private bool _isBleUp;
        public bool IsBleUp
        {
            get => _isBleUp;
            private set { if (Set(ref _isBleUp, value)) RecomputeBest(); }
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

        //public async Task<bool> SendAsync(string text)
        //{
        //    var payload = Finish(text);

        //    _busy = true;
        //    try
        //    {
        //        if (Mode == LinkMode.WifiOnly && WifiEnabled)
        //            return await _wifi.SendTextAsync(payload);

        //        if (Mode == LinkMode.BleOnly && BleEnabled)
        //            return await _ble.SendTextAsync(payload);

        //        // Auto mode
        //        if (WifiEnabled && await _wifi.ProbeAsync())
        //            return await _wifi.SendTextAsync(payload);

        //        if (BleEnabled && await _ble.ProbeAsync())
        //            return await _ble.SendTextAsync(payload);

        //        return false;
        //    }
        //    finally
        //    {
        //        _busy = false;
        //    }
        //}
        private readonly SendHealthGate _gate = new();

        public async Task<bool> SendAsync(string text)
        {
            return await _gate.RunSendAsync(async () =>
            {
                if (PauseSeconds > 0)
                    await Task.Delay(2 * 1000);

                // Use WiFi if connected, else BLE
                if (_wifi.IsConnected)
                    return await _wifi.SendTextAsync(text);
                if (_ble.IsConnected)
                    return await _ble.SendAsync(text);

                return false;
            });
        }
        public async Task<bool> ProbeAsync()
        {
            bool? result = await _gate.TryRunHealthCheckAsync(async () =>
            {
                return await _wifi.PingAsync();
            });

            return result ?? false; // if skipped, treat as failed probe
        }
        //public async Task<bool> ApplyConfigAsync(string configJson)
        //{
        //    _busy = true;
        //    try
        //    {
        //        if (Mode == LinkMode.WifiOnly && WifiEnabled)
        //            return await _wifi.SendConfigAsync(configJson);

        //        if (Mode == LinkMode.BleOnly && BleEnabled)
        //            return await _ble.SendConfigAsync(configJson);

        //        // Auto mode: prefer WiFi, fallback to BLE
        //        if (WifiEnabled && await _wifi.ProbeAsync())
        //            return await _wifi.SendConfigAsync(configJson);

        //        if (BleEnabled && await _ble.ProbeAsync())
        //            return await _ble.SendConfigAsync(configJson);

        //        return false;
        //    }
        //    finally
        //    {
        //        _busy = false;
        //    }
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

        private void StartBleMaintainLoop()
        {
            _bleCts?.Cancel();
            _bleCts = new CancellationTokenSource();
            _ = MaintainBleAsync(_bleCts.Token);
        }

        private async Task MaintainBleAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!BleEnabled)
                    {
                        IsBleUp = false;
                    }
                    else
                    {
                        IsBleUp = await _ble.ProbeAsync();
                    }
                }
                catch
                {
                    IsBleUp = false;
                }

                await Task.Delay(5000, ct);
            }
        }

        private async Task MaintainBleAsync()
        {
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

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        private void RecomputeBest()
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
