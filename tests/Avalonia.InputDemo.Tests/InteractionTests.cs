using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.InputDemo.ViewModels;
using Avalonia.InputDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.InputDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Control page, string name) => page.ByName<TextBlock>(name).Text!;

        private static Point CentreOf(Window window, Control control)
            => window.CenterOf(control);

        [AvaloniaFact]
        public void Click_handler_and_Tapped_add_one_even_while_the_command_is_locked()
        {
            var page = new InteractivityPage();
            var window = UiHelpers.Show(page);
            var vm = (InteractivityViewModel)page.DataContext!;

            window.Click(page.ButtonWithText("Click 处理器"));
            Assert.Equal(1, vm.Count);
            Assert.Equal("Click 处理器触发（不看 CanExecute）", Text(page, "HandlerReadout"));

            window.Click(page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Tapped 手势"));
            Assert.Equal(2, vm.Count);
            Assert.Equal("Tapped 触发（不看 CanExecute）", Text(page, "HandlerReadout"));
        }

        [AvaloniaFact]
        public void Command_button_ignores_clicks_while_locked_and_works_once_the_checkbox_unlocks_it()
        {
            var page = new InteractivityPage();
            var window = UiHelpers.Show(page);
            var vm = (InteractivityViewModel)page.DataContext!;
            var command = page.ButtonWithText("Command");

            // A command that cannot execute leaves IsEnabled alone; IsEffectivelyEnabled is the one that flips.
            Assert.False(command.IsEffectivelyEnabled);
            window.Click(command);
            Assert.Equal(0, vm.Count);

            window.Click(page.GetVisualDescendants().OfType<CheckBox>().Single());
            Dispatcher.UIThread.RunJobs();

            Assert.True(vm.Unlocked);
            Assert.True(command.IsEffectivelyEnabled);
            window.Click(command);
            Assert.Equal(1, vm.Count);
        }

        [AvaloniaFact]
        public void HotKey_runs_the_command_but_an_InputGesture_alone_does_not()
        {
            var page = new InteractivityPage();
            var window = UiHelpers.Show(page);
            var vm = (InteractivityViewModel)page.DataContext!;
            window.Click(page.GetVisualDescendants().OfType<CheckBox>().Single());
            Dispatcher.UIThread.RunJobs();

            // "只写 InputGesture" only displays the text; "只写 HotKey" and "两个都写" really bind the key.
            window.KeyPress(Key.D1, RawInputModifiers.Control, PhysicalKey.Digit1, "1");
            Assert.Equal(0, vm.Count);
            window.KeyPress(Key.D2, RawInputModifiers.Control, PhysicalKey.Digit2, "2");
            Assert.Equal(1, vm.Count);
            window.KeyPress(Key.D3, RawInputModifiers.Control, PhysicalKey.Digit3, "3");
            Assert.Equal(2, vm.Count);
        }

        [AvaloniaFact]
        public void Keyboard_readout_shows_the_key_the_physical_key_and_the_modifiers()
        {
            var page = new KeyboardPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();

            window.KeyPress(Key.A, RawInputModifiers.Shift, PhysicalKey.A, "A");

            Assert.Equal("Key=A  PhysicalKey=A  Modifiers=Shift  KeySymbol='A'", Text(page, "KeyReadout"));
        }

        [AvaloniaFact]
        public void Ctrl_Shift_K_fires_the_key_binding_and_Ctrl_K_alone_does_not()
        {
            var page = new KeyboardPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();
            Assert.Equal("Ctrl+Shift+K 触发了 0 次", Text(page, "BindReadout"));

            window.KeyPress(Key.K, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.K, "K");
            window.KeyPress(Key.K, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.K, "K");
            window.KeyPress(Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");

            Assert.Equal("Ctrl+Shift+K 触发了 2 次", Text(page, "BindReadout"));
        }

        [AvaloniaFact]
        public void Ctrl_S_hotkey_clicks_the_save_button()
        {
            var page = new KeyboardPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();
            Assert.Equal("保存了 0 次", Text(page, "SaveReadout"));

            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");

            Assert.Equal("保存了 2 次", Text(page, "SaveReadout"));
        }

        [AvaloniaFact]
        public void Pointer_readout_follows_move_press_and_wheel()
        {
            var page = new PointerPage();
            var window = UiHelpers.Show(page);
            var centre = CentreOf(window, page.ByName<Border>("Pad"));

            window.MouseMove(centre);
            Assert.StartsWith("Moved", Text(page, "Readout"));
            Assert.Contains("L=False", Text(page, "Readout"));

            window.MouseDown(centre, MouseButton.Left);
            Assert.StartsWith("Pressed", Text(page, "Readout"));
            Assert.Contains("L=True", Text(page, "Readout"));

            window.MouseUp(centre, MouseButton.Left);
            window.MouseWheel(centre, new Vector(0, 1));
            // The vector's own text depends on the culture, so only the prefix is pinned.
            Assert.StartsWith("Wheel  delta=", Text(page, "Readout"));
        }

        [AvaloniaFact]
        public void Dragging_the_handle_keeps_following_the_pointer_and_clamps_at_both_ends_of_the_track()
        {
            var page = new PointerPage();
            var window = UiHelpers.Show(page);
            var handle = page.ByName<Border>("Handle");
            var track = page.ByName<Canvas>("Track");
            Point OnTrack(double x) => track.PointIn(new Point(x, 25), window);

            window.MouseDown(CentreOf(window, handle), MouseButton.Left);
            Assert.Equal("拖动中（已捕获）", Text(page, "DragState"));

            // The handle is 40 wide and grabbed at its centre, so it sits 20 to the left of the pointer.
            window.MouseMove(OnTrack(150));
            Assert.Equal(130, Canvas.GetLeft(handle));

            // The pointer is captured, so moving far outside the track still drives the handle, clamped to 0..260.
            window.MouseMove(OnTrack(900));
            Assert.Equal(260, Canvas.GetLeft(handle));
            window.MouseMove(OnTrack(-900));
            Assert.Equal(0, Canvas.GetLeft(handle));

            window.MouseUp(OnTrack(-900), MouseButton.Left);
            Assert.Equal("空闲（捕获已释放）", Text(page, "DragState"));
        }

        [AvaloniaFact]
        public void Tab_skips_the_button_that_is_not_a_tab_stop()
        {
            var page = new FocusPage();
            var window = UiHelpers.Show(page);
            page.ByName<Button>("BtnA").Focus();
            Assert.StartsWith("焦点在 A", Text(page, "FocusReadout"));

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.StartsWith("焦点在 B  via Tab", Text(page, "FocusReadout"));

            // C has IsTabStop=False, so the next stop is D.
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.StartsWith("焦点在 D  via Tab", Text(page, "FocusReadout"));
        }

        [AvaloniaFact]
        public void Focus_via_Tab_shows_the_focus_ring_but_focus_via_pointer_does_not()
        {
            var page = new FocusPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ButtonWithText("Focus(Tab) → B"));
            var viaTab = Text(page, "FocusReadout");
            window.Click(page.ButtonWithText("Focus(Pointer) → B"));
            var viaPointer = Text(page, "FocusReadout");

            Assert.Contains("via Tab", viaTab);
            Assert.Contains(":focus-visible", viaTab);
            Assert.Contains("via Pointer", viaPointer);
            Assert.DoesNotContain(":focus-visible", viaPointer);
        }

        [AvaloniaFact]
        public void Query_button_reports_the_element_that_has_focus()
        {
            var page = new FocusPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ButtonWithText("读取 FocusManager 当前焦点"));

            // The clicked button has no name, so the readout falls back to its type name.
            Assert.Equal("FocusManager.GetFocusedElement() = Button", Text(page, "QueryReadout"));
        }

        [AvaloniaFact]
        public void Gestures_page_counts_tap_double_tap_and_right_tap_separately_and_resets()
        {
            var page = new GesturesPage();
            var window = UiHelpers.Show(page);
            var pad = page.ByName<Border>("Pad");
            var centre = CentreOf(window, pad);
            const string holding = "\nHolding: Started=0 Completed=0 Canceled=0";

            window.Click(pad);
            Assert.Equal("Tapped=1  DoubleTapped=0  RightTapped=0" + holding, Text(page, "Counts"));

            // A second press-release at the same spot straight away is the double tap, and it replaces the second Tapped.
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Assert.Equal("Tapped=1  DoubleTapped=1  RightTapped=0" + holding, Text(page, "Counts"));

            window.MouseDown(centre, MouseButton.Right);
            window.MouseUp(centre, MouseButton.Right);
            Assert.Equal("Tapped=1  DoubleTapped=1  RightTapped=1" + holding, Text(page, "Counts"));

            window.Click(page.ButtonWithText("清零"));
            Assert.Equal("Tapped=0  DoubleTapped=0  RightTapped=0" + holding, Text(page, "Counts"));
        }

        [AvaloniaFact]
        public void Digits_box_rejects_letters_typed_one_character_at_a_time()
        {
            var page = new TextInputPage();
            var window = UiHelpers.Show(page);
            var box = page.ByName<TextBox>("DigitsBox");

            // One character per TextInput event: a whole "a1b2" is a single event and is rejected as a unit.
            foreach (var ch in "a1b2")
            {
                window.Type(box, ch.ToString());
            }

            Assert.Equal("12", box.Text);
        }

        [AvaloniaFact]
        public void Options_readout_shows_the_attached_text_input_options()
        {
            var page = new TextInputPage();
            UiHelpers.Show(page);

            var readout = Text(page, "OptionsReadout");

            Assert.Contains("ContentType=Email", readout);
            Assert.Contains("ReturnKeyType=Send", readout);
            Assert.Contains("ContentType=Digits", readout);
            Assert.Contains("IsSensitive=True", readout);
        }
    }
}
