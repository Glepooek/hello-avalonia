using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;
using System.Linq;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class PanelsAndTreesPage : UserControl
    {
        public PanelsAndTreesPage()
        {
            InitializeComponent();

            var labels = Enumerable.Range(1, 12).Select(i => $"项 {i}").ToArray();
            DefaultPanelList.ItemsSource = labels;
            WrapPanelList.ItemsSource = labels;

            FolderTree.ItemsSource = new[]
            {
                new Folder("文档",
                    new Folder("工作", new Folder("2026 年报")),
                    new Folder("个人")),
                new Folder("图片",
                    new Folder("旅行")),
            };
        }
    }
}
