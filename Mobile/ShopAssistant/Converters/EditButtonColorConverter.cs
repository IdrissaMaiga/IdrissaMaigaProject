using System.Globalization;

namespace ShopAssistant.Converters;

public class EditButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEditMode)
        {
            if (isEditMode)
            {
                return Application.Current?.Resources["Secondary"] as Color ?? Color.FromArgb("#6C757D");
            }
            else
            {
                return Application.Current?.Resources["Primary"] as Color ?? Color.FromArgb("#0066FF");
            }
        }
        return Application.Current?.Resources["Primary"] as Color ?? Color.FromArgb("#0066FF");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

