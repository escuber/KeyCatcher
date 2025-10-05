using CommunityToolkit.Maui.Views;
using KeyCatcher.ViewModels;
using KeyCatcher.services;
using System.Windows.Input;

namespace KeyCatcher.Popups;

public partial class MacroManager : Popup
{
    public MacroManagerViewModel VM { get; }
    public KeyCatcherSettingsService _settings;
    public MacroManager(KeyCatcherSettingsService settings, CommHub hub )
    {
        InitializeComponent();
        
        _settings= settings;
        VM = new MacroManagerViewModel(settings, hub);
        BindingContext = VM;
        CloseCommand = new Command(async () => await CloseAsync());
    }
    private void BtnClose_Clicked(object sender, EventArgs e)
    {// real close
        _settings.Save();
        CloseAsync().Wait();
    }


    public async Task SaveAndCloseAsync()
    {
        //// 1. Write VM changes to settings service
        //VM.ApplyToService(_settings);

        //// 2. Save to local preferences
        //_settings.Save();

        //// 3. Push config to device if hub is connected (WiFi or BLE)
        //if (_hub != null && _hub.IsAnyUp)
        //{
        //    var payload = _settings.MakeMessage();
        //    try
        //    {
        //        // 20s timeout is typical, adjust as needed
        //        var ok = await _hub.SendAsync(payload);
        //        if (!ok)
        //        {
        //            await Application.Current.MainPage.DisplayAlert(
        //                "Device not updated",
        //                "Saved locally, but the device did not respond. Reconnect and retry.",
        //                "OK");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await Application.Current.MainPage.DisplayAlert("Update failed", ex.Message, "OK");
        //    }
        //}
        //else
        //{
        //    // Optionally alert: Saved locally, device not updated yet
        //    // await Application.Current.MainPage.DisplayAlert("Not connected", "Config saved. Connect and retry.", "OK");
        //}

        //// 4. Close the popup
        await CloseAsync();
    }

    async void onsaveandclose2(object sender, EventArgs e)
    {
        await VM.SaveAndClose();
        //system.diagnostics.debug.writeline("onsaveandclose fired");
        // do your save/apply logic here (or delegate to vm)
        // close with a result if you want:
        ///this.CloseAsync
        VM.TryPushConfigAsync();
        await CloseAsync();
        // or: await closeasync(true);  <-- requires `async void onsaveandclose(...)`
    }
    public ICommand CloseCommand { get; }
}
