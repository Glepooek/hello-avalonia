# 演示测试 07：Appium 项目骨架与 TestingDemo 基准 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 spec 批 7：新建唯一的 Appium 测试项目 `tests/Avalonia.Demos.AppiumTests`，包含运行门控 `AppiumFactAttribute`、会话夹具 `AppSession`、exe 定位 `AppPaths`、绕过中文输入法的 `UnicodeKeyboard`；并以 `TestingDemo` 的 A1–A3 作为后续批的基准样例。

**Architecture:** 测试通过 `Appium.WebDriver` 4.4.5 直连本机 WinAppDriver（`127.0.0.1:4723`），用 `app` 能力启动 `src\<应用>\bin\Debug\<应用>.exe`。
- **门控**：四个条件都满足才运行，否则跳过并给出原因。没有 WinAppDriver 时，`dotnet test hello-avalonia.slnx` 仍然全绿，Appium 用例显示为跳过。
- **会话**：每个测试自己 `using var app = new AppSession(...)`，测试之间不共享应用状态。
- **断言**：所有读状态的断言都经 `WaitForText`/`WaitFor` 轮询。
- **输入**：文字输入用 `SendInput` 的 Unicode 包，不用 WinAppDriver 的 `SendKeys`。

**Tech Stack:** .NET 10.0、Appium.WebDriver 4.4.5（Selenium 3）、WinAppDriver 1.2.1、xUnit v3（3.2.2）、Win32 `SendInput`

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（「Appium 设计」「Appium 环境配置」「批 0 实测结论 / 对批 7–10 的决定」；批 6 已完成）

## Global Constraints

- 新写的注释用**英文**；断言里的界面文字用界面上的**中文**原文（`计数器`、`共 2 项`、`已提交：张三，30 岁`）。
- 版本只写在根目录 `Directory.Packages.props`。`Appium.WebDriver` 4.4.5 已在批 0 登记，本批不改它。
- 项目放 `tests/Avalonia.Demos.AppiumTests`，登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 最后一项。
  - 对被测应用用 `ProjectReference ... ReferenceOutputAssembly="false"`，只保证构建顺序。
  - 本批只引用 `Avalonia.TestingDemo`，其余应用由批 8、9 按需追加。
- 运行门控按 spec「运行门控」四条，顺序固定：Windows、`HELLO_AVALONIA_APPIUM=1`、TCP 4723 能连上（**不看** `/status` 内容）、exe 已构建。
- 全局串行：`[assembly: CollectionBehavior(DisableTestParallelization = true)]`。
- 等待：一律 `AppSession.WaitForText`/`AppSession.WaitFor`，不要 `Thread.Sleep`（`Poll` 内部的 100 ms 间隔除外）；默认上限 3000 ms（见「已核实的事实」）。
- 文字输入一律 `AppSession.TypeInto`，**不用** `WindowsElement.SendKeys`（见 Review Focus 第 1 条）。`Clear()` 可以用。
- `src/` 本批**不改**：`TestingDemo` 的控件已全部有 `AutomationId`。破坏检查临时改的行要 `git checkout -- <文件>` 还原，并重新构建一次，免得 `bin` 里留着坏 exe。
- 每个测试结束时被测进程必须退出（`using` 释放会话）。破坏检查之后用 `Get-Process Avalonia.TestingDemo` 确认没有残留。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`。提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（本批是 `C:\Temp\probe-b7\`），**永不提交**。
- 此前的 523 个测试始终保持通过。
- 实跑 Appium 前先提醒使用者：运行期间不要动鼠标键盘、不要锁屏。

## Review Focus

按"最可能让使用者踩到"的顺序排列；每条在所属任务里都有对应测试或检查。

1. **中文输入法吞掉 Appium 的键入**
   - 现象：本机默认输入法是搜狗拼音。WinAppDriver 的 `SendKeys` 发的是虚拟键，进入输入法组字：`abc3` 变成 `安保处`，`nn` 整段消失。任何装了中文输入法的机器上，打字的测试都会随机失败，而且看起来像应用的缺陷。
   - 对策：`UnicodeKeyboard` 用 `KEYEVENTF_UNICODE` 直接投递字符，以 `WM_CHAR` 送到窗口，绕过输入法组字，中文也能原样输入。
   - 测试：Task 2 的表单与列表测试。破坏检查是"换回 `SendKeys`"，两条都会失败。
2. **没开 WinAppDriver 时整个解决方案的 `dotnet test` 变红，或误控桌面**
   - 门控任何一条不满足都必须跳过，退出码 0。
   - 测试：Task 1 Step 3 不设环境变量跑一次，全部跳过。
3. **测试结束后应用进程残留**
   - 残留的窗口会挡住下一次会话，也会挡住使用者。
   - 测试：Task 2 的 `Closing_the_session_ends_the_app` 按 PID 断言进程退出；破坏检查是"`Dispose` 不 `Quit`"。
4. **点完立刻读到旧值**（批 0 见过一次）
   - 所有断言都轮询。本批实测没有复现：点击后立刻读，已是新值。
   - 测试：每条 A3 都只用 `WaitForText`/`WaitFor` 读状态。
5. **测试之间串状态**
   - 计数器、列表都有状态，共享会话的话，后跑的测试依赖先跑的。
   - 对策：每个测试各起一个应用，约 2.7 秒启动开销。
   - 测试：Task 2 的 6 个测试都断言自己的初始状态（`0`、`共 0 项`、提交禁用）。

---

## 本份已核实的事实（编写计划时在 `C:\Temp\probe-b7\` 实测，2026-10-17）

WinAppDriver 1.2.1 已安装在 `C:\Program Files\Windows Application Driver\`，开发人员模式已开启。探针分两轮，原始记录在 `C:\Temp\probe-b7\notes-round2.txt` 与 `probes.cs.txt`、`unicode-probes.cs.txt`。

| 问题 | 结论 |
|---|---|
| 启动 | `new WindowsDriver<WindowsElement>(…)` 返回约 **2.7 s**；`Title` 为 `Avalonia Testing Demo`，`WindowHandles` 1 个 |
| 标签页 | `FindElementsByTagName("TabItem")` 与 `FindElementsByClassName("TabItem")` 都得到 3 个：`计数器`、`表单`、`列表`。`FindElementByName("表单").Click()` 后 `Selected` 为真 |
| 点击后的读值 | 点 `increment-btn` 后**立刻**读 `count-text` 已是 `1`，批 0 的旧值没有复现。所有等待都在 **58–266 ms** 内收敛（约 25 次）。默认上限由 spec 建议的 5 s 收紧到 3 s |
| 文本读取 | `TextBlock` 的 `.Text` 是 UIA Name；错误提示为空时读到空串。`TextBox` 的 `.Text` 是当前内容 |
| `SendKeys` | **被搜狗拼音截走**：`A` 能进（单字母上屏），`nn` 消失，`abc` 变成 `安保处`（`3` 选了第 3 个候选），`30` 变成 `0` |
| `SendInput` + `KEYEVENTF_UNICODE` | `Ann`、`abc`、`30`、`张三`、`apple`、`pear` 全部原样进入；先 `Click()` 让文本框获得焦点 |
| `Clear()` | 能清空 `TextBox` 并触发绑定（错误提示随之消失） |
| 列表项 | `items-list` 下 `FindElementsByTagName("ListItem")` 与 `ByClassName("ListBoxItem")` 都能拿到项，`.Text` 为条目文字。按名字点中后 `Selected` 为真 |
| 结束会话 | `Quit()` 后被测进程退出，**不需要** `CloseApp()`。探针里先 `CloseApp` 再 `Quit`，拿掉 `CloseApp` 后照样退出 |
| 门控 | 不设 `HELLO_AVALONIA_APPIUM` 时，`AppiumFactAttribute` 让全部用例跳过，`dotnet test` 退出码 0 |
| 端口 | 4723 由 `http.sys`（PID 4）承载；`netstat` 显示的是 4，真正的进程用 `netsh http show servicestate` 查。重复启动第二个 WinAppDriver 会报 `Address … is already in use` 后以退出码 5 退出 |

### 偏离 spec 的地方（都有理由，执行后写进回填）

- **会话不用 `IClassFixture`，每个测试一个**。spec 写的是 `IClassFixture` 级别；但 TestingDemo 的计数器与列表有状态，共享会话会让测试依赖执行顺序。启动约 2.7 s，6 个测试共约 35 s，可以接受。批 8 的 A1+A2 无状态，届时可以再按 spec 用类夹具。
- **键入不用 `SendKeys`**：见 Review Focus 第 1 条。spec「元素定位」只说沿用参考库的定位方式，没有覆盖输入。
- **`AppSession` 的会话延迟到首次访问 `Driver` 才创建**：被跳过的测试不会启动应用。

## 文件结构

**新增**（都在 `tests/Avalonia.Demos.AppiumTests/`）

| 文件 | 职责 |
|---|---|
| `Avalonia.Demos.AppiumTests.csproj` | 测试项目；只引用 Appium 与 xunit，对被测应用做仅构建顺序的引用 |
| `AssemblyInfo.cs` | 关闭测试并行 |
| `AppPaths.cs` | 向上找 `hello-avalonia.slnx` 定位仓库根；挑 Debug、Release 中较新的 exe |
| `AppiumFactAttribute.cs` | 运行门控，四个条件，跳过时给出原因 |
| `AppSession.cs` | 延迟创建的 WinAppDriver 会话、按 `AutomationId`/名字查找、`TypeInto`、`WaitForText`/`WaitFor` |
| `UnicodeKeyboard.cs` | `SendInput` + `KEYEVENTF_UNICODE`，绕过输入法 |
| `TestingDemoTests.cs` | A1 启动、A2 标签页、进程回收、A3 计数器/表单/列表 |

**修改**

| 文件 | 改动 |
|---|---|
| `hello-avalonia.slnx` | `/tests/` 末尾登记新项目（Task 1） |
| `README.md` | 在 `Avalonia.\*Demo.Tests` 一行之后加一行 Appium 项目说明（Task 3） |
| `CLAUDE.md` | 「命令」代码块加一条 Appium 运行命令（Task 3） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 7 实测结论」；「运行期间的注意事项」加一条输入法说明（Task 3） |

**不碰**：`src/`、`tests/Avalonia.Tests.Common`、`Directory.Packages.props`、其他测试项目。

**代码块抽取**：`C:\Temp\extract-plan.ps1` 已不存在。可以按 `#### \`路径\`` 标注手动 Write，或者从 `C:\Temp\probe-b7\ProbeAppium\` 复制同名文件；那里的 csproj 用的是绝对路径，必须用本计划里的版本。

---

### Task 1: Appium 项目骨架与运行门控

**Files:**
- Create: `tests/Avalonia.Demos.AppiumTests/` 下的 `Avalonia.Demos.AppiumTests.csproj`、`AssemblyInfo.cs`、`AppPaths.cs`、`AppiumFactAttribute.cs`、`AppSession.cs`、`UnicodeKeyboard.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（NuGet `Appium.WebDriver` 4.4.5）：
  - `OpenQA.Selenium.Appium.AppiumOptions.AddAdditionalCapability(string, object)`
  - `OpenQA.Selenium.Appium.Windows.WindowsDriver<WindowsElement>(Uri, AppiumOptions)`
  - `FindElementByAccessibilityId`、`FindElementByName`、`Quit()`
  - `OpenQA.Selenium.WebDriverException`
- Consumes（xunit v3）：`FactAttribute(string? sourceFilePath, int sourceLineNumber)` 构造函数与可写的 `Skip`。
- Produces（Task 2 与批 8–10 使用，均为 `public`）：
  - `AppPaths.RepoRoot : string`；`AppPaths.FindExe(string app) : string?`
  - `AppiumFactAttribute(string app)`；`AppiumFactAttribute.SwitchVariable = "HELLO_AVALONIA_APPIUM"`
  - `AppSession(string app) : IDisposable`，成员如下：
    - `DriverUri`、`DefaultTimeoutMs = 3000`、`Driver : WindowsDriver<WindowsElement>`
    - `ById(string) : WindowsElement`、`ByName(string) : WindowsElement`
    - `TypeInto(WindowsElement box, string text)`
    - `WaitForText(string automationId, string expected, int timeoutMs = DefaultTimeoutMs)`
    - 静态 `WaitFor(Func<bool> condition, string what, int timeoutMs = DefaultTimeoutMs)`
  - `UnicodeKeyboard.Type(string text)`

几处容易写错的地方：

- `AppiumFactAttribute` 的 `Skip` 必须在**构造函数**里算出，xunit 在发现阶段就读它。要把 `[CallerFilePath]`、`[CallerLineNumber]` 转给 `base(...)`，否则 v3 报不出测试的源码位置。
- `AppSession` 的 `Driver` 是延迟创建的，构造 `AppSession` 本身不启动应用。
- `UnicodeKeyboard` 的 `InputUnion` 必须包含 `MouseInput`：它是最大的成员，决定 `Marshal.SizeOf<Input>()`。按 Win32 文档，`cbSize` 与系统的 `INPUT` 大小不符时，`SendInput` 返回 0（本批未实测这种情况）。`Type` 发现发送数不足时会抛异常。
- `WaitForText` 把 null 当空串比较：空的 `TextBlock` 读出来可能是 null，也可能是空串。

- [ ] **Step 1: 建项目、登记解决方案**

#### `tests/Avalonia.Demos.AppiumTests/Avalonia.Demos.AppiumTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <!-- xunit v3 test projects are executables. -->
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Appium.WebDriver" />
        <PackageReference Include="xunit.v3" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
    </ItemGroup>

    <ItemGroup>
        <!-- Build order only: the tests launch each app's exe and never load its assembly. -->
        <ProjectReference Include="..\..\src\Avalonia.TestingDemo\Avalonia.TestingDemo.csproj" ReferenceOutputAssembly="false" />
    </ItemGroup>
</Project>
```

在 `hello-avalonia.slnx` 里，`<Project Path="tests/Avalonia.HtmlRendererDemo.Tests/Avalonia.HtmlRendererDemo.Tests.csproj" />` 一行之后加：

```xml
    <Project Path="tests/Avalonia.Demos.AppiumTests/Avalonia.Demos.AppiumTests.csproj" />
```

- [ ] **Step 2: 放入基础设施文件**

#### `tests/Avalonia.Demos.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.Demos.AppiumTests/AppPaths.cs`

```csharp
using System;
using System.IO;
using System.Linq;

namespace Avalonia.Demos.AppiumTests
{
    // Finds the built exe of a demo app: walks up to the folder holding hello-avalonia.slnx, then picks the newer of
    // src\<app>\bin\Debug and bin\Release (Directory.Build.props drops the target framework folder).
    public static class AppPaths
    {
        public static string RepoRoot { get; } = FindRepoRoot();

        public static string? FindExe(string app)
        {
            return new[] { "Debug", "Release" }
                .Select(config => Path.Combine(RepoRoot, "src", app, "bin", config, app + ".exe"))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static string FindRepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "hello-avalonia.slnx")))
                    return dir.FullName;
            }
            throw new InvalidOperationException($"No hello-avalonia.slnx above {AppContext.BaseDirectory}.");
        }
    }
}
```

#### `tests/Avalonia.Demos.AppiumTests/AppiumFactAttribute.cs`

```csharp
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Demos.AppiumTests
{
    // Appium drives the real mouse and keyboard, so a test only runs when it is switched on explicitly and can
    // actually work; otherwise it is skipped with the first reason that applies.
    public sealed class AppiumFactAttribute : FactAttribute
    {
        public const string SwitchVariable = "HELLO_AVALONIA_APPIUM";

        public AppiumFactAttribute(
            string app,
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            Skip = SkipReason(app);
        }

        private static string? SkipReason(string app)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Appium tests run on Windows only.";
            if (Environment.GetEnvironmentVariable(SwitchVariable) != "1")
                return $"Set {SwitchVariable}=1 to run Appium tests; they take over the mouse and keyboard.";
            if (!DriverListening())
                return $"WinAppDriver is not listening on {AppSession.DriverUri}.";
            if (AppPaths.FindExe(app) is null)
                return $"{app}.exe is not built; run dotnet build first.";
            return null;
        }

        // Only the TCP connection counts: WinAppDriver answers /status with "status 13" even when healthy.
        private static bool DriverListening()
        {
            try
            {
                using var client = new TcpClient();
                return client.ConnectAsync(AppSession.DriverUri.Host, AppSession.DriverUri.Port).Wait(1000)
                       && client.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
```

#### `tests/Avalonia.Demos.AppiumTests/AppSession.cs`

```csharp
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
```

#### `tests/Avalonia.Demos.AppiumTests/UnicodeKeyboard.cs`

```csharp
using System;
using System.Runtime.InteropServices;

namespace Avalonia.Demos.AppiumTests
{
    // Types text into whatever has keyboard focus as Unicode packets (KEYEVENTF_UNICODE). WinAppDriver's SendKeys
    // sends virtual keys, which a Chinese IME (Sogou or Microsoft Pinyin) turns into pinyin candidates: "abc3"
    // arrives as "安保处". Unicode packets reach the window as WM_CHAR and skip the IME's composition.
    public static class UnicodeKeyboard
    {
        private const uint InputKeyboard = 1;
        private const uint KeyEventUnicode = 0x0004;
        private const uint KeyEventKeyUp = 0x0002;

        public static void Type(string text)
        {
            var inputs = new Input[text.Length * 2];
            for (var i = 0; i < text.Length; i++)
            {
                inputs[2 * i] = Key(text[i], KeyEventUnicode);
                inputs[2 * i + 1] = Key(text[i], KeyEventUnicode | KeyEventKeyUp);
            }
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
                throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Length} events (error {Marshal.GetLastWin32Error()}).");
        }

        private static Input Key(char c, uint flags) => new()
        {
            Type = InputKeyboard,
            Union = new InputUnion { Keyboard = new KeyboardInput { Scan = c, Flags = flags } },
        };

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public InputUnion Union;
        }

        // Sized by MouseInput, the largest member, so Marshal.SizeOf<Input>() matches what SendInput expects.
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MouseInput Mouse;
            [FieldOffset(0)] public KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
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

Run: `dotnet build tests/Avalonia.Demos.AppiumTests 2>&1 | tail -3`
Expected: 0 个警告、0 个错误。

- [ ] **Step 3: 跳过路径（不需要 WinAppDriver）**

Task 2 的测试文件放进来之前，项目里没有测试。先放入 Task 2 Step 1 的 `TestingDemoTests.cs`，再在**没有** `HELLO_AVALONIA_APPIUM` 的终端里跑。

Run（PowerShell）：`Remove-Item Env:HELLO_AVALONIA_APPIUM -ErrorAction SilentlyContinue; dotnet test tests/Avalonia.Demos.AppiumTests; "exit=$LASTEXITCODE"`
Expected: 6 个全部"已跳过"，`exit=0`，没有任何 `Avalonia.TestingDemo` 窗口出现。

- [ ] **Step 4: 提交（与 Task 2 一起，见 Task 2 Step 4）**

基础设施没有自己的真实断言，单独提交没有可验证的内容，所以和 Task 2 的测试放在同一个提交里。

### Task 2: TestingDemo 的 A1–A3 基准

**Files:**
- Create: `tests/Avalonia.Demos.AppiumTests/TestingDemoTests.cs`

**Interfaces:**
- Consumes（Task 1）：`AppiumFactAttribute(string)`、`AppSession` 的全部成员。
- Consumes（`src/Avalonia.TestingDemo` 的 UIA 树）：
  - 窗口标题 `Avalonia Testing Demo`；标签页 `计数器`、`表单`、`列表`。
  - `AutomationId`：`count-text`、`increment-btn`、`decrement-btn`、`reset-btn`、`name-box`、`name-error`、`age-box`、`age-error`、`submit-btn`、`status-text`、`new-item-box`、`add-btn`、`remove-btn`、`items-list`、`summary-text`。
- Produces：无。

逐条覆盖，共 6 个：

| 测试 | 层 | 内容 |
|---|---|---|
| `App_starts_with_its_main_window_and_three_tabs` | A1 | 标题、窗口数、三个标签页的顺序 |
| `Every_tab_can_be_selected_and_shows_its_panel` | A2 | 逐个点标签页，等 `Selected`，等页内控件 `Displayed` |
| `Closing_the_session_ends_the_app` | 夹具 | 按 PID 等被测进程退出 |
| `Counter_buttons_change_the_count_and_which_buttons_are_enabled` | A3 | 加两次 → 2、减 → 1、清零 → 0，按钮可用状态随之变化 |
| `Form_checks_typed_input_and_submits_once_both_fields_are_valid` | A3 | 真实键入 `张`/`三`、`abc`/`30`，错误提示出现又消失，提交后的状态文字 |
| `List_adds_typed_items_and_removes_the_selected_one` | A3 | 键入两项、添加、按名字选中、删除、总数与按钮状态 |

A3 只测"只有真实进程才能验证"的部分：真实键入、真实点击、UIA 读到的 `Enabled`/`Selected`/`Text`。ViewModel 逻辑本身已由 `Avalonia.TestingDemo.Tests` 覆盖，这里不重复。

- [ ] **Step 1: 放入测试文件**

#### `tests/Avalonia.Demos.AppiumTests/TestingDemoTests.cs`

```csharp
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Avalonia.Demos.AppiumTests
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

- [ ] **Step 2: 实跑（需要 WinAppDriver；先提醒使用者不要动机器）**

另开一个终端运行 `& "C:\Program Files\Windows Application Driver\WinAppDriver.exe"` 并保持开着。如果 `Test-NetConnection 127.0.0.1 -Port 4723 -InformationLevel Quiet` 已经是 `True`，说明已有实例在跑，不要再起第二个。

Run（PowerShell）：`$env:HELLO_AVALONIA_APPIUM = "1"; dotnet test tests/Avalonia.Demos.AppiumTests`
Expected: 6 个全部通过，约 35 秒。

Run: 同上再跑一次。
Expected: 6 个全部通过。

Run: `@(Get-Process Avalonia.TestingDemo -ErrorAction SilentlyContinue).Count`
Expected: `0`。

- [ ] **Step 3: 破坏检查（每次一处，跑完还原；环境变量保持为 1）**

`src` 的改动用 `git checkout -- <文件>` 还原。改 `tests` 自己的文件时，此刻还没提交，先把原文件复制到 `C:\Temp\probe-b7\` 再改，跑完拷回，然后用 `diff -q` 确认一致。每行跑 `dotnet test tests/Avalonia.Demos.AppiumTests`（**不要** `--no-build`：要让被测 exe 重新构建）。

| 临时改动 | 预期失败的测试 |
|---|---|
| `src/Avalonia.TestingDemo/ViewModels/CounterViewModel.cs`：`Count++` → `Count += 2` | `Counter_buttons_…`（`count-text` 读到 `4`） |
| `src/Avalonia.TestingDemo/ViewModels/FormViewModel.cs`：`{Age} 岁` → `{Age} 年` | `Form_checks_…` |
| `src/Avalonia.TestingDemo/ViewModels/ListViewModel.cs`：`Items.Remove(item);` → `_ = item;` | `List_adds_…` |
| `src/Avalonia.TestingDemo/Views/MainWindow.axaml`：`Title="Avalonia Testing Demo"` → `Title="Testing Demo"` | `App_starts_…` |
| `tests/…/AppSession.cs`：`Dispose` 里去掉 `_driver?.Quit();` | `Closing_the_session_ends_the_app`（只跑这一条：`--filter "FullyQualifiedName~Closing_the_session"`；跑完 `Get-Process Avalonia.TestingDemo \| Stop-Process`） |
| `tests/…/AppSession.cs`：`TypeInto` 里 `UnicodeKeyboard.Type(text);` → `box.SendKeys(text);` | `Form_checks_…` 与 `List_adds_…`，**预期 2 个**；只在装了中文输入法的机器上成立，英文环境下会全绿，届时记为"不适用" |

全部还原后：

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -3; git status --short`
Expected: 构建 0 错误；`git status` 只有 `?? tests/Avalonia.Demos.AppiumTests/`、` M hello-avalonia.slnx`（以及尚未提交时的本计划文件）。

- [ ] **Step 4: 提交 Task 1 + Task 2**

```bash
git add hello-avalonia.slnx tests/Avalonia.Demos.AppiumTests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add the Appium test project with TestingDemo launch, tab and flow tests

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

### Task 3: README、CLAUDE.md、回填 spec 与全量验证

**Files:**
- Modify: `README.md`、`CLAUDE.md`、`docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

**Interfaces:**
- Consumes：Task 1–2 的提交与实测结果。
- Produces：文档。

- [ ] **Step 1: 全量验证（不设环境变量）**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -3`
Expected: 0 个错误；警告 ≤ 5（批 6 结束时 5 个）。

Run（PowerShell，新终端，确认没有 `HELLO_AVALONIA_APPIUM`）：`dotnet test hello-avalonia.slnx`
Expected: 523 个通过，Appium 项目 6 个跳过，0 个失败。

- [ ] **Step 2: README**

在 `[Avalonia.\*Demo.Tests](tests)` 那一行之后加一行：

```markdown
| [Avalonia.Demos.AppiumTests](tests/Avalonia.Demos.AppiumTests) | WinAppDriver + Appium 的真实进程测试（默认跳过；先启动 WinAppDriver，再设 `HELLO_AVALONIA_APPIUM=1`）：目前覆盖 TestingDemo 的启动、标签页与三个面板的键鼠流程 |
```

- [ ] **Step 3: CLAUDE.md**

「命令」代码块里，在 `--filter` 那一行之后加：

```bash
$env:HELLO_AVALONIA_APPIUM=1; dotnet test tests/Avalonia.Demos.AppiumTests   # PowerShell；需先运行 WinAppDriver，会接管鼠标键盘
```

- [ ] **Step 4: spec**

在「运行期间的注意事项」末尾加一条：

```markdown
- **中文输入法会吞掉 `SendKeys`**（批 7 实测，搜狗拼音）：WinAppDriver 的 `SendKeys` 发虚拟键，进入输入法组字，`abc3` 变成 `安保处`。测试一律用 `AppSession.TypeInto`（`SendInput` + `KEYEVENTF_UNICODE`），不受输入法影响，不必切换使用者的输入法。
```

把 `## 批 7 实测结论（执行后回填）` 这一行换成下面整段。数字按实测填写，与这里不同就按实测写，并在"执行时与计划不符"里说明。

```markdown
## 批 7 实测结论（执行后回填）

计划：`docs/superpowers/plans/2026-10-12-demo-tests-07-appium-foundation.md`；探针：`C:\Temp\probe-b7\`（未提交）。

**交付**：`tests/Avalonia.Demos.AppiumTests`，含 `AppPaths`、`AppiumFactAttribute`、`AppSession`、`UnicodeKeyboard`，以及 TestingDemo 的 6 个用例（A1 1、A2 1、夹具 1、A3 3）。
- 实跑：6 个全部通过，约 <实测> 秒，连跑两次。
- 不设开关：6 个跳过，退出码 0。
- 全量：523 通过 + 6 跳过；构建 0 错误，警告 <实测> 个。

**实测**（WinAppDriver 1.2.1，Windows 10 19045，搜狗拼音）：
- 启动约 2.7 s。
- 点击后读值没有复现批 0 的延迟，等待都在 58–266 ms 内收敛，默认上限定为 3 s。
- `Quit()` 即结束被测进程，不需要 `CloseApp()`。
- `FindElementsByTagName("TabItem")`/`("ListItem")` 可用。
- 4723 端口由 `http.sys` 承载，`netstat` 显示 PID 4，要用 `netsh http show servicestate` 才能找到 WinAppDriver 的进程号。

**与 spec 的偏离**：
- 会话每个测试一个，不用 `IClassFixture`：TestingDemo 有状态，共享会话会让测试依赖执行顺序。
- 键入用 `UnicodeKeyboard`，不用 `SendKeys`：见「运行期间的注意事项」。

**破坏检查**：<实测>/6 条按预期失败。`SendKeys` 那条只在中文输入法环境成立。

**执行时与计划不符**：<无，或逐条写>
```

`<实测>` 与 `<无，或逐条写>` 是执行时**必须替换**的记录位。

- [ ] **Step 5: 提交**

```bash
git add README.md CLAUDE.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 7 results and how to run the Appium tests

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

## Self-Review

**1. Spec 覆盖（批 7）**

| spec 要求 | 落在哪里 |
|---|---|
| Appium 项目骨架、`AppSession`、`AppiumFactAttribute` | Task 1 |
| `TestingDemo` 的 A1–A3 作为基准 | Task 2：A1 1 条、A2 1 条、A3 3 条 |
| 门控四条，`/status` 不作判据 | `AppiumFactAttribute.SkipReason`；Task 1 Step 3 |
| exe 路径：向上找 `.slnx`，Debug/Release 取较新 | `AppPaths.FindExe` |
| 构建顺序：`ReferenceOutputAssembly="false"` | csproj（本批只引用 TestingDemo） |
| 全局串行 | `AssemblyInfo.cs` |
| 所有断言带等待，统一的 `WaitFor`，批 7 实测后收紧上限 | `WaitForText`/`WaitFor`，3 s，依据见「已核实的事实」 |
| 会话关闭即结束进程 | `Closing_the_session_ends_the_app`，有破坏检查 |
| `src/` 只改 `AutomationId`（本批无需） | Task 2 Step 3 末尾的 `git status` |
| 回填「实测结论」 | Task 3 Step 4 |

**2. 占位符**：代码块由脚本从 `C:\Temp\probe-b7\ProbeAppium\` 实跑过的文件原样嵌入，csproj 除外（相对路径版本写在 Step 1）。Task 3 的 `<实测>` 等是执行时必须替换的记录位，Step 4 已写明。

**3. 名称一致性**：Task 2 只用 Task 1 Produces 里列出的成员：`TypeInto`、`WaitForText`、`WaitFor(condition, what)`、`ById`、`ByName`、`Driver`、`Dispose`。破坏检查表里的测试名与 Step 1 的方法名一致。

**4. Review Focus**：5 条都有对应检查。
- 第 1 条：表单、列表测试，加 `SendKeys` 破坏检查。
- 第 2 条：Task 1 Step 3、Task 3 Step 1。
- 第 3 条：`Closing_the_session_ends_the_app`，加 `Quit` 破坏检查。
- 第 4 条：所有 A3 只经轮询读值。
- 第 5 条：每个测试各起一个应用，并断言初始状态。
