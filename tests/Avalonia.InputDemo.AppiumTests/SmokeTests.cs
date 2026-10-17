using Avalonia.Appium.Common;

namespace Avalonia.InputDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.InputDemo";
        private const string Title = "Avalonia Input Demo";
        private static readonly string[] Tabs = { "指针", "焦点", "手势", "键盘与 HotKey", "交互的写法", "拖放", "文本输入", "路由事件" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
