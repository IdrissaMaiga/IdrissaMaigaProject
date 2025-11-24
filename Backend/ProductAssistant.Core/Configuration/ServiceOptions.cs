namespace ProductAssistant.Core.Configuration;

/// <summary>
/// Configuration options for external service URLs
/// </summary>
public class ServiceOptions
{
    public const string SectionName = "Services";
    
    public string ScrapingServiceUrl { get; set; } = string.Empty;
    public string AIServiceUrl { get; set; } = string.Empty;
    public int HttpClientTimeoutMinutes { get; set; } = 2;
}

