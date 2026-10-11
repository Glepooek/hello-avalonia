using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ButtonPage : UserControl
    {
        private int _clicks, _releases, _presses;

        public ButtonPage()
        {
            InitializeComponent();
        }

        private void OnClickButtonClick(object? sender, RoutedEventArgs e) => ClickCount.Text = $"点击次数：{++_clicks}";

        private void OnReleaseClick(object? sender, RoutedEventArgs e) => ReleaseCount.Text = (++_releases).ToString();

        private void OnPressClick(object? sender, RoutedEventArgs e) => PressCount.Text = (++_presses).ToString();
    }
}
