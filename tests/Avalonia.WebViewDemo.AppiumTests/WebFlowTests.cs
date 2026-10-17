using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Appium.Common;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.WebViewDemo.AppiumTests
{
    // A3: what only a real process can show. The web window really opens with the local test page, and the bridge
    // works both ways. The page text is read only as proof of these effects; the page itself is not under test.
    public class WebFlowTests
    {
        private const string App = "Avalonia.WebViewDemo";
        private const string OpenWebView = "1. NativeWebView (embedded in a Window)";
        private const string OpenWebDialog = "2. NativeWebDialog (standalone native window)";
        private const string CallJs = "3. Call JS from C# (open #1 first)";
        private const string PageGreeting = "Hello NativeWebView！以下测试JS调用C#方法！";

        // Batch 9 measured the first page load at about 1.7 s; WebView2 can start slower on a cold machine.
        private const int PageLoadTimeoutMs = 15000;

        [AppiumFact(App)]
        public void Web_window_shows_the_page_and_the_page_can_call_back_into_the_app()
        {
            using var app = new AppSession(App);
            var web = OpenWebWindow(app);

            PageButton(web, "检查更新").Click();
            AppSession.WaitFor(() => Texts(web).Any(t => t.StartsWith("Already up to date (")), "the page shows the app's reply");

            PageButton(web, "关闭窗口").Click();
            AppSession.WaitFor(() => app.FindWindow("WebViewWindow") is null, "the app closed the web window");
        }

        [AppiumFact(App)]
        public void Main_window_button_calls_into_the_page()
        {
            using var app = new AppSession(App);
            var web = OpenWebWindow(app);

            // The web window opens centred over its smaller owner and covers button 3, and an owned window always
            // stays above its owner, so a click there would land on the page. The page's own button minimizes it.
            PageButton(web, "最小化窗口").Click();
            var handle = new IntPtr(int.Parse(web.GetAttribute("NativeWindowHandle")));
            AppSession.WaitFor(() => IsIconic(handle), "the web window is minimized");

            app.ByName(CallJs).Click();
            AppSession.WaitFor(() => Texts(web).Contains("C#调用JS: 12 + 13 = 25"), "the page shows the result of the call");
        }

        [AppiumFact(App)]
        public void Second_button_opens_a_native_web_dialog()
        {
            using var app = new AppSession(App);

            app.ByName(OpenWebDialog).Click();

            // The dialog loads a public web site; only the window is checked, so the test needs no network.
            AppSession.WaitFor(() => app.FindWindow("NativeWebDialog Demo") is not null, "the NativeWebDialog window is open");
        }

        // The web window is owned by the main window, so it is found by title rather than in WindowHandles.
        private static WindowsElement OpenWebWindow(AppSession app)
        {
            app.ByName(OpenWebView).Click();
            WindowsElement? web = null;
            AppSession.WaitFor(() => (web = app.FindWindow("WebViewWindow")) is not null, "the web window is open");
            AppSession.WaitFor(() => Texts(web!).Contains(PageGreeting), "the test page has loaded", PageLoadTimeoutMs);
            return web!;
        }

        // WebView2 exposes the page's tree twice under the window, so every page button is found twice; batch 9
        // clicked the first one. Filtering on the type keeps a same-named element of another kind out.
        private static AppiumWebElement PageButton(WindowsElement web, string name) =>
            web.FindElementsByName(name).First(e => e.TagName == "ControlType.Button");

        private static List<string> Texts(WindowsElement window) =>
            window.FindElementsByTagName("Text").Select(e => e.Text ?? "").ToList();

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}
