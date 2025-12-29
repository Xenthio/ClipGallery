using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ClipGallery.UI.Converters;

public class BoolToAngleConverter : IValueConverter
{
    public static readonly BoolToAngleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? 90.0 : 0.0;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
