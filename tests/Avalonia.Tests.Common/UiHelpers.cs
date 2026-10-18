using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        // Both lookups name what they were looking for when it is missing, and refuse to pick silently between
        // two controls with the same name: a duplicate would make a test click or read the wrong one.
        public static T ByName<T>(this Visual root, string name) where T : Control
            => Single<T>(root.GetVisualDescendants().OfType<T>().Where(c => c.Name == name), $"Name '{name}'");

        public static T ByAutomationId<T>(this Visual root, string id) where T : Control
            => Single<T>(root.GetVisualDescendants().OfType<T>().Where(c => AutomationProperties.GetAutomationId(c) == id), $"AutomationId '{id}'");

        private static T Single<T>(System.Collections.Generic.IEnumerable<T> matches, string what) where T : Control
        {
            var found = matches.Take(2).ToList();
            return found.Count switch
            {
                0 => throw new InvalidOperationException($"No {typeof(T).Name} with {what} in the visual tree."),
                1 => found[0],
                _ => throw new InvalidOperationException($"More than one {typeof(T).Name} has {what}; the lookup would be ambiguous."),
            };
        }

        // A point of `control`, expressed in `target`'s coordinates. TranslatePoint returns null when the two are not
        // in the same visual tree (removed, or never attached), and a bare !.Value would only say "no value".
        public static Point PointIn(this Visual control, Point point, Visual target)
            => control.TranslatePoint(point, target)
                ?? throw new InvalidOperationException(
                    $"{control.GetType().Name} '{(control as Control)?.Name}' and {target.GetType().Name} are not in the same visual tree, so the point cannot be translated.");

        // The control's centre in window coordinates, for clicking.
        public static Point CenterOf(this Window window, Control control)
            => control.PointIn(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window);

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        public static Button ButtonWithText(this Visual root, string text)
            => root.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        // Demo pages sit in ScrollViewers, so scroll the target into view first or the click lands on something else.
        // Flush queued work before that: a page that scrolls itself after Show (a ListBox bringing its first item
        // into view) would otherwise scroll the target away again between BringIntoView and the click.
        public static void Click(this Window window, Control control)
        {
            Dispatcher.UIThread.RunJobs();
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var center = window.CenterOf(control);
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
        }

        // Focus first, then send text: KeyPressQwerty alone produces no TextInput.
        public static void Type(this Window window, Control control, string text)
        {
            control.Focus();
            window.KeyTextInput(text);
        }

        // Renders whatever is pending and returns the window's current frame as PNG bytes. Needs the Skia headless
        // setup in HeadlessApp; with the default headless drawing the bitmap is 1x1 and every frame looks the same.
        public static byte[] CaptureFrame(this Window window)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var stream = new MemoryStream();
            window.CaptureRenderedFrame()!.Save(stream, new PngBitmapEncoderOptions());
            return stream.ToArray();
        }

        // Pumps the dispatcher until the condition holds, for work that finishes on its own clock (Task.Run, timers,
        // HTTP, file watchers). Throws rather than returning false, so a forgotten assert cannot turn a timeout into a pass.
        public static async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                Dispatcher.UIThread.RunJobs();
                if (condition())
                    return;
                if (watch.ElapsedMilliseconds > timeoutMs)
                    throw new TimeoutException($"Condition not met within {timeoutMs} ms.");
                await Task.Delay(10);
            }
        }
    }
}
