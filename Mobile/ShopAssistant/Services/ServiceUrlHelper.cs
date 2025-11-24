using Microsoft.Extensions.Configuration;

namespace ShopAssistant.Services;

/// <summary>
/// Service URL helper for MAUI app to connect to backend services.
/// Reads URLs from appsettings.json configuration.
/// For WSL2 port forwarding, use WSL IP address (e.g., http://172.20.194.46:8080)
/// For Android emulator, use http://10.0.2.2:808x
/// </summary>
public static class ServiceUrlHelper
{
    private static IConfiguration? _configuration;
    
    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static string GetApiBaseUrl()
    {
        if (_configuration != null)
        {
            var url = _configuration["Services:ApiService:BaseUrl"];
            if (!string.IsNullOrEmpty(url))
            {
                return url;
            }
        }
        
        // Fallback to default
#if ANDROID
        return "http://10.0.2.2:8080";
#else
        return "http://localhost:8080";
#endif
    }

    public static string GetScrapingServiceUrl()
    {
        if (_configuration != null)
        {
            var url = _configuration["Services:ScrapingService:BaseUrl"];
            if (!string.IsNullOrEmpty(url))
            {
                return url;
            }
        }
        
        // Fallback to default
#if ANDROID
        return "http://10.0.2.2:8082/api/scraping";
#else
        return "http://localhost:8082/api/scraping";
#endif
    }

    public static string GetAIServiceUrl()
    {
        if (_configuration != null)
        {
            var url = _configuration["Services:AIService:BaseUrl"];
            if (!string.IsNullOrEmpty(url))
            {
                return url;
            }
        }
        
        // Fallback to default
#if ANDROID
        return "http://10.0.2.2:8081/api/ai";
#else
        return "http://localhost:8081/api/ai";
#endif
    }
}


