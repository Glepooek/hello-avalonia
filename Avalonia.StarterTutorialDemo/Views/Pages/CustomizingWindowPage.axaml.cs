using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.StarterTutorialDemo.Views.Pages
{
    public partial class CustomizingWindowPage : UserControl
    {
        public CustomizingWindowPage()
        {
            InitializeComponent();
        }

        private async void OpenWindowButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            // Same attributes the tutorial sets in XAML, applied in code for the pop-up.
            var window = new Window
            {
                Title = "GetStartedApp",
                Width = 250,
                Height = 180,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "固定 250×180，不可缩放",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };

            await window.ShowDialog(owner);
        }
    }
}
