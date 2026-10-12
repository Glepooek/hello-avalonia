using Avalonia.Controls;
using Avalonia.FundamentalsDemo.Models;
using Avalonia.LogicalTree;
using Avalonia.Shared.ViewModels;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.FundamentalsDemo.ViewModels
{
    /// <summary>
    /// Walks both trees from the same root so the two panes can be compared
    /// side by side. The visual tree includes every part a control template
    /// expanded into; the logical tree stops at what the XAML author wrote.
    /// </summary>
    public sealed class TreesViewModel : ViewModelBase
    {
        public ObservableCollection<TreeNodeInfo> VisualTree { get; } = new();

        public ObservableCollection<TreeNodeInfo> LogicalTree { get; } = new();

        public void Refresh(Control root)
        {
            VisualTree.Clear();
            LogicalTree.Clear();
            VisualTree.Add(BuildVisual(root));
            LogicalTree.Add(BuildLogical(root));
        }

        private static TreeNodeInfo BuildVisual(Visual visual)
        {
            var node = new TreeNodeInfo(Describe(visual));
            foreach (var child in visual.GetVisualChildren())
            {
                node.Children.Add(BuildVisual(child));
            }
            return node;
        }

        private static TreeNodeInfo BuildLogical(ILogical logical)
        {
            var node = new TreeNodeInfo(Describe(logical));
            foreach (var child in logical.LogicalChildren)
            {
                node.Children.Add(BuildLogical(child));
            }
            return node;
        }

        private static string Describe(object node)
        {
            var typeName = node.GetType().Name;
            var name = (node as Control)?.Name;
            return string.IsNullOrEmpty(name) ? typeName : $"{typeName} \"{name}\"";
        }
    }
}
