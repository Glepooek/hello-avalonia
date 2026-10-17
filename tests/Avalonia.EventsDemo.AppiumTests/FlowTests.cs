using System.Collections.Generic;
using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.EventsDemo.AppiumTests
{
    // A3: a real mouse click and a real key press, delivered by the operating system, arrive as Avalonia input
    // events in order. Headless input never says Mouse because it never comes from a device.
    public class FlowTests
    {
        private const string App = "Avalonia.EventsDemo";

        [AppiumFact(App)]
        public void Real_mouse_and_key_events_reach_the_pad_in_order()
        {
            using var app = new AppSession(App);
            app.SelectTab("输入事件");

            // The pad is a Border, which has no UIA element; clicking the text inside it lands on the pad.
            app.ByName("点这里获得焦点，再按键").Click();
            AppSession.WaitFor(() => Log(app).Count == 3, "the click logged three events");
            var click = Log(app);
            Assert.Equal("01  PointerEntered", click[0]);
            // The press position depends on where the window opened, so the line is checked up to it.
            Assert.StartsWith("02  PointerPressed  Mouse  left=True  right=False  at ", click[1]);
            Assert.Equal("03  PointerReleased", click[2]);

            // An arrow key makes no character, so a Chinese IME lets it through unchanged.
            KeyChord.Press(VirtualKey.Right);
            AppSession.WaitFor(() => Log(app).Count == 5, "the key press logged two more events");
            Assert.Equal(new[] { "04  KeyDown  key=Right  modifiers=None", "05  KeyUp  key=Right" }, Log(app).Skip(3));
        }

        private static List<string> Log(AppSession app) =>
            app.ById("LogList").FindElementsByTagName("ListItem").Select(e => e.Text ?? "").ToList();
    }
}
