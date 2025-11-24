using System.Globalization;

namespace ShopAssistant.Converters;

public class DateTimeToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            // Hide if date is default/unset (year 1 or year 0001)
            return dateTime.Year > 1 && dateTime != default(DateTime);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

