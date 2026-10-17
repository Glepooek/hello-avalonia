using Avalonia.Appium.Common;

namespace Avalonia.BehaviorsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.BehaviorsDemo";
        private const string Title = "Avalonia Behaviors Demo";
        private static readonly string[] Tabs = { "机制", "事件转命令", "数据驱动", "焦点键盘指针", "表单验证", "集合", "窗口弹出通知", "拖拽", "响应式", "动画", "文件系统", "网络", "剪贴板", "截图", "自定义" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
