using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RepeatButtonPage : UserControl
    {
        private int _count;

        public RepeatButtonPage()
        {
            InitializeComponent();
        }

        private void OnRepeaterClick(object? sender, RoutedEventArgs e) => RepeatCount.Text = $"触发次数：{++_count}";
    }
}
