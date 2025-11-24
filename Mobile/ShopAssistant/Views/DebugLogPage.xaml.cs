using ShopAssistant.ViewModels;

namespace ShopAssistant.Views;

public partial class DebugLogPage : ContentPage
{
    private readonly DebugLogViewModel _viewModel;

    public DebugLogPage(DebugLogViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel?.OnPageAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.OnPageDisappearing();
    }
}

