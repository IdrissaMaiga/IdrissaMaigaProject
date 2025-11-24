using System.Globalization;

namespace ShopAssistant.Converters;

public class MessageColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUserMessage)
        {
            if (isUserMessage)
            {
                return Application.Current?.Resources["Primary"] as Color ?? Color.FromArgb("#0066FF");
            }
            else
            {
                return Application.Current?.Resources["White"] as Color ?? Colors.White;
            }
        }
        return Application.Current?.Resources["White"] as Color ?? Colors.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



