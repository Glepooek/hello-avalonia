using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ContentTemplatesPage : UserControl
    {
        public ContentTemplatesPage()
        {
            InitializeComponent();

            SingleCircle.Content = new Circle { Name = "大圆", Radius = 40 };
            CircleList.ItemsSource = new[]
            {
                new Circle { Name = "小", Radius = 12 },
                new Circle { Name = "中", Radius = 24 },
                new Circle { Name = "大", Radius = 36 },
            };
        }
    }
}
