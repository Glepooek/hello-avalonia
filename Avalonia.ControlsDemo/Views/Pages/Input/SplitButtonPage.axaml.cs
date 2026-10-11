using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SplitButtonPage : UserControl
    {
        public SplitButtonPage()
        {
            InitializeComponent();
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e) => SplitResult.Text = "执行：保存";

        private void OnSaveAsClick(object? sender, RoutedEventArgs e) => SplitResult.Text = "执行：另存为";

        private void OnSaveAllClick(object? sender, RoutedEventArgs e) => SplitResult.Text = "执行：保存全部";
    }
}
