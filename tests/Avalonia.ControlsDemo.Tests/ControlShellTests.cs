using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Media, Menus, Navigation, Primitives and System.
    public class ControlShellTests
    {
        // ---- Media ----

        [AvaloniaFact]
        public void Recolor_button_cycles_the_built_drawing_through_the_palette()
        {
            var (window, _) = PageHelpers.Open<DrawingImagePage>();
            var drawing = (GeometryDrawing)((DrawingImage)window.ByName<Image>("Built").Source!).Drawing!;
            Assert.Equal("Brush = OrangeRed", window.Text("ColorText"));

            window.Press("RecolorButton");
            Assert.Equal("Brush = SeaGreen", window.Text("ColorText"));
            Assert.Same(Brushes.SeaGreen, drawing.Brush);

            window.Press("RecolorButton");
            window.Press("RecolorButton");
            window.Press("RecolorButton");
            Assert.Equal("Brush = OrangeRed", window.Text("ColorText"));
            Assert.Same(Brushes.OrangeRed, drawing.Brush);
        }

        // The readout follows LayoutUpdated, so it only changes after a layout pass. The 260×140 frame has a 1px
        // border, which leaves 258×138 for the image.
        [AvaloniaFact]
        public void Stretch_and_direction_boxes_change_the_rendered_size()
        {
            var (window, _) = PageHelpers.Open<ImagePage>();
            var pic = window.ByName<Image>("Pic");
            void Pick(string box, object value)
            {
                window.ByName<ComboBox>(box).SelectedItem = value;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
            }
            Assert.Equal("源图 40×20，Image 占位 258×129，期望尺寸 258×129", window.Text("SizeText"));

            Pick("StretchBox", Stretch.None);
            Assert.Equal(Stretch.None, pic.Stretch);
            Assert.Equal("源图 40×20，Image 占位 40×20，期望尺寸 40×20", window.Text("SizeText"));

            Pick("StretchBox", Stretch.Uniform);
            // DownOnly only limits measuring: the image still gets the whole slot and just draws its pixels at 40×20.
            Pick("DirectionBox", StretchDirection.DownOnly);
            Assert.Equal("源图 40×20，Image 占位 258×129，期望尺寸 40×20", window.Text("SizeText"));
        }

        [AvaloniaFact]
        public void Interpolation_box_sets_the_render_option_on_the_image()
        {
            var (window, _) = PageHelpers.Open<ImagePage>();
            var pic = window.ByName<Image>("Pic");

            window.ByName<ComboBox>("InterpolationBox").SelectedItem = BitmapInterpolationMode.None;

            Assert.Equal(BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode(pic));
        }

        [AvaloniaFact]
        public void Path_icon_follows_the_geometry_colour_and_size_controls()
        {
            var (window, page) = PageHelpers.Open<PathIconPage>();
            var icon = window.ByName<PathIcon>("Icon");
            Assert.Equal("32", window.Text("SizeText"));
            Assert.Equal(32, icon.Width);
            Assert.Same(page.FindResource("HeartGeometry"), icon.Data);

            window.ByName<ComboBox>("GeometryBox").SelectedIndex = 1;
            window.ByName<ComboBox>("ColorBox").SelectedIndex = 2;
            window.ByName<Slider>("SizeSlider").Value = 64;

            Assert.Same(page.FindResource("CheckGeometry"), icon.Data);
            Assert.Same(Brushes.SteelBlue, icon.Foreground);
            Assert.Equal("64", window.Text("SizeText"));
            Assert.Equal(64, icon.Height);
        }

        // ---- Menus ----

        private static void RightClick(Window window, Control control)
        {
            Dispatcher.UIThread.RunJobs();
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var center = window.CenterOf(control);
            window.MouseDown(center, MouseButton.Right);
            window.MouseUp(center, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
        }

        private static int MenuItemsWithHeader(Window window, string header)
            => window.GetVisualDescendants().OfType<MenuItem>().Count(m => header.Equals(m.Header));

        [AvaloniaFact]
        public void Right_click_raises_opening_and_opened_and_shows_the_items()
        {
            var (window, _) = PageHelpers.Open<ContextMenuPage>();
            var menu = window.ByName<Border>("Area").ContextMenu!;
            Assert.Equal(0, MenuItemsWithHeader(window, "属性"));

            RightClick(window, window.ByName<Border>("Area"));

            Assert.True(menu.IsOpen);
            Assert.Equal(1, MenuItemsWithHeader(window, "属性"));
            Assert.Equal("Opening 1 次，Opened 1 次，Closing 0 次", window.Text("LogText"));

            menu.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Opening 1 次，Opened 1 次，Closing 1 次", window.Text("LogText"));
        }

        [AvaloniaFact]
        public void Cancelling_in_opening_keeps_the_menu_closed()
        {
            var (window, _) = PageHelpers.Open<ContextMenuPage>();
            var menu = window.ByName<Border>("Area").ContextMenu!;
            window.Click(window.ByName<CheckBox>("CancelCheck"));
            Dispatcher.UIThread.RunJobs();

            RightClick(window, window.ByName<Border>("Area"));

            Assert.False(menu.IsOpen);
            Assert.Equal(0, MenuItemsWithHeader(window, "属性"));
            Assert.Equal("Opening 1 次，Opened 0 次，Closing 0 次", window.Text("LogText"));
        }

        // What the page's hint claims: Open() from code skips Opening, so the cancel box has no say.
        [AvaloniaFact]
        public void Opening_from_code_skips_opening_even_when_cancel_is_checked()
        {
            var (window, _) = PageHelpers.Open<ContextMenuPage>();
            var menu = window.ByName<Border>("Area").ContextMenu!;
            window.ByName<CheckBox>("CancelCheck").IsChecked = true;

            window.Press("OpenButton");

            Assert.True(menu.IsOpen);
            Assert.Equal("Opening 0 次，Opened 1 次，Closing 0 次", window.Text("LogText"));
        }

        // Header holds the raw "_新建": the access-key underscore shows up in the readout.
        [AvaloniaFact]
        public void Clicking_a_menu_item_reports_it_and_toggles_update_the_state_line()
        {
            var (window, page) = PageHelpers.Open<MenuPage>();
            var top = window.GetVisualDescendants().OfType<Menu>().Single().Items.OfType<MenuItem>().ToArray();
            void Choose(MenuItem parent, string name)
            {
                window.Click(parent);
                Dispatcher.UIThread.RunJobs();
                window.Click(window.ByName<MenuItem>(name));
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("粗体 = False，对齐 = 左", window.Text("StateText"));

            Choose(top[0], "NewItem");
            Assert.Equal("最近点击：_新建", window.Text("LastText"));

            Choose(top[1], "BoldItem");
            Choose(top[1], "CenterItem");
            Assert.Equal("最近点击：居中", window.Text("LastText"));
            Assert.Equal("粗体 = True，对齐 = 中", window.Text("StateText"));
            // The submenu has closed again, so look the item up in the name scope, not the visual tree.
            Assert.False(page.FindControl<MenuItem>("LeftItem")!.IsChecked);
        }

        [AvaloniaFact]
        public void Clicking_the_trigger_opens_the_flyout_and_picking_an_item_closes_it()
        {
            var (window, _) = PageHelpers.Open<MenuFlyoutPage>();
            Assert.Equal(0, MenuItemsWithHeader(window, "重命名"));

            window.Press("Trigger");
            Assert.Equal(1, MenuItemsWithHeader(window, "重命名"));
            Assert.Equal("Opened 1 次，Closed 0 次", window.Text("LogText"));

            window.Click(window.GetVisualDescendants().OfType<MenuItem>().Single(m => "重命名".Equals(m.Header)));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, MenuItemsWithHeader(window, "重命名"));
            Assert.Equal("Opened 1 次，Closed 1 次", window.Text("LogText"));
        }

        [AvaloniaFact]
        public void Placement_box_and_show_at_drive_the_same_flyout()
        {
            var (window, _) = PageHelpers.Open<MenuFlyoutPage>();
            var flyout = (MenuFlyout)window.ByName<Button>("Trigger").Flyout!;

            window.ByName<ComboBox>("PlacementBox").SelectedItem = PlacementMode.Top;
            window.Press("ShowButton");

            Assert.Equal(PlacementMode.Top, flyout.Placement);
            Assert.True(flyout.IsOpen);
            Assert.Equal("Opened 1 次，Closed 0 次", window.Text("LogText"));
        }

        // Headless exports nothing, but attaching and detaching the menu is the page's own logic.
        [AvaloniaFact]
        public void Attach_box_sets_and_clears_the_window_native_menu()
        {
            var (window, page) = PageHelpers.Open<NativeMenuPage>();
            try
            {
                Assert.Equal("NativeMenu.GetIsNativeMenuExported = False", window.Text("ExportText"));
                Assert.Equal("菜单含 1 个顶层项，点击「演示：你好」0 次", window.Text("ItemText"));
                Assert.Null(NativeMenu.GetMenu(window));

                window.Click(window.ByName<CheckBox>("AttachCheck"));
                Dispatcher.UIThread.RunJobs();
                Assert.Same(page.Built, NativeMenu.GetMenu(window));

                window.Click(window.ByName<CheckBox>("AttachCheck"));
                Dispatcher.UIThread.RunJobs();
                Assert.Null(NativeMenu.GetMenu(window));
            }
            finally
            {
                window.Close();
            }
        }

        // Avalonia 12 passes the TopLevelHost as RootVisual on detach, so "e.RootVisual is TopLevel" never matches:
        // the page has to remember the window it was attached to.
        [AvaloniaFact]
        public void Leaving_the_page_takes_the_native_menu_off_the_window()
        {
            var (window, page) = PageHelpers.Open<NativeMenuPage>();
            try
            {
                window.ByName<CheckBox>("AttachCheck").IsChecked = true;
                Assert.Same(page.Built, NativeMenu.GetMenu(window));

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.Null(NativeMenu.GetMenu(window));
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Navigation ----

        [AvaloniaFact]
        public void Carousel_buttons_step_through_the_pages_and_stop_at_both_ends()
        {
            var (window, _) = PageHelpers.Open<CarouselDemoPage>();
            Assert.Equal("SelectedIndex = 0，共 3 页", window.Text("StateText"));

            window.Press("PrevButton");
            Assert.Equal("SelectedIndex = 0，共 3 页", window.Text("StateText"));
            window.Press("NextButton");
            window.Press("NextButton");
            window.Press("NextButton");
            Assert.Equal("SelectedIndex = 2，共 3 页", window.Text("StateText"));
            window.Press("PrevButton");
            Assert.Equal("SelectedIndex = 1，共 3 页", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Carousel_transition_box_swaps_the_page_transition()
        {
            var (window, _) = PageHelpers.Open<CarouselDemoPage>();
            var carousel = window.GetVisualDescendants().OfType<Avalonia.Controls.CarouselPage>().Single();
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.Null(carousel.PageTransition);

            box.SelectedItem = "CrossFade";
            Assert.IsType<CrossFade>(carousel.PageTransition);
            box.SelectedItem = "PageSlide";
            Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.IsType<PageSlide>(carousel.PageTransition).Duration);
            box.SelectedItem = "无";
            Assert.Null(carousel.PageTransition);
        }

        [AvaloniaFact]
        public void Command_bar_buttons_report_their_label_and_the_toggle_keeps_its_state()
        {
            var (window, _) = PageHelpers.Open<CommandBarPage>();
            Assert.Equal("最近点击：（无）", window.Text("ClickText"));

            window.Press("NewButton");
            Assert.Equal("最近点击：新建", window.Text("ClickText"));

            window.Press("BoldButton");
            Assert.Equal("最近点击：加粗", window.Text("ClickText"));
            Assert.True(window.ByName<CommandBarToggleButton>("BoldButton").IsChecked);
        }

        [AvaloniaFact]
        public void Label_box_moves_the_labels()
        {
            var (window, _) = PageHelpers.Open<CommandBarPage>();
            var bar = window.ByName<CommandBar>("Bar");

            window.ByName<ComboBox>("LabelBox").SelectedItem = CommandBarDefaultLabelPosition.Collapsed;

            Assert.Equal(CommandBarDefaultLabelPosition.Collapsed, bar.DefaultLabelPosition);
        }

        // The overflow count is posted at Loaded priority after layout, so the readout lags one RunJobs behind.
        [AvaloniaFact]
        public void Narrowing_the_bar_overflows_commands_only_when_dynamic_overflow_is_on()
        {
            var (window, _) = PageHelpers.Open<CommandBarPage>();
            var slider = window.ByName<Slider>("WidthSlider");
            void Settle()
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("IsDynamicOverflowEnabled = False，进入溢出菜单的主命令：0 个", window.Text("OverflowText"));

            slider.Value = 80;
            Settle();
            Assert.Equal("IsDynamicOverflowEnabled = False，进入溢出菜单的主命令：0 个", window.Text("OverflowText"));

            window.ByName<CheckBox>("OverflowCheck").IsChecked = true;
            Settle();
            Assert.Equal("IsDynamicOverflowEnabled = True，进入溢出菜单的主命令：4 个", window.Text("OverflowText"));

            slider.Value = 520;
            Settle();
            Assert.Equal("IsDynamicOverflowEnabled = True，进入溢出菜单的主命令：0 个", window.Text("OverflowText"));
        }

        [AvaloniaFact]
        public void Content_page_reports_bar_clicks_and_the_bottom_bar_can_be_removed_and_restored()
        {
            var (window, _) = PageHelpers.Open<ContentDemoPage>();
            // BottomCheck spans x = 650–841, past the right edge of the 600px window.
            window.Width = 900;
            var demo = window.GetVisualDescendants().OfType<ContentPage>().Single();
            var bottom = demo.BottomCommandBar;
            Assert.NotNull(bottom);

            window.Press("RefreshButton");
            Assert.Equal("最近点击：刷新", window.Text("ClickText"));
            window.Press("ReplyButton");
            Assert.Equal("最近点击：回复", window.Text("ClickText"));

            window.Click(window.ByName<CheckBox>("BottomCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.Null(demo.BottomCommandBar);
            Assert.DoesNotContain(window.GetVisualDescendants().OfType<CommandBarButton>(), b => b.Name == "ReplyButton");

            window.Click(window.ByName<CheckBox>("BottomCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.Same(bottom, demo.BottomCommandBar);
            Assert.Single(window.GetVisualDescendants().OfType<CommandBarButton>(), b => b.Name == "ReplyButton");
        }

        [AvaloniaFact]
        public void Alignment_boxes_place_the_content_inside_the_page()
        {
            var (window, _) = PageHelpers.Open<ContentDemoPage>();
            var demo = window.GetVisualDescendants().OfType<ContentPage>().Single();

            window.ByName<ComboBox>("HAlignBox").SelectedItem = HorizontalAlignment.Right;
            window.ByName<ComboBox>("VAlignBox").SelectedItem = VerticalAlignment.Top;

            Assert.Equal(HorizontalAlignment.Right, demo.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Top, demo.VerticalContentAlignment);
        }

        [AvaloniaFact]
        public void Open_box_opens_the_drawer_and_the_button_inside_closes_it()
        {
            var (window, _) = PageHelpers.Open<DrawerDemoPage>();
            Assert.Equal("IsOpen = False，DisplayMode = Overlay", window.Text("StateText"));
            Assert.Equal("Opened 0 次，Closed 0 次", window.Text("EventText"));

            window.Click(window.ByName<CheckBox>("OpenCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("IsOpen = True，DisplayMode = Overlay", window.Text("StateText"));
            Assert.Equal("Opened 1 次，Closed 0 次", window.Text("EventText"));

            window.Press("InDrawerButton");
            Assert.Equal("IsOpen = False，DisplayMode = Overlay", window.Text("StateText"));
            Assert.Equal("Opened 1 次，Closed 1 次", window.Text("EventText"));
            Assert.False(window.ByName<CheckBox>("OpenCheck").IsChecked);
        }

        // The box only mirrors the drawer: Locked opens it on its own and the page reads that back.
        [AvaloniaFact]
        public void Locked_behavior_opens_the_drawer_inline_and_ticks_the_box()
        {
            var (window, _) = PageHelpers.Open<DrawerDemoPage>();

            window.ByName<ComboBox>("BehaviorBox").SelectedItem = DrawerBehavior.Locked;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("IsOpen = True，DisplayMode = Inline", window.Text("StateText"));
            Assert.True(window.ByName<CheckBox>("OpenCheck").IsChecked);
        }

        // The theme fills in PageSlide only once the page is on screen; the page syncs the box on Loaded.
        [AvaloniaFact]
        public void Navigation_transition_box_starts_on_the_theme_default_and_can_switch_it_off()
        {
            var (window, _) = PageHelpers.Open<NavigationDemoPage>();
            var nav = window.GetVisualDescendants().OfType<NavigationPage>().Single();
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.IsType<PageSlide>(nav.PageTransition);
            Assert.Equal("PageSlide", box.SelectedItem);

            box.SelectedItem = "无";

            Assert.Null(nav.PageTransition);
            Assert.Null(nav.ModalTransition);
        }

        // Headless never reports PageSlide as finished, so switch the transitions off before navigating.
        [AvaloniaFact]
        public async Task Push_pop_and_modal_buttons_move_the_two_stacks()
        {
            var (window, _) = PageHelpers.Open<NavigationDemoPage>();
            window.ByName<ComboBox>("TransitionBox").SelectedItem = "无";
            Assert.Equal("StackDepth = 1，CanGoBack = False，ModalStack = 0", window.Text("StateText"));

            window.Press("PopButton");
            Assert.Equal("Pushed 0 次，Popped 0 次，ModalPushed 0 次，ModalPopped 0 次", window.Text("EventText"));
            window.Press("PushButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 2，CanGoBack = True，ModalStack = 0");
            window.Press("PushButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 3，CanGoBack = True，ModalStack = 0");

            window.Press("ModalButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 3，CanGoBack = True，ModalStack = 1");
            window.Press("CloseModalButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 3，CanGoBack = True，ModalStack = 0");

            window.Press("RootButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 1，CanGoBack = False，ModalStack = 0");
            // PopToRoot raises Popped once for each page it removes.
            Assert.Equal("Pushed 2 次，Popped 2 次，ModalPushed 1 次，ModalPopped 1 次", window.Text("EventText"));
        }

        [AvaloniaFact]
        public void Tabbed_page_buttons_change_the_selected_page_and_its_header()
        {
            var (window, _) = PageHelpers.Open<TabbedDemoPage>();
            Assert.Equal("SelectedIndex = 0，SelectedPage.Header = 收件箱", window.Text("StateText"));

            window.Press("NextButton");
            Assert.Equal("SelectedIndex = 1，SelectedPage.Header = 已发送", window.Text("StateText"));
            window.Press("NextButton");
            window.Press("NextButton");
            Assert.Equal("SelectedIndex = 2，SelectedPage.Header = 草稿", window.Text("StateText"));
            window.Press("PrevButton");
            Assert.Equal("SelectedIndex = 1，SelectedPage.Header = 已发送", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Tabbed_page_placement_and_gesture_controls_reach_the_page()
        {
            var (window, _) = PageHelpers.Open<TabbedDemoPage>();
            var tabbed = window.GetVisualDescendants().OfType<TabbedPage>().Single();
            Assert.False(tabbed.IsGestureEnabled);

            window.ByName<ComboBox>("PlacementBox").SelectedItem = TabPlacement.Bottom;
            window.Click(window.ByName<CheckBox>("GestureCheck"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(TabPlacement.Bottom, tabbed.TabPlacement);
            Assert.True(tabbed.IsGestureEnabled);
        }

        [AvaloniaFact]
        public void Code_can_select_the_disabled_tab_but_a_click_cannot()
        {
            var (window, _) = PageHelpers.Open<TabControlPage>();
            var tabs = window.ByName<TabControl>("Tabs");
            Assert.Equal("SelectedIndex = 0，选中的标签 = 概览", window.Text("StateText"));

            window.Click((Control)tabs.ContainerFromIndex(1)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedIndex = 0，选中的标签 = 概览", window.Text("StateText"));

            window.Press("DisabledButton");
            Assert.Equal("SelectedIndex = 1，选中的标签 = 详情", window.Text("StateText"));
        }

        // The template swap resets the selection to 0; the page posts the old index back after layout.
        [AvaloniaFact]
        public void Moving_the_strip_keeps_the_selected_tab()
        {
            var (window, _) = PageHelpers.Open<TabControlPage>();
            var tabs = window.ByName<TabControl>("Tabs");
            window.Click((Control)tabs.ContainerFromIndex(2)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedIndex = 2，选中的标签 = 设置", window.Text("StateText"));

            window.ByName<ComboBox>("PlacementBox").SelectedItem = Dock.Left;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Dock.Left, tabs.TabStripPlacement);
            Assert.Equal("SelectedIndex = 2，选中的标签 = 设置", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Tab_strip_selection_drives_the_page_content_line()
        {
            var (window, _) = PageHelpers.Open<TabStripPage>();
            var strip = window.ByName<TabStrip>("Strip");
            Assert.Equal("SelectedIndex = 0，当前显示「全部」的内容", window.Text("ContentText"));

            window.Click((Control)strip.ContainerFromIndex(1)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedIndex = 1，当前显示「未读」的内容", window.Text("ContentText"));

            window.Press("LastButton");
            Assert.Equal("SelectedIndex = 2，当前显示「已标星」的内容", window.Text("ContentText"));
        }

        // ---- Primitives ----

        [AvaloniaFact]
        public void Scroll_methods_move_the_value_and_raise_scroll_with_their_type()
        {
            var (window, _) = PageHelpers.Open<ScrollBarPage>();
            Assert.Equal("Value = 0（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 0 次，最近一次：（无）", window.Text("ScrollText"));

            window.Press("LineDownButton");
            Assert.Equal("Value = 1（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 1 次，最近一次：SmallIncrement", window.Text("ScrollText"));

            window.Press("PageDownButton");
            Assert.Equal("Value = 11（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 2 次，最近一次：LargeIncrement", window.Text("ScrollText"));

            window.Press("EndButton");
            Assert.Equal("Value = 100（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            // Avalonia reports ScrollToEnd as LargeIncrement, not EndScroll.
            Assert.Equal("Scroll 事件 3 次，最近一次：LargeIncrement", window.Text("ScrollText"));
            Assert.Equal(100, window.ByName<Slider>("ValueSlider").Value);

            window.Press("PageUpButton");
            Assert.Equal("Value = 90（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 4 次，最近一次：LargeDecrement", window.Text("ScrollText"));
        }

        [AvaloniaFact]
        public void Writing_value_through_the_slider_moves_the_bar_without_a_scroll_event()
        {
            var (window, _) = PageHelpers.Open<ScrollBarPage>();

            window.ByName<Slider>("ValueSlider").Value = 42;

            Assert.Equal(42, window.ByName<ScrollBar>("Bar").Value);
            Assert.Equal("Value = 42（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 0 次，最近一次：（无）", window.Text("ScrollText"));
        }

        [AvaloniaFact]
        public void Visibility_box_and_auto_hide_box_reach_the_scroll_bar()
        {
            var (window, _) = PageHelpers.Open<ScrollBarPage>();
            var bar = window.ByName<ScrollBar>("Bar");
            Assert.True(bar.AllowAutoHide);

            window.ByName<ComboBox>("VisibilityBox").SelectedItem = ScrollBarVisibility.Hidden;
            window.Click(window.ByName<CheckBox>("AutoHideCheck"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(ScrollBarVisibility.Hidden, bar.Visibility);
            Assert.False(bar.AllowAutoHide);
        }

        // The app runs Dark, so Default resolves to Dark here. The scope starts with Requested = Default, not null.
        [AvaloniaFact]
        public void Variant_box_sets_the_live_scope_and_default_follows_the_window()
        {
            var (window, _) = PageHelpers.Open<ThemeVariantScopePage>();
            var box = window.ByName<ComboBox>("VariantBox");
            Assert.Equal("Requested = Default，Actual = Dark", window.Text("VariantText"));
            Assert.Equal(ThemeVariant.Default, box.SelectedItem);

            box.SelectedItem = ThemeVariant.Light;
            Assert.Equal("Requested = Light，Actual = Light", window.Text("VariantText"));

            box.SelectedItem = ThemeVariant.Dark;
            Assert.Equal("Requested = Dark，Actual = Dark", window.Text("VariantText"));
            box.SelectedItem = ThemeVariant.Default;
            Assert.Equal("Requested = Default，Actual = Dark", window.Text("VariantText"));
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
        }

        [AvaloniaFact]
        public void Light_and_dark_scopes_resolve_the_same_resource_to_different_brushes()
        {
            var (window, _) = PageHelpers.Open<ThemeVariantScopePage>();

            var light = ((ISolidColorBrush)window.ByName<Border>("LightBorder").Background!).Color;
            var dark = ((ISolidColorBrush)window.ByName<Border>("DarkBorder").Background!).Color;

            Assert.NotEqual(light, dark);
        }

        [AvaloniaFact]
        public void Colour_box_and_title_box_change_the_live_swatch()
        {
            var (window, _) = PageHelpers.Open<UserControlPage>();
            var live = window.ByName<SwatchView>("Live");
            var titleBox = window.ByName<TextBox>("TitleBox");
            // Color.ToString() prints the known colour name, not the hex value.
            Assert.Equal("Live.Title = 可改的一块，Live.Fill = Gray", window.Text("StateText"));

            window.ByName<ComboBox>("ColorBox").SelectedItem = "Orange";
            Assert.Equal("Live.Title = 可改的一块，Live.Fill = Orange", window.Text("StateText"));
            Assert.Same(Brushes.Orange, live.GetVisualDescendants().OfType<Border>().First().Background);

            window.Type(titleBox, "样本");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Live.Title = 样本，Live.Fill = Orange", window.Text("StateText"));
            Assert.Equal("样本", live.GetVisualDescendants().OfType<TextBlock>().Single().Text);

            titleBox.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Live.Title = 可改的一块，Live.Fill = Orange", window.Text("StateText"));
        }

        // ---- Primitives: windows ----

        // Every test that opens windows closes the host in finally; the sample is owned, so it goes with it.
        [AvaloniaFact]
        public void Sample_window_opens_owned_and_the_close_button_closes_it()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                Assert.Equal("所有者的 OwnedWindows 数：0", window.Text("OwnerText"));
                Assert.Equal("示例窗口：未打开", window.Text("StateText"));

                window.Press("OpenButton");
                var sample = page.Sample!;
                Assert.Same(sample, Assert.Single(window.OwnedWindows));
                Assert.Equal("示例窗口", sample.Title);
                Assert.Equal("所有者的 OwnedWindows 数：1", window.Text("OwnerText"));
                Assert.StartsWith("示例窗口：IsVisible = True，WindowState = Normal，Position = ", window.Text("StateText"));
                Assert.EndsWith("大小 = 320×200，Closing 触发 0 次", window.Text("StateText"));

                window.Press("OpenButton");
                Assert.Single(window.OwnedWindows);

                window.Press("CloseButton");
                Assert.False(sample.IsVisible);
                Assert.Null(page.Sample);
                Assert.Equal("所有者的 OwnedWindows 数：0", window.Text("OwnerText"));
                Assert.Equal("示例窗口：已关闭（上次共触发 Closing 1 次）", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Blocking_keeps_the_sample_open_but_leaving_the_page_still_closes_it()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                window.Click(window.ByName<CheckBox>("BlockCheck"));
                window.Press("OpenButton");
                var sample = page.Sample!;

                window.Press("CloseButton");
                sample.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(sample.IsVisible);
                Assert.Equal(2, page.ClosingCount);
                Assert.EndsWith("Closing 触发 2 次", window.Text("StateText"));

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.False(sample.IsVisible);
                Assert.Null(page.Sample);
                Assert.Equal("示例窗口：已关闭（上次共触发 Closing 3 次）", page.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Property_boxes_apply_to_the_open_sample()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                window.Press("OpenButton");
                var sample = page.Sample!;

                window.ByName<CheckBox>("ResizeCheck").IsChecked = false;
                window.ByName<CheckBox>("TopmostCheck").IsChecked = true;
                window.ByName<CheckBox>("TaskbarCheck").IsChecked = false;
                window.ByName<ComboBox>("SizeBox").SelectedItem = SizeToContent.WidthAndHeight;
                window.ByName<ComboBox>("StateBox").SelectedItem = WindowState.Maximized;
                Dispatcher.UIThread.RunJobs();

                Assert.False(sample.CanResize);
                Assert.True(sample.Topmost);
                Assert.False(sample.ShowInTaskbar);
                Assert.Equal(SizeToContent.WidthAndHeight, sample.SizeToContent);
                Assert.Equal(WindowState.Maximized, sample.WindowState);
                Assert.Contains("WindowState = Maximized", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        // A title-bar minimise is simulated by setting WindowState on the sample itself.
        [AvaloniaFact]
        public void A_state_change_on_the_sample_is_mirrored_into_the_state_box()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                window.Press("OpenButton");
                var sample = page.Sample!;

                sample.WindowState = WindowState.Minimized;
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(WindowState.Minimized, window.ByName<ComboBox>("StateBox").SelectedItem);
                Assert.Equal(WindowState.Minimized, sample.WindowState);
                Assert.Contains("WindowState = Minimized", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Decoration_controls_apply_to_the_system_sample()
        {
            var (window, page) = PageHelpers.Open<WindowDrawnDecorationsPage>();
            try
            {
                Assert.Equal("示例窗口：未打开", window.Text("StateText"));

                window.Press("OpenSystemButton");
                var sample = page.Sample!;
                Assert.Same(sample, Assert.Single(window.OwnedWindows));
                Assert.Equal("系统装饰示例", sample.Title);
                Assert.StartsWith("WindowDecorations = Full，ExtendClientAreaToDecorationsHint = False，", window.Text("StateText"));

                window.ByName<ComboBox>("DecorationsBox").SelectedItem = WindowDecorations.BorderOnly;
                window.ByName<CheckBox>("ExtendCheck").IsChecked = true;
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(WindowDecorations.BorderOnly, sample.WindowDecorations);
                Assert.True(sample.ExtendClientAreaToDecorationsHint);
                Assert.StartsWith("WindowDecorations = BorderOnly，ExtendClientAreaToDecorationsHint = True，", window.Text("StateText"));

                window.Press("CloseButton");
                Assert.False(sample.IsVisible);
                Assert.Equal("示例窗口：未打开", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Custom_title_bar_window_replaces_the_system_sample_and_reads_back_its_roles()
        {
            var (window, page) = PageHelpers.Open<WindowDrawnDecorationsPage>();
            try
            {
                window.Press("OpenSystemButton");
                var system = page.Sample!;

                window.Press("OpenCustomButton");
                var custom = page.Sample!;
                Assert.False(system.IsVisible);
                Assert.Same(custom, Assert.Single(window.OwnedWindows, w => w.IsVisible));
                Assert.Equal(WindowDecorations.None, custom.WindowDecorations);
                Assert.Equal("已标记的角色（读回）：TitleBar、MinimizeButton、MaximizeButton、CloseButton", window.Text("RolesText"));

                // The decoration box only drives the system sample.
                window.ByName<ComboBox>("DecorationsBox").SelectedItem = WindowDecorations.Full;
                Assert.Equal(WindowDecorations.None, custom.WindowDecorations);

                window.Content = null;
                Dispatcher.UIThread.RunJobs();
                Assert.False(custom.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- System ----

        // The page installs its icon on Application while it is on screen. Headless has no tray, so only the
        // page's own bookkeeping is tested; the icons are put back in finally whatever happens.
        [AvaloniaFact]
        public void Tray_icon_is_installed_while_the_page_is_shown_and_removed_when_it_leaves()
        {
            var app = Application.Current!;
            var saved = TrayIcon.GetIcons(app);
            var (window, page) = PageHelpers.Open<TrayIconPage>();
            try
            {
                var tray = page.Tray!;
                Assert.Same(tray, Assert.Single(TrayIcon.GetIcons(app)!));
                Assert.Equal("托盘图标：已挂上，IsVisible = True，ToolTipText = ControlsDemo 托盘图标，菜单项 4 个，NativeMenuExporter = 无（当前平台没有原生托盘实现）", window.Text("StateText"));

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.Null(page.Tray);
                Assert.Same(saved, TrayIcon.GetIcons(app));
                Assert.Equal("托盘图标：未挂上（页面不在屏幕上）", page.Text("StateText"));
            }
            finally
            {
                window.Close();
                TrayIcon.SetIcons(app, saved);
            }
        }

        [AvaloniaFact]
        public void Visible_box_and_tooltip_box_update_the_tray_icon()
        {
            var app = Application.Current!;
            var saved = TrayIcon.GetIcons(app);
            var (window, page) = PageHelpers.Open<TrayIconPage>();
            try
            {
                var tray = page.Tray!;

                window.Click(window.ByName<CheckBox>("VisibleCheck"));
                window.ByName<TextBox>("TipBox").Text = "新提示";
                Dispatcher.UIThread.RunJobs();

                Assert.False(tray.IsVisible);
                Assert.Equal("新提示", tray.ToolTipText);
                Assert.StartsWith("托盘图标：已挂上，IsVisible = False，ToolTipText = 新提示，", window.Text("StateText"));
            }
            finally
            {
                window.Close();
                TrayIcon.SetIcons(app, saved);
            }
        }

        // No platform exporter runs the menu here, so invoke the items' commands the way an exporter would.
        [AvaloniaFact]
        public void Tray_menu_commands_are_counted()
        {
            var app = Application.Current!;
            var saved = TrayIcon.GetIcons(app);
            var (window, page) = PageHelpers.Open<TrayIconPage>();
            try
            {
                // NativeMenuItemSeparator is a NativeMenuItem too (Header "-"), so filter it out.
                var items = page.Tray!.Menu!.Items.OfType<NativeMenuItem>().Where(i => i is not NativeMenuItemSeparator).ToArray();
                Assert.Equal(new[] { "显示", "开机提示", "退出（演示用，不会真的退出）" }, items.Select(i => i.Header));
                Assert.Equal("菜单点击：显示 0 次，开机提示 0 次（IsChecked = True），退出 0 次；图标本身被点击 0 次（Clicked）", window.Text("MenuText"));

                items[0].Command!.Execute(null);
                items[2].Command!.Execute(null);
                items[2].Command!.Execute(null);

                Assert.Equal("菜单点击：显示 1 次，开机提示 0 次（IsChecked = True），退出 2 次；图标本身被点击 0 次（Clicked）", window.Text("MenuText"));
            }
            finally
            {
                window.Close();
                TrayIcon.SetIcons(app, saved);
            }
        }
    }
}
