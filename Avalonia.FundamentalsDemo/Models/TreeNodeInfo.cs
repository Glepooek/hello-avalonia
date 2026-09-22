using System.Collections.ObjectModel;

namespace Avalonia.FundamentalsDemo.Models
{
    /// <summary>
    /// One node in a snapshot of either tree. The snapshot is a plain model
    /// rather than the live control, so the TreeView cannot accidentally
    /// re-parent the controls it is displaying.
    /// </summary>
    public sealed class TreeNodeInfo
    {
        public TreeNodeInfo(string label)
        {
            Label = label;
        }

        public string Label { get; }

        public ObservableCollection<TreeNodeInfo> Children { get; } = new();
    }
}
