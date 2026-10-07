using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.StylingDemo.Views.Pages
{
    public partial class StyleClassesPage : UserControl
    {
        public StyleClassesPage()
        {
            InitializeComponent();
            ShowClasses();
        }

        private void OnToggleGreen(object? sender, RoutedEventArgs e)
        {
            // Classes.Set adds or removes in one call; Add/Remove are the long-hand form.
            CodeSwatch.Classes.Set("green", !CodeSwatch.Classes.Contains("green"));
            ShowClasses();
        }

        private void ShowClasses()
        {
            CodeClassesText.Text = $"当前 Classes：{string.Join(" ", CodeSwatch.Classes)}";
        }
    }
}
