using System.Globalization;

namespace ShopAssistant.Converters;

public class ConversationSelectionBackgroundMultiConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null && values.Length == 2 && 
            values[0] is int conversationId && 
            values[1] is int selectedId)
        {
            return conversationId == selectedId 
                ? Color.FromArgb("#E3F2FD") // Light blue for selected
                : Colors.White;
        }
        return Colors.White;
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ConversationSelectionBorderMultiConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null && values.Length == 2 && 
            values[0] is int conversationId && 
            values[1] is int selectedId)
        {
            return conversationId == selectedId 
                ? Color.FromArgb("#2196F3") // Blue border for selected
                : Colors.Transparent;
        }
        return Colors.Transparent;
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ConversationSelectionTextMultiConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null && values.Length == 2 && 
            values[0] is int conversationId && 
            values[1] is int selectedId)
        {
            return conversationId == selectedId 
                ? Color.FromArgb("#1976D2") // Darker blue text for selected
                : Color.FromArgb("#212121"); // Default text color
        }
        return Color.FromArgb("#212121");
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

