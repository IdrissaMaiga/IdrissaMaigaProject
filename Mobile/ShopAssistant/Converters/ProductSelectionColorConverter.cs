using System.Globalization;
using ProductAssistant.Core.Models;

namespace ShopAssistant.Converters;

public class ProductSelectionColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.ObjectModel.ObservableCollection<Product> selectedProducts && 
            parameter is Product product)
        {
            return selectedProducts.Any(p => p.Id == product.Id) 
                ? Application.Current?.Resources["Error"] as Color ?? Colors.Red
                : Application.Current?.Resources["Primary"] as Color ?? Colors.Blue;
        }
        return Application.Current?.Resources["Primary"] as Color ?? Colors.Blue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

