namespace ProductAssistant.Core.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string username);
    bool ValidateToken(string token, out string? userId, out string? username);
    bool IsTokenExpired(string token);
}



