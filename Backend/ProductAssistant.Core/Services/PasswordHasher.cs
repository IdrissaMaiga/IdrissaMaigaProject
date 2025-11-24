using System.Security.Cryptography;
using System.Text;

namespace ProductAssistant.Core.Services;

public static class PasswordHasher
{
    private const int SaltSize = 32; // 256 bits
    private const int KeySize = 64; // 512 bits
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    public static (string hash, string salt) HashPassword(string password)
    {
        // Generate salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        
        // Generate hash
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Convert.FromBase64String(hash);

            var testHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                HashAlgorithm,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(testHash, hashBytes);
        }
        catch
        {
            return false;
        }
    }
}
