using System.Globalization;

namespace ShopAssistant.Converters;

public class ConversationSelectionTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int conversationId && parameter is int selectedId)
        {
            return conversationId == selectedId 
                ? Color.FromArgb("#1976D2") // Darker blue text for selected
                : Color.FromArgb("#212121"); // Normal text color
        }
        return Color.FromArgb("#212121");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

