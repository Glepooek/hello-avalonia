using Avalonia.Appium.Common;

namespace Avalonia.CustomControlsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.CustomControlsDemo";
        private const string Title = "Avalonia Custom Controls Demo";
        private static readonly string[] Tabs = { "用户控件", "模板化控件", "自绘控件", "属性与事件", "控件树", "自定义面板", "自定义 Flyout" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
