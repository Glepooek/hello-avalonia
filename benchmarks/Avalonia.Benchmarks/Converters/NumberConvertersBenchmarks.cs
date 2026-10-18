using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Shared.Converters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

/// <summary>
/// Measures the FuncValueConverter wrapper itself, not the cast: the same
/// truncation inlined would cost nothing, so any number here is delegate overhead.
/// </summary>
[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class NumberConvertersBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly IValueConverter _converter = NumberConverters.DoubleToInt;

    [Benchmark]
    public object? Convert() => _converter.Convert(42.7, typeof(int), null, Culture);
}
