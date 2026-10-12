# 演示测试 00：共享测试库、TestingDemo 迁移与 Appium 探针 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 `tests/Avalonia.Tests.Common` 共享测试库（`HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`），把 `Avalonia.TestingDemo.Tests` 迁到它上面且原有 24 个测试保持通过；再用仓库外探针回答 Appium 层能否按 spec 走（P1–P4），并把结论回填 spec。

**Architecture:** `Avalonia.Tests.Common` 是普通类库（不是 Exe，不带 xunit），只依赖 `Avalonia.Headless` 与 `Avalonia.Skia`，供后续 19 个 Headless 测试项目引用。每个应用的测试项目仍各自持有一个 `[assembly: AvaloniaTestApplication]`（程序集只能有一个），其 `TestAppBuilder` 只剩一行 `HeadlessApp.Build<App>()`。Appium 探针放在仓库外的 `C:\Temp\probe-appium`，不入库。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless / Avalonia.Skia、xUnit v3（3.2.2）、Appium.WebDriver 4.4.5、WinAppDriver 1.2.1

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文。
- 版本只写在根目录 `Directory.Packages.props`；csproj 的 `PackageReference` **不带 `Version`**，新增包先在那里登记。
- 代码风格沿用 `tests/Avalonia.TestingDemo.Tests` 现有文件：块作用域命名空间（`namespace X { ... }`）、测试方法名用英文下划线句式。
- `Avalonia.Tests.Common` 不是测试项目：不设 `OutputType=Exe`，不引用 `xunit.*`，所有公共类型都是 `public`。
- `Avalonia.TestingDemo.Tests` 里**原有的 24 个测试不改一行逻辑**，迁移前后都必须是 24 通过、0 失败。
- 目录约定：库和演示在 `src/`，测试和测试库在 `tests/`；新项目要登记到 `hello-avalonia.slnx` 对应的 `<Folder>`。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`。
- 探针放在仓库外（`C:\Temp\probe-*`），**永不提交**。
- 移动或重命名已构建过的项目目录前，先 `dotnet build-server shutdown`，否则 `git mv` 会因文件被占用报 `Permission denied`。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. 窗口里**没有 `TabControl`** 时，`TabWalker.Walk` 应抛带明确信息的 `InvalidOperationException`，而不是 `NullReferenceException`（Task 2）。
2. 某个标签页的内容在**布局时抛异常**：应记录在该页的 `Error` 里，并继续走完后面的标签页，而不是整个遍历中断（Task 2）。
3. `TabItem` 没有 `Header`（或不是 `TabItem`）：用 `#<序号>` 兜底命名，不崩溃（Task 2）。
4. `LogCollector` 用 `using` 离开作用域后必须**还原**原来的 `Logger.Sink`，否则一个测试会污染后面所有测试的日志（Task 2）。
5. 迁移后 `TestingDemo.Tests` 的 24 个测试逐个仍通过，包括依赖 Skia 真实像素的 `RenderSnapshotTests`——它们最容易因为 `Avalonia.Skia` 没带过来而退化成 1×1 位图（Task 1）。

---

## 本份已核实的事实（编写计划时在仓库外的临时项目里实测）

| 事项 | 实测结果 | 对计划的影响 |
|---|---|---|
| `Common` 的 4 个文件只引用 `Avalonia.Headless` + `Avalonia.Skia`（无 xunit）能编译 | 通过（`C:\Temp\probe-common-lib`） | Common 的 csproj 只需这两个包 |
| `HeadlessApp.Build<TApp>()` + `UiHelpers` + `TabWalker` 配合 `TestingDemo` 的 `App` 跑 `[AvaloniaFact]` | 4 个探针用例全过 | Task 2 的代码可直接使用 |
| `TabWalker` 对 `TestingDemo` 的 `MainWindow` | 依次得到 `计数器`、`表单`、`列表`，均无异常 | Task 2 的断言 |
| `TabWalker` 对 `LayoutDemo` 的 `MainWindow`（用 LayoutDemo 自己的 `App`） | 依次得到 `布局面板`、`定位与间距`、`响应式布局`，均无异常 | **spec 探针 P5 已通过**，Task 3 不再重复 |
| 某标签页内容在 `MeasureOverride` 里抛异常 | `window.UpdateLayout()` 把异常直接抛出，可被 `try/catch` 捕获，之后的标签页仍可继续 | Review Focus 第 2 条成立 |
| 窗口里没有 `TabControl` | `FirstOrDefault() ?? throw` 抛 `InvalidOperationException` | Review Focus 第 1 条成立 |
| `Logger.Sink = new LogCollector()` 收集绑定警告 | 对 `new Binding("Missing")`（运行时绑定）收到一条：`Binding: An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message} [Text, Missing, Missing, Could not find a matching property accessor for 'Missing' on 'System.Object'.]` | **spec 探针 P6 对运行时绑定成立**；编译绑定下的"结构体属性静默失败"**未验证**，见 Task 2 说明 |
| `Appium.WebDriver` 4.4.5 在 net10 上还原并编译 | 通过，无警告（`C:\Temp\probe-appium`） | **P1 的编译部分已通过**；能否连上 WinAppDriver 仍未验证（Task 3） |
| `WinAppDriver.exe` | 本机已装于 `C:\Program Files\Windows Application Driver\`；开发者模式已开；端口 4723 当前无进程监听 | Task 3 需要使用者先启动它；我在编写计划时启动被拒绝，所以 P1 的运行时部分、P2–P4 都**没有**实测 |
| `Avalonia.Skia`、`Avalonia.Headless` | `Directory.Packages.props` 里**没有**登记（只有 `Avalonia.Headless.XUnit`） | Task 1 必须先登记 |
| `C:\Temp\extract-plan.ps1` | 本机不存在 | 计划里的代码块照约定标注路径，但需要手动落盘或补回脚本 |

## 文件结构

**新增**

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.Tests.Common/Avalonia.Tests.Common.csproj` | 共享测试库工程 |
| `tests/Avalonia.Tests.Common/HeadlessApp.cs` | `Build<TApp>()`：统一的 Skia + Headless 应用构建器 |
| `tests/Avalonia.Tests.Common/UiHelpers.cs` | 从 `TestingDemo.Tests` 迁入：`Show`、`ByName`、`ByAutomationId`、`Click`、`Type` |
| `tests/Avalonia.Tests.Common/TabWalker.cs` | 遍历窗口里第一个 `TabControl` 的全部标签页 |
| `tests/Avalonia.Tests.Common/LogCollector.cs` | 收集 Warning 及以上的 Avalonia 日志 |
| `tests/Avalonia.TestingDemo.Tests/TabWalkerTests.cs` | `TabWalker` 的 4 个测试（其中第 1 个同时是 TestingDemo 的 H1 冒烟） |
| `tests/Avalonia.TestingDemo.Tests/LogCollectorTests.cs` | `LogCollector` 的 2 个测试 |

**修改**

| 文件 | 改动 |
|---|---|
| `Directory.Packages.props` | 登记 `Avalonia.Headless`、`Avalonia.Skia`、`Appium.WebDriver` |
| `hello-avalonia.slnx` | `/tests/` 文件夹加入 Common |
| `tests/Avalonia.TestingDemo.Tests/Avalonia.TestingDemo.Tests.csproj` | 加对 Common 的 `ProjectReference` |
| `tests/Avalonia.TestingDemo.Tests/TestAppBuilder.cs` | 改为调用 `HeadlessApp.Build<App>()` |
| `tests/Avalonia.TestingDemo.Tests/ControlQueryTests.cs`、`InteractionTests.cs` | 各加一行 `using Avalonia.Tests.Common;` |
| `CLAUDE.md` | 项目结构里补一条 Common 的用法 |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 0 实测结论」；批 6 去掉已完成的 TestingDemo H1 |

**删除**

| 文件 | 说明 |
|---|---|
| `tests/Avalonia.TestingDemo.Tests/UiHelpers.cs` | 用 `git mv` 移到 Common，保留历史 |

**仓库外（不提交）**

| 文件 | 说明 |
|---|---|
| `C:\Temp\probe-appium\probe-appium.csproj`、`Probes.cs` | Appium 探针 P1–P4 |

---

### Task 1: 建 `Avalonia.Tests.Common` 并把 TestingDemo.Tests 迁上去

**Files:**
- Modify: `Directory.Packages.props`
- Create: `tests/Avalonia.Tests.Common/Avalonia.Tests.Common.csproj`、`HeadlessApp.cs`
- Move + Modify: `tests/Avalonia.TestingDemo.Tests/UiHelpers.cs` → `tests/Avalonia.Tests.Common/UiHelpers.cs`
- Modify: `hello-avalonia.slnx`、`tests/Avalonia.TestingDemo.Tests/Avalonia.TestingDemo.Tests.csproj`、`TestAppBuilder.cs`、`ControlQueryTests.cs`、`InteractionTests.cs`

**Interfaces:**
- Consumes: 无（第一个任务）。
- Produces（后续所有测试项目依赖）：
  - `Avalonia.Tests.Common.HeadlessApp.Build<TApp>() : AppBuilder`，约束 `where TApp : Application, new()`
  - `Avalonia.Tests.Common.UiHelpers`（`public static`）：`Window Show(Control page)`、`T ByName<T>(this Visual root, string name)`、`T ByAutomationId<T>(this Visual root, string id)`、`void Click(this Window window, Control control)`、`void Type(this Window window, Control control, string text)`；`T` 均 `where T : Control`，签名与行为与迁移前完全一致

迁移的回归基线是 `TestingDemo.Tests` 原有的 24 个测试：本任务不新增测试，成败判据就是"迁移前后都是 24 通过"。

- [ ] **Step 1: 记录迁移前基线**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `已通过! - 失败: 0，通过: 24，已跳过: 0，总计: 24`。若不是 24，先停下排查，不要继续迁移。

- [ ] **Step 2: 登记 `Avalonia.Headless` 与 `Avalonia.Skia`**

在 `Directory.Packages.props` 里 `Avalonia.Headless.XUnit` 那一行**之前**插入两行（缩进 8 个空格，与相邻行一致）：

```xml
        <PackageVersion Include="Avalonia.Headless" Version="12.1.2" />
        <PackageVersion Include="Avalonia.Skia" Version="12.1.2" />
```

- [ ] **Step 3: 创建 Common 工程与 `HeadlessApp`**

#### `tests/Avalonia.Tests.Common/Avalonia.Tests.Common.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia.Headless" />
        <PackageReference Include="Avalonia.Skia" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.Tests.Common/HeadlessApp.cs`

```csharp
using Avalonia.Headless;

namespace Avalonia.Tests.Common
{
    public static class HeadlessApp
    {
        // UseSkia + UseHeadlessDrawing=false is what makes CaptureRenderedFrame return real pixels;
        // the default headless drawing produces 1x1 bitmaps.
        public static AppBuilder Build<TApp>() where TApp : Application, new() => AppBuilder.Configure<TApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
```

- [ ] **Step 4: 用 `git mv` 迁移 `UiHelpers.cs`，再覆盖内容**

Run:

```bash
dotnet build-server shutdown
git mv tests/Avalonia.TestingDemo.Tests/UiHelpers.cs tests/Avalonia.Tests.Common/UiHelpers.cs
```

然后整文件覆盖为下面的内容。与原文件相比只有两处变化：`internal` 改 `public`，命名空间改为 `Avalonia.Tests.Common`；方法体与注释一字不改。

#### `tests/Avalonia.Tests.Common/UiHelpers.cs`

```csharp
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.Tests.Common
{
    // Shared helpers for the headless UI tests.
    public static class UiHelpers
    {
        public static Window Show(Control page)
        {
            var window = new Window { Content = page, Width = 600, Height = 480 };
            window.Show();
            return window;
        }

        public static T ByName<T>(this Visual root, string name) where T : Control
            => root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        public static T ByAutomationId<T>(this Visual root, string id) where T : Control
            => root.GetVisualDescendants().OfType<T>().First(c => AutomationProperties.GetAutomationId(c) == id);

        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        public static void Click(this Window window, Control control)
        {
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
        }

        // Focus first, then send text: KeyPressQwerty alone produces no TextInput.
        public static void Type(this Window window, Control control, string text)
        {
            control.Focus();
            window.KeyTextInput(text);
        }
    }
}
```

- [ ] **Step 5: 把 Common 登记进解决方案**

在 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.Tests` 那行**之前**加一行：

```xml
    <Project Path="tests/Avalonia.Tests.Common/Avalonia.Tests.Common.csproj" />
```

- [ ] **Step 6: 让 TestingDemo.Tests 引用 Common，并把 `TestAppBuilder` 收成一行**

#### `tests/Avalonia.TestingDemo.Tests/Avalonia.TestingDemo.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <!-- xunit v3 test projects are executables, and Avalonia.Headless.XUnit 12.x is built on v3. -->
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia.Headless.XUnit" />
        <PackageReference Include="xunit.v3" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Avalonia.TestingDemo\Avalonia.TestingDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.TestingDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.TestingDemo;
using Avalonia.TestingDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.TestingDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

- [ ] **Step 7: 给两个测试文件各补一行 using**

用 Edit 工具，在 `ControlQueryTests.cs` 和 `InteractionTests.cs` 里，紧跟 `using Avalonia.TestingDemo.Views.Pages;` 之后各加一行：

```csharp
using Avalonia.Tests.Common;
```

然后确认没有遗漏的调用方：

Run: `grep -rn "UiHelpers" tests/Avalonia.TestingDemo.Tests`
Expected: 只剩 `ControlQueryTests.cs`、`InteractionTests.cs` 里的调用，且这两个文件都有上面那行 using；`RenderSnapshotTests.cs` 与 `ViewModelTests.cs` 不应出现。

- [ ] **Step 8: 构建并跑全部测试**

Run: `dotnet build tests/Avalonia.TestingDemo.Tests`
Expected: 0 错误。

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 24，失败: 0`。

再单独确认最容易因为 Skia 没带过来而退化的那组：

Run: `dotnet test tests/Avalonia.TestingDemo.Tests --filter "FullyQualifiedName~RenderSnapshotTests"`
Expected: 全部通过。若失败且信息里提到 1×1 位图，说明 `UseSkia()` 没生效，检查 Common 是否真的引用了 `Avalonia.Skia`。

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props hello-avalonia.slnx tests/Avalonia.Tests.Common tests/Avalonia.TestingDemo.Tests
git status --short
git commit -m "test: add Avalonia.Tests.Common and move TestingDemo.Tests onto it"
```

`git status --short` 里 `UiHelpers.cs` 应显示为 `R`（重命名）。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 2: `TabWalker` 与 `LogCollector`（TDD）

**Files:**
- Create: `tests/Avalonia.TestingDemo.Tests/TabWalkerTests.cs`、`LogCollectorTests.cs`
- Create: `tests/Avalonia.Tests.Common/TabWalker.cs`、`LogCollector.cs`

**Interfaces:**
- Consumes: Task 1 的 `UiHelpers.Show(Control)`（测试里用）。
- Produces（批 1–6 的冒烟测试依赖）：
  - `public sealed record TabVisit(int Index, string Header, Exception? Error)`
  - `public static class TabWalker`：`IReadOnlyList<TabVisit> Walk(Window window)`
    - 取窗口视觉树里**第一个** `TabControl`；没有则抛 `InvalidOperationException`，消息含 `TabControl`
    - 依次选中每个标签页，每次后执行 `Dispatcher.UIThread.RunJobs()` 与 `window.UpdateLayout()`；单页抛出的异常记入该页的 `Error`，**继续**走后面的页
    - 标签页标题取 `TabItem.Header?.ToString()`，取不到则用 `#<序号>`
    - 遍历结束后选中的是最后一页，调用方不要依赖遍历前的选中状态
  - `public sealed class LogCollector : ILogSink, IDisposable`：构造时把自己设为 `Logger.Sink`，`Dispose` 时还原；`List<string> Messages` 收集 Warning 及以上，格式 `"<area>: <模板>"`（带参数时末尾追加 ` [<参数, 逗号分隔>]`）

**关于 P6 的范围：** `LogCollector` 在编写计划时已对**运行时绑定**（`new Binding("Missing")`）实测有效。spec 里"绑定到结构体属性静默失败"的编译绑定场景**没有验证**，所以本任务只交付独立的收集器，**不**把它接进 `TabWalker`。是否在冒烟测试里断言"没有绑定警告"留给批 1：演示项目里可能有专门演示绑定错误的页面（如 `DataBindingDemo` 的"绑定调试"），直接断言可能误报，需要逐页确认后再定。

- [ ] **Step 1: 先写失败的测试**

#### `tests/Avalonia.TestingDemo.Tests/TabWalkerTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestingDemo.Views;
using Avalonia.Tests.Common;
using System;
using System.Linq;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class TabWalkerTests
    {
        // Stands in for a demo page that breaks as soon as it is laid out.
        private sealed class BrokenControl : Control
        {
            protected override Size MeasureOverride(Size availableSize) => throw new InvalidOperationException("broken page");
        }

        [AvaloniaFact]
        public void Walk_visits_every_tab_of_the_main_window()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "计数器", "表单", "列表" }, visits.Select(v => v.Header));
            Assert.Equal(new[] { 0, 1, 2 }, visits.Select(v => v.Index));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        [AvaloniaFact]
        public void Walk_reports_a_tab_that_throws_and_keeps_going()
        {
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Header = "ok", Content = new TextBlock { Text = "fine" } });
            tabs.Items.Add(new TabItem { Header = "bad", Content = new BrokenControl() });
            tabs.Items.Add(new TabItem { Header = "after", Content = new TextBlock { Text = "still fine" } });
            var window = UiHelpers.Show(tabs);

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "ok", "bad", "after" }, visits.Select(v => v.Header));
            Assert.Null(visits[0].Error);
            Assert.IsType<InvalidOperationException>(visits[1].Error);
            Assert.Null(visits[2].Error);
        }

        [AvaloniaFact]
        public void Walk_throws_a_clear_error_when_there_is_no_tab_control()
        {
            var window = UiHelpers.Show(new TextBlock { Text = "no tabs here" });

            var ex = Assert.Throws<InvalidOperationException>(() => TabWalker.Walk(window));

            Assert.Contains("TabControl", ex.Message);
        }

        [AvaloniaFact]
        public void Walk_names_a_tab_without_a_header_by_its_position()
        {
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Content = new TextBlock { Text = "no header" } });
            var window = UiHelpers.Show(tabs);

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "#0" }, visits.Select(v => v.Header));
        }
    }
}
```

#### `tests/Avalonia.TestingDemo.Tests/LogCollectorTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Tests.Common;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class LogCollectorTests
    {
        [AvaloniaFact]
        public void Collects_the_warning_for_a_binding_to_a_missing_property()
        {
            using var logs = new LogCollector();
            var text = new TextBlock { DataContext = new object() };
            text.Bind(TextBlock.TextProperty, new Binding("Missing"));

            UiHelpers.Show(text);

            Assert.Contains(logs.Messages, m => m.StartsWith("Binding:") && m.Contains("Missing"));
        }

        [AvaloniaFact]
        public void Restores_the_previous_sink_on_dispose()
        {
            var before = Logger.Sink;

            using (new LogCollector())
            {
            }

            Assert.Same(before, Logger.Sink);
        }
    }
}
```

- [ ] **Step 2: 运行，确认因为类型不存在而失败**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: 构建失败，错误形如 `CS0103`/`CS0246`：找不到 `TabWalker`、`LogCollector`。

- [ ] **Step 3: 写最小实现**

#### `tests/Avalonia.Tests.Common/TabWalker.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Tests.Common
{
    /// <summary>One tab selected by <see cref="TabWalker.Walk" />: its position, header text, and the exception it raised, if any.</summary>
    public sealed record TabVisit(int Index, string Header, Exception? Error);

    /// <summary>Selects every tab of a window's first TabControl in turn and forces a layout pass after each one.</summary>
    public static class TabWalker
    {
        public static IReadOnlyList<TabVisit> Walk(Window window)
        {
            var tabs = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault()
                ?? throw new InvalidOperationException("The window has no TabControl.");

            var visits = new List<TabVisit>();
            for (int i = 0; i < tabs.ItemCount; i++)
            {
                string header = (tabs.Items[i] as TabItem)?.Header?.ToString() ?? $"#{i}";
                try
                {
                    tabs.SelectedIndex = i;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    visits.Add(new TabVisit(i, header, null));
                }
                catch (Exception ex)
                {
                    // One broken page must not hide the pages after it.
                    visits.Add(new TabVisit(i, header, ex));
                }
            }

            return visits;
        }
    }
}
```

#### `tests/Avalonia.Tests.Common/LogCollector.cs`

```csharp
using Avalonia.Logging;
using System;
using System.Collections.Generic;

namespace Avalonia.Tests.Common
{
    /// <summary>Collects Avalonia log events at Warning level or above until it is disposed, then restores the previous sink.</summary>
    public sealed class LogCollector : ILogSink, IDisposable
    {
        private readonly ILogSink? _previous = Logger.Sink;

        public LogCollector() => Logger.Sink = this;

        public List<string> Messages { get; } = new();

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
            => Messages.Add($"{area}: {messageTemplate}");

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
            => Messages.Add($"{area}: {messageTemplate} [{string.Join(", ", propertyValues)}]");

        public void Dispose() => Logger.Sink = _previous;
    }
}
```

- [ ] **Step 4: 运行，确认全部通过**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 30，失败: 0`（原有 24 个 + `TabWalkerTests` 4 个 + `LogCollectorTests` 2 个）。

若 `Collects_the_warning_for_a_binding_to_a_missing_property` 失败，先看 `logs.Messages` 是否为空：为空说明日志被 `IsEnabled` 过滤或 `Logger.Sink` 没被替换，不要改断言去迁就。

- [ ] **Step 5: Commit**

```bash
git add tests/Avalonia.Tests.Common tests/Avalonia.TestingDemo.Tests
git commit -m "test: add TabWalker and LogCollector to Avalonia.Tests.Common"
```

---

### Task 3: Appium 探针 P1–P4（仓库外）

**Files:**
- Modify: `Directory.Packages.props`（登记 `Appium.WebDriver`）
- 仓库外，不提交：`C:\Temp\probe-appium\probe-appium.csproj`、`Probes.cs`

**Interfaces:**
- Consumes: 无（不依赖 Task 1–2；可与它们并行，但需要使用者配合启动 WinAppDriver）。
- Produces: 对三个问题的实测答案，供 Task 4 回填 spec，并决定批 7–10 怎么写：
  - **P1/P2**：WinAppDriver 能否驱动 Avalonia 12 窗口，且 `Appium.WebDriver` 4.4.5 能否在 net10 上建会话？
  - **P3（H-A）**：没有 `AutomationId` 的控件，能否用它的 `x:Name` 作为 `AccessibilityId` 找到？
  - **P4（H-B）**：`TabItem` 能否用它的 `Header` 文本找到并点击？

编写计划时已确认 `Appium.WebDriver` 4.4.5 在 net10 上还原、编译均无警告；**运行时**（连 WinAppDriver、找元素、点击）全部没有实测，因为启动 WinAppDriver 需要使用者操作。

- [ ] **Step 1: 登记 `Appium.WebDriver`**

在 `Directory.Packages.props` 里 `Avalonia.Headless.XUnit` 那一行**之后**插入（缩进 8 个空格）：

```xml
        <PackageVersion Include="Appium.WebDriver" Version="4.4.5" />
```

Run: `dotnet build hello-avalonia.slnx`
Expected: 0 错误（登记了但暂未被引用的包不影响构建）。

- [ ] **Step 2: 创建探针文件（若 `C:\Temp\probe-appium` 已存在且内容一致可跳过）**

**`C:\Temp\probe-appium\probe-appium.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Appium.WebDriver" Version="4.4.5" />
        <PackageReference Include="xunit.v3" Version="3.2.2" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
    </ItemGroup>
</Project>
```

**`C:\Temp\probe-appium\Probes.cs`**

```csharp
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.IO;
using Xunit;

namespace ProbeAppium
{
    // Needs WinAppDriver.exe running on 127.0.0.1:4723 and the demo apps built in Debug.
    public class Probes
    {
        private const string Repo = @"E:\ProjectxPlex\WPFCodePlex\hello-avalonia";

        private static WindowsDriver<AppiumWebElement> Open(string project)
        {
            string exe = Path.Combine(Repo, "src", project, "bin", "Debug", project + ".exe");
            Assert.True(File.Exists(exe), "Build first: " + exe);

            var options = new AppiumOptions();
            options.AddAdditionalCapability(MobileCapabilityType.App, exe);
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.Windows);
            options.AddAdditionalCapability(MobileCapabilityType.DeviceName, "WindowsPC");
            return new WindowsDriver<AppiumWebElement>(new Uri("http://127.0.0.1:4723"), options);
        }

        // P1 + P2: a session opens, and an element with an explicit AutomationId is found, clicked and read back.
        [Fact]
        public void P2_AutomationId_click_and_read()
        {
            var session = Open("Avalonia.TestingDemo");
            try
            {
                session.FindElementByAccessibilityId("increment-btn").Click();
                Assert.Equal("1", session.FindElementByAccessibilityId("count-text").Text);
            }
            finally { session.Quit(); }
        }

        // P4 (H-B): a TabItem is found by its Header text and becomes selected after a click.
        [Fact]
        public void P4_TabItem_found_by_header()
        {
            var session = Open("Avalonia.LayoutDemo");
            try
            {
                var tab = session.FindElementByName("定位与间距");
                tab.Click();
                Assert.True(tab.Selected);
            }
            finally { session.Quit(); }
        }

        // P3 (H-A): a control with only Name (no AutomationId) is found by that Name used as AccessibilityId.
        [Fact]
        public void P3_Name_works_as_AccessibilityId()
        {
            var session = Open("Avalonia.LayoutDemo");
            try
            {
                session.FindElementByName("定位与间距").Click();
                Assert.NotNull(session.FindElementByAccessibilityId("MarginSlider"));
            }
            finally { session.Quit(); }
        }
    }
}
```

`MarginSlider` 是 `src/Avalonia.LayoutDemo/Views/Pages/PositioningPage.axaml` 里 `<Slider Name="MarginSlider" …>`，该文件没有任何 `AutomationId`——这正是 P3 要的"只有 Name"的控件。

- [ ] **Step 3: 构建两个被测应用（Debug）**

Run:

```bash
dotnet build src/Avalonia.TestingDemo
dotnet build src/Avalonia.LayoutDemo
```

Expected: 0 错误。确认 `src\Avalonia.TestingDemo\bin\Debug\Avalonia.TestingDemo.exe` 与 `src\Avalonia.LayoutDemo\bin\Debug\Avalonia.LayoutDemo.exe` 存在。

- [ ] **Step 4: 请使用者启动 WinAppDriver，并确认可达**

**这一步必须由使用者做**（执行者启动它可能被权限拒绝）。请使用者在**另一个** PowerShell 窗口运行并保持打开：

```powershell
& "C:\Program Files\Windows Application Driver\WinAppDriver.exe"
```

然后执行者确认：

Run: `curl.exe -s -m 10 http://127.0.0.1:4723/status`
Expected: 返回一段 JSON，含 `build` 字段。连接被拒绝或超时就**不要往下跑探针**，回到本步让使用者检查（WinAppDriver 是否在运行、开发者模式是否开启）。

- [ ] **Step 5: 提醒使用者，然后跑探针**

先告知使用者：接下来约 1 分钟，探针会启动应用并**接管真实的鼠标和键盘**，请不要操作电脑、不要锁屏。

按 P2 → P4 → P3 的顺序**逐个**运行（每次只验证一个问题）：

Run: `dotnet test C:\Temp\probe-appium --filter "FullyQualifiedName~P2" --logger "console;verbosity=detailed"`
Run: `dotnet test C:\Temp\probe-appium --filter "FullyQualifiedName~P4" --logger "console;verbosity=detailed"`
Run: `dotnet test C:\Temp\probe-appium --filter "FullyQualifiedName~P3" --logger "console;verbosity=detailed"`

P3 依赖 P4 的点击，所以 **P4 失败时 P3 记为"未测"**，不要硬解读。

- [ ] **Step 6: 按下表判读（只看结果，不改探针去迁就）**

| 结果 | 含义 | 动作 |
|---|---|---|
| P2 通过 | WinAppDriver 能驱动 Avalonia 12 窗口，客户端可用，显式 `AutomationId` 能被读到 | 继续 P4、P3 |
| P2 建会话就失败（连接被拒、超时、`SessionNotCreated`） | 服务没起、被拦，或 WinAppDriver 起不来这个应用 | 回到 Step 4 检查服务；仍失败则**停止** |
| P2 会话建成，但找不到 `increment-btn` | 显式 `AutomationId` 也没映射到 UIA，整个定位思路有问题 | **停止**：不要跑 P3/P4，向使用者汇报，按 spec「备选方案」改用 Appium 2 或 FlaUI，重新设计批 7–10 |
| P4 通过 | H-B 成立：可按 `Header` 文本找标签页 | 批 8 按标题定位 |
| P4 失败 | H-B 不成立 | 批 8 改按序号定位；P3 记为未测 |
| P3 通过 | H-A 成立：`x:Name` 可直接当 `AccessibilityId`，`src/` 无需改动 | 批 7–10 不动 `src/` |
| P3 失败 | H-A 不成立 | 批 7–10 的计划要在**用到的控件**上补 `AutomationProperties.AutomationId` |

把每个探针的结果（通过/失败 + 失败时的异常信息）记下来，Task 4 要回填。

- [ ] **Step 7: 收尾**

请使用者关闭 WinAppDriver 的窗口。再检查没有遗留进程：

Run: `Get-Process Avalonia.TestingDemo,Avalonia.LayoutDemo -ErrorAction SilentlyContinue`
Expected: 无输出。有遗留则告知使用者，由其确认后结束；探针每个用例结尾都调用了 `session.Quit()`，正常不会遗留。

- [ ] **Step 8: Commit（只含包登记）**

```bash
git add Directory.Packages.props
git status --short
git commit -m "chore: register Appium.WebDriver for the upcoming Appium tests"
```

`git status --short` 里不应出现任何 `C:\Temp` 下的文件（它们本来就在仓库外）。

---

### Task 4: 回填 spec、更新 CLAUDE.md、整体验证

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

**Interfaces:**
- Consumes: Task 1–2 交付的 Common 与 30 个测试；Task 3 记下的 P2/P3/P4 结果。
- Produces: 更新后的 spec（批 1 起的计划以它为准）和 `CLAUDE.md`。

- [ ] **Step 1: `CLAUDE.md` 补一条 Common 的用法**

用 Edit 在 `CLAUDE.md` 里这一行**之后**插入一行：

旧（锚点，不改）：

```markdown
- `Avalonia.Shared` 是共用库（`DemoHeader`、`SharedStyles.axaml`、`ViewModelBase`、`EventLog` 等），通用代码放这里，不要在各项目复制。
```

新（插入在它后面）：

```markdown
- `tests/Avalonia.Tests.Common` 是测试共用库（`HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`）：各应用的测试项目引用它，不要在测试项目里复制辅助代码；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。
```

- [ ] **Step 2: 让 spec 里的 Common 设计与实际一致**

spec 第 90 行的 `TabWalker` 描述与最终实现不同（实际返回列表，由一个 `[AvaloniaFact]` 断言页头与无异常，不做 `[Theory]` 数据源，因为 `Theory` 数据在测试发现阶段求值，那时还没有 Avalonia 应用）。用 Edit 替换：

旧：

```markdown
| `TabWalker` | 对任意 `Window` 找到第一个 `TabControl`，逐个选中每个 `TabItem` 并强制布局，返回每页的 `Header` 与异常；供冒烟测试做 `[Theory]` 数据源 |
```

新（同一位置，多出一行 `LogCollector`）：

```markdown
| `TabWalker` | 对任意 `Window` 找到第一个 `TabControl`，逐个选中每个 `TabItem` 并强制布局，返回每页的 `Header` 与异常（单页抛错不中断后面的页）；冒烟测试在一个 `[AvaloniaFact]` 里断言页头列表与无异常 |
| `LogCollector` | 收集 Warning 及以上的 Avalonia 日志，`Dispose` 时还原 `Logger.Sink`；独立提供，是否接入冒烟测试由批 1 逐页确认后定 |
```

批 6 的描述去掉已完成的 TestingDemo H1。用 Edit 替换：

旧：

```markdown
| 6 | 06-headless-exceptions | MusicStore、WebViewDemo、HtmlRendererDemo；TestingDemo 补 H1 | 3 + 1 |
```

新：

```markdown
| 6 | 06-headless-exceptions | MusicStore、WebViewDemo、HtmlRendererDemo（TestingDemo 的 H1 已在批 0 随 `TabWalkerTests` 完成） | 3 |
```

- [ ] **Step 3: 回填「批 0 实测结论」**

把 spec 里这一行：

```markdown
## 批 0 实测结论（执行后回填）
```

替换为下面的整段。表中 P1 运行、P2、P3、P4 四行的「结论」只允许填三种写法之一：`通过`、`失败：<异常信息的一句话>`、`未测：<原因>`；「说明」写 Task 3 Step 6 判读表里对应的"含义"。其余行已是编写计划时的实测事实，原样保留。

```markdown
## 批 0 实测结论（计划 00 执行后）

| 探针 | 结论 | 说明 |
|---|---|---|
| P1 编译 | 通过 | `Appium.WebDriver` 4.4.5 在 net10 的 xunit v3 项目里还原、编译均无警告 |
| P1 运行 | （按 Task 3 结果填） | 能否连上 WinAppDriver 并创建会话；与 P2 的会话创建是同一次运行 |
| P2 | （按 Task 3 结果填） | 显式 `AutomationId`（`increment-btn`、`count-text`）能否被 WinAppDriver 找到并点击读回 |
| P3（H-A） | （按 Task 3 结果填） | 只有 `Name` 的 `MarginSlider` 能否用 `AccessibilityId` 找到 |
| P4（H-B） | （按 Task 3 结果填） | `TabItem` 能否按 `Header` 文本找到并点击后变为选中 |
| P5 | 通过 | `TabWalker` 对 `LayoutDemo`（用其自己的 `App`）依次得到 `布局面板`、`定位与间距`、`响应式布局`，均无异常 |
| P6 | 部分通过 | `LogCollector` 能收集运行时绑定到不存在属性的警告；编译绑定下"绑定到结构体属性"的静默失败**未验证**，所以没有接入 `TabWalker` |

决定与后续影响（按 Task 3 Step 6 的判读表写 2–4 条，每条一句话）：

- H-A 是否成立 → 批 7–10 要不要在 `src/` 补 `AutomationId`。
- H-B 是否成立 → 批 8 按标题还是按序号定位标签页。
- 若 P2 失败 → 写明已停止，批 7–10 暂缓，待使用者在 Appium 2 与 FlaUI 之间选择。
```

- [ ] **Step 4: 整体验证**

Run: `dotnet build hello-avalonia.slnx`
Expected: 0 错误；警告数不高于基线（本批开始前是 6 个）。

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 30，失败: 0`。

Run: `git diff --stat 0649089..HEAD -- src`
Expected: 无输出（`0649089` 是 spec 的提交；本批不改 `src/` 下任何文件）。

Run: `git status --short`
Expected: 只剩 `CLAUDE.md` 与 spec 两个未提交文件。

- [ ] **Step 5: Commit（不要推送）**

```bash
git add CLAUDE.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git commit -m "docs: record batch 0 probe results and document Avalonia.Tests.Common"
```

提交后向使用者汇报：Common 与 30 个测试已就绪；P2/P3/P4 的结果；批 1–10 能否按原设计走。**批 1 的计划要等使用者看过这份汇报后再写**，因为 H-A/H-B 的结果会改变批 7–10 的写法。

---
