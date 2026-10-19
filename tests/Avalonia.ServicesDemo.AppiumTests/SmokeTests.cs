using Avalonia.Appium.Common;

namespace Avalonia.ServicesDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.ServicesDemo";
        private const string Title = "Avalonia Services Demo";
        private static readonly string[] Tabs = { "剪贴板", "文件对话框", "StorageProvider", "焦点管理", "Launcher", "启动程序", "平台设置", "移动端服务", "可激活生命周期" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
