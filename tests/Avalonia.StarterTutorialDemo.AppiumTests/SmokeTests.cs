using Avalonia.Appium.Common;

namespace Avalonia.StarterTutorialDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.StarterTutorialDemo";
        private const string Title = "Avalonia Starter Tutorial Demo";
        private static readonly string[] Tabs = { "1 添加控件", "2 添加布局", "3 自定义窗口", "4 事件与响应", "5 数据转换", "6 练习" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
