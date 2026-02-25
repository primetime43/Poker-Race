using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RacePoker.Core.Enums;

namespace RacePoker.Wpf.Converters;

public class PhaseToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not GamePhase phase || parameter is not string targetPhases)
            return Visibility.Collapsed;

        var phases = targetPhases.Split(',').Select(s => s.Trim());
        return phases.Any(p => p.Equals(phase.ToString(), StringComparison.OrdinalIgnoreCase))
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
