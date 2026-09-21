using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Avalonia.Shared.Converters
{
    /// <summary>
    /// Turns a <see cref="double"/> into a uniform <see cref="Thickness"/>.
    /// <para>
    /// Avalonia cannot do this on its own: <see cref="Thickness"/> carries no
    /// TypeConverter and defines no conversion operator from double, so a
    /// binding like <c>Margin="{Binding #Slider.Value}"</c> fails at runtime
    /// without raising an exception — it only logs a binding error, leaving
    /// the control at its default margin.
    /// </para>
    /// </summary>
    public class DoubleToThicknessConverter : IValueConverter
    {
        public static readonly DoubleToThicknessConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is double length ? new Thickness(length) : AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Thickness thickness ? thickness.Left : AvaloniaProperty.UnsetValue;
        }
    }
}
