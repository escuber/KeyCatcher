using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;

namespace KeyCatcher
{
    public partial class Help : Popup
    {
        public Help()
        {
            InitializeComponent();
        }

        private void OnContactClicked(object sender, EventArgs e)
        {
            try
            {
                Launcher.OpenAsync(new Uri("mailto:jimgaudette@gmail.com?subject=KeyCatcher%20Help"));
            }
            catch
            {
                // Optional: handle launch error (show alert, etc)
            }
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
             CloseAsync();
        }
    }
}
