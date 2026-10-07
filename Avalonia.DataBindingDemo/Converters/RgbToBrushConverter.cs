using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.DataBindingDemo.Converters
{
    /// <summary>
    /// Three slider values in, one brush out. A MultiBinding hands the converter
    /// every source value at once, in the order the bindings were declared.
    /// </summary>
    public sealed class RgbToBrushConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 3 && values[0] is double r && values[1] is double g && values[2] is double b)
            {
                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }

            // While a source is still unresolved, keep whatever the target already shows.
            return BindingOperations.DoNothing;
        }
    }
}
