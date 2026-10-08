using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.TestingDemo.Tests
{
    // Shared helpers for the headless UI tests.
    internal static class UiHelpers
    {
        public static Window Show(Control page)
        {
            var window = new Window { Content = page, Width = 600, Height = 480 };
            window.Show();
            return window;
        }

        public static T ByName<T>(this Visual root, string name) where T : Control
            => root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        public static T ByAutomationId<T>(this Visual root, string id) where T : Control
            => root.GetVisualDescendants().OfType<T>().First(c => AutomationProperties.GetAutomationId(c) == id);

        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        public static void Click(this Window window, Control control)
        {
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
        }

        // Focus first, then send text: KeyPressQwerty alone produces no TextInput.
        public static void Type(this Window window, Control control, string text)
        {
            control.Focus();
            window.KeyTextInput(text);
        }
    }
}
