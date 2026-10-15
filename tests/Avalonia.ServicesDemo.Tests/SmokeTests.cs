using Avalonia.Headless.XUnit;
using Avalonia.ServicesDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.ServicesDemo.Tests
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
                new[] { "剪贴板", "文件对话框", "StorageProvider", "焦点管理", "Launcher", "平台设置", "移动端服务", "可激活生命周期" },
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
