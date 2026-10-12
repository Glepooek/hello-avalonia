using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ItemsControlHowToPage : UserControl
    {
        private const int BigCount = 1000;
        private readonly ObservableCollection<string> _tags = ["Avalonia", "XAML"];
        private int _nextTag = 1;

        public ItemsControlHowToPage()
        {
            InitializeComponent();

            Tags.ItemsSource = _tags;
            AddTagButton.Click += (_, _) => _tags.Add($"标签 {_nextTag++}");
            ClearTagsButton.Click += (_, _) => _tags.Clear();

            // Subscribe first, then assign ItemsSource, so the first containers are not missed.
            Striped.ContainerPrepared += (_, e) =>
            {
                if (e.Index % 2 == 1)
                {
                    e.Container.Classes.Add("odd");
                }
                else
                {
                    e.Container.Classes.Remove("odd");
                }
            };
            Striped.ItemsSource = Enumerable.Range(1, 6).Select(i => $"第 {i} 行").ToList();

            Big.ItemsSource = Enumerable.Range(1, BigCount).Select(i => $"第 {i} 项").ToList();
            Big.LayoutUpdated += (_, _) =>
                RealizedText.Text = $"共 {Big.ItemCount} 项，已实现 {Big.GetRealizedContainers().Count()} 个容器";
        }
    }
}
