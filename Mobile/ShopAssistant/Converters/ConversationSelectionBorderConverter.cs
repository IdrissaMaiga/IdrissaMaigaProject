using System.Globalization;

namespace ShopAssistant.Converters;

public class ConversationSelectionBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int conversationId && parameter is int selectedId)
        {
            return conversationId == selectedId 
                ? Color.FromArgb("#2196F3") // Blue border for selected
                : Colors.Transparent;
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

