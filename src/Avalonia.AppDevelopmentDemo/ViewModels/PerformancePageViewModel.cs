using System.Collections.Generic;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.ViewModels
{
    public sealed record PerformanceItem(int Index, string Name);

    public sealed class PerformancePageViewModel
    {
        public IReadOnlyList<PerformanceItem> Items { get; } = Enumerable.Range(1, 1000)
            .Select(index => new PerformanceItem(index, $"项目 {index:0000}"))
            .ToArray();
    }
}
