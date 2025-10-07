using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher.models;
using KeyCatcher.services;

namespace KeyCatcher.ViewModels;

public partial class MacroManagerViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<MacroItem> macros = new();
    [ObservableProperty] private MacroItem editingMacro = new();
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isBusy;

    private readonly KeyCatcherSettingsService _settings;
    private readonly CommHub? _hub;

    public MacroManagerViewModel(KeyCatcherSettingsService settings, CommHub? hub = null)
    {
        _settings = settings;
        _hub = hub;

        LoadMacros();
    }

    private void LoadMacros()
    {
        Macros.Clear();
        foreach (var m in _settings.Macros)
            Macros.Add(m);
    }

    [RelayCommand]
    private void AddMacro()
    {
        EditingMacro = new MacroItem();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditMacro(MacroItem macro)
    {
        if (macro == null) return;
        EditingMacro = new MacroItem { Name = macro.Name, Content = macro.Content };
        IsEditing = true;
    }

    [RelayCommand]
    private void RemoveMacro(MacroItem macro)
    {
        if (macro == null) return;

        Macros.Remove(macro);
        _settings.Macros = Macros.ToList();
        _settings.Save();

        _ = TryPushConfigAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingMacro = new MacroItem();
    }

    [RelayCommand]
    public async Task SaveAndClose()
    {
        try
        {

            IsBusy = true;   // show spinner
                             //VM.ApplyToService(_settings);

            await SaveMacroAsync();
            ///ApplyToService(_settings);
            _settings.Save();
            if (_hub != null && _hub.IsAnyUp)
            {
                var payload = _settings.MakeMessage();
                //await _hub.SendAsync(payload);
            }
            //await close();


            //IsEditing = false;

            //if (Hub is not null)
            //{
            //    var payload = _settings.MakeMessage();
            //    //try { 
            //    //await Hub.ap(payload, 20000);
            //    //}
            //    //catch { /* ignore transport error here */ }
            //}
        }
        finally
        {
            IsBusy = false;  // hide spinner



        }
    }

    [RelayCommand]
    private async Task SaveMacroAsync()
    {
        if (string.IsNullOrWhiteSpace(EditingMacro?.Name))
        {
            IsEditing = false;
            EditingMacro = new MacroItem();
            return;
        }

        var existing = Macros.FirstOrDefault(x => x.Name == EditingMacro.Name);
        if (existing != null)
        {
            var idx = Macros.IndexOf(existing);
            Macros[idx] = new MacroItem
            {
                Name = EditingMacro.Name,
                Content = EditingMacro.Content
            };
        }
        else
        {
            Macros.Add(new MacroItem
            {
                Name = EditingMacro.Name,
                Content = EditingMacro.Content
            });
        }

        _settings.Macros = Macros.ToList();
        _settings.Save();

       // await TryPushConfigAsync();

        IsEditing = false;
        EditingMacro = new MacroItem();
    }

    public  async Task TryPushConfigAsync()
    {
        if (_hub == null || !_hub.IsAnyUp) return;

        try
        {
            IsBusy = true;
            var payload = _settings.MakeMessage();
            var ok = await _hub.SendAsync(payload);
            if (!ok)
            {
                await App.Current.MainPage.DisplayAlert(
                    "Device not updated",
                    "Saved locally, but the device did not respond. Reconnect and retry.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Update failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
