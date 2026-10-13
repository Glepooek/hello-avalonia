using Avalonia.CustomControlsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.CustomControlsDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_walks_every_tab_without_errors()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(
                new[] { "用户控件", "模板化控件", "自绘控件", "属性与事件", "控件树", "自定义面板", "自定义 Flyout" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
        }
    }
}
