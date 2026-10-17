using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Appium.Common
{
    // One WinAppDriver session for one demo app. The app starts on first use of Driver, so a test that is skipped
    // never launches anything; Dispose ends the session and the app with it.
    public sealed class AppSession : IDisposable
    {
        public static readonly Uri DriverUri = new("http://127.0.0.1:4723");

        // Batch 7 measured every wait after a click or keystroke at under 300 ms; 3 s leaves room for a busy machine.
        public const int DefaultTimeoutMs = 3000;

        // Each app has its own Appium test project, and `dotnet test` on the solution runs test projects in parallel
        // processes. They all drive the one desktop, so a session holds this machine-wide lock from start to Dispose.
        private const string DesktopLockName = @"Local\HelloAvalonia.Appium";
        private static readonly TimeSpan DesktopLockTimeout = TimeSpan.FromMinutes(10);

        // How long Dispose waits for the app to exit after Quit; batch 8 and 9 saw apps take a few seconds.
        private const int QuitGraceMs = 5000;

        private readonly string _app;
        private WindowsDriver<WindowsElement>? _driver;
        private Process? _process;
        private Mutex? _desktopLock;

        public AppSession(string app) => _app = app;

        public WindowsDriver<WindowsElement> Driver => _driver ??= Start();

        // True when the app was still running after Quit and its grace period, and Dispose had to kill it.
        public bool KilledAfterQuit { get; private set; }

        public WindowsElement ById(string automationId) => Driver.FindElementByAccessibilityId(automationId);

        public WindowsElement ByName(string name) => Driver.FindElementByName(name);

        // WindowHandles leaves out owned windows and dialogs (batch 9), but the session finds them by title.
        public WindowsElement? FindWindow(string title) =>
            Driver.FindElementsByName(title).FirstOrDefault(w => w.TagName == "ControlType.Window");

        // Picked from the tab strip, not by name, so text on a page can never be clicked instead.
        public void SelectTab(string header)
        {
            var tab = Driver.FindElementsByTagName("TabItem").Single(t => t.Text == header);
            tab.Click();
            WaitFor(() => tab.Selected, $"tab {header} is selected");
        }

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
            AcquireDesktop();
            try
            {
                var options = new AppiumOptions();
                options.AddAdditionalCapability("app", exe);
                options.AddAdditionalCapability("platformName", "Windows");
                options.AddAdditionalCapability("deviceName", "WindowsPC");
                var driver = new WindowsDriver<WindowsElement>(DriverUri, options);
                _process = ProcessOf(driver);
                return driver;
            }
            catch
            {
                ReleaseDesktop();
                throw;
            }
        }

        // A Mutex belongs to the thread that took it; the tests are synchronous, so Start and Dispose share a thread.
        private void AcquireDesktop()
        {
            var mutex = new Mutex(false, DesktopLockName);
            try
            {
                if (!mutex.WaitOne(DesktopLockTimeout))
                    throw new TimeoutException($"Another Appium test held the desktop for over {DesktopLockTimeout.TotalMinutes} minutes.");
            }
            catch (AbandonedMutexException)
            {
                // A test process was killed mid-session; the lock is ours now.
            }
            catch
            {
                mutex.Dispose();
                throw;
            }
            _desktopLock = mutex;
        }

        private void ReleaseDesktop()
        {
            _desktopLock?.ReleaseMutex();
            _desktopLock?.Dispose();
            _desktopLock = null;
        }

        // Ending the session also closes the app WinAppDriver launched (batch 7 checked: no CloseApp needed). But
        // in batch 10 a test that failed with a Windows file dialog open left the app running after Quit, and the
        // next build could not replace its exe; so an app still running after the grace period is killed.
        public void Dispose()
        {
            try
            {
                _driver?.Quit();
            }
            finally
            {
                _driver = null;
                EndApp();
                ReleaseDesktop();
            }
        }

        private void EndApp()
        {
            if (_process is null)
                return;
            if (!_process.WaitForExit(QuitGraceMs))
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit();
                KilledAfterQuit = true;
            }
            _process.Dispose();
            _process = null;
        }

        // WinAppDriver names the app's main window by its handle as a hex string.
        private static Process ProcessOf(WindowsDriver<WindowsElement> driver)
        {
            var window = new IntPtr(Convert.ToInt64(driver.CurrentWindowHandle, 16));
            GetWindowThreadProcessId(window, out var processId);
            return Process.GetProcessById((int)processId);
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
