using Avalonia.Appium.Common;

namespace Avalonia.FundamentalsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.FundamentalsDemo";
        private const string Title = "Avalonia Fundamentals Demo";
        private static readonly string[] Tabs = { "纯代码 UI", "Code-behind", "MVVM", "TopLevel", "视觉树与逻辑树", "生命周期与资源" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
