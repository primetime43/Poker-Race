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
        bool returnWidth = false;

        if (parameter is string paramStr)
        {
            // Support "150000|width" to return a double (width) instead of Thickness
            if (paramStr.Contains('|'))
            {
                var parts = paramStr.Split('|');
                if (int.TryParse(parts[0], out int fd1))
                    finishDistance = fd1;
                if (parts.Length > 1 && parts[1] == "width")
                    returnWidth = true;
            }
            else if (int.TryParse(paramStr, out int fd2))
            {
                finishDistance = fd2;
            }
        }

        double maxPixels = 900;
        double fraction = Math.Clamp((double)score / finishDistance, 0.0, 1.0);
        double pixels = fraction * maxPixels;

        if (returnWidth)
            return pixels;

        return new Thickness(pixels, 0, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
