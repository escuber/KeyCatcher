using CommunityToolkit.Maui.Views;
namespace KeyCatcher.Popups;
public partial class BleProgressPopup : Popup
{
    public static readonly BindableProperty StatusTextProperty =
        BindableProperty.Create(nameof(StatusText), typeof(string), typeof(BleProgressPopup), string.Empty);

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public BleProgressPopup()
    {
        InitializeComponent();
        BindingContext = this;
       //his.Color = Colors.Transparent;
       
    }
}
