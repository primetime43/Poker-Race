using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Poker_Race
{
    class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double pBarWidth = GameMainPage._pBar.ActualWidth;
            double max = GameMainPage._pBar.Maximum;
            if (pBarWidth <= 0 || max <= 0)
                return new Thickness(0, 0, 0, 0);

            double ratio = System.Convert.ToDouble(value) / max;
            double horseWidth = 100.0;
            double leftMargin = ratio * (pBarWidth - horseWidth);
            return new Thickness(Math.Max(0, leftMargin), 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
