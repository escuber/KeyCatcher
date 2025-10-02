using CommunityToolkit.Maui.Views;
using KeyCatcher.ViewModels;
using KeyCatcher.services;
using KeyCatcher.models; // WifiCredential
using System.Windows.Input;

namespace KeyCatcher.Popups
{
    public partial class WifiCreds : Popup
    {
        private readonly KeyCatcherSettingsService _settings;
        private readonly CommHub? _hub;
        public WifiCredsViewModel VM { get; }

        // You can inject CommHub if you want to push <setup> after save
        public WifiCreds(KeyCatcherSettingsService settings, CommHub? hub = null)
        {
            InitializeComponent();
            _settings = settings;
            _hub = hub;

            VM = new WifiCredsViewModel();
            VM.InitFromService(_settings); // load current SSID, pss, and backups
            BindingContext = VM;

            // lightweight commands handled by the popup itself
            CancelEditCommand = new Command(() =>
            {
                VM.IsEditing = false;
                VM.EditingNetwork = new WifiCredential();
            });

            CloseCommand = new Command(() => CloseAsync());

            SaveAndCloseCommand = new Command(async () =>
            {
                // Persist to settings
                VM.ApplyToService(_settings);

                // Optional: push to device as <setup> and wait for the link to come back
                // If you prefer to do this elsewhere, remove this block.
                if (_hub is not null)
                {
                    var payload = _settings.MakeMessage(); // emits <setup>...<<END>>
                //  try { await _hub.ApplyConfigAsync(payload); }
                  //catch { /* swallow apply errors, the UI save already persisted */ }
                }

                CloseAsync();
            });
            ResizeToViewport();
            DeviceDisplay.MainDisplayInfoChanged += (_, __) => MainThread.BeginInvokeOnMainThread(ResizeToViewport);
        }
        void ResizeToViewport()
        {
            var di = DeviceDisplay.MainDisplayInfo;
            var w = di.Width / di.Density;
            var h = di.Height / di.Density;

            Card.WidthRequest = Math.Min(w * 0.98, 720);
            Card.MaximumHeightRequest = h * 0.90;

            // Ensure the editor never exceeds the card
          // EditorCard.MaximumHeightRequest = Card.MaximumHeightRequest - 24;
        }

        // Exposed to XAML
        public ICommand CancelEditCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SaveAndCloseCommand { get; }
    }
}