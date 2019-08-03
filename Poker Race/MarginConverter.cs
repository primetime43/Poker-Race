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
            //return new Thickness(~~(System.Convert.ToInt32(value) / 60), 0, 0, 0);
            return new Thickness(Math.Max(0, ((int)Math.Round(System.Convert.ToInt32(value) * (GameMainPage._pBar.Width / GameMainPage._pBar.Maximum))) - 100), 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return new Thickness(~~(System.Convert.ToInt32(value) / 60), 0, 0, 0);
            return new Thickness(Math.Max(0, ((int)Math.Round(System.Convert.ToInt32(value) * (GameMainPage._pBar.Width / GameMainPage._pBar.Maximum))) - 100), 0, 0, 0);
        }
    }
}
