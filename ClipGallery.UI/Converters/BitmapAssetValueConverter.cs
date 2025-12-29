using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.Globalization;
using System; // Added

namespace ClipGallery.UI.Converters;

public class BitmapAssetValueConverter : IValueConverter
{
    public static BitmapAssetValueConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            if (System.IO.File.Exists(path))
            {
                try
                {
                    return new Bitmap(path);
                }
                catch
                {
                    // Invalid image
                }
            }
        }
        return null; // Or return a default placeholder shim
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
