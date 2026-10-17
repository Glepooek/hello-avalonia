# 演示测试 08：15 个 TabControl 应用的 Appium A1 + A2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 spec 批 8：除 TestingDemo 外的 15 个 `TabControl` 应用，各有一个 `Avalonia.<X>Demo.AppiumTests` 项目，覆盖 A1（启动：标题、单窗口、标签页列表）与 A2（导航：逐个选中标签页，每页都显示自己的内容）。

**Architecture:** 检查逻辑只有一份，放在共享库 `tests/Avalonia.Appium.Common` 的新类 `TabbedApp` 里。
- 每个应用的项目只提供数据：应用名、窗口标题、标签页列表（按 `MainWindow.axaml` 原文），外加两个一行的 `[AppiumFact]`。
- 15 个项目的三个文件（`csproj`、`AssemblyInfo.cs`、`SmokeTests.cs`）形状相同，只有数据不同；本计划逐个列出，可整块抽取。
- 会话照批 7：每个测试一个 `AppSession`，A1 与 A2 各启动一次应用。跨测试进程靠 `AppSession` 的命名 Mutex 排队（批 7 之后的拆分已加，并已实测）。

**Tech Stack:** .NET 10.0、Appium.WebDriver 4.4.5（Selenium 3）、WinAppDriver 1.2.1、xUnit v3（3.2.2，含 `xunit.v3.assert`）

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（「Appium 层」A1/A2、「为什么 Appium 也每个应用一个项目」「批 0 实测结论 / 对批 7–10 的决定」「批 7 实测结论」）

## Global Constraints

- 新写的注释用**英文**；标签页标题、窗口标题按界面原文（中文标题照抄 `MainWindow.axaml` 的 `Header`）。
- 版本只写在根目录 `Directory.Packages.props`。本批新登记 `xunit.v3.assert` 3.2.2（与 `xunit.v3` 同版本，`TabbedApp` 要用 `Assert`）。
- 项目放 `tests/Avalonia.<X>Demo.AppiumTests`，登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`，接在 `Avalonia.TestingDemo.AppiumTests` 之后，按应用名字母序。
  - 对被测应用用 `ProjectReference ... ReferenceOutputAssembly="false"`，只保证构建顺序；再引用 `..\Avalonia.Appium.Common`。
- 运行门控、等待、键入的规则沿用批 7：`[AppiumFact(App)]`；一律 `AppSession.WaitFor`，不 `Thread.Sleep`；本批没有键入。
- **不用 XPath 找子元素**：WinAppDriver 的 `FindElementsByXPath("./*")` 返回的是元素自己（见「已核实的事实」），任何"非空"断言都会假通过。子孙元素一律用元素范围的 `FindElementsByTagName`。
- 标签页从 `FindElementsByTagName("TabItem")` 里按 `Text` 挑，不用 `ByName`：页面里的文字可能与标签页标题相同。
- `src/` 本批**不改**。破坏检查临时改的行用 `git checkout -- <文件>` 还原，并重新构建该应用，免得 `bin` 里留着坏 exe；之后确认没有残留的 `Avalonia.*Demo` 进程。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`。提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（本批是 `C:\Temp\probe-b8\`，含生成脚本 `gen.ps1`），**永不提交**。
- 此前的 523 个 Headless 测试始终保持通过；不设开关时所有 Appium 用例只允许"跳过"。
- 实跑 Appium 前先提醒使用者：运行期间不要动鼠标键盘、不要锁屏。

## Review Focus

按"最可能让使用者踩到"的顺序排列；每条在所属任务里都有对应测试或检查。

1. **页面没换，但树里仍有文字**：UIA 树没刷新，或点击落空、仍停在上一页时，"有可见文字"照样成立。A2 要求每页的文字组与上一页不同（Task 1 的破坏检查「两个标签页放同一页」钉住）。
2. **假通过的子元素查询**：`./*` 返回元素自己，写成"页面下有子元素"永远为真。只用 `FindElementsByTagName`（Task 1 的破坏检查「页面置空」钉住：页面没有文字时 A2 必须失败）。
3. **标签页点不到或点错**：Behaviors 的 15 个竖排标签页超出一列，第 15 个换到第二列；页面文字与标题同名时 `ByName` 可能点到文字。A2 从标签条里按 `Text` 挑并等 `Selected`（Task 2 实跑 Behaviors 全部 15 页；Task 1 的破坏检查「禁用一个标签页」钉住"点了没选中要失败"）。
4. **某页弹出第二个窗口或让应用退出**：A1 只看启动时；A2 走完全部标签页后再断言只有一个窗口，应用退出时后续查找会直接抛异常（Task 2 实跑）。
5. **多个测试进程抢桌面**：`dotnet test hello-avalonia.slnx` 并行启动 16 个 Appium 项目，靠 `AppSession` 的命名 Mutex 排队（Task 2 开着开关跑整个解决方案）。

---

## 已核实的事实（探针 `C:\Temp\probe-b8\`，2026-10-17，两轮各跑 15 个应用）

- 15 个应用共 129 个标签页，数量与 Headless H1 一致；启动 4–8 s，单个应用走完全部标签页 9–29 s。
- 每个标题按名字只匹配到一个元素（`ControlType.TabItem`），129 个标签页全部能点中并在约 300 ms 内 `Selected`。
- Behaviors 的竖排标签页第 15 个（`自定义`）换到第二列（X=189, Y=43），仍可点中。
- 15 个应用启动时与走完全部标签页后都只有 1 个窗口。
- **`FindElementsByXPath("./*")` 返回元素自己**：在 `TabItem` 上得到 `TabItem`、在 `Tab` 上得到 `Tab`，与页面内容无关。
- 元素范围的 `tabControl.FindElementsByTagName("Text")`：每页 1–19 个，选中后第一个就 `Displayed`；最慢 764 ms。标签页标题本身不是 `Text` 元素，`TabItem` 下也没有 `Text`。
- 129 页里相邻两页的文字组没有一次相同，所以"与上一页不同"可以作为"页面真的换了"的判据。
- `TabControl` 的 UIA 标签名是 `Tab`，每个应用恰好一个。

---

### Task 1: `TabbedApp` 与第一个使用者 LayoutDemo

**Files:**
- Modify: `Directory.Packages.props`（登记 `xunit.v3.assert` 3.2.2）
- Modify: `tests/Avalonia.Appium.Common/Avalonia.Appium.Common.csproj`
- Create: `tests/Avalonia.Appium.Common/TabbedApp.cs`
- Create: `tests/Avalonia.LayoutDemo.AppiumTests/`（三个文件）
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: `AppSession`（`Driver`、`static WaitFor(Func<bool>, string, int)`）、`AppiumFactAttribute(string app)`，均来自批 7。
- Produces: `TabbedApp.AssertStartsWithItsTabs(string app, string title, IReadOnlyList<string> tabs)` 与 `TabbedApp.AssertEveryTabShowsItsPage(string app, IReadOnlyList<string> tabs)`，Task 2 的 14 个项目都只调用这两个方法。

- [ ] **Step 1: 登记包并让共享库引用它**

`Directory.Packages.props` 在 `xunit.v3.extensibility.core` 之后加一行：

```xml
        <PackageVersion Include="xunit.v3.assert" Version="3.2.2" />
```

#### `tests/Avalonia.Appium.Common/Avalonia.Appium.Common.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Appium.WebDriver" />
        <PackageReference Include="xunit.v3.extensibility.core" />
        <PackageReference Include="xunit.v3.assert" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 写 `TabbedApp`**

A1 断言标题、单窗口和标签页列表（顺序也要对）。A2 逐个选中标签页，等到页面上有可见文字、且文字组与上一页不同；走完后再断言只有一个窗口。

#### `tests/Avalonia.Appium.Common/TabbedApp.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.Appium.Common
{
    // The A1 and A2 checks every TabControl demo shares; each app's test project only supplies its title and tabs.
    public static class TabbedApp
    {
        // A1: the app opens one main window with the expected title and exactly these tabs, in this order.
        public static void AssertStartsWithItsTabs(string app, string title, IReadOnlyList<string> tabs)
        {
            using var session = new AppSession(app);

            Assert.Equal(title, session.Driver.Title);
            Assert.Single(session.Driver.WindowHandles);
            Assert.Equal(tabs, session.Driver.FindElementsByTagName("TabItem").Select(t => t.Text));
        }

        // A2: each tab can be selected and then shows visible text of its own. Comparing with the previous tab's
        // texts proves the page really swapped; a stale tree would still have visible text.
        public static void AssertEveryTabShowsItsPage(string app, IReadOnlyList<string> tabs)
        {
            using var session = new AppSession(app);
            var tabControl = session.Driver.FindElementByTagName("Tab");
            IReadOnlyList<string>? previous = null;

            foreach (var header in tabs)
            {
                // Picked from the tab strip, not by name, so text on a page can never be clicked instead.
                var tab = session.Driver.FindElementsByTagName("TabItem").Single(t => t.Text == header);
                tab.Click();
                AppSession.WaitFor(() => tab.Selected, $"tab {header} is selected");

                IReadOnlyList<string> texts = Array.Empty<string>();
                AppSession.WaitFor(() =>
                {
                    var found = tabControl.FindElementsByTagName("Text");
                    texts = found.Select(e => e.Text ?? "").ToList();
                    return found.Any(e => e.Displayed) && (previous is null || !texts.SequenceEqual(previous));
                }, $"tab {header} shows visible text that differs from the previous tab");
                previous = texts;
            }

            Assert.Single(session.Driver.WindowHandles);
        }
    }
}
```

- [ ] **Step 3: 建 LayoutDemo 的 Appium 项目**

#### `tests/Avalonia.LayoutDemo.AppiumTests/Avalonia.LayoutDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.LayoutDemo\Avalonia.LayoutDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.LayoutDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.LayoutDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.LayoutDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.LayoutDemo";
        private const string Title = "Avalonia Layout Demo";
        private static readonly string[] Tabs = { "布局面板", "定位与间距", "响应式布局" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

- [ ] **Step 4: 登记到解决方案**

`hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.AppiumTests` 之后加（Task 2 的 14 个项目也按字母序插在这里，LayoutDemo 排在 InputDemo 之后）：

```xml
    <Project Path="tests/Avalonia.LayoutDemo.AppiumTests/Avalonia.LayoutDemo.AppiumTests.csproj" />
```

- [ ] **Step 5: 构建并确认不设开关时跳过**

Run: `dotnet build tests/Avalonia.LayoutDemo.AppiumTests`
Expected: 0 个错误、0 个警告。

Run（不设 `HELLO_AVALONIA_APPIUM`）: `dotnet test tests/Avalonia.LayoutDemo.AppiumTests --no-build`
Expected: `已跳过: 2`，退出码 0。

- [ ] **Step 6: 实跑**

先提醒使用者不要动鼠标键盘，确认 WinAppDriver 在监听（`Test-NetConnection 127.0.0.1 -Port 4723 -InformationLevel Quiet` 为 `True`）。

Run（PowerShell）: `$env:HELLO_AVALONIA_APPIUM='1'; dotnet test tests/Avalonia.LayoutDemo.AppiumTests`
Expected: `通过: 2`，约 12 s。

- [ ] **Step 7: 破坏检查**

每条只改 `src/Avalonia.LayoutDemo/Views/MainWindow.axaml` 一行，跑 Step 6 的命令，然后 `git checkout -- src/Avalonia.LayoutDemo/Views/MainWindow.axaml`。可用 `C:\Temp\probe-b6\break.sh`（先 `export HELLO_AVALONIA_APPIUM=1`）。

| 编号 | `sed` 表达式 | 预期（实测） | 钉住 |
|---|---|---|---|
| B1 | `s/Header="响应式布局"/Header="响应布局"/` | 2 个失败：A1 列表不符；A2 在标签条里找不到这个标题 | A1 的标签页列表 |
| B2 | `s/<pages:PositioningPage \/>/<pages:PanelsPage \/>/` | 1 个失败（A2）：第 2 页文字与第 1 页相同 | Review Focus 1 |
| B3 | `s/<pages:ResponsivePage \/>/<Border \/>/` | 1 个失败（A2）：第 3 页没有文字 | Review Focus 2 |
| B4 | `s/<TabItem Header="定位与间距">/<TabItem Header="定位与间距" IsEnabled="False">/` | 1 个失败（A2）：`Not true after 3000 ms: tab 定位与间距 is selected.` | Review Focus 3 |

全部还原后，重新跑一次 Step 6 的命令（它会重新构建 LayoutDemo，把 `bin` 里的坏 exe 换掉），Expected: `通过: 2`；`git status --short src` 为空。

- [ ] **Step 8: 提交**

本批的 15 个项目在 Task 2 结束后与 Task 1 一起提交（见 Task 2 Step 4），Task 1 不单独提交：只有 LayoutDemo 的中间状态没有独立价值。

### Task 2: 其余 14 个应用

**Files:**
- Create: `tests/Avalonia.<X>Demo.AppiumTests/`（每个三个文件，共 14 个项目）
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 1 的 `TabbedApp.AssertStartsWithItsTabs`、`TabbedApp.AssertEveryTabShowsItsPage`。
- Produces: 无（后续批 10 的 A3 流程可以往这些项目里加测试类）。

三个文件的形状与 Task 1 的 LayoutDemo 完全相同，只有应用名、标题和标签页不同。标题与标签页照抄各应用 `Views/MainWindow.axaml` 的 `Title` 和 `TabItem Header`，顺序不变。

- [ ] **Step 1: 建 14 个项目**

#### `tests/Avalonia.AppDevelopmentDemo.AppiumTests/Avalonia.AppDevelopmentDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.AppDevelopmentDemo\Avalonia.AppDevelopmentDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.AppDevelopmentDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.AppDevelopmentDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.AppDevelopmentDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.AppDevelopmentDemo";
        private const string Title = "Avalonia App Development Demo";
        private static readonly string[] Tabs = { "依赖注入", "本地化", "Web 内容", "日志", "未处理异常", "资源", "数据校验", "线程模型", "窗口管理", "无障碍" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.BehaviorsDemo.AppiumTests/Avalonia.BehaviorsDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.BehaviorsDemo\Avalonia.BehaviorsDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.BehaviorsDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.BehaviorsDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.BehaviorsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.BehaviorsDemo";
        private const string Title = "Avalonia Behaviors Demo";
        private static readonly string[] Tabs = { "机制", "事件转命令", "数据驱动", "焦点键盘指针", "表单验证", "集合", "窗口弹出通知", "拖拽", "响应式", "动画", "文件系统", "网络", "剪贴板", "截图", "自定义" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.CustomControlsDemo.AppiumTests/Avalonia.CustomControlsDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.CustomControlsDemo\Avalonia.CustomControlsDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.CustomControlsDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.CustomControlsDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.CustomControlsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.CustomControlsDemo";
        private const string Title = "Avalonia Custom Controls Demo";
        private static readonly string[] Tabs = { "用户控件", "模板化控件", "自绘控件", "属性与事件", "控件树", "自定义面板", "自定义 Flyout" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.DataBindingDemo.AppiumTests/Avalonia.DataBindingDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.DataBindingDemo\Avalonia.DataBindingDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.DataBindingDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.DataBindingDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.DataBindingDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.DataBindingDemo";
        private const string Title = "Avalonia Data Binding Demo";
        private static readonly string[] Tabs = { "绑定语法", "编译绑定", "集合", "主从视图", "多值绑定", "命令", "转换器", "校验", "集合视图", "异步与图片", "绑定调试", "标记扩展" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.DataTemplatesDemo.AppiumTests/Avalonia.DataTemplatesDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.DataTemplatesDemo\Avalonia.DataTemplatesDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.DataTemplatesDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.DataTemplatesDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.DataTemplatesDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.DataTemplatesDemo";
        private const string Title = "Avalonia Data Templates Demo";
        private static readonly string[] Tabs = { "控件内容", "内联模板", "模板集合", "选择器", "代码建模板", "复用", "ViewLocator", "面板与树", "对比 ControlTemplate" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.EventsDemo.AppiumTests/Avalonia.EventsDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.EventsDemo\Avalonia.EventsDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.EventsDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.EventsDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.EventsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.EventsDemo";
        private const string Title = "Avalonia Events Demo";
        private static readonly string[] Tabs = { "生命周期", "输入事件", "路由三阶段", "Handled", "自定义路由事件" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.FundamentalsDemo.AppiumTests/Avalonia.FundamentalsDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.FundamentalsDemo\Avalonia.FundamentalsDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.FundamentalsDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.FundamentalsDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.FundamentalsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.FundamentalsDemo";
        private const string Title = "Avalonia Fundamentals Demo";
        private static readonly string[] Tabs = { "纯代码 UI", "Code-behind", "MVVM", "TopLevel", "视觉树与逻辑树", "生命周期与资源" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.GraphicsDemo.AppiumTests/Avalonia.GraphicsDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.GraphicsDemo\Avalonia.GraphicsDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.GraphicsDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.GraphicsDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.GraphicsDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.GraphicsDemo";
        private const string Title = "Avalonia Graphics Demo";
        private static readonly string[] Tabs = { "画刷与渐变", "变换", "形状与几何", "自定义绘制", "特效", "裁剪与命中", "图标", "渲染选项", "关键帧动画", "控件过渡", "页面过渡", "缓动函数", "合成动画" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.InputDemo.AppiumTests/Avalonia.InputDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.InputDemo\Avalonia.InputDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.InputDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.InputDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.InputDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.InputDemo";
        private const string Title = "Avalonia Input Demo";
        private static readonly string[] Tabs = { "指针", "焦点", "手势", "键盘与 HotKey", "交互的写法", "拖放", "文本输入", "路由事件" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.PropertySystemDemo.AppiumTests/Avalonia.PropertySystemDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.PropertySystemDemo\Avalonia.PropertySystemDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.PropertySystemDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.PropertySystemDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.PropertySystemDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.PropertySystemDemo";
        private const string Title = "Avalonia Property System Demo";
        private static readonly string[] Tabs = { "StyledProperty", "DirectProperty", "附加属性", "值优先级", "元数据与回调" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.ServicesDemo.AppiumTests/Avalonia.ServicesDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.ServicesDemo\Avalonia.ServicesDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.ServicesDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.ServicesDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.ServicesDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.ServicesDemo";
        private const string Title = "Avalonia Services Demo";
        private static readonly string[] Tabs = { "剪贴板", "文件对话框", "StorageProvider", "焦点管理", "Launcher", "平台设置", "移动端服务", "可激活生命周期" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.StarterTutorialDemo.AppiumTests/Avalonia.StarterTutorialDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.StarterTutorialDemo\Avalonia.StarterTutorialDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.StarterTutorialDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.StarterTutorialDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.StarterTutorialDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.StarterTutorialDemo";
        private const string Title = "Avalonia Starter Tutorial Demo";
        private static readonly string[] Tabs = { "1 添加控件", "2 添加布局", "3 自定义窗口", "4 事件与响应", "5 数据转换", "6 练习" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.StylingDemo.AppiumTests/Avalonia.StylingDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.StylingDemo\Avalonia.StylingDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.StylingDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.StylingDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.StylingDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.StylingDemo";
        private const string Title = "Avalonia Styling Demo";
        private static readonly string[] Tabs = { "选择器", "样式类", "伪类", "ControlTheme", "主题变体", "容器查询", "字体", "样式共享", "优先级" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

#### `tests/Avalonia.XamlDemo.AppiumTests/Avalonia.XamlDemo.AppiumTests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.XamlDemo\Avalonia.XamlDemo.csproj" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\Avalonia.Appium.Common\Avalonia.Appium.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.XamlDemo.AppiumTests/AssemblyInfo.cs`

```csharp
using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

#### `tests/Avalonia.XamlDemo.AppiumTests/SmokeTests.cs`

```csharp
using Avalonia.Appium.Common;

namespace Avalonia.XamlDemo.AppiumTests
{
    // A1 and A2 for a TabControl demo; the checks themselves live in TabbedApp.
    public class SmokeTests
    {
        private const string App = "Avalonia.XamlDemo";
        private const string Title = "Avalonia XAML Demo";
        private static readonly string[] Tabs = { "命名空间", "x: 指令", "标记扩展", "类型转换器", "泛型", "XAML 编译", "平台相关 XAML" };

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_every_tab() => TabbedApp.AssertStartsWithItsTabs(App, Title, Tabs);

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_page() => TabbedApp.AssertEveryTabShowsItsPage(App, Tabs);
    }
}
```

- [ ] **Step 2: 登记到解决方案并构建**

`hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.AppiumTests` 之后按字母序共 15 行（含 Task 1 的 LayoutDemo）：

```xml
    <Project Path="tests/Avalonia.AppDevelopmentDemo.AppiumTests/Avalonia.AppDevelopmentDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.BehaviorsDemo.AppiumTests/Avalonia.BehaviorsDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.CustomControlsDemo.AppiumTests/Avalonia.CustomControlsDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.DataBindingDemo.AppiumTests/Avalonia.DataBindingDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.DataTemplatesDemo.AppiumTests/Avalonia.DataTemplatesDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.EventsDemo.AppiumTests/Avalonia.EventsDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.FundamentalsDemo.AppiumTests/Avalonia.FundamentalsDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.GraphicsDemo.AppiumTests/Avalonia.GraphicsDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.InputDemo.AppiumTests/Avalonia.InputDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.LayoutDemo.AppiumTests/Avalonia.LayoutDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.PropertySystemDemo.AppiumTests/Avalonia.PropertySystemDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.ServicesDemo.AppiumTests/Avalonia.ServicesDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.StarterTutorialDemo.AppiumTests/Avalonia.StarterTutorialDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.StylingDemo.AppiumTests/Avalonia.StylingDemo.AppiumTests.csproj" />
    <Project Path="tests/Avalonia.XamlDemo.AppiumTests/Avalonia.XamlDemo.AppiumTests.csproj" />
```

Run: `dotnet build hello-avalonia.slnx`
Expected: 0 个错误；新项目 0 个警告（`src/` 与 `Avalonia.MusicStore.Tests` 里既有的警告不算，见 spec「批 3 实测结论」关于口径的说明）。

核对数据（Bash，每个应用应输出 `ok`）：

```bash
for a in AppDevelopment Behaviors CustomControls DataBinding DataTemplates Events Fundamentals Graphics Input Layout PropertySystem Services StarterTutorial Styling Xaml; do x=$(grep -o 'TabItem Header="[^"]*"' src/Avalonia.${a}Demo/Views/MainWindow.axaml | sed 's/TabItem Header=//' | paste -sd, | sed 's/,/, /g'); t=$(grep -o 'Title="[^"]*"' src/Avalonia.${a}Demo/Views/MainWindow.axaml | head -1 | sed 's/Title=//'); f=tests/Avalonia.${a}Demo.AppiumTests/SmokeTests.cs; grep -qF "Tabs = { $x };" $f && grep -qF "Title = $t;" $f && echo -n "ok " || echo -n "MISMATCH:$a "; done; echo
```

- [ ] **Step 3: 全量实跑（开着开关跑整个解决方案）**

这一步同时验证 Review Focus 4、5：16 个 Appium 项目在并行进程里排队使用桌面，523 个 Headless 测试同时在跑。先提醒使用者不要动鼠标键盘，约 8 分钟。

Run（PowerShell）: `$env:HELLO_AVALONIA_APPIUM='1'; dotnet test hello-avalonia.slnx --no-build *> C:\Temp\probe-b8\full-live.txt`
Expected: 退出码 0；`[FAIL]` 0 行；通过数合计 559（523 + TestingDemo 6 + 本批 30）；结束后 `Get-Process Avalonia.*` 为空。

再不设开关跑一次：`dotnet test hello-avalonia.slnx --no-build`
Expected: 523 通过，36 跳过（Appium 用例），退出码 0。

- [ ] **Step 4: 提交代码**

```bash
git add Directory.Packages.props hello-avalonia.slnx tests/Avalonia.Appium.Common tests/Avalonia.*Demo.AppiumTests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add Appium launch and tab tests for the 15 tabbed demos" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

### Task 3: 文档与回填

**Files:**
- Modify: `README.md`（Appium 一行的覆盖范围）
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（「批 8 实测结论」）

- [ ] **Step 1: README**

把 `Avalonia.\*Demo.AppiumTests` 一行里的「目前覆盖 TestingDemo 的启动、标签页与三个面板的键鼠流程」改为「目前覆盖 16 个 TabControl 应用的启动与逐页导航，以及 TestingDemo 三个面板的键鼠流程」。

- [ ] **Step 2: 回填 spec「批 8 实测结论」**

写入：交付（15 个项目、30 个用例、`TabbedApp`、新登记的包）；实测数字（Step 3 的通过数、用时、破坏检查 4/4）；「已核实的事实」里对批 9–10 有用的结论（`./*` 陷阱、`Text` 元素可作页面内容判据、标签页按标签条挑选）；执行时与计划不符之处。

- [ ] **Step 3: 提交文档**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 8 results" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```
