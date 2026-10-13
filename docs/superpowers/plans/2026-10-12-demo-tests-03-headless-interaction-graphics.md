# 演示测试 03：Events、Input、Graphics、CustomControls 的 Headless 测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `EventsDemo`、`InputDemo`、`GraphicsDemo`、`CustomControlsDemo` 各建一个 Headless 测试项目，覆盖 spec 的 H1 冒烟（含 `LogCollector`）、H2 ViewModel/控件逻辑、H3 交互三层；并给共享库补两个辅助（`UiHelpers.CaptureFrame` 与 `CultureScope`），让"自绘控件真的重绘了"和"与区域设置无关的文字断言"可以被测。

**Architecture:** 沿用批 1、2 的结构：每个应用一个 `Avalonia.<X>Demo.Tests` 项目（xUnit v3 + `Avalonia.Headless.XUnit`），引用对应演示项目与 `tests/Avalonia.Tests.Common`；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。H1 用 `TabWalker` 走完 `MainWindow` 的全部标签页；H2 对 ViewModel 与自定义控件写测试；H3 用 `UiHelpers` 在单个页面里"操作 → 断言界面变化"。本批新增的共享库改动由 Task 1 先做、先写失败的测试；自绘控件用"截图字节比较"验证 `AffectsRender` 真的生效；弹出层、键盘热键、指针捕获都用 Headless 平台自带的输入模拟。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、Avalonia.Skia、xUnit v3（3.2.2）、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 3）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文。
- 版本只写在根目录 `Directory.Packages.props`；csproj 的 `PackageReference` **不带 `Version`**。本批用到的包都已登记，不需要改 `Directory.Packages.props`。
- 代码风格沿用 `tests/Avalonia.TestingDemo.Tests`：块作用域命名空间（`namespace X { ... }`）、测试方法名用英文下划线句式。
- 每个测试项目的 `TestAppBuilder` 只写 `HeadlessApp.Build<App>()`，不复制 `Avalonia.Tests.Common` 里的任何辅助代码。
- 目录约定：测试项目放 `tests/`，并登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`。
- 本批**不改 `src/` 下任何文件**（含不补 `AutomationId`）；各任务的"破坏检查"会临时改 `src/` 的一行，必须用 `git checkout --` 还原，提交前 `git status --short src` 必须无输出。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`。
- 探针放在仓库外（`C:\Temp\probe-*`），**永不提交**。
- 此前各批的 132 个测试始终保持通过。
- 全局状态必须还原：测试若改了 `Application.Current.RequestedThemeVariant`、`Logger.Sink`、`CultureInfo.CurrentCulture` 之类进程级状态，要在 `finally`/`Dispose` 里还原。
- 本批**不对动画、过渡、合成动画的中间态做断言**（spec 的规定）：`AnimationsPage`、`TransitionsPage`、`EasingPage` 的"走一趟"按钮只靠 H1。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. **自绘控件漏了 `AffectsRender`**：`Sketch`、`RingGauge` 的属性变了、画面不变，**没有异常也没有日志**。只断言属性值抓不到，必须比较渲染出的像素（Task 4 的 `Sketch` 三个开关、Task 5 的 `RingGauge`；Task 1 的 `CaptureFrame` 是它们的前提，各自的破坏检查证明测试真能看见回归）。
2. **带 `Command` 的按钮在 `CanExecute == false` 时 `IsEnabled` 仍是 `true`**：只有 `IsEffectivelyEnabled` 反映真相。断言 `IsEnabled` 会让"锁定时按钮应当变灰"的测试永远抓不到回归；同时点击锁定的按钮必须不执行命令（Task 3，`InteractivityPage`）。
3. **点击落在窗口之外会静默落空**：`CustomPanelPage` 的"减一个"按钮在 600 宽的测试窗口之外，且那一行不在 `ScrollViewer` 里，`Click` 的 `BringIntoView` 帮不上；表现是计数不变、无报错。测试把窗口放宽到 1000 再点，并断言计数真的减少（Task 5）。
4. **与区域设置相关的文字**：`Meter.Display` 用 `{ratio:P0}`、`EasingPage` 用 `F3`，在 sv-SE、fr-FR 等区域设置下的小数点、负号、百分号空格都不同。涉及的测试用 `CultureScope.Invariant()` 固定区域设置并还原（Task 1 提供，Task 4、5 使用）。
5. **弹出层的内容只有打开之后才在视觉树里**：`SwatchFlyout` 的色块在 Headless 里画在窗口的叠加层上，真实鼠标点击能选色、点外部能关闭，但**打开之前窗口里一个色块都没有**。若测试忘了先打开就去找色块，`Single`/`First` 会抛"没有匹配元素"，而用 `Any` 之类的写法会静默空转。测试先断言打开前 0 个、打开后恰好 6 个，再真实点击第三个色块，断言回填的文字与颜色、弹出层关闭；再点外部，断言轻触关闭（Task 5）。

---

## 本份已核实的事实（编写计划时在仓库外的临时项目里实测）

探针项目在 `C:\Temp\probe-b3-events`、`probe-b3-input`、`probe-b3-graphics`、`probe-b3-customcontrols`。执行前会把本计划里的测试文件抽取到真实项目中整体验证（Task 2–5 的"运行"步骤给出期望的数字）。

| 事项 | 实测结果 | 对计划的影响 |
|---|---|---|
| 四个应用的 `App`、`MainWindow` 都是 `public`，`MainWindow` 有无参构造 | 探针可直接 `new MainWindow()`、`HeadlessApp.Build<App>()` | 不需要 `InternalsVisibleTo` |
| 四个 `MainWindow` 的标签页标题 | Events：`生命周期`、`输入事件`、`路由三阶段`、`Handled`、`自定义路由事件`；Input：`指针`、`焦点`、`手势`、`键盘与 HotKey`、`交互的写法`、`拖放`、`文本输入`、`路由事件`；Graphics：`画刷与渐变`、`变换`、`形状与几何`、`自定义绘制`、`特效`、`裁剪与命中`、`图标`、`渲染选项`、`关键帧动画`、`控件过渡`、`页面过渡`、`缓动函数`、`合成动画`；CustomControls：`用户控件`、`模板化控件`、`自绘控件`、`属性与事件`、`控件树`、`自定义面板`、`自定义 Flyout` | Task 2–5 的页头断言 |
| `TabWalker` 走完四个应用，且全程挂着 `LogCollector` | 各 0 条日志、0 个页面异常（含 Graphics 的动画/合成页、CustomControls 的自绘页） | 四个项目都用批 1 的 `Walking_every_tab_logs_no_warnings`，`Assert.Empty` |
| `LifecyclePage` 的日志 | 点"加入按钮"后，`SizeChanged`、`Loaded` 要 `RunJobs` 才出现；顺序恒为 `AttachedToLogicalTree`、`AttachedToVisualTree`、`Initialized`、`SizeChanged`、`Loaded`；移除后接 `DetachedFromLogicalTree`、`DetachedFromVisualTree`、`Unloaded` | Task 2 |
| 路由与 Handled | 点 `Inner`：`Outer:tunnel`、`Middle:tunnel`、`Inner:tunnel`、`Inner:bubble`、`Middle:bubble`、`Outer:bubble`；按键（`Handled` 关）：六条，`handledEventsToo` 那条 `Handled=False`；`Handled` 开：四条，且最后一条 `Handled=True`、没有 `middle:bubble`/`outer:bubble` | Task 2 |
| `Window.KeyPress(Key, RawInputModifiers, PhysicalKey, string?)` | 能驱动 `KeyBinding`（`Ctrl+Shift+K`）、`Button.HotKey`（`Ctrl+S`）、`MenuItem.HotKey`（`Ctrl+D2/D3`）、Tab 导航、页面的 `KeyDown` 处理器；`Ctrl+D1`（只写 `InputGesture`）**不**触发 | Task 3 |
| `Window.Type(control, "a1b2")` 对"拒绝非数字"的 `TextInput` 隧道处理器 | 整串 `a1b2` 作为**一个** `TextInput` 事件，含字母就整串被 `Handled`，结果是空；逐字符 `Type` 才得到 `12` | Task 3 的 `TextInput` 测试逐字符输入 |
| 带 `Command` 的按钮，`CanExecute == false` | `IsEnabled` 仍为 `True`，`IsEffectivelyEnabled` 为 `False`；点击不执行命令 | Review Focus 第 2 条 |
| 指针页 | `MouseMove/Down/Wheel` 驱动 `Readout`；按下手柄后 `e.Pointer.Captured` 生效，窗口外的 `MouseMove` 仍会让手柄跟随并被夹在 `0..Track.Width-Handle.Width`（`0..260`）；释放后 `DragState` 回到 `空闲（捕获已释放）` | Task 3 |
| 焦点页 | `Focus(Tab)` 后样式类含 `:focus-visible`，`Focus(Pointer)` 后不含；`Tab` 键从 A 到 B、再到 D（跳过 `IsTabStop=False` 的 C）；点"读取 FocusManager 当前焦点"后读数是 `Button`（被点的按钮没有名字，回落到类型名） | Task 3 |
| 手势页 | 先点一次 `Pad`，再在同一点按下抬起一次 → `Tapped=1`、`DoubleTapped=1`（第二下算双击，不再加 `Tapped`；探针里连按三下才得到 `Tapped=2`）；右键按下抬起 → `RightTapped=1`；"清零"把三个计数清回 0 | Task 3 |
| `CaptureRenderedFrame` | `HeadlessApp.Build` 用的是 `UseSkia + UseHeadlessDrawing=false`，能截到真实像素；`ForceRenderTimerTick()` 后截图；同一场景连截两次字节相同，改一个画笔后字节不同 | Task 1 |
| `Sketch` 三个开关 | `UseClip`、`UseTransform`、`UseOpacity` 各自打开后截图都变化；`UseClip` 关回去后与最初一致 | Task 4 |
| 变换页 | 角度滑块到 90：`RenderRight` 的位置不变（231）、`LayoutRight` 左移 66（`110` 宽的色块转成 `44` 宽） | Task 4 |
| `ClipAndHitPage` | 点无 `Background` 的方块：没有计数；点 `Background="Transparent"` 的方块：+1；点 `IsHitTestVisible="False"` 的方块：没有计数 | Task 4 |
| `CustomPanelPage` | 600 宽窗口里"减一个"按钮点不到（计数停在 7）；把窗口放宽到 1000 后：加一个 7、减两个 5、再连点十次 0 | Review Focus 第 3 条；Task 5 |
| `SwatchFlyout` | 打开前窗口里 `Width == 32` 的按钮 0 个；点 `PickButton` 后 `IsOpen == true`、窗口视觉树里恰好 6 个（叠加层，不是独立窗口），真实点击第三个色块 → `ChosenText` 变 `#ffe8d54a`、`Chosen.Background` 同色、`IsOpen == false`；再次打开后点窗口里别处 → `IsOpen == false`（轻触关闭） | Review Focus 第 5 条；Task 5，无需反射 |
| 控件树页 | `OnlyVisualHost.Child.DataContext == null`；`VisualAndLogicalHost.Child.DataContext` 取得父级的 | Task 5 |
| 只靠 H1 的页面 | Events 无；Input：`DragDropPage`（`DoDragDropAsync` 是真实的操作系统拖放）、`RoutedEventsPage`（纯静态说明页）；Graphics：`BrushesPage`、`EffectsPage`、`IconsPage`、`DrawingPage`（除 `Sketch`）、`AnimationsPage`、`TransitionsPage`；CustomControls：`PropertiesAndEventsPage`（纯静态说明页） | 不为它们写 H3 测试 |

## 文件结构

**新增**（`<X>` 为 `Events`、`Input`、`Graphics`、`CustomControls`）

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.<X>Demo.Tests/Avalonia.<X>Demo.Tests.csproj` | 测试项目工程 |
| `tests/Avalonia.<X>Demo.Tests/TestAppBuilder.cs` | 一行：`HeadlessApp.Build<App>()` |
| `tests/Avalonia.<X>Demo.Tests/SmokeTests.cs` | H1：走完全部标签页、断言页头与无异常、断言日志 |
| `tests/Avalonia.<X>Demo.Tests/InteractionTests.cs` | H2/H3：控件级逻辑与"操作 → 断言界面变化" |
| `tests/Avalonia.InputDemo.Tests/ViewModelTests.cs` | H2：`InteractivityViewModel`（普通 `[Fact]`，不起 Avalonia） |
| `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs` 的新增用例、`CultureScopeTests.cs` | 本批两个共享库辅助的回归测试 |

**修改**

| 文件 | 改动 |
|---|---|
| `tests/Avalonia.Tests.Common/UiHelpers.cs` | 新增 `CaptureFrame(this Window)`（Task 1） |
| `tests/Avalonia.Tests.Common/CultureScope.cs`（新增） | `CultureScope.Invariant()`（Task 1） |
| `hello-avalonia.slnx` | `/tests/` 加入四个新项目（各在自己的任务里加一行） |
| `README.md` | 测试项目那一行的"目前覆盖"清单补上本批四个应用（Task 6） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 3 实测结论」（Task 6） |

**不碰**：`src/` 下任何文件、`Directory.Packages.props`、`CLAUDE.md`。

---

### Task 1: 共享库补两个辅助：`UiHelpers.CaptureFrame` 与 `CultureScope`

**Files:**
- Create: `tests/Avalonia.Tests.Common/CultureScope.cs`、`tests/Avalonia.TestingDemo.Tests/CultureScopeTests.cs`
- Modify: `tests/Avalonia.Tests.Common/UiHelpers.cs`、`tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs`

**Interfaces:**
- Consumes（批 0–2 已交付）：`UiHelpers.Show(Control) : Window`（600×480）、`HeadlessApp.Build<TApp>()`（`UseSkia` + `UseHeadlessDrawing=false`，所以截图是真像素）。
- Produces：
  - `UiHelpers.CaptureFrame(this Window window) : byte[]`——先 `RunJobs()` 与 `ForceRenderTimerTick()`，再把窗口当前渲染帧存成 PNG 字节。同一场景连截两次字节相同；场景变了字节不同。Task 4（`Sketch`）、Task 5（`RingGauge`）依赖它。
  - `CultureScope.Invariant() : CultureScope`（`IDisposable`）——把 `CultureInfo.CurrentCulture` 设成不变区域设置，`Dispose` 还原。Task 4（`EasingPage` 的 `F3`）、Task 5（`Meter` 的 `P0`）依赖它。

- [ ] **Step 1: 记录基线**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)"`
Expected: 0 错误。记下警告数（批 2 结束时是 5）；Task 6 要求结束时不高于这个数。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 132 个通过。

- [ ] **Step 2: 先写失败的测试**

用 Edit 在 `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs` 里做两处改动。

先补 using（放在 `using Avalonia.Headless.XUnit;` 之后）：

```csharp
using Avalonia.Media;
```

再在 `ButtonWithText_finds_the_button_by_its_text_and_skips_buttons_with_other_content` 之后、类的收尾 `}` 之前加：

```csharp
        // Self-drawn controls that forget AffectsRender change a property without changing the picture, so the only
        // way to see it is to compare pixels.
        [AvaloniaFact]
        public void CaptureFrame_is_stable_for_the_same_scene_and_changes_when_the_scene_changes()
        {
            var border = new Border { Background = Brushes.Red };
            var window = UiHelpers.Show(border);

            var first = window.CaptureFrame();
            var again = window.CaptureFrame();
            border.Background = Brushes.Blue;
            var changed = window.CaptureFrame();

            Assert.Equal(first, again);
            Assert.NotEqual(first, changed);
        }
```

#### `tests/Avalonia.TestingDemo.Tests/CultureScopeTests.cs`

```csharp
using Avalonia.Tests.Common;
using System.Globalization;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class CultureScopeTests
    {
        [Fact]
        public void Invariant_pins_the_culture_inside_the_scope_and_restores_it_afterwards()
        {
            var original = CultureInfo.CurrentCulture;
            var other = new CultureInfo("sv-SE");
            CultureInfo.CurrentCulture = other;
            try
            {
                using (CultureScope.Invariant())
                {
                    Assert.Same(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
                    Assert.Equal("0.5", 0.5.ToString("F1", CultureInfo.CurrentCulture));
                }

                Assert.Same(other, CultureInfo.CurrentCulture);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
```

- [ ] **Step 3: 确认它们失败**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: **编译失败**——`CS1061: "Window"未包含"CaptureFrame"的定义`、`CS0103: 当前上下文中不存在名称"CultureScope"`。

- [ ] **Step 4: 实现两个辅助**

用 Edit 在 `tests/Avalonia.Tests.Common/UiHelpers.cs` 里做两处改动。

先在 using 列表里补两行（`Avalonia.Media.Imaging` 放在 `using Avalonia.Input;` 之后，`System.IO` 放在 `using System.Linq;` 之前；`Bitmap.Save(Stream)` 的无选项重载已过时，会多一条 CS0618 警告，所以显式传 `PngBitmapEncoderOptions`）：

```csharp
using Avalonia.Media.Imaging;
```

```csharp
using System.IO;
```

再在 `Type` 方法之后、类的收尾 `}` 之前加：

```csharp
        // Renders whatever is pending and returns the window's current frame as PNG bytes. Needs the Skia headless
        // setup in HeadlessApp; with the default headless drawing the bitmap is 1x1 and every frame looks the same.
        public static byte[] CaptureFrame(this Window window)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var stream = new MemoryStream();
            window.CaptureRenderedFrame()!.Save(stream, new PngBitmapEncoderOptions());
            return stream.ToArray();
        }
```

#### `tests/Avalonia.Tests.Common/CultureScope.cs`

```csharp
using System;
using System.Globalization;

namespace Avalonia.Tests.Common
{
    // Pins CultureInfo.CurrentCulture for the length of a test and restores it afterwards, so assertions on formatted
    // text ("40 %", "0.062") do not depend on the machine's regional settings.
    public sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _saved = CultureInfo.CurrentCulture;

        private CultureScope(CultureInfo culture) => CultureInfo.CurrentCulture = culture;

        public static CultureScope Invariant() => new(CultureInfo.InvariantCulture);

        public void Dispose() => CultureInfo.CurrentCulture = _saved;
    }
}
```

- [ ] **Step 5: 确认通过，且没有回归**

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 34，失败: 0`（32 + 新增 2）。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 134 个通过（132 + 2）。

- [ ] **Step 6: Commit**

```bash
git add tests/Avalonia.Tests.Common/UiHelpers.cs tests/Avalonia.Tests.Common/CultureScope.cs tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs tests/Avalonia.TestingDemo.Tests/CultureScopeTests.cs
git status --short
git commit -m "test: add CaptureFrame and CultureScope to the shared test helpers"
```

`git status --short` 里只应出现上面四个文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 2: `Avalonia.EventsDemo.Tests`

**Files:**
- Create: `tests/Avalonia.EventsDemo.Tests/Avalonia.EventsDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click/Type`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.EventsDemo.App`、`Views.MainWindow`、`Views.Pages.{LifecyclePage, InputEventsPage, RoutingPage, HandledPage, CustomRoutedEventPage}`（每页有一个名为 `LogList` 的 `ListBox`，条目形如 `03  Inner:tunnel`）、`Controls.Notifier`（`Ping` 事件、`PingEvent`）、`Controls.PingEventArgs`（`Message`、`Source`）。
- Produces：测试项目 `Avalonia.EventsDemo.Tests`（10 个测试）。

本任务没有"先写失败的测试"的空间：被测代码都已存在，测试写出来就应该通过。验证"测试不是空转"靠最后的**破坏检查**。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.EventsDemo.Tests/Avalonia.EventsDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.EventsDemo\Avalonia.EventsDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.EventsDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.EventsDemo;
using Avalonia.EventsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.EventsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `DataBindingDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.EventsDemo.Tests/Avalonia.EventsDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.EventsDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.EventsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.EventsDemo.Tests
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
                new[] { "生命周期", "输入事件", "路由三阶段", "Handled", "自定义路由事件" },
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

- [ ] **Step 3: 事件顺序与路由的交互测试**

#### `tests/Avalonia.EventsDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.EventsDemo.Controls;
using Avalonia.EventsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System.Collections;
using System.Linq;
using Xunit;

namespace Avalonia.EventsDemo.Tests
{
    public class InteractionTests
    {
        // Each page keeps its EventLog private; the ListBox next to it shows the same entries.
        // Entries look like "03  Inner:tunnel", so drop the two-digit sequence number and the gap.
        private static string[] Messages(Control page)
            => ((IEnumerable)page.ByName<ListBox>("LogList").ItemsSource!)
                .Cast<string>()
                .Select(line => line.Substring(4))
                .ToArray();

        [AvaloniaFact]
        public void Adding_the_probed_button_logs_the_attach_events_in_order()
        {
            var page = new LifecyclePage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Button>("AddButton"));
            // SizeChanged and Loaded are dispatched, not raised synchronously.
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[] { "AttachedToLogicalTree", "AttachedToVisualTree", "Initialized", "SizeChanged", "Loaded" },
                Messages(page));
            Assert.False(page.ByName<Button>("AddButton").IsEnabled);
            Assert.True(page.ByName<Button>("RemoveButton").IsEnabled);
        }

        [AvaloniaFact]
        public void Removing_the_probed_button_logs_the_detach_events_after_the_attach_ones()
        {
            var page = new LifecyclePage();
            var window = UiHelpers.Show(page);
            window.Click(page.ByName<Button>("AddButton"));
            Dispatcher.UIThread.RunJobs();

            window.Click(page.ByName<Button>("RemoveButton"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[] { "DetachedFromLogicalTree", "DetachedFromVisualTree", "Unloaded" },
                Messages(page).Skip(5));
            Assert.True(page.ByName<Button>("AddButton").IsEnabled);
            Assert.False(page.ByName<Button>("RemoveButton").IsEnabled);
        }

        [AvaloniaFact]
        public void Pointer_press_tunnels_through_the_three_layers_and_bubbles_back_out()
        {
            var page = new RoutingPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Border>("Inner"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[] { "Outer:tunnel", "Middle:tunnel", "Inner:tunnel", "Inner:bubble", "Middle:bubble", "Outer:bubble" },
                Messages(page));
        }

        [AvaloniaFact]
        public void Key_press_reaches_every_layer_while_Handled_is_off()
        {
            var page = new HandledPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();

            window.KeyPress(Key.A, RawInputModifiers.None, PhysicalKey.A, "a");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[]
                {
                    "outer:tunnel", "middle:tunnel", "inner:bubble  (Handled was False)",
                    "middle:bubble", "outer:bubble", "outer (handledEventsToo)  Handled=False",
                },
                Messages(page));
        }

        [AvaloniaFact]
        public void Handled_stops_the_bubble_but_a_handledEventsToo_handler_still_runs()
        {
            var page = new HandledPage();
            var window = UiHelpers.Show(page);
            page.ByName<CheckBox>("HandleBox").IsChecked = true;
            page.ByName<TextBox>("Entry").Focus();

            window.KeyPress(Key.B, RawInputModifiers.None, PhysicalKey.B, "b");
            Dispatcher.UIThread.RunJobs();

            // No middle:bubble and no outer:bubble: the inner handler cut the route short.
            Assert.Equal(
                new[]
                {
                    "outer:tunnel", "middle:tunnel", "inner:bubble  (Handled was False)",
                    "outer (handledEventsToo)  Handled=True",
                },
                Messages(page));
        }

        [AvaloniaFact]
        public void Custom_routed_event_bubbles_from_its_source_up_to_the_listener()
        {
            var page = new CustomRoutedEventPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Notifier>("Alpha"));
            window.Click(page.ByName<Notifier>("Beta"));
            Dispatcher.UIThread.RunJobs();

            // Only Alpha has the CLR-wrapper handler; both reach the attached listener on the StackPanel.
            Assert.Equal(
                new[]
                {
                    "Alpha.Ping +=  (direct on the control)  Message='ping from Alpha'",
                    "Listener (attached)  Message='ping from Alpha'  Source=Alpha",
                    "Listener (attached)  Message='ping from Beta'  Source=Beta",
                },
                Messages(page));
        }

        [AvaloniaFact]
        public void Notifier_raises_Ping_with_its_own_name_and_itself_as_the_source()
        {
            var notifier = new Notifier { Name = "Solo" };
            PingEventArgs? received = null;
            notifier.Ping += (_, e) => received = e;
            var window = UiHelpers.Show(notifier);

            window.Click(notifier);

            Assert.NotNull(received);
            Assert.Equal("ping from Solo", received.Message);
            Assert.Same(notifier, received.Source);
        }

        [AvaloniaFact]
        public void Input_events_page_logs_the_pointer_the_key_and_the_text()
        {
            var page = new InputEventsPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Border>("Pad"));
            window.KeyPress(Key.Q, RawInputModifiers.None, PhysicalKey.Q, "q");
            window.Type(page.ByName<TextBox>("Entry"), "z");
            Dispatcher.UIThread.RunJobs();

            var lines = Messages(page);
            Assert.Equal("PointerEntered", lines[0]);
            // The position depends on the window layout, so only the prefix is pinned.
            Assert.StartsWith("PointerPressed  Mouse  left=True  right=False", lines[1]);
            Assert.Equal(
                new[] { "PointerReleased", "KeyDown  key=Q  modifiers=None", "TextInput  text='z'" },
                lines.Skip(2));
        }
    }
}
```

- [ ] **Step 4: 运行**

Run: `dotnet test tests/Avalonia.EventsDemo.Tests`
Expected: `通过: 10，失败: 0`（2 + 8）。

Run: `dotnet build tests/Avalonia.EventsDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

目的：证明 Handled 与自定义路由事件的测试真的在测"路由"。

**(a)** 用 Edit 在 `src/Avalonia.EventsDemo/Views/Pages/HandledPage.axaml.cs` 里让内层处理器不再置 `Handled`：

旧：

```csharp
                    e.Handled = true;
```

新：

```csharp
                    e.Handled = false;
```

Run: `dotnet test tests/Avalonia.EventsDemo.Tests --filter "FullyQualifiedName~Handled_stops"`
Expected: **失败**（多出 `middle:bubble`、`outer:bubble`，`Handled=True` 变成 `Handled=False`）。

立刻还原：

```bash
git checkout -- src/Avalonia.EventsDemo/Views/Pages/HandledPage.axaml.cs
git status --short src
```

Expected: `git status --short src` 无输出。

**(b)** 用 Edit 在 `src/Avalonia.EventsDemo/Controls/Notifier.cs` 里把自定义事件改成不冒泡：

旧：

```csharp
            RoutedEvent.Register<Notifier, PingEventArgs>(nameof(Ping), RoutingStrategies.Bubble);
```

新：

```csharp
            RoutedEvent.Register<Notifier, PingEventArgs>(nameof(Ping), RoutingStrategies.Direct);
```

Run: `dotnet test tests/Avalonia.EventsDemo.Tests --filter "FullyQualifiedName~Custom_routed_event"`
Expected: **失败**（`Listener (attached)` 两行消失）。

立刻还原：

```bash
git checkout -- src/Avalonia.EventsDemo/Controls/Notifier.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.EventsDemo.Tests`，`通过: 10`。若改了之后测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.EventsDemo.Tests
git status --short
git commit -m "test: add Headless tests for EventsDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.EventsDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 3: `Avalonia.InputDemo.Tests`

**Files:**
- Create: `tests/Avalonia.InputDemo.Tests/Avalonia.InputDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`ViewModelTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 2 之后）：`UiHelpers.Show/ByName/ButtonWithText/Click/Type`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`；Headless 的 `Window.KeyPress/MouseMove/MouseDown/MouseUp/MouseWheel`。
- Consumes（被测代码，只读）：`Avalonia.InputDemo.App`、`Views.MainWindow`、`Views.Pages.{PointerPage, FocusPage, GesturesPage, KeyboardPage, InteractivityPage, TextInputPage}`、`ViewModels.InteractivityViewModel`（`Count`、`Unlocked`、`IncrementCommand`；`CanExecute` 跟随 `Unlocked`）。`DragDropPage`、`RoutedEventsPage` 只靠 H1。
- Produces：测试项目 `Avalonia.InputDemo.Tests`（20 个测试）。

本任务没有"先写失败的测试"的空间：被测代码都已存在，测试写出来就应该通过。验证"测试不是空转"靠最后的**破坏检查**。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.InputDemo.Tests/Avalonia.InputDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.InputDemo\Avalonia.InputDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.InputDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.InputDemo;
using Avalonia.InputDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.InputDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `EventsDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.InputDemo.Tests/Avalonia.InputDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.InputDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.InputDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.InputDemo.Tests
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
                new[] { "指针", "焦点", "手势", "键盘与 HotKey", "交互的写法", "拖放", "文本输入", "路由事件" },
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

- [ ] **Step 3: H2 ViewModel 测试**

#### `tests/Avalonia.InputDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.InputDemo.ViewModels;
using Xunit;

namespace Avalonia.InputDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Increment_is_blocked_while_the_view_model_is_locked()
        {
            var vm = new InteractivityViewModel();

            Assert.False(vm.IncrementCommand.CanExecute(null));
        }

        [Fact]
        public void Unlocking_raises_CanExecuteChanged_and_enables_the_command()
        {
            var vm = new InteractivityViewModel();
            var raised = 0;
            vm.IncrementCommand.CanExecuteChanged += (_, _) => raised++;

            vm.Unlocked = true;

            Assert.Equal(1, raised);
            Assert.True(vm.IncrementCommand.CanExecute(null));
        }

        [Fact]
        public void Locking_again_disables_the_command_again()
        {
            var vm = new InteractivityViewModel { Unlocked = true };

            vm.Unlocked = false;

            Assert.False(vm.IncrementCommand.CanExecute(null));
        }

        [Fact]
        public void Increment_adds_one_each_time_it_runs()
        {
            var vm = new InteractivityViewModel { Unlocked = true };

            vm.IncrementCommand.Execute(null);
            vm.IncrementCommand.Execute(null);

            Assert.Equal(2, vm.Count);
        }
    }
}
```

- [ ] **Step 4: H3 页面交互测试**

#### `tests/Avalonia.InputDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.InputDemo.ViewModels;
using Avalonia.InputDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.InputDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Control page, string name) => page.ByName<TextBlock>(name).Text!;

        private static Point CentreOf(Window window, Control control)
            => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        [AvaloniaFact]
        public void Click_handler_and_Tapped_add_one_even_while_the_command_is_locked()
        {
            var page = new InteractivityPage();
            var window = UiHelpers.Show(page);
            var vm = (InteractivityViewModel)page.DataContext!;

            window.Click(page.ButtonWithText("Click 处理器"));
            Assert.Equal(1, vm.Count);
            Assert.Equal("Click 处理器触发（不看 CanExecute）", Text(page, "HandlerReadout"));

            window.Click(page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Tapped 手势"));
            Assert.Equal(2, vm.Count);
            Assert.Equal("Tapped 触发（不看 CanExecute）", Text(page, "HandlerReadout"));
        }

        [AvaloniaFact]
        public void Command_button_ignores_clicks_while_locked_and_works_once_the_checkbox_unlocks_it()
        {
            var page = new InteractivityPage();
            var window = UiHelpers.Show(page);
            var vm = (InteractivityViewModel)page.DataContext!;
            var command = page.ButtonWithText("Command");

            // A command that cannot execute leaves IsEnabled alone; IsEffectivelyEnabled is the one that flips.
            Assert.False(command.IsEffectivelyEnabled);
            window.Click(command);
            Assert.Equal(0, vm.Count);

            window.Click(page.GetVisualDescendants().OfType<CheckBox>().Single());
            Dispatcher.UIThread.RunJobs();

            Assert.True(vm.Unlocked);
            Assert.True(command.IsEffectivelyEnabled);
            window.Click(command);
            Assert.Equal(1, vm.Count);
        }

        [AvaloniaFact]
        public void HotKey_runs_the_command_but_an_InputGesture_alone_does_not()
        {
            var page = new InteractivityPage();
            var window = UiHelpers.Show(page);
            var vm = (InteractivityViewModel)page.DataContext!;
            window.Click(page.GetVisualDescendants().OfType<CheckBox>().Single());
            Dispatcher.UIThread.RunJobs();

            // "只写 InputGesture" only displays the text; "只写 HotKey" and "两个都写" really bind the key.
            window.KeyPress(Key.D1, RawInputModifiers.Control, PhysicalKey.Digit1, "1");
            Assert.Equal(0, vm.Count);
            window.KeyPress(Key.D2, RawInputModifiers.Control, PhysicalKey.Digit2, "2");
            Assert.Equal(1, vm.Count);
            window.KeyPress(Key.D3, RawInputModifiers.Control, PhysicalKey.Digit3, "3");
            Assert.Equal(2, vm.Count);
        }

        [AvaloniaFact]
        public void Keyboard_readout_shows_the_key_the_physical_key_and_the_modifiers()
        {
            var page = new KeyboardPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();

            window.KeyPress(Key.A, RawInputModifiers.Shift, PhysicalKey.A, "A");

            Assert.Equal("Key=A  PhysicalKey=A  Modifiers=Shift  KeySymbol='A'", Text(page, "KeyReadout"));
        }

        [AvaloniaFact]
        public void Ctrl_Shift_K_fires_the_key_binding_and_Ctrl_K_alone_does_not()
        {
            var page = new KeyboardPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();
            Assert.Equal("Ctrl+Shift+K 触发了 0 次", Text(page, "BindReadout"));

            window.KeyPress(Key.K, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.K, "K");
            window.KeyPress(Key.K, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.K, "K");
            window.KeyPress(Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");

            Assert.Equal("Ctrl+Shift+K 触发了 2 次", Text(page, "BindReadout"));
        }

        [AvaloniaFact]
        public void Ctrl_S_hotkey_clicks_the_save_button()
        {
            var page = new KeyboardPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();
            Assert.Equal("保存了 0 次", Text(page, "SaveReadout"));

            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");

            Assert.Equal("保存了 2 次", Text(page, "SaveReadout"));
        }

        [AvaloniaFact]
        public void Pointer_readout_follows_move_press_and_wheel()
        {
            var page = new PointerPage();
            var window = UiHelpers.Show(page);
            var centre = CentreOf(window, page.ByName<Border>("Pad"));

            window.MouseMove(centre);
            Assert.StartsWith("Moved", Text(page, "Readout"));
            Assert.Contains("L=False", Text(page, "Readout"));

            window.MouseDown(centre, MouseButton.Left);
            Assert.StartsWith("Pressed", Text(page, "Readout"));
            Assert.Contains("L=True", Text(page, "Readout"));

            window.MouseUp(centre, MouseButton.Left);
            window.MouseWheel(centre, new Vector(0, 1));
            // The vector's own text depends on the culture, so only the prefix is pinned.
            Assert.StartsWith("Wheel  delta=", Text(page, "Readout"));
        }

        [AvaloniaFact]
        public void Dragging_the_handle_keeps_following_the_pointer_and_clamps_at_both_ends_of_the_track()
        {
            var page = new PointerPage();
            var window = UiHelpers.Show(page);
            var handle = page.ByName<Border>("Handle");
            var track = page.ByName<Canvas>("Track");
            Point OnTrack(double x) => track.TranslatePoint(new Point(x, 25), window)!.Value;

            window.MouseDown(CentreOf(window, handle), MouseButton.Left);
            Assert.Equal("拖动中（已捕获）", Text(page, "DragState"));

            // The handle is 40 wide and grabbed at its centre, so it sits 20 to the left of the pointer.
            window.MouseMove(OnTrack(150));
            Assert.Equal(130, Canvas.GetLeft(handle));

            // The pointer is captured, so moving far outside the track still drives the handle, clamped to 0..260.
            window.MouseMove(OnTrack(900));
            Assert.Equal(260, Canvas.GetLeft(handle));
            window.MouseMove(OnTrack(-900));
            Assert.Equal(0, Canvas.GetLeft(handle));

            window.MouseUp(OnTrack(-900), MouseButton.Left);
            Assert.Equal("空闲（捕获已释放）", Text(page, "DragState"));
        }

        [AvaloniaFact]
        public void Tab_skips_the_button_that_is_not_a_tab_stop()
        {
            var page = new FocusPage();
            var window = UiHelpers.Show(page);
            page.ByName<Button>("BtnA").Focus();
            Assert.StartsWith("焦点在 A", Text(page, "FocusReadout"));

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.StartsWith("焦点在 B  via Tab", Text(page, "FocusReadout"));

            // C has IsTabStop=False, so the next stop is D.
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.StartsWith("焦点在 D  via Tab", Text(page, "FocusReadout"));
        }

        [AvaloniaFact]
        public void Focus_via_Tab_shows_the_focus_ring_but_focus_via_pointer_does_not()
        {
            var page = new FocusPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ButtonWithText("Focus(Tab) → B"));
            var viaTab = Text(page, "FocusReadout");
            window.Click(page.ButtonWithText("Focus(Pointer) → B"));
            var viaPointer = Text(page, "FocusReadout");

            Assert.Contains("via Tab", viaTab);
            Assert.Contains(":focus-visible", viaTab);
            Assert.Contains("via Pointer", viaPointer);
            Assert.DoesNotContain(":focus-visible", viaPointer);
        }

        [AvaloniaFact]
        public void Query_button_reports_the_element_that_has_focus()
        {
            var page = new FocusPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ButtonWithText("读取 FocusManager 当前焦点"));

            // The clicked button has no name, so the readout falls back to its type name.
            Assert.Equal("FocusManager.GetFocusedElement() = Button", Text(page, "QueryReadout"));
        }

        [AvaloniaFact]
        public void Gestures_page_counts_tap_double_tap_and_right_tap_separately_and_resets()
        {
            var page = new GesturesPage();
            var window = UiHelpers.Show(page);
            var pad = page.ByName<Border>("Pad");
            var centre = CentreOf(window, pad);
            const string holding = "\nHolding: Started=0 Completed=0 Canceled=0";

            window.Click(pad);
            Assert.Equal("Tapped=1  DoubleTapped=0  RightTapped=0" + holding, Text(page, "Counts"));

            // A second press-release at the same spot straight away is the double tap, and it replaces the second Tapped.
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Assert.Equal("Tapped=1  DoubleTapped=1  RightTapped=0" + holding, Text(page, "Counts"));

            window.MouseDown(centre, MouseButton.Right);
            window.MouseUp(centre, MouseButton.Right);
            Assert.Equal("Tapped=1  DoubleTapped=1  RightTapped=1" + holding, Text(page, "Counts"));

            window.Click(page.ButtonWithText("清零"));
            Assert.Equal("Tapped=0  DoubleTapped=0  RightTapped=0" + holding, Text(page, "Counts"));
        }

        [AvaloniaFact]
        public void Digits_box_rejects_letters_typed_one_character_at_a_time()
        {
            var page = new TextInputPage();
            var window = UiHelpers.Show(page);
            var box = page.ByName<TextBox>("DigitsBox");

            // One character per TextInput event: a whole "a1b2" is a single event and is rejected as a unit.
            foreach (var ch in "a1b2")
            {
                window.Type(box, ch.ToString());
            }

            Assert.Equal("12", box.Text);
        }

        [AvaloniaFact]
        public void Options_readout_shows_the_attached_text_input_options()
        {
            var page = new TextInputPage();
            UiHelpers.Show(page);

            var readout = Text(page, "OptionsReadout");

            Assert.Contains("ContentType=Email", readout);
            Assert.Contains("ReturnKeyType=Send", readout);
            Assert.Contains("ContentType=Digits", readout);
            Assert.Contains("IsSensitive=True", readout);
        }
    }
}
```

- [ ] **Step 5: 运行**

Run: `dotnet test tests/Avalonia.InputDemo.Tests`
Expected: `通过: 20，失败: 0`（2 + 4 + 14）。

Run: `dotnet build tests/Avalonia.InputDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 6: 破坏检查（临时改 `src/`，必须还原）**

目的：证明命令的可用性测试与指针夹取测试真的在测点上（Review Focus 第 2 条）。

**(a)** 用 Edit 在 `src/Avalonia.InputDemo/ViewModels/InteractivityViewModel.cs` 里删掉通知：

旧：

```csharp
        [NotifyCanExecuteChangedFor(nameof(IncrementCommand))]
```

新：把这一行整行删掉（保留上下两行）。

Run: `dotnet test tests/Avalonia.InputDemo.Tests --filter "FullyQualifiedName~Unlocking|FullyQualifiedName~Command_button"`
Expected: **失败**（`raised` 是 0；按钮解锁后仍然 `IsEffectivelyEnabled == false`）。

立刻还原：

```bash
git checkout -- src/Avalonia.InputDemo/ViewModels/InteractivityViewModel.cs
git status --short src
```

Expected: `git status --short src` 无输出。

**(b)** 用 Edit 在 `src/Avalonia.InputDemo/Views/Pages/PointerPage.axaml.cs` 里去掉夹取：

旧：

```csharp
            Canvas.SetLeft(Handle, Math.Clamp(x, 0, max));
```

新：

```csharp
            Canvas.SetLeft(Handle, x);
```

Run: `dotnet test tests/Avalonia.InputDemo.Tests --filter "FullyQualifiedName~Dragging_the_handle"`
Expected: **失败**（右端不再是 `260`）。构建可能多一条 `max` 未使用的警告，属预期。

立刻还原：

```bash
git checkout -- src/Avalonia.InputDemo/Views/Pages/PointerPage.axaml.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.InputDemo.Tests`，`通过: 20`。若改了之后测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 7: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.InputDemo.Tests
git status --short
git commit -m "test: add Headless tests for InputDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.InputDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 4: `Avalonia.GraphicsDemo.Tests`

**Files:**
- Create: `tests/Avalonia.GraphicsDemo.Tests/Avalonia.GraphicsDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/ButtonWithText/Click/CaptureFrame`、`CultureScope.Invariant()`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.GraphicsDemo.App`、`Views.MainWindow`、`Views.Pages.{EasingPage, ShapesPage, TransformsPage, ClipAndHitPage, CompositionPage, RenderOptionsPage, PageTransitionsPage}`、`Controls.Sketch`（`UseClip`、`UseTransform`、`UseOpacity`，各 `AffectsRender`）。`BrushesPage`、`EffectsPage`、`IconsPage`、`DrawingPage`、`AnimationsPage`、`TransitionsPage` 只靠 H1。
- Produces：测试项目 `Avalonia.GraphicsDemo.Tests`（13 个测试；Theory 的 3 行各算 1 个）。

本任务没有"先写失败的测试"的空间：被测代码都已存在，测试写出来就应该通过。验证"测试不是空转"靠最后的**破坏检查**。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.GraphicsDemo.Tests/Avalonia.GraphicsDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.GraphicsDemo\Avalonia.GraphicsDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.GraphicsDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.GraphicsDemo;
using Avalonia.GraphicsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.GraphicsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `InputDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.GraphicsDemo.Tests/Avalonia.GraphicsDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.GraphicsDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.GraphicsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.GraphicsDemo.Tests
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
                new[]
                {
                    "画刷与渐变", "变换", "形状与几何", "自定义绘制", "特效", "裁剪与命中", "图标",
                    "渲染选项", "关键帧动画", "控件过渡", "页面过渡", "缓动函数", "合成动画",
                },
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

- [ ] **Step 3: 自绘、变换、命中、缓动、合成的测试**

#### `tests/Avalonia.GraphicsDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.GraphicsDemo.Controls;
using Avalonia.GraphicsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System;
using Xunit;

namespace Avalonia.GraphicsDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Control page, string name) => page.ByName<TextBlock>(name).Text!;

        private static (Sketch Sketch, Window Window) ShowSketch()
        {
            var sketch = new Sketch();
            var window = new Window
            {
                Content = new Border { Background = Brushes.Black, Child = sketch },
                Width = 300,
                Height = 200,
            };
            window.Show();
            return (sketch, window);
        }

        // Sketch declares AffectsRender for its three switches. Without it the property changes and the picture does not,
        // with no exception and no log, so the only way to see it is to compare rendered frames.
        [AvaloniaTheory]
        [InlineData("UseClip")]
        [InlineData("UseTransform")]
        [InlineData("UseOpacity")]
        public void Each_sketch_switch_repaints_and_switching_it_off_restores_the_picture(string toggle)
        {
            var (sketch, window) = ShowSketch();
            var property = toggle switch
            {
                "UseClip" => Sketch.UseClipProperty,
                "UseTransform" => Sketch.UseTransformProperty,
                _ => Sketch.UseOpacityProperty,
            };
            var plain = window.CaptureFrame();

            sketch.SetValue(property, true);
            var scoped = window.CaptureFrame();
            sketch.SetValue(property, false);
            var restored = window.CaptureFrame();

            Assert.NotEqual(plain, scoped);
            Assert.Equal(plain, restored);
        }

        [AvaloniaFact]
        public void Easing_page_starts_on_CubicEaseInOut_and_samples_61_curve_points()
        {
            using var culture = CultureScope.Invariant();
            var page = new EasingPage();
            UiHelpers.Show(page);

            Assert.Equal("Ease(0.25)=0.062   Ease(0.5)=0.500   Ease(0.75)=0.938", Text(page, "Samples"));
            Assert.Equal(61, page.ByName<Polyline>("Curve").Points.Count);
        }

        [AvaloniaFact]
        public void Choosing_another_easing_resamples_the_curve()
        {
            using var culture = CultureScope.Invariant();
            var page = new EasingPage();
            UiHelpers.Show(page);
            var box = page.ByName<ComboBox>("EasingBox");

            // Index 0 is LinearEasing; index 6 is BackEaseInOut, which overshoots on both sides.
            box.SelectedIndex = 0;
            Assert.Equal("Ease(0.25)=0.250   Ease(0.5)=0.500   Ease(0.75)=0.750", Text(page, "Samples"));
            box.SelectedIndex = 6;
            Assert.Equal("Ease(0.25)=-0.188   Ease(0.5)=0.500   Ease(0.75)=1.188", Text(page, "Samples"));
        }

        [AvaloniaFact]
        public void Choosing_a_combine_mode_changes_the_combined_geometry()
        {
            var page = new ShapesPage();
            UiHelpers.Show(page);
            var combined = (CombinedGeometry)page.ByName<Path>("CombinedPath").Data!;
            Assert.Equal(GeometryCombineMode.Xor, combined.GeometryCombineMode);
            var xorBounds = combined.Bounds;

            page.ByName<ComboBox>("ModeBox").SelectedItem = GeometryCombineMode.Intersect;

            Assert.Equal(GeometryCombineMode.Intersect, combined.GeometryCombineMode);
            Assert.NotEqual(xorBounds, combined.Bounds);
        }

        [AvaloniaFact]
        public void Layout_transform_pushes_the_neighbour_away_but_render_transform_does_not()
        {
            var page = new TransformsPage();
            var window = UiHelpers.Show(page);
            var renderBefore = page.ByName<Border>("RenderRight").Bounds.X;
            var layoutBefore = page.ByName<Border>("LayoutRight").Bounds.X;

            page.ByName<Slider>("AngleSlider").Value = 90;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            // RenderTransform is applied after layout, so its neighbour stays put. Rotated by 90 degrees, the layout-
            // transformed 110-wide tile occupies 44 instead, which pulls its neighbour 66 to the left.
            Assert.Equal(renderBefore, page.ByName<Border>("RenderRight").Bounds.X);
            Assert.Equal(layoutBefore - 66, page.ByName<Border>("LayoutRight").Bounds.X, 0.5);
        }

        [AvaloniaFact]
        public void Only_the_transparent_probe_receives_taps_among_the_three_hit_test_cases()
        {
            var page = new ClipAndHitPage();
            var window = UiHelpers.Show(page);
            Assert.Equal("没有 Background=0   Transparent=0   IsHitTestVisible=False=0", Text(page, "HitReadout"));

            window.Click(page.ByName<Border>("NoBrush"));
            window.Click(page.ByName<Border>("Clear"));
            window.Click(page.ByName<Border>("Invisible"));

            // No Background means no hit; Transparent is a real brush and does hit; IsHitTestVisible=False never does.
            Assert.Equal("没有 Background=0   Transparent=1   IsHitTestVisible=False=0", Text(page, "HitReadout"));
        }

        [AvaloniaFact]
        public void Composition_sliders_write_through_to_the_element_visual()
        {
            var page = new CompositionPage();
            UiHelpers.Show(page);
            // Loaded is dispatched, and only then does the Target have a composition visual.
            Dispatcher.UIThread.RunJobs();
            Assert.StartsWith("已取得", Text(page, "Status"));
            var visual = ElementComposition.GetElementVisual(page.ByName<Border>("Target"))!;

            page.ByName<Slider>("OpacitySlider").Value = 0.5;
            page.ByName<Slider>("ScaleSlider").Value = 1.5;
            page.ByName<Slider>("AngleSlider").Value = 90;

            Assert.Equal(0.5f, visual.Opacity);
            Assert.Equal(1.5, visual.Scale.X);
            Assert.Equal((float)(Math.PI / 2), visual.RotationAngle, 0.001f);
        }

        [AvaloniaFact]
        public void Render_option_boxes_write_the_attached_render_options()
        {
            var page = new RenderOptionsPage();
            UiHelpers.Show(page);
            var pixels = page.ByName<Image>("Pixels");
            var top = page.ByName<Image>("BlendTop");
            Assert.Equal(BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode(pixels));

            page.ByName<ComboBox>("InterpolationBox").SelectedItem = BitmapInterpolationMode.HighQuality;
            page.ByName<ComboBox>("BlendBox").SelectedItem = BitmapBlendingMode.Plus;

            Assert.Equal(BitmapInterpolationMode.HighQuality, RenderOptions.GetBitmapInterpolationMode(pixels));
            Assert.Equal(BitmapBlendingMode.Plus, RenderOptions.GetBitmapBlendingMode(top));
        }

        [AvaloniaFact]
        public void Next_replaces_the_pager_content_and_the_kind_box_swaps_the_transition()
        {
            var page = new PageTransitionsPage();
            var window = UiHelpers.Show(page);
            var pager = page.ByName<TransitioningContentControl>("Pager");
            string PageText() => ((TextBlock)((Border)pager.Content!).Child!).Text!;
            Assert.Equal("第 1 页", PageText());
            Assert.IsType<PageSlide>(pager.PageTransition);

            window.Click(page.ButtonWithText("下一页"));

            Assert.Equal("第 2 页", PageText());
            Assert.Equal("第 2 页", Text(page, "Readout"));

            // Index 2 is the cross-fade.
            page.ByName<ComboBox>("KindBox").SelectedIndex = 2;
            Assert.IsType<CrossFade>(pager.PageTransition);
        }
    }
}
```

- [ ] **Step 4: 运行**

Run: `dotnet test tests/Avalonia.GraphicsDemo.Tests`
Expected: `通过: 13，失败: 0`（2 + 3 + 8）。

Run: `dotnet build tests/Avalonia.GraphicsDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

目的：证明 `Sketch` 的像素测试真能看见 `AffectsRender` 缺失（Review Focus 第 1 条），命中测试真的在测 `Transparent`。

**(a)** 用 Edit 在 `src/Avalonia.GraphicsDemo/Controls/Sketch.cs` 里注释掉 `AffectsRender`：

旧：

```csharp
            AffectsRender<Sketch>(UseClipProperty, UseTransformProperty, UseOpacityProperty);
```

新：

```csharp
            // AffectsRender<Sketch>(UseClipProperty, UseTransformProperty, UseOpacityProperty);
```

Run: `dotnet test tests/Avalonia.GraphicsDemo.Tests --filter "FullyQualifiedName~Each_sketch_switch"`
Expected: **失败 3 个**（`Assert.NotEqual` 失败：开关打开后画面没变）。

立刻还原：

```bash
git checkout -- src/Avalonia.GraphicsDemo/Controls/Sketch.cs
git status --short src
```

Expected: `git status --short src` 无输出。

**(b)** 用 Edit 在 `src/Avalonia.GraphicsDemo/Views/Pages/ClipAndHitPage.axaml` 里去掉 `Clear` 方块的透明背景：

旧：

```xml
                    <Border Name="Clear" Classes="probe" Background="Transparent" Tapped="OnTapped" />
```

新：

```xml
                    <Border Name="Clear" Classes="probe" Tapped="OnTapped" />
```

Run: `dotnet test tests/Avalonia.GraphicsDemo.Tests --filter "FullyQualifiedName~Only_the_transparent_probe"`
Expected: **失败**（`Transparent=1` 变成 `Transparent=0`）。

立刻还原：

```bash
git checkout -- src/Avalonia.GraphicsDemo/Views/Pages/ClipAndHitPage.axaml
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.GraphicsDemo.Tests`，`通过: 13`。若改了之后测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.GraphicsDemo.Tests
git status --short
git commit -m "test: add Headless tests for GraphicsDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.GraphicsDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 5: `Avalonia.CustomControlsDemo.Tests`

**Files:**
- Create: `tests/Avalonia.CustomControlsDemo.Tests/Avalonia.CustomControlsDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/ButtonWithText/Click/CaptureFrame`、`CultureScope.Invariant()`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.CustomControlsDemo.App`、`Views.MainWindow`、`Views.Pages.{UserControlPage, TemplatedControlPage, CustomDrawnPage, ControlTreesPage, CustomPanelPage, CustomFlyoutPage}`、`Controls.{Meter, RingGauge, RadialPanel, SwatchFlyout, OnlyVisualHost, VisualAndLogicalHost, LabeledSlider}`。`PropertiesAndEventsPage` 只靠 H1。
- Produces：测试项目 `Avalonia.CustomControlsDemo.Tests`（18 个测试）。

本任务没有"先写失败的测试"的空间：被测代码都已存在，测试写出来就应该通过。验证"测试不是空转"靠最后的**破坏检查**。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.CustomControlsDemo.Tests/Avalonia.CustomControlsDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.CustomControlsDemo\Avalonia.CustomControlsDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.CustomControlsDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.CustomControlsDemo;
using Avalonia.CustomControlsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.CustomControlsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `GraphicsDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.CustomControlsDemo.Tests/Avalonia.CustomControlsDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.CustomControlsDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.CustomControlsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.CustomControlsDemo.Tests
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
                new[] { "用户控件", "模板化控件", "自绘控件", "属性与事件", "控件树", "自定义面板", "自定义 Flyout" },
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

- [ ] **Step 3: 自定义控件、面板、弹出层、控件树的测试**

#### `tests/Avalonia.CustomControlsDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.CustomControlsDemo.Controls;
using Avalonia.CustomControlsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.CustomControlsDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Control page, string name) => page.ByName<TextBlock>(name).Text!;

        private static Border FillOf(Meter meter)
            => meter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Fill");

        private static double ScaleOf(Meter meter) => ((ScaleTransform)FillOf(meter).RenderTransform!).ScaleX;

        private static (RadialPanel Panel, Window Window) ShowRadial(int children, double radius)
        {
            var panel = new RadialPanel { Radius = radius };
            for (var i = 0; i < children; i++)
            {
                panel.Children.Add(new Border { Width = 20, Height = 20 });
            }

            var window = new Window { Content = panel, Width = 400, Height = 400 };
            window.Show();
            return (panel, window);
        }

        // The swatches live on the window's overlay layer, and only while the flyout is open.
        private static List<Button> Swatches(Window window)
            => window.GetVisualDescendants().OfType<Button>().Where(b => b.Width == 32).ToList();

        [AvaloniaFact]
        public void Meter_follows_the_slider_and_its_template_part_scales_with_it()
        {
            using var culture = CultureScope.Invariant();
            var page = new TemplatedControlPage();
            UiHelpers.Show(page);
            var meter = page.ByName<Meter>("TheMeter");
            var slider = page.ByName<Slider>("ValueSlider");
            Assert.Equal(40, meter.Value);
            // The invariant culture writes P0 with a space ("40 %"); en-US and zh-CN would give "40%".
            Assert.Equal("40 %", meter.Display);
            Assert.Equal(0.4, ScaleOf(meter), 3);
            Assert.DoesNotContain(":full", meter.Classes);

            slider.Value = 100;
            Assert.Equal("100 %", meter.Display);
            Assert.Equal(1, ScaleOf(meter), 3);
            Assert.Contains(":full", meter.Classes);

            // Back down: the :full pseudo-class has to clear again.
            slider.Value = 40;
            Assert.Equal("40 %", meter.Display);
            Assert.DoesNotContain(":full", meter.Classes);
        }

        [AvaloniaFact]
        public void Meter_value_is_clamped_to_zero_and_the_maximum_and_follows_maximum_changes()
        {
            using var culture = CultureScope.Invariant();
            var page = new TemplatedControlPage();
            UiHelpers.Show(page);
            var meter = page.ByName<Meter>("TheMeter");

            meter.Value = 500;
            Assert.Equal(100, meter.Value);
            meter.Value = -5;
            Assert.Equal(0, meter.Value);

            meter.Maximum = 50;
            meter.Value = 25;
            Assert.Equal("50 %", meter.Display);
            Assert.DoesNotContain(":full", meter.Classes);
            meter.Value = 50;
            Assert.Contains(":full", meter.Classes);

            // A maximum of zero must not divide by zero.
            meter.Maximum = 0;
            Assert.Equal("0 %", meter.Display);
        }

        [AvaloniaFact]
        public void Swapping_to_the_text_theme_drops_the_fill_part_and_the_display_keeps_working()
        {
            using var culture = CultureScope.Invariant();
            var page = new TemplatedControlPage();
            var window = UiHelpers.Show(page);
            var meter = page.ByName<Meter>("TheMeter");
            Assert.Null(meter.Theme);
            Assert.NotNull(FillOf(meter));

            window.Click(page.ByName<Button>("SwapButton"));
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(meter.Theme);
            Assert.DoesNotContain(meter.GetVisualDescendants().OfType<Border>(), b => b.Name == "PART_Fill");
            // The part is gone, so the guard in Refresh has to cope with it.
            meter.Value = 70;
            Assert.Equal("70 %", meter.Display);
        }

        [AvaloniaFact]
        public void Ring_gauge_measures_itself_and_follows_the_slider()
        {
            var page = new CustomDrawnPage();
            UiHelpers.Show(page);
            var gauge = page.ByName<RingGauge>("Gauge");
            Assert.Equal(new Size(120, 120), gauge.Bounds.Size);
            Assert.Equal(35, gauge.Value);

            page.ByName<Slider>("GaugeSlider").Value = 90;

            Assert.Equal(90, gauge.Value);
        }

        // RingGauge declares AffectsRender for Value; without it the property changes and the ring does not. The gauge
        // stands alone here: on the demo page a readout next to it changes too, which would repaint the frame anyway.
        [AvaloniaFact]
        public void Ring_gauge_repaints_when_the_value_changes()
        {
            var gauge = new RingGauge { Value = 35 };
            var window = new Window
            {
                Content = new Border { Background = Brushes.Black, Child = gauge },
                Width = 200,
                Height = 200,
            };
            window.Show();
            var before = window.CaptureFrame();

            gauge.Value = 90;

            Assert.NotEqual(before, window.CaptureFrame());
        }

        [AvaloniaFact]
        public void Radial_panel_places_children_evenly_clockwise_from_twelve_o_clock()
        {
            var (panel, _) = ShowRadial(children: 4, radius: 100);

            // The 400x400 panel is centred on (200, 200); each 20x20 child sits on a circle of radius 100 around it.
            Assert.Equal(
                new[] { new Rect(190, 90, 20, 20), new Rect(290, 190, 20, 20), new Rect(190, 290, 20, 20), new Rect(90, 190, 20, 20) },
                panel.Children.Select(c => c.Bounds));
        }

        [AvaloniaFact]
        public void Radial_panel_start_angle_rotates_every_child_to_the_next_position()
        {
            var (panel, window) = ShowRadial(children: 4, radius: 100);

            panel.StartAngle = 90;
            window.UpdateLayout();

            Assert.Equal(
                new[] { new Rect(290, 190, 20, 20), new Rect(190, 290, 20, 20), new Rect(90, 190, 20, 20), new Rect(190, 90, 20, 20) },
                panel.Children.Select(c => c.Bounds));
        }

        [AvaloniaFact]
        public void Radial_panel_size_comes_from_the_radius_and_the_largest_child()
        {
            var (panel, window) = ShowRadial(children: 4, radius: 100);
            Assert.Equal(new Size(220, 220), panel.DesiredSize);

            panel.Radius = 40;
            window.UpdateLayout();

            Assert.Equal(new Size(100, 100), panel.DesiredSize);
        }

        [AvaloniaFact]
        public void Radial_panel_without_children_measures_to_the_bare_circle()
        {
            var (panel, _) = ShowRadial(children: 0, radius: 100);

            Assert.Equal(new Size(200, 200), panel.DesiredSize);
        }

        [AvaloniaFact]
        public void Add_and_remove_buttons_change_the_dot_count_and_it_never_goes_below_zero()
        {
            var page = new CustomPanelPage();
            var window = UiHelpers.Show(page);
            // The control row is wider than the 600px test window and is not inside a ScrollViewer, so a click on
            // the right-hand button would miss silently.
            window.Width = 1000;
            window.UpdateLayout();
            var radial = page.ByName<RadialPanel>("Radial");
            Assert.Equal(6, radial.Children.Count);

            window.Click(page.ByName<Button>("AddButton"));
            Assert.Equal(7, radial.Children.Count);

            window.Click(page.ByName<Button>("RemoveButton"));
            window.Click(page.ByName<Button>("RemoveButton"));
            Assert.Equal(5, radial.Children.Count);

            for (var i = 0; i < 10; i++)
            {
                window.Click(page.ByName<Button>("RemoveButton"));
            }

            Assert.Empty(radial.Children);
            window.Click(page.ByName<Button>("AddButton"));
            Assert.Single(radial.Children);
        }

        [AvaloniaFact]
        public void Radius_and_angle_sliders_drive_the_panel()
        {
            var page = new CustomPanelPage();
            UiHelpers.Show(page);
            var radial = page.ByName<RadialPanel>("Radial");
            Assert.Equal(80, radial.Radius);
            Assert.Equal(0, radial.StartAngle);

            page.ByName<Slider>("RadiusSlider").Value = 40;
            page.ByName<Slider>("AngleSlider").Value = 90;

            Assert.Equal(40, radial.Radius);
            Assert.Equal(90, radial.StartAngle);
        }

        [AvaloniaFact]
        public void Picking_a_swatch_fills_the_chosen_box_and_closes_the_flyout()
        {
            var page = new CustomFlyoutPage();
            var window = UiHelpers.Show(page);
            var pick = page.ByName<Button>("PickButton");
            var flyout = (SwatchFlyout)pick.Flyout!;
            Assert.Empty(Swatches(window));
            Assert.Equal("尚未选择", Text(page, "ChosenText"));

            window.Click(pick);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);
            var swatches = Swatches(window);
            Assert.Equal(6, swatches.Count);

            window.Click(swatches[2]);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("#ffe8d54a", Text(page, "ChosenText"));
            Assert.Equal(Color.Parse("#E8D54A"), ((SolidColorBrush)page.ByName<Border>("Chosen").Background!).Color);
            Assert.False(flyout.IsOpen);
        }

        [AvaloniaFact]
        public void Clicking_elsewhere_dismisses_the_open_flyout_without_choosing_a_colour()
        {
            var page = new CustomFlyoutPage();
            var window = UiHelpers.Show(page);
            var pick = page.ByName<Button>("PickButton");
            var flyout = (SwatchFlyout)pick.Flyout!;
            window.Click(pick);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);

            window.Click(page.ByName<TextBlock>("ChosenText"));
            Dispatcher.UIThread.RunJobs();

            Assert.False(flyout.IsOpen);
            Assert.Equal("尚未选择", Text(page, "ChosenText"));
        }

        [AvaloniaFact]
        public void A_visual_only_host_hides_the_data_context_from_its_child_but_a_dual_tree_host_does_not()
        {
            var visualOnly = new OnlyVisualHost();
            var both = new VisualAndLogicalHost();
            var window = new Window
            {
                Content = new StackPanel { DataContext = "ctx", Children = { visualOnly, both } },
                Width = 300,
                Height = 200,
            };
            window.Show();

            Assert.Null(visualOnly.Child.DataContext);
            Assert.Equal("ctx", both.Child.DataContext);
            Assert.Equal("ctx", both.Child.Text);
        }

        [AvaloniaFact]
        public void Control_trees_page_reports_both_data_contexts_once_it_has_loaded()
        {
            var page = new ControlTreesPage();
            UiHelpers.Show(page);
            // OnLoaded runs from the dispatcher.
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                "视觉树宿主的子元素 DataContext = null；双树宿主的子元素 DataContext = 来自父级的 DataContext",
                Text(page, "Readout"));
        }

        [AvaloniaFact]
        public void Labeled_sliders_stay_in_sync_in_both_directions()
        {
            var page = new UserControlPage();
            UiHelpers.Show(page);
            var size = page.ByName<LabeledSlider>("Size");
            var fade = page.ByName<LabeledSlider>("Fade");
            Assert.Equal(20, size.Value);
            Assert.Equal(20, fade.Value);

            size.Value = 33;
            Assert.Equal(33, fade.Value);

            // The inner Slider writes back through the TwoWay Value property, and the binding carries it on to Size.
            fade.GetVisualDescendants().OfType<Slider>().First().Value = 12;
            Assert.Equal(12, fade.Value);
            Assert.Equal(12, size.Value);
        }
    }
}
```

- [ ] **Step 4: 运行**

Run: `dotnet test tests/Avalonia.CustomControlsDemo.Tests`
Expected: `通过: 18，失败: 0`（2 + 16）。

Run: `dotnet build tests/Avalonia.CustomControlsDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

目的：证明 `Meter` 的夹取、`RadialPanel` 的几何、`RingGauge` 的重绘三组测试真的在测点上。

**(a)** 用 Edit 在 `src/Avalonia.CustomControlsDemo/Controls/Meter.cs` 里去掉夹取：

旧：

```csharp
                coerce: (o, v) => Math.Clamp(v, 0, ((Meter)o).Maximum));
```

新：

```csharp
                coerce: (o, v) => v);
```

Run: `dotnet test tests/Avalonia.CustomControlsDemo.Tests --filter "FullyQualifiedName~Meter_value_is_clamped"`
Expected: **失败**（`Expected: 100  Actual: 500`）。

立刻还原：

```bash
git checkout -- src/Avalonia.CustomControlsDemo/Controls/Meter.cs
git status --short src
```

Expected: `git status --short src` 无输出。

**(b)** 用 Edit 在 `src/Avalonia.CustomControlsDemo/Controls/RadialPanel.cs` 里把 Y 方向的符号改反：

旧：

```csharp
                var y = centre.Y - Radius * Math.Cos(angle) - child.DesiredSize.Height / 2;
```

新：

```csharp
                var y = centre.Y + Radius * Math.Cos(angle) - child.DesiredSize.Height / 2;
```

Run: `dotnet test tests/Avalonia.CustomControlsDemo.Tests --filter "FullyQualifiedName~Radial_panel"`
Expected: **失败**（第一个子元素跑到 `(190, 290)`，即六点钟方向）。

立刻还原：

```bash
git checkout -- src/Avalonia.CustomControlsDemo/Controls/RadialPanel.cs
git status --short src
```

Expected: `git status --short src` 无输出。

**(c)** 用 Edit 在 `src/Avalonia.CustomControlsDemo/Controls/RingGauge.cs` 里注释掉 `AffectsRender`：

旧：

```csharp
            AffectsRender<RingGauge>(ValueProperty);
```

新：

```csharp
            // AffectsRender<RingGauge>(ValueProperty);
```

Run: `dotnet test tests/Avalonia.CustomControlsDemo.Tests --filter "FullyQualifiedName~Ring_gauge"`
Expected: **失败 1 个**（`Ring_gauge_repaints_when_the_value_changes`：`Assert.NotEqual` 失败，值变了画面没变；`..._follows_the_slider` 仍然通过）。

立刻还原：

```bash
git checkout -- src/Avalonia.CustomControlsDemo/Controls/RingGauge.cs
git status --short src
```

Expected: `git status --short src` 无输出。再跑一次 `dotnet test tests/Avalonia.CustomControlsDemo.Tests`，`通过: 18`。若改了之后测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 6: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.CustomControlsDemo.Tests
git status --short
git commit -m "test: add Headless tests for CustomControlsDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.CustomControlsDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 6: 文档同步、回填 spec、整体验证

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

**Interfaces:**
- Consumes: Task 1–5 交付的共享库改动与 4 个测试项目（本批共 61 + 2 个新测试）、Task 1 Step 1 记下的构建警告基线。
- Produces: 与现状一致的 `README.md`；回填后的 spec（批 4 起的计划以它为准）。

- [ ] **Step 1: `README.md` 的测试项目行补上本批四个应用**

用 Edit 替换这一行里的括号部分：

旧：

```markdown
Styling、DataBinding、DataTemplates、PropertySystem）
```

新：

```markdown
Styling、DataBinding、DataTemplates、PropertySystem、Events、Input、Graphics、CustomControls）
```

- [ ] **Step 2: spec 里 `UiHelpers` 一行与 Task 1 的改动一致**

用 Edit 替换：

旧：

```markdown
`Type` 是在光标处插入而非替换 |
```

新：

```markdown
`Type` 是在光标处插入而非替换；批 3 起新增 `ButtonWithText`（批 2 评审时上提）与 `CaptureFrame`（截取当前渲染帧的 PNG 字节，用来断言"自绘控件真的重绘了"），并新增 `CultureScope.Invariant()`（固定 `CultureInfo.CurrentCulture` 并在 `Dispose` 时还原） |
```

- [ ] **Step 3: 回填「批 3 实测结论」**

把 spec 里这一行：

```markdown
## 批 3 实测结论（执行后回填）
```

替换为下面的整段。「执行时的实测」一栏要按本次执行的真实输出填写：三个数字（构建警告数、全解决方案测试数）填实际值，破坏检查一栏按各任务 Step 的真实输出填写；若某项与下面写的预期不符，写实际情况，不要照抄。

```markdown
## 批 3 实测结论（计划 03 执行后）

| 项目 | 测试数 | H1 页数 | 说明 |
|---|---|---|---|
| `Avalonia.EventsDemo.Tests` | 10 | 5 | 生命周期事件的先后顺序（含需要 `RunJobs` 的 `SizeChanged`/`Loaded`）、隧道与冒泡的六步顺序、`Handled` 打开/关闭两种结果（含 `handledEventsToo`）、自定义路由事件的冒泡与 `Source`、输入事件日志 |
| `Avalonia.InputDemo.Tests` | 20 | 8 | H2：`InteractivityViewModel` 的命令可用性；H3：Click/Tapped/Command/HotKey 的区别、键盘读数与 `KeyBinding`、指针捕获与夹取、焦点环（`:focus-visible`）与 Tab 顺序、手势计数、逐字符输入过滤 |
| `Avalonia.GraphicsDemo.Tests` | 13 | 13 | `Sketch` 三个开关的像素比较（Theory）、缓动采样（区域设置固定）、组合几何、布局变换与渲染变换、命中测试、合成视觉属性、`RenderOptions`、页面过渡的切换 |
| `Avalonia.CustomControlsDemo.Tests` | 18 | 7 | `Meter`（夹取、`:full`、模板部件、换主题）、`RingGauge` 重绘、`RadialPanel` 几何与尺寸、弹出层真实点击选色与轻触关闭、控件树的 `DataContext`、`LabeledSlider` 双向同步 |
| `Avalonia.TestingDemo.Tests`（新增） | 2 | — | `UiHelpersTests.CaptureFrame`（稳定且随场景变化）、`CultureScopeTests`（固定并还原） |

执行时的实测：构建警告数 <填实际值>（批开始前 <基线>）；`dotnet test hello-avalonia.slnx` 共 <填实际值> 个测试通过（TestingDemo 34 + 批 1 的 37 + 批 2 的 63 + 本批 61）；破坏检查：Events 把 `e.Handled = true` 改成 `false` → <填个数> 个失败、把路由策略改成 `Direct` → <填个数> 个；Input 删 `NotifyCanExecuteChangedFor` → <填个数> 个、去掉 `Math.Clamp` → <填个数> 个；Graphics 注释掉 `Sketch` 的 `AffectsRender` → <填个数> 个、去掉 `Clear` 方块的 `Background="Transparent"` → <填个数> 个；CustomControls 去掉 `Meter` 的夹取 → <填个数> 个、`RadialPanel` 的 Y 符号取反 → <填个数> 个、注释掉 `RingGauge` 的 `AffectsRender` → <填个数> 个，均已还原。

**对后续批的决定与发现**（计划阶段在探针里实测，执行阶段原样复现）：

- **自绘控件要比像素，不能只比属性，而且要单独摆放**：`UiHelpers.CaptureFrame` 依赖 `HeadlessApp` 的 Skia 配置（`UseHeadlessDrawing=false`）；同一场景连截两次字节相同，改一个画笔字节就变，所以"属性变了画面没变"的漏标 `AffectsRender` 能被抓到。但**在演示页里测不出来**：计划第一版把 `RingGauge` 放在 `CustomDrawnPage` 里拖滑块，注释掉 `AffectsRender` 后测试仍通过——页面上旁边的读数同时变了，整帧照样不同。自绘控件的重绘测试必须像 `Sketch` 那样把控件单独放进窗口，只改它自己的属性。批 4–6 里凡是自绘或带 `Render` 重写的控件都照此处理，并且用"注释掉 `AffectsRender`"的破坏检查证明测试真能看见。
- **区域设置会改变格式化文字，而且固定成不变区域设置后格式也不是你以为的那样**：`P0`、`F3` 在 sv-SE、fr-FR 下的符号和空格不同；涉及格式化数字的断言一律包在 `using var culture = CultureScope.Invariant();` 里，`Dispose` 会还原，不会串到别的测试。注意不变区域设置把 `P0` 写成 `"40 %"`（带空格），而 en-US、zh-CN 是 `"40%"`，所以断言写 `"40 %"`；不要凭印象写 `"40%"`。
- **连点两下的手势计数**：第二下算 `DoubleTapped`，**不会**再给 `Tapped` 加一；连点三下 `Tapped` 才是 2。
- **带 `Command` 的按钮在 `CanExecute == false` 时 `IsEnabled` 仍是 `true`**：断言"灰了"要用 `IsEffectivelyEnabled`，同时点击不执行命令。
- **弹出层的内容在 Headless 里画在窗口的叠加层上**：打开之前视觉树里没有，打开之后可以用 `window.GetVisualDescendants()` 找到，真实点击能选色、点别处能轻触关闭，不需要反射去取 `Popup`。断言要先写"打开前 0 个、打开后恰好 N 个"，防止"找不到"被 `Any` 之类的写法吞掉。
- **点击落在窗口之外会静默落空**：`Click` 的 `BringIntoView` 只在 `ScrollViewer` 里有用；控件所在行比 600 宽的测试窗口更宽、又不在滚动视口里时，要先把 `window.Width` 放宽再点（`CustomPanelPage`）。
- **`Window.KeyPress` 能驱动 `KeyBinding`、`Button.HotKey`、`MenuItem.HotKey`、Tab 导航与页面的 `KeyDown` 处理器**；只写 `InputGesture`（不写 `HotKey`）的菜单项不会被触发，这正是 `InteractivityPage` 在演示的差别。
- **拒绝输入的 `TextInput` 隧道处理器按"事件"过滤**：一次 `Type(box, "a1b2")` 是**一个** `TextInput` 事件，含字母就整串被拒；要模拟用户逐字敲，需逐字符调用 `Type`。
- **事件顺序的断言**：`SizeChanged`、`Loaded` 经 dispatcher 派发，要 `RunJobs()` 之后才出现在日志里；`AttachedToLogicalTree`、`AttachedToVisualTree`、`Initialized` 是同步的。
- **只靠 H1 的页面**：Input 的 `DragDropPage`（`DoDragDropAsync` 是真实的操作系统拖放）、`RoutedEventsPage`（静态说明页）；Graphics 的 `BrushesPage`、`EffectsPage`、`IconsPage`、`DrawingPage`（除 `Sketch`）、`AnimationsPage`、`TransitionsPage`；CustomControls 的 `PropertiesAndEventsPage`（静态说明页）。动画与过渡的中间态按 spec 不断言。
```

- [ ] **Step 4: 整体验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)|个错误|Error\(s\)"`
Expected: 0 错误；警告数不高于 Task 1 Step 1 记下的基线。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 195 个通过（TestingDemo 34 + 批 1 的 37 + 批 2 的 63 + 本批 61）。

Run: `git status --short`
Expected: 只有 Step 1–3 改动的 `README.md` 与 spec 文件，没有 `C:\Temp\probe-*` 的文件；`git status --short src` 无输出。

- [ ] **Step 5: Commit**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git status --short
git commit -m "docs: record batch 3 results and list the new test projects"
```

`git status --short` 里只应出现上面两个文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

## Self-Review

**1. Spec 覆盖（批 3）：**

| spec 要求 | 对应 |
|---|---|
| EventsDemo、InputDemo、GraphicsDemo、CustomControlsDemo 各一个 `Avalonia.<X>Demo.Tests` 项目 | Task 2–5 |
| H1：走完全部标签页、断言页头与无异常 | 每个项目的 `SmokeTests.Main_window_walks_every_tab_without_errors` |
| H1：接入 `LogCollector` | 每个项目的 `Walking_every_tab_logs_no_warnings`（四个应用实测均 0 条） |
| H2：ViewModel / 控件逻辑 | Input 的 `ViewModelTests`（4）；CustomControls 的 `Meter`、`RadialPanel`、`LabeledSlider` 直接测试 |
| H3：关键页面交互 | 每个项目的 `InteractionTests` |
| 不对动画与过渡的中间态做断言 | Global Constraints 末条；`AnimationsPage`、`TransitionsPage` 只靠 H1 |
| 共享辅助放 `Avalonia.Tests.Common`，不在测试项目里复制 | Task 1（`CaptureFrame`、`CultureScope`）；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()` |
| 回填 spec「批 3 实测结论」、更新 README | Task 6 |

没有发现缺口。

**2. 占位符扫描：** 计划里没有 TBD/TODO/"类似 Task N"。Task 6 Step 3 里 `<填实际值>`、`<填个数>` 是写给执行者的明确填空（执行后按真实输出填，批 2 同样做法），不是未完成的设计。

**3. 类型一致性：**
- `UiHelpers.CaptureFrame(this Window) : byte[]`（Task 1 定义）→ Task 4 `Each_sketch_switch_repaints...`、Task 5 `Ring_gauge_repaints_when_the_value_changes` 同名同签名使用。
- `CultureScope.Invariant() : CultureScope`（Task 1 定义）→ Task 4 两处缓动测试、Task 5 三处 `Meter` 测试按 `using var culture = ...` 使用。
- `UiHelpers.Show/ByName/ButtonWithText/Click/Type` 沿用批 0–2 的签名，Task 2–5 未引入新的重载。
- 测试数：Task 1 +2、Task 2 10、Task 3 20（2+4+14）、Task 4 13（2+3+8，Theory 三行各算一个）、Task 5 18（2+16）；累计 132→134→144→164→177→195，与各 Step 的 `Expected` 以及 Task 6 Step 4 一致。
- slnx 插入顺序：DataBinding 之后依次是 Events、Input、Graphics、CustomControls，每个任务的 Edit 锚点都是前一个任务刚加的那行。

**4. Review Focus 覆盖：**

| Review Focus | 测试 |
|---|---|
| 1. 自绘控件漏 `AffectsRender` | Task 4 `Each_sketch_switch_repaints...`（破坏检查 (a) 期望 3 个失败）；Task 5 `Ring_gauge_repaints_when_the_value_changes`（单独摆放，破坏检查 (c)） |
| 2. Command 按钮的 `IsEnabled` 假象 | Task 3 `Command_button_ignores_clicks_while_locked...`（`IsEffectivelyEnabled`，破坏检查 (a)） |
| 3. 窗口外的点击静默落空 | Task 5 `Add_and_remove_buttons_change_the_dot_count...`（先放宽窗口，再断言计数真的减少） |
| 4. 区域设置相关文字 | Task 4 缓动两个测试、Task 5 `Meter` 三个测试都包在 `CultureScope.Invariant()` 里；Task 1 有 `CultureScope` 自身的回归测试 |
| 5. 弹出层内容只有打开后才存在 | Task 5 `Picking_a_swatch...`（打开前 0 个、打开后恰好 6 个、真实点击第三个）、`Clicking_elsewhere_dismisses...` |






