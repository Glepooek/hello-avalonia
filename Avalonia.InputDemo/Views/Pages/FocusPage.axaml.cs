using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class FocusPage : UserControl
    {
        public FocusPage()
        {
            InitializeComponent();
        }

        // GotFocus bubbles, so one handler on the row sees all four buttons.
        // The argument type is FocusChangedEventArgs (Avalonia.Input) — there is no GotFocusEventArgs.
        private void OnRowGotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (e.Source is Button button)
            {
                var pseudo = string.Join(" ", button.Classes.Where(c => c.StartsWith(':')));
                FocusReadout.Text = $"焦点在 {button.Content}  via {e.NavigationMethod}  [{pseudo}]";
            }
        }

        private void OnFocusViaTab(object? sender, RoutedEventArgs e) => BtnB.Focus(NavigationMethod.Tab);

        private void OnFocusViaPointer(object? sender, RoutedEventArgs e) => BtnB.Focus(NavigationMethod.Pointer);

        private void OnQueryFocus(object? sender, RoutedEventArgs e)
        {
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            QueryReadout.Text = $"FocusManager.GetFocusedElement() = {(focused as Control)?.Name ?? focused?.GetType().Name ?? "null"}";
        }
    }
}
