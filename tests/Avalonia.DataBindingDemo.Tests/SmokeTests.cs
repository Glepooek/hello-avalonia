using Avalonia.DataBindingDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System.Linq;
using Xunit;

namespace Avalonia.DataBindingDemo.Tests
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
                new[] { "绑定语法", "编译绑定", "集合", "主从视图", "多值绑定", "命令", "转换器", "校验", "集合视图", "异步与图片", "绑定调试", "标记扩展" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // The debugging tab breaks two bindings on purpose; any other warning, or a missing one, is a regression.
        // This also doubles as the positive control for LogCollector: it must see real binding warnings.
        [AvaloniaFact]
        public void Walking_every_tab_logs_only_the_two_deliberate_debugging_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, logs.Messages.Count);
            Assert.Single(logs.Messages, m => m.Contains("Selected.Name") && m.Contains("Value is null"));
            Assert.Single(logs.Messages, m => m.Contains("Selectd") && m.Contains("Could not find a matching property accessor"));
        }
    }
}
