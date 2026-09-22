using System.Collections.Generic;

namespace Avalonia.XamlDemo.Models
{
    public enum PriorityLevel
    {
        Low,
        Normal,
        High,
    }

    /// <summary>Static members reachable from XAML through x:Static.</summary>
    public static class DemoConstants
    {
        public const string AppTitle = "XAML 参考演示";

        public static readonly PriorityLevel DefaultPriority = PriorityLevel.Normal;
    }

    /// <summary>
    /// A closed generic type. XAML can also spell the open form with
    /// x:TypeArguments; this named subclass is the alternative.
    /// </summary>
    public sealed class PriorityList : List<PriorityLevel>
    {
    }
}
