using Avalonia.Data.Converters;
using System;

namespace Avalonia.DataBindingDemo.Converters
{
    public static class InlineConverters
    {
        // One-way only: FuncValueConverter has no ConvertBack.
        public static readonly FuncValueConverter<double, string> Thermometer =
            new(c => c switch
            {
                < 0 => "❄ 结冰",
                < 25 => "🙂 舒适",
                _ => "🔥 炎热",
            } + $"（{Math.Round(c)} °C）");
    }
}
