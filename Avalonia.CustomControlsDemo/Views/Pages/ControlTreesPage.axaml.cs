using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class ControlTreesPage : UserControl
    {
        public ControlTreesPage() => InitializeComponent();

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Readout.Text =
                $"视觉树宿主的子元素 DataContext = {VisualOnly.Child.DataContext ?? "null"}；" +
                $"双树宿主的子元素 DataContext = {Both.Child.DataContext ?? "null"}";
        }
    }
}
