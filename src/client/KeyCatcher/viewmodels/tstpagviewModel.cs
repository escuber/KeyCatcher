using KeyCatcher_acc.services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher.services;

namespace KeyCatcher.ViewModels;

public partial class tstpagViewModel : ObservableObject
{
    private readonly CommHub _hub;
    private readonly KeyCatcherSettingsService _settings;
    private readonly KeyCatcherBleService _ble;
    private readonly KeyCatcherWiFiService _wifi;

    // Proxy hub state
    public bool BleUp => _hub?.IsBleUp ?? false;
    public bool WifiUp => _hub?.IsWifiUp ?? false;

    [ObservableProperty] private string inputType = "BOTH";
    [ObservableProperty] private string status = "Idle";
    [ObservableProperty] private string messageText = string.Empty;
    [ObservableProperty] private string rsltText = string.Empty;

    public tstpagViewModel(
        CommHub hub,
        KeyCatcherSettingsService settings,
        KeyCatcherBleService ble,
        KeyCatcherWiFiService wifi)
    {
        _hub = hub;
        _settings = settings;
        _ble = ble;
        _wifi = wifi;

        InputType = _settings.InputType;

        // Bubble hub status changes to UI
        _hub.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CommHub.IsBleUp))
                OnPropertyChanged(nameof(BleUp));
            else if (e.PropertyName == nameof(CommHub.IsWifiUp))
                OnPropertyChanged(nameof(WifiUp));
        };
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        if (!await _hub.SendAsync(MessageText))
            await Shell.Current.DisplayAlert("Error", "No link is up", "OK");
        else
            MessageText = string.Empty;
    }
}