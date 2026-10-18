using System.Globalization;
using Avalonia.DataBindingDemo.Converters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class CelsiusConverterBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly CelsiusToFahrenheitConverter _converter = new();

    [Benchmark(Baseline = true)]
    public object? Convert() => _converter.Convert(21.5, typeof(string), null, Culture);

    // ConvertBack runs on every keystroke: double.TryParse plus the round trip back.
    [Benchmark]
    public object? ConvertBack() => _converter.ConvertBack("70.7", typeof(double), null, Culture);
}
