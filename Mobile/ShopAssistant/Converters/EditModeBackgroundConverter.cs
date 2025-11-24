using System.Globalization;

namespace ShopAssistant.Converters;

public class EditModeBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEditMode)
        {
            if (isEditMode)
            {
                return Application.Current?.Resources["BackgroundSecondary"] as Color ?? Color.FromArgb("#F9FAFB");
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

