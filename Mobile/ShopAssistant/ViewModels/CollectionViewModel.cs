using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;
using ShopAssistant.Services;
using System.Collections.ObjectModel;

namespace ShopAssistant.ViewModels;

/// <summary>
/// ViewModel for the Collection page.
/// Displays all products saved by the user from chat conversations.
/// Users can search, view, and manage their saved product collection.
/// Products are added to collection from the ChatPage unified interface.
/// </summary>
public partial class CollectionViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly IAuthService _authService;
    private readonly INetworkService _networkService;

    [ObservableProperty]
    private ObservableCollection<Product> products = new();

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isOnline = true;

    [ObservableProperty]
    private string networkStatus = "Online";

    [ObservableProperty]
    private string searchText = string.Empty;

    public CollectionViewModel(
        IProductService productService,
        IAuthService authService,
        INetworkService networkService)
    {
        _productService = productService;
        _authService = authService;
        _networkService = networkService;
        Title = "My Collection";
        
        // Subscribe to network changes
        _networkService.ConnectivityChanged += OnConnectivityChanged;
        UpdateNetworkStatus();
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            IsBusy = true;
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                Products.Clear();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"🔍 CollectionViewModel.LoadProductsAsync - UserId: {userId}");
            var allProducts = await _productService.GetAllProductsAsync(userId);
            System.Diagnostics.Debug.WriteLine($"📦 Loading collection for UserId={userId}: Found {allProducts.Count} products");
            
            if (allProducts.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"📋 First {Math.Min(5, allProducts.Count)} products:");
                foreach (var p in allProducts.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {p.Name} (Id={p.Id}, UserId={p.UserId ?? "NULL"}, ImageUrl={p.ImageUrl ?? "NULL"})");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ No products found for UserId={userId}");
            }
            
            Products.Clear();
            foreach (var product in allProducts)
            {
                Products.Add(product);
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ CollectionViewModel.Products.Count = {Products.Count}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        IsOnline = isConnected;
        NetworkStatus = isConnected ? "Online" : "Offline";
    }

    private void UpdateNetworkStatus()
    {
        IsOnline = _networkService.IsConnected;
        NetworkStatus = IsOnline ? "Online" : "Offline";
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadProductsAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task OpenProductUrlAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not open URL: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product product)
    {
        if (product == null)
            return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Product",
            $"Are you sure you want to remove '{product.Name}' from your collection?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            var deleted = await _productService.DeleteProductAsync(product.Id);
            if (deleted)
            {
                Products.Remove(product);
                await Shell.Current.DisplayAlert("Success", "Product removed from collection.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to delete product: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        try
        {
            IsBusy = true;
            
            // Reload all products first
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                Products.Clear();
                return;
            }

            var allProducts = await _productService.GetAllProductsAsync(userId);
            
            // Filter if search text provided
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Products.Clear();
                foreach (var product in allProducts)
                {
                    Products.Add(product);
                }
                return;
            }

            var filtered = allProducts.Where(p => 
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.StoreName != null && p.StoreName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            Products.Clear();
            foreach (var product in filtered)
            {
                Products.Add(product);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to search products: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToChatAsync()
    {
        try
        {
            // For TabBar navigation, try setting CurrentItem directly first (more reliable)
            if (Shell.Current?.CurrentItem is TabBar tabBar)
            {
                foreach (var item in tabBar.Items)
                {
                    if (item.Route == "Chat")
                    {
                        Shell.Current.CurrentItem = item;
                        return;
                    }
                }
            }
            
            // Fallback: use route navigation if CurrentItem approach doesn't work
            await Shell.Current.GoToAsync("//Chat");
        }
        catch (Exception ex)
        {
            // If navigation fails, show error to user
            await Shell.Current.DisplayAlert("Navigation Error", $"Failed to navigate to chat: {ex.Message}", "OK");
        }
    }
}

