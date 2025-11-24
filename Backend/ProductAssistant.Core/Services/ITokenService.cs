namespace ProductAssistant.Core.Services;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for a user
    /// </summary>
    string GenerateToken(string userId, string email, string? name = null);
    
    /// <summary>
    /// Validates a JWT token and returns the user ID if valid
    /// </summary>
    string? ValidateToken(string token);
    
    /// <summary>
    /// Validates a JWT token and outputs userId and username if valid
    /// </summary>
    bool ValidateToken(string token, out string? userId, out string? username);
}

