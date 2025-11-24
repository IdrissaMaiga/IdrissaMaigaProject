using System.Globalization;

namespace ShopAssistant.Converters;

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string parameterString)
        {
            var texts = parameterString.Split('|');
            if (texts.Length == 2)
            {
                return boolValue ? texts[0] : texts[1];
            }
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

