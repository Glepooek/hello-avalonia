using Avalonia.Appium.Common;

namespace Avalonia.DataBindingDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.DataBindingDemo";
        private const string Title = "Avalonia Data Binding Demo";
        private static readonly string[] Tabs = { "绑定语法", "编译绑定", "集合", "主从视图", "多值绑定", "命令", "转换器", "校验", "集合视图", "异步与图片", "绑定调试", "标记扩展" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
