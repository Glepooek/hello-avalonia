using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.ServicesDemo.Views;
using Avalonia.ServicesDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ServicesDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Visual root, string name) => root.ByName<TextBlock>(name).Text ?? "";

        // Every service call on these pages is async, so click and then wait for the result line to change.
        private static async Task ClickAndWait(Window window, string buttonName, Func<string, bool> resultIs)
        {
            window.Click(window.ByName<Button>(buttonName));
            await UiHelpers.WaitUntil(() => resultIs(Text(window, "Result")));
        }

        // ClearButton ends a row wider than the 600px test window, so widen it or the click lands outside.
        [AvaloniaFact]
        public async Task Clipboard_text_round_trips_and_clear_empties_it()
        {
            var window = UiHelpers.Show(new ClipboardPage());
            window.Width = 900;

            await ClickAndWait(window, "SetTextButton", r => r == "已用 SetTextAsync 写入");
            await ClickAndWait(window, "ReadButton", r => r.StartsWith("TryGetTextAsync"));
            Assert.Equal("TryGetTextAsync = \"来自 Avalonia 的文本\"", Text(window, "Result"));

            await ClickAndWait(window, "ClearButton", r => r == "已清空");
            await ClickAndWait(window, "ReadButton", r => r.StartsWith("TryGetTextAsync"));
            Assert.Equal("TryGetTextAsync 返回 null（剪贴板里没有文本）", Text(window, "Result"));
        }

        // Headless has no picker UI: the page must say so instead of awaiting a dialog that never comes.
        [AvaloniaFact]
        public async Task File_dialog_buttons_report_that_the_platform_cannot_show_pickers()
        {
            var window = UiHelpers.Show(new FileDialogsPage());

            await ClickAndWait(window, "OpenButton", r => r.Contains("CanOpen"));
            Assert.Equal("当前平台的 StorageProvider.CanOpen 为 False，无法弹出打开对话框", Text(window, "Result"));
            await ClickAndWait(window, "SaveButton", r => r.Contains("CanSave"));
            Assert.Equal("当前平台的 StorageProvider.CanSave 为 False，无法弹出保存对话框", Text(window, "Result"));
            await ClickAndWait(window, "FolderButton", r => r.Contains("CanPickFolder"));
            Assert.Equal("当前平台的 StorageProvider.CanPickFolder 为 False，无法弹出文件夹对话框", Text(window, "Result"));
        }

        [AvaloniaFact]
        public void Storage_page_shows_the_capabilities_of_the_platform()
        {
            var window = UiHelpers.Show(new StorageProviderPage());
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("CanOpen = False，CanSave = False，CanPickFolder = False", Text(window, "Capabilities"));
        }

        // TryGetFolderFromPathAsync reads the real disk even without picker support, so give it a folder whose
        // contents the test controls. Enumeration order is the file system's, so compare as sets.
        [AvaloniaFact]
        public async Task Folder_from_path_lists_the_items_of_a_real_folder()
        {
            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "services-demo-tests-" + Guid.NewGuid().ToString("N")));
            try
            {
                File.WriteAllText(Path.Combine(dir.FullName, "a.txt"), "a");
                dir.CreateSubdirectory("sub");
                var window = UiHelpers.Show(new StorageProviderPage());
                window.ByName<TextBox>("PathBox").Text = dir.FullName;

                await ClickAndWait(window, "PathButton", r => r.StartsWith("TryGetFolderFromPathAsync"));

                Assert.Equal($"TryGetFolderFromPathAsync → {dir.Name}，前 2 项", Text(window, "Result"));
                Assert.Equal(
                    new[] { "[夹] sub", "[文件] a.txt" }.OrderBy(s => s),
                    window.ByName<ListBox>("Items").Items.Cast<string>().OrderBy(s => s));
            }
            finally
            {
                dir.Delete(recursive: true);
            }
        }

        [AvaloniaFact]
        public async Task Folder_from_a_missing_path_reports_null_instead_of_throwing()
        {
            var window = UiHelpers.Show(new StorageProviderPage());
            window.ByName<TextBox>("PathBox").Text = Path.Combine(Path.GetTempPath(), "services-demo-missing-" + Guid.NewGuid().ToString("N"));

            await ClickAndWait(window, "PathButton", r => r.StartsWith("TryGetFolderFromPathAsync"));

            Assert.Equal("TryGetFolderFromPathAsync 返回 null（路径不存在，或平台不提供）", Text(window, "Result"));
            Assert.Equal(0, window.ByName<ListBox>("Items").ItemCount);
        }

        // The type check runs before any click: if the headless platform ever ships a real launcher, this test must
        // fail here rather than open a browser on the machine running the tests.
        [AvaloniaFact]
        public async Task Launcher_buttons_report_the_result_of_the_noop_launcher()
        {
            var window = UiHelpers.Show(new LauncherPage());
            Assert.Equal("NoopLauncher", TopLevel.GetTopLevel(window)!.Launcher!.GetType().Name);

            await ClickAndWait(window, "UriButton", r => r.StartsWith("LaunchUriAsync"));
            Assert.Equal("LaunchUriAsync 返回 False", Text(window, "Result"));

            window.ByName<TextBox>("UriBox").Text = "not a uri";
            await ClickAndWait(window, "UriButton", r => r == "不是合法的绝对 URI");

            await ClickAndWait(window, "DirButton", r => r.StartsWith("LaunchDirectoryInfoAsync"));
            Assert.Equal("LaunchDirectoryInfoAsync 返回 False", Text(window, "Result"));
        }

        [AvaloniaFact]
        public void Mobile_services_are_reported_as_missing_on_the_desktop()
        {
            var window = UiHelpers.Show(new MobileServicesPage());
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("InputPane：null（当前平台没有软键盘服务）", Text(window, "InputPaneLine"));
            Assert.Equal("InsetsManager：null（当前平台没有安全区服务）", Text(window, "InsetsLine"));
        }

        [AvaloniaFact]
        public void Platform_settings_page_lists_the_hotkeys_and_the_theme()
        {
            var window = UiHelpers.Show(new PlatformSettingsPage());
            Dispatcher.UIThread.RunJobs();

            Assert.StartsWith("ThemeVariant = ", Text(window, "ThemeLine"));
            Assert.Equal("Copy：Ctrl+C、Ctrl+Insert", Text(window, "CopyLine"));
            Assert.Equal("Paste：Ctrl+V、Shift+Insert", Text(window, "PasteLine"));
            Assert.Equal("Undo：Ctrl+Z", Text(window, "UndoLine"));
        }

        private static int Activations(Visual root)
            => int.Parse(Regex.Match(Text(root, "ActivationCounts"), @"Window\.Activated 触发 (\d+) 次").Groups[1].Value);

        [AvaloniaFact]
        public void Activation_page_counts_window_activation_without_a_platform_lifetime()
        {
            var window = UiHelpers.Show(new ActivatableLifetimePage());
            Dispatcher.UIThread.RunJobs();
            var before = Activations(window);

            window.Activate();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("IActivatableLifetime：null（当前平台不支持，见下表）", Text(window, "AvailabilityLine"));
            Assert.Equal("ActivationKind 取值：File、OpenUri、Reopen、Background", Text(window, "WindowLine"));
            Assert.Equal(before + 1, Activations(window));
        }

        // Loaded runs on every visit to the tab; before the fix each visit added another Activated handler,
        // so one activation counted once per visit so far.
        [AvaloniaFact]
        public void Revisiting_the_activation_tab_still_counts_one_activation_once()
        {
            var window = new MainWindow();
            window.Show();
            var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
            void Select(int index)
            {
                tabs.SelectedIndex = index;
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("可激活生命周期", (tabs.Items[7] as TabItem)?.Header);

            Select(7);
            Select(0);
            Select(7);
            var before = Activations(window);
            window.Activate();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(before + 1, Activations(window));
        }
    }
}
