using System.Globalization;

namespace ShopAssistant.Converters;

public class MessageTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUserMessage)
        {
            return isUserMessage ? Colors.White : Application.Current?.Resources["TextPrimary"] as Color ?? Colors.Black;
        }
        return Application.Current?.Resources["TextPrimary"] as Color ?? Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

