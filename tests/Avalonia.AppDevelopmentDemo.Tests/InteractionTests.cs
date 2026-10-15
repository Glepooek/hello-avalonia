using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Window window, string name) => window.ByName<TextBlock>(name).Text ?? "";

        private static void Press(Window window, string buttonName)
        {
            window.Click(window.ByName<Button>(buttonName));
            Dispatcher.UIThread.RunJobs();
        }

        // Ids come from a static counter, so compare lines across clicks instead of asserting numbers.
        [AvaloniaFact]
        public void Resolving_twice_shows_new_scoped_instances_and_the_same_singleton()
        {
            var window = UiHelpers.Show(new DependencyInjectionPage());

            Press(window, "ResolveButton");
            var transient = Text(window, "TransientLine");
            var scoped = Text(window, "ScopedLine");
            var singleton = Text(window, "SingletonLine");
            Press(window, "ResolveButton");

            Assert.StartsWith("Transient：", transient);
            Assert.EndsWith("不同实例", transient);
            Assert.EndsWith("同一个实例", scoped);
            Assert.EndsWith("同一个实例", singleton);
            // A new scope per click: the scoped id moves on, the singleton id does not.
            Assert.NotEqual(scoped, Text(window, "ScopedLine"));
            Assert.Equal(singleton, Text(window, "SingletonLine"));
        }

        [AvaloniaFact]
        public void Greeter_comes_from_the_container_and_a_scoped_service_cannot_come_from_the_root()
        {
            using var culture = CultureScope.Invariant();
            var window = UiHelpers.Show(new DependencyInjectionPage());

            Press(window, "GreetButton");
            Press(window, "RootScopedButton");

            Assert.Matches(new Regex(@"^你好，Avalonia。现在是 \d{2}:\d{2}:\d{2}$"), Text(window, "GreetLine"));
            Assert.Equal(
                "InvalidOperationException：Cannot resolve scoped service 'Avalonia.AppDevelopmentDemo.Services.Counter' from root provider.",
                Text(window, "ScopeErrorLine"));
        }

        [AvaloniaFact]
        public void Switching_language_reloads_the_strings_from_the_resource_file()
        {
            var window = UiHelpers.Show(new LocalizationPage());
            Assert.Equal(new[] { "你好，世界", "再见", "culture = zh-CN" },
                new[] { Text(window, "GreetingText"), Text(window, "FarewellText"), Text(window, "CultureText") });

            window.Click(window.ByName<RadioButton>("EnglishRadio"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "Hello, world", "Goodbye", "culture = en-US" },
                new[] { Text(window, "GreetingText"), Text(window, "FarewellText"), Text(window, "CultureText") });

            window.Click(window.ByName<RadioButton>("ChineseRadio"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("你好，世界", Text(window, "GreetingText"));
        }

        // The page's point: a one-shot lookup keeps the brush it got, DynamicResource follows the key, and both
        // see a change made to the brush object they share.
        [AvaloniaFact]
        public void Replacing_a_resource_moves_only_the_dynamic_swatch_but_mutating_it_moves_both()
        {
            var window = UiHelpers.Show(new ResourcesPage());
            string[] Lines() => new[] { Text(window, "StaticLine"), Text(window, "DynamicLine") };
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Red" }, Lines());

            Press(window, "ReplaceButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Blue" }, Lines());

            // The key now points at the blue brush, which the static swatch never saw.
            Press(window, "MutateButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Green" }, Lines());

            Press(window, "ResetButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Red" }, Lines());

            Press(window, "MutateButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Green", "DynamicResource 的：Green" }, Lines());
        }

        [AvaloniaFact]
        public void Reading_the_peer_shows_the_automation_properties_set_in_xaml()
        {
            var window = UiHelpers.Show(new AccessibilityPage());

            Press(window, "ReadButton");

            Assert.Equal("Peer 类型：ButtonAutomationPeer", Text(window, "PeerType"));
            Assert.Equal("Name：Confirm", Text(window, "PeerName"));
            Assert.Equal("AutomationId：ok-btn", Text(window, "PeerId"));
            Assert.Equal("HelpText：Confirms the form", Text(window, "PeerHelp"));
            Assert.Equal("ControlType：Button", Text(window, "PeerControl"));
            Assert.Equal("Decoration 的 AccessibilityView = Raw", Text(window, "ViewLine"));
        }

        // The framework entry goes through Dispatcher.Post, so wait for it instead of reading right after the click.
        [AvaloniaFact]
        public async Task Logging_page_lists_app_log_entries_and_the_framework_binding_warning()
        {
            var window = UiHelpers.Show(new LoggingPage());
            try
            {
                var app = window.ByName<ItemsControl>("AppEntries");
                var framework = window.ByName<ItemsControl>("AvaloniaEntries");

                Press(window, "AppLogButton");
                Press(window, "AvaloniaLogButton");
                await UiHelpers.WaitUntil(() => framework.ItemCount > 0);

                Assert.Equal(new[] { "Debug:d 1", "Warning:w", "Error:e" }, app.Items.Cast<string>());
                var entry = Assert.Single(framework.Items.Cast<string>());
                Assert.StartsWith("Warning/Binding:", entry);
                Assert.Contains("Missing", entry);

                Press(window, "AppClearButton");
                Press(window, "AvaloniaClearButton");
                Assert.Equal(0, app.ItemCount);
                Assert.Equal(0, framework.ItemCount);
            }
            finally
            {
                window.Close();
            }
        }

        // The handler marks the exception handled, so RunJobs must not rethrow it into the test.
        [AvaloniaFact]
        public void A_throwing_posted_callback_reaches_the_page_handler_instead_of_crashing()
        {
            var window = UiHelpers.Show(new UnhandledExceptionsPage());
            try
            {
                Press(window, "ThrowButton");

                Assert.Equal("处理器被调用：1 次", Text(window, "CountLine"));
                Assert.Equal("最近一次异常：InvalidOperationException：thrown from a posted callback", Text(window, "MessageLine"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Writing_from_a_background_thread_throws_and_leaves_the_control_unchanged()
        {
            var window = UiHelpers.Show(new ThreadingPage());

            window.Click(window.ByName<Button>("DirectButton"));
            await UiHelpers.WaitUntil(() => Text(window, "Result").StartsWith("CheckAccess"));

            Assert.StartsWith("CheckAccess = False，InvalidOperationException：", Text(window, "Result"));
            Assert.Equal("（被写的控件）", Text(window, "Target"));
        }

        [AvaloniaFact]
        public async Task Posting_back_to_the_ui_thread_writes_the_control()
        {
            var window = UiHelpers.Show(new ThreadingPage());

            window.Click(window.ByName<Button>("PostButton"));
            await UiHelpers.WaitUntil(() => Text(window, "Target") == "written via Post");

            Assert.Equal("Post 的回调里 CheckAccess = True，写入成功", Text(window, "Result"));
        }

        [AvaloniaFact]
        public async Task Code_after_await_resumes_on_the_ui_thread()
        {
            var window = UiHelpers.Show(new ThreadingPage());

            window.Click(window.ByName<Button>("AwaitButton"));
            await UiHelpers.WaitUntil(() => Text(window, "Target") == "written after await");

            Assert.Equal("await 之后 CheckAccess = True，写入成功", Text(window, "Result"));
        }

        // The page needs a real Window as its TopLevel to act as owner; UiHelpers.Show provides one.
        [AvaloniaFact]
        public void Show_with_owner_makes_the_page_window_the_owner()
        {
            var window = UiHelpers.Show(new WindowManagementPage());
            try
            {
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("WindowState = Normal，CanResize = True，SizeToContent = Manual，Topmost = False", Text(window, "StateLine"));

                Press(window, "ShowButton");

                Assert.Equal("Show(owner) 之后 child.Owner 是主窗口：True", Text(window, "Result"));
                var child = Assert.Single(window.OwnedWindows);
                Assert.Equal("非模态子窗口", child.Title);
            }
            finally
            {
                window.Close();
            }
        }

        // Headless ShowDialog does not block the test: the dialog is just another owned window to click in.
        [AvaloniaFact]
        public async Task Show_dialog_returns_the_value_passed_to_close()
        {
            var window = UiHelpers.Show(new WindowManagementPage());
            try
            {
                Press(window, "DialogButton");
                var dialog = Assert.Single(window.OwnedWindows);
                Assert.Equal("模态对话框", dialog.Title);

                dialog.Click(dialog.GetVisualDescendants().OfType<Button>().Single());
                await UiHelpers.WaitUntil(() => Text(window, "Result") == "ShowDialog 返回：accepted");

                Assert.False(dialog.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Closing_handler_keeps_the_window_open_until_closing_is_allowed()
        {
            var window = UiHelpers.Show(new WindowManagementPage());
            try
            {
                Press(window, "GuardButton");
                var guard = Assert.Single(window.OwnedWindows);
                Assert.Equal("拦截关闭", guard.Title);

                guard.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(guard.IsVisible);
                Assert.StartsWith("Closing 里 e.Cancel = true", Text(window, "Result"));

                guard.Click(guard.GetVisualDescendants().OfType<CheckBox>().Single());
                guard.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.False(guard.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
