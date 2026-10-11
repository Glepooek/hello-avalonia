using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TreeViewHowToPage : UserControl
    {
        public TreeViewHowToPage()
        {
            InitializeComponent();
            var model = new TreeViewHowToViewModel();
            DataContext = model;

            ExpandAllButton.Click += (_, _) => model.ExpandAll(model.Roots);
            CollapseAllButton.Click += (_, _) => model.CollapseAll(model.Roots);

            Mixed.SelectionChanged += (_, _) =>
                MixedText.Text = Mixed.SelectedItem switch
                {
                    FolderNode folder => $"选中文件夹：{folder.Name}（{folder.Children.Count} 项）",
                    FileNode file => $"选中文件：{file.Name}（{file.SizeKb} KB）",
                    _ => "选中：（无）",
                };

            // Expanding the root runs the model's loader; show how many times it actually loaded.
            LazyTree.LayoutUpdated += (_, _) => LazyText.Text = $"远程目录已加载 {model.Lazy[0].LoadCount} 次";
        }
    }
}
