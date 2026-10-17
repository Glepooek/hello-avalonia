using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.HtmlRendererDemo.ViewModels;
using Avalonia.HtmlRendererDemo.Views;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using TheArtOfDev.HtmlRenderer.Avalonia;
using Xunit;

namespace Avalonia.HtmlRendererDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_shows_the_html_panel_and_logs_nothing()
        {
            using var logs = new LogCollector();
            var window = new MainWindow { DataContext = new MainWindowViewModel() };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                Assert.Equal("Avalonia HtmlRenderer & IconFont Demo", window.Title);
                var panel = Assert.Single(window.GetVisualDescendants().OfType<HtmlPanel>());
                Assert.False(string.IsNullOrEmpty(panel.Text));
                Assert.Single(window.GetVisualDescendants().OfType<PathIcon>());
                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
