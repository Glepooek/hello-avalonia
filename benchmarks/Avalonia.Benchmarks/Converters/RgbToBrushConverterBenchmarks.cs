using System.Globalization;
using Avalonia.DataBindingDemo.Converters;
using Avalonia.Media;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class RgbToBrushConverterBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly RgbToBrushConverter _converter = new();
    private readonly object?[] _rgb = [120d, 200d, 45d];
    private readonly object?[] _incomplete = [120d, 200d];

    // Every resolved call allocates a SolidColorBrush; Allocated is the number that matters here.
    [Benchmark(Baseline = true)]
    public object? Convert() => _converter.Convert(_rgb, typeof(IBrush), null, Culture);

    // Unresolved sources take the DoNothing path: no allocation, same delegate call.
    [Benchmark]
    public object? ConvertIncomplete() => _converter.Convert(_incomplete, typeof(IBrush), null, Culture);
}
