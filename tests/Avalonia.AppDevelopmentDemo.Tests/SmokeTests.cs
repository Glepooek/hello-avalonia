using Avalonia.AppDevelopmentDemo.Views;
using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.AppDevelopmentDemo.Logging;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Application_container_constructs_the_main_window_and_its_dependencies()
        {
            using var services = ApplicationServices.CreateServiceProvider();

            var window = services.GetRequiredService<MainWindow>();

            Assert.NotNull(window);
            Assert.Contains(
                services.GetRequiredService<ListLoggerProvider>().Entries,
                entry => entry.StartsWith("Information:Main window created for 你好，Avalonia。现在是 "));
        }

        [AvaloniaFact]
        public void Cross_cutting_pages_are_read_only_explanations()
        {
            var pages = new UserControl[]
            {
                new DependencyInjectionPage(),
                new LoggingPage(),
                new UnhandledExceptionsPage()
            };

            Assert.All(pages, page => Assert.Empty(page.GetVisualDescendants().OfType<Button>()));
        }

        [AvaloniaFact]
        public void Main_window_walks_every_tab_without_errors()
        {
            using var services = ApplicationServices.CreateServiceProvider();
            var window = services.GetRequiredService<MainWindow>();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(
                new[] { "依赖注入", "本地化", "Web 内容", "日志", "未处理异常", "资源", "数据校验", "线程模型", "窗口管理", "无障碍" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            using var services = ApplicationServices.CreateServiceProvider();
            var window = services.GetRequiredService<MainWindow>();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
            Assert.Same(logs, Logger.Sink);
        }

        [AvaloniaFact]
        public void Logging_tab_does_not_replace_the_application_log_sink()
        {
            using var logs = new LogCollector();
            using var services = ApplicationServices.CreateServiceProvider();
            var window = services.GetRequiredService<MainWindow>();
            window.Show();
            var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
            try
            {
                Assert.Equal("日志", (tabs.Items[3] as TabItem)?.Header);

                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Assert.Same(logs, Logger.Sink);

                tabs.SelectedIndex = 4;
                Dispatcher.UIThread.RunJobs();
                Assert.Same(logs, Logger.Sink);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
