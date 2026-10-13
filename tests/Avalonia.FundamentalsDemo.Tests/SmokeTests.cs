using Avalonia.FundamentalsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.FundamentalsDemo.Tests
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
                new[] { "纯代码 UI", "Code-behind", "MVVM", "TopLevel", "视觉树与逻辑树", "生命周期与资源" },
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
