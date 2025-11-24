using System.Globalization;
using ShopAssistant.Services;

namespace ShopAssistant.Converters;

/// <summary>
/// Converts external image URLs to use the backend image proxy
/// This bypasses CORS and User-Agent restrictions from CDNs like Arukereso
/// </summary>
public class ImageUrlProxyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        System.Diagnostics.Debug.WriteLine($"[ImageUrlProxyConverter] Input: {value}");
        
        if (value is not string imageUrl || string.IsNullOrWhiteSpace(imageUrl))
        {
            System.Diagnostics.Debug.WriteLine("[ImageUrlProxyConverter] Null or empty URL");
            return null;
        }

        // If it's already a proxied URL, return as-is
        if (imageUrl.Contains("/api/imageproxy", StringComparison.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine("[ImageUrlProxyConverter] Already proxied");
            return imageUrl;
        }

        // If it's an external URL (from Arukereso CDN), proxy it through backend
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var apiBaseUrl = ServiceUrlHelper.GetApiBaseUrl();
            var encodedUrl = Uri.EscapeDataString(imageUrl);
            var proxiedUrl = $"{apiBaseUrl}/api/imageproxy?url={encodedUrl}";
            System.Diagnostics.Debug.WriteLine($"[ImageUrlProxyConverter] Proxied: {proxiedUrl}");
            return proxiedUrl;
        }

        // For local/relative URLs, return as-is
        System.Diagnostics.Debug.WriteLine("[ImageUrlProxyConverter] Returned as-is");
        return imageUrl;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

