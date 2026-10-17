using Avalonia.Appium.Common;

namespace Avalonia.AppDevelopmentDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.AppDevelopmentDemo";
        private const string Title = "Avalonia App Development Demo";
        private static readonly string[] Tabs = { "依赖注入", "本地化", "Web 内容", "日志", "未处理异常", "资源", "数据校验", "线程模型", "窗口管理", "无障碍" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
