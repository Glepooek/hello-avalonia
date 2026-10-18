using System;
using Avalonia.Shared.Helpers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Helpers;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class EventLogBenchmarks
{
    private EventLog _log = null!;

    // 0 = empty, 59 = one below the trim threshold, 60 = every write now removes an entry.
    [Params(0, 59, 60)]
    public int Prefill;

    [IterationSetup]
    public void NewLog()
    {
        _log = new EventLog();
        for (var i = 0; i < Prefill; i++)
        {
            _log.Write("evt");
        }
    }

    // Write does string interpolation plus, past 60 entries, an ObservableCollection.RemoveAt(0).
    [Benchmark]
    public void Write()
    {
        _log.Write("pointer pressed");

        if (_log.Entries.Count > 60) throw new InvalidOperationException("EventLog stopped trimming");
    }
}
