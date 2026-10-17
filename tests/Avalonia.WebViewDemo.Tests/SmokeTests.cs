using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.WebViewDemo.ViewModels;
using Avalonia.WebViewDemo.Views;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Avalonia.WebViewDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_shows_the_three_demo_buttons_and_logs_nothing()
        {
            using var logs = new LogCollector();
            var window = new MainWindow { DataContext = new MainWindowViewModel() };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var buttons = window.GetVisualDescendants().OfType<Button>().ToArray();
                Assert.Equal(3, buttons.Length);
                Assert.All(buttons, b => Assert.NotNull(b.Command));
                Assert.NotNull(window.ButtonWithText("1. NativeWebView (embedded in a Window)"));
                Assert.NotNull(window.ButtonWithText("2. NativeWebDialog (standalone native window)"));
                Assert.NotNull(window.ButtonWithText("3. Call JS from C# (open #1 first)"));
                Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Avalonia 12 WebView");
                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }

        // The headless UI thread is MTA, and WebView2 refuses to start there (RPC_E_CHANGED_MODE), so the
        // window is only constructed, never shown; that still runs everything the constructor sets up.
        [AvaloniaFact]
        public void Web_view_window_points_at_the_bundled_page_and_shows_the_spinner()
        {
            var window = new WebViewWindow();
            try
            {
                var source = window.FindControl<NativeWebView>("webView")!.Source!;
                Assert.True(source.IsFile);
                Assert.Equal(Path.Combine(AppContext.BaseDirectory, "TestWeb", "index.html"), source.LocalPath);
                Assert.True(File.Exists(source.LocalPath));
                Assert.True(window.FindControl<Canvas>("canvas")!.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
