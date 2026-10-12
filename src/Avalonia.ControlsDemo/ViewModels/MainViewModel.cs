using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    public sealed partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Nodes = BuildTree(PageCatalog.All);

            // Open on the first page so the window is never an empty shell.
            var firstCategory = Nodes.FirstOrDefault(n => n.Children.Count > 0);
            if (firstCategory is not null)
            {
                firstCategory.IsExpanded = true;
                SelectedNode = firstCategory.Children[0];
            }
        }

        /// <summary>Category nodes, each holding its page nodes.</summary>
        public IReadOnlyList<NavNode> Nodes { get; }

        [ObservableProperty]
        private NavNode? _selectedNode;

        /// <summary>
        /// The page shown on the right. A fresh instance per selection, so every visit starts from
        /// the page's initial state. Null while a category row is selected.
        /// </summary>
        [ObservableProperty]
        private Control? _currentPage;

        partial void OnSelectedNodeChanged(NavNode? value) => CurrentPage = value?.Entry?.CreatePage();

        // GroupBy keeps first-appearance order, and PageCatalog registers categories in sidebar order.
        private static IReadOnlyList<NavNode> BuildTree(IReadOnlyList<PageEntry> entries)
            => entries
                .GroupBy(e => e.Category)
                .Select(g => new NavNode(
                    g.Key,
                    children: g.Select(e => new NavNode(e.Title, e)).ToList()))
                .ToList();
    }
}
