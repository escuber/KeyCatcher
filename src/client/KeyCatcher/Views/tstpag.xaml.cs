using KeyCatcher.ViewModels;

namespace KeyCatcher.Views;

public partial class tstpag : ContentPage
{
    public tstpag(tstpagViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}