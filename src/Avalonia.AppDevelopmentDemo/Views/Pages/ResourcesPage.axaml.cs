using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class ResourcesPage : UserControl
    {
        public ResourcesPage()
        {
            InitializeComponent();
            Reset();
            RefreshThemeLines();
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

        private void OnLightTheme(object? sender, RoutedEventArgs e)
        {
            ThemeScope.RequestedThemeVariant = ThemeVariant.Light;
            Dispatcher.UIThread.Post(RefreshThemeLines, DispatcherPriority.Loaded);
        }

        private void OnDarkTheme(object? sender, RoutedEventArgs e)
        {
            ThemeScope.RequestedThemeVariant = ThemeVariant.Dark;
            Dispatcher.UIThread.Post(RefreshThemeLines, DispatcherPriority.Loaded);
        }

        private void Refresh()
        {
            StaticLine.Text = $"TryFindResource 取到的：{Describe(StaticSwatch.Background)}";
            DynamicLine.Text = $"DynamicResource 的：{Describe(DynamicSwatch.Background)}";
        }

        private void RefreshThemeLines()
        {
            MergedLine.Text = $"合并字典：{Describe(MergedSwatch.Background)}";
            ThemeLine.Text = $"主题资源：{Describe(ThemeSwatch.Background)}";
        }

        private static string Describe(IBrush? brush)
        {
            if (brush is not ISolidColorBrush solid)
                return "（无）";

            var value = solid.Color.ToString();
            return value.StartsWith('#') ? value.ToUpperInvariant() : value;
        }
    }
}
