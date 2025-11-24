using System.Globalization;

namespace ShopAssistant.Converters;

public class ApiKeyStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isApiKeySet && isApiKeySet)
        {
            return Application.Current?.Resources["Success"] as Color ?? Color.FromArgb("#10B981");
        }
        return Application.Current?.Resources["BackgroundSecondary"] as Color ?? Color.FromArgb("#F9FAFB");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

