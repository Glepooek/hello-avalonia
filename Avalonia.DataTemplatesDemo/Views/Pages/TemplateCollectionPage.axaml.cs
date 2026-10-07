using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class TemplateCollectionPage : UserControl
    {
        public TemplateCollectionPage()
        {
            InitializeComponent();

            Shape[] shapes =
            [
                new Circle { Name = "圆", Radius = 3 },
                new Square { Name = "方", Side = 4 },
                new Triangle { Name = "三角形", Base = 5 },
            ];

            SpecificFirst.ItemsSource = shapes;
            BaseFirst.ItemsSource = shapes;
        }
    }
}
