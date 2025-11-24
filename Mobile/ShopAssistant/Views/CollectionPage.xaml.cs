using ShopAssistant.ViewModels;
using ProductAssistant.Core.Models;

namespace ShopAssistant.Views;

public partial class CollectionPage : ContentPage
{
    public CollectionPage(CollectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CollectionViewModel vm)
        {
            // Refresh collection when page appears (in case products were added from Chat)
            await vm.LoadProductsAsync();
        }
    }
}

