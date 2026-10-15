using Avalonia.Controls;
using Avalonia.Tests.Common;
using Avalonia.Threading;

namespace Avalonia.ControlsDemo.Tests
{
    // Shared by every page test in this project (05a How-to pages, 05b control pages).
    internal static class PageHelpers
    {
        public static (Window Window, TPage Page) Open<TPage>() where TPage : Control, new()
        {
            var page = new TPage();
            var window = UiHelpers.Show(page);
            Dispatcher.UIThread.RunJobs();
            return (window, page);
        }

        public static string Text(this Visual root, string name) => root.ByName<TextBlock>(name).Text ?? "";

        public static void Press(this Window window, string name)
        {
            window.Click(window.ByName<Control>(name));
            Dispatcher.UIThread.RunJobs();
        }

        public static void PressText(this Window window, string text)
        {
            window.Click(window.ButtonWithText(text));
            Dispatcher.UIThread.RunJobs();
        }
    }
}
