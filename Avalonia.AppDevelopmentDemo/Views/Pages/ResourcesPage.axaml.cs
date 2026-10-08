using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class ResourcesPage : UserControl
    {
        public ResourcesPage()
        {
            InitializeComponent();
            Reset();
        }

        private void Reset()
        {
            Resources["Brand"] = new SolidColorBrush(Colors.Red);
            // The one-shot lookup: the brush is read once and assigned, so it is never refreshed.
            StaticSwatch.Background = this.TryFindResource("Brand", ActualThemeVariant, out var brush) ? brush as IBrush : null;
            Refresh();
        }

        private void OnReplace(object? sender, RoutedEventArgs e)
        {
            Resources["Brand"] = new SolidColorBrush(Colors.Blue);
            Refresh();
        }

        private void OnMutate(object? sender, RoutedEventArgs e)
        {
            if (Resources["Brand"] is SolidColorBrush brush)
                brush.Color = Colors.Green;
            Refresh();
        }

        private void OnReset(object? sender, RoutedEventArgs e) => Reset();

        private void Refresh()
        {
            StaticLine.Text = $"TryFindResource 取到的：{Describe(StaticSwatch.Background)}";
            DynamicLine.Text = $"DynamicResource 的：{Describe(DynamicSwatch.Background)}";
        }

        private static string Describe(IBrush? brush)
            => brush is ISolidColorBrush solid ? solid.Color.ToString() : "（无）";
    }
}
