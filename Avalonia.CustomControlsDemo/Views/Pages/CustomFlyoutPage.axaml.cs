using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class CustomFlyoutPage : UserControl
    {
        public CustomFlyoutPage() => InitializeComponent();

        private void OnColorPicked(object? sender, Color colour)
        {
            Chosen.Background = new SolidColorBrush(colour);
            ChosenText.Text = colour.ToString();
        }
    }
}
