namespace ProductAssistant.Core.Configuration;

/// <summary>
/// Configuration options for authentication settings
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";
    
    public string SecretKey { get; set; } = string.Empty;
    public int TokenExpirationMinutes { get; set; } = 60;
}

