using Avalonia.AppDevelopmentDemo.Views;
using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
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
                new[] { "依赖注入", "本地化", "Web 内容", "日志", "未处理异常", "资源", "数据校验", "线程模型", "窗口管理", "无障碍" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        // Blind spot: while the 日志 tab is selected, LoggingPage owns Logger.Sink and this collector hears nothing
        // (see the next test), so a warning raised on that page alone would not fail this test.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
            Assert.Same(logs, Logger.Sink);
        }

        // Logger.Sink is process-wide: if LoggingPage did not hand it back on unload, every later test in the
        // process would report to the page instead of to its own LogCollector.
        [AvaloniaFact]
        public void Logging_tab_owns_the_log_sink_only_while_it_is_selected()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();
            var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
            try
            {
                Assert.Equal("日志", (tabs.Items[3] as TabItem)?.Header);

                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Assert.NotSame(logs, Logger.Sink);

                tabs.SelectedIndex = 4;
                Dispatcher.UIThread.RunJobs();
                Assert.Same(logs, Logger.Sink);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Closing_the_window_on_the_logging_page_restores_the_previous_sink()
        {
            using var logs = new LogCollector();
            var window = UiHelpers.Show(new LoggingPage());
            Dispatcher.UIThread.RunJobs();
            Assert.NotSame(logs, Logger.Sink);

            window.Close();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(logs, Logger.Sink);
        }
    }
}
