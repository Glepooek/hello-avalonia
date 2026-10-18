using System.Globalization;
using Avalonia.DataBindingDemo.Converters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class CelsiusToFahrenheitConverterBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly CelsiusToFahrenheitConverter _converter = new();

    // Runs on every slider tick; ToString("F1") allocates a new string each time.
    [Benchmark]
    public object? Convert() => _converter.Convert(21.5, typeof(string), null, Culture);

    // Runs on every keystroke: double.TryParse, then the result is boxed into object.
    [Benchmark]
    public object? ConvertBack() => _converter.ConvertBack("70.7", typeof(double), null, Culture);
}
