using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class CustomPanelPage : UserControl
    {
        public CustomPanelPage()
        {
            InitializeComponent();
            for (var i = 0; i < 6; i++)
            {
                Radial.Children.Add(CreateDot());
            }
        }

        private static Border CreateDot() => new()
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = Brushes.OrangeRed,
        };

        private void OnAddClick(object? sender, RoutedEventArgs e) => Radial.Children.Add(CreateDot());

        private void OnRemoveClick(object? sender, RoutedEventArgs e)
        {
            if (Radial.Children.Count > 0)
            {
                Radial.Children.RemoveAt(Radial.Children.Count - 1);
            }
        }
    }
}
