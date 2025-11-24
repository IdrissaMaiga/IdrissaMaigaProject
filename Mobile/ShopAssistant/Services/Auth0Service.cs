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
        // HttpClient base address is http://localhost/api, so use relative paths
        // Store relative path for endpoints
        _apiBaseUrl = "/api/auth";
        
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

                return true;
            }

            return false;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception)
        {
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
                // Try to read error message from response
                var errorContent = await response.Content.ReadAsStringAsync();
                
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    try
                    {
                        var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorJson.TryGetProperty("error", out var errorProp))
                        {
                            var errorMessage = errorProp.GetString();
                            if (!string.IsNullOrWhiteSpace(errorMessage))
                            {
                                throw new Exception(errorMessage);
                            }
                        }
                        // Try "message" property as fallback
                        if (errorJson.TryGetProperty("message", out var messageProp))
                        {
                            var errorMessage = messageProp.GetString();
                            if (!string.IsNullOrWhiteSpace(errorMessage))
                            {
                                throw new Exception(errorMessage);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // If JSON parsing fails, try to use the raw content if it's not too long
                        if (errorContent.Length < 200)
                        {
                            throw new Exception(errorContent);
                        }
                    }
                }
                
                // Fallback to status code based message
                var statusMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.BadRequest => "Invalid registration information. Please check your input.",
                    System.Net.HttpStatusCode.Conflict => "Username or email already exists.",
                    System.Net.HttpStatusCode.InternalServerError => "Server error. Please try again later.",
                    _ => $"Registration failed ({(int)response.StatusCode}). Please check your information and try again."
                };
                throw new Exception(statusMessage);
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

                return true;
            }

            throw new Exception("Registration failed. Invalid response from server.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Cannot connect to server: {ex.Message}");
        }
        catch
        {
            throw; // Re-throw to preserve error message
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
                catch (Exception)
                {
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

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private DateTime _lastValidationTime = DateTime.MinValue;
    private bool _lastValidationResult = false;
    private const int ValidationCacheSeconds = 30; // Cache validation result for 30 seconds

    public async Task<bool> IsAuthenticatedAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[Auth0Service] IsAuthenticatedAsync called - Token: {!string.IsNullOrEmpty(_currentToken)}, UserId: {!string.IsNullOrEmpty(_currentUserId)}");
        
        // Check if we have a token
        if (string.IsNullOrEmpty(_currentToken))
        {
            System.Diagnostics.Debug.WriteLine("[Auth0Service] No token found");
            return false;
        }

        // Check if we have userId (required for authentication)
        if (string.IsNullOrEmpty(_currentUserId))
        {
            System.Diagnostics.Debug.WriteLine("[Auth0Service] No userId found");
            return false;
        }

        // Check token expiration
        if (_tokenExpiration.HasValue && _tokenExpiration.Value <= DateTime.UtcNow)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth0Service] Token expired at {_tokenExpiration.Value}");
            await LogoutAsync();
            return false;
        }

        // Use cached validation result if recent (within last 30 seconds)
        if ((DateTime.UtcNow - _lastValidationTime).TotalSeconds < ValidationCacheSeconds)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth0Service] Using cached validation result: {_lastValidationResult}");
            return _lastValidationResult;
        }

        // Validate token with server
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Auth0Service] Validating token with server: {_apiBaseUrl}/validate");
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/validate?token={Uri.EscapeDataString(_currentToken)}");
            System.Diagnostics.Debug.WriteLine($"[Auth0Service] Validation response: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (result.TryGetProperty("isValid", out var isValidProp) && isValidProp.GetBoolean())
                {
                    _lastValidationTime = DateTime.UtcNow;
                    _lastValidationResult = true;
                    System.Diagnostics.Debug.WriteLine("[Auth0Service] Token is valid");
                    return true;
                }
                // If isValid is false or missing, token is invalid
                System.Diagnostics.Debug.WriteLine("[Auth0Service] Token validation returned isValid=false");
                await LogoutAsync();
                _lastValidationResult = false;
                return false;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Endpoint not found - might be server issue, allow if we have token and userId
                // This handles cases where server is misconfigured but user has valid credentials
                System.Diagnostics.Debug.WriteLine("[Auth0Service] Validation endpoint not found, allowing based on local credentials");
                _lastValidationTime = DateTime.UtcNow;
                _lastValidationResult = !string.IsNullOrEmpty(_currentToken) && !string.IsNullOrEmpty(_currentUserId);
                return _lastValidationResult;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token is invalid or expired - logout
                System.Diagnostics.Debug.WriteLine("[Auth0Service] Token unauthorized, logging out");
                await LogoutAsync();
                _lastValidationResult = false;
                return false;
            }
            else
            {
                // Other server errors - fall back to expiration check, or allow if we have credentials
                System.Diagnostics.Debug.WriteLine($"[Auth0Service] Server error {response.StatusCode}, falling back to local check");
                if (_tokenExpiration.HasValue && _tokenExpiration.Value > DateTime.UtcNow)
                {
                    _lastValidationTime = DateTime.UtcNow;
                    _lastValidationResult = true;
                    return true;
                }
                // If no expiration set but we have token and userId, allow (server might be down)
                _lastValidationTime = DateTime.UtcNow;
                _lastValidationResult = !string.IsNullOrEmpty(_currentToken) && !string.IsNullOrEmpty(_currentUserId);
                return _lastValidationResult;
            }
        }
        catch (HttpRequestException ex)
        {
            // If server is unreachable, check if token is not expired as fallback
            // If no expiration set, allow if we have token and userId (offline mode)
            System.Diagnostics.Debug.WriteLine($"[Auth0Service] HTTP error during validation: {ex.Message}");
            if (_tokenExpiration.HasValue)
            {
                _lastValidationTime = DateTime.UtcNow;
                _lastValidationResult = _tokenExpiration.Value > DateTime.UtcNow;
                return _lastValidationResult;
            }
            // Allow offline access if we have credentials
            _lastValidationTime = DateTime.UtcNow;
            _lastValidationResult = !string.IsNullOrEmpty(_currentToken) && !string.IsNullOrEmpty(_currentUserId);
            return _lastValidationResult;
        }
        catch (Exception ex)
        {
            // On error, check expiration as fallback, or allow if we have credentials
            System.Diagnostics.Debug.WriteLine($"[Auth0Service] Exception during validation: {ex.Message}");
            if (_tokenExpiration.HasValue && _tokenExpiration.Value > DateTime.UtcNow)
            {
                _lastValidationTime = DateTime.UtcNow;
                _lastValidationResult = true;
                return true;
            }
            // If no expiration set but we have token and userId, allow (might be server issue)
            _lastValidationTime = DateTime.UtcNow;
            _lastValidationResult = !string.IsNullOrEmpty(_currentToken) && !string.IsNullOrEmpty(_currentUserId);
            return _lastValidationResult;
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
        }
        catch (Exception)
        {
        }
    }

    private void LoadSavedCredentials()
    {
        try
        {
            _currentToken = Preferences.Default.Get<string>("auth_token", null) ?? string.Empty;
            _currentUserId = Preferences.Default.Get<string>("auth_userId", null) ?? string.Empty;
            _currentUsername = Preferences.Default.Get<string>("auth_username", null) ?? string.Empty;
            _currentEmail = Preferences.Default.Get<string>("auth_email", null) ?? string.Empty;
            
            var expirationStr = Preferences.Default.Get<string>("auth_token_expiration", null) ?? string.Empty;
            if (!string.IsNullOrEmpty(expirationStr) && DateTime.TryParse(expirationStr, out var expiration))
            {
                _tokenExpiration = expiration;
            }
        }
        catch (Exception)
        {
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
        }
        catch (Exception)
        {
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

