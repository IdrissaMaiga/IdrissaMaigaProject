using System.Globalization;
using ProductAssistant.Core.Models;

namespace ShopAssistant.Converters;

public class ProductSelectionButtonConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.ObjectModel.ObservableCollection<Product> selectedProducts && 
            parameter is Product product)
        {
            // Show "Remove" if product is already selected, "Add" if not
            return selectedProducts.Any(p => p.Id == product.Id) ? "Remove" : "Add";
        }
        return "Add";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

