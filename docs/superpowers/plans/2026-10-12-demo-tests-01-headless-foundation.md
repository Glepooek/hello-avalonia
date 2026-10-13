# 演示测试 01：Fundamentals、Xaml、Layout、StarterTutorial 的 Headless 测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `FundamentalsDemo`、`XamlDemo`、`LayoutDemo`、`StarterTutorialDemo` 各建一个 Headless 测试项目，覆盖 spec 的 H1 冒烟、H2 ViewModel、H3 交互三层，并把 `LogCollector` 接入 H1 冒烟。

**Architecture:** 每个应用一个 `Avalonia.<X>Demo.Tests` 项目（xUnit v3 + `Avalonia.Headless.XUnit`），引用对应演示项目与 `tests/Avalonia.Tests.Common`；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。H1 用 `TabWalker` 走完 `MainWindow` 的全部标签页；H2 用普通 `[Fact]`；H3 把单个页面放进窗口，用 `UiHelpers.Click/Type` 或直接改控件属性，再断言界面变化。**本批不改 `src/` 下任何文件。**

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、xUnit v3（3.2.2）、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 1）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文。
- 版本只写在根目录 `Directory.Packages.props`；csproj 的 `PackageReference` **不带 `Version`**。本批用到的包都已登记，不需要改 `Directory.Packages.props`。
- 代码风格沿用 `tests/Avalonia.TestingDemo.Tests`：块作用域命名空间（`namespace X { ... }`）、测试方法名用英文下划线句式。
- 每个测试项目的 `TestAppBuilder` 只写 `HeadlessApp.Build<App>()`，不复制 `Avalonia.Tests.Common` 里的任何辅助代码。
- 目录约定：测试项目放 `tests/`，并登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`。
- 本批**不改 `src/` 下任何文件**（含不补 `AutomationId`）；Task 里的"破坏检查"会临时改 `src/` 的一行，必须用 `git checkout --` 还原，提交前 `git status` 确认 `src/` 干净。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`。
- 探针放在仓库外（`C:\Temp\probe-*`），**永不提交**。
- `TestingDemo.Tests` 的 30 个测试（24 个原有 + 批 0 新增 6 个）始终保持通过。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. **事件经 dispatcher 延后派发**：`TextChanged`、`SizeChanged` 在 Headless 里不是同步触发。测试若不 `Dispatcher.UIThread.RunJobs()` 就断言，会读到旧值，甚至**假通过**（窗口初始宽度恰好满足断言）。所以相关断言必须先 `RunJobs`，且断言 `CurrentWidth` 这类能证明事件真的派发过的值（Task 3、Task 4）。
2. **`Button.Content` 不一定是字符串**：`DemoHeader` 里有个按钮的 Content 是 `PathIcon`，把 `Content` 强转 `string` 会抛 `InvalidCastException`。查找按钮一律写 `"文字".Equals(b.Content)`（Task 1、2、4）。
3. **结构体属性绑定静默失败**：`PositioningPage` 的 `Margin`/`Padding` 经转换器绑到 `Thickness`，失败时既无异常也无日志，只能在运行时断言控件上的真实值（Task 3）。
4. **本地值压制样式**：`ResponsivePage` 的 `UniformGrid` 故意不写 `Columns`；若有人补上，容器查询会永久失效且无日志。Task 3 的容器查询测试在 300/600/900 三个宽度各断言一次列数。
5. **破坏检查**：每个项目挑一个关键测试，临时改 `src/` 里一行让它失败，证明测试不是空转（Task 1–4）。

---

## 本份已核实的事实（编写计划时在仓库外的临时项目里实测）

探针项目在 `C:\Temp\probe-b1-fundamentals`、`probe-b1-xaml`、`probe-b1-layout`、`probe-b1-starter`，代码与本计划里的文件一致（探针里多一个独立的 `LogProbe`，计划里并入了 `SmokeTests`）。

| 事项 | 实测结果 | 对计划的影响 |
|---|---|---|
| 四个应用的 `App`、`MainWindow` 都是 `public`，`MainWindow` 有无参构造 | 探针可直接 `new MainWindow()`、`HeadlessApp.Build<App>()` | 测试项目不需要 `InternalsVisibleTo` |
| 四个 `MainWindow` 的标签页标题 | Fundamentals：`纯代码 UI`、`Code-behind`、`MVVM`、`TopLevel`、`视觉树与逻辑树`、`生命周期与资源`；Xaml：`命名空间`、`x: 指令`、`标记扩展`、`类型转换器`、`泛型`、`XAML 编译`、`平台相关 XAML`；Layout：`布局面板`、`定位与间距`、`响应式布局`；Starter：`1 添加控件` … `6 练习` | H1 断言的页头列表 |
| `TabWalker` 走完四个窗口，且全程挂着 `LogCollector` | **0 条 Warning 及以上日志，0 个页面异常** | spec 留给批 1 的问题"是否把 `LogCollector` 接进冒烟测试"：**可以接**，四个应用都没有误报。注意这只覆盖运行时能收集到的日志，编译绑定下结构体属性的静默失败仍收集不到，所以 Task 3 另有运行时断言 |
| 把 `Button.Content` 强转 `string` | 在 `MvvmPage` 里抛 `InvalidCastException`（`PathIcon`） | Review Focus 第 2 条 |
| `ExercisesPage` 里直接 `celsius.Text = "100"` 后立刻读 `Fahrenheit` | 读到 `32.0`（初始值 0 的换算结果），不是 `212.0` | `TextChanged` 经 dispatcher 延后派发，要 `RunJobs`（Review Focus 第 1 条） |
| `ResponsivePage` 把窗口宽度改成 800 再改成 400 | 不 `RunJobs` 时 800 的断言假通过、400 的断言失败 | 同上，并要求断言 `CurrentWidth` |
| `ContainerQuery` 在 Headless 里随窗口宽度生效 | 300 → 1 列，600 → 2 列，900 → 4 列，都对 | Task 3 的容器查询测试可用 |
| `PositioningPage` 滑块 → `Margin`/`Padding` 的转换器绑定 | 滑块设为 20/30 后，`Border.Margin == Thickness(20)`、`Border.Padding == Thickness(30)` | Task 3 的运行时断言成立 |
| 单页测试：`UiHelpers.Show(new XxxPage())` 之后 `Click` 页面上的按钮 | 本计划用到的所有按钮都在 600×480 窗口的可见区内，点击生效 | 不需要 `ScrollIntoView` |
| 四个探针项目的测试数 | Fundamentals 10、Xaml 9、Layout 10、Starter 8，共 37，全过 | 计划交付后的期望数字 |

## 文件结构

**新增**（每个应用一个项目，`<X>` 为 `Fundamentals`、`Xaml`、`Layout`、`StarterTutorial`）

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.<X>Demo.Tests/Avalonia.<X>Demo.Tests.csproj` | 测试项目工程（xUnit v3 Exe，引用演示项目与 Common） |
| `tests/Avalonia.<X>Demo.Tests/TestAppBuilder.cs` | 一行：`HeadlessApp.Build<App>()` |
| `tests/Avalonia.<X>Demo.Tests/SmokeTests.cs` | H1：走完全部标签页、断言页头列表与无异常、断言无 Warning 日志 |
| `tests/Avalonia.<X>Demo.Tests/InteractionTests.cs` | H3：关键页面的"操作 → 断言界面变化" |
| `tests/Avalonia.FundamentalsDemo.Tests/ViewModelTests.cs` | H2：`MvvmViewModel`、`TreesViewModel` |
| `tests/Avalonia.LayoutDemo.Tests/ViewModelTests.cs` | H2：`ResponsiveViewModel` |
| `tests/Avalonia.XamlDemo.Tests/MarkupExtensionTests.cs` | H2 的替代：`RepeatTextExtension`（XamlDemo 没有 `ViewModels/`，这是它唯一的非界面逻辑） |

`StarterTutorialDemo` 照官方教程用 code-behind、没有 `ViewModels/`，所以**没有** H2 文件（项目约定：别补 ViewModel）。

**修改**

| 文件 | 改动 |
|---|---|
| `hello-avalonia.slnx` | `/tests/` 文件夹加入四个新项目（各在自己的任务里加一行） |
| `CLAUDE.md` | 命令一节里"唯一的测试项目"的说法已不成立，改成按项目列出的写法（Task 5） |
| `README.md` | 项目清单里补一行说明各应用测试项目的命名规则（Task 5） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 1 实测结论」（Task 5） |

**不碰**：`src/` 下任何文件、`Directory.Packages.props`、`tests/Avalonia.Tests.Common`、`tests/Avalonia.TestingDemo.Tests`。

---

### Task 1: `Avalonia.FundamentalsDemo.Tests`

**Files:**
- Create: `tests/Avalonia.FundamentalsDemo.Tests/Avalonia.FundamentalsDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`ViewModelTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（批 0 已交付）：`Avalonia.Tests.Common.HeadlessApp.Build<TApp>()`、`UiHelpers.Show/ByName/Click/Type`、`TabWalker.Walk(Window) : IReadOnlyList<TabVisit>`（`TabVisit(int Index, string Header, Exception? Error)`）、`LogCollector`（`Messages : List<string>`）。
- Consumes（被测代码，只读）：`Avalonia.FundamentalsDemo.App`、`Views.MainWindow`、`Views.Pages.{CodeBehindPage, MvvmPage, TreesPage}`、`ViewModels.MvvmViewModel`（`Input`、`Greeting`、`GreetCommand`）、`ViewModels.TreesViewModel`（`VisualTree`、`LogicalTree`、`Refresh(Control)`）、`Models.TreeNodeInfo.Label`。
- Produces：测试项目 `Avalonia.FundamentalsDemo.Tests`（10 个测试）。Task 2–4 照这个项目的结构复制。

本任务没有"先写失败的测试"的空间：被测代码都已存在，测试写出来就应该通过。所以验证"测试不是空转"靠最后的**破坏检查**（Step 7）。

- [ ] **Step 1: 记录基线**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)"`
Expected: 0 错误。记下警告数（批 0 结束时是 6）；Task 5 要求结束时不高于这个数。

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 30，失败: 0`。

- [ ] **Step 2: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.FundamentalsDemo.Tests/Avalonia.FundamentalsDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.FundamentalsDemo\Avalonia.FundamentalsDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.FundamentalsDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.FundamentalsDemo;
using Avalonia.FundamentalsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.FundamentalsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.Tests` 那行**之前**加一行：

```xml
    <Project Path="tests/Avalonia.FundamentalsDemo.Tests/Avalonia.FundamentalsDemo.Tests.csproj" />
```

- [ ] **Step 3: H1 冒烟测试**

#### `tests/Avalonia.FundamentalsDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.FundamentalsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.FundamentalsDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_walks_every_tab_without_errors()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(
                new[] { "纯代码 UI", "Code-behind", "MVVM", "TopLevel", "视觉树与逻辑树", "生命周期与资源" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.FundamentalsDemo.Tests`
Expected: `通过: 2，失败: 0`。若第二个测试失败，失败信息会列出收集到的日志：逐条判断是页面真有绑定错误还是演示故意为之，**不要**为了过测试而改断言；真实问题记到 Task 5 的回填里。

- [ ] **Step 4: H2 ViewModel 测试**

#### `tests/Avalonia.FundamentalsDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.FundamentalsDemo.ViewModels;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.FundamentalsDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Greet_is_disabled_until_the_input_has_text()
        {
            var vm = new MvvmViewModel();

            Assert.False(vm.GreetCommand.CanExecute(null));

            vm.Input = "Al";

            Assert.True(vm.GreetCommand.CanExecute(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Greet_stays_disabled_for_blank_input(string input)
        {
            var vm = new MvvmViewModel { Input = input };

            Assert.False(vm.GreetCommand.CanExecute(null));
        }

        [Fact]
        public void Greet_trims_the_name_into_the_greeting()
        {
            var vm = new MvvmViewModel { Input = "  Al  " };

            vm.GreetCommand.Execute(null);

            Assert.Equal("你好，Al！", vm.Greeting);
        }

        // TreesViewModel walks real controls, so it needs the Avalonia platform.
        [AvaloniaFact]
        public void Trees_refresh_replaces_both_trees_instead_of_appending()
        {
            var vm = new TreesViewModel();
            var button = new Button();

            vm.Refresh(button);
            vm.Refresh(button);

            Assert.Single(vm.VisualTree);
            Assert.Single(vm.LogicalTree);
            Assert.StartsWith("Button", vm.VisualTree[0].Label);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.FundamentalsDemo.Tests --filter "FullyQualifiedName~ViewModelTests"`
Expected: `通过: 5，失败: 0`（`Theory` 的两组数据各算一个）。

- [ ] **Step 5: H3 交互测试**

三个页面各一条：`CodeBehindPage`（按名字查控件）、`MvvmPage`（控件没有 `Name`，按文字查）、`TreesPage`（点击后两个 `TreeView` 都被填充）。

#### `tests/Avalonia.FundamentalsDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.FundamentalsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.FundamentalsDemo.Tests
{
    public class InteractionTests
    {
        [AvaloniaFact]
        public void Code_behind_page_asks_for_a_name_when_empty_and_greets_once_one_is_typed()
        {
            var window = UiHelpers.Show(new CodeBehindPage());
            var result = window.ByName<TextBlock>("ResultText");

            window.Click(window.ByName<Button>("GreetButton"));
            Assert.Equal("请先输入名字", result.Text);

            window.Type(window.ByName<TextBox>("NameBox"), "Al");
            window.Click(window.ByName<Button>("GreetButton"));
            Assert.Equal("你好，Al！", result.Text);
        }

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        [AvaloniaFact]
        public void Mvvm_page_enables_the_button_once_a_name_is_typed_and_greets()
        {
            var window = UiHelpers.Show(new MvvmPage());
            var greet = window.GetVisualDescendants().OfType<Button>().Single(b => "打招呼".Equals(b.Content));
            var box = window.GetVisualDescendants().OfType<TextBox>().Single();

            Assert.False(greet.IsEffectivelyEnabled);

            window.Type(box, "Al");
            Assert.True(greet.IsEffectivelyEnabled);

            window.Click(greet);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "你好，Al！");
        }

        [AvaloniaFact]
        public void Trees_page_snapshot_fills_both_tree_views()
        {
            var window = UiHelpers.Show(new TreesPage());
            var snapshot = window.GetVisualDescendants().OfType<Button>().Single(b => "生成快照".Equals(b.Content));

            window.Click(snapshot);

            Assert.Contains("快照已生成", window.ByName<TextBlock>("HintText").Text);
            var trees = window.GetVisualDescendants().OfType<TreeView>().ToList();
            Assert.Equal(2, trees.Count);
            Assert.All(trees, t => Assert.Single(t.ItemsSource!.Cast<object>()));
        }
    }
}
```

Run: `dotnet test tests/Avalonia.FundamentalsDemo.Tests`
Expected: `通过: 10，失败: 0`（2 + 5 + 3）。

- [ ] **Step 6: 构建确认**

Run: `dotnet build tests/Avalonia.FundamentalsDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 7: 破坏检查（临时改 `src/`，必须还原）**

目的：证明 `Mvvm_page_enables_the_button_once_a_name_is_typed_and_greets` 真的在测"命令可执行状态随输入刷新"。

用 Edit 在 `src/Avalonia.FundamentalsDemo/ViewModels/MvvmViewModel.cs` 里删掉这一行：

```csharp
        [NotifyCanExecuteChangedFor(nameof(GreetCommand))]
```

Run: `dotnet test tests/Avalonia.FundamentalsDemo.Tests --filter "FullyQualifiedName~Mvvm_page_enables"`
Expected: **失败**（输入后按钮仍是禁用的）。

立刻还原：

```bash
git checkout -- src/Avalonia.FundamentalsDemo/ViewModels/MvvmViewModel.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.FundamentalsDemo.Tests`，`通过: 10`。若删掉那一行测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 8: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.FundamentalsDemo.Tests
git status --short
git commit -m "test: add Headless tests for FundamentalsDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.FundamentalsDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 2: `Avalonia.XamlDemo.Tests`

**Files:**
- Create: `tests/Avalonia.XamlDemo.Tests/Avalonia.XamlDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`MarkupExtensionTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（批 0）：同 Task 1 的 `HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`。
- Consumes（被测代码，只读）：`Avalonia.XamlDemo.App`、`Views.MainWindow`、`Views.Pages.{DirectivesPage, CompilationPage}`、`MarkupExtensions.RepeatTextExtension`（`RepeatTextExtension(string)`、`Count`（默认 2）、`ProvideValue(IServiceProvider) : object`）。
- Produces：测试项目 `Avalonia.XamlDemo.Tests`（9 个测试）。

XamlDemo 没有 `ViewModels/`，所以 H2 的位置由 `RepeatTextExtension` 顶替：它是本项目唯一的非界面逻辑，`Count` 被 `Math.Max(1, Count)` 兜底这一点值得钉住。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.XamlDemo.Tests/Avalonia.XamlDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.XamlDemo\Avalonia.XamlDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.XamlDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.Tests.Common;
using Avalonia.XamlDemo;
using Avalonia.XamlDemo.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.XamlDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.Tests` 那行**之前**加一行：

```xml
    <Project Path="tests/Avalonia.XamlDemo.Tests/Avalonia.XamlDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.XamlDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.XamlDemo.Views;
using System.Linq;
using Xunit;

namespace Avalonia.XamlDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_walks_every_tab_without_errors()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(
                new[] { "命名空间", "x: 指令", "标记扩展", "类型转换器", "泛型", "XAML 编译", "平台相关 XAML" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.XamlDemo.Tests`
Expected: `通过: 2，失败: 0`。第二个测试失败时的处理同 Task 1 Step 3：逐条判断日志，不要改断言迁就。

- [ ] **Step 3: `RepeatTextExtension` 单测**

#### `tests/Avalonia.XamlDemo.Tests/MarkupExtensionTests.cs`

```csharp
using Avalonia.XamlDemo.MarkupExtensions;
using Xunit;

namespace Avalonia.XamlDemo.Tests
{
    // No [AvaloniaFact]: the extension is a plain class, ProvideValue never touches the service provider.
    public class MarkupExtensionTests
    {
        [Fact]
        public void RepeatText_repeats_the_text_count_times()
        {
            var extension = new RepeatTextExtension("ab") { Count = 3 };

            Assert.Equal("ababab", extension.ProvideValue(null!));
        }

        [Fact]
        public void RepeatText_defaults_to_two_repeats()
        {
            Assert.Equal("abab", new RepeatTextExtension("ab").ProvideValue(null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void RepeatText_never_drops_below_one_repeat(int count)
        {
            var extension = new RepeatTextExtension("ab") { Count = count };

            Assert.Equal("ab", extension.ProvideValue(null!));
        }
    }
}
```

Run: `dotnet test tests/Avalonia.XamlDemo.Tests --filter "FullyQualifiedName~MarkupExtensionTests"`
Expected: `通过: 4，失败: 0`。

- [ ] **Step 4: H3 交互测试**

`DirectivesPage` 同时验证 code-behind 路径（`x:Name` 生成的字段）与 `{Binding #SourceBox.Text}` 元素名绑定；`CompilationPage` 验证 code-behind 构造时写入的文字与元素名绑定。

#### `tests/Avalonia.XamlDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using Avalonia.XamlDemo.Views.Pages;
using System.Linq;
using Xunit;

namespace Avalonia.XamlDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        [AvaloniaFact]
        public void Directives_page_reads_the_text_box_from_code_behind_and_through_element_binding()
        {
            var window = UiHelpers.Show(new DirectivesPage());
            var box = window.ByName<TextBox>("SourceBox");

            box.Text = "hello";
            window.Click(window.GetVisualDescendants().OfType<Button>().Single(b => "从 code-behind 读取".Equals(b.Content)));

            Assert.Equal("code-behind 读到：hello", window.ByName<TextBlock>("ReadResult").Text);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "元素名绑定读到：hello");
        }

        [AvaloniaFact]
        public void Compilation_page_reports_the_demo_assembly()
        {
            var window = UiHelpers.Show(new CompilationPage());

            Assert.Contains("Avalonia.XamlDemo", window.ByName<TextBlock>("CompiledFlagText").Text);
        }

        [AvaloniaFact]
        public void Compilation_page_element_binding_follows_the_probe_text_box()
        {
            var window = UiHelpers.Show(new CompilationPage());

            window.ByName<TextBox>("Probe").Text = "changed";

            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "读到：changed");
        }
    }
}
```

Run: `dotnet test tests/Avalonia.XamlDemo.Tests`
Expected: `通过: 9，失败: 0`（2 + 4 + 3）。

- [ ] **Step 5: 构建确认**

Run: `dotnet build tests/Avalonia.XamlDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 6: 破坏检查（临时改 `src/`，必须还原）**

用 Edit 在 `src/Avalonia.XamlDemo/MarkupExtensions/RepeatTextExtension.cs` 里把 `Math.Max(1, Count)` 改成 `Count`。

Run: `dotnet test tests/Avalonia.XamlDemo.Tests --filter "FullyQualifiedName~never_drops"`
Expected: **失败 2 个**（`count: 0`、`count: -5`）。

立刻还原：

```bash
git checkout -- src/Avalonia.XamlDemo/MarkupExtensions/RepeatTextExtension.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑 `dotnet test tests/Avalonia.XamlDemo.Tests`，`通过: 9`。

- [ ] **Step 7: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.XamlDemo.Tests
git status --short
git commit -m "test: add Headless tests for XamlDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.XamlDemo.Tests/` 下的文件。提交信息末尾附 Co-Authored-By 行。

---

### Task 3: `Avalonia.LayoutDemo.Tests`

**Files:**
- Create: `tests/Avalonia.LayoutDemo.Tests/Avalonia.LayoutDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`ViewModelTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（批 0）：同 Task 1 的 `HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`。
- Consumes（被测代码，只读）：`Avalonia.LayoutDemo.App`、`Views.MainWindow`、`Views.Pages.{PositioningPage, ResponsivePage}`（后者的 `DataContext` 是 `ViewModels.ResponsiveViewModel`）、`ResponsiveViewModel`（`IsCompact`、`IsWide`、`CurrentWidth`、`UpdateLayout(double)`，阈值 500）；页面里的控件名 `MarginSlider`、`PaddingSlider`、`TileGrid`。
- Produces：测试项目 `Avalonia.LayoutDemo.Tests`（10 个测试）。

本任务含 Review Focus 第 1、3、4 条的测试：事件延后派发（`RunJobs`）、结构体属性绑定（`Thickness`）、容器查询不被本地值压制（`Columns`）。三条都是 CLAUDE.md「Avalonia 坑」里反复踩过的，所以放在运行时断言里。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.LayoutDemo.Tests/Avalonia.LayoutDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.LayoutDemo\Avalonia.LayoutDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.LayoutDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.LayoutDemo;
using Avalonia.LayoutDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.LayoutDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.Tests` 那行**之前**加一行：

```xml
    <Project Path="tests/Avalonia.LayoutDemo.Tests/Avalonia.LayoutDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.LayoutDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.LayoutDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.LayoutDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_walks_every_tab_without_errors()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "布局面板", "定位与间距", "响应式布局" }, visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.LayoutDemo.Tests`
Expected: `通过: 2，失败: 0`。第二个测试失败时的处理同 Task 1 Step 2。

- [ ] **Step 3: H2 ViewModel 测试**

#### `tests/Avalonia.LayoutDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.LayoutDemo.ViewModels;
using Xunit;

namespace Avalonia.LayoutDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Responsive_starts_compact()
        {
            var vm = new ResponsiveViewModel();

            Assert.True(vm.IsCompact);
            Assert.False(vm.IsWide);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(499.9, true)]
        [InlineData(500, false)]
        [InlineData(1200, false)]
        public void Responsive_switches_at_the_500_pixel_breakpoint(double width, bool expectedCompact)
        {
            var vm = new ResponsiveViewModel();

            vm.UpdateLayout(width);

            Assert.Equal(width, vm.CurrentWidth);
            Assert.Equal(expectedCompact, vm.IsCompact);
            Assert.Equal(!expectedCompact, vm.IsWide);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.LayoutDemo.Tests --filter "FullyQualifiedName~ViewModelTests"`
Expected: `通过: 5，失败: 0`（1 + 4 组数据）。

- [ ] **Step 4: H3 交互测试**

#### `tests/Avalonia.LayoutDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutDemo.ViewModels;
using Avalonia.LayoutDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.LayoutDemo.Tests
{
    public class InteractionTests
    {
        // Both bindings go through a converter into a Thickness. A binding to a struct property fails
        // silently, so this has to be asserted on the live control rather than trusted from the build.
        [AvaloniaFact]
        public void Positioning_sliders_drive_the_margin_and_padding_of_the_sample_borders()
        {
            var window = UiHelpers.Show(new PositioningPage());
            var marginBorder = (Border)window.GetVisualDescendants().OfType<TextBlock>()
                .First(t => t.Text == "我有 Margin").Parent!;
            var paddingBorder = (Border)window.GetVisualDescendants().OfType<TextBlock>()
                .First(t => t.Text == "我有 Padding").Parent!;

            window.ByName<Slider>("MarginSlider").Value = 20;
            window.ByName<Slider>("PaddingSlider").Value = 30;

            Assert.Equal(new Thickness(20), marginBorder.Margin);
            Assert.Equal(new Thickness(30), paddingBorder.Padding);
        }

        [AvaloniaFact]
        public void Responsive_page_breakpoint_follows_the_window_width()
        {
            var page = new ResponsivePage();
            var window = UiHelpers.Show(page);
            var vm = (ResponsiveViewModel)page.DataContext!;

            // SizeChanged reaches the view model through the dispatcher, so drain it after every resize.
            // Asserting CurrentWidth too keeps the first step from passing on the initial 600 px layout.
            window.Width = 800;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(800, vm.CurrentWidth);
            Assert.True(vm.IsWide);

            window.Width = 400;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(400, vm.CurrentWidth);
            Assert.True(vm.IsCompact);
        }

        // Columns is deliberately not set on the element: a local value would suppress the container queries.
        [AvaloniaFact]
        public void Responsive_page_container_query_changes_the_tile_columns_with_the_width()
        {
            var window = UiHelpers.Show(new ResponsivePage());
            var grid = window.ByName<UniformGrid>("TileGrid");

            window.Width = 300;
            window.UpdateLayout();
            Assert.Equal(1, grid.Columns);

            window.Width = 600;
            window.UpdateLayout();
            Assert.Equal(2, grid.Columns);

            window.Width = 900;
            window.UpdateLayout();
            Assert.Equal(4, grid.Columns);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.LayoutDemo.Tests`
Expected: `通过: 10，失败: 0`（2 + 5 + 3）。

- [ ] **Step 5: 构建确认**

Run: `dotnet build tests/Avalonia.LayoutDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 6: 破坏检查（临时改 `src/`，必须还原）**

用 Edit 在 `src/Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.cs` 里把 `500d` 改成 `600d`。

Run: `dotnet test tests/Avalonia.LayoutDemo.Tests --filter "FullyQualifiedName~ViewModelTests"`
Expected: **失败 1 个**（`width: 500, expectedCompact: False`）。

立刻还原：

```bash
git checkout -- src/Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑 `dotnet test tests/Avalonia.LayoutDemo.Tests`，`通过: 10`。

- [ ] **Step 7: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.LayoutDemo.Tests
git status --short
git commit -m "test: add Headless tests for LayoutDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.LayoutDemo.Tests/` 下的文件。提交信息末尾附 Co-Authored-By 行。

---

### Task 4: `Avalonia.StarterTutorialDemo.Tests`

**Files:**
- Create: `tests/Avalonia.StarterTutorialDemo.Tests/Avalonia.StarterTutorialDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（批 0）：同 Task 1 的 `HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`。
- Consumes（被测代码，只读）：`Avalonia.StarterTutorialDemo.App`、`Views.MainWindow`、`Views.Pages.{EventsPage, ConvertingDataPage, ExercisesPage}`；页面里的控件名 `ClickInfo`、`Celsius`、`Fahrenheit`；`EventsPage` 与 `ConvertingDataPage` 里文字为 `Calculate` 的按钮。
- Produces：测试项目 `Avalonia.StarterTutorialDemo.Tests`（8 个测试）。

照官方教程用 code-behind，没有 `ViewModels/`，所以**没有 H2 文件，也不要为了测试去补 ViewModel**（CLAUDE.md 的明确约定）。`CustomizingWindowPage` 会弹出窗口，属于 spec 里"不进入 Headless"的窗口管理类，这里只靠 H1 保证它能构造。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.StarterTutorialDemo.Tests/Avalonia.StarterTutorialDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.StarterTutorialDemo\Avalonia.StarterTutorialDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.StarterTutorialDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.StarterTutorialDemo;
using Avalonia.StarterTutorialDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.StarterTutorialDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.TestingDemo.Tests` 那行**之前**加一行：

```xml
    <Project Path="tests/Avalonia.StarterTutorialDemo.Tests/Avalonia.StarterTutorialDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.StarterTutorialDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.StarterTutorialDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.StarterTutorialDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_walks_every_tab_without_errors()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(
                new[] { "1 添加控件", "2 添加布局", "3 自定义窗口", "4 事件与响应", "5 数据转换", "6 练习" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.StarterTutorialDemo.Tests`
Expected: `通过: 2，失败: 0`。第二个测试失败时的处理同 Task 1 Step 2。

- [ ] **Step 3: H3 交互测试**

`ExercisesPage` 的换算由 `TextChanged` 触发，经 dispatcher 延后派发：必须在每次改文字后 `RunJobs`，否则读到的是初始值 0 的换算结果 `32.0`（探针里实测过）。`ConvertingDataPage` 由按钮触发，同步完成，不需要 `RunJobs`。

#### `tests/Avalonia.StarterTutorialDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.StarterTutorialDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.StarterTutorialDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        [AvaloniaFact]
        public void Events_page_counts_clicks()
        {
            var window = UiHelpers.Show(new EventsPage());
            var button = window.GetVisualDescendants().OfType<Button>().Single(b => "Calculate".Equals(b.Content));

            window.Click(button);
            window.Click(button);

            Assert.Equal("已点击 2 次", window.ByName<TextBlock>("ClickInfo").Text);
        }

        [AvaloniaTheory]
        [InlineData("100", "212.0")]
        [InlineData("-40", "-40.0")]
        [InlineData("37", "98.6")]
        public void Converting_page_calculate_button_converts_celsius_to_fahrenheit(string celsius, string expected)
        {
            var window = UiHelpers.Show(new ConvertingDataPage());
            window.ByName<TextBox>("Celsius").Text = celsius;

            window.Click(window.GetVisualDescendants().OfType<Button>().Single(b => "Calculate".Equals(b.Content)));

            Assert.Equal(expected, window.ByName<TextBox>("Fahrenheit").Text);
        }

        [AvaloniaFact]
        public void Converting_page_resets_both_boxes_on_input_that_is_not_a_number()
        {
            var window = UiHelpers.Show(new ConvertingDataPage());
            window.ByName<TextBox>("Celsius").Text = "abc";

            window.Click(window.GetVisualDescendants().OfType<Button>().Single(b => "Calculate".Equals(b.Content)));

            Assert.Equal("0", window.ByName<TextBox>("Celsius").Text);
            Assert.Equal("0", window.ByName<TextBox>("Fahrenheit").Text);
        }

        [AvaloniaFact]
        public void Exercises_page_converts_live_as_the_celsius_text_changes()
        {
            var window = UiHelpers.Show(new ExercisesPage());
            var celsius = window.ByName<TextBox>("Celsius");
            var fahrenheit = window.ByName<TextBox>("Fahrenheit");

            // TextChanged is raised through the dispatcher, so drain it after every edit.
            celsius.Text = "100";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("212.0", fahrenheit.Text);

            celsius.Text = "-";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", fahrenheit.Text);

            celsius.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", fahrenheit.Text);
        }
    }
}
```

Run: `dotnet test tests/Avalonia.StarterTutorialDemo.Tests`
Expected: `通过: 8，失败: 0`（2 + 1 + 3 + 1 + 1）。

- [ ] **Step 4: 构建确认**

Run: `dotnet build tests/Avalonia.StarterTutorialDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

用 Edit 在 `src/Avalonia.StarterTutorialDemo/Views/Pages/ConvertingDataPage.axaml.cs` 里把 `(9d / 5d)` 改成 `(9d / 4d)`。

Run: `dotnet test tests/Avalonia.StarterTutorialDemo.Tests --filter "FullyQualifiedName~Converting_page_calculate"`
Expected: **失败 3 个**（`100`、`-40`、`37` 三组数据都算错）。

立刻还原：

```bash
git checkout -- src/Avalonia.StarterTutorialDemo/Views/Pages/ConvertingDataPage.axaml.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑 `dotnet test tests/Avalonia.StarterTutorialDemo.Tests`，`通过: 8`。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.StarterTutorialDemo.Tests
git status --short
git commit -m "test: add Headless tests for StarterTutorialDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.StarterTutorialDemo.Tests/` 下的文件。提交信息末尾附 Co-Authored-By 行。

---

### Task 5: 文档同步、回填 spec、整体验证

**Files:**
- Modify: `CLAUDE.md`、`README.md`
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

**Interfaces:**
- Consumes: Task 1–4 交付的 4 个测试项目（共 37 个测试）与 Task 1 Step 1 记下的构建警告基线。
- Produces: 与现状一致的 `CLAUDE.md` / `README.md`；回填后的 spec（批 2 起的计划以它为准）。

- [ ] **Step 1: `CLAUDE.md` 命令一节**

"唯一的测试项目"已不成立。用 Edit 替换：

旧：

```markdown
dotnet test tests/Avalonia.TestingDemo.Tests     # 唯一的测试项目（xUnit v3 + Headless）
```

新：

```markdown
dotnet test hello-avalonia.slnx                  # 全部测试项目（xUnit v3 + Headless），每个应用一个 Avalonia.<X>Demo.Tests
dotnet test tests/Avalonia.TestingDemo.Tests     # 只跑单个测试项目，换成任意 Avalonia.*Demo.Tests
```

下一行带 `--filter` 的命令保持不动。

- [ ] **Step 2: `README.md` 项目清单补一行**

用 Edit 在这一行**之后**插入一行：

旧（锚点，不改）：

```markdown
| [Avalonia.TestingDemo.Tests](tests/Avalonia.TestingDemo.Tests) | xUnit v3 + Headless 测试：控件查找、模拟输入、ViewModel 单测、渲染像素断言（`dotnet test`） |
```

新（插入在它后面）：

```markdown
| [Avalonia.\*Demo.Tests](tests) | 各演示应用的 Headless 测试（命名为 `Avalonia.<应用>Demo.Tests`，目前覆盖 Fundamentals、Xaml、Layout、StarterTutorial）：走完全部标签页、ViewModel 单测、关键页面交互；共用辅助在 `tests/Avalonia.Tests.Common` |
```

- [ ] **Step 3: spec 里 `LogCollector` 的描述与批 1 的结论一致**

用 Edit 替换：

旧：

```markdown
| `LogCollector` | 收集 Warning 及以上的 Avalonia 日志，`Dispose` 时还原 `Logger.Sink`；独立提供，是否接入冒烟测试由批 1 逐页确认后定 |
```

新：

```markdown
| `LogCollector` | 收集 Warning 及以上的 Avalonia 日志，`Dispose` 时还原 `Logger.Sink`；批 1 起每个应用的冒烟测试里多一个 `Walking_every_tab_logs_no_warnings`，走完全部标签页后断言没有任何 Warning 日志 |
```

- [ ] **Step 4: 回填「批 1 实测结论」**

把 spec 里这一行：

```markdown
## 批 1 实测结论（执行后回填）
```

替换为下面的整段。「执行时的实测」一栏要按本次执行的真实输出填写：三个数字（构建警告数、全解决方案测试数、构建耗时）填实际值，破坏检查若有一次没按预期失败就如实写。

```markdown
## 批 1 实测结论（计划 01 执行后）

| 项目 | 测试数 | H1 页数 | 说明 |
|---|---|---|---|
| `Avalonia.FundamentalsDemo.Tests` | 10 | 6 | H2：`MvvmViewModel`、`TreesViewModel`；H3：`CodeBehindPage`、`MvvmPage`、`TreesPage` |
| `Avalonia.XamlDemo.Tests` | 9 | 7 | 无 ViewModel，H2 由 `RepeatTextExtension` 顶替；H3：`DirectivesPage`、`CompilationPage` |
| `Avalonia.LayoutDemo.Tests` | 10 | 3 | H2：`ResponsiveViewModel`；H3：`PositioningPage`、`ResponsivePage`（含容器查询） |
| `Avalonia.StarterTutorialDemo.Tests` | 8 | 6 | 无 ViewModel，不补；H3：`EventsPage`、`ConvertingDataPage`、`ExercisesPage`；`CustomizingWindowPage` 弹窗只靠 H1 |

执行时的实测：构建警告数 <填实际值>（批开始前 <基线>）；`dotnet test hello-avalonia.slnx` 共 <填实际值> 个测试通过；全解决方案构建约 <填实际值> 秒（批 0 记录的是约 70 秒）。

**对后续批的决定与发现**（每条都是编写计划时在探针里实测的，不是推测）：

- **`LogCollector` 接入冒烟测试：成立，已接入。** 四个应用走完全部标签页都是 0 条 Warning 日志。局限不变：它只能收到运行时绑定的警告，编译绑定下"绑定到结构体属性"的静默失败收不到，所以这类绑定仍要像 `LayoutDemo` 的 `Thickness` 测试那样在运行时断言控件上的真实值。批 2（DataBinding、Styling）最可能撞上故意演示绑定错误的页面，接入前逐页确认。
- **`TextChanged`、`SizeChanged` 经 dispatcher 延后派发**：测试里改完文字或窗口宽度，要 `Dispatcher.UIThread.RunJobs()` 之后才能断言。不这样做时，相关断言会读到旧值，更糟的是可能假通过（窗口初始宽度恰好满足断言）；断言 `CurrentWidth` 这类能证明事件真的派发过的值可以防假通过。这条对批 2–6 的所有事件相关测试都适用。
- **`Button.Content` 不一定是字符串**：`DemoHeader` 里有个按钮的 `Content` 是 `PathIcon`。按文字查按钮用 `"文字".Equals(b.Content)`，不要强转 `string`。批 2–6 沿用。
- **容器查询在 Headless 里生效**：`ContainerQuery` 随窗口宽度正确切换，可以直接断言。
- **破坏检查有效**：四个项目各挑一个关键测试，临时改 `src/` 一行后均如期失败（`NotifyCanExecuteChangedFor` 删除 → 1 个失败；`Math.Max(1, Count)` → 2 个；`500d`→`600d` → 1 个；`9d/5d`→`9d/4d` → 3 个），并已还原。
```

- [ ] **Step 5: 整体验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)|已用时间|Elapsed"`
Expected: 0 错误；警告数不高于 Task 1 Step 1 记下的基线。把警告数与耗时填进上面的回填表。

Run: `dotnet test hello-avalonia.slnx`
Expected: 全部通过，失败 0；总数 = 30（TestingDemo）+ 37（本批）= 67。注意 `Avalonia.Tests.Common` 不是测试项目，不贡献测试数。把 67（或实际数）填进回填表。

Run: `git diff --stat 8929692..HEAD -- src`
Expected: 无输出（`8929692` 是编写本计划时的 HEAD；本批不改 `src/` 下任何文件）。

Run: `git status --short`
Expected: 只剩 `CLAUDE.md`、`README.md` 与 spec 三个未提交文件（本计划文件应在开始执行前已单独提交；若它还是未跟踪状态，先 `git add` 它并以 `docs: add plan for demo test batch 1` 提交）。探针目录 `C:\Temp\probe-*` 在仓库外，不会出现在这里。

- [ ] **Step 6: Commit（不要推送）**

```bash
git add CLAUDE.md README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git commit -m "docs: record batch 1 results and describe the per-app test projects"
```

提交信息末尾附 Co-Authored-By 行。提交后向使用者汇报：4 个测试项目与 37 个测试已就绪；`LogCollector` 已接入冒烟测试；事件延后派发这条发现对批 2–6 的影响。**批 2 的计划**（Styling、DataBinding、DataTemplates、PropertySystem）等使用者看过汇报后再写。

---
