using Avalonia.Appium.Common;

namespace Avalonia.PropertySystemDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.PropertySystemDemo";
        private const string Title = "Avalonia Property System Demo";
        private static readonly string[] Tabs = { "StyledProperty", "DirectProperty", "附加属性", "值优先级", "元数据与回调" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
