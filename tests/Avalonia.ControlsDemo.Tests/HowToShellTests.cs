using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the How-to pages of Media, Menus, Navigation, Primitives and System.
    public class HowToShellTests
    {
        // ---- Media ----

        // The placeholder's Border is what hides, so the TextBlock itself stays IsVisible; check the effective value.
        [AvaloniaFact]
        public void Loading_the_logo_hides_the_placeholder_and_clearing_brings_it_back()
        {
            var (window, _) = PageHelpers.Open<ImageHowToPage>();
            var placeholder = window.ByName<TextBlock>("PlaceholderText");
            Assert.True(placeholder.IsEffectivelyVisible);
            Assert.IsType<WriteableBitmap>(window.ByName<Image>("Avatar").Source);

            window.Press("LoadButton");
            Assert.False(placeholder.IsEffectivelyVisible);
            Assert.Equal(64, ((Bitmap)window.ByName<Image>("PhotoImage").Source!).PixelSize.Width);
            Assert.Equal("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico（DecodeToWidth 64）", window.Text("SourceText"));

            window.Press("ClearButton");
            Assert.True(placeholder.IsEffectivelyVisible);
            Assert.Equal("（未加载）", window.Text("SourceText"));
        }

        [AvaloniaFact]
        public void Screenshot_button_renders_the_card_into_a_bitmap()
        {
            var (window, _) = PageHelpers.Open<ImageHowToPage>();
            Assert.Equal("尚未截图", window.Text("ShotText"));

            window.Press("ShotButton");

            Assert.Equal("已截图 160×60", window.Text("ShotText"));
            Assert.IsType<RenderTargetBitmap>(window.ByName<Image>("ShotImage").Source);
        }

        // ---- Menus ----

        // KeyBindings on a UserControl only see keys while focus is inside it: right after Show nothing has focus.
        [AvaloniaFact]
        public void Ctrl_s_saves_only_once_focus_is_inside_the_page()
        {
            var (window, page) = PageHelpers.Open<MenuHowToPage>();
            var vm = (MenuHowToViewModel)page.DataContext!;
            void CtrlS()
            {
                window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
                window.KeyRelease(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
                Dispatcher.UIThread.RunJobs();
            }

            CtrlS();
            Assert.Equal("最近操作：（无）", window.Text("HotKeyText"));

            window.Click(window.ByName<ListBox>("Items").ContainerFromIndex(0)!);
            Dispatcher.UIThread.RunJobs();
            CtrlS();
            Assert.Equal("最近操作：保存（Ctrl+S 或菜单都会走到这里）", window.Text("HotKeyText"));
            Assert.Equal("保存（Ctrl+S 或菜单都会走到这里）", vm.LastAction);
        }

        [AvaloniaFact]
        public void Context_menu_deletes_the_selected_item()
        {
            var (window, page) = PageHelpers.Open<MenuHowToPage>();
            var vm = (MenuHowToViewModel)page.DataContext!;
            var list = window.ByName<ListBox>("Items");
            list.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();

            list.ContextMenu!.Open(list);
            Dispatcher.UIThread.RunJobs();
            window.Click(list.ContextMenu.Items.OfType<MenuItem>().Single(m => "删除".Equals(m.Header)));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new[] { "苹果", "樱桃" }, vm.Items);
            Assert.Equal("最近操作：删除 面包", window.Text("HotKeyText"));
            Assert.False(list.ContextMenu.IsOpen);
        }

        [AvaloniaFact]
        public void Check_menu_item_toggles_bold_and_clearing_recent_files_leaves_a_placeholder()
        {
            var (window, page) = PageHelpers.Open<MenuHowToPage>();
            var vm = (MenuHowToViewModel)page.DataContext!;
            var format = window.GetVisualDescendants().OfType<Menu>().First().Items.OfType<MenuItem>().ElementAt(1);

            window.Click(format);
            Dispatcher.UIThread.RunJobs();
            window.Click(format.Items.OfType<MenuItem>().Single(m => "_粗体".Equals(m.Header)));
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.IsBold);

            var recent = window.ByName<MenuItem>("RecentMenu");
            Assert.Equal(3, recent.ItemCount);
            window.PressText("清空最近的文件");
            Assert.IsType<EmptyRecent>(recent.Items.Cast<object>().Single());
        }

        // ---- Navigation ----

        [AvaloniaFact]
        public void Navigation_buttons_move_through_the_history()
        {
            var (window, _) = PageHelpers.Open<NavigationHowToPage>();
            string Line(string prefix) => window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text?.StartsWith(prefix) == true).Text!;
            Assert.False(window.ButtonWithText("◀ 后退").IsEffectivelyEnabled);

            window.PressText("设置");
            window.PressText("关于");
            Assert.Equal("当前页：关于", Line("当前页"));
            Assert.Equal("后退栈 2 项，前进栈 0 项", Line("后退栈"));

            window.PressText("◀ 后退");
            Assert.Equal("当前页：设置", Line("当前页"));
            Assert.True(window.ButtonWithText("前进 ▶").IsEffectivelyEnabled);
            window.PressText("前进 ▶");
            Assert.Equal("当前页：关于", Line("当前页"));
        }

        // Avalonia 12's TransitioningContentControl starts with a private cross-fade class, not CrossFade, so the
        // page used to show "无" while every page change still faded.
        [AvaloniaFact]
        public void Transition_box_shows_the_transition_the_host_really_uses()
        {
            var (window, _) = PageHelpers.Open<NavigationHowToPage>();

            Assert.NotNull(window.ByName<TransitioningContentControl>("Host").PageTransition);
            Assert.Equal("CrossFade", window.ByName<ComboBox>("TransitionBox").SelectedItem);
        }

        [AvaloniaFact]
        public void Transition_box_swaps_the_host_transition()
        {
            var (window, _) = PageHelpers.Open<NavigationHowToPage>();
            var box = window.ByName<ComboBox>("TransitionBox");
            var host = window.ByName<TransitioningContentControl>("Host");

            box.SelectedItem = "PageSlide";
            Assert.IsType<PageSlide>(host.PageTransition);
            box.SelectedItem = "无";
            Assert.Null(host.PageTransition);
            box.SelectedItem = "CrossFade";
            Assert.Equal(TimeSpan.FromMilliseconds(250), Assert.IsType<CrossFade>(host.PageTransition).Duration);
        }

        [AvaloniaFact]
        public void New_tab_button_adds_and_selects_and_a_close_button_removes_its_tab()
        {
            var (window, page) = PageHelpers.Open<TabControlHowToPage>();
            var vm = (TabControlHowToViewModel)page.DataContext!;
            var tabs = window.ByName<TabControl>("Tabs");
            Button[] Closers() => tabs.GetVisualDescendants().OfType<Button>().ToArray();
            Assert.Equal(3, Closers().Length);

            window.PressText("新建标签");
            Assert.Equal(4, tabs.ItemCount);
            Assert.Equal(3, tabs.SelectedIndex);
            Assert.Equal(4, Closers().Length);

            window.Click(Closers()[0]);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "日志", "设置", "文档 4" }, vm.Tabs.Select(t => t.Title));
            Assert.Equal("文档 4", vm.SelectedTab?.Title);
        }

        // Moving the strip rebuilds the headers, which drops the selection; the page puts it back afterwards.
        [AvaloniaFact]
        public void Moving_the_tab_strip_keeps_the_selected_tab()
        {
            var (window, page) = PageHelpers.Open<TabControlHowToPage>();
            var vm = (TabControlHowToViewModel)page.DataContext!;
            var tabs = window.ByName<TabControl>("Tabs");
            tabs.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();

            window.ByName<ComboBox>("PlacementBox").SelectedItem = Dock.Left;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Dock.Left, tabs.TabStripPlacement);
            Assert.Equal("设置", vm.SelectedTab?.Title);
        }

        // Only the first step is built when the page opens; each step builds once, when first shown.
        [AvaloniaFact]
        public void Wizard_builds_each_step_when_it_is_first_shown()
        {
            var (window, page) = PageHelpers.Open<TabControlHowToPage>();
            var vm = (TabControlHowToViewModel)page.DataContext!;
            Assert.Equal(1, vm.CreatedCount);
            Assert.False(window.ButtonWithText("上一步").IsEffectivelyEnabled);

            window.PressText("下一步");
            window.PressText("下一步");
            Assert.Equal(3, vm.CreatedCount);
            Assert.False(window.ButtonWithText("下一步").IsEffectivelyEnabled);

            window.PressText("上一步");
            Assert.Equal(1, vm.WizardIndex);
            Assert.Equal(3, vm.CreatedCount);
        }

        // ---- Primitives: windows ----

        // Every test that opens windows closes the host in finally: the page closes what it opened when it leaves
        // the visual tree, so nothing leaks into later tests.
        [AvaloniaFact]
        public void Free_and_owned_windows_are_counted_and_close_all_closes_both_kinds()
        {
            var (window, _) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                Assert.Equal("独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）", window.Text("WindowsText"));

                window.Press("OwnedButton");
                window.Press("FreeButton");
                window.Press("OwnedButton");
                Assert.Equal("独立窗口 1 个，子窗口 2 个；所有者的 OwnedWindows 共 2 个（含下面两块开的窗口）", window.Text("WindowsText"));
                Assert.Equal(2, window.OwnedWindows.Count);

                window.Press("CloseAllButton");
                Assert.Equal("独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）", window.Text("WindowsText"));
                Assert.Empty(window.OwnedWindows);
            }
            finally
            {
                window.Close();
            }
        }

        // RestoreButton ends at x=567 of the 600px window; widen it so the click cannot miss.
        [AvaloniaFact]
        public void Place_buttons_save_move_and_restore_the_sample_window()
        {
            var (window, _) = PageHelpers.Open<WindowHowToPage>();
            window.Width = 900;
            try
            {
                Assert.Equal("示例窗口：未打开；已记下：（还没有）", window.Text("PlaceText"));
                window.Press("PlaceOpenButton");
                window.Press("SaveButton");
                var saved = window.Text("PlaceText");
                Assert.EndsWith("ClientSize = 280×140；已记下：Position = " + saved.Split("Position = ")[1].Split('，')[0] + "，ClientSize = 280×140", saved);

                window.Press("MoveButton");
                Assert.StartsWith("示例窗口：Position = 200, 120，ClientSize = 420×260；", window.Text("PlaceText"));

                window.Press("RestoreButton");
                Assert.Equal(saved, window.Text("PlaceText"));

                window.Press("PlaceCloseButton");
                Assert.StartsWith("示例窗口：未打开；已记下：Position = ", window.Text("PlaceText"));
            }
            finally
            {
                window.Close();
            }
        }

        // ShowDialog does not block in Headless: the confirm box is simply another owned window of the editor.
        [AvaloniaFact]
        public void Closing_a_dirty_editor_asks_first_and_only_ok_lets_it_close()
        {
            var (window, page) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                window.Press("EditorButton");
                var editor = page.Editor!;
                editor.Type(editor.ByName<TextBox>("EditorBox"), "x");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("有未保存的更改", editor.Text("EditorState"));

                editor.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(editor.IsVisible);
                var confirm = Assert.Single(editor.OwnedWindows);
                confirm.Click(confirm.ByName<Button>("CancelButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("窗口有未保存的更改；Closing 被拦下 1 次；最近一次：用户选了「取消」，窗口留着", window.Text("ConfirmText"));
                Assert.True(editor.IsVisible);

                editor.Close();
                Dispatcher.UIThread.RunJobs();
                confirm = Assert.Single(editor.OwnedWindows);
                confirm.Click(confirm.ByName<Button>("OkButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("窗口未打开；Closing 被拦下 2 次；最近一次：用户选了「确定」，窗口随后关闭", window.Text("ConfirmText"));
                Assert.False(editor.IsVisible);
                Assert.Null(page.Editor);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void A_saved_editor_closes_without_asking()
        {
            var (window, page) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                window.Press("EditorButton");
                var editor = page.Editor!;
                editor.Type(editor.ByName<TextBox>("EditorBox"), "y");
                editor.Click(editor.ByName<Button>("EditorSaveButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("窗口没有未保存的更改；Closing 被拦下 0 次；最近一次：（还没触发）", window.Text("ConfirmText"));

                editor.Close();
                Dispatcher.UIThread.RunJobs();

                Assert.False(editor.IsVisible);
                Assert.Equal("窗口未打开；Closing 被拦下 0 次；最近一次：（还没触发）", window.Text("ConfirmText"));
            }
            finally
            {
                window.Close();
            }
        }

        // Review Focus: the page owns the windows it opened. Taking it out of the visual tree must close them all,
        // including the free ones that no owner would close.
        [AvaloniaFact]
        public void Leaving_the_page_closes_every_window_it_opened()
        {
            var (window, page) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                window.Press("FreeButton");
                window.Press("OwnedButton");
                window.Press("PlaceOpenButton");
                window.Press("EditorButton");
                var editor = page.Editor!;
                editor.Type(editor.ByName<TextBox>("EditorBox"), "dirty");
                Dispatcher.UIThread.RunJobs();
                var opened = window.OwnedWindows.ToList();

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.All(opened, w => Assert.False(w.IsVisible));
                Assert.Equal("独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）", page.Text("WindowsText"));
                // A dirty editor would normally ask; leaving the page must not leave a confirm box behind.
                Assert.Empty(editor.OwnedWindows);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Primitives: dialogs ----

        private static Window Dialog(Window owner) => Assert.Single(owner.OwnedWindows);

        [AvaloniaFact]
        public async Task Confirm_dialog_reports_ok_and_closing_the_window()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("删除…");
                Assert.Equal("确认", Dialog(window).Title);
                Dialog(window).Click(Dialog(window).ByName<Button>("OkButton"));
                await UiHelpers.WaitUntil(() => vm.ConfirmResult == "结果 true：用户点了「确定」");
                Assert.Empty(window.OwnedWindows);

                window.PressText("删除…");
                Dialog(window).Close();
                await UiHelpers.WaitUntil(() => vm.ConfirmResult == "结果 null：用户直接关掉了窗口，既没确定也没取消");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Name_prompt_enables_ok_only_for_text_and_trims_the_answer()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("起名字…");
                var dialog = Dialog(window);
                var ok = dialog.ByName<Button>("OkButton");
                Assert.False(ok.IsEnabled);
                Assert.True(dialog.ByName<Button>("CancelButton").IsEnabled);

                dialog.Type(dialog.ByName<TextBox>("AnswerBox"), "  小明 ");
                Dispatcher.UIThread.RunJobs();
                Assert.True(ok.IsEnabled);
                dialog.Click(ok);

                await UiHelpers.WaitUntil(() => vm.NameResult == "名字：小明");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Required_prompt_cannot_be_cancelled_or_closed_without_an_answer()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("必答的问题…");
                var dialog = Dialog(window);
                Assert.False(dialog.ByName<Button>("CancelButton").IsEnabled);

                dialog.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(dialog.IsVisible);

                dialog.Type(dialog.ByName<TextBox>("AnswerBox"), "答");
                Dispatcher.UIThread.RunJobs();
                dialog.Click(dialog.ByName<Button>("OkButton"));
                await UiHelpers.WaitUntil(() => vm.NameResult == "名字（必答）：答");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Colour_dialog_picks_a_swatch_and_cancel_keeps_it()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("选颜色…");
                Dialog(window).Click(Dialog(window).ByName<Button>("Swatch_Blue"));
                await UiHelpers.WaitUntil(() => vm.ColorResult != "（还没选过）");
                Assert.Equal(Colors.SteelBlue, vm.PickedColor);

                window.PressText("选颜色…");
                Dialog(window).Click(Dialog(window).ByName<Button>("CancelButton"));
                await UiHelpers.WaitUntil(() => vm.ColorResult == "结果 null：没选，颜色保持不变");
                Assert.Equal(Colors.SteelBlue, vm.PickedColor);
            }
            finally
            {
                window.Close();
            }
        }

        // Headless has no picker: the open button must stay disabled instead of awaiting a dialog that never comes.
        [AvaloniaFact]
        public void Storage_line_reports_no_picker_and_keeps_the_open_button_disabled()
        {
            var (window, _) = PageHelpers.Open<DialogsHowToPage>();

            Assert.Equal("CanOpen = False，CanSave = False，CanPickFolder = False", window.Text("StorageText"));
            Assert.False(window.ByName<Button>("OpenFileButton").IsEnabled);
            Assert.Equal("（还没选过）", window.Text("FileText"));
        }

        // ---- System ----

        // The smoke walk shows this page over the real %AppData%, where a settings file may already exist. Showing it
        // must save nothing back, even when the file holds a font size below the slider's minimum.
        [AvaloniaFact]
        public void Showing_the_page_over_an_existing_settings_file_saves_nothing()
        {
            var root = Path.Combine(Path.GetTempPath(), "controls-demo-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var settings = Path.Combine(root, "settings");
                Directory.CreateDirectory(settings);
                var file = Path.Combine(settings, "settings.json");
                File.WriteAllText(file, "{ \"Username\": \"小明\", \"FontSize\": 8, \"NotifyEnabled\": true }");
                var written = File.ReadAllText(file);

                var page = new DataPersistenceHowToPage();
                var vm = new DataPersistenceHowToViewModel(settings, Path.Combine(root, "recent"));
                page.DataContext = vm;
                var window = UiHelpers.Show(page);
                try
                {
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Dispatcher.UIThread.RunJobs();

                    Assert.Equal(0, vm.SaveCount);
                    Assert.Equal(written, File.ReadAllText(file));
                    Assert.False(Directory.Exists(Path.Combine(root, "recent")));
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }

        // The page's own view model would read and write the real %AppData%; swap in one on a temp folder before
        // anything can change a setting. Constructing the page only reads, which the smoke walk already relies on.
        [AvaloniaFact]
        public void Typing_a_name_saves_it_and_the_buttons_corrupt_reload_and_reset()
        {
            var root = Path.Combine(Path.GetTempPath(), "controls-demo-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var page = new DataPersistenceHowToPage();
                var vm = new DataPersistenceHowToViewModel(Path.Combine(root, "settings"), Path.Combine(root, "recent"));
                page.DataContext = vm;
                var window = UiHelpers.Show(page);
                Dispatcher.UIThread.RunJobs();
                bool Shows(string text) => window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == text);

                window.Type(window.GetVisualDescendants().OfType<TextBox>().First(), "小明");
                Dispatcher.UIThread.RunJobs();
                Assert.True(Shows("预览：你好，小明"));
                Assert.True(Shows("已自动保存（第 1 次）"));
                Assert.Contains("小明", vm.FileText);

                window.PressText("写坏文件");
                window.PressText("重新读取");
                Assert.True(Shows("设置文件损坏，已回退到默认值"));
                Assert.True(Shows("预览：你好，"));

                window.PressText("恢复默认");
                Assert.True(Shows("已恢复默认值并保存"));

                for (var i = 0; i < 12; i++) window.PressText("假装打开一个文件");
                Assert.True(Shows("共 10 条，上限 10"));
                window.Close();
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }
    }
}
