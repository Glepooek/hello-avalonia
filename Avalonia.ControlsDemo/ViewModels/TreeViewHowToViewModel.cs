using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>
    /// Every node in the mixed tree derives from this, so one TreeViewItem style can bind IsExpanded for folders and
    /// files alike. A style selector matches every TreeViewItem, so a binding typed to FolderNode would fail on files.
    /// </summary>
    public abstract partial class TreeNodeBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isExpanded;
    }

    public sealed class FolderNode : TreeNodeBase
    {
        public FolderNode(string name, params TreeNodeBase[] children)
        {
            Name = name;
            Children = new ObservableCollection<TreeNodeBase>(children);
        }

        public string Name { get; }

        public ObservableCollection<TreeNodeBase> Children { get; }
    }

    public sealed class FileNode : TreeNodeBase
    {
        public FileNode(string name, int sizeKb)
        {
            Name = name;
            SizeKb = sizeKb;
        }

        public string Name { get; }

        public int SizeKb { get; }
    }

    /// <summary>
    /// Loads its children the first time it is expanded. It starts with one placeholder child so the
    /// TreeView draws an expander arrow; the placeholder is replaced by the real children on first expand.
    /// </summary>
    public sealed partial class LazyNode : ObservableObject
    {
        private bool _loaded;

        [ObservableProperty]
        private bool _isExpanded;

        public LazyNode(string name)
        {
            Name = name;
            Children = [new LazyNode("加载中…", isLeaf: true)];
        }

        private LazyNode(string name, bool isLeaf)
        {
            Name = name;
            Children = [];
        }

        public string Name { get; }

        public int LoadCount { get; private set; }

        public ObservableCollection<LazyNode> Children { get; }

        partial void OnIsExpandedChanged(bool value)
        {
            if (!value || _loaded)
            {
                return;
            }

            _loaded = true;
            LoadCount++;
            Children.Clear();
            for (var i = 1; i <= 3; i++)
            {
                Children.Add(new LazyNode($"子项 {i}", isLeaf: true));
            }
        }
    }

    public sealed partial class TreeViewHowToViewModel : ObservableObject
    {
        public TreeViewHowToViewModel()
        {
            Roots =
            [
                new FolderNode("项目",
                    new FolderNode("src", new FileNode("App.axaml", 4), new FileNode("Program.cs", 2)),
                    new FileNode("README.md", 1)),
                new FolderNode("素材", new FileNode("logo.png", 38)),
            ];
            Lazy = [new LazyNode("远程目录")];
        }

        public ObservableCollection<TreeNodeBase> Roots { get; }

        public ObservableCollection<LazyNode> Lazy { get; }

        [ObservableProperty]
        private string _selectionText = "选中：（无）";

        public void ExpandAll(IEnumerable<TreeNodeBase> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is FolderNode folder)
                {
                    folder.IsExpanded = true;
                    ExpandAll(folder.Children);
                }
            }
        }

        public void CollapseAll(IEnumerable<TreeNodeBase> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is FolderNode folder)
                {
                    folder.IsExpanded = false;
                    CollapseAll(folder.Children);
                }
            }
        }
    }
}
