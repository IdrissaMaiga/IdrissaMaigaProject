using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductAssistant.Core.Services;
using ShopAssistant.Services;

namespace ShopAssistant.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settingsService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string apiKey = string.Empty;

    [ObservableProperty]
    private bool isApiKeySet;

    public SettingsViewModel(SettingsService settingsService, IAuthService authService)
    {
        _settingsService = settingsService;
        _authService = authService;
        Title = "Settings";
        LoadApiKey();
    }

    private async void LoadApiKey()
    {
        var key = await _settingsService.GetGeminiApiKeyAsync();
        ApiKey = key ?? string.Empty;
        IsApiKeySet = !string.IsNullOrWhiteSpace(ApiKey);
    }

    [RelayCommand]
    private async Task SaveApiKeyAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await _settingsService.SaveGeminiApiKeyAsync(ApiKey);
            IsApiKeySet = !string.IsNullOrWhiteSpace(ApiKey);
            
            await Shell.Current.DisplayAlert(
                "Success", 
                "Gemini API key has been saved.", 
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Error", 
                $"An error occurred while saving: {ex.Message}", 
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearApiKeyAsync()
    {
        if (IsBusy)
            return;

        var result = await Shell.Current.DisplayAlert(
            "Confirm",
            "Are you sure you want to delete the API key?",
            "Yes",
            "No");

        if (result)
        {
            try
            {
                IsBusy = true;
                await _settingsService.SaveGeminiApiKeyAsync(string.Empty);
                ApiKey = string.Empty;
                IsApiKeySet = false;
                
                await Shell.Current.DisplayAlert(
                    "Success",
                    "API key has been deleted.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"An error occurred while deleting: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
            return;

        var confirm = await Shell.Current.DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            await _authService.LogoutAsync();
            
            // Navigate to login page
            if (Shell.Current is AppShell appShell)
            {
                await appShell.OnUserLoggedOut();
            }
            else
            {
                await Shell.Current.GoToAsync("//Login");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Error",
                $"An error occurred during logout: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewDebugLogsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//DebugLog");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Error",
                $"Failed to open debug logs: {ex.Message}",
                "OK");
        }
    }
}

