using System.Globalization;
using System.Linq;
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
    private object?[] _values = [];

    // 3 = every source resolved, allocates a SolidColorBrush; 2 = one still unresolved, DoNothing path.
    [Params(3, 2)]
    public int Channels;

    [GlobalSetup]
    public void Setup() => _values = new object?[] { 120d, 200d, 45d }.Take(Channels).ToArray();

    // Allocated is the number that matters here.
    [Benchmark]
    public object? Convert() => _converter.Convert(_values, typeof(IBrush), null, Culture);
}
