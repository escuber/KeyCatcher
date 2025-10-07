using CommunityToolkit.Maui.Views;

namespace KeyCatcher.Views.popups;
public partial class menu : Popup
{
    public menu()
    {
        InitializeComponent();
    }

    private void OnHelpClicked(object sender, EventArgs e)
    {
        CloseAsync(); // closes popup
        Application.Current.MainPage.DisplayAlert("Help", "Help clicked!", "OK");
    }

    private void OnAboutClicked(object sender, EventArgs e)
    {
        CloseAsync();
        Application.Current.MainPage.DisplayAlert("About", "About KeyCatcher", "OK");
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        CloseAsync();
        // Navigate or open settings
    }
}