# 测试规范（Avalonia / WPF）

配套文档：`docs/2026-10-18-avalonia-wpf-testing-standards.md`（含出处与置信度）。以下是可执行的硬规则。性能基准（BenchmarkDotNet）不在本文范围，见 `docs/2026-10-18-benchmarkdotnet-standards.md`（基准是性能测试，但不进功能回归的阻断门禁）。

## 1. 选层

按以下顺序选择最低成本的层，不要跳级：

1. **纯 ViewModel / Service / Converter** → 普通 xUnit `[Fact]`，不启动 Avalonia。
2. **依赖 Avalonia 属性系统、布局、样式、绑定、输入** → headless + `[AvaloniaFact]`。
3. **渲染像素是需求**（自定义控件、主题）→ headless + Skia 视觉回归，给容差。
4. **只有真实窗口/系统 API 才出现**（原生对话框、菜单、焦点、DPI、托盘、无障碍、安装器）→ Appium（WPF 可用 FlaUI）。

判断口诀：*在进程内能复现 → headless；只有真实窗口才行 → Appium；需要像素 → headless + Skia。*

**不要**把所有 UI 验证塞进 Appium，也不要只做单测而跳过整层 L1。

## 2. Headless 层

- UI 测试一律用 `[AvaloniaFact]` / `[AvaloniaTheory]`。`[Fact]` 不会派发到 UI 线程，也不泵 `RunJobs()`。
- 纯 VM 测试保留 `[Fact]`；同一个测试类里不要混用两种特性。
- 测试项目统一调用 `HeadlessApp.Build<App>()`，不复制 AppBuilder 配置。
- 视觉回归依赖 `UseSkia()` + `UseHeadlessDrawing = false`；默认绘制后端只产出 1×1 位图。
- 输入用 `Window` 的扩展方法（`MouseDown`/`MouseUp`/`KeyPress`/`KeyTextInput`/`DragDrop` 等）。**禁止** `RaiseEvent(new RoutedEventArgs(Button.ClickEvent))`：它不会调用 `OnClick`，绑定命令不会执行，测试会假绿。
- 断言依赖刚发生的事时先冲刷：`UpdateLayout()` → `Dispatcher.UIThread.RunJobs()` → 动画再加 `ForceRenderTimerTick()`。
- 会失败就抛异常，不返回 `bool`（参考 `UiHelpers.WaitUntil`）。
- 区域相关断言用 `CultureScope.Invariant()`。

## 3. Appium 层

- 只保留每个应用 1–3 条关键旅程；不重复 headless 已覆盖的断言。
- 必须环境变量门控（`HELLO_AVALONIA_APPIUM=1`），默认跳过；前置条件是 WinAppDriver 已启动、Windows 10+ 开发者模式、可用桌面会话。
- 键入一律 `AppSession.TypeInto`，按键一律 `KeyChord.Press`；不用 WinAppDriver 的 `SendKeys`（中文输入法吞字母）。
- 不按 Ctrl+Shift 组合键（Windows 输入法切换热键，结果不稳定）。
- 子窗口用 `AppSession.FindWindow` 按标题找，`WindowHandles` 不列有所有者的窗口。
- 关键流程上的控件必须有 `AutomationProperties.AutomationId`。
- 串行执行（`AppSession` 的跨进程 Mutex 已处理）；不要依赖固定窗口位置或固定端口。

## 4. ViewModel 与 mock

- 默认 `new XxxViewModel()` 直接测；只有 VM 内部用 `Dispatcher.UIThread` / `Application.Current` / `AssetLoader` / `Bitmap` 时才升级为 `[AvaloniaFact]`。
- 异步测试写 `async Task`，不 `.Result` / `.Wait()`；`AsyncRelayCommand` 用 `await ExecuteAsync(...)`。
- 时间、文件系统、随机数包装成接口注入，测试用可控实现。
- 替身只用于接口，或有 `virtual` 成员的具体类。NSubstitute 对非 virtual 成员**会执行真代码**，构造函数也会执行。
- HTTP mock 三档：
  1. 替换 `HttpMessageHandler`（`RichardSzalay.MockHttp`，或用 `IHttpClientFactory` 同名注册覆写 primary handler）——首选；
  2. `WireMock.Net`——需要真实 HTTP 语义时，端口用 0 动态分配；
  3. 替换服务接口——只测 VM 编排逻辑时。
- 禁止单测请求真实公网 API、`Thread.Sleep`、静态 `HttpClient` 单例。

## 5. SQLite / 本地存储

- 禁止用 EF Core in-memory provider 测查询；禁止手写 fake `DbSet`。
- 用 SQLite `:memory:`，**连接在测试类里保持打开**，`DbContext` 每次新建；`Dispose` 关闭连接即销毁数据库。
- 事务、约束、原生 SQL 用这套方式真实执行；`Migrate()` 至少测一条。
- 需要 WAL/连接池/多连接时用临时文件数据库，用完删除。
- provider 专有函数（如 `EF.Functions.DateDiffDay`）必须有少量针对真实 provider 的测试，SQLite 不能替代。
- VM 测试里用仓储接口替身，不启动数据库。

## 6. Benchmark

- 基准不是单元测试：**禁止**在测试里对耗时做硬断言。
- 独立 `benchmarks/` 项目，`Release` 命令行运行，不挂调试器；不要硬写 `<Optimize>`——`Directory.Build.props` 已按配置设置。
- 基准方法必须使用或返回结果，防止 JIT 死代码消除。
- 结果绑定运行时/架构/OS/JIT，不外推。
- 报告回归用 `[JsonExporterAttribute.FullCompressed]` 生成 `*-report-full-compressed.json`，接 `benchmark-action/github-action-benchmark`（`tool: benchmarkdotnet`）。
- 门禁必须显式设阈值：默认 `alert-threshold` 200%、`fail-on-alert` false，不设置就永远不会失败。
- 托管 runner 噪声约 ±10~20%，阈值不要低于此；不稳定机器只告警不失败。
- 不在 `pull_request` 事件上运行并推送 gh-pages。

## 7. WPF 差异

- 原生 WPF 没有官方 headless：L1 用 `[WpfFact]` / `[WpfTheory]`（Xunit.StaFact，STA + WPF Dispatcher）。
- 有 Avalonia XPF 许可证时，L1 优先用 XPF headless（`[AvaloniaFact]`，CI 也要许可证）；输入扩展要先用 `XpfWpfAbstraction.GetAvaloniaWindowForWindow` 拿到 Avalonia 窗口。
- 输入管线、原生窗口、DPI、无障碍仍归 L3（FlaUI/UIA3 或 Appium）。
- WPF 里 `RaiseEvent(Button.ClickEvent)` 同样不执行 `Command`。
- ViewModel、HTTP mock、SQLite、Benchmark 规则与 Avalonia 相同。

## 8. 组织与 CI

- 目录：被测应用 `src/Avalonia.<X>Demo/`，测试 `tests/Avalonia.<X>Demo.Tests/`，Appium `tests/Avalonia.<X>Demo.AppiumTests/`；共用辅助放 `Avalonia.Tests.Common` / `Avalonia.Appium.Common`，不复制。
- 每个应用至少一条冒烟测试：主窗口能 Show、每个 Tab/页面能切换（`TabWalker.Walk`）、主 VM 能构造。
- 测试方法名描述行为；多输入用 `[Theory] + [InlineData]`。
- 绑定到结构体属性的行为必须在实时控件上断言——静默失败，构建和冒烟测试都检不出。
- 执行顺序：build → `dotnet test`（L0 + L1，强制门禁）→ Appium（可选）→ benchmark（独立阶段）。
- 覆盖率不是 KPI；优先覆盖 `CanExecute` 分支、错误路径、边界值。
- 不用重试掩盖真实失败。
