using ProductAssistant.Core.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShopAssistant.Services;

public class Auth0Service : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private string? _currentToken;
    private string? _currentUserId;
    private string? _currentUsername;
    private string? _currentEmail;
    private DateTime? _tokenExpiration;

    public Auth0Service(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiBaseUrl = $"{ServiceUrlHelper.GetApiBaseUrl()}/api/auth";
        
        System.Diagnostics.Debug.WriteLine($"Auth0Service initialized with API URL: {_apiBaseUrl}");
        
        // Load saved credentials if any
        LoadSavedCredentials();
    }

    public async Task<bool> LoginAsync()
    {
        return await LoginAsync("demo-user", "password");
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        try
        {
            var request = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/login", request);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Login failed with status: {response.StatusCode}");
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
            {
                _currentToken = authResponse.Token;
                _currentUserId = authResponse.UserId;
                _currentUsername = authResponse.Username;
                _currentEmail = authResponse.Email;
                _tokenExpiration = authResponse.ExpiresAt;

                // Save credentials
                SaveCredentials();

                System.Diagnostics.Debug.WriteLine($"Login successful for user: {username}");
                return true;
            }

            return false;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network error during login: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        try
        {
            var request = new { Username = username, Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Registration failed: {response.StatusCode} - {errorContent}");
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
            {
                _currentToken = authResponse.Token;
                _currentUserId = authResponse.UserId;
                _currentUsername = authResponse.Username;
                _currentEmail = authResponse.Email;
                _tokenExpiration = authResponse.ExpiresAt;

                // Save credentials
                SaveCredentials();

                System.Diagnostics.Debug.WriteLine($"Registration successful for user: {username}");
                return true;
            }

            return false;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network error during registration: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentUserId) && !string.IsNullOrEmpty(_currentToken))
            {
                try
                {
                    var request = new { UserId = _currentUserId, Token = _currentToken };
                    await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/logout", request);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Server logout failed (continuing anyway): {ex.Message}");
                }
            }

            // Clear all authentication state
            _currentToken = null;
            _currentUserId = null;
            _currentUsername = null;
            _currentEmail = null;
            _tokenExpiration = null;

            // Clear saved credentials
            ClearSavedCredentials();

            System.Diagnostics.Debug.WriteLine("Logout successful");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        // Check if we have a token
        if (string.IsNullOrEmpty(_currentToken))
        {
            System.Diagnostics.Debug.WriteLine("Not authenticated: No token");
            return false;
        }

        // Check if we have userId (required for authentication)
        if (string.IsNullOrEmpty(_currentUserId))
        {
            System.Diagnostics.Debug.WriteLine("Not authenticated: No userId");
            return false;
        }

        // Check token expiration
        if (_tokenExpiration.HasValue && _tokenExpiration.Value <= DateTime.UtcNow)
        {
            System.Diagnostics.Debug.WriteLine("Not authenticated: Token expired");
            await LogoutAsync();
            return false;
        }

        // Validate token with server
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/validate?token={Uri.EscapeDataString(_currentToken)}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (result.TryGetProperty("isValid", out var isValidProp) && isValidProp.GetBoolean())
                {
                    System.Diagnostics.Debug.WriteLine($"Token validated successfully for user: {_currentUserId}");
                    return true;
                }
            }
            
            System.Diagnostics.Debug.WriteLine("Not authenticated: Token validation failed");
            await LogoutAsync();
            return false;
        }
        catch (HttpRequestException ex)
        {
            // If server is unreachable, check if token is not expired as fallback
            // This allows offline usage but is less secure
            var isValid = _tokenExpiration.HasValue && _tokenExpiration.Value > DateTime.UtcNow;
            System.Diagnostics.Debug.WriteLine($"Network error during validation, using cached auth: {isValid} - {ex.Message}");
            return isValid;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication check error: {ex.Message}");
            // On error, check expiration as fallback
            var isValid = _tokenExpiration.HasValue && _tokenExpiration.Value > DateTime.UtcNow;
            return isValid;
        }
    }

    public Task<string?> GetUserIdAsync()
    {
        return Task.FromResult(_currentUserId);
    }

    public Task<string?> GetUserEmailAsync()
    {
        return Task.FromResult(_currentEmail);
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(_currentToken);
    }

    private void SaveCredentials()
    {
        try
        {
            Preferences.Default.Set("auth_token", _currentToken ?? string.Empty);
            Preferences.Default.Set("auth_userId", _currentUserId ?? string.Empty);
            Preferences.Default.Set("auth_username", _currentUsername ?? string.Empty);
            Preferences.Default.Set("auth_email", _currentEmail ?? string.Empty);
            
            if (_tokenExpiration.HasValue)
            {
                Preferences.Default.Set("auth_token_expiration", _tokenExpiration.Value.ToString("O"));
            }
            
            System.Diagnostics.Debug.WriteLine("Credentials saved successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving credentials: {ex.Message}");
        }
    }

    private void LoadSavedCredentials()
    {
        try
        {
            _currentToken = Preferences.Default.Get<string>("auth_token", null);
            _currentUserId = Preferences.Default.Get<string>("auth_userId", null);
            _currentUsername = Preferences.Default.Get<string>("auth_username", null);
            _currentEmail = Preferences.Default.Get<string>("auth_email", null);
            
            var expirationStr = Preferences.Default.Get<string>("auth_token_expiration", null);
            if (!string.IsNullOrEmpty(expirationStr) && DateTime.TryParse(expirationStr, out var expiration))
            {
                _tokenExpiration = expiration;
            }
            
            if (!string.IsNullOrEmpty(_currentToken))
            {
                System.Diagnostics.Debug.WriteLine("Saved credentials loaded successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading credentials: {ex.Message}");
        }
    }

    private void ClearSavedCredentials()
    {
        try
        {
            Preferences.Default.Remove("auth_token");
            Preferences.Default.Remove("auth_userId");
            Preferences.Default.Remove("auth_username");
            Preferences.Default.Remove("auth_email");
            Preferences.Default.Remove("auth_token_expiration");
            
            System.Diagnostics.Debug.WriteLine("Saved credentials cleared");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing credentials: {ex.Message}");
        }
    }
}

// DTOs
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

