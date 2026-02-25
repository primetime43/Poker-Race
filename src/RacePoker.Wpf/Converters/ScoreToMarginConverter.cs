using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RacePoker.Wpf.Converters;

public class ScoreToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int score = value is int s ? s : 0;
        int finishDistance = 150000;

        if (parameter is string paramStr && int.TryParse(paramStr, out int fd))
            finishDistance = fd;

        // Map score to a max of ~900px track width
        double maxPixels = 900;
        double fraction = Math.Clamp((double)score / finishDistance, 0.0, 1.0);
        double leftMargin = fraction * maxPixels;

        return new Thickness(leftMargin, 0, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
