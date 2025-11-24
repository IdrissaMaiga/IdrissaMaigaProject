namespace ProductAssistant.Core.Services;

public interface IAuthService
{
    Task<bool> LoginAsync();
    Task<bool> LoginAsync(string username, string password);
    Task<bool> RegisterAsync(string username, string email, string password);
    Task<bool> LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetUserEmailAsync();
    Task<string?> GetAccessTokenAsync();
}








