# 演示测试 10：其余应用的 Appium A3 流程 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 spec 批 10：给 InputDemo、EventsDemo、ServicesDemo、AppDevelopmentDemo、MusicStore 各加一个 `FlowTests.cs`，用真实进程验证 Headless 做不到的事——操作系统送来的键鼠输入、Windows 文件对话框、UIA 树里的自动化属性、弹出的子窗口。

**Architecture:** 测试放进批 8、9 已有的 `Avalonia.<X>.AppiumTests` 项目，不新建项目。
- 共享库 `Avalonia.Appium.Common` 新增三样：`AppSession.SelectTab`、`AppSession.FindWindow`（批 9 的"按标题找有所有者的窗口"）、`KeyChord`（经 `SendInput` 按虚拟键）。`UnicodeKeyboard` 与 `KeyChord` 共用新的内部类 `NativeInput`。
- `src/` 只改一处：给 MusicStore 只有图标的商店按钮加 `AutomationProperties.Name`（使用者 2026-10-17 同意）。

**Tech Stack:** .NET 10.0、Appium.WebDriver 4.4.5（Selenium 3）、WinAppDriver 1.2.1、xUnit v3（3.2.2）、Win32 `SendInput`

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（「Appium 层」A3 的候选、「批 9 实测结论」的"对批 10 有用的发现"）

## Global Constraints

- 新写的注释用**英文**；断言里的界面文字按原文。
- 本批**不新增包**，`Directory.Packages.props` 不改；**不新建项目**，`hello-avalonia.slnx` 不改。
- 运行门控、等待规则沿用批 7–9：`[AppiumFact(App)]`；一律 `AppSession.WaitFor`/`WaitForText`，不 `Thread.Sleep`。
- **键入文字**仍用 `AppSession.TypeInto`；**按键**（Tab、方向键、Escape、Ctrl 组合键）用 `KeyChord.Press`，不用 WinAppDriver 的 `Keyboard.SendKeys`（它已过时，且本批实测送不到方向键）。
- **不按 Ctrl+Shift 组合键**：本机装了两个中文输入法，Ctrl+Shift 是 Windows 切换输入法的热键，含它的组合键这次能触发、下次不能，还可能改掉使用者的输入法。InputDemo 的 Ctrl+Shift+K KeyBinding 留给 Headless（已有 `Ctrl_Shift_K_fires_the_key_binding_and_Ctrl_K_alone_does_not`）。
- 测试**不读写任何文件**：文件对话框只打开再用 Escape 取消；MusicStore 不在搜索框里键入，所以不联网。
- 断言不依赖 Windows 的显示语言：对话框用 Escape 取消，不点按系统语言命名的"取消"；MusicStore 不断言本地化的"购买专辑"。
- `src/` 只改 `src/Avalonia.MusicStore/Views/MainWindow.axaml` 一行；破坏检查临时改的行用备份还原（该文件有未提交的改动，不能 `git checkout`），之后 `dotnet build hello-avalonia.slnx`。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`。提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（本批是 `C:\Temp\probe-b10\`），**永不提交**；探针里指向仓库 `src` 的目录联接用完只删联接本身（`cmd /c rmdir`），绝不递归删除。
- 此前的 523 个 Headless 测试始终保持通过；不设开关时所有 Appium 用例只允许"跳过"。
- 实跑 Appium 前先提醒使用者：运行期间不要动鼠标键盘、不要锁屏。

## Review Focus

按"最可能让使用者踩到"的顺序排列；每条在所属任务里都有对应测试或检查。

1. **测试改掉使用者的输入法**：Ctrl+Shift 在装了多个输入法的机器上会切换输入法。本批不按任何 Ctrl+Shift 组合键（Task 2；`KeyChord` 的注释写明原因）。
2. **按键"按了但没到"**：WinAppDriver 的 `Keyboard.SendKeys` 送得到 Tab、Ctrl+S，送不到方向键（EventsDemo 的事件流里没有 KeyDown）。所有按键走 `KeyChord`（Task 1；Task 2 的 EventsDemo 用例即是回归测试）。
3. **点不到不在 UIA 树里的元素**：EventsDemo 的 `Pad` 是 `Border`，没有 UIA 元素，按 AutomationId 找不到。点它里面的 `TextBlock`，点击照样落在 Pad 上（Task 2）。
4. **系统对话框：打开慢，失败时还会留下来**：首次打开文件对话框实测 4.6 s，等待上限 15 s（Task 3）。测试在对话框开着时失败，`Quit` 关不掉应用，`AppSession.Dispose` 兜底结束进程（Task 1；破坏检查 S1 后确认没有残留进程）。
5. **只有图标的按钮找不到**：MusicStore 商店按钮的 UIA 名字原本是 `Avalonia.Controls.PathIcon`。加 `AutomationProperties.Name` 后按名字找；破坏检查 M1 证明去掉它测试就失败（Task 4）。

---

## 已核实的事实（探针 `C:\Temp\probe-b10\`，2026-10-17，两轮，外加测试本身的实跑）

- **InputDemo 焦点页**：按 `Name` 找得到 `BtnA`（`ControlType.Button`）。点 A → `焦点在 A  via Pointer  [:pointerover :focus-within :focus]`；Tab → B（`via Tab`，带 `:focus-visible`）；再 Tab → D（跳过 `IsTabStop=False` 的 C）；Shift+Tab → B。每步约 200 ms。`SendKeys` 与 `SendInput` 两种方式都通过。
- **InputDemo 键盘页**：HotKey Ctrl+S 两种方式都使 `保存了 N 次` 加一。Ctrl+Shift+K：
  - 焦点在输入框里时，`KeyReadout` 读到 `Key=ImeProcessed  Modifiers=Control, Shift`——搜狗拼音截走了 Shift+字母；Ctrl+K 则读到 `Key=K`。
  - 探针里焦点在按钮上时触发了 1 次；同样写进测试后，连续实跑两次都是 0 次。本机 `zh-Hans-CN` 下装有搜狗拼音（`{E7EA138E…}`）与微软拼音（`{81D4E9C9…}`）两个输入法。"每按一次就切换一次输入法"**没有直接验证**（验证本身要再切一次使用者的输入法）。
- **EventsDemo 输入事件页**：`Pad` 按 AutomationId 找不到（0 个），`LogList`、`Entry`、`清空` 都找得到。点 Pad 里的文字 `点这里获得焦点，再按键` → 日志依次为 `01  PointerEntered`、`02  PointerPressed  Mouse  left=True  right=False  at 413, 44`、`03  PointerReleased`；`SendInput` Right → `04  KeyDown  key=Right  modifiers=None`、`05  KeyUp  key=Right`。改用 `SendKeys` ArrowRight 时 3 s 内没有新日志。
- **ServicesDemo 文件对话框页**：点"打开文本文件"后约 4.6 s 出现标题为 `打开文本文件` 的 `Window`；点"保存文本文件"后约 2.7 s 出现 `保存文件`。两种取消都约 0.3 s 内显示结果：`已取消（OpenFilePickerAsync 返回空列表）`、`已取消（StorageFile 为 null）`；之后会话里只剩主窗口。
- **AppDevelopmentDemo 无障碍页**：`ok-btn` 为 `ControlType.Button`，UIA 名字 `Confirm`（不是内容 `确定`，按名字找 `确定` 得 0 个），`HelpText` 为 `Confirms the form`。点"读取 Peer"后约 0.2 s，页面显示 `ButtonAutomationPeer`、`Name：Confirm`、`AutomationId：ok-btn`、`HelpText：Confirms the form`、`ControlType：Button`。
- **MusicStore**：加 `AutomationProperties.Name="Music Store"` 后按名字找到 1 个 `ControlType.Button`；点它约 0.8 s 出现 `MusicStoreWindow`。窗口里 1 个 `Edit`、1 个 `List`；按钮有 Avalonia 自绘的 `Fullscreen`、`Minimize`、`Maximize`、`Close`，另有原生标题栏的 `最小化`、`最大化`、`关闭`（`关闭` 为禁用）。Escape 关不掉窗口。

---

### Task 1: 共享库——选标签页、找窗口、按键、结束残留的应用

**Files:**
- Create: `tests/Avalonia.Appium.Common/NativeInput.cs`、`tests/Avalonia.Appium.Common/KeyChord.cs`
- Modify:
  - `tests/Avalonia.Appium.Common/UnicodeKeyboard.cs`：改用 `NativeInput`，公开 API 不变。
  - `tests/Avalonia.Appium.Common/AppSession.cs`：加 `FindWindow`、`SelectTab`；启动时记下被测进程，`Dispose` 在 `Quit` 后等 5 s，进程还在就连同子进程结束，并记在 `KilledAfterQuit`。
  - `tests/Avalonia.TestingDemo.AppiumTests/TestingDemoTests.cs`：`Closing_the_session_ends_the_app` 加断言 `KilledAfterQuit` 为假，让它验证的仍是 `Quit` 本身。
  - `tests/Avalonia.WebViewDemo.AppiumTests/WebFlowTests.cs`：三处按标题找窗口改用 `FindWindow`。

**Interfaces:**
- Consumes: 批 7 的 `AppSession`（`Driver`、`WaitFor`）。
- Produces（Task 2–4 使用）:
  - `WindowsElement? AppSession.FindWindow(string title)`：会话里标题为 `title` 的 `ControlType.Window`，没有则 `null`。
  - `void AppSession.SelectTab(string header)`：从标签条按 `Text` 选中标签页并等到 `Selected`。
  - `enum VirtualKey : ushort { Tab, Shift, Control, Escape, Right, S }`、`static void KeyChord.Press(params VirtualKey[] keys)`：按下的顺序即参数顺序，松开时倒序。
  - `bool AppSession.KilledAfterQuit`：`Dispose` 是否不得不结束进程。

`TabbedApp.AssertEveryTabShowsItsPage` 里有一段相同的选标签页代码，它还要拿 `tab` 元素做别的事，本批不动它。

为什么要兜底：破坏检查 S1 让测试在"打开"对话框还开着时失败，`Quit` 之后 ServicesDemo 仍在运行，下一条检查构建时 exe 被它锁住（MSB3027）。对话框留在桌面上，还会抢走后续测试的焦点。

- [ ] **Step 1: 写共享库**

#### `tests/Avalonia.Appium.Common/NativeInput.cs`

```csharp
using System;
using System.Runtime.InteropServices;

namespace Avalonia.Appium.Common
{
    // The Win32 SendInput call that UnicodeKeyboard and KeyChord share. Input goes to whatever window has keyboard
    // focus, which during a test is the app WinAppDriver launched and clicked.
    internal static class NativeInput
    {
        public const uint KeyEventKeyUp = 0x0002;
        public const uint KeyEventUnicode = 0x0004;

        public static Input Key(ushort virtualKey, ushort scan, uint flags) => new()
        {
            Type = 1, // INPUT_KEYBOARD
            Union = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = virtualKey, Scan = scan, Flags = flags } },
        };

        public static void Send(Input[] inputs)
        {
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
                throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Length} events (error {Marshal.GetLastWin32Error()}).");
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            public uint Type;
            public InputUnion Union;
        }

        // Sized by MouseInput, the largest member, so Marshal.SizeOf<Input>() matches what SendInput expects.
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput Mouse;
            [FieldOffset(0)] public KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }
    }
}
```

#### `tests/Avalonia.Appium.Common/KeyChord.cs`

```csharp
using System.Linq;

namespace Avalonia.Appium.Common
{
    // The keys the tests press. Values are Win32 virtual-key codes.
    public enum VirtualKey : ushort
    {
        Tab = 0x09,
        Shift = 0x10,
        Control = 0x11,
        Escape = 0x1B,
        Right = 0x27,
        S = 0x53,
    }

    // Presses a key or chord as virtual keys through SendInput: the keys go down in order and come up in reverse.
    // In batch 10 WinAppDriver's Keyboard.SendKeys pressed Tab and Ctrl+S, but its Right arrow left no KeyDown in
    // the event log, while SendInput delivered it. Avoid Ctrl+Shift chords: Windows also uses Ctrl+Shift to switch
    // between installed input methods, and a chord with it worked in one run and not the next.
    public static class KeyChord
    {
        public static void Press(params VirtualKey[] keys)
        {
            var down = keys.Select(k => NativeInput.Key((ushort)k, 0, 0));
            var up = keys.Reverse().Select(k => NativeInput.Key((ushort)k, 0, NativeInput.KeyEventKeyUp));
            NativeInput.Send(down.Concat(up).ToArray());
        }
    }
}
```

#### `tests/Avalonia.Appium.Common/UnicodeKeyboard.cs`

```csharp
namespace Avalonia.Appium.Common
{
    // Types text into whatever has keyboard focus as Unicode packets (KEYEVENTF_UNICODE). WinAppDriver's SendKeys
    // sends virtual keys, which a Chinese IME (Sogou or Microsoft Pinyin) turns into pinyin candidates: "abc3"
    // arrives as "安保处". Unicode packets reach the window as WM_CHAR and skip the IME's composition.
    public static class UnicodeKeyboard
    {
        public static void Type(string text)
        {
            var inputs = new NativeInput.Input[text.Length * 2];
            for (var i = 0; i < text.Length; i++)
            {
                inputs[2 * i] = NativeInput.Key(0, text[i], NativeInput.KeyEventUnicode);
                inputs[2 * i + 1] = NativeInput.Key(0, text[i], NativeInput.KeyEventUnicode | NativeInput.KeyEventKeyUp);
            }
            NativeInput.Send(inputs);
        }
    }
}
```

#### `tests/Avalonia.Appium.Common/AppSession.cs`

```csharp
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
```

#### `tests/Avalonia.TestingDemo.AppiumTests/TestingDemoTests.cs`

```csharp
using System.Diagnostics;
using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.TestingDemo.AppiumTests
{
    // Batch 7 baseline: TestingDemo has an AutomationId on every control it tests, so these tests show the shape
    // every later Appium test follows. Each test starts its own app, so no state carries from one test to the next.
    public class TestingDemoTests
    {
        private const string App = "Avalonia.TestingDemo";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_three_tabs()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia Testing Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            Assert.Equal(new[] { "计数器", "表单", "列表" }, app.Driver.FindElementsByTagName("TabItem").Select(t => t.Text));
        }

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_panel()
        {
            using var app = new AppSession(App);

            foreach (var (header, control) in new[] { ("表单", "name-box"), ("列表", "items-list"), ("计数器", "count-text") })
            {
                var tab = app.ByName(header);
                tab.Click();
                AppSession.WaitFor(() => tab.Selected, $"tab {header} is selected");
                AppSession.WaitFor(() => app.ById(control).Displayed, $"{control} is shown on tab {header}");
            }
        }

        [AppiumFact(App)]
        public void Closing_the_session_ends_the_app()
        {
            var before = Process.GetProcessesByName(App).Select(p => p.Id).ToHashSet();
            var app = new AppSession(App);
            _ = app.Driver.Title;
            var started = Assert.Single(Process.GetProcessesByName(App), p => !before.Contains(p.Id));

            app.Dispose();

            // Dispose kills an app that outlives Quit; this test is about Quit, so the kill must not have been needed.
            Assert.False(app.KilledAfterQuit);
            AppSession.WaitFor(() => started.HasExited, $"process {started.Id} has exited");
        }

        [AppiumFact(App)]
        public void Counter_buttons_change_the_count_and_which_buttons_are_enabled()
        {
            using var app = new AppSession(App);
            Assert.Equal("0", app.ById("count-text").Text);
            Assert.False(app.ById("decrement-btn").Enabled);
            Assert.False(app.ById("reset-btn").Enabled);

            app.ById("increment-btn").Click();
            app.ById("increment-btn").Click();
            app.WaitForText("count-text", "2");
            AppSession.WaitFor(() => app.ById("decrement-btn").Enabled, "decrement is enabled above zero");

            app.ById("decrement-btn").Click();
            app.WaitForText("count-text", "1");

            app.ById("reset-btn").Click();
            app.WaitForText("count-text", "0");
            AppSession.WaitFor(() => !app.ById("reset-btn").Enabled, "reset is disabled at zero");
        }

        [AppiumFact(App)]
        public void Form_checks_typed_input_and_submits_once_both_fields_are_valid()
        {
            using var app = new AppSession(App);
            app.ByName("表单").Click();
            Assert.False(app.ById("submit-btn").Enabled);

            app.TypeInto(app.ById("name-box"), "张");
            app.WaitForText("name-error", "姓名至少 2 个字符");
            app.TypeInto(app.ById("name-box"), "三");
            app.WaitForText("name-error", "");

            var age = app.ById("age-box");
            app.TypeInto(age, "abc");
            app.WaitForText("age-error", "年龄必须是整数");
            age.Clear();
            app.TypeInto(age, "30");
            app.WaitForText("age-error", "");
            AppSession.WaitFor(() => app.ById("submit-btn").Enabled, "submit is enabled with valid input");

            app.ById("submit-btn").Click();
            app.WaitForText("status-text", "已提交：张三，30 岁");
        }

        [AppiumFact(App)]
        public void List_adds_typed_items_and_removes_the_selected_one()
        {
            using var app = new AppSession(App);
            app.ByName("列表").Click();
            app.WaitForText("summary-text", "共 0 项");
            Assert.False(app.ById("add-btn").Enabled);

            foreach (var item in new[] { "apple", "pear" })
            {
                app.TypeInto(app.ById("new-item-box"), item);
                AppSession.WaitFor(() => app.ById("add-btn").Enabled, $"add is enabled after typing {item}");
                app.ById("add-btn").Click();
                app.WaitForText("new-item-box", "");
            }
            app.WaitForText("summary-text", "共 2 项");
            var list = app.ById("items-list");
            Assert.Equal(new[] { "apple", "pear" }, list.FindElementsByTagName("ListItem").Select(i => i.Text));

            list.FindElementByName("apple").Click();
            AppSession.WaitFor(() => app.ById("remove-btn").Enabled, "remove is enabled with a selection");
            app.ById("remove-btn").Click();

            app.WaitForText("summary-text", "共 1 项");
            AppSession.WaitFor(() => list.FindElementsByName("apple").Count == 0, "apple is gone from the list");
            AppSession.WaitFor(() => !app.ById("remove-btn").Enabled, "remove is disabled without a selection");
        }
    }
}
```

#### `tests/Avalonia.WebViewDemo.AppiumTests/WebFlowTests.cs`

```csharp
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
```

- [ ] **Step 2: 构建并回归**

Run: `dotnet build tests/Avalonia.Appium.Common`，再 `dotnet build tests/Avalonia.TestingDemo.AppiumTests` 与 `dotnet build tests/Avalonia.WebViewDemo.AppiumTests`
Expected: 0 个错误、0 个警告。

先提醒使用者，再实跑用到 `UnicodeKeyboard` 与 `FindWindow` 的两个项目（PowerShell）：`$env:HELLO_AVALONIA_APPIUM='1'; dotnet test tests/Avalonia.TestingDemo.AppiumTests --no-build; dotnet test tests/Avalonia.WebViewDemo.AppiumTests --no-build`
Expected: `通过: 6`、`通过: 4`。

### Task 2: InputDemo 与 EventsDemo——真实键鼠

**Files:**
- Create: `tests/Avalonia.InputDemo.AppiumTests/FlowTests.cs`、`tests/Avalonia.EventsDemo.AppiumTests/FlowTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `SelectTab`、`KeyChord.Press`；批 7 的 `ById`、`ByName`、`WaitFor`、`WaitForText`。
- Produces: 无。

三个用例：
1. `Tab_skips_the_button_that_is_no_tab_stop_and_only_keyboard_focus_is_visible`：点 A（`via Pointer`，没有 `:focus-visible`）→ Tab 到 B → Tab 跳过 C 到 D → Shift+Tab 回 B（都是 `via Tab`，带 `:focus-visible`）。读数只比开头与 `:focus-visible`：`:pointerover` 随鼠标停的位置变化。
2. `Button_hot_key_fires_from_a_real_key_chord`：点"保存"计 1 次，Ctrl+S 计 2 次。
3. `Real_mouse_and_key_events_reach_the_pad_in_order`：点 Pad 里的文字 → 三条指针事件（`Mouse`、左键）；按 Right → KeyDown、KeyUp。按下坐标随窗口位置变化，只比到 `at ` 为止。

- [ ] **Step 1: 写两个测试文件**

#### `tests/Avalonia.InputDemo.AppiumTests/FlowTests.cs`

```csharp
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
```

#### `tests/Avalonia.EventsDemo.AppiumTests/FlowTests.cs`

```csharp
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
```

- [ ] **Step 2: 构建、跳过、实跑**

Run: `dotnet build tests/Avalonia.InputDemo.AppiumTests`、`dotnet build tests/Avalonia.EventsDemo.AppiumTests`
Expected: 0 个错误、0 个警告。

不设开关：分别 `已跳过: 4`、`已跳过: 3`。先提醒使用者，再实跑（PowerShell）：`$env:HELLO_AVALONIA_APPIUM='1'; dotnet test tests/Avalonia.InputDemo.AppiumTests --no-build`（EventsDemo 同样）
Expected: `通过: 4`（约 30 s）、`通过: 3`（约 21 s）。InputDemo 连跑三次都应通过。

- [ ] **Step 3: 破坏检查**

用 `C:\Temp\probe-b10\break.sh <编号> <文件> <sed 表达式> <测试项目>`（先 `export HELLO_AVALONIA_APPIUM=1`）。它先备份文件、跑完从备份还原，所以文件里未提交的改动不会丢。

| 编号 | 文件 | `sed` 表达式 | 预期（实测） |
|---|---|---|---|
| I1 | `src/Avalonia.InputDemo/Views/Pages/FocusPage.axaml` | `s/ IsTabStop="False"//` | 只有 `Tab_skips_…` 失败（Tab 从 B 到了 C） |
| I2 | `src/Avalonia.InputDemo/Views/Pages/KeyboardPage.axaml` | `s/ HotKey="Ctrl+S"//` | 只有 `Button_hot_key_…` 失败 |
| E1 | `src/Avalonia.EventsDemo/Views/Pages/InputEventsPage.axaml` | `s/PointerReleased="OnReleased"//` | 只有 `Real_mouse_…` 失败（点击只记两条） |
| E2 | 同上 | `s/KeyDown="OnKeyDown" //` | 只有 `Real_mouse_…` 失败（按键只记一条） |

实测：4/4 按预期，每条只有目标用例失败。

### Task 3: ServicesDemo 与 AppDevelopmentDemo——系统对话框与 UIA 属性

**Files:**
- Create: `tests/Avalonia.ServicesDemo.AppiumTests/FlowTests.cs`、`tests/Avalonia.AppDevelopmentDemo.AppiumTests/FlowTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `SelectTab`、`FindWindow`、`KeyChord.Press`；批 7 的 `ById`、`ByName`、`WaitFor`、`WaitForText`。
- Produces: 无。

两个用例：
1. `Open_and_save_dialogs_can_be_cancelled`：打开对话框出现 → Escape → 页面显示取消结果、对话框消失；保存对话框同样。等对话框出现的上限 15 s。
2. `Automation_properties_reach_the_UIA_tree_and_match_the_peer`：`ok-btn` 在 UIA 里是 `Button`、名字 `Confirm`、`HelpText` 为 `Confirms the form`，按内容 `确定` 找不到；"读取 Peer"后页面显示同样的值。

- [ ] **Step 1: 写两个测试文件**

#### `tests/Avalonia.ServicesDemo.AppiumTests/FlowTests.cs`

```csharp
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.ServicesDemo.AppiumTests
{
    // A3: the real Windows file dialogs open and can be cancelled without a crash. Nothing is picked, so the
    // tests read and write no files. Escape cancels both dialogs; their Cancel button is named in the language
    // of Windows, so it is not clicked.
    public class FlowTests
    {
        private const string App = "Avalonia.ServicesDemo";

        // The batch 10 probe saw the first open dialog after 4.6 s and the save dialog after 2.7 s.
        private const int DialogTimeoutMs = 15000;

        [AppiumFact(App)]
        public void Open_and_save_dialogs_can_be_cancelled()
        {
            using var app = new AppSession(App);
            app.SelectTab("文件对话框");

            app.ById("OpenButton").Click();
            WaitForDialog(app, "打开文本文件");
            KeyChord.Press(VirtualKey.Escape);
            app.WaitForText("Result", "已取消（OpenFilePickerAsync 返回空列表）");
            AppSession.WaitFor(() => app.FindWindow("打开文本文件") is null, "the open dialog is closed");

            app.ById("SaveButton").Click();
            WaitForDialog(app, "保存文件");
            KeyChord.Press(VirtualKey.Escape);
            app.WaitForText("Result", "已取消（StorageFile 为 null）");
            AppSession.WaitFor(() => app.FindWindow("保存文件") is null, "the save dialog is closed");
        }

        private static void WaitForDialog(AppSession app, string title) =>
            AppSession.WaitFor(() => app.FindWindow(title) is not null, $"the {title} dialog is open", DialogTimeoutMs);
    }
}
```

#### `tests/Avalonia.AppDevelopmentDemo.AppiumTests/FlowTests.cs`

```csharp
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.AppiumTests
{
    // A3: the AutomationProperties set in XAML reach the real Windows UIA tree, which is what a screen reader or a
    // UI test sees, and they agree with what the page reads back from Avalonia's own AutomationPeer.
    public class FlowTests
    {
        private const string App = "Avalonia.AppDevelopmentDemo";

        [AppiumFact(App)]
        public void Automation_properties_reach_the_UIA_tree_and_match_the_peer()
        {
            using var app = new AppSession(App);
            app.SelectTab("无障碍");

            var ok = app.ById("ok-btn");
            Assert.Equal("ControlType.Button", ok.TagName);
            // AutomationProperties.Name replaces the button's content "确定" as its UIA name.
            Assert.Equal("Confirm", ok.Text);
            Assert.Empty(app.Driver.FindElementsByName("确定"));
            Assert.Equal("Confirms the form", ok.GetAttribute("HelpText"));

            app.ByName("读取 Peer").Click();
            app.WaitForText("PeerName", "Name：Confirm");
            Assert.Equal("AutomationId：ok-btn", app.ById("PeerId").Text);
            Assert.Equal("HelpText：Confirms the form", app.ById("PeerHelp").Text);
            Assert.Equal("ControlType：Button", app.ById("PeerControl").Text);
        }
    }
}
```

- [ ] **Step 2: 构建、跳过、实跑**

Run: `dotnet build tests/Avalonia.ServicesDemo.AppiumTests`、`dotnet build tests/Avalonia.AppDevelopmentDemo.AppiumTests`
Expected: 0 个错误、0 个警告。

不设开关：各 `已跳过: 3`。先提醒使用者（会弹出 Windows 文件对话框，测试自己取消），再实跑。
Expected: 各 `通过: 3`（约 35 s、31 s）。

- [ ] **Step 3: 破坏检查**

| 编号 | 文件 | `sed` 表达式 | 预期（实测） |
|---|---|---|---|
| S1 | `src/Avalonia.ServicesDemo/Views/Pages/FileDialogsPage.axaml.cs` | `s/Title = "打开文本文件",/Title = "打开",/` | 只有 `Open_and_save_…` 失败（等不到对话框）；测试结束后没有残留的 `Avalonia.ServicesDemo` 进程 |
| S2 | 同上 | `s/Result.Text = "已取消（StorageFile 为 null）";/Result.Text = "已取消";/` | 只有 `Open_and_save_…` 失败 |
| A1 | `src/Avalonia.AppDevelopmentDemo/Views/Pages/AccessibilityPage.axaml` | `s/AutomationProperties.HelpText="Confirms the form"//` | 只有 `Automation_properties_…` 失败 |

实测：3/3 按预期。S1 第一次跑时还没有 Task 1 的兜底，对话框留在桌面上，紧接着的 S2 构建失败（MSB3027，exe 被锁）；加兜底后重跑 S1、S2，都只有目标用例失败，S1 之后没有残留进程。

### Task 4: MusicStore——商店窗口

**Files:**
- Modify: `src/Avalonia.MusicStore/Views/MainWindow.axaml`（商店按钮加 `AutomationProperties.Name="Music Store"`，与它的 `ToolTip.Tip` 同文）
- Create: `tests/Avalonia.MusicStore.AppiumTests/FlowTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `FindWindow`；批 7 的 `ByName`、`WaitFor`。
- Produces: 无。

用例 `Store_button_opens_the_store_window_and_its_close_button_closes_it`：按名字点商店按钮 → `MusicStoreWindow` 出现，里面 1 个 `Edit`、1 个 `List` → 点它自绘的 `Close` → 窗口消失，主窗口还在。不键入搜索词，所以不联网。

- [ ] **Step 1: 给按钮起名**

改动只有一行（`ToolTip.Tip` 之后）。整个文件如下：

#### `src/Avalonia.MusicStore/Views/MainWindow.axaml`

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="using:Avalonia.MusicStore.ViewModels"
        xmlns:views="using:Avalonia.MusicStore.Views"
        mc:Ignorable="d"
        d:DesignWidth="800"
        d:DesignHeight="450"
        x:Class="Avalonia.MusicStore.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia.MusicStore"
        Width="1000"
        Height="700"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        Loaded="OnLoaded">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <Panel>
        <ExperimentalAcrylicBorder IsHitTestVisible="False">
            <ExperimentalAcrylicBorder.Material>
                <ExperimentalAcrylicMaterial BackgroundSource="Digger"
                                             TintColor="Black"
                                             TintOpacity="1"
                                             MaterialOpacity="0.65" />
            </ExperimentalAcrylicBorder.Material>
        </ExperimentalAcrylicBorder>

        <Grid Margin="40" RowDefinitions="auto,*">
            <Button Grid.Row="0"
                    HorizontalAlignment="Right"
                    Command="{Binding ShowAlbumsCommand}"
                    ToolTip.Tip="Music Store"
                    AutomationProperties.Name="Music Store">
                <PathIcon Data="{StaticResource store_microsoft_regular}" />
            </Button>

            <ItemsControl Grid.Row="1"
                          Margin="0,40,0,0"
                          ItemsSource="{Binding Albums}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <views:AlbumView Margin="0,0,20,20" />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </Grid>
    </Panel>
</Window>
```

- [ ] **Step 2: 写测试文件**

#### `tests/Avalonia.MusicStore.AppiumTests/FlowTests.cs`

```csharp
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.MusicStore.AppiumTests
{
    // A3: the store window opens over the main window and closes again. Nothing is typed into the search box,
    // so the store never queries the iTunes service and the test needs no network.
    public class FlowTests
    {
        private const string App = "Avalonia.MusicStore";
        private const string StoreWindow = "MusicStoreWindow";

        [AppiumFact(App)]
        public void Store_button_opens_the_store_window_and_its_close_button_closes_it()
        {
            using var app = new AppSession(App);

            // The button shows only an icon; its AutomationProperties.Name gives it a name in the UIA tree.
            app.ByName("Music Store").Click();
            AppSession.WaitFor(() => app.FindWindow(StoreWindow) is not null, "the store window is open");
            var store = app.FindWindow(StoreWindow)!;
            // The search box and the result list; the buy button's text comes from the localized resources.
            Assert.Single(store.FindElementsByTagName("Edit"));
            Assert.Single(store.FindElementsByTagName("List"));

            // The window extends its client area over the title bar, so Avalonia draws its own caption buttons.
            store.FindElementByName("Close").Click();
            AppSession.WaitFor(() => app.FindWindow(StoreWindow) is null, "the store window is closed");
            Assert.Equal("Avalonia.MusicStore", app.Driver.Title);
        }
    }
}
```

- [ ] **Step 3: 构建、Headless 回归、实跑**

Run: `dotnet build tests/Avalonia.MusicStore.AppiumTests`
Expected: 0 个错误；警告只有 `iTunesSearch` 已有的 NU1701。

`dotnet test tests/Avalonia.MusicStore.Tests`：Headless 测试全部通过（按钮多一个附加属性，不影响窗口与 ViewModel 的测试）。

不设开关：`已跳过: 2`。先提醒使用者，再实跑：Expected `通过: 2`（约 10 s）。

- [ ] **Step 4: 破坏检查**

| 编号 | 文件 | `sed` 表达式 | 预期（实测） |
|---|---|---|---|
| M1 | `src/Avalonia.MusicStore/Views/MainWindow.axaml` | `s/ AutomationProperties.Name="Music Store"//` | 只有 `Store_button_…` 失败（按名字找不到按钮） |

实测：按预期，`Store_button_…` 失败，`App_starts_…` 通过。

这个文件有本任务未提交的改动，必须用 `break.sh` 的备份还原，不能 `git checkout`。

### Task 5: 全量验证、提交与回填

**Files:**
- Modify: `README.md`、`CLAUDE.md`、`docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

- [ ] **Step 1: 开着开关跑整个解决方案**

先提醒使用者，约 10 分钟。Run（PowerShell）：`$env:HELLO_AVALONIA_APPIUM='1'; dotnet build hello-avalonia.slnx; dotnet test hello-avalonia.slnx --no-build *> C:\Temp\probe-b10\full-live.txt`
Expected: 退出码 0；`[FAIL]` 0 行；通过数合计 572（批 9 的 566 + 本批 6）；之后没有残留的 `Avalonia.*` 进程。

再不设开关跑一次：Expected 523 通过、49 跳过、退出码 0。

- [ ] **Step 2: 提交代码**

```bash
git add src/Avalonia.MusicStore/Views/MainWindow.axaml tests/Avalonia.Appium.Common tests/Avalonia.TestingDemo.AppiumTests tests/Avalonia.WebViewDemo.AppiumTests tests/Avalonia.InputDemo.AppiumTests tests/Avalonia.EventsDemo.AppiumTests tests/Avalonia.ServicesDemo.AppiumTests tests/Avalonia.AppDevelopmentDemo.AppiumTests tests/Avalonia.MusicStore.AppiumTests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add Appium flow tests for input, events, file dialogs, accessibility and the music store" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 3: 文档**

- README 的 Appium 一行补上本批的流程。
- CLAUDE.md 的 `Avalonia.Appium.Common` 一条：列出 `KeyChord`；按键用 `KeyChord.Press`，不用 `Keyboard.SendKeys`；不按 Ctrl+Shift 组合键。
- spec：A3 候选标明已交付；风险表里"MusicStore 只做 A1"一行改为本批的决定；回填「批 10 实测结论」。

- [ ] **Step 4: 提交文档**

```bash
git add README.md CLAUDE.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 10 results" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```
