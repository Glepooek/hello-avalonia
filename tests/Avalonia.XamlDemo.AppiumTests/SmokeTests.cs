using Avalonia.Appium.Common;

namespace Avalonia.XamlDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.XamlDemo";
        private const string Title = "Avalonia XAML Demo";
        private static readonly string[] Tabs = { "命名空间", "x: 指令", "标记扩展", "类型转换器", "泛型", "XAML 编译", "平台相关 XAML" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
