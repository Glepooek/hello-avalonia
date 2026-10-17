# 演示测试 09：四个非标签页应用的 Appium A1 与 WebViewDemo 的 A3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 spec 批 9：ControlsDemo、HtmlRendererDemo、MusicStore、WebViewDemo 各有一个 `Avalonia.<X>.AppiumTests` 项目，覆盖 A1（启动）；WebViewDemo 再加 A3：网页窗口真的打开、JS 与 C# 双向调用都通、原生对话框能弹出。

**Architecture:** 四个应用没有 `TabControl`，用不上批 8 的 `TabbedApp`，A1 在各自项目里直接用 `AppSession` 写，每个只断言该应用特有的主内容。
- 项目形状照批 8：`csproj` 与 `AssemblyInfo.cs` 与 LayoutDemo 的相同，只换被测应用。
- WebViewDemo 的 A3 只在应用会话里操作：有所有者的窗口不在 `WindowHandles` 里，按窗口标题找元素；网页内容经 WebView2 暴露给 UIA，读到的文字只用来证明桥的效果。

**Tech Stack:** .NET 10.0、Appium.WebDriver 4.4.5（Selenium 3）、WinAppDriver 1.2.1、xUnit v3（3.2.2）、WebView2、Win32 `IsIconic`

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（「Appium 层」A1/A3、「范围与非目标」中关于 WebViewDemo 与 MusicStore 的两条、「批 8 实测结论」）

## Global Constraints

- 新写的注释用**英文**；断言里的界面文字按原文（英文按钮名、中文网页文字）。
- 本批**不新增包**，`Directory.Packages.props` 不改。
- 项目放 `tests/Avalonia.<X>.AppiumTests`（`MusicStore` 没有 `Demo` 后缀，项目名是 `Avalonia.MusicStore.AppiumTests`），登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`，与批 8 的项目一起按应用名字母序。
- 运行门控、等待规则沿用批 7、8：`[AppiumFact(App)]`；一律 `AppSession.WaitFor`，不 `Thread.Sleep`；本批没有键入。
- spec 的两条非目标照旧：
  - **MusicStore 只做 A1**：搜索走 iTunes 网络，不点商店、不断言专辑数量（列表从应用目录下的本地缓存读取，数量随之前的运行变化）。
  - **不为网页内容本身写断言**：本地测试页的问候语只用来判断"页面加载完"，桥调用后出现的文字只用来证明调用生效；`NativeWebDialog` 加载公网站点，只断言窗口出现，测试不需要网络。
- `src/` 本批**不改**。破坏检查临时改的行用 `git checkout -- <文件>` 还原，之后 `dotnet build hello-avalonia.slnx` 把 `bin` 里的坏 exe 换掉。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`。提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（本批是 `C:\Temp\probe-b9\`），**永不提交**；探针里指向仓库 `src` 的目录联接用完只删联接本身（`cmd /c rmdir`），绝不递归删除。
- 此前的 523 个 Headless 测试始终保持通过；不设开关时所有 Appium 用例只允许"跳过"。
- 实跑 Appium 前先提醒使用者：运行期间不要动鼠标键盘、不要锁屏。

## Review Focus

按"最可能让使用者踩到"的顺序排列；每条在所属任务里都有对应测试或检查。

1. **弹出的窗口"找不到"被误判成没打开**：WinAppDriver 的 `WindowHandles` 不列有所有者的窗口，`WebViewWindow` 打开后它仍是 1。测试按窗口标题在应用会话里找 `Window` 元素（Task 2 的 A3；破坏检查 W4 证明对话框不弹出时测试会失败）。
2. **点击落在盖住按钮的窗口上**：网页窗口 900×600 以所有者为中心显示，盖住 600×400 主窗口上的按钮 3；有所有者的窗口总在所有者之上。测试先用网页自己的"最小化窗口"按钮让开，并等 `IsIconic` 为真再点（Task 2；探针第三轮复现了点击落空）。
3. **桥只通一个方向**：JS → C#（"检查更新"有回复、"关闭窗口"真的关窗）与 C# → JS（按钮 3 后页面显示结果）分开断言，任一方向断开都只让对应用例失败（破坏检查 W1–W3）。
4. **WebView2 冷启动慢**：首次加载实测约 1.7 s，等页面加载用 15 s 上限，其余等待用默认 3 s（Task 2）。
5. **会话结束后进程还在退出**：WebView2 关闭要几秒，`Dispose` 释放桌面锁时被测进程可能还没退完；全量实跑后确认没有残留的 `Avalonia.*` 与本批启动的 `msedgewebview2`（Task 3）。

---

## 已核实的事实（探针 `C:\Temp\probe-b9\`，2026-10-17，四轮）

- **ControlsDemo**：启动约 5 s；导航树（UIA `Tree`）下 28 个 `TreeItem` 的 `Text` 都为空，分类名与页面名以 `Text` 元素出现；启动时只展开第一个分类，前两个文字是 `Data display`、`Carousel`。
- **HtmlRendererDemo**：`HtmlPanel` 自绘，不在 UIA 树里；`Text` 有 `HtmlPanel (rich text)`、`IconFont`、图标字形、`PathIcon`。
- **MusicStore**：标题 `Avalonia.MusicStore`；一个 `List`（专辑列表）；商店按钮的 UIA 名字是 `Avalonia.Controls.PathIcon`（内容的 `ToString()`），本批不用它。
- **WebViewDemo**：
  - 点按钮 1 后，Win32 `EnumWindows` 在 2 s 内看到 `WebViewWindow`，但 WinAppDriver 的 `WindowHandles` 10 s 内一直是 1；应用会话里 `FindElementsByName("WebViewWindow")` 能找到 `ControlType.Window`（约 0.9 s）。
  - 网页内容经 WebView2 暴露给 UIA：窗口元素下的 `Text` 在约 1.7 s 后出现问候语；网页的树出现两遍，每段文字、每个按钮都能找到两个。
  - 页面"检查更新"→ 约 1.4 s 后出现 `Already up to date (日期时间)`；"关闭窗口"→ 约 0.8 s 后窗口元素消失，Win32 也只剩主窗口。
  - 按钮 3 在网页窗口打开时**点不中**：网页窗口矩形 X −149…753、Y −69…563 盖住按钮 3 的中心 (301, 314)，10 s 内页面没有结果。用页面的"最小化窗口"让开后，约 1.1 s 出现 `C#调用JS: 12 + 13 = 25`，最小化窗口里的文字仍可读。用 Win32 `MoveWindow` 挪开不可靠：要求 X=613，UIA 报 X=278（两套坐标不一致），仍盖着按钮。
  - 按钮 2 的 `NativeWebDialog` 约 0.3 s 出现，Win32 与应用会话（`ControlType.Window`，名字 `NativeWebDialog Demo`）都能看到。
  - 会话结束后 `Avalonia.WebViewDemo` 与 `msedgewebview2` 还要几秒才退出完。
- 元素范围的查找返回 `AppiumWebElement`，不是 `WindowsElement`（编译期 CS0266）。

---

### Task 1: ControlsDemo、HtmlRendererDemo、MusicStore 的 A1

**Files:**
- Create: `tests/Avalonia.ControlsDemo.AppiumTests/`、`tests/Avalonia.HtmlRendererDemo.AppiumTests/`、`tests/Avalonia.MusicStore.AppiumTests/`（各三个文件）
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: 批 7 的 `AppSession`（`Driver`）、`AppiumFactAttribute(string app)`。
- Produces: 无。

- [ ] **Step 1: 建三个项目**

#### `tests/Avalonia.ControlsDemo.AppiumTests/Avalonia.ControlsDemo.AppiumTests.csproj`

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
        <!-- Build order only: the tests launch the app's exe and never load its assembly. -->
        <ProjectReference Include="..\..\src\Avalonia.ControlsDemo\Avalonia.ControlsDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.ControlsDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.ControlsDemo.AppiumTests/SmokeTests.cs`

```csharp
using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.ControlsDemo.AppiumTests
{
    // A1 for the one demo that navigates with a TreeView instead of a TabControl.
    public class SmokeTests
    {
        private const string App = "Avalonia.ControlsDemo";

        // The order of the official Controls sidebar, as Navigation/Categories.cs lists it.
        private static readonly string[] Categories =
        {
            "Data display", "Feedback", "Input", "Layout", "Media", "Menus", "Navigation", "Primitives", "System", "Web",
            "付费控件", "How-to 路标",
        };

        [AppiumFact(App)]
        public void App_starts_with_every_category_in_the_tree_and_the_first_one_open()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia Controls Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            // Tree items carry no text of their own; their headers show up as Text elements under the tree.
            var texts = app.Driver.FindElementByTagName("Tree").FindElementsByTagName("Text").Select(t => t.Text).ToList();
            Assert.Equal(Categories, texts.Where(Categories.Contains));
            Assert.Equal(new[] { "Data display", "Carousel" }, texts.Take(2));
        }
    }
}
```

#### `tests/Avalonia.HtmlRendererDemo.AppiumTests/Avalonia.HtmlRendererDemo.AppiumTests.csproj`

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
        <!-- Build order only: the tests launch the app's exe and never load its assembly. -->
        <ProjectReference Include="..\..\src\Avalonia.HtmlRendererDemo\Avalonia.HtmlRendererDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.HtmlRendererDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.HtmlRendererDemo.AppiumTests/SmokeTests.cs`

```csharp
using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.HtmlRendererDemo.AppiumTests
{
    // A1. HtmlPanel draws its own text and has no UIA children, so the labels around it are what can be checked.
    public class SmokeTests
    {
        private const string App = "Avalonia.HtmlRendererDemo";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_labels()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia HtmlRenderer & IconFont Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            var texts = app.Driver.FindElementsByTagName("Text").Select(t => t.Text).ToList();
            Assert.Contains("HtmlPanel (rich text)", texts);
            Assert.Contains("IconFont", texts);
            Assert.Contains("PathIcon", texts);
        }
    }
}
```

#### `tests/Avalonia.MusicStore.AppiumTests/Avalonia.MusicStore.AppiumTests.csproj`

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
        <!-- Build order only: the tests launch the app's exe and never load its assembly. -->
        <ProjectReference Include="..\..\src\Avalonia.MusicStore\Avalonia.MusicStore.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.MusicStore.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.MusicStore.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.MusicStore.AppiumTests
{
    // A1 only: searching the store goes to the iTunes service, so nothing here depends on the network. The album
    // list is filled from the app's local cache, so its length depends on earlier runs and is not checked.
    public class SmokeTests
    {
        private const string App = "Avalonia.MusicStore";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_album_list()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia.MusicStore", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            Assert.Single(app.Driver.FindElementsByTagName("List"));
        }
    }
}
```

- [ ] **Step 2: 登记到解决方案**

`hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里按应用名字母序插入（分别排在 `BehaviorsDemo`、`GraphicsDemo`、`LayoutDemo` 的 Appium 项目之后）：

```xml
    <Project Path="tests/Avalonia.ControlsDemo.AppiumTests/Avalonia.ControlsDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.HtmlRendererDemo.AppiumTests/Avalonia.HtmlRendererDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.MusicStore.AppiumTests/Avalonia.MusicStore.AppiumTests.csproj" />
```

- [ ] **Step 3: 构建、跳过、实跑**

Run: `dotnet build tests/Avalonia.ControlsDemo.AppiumTests`（另两个同样）
Expected: 0 个错误，新项目 0 个警告。

不设开关 `dotnet test tests/Avalonia.ControlsDemo.AppiumTests --no-build`：`已跳过: 1`。

先提醒使用者，再实跑（PowerShell）：`$env:HELLO_AVALONIA_APPIUM='1'; dotnet test tests/Avalonia.ControlsDemo.AppiumTests`
Expected: 三个项目各 `通过: 1`（约 7 s、3 s、3 s）。

- [ ] **Step 4: 破坏检查**

用 `C:\Temp\probe-b6\break.sh <文件> <sed 表达式> <测试项目>`（先 `export HELLO_AVALONIA_APPIUM=1`），每条都会还原文件。

| 编号 | 文件 | `sed` 表达式 | 预期（实测） |
|---|---|---|---|
| C1 | `src/Avalonia.ControlsDemo/Navigation/Categories.cs` | `s/"Feedback";/"Feedbacks";/` | ControlsDemo 的 1 个失败 |
| H1 | `src/Avalonia.HtmlRendererDemo/Views/MainWindow.axaml` | `s/Text="IconFont"/Text="Icon Font"/` | HtmlRendererDemo 的 1 个失败 |
| M1 | `src/Avalonia.MusicStore/Views/MainWindow.axaml` | `s/Title="Avalonia.MusicStore"/Title="MusicStore"/` | MusicStore 的 1 个失败 |

### Task 2: WebViewDemo 的 A1 与 A3

**Files:**
- Create: `tests/Avalonia.WebViewDemo.AppiumTests/`（`csproj`、`AssemblyInfo.cs`、`SmokeTests.cs`、`WebFlowTests.cs`）
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: 批 7 的 `AppSession`（`Driver`、`ByName`、`static WaitFor(Func<bool>, string, int)`）、`AppiumFactAttribute(string app)`。
- Produces: 无。

A3 的三个用例各开一个会话：
1. `Web_window_shows_the_page_and_the_page_can_call_back_into_the_app`：按钮 1 打开网页窗口并加载测试页 → 页面"检查更新"得到应用的回复 → 页面"关闭窗口"让应用关窗。
2. `Main_window_button_calls_into_the_page`：打开网页窗口 → 用页面的"最小化窗口"让开并等 `IsIconic` → 按钮 3 → 页面显示 `C#调用JS: 12 + 13 = 25`。
3. `Second_button_opens_a_native_web_dialog`：按钮 2 → 出现名为 `NativeWebDialog Demo` 的 `Window` 元素。

- [ ] **Step 1: 建项目**

#### `tests/Avalonia.WebViewDemo.AppiumTests/Avalonia.WebViewDemo.AppiumTests.csproj`

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
        <!-- Build order only: the tests launch the app's exe and never load its assembly. -->
        <ProjectReference Include="..\..\src\Avalonia.WebViewDemo\Avalonia.WebViewDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.WebViewDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.WebViewDemo.AppiumTests/SmokeTests.cs`

```csharp
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
            AppSession.WaitFor(() => app.Driver.FindElementsByName("WebViewWindow").Count == 0, "the app closed the web window");
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
            AppSession.WaitFor(() => app.Driver.FindElementsByName("NativeWebDialog Demo").Any(w => w.TagName == "ControlType.Window"),
                "the NativeWebDialog window is open");
        }

        // WinAppDriver does not list owned windows in WindowHandles, but finds them by title from the app's session.
        private static WindowsElement OpenWebWindow(AppSession app)
        {
            app.ByName(OpenWebView).Click();
            WindowsElement? web = null;
            AppSession.WaitFor(() => (web = app.Driver.FindElementsByName("WebViewWindow").FirstOrDefault()) is not null, "the web window is open");
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

- [ ] **Step 2: 登记到解决方案**

排在 `StylingDemo` 的 Appium 项目之后：

```xml
    <Project Path="tests/Avalonia.WebViewDemo.AppiumTests/Avalonia.WebViewDemo.AppiumTests.csproj" />
```

- [ ] **Step 3: 构建、跳过、实跑**

Run: `dotnet build tests/Avalonia.WebViewDemo.AppiumTests`
Expected: 0 个错误、0 个警告。（`PageButton` 若写成返回 `WindowsElement` 会报 CS0266：元素范围的查找返回 `AppiumWebElement`。）

不设开关：`已跳过: 4`。先提醒使用者，再实跑（PowerShell）：`$env:HELLO_AVALONIA_APPIUM='1'; dotnet test tests/Avalonia.WebViewDemo.AppiumTests`
Expected: `通过: 4`，约 25 s。

- [ ] **Step 4: 破坏检查**

| 编号 | 文件 | `sed` 表达式 | 预期（实测） |
|---|---|---|---|
| W1 | `src/Avalonia.WebViewDemo/Views/WebViewWindow.axaml.cs` | `s/case "checkAppUpdate":/case "checkAppUpdate2":/` | 1 个失败：`Web_window_shows_…` |
| W2 | 同上 | `s/^                Close();/                \/\/ Close();/` | 1 个失败：`Web_window_shows_…` |
| W3 | 同上 | `s/if (message?.Reult != true)/if (message?.Reult == true)/` | 1 个失败：`Main_window_button_calls_into_the_page` |
| W4 | `src/Avalonia.WebViewDemo/ViewModels/MainWindowViewModel.cs` | `76s/dialog.Show(owner);/\/\/ dialog.Show(owner);/` | 1 个失败：`Second_button_opens_a_native_web_dialog` |

W4 只改第 76 行：实跑时主窗口存在，走的是带所有者的分支；第 31 行是按钮 1 的 `Show`，不要动。改之前用 `grep -n 'dialog.Show(' <文件>` 确认行号。

全部还原后 `dotnet build hello-avalonia.slnx`，再跑 Step 3 的实跑命令，Expected: `通过: 4`；`git status --short src` 为空。

### Task 3: 全量验证、提交与回填

**Files:**
- Modify: `README.md`、`docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

- [ ] **Step 1: 开着开关跑整个解决方案**

先提醒使用者，约 9 分钟。Run（PowerShell）：`$env:HELLO_AVALONIA_APPIUM='1'; dotnet test hello-avalonia.slnx --no-build *> C:\Temp\probe-b9\full-live.txt`
Expected: 退出码 0；`[FAIL]` 0 行；通过数合计 566（523 + 批 7 的 6 + 批 8 的 30 + 本批 7）；之后 `Get-Process Avalonia.*` 为空。

再不设开关跑一次：Expected 523 通过、43 跳过、退出码 0。

- [ ] **Step 2: 提交代码**

```bash
git add hello-avalonia.slnx tests/Avalonia.ControlsDemo.AppiumTests tests/Avalonia.HtmlRendererDemo.AppiumTests tests/Avalonia.MusicStore.AppiumTests tests/Avalonia.WebViewDemo.AppiumTests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add Appium tests for the four demos without tabs and the WebView flows" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 3: README 与 spec**

- README 的 `Avalonia.\*Demo.AppiumTests` 一行改为覆盖全部 20 个应用的启动，并写明 WebView 的双向调用与原生对话框。
- spec「批 9 实测结论」：交付、实测数字、破坏检查 7/7、对批 10 有用的发现（有所有者的窗口不在 `WindowHandles` 里、被盖住的元素点不中、WebView2 把网页树暴露两遍）、执行时与计划不符之处。

- [ ] **Step 4: 提交文档**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 9 results" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```
