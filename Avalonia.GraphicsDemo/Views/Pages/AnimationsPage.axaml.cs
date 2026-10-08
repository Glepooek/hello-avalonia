using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class AnimationsPage : UserControl
    {
        public AnimationsPage()
        {
            InitializeComponent();
        }

        private void OnRunToggle(object? sender, RoutedEventArgs e)
        {
            var on = ((ToggleButton)RunToggle).IsChecked == true;

            // Adding or removing the class is the whole switch: the animations are declared in styles.
            foreach (var ball in new[] { Pulse, Once, Rainbow })
            {
                ball.Classes.Set("run", on);
            }
        }
    }
}
