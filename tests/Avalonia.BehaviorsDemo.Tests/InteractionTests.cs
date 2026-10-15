using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
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

namespace Avalonia.BehaviorsDemo.Tests
{
    public class InteractionTests
    {
        private static (Window Window, TPage Page, TViewModel Vm) Open<TPage, TViewModel>()
            where TPage : UserControl, new()
        {
            var page = new TPage();
            var window = UiHelpers.Show(page);
            Dispatcher.UIThread.RunJobs();
            return (window, page, (TViewModel)page.DataContext!);
        }

        private static string Text(Window window, string name) => window.ByName<TextBlock>(name).Text ?? "";

        private static void Press(Window window, string name)
        {
            window.Click(window.ByName<Control>(name));
            Dispatcher.UIThread.RunJobs();
        }

        // Focus first: the key goes to the focused element, and the triggers on this page listen on the TextBox.
        private static void SendKey(Window window, Control target, Key key, PhysicalKey physical,
            RawInputModifiers modifiers = RawInputModifiers.None, string? symbol = null)
        {
            target.Focus();
            Dispatcher.UIThread.RunJobs();
            window.KeyPress(key, modifiers, physical, symbol);
            window.KeyRelease(key, modifiers, physical, symbol);
            Dispatcher.UIThread.RunJobs();
        }

        private static Point Center(Window window, Control control)
            => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        // The drag behaviors read the button state from the move itself: a MouseMove without LeftMouseButton is a
        // hover, and nothing moves.
        private static void Drag(Window window, Point from, Point to, RawInputModifiers held = RawInputModifiers.LeftMouseButton)
        {
            window.MouseDown(from, MouseButton.Left);
            for (var i = 1; i <= 10; i++)
            {
                window.MouseMove(new Point(from.X + (to.X - from.X) * i / 10, from.Y + (to.Y - from.Y) * i / 10), held);
                Dispatcher.UIThread.RunJobs();
            }
            window.MouseUp(to, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        private static IInputElement? Focused(Window window) => window.FocusManager!.GetFocusedElement();

        // ---- 机制 ----

        [AvaloniaFact]
        public void One_click_runs_the_trigger_actions_in_declaration_order()
        {
            var (window, _, vm) = Open<MechanismPage, MechanismViewModel>();
            Assert.Equal("Interaction.GetBehaviors(RunButton) 读到 1 个：EventTriggerBehavior", Text(window, "CodeReadout"));

            Press(window, "RunButton");

            Assert.Equal(new[] { "01  ① Trigger 收到 Click", "02  ③ 第二个 InvokeCommandAction 紧跟着执行", "03  -- 第 1 次触发结束 --" },
                vm.Log.Entries);
            Assert.Equal("② ChangePropertyAction 改写了这行文字", Text(window, "Readout"));
        }

        [AvaloniaFact]
        public void A_disabled_trigger_runs_none_of_its_actions()
        {
            var (window, _, vm) = Open<MechanismPage, MechanismViewModel>();
            vm.BehaviorEnabled = false;
            Dispatcher.UIThread.RunJobs();

            Press(window, "RunButton");

            Assert.Empty(vm.Log.Entries);
            Assert.Equal(0, vm.Runs);
            Assert.Equal("（尚未触发）", Text(window, "Readout"));
        }

        // ---- 事件转命令 ----

        [AvaloniaFact]
        public void Event_triggers_pass_parameters_event_args_and_call_methods()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            Press(window, "AddOneButton");
            Press(window, "AddTenButton");
            Press(window, "AddHundredButton");
            Press(window, "PressBorder");
            Press(window, "HelloButton");
            Press(window, "SourceButton");

            Assert.Equal(111, vm.Total);
            Assert.Equal("PointerPressedEventArgs", vm.LastArgs);
            Assert.Equal(1, vm.HelloCount);
            // The trigger sits on ListenerBlock but listens to SourceButton through SourceObject.
            Assert.Equal(1, vm.SourceHits);
        }

        [AvaloniaFact]
        public void Throttle_runs_the_first_click_and_drops_the_rest_of_the_burst()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            for (var i = 0; i < 3; i++)
                Press(window, "ThrottleButton");

            Assert.Equal(3, vm.ThrottleAttempts);
            Assert.Equal(1, vm.ThrottleRuns);
        }

        // "Nothing yet" alone would also pass if the debounce never ran, so wait for the run that must follow.
        [AvaloniaFact]
        public async Task Debounce_runs_once_after_the_burst_goes_quiet()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            for (var i = 0; i < 3; i++)
                Press(window, "DebounceButton");
            Assert.Equal(3, vm.DebounceAttempts);
            Assert.Equal(0, vm.DebounceRuns);

            await UiHelpers.WaitUntil(() => vm.DebounceRuns == 1);
        }

        // The page's trap: a bare DelayAction does not hold back the actions after it; inside AsyncActionGroup it does.
        [AvaloniaFact]
        public async Task Delay_only_waits_inside_an_async_action_group()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            Press(window, "BareDelayButton");
            Assert.Equal(2, vm.Log.Entries.Count);
            Assert.EndsWith("A 前", vm.Log.Entries[0]);
            Assert.EndsWith("A 后（没有等）", vm.Log.Entries[1]);

            vm.ClearLogCommand.Execute(null);
            Press(window, "GroupDelayButton");
            Assert.EndsWith("B 前", Assert.Single(vm.Log.Entries));

            await UiHelpers.WaitUntil(() => vm.Log.Entries.Count == 2);
            Assert.EndsWith("B 后（等了 0.8 秒）", vm.Log.Entries[1]);
        }

        // ---- 数据驱动 ----

        [AvaloniaFact]
        public void Data_triggers_follow_the_view_model()
        {
            var (window, _, vm) = Open<DataTriggerPage, DataTriggerViewModel>();
            Assert.Equal("在线", Text(window, "StatusText"));
            Assert.Equal(Colors.LimeGreen, ((ISolidColorBrush)window.ByName<TextBlock>("StatusText").Foreground!).Color);
            Assert.Equal("不及格", Text(window, "GradeText"));
            Assert.Equal("条件未满足", Text(window, "PassText"));
            // ConditionalBehavior ignores the initial value, unlike DataTriggerBehavior.
            Assert.Equal("（初始值不触发）", Text(window, "FlagText"));

            vm.IsOnline = false;
            vm.Score = 60;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("离线", Text(window, "StatusText"));
            Assert.Equal("及格", Text(window, "GradeText"));
            Assert.Equal("条件未满足", Text(window, "PassText"));

            vm.Agreed = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("可以提交", Text(window, "PassText"));

            vm.Score = 59;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("不及格", Text(window, "GradeText"));
            Assert.Equal("条件未满足", Text(window, "PassText"));

            vm.Flag = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Flag 变成了 True", Text(window, "FlagText"));
            vm.Flag = false;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Flag 变成了 False", Text(window, "FlagText"));
        }

        // The one fixed wait in this batch: "stops at five" is a claim about the absence of a sixth tick, and no
        // condition can wait for something that must not happen. Two more tick intervals (300 ms each) is the margin.
        [AvaloniaFact]
        public async Task Timer_trigger_stops_after_its_total_ticks()
        {
            var (_, _, vm) = Open<DataTriggerPage, DataTriggerViewModel>();

            await UiHelpers.WaitUntil(() => vm.TimerTicks == 5);
            await Task.Delay(700);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(5, vm.TimerTicks);
        }

        [AvaloniaFact]
        public async Task Task_completed_trigger_fires_again_for_every_new_task()
        {
            var (window, _, vm) = Open<DataTriggerPage, DataTriggerViewModel>();

            Press(window, "StartWorkButton");
            Assert.Equal("运行中", vm.WorkStatus);
            await UiHelpers.WaitUntil(() => vm.DoneCount == 1);
            Assert.Equal("已完成", vm.WorkStatus);

            Press(window, "StartWorkButton");
            await UiHelpers.WaitUntil(() => vm.DoneCount == 2);
        }

        // ---- 焦点键盘指针 ----

        [AvaloniaFact]
        public void Focus_behaviors_move_focus_and_report_it_back_two_way()
        {
            var (window, _, vm) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();
            Assert.Same(window.ByName<TextBox>("AutoBox"), Focused(window));

            vm.WantFocus = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(window.ByName<TextBox>("FocusTarget"), Focused(window));

            var select = window.ByName<TextBox>("SelectBox");
            select.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();
            Assert.False(vm.WantFocus);
            Assert.Equal(0, select.SelectionStart);
            Assert.Equal(select.Text!.Length, select.SelectionEnd);
        }

        // TextBox handles Enter itself, so a Direct-routed KeyTrigger never sees it; Tunnel sees it first.
        [AvaloniaFact]
        public void Key_triggers_need_tunnel_routing_to_see_keys_a_text_box_handles()
        {
            var (window, _, vm) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();

            SendKey(window, window.ByName<TextBox>("NoRouteBox"), Key.Enter, PhysicalKey.Enter);
            SendKey(window, window.ByName<TextBox>("TunnelBox"), Key.Enter, PhysicalKey.Enter);
            SendKey(window, window.ByName<TextBox>("GestureBox"), Key.S, PhysicalKey.S, RawInputModifiers.Control, "s");
            SendKey(window, window.ByName<TextBox>("GestureBox"), Key.F5, PhysicalKey.F5);

            Assert.Equal(0, vm.EnterDirectCount);
            Assert.Equal(1, vm.EnterTunnelCount);
            Assert.Equal(1, vm.SaveCount);
            Assert.Equal(1, vm.F5Count);
        }

        [AvaloniaFact]
        public void Enter_drops_focus_and_tab_cannot_leave_the_focus_trap()
        {
            var (window, _, _) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();

            SendKey(window, window.ByName<TextBox>("LoseBox"), Key.Enter, PhysicalKey.Enter);
            Assert.Null(Focused(window));

            SendKey(window, window.ByName<TextBox>("Trap2"), Key.Tab, PhysicalKey.Tab);
            Assert.Same(window.ByName<TextBox>("Trap1"), Focused(window));
        }

        // The page's caption says it: with the default routing the pressed trigger fires twice for one press.
        [AvaloniaFact]
        public void Default_routed_pointer_trigger_fires_twice_for_one_press()
        {
            var (window, _, vm) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();

            Press(window, "PressDefaultBorder");
            Press(window, "PressBubbleBorder");
            Press(window, "TapBorder");

            Assert.Equal(2, vm.PressDefaultCount);
            Assert.Equal(1, vm.PressBubbleCount);
            Assert.Equal(1, vm.TapCount);
        }

        // ---- 表单验证 ----

        // The validation behavior joins its messages with Environment.NewLine, which is \r\n on Windows.
        private static string Lines(params string[] messages) => string.Join(Environment.NewLine, messages);

        [AvaloniaFact]
        public void Validation_rules_report_every_failure_and_gate_the_submit_button()
        {
            var (window, _, vm) = Open<ValidationPage, ValidationViewModel>();
            var username = window.ByName<TextBox>("UsernameBox");
            var email = window.ByName<TextBox>("EmailBox");
            var submit = window.ByName<Button>("SubmitButton");
            Assert.False(vm.UsernameValid);
            Assert.Equal(Lines("用户名不能为空", "至少 3 个字符", "只能包含字母、数字、下划线"), vm.UsernameError);
            Assert.Equal(Lines("邮箱不能为空", "邮箱格式不对"), vm.EmailError);
            // A command button stays IsEnabled while CanExecute is false; IsEffectivelyEnabled is what the user sees.
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(username, "ab");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("至少 3 个字符", vm.UsernameError);
            window.Type(username, "!");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("只能包含字母、数字、下划线", vm.UsernameError);
            username.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Lines("用户名不能为空", "至少 3 个字符"), vm.UsernameError);

            window.Type(username, "abc_1");
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.UsernameValid);
            Assert.Null(vm.UsernameError);
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(email, "a@b");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("邮箱格式不对", vm.EmailError);
            window.Type(email, ".co");
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.EmailValid);
            Assert.True(submit.IsEffectivelyEnabled);

            window.Click(submit);
            Assert.Equal("已提交：两个字段都通过了校验", vm.SubmitResult);
        }

        // ---- 集合 ----

        // Review Focus 1: this is the first click on a freshly shown CollectionPage, the case where MultiList
        // scrolls the page after Show. With the old Click it silently missed and the fruit count stayed at 3.
        [AvaloniaFact]
        public void The_first_click_on_the_page_adds_an_item_and_clear_empties_the_list()
        {
            var (window, _, vm) = Open<CollectionPage, CollectionViewModel>();

            Press(window, "AddFruitButton");
            Assert.Equal(new[] { "苹果", "香蕉", "橙子", "葡萄" }, vm.Fruits);

            Press(window, "ClearFruitsButton");
            Assert.Empty(vm.Fruits);
        }

        [AvaloniaFact]
        public void A_delete_button_inside_the_template_removes_its_own_item()
        {
            var (window, _, vm) = Open<CollectionPage, CollectionViewModel>();
            var deletes = window.ByName<ListBox>("TodoList").GetVisualDescendants().OfType<Button>()
                .Where(b => "删除".Equals(b.Content)).ToList();
            Assert.Equal(3, deletes.Count);

            window.Click(deletes[1]);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "写计划", "提交" }, vm.Todos);

            window.Click(window.ButtonWithText("恢复三项"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(3, vm.Todos.Count);
        }

        // Virtualization is the proof: item 150 only gets a container once it has really been scrolled into view.
        [AvaloniaFact]
        public void Scroll_to_item_brings_a_virtualized_item_into_view()
        {
            var (window, _, _) = Open<CollectionPage, CollectionViewModel>();
            var big = window.ByName<ListBox>("BigList");
            Assert.Null(big.ContainerFromIndex(150));

            Press(window, "ScrollButton");
            window.UpdateLayout();

            Assert.True(big.GetVisualDescendants().OfType<ScrollViewer>().First().Offset.Y > 0);
            Assert.NotNull(big.ContainerFromIndex(150));
            Assert.Null(big.ContainerFromIndex(0));
        }

        [AvaloniaFact]
        public void Ctrl_a_selects_every_item_of_the_multi_select_list()
        {
            var (window, _, _) = Open<CollectionPage, CollectionViewModel>();
            var multi = window.ByName<ListBox>("MultiList");

            window.Click(multi.ContainerFromIndex(0)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Single(multi.SelectedItems!);
            window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
            window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(8, multi.SelectedItems!.Count);
        }

        // ---- 窗口弹出通知 ----

        // The opener buttons sit in rows wider than the 600px test window; widen it so the clicks land (batch 3,
        // CustomPanelPage). The probe ran at 1000.
        [AvaloniaFact]
        public void Show_actions_open_a_flyout_a_popup_and_a_tooltip_on_other_controls()
        {
            var (window, _, _) = Open<WindowPopupPage, object>();
            window.Width = 1000;
            Dispatcher.UIThread.RunJobs();
            var flyout = FlyoutBase.GetAttachedFlyout(window.ByName<Border>("FlyoutAnchor"))!;
            var popup = window.ByName<Popup>("DemoPopup");
            var tipTarget = window.ByName<Border>("TipTarget");
            Assert.False(flyout.IsOpen);
            Assert.False(popup.IsOpen);
            Assert.False(ToolTip.GetIsOpen(tipTarget));

            Press(window, "FlyoutOpener");
            Assert.True(flyout.IsOpen);
            flyout.Hide();
            Press(window, "PopupOpener");
            Assert.True(popup.IsOpen);
            popup.Close();
            Press(window, "TipOpener");
            Assert.True(ToolTip.GetIsOpen(tipTarget));
            ToolTip.SetIsOpen(tipTarget, false);
        }

        [AvaloniaFact]
        public async Task Notification_buttons_show_one_card_each_with_its_type()
        {
            var (window, _, _) = Open<WindowPopupPage, object>();
            window.Width = 1000;
            NotificationCard[] Cards() => window.GetVisualDescendants().OfType<NotificationCard>().ToArray();
            Assert.Empty(Cards());

            foreach (var name in new[] { "SuccessButton", "InfoButton", "WarningButton", "ErrorButton" })
                Press(window, name);
            await UiHelpers.WaitUntil(() => Cards().Length == 4);

            Assert.Equal(
                new[] { NotificationType.Success, NotificationType.Information, NotificationType.Warning, NotificationType.Error },
                Cards().Select(c => c.NotificationType));

            // The unbound action has no NotificationManager to show through, so it must add nothing.
            Press(window, "UnboundButton");
            Assert.Equal(4, Cards().Length);
        }

        [AvaloniaFact]
        public void Close_window_action_closes_only_the_child_window()
        {
            var (window, page, _) = Open<WindowPopupPage, object>();
            try
            {
                Press(window, "OpenChildButton");
                var child = page.Child!;
                Assert.True(child.IsVisible);
                Assert.Same(window, child.Owner);

                child.Click(child.ByName<Button>("CloseChildButton"));
                Dispatcher.UIThread.RunJobs();

                Assert.False(child.IsVisible);
                Assert.Empty(window.OwnedWindows);
                Assert.True(window.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- 拖拽 ----

        [AvaloniaFact]
        public void Drag_behaviors_ignore_moves_without_the_left_button_held()
        {
            var (window, _, vm) = Open<DragPage, DragViewModel>();
            var chip = window.ByName<Border>("CanvasChip");
            var from = Center(window, chip);

            Drag(window, from, new Point(from.X + 60, from.Y + 40), held: RawInputModifiers.None);

            Assert.Equal(10, Canvas.GetLeft(chip));
            Assert.Equal(10, Canvas.GetTop(chip));
            Assert.Equal("A → B → C → D", vm.OrderText);
        }

        // Canvas drag moves Canvas.Left/Top; MouseDragElementBehavior leaves them alone and adds a TranslateTransform.
        [AvaloniaFact]
        public void Dragging_a_chip_moves_it_by_the_pointer_delta()
        {
            var (window, _, _) = Open<DragPage, DragViewModel>();
            var canvasChip = window.ByName<Border>("CanvasChip");
            var from = Center(window, canvasChip);
            Drag(window, from, new Point(from.X + 60, from.Y + 40));
            Assert.Equal(70, Canvas.GetLeft(canvasChip));
            Assert.Equal(50, Canvas.GetTop(canvasChip));

            var mouseChip = window.ByName<Border>("MouseChip");
            mouseChip.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            from = Center(window, mouseChip);
            Drag(window, from, new Point(from.X + 60, from.Y + 40));
            Assert.Equal(10, Canvas.GetLeft(mouseChip));
            var shift = Assert.IsType<TranslateTransform>(mouseChip.RenderTransform);
            Assert.Equal(60, shift.X);
            Assert.Equal(40, shift.Y);
        }

        [AvaloniaFact]
        public void Dragging_a_list_item_past_another_reorders_the_source_collection()
        {
            var (window, _, vm) = Open<DragPage, DragViewModel>();
            var list = window.ByName<ListBox>("OrderList");
            list.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var first = Center(window, list.ContainerFromIndex(0)!);
            var third = Center(window, list.ContainerFromIndex(2)!);

            Drag(window, first, new Point(first.X, third.Y + 4));

            Assert.Equal("B → C → A → D", vm.OrderText);
        }

        // ---- 响应式 ----

        [AvaloniaFact]
        public void Adaptive_and_aspect_ratio_setters_switch_classes_as_the_width_changes()
        {
            var (window, _, vm) = Open<ResponsivePage, ResponsiveViewModel>();
            var adaptive = window.ByName<Border>("AdaptiveBox");
            var ratio = window.ByName<Border>("RatioBox");

            foreach (var (width, size, shape) in new[]
            {
                (360.0, "wide", "flat"), (200.0, "narrow", "flat"), (300.0, "wide", "flat"), (100.0, "narrow", "flat"), (60.0, "narrow", "tall"),
            })
            {
                vm.PanelWidth = width;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.True(adaptive.Classes.Contains(size), $"width {width}: AdaptiveBox classes are [{string.Join(",", adaptive.Classes)}]");
                Assert.True(ratio.Classes.Contains(shape), $"width {width}: RatioBox classes are [{string.Join(",", ratio.Classes)}]");
            }
            Assert.Equal("窄：宽度小于 300", Text(window, "AdaptiveLabel"));
            Assert.Equal("高：宽高比小于 1", Text(window, "RatioLabel"));
        }

        // ---- 文件系统 ----

        // The view model wipes %TEMP%\avalonia-behaviors-demo\files on construction, so this test always starts empty
        // (and would also wipe the sandbox of a running copy of the app). Watcher counts are timing-dependent
        // (a new file raises one or two events), so only "at least one" is asserted.
        [AvaloniaFact]
        public async Task File_actions_stay_in_the_sandbox_and_the_watcher_sees_the_write()
        {
            var (window, _, vm) = Open<FileSystemPage, FileSystemViewModel>();
            Assert.Equal("（沙箱是空的）", vm.Listing);

            Press(window, "WriteButton");
            Assert.False(File.Exists(vm.FilePath));

            Press(window, "MkdirButton");
            Assert.Equal("notes", vm.Listing);

            Press(window, "WriteButton");
            Assert.Equal("你好，Behaviors\n", File.ReadAllText(vm.FilePath));
            await UiHelpers.WaitUntil(() => vm.WatchHits >= 1);
            Assert.Equal("notes\n" + Path.Combine("notes", "hello.txt"), vm.Listing);

            Press(window, "AppendButton");
            Assert.Equal("你好，Behaviors\n你好，Behaviors\n", File.ReadAllText(vm.FilePath));

            Press(window, "DeleteFileButton");
            Press(window, "DeleteDirButton");
            Assert.False(Directory.Exists(vm.SubDir));
        }

        // ---- 网络 ----

        // The page talks to its own loopback server, so this needs no network access.
        [AvaloniaFact]
        public async Task Http_request_action_posts_and_gets_from_the_loopback_server()
        {
            var (window, _, _) = Open<NetworkPage, NetworkViewModel>();
            Assert.Equal("状态码 0", Text(window, "PostStatus"));

            Press(window, "PostButton");
            await UiHelpers.WaitUntil(() => Text(window, "PostBody") != "");
            Assert.Equal("pong:ping", Text(window, "PostBody"));
            Assert.Equal("状态码 201", Text(window, "PostStatus"));

            Press(window, "GetButton");
            await UiHelpers.WaitUntil(() => Text(window, "GetBody") != "");
            Assert.Equal("pong", Text(window, "GetBody"));
            Assert.Equal("状态码 200", Text(window, "GetStatus"));
        }

        // ---- 剪贴板 ----

        // The headless clipboard is an in-memory stub that lives for the whole test process, so start by clearing it.
        [AvaloniaFact]
        public async Task Clipboard_actions_copy_read_and_clear()
        {
            var (window, _, vm) = Open<ClipboardPage, ClipboardViewModel>();
            Press(window, "ClearButton");

            Press(window, "CopyButton");
            Press(window, "PasteButton");
            await UiHelpers.WaitUntil(() => vm.Pasted == vm.Input);

            Press(window, "ClearButton");
            Press(window, "PasteButton");
            await UiHelpers.WaitUntil(() => vm.Pasted == "");
        }

        // ---- 截图 ----

        [AvaloniaFact]
        public async Task Render_target_bitmap_behavior_redraws_into_the_image_on_demand()
        {
            var (window, _, vm) = Open<ScreenshotPage, ScreenshotViewModel>();
            await UiHelpers.WaitUntil(() => vm.Renderer.Calls >= 1);
            Assert.IsType<RenderTargetBitmap>(window.ByName<Image>("PreviewImage").Source);
            var calls = vm.Renderer.Calls;
            var before = window.CaptureFrame();

            Press(window, "RerenderButton");
            await UiHelpers.WaitUntil(() => vm.Renderer.Calls > calls);

            // The renderer changes colour on every call, so a real redraw changes pixels.
            Assert.NotEqual(before, window.CaptureFrame());
        }

        // ---- 自定义 ----

        [AvaloniaFact]
        public void Upper_case_behavior_rewrites_typed_and_assigned_text()
        {
            var (window, _, _) = Open<CustomPage, CustomViewModel>();
            var box = window.ByName<TextBox>("UpperBox");

            window.Type(box, "abc");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("ABC", box.Text);

            box.Text = "hello";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("HELLO", box.Text);
        }

        [AvaloniaFact]
        public void Click_count_trigger_fires_on_every_second_press()
        {
            var (window, _, vm) = Open<CustomPage, CustomViewModel>();

            Press(window, "ClickPad");
            Assert.Equal(0, vm.Fires);
            Assert.Equal("星星：", Text(window, "Output"));

            Press(window, "ClickPad");
            Assert.Equal(1, vm.Fires);
            Assert.Equal("星星：★", Text(window, "Output"));

            for (var i = 0; i < 3; i++)
                Press(window, "ClickPad");
            Assert.Equal(2, vm.Fires);
            Assert.Equal("星星：★★", Text(window, "Output"));
        }
    }
}
