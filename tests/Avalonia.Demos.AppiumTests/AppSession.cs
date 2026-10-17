using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Diagnostics;
using System.Threading;

namespace Avalonia.Demos.AppiumTests
{
    // One WinAppDriver session for one demo app. The app starts on first use of Driver, so a test that is skipped
    // never launches anything; Dispose ends the session and the app with it.
    public sealed class AppSession : IDisposable
    {
        public static readonly Uri DriverUri = new("http://127.0.0.1:4723");

        // Batch 7 measured every wait after a click or keystroke at under 300 ms; 3 s leaves room for a busy machine.
        public const int DefaultTimeoutMs = 3000;

        private readonly string _app;
        private WindowsDriver<WindowsElement>? _driver;

        public AppSession(string app) => _app = app;

        public WindowsDriver<WindowsElement> Driver => _driver ??= Start();

        public WindowsElement ById(string automationId) => Driver.FindElementByAccessibilityId(automationId);

        public WindowsElement ByName(string name) => Driver.FindElementByName(name);

        // Clicks the box to focus it, then types. See UnicodeKeyboard for why WinAppDriver's SendKeys is not used.
        public void TypeInto(WindowsElement box, string text)
        {
            box.Click();
            UnicodeKeyboard.Type(text);
        }

        // UIA can lag behind an action, so every check after a click or keystroke polls instead of reading once.
        public void WaitForText(string automationId, string expected, int timeoutMs = DefaultTimeoutMs)
        {
            string? last = null;
            if (!Poll(() => (last = ById(automationId).Text ?? "") == expected, timeoutMs))
                throw new TimeoutException($"'{automationId}' still reads '{last}' after {timeoutMs} ms; expected '{expected}'.");
        }

        public static void WaitFor(Func<bool> condition, string what, int timeoutMs = DefaultTimeoutMs)
        {
            if (!Poll(condition, timeoutMs))
                throw new TimeoutException($"Not true after {timeoutMs} ms: {what}.");
        }

        private static bool Poll(Func<bool> condition, int timeoutMs)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    if (condition())
                        return true;
                }
                catch (WebDriverException)
                {
                    // The element may not exist yet; keep polling until the timeout.
                }
                if (watch.ElapsedMilliseconds > timeoutMs)
                    return false;
                Thread.Sleep(100);
            }
        }

        private WindowsDriver<WindowsElement> Start()
        {
            var exe = AppPaths.FindExe(_app) ?? throw new InvalidOperationException($"{_app}.exe is not built.");
            var options = new AppiumOptions();
            options.AddAdditionalCapability("app", exe);
            options.AddAdditionalCapability("platformName", "Windows");
            options.AddAdditionalCapability("deviceName", "WindowsPC");
            return new WindowsDriver<WindowsElement>(DriverUri, options);
        }

        // Ending the session also closes the app WinAppDriver launched (batch 7 checked: no CloseApp needed).
        public void Dispose()
        {
            _driver?.Quit();
            _driver = null;
        }
    }
}
