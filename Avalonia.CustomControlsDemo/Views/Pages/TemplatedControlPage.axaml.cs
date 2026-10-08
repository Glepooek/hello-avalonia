using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class TemplatedControlPage : UserControl
    {
        public TemplatedControlPage() => InitializeComponent();

        private void OnSwapClick(object? sender, RoutedEventArgs e)
        {
            // Looks up the key in Application.Resources, where Meter.axaml was merged.
            if (this.TryFindResource("TextMeter", ActualThemeVariant, out var theme) && theme is ControlTheme textTheme)
            {
                TheMeter.Theme = textTheme;
            }
        }
    }
}
