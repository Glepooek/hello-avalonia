using Avalonia.Data.Converters;

namespace Avalonia.Shared.Converters
{
    /// <summary>Numeric conversions the demo pages need to wire a double-valued control to an int property.</summary>
    public static class NumberConverters
    {
        /// <summary>double → int by truncation; anything that is not a double yields 0.</summary>
        public static readonly IValueConverter DoubleToInt = new FuncValueConverter<double, int>(v => (int)v);
    }
}
