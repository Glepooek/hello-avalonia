using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
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
