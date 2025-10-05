using CommunityToolkit.Maui.Views;
using KeyCatcher.ViewModels;
using KeyCatcher.services;
using System.Windows.Input;

namespace KeyCatcher.Popups;

public partial class MacroManager : Popup
{
    public MacroManagerViewModel VM { get; }
    public KeyCatcherSettingsService _settings;
    public MacroManager(KeyCatcherSettingsService settings, CommHub? hub = null)
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


    public ICommand CloseCommand { get; }
}
