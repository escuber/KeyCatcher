using KeyCatcher_acc.ViewModels;

namespace KeyCatcher_acc.Views;

public partial class tstpag : ContentPage
{
    public tstpag(tstpagViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}