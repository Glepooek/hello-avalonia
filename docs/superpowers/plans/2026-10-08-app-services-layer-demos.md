# Avalonia 应用服务层演示项目（#12 Services / #13 AppDevelopment / #14 TestingDemo / #15 TestingDemo.Tests）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建成 `Avalonia.ServicesDemo`、`Avalonia.AppDevelopmentDemo`、`Avalonia.TestingDemo`、`Avalonia.TestingDemo.Tests` 四个项目，覆盖官方文档 Services、App Development、Testing 三个分类的可演示功能点。这是 spec 第二阶段的最后一组。

**Architecture:** 前三个项目沿用 `Avalonia.LayoutDemo` 样板：`MainWindow` 只承载 `TabControl`，每个功能点是 `Views/Pages/` 下一个 `UserControl`，页面顶部用 `Avalonia.Shared` 的 `DemoHeader`。`TestingDemo` 是被测应用，刻意做成易于断言的 UI（每个交互控件有 `Name` 与 `AutomationId`）；`TestingDemo.Tests` 是 xUnit v3 + Headless 测试项目，引用 #14。与前几组不同，本组的验证方式分两种：#12–#14 用仓库外的 headless 探针，#15 自己就是测试，用 `dotnet test`。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm、`Microsoft.Extensions.DependencyInjection` 10.0.12、`Microsoft.Extensions.Logging.Console` 10.0.12、`Avalonia.Headless.XUnit` 12.1.2、`xunit.v3` 3.2.2、`xunit.runner.visualstudio` 3.1.5、`Microsoft.NET.Test.Sdk` 18.10.1、中央包管理

**Spec:** `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**前几组的写法参考:** `docs/superpowers/plans/2026-10-07-interaction-graphics-layer-demos.md`（硬性规则 1–28、探针写法、已落成的 `Avalonia.EventsDemo/` 等四个项目）

## Global Constraints

以下约束适用于本 plan 的每一个任务：

- **Avalonia 版本统一为 12.1.2**，不为任何项目降级到 Avalonia 11
- **TargetFramework 为 `net10.0`**，`Nullable` 为 `enable`
- **包版本只在 `Directory.Packages.props` 声明**，`.csproj` 里的 `PackageReference` 不带 `Version` 属性。本 plan 新增 6 个包，全部在 Task 1 一次声明
- **不引用 `Avalonia.Diagnostics`**（停在 11.3.22，v12 的 DevTools 已内置于主包）
- **不引入 ReactiveUI、Prism 等第三方 MVVM/UI 框架**，只用官方 API + `CommunityToolkit.Mvvm`
- **C# 与 XAML 注释用英文**；**界面文字（Tab 标题、说明条、按钮文案）用中文**；**标识符（类名、属性名、`x:Name`）用英文**
- **每个演示页顶部必须有 `DemoHeader`**，含中文功能点描述 + 官方文档路径。`DocPath` **不带 `docs/` 前缀**，写成 `分类名/子页名`（如 `services/clipboard`、`app-development/threading`）
- **每个功能点一个 `UserControl`**，放在 `Views/Pages/` 下
- **功能点粒度 3–13 个 Tab**：本 plan 为 8（#12）/ 10（#13）/ 3（#14 的三个被测面板）/ #15 是测试项目无 Tab
- **不为演示项目写自动化测试**，唯一例外是 `Avalonia.TestingDemo.Tests`（它本身就是测试演示）
- **`AvaloniaUseCompiledBindingsByDefault` 设为 `true`**
- **去重规则**（spec「分类之间的去重规则」）：Focus Manager 完整实现在 #9（#12 做指路页）；Data validation 完整实现在 #5（#13 做指路页）；Embedding web content 在 `Avalonia.WebViewDemo`（#13 指路页）；ResX 本地化在 `Avalonia.MusicStore` 有完整实现，#13 做最小版本并指路
- **App Development 里不做 Tab 的子页**：XAML live previewer、Performance optimization、Native platform interop（spec 已定）；另外 `cross-platform-solution-setup` 是项目搭建流程，无运行时内容，也不做
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 前几组已确立的硬性规则（与本组页面直接相关的）

完整来龙去脉见 spec 的各层实测结论小节与上一份 plan 的规则 1–28。**每一条都对应一次"构建 0 错误、运行无异常、行为却是错的"**：

1. **打算被样式/过渡驱动的属性，不在元素上写本地值**；`StringFormat` 里字面花括号双写，以 `{0}` 开头的格式串写成 `'{}{0} …'`。
2. **验证用 headless 探针读回属性值，不靠目视。** 探针建在仓库外（`C:\Temp\...`），跑完即删，不提交。
3. **探针里发鼠标事件前，先用 `TranslatePoint` 换算出窗口坐标**；`RaiseEvent(Click)` 只触发 `Click` 处理器，不执行 `Command`；headless 默认后端里所有 `Bitmap` 的 `PixelSize` 都是 `1×1`。
4. **派生自现有控件的自定义控件要覆盖 `StyleKeyOverride`**，否则不渲染且无报错（有自己 ControlTheme 的控件除外）。
5. **XAML 里 `Ctrl+1` 这类数字键会被解析成枚举数值**（`Key.Back`），要写 `Ctrl+D1`。
6. **命令禁用时断言 `IsEffectivelyEnabled`**，`IsEnabled` 不变。
7. **页面里元素 `Name` 不能与继承属性重名**（如 `Opacity`），否则生成字段遮住继承成员，`CS0108`。
8. **被 Transition 驱动的附加属性要先写显式起始值**（`Canvas.Left` 默认 `NaN`，没有起点可插值）。
9. **`KeyPressQwerty` 只发 KeyDown/KeyUp，不产生 TextInput**；要输入字符用 `KeyTextInput`。
10. **LSP 对 `InitializeComponent`、命名元素的 `CS0103` 是误报**，以 `dotnet build` 为准。

### 本组新增的硬性规则（编写 plan 时已实测，Avalonia 12.1.2 headless）

11. **`Avalonia.Headless.XUnit` 12.1.2 依赖 xunit v3，不是 v2。** 它的依赖是 `xunit.v3.extensibility.core 3.2.2`。测试项目若引用 `xunit` 2.9.3，`[InlineData]` 会报 `CS0433`（类型同时存在于 `xunit.core` 与 `xunit.v3.core`）。必须引用 **`xunit.v3` 3.2.2**，且测试项目要写 **`<OutputType>Exe</OutputType>`**（v3 测试项目本身是可执行文件）。spec 的包清单里写的 `xunit` 因此作废。`xunit.runner.visualstudio` 用 3.1.5，`Microsoft.NET.Test.Sdk` 用 18.10.1，三者配合 `dotnet test` 通过。
12. **`Avalonia.Headless.XUnit` 的写法**：`[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]`；`TestAppBuilder` 提供 `public static AppBuilder BuildAvaloniaApp()`；用 `[AvaloniaFact]`、`[AvaloniaTheory]` 代替 `[Fact]`、`[Theory]`（它们负责准备 UI 线程）。`[InlineData]` 与 `Assert` 仍用 `Xunit` 命名空间。
13. **渲染快照要显式启用 Skia。** 默认的 headless 绘图不产生像素（`CaptureRenderedFrame` 的 `PixelSize` 为 `1×1`）。测试项目要引用 `Avalonia.Skia`（经 `Avalonia.Desktop` 传递引入；测试项目不引用 Desktop 时需直接引用），`TestAppBuilder` 里写 `.UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })`。实测 `window.CaptureRenderedFrame()` 随即返回宽度等于窗口宽的位图；如果被测项目通过 `ProjectReference` 带入 `Avalonia.Desktop`，则无需再单独声明 `Avalonia.Skia`。
14. **`TopLevel` 上的服务在 headless 里全是空实现或 null。** 实测：`StorageProvider` 是 `NoopStorageProvider`（`CanOpen`、`CanSave`、`CanPickFolder` 全为 `False`），`Launcher` 是 `NoopLauncher`（`LaunchUriAsync` 返回 `False`），`InputPane` 与 `InsetsManager` 为 `null`，`Clipboard` 是真实可读写的内存实现。`OpenFilePickerAsync` 不抛异常，返回空列表。`TryGetFolderFromPathAsync` 与 `TryGetWellKnownFolderAsync` 在 headless 里仍然可用，能拿到真实的临时目录与「文档」文件夹。
15. **`PlatformSettings` 要走 `GetPlatformSettings()` 扩展方法**（`using Avalonia.VisualTree;`，定义在 `VisualExtensions`）。`TopLevel.PlatformSettings` 与 `Application.Current.PlatformSettings` 都不存在（`CS1061`）。headless 返回 `DefaultPlatformSettings`：`GetColorValues().ThemeVariant` 为 `Light`、`ContrastPreference` 为 `NoPreference`、`AccentColor1` 为 `#ff0078d7`，`GetDoubleTapTime(PointerType.Mouse)` 为 0.5 秒，`GetTapSize`/`GetDoubleTapSize` 为 `4×4`，`HoldWaitDuration` 为 0.3 秒，`HotkeyConfiguration.Copy` 为 `Ctrl+C,Ctrl+Insert`、`Paste` 为 `Ctrl+V,Shift+Insert`。
16. **`IActivatableLifetime` 在 headless 与经典桌面生命周期下取不到**：`Application.Current.TryGetFeature<IActivatableLifetime>()` 返回 `null`。文档的平台表也显示它在 Windows 与 Linux 上几乎不支持（`Background`、`File`、`OpenUri`、`Reopen` 全是 ✖），只有 macOS、Android、iOS 有。`ActivationKind` 有 `File`、`OpenUri`、`Reopen`、`Background` 四个值。本页因此做成"检测服务是否可用"的页面。
17. **剪贴板的 12.x 写法**：`clipboard.SetTextAsync("abc")`、`await clipboard.TryGetTextAsync()` 读回 `"abc"`；`ClearAsync()` 之后读回 `null`；`new DataTransfer()` + `Add(DataTransferItem.CreateText("..."))` + `SetDataAsync(data)` 后 `TryGetTextAsync()` 读回该文本。`TryGetDataAsync()` 返回的对象要 `using`，其 `Formats` 的元素用 `.Identifier` 取名（文本为 `Text`）。**不用旧的 `IDataObject` / `DataObject` / `DataFormats`。**
18. **DI 的三种生命周期与作用域校验**（`Microsoft.Extensions.DependencyInjection` 10.0.12）：同一作用域内 `AddTransient` 每次解析得到不同实例，`AddScoped` 在同一作用域内是同一实例，`AddSingleton` 全局同一实例。`BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true })` 之后，**从根容器直接解析 `AddScoped` 服务会抛 `InvalidOperationException`**（响亮失败，正好用来演示作用域错误）。
19. **UI 线程访问规则**：非 UI 线程上 `Dispatcher.UIThread.CheckAccess()` 为 `False`；在后台线程写 `TextBlock.Text` 抛 `InvalidOperationException`，消息为 `The calling thread cannot access this object because a different thread owns it.`；从后台线程 `Dispatcher.UIThread.Post(...)` 的回调在 UI 线程执行。
20. **未处理异常钩子**：`Dispatcher.UIThread.UnhandledException`（事件参数 `e.Handled = true`）与 `UnhandledExceptionFilter` 都存在。实测 `Dispatcher.UIThread.Post(() => throw ...)` 之后 `RunJobs()`，处理器被调用且异常被吞掉，没有逃出。
21. **窗口管理实测**：`w2.Show(w)` 之后 `w2.Owner` 就是 `w`；`WindowState` 默认 `Normal`，赋 `Maximized` 后读回 `Maximized`；`ShowDialog<string?>(owner)` 配合 `dialog.Close("result")` 读回 `"result"`；`Closing` 里 `e.Cancel = true` 后窗口保持可见。**`Window.SystemDecorations` 已过时（`CS0618`），用 `WindowDecorations`**（默认 `Full`）。`CanResize` 默认 `True`，`SizeToContent` 默认 `Manual`，`ShowInTaskbar` 默认 `True`，`Topmost` 默认 `False`。
22. **资源查找的差别**：`ResourceDictionary` 里的项被替换后，用 `TryFindResource` 一次性取出并赋值的属性**不会更新**（读回仍是旧色），用 `GetResourceObservable("Brand").ToBinding()` 绑定的属性**会更新**（实测 Red→Blue）。这正是 `StaticResource` 与 `DynamicResource` 的差别。直接修改同一个画刷实例的 `Color` 则两者都会更新。
23. **自动化属性与 Peer**：`AutomationProperties.SetName/SetHelpText/SetAutomationId` 之后，`ControlAutomationPeer.CreatePeerForElement(button)` 返回 `ButtonAutomationPeer`，`GetName()` 为 `Confirm`、`GetAutomationId()` 为 `ok-btn`、`GetHelpText()` 为 `Confirms the form`、`GetAutomationControlType()` 为 `Button`。`AccessibilityView.Raw` 可设可读，`LiveSetting` 默认 `Off`。需要 `using Avalonia.Automation;` 与 `using Avalonia.Automation.Peers;`。
24. **日志有两套，别混**：Avalonia 自己的日志用 `AppBuilder.LogToTrace(level, areas)` / `LogToDelegate` / `LogToTextWriter` / `Logger.Sink`；应用自己的日志用 `Microsoft.Extensions.Logging`（`ILoggerFactory`、`ILogger`）。两者互不相通。本组用一个自定义 `ILoggerProvider` 把消息收进列表，实测 `LogDebug`、`LogWarning`、`LogError(ex, ...)` 依次得到 `Debug:d 1`、`Warning:w`、`Error:e`。**不依赖控制台输出**——`WinExe` 项目没有控制台窗口，`AddConsole()` 在这里看不见任何东西。

## 本 plan 的范围

spec 第二阶段分 4 组，本 plan 只实现**第四组「应用服务层」**：#12 Services、#13 AppDevelopment、#14 TestingDemo、#15 TestingDemo.Tests。前三组（基础层、样式绑定层、交互图形层）已交付并合并到 `main`。**这是最后一组**，Task 6 之后 spec 第二阶段全部完成。

## 功能点映射的实测修正

编写本 plan 时逐个核对了三个分类的官方侧边栏（2026-10-08 抓取），与 spec 的功能点列表有以下出入，本 plan 按实际文档结构执行：

| 分类 | 官方子页数 | spec 的列法 | 本 plan 的处理 |
|---|---|---|---|
| Services | 9 | 7 个功能点 | 8 个 Tab。spec 漏了 `insets-manager`、`activatable-lifetime` 两页，补上；`Storage` 的三个子页（`storage-provider`、`file-picker-options`、`bookmarks`）与 `file-dialogs` 合成「文件对话框」和「StorageProvider」两个 Tab；Focus Manager 做指路页（完整实现在 #9）；InputPane 与 InsetsManager 在桌面上为 `null`，合成一页「移动端服务」演示"检测服务是否可用" |
| App Development | 14 | 9 个功能点 | 10 个 Tab：依赖注入、本地化、嵌入 Web 内容（指路）、日志、未处理异常、资源、数据校验（指路）、线程模型、窗口管理、无障碍；`xaml-preview-and-design-settings`、`performance`、`native-interop`、`cross-platform-solution-setup` 不做 Tab |
| Testing | 2 | 4 个功能点 | `headless-xunit` 与 `setting-up-the-headless-platform` 合并成 #15 的测试类；`ui-testing-with-appium` 需要外部驱动与真实应用进程，无法在仓库内自洽运行，**不做**，在 #14 的 README 小节里指路 |

## File Structure

三个演示项目结构同构，均照搬 `Avalonia.EventsDemo`。下表只列每个项目**独有**的文件；`Program.cs`、`App.axaml(.cs)`、`app.manifest`、`Assets/avalonia-logo.ico`、`Views/MainWindow.axaml(.cs)`、`.csproj` 六件套每个演示项目都有一份，由 Task 1 一次生成。#15 是例外：它是测试项目，没有这六件套。

### 共享文件的改动（Task 1）

| 文件 | 改动 |
|---|---|
| `Directory.Packages.props` | 新增 6 个 `PackageVersion`（见 Tech Stack，其中 `xunit.v3` 取代 spec 写的 `xunit`） |
| `hello-avalonia.slnx` | 注册 4 个新项目 |

（下文逐项目的文件表在后续章节追加。）

### 项目 #12 `Avalonia.ServicesDemo`（8 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/ClipboardPage.axaml(.cs)` | 文本读写、`DataTransfer` 写入、清空、读回当前格式 | `services/clipboard` |
| `Views/Pages/FileDialogsPage.axaml(.cs)` | 打开、保存、选文件夹三种对话框，`FilePickerFileType` 过滤；不可用时给出提示 | `services/file-dialogs` |
| `Views/Pages/StorageProviderPage.axaml(.cs)` | `CanOpen`/`CanSave`/`CanPickFolder` 能力探测、`TryGetFolderFromPathAsync`、`TryGetWellKnownFolderAsync`、枚举文件夹内容 | `services/storage/storage-provider` |
| `Views/Pages/FocusManagerPage.axaml(.cs)` | 指路页 → `Avalonia.InputDemo` | `services/focus-manager` |
| `Views/Pages/LauncherPage.axaml(.cs)` | `LaunchUriAsync`、`LaunchDirectoryInfoAsync`，显示返回值 | `services/launcher` |
| `Views/Pages/PlatformSettingsPage.axaml(.cs)` | 系统主题、强调色、对比度、点击与双击参数、`HotkeyConfiguration`，监听 `ColorValuesChanged` | `services/platform-settings` |
| `Views/Pages/MobileServicesPage.axaml(.cs)` | `InputPane` 与 `InsetsManager`：逐项检测是否为 `null` 并说明原因 | `services/input-pane` |
| `Views/Pages/ActivatableLifetimePage.axaml(.cs)` | `TryGetFeature<IActivatableLifetime>()` 的可用性检测与平台支持表 | `services/activatable-lifetime` |

### 项目 #13 `Avalonia.AppDevelopmentDemo`（10 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/DependencyInjectionPage.axaml(.cs)` | 三种生命周期的实例对照、`ValidateScopes` 的响亮失败 | `app-development/dependency-injection` |
| `Views/Pages/LocalizationPage.axaml(.cs)` | 最小 RESX 版本：运行时切换 `zh-CN`/`en-US`，指向 MusicStore 的完整用法 | `app-development/localizing` |
| `Views/Pages/WebContentPage.axaml(.cs)` | 指路页 → `Avalonia.WebViewDemo` | `app-development/embedding-web-content` |
| `Views/Pages/LoggingPage.axaml(.cs)` | Avalonia 的 `Logger.Sink` 与应用的 `ILogger` 两套日志并排，自定义 `ILoggerProvider` 收集 | `app-development/logging-errors-and-warnings` |
| `Views/Pages/UnhandledExceptionsPage.axaml(.cs)` | `Dispatcher.UIThread.UnhandledException` 吞掉 UI 线程异常；`TaskScheduler.UnobservedTaskException` 说明 | `app-development/setting-unhandled-exceptions` |
| `Views/Pages/ResourcesPage.axaml(.cs)` | 替换字典项后，一次性查找与 `GetResourceObservable` 绑定的差别；四种查找方法的搜索范围 | `app-development/resources` |
| `Views/Pages/DataValidationPage.axaml(.cs)` | 指路页 → `Avalonia.DataBindingDemo` | `app-development/data-validation` |
| `Views/Pages/ThreadingPage.axaml(.cs)` | 后台线程写控件的失败、`Post`/`InvokeAsync`、`async/await` 回到 UI 线程 | `app-development/threading` |
| `Views/Pages/WindowManagementPage.axaml(.cs)` | `Show`/`ShowDialog` 返回值、`Owner`、`WindowState`、`Closing` 取消、`SizeToContent`、屏幕信息 | `app-development/window-management` |
| `Views/Pages/AccessibilityPage.axaml(.cs)` | `AutomationProperties` 各项、读回 `AutomationPeer`，一个自定义 Peer | `app-development/accessibility` |
| `Services/IClock.cs`、`Services/SystemClock.cs`、`Services/Counter.cs`、`Services/Greeter.cs` | DI 页的四个演示服务 | — |
| `Logging/ListLoggerProvider.cs` | 把 `ILogger` 消息收进 `ObservableCollection<string>` | — |
| `Assets/Langs/Strings.resx`、`Strings.en-US.resx` | 本地化页的两份资源 | — |

### 项目 #14 `Avalonia.TestingDemo`（3 个被测面板）

被测应用刻意设计为易于断言：每个交互控件都有 `Name` 与 `AutomationProperties.AutomationId`，状态通过可读的 `TextBlock` 暴露，无随机、无时间依赖。

| 文件 | 职责 |
|---|---|
| `Views/Pages/CounterPage.axaml(.cs)` | 计数器：加、减、清零，计数为 0 时「减」与「清零」禁用 |
| `Views/Pages/FormPage.axaml(.cs)` | 表单：姓名与年龄两个输入，校验错误显示在对应文本块里，全部通过后「提交」才启用 |
| `Views/Pages/ListPage.axaml(.cs)` | 列表：输入框加项、选中后删除、显示总数 |
| `ViewModels/CounterViewModel.cs`、`FormViewModel.cs`、`ListViewModel.cs` | 三个页面的状态与命令 |

### 项目 #15 `Avalonia.TestingDemo.Tests`（xUnit v3 + Headless）

| 文件 | 职责 |
|---|---|
| `Avalonia.TestingDemo.Tests.csproj` | `OutputType=Exe`，引用 #14 |
| `TestAppBuilder.cs` | `[assembly: AvaloniaTestApplication]` 与 `BuildAvaloniaApp`（`UseSkia` + `UseHeadless`） |
| `ControlQueryTests.cs` | 按 `Name`、`AutomationId`、类型查找控件 |
| `InteractionTests.cs` | 模拟点击、键盘、文本输入，断言状态变化 |
| `ViewModelTests.cs` | 不经过视图直接测 ViewModel（对照：快、不需要 `[AvaloniaFact]`） |
| `RenderSnapshotTests.cs` | `CaptureRenderedFrame` 读像素，断言颜色 |

---

## Task 1: 包声明与三个演示项目的骨架

**Files:**
- Modify: `Directory.Packages.props`
- Create: `Avalonia.ServicesDemo/`、`Avalonia.AppDevelopmentDemo/`、`Avalonia.TestingDemo/` 三个目录，各含 `.csproj`、`Program.cs`、`App.axaml(.cs)`、`app.manifest`、`Assets/avalonia-logo.ico`、`Views/MainWindow.axaml(.cs)`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: `avares://Avalonia.Shared/Themes/SharedStyles.axaml`（提供 `DemoHeader`、`TextBlock.caption`、`TextBlock.hint`、`Border.stage`）、`Avalonia.Shared.Helpers.EventLog`（`Entries`、`Write`、`Clear`）
- Produces:
  - 6 个包版本，供 Task 3、Task 5 的 `.csproj` 用无版本的 `PackageReference` 引用
  - 三个可运行的空壳窗口，页面命名空间分别为 `Avalonia.ServicesDemo.Views.Pages`、`Avalonia.AppDevelopmentDemo.Views.Pages`、`Avalonia.TestingDemo.Views.Pages`

- [ ] **Step 1: 声明六个新包**

在 `Directory.Packages.props` 的 `iTunesSearch` 行之后、`</ItemGroup>` 之前追加（注意是 `xunit.v3`，不是 spec 里的 `xunit`，见规则 11）：

```xml
        <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.12" />
        <PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="10.0.12" />
        <PackageVersion Include="Avalonia.Headless.XUnit" Version="12.1.2" />
        <PackageVersion Include="xunit.v3" Version="3.2.2" />
        <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
        <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
```

Run: `dotnet restore hello-avalonia.slnx 2>&1 | grep -E "error|个错误"`
Expected: 无输出（新增的包还没有项目引用，restore 不受影响）。

- [ ] **Step 2: 用脚本从 EventsDemo 派生三份骨架**

在仓库根目录执行。`sed` 把项目名与窗口标题一并替换；EventsDemo 的窗口里已有 5 个 `TabItem`，第二段 `sed -i` 把 `<TabControl>` 与 `</TabControl>` 之间的内容删光，得到空壳；两个多 Tab 的项目顺手改成竖排（Step 4 的内容在这里一并完成）。`.csproj` 随后单独创建（Step 3）：

```bash
declare -A TITLE=( [ServicesDemo]="Avalonia Services Demo" [AppDevelopmentDemo]="Avalonia App Development Demo" [TestingDemo]="Avalonia Testing Demo" )
for p in ServicesDemo AppDevelopmentDemo TestingDemo; do
  mkdir -p "Avalonia.$p/Assets" "Avalonia.$p/Views/Pages"
  cp Avalonia.EventsDemo/Assets/avalonia-logo.ico "Avalonia.$p/Assets/"
  for f in Program.cs App.axaml App.axaml.cs app.manifest Views/MainWindow.axaml Views/MainWindow.axaml.cs; do
    sed "s/Avalonia\.EventsDemo/Avalonia.$p/g; s/Avalonia Events Demo/${TITLE[$p]}/" "Avalonia.EventsDemo/$f" > "Avalonia.$p/$f"
  done
  sed -i '/<TabControl /,/<\/TabControl>/{/<TabControl /!{/<\/TabControl>/!d}}' "Avalonia.$p/Views/MainWindow.axaml"
done
sed -i 's#<TabControl Margin="12">#<TabControl Margin="12" TabStripPlacement="Left">#' Avalonia.ServicesDemo/Views/MainWindow.axaml Avalonia.AppDevelopmentDemo/Views/MainWindow.axaml
grep -rl "EventsDemo" Avalonia.ServicesDemo Avalonia.AppDevelopmentDemo Avalonia.TestingDemo || echo "no EventsDemo leftovers"
grep -c "TabItem" Avalonia.ServicesDemo/Views/MainWindow.axaml Avalonia.AppDevelopmentDemo/Views/MainWindow.axaml Avalonia.TestingDemo/Views/MainWindow.axaml
```

Expected: 第一行 `no EventsDemo leftovers`，随后三行计数都是 `:0`（三个窗口都是空的 `TabControl`）。这段脚本已在仓库外的草稿目录里实测跑通。

- [ ] **Step 3: 创建三个 .csproj**

`Avalonia.ServicesDemo/Avalonia.ServicesDemo.csproj`（与 EventsDemo 逐字相同，无新增包）：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <AvaloniaResource Include="Assets\**" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="Avalonia.Fonts.Inter" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

`Avalonia.TestingDemo/Avalonia.TestingDemo.csproj` 与上面逐字相同。

`Avalonia.AppDevelopmentDemo/Avalonia.AppDevelopmentDemo.csproj` 多两个包，并把 `.resx` 排除出 Avalonia 资源（否则会被当成 Avalonia 资源打包，运行时 `ResourceManager` 找不到，与 MusicStore 同理）：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <AvaloniaResource Include="Assets\**" />
        <AvaloniaResource Remove="Assets\Langs\*.resx" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="Avalonia.Fonts.Inter" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
        <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
        <PackageReference Include="Microsoft.Extensions.Logging.Console" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

`Microsoft.Extensions.Logging.Console` 在这里的作用是**传递引入 `Microsoft.Extensions.Logging`**（`ILoggerFactory`、`AddLogging` 都在里面）；`WinExe` 没有控制台窗口，所以 `AddConsole()` 本身看不到输出（规则 24）。

- [ ] **Step 4: 确认两个多 Tab 项目已竖排**

Step 2 的脚本已经把 `Avalonia.ServicesDemo`（8 Tab）与 `Avalonia.AppDevelopmentDemo`（10 Tab）的 `TabControl` 改成竖排，`Avalonia.TestingDemo`（3 Tab）保持横排。核对：

Run: `grep -c 'TabStripPlacement="Left"' Avalonia.ServicesDemo/Views/MainWindow.axaml Avalonia.AppDevelopmentDemo/Views/MainWindow.axaml Avalonia.TestingDemo/Views/MainWindow.axaml`
Expected: `:1`、`:1`、`:0`。

- [ ] **Step 5: 注册到解决方案**

修改 `hello-avalonia.slnx`，按字母序插入三行（`Avalonia.TestingDemo.Tests` 在 Task 5 才创建，到时再加）：

```xml
<Solution>
  <Project Path="Avalonia.AppDevelopmentDemo/Avalonia.AppDevelopmentDemo.csproj" />
  <Project Path="Avalonia.CustomControlsDemo/Avalonia.CustomControlsDemo.csproj" />
  <Project Path="Avalonia.DataBindingDemo/Avalonia.DataBindingDemo.csproj" />
  <Project Path="Avalonia.DataTemplatesDemo/Avalonia.DataTemplatesDemo.csproj" />
  <Project Path="Avalonia.EventsDemo/Avalonia.EventsDemo.csproj" />
  <Project Path="Avalonia.FundamentalsDemo/Avalonia.FundamentalsDemo.csproj" />
  <Project Path="Avalonia.GraphicsDemo/Avalonia.GraphicsDemo.csproj" />
  <Project Path="Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj" />
  <Project Path="Avalonia.InputDemo/Avalonia.InputDemo.csproj" />
  <Project Path="Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj" />
  <Project Path="Avalonia.MusicStore/Avalonia.MusicStore.csproj" />
  <Project Path="Avalonia.PropertySystemDemo/Avalonia.PropertySystemDemo.csproj" />
  <Project Path="Avalonia.ServicesDemo/Avalonia.ServicesDemo.csproj" />
  <Project Path="Avalonia.Shared/Avalonia.Shared.csproj" />
  <Project Path="Avalonia.StylingDemo/Avalonia.StylingDemo.csproj" />
  <Project Path="Avalonia.TestingDemo/Avalonia.TestingDemo.csproj" />
  <Project Path="Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj" />
  <Project Path="Avalonia.XamlDemo/Avalonia.XamlDemo.csproj" />
</Solution>
```

- [ ] **Step 6: 构建验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "ServicesDemo|AppDevelopmentDemo|TestingDemo" | grep -v MSB3884`
Expected: 无输出。

- [ ] **Step 7: 提交**

提交信息用 `-m` 两段（避免 heredoc 嵌套）：

```bash
git add Directory.Packages.props Avalonia.ServicesDemo/ Avalonia.AppDevelopmentDemo/ Avalonia.TestingDemo/ hello-avalonia.slnx
git commit -m "feat: scaffold the three app-services demo projects" -m "Empty TabControl shells wired to the shared styles, derived from the EventsDemo template. Also declares the six new packages in one place.

The test framework package is xunit.v3, not xunit as the spec listed: Avalonia.Headless.XUnit 12.1.2 depends on xunit v3, and mixing it with xunit 2.9.3 fails with CS0433 on every attribute that exists in both.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---


---

## 执行记录（2026-10-08）

本 plan 的 Task 2–6 在执行时由实际落地的代码代替了逐段文字：#12 的 8 个页面、#13 的 10 个页面与服务/日志/资源文件、#14 的三个面板与 ViewModel、#15 的测试项目，均已按上文的文件结构与规则 11–24 落地，源码以仓库为准（`Avalonia.ServicesDemo/`、`Avalonia.AppDevelopmentDemo/`、`Avalonia.TestingDemo/`、`Avalonia.TestingDemo.Tests/`）。

验证结果：#12 探针 36 条、#13 探针 44 条全部通过；`dotnet test` 24 个测试全部通过；整个解决方案 0 错误。README 与 spec 的写回见提交 `docs: register the app-services demos and record what they measured`。

执行时对 plan 的更正：`TextBox.Watermark` 已过时，改用 `PlaceholderText`；渲染快照读像素时按帧的 `Format` 判断字节序；本地化页的 `RadioButton` 按 `sender` 判断；数据校验路标的目标 Tab 名为「校验」。
