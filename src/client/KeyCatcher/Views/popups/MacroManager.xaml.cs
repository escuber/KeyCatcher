using CommunityToolkit.Maui.Views;
using KeyCatcher.ViewModels;
using KeyCatcher.services;
using System.Windows.Input;

namespace KeyCatcher.Popups;

public partial class MacroManager : Popup
{
    public MacroManagerViewModel VM { get; }

    public MacroManager(KeyCatcherSettingsService settings, CommHub? hub = null)
    {
        InitializeComponent();
        VM = new MacroManagerViewModel(settings, hub);
        BindingContext = VM;
        CloseCommand = new Command(async () => await CloseAsync());
    }

    public ICommand CloseCommand { get; }
}
