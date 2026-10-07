using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.StylingDemo.Views.Pages
{
    public partial class ThemesPage : UserControl
    {
        public ThemesPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ShowActual();
        }

        private void OnLight(object? sender, RoutedEventArgs e) => Apply(ThemeVariant.Light);

        private void OnDark(object? sender, RoutedEventArgs e) => Apply(ThemeVariant.Dark);

        // Default means "no preference": the platform's setting decides.
        private void OnDefault(object? sender, RoutedEventArgs e) => Apply(ThemeVariant.Default);

        private void Apply(ThemeVariant variant)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = variant;
            }

            ShowActual();
        }

        private void ShowActual()
        {
            var app = Application.Current;
            ActualText.Text = $"请求：{app?.RequestedThemeVariant}　实际：{app?.ActualThemeVariant}";
        }
    }
}
