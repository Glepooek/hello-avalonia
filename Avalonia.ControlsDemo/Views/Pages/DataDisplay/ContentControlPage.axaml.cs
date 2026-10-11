using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ContentControlPage : UserControl
    {
        public ContentControlPage()
        {
            InitializeComponent();

            TextButton.Click += (_, _) => Apply("你好，ContentControl");
            ControlButton.Click += (_, _) => Apply(new Border
            {
                Padding = new Thickness(10, 4),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.SteelBlue,
                Child = new TextBlock { Text = "我是一个 Border", Foreground = Brushes.White },
            });
            ModelButton.Click += (_, _) => Apply(new DemoProduct { Name = "Apple", Price = 1.2m });
        }

        private void Apply(object content)
        {
            Host.Content = content;
            TypeText.Text = $"Content 的类型：{content.GetType().Name}";
        }
    }
}
