using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductAssistant.Core.Services;
using ShopAssistant.Services;

namespace ShopAssistant.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private bool isRegisterMode;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Login";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        // Validate inputs
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Please enter a username";
            await Shell.Current.DisplayAlert("Validation Error", ErrorMessage, "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter a password";
            await Shell.Current.DisplayAlert("Validation Error", ErrorMessage, "OK");
            return;
        }

        if (IsRegisterMode && string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter an email address";
            await Shell.Current.DisplayAlert("Validation Error", ErrorMessage, "OK");
            return;
        }

        // Validate email format if in register mode
        if (IsRegisterMode && !IsValidEmail(Email))
        {
            ErrorMessage = "Please enter a valid email address";
            await Shell.Current.DisplayAlert("Validation Error", ErrorMessage, "OK");
            return;
        }

        try
        {
            IsBusy = true;

            bool result;
            if (IsRegisterMode)
            {
                try
                {
                result = await _authService.RegisterAsync(Username, Email, Password);
                
                if (result)
                {
                    IsAuthenticated = true;
                    await Shell.Current.DisplayAlert("Success", "Registration successful! Welcome to Shop Assistant.", "OK");
                    await NavigateToMainApp();
                }
                else
                {
                        ErrorMessage = "Registration failed. Please try again.";
                        await Shell.Current.DisplayAlert("Registration Failed", ErrorMessage, "OK");
                    }
                }
                catch (Exception regEx)
                {
                    ErrorMessage = regEx.Message;
                    await Shell.Current.DisplayAlert("Registration Failed", ErrorMessage, "OK");
                }
            }
            else
            {
                result = await _authService.LoginAsync(Username, Password);
                
                if (result)
                {
                    IsAuthenticated = true;
                    await NavigateToMainApp();
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                    await Shell.Current.DisplayAlert("Login Failed", ErrorMessage, "OK");
                }
            }
        }
        catch (HttpRequestException)
        {
#if ANDROID
            ErrorMessage = "Cannot connect to server. Make sure:\n1. Docker services are running\n2. You're using Android emulator (not physical device)\n3. Services are accessible on host machine";
#else
            ErrorMessage = "Cannot connect to server. Please check your network connection and ensure Docker services are running.";
#endif
            await Shell.Current.DisplayAlert("Connection Error", ErrorMessage, "OK");
        }
        catch (Exception ex)
        {
            // Only show generic error if we haven't already shown a specific error
            if (string.IsNullOrEmpty(ErrorMessage))
        {
            ErrorMessage = "An unexpected error occurred";
            await Shell.Current.DisplayAlert("Error", $"{ErrorMessage}: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DemoLoginAsync()
    {
        if (IsBusy)
            return;

        Username = "demo-user";
        Password = "password";
        IsRegisterMode = false;
        await LoginAsync();
    }

    [RelayCommand]
    private void ToggleRegisterMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = string.Empty;
        
        // Clear email when switching to login mode
        if (!IsRegisterMode)
        {
            Email = string.Empty;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await _authService.LogoutAsync();
            IsAuthenticated = false;
            
            // Clear all fields
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            ErrorMessage = string.Empty;
            IsRegisterMode = false;
            
            // Navigate to login page properly
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
            await Shell.Current.DisplayAlert("Error", $"An error occurred during logout: {ex.Message}", "OK");
            // Still try to navigate to login even if logout fails
            if (Shell.Current is AppShell appShell)
            {
                await appShell.OnUserLoggedOut();
            }
            else
            {
                await Shell.Current.GoToAsync("//Login");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NavigateToMainApp()
    {
        try
        {
            // Clear form
            Password = string.Empty;
            ErrorMessage = string.Empty;
            
            // Small delay to ensure authentication state is updated
            await Task.Delay(100);
            
            // Navigate to main app
            if (Shell.Current is AppShell appShell)
            {
                await appShell.OnUserLoggedIn();
            }
            else
            {
                // Fallback navigation
                await Shell.Current.GoToAsync("//Chat");
            }
        }
        catch (Exception ex)
        {
            // Log error and try fallback navigation
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            try
            {
                await Shell.Current.GoToAsync("//Chat");
            }
            catch
            {
                // If that fails, try Collection route
                await Shell.Current.GoToAsync("//Collection");
            }
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

