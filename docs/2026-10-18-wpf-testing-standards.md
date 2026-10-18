# WPF 项目测试规范

> 适用范围：WPF 桌面端 MVVM 项目。本仓库目前**没有 WPF 项目**，本文是为将来新增 WPF 项目准备的规则，也是与 Avalonia 的差异清单——**不能照搬 Avalonia 的做法**。
>
> 与 UI 框架无关的部分（分层模型、ViewModel / 服务层、HTTP mock、SQLite、命名、覆盖率、CI）见 [`2026-10-18-testing-common-standards.md`](2026-10-18-testing-common-standards.md)。Avalonia 项目见 [`2026-10-18-avalonia-testing-standards.md`](2026-10-18-avalonia-testing-standards.md)。
>
> 证据来源：Xunit.StaFact 文档、Avalonia XPF 文档（均为一手）。正文方括号是本文自己的出处编号，见文末「证据与置信度」。标「（推断）」的结论没有官方文档直接背书，标「（已实测）」的结论在本仓库的探针里跑过。
>
> 性能基准（BenchmarkDotNet）不在本文范围，见 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md)。

---

## 0. 一页结论

| 问题 | 结论 |
| --- | --- |
| WPF 有没有 headless？ | 原生 WPF **没有**官方 headless；L1 用 `[WpfFact]`（Xunit.StaFact）在 STA 线程里直接创建控件 [1] |
| 有 Avalonia XPF 许可证怎么办？ | L1 首选 XPF headless（`[AvaloniaFact]`），用 Avalonia headless 跑 WPF UI；**CI 里也需要许可证** [2] |
| L1 用什么特性？ | `[WpfFact]` / `[WpfTheory]`：STA 线程 + WPF Dispatcher；非 Windows 自动 Skipped [1] |
| 怎么模拟点击？ | `RaiseEvent(Button.ClickEvent)` 同样**不会**执行 `Command`；用 `ButtonAutomationPeer` 的 `IInvokeProvider.Invoke()`，再冲刷 Dispatcher（已实测）；只测命令逻辑时直接 `Command.Execute(...)` |
| 键盘 / 焦点 / 输入法 / DPI 怎么测？ | 进程内测不了，归 L3：FlaUI（UIA3）或 Appium + WinAppDriver（推断） |

---

## 1. 关键事实

**原生 WPF 没有 Avalonia 那样的官方 in-process headless 平台。** 分层名字沿用通用文档第 1 节，但 L1 / L2 的机制要换：

| 层 | 原生 WPF 的做法 |
| --- | --- |
| L0 单测 | xUnit `[Fact]`，与 Avalonia 相同 |
| L1 UI 测试 | `[WpfFact]` + 进程内创建真实控件（见第 2 节） |
| L2 视觉回归 | `RenderTargetBitmap` 渲染控件树后比对，同样在 `[WpfFact]` 里 |
| L3 端到端 | FlaUI（UIA3），或 Appium + WinAppDriver |

**例外：Avalonia XPF**（商业的 WPF 兼容层）。官方文档建议「try Headless testing first as it's fast and portable」，做法与 Avalonia 基本一致，但有几处差别 [2]：

- 引用 `Avalonia.Headless.XUnit`（或 NUnit 版本），版本取 `$(XpfAvaloniaVersion)`；测试项目需要 `Xpf.Sdk`，TFM 用 `net8.0-windows` 或更高（Linux/macOS 上加 `EnableWindowsTargeting=true`）；
- **CI 里也必须有有效的 XPF 许可证**，否则运行时校验失败；
- AppBuilder 用 `Configure<DefaultXpfAvaloniaApplication>().WithAvaloniaXpf().UseSkia().UseHeadless(...)`；
- headless 输入扩展**只能用在 Avalonia `Window` 上**，要先 `XpfWpfAbstraction.GetAvaloniaWindowForWindow(wpfWindow)` 拿到对应窗口；
- 普通 WPF 控件库也能被这样的测试项目引用并测试；
- 测试间状态泄漏或 `TaskScheduler` 报错时，可设 `AvaloniaTestIsolationLevel.PerAssembly`。

团队已经有 XPF 许可证时，L1 走 XPF headless，写法参考 Avalonia 文档；没有的话按下面的 `[WpfFact]` 方案走。

---

## 2. WPF 的 L1：`[WpfFact]` + 进程内控件

`Xunit.StaFact` 提供 `[WpfFact]` / `[WpfTheory]`：把测试体放到 **STA 线程**并安装 WPF 的 `DispatcherSynchronizationContext`，效果等同于 GUI 主线程。官方描述为「Run your xunit-based tests on an STA thread with the WPF Dispatcher」[1]。

### 2.1 上手

**① 测试项目**：与被测项目同为 Windows TFM 并启用 WPF。`Xunit.StaFact` 要先登记到 `Directory.Packages.props`，大版本跟 xUnit v3 的大版本走：仓库的 `xunit.v3` 3.x 用 `Xunit.StaFact` 3.x（实测 3.0.13）；4.x 依赖 `xunit.v3` 4.x。

```xml
<PropertyGroup>
    <OutputType>Exe</OutputType>                         <!-- xunit v3 -->
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
</PropertyGroup>
<ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Xunit.StaFact" />
</ItemGroup>
```

**② 第一个 L1 测试**：创建控件 → 绑定 VM → 通过自动化接口点击 → 冲刷 → 断言 VM。控件不用放进窗口。

```csharp
[WpfFact]
public void Save_button_runs_its_command()
{
    var vm = new EditorViewModel();
    var button = new Button { Command = vm.SaveCommand };

    // The automation peer goes through OnClick, so the bound Command runs; RaiseEvent(ClickEvent) would not.
    var invoke = (IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!;
    invoke.Invoke();
    // Invoke only queues the click on the dispatcher; drain it before asserting.
    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    Assert.True(vm.Saved);
}
```

实测（`.claude/temp/probe-wpf-click`，2026-10-18）：

- `Invoke()` 后不冲刷，命令**还没执行**；冲刷一次后执行且只执行一次。忘了冲刷，"命令没执行"类断言会假绿；
- 按钮禁用（`CanExecute` 为 false）时 `Invoke()` 抛 `ElementNotEnabledException`，可以直接用 `Assert.Throws` 测禁用分支；
- `RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent))` 不执行命令。

### 2.2 规则

- 任何 `new Window()` / `new Button()` / 依赖 `Dispatcher` 的测试都用 `[WpfFact]`，参数化用 `[WpfTheory]`；
- 非 Windows 平台会自动 **Skipped** [1]，WPF 测试项目只能在 Windows 上跑；
- 只测布局和模板时不必显示窗口：`control.ApplyTemplate()` 后用 `Template.FindName` 查模板部件，`Measure` / `Arrange` 后读 `ActualWidth` 等尺寸（已实测，用的是默认主题）；
- 需要 `Loaded` 事件或依赖它的样式触发器时，把控件放进 `Window` 并 `Show()`（需要桌面会话），断言后 `Close()`（推断）。

**WPF L1 能覆盖的**（推断）：绑定、命令、转换器、模板应用、依赖属性、样式触发器。
**WPF L1 覆盖不了的**（推断）：真实键盘 / 鼠标输入管线、焦点、窗口动画、DPI 与多显示器——这些归 L3。

## 3. WPF 的输入模拟

WPF 没有 Avalonia 那套 `Window.KeyTextInput` 扩展。按目的选：

| 你要证明的事 | 做法 |
| --- | --- |
| 按钮点下去命令会执行 | `ButtonAutomationPeer` + `IInvokeProvider.Invoke()` + 冲刷 Dispatcher（见 2.1，已实测） |
| 命令本身的逻辑 | `vm.XxxCommand.Execute(...)`，属于 L0，不用控件 |
| 文本变化后的逻辑 | 直接设 `TextBox.Text`——只测逻辑，**不要**当成输入测试 |
| 键盘路由、焦点、输入法 | 放到 L3（FlaUI/UIA 或 Appium）；进程内的 `InputManager` / `Keyboard` 编程式输入不可靠（推断） |

**与 Avalonia 相同的教训**：在 WPF 里 `button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))` 同样**不会**执行 `OnClick` 里的 `Command`。

## 4. WPF 的 L3

- **FlaUI（UIA3）**：进程外驱动，支持 WPF 原生；不需要 WinAppDriver，适合纯 Windows 桌面 CI（仍需桌面会话）。
- **Appium + WinAppDriver**：与 Avalonia 相同的前置条件（Windows 10+、开发者模式、显示器），见 Avalonia 文档「Appium 的使用条件」。
- 选择：已有 Appium 基础设施就沿用；只做 WPF 且不想装 WinAppDriver，用 FlaUI。

---

## 5. WPF 的视觉回归（L2）

机制与 Avalonia 不同：用 `RenderTargetBitmap` 渲染控件树（先 `Measure` / `Arrange`），在 `[WpfFact]` 里执行。

何时做、默认用采样像素断言、整图比对的约定、什么时候跳过——这些规则与 Avalonia 相同，见 [Avalonia 文档](2026-10-18-avalonia-testing-standards.md)第 4 节。

---

## 6. 共享的部分

以下内容 WPF 与 Avalonia 完全一致，规则直接复用，不在本文重复：

- ViewModel 纯单测（[通用文档](2026-10-18-testing-common-standards.md)第 2.1–2.2 节）；
- 替身替在接口上、HTTP mock 三档（通用文档第 2.3–2.4 节）；
- SQLite 测试（通用文档第 3 节）；
- 命名、断言、覆盖率与冒烟测试、CI 组织（通用文档第 4–6 节）。

---

## 7. 证据与置信度

### 7.1 一手来源与结论对应

| # | 来源 | 支撑的规则 | 置信度 |
| --- | --- | --- | --- |
| [1] | https://github.com/AArnott/Xunit.StaFact | `[WpfFact]`/`[WpfTheory]`、STA + WPF Dispatcher 同步上下文、非 Windows 标记 Skipped | 高（官方 README） |
| [2] | https://docs.avaloniaui.net/xpf/testing/headless-testing | XPF headless 的包、许可证、AppBuilder、窗口桥接、隔离级别 | 高（官方原文） |

### 7.2 未充分核实的部分（使用时需自行验证）

- **Avalonia XPF headless**：已读官方页 [2]，但未在本仓库实测；页面没有列出 XPF headless 测不了的 WPF 功能。
- **WPF 进程内 UI 测试的边界**：文中标「（推断）」的条目基于机制推断，没有 WPF 官方文档直接背书。第 2.1–2.2 节标「已实测」的点击、禁用、模板与布局结论已在 `[WpfFact]` 探针中验证（net10.0-windows、xunit.v3 3.2.2、Xunit.StaFact 3.0.13）。
- **FlaUI 与 UIA3**：作为 WPF 的 L3 替代方案是合理选择，本次未逐一核验其文档。