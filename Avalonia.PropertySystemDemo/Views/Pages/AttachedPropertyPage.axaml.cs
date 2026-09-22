using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.PropertySystemDemo.Controls;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class AttachedPropertyPage : UserControl
    {
        public AttachedPropertyPage()
        {
            InitializeComponent();
            ShowInherited();
        }

        private void OnHighlightToggled(object? sender, RoutedEventArgs e)
        {
            var on = (sender as CheckBox)?.IsChecked == true;
            HighlightBehavior.SetIsHighlighted(TargetA, on);
        }

        private void OnSetTagLineClick(object? sender, RoutedEventArgs e)
        {
            // Set once, on the outermost Border only.
            HighlightBehavior.SetTagLine(InheritRoot, "我在最外层被设置");
            ShowInherited();
        }

        private void ShowInherited()
        {
            Level1Text.Text = $"第 1 层读到：{Describe(HighlightBehavior.GetTagLine(Level1Text))}";
            Level2Text.Text = $"第 2 层读到：{Describe(HighlightBehavior.GetTagLine(Level2Text))}";
        }

        private static string Describe(string? value)
            => string.IsNullOrEmpty(value) ? "（尚未设置）" : value;
    }
}
