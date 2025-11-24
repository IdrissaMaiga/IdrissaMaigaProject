using System.Globalization;

namespace ShopAssistant.Converters;

public class ConversationSelectionBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int conversationId && parameter is int selectedId)
        {
            return conversationId == selectedId 
                ? Color.FromArgb("#E3F2FD") // Light blue for selected
                : Colors.White;
        }
        return Colors.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

