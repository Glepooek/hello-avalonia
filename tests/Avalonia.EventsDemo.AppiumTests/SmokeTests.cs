using Avalonia.Appium.Common;

namespace Avalonia.EventsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.EventsDemo";
        private const string Title = "Avalonia Events Demo";
        private static readonly string[] Tabs = { "生命周期", "输入事件", "路由三阶段", "Handled", "自定义路由事件" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
