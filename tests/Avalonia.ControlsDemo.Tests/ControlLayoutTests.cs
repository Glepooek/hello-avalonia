using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Layout.
    public class ControlLayoutTests
    {
        // ---- Containers ----

        // Thickness and CornerRadius are structs set from code, so assert the real values and where the child lands.
        [AvaloniaFact]
        public void Border_sliders_set_thickness_and_radius_and_push_the_child_in()
        {
            var (window, _) = PageHelpers.Open<BorderPage>();
            try
            {
                var target = window.ByName<Border>("Target");
                var child = (Control)target.Child!;
                Assert.Equal(new Thickness(2), target.BorderThickness);
                Assert.Equal(new CornerRadius(8), target.CornerRadius);
                Assert.Equal(14, child.Bounds.X);

                window.ByName<Slider>("ThicknessSlider").Value = 6;
                window.ByName<Slider>("RadiusSlider").Value = 20;
                window.UpdateLayout();

                Assert.Equal(new Thickness(6), target.BorderThickness);
                Assert.Equal(new CornerRadius(20), target.CornerRadius);
                Assert.Equal(18, child.Bounds.X);
            }
            finally
            {
                window.Close();
            }
        }

        private static ToggleButton ExpanderHeader(Expander expander)
            => expander.GetVisualDescendants().OfType<ToggleButton>().First(t => t.TemplatedParent == expander);

        [AvaloniaFact]
        public void Clicking_the_expander_header_counts_expanded_and_collapsed()
        {
            var (window, _) = PageHelpers.Open<ExpanderPage>();
            try
            {
                var basic = window.ByName<Expander>("Basic");
                Assert.Equal("已展开 0 次，已折叠 0 次", window.Text("BasicResult"));

                window.Click(ExpanderHeader(basic));
                Dispatcher.UIThread.RunJobs();
                Assert.True(basic.IsExpanded);
                Assert.Equal("已展开 1 次，已折叠 0 次", window.Text("BasicResult"));

                window.Click(ExpanderHeader(basic));
                Dispatcher.UIThread.RunJobs();
                Assert.False(basic.IsExpanded);
                Assert.Equal("已展开 1 次，已折叠 1 次", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Disabled_expander_ignores_the_mouse_but_not_code()
        {
            var (window, _) = PageHelpers.Open<ExpanderPage>();
            try
            {
                var basic = window.ByName<Expander>("Basic");
                window.Press("EnabledCheck");
                Assert.False(basic.IsEnabled);

                window.Click(ExpanderHeader(basic));
                Dispatcher.UIThread.RunJobs();
                Assert.False(basic.IsExpanded);
                Assert.Equal("已展开 0 次，已折叠 0 次", window.Text("BasicResult"));

                basic.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已展开 1 次，已折叠 0 次", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Direction_box_moves_the_expander_content_to_that_side()
        {
            var (window, _) = PageHelpers.Open<ExpanderPage>();
            try
            {
                var box = window.ByName<ComboBox>("DirectionBox");
                var sideways = window.ByName<Expander>("Sideways");
                // Collapsed content is not in the visual tree yet, so look it up after expanding.
                TextBlock Content() => sideways.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "展开的内容");
                Assert.Equal(ExpandDirection.Down, box.SelectedItem);
                sideways.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                var content = Content();
                var header = ExpanderHeader(sideways);
                Assert.True(content.TranslatePoint(default, header)!.Value.Y >= header.Bounds.Height);

                box.SelectedItem = ExpandDirection.Right;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(ExpandDirection.Right, sideways.ExpandDirection);
                content = Content();
                header = ExpanderHeader(sideways);
                Assert.True(content.TranslatePoint(default, header)!.Value.X >= header.Bounds.Width);
            }
            finally
            {
                window.Close();
            }
        }

        // The flyout content lives in the overlay layer: absent before opening, present exactly once after.
        [AvaloniaFact]
        public void Opening_and_dismissing_the_flyout_updates_the_status()
        {
            var (window, _) = PageHelpers.Open<FlyoutPage>();
            try
            {
                int Confirms() => window.GetVisualDescendants().OfType<Button>().Count(b => b.Name == "ConfirmButton");
                Assert.Equal("尚未操作", window.Text("FlyoutResult"));
                Assert.Equal(0, Confirms());

                window.Press("OpenButton");
                Assert.Equal("Flyout 已打开", window.Text("FlyoutResult"));
                Assert.Equal(1, Confirms());

                window.Click(window.ByName<TextBlock>("FlyoutResult"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Flyout 已关闭", window.Text("FlyoutResult"));
                Assert.Equal(0, Confirms());
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTheory]
        [InlineData("上", "在上方")]
        [InlineData("右", "在右侧")]
        [InlineData("下", "在下方")]
        [InlineData("左", "在左侧")]
        public void Placement_buttons_open_their_flyout_on_that_side(string button, string text)
        {
            var (window, _) = PageHelpers.Open<FlyoutPage>();
            try
            {
                TextBlock[] Shown() => window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Text == text).ToArray();
                Assert.Empty(Shown());

                var target = window.ButtonWithText(button);
                window.PressText(button);
                window.UpdateLayout();

                var shown = Assert.Single(Shown());
                var topLeft = shown.TranslatePoint(default, target)!.Value;
                var bottomRight = shown.TranslatePoint(new Point(shown.Bounds.Width, shown.Bounds.Height), target)!.Value;
                switch (button)
                {
                    case "上": Assert.True(bottomRight.Y <= 0, $"bottom {bottomRight.Y}"); break;
                    case "右": Assert.True(topLeft.X >= target.Bounds.Width, $"left {topLeft.X}"); break;
                    case "下": Assert.True(topLeft.Y >= target.Bounds.Height, $"top {topLeft.Y}"); break;
                    case "左": Assert.True(bottomRight.X <= 0, $"right {bottomRight.X}"); break;
                }
            }
            finally
            {
                window.Close();
            }
        }

        private static Button PagerButton(PipsPager pager, string part)
            => pager.GetVisualDescendants().OfType<Button>().Single(b => b.Name == part);

        private static ListBoxItem[] Pips(PipsPager pager)
            => pager.GetVisualDescendants().OfType<ListBoxItem>().ToArray();

        [AvaloniaFact]
        public void Next_button_and_pips_change_the_page_text()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var pager = window.ByName<PipsPager>("Pager");
                Assert.Equal("第 1 页", window.Text("PageText"));

                window.Click(PagerButton(pager, "PART_NextButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("第 2 页", window.Text("PageText"));

                window.Click(Pips(pager)[3]);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(3, pager.SelectedPageIndex);
                Assert.Equal("第 4 页", window.Text("PageText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Pages_slider_sets_the_number_of_pips()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var pager = window.ByName<PipsPager>("Pager");
                Assert.Equal(5, Pips(pager).Length);

                window.ByName<Slider>("PagesSlider").Value = 8;
                window.UpdateLayout();

                Assert.Equal(8, pager.NumberOfPages);
                Assert.Equal(8, Pips(pager).Length);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Next_check_box_hides_and_shows_the_next_button()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var next = PagerButton(window.ByName<PipsPager>("Pager"), "PART_NextButton");
                Assert.True(next.IsEffectivelyVisible);

                window.Press("NextCheck");
                Assert.False(next.IsEffectivelyVisible);
                window.Press("NextCheck");
                Assert.True(next.IsEffectivelyVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // Avalonia 12's PipsPager shows both buttons by default, so the check boxes have to start ticked.
        [AvaloniaFact]
        public void Pager_button_check_boxes_start_in_sync_with_the_pager()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var pager = window.ByName<PipsPager>("Pager");
                Assert.Equal(pager.IsPreviousButtonVisible, window.ByName<CheckBox>("PreviousCheck").IsChecked);
                Assert.Equal(pager.IsNextButtonVisible, window.ByName<CheckBox>("NextCheck").IsChecked);
            }
            finally
            {
                window.Close();
            }
        }

        // RequestRefresh goes through the same deferral path as the pull gesture; the handler finishes 300 ms later.
        [AvaloniaFact]
        public async Task Refresh_button_runs_the_refresh_handler_once_per_press()
        {
            var (window, _) = PageHelpers.Open<RefreshContainerPage>();
            try
            {
                Assert.Equal("刷新 0 次", window.Text("RefreshResult"));
                Assert.Equal("内容：尚未刷新", window.Text("Body"));

                window.Press("RefreshButton");
                await UiHelpers.WaitUntil(() => window.Text("RefreshResult") == "刷新 1 次");
                Assert.Matches(@"^内容：\d\d:\d\d:\d\d 刷新$", window.Text("Body"));

                window.Press("RefreshButton");
                await UiHelpers.WaitUntil(() => window.Text("RefreshResult") == "刷新 2 次");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Scroll_buttons_move_the_offset_and_the_readout_follows()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerPage>();
            try
            {
                var scroller = window.ByName<ScrollViewer>("Scroller");
                Assert.Equal(ScrollBarVisibility.Disabled, window.ByName<ComboBox>("HBox").SelectedItem);
                Assert.Equal(ScrollBarVisibility.Auto, window.ByName<ComboBox>("VBox").SelectedItem);
                Assert.Equal("Offset = 0, 0", window.Text("OffsetText"));

                window.Press("EndButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                // ScrollToEnd goes to the bottom-left corner: the vertical maximum, horizontal 0.
                Assert.Equal(new Vector(0, 360), scroller.Offset);
                Assert.Equal("Offset = 0, 360", window.Text("OffsetText"));

                window.Press("HomeButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Offset = 0, 0", window.Text("OffsetText"));

                window.Press("LineButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Offset = 0, 16", window.Text("OffsetText"));
            }
            finally
            {
                window.Close();
            }
        }

        private static ScrollBar ScrollerBar(ScrollViewer scroller, string part)
            => scroller.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Name == part);

        [AvaloniaFact]
        public void Visibility_boxes_show_and_hide_the_scroll_bars()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerPage>();
            try
            {
                var scroller = window.ByName<ScrollViewer>("Scroller");
                Assert.False(ScrollerBar(scroller, "PART_HorizontalScrollBar").IsEffectivelyVisible);
                Assert.True(ScrollerBar(scroller, "PART_VerticalScrollBar").IsEffectivelyVisible);

                window.ByName<ComboBox>("HBox").SelectedItem = ScrollBarVisibility.Visible;
                window.ByName<ComboBox>("VBox").SelectedItem = ScrollBarVisibility.Hidden;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                Assert.Equal(ScrollBarVisibility.Visible, scroller.HorizontalScrollBarVisibility);
                Assert.True(ScrollerBar(scroller, "PART_HorizontalScrollBar").IsEffectivelyVisible);
                Assert.False(ScrollerBar(scroller, "PART_VerticalScrollBar").IsEffectivelyVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // Writing an Offset past the end is clamped to extent minus viewport (600x500 content in a 320x140 viewer).
        // Code can scroll sideways even while the horizontal bar is Disabled.
        [AvaloniaFact]
        public void Out_of_range_offset_is_clamped_to_the_maximum()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerPage>();
            try
            {
                var scroller = window.ByName<ScrollViewer>("Scroller");
                scroller.Offset = new Vector(1000, 1000);
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(new Vector(280, 360), scroller.Offset);
                Assert.Equal("Offset = 280, 360", window.Text("OffsetText"));
            }
            finally
            {
                window.Close();
            }
        }

        private static Control SplitPart(SplitView split, string name)
            => split.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name);

        [AvaloniaFact]
        public async Task Toggle_button_opens_the_compact_inline_pane_and_pushes_the_content()
        {
            var (window, _) = PageHelpers.Open<SplitViewPage>();
            try
            {
                var split = window.ByName<SplitView>("Split");
                Assert.Equal(SplitViewDisplayMode.CompactInline, window.ByName<ComboBox>("ModeBox").SelectedItem);
                Assert.Equal("窗格：关闭", window.Text("PaneText"));
                Assert.Equal(48, SplitPart(split, "PART_PaneRoot").Bounds.Width);
                Assert.Equal(48, SplitPart(split, "ContentRoot").Bounds.X);

                // PaneOpened fires when the pane starts opening; its width animates, so wait for the final layout.
                window.Press("ToggleButton");
                await UiHelpers.WaitUntil(() => window.Text("PaneText") == "窗格：打开");
                await UiHelpers.WaitUntil(() => { window.UpdateLayout(); return SplitPart(split, "PART_PaneRoot").Bounds.Width == 180; });
                Assert.True(split.IsPaneOpen);
                Assert.Equal(180, SplitPart(split, "ContentRoot").Bounds.X);

                window.Press("ToggleButton");
                await UiHelpers.WaitUntil(() => window.Text("PaneText") == "窗格：关闭");
                await UiHelpers.WaitUntil(() => { window.UpdateLayout(); return SplitPart(split, "PART_PaneRoot").Bounds.Width == 48; });
                Assert.Equal(48, SplitPart(split, "ContentRoot").Bounds.X);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Overlay_mode_opens_the_pane_over_the_content_and_right_placement_moves_it()
        {
            var (window, _) = PageHelpers.Open<SplitViewPage>();
            try
            {
                var split = window.ByName<SplitView>("Split");
                window.ByName<ComboBox>("ModeBox").SelectedItem = SplitViewDisplayMode.Overlay;
                window.ByName<ComboBox>("PlacementBox").SelectedItem = SplitViewPanePlacement.Right;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(SplitViewDisplayMode.Overlay, split.DisplayMode);
                Assert.Equal(SplitViewPanePlacement.Right, split.PanePlacement);

                window.Press("ToggleButton");
                await UiHelpers.WaitUntil(() => window.Text("PaneText") == "窗格：打开");
                var pane = SplitPart(split, "PART_PaneRoot");
                var content = SplitPart(split, "ContentRoot");
                await UiHelpers.WaitUntil(() => { window.UpdateLayout(); return pane.Bounds.Width == 180; });
                // Overlay leaves the content full width under the open pane, which now hugs the right edge.
                Assert.Equal(0, content.Bounds.X);
                Assert.Equal(SplitPart(split, "Container").Bounds.Width, content.Bounds.Width);
                Assert.Equal(content.Bounds.Right, pane.Bounds.Right);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Stretch_box_rescales_the_viewbox_child()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ViewboxPage>();
            try
            {
                Assert.Equal(Stretch.Uniform, window.ByName<ComboBox>("StretchBox").SelectedItem);
                Assert.Equal("水平缩放约 2.76 倍", window.Text("ScaleText"));

                void Pick(Stretch stretch)
                {
                    window.ByName<ComboBox>("StretchBox").SelectedItem = stretch;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }

                Pick(Stretch.Fill);
                Assert.Equal("水平缩放约 3.18 倍", window.Text("ScaleText"));
                Pick(Stretch.None);
                Assert.Equal("水平缩放约 1.00 倍", window.Text("ScaleText"));
                // The child itself is never resized.
                Assert.Equal(new Size(100, 50), window.ByName<Border>("Inner").Bounds.Size);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Decorator ----

        [AvaloniaFact]
        public void Padding_slider_grows_the_decorator_around_its_child()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<DecoratorPage>();
            try
            {
                var decorator = window.ByName<Decorator>("PaddedDecorator");
                Assert.Equal(new Thickness(10), decorator.Padding);
                Assert.Equal("占 140 × 60", window.Text("SizeText"));

                window.ByName<Slider>("PaddingSlider").Value = 25;
                window.UpdateLayout();

                Assert.Equal(new Thickness(25), decorator.Padding);
                Assert.Equal(new Size(170, 90), decorator.Bounds.Size);
                Assert.Equal("占 170 × 90", window.Text("SizeText"));
            }
            finally
            {
                window.Close();
            }
        }

        // ---- LayoutTransformControl ----

        [AvaloniaFact]
        public void Angle_slider_resizes_the_layout_host_but_not_the_render_host()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<LayoutTransformControlPage>();
            try
            {
                Assert.Equal("宿主 40 × 100", window.Text("LayoutSize"));
                Assert.Equal("宿主 100 × 40", window.Text("RenderSize"));

                window.ByName<Slider>("AngleSlider").Value = 45;
                window.UpdateLayout();
                Assert.Equal("宿主 99 × 99", window.Text("LayoutSize"));
                Assert.Equal("宿主 100 × 40", window.Text("RenderSize"));

                window.ByName<Slider>("AngleSlider").Value = 0;
                window.UpdateLayout();
                Assert.Equal("宿主 100 × 40", window.Text("LayoutSize"));
                Assert.Equal(new Size(100, 40), window.ByName<LayoutTransformControl>("LayoutHost").Bounds.Size);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Panels ----

        [AvaloniaFact]
        public void Left_and_top_sliders_move_the_dot_on_the_canvas()
        {
            var (window, _) = PageHelpers.Open<CanvasPage>();
            try
            {
                var dot = window.ByName<Border>("Dot");
                Assert.Equal(new Rect(200, 30, 40, 40), dot.Bounds);

                window.ByName<Slider>("LeftSlider").Value = 100;
                window.ByName<Slider>("TopSlider").Value = 80;
                window.UpdateLayout();

                Assert.Equal(100, Canvas.GetLeft(dot));
                Assert.Equal(new Rect(100, 80, 40, 40), dot.Bounds);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Clearing_last_child_fill_docks_the_last_child_left_at_its_own_width()
        {
            var (window, _) = PageHelpers.Open<DockPanelPage>();
            try
            {
                var last = window.ByName<Border>("LastChild");
                Assert.Equal(new Rect(60, 30, 200, 100), last.Bounds);

                window.Press("FillCheck");
                window.UpdateLayout();

                Assert.False(window.ByName<DockPanel>("Dock").LastChildFill);
                Assert.Equal(60, last.Bounds.X);
                Assert.Equal(last.Child!.DesiredSize.Width, last.Bounds.Width);
                Assert.True(last.Bounds.Width < 200);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Grid_columns_split_by_their_definitions_and_spacing_opens_gaps()
        {
            var (window, _) = PageHelpers.Open<GridPage>();
            try
            {
                var grid = window.ByName<Grid>("Demo");
                // ColumnDefinition.ActualWidth includes the spacing once ColumnSpacing is set, so read the cells instead.
                Rect Cell(int i) => grid.Children[i].Bounds;
                Assert.Equal(80, Cell(0).Width);
                Assert.Equal(2 * Cell(2).Width, Cell(3).Width);
                Assert.Equal(360, Cell(3).Right);
                Assert.Equal(40, Cell(4).Y);

                window.ByName<Slider>("SpacingSlider").Value = 10;
                window.UpdateLayout();

                Assert.Equal(10, grid.ColumnSpacing);
                Assert.Equal(10, grid.RowSpacing);
                Assert.Equal(new double[] { 0, 90, 146, 224 }, Enumerable.Range(0, 4).Select(i => Cell(i).X));
                Assert.Equal(2 * Cell(2).Width, Cell(3).Width);
                Assert.Equal(360, Cell(3).Right);
                Assert.Equal(50, Cell(4).Y);

                window.Press("LinesCheck");
                Assert.True(grid.ShowGridLines);
            }
            finally
            {
                window.Close();
            }
        }

        private static Point Center(Window window, Control control)
            => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        [AvaloniaFact]
        public void Dragging_the_splitter_resizes_the_left_column()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<GridSplitterPage>();
            try
            {
                var split = window.ByName<Grid>("Split");
                Assert.Equal("左栏宽度 200", window.Text("WidthText"));

                var from = Center(window, window.ByName<GridSplitter>("Splitter"));
                window.MouseDown(from, MouseButton.Left);
                window.MouseMove(from + new Point(30, 0), RawInputModifiers.LeftMouseButton);
                window.MouseUp(from + new Point(30, 0), MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                Assert.Equal(230, split.ColumnDefinitions[0].ActualWidth);
                Assert.Equal("左栏宽度 230", window.Text("WidthText"));
            }
            finally
            {
                window.Close();
            }
        }

        // With ShowsPreview only a preview line moves during the drag; the column changes on release.
        [AvaloniaFact]
        public void Shows_preview_holds_the_column_until_the_drag_ends()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<GridSplitterPage>();
            try
            {
                var split = window.ByName<Grid>("Split");
                var splitter = window.ByName<GridSplitter>("Splitter");
                window.Press("PreviewCheck");
                Assert.True(splitter.ShowsPreview);

                var from = Center(window, splitter);
                window.MouseDown(from, MouseButton.Left);
                window.MouseMove(from + new Point(30, 0), RawInputModifiers.LeftMouseButton);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(200, split.ColumnDefinitions[0].ActualWidth);

                window.MouseUp(from + new Point(30, 0), MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(230, split.ColumnDefinitions[0].ActualWidth);
                Assert.Equal("左栏宽度 230", window.Text("WidthText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Arrow_keys_on_the_focused_splitter_move_it_by_the_keyboard_increment()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<GridSplitterPage>();
            try
            {
                var splitter = window.ByName<GridSplitter>("Splitter");
                window.Click(splitter);
                Dispatcher.UIThread.RunJobs();
                Assert.True(splitter.IsFocused);

                window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal("左栏宽度 210", window.Text("WidthText"));

                window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                window.KeyRelease(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                window.KeyRelease(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal("左栏宽度 190", window.Text("WidthText"));
            }
            finally
            {
                window.Close();
            }
        }

        // Hit testing tells which child is really on top, not just which ZIndex is larger.
        [AvaloniaFact]
        public void Swap_button_puts_the_lower_child_on_top()
        {
            var (window, _) = PageHelpers.Open<PanelPage>();
            try
            {
                var under = window.ByName<Border>("Under");
                var over = window.ByName<Border>("Over");
                var probe = window.ByName<Panel>("Stack").TranslatePoint(new Point(150, 60), window)!.Value;
                Border? Top() => (window.InputHitTest(probe) as Visual)?.GetSelfAndVisualAncestors()
                    .OfType<Border>().FirstOrDefault(b => b == under || b == over);
                Assert.Same(over, Top());

                window.Press("SwapButton");
                Assert.Equal(-1, over.ZIndex);
                Assert.Equal(1, under.ZIndex);
                Assert.Same(under, Top());

                window.Press("SwapButton");
                Assert.Same(over, Top());
            }
            finally
            {
                window.Close();
            }
        }

        // With RightOf and AlignRightWithPanel both set, the fixed-width follower centres between the two constraints.
        [AvaloniaFact]
        public void Align_right_with_panel_centres_the_follower_between_its_constraints()
        {
            var (window, _) = PageHelpers.Open<RelativePanelPage>();
            try
            {
                var follower = window.ByName<Border>("Follower");
                Assert.Equal(new Rect(90, 0, 90, 40), follower.Bounds);
                Assert.Equal(new Rect(250, 110, 90, 30), window.ByName<Border>("Edge").Bounds);

                window.Press("PanelRightCheck");
                window.UpdateLayout();

                Assert.True(RelativePanel.GetAlignRightWithPanel(follower));
                Assert.Equal(new Rect(170, 0, 90, 40), follower.Bounds);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Orientation_and_spacing_rearrange_the_stack()
        {
            var (window, _) = PageHelpers.Open<StackPanelPage>();
            try
            {
                var stack = window.ByName<StackPanel>("Stack");
                double[] Ys() => stack.Children.Select(c => c.Bounds.Y).ToArray();
                double[] Xs() => stack.Children.Select(c => c.Bounds.X).ToArray();
                Assert.Equal(Orientation.Vertical, window.ByName<ComboBox>("OrientationBox").SelectedItem);
                Assert.Equal(new double[] { 0, 36, 72 }, Ys());

                window.ByName<ComboBox>("OrientationBox").SelectedItem = Orientation.Horizontal;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(new double[] { 0, 66, 132 }, Xs());

                window.ByName<Slider>("SpacingSlider").Value = 20;
                window.UpdateLayout();
                Assert.Equal(new double[] { 0, 80, 160 }, Xs());
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Columns_and_first_column_sliders_reflow_the_uniform_grid()
        {
            var (window, _) = PageHelpers.Open<UniformGridPage>();
            try
            {
                var grid = window.ByName<UniformGrid>("Uniform");
                // Each cell's Border has Margin 2, so Bounds sit 2 px inside the cell.
                Assert.Equal(new Rect(2, 72, 96, 66), grid.Children[3].Bounds);

                window.ByName<Slider>("ColumnsSlider").Value = 2;
                window.UpdateLayout();
                Assert.Equal(2, grid.Columns);
                // Three rows of 46.67 px; layout rounding puts the third item at y = 2 + 47.
                Assert.Equal(new Point(2, 49), grid.Children[2].Bounds.Position);

                window.ByName<Slider>("ColumnsSlider").Value = 3;
                window.ByName<Slider>("FirstSlider").Value = 1;
                window.UpdateLayout();
                Assert.Equal(1, grid.FirstColumn);
                Assert.Equal(102, grid.Children[0].Bounds.X);
                Assert.Equal(new Point(2, 72), grid.Children[2].Bounds.Position);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Wrap_panel_wraps_by_width_and_follows_spacing_and_orientation()
        {
            var (window, _) = PageHelpers.Open<WrapPanelPage>();
            try
            {
                var wrap = window.ByName<WrapPanel>("Wrap");
                var fourth = wrap.Children[3];
                // 70 + 50 + 80 + 60 plus three 4 px gaps is 272 > 260, so the fourth item starts the second line.
                Assert.Equal(new Point(0, 44), fourth.Bounds.Position);

                window.ByName<Slider>("ItemGapSlider").Value = 0;
                window.UpdateLayout();
                Assert.Equal(0, wrap.ItemSpacing);
                Assert.Equal(new Point(200, 0), fourth.Bounds.Position);

                window.ByName<Slider>("ItemGapSlider").Value = 4;
                window.ByName<ComboBox>("OrientationBox").SelectedItem = Orientation.Vertical;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(Orientation.Vertical, wrap.Orientation);
                // 30 + 40 + 30 + 50 plus gaps is 162 > 150, so it starts the second column (after the 80-wide first
                // column and a 4 px line gap), centred in that column's 90 px width.
                Assert.Equal(new Point(99, 0), fourth.Bounds.Position);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
