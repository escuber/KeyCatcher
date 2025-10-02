using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace KeyCatcher.Views
{
    public partial class header : ContentView
    {

        public header()
        {
            InitializeComponent();
        }
        void ShowBlePopup(string status)
        {
            var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
            if (page is null) return;

            // construct popup and pass services
           // var popup = new KeyCatcher.Popups.WifiCreds(settings, Hub);

          //  // await the popup result
          //  var result = await page.ShowPopupAsync(popup);

        }
        public string BleIconColor => IsBleUp ? "DodgerBlue" : "Grey";
            //Colors.DodgerBlue : Colors.Gray;
        public string WifiIconColor => IsWifiUp ? "LimeGreen" : "Red";
        // Bindable Title
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(header), default(string));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // BLE status
        public static readonly BindableProperty IsBleUpProperty =
            BindableProperty.Create(nameof(IsBleUp), typeof(bool), typeof(header), false);
        public bool IsBleUp
        {
            get => (bool)GetValue(IsBleUpProperty);
            set
            {
                SetValue(IsBleUpProperty, value);
                OnPropertyChanged(nameof(BleIconColor));
            }
        }

        // WiFi status
        public static readonly BindableProperty IsWifiUpProperty =
            BindableProperty.Create(nameof(IsWifiUp), typeof(bool), typeof(header), false);
        public bool IsWifiUp
        {
            get => (bool)GetValue(IsWifiUpProperty);
            set
            {
                SetValue(IsWifiUpProperty, value);
                OnPropertyChanged(nameof(WifiIconColor));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // Show help (both down)
        public static readonly BindableProperty ShowHelpProperty =
            BindableProperty.Create(nameof(ShowHelp), typeof(bool), typeof(header), false);
        public bool ShowHelp
        {
            get => (bool)GetValue(ShowHelpProperty);
            set => SetValue(ShowHelpProperty, value);
        }

        // Commands
        public static readonly BindableProperty BleCommandProperty =
            BindableProperty.Create(nameof(BleCommand), typeof(ICommand), typeof(header));
        public ICommand BleCommand
        {
            get => (ICommand)GetValue(BleCommandProperty);
            set => SetValue(BleCommandProperty, value);
        }

        public static readonly BindableProperty WifiCommandProperty =
            BindableProperty.Create(nameof(WifiCommand), typeof(ICommand), typeof(header));
        public ICommand WifiCommand
        {
            get => (ICommand)GetValue(WifiCommandProperty);
            set => SetValue(WifiCommandProperty, value);
        }

        public static readonly BindableProperty HelpCommandProperty =
            BindableProperty.Create(nameof(HelpCommand), typeof(ICommand), typeof(header));
        public ICommand HelpCommand
        {
            get => (ICommand)GetValue(HelpCommandProperty);
            set => SetValue(HelpCommandProperty, value);
        }


  



  
    }
}