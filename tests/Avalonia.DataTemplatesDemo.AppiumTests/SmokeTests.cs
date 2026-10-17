using Avalonia.Appium.Common;

namespace Avalonia.DataTemplatesDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.DataTemplatesDemo";
        private const string Title = "Avalonia Data Templates Demo";
        private static readonly string[] Tabs = { "控件内容", "内联模板", "模板集合", "选择器", "代码建模板", "复用", "ViewLocator", "面板与树", "对比 ControlTemplate" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
