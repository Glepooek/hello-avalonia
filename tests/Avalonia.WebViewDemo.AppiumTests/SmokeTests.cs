using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.WebViewDemo.AppiumTests
{
    // A1: the main window opens alone with its three buttons; the windows they open are covered by WebFlowTests.
    public class SmokeTests
    {
        private const string App = "Avalonia.WebViewDemo";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_three_buttons()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia WebView Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            Assert.True(app.ByName("1. NativeWebView (embedded in a Window)").Enabled);
            Assert.True(app.ByName("2. NativeWebDialog (standalone native window)").Enabled);
            Assert.True(app.ByName("3. Call JS from C# (open #1 first)").Enabled);
        }
    }
}
