using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Data display and Feedback.
    public class ControlDataFeedbackTests
    {
        private static string[] Texts(Visual root) => root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();

        private static NotificationCard[] Cards(Window window) => window.GetVisualDescendants().OfType<NotificationCard>().ToArray();

        // ---- Data display ----

        [AvaloniaFact]
        public void Carousel_buttons_step_through_the_slides_and_stop_at_both_ends()
        {
            var (window, _) = PageHelpers.Open<CarouselPage>();
            var slides = window.ByName<Carousel>("Slides");
            Assert.Equal("第 1 / 3 张", window.Text("IndexText"));

            window.Press("PreviousButton");
            Assert.Equal(0, slides.SelectedIndex);
            Assert.Equal("第 1 / 3 张", window.Text("IndexText"));

            window.Press("NextButton");
            Assert.Equal("第 2 / 3 张", window.Text("IndexText"));
            window.Press("NextButton");
            window.Press("NextButton");
            Assert.Equal(2, slides.SelectedIndex);
            Assert.Equal("第 3 / 3 张", window.Text("IndexText"));

            window.Press("PreviousButton");
            Assert.Equal("第 2 / 3 张", window.Text("IndexText"));
        }

        [AvaloniaFact]
        public void Carousel_transition_box_and_swipe_check_set_the_carousel()
        {
            var (window, _) = PageHelpers.Open<CarouselPage>();
            var slides = window.ByName<Carousel>("Slides");
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.Equal("无", box.SelectedItem);
            Assert.Null(slides.PageTransition);
            Assert.False(slides.IsSwipeEnabled);

            box.SelectedIndex = 1;
            Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.IsType<PageSlide>(slides.PageTransition).Duration);
            box.SelectedIndex = 2;
            Assert.IsType<CrossFade>(slides.PageTransition);
            box.SelectedIndex = 0;
            Assert.Null(slides.PageTransition);

            window.Click(window.ByName<CheckBox>("SwipeCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(slides.IsSwipeEnabled);
        }

        [AvaloniaFact]
        public void Items_control_buttons_add_and_remove_items_and_the_count_follows()
        {
            var (window, _) = PageHelpers.Open<ItemsControlPage>();
            var items = window.ByName<ItemsControl>("Items");
            Assert.Equal("ItemCount = 3", window.Text("CountText"));

            window.Press("AddButton");
            window.Press("AddButton");
            Assert.Equal(new[] { "Apple", "Bread", "Cherry", "新项 1", "新项 2" }, items.Items.Cast<string>());
            Assert.Equal("ItemCount = 5", window.Text("CountText"));

            for (int i = 0; i < 6; i++)
                window.Press("RemoveButton");
            Assert.Equal(0, items.ItemCount);
            Assert.Equal("ItemCount = 0", window.Text("CountText"));

            // The counter keeps going after the list was emptied.
            window.Press("AddButton");
            Assert.Equal(new[] { "新项 3" }, items.Items.Cast<string>());
        }

        [AvaloniaFact]
        public void Items_panel_box_swaps_the_panel_the_items_are_laid_out_in()
        {
            var (window, _) = PageHelpers.Open<ItemsControlPage>();
            var items = window.ByName<ItemsControl>("Items");
            Assert.IsType<StackPanel>(items.ItemsPanelRoot);

            window.ByName<ComboBox>("PanelBox").SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.IsType<WrapPanel>(items.ItemsPanelRoot);
            // In a WrapPanel the three short items share the first row.
            Assert.Single(Enumerable.Range(0, 3).Select(i => items.ContainerFromIndex(i)!.Bounds.Y).Distinct());
        }

        [AvaloniaFact]
        public void List_box_select_all_needs_multiple_mode_and_single_mode_keeps_one()
        {
            var (window, _) = PageHelpers.Open<ListBoxPage>();
            var list = window.ByName<ListBox>("List");
            var mode = window.ByName<ComboBox>("ModeBox");
            Assert.Equal(SelectionMode.Single, mode.SelectedItem);
            Assert.Equal("已选 0 项", window.Text("SelectionText"));

            window.Click(list.ContainerFromIndex(1)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("已选 1 项", window.Text("SelectionText"));
            Assert.Equal("第 2 项", list.SelectedItem);

            mode.SelectedItem = SelectionMode.Multiple;
            window.Press("SelectAllButton");
            Assert.Equal("已选 20 项", window.Text("SelectionText"));

            mode.SelectedItem = SelectionMode.Single;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("已选 1 项", window.Text("SelectionText"));

            window.Press("ClearButton");
            Assert.Equal("已选 0 项", window.Text("SelectionText"));
        }

        [AvaloniaFact]
        public void List_box_jump_button_brings_the_last_item_into_view()
        {
            var (window, _) = PageHelpers.Open<ListBoxPage>();
            var list = window.ByName<ListBox>("List");
            Assert.Null(list.ContainerFromIndex(19));

            window.Press("JumpButton");
            window.UpdateLayout();

            Assert.NotNull(list.ContainerFromIndex(19));
            Assert.Null(list.ContainerFromIndex(0));
        }

        [AvaloniaFact]
        public void Content_control_draws_a_string_a_control_and_a_templated_object_differently()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ContentControlPage>();
            var host = window.ByName<ContentControl>("Host");
            Assert.Equal("Content 的类型：（未设置）", window.Text("TypeText"));
            Assert.Empty(Texts(host));

            window.PressText("Content = 字符串");
            Assert.Equal("Content 的类型：String", window.Text("TypeText"));
            Assert.Equal(new[] { "你好，ContentControl" }, Texts(host));

            window.PressText("Content = 控件");
            Assert.Equal("Content 的类型：Border", window.Text("TypeText"));
            Assert.Same(host.Content, host.GetVisualDescendants().OfType<Border>().First());
            Assert.Equal(new[] { "我是一个 Border" }, Texts(host));

            window.PressText("Content = 数据对象");
            Assert.Equal("Content 的类型：DemoProduct", window.Text("TypeText"));
            Assert.Equal(new[] { "Apple", "¤1.20" }, Texts(host));
        }

        [AvaloniaFact]
        public void Data_grid_selection_shows_the_row_and_the_option_boxes_set_the_grid()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<DataGridPage>();
            var grid = window.ByName<DataGrid>("Grid");
            Assert.Equal("选中：（无）", window.Text("SelectedText"));

            var row = grid.GetVisualDescendants().OfType<DataGridRow>().First(r => ((DemoProduct)r.DataContext!).Name == "Bread");
            window.Click(row);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中：Bread（Bakery，3.5）", window.Text("SelectedText"));

            window.ByName<ComboBox>("LinesBox").SelectedItem = DataGridGridLinesVisibility.All;
            window.ByName<ComboBox>("SelectionBox").SelectedItem = DataGridSelectionMode.Single;
            Assert.Equal(DataGridGridLinesVisibility.All, grid.GridLinesVisibility);
            Assert.Equal(DataGridSelectionMode.Single, grid.SelectionMode);

            Assert.False(grid.IsReadOnly);
            window.Click(window.ByName<CheckBox>("ReadOnlyCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(grid.IsReadOnly);
            Assert.False(grid.CanUserReorderColumns);
            window.Click(window.ByName<CheckBox>("ReorderCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(grid.CanUserReorderColumns);
        }

        [AvaloniaFact]
        public void Clicking_a_data_grid_header_sorts_the_rows()
        {
            var (window, _) = PageHelpers.Open<DataGridPage>();
            var grid = window.ByName<DataGrid>("Grid");
            string?[] Names()
            {
                window.UpdateLayout();
                return grid.GetVisualDescendants().OfType<DataGridRow>().OrderBy(r => r.Index)
                    .Select(r => (r.DataContext as DemoProduct)?.Name).ToArray();
            }
            Assert.Equal("Apple", Names().First());

            window.Click(grid.GetVisualDescendants().OfType<DataGridColumnHeader>().First(h => "价格".Equals(h.Content)));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new[] { "Banana", "Apple", "Bagel" }, Names().Take(3));
        }

        [AvaloniaFact]
        public void Table_view_selection_shows_the_row_and_the_options_set_the_table()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<TableViewPage>();
            var table = window.ByName<TableView>("Table");
            var resize = window.ByName<CheckBox>("ResizeCheck");
            Assert.Equal("选中：（无）", window.Text("SelectedText"));
            Assert.Equal(table.CanUserResizeColumns, resize.IsChecked);

            window.Click(table.ContainerFromIndex(2)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中：Cherry（Fruit，5.0）", window.Text("SelectedText"));

            window.ByName<ComboBox>("ModeBox").SelectedItem = SelectionMode.Multiple;
            Assert.Equal(SelectionMode.Multiple, table.SelectionMode);

            bool before = table.CanUserResizeColumns;
            window.Click(resize);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(!before, table.CanUserResizeColumns);
        }

        [AvaloniaFact]
        public void Tree_buttons_expand_and_collapse_every_level_and_selection_names_the_node()
        {
            var (window, _) = PageHelpers.Open<TreeViewPage>();
            var tree = window.ByName<TreeView>("Tree");
            string[] Visible()
            {
                window.UpdateLayout();
                return tree.GetVisualDescendants().OfType<TreeViewItem>().Where(i => i.IsEffectivelyVisible)
                    .Select(i => ((DemoTreeNode)i.DataContext!).Label).ToArray();
            }
            Assert.Equal(new[] { "文档", "图片" }, Visible());
            Assert.Equal("选中：（无）", window.Text("SelectedText"));

            window.Press("ExpandAllButton");
            Assert.Equal(new[] { "文档", "报告", "2025 年报", "2026 季报", "合同", "图片", "旅行" }, Visible());

            window.Click(tree.GetVisualDescendants().OfType<TreeViewItem>().First(i => ((DemoTreeNode)i.DataContext!).Label == "2026 季报"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中：2026 季报", window.Text("SelectedText"));

            window.Press("CollapseAllButton");
            Assert.Equal(new[] { "文档", "图片" }, Visible());
        }

        [AvaloniaFact]
        public void Tree_mode_box_sets_the_selection_mode()
        {
            var (window, _) = PageHelpers.Open<TreeViewPage>();
            var box = window.ByName<ComboBox>("ModeBox");
            Assert.Equal(SelectionMode.Single, box.SelectedItem);

            box.SelectedItem = SelectionMode.Multiple;
            Assert.Equal(SelectionMode.Multiple, window.ByName<TreeView>("Tree").SelectionMode);
        }

        [AvaloniaFact]
        public void Alt_n_on_the_label_moves_focus_to_its_target_box()
        {
            var (window, _) = PageHelpers.Open<LabelPage>();
            var box = window.ByName<TextBox>("NameBox");
            Assert.Equal("输入框：未聚焦", window.Text("FocusText"));

            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            window.KeyPress(Key.N, RawInputModifiers.Alt, PhysicalKey.N, "n");
            window.KeyRelease(Key.N, RawInputModifiers.Alt, PhysicalKey.N, "n");
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(box.IsFocused);
            Assert.Equal("输入框：已聚焦", window.Text("FocusText"));
        }

        [AvaloniaFact]
        public void Selection_buttons_select_and_clear_text_and_the_readout_follows()
        {
            var (window, _) = PageHelpers.Open<SelectableTextBlockPage>();
            var selectable = window.ByName<SelectableTextBlock>("Selectable");
            Assert.Equal("SelectedText：（空）", window.Text("SelectionText"));

            window.Press("FirstWordButton");
            Assert.Equal("SelectedText：Hello", window.Text("SelectionText"));

            window.Press("SelectAllButton");
            Assert.Equal("SelectedText：Hello Avalonia：用鼠标拖选这段文字，按 Ctrl+C 复制。", window.Text("SelectionText"));
            Assert.Equal(selectable.Text, selectable.SelectedText);

            window.Press("ClearButton");
            Assert.Equal("SelectedText：（空）", window.Text("SelectionText"));
            Assert.Equal("", selectable.SelectedText);
        }

        [AvaloniaFact]
        public void Dragging_across_the_text_selects_it_and_the_release_updates_the_readout()
        {
            var (window, _) = PageHelpers.Open<SelectableTextBlockPage>();
            var selectable = window.ByName<SelectableTextBlock>("Selectable");
            var start = selectable.PointIn(new Point(1, 8), window);
            var end = selectable.PointIn(new Point(60, 8), window);

            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            window.MouseUp(end, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.False(string.IsNullOrEmpty(selectable.SelectedText));
            Assert.StartsWith("H", selectable.SelectedText);
            Assert.Equal($"SelectedText：{selectable.SelectedText}", window.Text("SelectionText"));
        }

        [AvaloniaFact]
        public void Text_block_sliders_and_boxes_set_the_sample()
        {
            var (window, _) = PageHelpers.Open<TextBlockPage>();
            var sample = window.ByName<TextBlock>("Sample");
            Assert.Equal(TextWrapping.NoWrap, window.ByName<ComboBox>("WrapBox").SelectedItem);
            Assert.Equal(TextAlignment.Start, window.ByName<ComboBox>("AlignBox").SelectedItem);
            double oneLine = sample.Bounds.Height;

            window.ByName<Slider>("SizeSlider").Value = 20;
            Assert.Equal(20, sample.FontSize);

            window.ByName<ComboBox>("WrapBox").SelectedItem = TextWrapping.Wrap;
            window.UpdateLayout();
            Assert.Equal(TextWrapping.Wrap, sample.TextWrapping);
            double wrapped = sample.Bounds.Height;
            Assert.True(wrapped > oneLine * 2, $"wrapped {wrapped}, one line {oneLine}");

            window.ByName<Slider>("MaxLinesSlider").Value = 1;
            window.UpdateLayout();
            Assert.Equal(1, sample.MaxLines);
            Assert.True(sample.Bounds.Height < wrapped, $"MaxLines 1 {sample.Bounds.Height}, wrapped {wrapped}");

            window.ByName<ComboBox>("AlignBox").SelectedItem = TextAlignment.Center;
            Assert.Equal(TextAlignment.Center, sample.TextAlignment);
        }

        [AvaloniaFact]
        public void Trimming_controls_set_width_and_mode_while_the_text_stays_whole()
        {
            var (window, _) = PageHelpers.Open<TextTrimmingPage>();
            var trimmed = window.ByName<TextBlock>("Trimmed");
            var box = window.ByName<ComboBox>("TrimBox");
            const string path = @"C:\Users\Administrator\Documents\Reports\2026\summary-final.docx";
            Assert.Equal("CharacterEllipsis", box.SelectedItem);
            Assert.Same(TextTrimming.CharacterEllipsis, trimmed.TextTrimming);
            Assert.Equal($"Text 长度 = {path.Length}（无论怎么截都不变）", window.Text("LengthText"));

            window.ByName<Slider>("WidthSlider").Value = 200;
            Assert.Equal(200, trimmed.Width);

            box.SelectedItem = "PathSegmentEllipsis";
            Assert.Same(TextTrimming.PathSegmentEllipsis, trimmed.TextTrimming);
            box.SelectedItem = "None";
            Assert.Same(TextTrimming.None, trimmed.TextTrimming);
            Assert.Equal(path, trimmed.Text);
        }

        [AvaloniaFact]
        public async Task Next_button_changes_the_page_and_counts_only_real_transitions()
        {
            var (window, _) = PageHelpers.Open<TransitioningContentControlPage>();
            var host = window.ByName<TransitioningContentControl>("Host");
            Assert.Equal("已完成 0 次过渡", window.Text("DoneText"));

            window.Press("NextButton");
            Assert.Equal("第 2 页", host.Content);
            await UiHelpers.WaitUntil(() => window.Text("DoneText") == "已完成 1 次过渡");

            window.Press("NextButton");
            Assert.Equal("第 3 页", host.Content);
            await UiHelpers.WaitUntil(() => window.Text("DoneText") == "已完成 2 次过渡");
        }

        [AvaloniaFact]
        public void Transition_box_and_reverse_check_set_the_host()
        {
            var (window, _) = PageHelpers.Open<TransitioningContentControlPage>();
            var host = window.ByName<TransitioningContentControl>("Host");
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.Equal("PageSlide", box.SelectedItem);
            Assert.Equal(TimeSpan.FromMilliseconds(400), Assert.IsType<PageSlide>(host.PageTransition).Duration);

            box.SelectedIndex = 1;
            Assert.IsType<CrossFade>(host.PageTransition);
            box.SelectedIndex = 2;
            Assert.IsType<Rotate3DTransition>(host.PageTransition);

            Assert.False(host.IsTransitionReversed);
            window.Click(window.ByName<CheckBox>("ReverseCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(host.IsTransitionReversed);
        }

        // ---- Feedback ----

        [AvaloniaFact]
        public async Task Send_button_shows_a_card_of_the_chosen_type_and_close_all_removes_them()
        {
            var (window, _) = PageHelpers.Open<NotificationPage>();
            try
            {
                Assert.Empty(Cards(window));
                Assert.Equal("已发送 0 条，点击 0 次", window.Text("SentText"));

                window.Press("ShowButton");
                window.ByName<ComboBox>("TypeBox").SelectedItem = NotificationType.Warning;
                window.Press("ShowButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 2);

                Assert.Equal("已发送 2 条，点击 0 次", window.Text("SentText"));
                var cards = Cards(window);
                Assert.Equal(new[] { NotificationType.Information, NotificationType.Warning }, cards.Select(c => c.NotificationType));
                Assert.Equal(new[] { "第 2 条", "这是一条 Warning 通知" }, Texts(cards[1]));

                window.Press("CloseAllButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 0);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Clicking_a_clickable_card_counts_the_click()
        {
            var (window, _) = PageHelpers.Open<NotificationPage>();
            try
            {
                window.Press("ClickableButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 1);
                Assert.Equal("已发送 1 条，点击 0 次", window.Text("SentText"));

                window.Click(Cards(window)[0]);
                Dispatcher.UIThread.RunJobs();

                Assert.Equal("已发送 1 条，点击 1 次", window.Text("SentText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Max_items_slider_caps_the_cards_and_the_oldest_one_goes()
        {
            var (window, page) = PageHelpers.Open<NotificationPage>();
            try
            {
                Assert.Equal(3, page.Manager!.MaxItems);
                window.ByName<Slider>("MaxItemsSlider").Value = 1;
                Assert.Equal("1", window.Text("MaxItemsText"));
                Assert.Equal(1, page.Manager.MaxItems);

                window.Press("ShowButton");
                window.Press("ShowButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 1);

                Assert.Equal("第 2 条", Texts(Cards(window)[0])[0]);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Position_box_and_seconds_slider_reach_the_manager_and_the_readout()
        {
            var (window, page) = PageHelpers.Open<NotificationPage>();
            try
            {
                Assert.Equal(NotificationPosition.TopRight, page.Manager!.Position);
                window.ByName<ComboBox>("PositionBox").SelectedItem = NotificationPosition.BottomLeft;
                Assert.Equal(NotificationPosition.BottomLeft, page.Manager.Position);

                window.ByName<Slider>("SecondsSlider").Value = 7;
                Assert.Equal("7", window.Text("SecondsText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Popup_toggle_opens_and_closes_and_counts_the_openings()
        {
            var (window, _) = PageHelpers.Open<PopupPage>();
            var pop = window.ByName<Popup>("Pop");
            int Shown() => window.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "我是 Popup 里的内容");
            Assert.Equal("IsOpen = False，打开 0 次", window.Text("StateText"));
            Assert.Equal(0, Shown());

            window.Press("ToggleButton");
            Assert.True(pop.IsOpen);
            Assert.Equal("IsOpen = True，打开 1 次", window.Text("StateText"));
            Assert.Equal(1, Shown());

            window.Press("ToggleButton");
            Assert.False(pop.IsOpen);
            Assert.Equal("IsOpen = False，打开 1 次", window.Text("StateText"));
            Assert.Equal(0, Shown());

            window.Press("ToggleButton");
            Assert.Equal("IsOpen = True，打开 2 次", window.Text("StateText"));
            window.Press("ToggleButton");
        }

        [AvaloniaFact]
        public void Clicking_outside_closes_the_popup_only_with_light_dismiss()
        {
            var (window, _) = PageHelpers.Open<PopupPage>();
            var pop = window.ByName<Popup>("Pop");
            var outside = window.ByName<TextBlock>("StateText");

            window.Press("ToggleButton");
            window.Click(outside);
            Dispatcher.UIThread.RunJobs();
            Assert.True(pop.IsOpen);
            window.Press("ToggleButton");

            window.Click(window.ByName<CheckBox>("LightDismissCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(pop.IsLightDismissEnabled);
            window.Press("ToggleButton");
            Assert.True(pop.IsOpen);
            window.Click(outside);
            Dispatcher.UIThread.RunJobs();

            Assert.False(pop.IsOpen);
            Assert.Equal("IsOpen = False，打开 2 次", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Popup_placement_box_sets_the_placement()
        {
            var (window, _) = PageHelpers.Open<PopupPage>();
            var box = window.ByName<ComboBox>("PlacementBox");
            Assert.Equal(PlacementMode.Bottom, box.SelectedItem);

            box.SelectedItem = PlacementMode.Right;
            Assert.Equal(PlacementMode.Right, window.ByName<Popup>("Pop").Placement);
        }

        [AvaloniaFact]
        public void Value_slider_drives_both_bars_and_their_percentages()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ProgressBarPage>();
            Assert.Equal("Value = 40，Percentage = 40%", window.Text("PercentText"));
            Assert.Equal("Value = -10，Percentage = 40%", window.Text("RangeText"));

            window.ByName<Slider>("ValueSlider").Value = 75;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Equal(75, window.ByName<ProgressBar>("Bar").Value);
            Assert.Equal("Value = 75，Percentage = 75%", window.Text("PercentText"));
            Assert.Equal("Value = 25，Percentage = 75%", window.Text("RangeText"));
        }

        [AvaloniaFact]
        public void Progress_check_boxes_set_indeterminate_text_and_orientation()
        {
            var (window, _) = PageHelpers.Open<ProgressBarPage>();
            var bar = window.ByName<ProgressBar>("Bar");
            void Check(string name)
            {
                window.Click(window.ByName<CheckBox>(name));
                Dispatcher.UIThread.RunJobs();
            }

            Check("IndeterminateCheck");
            Assert.True(bar.IsIndeterminate);
            Check("TextCheck");
            Assert.True(bar.ShowProgressText);

            Check("VerticalCheck");
            window.UpdateLayout();
            Assert.Equal(Orientation.Vertical, bar.Orientation);
            Assert.Equal(20, bar.Bounds.Width);

            Check("VerticalCheck");
            window.UpdateLayout();
            Assert.Equal(Orientation.Horizontal, bar.Orientation);
            Assert.Equal(280, bar.Bounds.Width);
        }

        // The Fluent theme gives a vertical ProgressBar MinHeight = 200, which beats a plain Height = 100: the page
        // has to lift MinHeight too, or the bar comes out 200 tall and overflows its 120-high frame.
        [AvaloniaFact]
        public void Vertical_bar_takes_the_height_the_page_sets()
        {
            var (window, _) = PageHelpers.Open<ProgressBarPage>();
            var bar = window.ByName<ProgressBar>("Bar");

            window.Click(window.ByName<CheckBox>("VerticalCheck"));
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Equal(new Size(20, 100), bar.Bounds.Size);
        }

        [AvaloniaFact]
        public void Tooltip_open_button_toggles_the_tip_and_the_readout()
        {
            var (window, _) = PageHelpers.Open<ToolTipPage>();
            var target = window.ByName<Button>("Target");
            int Shown() => window.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "我是简单的文字提示");
            Assert.Equal("IsOpen = False", window.Text("StateText"));
            Assert.Equal(0, Shown());

            window.Press("OpenButton");
            Assert.True(ToolTip.GetIsOpen(target));
            Assert.Equal("IsOpen = True", window.Text("StateText"));
            Assert.Equal(1, Shown());

            window.Press("OpenButton");
            Assert.False(ToolTip.GetIsOpen(target));
            Assert.Equal("IsOpen = False", window.Text("StateText"));
            Assert.Equal(0, Shown());
        }

        [AvaloniaFact]
        public void Delay_slider_and_placement_box_set_the_target_tip()
        {
            var (window, _) = PageHelpers.Open<ToolTipPage>();
            var target = window.ByName<Button>("Target");
            Assert.Equal(PlacementMode.Pointer, window.ByName<ComboBox>("PlacementBox").SelectedItem);

            window.ByName<Slider>("DelaySlider").Value = 1000;
            Assert.Equal("1000", window.Text("DelayText"));
            Assert.Equal(1000, ToolTip.GetShowDelay(target));

            window.ByName<ComboBox>("PlacementBox").SelectedItem = PlacementMode.Top;
            Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(target));
        }
    }
}
