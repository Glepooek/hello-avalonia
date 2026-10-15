using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the How-to pages of Data display, Feedback, Input and Layout.
    public class HowToDataInputTests
    {
        // ---- Data display ----

        [AvaloniaFact]
        public void Tags_can_be_added_and_cleared_and_the_empty_hint_follows()
        {
            var (window, _) = PageHelpers.Open<ItemsControlHowToPage>();
            var tags = window.ByName<ItemsControl>("Tags");
            var empty = window.ByName<TextBlock>("EmptyText");
            Assert.Equal(new[] { "Avalonia", "XAML" }, tags.Items.Cast<string>());
            Assert.False(empty.IsVisible);

            window.Press("AddTagButton");
            Assert.Equal(new[] { "Avalonia", "XAML", "标签 1" }, tags.Items.Cast<string>());

            window.Press("ClearTagsButton");
            Assert.Equal(0, tags.ItemCount);
            Assert.True(empty.IsVisible);

            // The counter keeps going after a clear.
            window.Press("AddTagButton");
            Assert.Equal(new[] { "标签 2" }, tags.Items.Cast<string>());
            Assert.False(empty.IsVisible);
        }

        [AvaloniaFact]
        public void Striped_rows_mark_every_odd_container_and_the_big_list_is_virtualized()
        {
            var (window, _) = PageHelpers.Open<ItemsControlHowToPage>();
            var striped = window.ByName<ItemsControl>("Striped");
            var big = window.ByName<ItemsControl>("Big");

            Assert.Equal(new[] { false, true, false, true },
                Enumerable.Range(0, 4).Select(i => striped.ContainerFromIndex(i)!.Classes.Contains("odd")));
            Assert.Equal(1000, big.ItemCount);
            Assert.NotNull(big.ContainerFromIndex(0));
            Assert.Null(big.ContainerFromIndex(999));
            Assert.StartsWith("共 1000 项，已实现 ", window.Text("RealizedText"));
        }

        [AvaloniaFact]
        public void Ticking_a_row_check_box_changes_the_data_not_the_selection()
        {
            var (window, page) = PageHelpers.Open<ListBoxHowToPage>();
            var vm = (ListBoxHowToViewModel)page.DataContext!;
            var todos = window.ByName<ListBox>("Todos");
            Assert.Equal("已完成 0 项", window.Text("DoneText"));

            window.Click(todos.GetVisualDescendants().OfType<CheckBox>().ElementAt(1));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("已完成 1 项", window.Text("DoneText"));
            Assert.True(vm.Todos[1].IsDone);
            Assert.Equal(-1, todos.SelectedIndex);
        }

        [AvaloniaFact]
        public void Data_grids_show_groups_descending_names_and_all_columns()
        {
            var (window, _) = PageHelpers.Open<DataGridHowToPage>();
            string?[] Rows(string name)
            {
                var grid = window.ByName<DataGrid>(name);
                grid.BringIntoView();
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                return grid.GetVisualDescendants().OfType<DataGridRow>().Select(r => (r.DataContext as DemoProduct)?.Name).ToArray();
            }

            Assert.Equal(2, window.ByName<DataGrid>("GroupGrid").GetVisualDescendants().OfType<DataGridRowGroupHeader>().Count());
            // Only the rows that fit are realized, so compare the start of the sorted list.
            Assert.Equal(new[] { "Croissant", "Cherry", "Bread" }, Rows("SortGrid").Take(3));
            Assert.Equal(new[] { "名称", "分类", "价格", "有货" }, window.ByName<DataGrid>("DetailGrid").Columns.Select(c => c.Header));
        }

        [AvaloniaFact]
        public void Tree_buttons_expand_and_collapse_and_selection_describes_the_node()
        {
            var (window, page) = PageHelpers.Open<TreeViewHowToPage>();
            var vm = (TreeViewHowToViewModel)page.DataContext!;
            var mixed = window.ByName<TreeView>("Mixed");
            int Visible() => mixed.GetVisualDescendants().OfType<TreeViewItem>().Count(i => i.IsEffectivelyVisible);
            Assert.Equal(2, Visible());

            window.Press("ExpandAllButton");
            window.UpdateLayout();
            Assert.Equal(7, Visible());

            var src = (FolderNode)((FolderNode)vm.Roots[0]).Children[0];
            mixed.SelectedItem = src.Children[1];
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中文件：Program.cs（2 KB）", window.Text("MixedText"));
            mixed.SelectedItem = vm.Roots[0];
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中文件夹：项目（2 项）", window.Text("MixedText"));

            window.Press("CollapseAllButton");
            window.UpdateLayout();
            Assert.Equal(2, Visible());
        }

        [AvaloniaFact]
        public void Expanding_the_lazy_node_loads_once_and_the_page_counts_it()
        {
            var (window, page) = PageHelpers.Open<TreeViewHowToPage>();
            var node = ((TreeViewHowToViewModel)page.DataContext!).Lazy[0];
            Assert.Equal("远程目录已加载 0 次", window.Text("LazyText"));

            node.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            node.IsExpanded = false;
            node.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("远程目录已加载 1 次", window.Text("LazyText"));
        }

        // ---- Feedback ----

        // Info and success toasts last 3 s; WaitUntil's 5 s default covers that with room to spare.
        [AvaloniaFact]
        public async Task Timed_toasts_disappear_and_the_sticky_one_needs_its_close_button()
        {
            var (window, page) = PageHelpers.Open<NotificationsHowToPage>();
            var vm = (NotificationsHowToViewModel)page.DataContext!;

            window.Press("InfoButton");
            window.Press("SuccessButton");
            window.Press("StickyButton");
            Assert.Equal(new[] { "第 1 条信息", "第 2 条：已完成", "第 3 条：出错了，需要手动关闭" }, vm.Toasts.Select(t => t.Message));

            await UiHelpers.WaitUntil(() => vm.Toasts.Count == 1);
            Assert.True(vm.Toasts[0].IsError);

            window.Click(window.ByName<ItemsControl>("ToastList").GetVisualDescendants().OfType<Button>().Single());
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(vm.Toasts);
        }

        [AvaloniaFact]
        public async Task Status_line_says_saved_then_ready_and_the_banner_opens()
        {
            var (window, _) = PageHelpers.Open<NotificationsHowToPage>();
            Assert.False(window.ByName<Border>("Banner").IsVisible);

            window.Press("StatusButton");
            Assert.StartsWith("已保存（", window.Text("StatusText"));
            await UiHelpers.WaitUntil(() => window.Text("StatusText") == "就绪");

            window.Press("BannerButton");
            Assert.True(window.ByName<Border>("Banner").IsVisible);
        }

        // ---- Input ----

        [AvaloniaFact]
        public void Date_and_time_pickers_feed_the_combined_label_and_reject_past_dates()
        {
            var (window, _) = PageHelpers.Open<DatePickerHowToPage>();
            var date = window.ByName<DatePicker>("DateBox");
            Assert.Equal("请同时选择日期和时间", window.Text("CombinedLabel"));

            date.SelectedDate = DateTimeOffset.Now.AddDays(-1);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("日期不能早于今天", window.Text("DateErrorLabel"));

            date.SelectedDate = new DateTimeOffset(2099, 1, 2, 0, 0, 0, TimeSpan.Zero);
            window.ByName<TimePicker>("TimeBox").SelectedTime = new TimeSpan(9, 30, 0);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("预约时间：2099-01-02 09:30", window.Text("CombinedLabel"));
            Assert.Equal("", window.Text("DateErrorLabel"));
        }

        [AvaloniaFact]
        public void Combo_boxes_report_selected_value_and_search_picks_an_employee()
        {
            var (window, _) = PageHelpers.Open<ComboBoxHowToPage>();
            var priority = window.ByName<ComboBox>("PriorityBox");
            var search = window.ByName<AutoCompleteBox>("SearchEmployee");
            Assert.Equal(5, window.ByName<ComboBox>("EmployeeBox").ItemCount);
            Assert.Equal("SelectedValue = (空)", window.Text("PriorityResult"));

            priority.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedValue = High", window.Text("PriorityResult"));

            window.Type(search.GetVisualDescendants().OfType<TextBox>().First(), "王");
            Dispatcher.UIThread.RunJobs();
            Assert.True(search.IsDropDownOpen);
            Assert.Equal("未选择", window.Text("SearchResult"));

            search.SelectedItem = search.ItemsSource!.Cast<Employee>().Single(e => e.Name == "王芳");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("已选：王芳，市场部", window.Text("SearchResult"));
            search.SelectedItem = null;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("未选择", window.Text("SearchResult"));
        }

        [AvaloniaFact]
        public void Colour_sliders_drive_the_hex_text_and_the_preview()
        {
            var (window, _) = PageHelpers.Open<SliderHowToPage>();
            Assert.Equal("#4080C0", window.Text("HexText"));

            window.ByName<Slider>("RedSlider").Value = 255;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("#FF80C0", window.Text("HexText"));
            Assert.Equal(Color.Parse("#FF80C0"), ((ISolidColorBrush)window.ByName<Border>("Preview").Background!).Color);
        }

        // A click at 90% of the track jumps there; the disabled and hit-test-invisible sliders must not move.
        [AvaloniaFact]
        public void Clicking_the_track_moves_only_the_slider_that_accepts_input()
        {
            var (window, _) = PageHelpers.Open<SliderHowToPage>();
            void ClickAt90(string name)
            {
                var slider = window.ByName<Slider>(name);
                slider.BringIntoView();
                Dispatcher.UIThread.RunJobs();
                var point = slider.TranslatePoint(new Point(slider.Bounds.Width * 0.9, slider.Bounds.Height / 2), window)!.Value;
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
            }

            ClickAt90("DisabledSlider");
            ClickAt90("NoHitSlider");
            ClickAt90("IntSlider");

            Assert.Equal(5, window.ByName<Slider>("DisabledSlider").Value);
            Assert.Equal(5, window.ByName<Slider>("NoHitSlider").Value);
            Assert.Equal(9, window.ByName<Slider>("IntSlider").Value);
            Assert.Equal("Value = 9", window.Text("IntResult"));
        }

        [AvaloniaFact]
        public void Username_box_shows_each_validation_message_as_you_type()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("UsernameBox");

            window.Type(box, "ab");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("至少 3 个字符", window.Text("ErrorLabel"));
            window.Type(box, "c");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("校验通过（输入至少 3 个字符）", window.Text("ErrorLabel"));
            box.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("用户名必填", window.Text("ErrorLabel"));
        }

        // The filter rewrites the text after the fact, so a mixed paste keeps its digits and an assignment is
        // filtered too; the hint only shows when something was dropped.
        [AvaloniaFact]
        public void Digits_box_drops_everything_but_digits()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("DigitsBox");

            window.Type(box, "1");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("1", ""), (box.Text, window.Text("DigitsResult")));
            window.Type(box, "a");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("1", "已滤掉非数字"), (box.Text, window.Text("DigitsResult")));
            window.Type(box, "2");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("12", ""), (box.Text, window.Text("DigitsResult")));
            window.Type(box, "3b4");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("1234", box.Text);
            box.Text = "x9";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("9", "已滤掉非数字"), (box.Text, window.Text("DigitsResult")));
        }

        [AvaloniaFact]
        public void Search_box_filters_the_fruit_list_and_the_clear_button_resets_it()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("SearchBox");
            var list = window.ByName<ListBox>("SearchList");
            Assert.Equal(7, list.ItemCount);

            window.Type(box, "西");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "西瓜" }, list.Items.Cast<string>());
            box.Text = "子";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "橙子" }, list.Items.Cast<string>());

            window.Press("ClearButton");
            Assert.Equal("", box.Text);
            Assert.Equal(7, list.ItemCount);
        }

        [AvaloniaFact]
        public void Undo_and_redo_buttons_drive_the_text_box_history()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("UndoBox");
            box.CaretIndex = box.Text!.Length;

            window.Type(box, "！");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("改一改，再撤销！", box.Text);

            window.Press("UndoButton");
            Assert.Equal("改一改，再撤销", box.Text);
            window.Press("RedoButton");
            Assert.Equal("改一改，再撤销！", box.Text);
        }

        // ---- Layout ----

        [AvaloniaFact]
        public void Accordion_keeps_at_most_one_section_open()
        {
            var (window, _) = PageHelpers.Open<ExpanderHowToPage>();
            Expander Section(int n) => window.ByName<Expander>("Section" + n);
            void ClickHeader(int n)
            {
                window.Click(Section(n).GetVisualDescendants().OfType<ToggleButton>().First());
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("OpenSection = -1（-1 表示全收起）", window.Text("OpenLabel"));

            ClickHeader(2);
            Assert.Equal("OpenSection = 1（-1 表示全收起）", window.Text("OpenLabel"));
            ClickHeader(1);
            Assert.Equal("OpenSection = 0（-1 表示全收起）", window.Text("OpenLabel"));
            Assert.False(Section(2).IsExpanded);
            ClickHeader(1);
            Assert.Equal("OpenSection = -1（-1 表示全收起）", window.Text("OpenLabel"));
        }

        [AvaloniaFact]
        public void Direction_box_and_check_box_change_the_demo_expander()
        {
            var (window, _) = PageHelpers.Open<ExpanderHowToPage>();
            var box = window.ByName<ComboBox>("DirectionBox");
            var demo = window.ByName<Expander>("DirectionDemo");
            Assert.Equal(new object[] { ExpandDirection.Down, ExpandDirection.Up, ExpandDirection.Left, ExpandDirection.Right },
                box.Items.Cast<object>());

            box.SelectedItem = ExpandDirection.Up;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ExpandDirection.Up, demo.ExpandDirection);

            window.Press("DisabledCheck");
            Assert.False(demo.IsEnabled);
        }

        // Each ScrollToEnd reaches the bottom, which loads ten more, up to sixty.
        [AvaloniaFact]
        public void Scrolling_to_the_end_loads_more_items_until_sixty()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerHowToPage>();
            Assert.Equal("已加载 20 项", window.Text("FeedCount"));

            for (var i = 0; i < 5; i++)
            {
                window.Press("LoadButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal("已加载 60 项", window.Text("FeedCount"));
            Assert.Equal(60, window.ByName<StackPanel>("FeedList").Children.Count);
        }

        [AvaloniaFact]
        public void Jump_button_scrolls_the_target_into_view_and_chaining_can_be_turned_off()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerHowToPage>();
            var jump = window.ByName<ScrollViewer>("JumpScroller");
            Assert.Equal(0, jump.Offset.Y);

            window.Press("JumpButton");
            window.UpdateLayout();
            Assert.True(jump.Offset.Y > 0);

            Assert.True(window.ByName<ScrollViewer>("ChainInner").IsScrollChainingEnabled);
            window.Press("ChainCheck");
            Assert.False(window.ByName<ScrollViewer>("ChainInner").IsScrollChainingEnabled);
        }

        [AvaloniaFact]
        public void Grid_lines_toggle_and_shared_size_columns_line_up()
        {
            var (window, _) = PageHelpers.Open<GridHowToPage>();
            var formA = window.ByName<Grid>("FormA");
            var formB = window.ByName<Grid>("FormB");

            Assert.Equal("Label", formA.ColumnDefinitions[0].SharedSizeGroup);
            Assert.Equal(formA.ColumnDefinitions[0].ActualWidth, formB.ColumnDefinitions[0].ActualWidth);

            window.Press("LinesCheck");
            Assert.True(window.ByName<Grid>("SpanGrid").ShowGridLines);
        }
    }
}
