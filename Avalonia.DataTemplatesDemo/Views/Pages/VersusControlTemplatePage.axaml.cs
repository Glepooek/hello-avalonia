using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class VersusControlTemplatePage : UserControl
    {
        public VersusControlTemplatePage()
        {
            InitializeComponent();

            var circle = new Circle { Name = "同一个圆", Radius = 7 };
            foreach (var button in new[] { Neither, DataOnly, ControlOnly, Both })
            {
                button.Content = circle;
            }
        }
    }
}
