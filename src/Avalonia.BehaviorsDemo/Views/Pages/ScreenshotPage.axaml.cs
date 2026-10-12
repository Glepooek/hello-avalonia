using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ScreenshotPage : UserControl
    {
        public ScreenshotPage()
        {
            InitializeComponent();
            DataContext = new ScreenshotViewModel();
        }
    }
}
