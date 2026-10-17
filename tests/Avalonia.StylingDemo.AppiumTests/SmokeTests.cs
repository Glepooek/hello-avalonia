using Avalonia.Appium.Common;

namespace Avalonia.StylingDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.StylingDemo";
        private const string Title = "Avalonia Styling Demo";
        private static readonly string[] Tabs = { "选择器", "样式类", "伪类", "ControlTheme", "主题变体", "容器查询", "字体", "样式共享", "优先级" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
