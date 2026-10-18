using System;
using Avalonia.DataBindingDemo.ViewModels;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.ViewModels;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class CollectionViewsFilterBenchmarks
{
    private CollectionViewsViewModel _vm = null!;

    // Rebuild() filters on Name, so these are name fragments, not cities.
    [Params("", "张", "孙")]
    public string Term = "";

    // Filter is state: the second run would start from an already rebuilt list.
    [IterationSetup]
    public void NewViewModel() => _vm = new CollectionViewsViewModel();

    // Setting Filter runs the private Rebuild(): Where + OrderBy + ObservableCollection refill.
    [Benchmark]
    public void ApplyFilter()
    {
        _vm.Filter = Term;

        if (_vm.Rows.Count == 0) throw new InvalidOperationException("filter dropped every row");
    }
}
