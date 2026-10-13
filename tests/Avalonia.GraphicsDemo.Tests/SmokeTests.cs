using Avalonia.GraphicsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.GraphicsDemo.Tests
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
                new[]
                {
                    "画刷与渐变", "变换", "形状与几何", "自定义绘制", "特效", "裁剪与命中", "图标",
                    "渲染选项", "关键帧动画", "控件过渡", "页面过渡", "缓动函数", "合成动画",
                },
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
