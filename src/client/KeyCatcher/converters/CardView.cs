using Microsoft.Maui.Controls;

namespace KeyCatcher.Controls;

public class CardView : TemplatedView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CardView), string.Empty);

    public static readonly BindableProperty HeaderContentProperty =
        BindableProperty.Create(nameof(HeaderContent), typeof(View), typeof(CardView));

    public static readonly BindableProperty BodyContentProperty =
        BindableProperty.Create(nameof(BodyContent), typeof(View), typeof(CardView));

    public static readonly BindableProperty FooterContentProperty =
        BindableProperty.Create(nameof(FooterContent), typeof(View), typeof(CardView));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public View HeaderContent
    {
        get => (View)GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public View BodyContent
    {
        get => (View)GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public View FooterContent
    {
        get => (View)GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }
}
