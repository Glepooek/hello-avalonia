using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>
    /// One row of the navigation tree: either a category (no <see cref="Entry"/>, has children)
    /// or a page (has an entry, no children). A single type keeps the TreeView template and the
    /// IsExpanded binding uniform across both levels.
    /// </summary>
    public sealed partial class NavNode : ObservableObject
    {
        public NavNode(string title, PageEntry? entry = null, IReadOnlyList<NavNode>? children = null)
        {
            Entry = entry;
            Children = children ?? Array.Empty<NavNode>();
            DisplayTitle = entry?.Kind switch
            {
                PageKind.HowTo => $"实战：{title}",
                PageKind.Signpost => $"↗ {title}",
                _ => title,
            };
        }

        public PageEntry? Entry { get; }

        public IReadOnlyList<NavNode> Children { get; }

        public string DisplayTitle { get; }

        [ObservableProperty]
        private bool _isExpanded;
    }
}
