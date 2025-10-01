namespace KeyCatcher.Views;

public partial class header : ContentView
{

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public static readonly BindableProperty TitleProperty =
    BindableProperty.Create(nameof(Title), typeof(string), typeof(header), default(string));

    public header()
	{
        InitializeComponent();
        BindingContext = this; // or set from parent

    }
}