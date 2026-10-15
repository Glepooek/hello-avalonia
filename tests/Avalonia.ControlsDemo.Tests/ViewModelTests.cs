using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Tests.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // Plain [Fact] where the view model only touches CommunityToolkit types; [AvaloniaFact] where it needs the
    // platform (StreamGeometry.Parse, AssetLoader, Bitmap) or awaits a continuation on the UI thread.
    public class ViewModelTests
    {
        // ---- TextBox / DatePicker validation ----

        [Fact]
        public void Username_validation_reports_required_then_min_length_then_passes()
        {
            var vm = new TextBoxHowToViewModel();
            Assert.Equal("校验通过（输入至少 3 个字符）", vm.ErrorText);

            vm.Username = "ab";
            Assert.Equal("至少 3 个字符", vm.ErrorText);
            vm.Username = "";
            Assert.Equal("用户名必填", vm.ErrorText);
            vm.Username = "abc";
            Assert.False(vm.HasErrors);
            Assert.Equal("校验通过（输入至少 3 个字符）", vm.ErrorText);
        }

        [Fact]
        public void Error_text_change_is_announced_with_the_validation_result()
        {
            var vm = new TextBoxHowToViewModel();
            var raised = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.ErrorText)) raised++; };

            vm.Username = "ab";

            Assert.True(raised >= 1);
        }

        [Fact]
        public void Date_and_time_combine_only_when_both_are_set_and_past_dates_are_rejected()
        {
            var vm = new DatePickerHowToViewModel();
            Assert.Equal("请同时选择日期和时间", vm.Combined);
            Assert.Equal("", vm.ErrorText);

            vm.Date = DateTimeOffset.Now.AddDays(-1);
            Assert.Equal("日期不能早于今天", vm.ErrorText);

            vm.Date = new DateTimeOffset(2099, 1, 2, 0, 0, 0, TimeSpan.Zero);
            Assert.Equal("", vm.ErrorText);
            Assert.Equal("请同时选择日期和时间", vm.Combined);
            vm.Time = new TimeSpan(9, 30, 0);
            Assert.Equal("预约时间：2099-01-02 09:30", vm.Combined);
        }

        [Fact]
        public void Today_is_not_in_the_past()
        {
            var vm = new DatePickerHowToViewModel { Date = DateTimeOffset.Now };

            Assert.False(vm.HasErrors);
        }

        // ---- ListBox / Expander ----

        [Fact]
        public void Done_count_follows_the_check_boxes_on_the_data()
        {
            var vm = new ListBoxHowToViewModel();
            var raised = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.DoneCount)) raised++; };
            Assert.Equal(new[] { "写计划", "跑探针", "目视检查", "提交" }, vm.Todos.Select(t => t.Title));

            vm.Todos[1].IsDone = true;
            vm.Todos[3].IsDone = true;

            Assert.Equal(2, vm.DoneCount);
            Assert.Equal(2, raised);
        }

        [Fact]
        public void Opening_one_expander_section_closes_the_other()
        {
            var vm = new ExpanderHowToViewModel();
            Assert.Equal(-1, vm.OpenSection);

            vm.IsAdvancedOpen = true;
            Assert.Equal(1, vm.OpenSection);
            vm.IsGeneralOpen = true;
            Assert.Equal(0, vm.OpenSection);
            Assert.False(vm.IsAdvancedOpen);

            // Collapsing a section that is not the open one must not close the open one.
            vm.IsAboutOpen = false;
            Assert.Equal(0, vm.OpenSection);
            vm.IsGeneralOpen = false;
            Assert.Equal(-1, vm.OpenSection);
        }

        // ---- TreeView ----

        [Fact]
        public void Expand_all_and_collapse_all_recurse_through_folders()
        {
            var vm = new TreeViewHowToViewModel();
            var project = (FolderNode)vm.Roots[0];
            var src = (FolderNode)project.Children[0];

            vm.ExpandAll(vm.Roots);
            Assert.True(project.IsExpanded);
            Assert.True(src.IsExpanded);
            Assert.True(vm.Roots[1].IsExpanded);
            // Files have nothing to expand.
            Assert.False(src.Children[0].IsExpanded);

            vm.CollapseAll(vm.Roots);
            Assert.False(project.IsExpanded);
            Assert.False(src.IsExpanded);
        }

        [Fact]
        public void Lazy_node_loads_its_children_on_the_first_expand_only()
        {
            var node = new TreeViewHowToViewModel().Lazy[0];
            Assert.Equal("远程目录", node.Name);
            Assert.Equal(new[] { "加载中…" }, node.Children.Select(c => c.Name));

            node.IsExpanded = true;
            Assert.Equal(new[] { "子项 1", "子项 2", "子项 3" }, node.Children.Select(c => c.Name));
            Assert.Equal(1, node.LoadCount);

            node.IsExpanded = false;
            node.IsExpanded = true;
            Assert.Equal(1, node.LoadCount);
            Assert.Equal(3, node.Children.Count);
        }

        // ---- DataGrid ----

        [Fact]
        public void Data_grid_views_group_by_category_and_sort_by_name_descending()
        {
            var vm = new DataGridHowToViewModel();

            Assert.Equal(new object?[] { "Fruit", "Bakery" },
                vm.Grouped.Groups!.Cast<Avalonia.Collections.DataGridCollectionViewGroup>().Select(g => g.Key));
            Assert.Equal(new[] { "Croissant", "Cherry", "Bread", "Banana", "Bagel", "Apple" },
                vm.Sorted.Cast<DemoProduct>().Select(p => p.Name));
            Assert.Equal(new[] { "Apple", "Bread", "Cherry", "Bagel", "Banana", "Croissant" }, vm.Details.Select(p => p.Name));
        }

        // ---- Menu ----

        [Fact]
        public void Recent_menu_shows_a_disabled_placeholder_once_the_list_is_empty()
        {
            var vm = new MenuHowToViewModel();
            var raised = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.RecentMenuItems)) raised++; };
            Assert.Equal(new[] { "报告.docx", "预算.xlsx", "方案.pptx" }, vm.RecentMenuItems.Cast<RecentFile>().Select(f => f.Name));

            vm.OpenRecentCommand.Execute(vm.RecentFiles[1]);
            Assert.Equal("打开 预算.xlsx", vm.LastAction);

            vm.ClearRecentCommand.Execute(null);
            var placeholder = Assert.IsType<EmptyRecent>(Assert.Single(vm.RecentMenuItems));
            Assert.False(vm.OpenRecentCommand.CanExecute(placeholder));
            Assert.True(raised >= 1);
        }

        [Fact]
        public void Delete_keeps_the_last_item_and_save_reports_the_shared_command()
        {
            var vm = new MenuHowToViewModel();
            Assert.Equal("（无）", vm.LastAction);
            Assert.False(vm.DeleteCommand.CanExecute(null));

            vm.DeleteCommand.Execute("苹果");
            vm.DeleteCommand.Execute("面包");
            Assert.Equal(new[] { "樱桃" }, vm.Items);
            Assert.Equal("删除 面包", vm.LastAction);
            Assert.False(vm.DeleteCommand.CanExecute("樱桃"));

            vm.SaveCommand.Execute(null);
            Assert.Equal("保存（Ctrl+S 或菜单都会走到这里）", vm.LastAction);
        }

        // ---- Navigation ----

        [Fact]
        public void Navigation_keeps_back_and_forward_stacks()
        {
            var vm = new NavigationHowToViewModel();
            Assert.Equal("首页", vm.Current.Title);
            Assert.Equal("后退栈 0 项，前进栈 0 项", vm.HistoryText);
            Assert.False(vm.GoBackCommand.CanExecute(null));

            vm.GoSettingsCommand.Execute(null);
            vm.GoSettingsCommand.Execute(null);
            vm.GoAboutCommand.Execute(null);
            Assert.Equal("关于", vm.Current.Title);
            // Going to the page you are on adds nothing.
            Assert.Equal("后退栈 2 项，前进栈 0 项", vm.HistoryText);

            vm.GoBackCommand.Execute(null);
            Assert.Equal("设置", vm.Current.Title);
            Assert.Equal("后退栈 1 项，前进栈 1 项", vm.HistoryText);
            Assert.True(vm.GoForwardCommand.CanExecute(null));

            // A new navigation drops the forward history.
            vm.GoHomeCommand.Execute(null);
            Assert.Equal("后退栈 2 项，前进栈 0 项", vm.HistoryText);
            Assert.False(vm.GoForwardCommand.CanExecute(null));
        }

        [Fact]
        public void Navigation_pages_carry_their_own_state()
        {
            Assert.StartsWith("欢迎回来。", new HomeNavViewModel().Welcome);
            var settings = new SettingsNavViewModel();
            Assert.Equal(50, settings.Volume);
            Assert.False(settings.NotifyEnabled);
            Assert.Equal("ControlsDemo · Avalonia 12.1.2", new AboutNavViewModel().Version);
        }

        // ---- TabControl (its icons are StreamGeometry, which needs the platform) ----

        [AvaloniaFact]
        public void Closing_tabs_never_removes_the_last_one_and_moves_the_selection_right()
        {
            var vm = new TabControlHowToViewModel();
            Assert.Equal(new[] { "说明", "日志", "设置" }, vm.Tabs.Select(t => t.Title));
            Assert.Same(vm.Tabs[0], vm.SelectedTab);

            vm.AddTabCommand.Execute(null);
            Assert.Equal("文档 4", vm.SelectedTab?.Title);

            vm.SelectedTab = vm.Tabs[1];
            vm.CloseTabCommand.Execute(vm.Tabs[1]);
            Assert.Equal("设置", vm.SelectedTab?.Title);
            // Closing the selected last tab selects the new last one.
            vm.SelectedTab = vm.Tabs[2];
            vm.CloseTabCommand.Execute(vm.Tabs[2]);
            Assert.Equal("设置", vm.SelectedTab?.Title);
            // Closing a tab that is not selected leaves the selection alone.
            vm.CloseTabCommand.Execute(vm.Tabs[0]);
            Assert.Equal(new[] { "设置" }, vm.Tabs.Select(t => t.Title));

            vm.CloseTabCommand.Execute(vm.Tabs[0]);
            Assert.Single(vm.Tabs);
        }

        [AvaloniaFact]
        public void Wizard_steps_build_their_content_once_and_stop_at_both_ends()
        {
            var vm = new TabControlHowToViewModel();
            Assert.Equal(0, vm.CreatedCount);
            Assert.False(vm.PreviousStepCommand.CanExecute(null));

            Assert.StartsWith("「资料」", vm.WizardSteps[1].Body);
            _ = vm.WizardSteps[1].Body;
            Assert.Equal(1, vm.CreatedCount);

            vm.NextStepCommand.Execute(null);
            vm.NextStepCommand.Execute(null);
            Assert.Equal(2, vm.WizardIndex);
            Assert.False(vm.NextStepCommand.CanExecute(null));
            Assert.True(vm.PreviousStepCommand.CanExecute(null));
        }

        // ---- Notifications (timed removal awaits on the UI thread) ----

        [AvaloniaFact]
        public async Task Timed_toasts_remove_themselves_and_sticky_ones_stay()
        {
            var vm = new NotificationsHowToViewModel();

            vm.Show("a", "info", 50);
            vm.Show("b", "error", 0);
            Assert.Equal(2, vm.Toasts.Count);
            Assert.True(vm.Toasts[0].IsInfo);
            Assert.True(vm.Toasts[1].IsError);

            await UiHelpers.WaitUntil(() => vm.Toasts.Count == 1);
            Assert.Equal("b", vm.Toasts[0].Message);

            vm.DismissCommand.Execute(vm.Toasts[0]);
            Assert.Empty(vm.Toasts);
        }

        [AvaloniaFact]
        public async Task Status_message_returns_to_ready_and_the_banner_toggles()
        {
            var vm = new NotificationsHowToViewModel();
            Assert.Equal("就绪", vm.StatusMessage);

            _ = vm.ShowStatusCommand.ExecuteAsync(null);
            Assert.StartsWith("已保存（", vm.StatusMessage);
            await UiHelpers.WaitUntil(() => vm.StatusMessage == "就绪");

            vm.ShowBannerNowCommand.Execute(null);
            Assert.True(vm.ShowBanner);
            vm.DismissBannerCommand.Execute(null);
            Assert.False(vm.ShowBanner);
        }

        // ---- Dialogs (through a fake service: no window opens) ----

        private sealed class FakeDialogs : IDialogService
        {
            public bool? Confirm { get; set; }
            public string? Answer { get; set; }
            public Color? Picked { get; set; }
            public (string Title, string Prompt, bool Required)? LastPrompt { get; private set; }
            public Color? LastInitial { get; private set; }

            public Task<bool?> ConfirmAsync(string title, string message) => Task.FromResult(Confirm);

            public Task<string?> PromptAsync(string title, string prompt, bool required)
            {
                LastPrompt = (title, prompt, required);
                return Task.FromResult(Answer);
            }

            public Task<Color?> PickColorAsync(Color initial)
            {
                LastInitial = initial;
                return Task.FromResult(Picked);
            }
        }

        [Fact]
        public async Task Confirm_reports_all_three_answers()
        {
            var dialogs = new FakeDialogs();
            var vm = new DialogsHowToViewModel(dialogs);
            Assert.Equal("（还没问过）", vm.ConfirmResult);

            dialogs.Confirm = true;
            await vm.AskConfirmCommand.ExecuteAsync(null);
            Assert.Equal("结果 true：用户点了「确定」", vm.ConfirmResult);
            dialogs.Confirm = false;
            await vm.AskConfirmCommand.ExecuteAsync(null);
            Assert.Equal("结果 false：用户点了「取消」", vm.ConfirmResult);
            dialogs.Confirm = null;
            await vm.AskConfirmCommand.ExecuteAsync(null);
            Assert.Equal("结果 null：用户直接关掉了窗口，既没确定也没取消", vm.ConfirmResult);
        }

        [Fact]
        public async Task Prompts_pass_the_required_flag_and_report_the_answer()
        {
            var dialogs = new FakeDialogs { Answer = "小明" };
            var vm = new DialogsHowToViewModel(dialogs);

            await vm.AskNameCommand.ExecuteAsync(null);
            Assert.Equal("名字：小明", vm.NameResult);
            Assert.False(dialogs.LastPrompt!.Value.Required);

            await vm.AskRequiredNameCommand.ExecuteAsync(null);
            Assert.Equal("名字（必答）：小明", vm.NameResult);
            Assert.True(dialogs.LastPrompt!.Value.Required);

            dialogs.Answer = null;
            await vm.AskNameCommand.ExecuteAsync(null);
            Assert.Equal("没有名字（取消或直接关闭）", vm.NameResult);
            await vm.AskRequiredNameCommand.ExecuteAsync(null);
            Assert.Equal("没有名字", vm.NameResult);
        }

        [Fact]
        public async Task Picking_a_color_starts_from_the_current_one_and_cancel_keeps_it()
        {
            var dialogs = new FakeDialogs { Picked = Colors.SteelBlue };
            var vm = new DialogsHowToViewModel(dialogs);
            Assert.Equal(Colors.Gray, vm.PickedColor);
            Assert.Equal("（还没选过）", vm.ColorResult);

            await vm.PickColorCommand.ExecuteAsync(null);
            Assert.Equal(Colors.Gray, dialogs.LastInitial);
            Assert.Equal(Colors.SteelBlue, vm.PickedColor);
            Assert.Equal(Colors.SteelBlue, ((ISolidColorBrush)vm.PickedBrush).Color);
            Assert.Equal($"结果 Color：{Colors.SteelBlue}", vm.ColorResult);

            dialogs.Picked = null;
            await vm.PickColorCommand.ExecuteAsync(null);
            Assert.Equal(Colors.SteelBlue, dialogs.LastInitial);
            Assert.Equal(Colors.SteelBlue, vm.PickedColor);
            Assert.Equal("结果 null：没选，颜色保持不变", vm.ColorResult);
        }

        // ---- Image (AssetLoader and Bitmap need the platform) ----

        [AvaloniaFact]
        public void Image_loads_the_logo_decoded_to_64_pixels_and_clears_it()
        {
            var vm = new ImageHowToViewModel();
            Assert.Null(vm.Photo);
            Assert.Equal("（未加载）", vm.Source);

            vm.LoadCommand.Execute(null);
            Assert.Equal(64, vm.Photo!.PixelSize.Width);
            Assert.Equal("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico（DecodeToWidth 64）", vm.Source);

            vm.ClearCommand.Execute(null);
            Assert.Null(vm.Photo);
            Assert.Equal("（未加载）", vm.Source);
        }
    }
}
