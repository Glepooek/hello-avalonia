using Avalonia.Appium.Common;

namespace Avalonia.GraphicsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.GraphicsDemo";
        private const string Title = "Avalonia Graphics Demo";
        private static readonly string[] Tabs = { "画刷与渐变", "变换", "形状与几何", "自定义绘制", "特效", "裁剪与命中", "图标", "渲染选项", "关键帧动画", "控件过渡", "页面过渡", "缓动函数", "合成动画" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
