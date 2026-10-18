# Avalonia 项目测试规范

> 适用范围：本仓库（`hello-avalonia`）以及同类型的 Avalonia MVVM 项目。本文只写 **Avalonia 专属**的规则：Headless 与 Appium 的取舍、Headless 配置与输入、视觉回归、本仓库测试项目的布局。
>
> 与 UI 框架无关的部分（分层模型、ViewModel / 服务层、HTTP mock、SQLite、命名、覆盖率、CI）见 [`2026-10-18-testing-common-standards.md`](2026-10-18-testing-common-standards.md)。WPF 项目见 [`2026-10-18-wpf-testing-standards.md`](2026-10-18-wpf-testing-standards.md)。
>
> 证据来源：Avalonia 官方测试文档（一手）。正文方括号是本文自己的出处编号，见文末「证据与置信度」。
>
> 性能基准（BenchmarkDotNet）不在本文范围，见 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md)。

---

## 0. 一页结论

| 问题 | 结论 |
| --- | --- |
| UI 测试默认用哪个？ | **Headless**。跑完整控件树 / 布局 / 样式 / 绑定，无窗口、无显示器，CI 随处可跑 [2] |
| UI 测试特性用哪个？ | 一律 `[AvaloniaFact]` / `[AvaloniaTheory]`；普通 `[Fact]` 不会派发到 UI 线程 [3] |
| 模拟点击怎么写？ | `window.Click(control)`（`UiHelpers`，内部走 `MouseDown` / `MouseUp`）；**禁止** `RaiseEvent(Button.ClickEvent)`，绑定命令不会执行，测试会假绿 [2] |
| Appium 什么时候上？ | 只测 **真实窗口、原生菜单/焦点、平台特性、无障碍**，以及每个应用 1–3 条关键用户旅程 [4] |
| 视觉回归怎么做？ | headless + Skia 截帧，采样像素断言颜色；只给自定义控件 / 主题做 |
| 键入与按键怎么写（Appium）？ | 键入 `AppSession.TypeInto`，按键 `KeyChord.Press`；不用 WinAppDriver 的 `SendKeys` |

---

## 1. 5 分钟上手

以 `LayoutDemo` 为例，代码取自 `tests/Avalonia.LayoutDemo.Tests/` 的现有文件（③④ 做了删减）。

**① 测试项目**：`tests/Avalonia.<X>Demo.Tests/Avalonia.<X>Demo.Tests.csproj`，登记到 `hello-avalonia.slnx` 的 tests 文件夹。包版本已在 `Directory.Packages.props`，这里不写 `Version`。

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

**② `TestAppBuilder.cs`**：照抄，只改命名空间。

```csharp
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
}
```

**③ 第一个 L0 测试**（`ViewModelTests.cs`，普通 `[Fact]`，不启动 Avalonia）：

```csharp
[Fact]
public void Responsive_starts_compact()
{
    var vm = new ResponsiveViewModel();

    Assert.True(vm.IsCompact);
}
```

**④ 第一个 L1 测试**（`InteractionTests.cs`，`[AvaloniaFact]`）：放进窗口 → 按名字找控件 → 操作 → 断言控件状态。

```csharp
[AvaloniaFact]
public void Margin_slider_drives_the_sample_border()
{
    var window = UiHelpers.Show(new PositioningPage());
    var border = (Border)window.GetVisualDescendants().OfType<TextBlock>()
        .First(t => t.Text == "我有 Margin").Parent!;

    window.ByName<Slider>("MarginSlider").Value = 20;

    Assert.Equal(new Thickness(20), border.Margin);
}
```

**⑤ 运行**：

```bash
dotnet test tests/Avalonia.LayoutDemo.Tests
dotnet test tests/Avalonia.LayoutDemo.Tests --filter "FullyQualifiedName~InteractionTests"
```

再加一条冒烟测试（见 5.3）就满足「每个应用至少一条」的门槛。要点击、键入、等待异步，查 3.4 的 `UiHelpers` 速查表。

---

## 2. 分层与目录映射

各层的职责、工具、速度对比见[通用文档](2026-10-18-testing-common-standards.md)第 1 节。在本仓库里的落点：

```
src/Avalonia.<X>Demo/                 被测应用
tests/Avalonia.<X>Demo.Tests/         L0 + L1 + L2（默认每个提交都跑）
tests/Avalonia.<X>Demo.AppiumTests/   L3（HELLO_AVALONIA_APPIUM=1 门控，默认跳过）
tests/Avalonia.Tests.Common/          L1/L2 共用辅助（HeadlessApp、UiHelpers、TabWalker、CultureScope、LogCollector）
tests/Avalonia.Appium.Common/         L3 共用辅助（AppSession、AppiumFactAttribute、KeyChord、UnicodeKeyboard）
```

---

## 3. UI 测试：Headless 还是 Appium

### 3.1 判断表

| 你要验证的东西 | 选 Headless | 选 Appium / FlaUI |
| --- | --- | --- |
| ViewModel 的属性、命令、校验 | L0 单测即可，不用 UI | 不必 |
| 绑定是否生效、转换器是否被调用 | ✅ | ❌ 太慢 |
| 控件模板 / 样式 / 伪类（hover、pressed） | ✅ | ❌ |
| 布局、断点、自适应、`UpdateLayout()` 后的尺寸 | ✅ | ❌ |
| 键盘路由、Tab 顺序、快捷键、TextInput | ✅ | 仅当要验证**原生输入法/系统级快捷键** |
| 拖放（应用内） | ✅ | 仅当跨进程/跨窗口拖放 |
| 动画、定时器的时序 | ✅ + `RunJobs()` / `ForceRenderTimerTick()` | 不稳定，避免 |
| 对话框（`StorageProvider`、文件选择器） | 只能断言「平台不支持」分支 | ✅ 真实选择器 |
| 原生窗口管理（最小化、多显示器、DPI、任务栏） | ❌ | ✅ |
| 系统托盘、右键菜单、原生菜单栏 | ❌ | ✅ |
| 剪贴板与系统集成 | ⚠️ headless 有内存实现，可测逻辑 | 仅当要验证真实系统剪贴板格式 |
| 无障碍（AutomationProperties → UIA 树） | ❌ 不验证 | ✅ |
| 安装包/启动器/多进程 | ❌ | ✅ |
| 关键用户旅程（登录 → 下单 → 退出） | 可测逻辑，但不测原生层 | ✅ 少量 |

**判断口诀**：

1. **在进程内能复现** → headless。
2. **只有在真实窗口/系统 API 上才出现** → Appium。
3. **需要像素** → headless + Skia（L2），不是 Appium（截图对比在 Appium 里更不稳）。

### 3.2 Headless 的确切能力边界

官方文档：headless 平台「provides the full Avalonia control tree, layout, styling, and data binding」「replaces the real windowing and rendering backends with in-memory implementations」，因此不需要显示器，CI 随处可跑 [2]。

**它没有的**：

- 原生窗口与平台窗口管理；
- 原生菜单、焦点管理、系统输入法；
- 真实无障碍树；
- 默认绘制后端**不产生真实像素**——要做视觉回归必须 `UseSkia()` + `UseHeadlessDrawing = false`（本仓库 `HeadlessApp.Build<TApp>()` 已经这样配）。

### 3.3 Headless 项目配置（硬性）

1. 包：`Avalonia.Headless.XUnit`。headless 控件同样需要主题才能正确测量和套用样式 [3]；`HeadlessApp.Build<App>()` 用的是应用自己的 `App`，主题随 `App.axaml` 一起带进来，不用另外加。只有用自建的测试 `Application` 时才要自己引主题包（如 `Avalonia.Themes.Fluent`）。
2. 程序集级注册：测试项目没有 `Program.cs`，`AppBuilder` 由 `[assembly: AvaloniaTestApplication(...)]` 暴露 [3]。本仓库统一写成第 1 节 ② 那样，一行 `HeadlessApp.Build<App>()`（内部是 `UseSkia()` + `UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })`），不要在各测试项目里复制配置。
3. **UI 测试一律用 `[AvaloniaFact]` / `[AvaloniaTheory]`**。差别不只是名字：这两个特性挂的是 `AvaloniaFactDiscoverer` → `AvaloniaTestCase`，它把测试体派发到 UI 线程并泵 `RunJobs()`；普通 `[Fact]` 走的是标准 `XunitTestCase`，**永远不会**做这件事，测试会随机失败或死锁。
4. 纯 ViewModel 测试保留普通 `[Fact]`（见[通用文档](2026-10-18-testing-common-standards.md)第 2 节）。

### 3.4 Headless 输入：必须走路由输入管线

用 `Window` 的扩展方法模拟输入：`MouseDown` / `MouseUp` / `MouseMove` / `MouseWheel` / `KeyPress` / `KeyRelease` / `KeyPressQwerty` / `KeyTextInput` / `DragDrop` [2]。

**禁止** `button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))`：它只触发事件处理器，不会调用 `OnClick()`，因此绑定的 `ICommand` **不会执行**，测试会假绿 [2]。

日常不必直接调这些扩展，先用 `UiHelpers`（`tests/Avalonia.Tests.Common/UiHelpers.cs`），它已经封装了本仓库踩过的坑：

| 方法 | 用途 | 替你处理了什么 |
| --- | --- | --- |
| `UiHelpers.Show(page)` | 把页面放进 600×480 的窗口并 `Show()` | — |
| `root.ByName<T>(name)` / `root.ByAutomationId<T>(id)` | 在可视树里找唯一控件 | 找不到或找到多个都抛异常并写明找的是什么，不会静默选错 |
| `root.ButtonWithText(text)` | 按文字找按钮 | `Content` 不是字符串时也能比较 |
| `window.Click(control)` | 在控件中心真实左键点击，会执行绑定命令 | 先 `RunJobs()` → `BringIntoView()` → 再 `RunJobs()`；页面在 `ScrollViewer` 里，不先滚进视野会点偏 |
| `window.Type(control, text)` | 键入文字 | 先 `Focus()` 再 `KeyTextInput()`；只发 `KeyPressQwerty` 不产生 `TextInput` |
| `await UiHelpers.WaitUntil(cond)` | 等 `Task.Run`、定时器、HTTP 等自己会完成的工作 | 边等边泵调度器；超时**抛异常**而不是返回 `false` |
| `window.CaptureFrame()` | 截当前帧为 PNG 字节 | 先 `RunJobs()` + `ForceRenderTimerTick()` |
| `TabWalker.Walk(window)` | 冒烟测试遍历每个 Tab | 单页失败不掩盖后面的页 |

缺能力时先加到 `Avalonia.Tests.Common`，不要在测试项目里写私有版本。

### 3.5 异步与渲染同步

headless 不会自动替你把排队的工作跑完。下面三种情况必须显式冲刷 [2]：

- 布局/尺寸变化后：`window.UpdateLayout()`；
- 调度器上排队的回调：`Dispatcher.UIThread.RunJobs()`；
- 动画/渲染：`AvaloniaHeadlessPlatform.ForceRenderTimerTick()`。

输入辅助方法和 `CaptureRenderedFrame()` 内部会做一部分，**普通测试代码不会**。规则：凡是断言依赖「刚才发生的事已经生效」，就先冲刷，再断言。

### 3.6 Appium 的使用条件（硬性）

- Windows 上 Appium 依赖 **WinAppDriver 作为服务端**，需预装并先启动；WinAppDriver 要求 **Windows 10+ 且开启开发者模式** [4]。
- **需要真实显示器或虚拟显示器**；Linux 没有稳定的桌面驱动，官方明确建议 Linux CI 用 headless [4]。
- 因此 Appium **必须是可选层**：用环境变量门控，默认跳过（本仓库 `HELLO_AVALONIA_APPIUM=1` + `AppiumFactAttribute`）。
- 仓库实测补充：中文输入法会吞掉 WinAppDriver 的 `SendKeys`，方向键也送不到；一律用 `AppSession.TypeInto` / `KeyChord.Press`。不要按 Ctrl+Shift 组合键（Windows 切换输入法热键，结果时有时无）。
- 子窗口用 `AppSession.FindWindow` 按标题找，`WindowHandles` 不列**有所有者的**窗口（owned window）。
- Appium 项目会**接管鼠标键盘**，多个项目并行要串行化（`AppSession` 用跨进程命名 Mutex 做到这一点）。

**Appium 里不要做的事**：重复 headless 已覆盖的断言、验证样式细节、跑大量参数化用例、依赖固定端口或固定窗口位置。

### 3.7 Appium 里保留什么

每个应用 **1–3 条**关键旅程即可，例如：

1. 启动 → 主窗口出现 → 关键控件可被自动化 ID 找到；
2. 一次完整的输入 → 提交 → 结果可见；
3. 原生有差异的行为：文件对话框、窗口关闭确认、托盘、无障碍名称。

控件要能被稳定定位，优先给关键控件设 `AutomationProperties.AutomationId`（`Name` 只在不依赖本地化时可用）。这不是所有控件的硬要求，但**关键流程上的控件必须有**——这条是本仓库的约定，不是官方强制的。

---

## 4. 视觉回归测试（L2）

### 4.1 何时做

满足**全部**条件才做：

1. 你拥有自定义控件、主题或自绘渲染；
2. 外观本身就是需求（品牌色、圆角、图标对齐）；
3. 已经有一个 headless UI 测试流程可以复用。

**不要**给所有页面做像素基线：字体渲染、抗锯齿、SKU 差异会让测试长期红，维护成本超过收益。

### 4.2 默认做法：采样像素断言

前提是 `UseSkia()` + `UseHeadlessDrawing = false`（`HeadlessApp.Build<App>()` 已配），否则截到的是 1×1 位图。本仓库现成示例是 `tests/Avalonia.TestingDemo.Tests/RenderSnapshotTests.cs`：截帧后读几个关键点的颜色，直接断言。

```csharp
var window = new Window { Width = 200, Height = 100, Content = new Border { Background = Brushes.Red } };
window.Show();

var frame = window.CaptureRenderedFrame()!;

Assert.Equal(Colors.Red, PixelAt(frame, 100, 50));   // PixelAt 见 RenderSnapshotTests.cs，按帧的像素格式读字节
```

采样点选在色块中央、远离边缘和文字，避开抗锯齿；不需要维护基线图片。

### 4.3 需要整图比对时（仓库尚未实现）

采样不够用（例如要锁住整个自定义控件的外观）时再建基线流程，约定如下，**本仓库目前没有这套设施**：

- 基线图片随测试项目提交，文件名包含用例名；
- 比对时给出**容差**（像素差异比例），不要逐字节比较；
- 失败时输出「实际 / 预期 / 差异图」三张，方便人工判断；
- 基线更新必须是显式操作（如环境变量开关），不能在普通测试里自动覆盖。

### 4.4 什么时候跳过

- 动画中间帧（不稳定）；
- 依赖系统字体的文本排版；
- 高 DPI 下的亚像素渲染。

---

## 5. 测试代码的组织

命名、断言、覆盖率等通用规则见[通用文档](2026-10-18-testing-common-standards.md)第 4–5 节；这里只写 Avalonia 项目特有的部分。

### 5.1 文件划分

按测试对象命名文件，常见的几个：

```
tests/Avalonia.<X>Demo.Tests/
  TestAppBuilder.cs       // 只写 HeadlessApp.Build<App>() 一行
  ViewModelTests.cs       // L0：纯 xUnit
  ServiceTests.cs 等      // L0：服务、缓存等非 UI 逻辑
  SmokeTests.cs           // L1：主窗口能 Show、每个 Tab 能切换
  InteractionTests.cs     // L1：headless + [AvaloniaFact]
  RenderSnapshotTests.cs  // L2（可选）
```

**一个测试类只服务一种模式**：`[Fact]` 和 `[AvaloniaFact]` 不要混在同一个类里，否则读者无法判断某个测试需不需要 UI 线程。

### 5.2 共享辅助

- 通用辅助放 `tests/Avalonia.Tests.Common` / `tests/Avalonia.Appium.Common`，**不要**在各测试项目复制；
- 辅助方法内部要处理本仓库已知的时序坑（`RunJobs`、`BringIntoView`、`Focus`），见第 3.4 节。

### 5.3 冒烟测试

通用要求见通用文档第 5.1 节。本仓库的做法：用 `TabWalker.Walk` 遍历每个 Tab / 页面，单个页面失败不会掩盖后面的页面；现成写法照抄任一 `tests/Avalonia.*.Tests/SmokeTests.cs`。

### 5.4 结构体属性绑定

绑定到**结构体属性**（`Thickness`、`CornerRadius` 等）的情况必须在实时控件上断言——绑定失败是**静默**的，构建和冒烟测试都检不出（见 `CLAUDE.md`「Avalonia 坑」）。示例：`tests/Avalonia.LayoutDemo.Tests/InteractionTests.cs` 的 `Positioning_sliders_drive_the_margin_and_padding_of_the_sample_borders`。

---

## 6. 证据与置信度

| # | 来源 | 支撑的规则 | 置信度 |
| --- | --- | --- | --- |
| [1] | https://docs.avaloniaui.net/docs/testing/ | 分层策略、四层职责、Appium 的定位 | 高（官方原文） |
| [2] | https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform | headless 能力边界、输入辅助方法、`RaiseEvent(Click)` 不执行命令、`RunJobs`/`ForceRenderTimerTick`、VM 测试不需要 headless | 高 |
| [3] | https://docs.avaloniaui.net/docs/testing/headless-xunit | `[AvaloniaFact]`/`[AvaloniaTheory]`、`AvaloniaTestApplication`、主题包要求 | 高 |
| [4] | https://docs.avaloniaui.net/docs/testing/ui-testing-with-appium | WinAppDriver 前置条件、Linux 无稳定驱动、Appium 覆盖范围 | 高 |

`UiHelpers`、`TabWalker`、输入法相关的结论来自本仓库实测，不是官方文档。
