using System.Globalization;
using Avalonia.Shared.Converters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class DoubleToThicknessConverterBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly DoubleToThicknessConverter _converter = DoubleToThicknessConverter.Instance;
    private object? _value;

    // "string" takes the UnsetValue branch, the one a careless edit turns into a throw.
    [Params("double", "string")]
    public string Input = "double";

    [GlobalSetup]
    public void Setup() => _value = Input == "double" ? 12.5 : "12.5";

    [Benchmark]
    public object? Convert() => _converter.Convert(_value, typeof(Thickness), null, Culture);
}
