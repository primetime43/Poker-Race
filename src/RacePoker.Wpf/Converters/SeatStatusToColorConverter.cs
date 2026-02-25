using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RacePoker.Core.Enums;

namespace RacePoker.Wpf.Converters;

public class SeatStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SeatStatus status)
            return Brushes.Gray;

        return status switch
        {
            SeatStatus.Joined => Brushes.LimeGreen,
            SeatStatus.Ready => Brushes.Gold,
            SeatStatus.Folded => Brushes.DarkRed,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
