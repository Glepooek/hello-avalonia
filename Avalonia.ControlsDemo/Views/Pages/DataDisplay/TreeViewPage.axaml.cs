using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TreeViewPage : UserControl
    {
        public TreeViewPage()
        {
            InitializeComponent();

            Tree.ItemsSource = DemoTreeNode.Sample();
            Tree.SelectionChanged += (_, _) =>
                SelectedText.Text = Tree.SelectedItem is DemoTreeNode node ? $"选中：{node.Label}" : "选中：（无）";

            // The roots are TreeViewItem containers only after layout, so expand through GetRealizedTreeContainers.
            ExpandAllButton.Click += (_, _) =>
            {
                foreach (var item in Tree.GetRealizedTreeContainers().OfType<TreeViewItem>())
                {
                    Tree.ExpandSubTree(item);
                }
            };
            CollapseAllButton.Click += (_, _) =>
            {
                foreach (var item in Tree.GetRealizedTreeContainers().OfType<TreeViewItem>())
                {
                    Tree.CollapseSubTree(item);
                }
            };

            ModeBox.ItemsSource = Enum.GetValues<SelectionMode>();
            ModeBox.SelectedItem = Tree.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SelectionMode mode)
                {
                    Tree.SelectionMode = mode;
                }
            };
        }
    }
}
