using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ThemeVariantScopePage : UserControl
    {
        public ThemeVariantScopePage()
        {
            InitializeComponent();

            VariantBox.ItemsSource = new[] { ThemeVariant.Default, ThemeVariant.Light, ThemeVariant.Dark };
            VariantBox.SelectedItem = LiveScope.RequestedThemeVariant ?? ThemeVariant.Default;
            VariantBox.SelectionChanged += (_, _) =>
            {
                if (VariantBox.SelectedItem is ThemeVariant variant) LiveScope.RequestedThemeVariant = variant;
                // ActualThemeVariantChanged can fire before Requested has been stored, so refresh here too.
                Refresh();
            };
            LiveScope.ActualThemeVariantChanged += (_, _) => Refresh();

            Refresh();
        }

        private void Refresh()
        {
            VariantText.Text = $"Requested = {LiveScope.RequestedThemeVariant?.ToString() ?? "（空）"}，Actual = {LiveScope.ActualThemeVariant}";
        }
    }
}
