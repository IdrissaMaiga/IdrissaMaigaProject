using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProductAssistant.Core.Services;

public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private const int TokenValidityDays = 7;

    public TokenService(string secretKey)
    {
        _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
    }

    public string GenerateToken(string userId, string username)
    {
        var tokenData = new TokenData
        {
            UserId = userId,
            Username = username,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(TokenValidityDays),
            Nonce = Guid.NewGuid().ToString()
        };

        var json = JsonSerializer.Serialize(tokenData);
        var tokenBytes = Encoding.UTF8.GetBytes(json);
        
        // Create HMAC signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var signature = hmac.ComputeHash(tokenBytes);
        
        // Combine token data and signature
        var tokenWithSignature = new
        {
            Data = Convert.ToBase64String(tokenBytes),
            Signature = Convert.ToBase64String(signature)
        };
        
        var finalJson = JsonSerializer.Serialize(tokenWithSignature);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(finalJson));
    }

    public bool ValidateToken(string token, out string? userId, out string? username)
    {
        userId = null;
        username = null;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            // Decode the outer token
            var outerBytes = Convert.FromBase64String(token);
            var outerJson = Encoding.UTF8.GetString(outerBytes);
            var tokenWithSignature = JsonSerializer.Deserialize<TokenWithSignature>(outerJson);
            
            if (tokenWithSignature == null || string.IsNullOrEmpty(tokenWithSignature.Data) || string.IsNullOrEmpty(tokenWithSignature.Signature))
                return false;

            // Verify signature
            var tokenBytes = Convert.FromBase64String(tokenWithSignature.Data);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var computedSignature = hmac.ComputeHash(tokenBytes);
            var providedSignature = Convert.FromBase64String(tokenWithSignature.Signature);

            if (!CryptographicOperations.FixedTimeEquals(computedSignature, providedSignature))
                return false;

            // Decode token data
            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var tokenData = JsonSerializer.Deserialize<TokenData>(tokenJson);
            
            if (tokenData == null)
                return false;

            // Check expiration
            if (tokenData.ExpiresAt <= DateTime.UtcNow)
                return false;

            userId = tokenData.UserId;
            username = tokenData.Username;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsTokenExpired(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        try
        {
            var outerBytes = Convert.FromBase64String(token);
            var outerJson = Encoding.UTF8.GetString(outerBytes);
            var tokenWithSignature = JsonSerializer.Deserialize<TokenWithSignature>(outerJson);
            
            if (tokenWithSignature == null || string.IsNullOrEmpty(tokenWithSignature.Data))
                return true;

            var tokenBytes = Convert.FromBase64String(tokenWithSignature.Data);
            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var tokenData = JsonSerializer.Deserialize<TokenData>(tokenJson);
            
            if (tokenData == null)
                return true;

            return tokenData.ExpiresAt <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private class TokenData
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }

    private class TokenWithSignature
    {
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}



