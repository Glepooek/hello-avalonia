using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.Tests.Common
{
    // Shared helpers for the headless UI tests.
    public static class UiHelpers
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

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        public static Button ButtonWithText(this Visual root, string text)
            => root.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        // Demo pages sit in ScrollViewers, so scroll the target into view first or the click lands on something else.
        public static void Click(this Window window, Control control)
        {
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
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
