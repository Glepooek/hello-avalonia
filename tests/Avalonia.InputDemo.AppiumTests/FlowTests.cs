using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.InputDemo.AppiumTests
{
    // A3: real keyboard input from the operating system, which the Headless tests only simulate inside Avalonia.
    public class FlowTests
    {
        private const string App = "Avalonia.InputDemo";

        [AppiumFact(App)]
        public void Tab_skips_the_button_that_is_no_tab_stop_and_only_keyboard_focus_is_visible()
        {
            using var app = new AppSession(App);
            app.SelectTab("焦点");

            // The readout lists the button's pseudo-classes; :pointerover depends on where the mouse rests, so
            // only the start of the readout and :focus-visible are checked.
            app.ById("BtnA").Click();
            WaitForFocus(app, "焦点在 A  via Pointer", visible: false);

            KeyChord.Press(VirtualKey.Tab);
            WaitForFocus(app, "焦点在 B  via Tab", visible: true);

            // C has IsTabStop=False, so Tab goes from B straight to D.
            KeyChord.Press(VirtualKey.Tab);
            WaitForFocus(app, "焦点在 D  via Tab", visible: true);

            KeyChord.Press(VirtualKey.Shift, VirtualKey.Tab);
            WaitForFocus(app, "焦点在 B  via Tab", visible: true);
        }

        [AppiumFact(App)]
        public void Button_hot_key_fires_from_a_real_key_chord()
        {
            using var app = new AppSession(App);
            app.SelectTab("键盘与 HotKey");

            app.ByName("保存（Ctrl+S）").Click();
            app.WaitForText("SaveReadout", "保存了 1 次");

            KeyChord.Press(VirtualKey.Control, VirtualKey.S);
            app.WaitForText("SaveReadout", "保存了 2 次");

            // The page's Ctrl+Shift+K key binding is left to the Headless tests: Ctrl+Shift is also the Windows
            // hot key that switches between installed input methods, and in batch 10 the chord fired the binding
            // in one run and not in the next.
        }

        private static void WaitForFocus(AppSession app, string start, bool visible) =>
            AppSession.WaitFor(() =>
            {
                var readout = app.ById("FocusReadout").Text ?? "";
                return readout.StartsWith(start) && readout.Contains(":focus-visible") == visible;
            }, $"focus readout starts with '{start}' and {(visible ? "shows" : "has no")} :focus-visible");
    }
}
