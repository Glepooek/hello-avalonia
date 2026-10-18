using Avalonia.DataBindingDemo.ViewModels;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.ViewModels;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class CollectionViewsViewModelBenchmarks
{
    // Rebuild() filters on Name: "" keeps all six rows, "张" keeps one, "周" keeps none.
    [Params("", "张", "周")]
    public string Term = "";

    // The constructor already runs one full Rebuild(); Ratio on Filter is the extra cost of filtering.
    [Benchmark(Baseline = true)]
    public int Constructor() => new CollectionViewsViewModel().Rows.Count;

    // Setting Filter runs the private Rebuild(): Where + OrderBy + ObservableCollection refill.
    // A fresh VM per call keeps every call on the same path without [IterationSetup].
    // "" equals the initial value, so the setter skips Rebuild() and Ratio stays near 1.
    [Benchmark]
    public int Filter() => new CollectionViewsViewModel { Filter = Term }.Rows.Count;
}
