using System;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>An item type for templated ComboBox content; public so XAML can name it in x:DataType.</summary>
    public sealed record Employee(string Name, string Department);

    public enum Priority
    {
        Low,
        Normal,
        High,
    }

    /// <summary>An enum value paired with the text shown for it; enum members themselves cannot carry display text.</summary>
    public sealed record PriorityOption(Priority Value, string Display)
    {
        public static PriorityOption[] All { get; } = Enum.GetValues<Priority>()
            .Select(p => new PriorityOption(p, p switch
            {
                Priority.Low => "低",
                Priority.Normal => "普通",
                _ => "高",
            }))
            .ToArray();
    }
}
