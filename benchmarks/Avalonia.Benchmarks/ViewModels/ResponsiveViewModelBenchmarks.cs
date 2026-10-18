using System;
using Avalonia.LayoutDemo.ViewModels;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.ViewModels;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class ResponsiveViewModelBenchmarks
{
    // Mirrors ResponsiveViewModel's private threshold so the guard can tell whether
    // the breakpoint decision was actually applied. A change here means the business
    // rule changed, and this benchmark should be re-read rather than silently pass.
    private const double Breakpoint = 500d;
    private const double CompactWidth = 420d;
    private const double WideWidth = 900d;

    private ResponsiveViewModel _vm = null!;

    // false: [ObservableProperty] setters compare before assigning, so passing the width the
    // VM already has raises nothing. This is the floor, and writing the benchmark this way
    // by accident is the trap — it reports "free" and answers nothing.
    // true: alternating across the breakpoint makes all three setters assign and notify,
    // which is what a resize that actually changes the layout pays for.
    [Params(false, true)]
    public bool CrossesBreakpoint;

    // Primed to CompactWidth so the no-change case starts already settled.
    [GlobalSetup]
    public void NewViewModel()
    {
        _vm = new ResponsiveViewModel();
        _vm.UpdateLayout(CompactWidth);
    }

    [Benchmark]
    public void UpdateLayout()
    {
        var width = CrossesBreakpoint && _vm.CurrentWidth == CompactWidth ? WideWidth : CompactWidth;
        _vm.UpdateLayout(width);
        Guard(width);
    }

    private void Guard(double expectedWidth)
    {
        if (_vm.CurrentWidth != expectedWidth) throw new InvalidOperationException("CurrentWidth was not applied");
        if (_vm.IsCompact != (expectedWidth < Breakpoint)) throw new InvalidOperationException("breakpoint was not applied");
    }
}
