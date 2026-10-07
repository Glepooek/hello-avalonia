using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ControlContentPage : UserControl
    {
        public ControlContentPage()
        {
            InitializeComponent();

            // Same kind of object into two ContentControls; neither has a template for it.
            ObjectContent.Content = new Circle { Name = "圆", Radius = 3 };
            ObjectButton.Content = new Circle { Name = "圆", Radius = 3 };
            TemplatedContent.Content = new Circle { Name = "圆", Radius = 3 };
        }
    }
}
