# 演示测试 02：Styling、DataBinding、DataTemplates、PropertySystem 的 Headless 测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `PropertySystemDemo`、`DataTemplatesDemo`、`StylingDemo`、`DataBindingDemo` 各建一个 Headless 测试项目，覆盖 spec 的 H1 冒烟（含 `LogCollector`）、H2 ViewModel、H3 交互三层；并让 `UiHelpers.Click` 能点到滚动视口之外的控件。

**Architecture:** 沿用批 1 的结构：每个应用一个 `Avalonia.<X>Demo.Tests` 项目（xUnit v3 + `Avalonia.Headless.XUnit`），引用对应演示项目与 `tests/Avalonia.Tests.Common`；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。H1 用 `TabWalker` 走完 `MainWindow` 的全部标签页；H2 对带逻辑的 ViewModel、转换器写普通 `[Fact]`；H3 用 `UiHelpers` 在单个页面里"操作 → 断言界面变化"。本批新增一处共享库改动（`UiHelpers.Click` 先 `BringIntoView`），由 Task 1 先做、先写失败的测试。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、xUnit v3（3.2.2）、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 2）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文。
- 版本只写在根目录 `Directory.Packages.props`；csproj 的 `PackageReference` **不带 `Version`**。本批用到的包都已登记，不需要改 `Directory.Packages.props`。
- 代码风格沿用 `tests/Avalonia.TestingDemo.Tests`：块作用域命名空间（`namespace X { ... }`）、测试方法名用英文下划线句式。
- 每个测试项目的 `TestAppBuilder` 只写 `HeadlessApp.Build<App>()`，不复制 `Avalonia.Tests.Common` 里的任何辅助代码。
- 目录约定：测试项目放 `tests/`，并登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`。
- 本批**不改 `src/` 下任何文件**（含不补 `AutomationId`）；各任务的"破坏检查"会临时改 `src/` 的一行，必须用 `git checkout --` 还原，提交前 `git status --short src` 必须无输出。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`。
- 探针放在仓库外（`C:\Temp\probe-*`），**永不提交**。
- `TestingDemo.Tests` 原有 30 个测试与批 1 的 37 个测试始终保持通过。
- 全局状态必须还原：测试若改了 `Application.Current.RequestedThemeVariant`、`Logger.Sink` 之类进程级状态，要在 `finally`/`Dispose` 里还原。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. **`UiHelpers.Click` 点不到滚动视口之外的控件**：测试窗口是 600×480，演示页大多包在 `ScrollViewer` 里，按钮在折叠线以下时 `MouseDown/MouseUp` 落在别的元素上，**命令不执行、也不报错**，断言只会看到"什么都没发生"（Task 1 先用失败的测试固定，ValidationPage 的提交按钮、PrecedencePage 的"重新读取"都踩过）。
2. **`UiHelpers.Type` 是在光标处插入，不是替换**：对已有文字的 `TextBox` 连续 `Type` 会得到 `张三丰张三` 这类拼接结果，断言既可能误失败也可能误通过。需要替换语义时先把 `Text` 置空再 `Type`（Task 5 的 `Replace` 辅助）。
3. **`DataBindingDemo` 的全局日志钩子与故意写错的绑定**：`DebuggingPage` 安装静态的 `BindingLogSink`，并故意放了两个坏绑定。"走完全部标签页无 Warning"的断言在这里不能是 `Assert.Empty`，而要**恰好两条**（`Selected.Name` 的 `Value is null`、`Selectd` 的 `Could not find a matching property accessor`），多一条或少一条都是回归（Task 5，兼作 `LogCollector` 的正向对照）。
4. **进程级主题状态**：`ThemesPage` 的按钮直接改 `Application.Current.RequestedThemeVariant`，测试若不还原会污染同进程里后面的测试（Task 4 用 `try/finally`）。
5. **本地值压制样式**：`StyledPropertyPage.StyledGauge` 故意不写 `Value`，`warning` 样式类才能把它设成 90；若有人给它补了默认值，样式永久失效且无日志。Task 2 的测试断言加类后为 90、去掉类后回到 0。

---

## 本份已核实的事实（编写计划时在仓库外的临时项目里实测）

探针项目在 `C:\Temp\probe-b2-propertysystem`、`probe-b2-datatemplates`、`probe-b2-styling`、`probe-b2-databinding`，测试代码与本计划里的文件一致（探针里多一个 `ClickShim`——它用同名扩展方法在探针里模拟 Task 1 的 `Click` 改动——和一个只打印文本的 `Dump` 测试，计划里不含）。

| 事项 | 实测结果 | 对计划的影响 |
|---|---|---|
| 四个应用的 `App`、`MainWindow` 都是 `public`，`MainWindow` 有无参构造 | 探针可直接 `new MainWindow()`、`HeadlessApp.Build<App>()` | 不需要 `InternalsVisibleTo` |
| 四个 `MainWindow` 的标签页标题 | PropertySystem：`StyledProperty`、`DirectProperty`、`附加属性`、`值优先级`、`元数据与回调`；DataTemplates：`控件内容`、`内联模板`、`模板集合`、`选择器`、`代码建模板`、`复用`、`ViewLocator`、`面板与树`、`对比 ControlTemplate`；Styling：`选择器`、`样式类`、`伪类`、`ControlTheme`、`主题变体`、`容器查询`、`字体`、`样式共享`、`优先级`；DataBinding：`绑定语法`、`编译绑定`、`集合`、`主从视图`、`多值绑定`、`命令`、`转换器`、`校验`、`集合视图`、`异步与图片`、`绑定调试`、`标记扩展` | Task 2–5 的页头断言 |
| `TabWalker` 走完 PropertySystem / DataTemplates / Styling，且全程挂着 `LogCollector` | 各 0 条 Warning 及以上日志，0 个页面异常 | 三者沿用批 1 的 `Walking_every_tab_logs_no_warnings` |
| `TabWalker` 走完 DataBinding，挂着 `LogCollector` | 0 个页面异常；**恰好 2 条** Warning（`Selected.Name` → `Value is null.`；`Selectd` → `Could not find a matching property accessor for 'Selectd' on '...DebuggingViewModel'`），两个 `[Fact]` 各跑一次结果相同 | Review Focus 第 3 条；`BindingLogSink.Current` 是静态的，但每次测试结束 `LogCollector.Dispose` 把 `Logger.Sink` 还原为 `null`，不会串到下一个测试 |
| `UiHelpers.Click` 点击滚动视口之外的按钮 | 原版：点击次数 0；先 `BringIntoView()` 再 `RunJobs()` 后：点击次数 1 | Task 1 |
| `UiHelpers.Type` 对已有文字的 `TextBox` | 光标在 0，文字被插入到最前面（`张三` → `张三丰张三`），`Fahrenheit` 里 `32.0` + 旧 `212.0` → `32.0212.0` 解析失败 | Review Focus 第 2 条 |
| 编译绑定下 `TextBox.Text` 与 `Slider.Value` 的双向绑定 | 属性变化即写回源，**不需要** `RunJobs`（`TextChanged` 事件才需要） | 本批的 TextBox 断言都不用 `RunJobs` |
| DataTemplates 各页渲染出的文字 | 见 Task 3 各测试里的字面量（全部由 `Dump` 探针从真实窗口里读出再写进断言） | 精确断言，不做模糊匹配 |
| `PanelsAndTreesPage` 的 `DefaultPanelList`（12 项） | 在 600×480 窗口里只实现 4 项（虚拟化）；`WrapPanelList` 实现全部 12 项 | Task 3 用"少于 12"而不是写死 4，避免依赖窗口高度 |
| 探针测试数 | PropertySystem 14、DataTemplates 14、Styling 10、DataBinding 25，共 63，全过 | 计划交付后的期望数字 |

## 文件结构

**新增**（`<X>` 为 `PropertySystem`、`DataTemplates`、`Styling`、`DataBinding`）

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.<X>Demo.Tests/Avalonia.<X>Demo.Tests.csproj` | 测试项目工程 |
| `tests/Avalonia.<X>Demo.Tests/TestAppBuilder.cs` | 一行：`HeadlessApp.Build<App>()` |
| `tests/Avalonia.<X>Demo.Tests/SmokeTests.cs` | H1：走完全部标签页、断言页头与无异常、断言日志 |
| `tests/Avalonia.<X>Demo.Tests/InteractionTests.cs` | H2/H3：控件级逻辑与"操作 → 断言界面变化" |
| `tests/Avalonia.DataBindingDemo.Tests/ViewModelTests.cs` | H2：ViewModel 与转换器（普通 `[Fact]`，不起 Avalonia） |
| `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs` | `UiHelpers.Click` 的回归测试（共享库的测试一直放在 `TestingDemo.Tests` 里，同 `TabWalkerTests`） |

`PropertySystemDemo` 的 `GaugeControl`、`StylingDemo` 的 `ToggleChip` 是控件而不是 ViewModel，它们的 H2 级测试放在各自的 `InteractionTests.cs` 里（需要 `[AvaloniaFact]`）。`DataTemplatesDemo` 的 `ViewLocator` 同理。

**修改**

| 文件 | 改动 |
|---|---|
| `tests/Avalonia.Tests.Common/UiHelpers.cs` | `Click` 先 `BringIntoView()` + `RunJobs()`（Task 1） |
| `hello-avalonia.slnx` | `/tests/` 加入四个新项目（各在自己的任务里加一行） |
| `README.md` | 测试项目那一行的"目前覆盖"清单补上本批四个应用（Task 6） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 2 实测结论」（Task 6） |

**不碰**：`src/` 下任何文件、`Directory.Packages.props`、`CLAUDE.md`。

---

### Task 1: `UiHelpers.Click` 先把目标滚进视口

**Files:**
- Create: `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs`
- Modify: `tests/Avalonia.Tests.Common/UiHelpers.cs`

**Interfaces:**
- Consumes（批 0 已交付）：`UiHelpers.Show(Control) : Window`（600×480）、`UiHelpers.Click(this Window, Control)`。
- Produces：`UiHelpers.Click` 的签名与行为不变，只多一步"先滚进视口"；对已在视口内的控件是空操作，所以批 0、批 1 的全部测试不受影响。Task 2–5 里所有点击视口外按钮的测试依赖它。

- [ ] **Step 1: 记录基线**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)"`
Expected: 0 错误。记下警告数（批 1 结束时是 6）；Task 6 要求结束时不高于这个数。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 67 个通过（TestingDemo 30 + 批 1 的 37）。

- [ ] **Step 2: 先写失败的测试**

#### `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class UiHelpersTests
    {
        [AvaloniaFact]
        public void Click_reaches_a_control_that_starts_below_the_scroll_viewport()
        {
            var clicks = 0;
            var button = new Button { Content = "far away", Height = 40 };
            button.Click += (_, _) => clicks++;
            // The test window is 600x480, so a 2000px spacer pushes the button far out of view.
            var panel = new StackPanel { Children = { new Border { Height = 2000 }, button } };
            var window = UiHelpers.Show(new ScrollViewer { Content = panel });

            window.Click(button);

            Assert.Equal(1, clicks);
        }
    }
}
```

- [ ] **Step 3: 确认它失败**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests --filter "FullyQualifiedName~UiHelpersTests"`
Expected: **失败**，`Expected: 1  Actual: 0`（点击落在视口外，没有触发 `Click`）。

- [ ] **Step 4: 改 `UiHelpers.Click`**

用 Edit 在 `tests/Avalonia.Tests.Common/UiHelpers.cs` 里做两处改动。

先在文件顶部的 using 列表里补一行（保持字母序，放在 `using Avalonia.VisualTree;` 之前或之后均可，以文件里现有顺序为准）：

```csharp
using Avalonia.Threading;
```

再替换 `Click`：

旧：

```csharp
        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        public static void Click(this Window window, Control control)
        {
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
```

新：

```csharp
        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        // Demo pages sit in ScrollViewers, so scroll the target into view first or the click lands on something else.
        public static void Click(this Window window, Control control)
        {
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
```

`MouseDown`/`MouseUp` 两行保持不动。

- [ ] **Step 5: 确认通过，且没有回归**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 31，失败: 0`（30 + 新增 1）。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 68 个通过（67 + 1）。批 1 的 37 个交互测试在新 `Click` 下仍全过，说明"对已在视口内的控件是空操作"成立。

- [ ] **Step 6: Commit**

```bash
git add tests/Avalonia.Tests.Common/UiHelpers.cs tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs
git status --short
git commit -m "test: make UiHelpers.Click scroll the target into view first"
```

`git status --short` 里只应出现上面两个文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 2: `Avalonia.PropertySystemDemo.Tests`

**Files:**
- Create: `tests/Avalonia.PropertySystemDemo.Tests/Avalonia.PropertySystemDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.PropertySystemDemo.App`、`Views.MainWindow`、`Views.Pages.{StyledPropertyPage, DirectPropertyPage, AttachedPropertyPage, PrecedencePage, MetadataPage}`、`Controls.GaugeControl`（`Value`、`Readout`，`Value` 被 coerce 到 0–100）、`Controls.HighlightBehavior`（`Get/SetIsHighlighted`、`Get/SetTagLine`）。
- Produces：测试项目 `Avalonia.PropertySystemDemo.Tests`（14 个测试）。

本任务没有"先写失败的测试"的空间：被测代码都已存在，测试写出来就应该通过（唯一的新行为——`Click` 滚动——在 Task 1 已用失败的测试固定）。验证"测试不是空转"靠最后的**破坏检查**。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.PropertySystemDemo.Tests/Avalonia.PropertySystemDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.PropertySystemDemo\Avalonia.PropertySystemDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.PropertySystemDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.PropertySystemDemo;
using Avalonia.PropertySystemDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.PropertySystemDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `StarterTutorialDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.PropertySystemDemo.Tests/Avalonia.PropertySystemDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.PropertySystemDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.PropertySystemDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.PropertySystemDemo.Tests
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
                new[] { "StyledProperty", "DirectProperty", "附加属性", "值优先级", "元数据与回调" },
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

- [ ] **Step 3: 控件级与页面交互测试**

#### `tests/Avalonia.PropertySystemDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.PropertySystemDemo.Controls;
using Avalonia.PropertySystemDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.PropertySystemDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaTheory]
        [InlineData(150, 100, "100%")]
        [InlineData(-20, 0, "0%")]
        [InlineData(55, 55, "55%")]
        public void Gauge_coerces_value_into_range_and_mirrors_it_in_readout(double written, double stored, string readout)
        {
            var gauge = new GaugeControl();
            Assert.Equal("0%", gauge.Readout);

            gauge.Value = written;

            Assert.Equal(stored, gauge.Value);
            Assert.Equal(readout, gauge.Readout);
        }

        [AvaloniaFact]
        public void Highlight_is_a_plain_attached_property_but_tag_line_inherits_down_the_tree()
        {
            var parent = new StackPanel();
            var child = new Button();
            parent.Children.Add(child);
            UiHelpers.Show(parent);

            Assert.False(HighlightBehavior.GetIsHighlighted(child));
            HighlightBehavior.SetIsHighlighted(parent, true);
            Assert.False(HighlightBehavior.GetIsHighlighted(child));

            Assert.Null(HighlightBehavior.GetTagLine(child));
            HighlightBehavior.SetTagLine(parent, "hello");
            Assert.Equal("hello", HighlightBehavior.GetTagLine(child));
        }

        [AvaloniaFact]
        public void Local_slider_value_is_clamped_by_the_gauge_it_drives()
        {
            var window = UiHelpers.Show(new StyledPropertyPage());
            var gauge = window.GetVisualDescendants().OfType<GaugeControl>().First();

            window.ByName<Slider>("ValueSlider").Value = 150;

            Assert.Equal(100, gauge.Value);
        }

        // StyledGauge deliberately has no local Value: a local value would outrank the style setter for good.
        [AvaloniaFact]
        public void Warning_class_sets_the_value_through_a_style_and_removing_it_restores_the_default()
        {
            var window = UiHelpers.Show(new StyledPropertyPage());
            var gauge = window.ByName<GaugeControl>("StyledGauge");
            Assert.Equal(0, gauge.Value);

            window.ByName<CheckBox>("WarningToggle").IsChecked = true;
            Assert.Equal(90, gauge.Value);
            Assert.Equal("90%", gauge.Readout);

            window.ByName<CheckBox>("WarningToggle").IsChecked = false;
            Assert.Equal(0, gauge.Value);
        }

        [AvaloniaFact]
        public void Direct_property_readout_follows_the_driving_slider()
        {
            var window = UiHelpers.Show(new DirectPropertyPage());

            window.ByName<Slider>("Driver").Value = 60;

            Assert.Equal("60%", window.ByName<GaugeControl>("Gauge").Readout);
        }

        [AvaloniaFact]
        public void Attached_page_highlights_target_a_only_and_tag_line_reaches_both_levels()
        {
            var window = UiHelpers.Show(new AttachedPropertyPage());
            var a = window.ByName<Button>("TargetA");
            var b = window.ByName<Button>("TargetB");

            window.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked = true;
            Assert.True(HighlightBehavior.GetIsHighlighted(a));
            Assert.False(HighlightBehavior.GetIsHighlighted(b));

            Assert.Equal("第 1 层读到：（尚未设置）", window.ByName<TextBlock>("Level1Text").Text);
            window.Click(ButtonWithText(window, "在最外层设置 TagLine"));
            Assert.Equal("第 1 层读到：我在最外层被设置", window.ByName<TextBlock>("Level1Text").Text);
            Assert.Equal("第 2 层读到：我在最外层被设置", window.ByName<TextBlock>("Level2Text").Text);
        }

        // The readouts are first filled on attach, before layout, so they say 0; the button re-reads after layout.
        [AvaloniaFact]
        public void Precedence_page_local_value_beats_style_and_binding_follows_the_view_model()
        {
            var window = UiHelpers.Show(new PrecedencePage());

            window.Click(ButtonWithText(window, "重新读取"));

            Assert.Equal("A 实际宽度：300（样式给的 300）", window.ByName<TextBlock>("ReadoutA").Text);
            Assert.Equal("B 实际宽度：150（本地值 150 压过样式 300）", window.ByName<TextBlock>("ReadoutB").Text);
            Assert.Equal("C 实际宽度：220（跟随滑块）", window.ByName<TextBlock>("ReadoutC").Text);
        }

        [AvaloniaTheory]
        [InlineData("写入 150", "写入 150，实际存下 100（被 coerce 夹取了）")]
        [InlineData("写入 -20", "写入 -20，实际存下 0（被 coerce 夹取了）")]
        [InlineData("写入 55", "写入 55，实际存下 55（在范围内，原样保留）")]
        public void Metadata_page_reports_what_the_coerce_callback_did(string button, string expected)
        {
            var window = UiHelpers.Show(new MetadataPage());

            window.Click(ButtonWithText(window, button));

            Assert.Equal(expected, window.ByName<TextBlock>("CoerceLog").Text);
        }
    }
}
```

- [ ] **Step 4: 运行**

Run: `dotnet test tests/Avalonia.PropertySystemDemo.Tests`
Expected: `通过: 14，失败: 0`（2 + 12：Theory 的 3 行与 3 行各算 3 个）。

Run: `dotnet build tests/Avalonia.PropertySystemDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

目的：证明 `Warning_class_sets_the_value_through_a_style_and_removing_it_restores_the_default` 真的在测"样式类驱动属性"（Review Focus 第 5 条）。

用 Edit 在 `src/Avalonia.PropertySystemDemo/Views/Pages/StyledPropertyPage.axaml` 的 `StyledGauge` 元素上加一个本地值 `Value="10"`：

旧：

```xml
                    <controls:GaugeControl Name="StyledGauge" Caption="样式类驱动" />
```

新：

```xml
                    <controls:GaugeControl Name="StyledGauge" Caption="样式类驱动" Value="10" />
```

Run: `dotnet test tests/Avalonia.PropertySystemDemo.Tests --filter "FullyQualifiedName~Warning_class"`
Expected: **失败**（`Expected: 0  Actual: 10`——本地值压制了样式，加类后也不是 90）。

立刻还原：

```bash
git checkout -- src/Avalonia.PropertySystemDemo/Views/Pages/StyledPropertyPage.axaml
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.PropertySystemDemo.Tests`，`通过: 14`。若加了本地值测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.PropertySystemDemo.Tests
git status --short
git commit -m "test: add Headless tests for PropertySystemDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.PropertySystemDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 3: `Avalonia.DataTemplatesDemo.Tests`

**Files:**
- Create: `tests/Avalonia.DataTemplatesDemo.Tests/Avalonia.DataTemplatesDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.DataTemplatesDemo.App`、`Views.MainWindow`、`ViewLocator`（`Match(object?)`、`Build(object?)`）、`ViewModels.{DashboardViewModel, SettingsViewModel}`（`Summary`、`DarkMode`）、`Views.Located.{DashboardView, SettingsView}`、`Views.Pages.{ControlContentPage, ContentTemplatesPage, TemplateCollectionPage, SelectorPage, CodeTemplatesPage, ReusePage, VersusControlTemplatePage, PanelsAndTreesPage, ViewLocatorPage}`。
- Produces：测试项目 `Avalonia.DataTemplatesDemo.Tests`（14 个测试）。

这个应用全是"数据 → 渲染出的文字"，所以断言一律用页面里**渲染出来的 `TextBlock` 文字**，字面量都是 `Dump` 探针从真实窗口里读出来的，不是照 XAML 猜的。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.DataTemplatesDemo.Tests/Avalonia.DataTemplatesDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.DataTemplatesDemo\Avalonia.DataTemplatesDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.DataTemplatesDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.DataTemplatesDemo;
using Avalonia.DataTemplatesDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.DataTemplatesDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `PropertySystemDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.DataTemplatesDemo.Tests/Avalonia.DataTemplatesDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.DataTemplatesDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.DataTemplatesDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.DataTemplatesDemo.Tests
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
                new[] { "控件内容", "内联模板", "模板集合", "选择器", "代码建模板", "复用", "ViewLocator", "面板与树", "对比 ControlTemplate" },
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

- [ ] **Step 3: 模板渲染与 ViewLocator 测试**

#### `tests/Avalonia.DataTemplatesDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.ViewModels;
using Avalonia.DataTemplatesDemo.Views.Located;
using Avalonia.DataTemplatesDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.DataTemplatesDemo.Tests
{
    public class InteractionTests
    {
        private static List<string?> Texts(Visual root)
            => root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaFact]
        public void Controls_render_themselves_but_arbitrary_objects_fall_back_to_to_string()
        {
            var window = UiHelpers.Show(new ControlContentPage());

            Assert.Equal(new[] { "我本身就是控件" }, Texts(window.ByName<ContentControl>("ControlContent")));
            Assert.Equal(new[] { "一段普通字符串" }, Texts(window.ByName<ContentControl>("StringContent")));
            Assert.Equal(new[] { "Avalonia.DataTemplatesDemo.Models.Circle" }, Texts(window.ByName<ContentControl>("ObjectContent")));
            Assert.Equal(new[] { "Avalonia.DataTemplatesDemo.Models.Circle" }, Texts(window.ByName<Button>("ObjectButton")));
            Assert.Equal(new[] { "现在由 ContentTemplate 决定怎么画。" }, Texts(window.ByName<ContentControl>("TemplatedContent")));
        }

        [AvaloniaFact]
        public void Inline_templates_draw_a_single_content_and_every_list_item()
        {
            var window = UiHelpers.Show(new ContentTemplatesPage());

            Assert.Equal(new[] { "半径 40" }, Texts(window.ByName<ContentControl>("SingleCircle")));
            Assert.Equal(new[] { "小", "中", "大" }, Texts(window.ByName<ListBox>("CircleList")));
        }

        [AvaloniaFact]
        public void Template_collection_picks_the_first_template_that_matches_so_order_matters()
        {
            var window = UiHelpers.Show(new TemplateCollectionPage());

            Assert.Equal(
                new[] { "● 圆，半径 3", "■ 方，边长 4", "（通用 Shape 模板）三角形" },
                Texts(window.ByName<ItemsControl>("SpecificFirst")));
            // The base-class template is listed first, so it swallows every subclass.
            Assert.Equal(
                new[] { "（通用 Shape 模板）圆", "（通用 Shape 模板）方", "（通用 Shape 模板）三角形" },
                Texts(window.ByName<ItemsControl>("BaseFirst")));
        }

        [AvaloniaFact]
        public void Selector_template_gives_each_person_the_template_for_their_sex()
        {
            var window = UiHelpers.Show(new SelectorPage());
            var list = window.ByName<ListBox>("PeopleList");

            var rows = list.GetVisualDescendants().OfType<StackPanel>()
                .Where(p => p.Classes.Contains("male") || p.Classes.Contains("female"))
                .ToList();

            Assert.Equal(new[] { "male", "female", "male" }, rows.Select(p => p.Classes.Contains("male") ? "male" : "female"));
            Assert.Equal(new[] { "1", "anyu", "Beijing" }, Texts(rows[0]));
        }

        [AvaloniaFact]
        public void Code_built_templates_render_and_a_match_predicate_filters_which_items_they_take()
        {
            var window = UiHelpers.Show(new CodeTemplatesPage());

            Assert.Equal(new[] { "代码模板：小", "代码模板：中", "代码模板：大" }, Texts(window.ByName<ListBox>("CodeList")));
            Assert.Equal(new[] { "兜底模板：小", "大圆模板：中", "大圆模板：大" }, Texts(window.ByName<ItemsControl>("FilteredList")));
        }

        [AvaloniaFact]
        public void One_template_resource_serves_content_list_and_combo_and_implicit_templates_work_anywhere_below()
        {
            var window = UiHelpers.Show(new ReusePage());

            Assert.Equal(new[] { "共享卡片：甲" }, Texts(window.ByName<ContentControl>("ReuseContent")));
            Assert.Equal(new[] { "共享卡片：甲", "共享卡片：乙" }, Texts(window.ByName<ListBox>("ReuseList")));
            Assert.Equal(new[] { "页面级隐式模板：边长 4" }, Texts(window.ByName<ContentControl>("ImplicitA")));
            Assert.Equal(new[] { "页面级隐式模板：边长 6" }, Texts(window.ByName<ContentControl>("ImplicitB")));
        }

        [AvaloniaFact]
        public void Control_template_and_data_template_are_independent_axes()
        {
            var window = UiHelpers.Show(new VersusControlTemplatePage());
            const string fallback = "Avalonia.DataTemplatesDemo.Models.Circle";

            Assert.Equal(new[] { fallback }, Texts(window.ByName<Button>("Neither")));
            Assert.Equal(new[] { "同一个圆" }, Texts(window.ByName<Button>("DataOnly")));
            Assert.Equal(new[] { fallback }, Texts(window.ByName<Button>("ControlOnly")));
            Assert.Equal(new[] { "同一个圆" }, Texts(window.ByName<Button>("Both")));
            // Only the buttons given the pill theme have the custom chrome.
            Assert.DoesNotContain(window.ByName<Button>("Neither").GetVisualDescendants().OfType<Border>(), b => b.Name == "Chrome");
            Assert.Contains(window.ByName<Button>("Both").GetVisualDescendants().OfType<Border>(), b => b.Name == "Chrome");
        }

        [AvaloniaFact]
        public void Default_panel_virtualizes_while_the_wrap_panel_realizes_every_item()
        {
            var window = UiHelpers.Show(new PanelsAndTreesPage());

            Assert.Equal(12, Texts(window.ByName<ListBox>("WrapPanelList")).Count);
            // Not pinned to an exact number: how many rows fit depends on the window height.
            Assert.True(Texts(window.ByName<ListBox>("DefaultPanelList")).Count < 12);
        }

        [AvaloniaFact]
        public void Tree_template_shows_children_after_a_node_is_expanded()
        {
            var window = UiHelpers.Show(new PanelsAndTreesPage());
            var tree = window.ByName<TreeView>("FolderTree");
            Assert.DoesNotContain("📁 工作", Texts(tree));

            ((TreeViewItem)tree.ContainerFromIndex(0)!).IsExpanded = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("📁 工作", Texts(tree));
            Assert.Contains("📁 个人", Texts(tree));
        }

        [AvaloniaFact]
        public void View_locator_page_swaps_the_view_with_the_view_model_and_shows_a_visible_miss()
        {
            var window = UiHelpers.Show(new ViewLocatorPage());
            var host = window.ByName<ContentControl>("Host");
            Assert.Single(host.GetVisualDescendants().OfType<DashboardView>());

            window.Click(ButtonWithText(window, "SettingsViewModel"));
            Assert.Single(host.GetVisualDescendants().OfType<SettingsView>());
            Assert.Empty(host.GetVisualDescendants().OfType<DashboardView>());

            window.Click(ButtonWithText(window, "MissingViewModel（没有对应视图）"));
            Assert.Equal(
                new[] { "找不到视图：Avalonia.DataTemplatesDemo.Views.Located.MissingView" },
                Texts(host));
        }

        [AvaloniaFact]
        public void View_locator_matches_only_view_models_and_builds_the_conventional_view()
        {
            var locator = new ViewLocator();

            Assert.True(locator.Match(new DashboardViewModel()));
            Assert.False(locator.Match("not a view model"));
            Assert.False(locator.Match(null));
            Assert.IsType<DashboardView>(locator.Build(new DashboardViewModel()));
            Assert.IsType<SettingsView>(locator.Build(new SettingsViewModel()));
        }

        [Fact]
        public void Dashboard_summary_and_settings_default_are_stable_strings()
        {
            Assert.Equal("今日待办 3 项，已完成 1 项", new DashboardViewModel().Summary);
            Assert.True(new SettingsViewModel().DarkMode);
        }
    }
}
```

- [ ] **Step 4: 运行**

Run: `dotnet test tests/Avalonia.DataTemplatesDemo.Tests`
Expected: `通过: 14，失败: 0`（2 + 12）。

Run: `dotnet build tests/Avalonia.DataTemplatesDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

目的：证明 ViewLocator 两个测试真的在测"按命名约定把 ViewModel 映射到视图"。

用 Edit 在 `src/Avalonia.DataTemplatesDemo/ViewLocator.cs` 里改一处：

旧：

```csharp
                .Replace(".ViewModels.", ".Views.Located.")
```

新：

```csharp
                .Replace(".ViewModels.", ".Views.")
```

Run: `dotnet test tests/Avalonia.DataTemplatesDemo.Tests --filter "FullyQualifiedName~View_locator"`
Expected: **失败**（至少 2 个：`Build` 返回的是"找不到视图"的 `TextBlock`，页面里找不到 `DashboardView`）。

立刻还原：

```bash
git checkout -- src/Avalonia.DataTemplatesDemo/ViewLocator.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.DataTemplatesDemo.Tests`，`通过: 14`。若改了映射**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.DataTemplatesDemo.Tests
git status --short
git commit -m "test: add Headless tests for DataTemplatesDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.DataTemplatesDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 4: `Avalonia.StylingDemo.Tests`

**Files:**
- Create: `tests/Avalonia.StylingDemo.Tests/Avalonia.StylingDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.StylingDemo.App`、`Views.MainWindow`、`Controls.ToggleChip`（`IsOn`，`:on` 伪类）、`Views.Pages.{PseudoClassesPage, StyleClassesPage, ControlThemesPage, ThemesPage, FontsPage}`。
- Produces：测试项目 `Avalonia.StylingDemo.Tests`（10 个测试）。无 ViewModel，不补（项目约定）。

样式类页面（`SelectorsPage`、`ContainerQueriesPage`、`SharingStylesPage`、`PrecedencePage`）只靠 H1：它们靠颜色、悬停等视觉效果演示，没有可靠的非视觉断言点；容器查询的行为已在批 1 的 `LayoutDemo` 里测过。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.StylingDemo.Tests/Avalonia.StylingDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.StylingDemo\Avalonia.StylingDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.StylingDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.StylingDemo;
using Avalonia.StylingDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.StylingDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `DataTemplatesDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.StylingDemo.Tests/Avalonia.StylingDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.StylingDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.StylingDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.StylingDemo.Tests
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
                new[] { "选择器", "样式类", "伪类", "ControlTheme", "主题变体", "容器查询", "字体", "样式共享", "优先级" },
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

- [ ] **Step 3: 伪类、样式类、ControlTheme、主题变体、字体的测试**

#### `tests/Avalonia.StylingDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.StylingDemo.Controls;
using Avalonia.StylingDemo.Views.Pages;
using Avalonia.Styling;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.StylingDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaFact]
        public void Toggle_chip_owns_the_on_pseudo_class_while_it_is_latched()
        {
            var chip = new ToggleChip();
            Assert.False(chip.IsOn);
            Assert.DoesNotContain(":on", chip.Classes);

            chip.IsOn = true;
            Assert.Contains(":on", chip.Classes);

            chip.IsOn = false;
            Assert.DoesNotContain(":on", chip.Classes);
        }

        [AvaloniaFact]
        public void Clicking_the_chip_latches_it_and_the_page_text_follows()
        {
            var window = UiHelpers.Show(new PseudoClassesPage());
            var chip = window.ByName<ToggleChip>("Chip");

            window.Click(chip);

            Assert.True(chip.IsOn);
            Assert.Contains(":on", chip.Classes);
            Assert.Contains("IsOn = True", window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));

            window.Click(chip);
            Assert.False(chip.IsOn);
            Assert.DoesNotContain(":on", chip.Classes);
        }

        [AvaloniaFact]
        public void Unchecking_the_box_disables_the_controlled_button_and_marks_it_with_the_disabled_pseudo_class()
        {
            var window = UiHelpers.Show(new PseudoClassesPage());
            var controlled = ButtonWithText(window, "被控制的按钮");
            Assert.DoesNotContain(":disabled", controlled.Classes);

            window.ByName<CheckBox>("EnableBox").IsChecked = false;

            Assert.False(controlled.IsEnabled);
            Assert.Contains(":disabled", controlled.Classes);
        }

        [AvaloniaFact]
        public void Warn_switch_adds_and_removes_the_warn_class_through_a_binding()
        {
            var window = UiHelpers.Show(new StyleClassesPage());
            var swatch = window.ByName<Border>("BoundSwatch");
            Assert.DoesNotContain("warn", swatch.Classes);

            window.ByName<ToggleSwitch>("WarnSwitch").IsChecked = true;
            Assert.Contains("warn", swatch.Classes);

            window.ByName<ToggleSwitch>("WarnSwitch").IsChecked = false;
            Assert.DoesNotContain("warn", swatch.Classes);
        }

        [AvaloniaFact]
        public void Toggle_green_button_edits_the_classes_of_the_code_swatch()
        {
            var window = UiHelpers.Show(new StyleClassesPage());
            var text = window.ByName<TextBlock>("CodeClassesText");
            Assert.Equal("当前 Classes：swatch", text.Text);

            window.Click(ButtonWithText(window, "切换 green"));
            Assert.Equal("当前 Classes：swatch green", text.Text);

            window.Click(ButtonWithText(window, "切换 green"));
            Assert.Equal("当前 Classes：swatch", text.Text);
        }

        [AvaloniaFact]
        public void Control_themes_set_size_and_derived_themes_inherit_the_base_setters()
        {
            var window = UiHelpers.Show(new ControlThemesPage());

            Assert.Equal(200, window.ByName<Button>("YellowButton").Width);
            Assert.Equal(40, window.ByName<Button>("YellowButton").Height);
            Assert.Equal(200, window.ByName<Button>("WideButton").Width);
            Assert.Equal(200, window.ByName<Button>("TallWideButton").Width);
            Assert.Equal(56, window.ByName<Button>("TallWideButton").Height);
        }

        // The page writes Application.RequestedThemeVariant, which is process-wide: always put it back.
        [AvaloniaFact]
        public void Theme_buttons_change_the_requested_and_actual_variant_and_the_app_is_restored()
        {
            var app = Avalonia.Application.Current!;
            var original = app.RequestedThemeVariant;
            try
            {
                var window = UiHelpers.Show(new ThemesPage());
                var text = window.ByName<TextBlock>("ActualText");

                window.Click(ButtonWithText(window, "Dark"));
                Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
                Assert.Equal("请求：Dark　实际：Dark", text.Text);

                window.Click(ButtonWithText(window, "Light"));
                Assert.Equal("请求：Light　实际：Light", text.Text);
            }
            finally
            {
                app.RequestedThemeVariant = original;
            }

            Assert.Equal(original, app.RequestedThemeVariant);
        }

        [AvaloniaFact]
        public void Letter_spacing_and_line_height_follow_their_sliders()
        {
            var window = UiHelpers.Show(new FontsPage());

            window.ByName<Slider>("SpacingSlider").Value = 4;
            window.ByName<Slider>("LineSlider").Value = 40;

            // The sample text is the only wrapping 16pt TextBlock on the page.
            var sample = window.GetVisualDescendants().OfType<TextBlock>()
                .Single(t => t.TextWrapping == Avalonia.Media.TextWrapping.Wrap && t.FontSize == 16);
            Assert.Equal(4, sample.LetterSpacing);
            Assert.Equal(40, sample.LineHeight);
        }
    }
}
```

- [ ] **Step 4: 运行**

Run: `dotnet test tests/Avalonia.StylingDemo.Tests`
Expected: `通过: 10，失败: 0`（2 + 8）。

Run: `dotnet build tests/Avalonia.StylingDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

目的：证明两个 `ToggleChip` 测试真的在测"IsOn 变化时更新 `:on` 伪类"。

用 Edit 在 `src/Avalonia.StylingDemo/Controls/ToggleChip.cs` 里改一处：

旧：

```csharp
                PseudoClasses.Set(":on", IsOn);
```

新：

```csharp
                PseudoClasses.Set(":off", IsOn);
```

Run: `dotnet test tests/Avalonia.StylingDemo.Tests --filter "FullyQualifiedName~chip"`
Expected: **失败**（2 个：`Toggle_chip_owns...` 与 `Clicking_the_chip...`，`Classes` 里没有 `:on`）。

立刻还原：

```bash
git checkout -- src/Avalonia.StylingDemo/Controls/ToggleChip.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.StylingDemo.Tests`，`通过: 10`。若改了伪类名**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.StylingDemo.Tests
git status --short
git commit -m "test: add Headless tests for StylingDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.StylingDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 5: `Avalonia.DataBindingDemo.Tests`

**Files:**
- Create: `tests/Avalonia.DataBindingDemo.Tests/Avalonia.DataBindingDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`ViewModelTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click/Type`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.DataBindingDemo.App`、`Views.MainWindow`、`ViewModels.{CommandsViewModel, ValidationViewModel, CollectionsViewModel, CollectionViewsViewModel, DebuggingViewModel}`、`Models.{Contact, ContactGroupHeader}`、`Converters.{CelsiusToFahrenheitConverter, InlineConverters}`、`Views.Pages.{CommandsPage, ValidationPage, ConvertersPage, CollectionsPage, MasterDetailPage, CollectionViewsPage}`。
- Produces：测试项目 `Avalonia.DataBindingDemo.Tests`（25 个测试）。

`AsyncPage`（2 秒的 `Task<string>`、`DispatcherTimer` 时钟、`Bitmap` 资源）与 `DebuggingPage` 的日志列表只靠 H1：前者依赖真实时间，后者的行为已由冒烟里"恰好两条警告"的断言覆盖。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.DataBindingDemo.Tests/Avalonia.DataBindingDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.DataBindingDemo\Avalonia.DataBindingDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.DataBindingDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.DataBindingDemo;
using Avalonia.DataBindingDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.DataBindingDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `StylingDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.DataBindingDemo.Tests/Avalonia.DataBindingDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试（含"恰好两条"的日志断言）**

#### `tests/Avalonia.DataBindingDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.DataBindingDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System.Linq;
using Xunit;

namespace Avalonia.DataBindingDemo.Tests
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
                new[] { "绑定语法", "编译绑定", "集合", "主从视图", "多值绑定", "命令", "转换器", "校验", "集合视图", "异步与图片", "绑定调试", "标记扩展" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // The debugging tab breaks two bindings on purpose; any other warning, or a missing one, is a regression.
        // This also doubles as the positive control for LogCollector: it must see real binding warnings.
        [AvaloniaFact]
        public void Walking_every_tab_logs_only_the_two_deliberate_debugging_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, logs.Messages.Count);
            Assert.Single(logs.Messages, m => m.Contains("Selected.Name") && m.Contains("Value is null"));
            Assert.Single(logs.Messages, m => m.Contains("Selectd") && m.Contains("Could not find a matching property accessor"));
        }
    }
}
```

- [ ] **Step 3: H2 ViewModel 与转换器测试**

这些测试不起 Avalonia，用普通 `[Fact]`/`[Theory]`。

#### `tests/Avalonia.DataBindingDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.Data;
using Avalonia.DataBindingDemo.Converters;
using Avalonia.DataBindingDemo.Models;
using Avalonia.DataBindingDemo.ViewModels;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Avalonia.DataBindingDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Decrement_is_disabled_at_zero_and_notified_when_count_changes()
        {
            var vm = new CommandsViewModel();
            var raised = 0;
            vm.DecrementCommand.CanExecuteChanged += (_, _) => raised++;
            Assert.False(vm.DecrementCommand.CanExecute(null));

            vm.IncrementCommand.Execute(null);

            Assert.True(vm.DecrementCommand.CanExecute(null));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Add_command_parses_its_parameter()
        {
            var vm = new CommandsViewModel();

            vm.AddCommand.Execute("5");
            vm.AddCommand.Execute("10");

            Assert.Equal(15, vm.Count);
        }

        [Fact]
        public void User_name_needs_three_characters_to_pass_validation()
        {
            var vm = new ValidationViewModel();

            vm.UserName = "ab";
            Assert.True(vm.HasErrors);
            Assert.Contains("用户名至少 3 个字符", vm.GetErrors(nameof(vm.UserName)).Select(e => e.ErrorMessage));
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.UserName = "abc";
            Assert.False(vm.HasErrors);
            Assert.True(vm.SubmitCommand.CanExecute(null));
        }

        [Fact]
        public void Bad_email_is_reported_and_blocks_submit()
        {
            var vm = new ValidationViewModel { UserName = "abc" };

            vm.Email = "not-an-email";

            Assert.Contains("邮箱格式不对", vm.GetErrors(nameof(vm.Email)).Select(e => e.ErrorMessage));
            Assert.False(vm.SubmitCommand.CanExecute(null));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(151)]
        public void Age_outside_the_range_throws_and_keeps_the_old_value(int age)
        {
            var vm = new ValidationViewModel();

            var ex = Assert.Throws<DataValidationException>(() => vm.Age = age);

            Assert.Equal("年龄必须在 0 到 150 之间", ex.Message);
            Assert.Equal(18, vm.Age);
        }

        [Fact]
        public void Add_grows_both_lists_but_remove_last_only_touches_the_observable_one()
        {
            var vm = new CollectionsViewModel();

            vm.AddCommand.Execute(null);
            vm.RemoveLastCommand.Execute(null);
            vm.RemoveLastCommand.Execute(null);

            Assert.Equal(2, vm.Contacts.Count);
            Assert.Equal(4, vm.FrozenContacts.Count);
        }

        [Fact]
        public void Filter_sort_and_grouping_rebuild_the_rows()
        {
            var vm = new CollectionViewsViewModel();
            Assert.Equal(6, vm.Rows.Count);

            vm.Filter = "张";
            Assert.Equal(new[] { "张三" }, vm.Rows.Cast<Contact>().Select(c => c.Name));

            vm.Filter = "";
            vm.SortByAge = true;
            Assert.Equal(new[] { 22, 26, 28, 30, 35, 41 }, vm.Rows.Cast<Contact>().Select(c => c.Age));

            // Groups come out in order of first appearance in the age-sorted list, not alphabetically.
            vm.GroupByCity = true;
            var headers = vm.Rows.OfType<ContactGroupHeader>().ToList();
            Assert.Equal(new[] { "北京", "深圳", "上海", "广州" }, headers.Select(h => h.Key));
            Assert.Equal(2, headers[0].Count);
            Assert.Equal(10, vm.Rows.Count);
        }

        [Fact]
        public void Toggle_flips_selected_between_null_and_a_contact()
        {
            var vm = new DebuggingViewModel();

            vm.ToggleCommand.Execute(null);
            Assert.Equal("张三", vm.Selected?.Name);

            vm.ToggleCommand.Execute(null);
            Assert.Null(vm.Selected);
        }

        [Fact]
        public void Contact_to_string_shows_name_and_city()
        {
            Assert.Equal("张三（北京）", new Contact("张三", "北京", 28).ToString());
        }

        [Theory]
        [InlineData(-5.0, "❄ 结冰（-5 °C）")]
        [InlineData(20.0, "🙂 舒适（20 °C）")]
        [InlineData(30.0, "🔥 炎热（30 °C）")]
        public void Thermometer_picks_the_band_for_the_temperature(double celsius, string expected)
        {
            Assert.Equal(expected, InlineConverters.Thermometer.Convert(celsius, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Celsius_converter_round_trips_and_ignores_unparseable_input()
        {
            var converter = new CelsiusToFahrenheitConverter();
            var inv = CultureInfo.InvariantCulture;

            Assert.Equal("212.0", converter.Convert(100.0, typeof(string), null, inv));
            Assert.Equal(100.0, (double)converter.ConvertBack("212", typeof(double), null, inv)!, 6);
            Assert.Same(BindingOperations.DoNothing, converter.ConvertBack("-", typeof(double), null, inv));
            Assert.Same(BindingOperations.DoNothing, converter.Convert("text", typeof(string), null, inv));
        }
    }
}
```

- [ ] **Step 4: H3 页面交互测试**

#### `tests/Avalonia.DataBindingDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.Models;
using Avalonia.DataBindingDemo.ViewModels;
using Avalonia.DataBindingDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Avalonia.DataBindingDemo.Tests
{
    public class InteractionTests
    {
        // UiHelpers.Type inserts at the caret, so clear the box first to get replace semantics.
        private static void Replace(Window window, TextBox box, string text)
        {
            box.Text = "";
            window.Type(box, text);
        }

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaFact]
        public void Commands_page_counts_and_enables_decrement_only_above_zero()
        {
            var window = UiHelpers.Show(new CommandsPage());
            var decrement = window.ByName<Button>("DecrementButton");
            Assert.False(decrement.IsEffectivelyEnabled);

            window.Click(ButtonWithText(window, "+1"));
            Assert.Equal("Count = 1", window.ByName<TextBlock>("CountText").Text);
            Assert.True(decrement.IsEffectivelyEnabled);

            window.Click(decrement);
            Assert.Equal("Count = 0", window.ByName<TextBlock>("CountText").Text);
            Assert.False(decrement.IsEffectivelyEnabled);
        }

        [AvaloniaFact]
        public void Commands_page_passes_the_command_parameter_and_binds_to_a_method()
        {
            var window = UiHelpers.Show(new CommandsPage());

            window.Click(ButtonWithText(window, "+10"));
            window.Click(window.ByName<Button>("MethodButton"));

            Assert.Equal("Count = 10", window.ByName<TextBlock>("CountText").Text);
            Assert.Equal("方法绑定被调用，参数 = 小明", window.ByName<TextBlock>("LastActionText").Text);
        }

        [AvaloniaFact]
        public void Validation_page_enables_submit_only_for_a_valid_user_name()
        {
            var page = new ValidationPage();
            var window = UiHelpers.Show(page);
            var submit = window.ByName<Button>("SubmitButton");
            Assert.False(submit.IsEffectivelyEnabled);

            Replace(window, window.ByName<TextBox>("UserNameBox"), "ab");
            Assert.False(submit.IsEffectivelyEnabled);

            Replace(window, window.ByName<TextBox>("UserNameBox"), "abc");
            Assert.True(submit.IsEffectivelyEnabled);

            // The submit button sits below the fold of the 600x480 test window; Click scrolls it into view.
            window.Click(submit);
            Assert.Equal("已提交：abc / someone@example.com / 18 岁", ((ValidationViewModel)page.DataContext!).SubmitResult);
        }

        [AvaloniaFact]
        public void Validation_page_keeps_the_old_age_when_the_setter_rejects_a_value()
        {
            var page = new ValidationPage();
            var window = UiHelpers.Show(page);

            Replace(window, window.ByName<TextBox>("AgeBox"), "200");

            Assert.Equal(18, ((ValidationViewModel)page.DataContext!).Age);
            Assert.True(DataValidationErrors.GetHasErrors(window.ByName<TextBox>("AgeBox")));
        }

        [AvaloniaFact]
        public void Converters_page_converts_celsius_to_fahrenheit_and_back()
        {
            var window = UiHelpers.Show(new ConvertersPage());
            var celsius = window.ByName<Slider>("Celsius");
            var fahrenheit = window.ByName<TextBox>("Fahrenheit");

            celsius.Value = 100;
            // The binding formats with the current culture, so build the expectation the same way.
            Assert.Equal(212d.ToString("F1", CultureInfo.CurrentCulture), fahrenheit.Text);
            Assert.Equal("🔥 炎热（100 °C）", window.ByName<TextBlock>("FuncText").Text);

            Replace(window, fahrenheit, 32d.ToString("F1", CultureInfo.CurrentCulture));
            Assert.Equal(0, celsius.Value, 6);

            // Half-typed input is ignored by the converter instead of resetting the source.
            Replace(window, fahrenheit, "-");
            Assert.Equal(0, celsius.Value, 6);
        }

        [AvaloniaFact]
        public void Converters_page_toggles_hints_with_the_built_in_string_converters()
        {
            var window = UiHelpers.Show(new ConvertersPage());
            Assert.True(window.ByName<TextBlock>("EmptyWarning").IsVisible);
            Assert.False(window.ByName<TextBlock>("NotEmptyNote").IsVisible);

            Replace(window, window.ByName<TextBox>("Input"), "x");

            Assert.False(window.ByName<TextBlock>("EmptyWarning").IsVisible);
            Assert.True(window.ByName<TextBlock>("NotEmptyNote").IsVisible);
        }

        [AvaloniaFact]
        public void Collections_page_shows_the_new_row_only_in_the_observable_list()
        {
            var window = UiHelpers.Show(new CollectionsPage());
            Assert.Equal("共 3 人", window.ByName<TextBlock>("LiveCount").Text);

            window.Click(ButtonWithText(window, "两边各加一人"));

            Assert.Equal("共 4 人", window.ByName<TextBlock>("LiveCount").Text);
            Assert.Equal(4, window.ByName<ListBox>("LiveList").ItemCount);
            // The plain List raised no change notification: the bound count and the rows stay at 3.
            Assert.Equal("共 3 人", window.ByName<TextBlock>("FrozenCount").Text);
            Assert.Equal(3, window.ByName<ListBox>("FrozenList").ItemCount);
        }

        [AvaloniaFact]
        public void Master_detail_page_reveals_the_detail_panel_and_edits_write_back()
        {
            var window = UiHelpers.Show(new MasterDetailPage());
            Assert.True(window.ByName<TextBlock>("EmptyHint").IsVisible);
            Assert.False(window.ByName<StackPanel>("DetailPanel").IsVisible);

            var list = window.ByName<ListBox>("MasterList");
            list.SelectedIndex = 0;

            Assert.False(window.ByName<TextBlock>("EmptyHint").IsVisible);
            Assert.True(window.ByName<StackPanel>("DetailPanel").IsVisible);
            Assert.Equal("张三", window.ByName<TextBox>("DetailName").Text);

            Replace(window, window.ByName<TextBox>("DetailName"), "张三丰");
            Assert.Equal("张三丰", ((Contact)list.SelectedItem!).Name);
        }

        [AvaloniaFact]
        public void Collection_views_page_filters_rows_as_the_filter_text_is_typed()
        {
            var window = UiHelpers.Show(new CollectionViewsPage());
            var rows = window.ByName<ListBox>("RowList");
            Assert.Equal(6, rows.ItemCount);

            Replace(window, window.ByName<TextBox>("FilterBox"), "张");

            Assert.Equal(1, rows.ItemCount);
        }
    }
}
```

- [ ] **Step 5: 运行**

Run: `dotnet test tests/Avalonia.DataBindingDemo.Tests`
Expected: `通过: 25，失败: 0`（2 + 14 + 9；Theory 的 2 行与 3 行各算 2 个、3 个）。

Run: `dotnet build tests/Avalonia.DataBindingDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

若 `Validation_page_enables_submit...` 在最后一个断言读到空字符串，先确认 Task 1 的 `Click` 改动已在（Review Focus 第 1 条）。

- [ ] **Step 6: 破坏检查（临时改 `src/`，必须还原）**

目的：证明命令的 CanExecute 刷新测试（H2 与 H3 各一个）真的在测 `[NotifyCanExecuteChangedFor]`。

用 Edit 在 `src/Avalonia.DataBindingDemo/ViewModels/CommandsViewModel.cs` 里删掉这一行：

```csharp
        [NotifyCanExecuteChangedFor(nameof(DecrementCommand))]
```

Run: `dotnet test tests/Avalonia.DataBindingDemo.Tests --filter "FullyQualifiedName~Decrement|FullyQualifiedName~Commands_page_counts"`
Expected: **失败**（2 个：`CanExecuteChanged` 一次都没触发；页面上点 +1 后 `-1` 按钮仍是禁用的）。

立刻还原：

```bash
git checkout -- src/Avalonia.DataBindingDemo/ViewModels/CommandsViewModel.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.DataBindingDemo.Tests`，`通过: 25`。若删掉那一行测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 7: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.DataBindingDemo.Tests
git status --short
git commit -m "test: add Headless tests for DataBindingDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.DataBindingDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 6: 文档同步、回填 spec、整体验证

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

**Interfaces:**
- Consumes: Task 1–5 交付的共享库改动与 4 个测试项目（本批共 63 + 1 个新测试）、Task 1 Step 1 记下的构建警告基线。
- Produces: 与现状一致的 `README.md`；回填后的 spec（批 3 起的计划以它为准）。

- [ ] **Step 1: `README.md` 的测试项目行补上本批四个应用**

用 Edit 替换这一行里的括号部分：

旧：

```markdown
目前覆盖 Fundamentals、Xaml、Layout、StarterTutorial）
```

新：

```markdown
目前覆盖 Fundamentals、Xaml、Layout、StarterTutorial、Styling、DataBinding、DataTemplates、PropertySystem）
```

- [ ] **Step 2: spec 里 `UiHelpers` 一行与 Task 1 的改动一致**

用 Edit 替换：

旧：

```markdown
| `UiHelpers` | 从 `TestingDemo.Tests` 迁入并改为 `public`：`Show`、`ByName`、`ByAutomationId`、`Click`、`Type`，签名与行为不变 |
```

新：

```markdown
| `UiHelpers` | 从 `TestingDemo.Tests` 迁入并改为 `public`：`Show`、`ByName`、`ByAutomationId`、`Click`、`Type`，签名不变；批 2 起 `Click` 点击前先 `BringIntoView()` 并 `RunJobs()`，以点到滚动视口之外的控件；`Type` 是在光标处插入而非替换 |
```

- [ ] **Step 3: 回填「批 2 实测结论」**

把 spec 里这一行：

```markdown
## 批 2 实测结论（执行后回填）
```

替换为下面的整段。「执行时的实测」一栏要按本次执行的真实输出填写：三个数字（构建警告数、全解决方案测试数、构建耗时）填实际值，破坏检查一栏按 Step 4 各任务的真实输出填写；若某项与下面写的预期不符，写实际情况，不要照抄。

```markdown
## 批 2 实测结论（计划 02 执行后）

| 项目 | 测试数 | H1 页数 | 说明 |
|---|---|---|---|
| `Avalonia.PropertySystemDemo.Tests` | 14 | 5 | 无 ViewModel；`GaugeControl` 的 coerce 与只读 `Readout`、附加属性与继承、值优先级、元数据；`warning` 样式类设值 |
| `Avalonia.DataTemplatesDemo.Tests` | 14 | 9 | 渲染文字精确断言；模板集合顺序、选择器、代码建模板、复用、`ViewLocator`（含"找不到视图"）、面板虚拟化、树展开 |
| `Avalonia.StylingDemo.Tests` | 10 | 9 | `ToggleChip` 的 `:on` 伪类、样式类绑定、`ControlTheme` 继承、主题变体（带还原）、字体滑块 |
| `Avalonia.DataBindingDemo.Tests` | 25 | 12 | H2：命令、校验、集合、集合视图、转换器；H3：命令、校验、转换器、集合、主从、集合视图；冒烟里"恰好两条"警告 |
| `Avalonia.TestingDemo.Tests`（新增） | 1 | — | `UiHelpersTests`：`Click` 能点到视口之外 |

执行时的实测：构建警告数 <填实际值>（批开始前 <基线>）；`dotnet test hello-avalonia.slnx` 共 <填实际值> 个测试通过；全解决方案构建约 <填实际值> 秒；破坏检查 <逐项填：PropertySystem 加本地值 Value="10"、DataTemplates 改 ViewLocator 映射、Styling 把 `:on` 改成 `:off`、DataBinding 删 `NotifyCanExecuteChangedFor` 各自失败的个数>，均已还原。

**对后续批的决定与发现**（计划阶段在探针里实测，执行阶段原样复现）：

- **`UiHelpers.Click` 必须先把目标滚进视口**：测试窗口 600×480，演示页大多在 `ScrollViewer` 里，视口外的按钮点击会静默落空（命令不执行、无日志）。已在共享库修复并有回归测试；批 3–6 的点击测试不需要再自己 `BringIntoView`。
- **`UiHelpers.Type` 是插入而非替换**：对已有文字的 `TextBox` 需要先把 `Text` 置空再 `Type`（批 2 在 `DataBindingDemo.Tests` 里用本地 `Replace` 辅助）。若后续批里这个模式再出现两次以上，再考虑上提到共享库。
- **`LogCollector` 在有故意坏绑定的应用上的用法**：`DataBindingDemo` 的 `DebuggingPage` 故意放了两个坏绑定，冒烟断言写成"恰好两条、各按关键字匹配"，既防多报也防漏报，同时是 `LogCollector` 的正向对照（它确实能收到真实的绑定警告）。`BindingLogSink.Current` 是静态单例，但 `LogCollector.Dispose` 会还原 `Logger.Sink`，没有串到别的测试。
- **编译绑定下 `TextBox.Text`/`Slider.Value` 的双向绑定同步生效**，不需要 `RunJobs`；需要 `RunJobs` 的只有 `TextChanged`、`SizeChanged`、展开树节点这类经 dispatcher 派发的事件。
- **进程级状态要在 `finally` 里还原**：`ThemesPage` 直接改 `Application.Current.RequestedThemeVariant`，测试用 `try/finally` 还原；批 3–6 里改全局状态的页面（窗口管理、剪贴板、`Application.Current` 上的资源）照此处理。
- **虚拟化的断言不要写死数量**：`DefaultPanelList` 在 600×480 里只实现 4 项，写"少于总数"而不是 `4`，避免依赖窗口高度。
- **只靠 H1 的页面**：`AsyncPage`（真实时间、`Bitmap` 资源）、`DebuggingPage` 的日志列表、`StylingDemo` 的 `SelectorsPage`/`ContainerQueriesPage`/`SharingStylesPage`/`PrecedencePage`（只有视觉效果）。
```

- [ ] **Step 4: 整体验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)|个错误|Error\(s\)"`
Expected: 0 错误；警告数不高于 Task 1 Step 1 记下的基线。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 131 个通过（TestingDemo 31 + 批 1 的 37 + 本批 63）。

Run: `git status --short`
Expected: 只有 Step 1–3 改动的 `README.md` 与 spec 文件，没有 `C:\Temp\probe-*` 的文件；`git status --short src` 无输出。

- [ ] **Step 5: Commit**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git status --short
git commit -m "docs: record batch 2 results and list the new test projects"
```

`git status --short` 里只应出现上面两个文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。
