using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Avalonia.DataBindingDemo.Converters
{
    /// <summary>
    /// A two-way converter: ConvertBack runs when the user types into the
    /// Fahrenheit box and pushes the value back to the Celsius slider.
    /// </summary>
    public sealed class CelsiusToFahrenheitConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is double c ? (c * 9 / 5 + 32).ToString("F1", culture) : BindingOperations.DoNothing;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Half-typed input such as "-" is normal while editing: leave the source alone
            // rather than flag an error. Reporting errors is ValidationPage's job.
            return value is string s && double.TryParse(s, NumberStyles.Float, culture, out var f)
                ? (f - 32) * 5 / 9
                : BindingOperations.DoNothing;
        }
    }
}
