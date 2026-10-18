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

    [Benchmark(Baseline = true)]
    public object? Convert() => _converter.Convert(12.5, typeof(Thickness), null, Culture);

    // The UnsetValue branch is the one a careless edit turns into a throw.
    [Benchmark]
    public object? ConvertInvalid() => _converter.Convert("12.5", typeof(Thickness), null, Culture);
}
