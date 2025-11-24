using System.Globalization;

namespace ShopAssistant.Converters;

public class MessageColumnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUserMessage)
        {
            return isUserMessage ? 2 : 0; // User messages on right (column 2), AI messages on left (column 0)
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

