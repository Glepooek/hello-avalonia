---
applyTo: "tests/**"
---

# 测试规范（Avalonia / WPF）

配套文档：`docs/2026-10-18-testing-common-standards.md`、`docs/2026-10-18-avalonia-testing-standards.md`、`docs/2026-10-18-wpf-testing-standards.md`。

## 选层

按成本从低到高选择，不要跳级：

1. 纯 ViewModel / Service / Converter：普通 xUnit `[Fact]`，不启动 Avalonia。
2. 依赖属性系统、布局、样式、绑定、输入：Headless + `[AvaloniaFact]`。
3. 需要像素断言：Headless + Skia，采样关键点颜色（示例 `tests/Avalonia.TestingDemo.Tests/RenderSnapshotTests.cs`）。
4. 只有真实窗口或系统 API 才会出现（原生对话框、菜单、焦点、DPI、托盘、无障碍）：Appium。

## Headless

- 用到控件、`Dispatcher.UIThread`、`Application.Current`、`AssetLoader`、`Bitmap` 时使用 `[AvaloniaFact]` / `[AvaloniaTheory]`。`[Fact]` 不会派发到 UI 线程。
- 统一调用 `HeadlessApp.Build<App>()`，不要复制 AppBuilder 配置。
- 交互优先使用 `UiHelpers`（`ByName`、`ByAutomationId`、`ButtonWithText`、`Click`、`Type`、`WaitUntil`、`CaptureFrame`）和 `TabWalker.Walk`。缺少的能力加到 `Avalonia.Tests.Common`，不要在测试项目里写私有版本。
- 禁止使用 `RaiseEvent(new RoutedEventArgs(Button.ClickEvent))`：它不会执行绑定命令，测试会假绿。
- 断言依赖刚发生的变化时先冲刷：`UpdateLayout()` → `Dispatcher.UIThread.RunJobs()`；动画再加 `ForceRenderTimerTick()`。
- 等待条件失败时抛出异常，不返回 `bool`。
- 区域相关断言使用 `CultureScope.Invariant()`。
- 绑定到结构体属性的行为必须在实时控件上断言，因为失败是静默的。

## Appium

- 每个应用保留 1–3 条关键旅程，不重复 Headless 已覆盖的断言。
- 必须用 `HELLO_AVALONIA_APPIUM=1` 与 `AppiumFactAttribute` 门控，默认跳过。
- 键入使用 `AppSession.TypeInto`，按键使用 `KeyChord.Press`；不要用 WinAppDriver 的 `SendKeys`，不要按 Ctrl+Shift 组合键。
- 子窗口用 `AppSession.FindWindow` 按标题查找。
- 关键流程的控件必须设置 `AutomationProperties.AutomationId`。
- 测试串行执行，不依赖固定窗口位置或端口。

## ViewModel 与替身

- 默认直接 `new` 被测 ViewModel。异步测试返回 `async Task`，不使用 `.Result` / `.Wait()`。
- 时间、文件系统、随机数包装成接口注入。
- 替身只用于接口或有 `virtual` 成员的类；NSubstitute 对非 virtual 成员会执行真实代码。
- HTTP 优先替换 `HttpMessageHandler`（MockHttp），其次 WireMock.Net（端口用 0），最后替换服务接口。
- 禁止单测访问真实公网 API、使用 `Thread.Sleep`、使用静态 `HttpClient` 单例。

## 存储

- 禁止用 EF Core in-memory provider 或手写 fake `DbSet` 测查询。
- 使用 SQLite `:memory:`，连接在测试类中保持打开，`DbContext` 每次新建。
- 需要 WAL 或多连接时使用临时文件数据库，用完删除。
- provider 专有函数必须有针对真实 provider 的测试。

## WPF 差异

- L1 使用 `[WpfFact]` / `[WpfTheory]`（Xunit.StaFact）。
- 点击按钮用 `ButtonAutomationPeer` 的 `IInvokeProvider.Invoke()`，之后必须用 `Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle)` 冲刷。`RaiseEvent(Button.ClickEvent)` 同样不会执行 `Command`。

## 组织

- 每个应用至少有一条冒烟测试：主窗口可显示、每个页面可切换、主 ViewModel 可构造。
- 测试方法名描述行为；多组输入用 `[Theory]` + `[InlineData]`。
- 覆盖率不是 KPI，优先覆盖 `CanExecute` 分支、错误路径和边界值。
- 不用重试掩盖真实失败；时序类偶发失败应补充冲刷。
