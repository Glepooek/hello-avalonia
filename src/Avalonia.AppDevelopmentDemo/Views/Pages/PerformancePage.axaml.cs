using Avalonia.Controls;
using Avalonia.AppDevelopmentDemo.ViewModels;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class PerformancePage : UserControl
    {
        public PerformancePage()
        {
            InitializeComponent();
            DataContext = new PerformancePageViewModel();
        }

        private void OnMeasureRealized(object? sender, RoutedEventArgs e)
        {
            var realized = VirtualizedList.GetVisualDescendants().OfType<ListBoxItem>().Count();
            RealizedCountText.Text = $"已实现容器：{realized}";
        }
    }
}
