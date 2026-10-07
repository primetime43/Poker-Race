using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using RacePoker.Core.Models;

namespace RacePoker.Wpf.Converters;

public class CardToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Card card)
            return null;

        var uri = new Uri($"pack://application:,,,/RacePoker.Wpf;component/Assets/PokerCards/{card.ImageFileName}", UriKind.Absolute);
        return new BitmapImage(uri);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
