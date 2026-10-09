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

- [x] **Step 1: 声明六个新包**

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

- [x] **Step 2: 用脚本从 EventsDemo 派生三份骨架**

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

- [x] **Step 3: 创建三个 .csproj**

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

- [x] **Step 4: 确认两个多 Tab 项目已竖排**

Step 2 的脚本已经把 `Avalonia.ServicesDemo`（8 Tab）与 `Avalonia.AppDevelopmentDemo`（10 Tab）的 `TabControl` 改成竖排，`Avalonia.TestingDemo`（3 Tab）保持横排。核对：

Run: `grep -c 'TabStripPlacement="Left"' Avalonia.ServicesDemo/Views/MainWindow.axaml Avalonia.AppDevelopmentDemo/Views/MainWindow.axaml Avalonia.TestingDemo/Views/MainWindow.axaml`
Expected: `:1`、`:1`、`:0`。

- [x] **Step 5: 注册到解决方案**

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

- [x] **Step 6: 构建验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "ServicesDemo|AppDevelopmentDemo|TestingDemo" | grep -v MSB3884`
Expected: 无输出。

- [x] **Step 7: 提交**

提交信息用 `-m` 两段（避免 heredoc 嵌套）：

```bash
git add Directory.Packages.props Avalonia.ServicesDemo/ Avalonia.AppDevelopmentDemo/ Avalonia.TestingDemo/ hello-avalonia.slnx
git commit -m "feat: scaffold the three app-services demo projects" -m "Empty TabControl shells wired to the shared styles, derived from the EventsDemo template. Also declares the six new packages in one place.

The test framework package is xunit.v3, not xunit as the spec listed: Avalonia.Headless.XUnit 12.1.2 depends on xunit v3, and mixing it with xunit 2.9.3 fails with CS0433 on every attribute that exists in both.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---



## Task 2: Avalonia.ServicesDemo（8 个页面）

**Files:**
- Modify: `Avalonia.ServicesDemo/Views/MainWindow.axaml`
- Create: `Avalonia.ServicesDemo/Views/Pages/` 下 8 个页面各一对 `.axaml` / `.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的空壳窗口；`shared:DemoHeader`（`Title`、`DocPath`）；样式 `caption`、`hint`、`stage`
- Produces: 页面类 `ClipboardPage`、`FileDialogsPage`、`StorageProviderPage`、`FocusManagerPage`、`LauncherPage`、`PlatformSettingsPage`、`MobileServicesPage`、`ActivatableLifetimePage`，命名空间 `Avalonia.ServicesDemo.Views.Pages`。探针按下表的 `Name` 读回状态

**探针依赖的元素名：** Clipboard：`Source`、`SetTextButton`、`ReadButton`、`ClearButton`、`SetDataButton`、`FormatsButton`、`Result`；FileDialogs：`OpenButton`、`SaveButton`、`FolderButton`、`Result`、`FileText`；StorageProvider：`Capabilities`、`FolderBox`、`PathBox`、`PathButton`、`ListButton`、`Result`；Launcher：`UriBox`、`UriButton`、`Result`；PlatformSettings：`ThemeLine`、`AccentSwatch`、`DoubleTapLine`、`HoldLine`、`CopyLine`；MobileServices：`InputPaneLine`、`InsetsLine`；ActivatableLifetime：`AvailabilityLine`、`WindowLine`、`ActivationCounts`。

- [x] **Step 1: 写 MainWindow 的 8 个 Tab**

把 `Avalonia.ServicesDemo/Views/MainWindow.axaml` 的 `<TabControl>` 换成下面的内容（`Window` 根元素保持 Task 1 生成的样子，需有 `xmlns:pages="using:Avalonia.ServicesDemo.Views.Pages"`）：

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
        <TabItem Header="剪贴板"><pages:ClipboardPage /></TabItem>
        <TabItem Header="文件对话框"><pages:FileDialogsPage /></TabItem>
        <TabItem Header="StorageProvider"><pages:StorageProviderPage /></TabItem>
        <TabItem Header="焦点管理"><pages:FocusManagerPage /></TabItem>
        <TabItem Header="Launcher"><pages:LauncherPage /></TabItem>
        <TabItem Header="平台设置"><pages:PlatformSettingsPage /></TabItem>
        <TabItem Header="移动端服务"><pages:MobileServicesPage /></TabItem>
        <TabItem Header="可激活生命周期"><pages:ActivatableLifetimePage /></TabItem>
    </TabControl>
```

- [x] **Step 2: 写 8 个页面**

以下源码就是落地并通过探针的版本，逐字照抄。

**`Avalonia.ServicesDemo/Views/Pages/ClipboardPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.ClipboardPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="剪贴板：读写文本、用 DataTransfer 写入、清空，并看当前有哪些格式"
                               DocPath="services/clipboard" />

            <TextBlock Classes="caption" Text="1. 写入文本" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBox Name="Source" Width="260" Text="来自 Avalonia 的文本" />
                <Button Name="SetTextButton" Content="SetTextAsync" Click="OnSetText" />
                <Button Name="SetDataButton" Content="SetDataAsync (DataTransfer)" Click="OnSetData" />
                <Button Name="ClearButton" Content="ClearAsync" Click="OnClear" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 读回" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ReadButton" Content="TryGetTextAsync" Click="OnRead" />
                <Button Name="FormatsButton" Content="列出格式 (TryGetDataAsync)" Click="OnFormats" />
            </StackPanel>
            <Border Classes="stage" Padding="10" Margin="0,8,0,0">
                <TextBlock Name="Result" Text="（还没有读取）" TextWrapping="Wrap" />
            </Border>

            <TextBlock Classes="caption" Text="3. 粘贴测试" />
            <TextBox Name="PasteTarget" Width="360" HorizontalAlignment="Left"
                     PlaceholderText="在这里按 Ctrl+V 粘贴上面写入的内容" />

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="12.x 的剪贴板不再使用 IDataObject / DataObject / DataFormats，改用 DataTransfer、DataTransferItem。TryGetDataAsync 返回的对象要 using 释放；SetDataAsync 之后所有权交给 Avalonia，不要自己释放。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/ClipboardPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using System.Linq;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class ClipboardPage : UserControl
    {
        public ClipboardPage()
        {
            InitializeComponent();
        }

        // The clipboard hangs off the TopLevel, and is null until the control is attached.
        private IClipboard? Clipboard => TopLevel.GetTopLevel(this)?.Clipboard;

        private async void OnSetText(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                Result.Text = "当前平台没有剪贴板服务";
                return;
            }

            await clipboard.SetTextAsync(Source.Text ?? "");
            Result.Text = "已用 SetTextAsync 写入";
        }

        private async void OnSetData(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                return;
            }

            // Ownership passes to Avalonia on SetDataAsync, so the DataTransfer is not disposed here.
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(Source.Text ?? ""));
            await clipboard.SetDataAsync(data);
            Result.Text = "已用 SetDataAsync(DataTransfer) 写入";
        }

        private async void OnClear(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is { } clipboard)
            {
                await clipboard.ClearAsync();
                Result.Text = "已清空";
            }
        }

        private async void OnRead(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                return;
            }

            var text = await clipboard.TryGetTextAsync();
            Result.Text = text is null ? "TryGetTextAsync 返回 null（剪贴板里没有文本）" : $"TryGetTextAsync = \"{text}\"";
        }

        private async void OnFormats(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                return;
            }

            // The returned transfer is disposable; call TryGetDataAsync once rather than chaining extension methods.
            using var data = await clipboard.TryGetDataAsync();
            Result.Text = data is null
                ? "TryGetDataAsync 返回 null（剪贴板为空）"
                : "格式：" + string.Join("、", data.Formats.Select(f => f.Identifier));
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/FileDialogsPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.FileDialogsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="文件对话框：打开、保存、选文件夹，由 StorageProvider 统一提供"
                               DocPath="services/file-dialogs" />

            <TextBlock Classes="caption" Text="1. 三种对话框" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="OpenButton" Content="打开文本文件" Click="OnOpen" />
                <Button Name="SaveButton" Content="保存文本文件" Click="OnSave" />
                <Button Name="FolderButton" Content="选择文件夹" Click="OnFolder" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 结果" />
            <Border Classes="stage" Padding="10">
                <TextBlock Name="Result" Text="（还没有操作）" TextWrapping="Wrap" />
            </Border>

            <TextBlock Classes="caption" Text="3. 文件内容（打开后显示）" />
            <TextBox Name="FileText" Height="140" AcceptsReturn="True" IsReadOnly="True" />

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="用户取消时，打开返回空列表、保存返回 null，不抛异常——所以每次都要检查返回值。「保存」按 SaveFilePickerWithResultAsync 写，能同时拿到用户选了哪种文件类型。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/FileDialogsPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class FileDialogsPage : UserControl
    {
        private static readonly FilePickerFileType TextType = new("文本文件") { Patterns = new[] { "*.txt" } };
        private static readonly FilePickerFileType MarkdownType = new("Markdown") { Patterns = new[] { "*.md" } };

        public FileDialogsPage()
        {
            InitializeComponent();
        }

        private IStorageProvider? Storage => TopLevel.GetTopLevel(this)?.StorageProvider;

        private async void OnOpen(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { CanOpen: true } storage)
            {
                Result.Text = "当前平台的 StorageProvider.CanOpen 为 False，无法弹出打开对话框";
                return;
            }

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开文本文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { TextType, FilePickerFileTypes.All },
            });

            // Cancelling yields an empty list, not an exception.
            if (files.Count == 0)
            {
                Result.Text = "已取消（OpenFilePickerAsync 返回空列表）";
                return;
            }

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            FileText.Text = await reader.ReadToEndAsync();
            Result.Text = $"已打开：{files[0].Name}";
        }

        private async void OnSave(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { CanSave: true } storage)
            {
                Result.Text = "当前平台的 StorageProvider.CanSave 为 False，无法弹出保存对话框";
                return;
            }

            var result = await storage.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
            {
                Title = "保存文件",
                SuggestedFileName = "demo",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { TextType, MarkdownType },
            });

            if (result.File is not { } file)
            {
                Result.Text = "已取消（StorageFile 为 null）";
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("Hello from Avalonia.ServicesDemo");
            Result.Text = $"已保存：{file.Name}，用户选的类型：{result.SelectedFileType?.Name}";
        }

        private async void OnFolder(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { CanPickFolder: true } storage)
            {
                Result.Text = "当前平台的 StorageProvider.CanPickFolder 为 False，无法弹出文件夹对话框";
                return;
            }

            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择文件夹" });
            Result.Text = folders.Count == 0 ? "已取消" : $"已选择：{folders.First().Name}";
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/StorageProviderPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.StorageProviderPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="StorageProvider：能力探测、按路径取文件夹、知名文件夹、枚举内容"
                               DocPath="services/storage/storage-provider" />

            <TextBlock Classes="caption" Text="1. 能力探测" />
            <Border Classes="stage" Padding="10">
                <TextBlock Name="Capabilities" />
            </Border>

            <TextBlock Classes="caption" Text="2. 知名文件夹（WellKnownFolder）" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <ComboBox Name="FolderBox" Width="160" />
                <Button Name="ListButton" Content="列出内容" Click="OnList" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. 按路径取文件夹" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBox Name="PathBox" Width="360" />
                <Button Name="PathButton" Content="TryGetFolderFromPathAsync" Click="OnPath" />
            </StackPanel>

            <TextBlock Classes="caption" Text="4. 结果" />
            <Border Classes="stage" Padding="10">
                <TextBlock Name="Result" Text="（还没有操作）" TextWrapping="Wrap" />
            </Border>
            <ListBox Name="Items" Height="160" Margin="0,8,0,0" />

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="CanOpen / CanSave / CanPickFolder 为 False 只表示不能弹出选择器；TryGetFolderFromPathAsync、TryGetWellKnownFolderAsync 与枚举仍可使用。路径找不到时返回 null，不抛异常。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/StorageProviderPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class StorageProviderPage : UserControl
    {
        public StorageProviderPage()
        {
            InitializeComponent();

            // Enum.GetValues keeps the list honest if a folder is added upstream.
            FolderBox.ItemsSource = Enum.GetValues<WellKnownFolder>();
            FolderBox.SelectedIndex = 1;
            PathBox.Text = Path.GetTempPath();
        }

        private IStorageProvider? Storage => TopLevel.GetTopLevel(this)?.StorageProvider;

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Capabilities.Text = Storage is { } s
                ? $"CanOpen = {s.CanOpen}，CanSave = {s.CanSave}，CanPickFolder = {s.CanPickFolder}"
                : "StorageProvider 为 null";
        }

        private async void OnList(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { } storage || FolderBox.SelectedItem is not WellKnownFolder kind)
            {
                return;
            }

            var folder = await storage.TryGetWellKnownFolderAsync(kind);
            await Show(folder, $"WellKnownFolder.{kind}");
        }

        private async void OnPath(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { } storage)
            {
                return;
            }

            // The string overload is an extension method meant for desktop; the core API takes a Uri.
            var folder = await storage.TryGetFolderFromPathAsync(PathBox.Text ?? "");
            await Show(folder, "TryGetFolderFromPathAsync");
        }

        private async System.Threading.Tasks.Task Show(IStorageFolder? folder, string source)
        {
            Items.ItemsSource = null;
            if (folder is null)
            {
                Result.Text = $"{source} 返回 null（路径不存在，或平台不提供）";
                return;
            }

            var names = new List<string>();
            await foreach (var item in folder.GetItemsAsync())
            {
                names.Add((item is IStorageFolder ? "[夹] " : "[文件] ") + item.Name);
                if (names.Count >= 50)
                {
                    break;
                }
            }

            Items.ItemsSource = names;
            Result.Text = $"{source} → {folder.Name}，前 {names.Count} 项";
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/FocusManagerPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.FocusManagerPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="Focus Manager：完整演示在 Avalonia.InputDemo"
                           DocPath="services/focus-manager" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="FocusManager 通过 TopLevel.FocusManager 取得，既是一个服务，也是输入交互的一部分。官方文档把它同时列在 Services 和 Input &amp; Interaction 下，完整演示放在更贴合的 Input 项目里。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.InputDemo   →  「焦点」标签页" />
                <TextBlock Classes="hint"
                           Text="那里演示 GetFocusedElement、Tab 顺序、IsTabStop、NavigationMethod 与 :focus-visible 的区别。" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/FocusManagerPage.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class FocusManagerPage : UserControl
    {
        public FocusManagerPage()
        {
            InitializeComponent();
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/LauncherPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.LauncherPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Launcher：用系统默认程序打开网址、文件与文件夹"
                               DocPath="services/launcher" />

            <TextBlock Classes="caption" Text="1. 打开网址" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBox Name="UriBox" Width="320" Text="https://avaloniaui.net" />
                <Button Name="UriButton" Content="LaunchUriAsync" Click="OnUri" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 打开文件夹（仅桌面，非沙箱）" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBox Name="DirBox" Width="320" />
                <Button Name="DirButton" Content="LaunchDirectoryInfoAsync" Click="OnDirectory" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. 结果" />
            <Border Classes="stage" Padding="10">
                <TextBlock Name="Result" Text="（还没有操作）" TextWrapping="Wrap" />
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="返回 true 只表示系统接受了请求，不保证真的有程序被打开；返回 false 常见于没有可用的启动器（例如无头环境）。LaunchFileAsync 接收 IStorageItem，来自 StorageProvider 或剪贴板；LaunchFileInfoAsync / LaunchDirectoryInfoAsync 是直接收 FileInfo / DirectoryInfo 的扩展方法，只在 Windows、macOS、Linux 上可用。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/LauncherPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class LauncherPage : UserControl
    {
        public LauncherPage()
        {
            InitializeComponent();
            DirBox.Text = Path.GetTempPath();
        }

        private ILauncher? Launcher => TopLevel.GetTopLevel(this)?.Launcher;

        private async void OnUri(object? sender, RoutedEventArgs e)
        {
            if (Launcher is not { } launcher)
            {
                Result.Text = "Launcher 为 null";
                return;
            }

            if (!Uri.TryCreate(UriBox.Text, UriKind.Absolute, out var uri))
            {
                Result.Text = "不是合法的绝对 URI";
                return;
            }

            var accepted = await launcher.LaunchUriAsync(uri);
            Result.Text = $"LaunchUriAsync 返回 {accepted}";
        }

        private async void OnDirectory(object? sender, RoutedEventArgs e)
        {
            if (Launcher is not { } launcher)
            {
                Result.Text = "Launcher 为 null";
                return;
            }

            var accepted = await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(DirBox.Text ?? ""));
            Result.Text = $"LaunchDirectoryInfoAsync 返回 {accepted}";
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/PlatformSettingsPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.PlatformSettingsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="PlatformSettings：读取系统主题、强调色、点击参数与快捷键配置"
                               DocPath="services/platform-settings" />

            <TextBlock Classes="caption" Text="1. 颜色值（GetColorValues）" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Name="ThemeLine" />
                    <TextBlock Name="ContrastLine" />
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Text="强调色" VerticalAlignment="Center" />
                        <Border Name="AccentSwatch" Width="28" Height="28" CornerRadius="4" />
                        <TextBlock Name="AccentLine" VerticalAlignment="Center" />
                    </StackPanel>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 系统变化事件（ColorValuesChanged）" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="ChangeLine" Text="在系统设置里切换深浅色或强调色，这里会更新" />
                    <TextBlock Classes="hint"
                               Text="这是系统级的变化，不是应用的 ThemeVariant。要让应用跟随系统，把 RequestedThemeVariant 设为 Default。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 点击与悬停参数" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="TapLine" />
                    <TextBlock Name="DoubleTapLine" />
                    <TextBlock Name="HoldLine" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="4. 快捷键配置（HotkeyConfiguration）" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="CopyLine" />
                    <TextBlock Name="PasteLine" />
                    <TextBlock Name="UndoLine" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/PlatformSettingsPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Linq;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class PlatformSettingsPage : UserControl
    {
        private IPlatformSettings? _settings;

        public PlatformSettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // GetPlatformSettings is an extension on Visual; there is no TopLevel.PlatformSettings property.
            _settings = this.GetPlatformSettings();
            if (_settings is null)
            {
                ThemeLine.Text = "GetPlatformSettings() 返回 null";
                return;
            }

            ShowColors(_settings.GetColorValues());
            _settings.ColorValuesChanged += OnColorValuesChanged;

            TapLine.Text = $"GetTapSize(Mouse) = {_settings.GetTapSize(PointerType.Mouse)}";
            DoubleTapLine.Text = $"GetDoubleTapSize(Mouse) = {_settings.GetDoubleTapSize(PointerType.Mouse)}，GetDoubleTapTime(Mouse) = {_settings.GetDoubleTapTime(PointerType.Mouse).TotalMilliseconds} ms";
            HoldLine.Text = $"HoldWaitDuration = {_settings.HoldWaitDuration.TotalMilliseconds} ms（按下到 Holding 事件的延迟）";

            var keys = _settings.HotkeyConfiguration;
            CopyLine.Text = "Copy：" + string.Join("、", keys.Copy);
            PasteLine.Text = "Paste：" + string.Join("、", keys.Paste);
            UndoLine.Text = "Undo：" + string.Join("、", keys.Undo);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            // The service outlives the page, so unsubscribe or the handler keeps this control alive.
            if (_settings is not null)
            {
                _settings.ColorValuesChanged -= OnColorValuesChanged;
            }

            base.OnUnloaded(e);
        }

        private void OnColorValuesChanged(object? sender, PlatformColorValues values)
        {
            // The event may arrive on a platform thread.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ShowColors(values);
                ChangeLine.Text = $"{DateTime.Now:HH:mm:ss} 系统颜色已变化 → {values.ThemeVariant}";
            });
        }

        private void ShowColors(PlatformColorValues values)
        {
            ThemeLine.Text = $"ThemeVariant = {values.ThemeVariant}";
            ContrastLine.Text = $"ContrastPreference = {values.ContrastPreference}";
            AccentSwatch.Background = new SolidColorBrush(values.AccentColor1);
            AccentLine.Text = values.AccentColor1.ToString();
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/MobileServicesPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.MobileServicesPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="移动端服务：InputPane 与 InsetsManager 在桌面上可能不存在，先检测再使用"
                               DocPath="services/input-pane" />

            <TextBlock Classes="caption" Text="1. 当前平台的服务可用性" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="InputPaneLine" />
                    <TextBlock Name="InsetsLine" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 为什么要检测" />
            <TextBlock Classes="hint"
                       Text="这两个服务面向触屏设备：InputPane 报告软键盘是否弹出、遮住了哪块区域；InsetsManager 报告刘海、状态栏、手势条占用的安全区。桌面平台没有对应概念，TopLevel 上这两个属性返回 null，直接 .State 或 .SafeAreaPadding 会 NullReferenceException。" />

            <TextBlock Classes="caption" Text="3. 正确的写法" />
            <Border Classes="stage" Padding="10">
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="if (TopLevel.GetTopLevel(this)?.InputPane is { } pane)&#x0a;{&#x0a;    pane.StateChanged += (s, e) =&gt; Adjust(e.EndRect);&#x0a;}" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/MobileServicesPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class MobileServicesPage : UserControl
    {
        public MobileServicesPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Both services hang off the TopLevel and are null where the platform has no equivalent.
            var topLevel = TopLevel.GetTopLevel(this);

            InputPaneLine.Text = topLevel?.InputPane is { } pane
                ? $"InputPane：可用，State = {pane.State}，OccludedRect = {pane.OccludedRect}"
                : "InputPane：null（当前平台没有软键盘服务）";

            InsetsLine.Text = topLevel?.InsetsManager is { } insets
                ? $"InsetsManager：可用，SafeAreaPadding = {insets.SafeAreaPadding}"
                : "InsetsManager：null（当前平台没有安全区服务）";
        }
    }
}
```

**`Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.ServicesDemo.Views.Pages.ActivatableLifetimePage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="可激活生命周期：应用进入/离开后台、被深链接或文件唤起，平台支持差异很大"
                               DocPath="services/activatable-lifetime" />

            <TextBlock Classes="caption" Text="1. 当前平台" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="AvailabilityLine" />
                    <TextBlock Name="WindowLine" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. ActivationKind 的四个取值与平台支持（取自官方文档）" />
            <Border Classes="stage" Padding="10">
                <Grid ColumnDefinitions="120,*,*,*,*,*,*" RowDefinitions="Auto,Auto,Auto,Auto" RowSpacing="4">
                    <TextBlock Grid.Column="1" Text="Windows" FontWeight="SemiBold" />
                    <TextBlock Grid.Column="2" Text="macOS" FontWeight="SemiBold" />
                    <TextBlock Grid.Column="3" Text="Linux" FontWeight="SemiBold" />
                    <TextBlock Grid.Column="4" Text="Browser" FontWeight="SemiBold" />
                    <TextBlock Grid.Column="5" Text="Android" FontWeight="SemiBold" />
                    <TextBlock Grid.Column="6" Text="iOS" FontWeight="SemiBold" />

                    <TextBlock Grid.Row="1" Text="Background" />
                    <TextBlock Grid.Row="1" Grid.Column="1" Text="✖" />
                    <TextBlock Grid.Row="1" Grid.Column="2" Text="✔" />
                    <TextBlock Grid.Row="1" Grid.Column="3" Text="✖" />
                    <TextBlock Grid.Row="1" Grid.Column="4" Text="✔" />
                    <TextBlock Grid.Row="1" Grid.Column="5" Text="✔" />
                    <TextBlock Grid.Row="1" Grid.Column="6" Text="✔" />

                    <TextBlock Grid.Row="2" Text="File / OpenUri" />
                    <TextBlock Grid.Row="2" Grid.Column="1" Text="✖" />
                    <TextBlock Grid.Row="2" Grid.Column="2" Text="✔" />
                    <TextBlock Grid.Row="2" Grid.Column="3" Text="✖" />
                    <TextBlock Grid.Row="2" Grid.Column="4" Text="✖" />
                    <TextBlock Grid.Row="2" Grid.Column="5" Text="✔" />
                    <TextBlock Grid.Row="2" Grid.Column="6" Text="✔" />

                    <TextBlock Grid.Row="3" Text="Reopen" />
                    <TextBlock Grid.Row="3" Grid.Column="1" Text="✖" />
                    <TextBlock Grid.Row="3" Grid.Column="2" Text="✔" />
                    <TextBlock Grid.Row="3" Grid.Column="3" Text="✖" />
                    <TextBlock Grid.Row="3" Grid.Column="4" Text="✖" />
                    <TextBlock Grid.Row="3" Grid.Column="5" Text="✖" />
                    <TextBlock Grid.Row="3" Grid.Column="6" Text="✖" />
                </Grid>
            </Border>

            <TextBlock Classes="caption" Text="3. 窗口级别的替代：Activated / Deactivated" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="ActivationCounts" />
                    <TextBlock Classes="hint"
                               Text="切到别的程序再切回来，计数会增加。这是 Window 自己的事件，所有桌面平台都有，和应用级的 IActivatableLifetime 是两回事。" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class ActivatableLifetimePage : UserControl
    {
        private int _activated;
        private int _deactivated;

        public ActivatableLifetimePage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // TryGetFeature returns null on platforms whose lifetime does not implement the service (Windows, Linux).
            var lifetime = Application.Current?.TryGetFeature<IActivatableLifetime>();
            AvailabilityLine.Text = lifetime is null
                ? "IActivatableLifetime：null（当前平台不支持，见下表）"
                : $"IActivatableLifetime：可用（{lifetime.GetType().Name}）";

            if (lifetime is not null)
            {
                lifetime.Activated += (_, args) => AvailabilityLine.Text = $"Activated：{args.Kind}";
                lifetime.Deactivated += (_, args) => AvailabilityLine.Text = $"Deactivated：{args.Kind}";
            }

            WindowLine.Text = "ActivationKind 取值：" + string.Join("、", Enum.GetNames<ActivationKind>());

            // Window-level activation exists everywhere a desktop window does.
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Activated += (_, _) => { _activated++; ShowCounts(); };
                window.Deactivated += (_, _) => { _deactivated++; ShowCounts(); };
            }

            ShowCounts();
        }

        private void ShowCounts() => ActivationCounts.Text = $"Window.Activated 触发 {_activated} 次，Window.Deactivated 触发 {_deactivated} 次";
    }
}
```

- [x] **Step 3: 构建**

Run: `dotnet build Avalonia.ServicesDemo 2>&1 | grep -E "error|个错误"`
Expected: `0 个错误`。LSP 对 `InitializeComponent` 与命名元素报的 `CS0103` 是误报，以 `dotnet build` 为准（规则 10）。

- [x] **Step 4: 跑 headless 探针（仓库外，不提交）**

在 `C:\Temp\probe-services\` 下建两个文件。csproj 要关掉中央包管理，否则 `NU1008`：


**`probe-services.csproj（路径改成实际仓库位置）`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <NoWarn>$(NoWarn);CS8321</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia.Headless" Version="12.1.2" />
    <ProjectReference Include="..\Avalonia.ServicesDemo\Avalonia.ServicesDemo.csproj" />
  </ItemGroup>
</Project>
```

**`Program.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.ServicesDemo;
using Avalonia.ServicesDemo.Views;
using Avalonia.ServicesDemo.Views.Pages;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// A sink that remembers every warning-or-worse message, so "no binding warnings" is asserted, not eyeballed.
class CaptureSink : ILogSink
{
    public List<string> Hits { get; } = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Hits.Add($"{level}/{area}: {messageTemplate}");
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) => Hits.Add($"{level}/{area}: {messageTemplate}");
}

static class Program
{
    static int _pass, _fail;

    static void Check(string name, bool ok, object? detail = null)
    {
        if (ok) { _pass++; Console.WriteLine($"PASS  {name}"); }
        else { _fail++; Console.WriteLine($"FAIL  {name}  [{detail}]"); }
    }

    static void Pump() { for (int i = 0; i < 5; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(5); } }

    static T Wait<T>(Task<T> t)
    {
        while (!t.IsCompleted) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(5); }
        return t.GetAwaiter().GetResult();
    }

    static T Find<T>(Visual root, string name) where T : Control
        => root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    [STAThread]
    static int Main()
    {
        var sink = new CaptureSink();
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
        Logger.Sink = sink;

        // ---- shell: 8 tabs in order, each selectable and rendering the right page
        var window = new MainWindow();
        window.Show();
        Pump();
        var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
        var headers = tabs.Items.OfType<TabItem>().Select(t => t.Header?.ToString()).ToArray();
        Check("shell has 8 tabs", headers.Length == 8, headers.Length);
        Check("shell tab headers in order",
            string.Join("|", headers) == "剪贴板|文件对话框|StorageProvider|焦点管理|Launcher|平台设置|移动端服务|可激活生命周期",
            string.Join("|", headers));
        var expected = new[] { typeof(ClipboardPage), typeof(FileDialogsPage), typeof(StorageProviderPage), typeof(FocusManagerPage),
                               typeof(LauncherPage), typeof(PlatformSettingsPage), typeof(MobileServicesPage), typeof(ActivatableLifetimePage) };
        for (int i = 0; i < expected.Length; i++)
        {
            tabs.SelectedIndex = i; Pump();
            var content = (tabs.Items.OfType<TabItem>().ElementAt(i)).Content;
            Check($"tab {i} content is {expected[i].Name}", content?.GetType() == expected[i], content?.GetType().Name);
        }

        // ---- clipboard page: write, read back, clear, DataTransfer
        var clip = new ClipboardPage();
        var host = new Window { Content = clip, Width = 760, Height = 560 };
        host.Show(); Pump();
        Find<TextBox>(clip, "Source").Text = "round trip";
        Find<Button>(clip, "SetTextButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Find<Button>(clip, "ReadButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("clipboard: text round trip", Find<TextBlock>(clip, "Result").Text!.Contains("round trip"), Find<TextBlock>(clip, "Result").Text);
        Find<Button>(clip, "ClearButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Find<Button>(clip, "ReadButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("clipboard: cleared reads null", Find<TextBlock>(clip, "Result").Text!.Contains("null"), Find<TextBlock>(clip, "Result").Text);
        Find<TextBox>(clip, "Source").Text = "via transfer";
        Find<Button>(clip, "SetDataButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Find<Button>(clip, "ReadButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("clipboard: DataTransfer round trip", Find<TextBlock>(clip, "Result").Text!.Contains("via transfer"), Find<TextBlock>(clip, "Result").Text);
        Find<Button>(clip, "FormatsButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("clipboard: formats lists Text", Find<TextBlock>(clip, "Result").Text!.Contains("Text"), Find<TextBlock>(clip, "Result").Text);

        // ---- file dialogs page: headless cannot open pickers, and says so instead of throwing
        var fd = new FileDialogsPage();
        var host2 = new Window { Content = fd, Width = 760, Height = 560 }; host2.Show(); Pump();
        Find<Button>(fd, "OpenButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("file dialogs: open reports CanOpen False", Find<TextBlock>(fd, "Result").Text!.Contains("CanOpen"), Find<TextBlock>(fd, "Result").Text);
        Find<Button>(fd, "SaveButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("file dialogs: save reports CanSave False", Find<TextBlock>(fd, "Result").Text!.Contains("CanSave"), Find<TextBlock>(fd, "Result").Text);
        Find<Button>(fd, "FolderButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("file dialogs: folder reports CanPickFolder False", Find<TextBlock>(fd, "Result").Text!.Contains("CanPickFolder"), Find<TextBlock>(fd, "Result").Text);

        // ---- storage provider page: capability line, well-known folder, path lookup
        var sp = new StorageProviderPage();
        var host3 = new Window { Content = sp, Width = 760, Height = 560 }; host3.Show(); Pump();
        Check("storage: capability line", Find<TextBlock>(sp, "Capabilities").Text!.Contains("CanOpen = False"), Find<TextBlock>(sp, "Capabilities").Text);
        var box = Find<ComboBox>(sp, "FolderBox");
        Check("storage: ComboBox lists 6 well-known folders", box.Items.Count == 6, box.Items.Count);
        Find<Button>(sp, "PathButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (int i = 0; i < 40 && Find<TextBlock>(sp, "Result").Text!.StartsWith("（"); i++) Pump();
        Check("storage: path lookup finds the temp folder", Find<TextBlock>(sp, "Result").Text!.Contains("TryGetFolderFromPathAsync →"), Find<TextBlock>(sp, "Result").Text);
        Find<TextBox>(sp, "PathBox").Text = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "no-such-folder-" + Guid.NewGuid());
        Find<Button>(sp, "PathButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (int i = 0; i < 40 && !Find<TextBlock>(sp, "Result").Text!.Contains("null"); i++) Pump();
        Check("storage: missing path returns null, no throw", Find<TextBlock>(sp, "Result").Text!.Contains("返回 null"), Find<TextBlock>(sp, "Result").Text);
        box.SelectedItem = WellKnownFolder.Documents;
        Find<Button>(sp, "ListButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (int i = 0; i < 40 && !Find<TextBlock>(sp, "Result").Text!.Contains("WellKnownFolder"); i++) Pump();
        Check("storage: well-known Documents resolves", Find<TextBlock>(sp, "Result").Text!.Contains("WellKnownFolder.Documents"), Find<TextBlock>(sp, "Result").Text);

        // ---- launcher page: headless launcher declines, page reports False
        var lp = new LauncherPage();
        var host4 = new Window { Content = lp, Width = 760, Height = 560 }; host4.Show(); Pump();
        Find<Button>(lp, "UriButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (int i = 0; i < 40 && Find<TextBlock>(lp, "Result").Text!.StartsWith("（"); i++) Pump();
        Check("launcher: uri returns False in headless", Find<TextBlock>(lp, "Result").Text == "LaunchUriAsync 返回 False", Find<TextBlock>(lp, "Result").Text);
        Find<TextBox>(lp, "UriBox").Text = "not a uri";
        Find<Button>(lp, "UriButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
        Check("launcher: invalid uri is rejected before launching", Find<TextBlock>(lp, "Result").Text!.Contains("不是合法"), Find<TextBlock>(lp, "Result").Text);

        // ---- platform settings page
        var ps = new PlatformSettingsPage();
        var host5 = new Window { Content = ps, Width = 760, Height = 700 }; host5.Show(); Pump();
        Check("platform: theme line", Find<TextBlock>(ps, "ThemeLine").Text == "ThemeVariant = Light", Find<TextBlock>(ps, "ThemeLine").Text);
        Check("platform: accent swatch painted", Find<Border>(ps, "AccentSwatch").Background is SolidColorBrush, Find<Border>(ps, "AccentSwatch").Background);
        Check("platform: double tap time 500 ms", Find<TextBlock>(ps, "DoubleTapLine").Text!.Contains("500 ms"), Find<TextBlock>(ps, "DoubleTapLine").Text);
        Check("platform: hold wait 300 ms", Find<TextBlock>(ps, "HoldLine").Text!.Contains("300 ms"), Find<TextBlock>(ps, "HoldLine").Text);
        Check("platform: copy gesture lists Ctrl+C", Find<TextBlock>(ps, "CopyLine").Text!.Contains("Ctrl+C"), Find<TextBlock>(ps, "CopyLine").Text);
        host5.Content = null; Pump();

        // ---- mobile services page: both null on a desktop-style headless platform
        var ms = new MobileServicesPage();
        var host6 = new Window { Content = ms, Width = 760, Height = 560 }; host6.Show(); Pump();
        Check("mobile: InputPane reported null", Find<TextBlock>(ms, "InputPaneLine").Text!.Contains("null"), Find<TextBlock>(ms, "InputPaneLine").Text);
        Check("mobile: InsetsManager reported null", Find<TextBlock>(ms, "InsetsLine").Text!.Contains("null"), Find<TextBlock>(ms, "InsetsLine").Text);

        // ---- activatable lifetime page
        var al = new ActivatableLifetimePage();
        var host7 = new Window { Content = al, Width = 760, Height = 700 }; host7.Show(); Pump();
        Check("activatable: reported unavailable", Find<TextBlock>(al, "AvailabilityLine").Text!.Contains("null"), Find<TextBlock>(al, "AvailabilityLine").Text);
        Check("activatable: lists the four ActivationKind values", Find<TextBlock>(al, "WindowLine").Text!.Contains("Reopen") && Find<TextBlock>(al, "WindowLine").Text!.Contains("OpenUri"), Find<TextBlock>(al, "WindowLine").Text);
        Check("activatable: counters rendered", Find<TextBlock>(al, "ActivationCounts").Text!.StartsWith("Window.Activated"), Find<TextBlock>(al, "ActivationCounts").Text);

        // ---- focus manager signpost
        var fm = new FocusManagerPage();
        var host8 = new Window { Content = fm, Width = 760, Height = 560 }; host8.Show(); Pump();
        Check("signpost: points at InputDemo", fm.GetVisualDescendants().OfType<SelectableTextBlock>().Any(t => t.Text!.Contains("Avalonia.InputDemo")), "");

        // ---- nothing above should have produced a binding/layout warning
        Check("no warning-or-worse log entries", sink.Hits.Count == 0, string.Join(" || ", sink.Hits));

        Console.WriteLine($"{_pass} passed, {_fail} failed");
        return _fail == 0 ? 0 : 1;
    }
}
```

Run: `cd C:/Temp/probe-services && dotnet run 2>&1 | tail -45`
Expected: 末行 `36 passed, 0 failed`。注意探针里 `ProjectReference` 要指向仓库里的 `Avalonia.ServicesDemo.csproj`。

- [x] **Step 5: 提交**

```bash
git add Avalonia.ServicesDemo/
git commit -m "feat: demonstrate the Services category" -m "Eight pages: clipboard, file dialogs, StorageProvider, a focus-manager signpost, launcher, platform settings, and availability checks for InputPane, InsetsManager and IActivatableLifetime. Headless has no-op storage and launcher services, so these pages report what is unavailable instead of throwing.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 3: Avalonia.AppDevelopmentDemo（10 个页面）

**Files:**
- Modify: `Avalonia.AppDevelopmentDemo/Avalonia.AppDevelopmentDemo.csproj`（Task 1 已建，此处无需再改）、`Views/MainWindow.axaml`
- Create: `Services/IClock.cs`、`Services/SystemClock.cs`、`Services/Counter.cs`、`Services/Greeter.cs`、`Logging/ListLoggerProvider.cs`、`Assets/Langs/Strings.resx`、`Assets/Langs/Strings.en-US.resx`、`Views/Pages/` 下 10 个页面各一对文件

**Interfaces:**
- Consumes: Task 1 的空壳与 csproj（含 DI 与 Logging 包、`.resx` 排除）
- Produces: `IClock { DateTime Now }`、`SystemClock : IClock`、`Counter { int Id }`（每个实例取下一个递增号）、`Greeter(IClock)` 的 `Greet(string)`、`ListLoggerProvider.Entries`（`ObservableCollection<string>`，条目格式 `Level:message`）

**本任务要避开的两个坑（探针当场抓到的）：**
- `RadioButton.IsCheckedChanged` 触发时，同组另一个按钮还没取消选中——要按 `sender` 判断，不能读兄弟按钮的状态。
- `Logger.Sink` 是进程级的：页面在 `OnLoaded` 记下旧 sink、换成自己的，`OnUnloaded` 再还原。

- [x] **Step 1: 写服务、日志提供者与两份资源**

**`Avalonia.AppDevelopmentDemo/Services/IClock.cs`**

```csharp
using System;

namespace Avalonia.AppDevelopmentDemo.Services
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Services/SystemClock.cs`**

```csharp
using System;

namespace Avalonia.AppDevelopmentDemo.Services
{
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
```

**`Avalonia.AppDevelopmentDemo/Services/Counter.cs`**

```csharp
using System.Threading;

namespace Avalonia.AppDevelopmentDemo.Services
{
    // Every instance takes the next number, so the id shows which instance a resolve returned.
    public class Counter
    {
        private static int _next;

        public int Id { get; } = Interlocked.Increment(ref _next);
    }
}
```

**`Avalonia.AppDevelopmentDemo/Services/Greeter.cs`**

```csharp
namespace Avalonia.AppDevelopmentDemo.Services
{
    // Takes IClock in the constructor: the container supplies it, nothing here calls new.
    public class Greeter
    {
        private readonly IClock _clock;

        public Greeter(IClock clock) => _clock = clock;

        public string Greet(string name) => $"你好，{name}。现在是 {_clock.Now:HH:mm:ss}";
    }
}
```

**`Avalonia.AppDevelopmentDemo/Logging/ListLoggerProvider.cs`**

```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;

namespace Avalonia.AppDevelopmentDemo.Logging
{
    // Collects ILogger output into a list the UI can show; WinExe has no console to read.
    public sealed class ListLoggerProvider : ILoggerProvider
    {
        public ObservableCollection<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

        public void Dispose() { }

        private sealed class ListLogger : ILogger
        {
            private readonly ObservableCollection<string> _entries;

            public ListLogger(ObservableCollection<string> entries) => _entries = entries;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => _entries.Add($"{logLevel}:{formatter(state, exception)}");
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Assets/Langs/Strings.resx`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="Greeting" xml:space="preserve"><value>你好，世界</value></data>
  <data name="Farewell" xml:space="preserve"><value>再见</value></data>
</root>
```

**`Avalonia.AppDevelopmentDemo/Assets/Langs/Strings.en-US.resx`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="Greeting" xml:space="preserve"><value>Hello, world</value></data>
  <data name="Farewell" xml:space="preserve"><value>Goodbye</value></data>
</root>
```

- [x] **Step 2: 写 MainWindow 的 10 个 Tab**

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
        <TabItem Header="依赖注入"><pages:DependencyInjectionPage /></TabItem>
        <TabItem Header="本地化"><pages:LocalizationPage /></TabItem>
        <TabItem Header="Web 内容"><pages:WebContentPage /></TabItem>
        <TabItem Header="日志"><pages:LoggingPage /></TabItem>
        <TabItem Header="未处理异常"><pages:UnhandledExceptionsPage /></TabItem>
        <TabItem Header="资源"><pages:ResourcesPage /></TabItem>
        <TabItem Header="数据校验"><pages:DataValidationPage /></TabItem>
        <TabItem Header="线程模型"><pages:ThreadingPage /></TabItem>
        <TabItem Header="窗口管理"><pages:WindowManagementPage /></TabItem>
        <TabItem Header="无障碍"><pages:AccessibilityPage /></TabItem>
    </TabControl>
```

窗口根元素需有 `xmlns:pages="using:Avalonia.AppDevelopmentDemo.Views.Pages"`。

- [x] **Step 3: 写 10 个页面**

以下源码就是落地并通过探针的版本，逐字照抄。`DataValidationPage` 的路标要写「校验」标签页（`Avalonia.DataBindingDemo` 里该 Tab 叫「校验」）。

**`Avalonia.AppDevelopmentDemo/Views/Pages/DependencyInjectionPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.DependencyInjectionPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="依赖注入：三种生命周期，以及作用域校验的响亮失败"
                               DocPath="app-development/dependency-injection" />

            <TextBlock Classes="caption" Text="1. 同一作用域里解析两次" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ResolveButton" Content="解析" Click="OnResolve" />
            </StackPanel>
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="TransientLine" Text="Transient：（未解析）" />
                    <TextBlock Name="ScopedLine" Text="Scoped：（未解析）" />
                    <TextBlock Name="SingletonLine" Text="Singleton：（未解析）" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 构造函数注入" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="GreetButton" Content="Greeter 问候" Click="OnGreet" />
            </StackPanel>
            <Border Classes="stage" Padding="10">
                <TextBlock Name="GreetLine" Text="（未调用）" />
            </Border>

            <TextBlock Classes="caption" Text="3. 从根容器解析 Scoped 服务" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="RootScopedButton" Content="从根容器解析 Scoped" Click="OnRootScoped" />
            </StackPanel>
            <Border Classes="stage" Padding="10">
                <TextBlock Name="ScopeErrorLine" Text="（未调用）" TextWrapping="Wrap" />
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="每个 Counter 实例带一个递增的 Id，所以 Id 相同就是同一个实例。ValidateScopes = true 时，从根容器拿 Scoped 服务会立刻抛 InvalidOperationException，而不是悄悄把它当成单例用。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/DependencyInjectionPage.axaml.cs`**

```csharp
using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class DependencyInjectionPage : UserControl
    {
        private readonly ServiceProvider _root;

        public DependencyInjectionPage()
        {
            InitializeComponent();

            var services = new ServiceCollection();
            services.AddSingleton<IClock, SystemClock>();
            services.AddTransient<Greeter>();
            services.AddKeyedTransient<Counter>("transient");
            services.AddKeyedScoped<Counter>("scoped");
            services.AddKeyedSingleton<Counter>("singleton");

            // ValidateScopes makes a scoped service resolved from the root fail loudly.
            _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        private void OnResolve(object? sender, RoutedEventArgs e)
        {
            using var scope = _root.CreateScope();
            var sp = scope.ServiceProvider;

            TransientLine.Text = Describe("Transient", sp.GetRequiredKeyedService<Counter>("transient"), sp.GetRequiredKeyedService<Counter>("transient"));
            ScopedLine.Text = Describe("Scoped", sp.GetRequiredKeyedService<Counter>("scoped"), sp.GetRequiredKeyedService<Counter>("scoped"));
            SingletonLine.Text = Describe("Singleton", sp.GetRequiredKeyedService<Counter>("singleton"), sp.GetRequiredKeyedService<Counter>("singleton"));
        }

        private static string Describe(string kind, Counter a, Counter b)
            => $"{kind}：第一次 Id = {a.Id}，第二次 Id = {b.Id}，{(ReferenceEquals(a, b) ? "同一个实例" : "不同实例")}";

        private void OnGreet(object? sender, RoutedEventArgs e)
        {
            GreetLine.Text = _root.GetRequiredService<Greeter>().Greet("Avalonia");
        }

        private void OnRootScoped(object? sender, RoutedEventArgs e)
        {
            try
            {
                var counter = _root.GetRequiredKeyedService<Counter>("scoped");
                ScopeErrorLine.Text = $"没有抛异常（Id = {counter.Id}）";
            }
            catch (InvalidOperationException ex)
            {
                ScopeErrorLine.Text = $"InvalidOperationException：{ex.Message}";
            }
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/LocalizationPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.LocalizationPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="本地化：RESX 资源，运行时切换语言"
                           DocPath="app-development/localizing" />

        <TextBlock Classes="caption" Text="1. 语言" />
        <StackPanel Orientation="Horizontal" Spacing="8">
            <RadioButton Name="ChineseRadio" GroupName="Lang" Content="zh-CN（默认资源）" IsChecked="True" IsCheckedChanged="OnLangChanged" />
            <RadioButton Name="EnglishRadio" GroupName="Lang" Content="en-US" IsCheckedChanged="OnLangChanged" />
        </StackPanel>

        <TextBlock Classes="caption" Text="2. 从资源读出的文字" />
        <Border Classes="stage" Padding="10">
            <StackPanel Spacing="4">
                <TextBlock Name="GreetingText" FontSize="20" />
                <TextBlock Name="FarewellText" />
                <TextBlock Name="CultureText" Classes="hint" />
            </StackPanel>
        </Border>

        <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                   Text="这里是最小版本：ResourceManager.GetString(key, culture) 直接按指定文化取值，不改线程文化。把 .resx 排除出 AvaloniaResource（见 csproj），否则运行时找不到。完整用法（x:Static 绑定、资源类生成）在 Avalonia.MusicStore。" />
    </StackPanel>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/LocalizationPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class LocalizationPage : UserControl
    {
        // The manifest name is the assembly's root namespace + folder path + file name.
        private static readonly ResourceManager Strings =
            new("Avalonia.AppDevelopmentDemo.Assets.Langs.Strings", Assembly.GetExecutingAssembly());

        public LocalizationPage()
        {
            InitializeComponent();
            Apply(new CultureInfo("zh-CN"));
        }

        private void OnLangChanged(object? sender, RoutedEventArgs e)
        {
            // Decide from the sender, not from the sibling's state: the sibling is unchecked only after this event.
            if (sender is RadioButton { IsChecked: true } radio)
                Apply(new CultureInfo(ReferenceEquals(radio, EnglishRadio) ? "en-US" : "zh-CN"));
        }

        private void Apply(CultureInfo culture)
        {
            GreetingText.Text = Strings.GetString("Greeting", culture);
            FarewellText.Text = Strings.GetString("Farewell", culture);
            CultureText.Text = $"culture = {culture.Name}";
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/WebContentPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.WebContentPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="嵌入 Web 内容：完整演示在 Avalonia.WebViewDemo"
                           DocPath="app-development/embedding-web-content" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="Avalonia 12 的 Web 内容由 Avalonia.Controls.WebView 提供（NativeWebView 与 WebView 控件）。它依赖各平台的原生浏览器组件，不适合放进一个要保持轻量的页面标签里，所以完整演示放在单独的项目。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.WebViewDemo" />
                <TextBlock Classes="hint"
                           Text="另有 Avalonia.HtmlRendererDemo：不用浏览器，直接把 HTML 子集渲染成 Avalonia 控件。" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/WebContentPage.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class WebContentPage : UserControl
    {
        public WebContentPage()
        {
            InitializeComponent();
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/LoggingPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.LoggingPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="日志：Avalonia 自己的日志与应用的 ILogger，是两套"
                               DocPath="app-development/logging-errors-and-warnings" />

            <TextBlock Classes="caption" Text="1. 应用日志（Microsoft.Extensions.Logging）" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="AppLogButton" Content="写 Debug / Warning / Error" Click="OnAppLog" />
                <Button Name="AppClearButton" Content="清空" Click="OnAppClear" />
            </StackPanel>
            <Border Classes="stage" Padding="10" MinHeight="80">
                <ItemsControl Name="AppEntries" />
            </Border>

            <TextBlock Classes="caption" Text="2. Avalonia 日志（Logger.Sink）" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="AvaloniaLogButton" Content="触发一条绑定警告" Click="OnAvaloniaLog" />
                <Button Name="AvaloniaClearButton" Content="清空" Click="OnAvaloniaClear" />
            </StackPanel>
            <Border Classes="stage" Padding="10" MinHeight="80">
                <ItemsControl Name="AvaloniaEntries" />
            </Border>
            <StackPanel Name="Probe" IsVisible="False" />

            <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                       Text="两套日志互不相通：ILogger 是应用自己的，Logger.Sink 接的是框架内部（绑定、布局、样式）的消息。WinExe 没有控制台，所以这里用自定义 ILoggerProvider 与 ILogSink 把消息收进列表。AppBuilder.LogToTrace() 则是把框架日志送到调试输出。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/LoggingPage.axaml.cs`**

```csharp
using Avalonia.AppDevelopmentDemo.Logging;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class LoggingPage : UserControl
    {
        private readonly ListLoggerProvider _provider = new();
        private readonly ILoggerFactory _factory;
        private readonly ILogger _logger;
        private readonly ObservableCollection<string> _avaloniaEntries = new();
        private ILogSink? _previousSink;
        private PageSink? _sink;

        public LoggingPage()
        {
            InitializeComponent();

            _factory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug).AddProvider(_provider));
            _logger = _factory.CreateLogger("Demo");

            AppEntries.ItemsSource = _provider.Entries;
            AvaloniaEntries.ItemsSource = _avaloniaEntries;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            // Logger.Sink is process-wide, so remember the old sink and put it back on unload.
            _previousSink = Logger.Sink;
            _sink = new PageSink(line => Dispatcher.UIThread.Post(() => _avaloniaEntries.Add(line)));
            Logger.Sink = _sink;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            if (ReferenceEquals(Logger.Sink, _sink))
                Logger.Sink = _previousSink;
        }

        private void OnAppLog(object? sender, RoutedEventArgs e)
        {
            _logger.LogDebug("d {Index}", 1);
            _logger.LogWarning("w");
            _logger.LogError(new InvalidOperationException("boom"), "e");
        }

        private void OnAppClear(object? sender, RoutedEventArgs e) => _provider.Entries.Clear();

        private void OnAvaloniaLog(object? sender, RoutedEventArgs e)
        {
            // A reflection binding to a property that does not exist: the framework reports it
            // through Logger rather than throwing.
            var probe = new TextBlock { DataContext = new object() };
            probe.Bind(TextBlock.TextProperty, new Binding("Missing"));
            ((Panel)Probe).Children.Add(probe);
            ((Panel)Probe).Children.Clear();
        }

        private void OnAvaloniaClear(object? sender, RoutedEventArgs e) => _avaloniaEntries.Clear();

        private sealed class PageSink : ILogSink
        {
            private readonly Action<string> _write;

            public PageSink(Action<string> write) => _write = write;

            public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

            public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
                => _write($"{level}/{area}: {messageTemplate}");

            public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
                => _write($"{level}/{area}: {messageTemplate} [{string.Join(", ", propertyValues.Select(v => v?.ToString()))}]");
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/UnhandledExceptionsPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.UnhandledExceptionsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="未处理异常：UI 线程上的最后一道防线"
                               DocPath="app-development/setting-unhandled-exceptions" />

            <TextBlock Classes="caption" Text="1. 在 UI 线程上抛一个异常" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ThrowButton" Content="Post 一个会抛异常的回调" Click="OnThrow" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 处理器收到了什么" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="CountLine" Text="处理器被调用：0 次" />
                    <TextBlock Name="MessageLine" Text="最近一次异常：（无）" TextWrapping="Wrap" />
                </StackPanel>
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                       Text="Dispatcher.UIThread.UnhandledException 只管 UI 线程派发的回调。处理器里设 e.Handled = true 就吞掉异常，应用继续运行；不设，异常继续向上抛，经典桌面应用会退出——所以本页的处理器固定吞掉，只演示这条路径。后台 Task 里没人 await 的异常走 TaskScheduler.UnobservedTaskException，不经过这个事件。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/UnhandledExceptionsPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class UnhandledExceptionsPage : UserControl
    {
        private int _count;

        public UnhandledExceptionsPage()
        {
            InitializeComponent();
        }

        // The event is static-ish (one per dispatcher), so subscribe only while the page is on screen.
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Dispatcher.UIThread.UnhandledException += OnUnhandled;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            Dispatcher.UIThread.UnhandledException -= OnUnhandled;
        }

        private void OnUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _count++;
            CountLine.Text = $"处理器被调用：{_count} 次";
            MessageLine.Text = $"最近一次异常：{e.Exception.GetType().Name}：{e.Exception.Message}";
            e.Handled = true;
        }

        private void OnThrow(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => throw new InvalidOperationException("thrown from a posted callback"));
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.ResourcesPage">

    <UserControl.Resources>
        <SolidColorBrush x:Key="Brand" Color="Red" />
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="资源：一次性查找与动态绑定的差别"
                               DocPath="app-development/resources" />

            <TextBlock Classes="caption" Text="1. 两个色块，都取自资源 Brand" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <StackPanel Spacing="4">
                    <Border Name="StaticSwatch" Width="120" Height="48" CornerRadius="4" />
                    <TextBlock Classes="hint" Text="TryFindResource 一次取值" />
                </StackPanel>
                <StackPanel Spacing="4">
                    <Border Name="DynamicSwatch" Width="120" Height="48" CornerRadius="4"
                            Background="{DynamicResource Brand}" />
                    <TextBlock Classes="hint" Text="DynamicResource" />
                </StackPanel>
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 替换字典里的 Brand" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ReplaceButton" Content="把 Brand 换成蓝色画刷" Click="OnReplace" />
                <Button Name="MutateButton" Content="改同一个画刷的 Color" Click="OnMutate" />
                <Button Name="ResetButton" Content="还原" Click="OnReset" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. 读回" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="StaticLine" />
                    <TextBlock Name="DynamicLine" />
                </StackPanel>
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                       Text="替换字典里的项：只有 DynamicResource（或 GetResourceObservable 绑定）会跟着变，一次性取值的不会。直接改同一个画刷实例的 Color：两者都会变，因为它们拿的是同一个对象。查找方法：FindResource 找不到返回 null，TryFindResource 返回 bool，都沿逻辑树向上，最后到 Application.Resources。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class ResourcesPage : UserControl
    {
        public ResourcesPage()
        {
            InitializeComponent();
            Reset();
        }

        private void Reset()
        {
            Resources["Brand"] = new SolidColorBrush(Colors.Red);
            // The one-shot lookup: the brush is read once and assigned, so it is never refreshed.
            StaticSwatch.Background = this.TryFindResource("Brand", ActualThemeVariant, out var brush) ? brush as IBrush : null;
            Refresh();
        }

        private void OnReplace(object? sender, RoutedEventArgs e)
        {
            Resources["Brand"] = new SolidColorBrush(Colors.Blue);
            Refresh();
        }

        private void OnMutate(object? sender, RoutedEventArgs e)
        {
            if (Resources["Brand"] is SolidColorBrush brush)
                brush.Color = Colors.Green;
            Refresh();
        }

        private void OnReset(object? sender, RoutedEventArgs e) => Reset();

        private void Refresh()
        {
            StaticLine.Text = $"TryFindResource 取到的：{Describe(StaticSwatch.Background)}";
            DynamicLine.Text = $"DynamicResource 的：{Describe(DynamicSwatch.Background)}";
        }

        private static string Describe(IBrush? brush)
            => brush is ISolidColorBrush solid ? solid.Color.ToString() : "（无）";
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/DataValidationPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.DataValidationPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="数据校验：完整演示在 Avalonia.DataBindingDemo"
                           DocPath="app-development/data-validation" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="数据校验靠绑定把 INotifyDataErrorInfo 或 DataValidationException 的错误传到控件的 DataValidationErrors 上。它是数据绑定的一部分，完整演示放在数据绑定项目里。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.DataBindingDemo   →  「校验」标签页" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/DataValidationPage.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class DataValidationPage : UserControl
    {
        public DataValidationPage()
        {
            InitializeComponent();
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/ThreadingPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.ThreadingPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="线程模型：控件只能在 UI 线程上访问"
                               DocPath="app-development/threading" />

            <TextBlock Classes="caption" Text="1. 在后台线程直接写控件" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="DirectButton" Content="后台线程直接写 Text" Click="OnDirect" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 用 Dispatcher.UIThread.Post 回到 UI 线程" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="PostButton" Content="后台线程 Post 回来写" Click="OnPost" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. async / await 自动回到 UI 线程" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="AwaitButton" Content="await Task.Run 之后写" Click="OnAwait" />
            </StackPanel>

            <TextBlock Classes="caption" Text="4. 结果" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="Target" Text="（被写的控件）" />
                    <TextBlock Name="Result" Text="（还没有操作）" TextWrapping="Wrap" />
                </StackPanel>
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                       Text="Dispatcher.UIThread.CheckAccess() 回答「我现在在 UI 线程吗」。从后台线程碰控件会抛 InvalidOperationException；Post 是不等待的投递，InvokeAsync 返回可 await 的任务。await 之后的代码回到原来的同步上下文，所以在 UI 线程上 await 之后依然在 UI 线程。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/ThreadingPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class ThreadingPage : UserControl
    {
        public ThreadingPage()
        {
            InitializeComponent();
        }

        private async void OnDirect(object? sender, RoutedEventArgs e)
        {
            // The write happens inside Task.Run, i.e. on a thread-pool thread.
            var message = await Task.Run(() =>
            {
                var onUi = Dispatcher.UIThread.CheckAccess();
                try
                {
                    Target.Text = "written from the background";
                    return $"CheckAccess = {onUi}，写入成功（不应该发生）";
                }
                catch (InvalidOperationException ex)
                {
                    return $"CheckAccess = {onUi}，InvalidOperationException：{ex.Message}";
                }
            });
            Result.Text = message;
        }

        private async void OnPost(object? sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Target.Text = "written via Post";
                    Result.Text = $"Post 的回调里 CheckAccess = {Dispatcher.UIThread.CheckAccess()}，写入成功";
                });
            });
        }

        private async void OnAwait(object? sender, RoutedEventArgs e)
        {
            await Task.Run(() => System.Threading.Thread.Sleep(10));
            Target.Text = "written after await";
            Result.Text = $"await 之后 CheckAccess = {Dispatcher.UIThread.CheckAccess()}，写入成功";
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/WindowManagementPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.WindowManagementPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="窗口管理：Show、ShowDialog、Owner、关闭拦截"
                               DocPath="app-development/window-management" />

            <TextBlock Classes="caption" Text="1. 打开窗口" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ShowButton" Content="Show(owner)：非模态子窗口" Click="OnShow" />
                <Button Name="DialogButton" Content="ShowDialog&lt;string?&gt;：模态，取返回值" Click="OnDialog" />
                <Button Name="GuardButton" Content="拦截关闭的窗口" Click="OnGuard" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 结果" />
            <Border Classes="stage" Padding="10">
                <TextBlock Name="Result" Text="（还没有操作）" TextWrapping="Wrap" />
            </Border>

            <TextBlock Classes="caption" Text="3. 当前窗口的属性" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="StateLine" />
                    <TextBlock Name="DecorationsLine" />
                    <TextBlock Name="ScreenLine" />
                </StackPanel>
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                       Text="ShowDialog&lt;T&gt; 的 T 就是 Close(result) 传回的类型。Closing 事件里设 e.Cancel = true 可以阻止关闭。过时提示：Window.SystemDecorations 已被 WindowDecorations 取代。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/WindowManagementPage.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class WindowManagementPage : UserControl
    {
        public WindowManagementPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Describe();
        }

        private Window? Host => TopLevel.GetTopLevel(this) as Window;

        private void Describe()
        {
            if (Host is not { } w)
                return;
            StateLine.Text = $"WindowState = {w.WindowState}，CanResize = {w.CanResize}，SizeToContent = {w.SizeToContent}，Topmost = {w.Topmost}";
            DecorationsLine.Text = $"WindowDecorations = {w.WindowDecorations}，ShowInTaskbar = {w.ShowInTaskbar}";
            var screens = w.Screens;
            ScreenLine.Text = screens is null
                ? "Screens = null"
                : $"屏幕 {screens.All.Count} 个，主屏工作区 = {screens.Primary?.WorkingArea}";
        }

        private static Window MakeChild(string title, Control? extra = null)
        {
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 8 };
            panel.Children.Add(new TextBlock { Text = title });
            if (extra is not null)
                panel.Children.Add(extra);
            return new Window
            {
                Title = title,
                Width = 320,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = panel,
            };
        }

        private void OnShow(object? sender, RoutedEventArgs e)
        {
            if (Host is not { } owner)
                return;
            var child = MakeChild("非模态子窗口");
            child.Show(owner);
            Result.Text = $"Show(owner) 之后 child.Owner 是主窗口：{ReferenceEquals(child.Owner, owner)}";
        }

        private async void OnDialog(object? sender, RoutedEventArgs e)
        {
            if (Host is not { } owner)
                return;
            var ok = new Button { Content = "确定，返回 \"accepted\"" };
            var dialog = MakeChild("模态对话框", ok);
            ok.Click += (_, _) => dialog.Close("accepted");

            var answer = await dialog.ShowDialog<string?>(owner);
            // Closing with the title-bar button returns the default, null.
            Result.Text = $"ShowDialog 返回：{answer ?? "null"}";
        }

        private void OnGuard(object? sender, RoutedEventArgs e)
        {
            if (Host is not { } owner)
                return;
            var allow = false;
            var allowBox = new CheckBox { Content = "允许关闭" };
            allowBox.IsCheckedChanged += (_, _) => allow = allowBox.IsChecked == true;
            var child = MakeChild("拦截关闭", allowBox);
            child.Closing += (_, args) =>
            {
                if (!allow)
                {
                    args.Cancel = true;
                    Result.Text = "Closing 里 e.Cancel = true，窗口还在；勾上「允许关闭」再关";
                }
            };
            child.Show(owner);
        }
    }
}
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/AccessibilityPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.AppDevelopmentDemo.Views.Pages.AccessibilityPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="无障碍：AutomationProperties 与 AutomationPeer"
                               DocPath="app-development/accessibility" />

            <TextBlock Classes="caption" Text="1. 带自动化属性的按钮" />
            <Button Name="Target" Content="确定"
                    AutomationProperties.AutomationId="ok-btn"
                    AutomationProperties.Name="Confirm"
                    AutomationProperties.HelpText="Confirms the form" />

            <TextBlock Classes="caption" Text="2. 读回 AutomationPeer（屏幕阅读器看到的就是这些）" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ReadButton" Content="读取 Peer" Click="OnRead" />
            </StackPanel>
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="PeerType" Text="Peer 类型：（未读取）" />
                    <TextBlock Name="PeerName" Text="Name：" />
                    <TextBlock Name="PeerId" Text="AutomationId：" />
                    <TextBlock Name="PeerHelp" Text="HelpText：" />
                    <TextBlock Name="PeerControl" Text="ControlType：" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 把装饰性元素从无障碍树里拿掉" />
            <Border Name="Decoration" Height="24" Background="LightSteelBlue"
                    AutomationProperties.AccessibilityView="Raw" />
            <TextBlock Name="ViewLine" Classes="hint" />

            <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                       Text="Name 是读出来的名字，HelpText 是补充说明，AutomationId 是给自动化测试定位用的稳定标识（#14 的测试就靠它）。AccessibilityView 为 Raw 的元素只在「原始视图」出现，普通屏幕阅读器会跳过。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**`Avalonia.AppDevelopmentDemo/Views/Pages/AccessibilityPage.axaml.cs`**

```csharp
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class AccessibilityPage : UserControl
    {
        public AccessibilityPage()
        {
            InitializeComponent();
            ViewLine.Text = $"Decoration 的 AccessibilityView = {AutomationProperties.GetAccessibilityView(Decoration)}";
        }

        private void OnRead(object? sender, RoutedEventArgs e)
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(Target);
            PeerType.Text = $"Peer 类型：{peer.GetType().Name}";
            PeerName.Text = $"Name：{peer.GetName()}";
            PeerId.Text = $"AutomationId：{peer.GetAutomationId()}";
            PeerHelp.Text = $"HelpText：{peer.GetHelpText()}";
            PeerControl.Text = $"ControlType：{peer.GetAutomationControlType()}";
        }
    }
}
```

- [x] **Step 4: 构建**

Run: `dotnet build Avalonia.AppDevelopmentDemo 2>&1 | grep -E "error|个错误"`
Expected: `0 个错误`。

- [x] **Step 5: 跑 headless 探针（仓库外，不提交）**

建 `C:\Temp\probe-appdev\` 两个文件。探针里断言资源色值用颜色名（`Red`/`Blue`/`Green`），因为 `Color.ToString()` 对命名色返回名字而不是十六进制：


**`probe-appdev.csproj（路径改成实际仓库位置）`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <NoWarn>$(NoWarn);CS8321</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia.Headless" Version="12.1.2" />
    <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.AppDevelopmentDemo\Avalonia.AppDevelopmentDemo.csproj" />
  </ItemGroup>
</Project>
```

**`Program.cs`**

```csharp
using Avalonia;
using Avalonia.AppDevelopmentDemo;
using Avalonia.AppDevelopmentDemo.Views;
using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;

static class Program
{
    static int _pass, _fail;

    static void Check(string name, bool ok, object? detail = null)
    {
        if (ok) { _pass++; Console.WriteLine($"PASS  {name}"); }
        else { _fail++; Console.WriteLine($"FAIL  {name}  [{detail}]"); }
    }

    static void Pump() { for (int i = 0; i < 8; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(10); } }

    static T Find<T>(Visual root, string name) where T : Control
        => root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    static void Click(Visual root, string name)
    {
        Find<Button>(root, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();
    }

    static string Text(Visual root, string name) => Find<TextBlock>(root, name).Text ?? "";

    static Window Host(Control page)
    {
        var w = new Window { Content = page, Width = 800, Height = 700 };
        w.Show(); Pump();
        return w;
    }

    [STAThread]
    static int Main()
    {
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

        // ---- shell
        var window = new MainWindow(); window.Show(); Pump();
        var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
        var headers = tabs.Items.OfType<TabItem>().Select(t => t.Header?.ToString()).ToArray();
        Check("shell has 10 tabs", headers.Length == 10, headers.Length);
        var expected = new[] { typeof(DependencyInjectionPage), typeof(LocalizationPage), typeof(WebContentPage), typeof(LoggingPage),
            typeof(UnhandledExceptionsPage), typeof(ResourcesPage), typeof(DataValidationPage), typeof(ThreadingPage),
            typeof(WindowManagementPage), typeof(AccessibilityPage) };
        for (int i = 0; i < expected.Length; i++)
        {
            tabs.SelectedIndex = i; Pump();
            var content = tabs.Items.OfType<TabItem>().ElementAt(i).Content;
            Check($"tab {i} is {expected[i].Name}", content?.GetType() == expected[i], content?.GetType().Name);
        }

        // ---- DI
        var di = new DependencyInjectionPage(); Host(di);
        Click(di, "ResolveButton");
        Check("DI: transient differs", Text(di, "TransientLine").EndsWith("不同实例"), Text(di, "TransientLine"));
        Check("DI: scoped same", Text(di, "ScopedLine").EndsWith("同一个实例"), Text(di, "ScopedLine"));
        Check("DI: singleton same", Text(di, "SingletonLine").EndsWith("同一个实例"), Text(di, "SingletonLine"));
        Click(di, "ResolveButton");
        var firstSingleton = Text(di, "SingletonLine");
        Check("DI: singleton id stable across scopes", firstSingleton == Text(di, "SingletonLine"), firstSingleton);
        Click(di, "GreetButton");
        Check("DI: greeter got a clock", Text(di, "GreetLine").StartsWith("你好，Avalonia。现在是 "), Text(di, "GreetLine"));
        Click(di, "RootScopedButton");
        Check("DI: root scoped resolve throws", Text(di, "ScopeErrorLine").StartsWith("InvalidOperationException"), Text(di, "ScopeErrorLine"));

        // ---- localization
        var loc = new LocalizationPage(); Host(loc);
        Check("loc: default zh", Text(loc, "GreetingText") == "你好，世界", Text(loc, "GreetingText"));
        Find<RadioButton>(loc, "EnglishRadio").IsChecked = true; Pump();
        Check("loc: en greeting", Text(loc, "GreetingText") == "Hello, world", Text(loc, "GreetingText"));
        Check("loc: en farewell", Text(loc, "FarewellText") == "Goodbye", Text(loc, "FarewellText"));
        Find<RadioButton>(loc, "ChineseRadio").IsChecked = true; Pump();
        Check("loc: back to zh", Text(loc, "FarewellText") == "再见", Text(loc, "FarewellText"));

        // ---- logging
        var log = new LoggingPage(); Host(log);
        Click(log, "AppLogButton");
        var app = Find<ItemsControl>(log, "AppEntries").ItemsSource!.Cast<string>().ToArray();
        Check("logging: three app entries in order", string.Join("|", app) == "Debug:d 1|Warning:w|Error:e", string.Join("|", app));
        Click(log, "AvaloniaLogButton");
        var av = Find<ItemsControl>(log, "AvaloniaEntries").ItemsSource!.Cast<string>().ToArray();
        Check("logging: framework sink captured a binding warning", av.Length >= 1 && av.Any(s => s.Contains("Binding")), string.Join(" || ", av));
        Click(log, "AppClearButton");
        Check("logging: app clear works", !Find<ItemsControl>(log, "AppEntries").ItemsSource!.Cast<string>().Any());

        // ---- unhandled exceptions
        var ue = new UnhandledExceptionsPage(); Host(ue);
        Click(ue, "ThrowButton");
        Check("unhandled: handler ran once", Text(ue, "CountLine") == "处理器被调用：1 次", Text(ue, "CountLine"));
        Check("unhandled: message captured", Text(ue, "MessageLine").Contains("InvalidOperationException"), Text(ue, "MessageLine"));

        // ---- resources
        var res = new ResourcesPage(); Host(res);
        Check("res: both start red", Text(res, "StaticLine").Contains("Red") && Text(res, "DynamicLine").Contains("Red"), Text(res, "StaticLine") + " / " + Text(res, "DynamicLine"));
        Click(res, "ReplaceButton");
        Check("res: replace -> static stays red", Text(res, "StaticLine").Contains("Red"), Text(res, "StaticLine"));
        Check("res: replace -> dynamic turns blue", Text(res, "DynamicLine").Contains("Blue"), Text(res, "DynamicLine"));
        Click(res, "ResetButton");
        Click(res, "MutateButton");
        Check("res: mutate -> static turns green", Text(res, "StaticLine").Contains("Green"), Text(res, "StaticLine"));
        Check("res: mutate -> dynamic turns green", Text(res, "DynamicLine").Contains("Green"), Text(res, "DynamicLine"));

        // ---- threading
        var th = new ThreadingPage(); Host(th);
        Click(th, "DirectButton");
        for (int i = 0; i < 40 && Text(th, "Result").StartsWith("（"); i++) Pump();
        Check("threading: direct write throws", Text(th, "Result").Contains("CheckAccess = False，InvalidOperationException"), Text(th, "Result"));
        Click(th, "PostButton");
        for (int i = 0; i < 40 && !Text(th, "Result").Contains("Post 的回调"); i++) Pump();
        Check("threading: post lands on UI thread", Text(th, "Result").Contains("CheckAccess = True，写入成功") && Text(th, "Target") == "written via Post", Text(th, "Result"));
        Click(th, "AwaitButton");
        for (int i = 0; i < 40 && !Text(th, "Result").Contains("await 之后"); i++) Pump();
        Check("threading: await resumes on UI thread", Text(th, "Result").Contains("CheckAccess = True，写入成功") && Text(th, "Target") == "written after await", Text(th, "Result"));

        // ---- window management
        var wm = new WindowManagementPage(); var wmHost = Host(wm);
        Check("window: state line", Text(wm, "StateLine").Contains("WindowState = Normal") && Text(wm, "StateLine").Contains("CanResize = True"), Text(wm, "StateLine"));
        Check("window: decorations line", Text(wm, "DecorationsLine").Contains("WindowDecorations = Full"), Text(wm, "DecorationsLine"));
        Click(wm, "ShowButton");
        Check("window: Show(owner) sets Owner", Text(wm, "Result").Contains("True"), Text(wm, "Result"));
        Click(wm, "GuardButton");
        var guard = wmHost.OwnedWindows.First(w => w.Title == "拦截关闭");
        guard.Close(); Pump();
        Check("window: Closing cancel keeps it open", guard.IsVisible && Text(wm, "Result").Contains("e.Cancel"), Text(wm, "Result"));

        // ---- accessibility
        var ac = new AccessibilityPage(); Host(ac);
        Click(ac, "ReadButton");
        Check("a11y: peer type", Text(ac, "PeerType").EndsWith("ButtonAutomationPeer"), Text(ac, "PeerType"));
        Check("a11y: name", Text(ac, "PeerName") == "Name：Confirm", Text(ac, "PeerName"));
        Check("a11y: id", Text(ac, "PeerId") == "AutomationId：ok-btn", Text(ac, "PeerId"));
        Check("a11y: help", Text(ac, "PeerHelp") == "HelpText：Confirms the form", Text(ac, "PeerHelp"));
        Check("a11y: control type", Text(ac, "PeerControl") == "ControlType：Button", Text(ac, "PeerControl"));
        Check("a11y: raw view reported", Text(ac, "ViewLine").Contains("Raw"), Text(ac, "ViewLine"));

        Console.WriteLine($"{_pass} passed, {_fail} failed");
        return _fail == 0 ? 0 : 1;
    }
}
```

Run: `cd C:/Temp/probe-appdev && dotnet run 2>&1 | tail -50`
Expected: 末行 `44 passed, 0 failed`。

- [x] **Step 6: 提交**

```bash
git add Avalonia.AppDevelopmentDemo/
git commit -m "feat: demonstrate the App Development category" -m "Ten pages: dependency injection, localization, logging, unhandled exceptions, resources, threading, window management and accessibility, plus signposts for web content and data validation.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 4: Avalonia.TestingDemo（被测应用）

**Files:**
- Create: `ViewModels/CounterViewModel.cs`、`FormViewModel.cs`、`ListViewModel.cs`；`Views/Pages/CounterPage`、`FormPage`、`ListPage` 各一对文件
- Modify: `Views/MainWindow.axaml`

**Interfaces:**
- Consumes: Task 1 的空壳；`Avalonia.Shared.ViewModels.ViewModelBase`
- Produces（Task 5 的测试依赖这些名字，**不要改**）：
  - `CounterViewModel`：`Count`、`IncrementCommand`、`DecrementCommand`、`ResetCommand`
  - `FormViewModel`：`Name`、`Age`、`NameError`、`AgeError`、`CanSubmit`、`Status`、`SubmitCommand`
  - `ListViewModel`：`Items`、`NewItem`、`Selected`、`Summary`、`AddCommand`、`RemoveCommand`
  - 页面里每个交互控件都有 `Name` 与 `AutomationProperties.AutomationId`，见下面各页

**设计要点：** 每个页面在自己的 XAML 里写 `<UserControl.DataContext>`，所以 `new CounterPage()` 就是一个自带状态的完整面板，测试里不用再配 DataContext。

- [x] **Step 1: 写三个 ViewModel**

**`Avalonia.TestingDemo/ViewModels/CounterViewModel.cs`**

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.TestingDemo.ViewModels
{
    public partial class CounterViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DecrementCommand), nameof(ResetCommand))]
        private int _count;

        [RelayCommand]
        private void Increment() => Count++;

        [RelayCommand(CanExecute = nameof(HasCount))]
        private void Decrement() => Count--;

        [RelayCommand(CanExecute = nameof(HasCount))]
        private void Reset() => Count = 0;

        private bool HasCount() => Count > 0;
    }
}
```

**`Avalonia.TestingDemo/ViewModels/FormViewModel.cs`**

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.TestingDemo.ViewModels
{
    public partial class FormViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NameError), nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private string _name = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AgeError), nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private string _age = "";

        [ObservableProperty]
        private string _status = "未提交";

        // Empty strings mean "no error"; a field that was never typed into starts invalid via CanSubmit.
        public string NameError => string.IsNullOrWhiteSpace(Name) ? "" : Name.Trim().Length < 2 ? "姓名至少 2 个字符" : "";

        public string AgeError
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Age))
                    return "";
                if (!int.TryParse(Age, out var age))
                    return "年龄必须是整数";
                return age is < 0 or > 150 ? "年龄必须在 0 到 150 之间" : "";
            }
        }

        public bool CanSubmit =>
            !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Age) && NameError == "" && AgeError == "";

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private void Submit() => Status = $"已提交：{Name.Trim()}，{Age} 岁";
    }
}
```

**`Avalonia.TestingDemo/ViewModels/ListViewModel.cs`**

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Avalonia.TestingDemo.ViewModels
{
    public partial class ListViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private string _newItem = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        private string? _selected;

        public ListViewModel()
        {
            Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        }

        public string Summary => $"共 {Items.Count} 项";

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private void Add()
        {
            Items.Add(NewItem.Trim());
            NewItem = "";
        }

        private bool CanAdd() => !string.IsNullOrWhiteSpace(NewItem);

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove()
        {
            if (Selected is { } item)
                Items.Remove(item);
        }

        private bool CanRemove() => Selected is not null;
    }
}
```

- [x] **Step 2: 写三个页面**

`TextBox.Watermark` 在 12.x 已过时（`AVLN5001`），用 `PlaceholderText`。

**`Avalonia.TestingDemo/Views/Pages/CounterPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.TestingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="420"
             x:Class="Avalonia.TestingDemo.Views.Pages.CounterPage"
             x:DataType="vm:CounterViewModel">

    <UserControl.DataContext>
        <vm:CounterViewModel />
    </UserControl.DataContext>

    <StackPanel Margin="12">
        <shared:DemoHeader Title="被测面板一：计数器，计数为 0 时「减」与「清零」禁用"
                           DocPath="testing/headless-xunit" />

        <TextBlock Classes="caption" Text="计数" />
        <TextBlock Name="CountText" AutomationProperties.AutomationId="count-text"
                   FontSize="32" Text="{Binding Count}" />

        <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,8,0,0">
            <Button Name="IncrementButton" AutomationProperties.AutomationId="increment-btn"
                    Content="加" Command="{Binding IncrementCommand}" />
            <Button Name="DecrementButton" AutomationProperties.AutomationId="decrement-btn"
                    Content="减" Command="{Binding DecrementCommand}" />
            <Button Name="ResetButton" AutomationProperties.AutomationId="reset-btn"
                    Content="清零" Command="{Binding ResetCommand}" />
        </StackPanel>

        <TextBlock Classes="hint" Margin="0,12,0,0" TextWrapping="Wrap"
                   Text="这个面板就是为 Avalonia.TestingDemo.Tests 准备的：每个控件都有 Name 和 AutomationId，状态都显示在文本里，没有随机和时间依赖。" />
    </StackPanel>
</UserControl>
```

**`Avalonia.TestingDemo/Views/Pages/CounterPage.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.TestingDemo.Views.Pages
{
    public partial class CounterPage : UserControl
    {
        public CounterPage()
        {
            InitializeComponent();
        }
    }
}
```

**`Avalonia.TestingDemo/Views/Pages/FormPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.TestingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="420"
             x:Class="Avalonia.TestingDemo.Views.Pages.FormPage"
             x:DataType="vm:FormViewModel">

    <UserControl.DataContext>
        <vm:FormViewModel />
    </UserControl.DataContext>

    <StackPanel Margin="12" Spacing="6">
        <shared:DemoHeader Title="被测面板二：表单，两项都合法后「提交」才启用"
                           DocPath="testing/headless-xunit" />

        <TextBlock Classes="caption" Text="姓名" />
        <TextBox Name="NameBox" AutomationProperties.AutomationId="name-box"
                 Width="280" HorizontalAlignment="Left" Text="{Binding Name}" />
        <TextBlock Name="NameError" AutomationProperties.AutomationId="name-error"
                   Foreground="Firebrick" Text="{Binding NameError}" />

        <TextBlock Classes="caption" Text="年龄" />
        <TextBox Name="AgeBox" AutomationProperties.AutomationId="age-box"
                 Width="280" HorizontalAlignment="Left" Text="{Binding Age}" />
        <TextBlock Name="AgeError" AutomationProperties.AutomationId="age-error"
                   Foreground="Firebrick" Text="{Binding AgeError}" />

        <Button Name="SubmitButton" AutomationProperties.AutomationId="submit-btn"
                Content="提交" Command="{Binding SubmitCommand}" />
        <TextBlock Name="StatusText" AutomationProperties.AutomationId="status-text"
                   Text="{Binding Status}" />
    </StackPanel>
</UserControl>
```

**`Avalonia.TestingDemo/Views/Pages/FormPage.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.TestingDemo.Views.Pages
{
    public partial class FormPage : UserControl
    {
        public FormPage()
        {
            InitializeComponent();
        }
    }
}
```

**`Avalonia.TestingDemo/Views/Pages/ListPage.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.TestingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="420"
             x:Class="Avalonia.TestingDemo.Views.Pages.ListPage"
             x:DataType="vm:ListViewModel">

    <UserControl.DataContext>
        <vm:ListViewModel />
    </UserControl.DataContext>

    <StackPanel Margin="12" Spacing="6">
        <shared:DemoHeader Title="被测面板三：列表，加项、选中后删除、显示总数"
                           DocPath="testing/headless-xunit" />

        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBox Name="NewItemBox" AutomationProperties.AutomationId="new-item-box"
                     Width="240" PlaceholderText="新条目" Text="{Binding NewItem}" />
            <Button Name="AddButton" AutomationProperties.AutomationId="add-btn"
                    Content="添加" Command="{Binding AddCommand}" />
            <Button Name="RemoveButton" AutomationProperties.AutomationId="remove-btn"
                    Content="删除选中项" Command="{Binding RemoveCommand}" />
        </StackPanel>

        <ListBox Name="ItemsList" AutomationProperties.AutomationId="items-list"
                 Height="160" ItemsSource="{Binding Items}" SelectedItem="{Binding Selected}" />

        <TextBlock Name="SummaryText" AutomationProperties.AutomationId="summary-text"
                   Text="{Binding Summary}" />
    </StackPanel>
</UserControl>
```

**`Avalonia.TestingDemo/Views/Pages/ListPage.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.TestingDemo.Views.Pages
{
    public partial class ListPage : UserControl
    {
        public ListPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 3: 写 MainWindow 的 3 个 Tab**

```xml
    <TabControl Margin="12">
        <TabItem Header="计数器"><pages:CounterPage /></TabItem>
        <TabItem Header="表单"><pages:FormPage /></TabItem>
        <TabItem Header="列表"><pages:ListPage /></TabItem>
    </TabControl>
```

- [x] **Step 4: 构建**

Run: `dotnet build Avalonia.TestingDemo 2>&1 | grep -E "warning|个错误|个警告" | grep -v CS8618`
Expected: `0 个错误`，无 `AVLN` 警告。（`Avalonia.Shared` 里已有的 `CS8618` 与本任务无关。）

---

## Task 5: Avalonia.TestingDemo.Tests（xUnit v3 + Headless）

**Files:**
- Create: `Avalonia.TestingDemo.Tests/` 下 `Avalonia.TestingDemo.Tests.csproj`、`TestAppBuilder.cs`、`UiHelpers.cs`、`ViewModelTests.cs`、`ControlQueryTests.cs`、`InteractionTests.cs`、`RenderSnapshotTests.cs`
- Modify: `hello-avalonia.slnx`（加 `Avalonia.TestingDemo.Tests`）

**Interfaces:**
- Consumes: Task 4 的 ViewModel、页面类与 `Name`/`AutomationId`（`increment-btn`、`name-box` 等）
- Produces: `UiHelpers`（`Show(Control)`、`ByName<T>`、`ByAutomationId<T>`、`Click(Window, Control)`、`Type(Window, Control, string)`），供各测试类共用

**测试写法要点（每条都对应一次踩坑）：**
- `[AvaloniaFact]` / `[AvaloniaTheory]` 负责准备 UI 线程；纯 ViewModel 测试用普通 `[Fact]`，更快（规则 12）。
- `Click` 用真实鼠标事件（`MouseDown`/`MouseUp`），因为 `RaiseEvent(Click)` 不执行 `Command`（规则 3）。
- `Type` 先 `Focus()` 再 `KeyTextInput`，因为 `KeyPressQwerty` 不产生 `TextInput`（规则 9）。
- 命令禁用断言 `IsEffectivelyEnabled`（规则 6）。
- 渲染快照要 Skia，`TestAppBuilder` 里 `UseHeadlessDrawing = false`（规则 13）；读像素时按帧的 `Format` 判断字节序，**不要假设 BGRA**——实测 `CaptureRenderedFrame` 返回 RGBA，假设错了红蓝会互换。

- [x] **Step 1: 建项目、注册到解决方案，先写 ViewModel 测试**

**`Avalonia.TestingDemo.Tests/Avalonia.TestingDemo.Tests.csproj`**

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
        <ProjectReference Include="..\Avalonia.TestingDemo\Avalonia.TestingDemo.csproj" />
    </ItemGroup>
</Project>
```

**`Avalonia.TestingDemo.Tests/TestAppBuilder.cs`**

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.TestingDemo;
using Avalonia.TestingDemo.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.TestingDemo.Tests
{
    public static class TestAppBuilder
    {
        // UseSkia + UseHeadlessDrawing=false is what makes CaptureRenderedFrame return real pixels;
        // the default headless drawing produces 1x1 bitmaps.
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
```

**`Avalonia.TestingDemo.Tests/ViewModelTests.cs`**

```csharp
using Avalonia.TestingDemo.ViewModels;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    // No [AvaloniaFact] here: a ViewModel has no controls, so plain [Fact] is enough and much faster.
    public class ViewModelTests
    {
        [Fact]
        public void Counter_starts_at_zero_with_decrement_and_reset_disabled()
        {
            var vm = new CounterViewModel();

            Assert.Equal(0, vm.Count);
            Assert.False(vm.DecrementCommand.CanExecute(null));
            Assert.False(vm.ResetCommand.CanExecute(null));
        }

        [Fact]
        public void Counter_increment_enables_decrement_and_reset()
        {
            var vm = new CounterViewModel();

            vm.IncrementCommand.Execute(null);

            Assert.Equal(1, vm.Count);
            Assert.True(vm.DecrementCommand.CanExecute(null));
            Assert.True(vm.ResetCommand.CanExecute(null));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("a", "姓名至少 2 个字符")]
        [InlineData("Al", "")]
        public void Form_name_error_follows_the_name(string name, string expected)
        {
            var vm = new FormViewModel { Name = name };

            Assert.Equal(expected, vm.NameError);
        }

        [Theory]
        [InlineData("abc", "年龄必须是整数")]
        [InlineData("-1", "年龄必须在 0 到 150 之间")]
        [InlineData("151", "年龄必须在 0 到 150 之间")]
        [InlineData("30", "")]
        public void Form_age_error_follows_the_age(string age, string expected)
        {
            var vm = new FormViewModel { Age = age };

            Assert.Equal(expected, vm.AgeError);
        }

        [Fact]
        public void Form_submit_needs_both_fields_valid()
        {
            var vm = new FormViewModel();
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.Name = "Alice";
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.Age = "30";
            Assert.True(vm.SubmitCommand.CanExecute(null));

            vm.SubmitCommand.Execute(null);
            Assert.Equal("已提交：Alice，30 岁", vm.Status);
        }

        [Fact]
        public void List_add_trims_clears_the_input_and_updates_the_summary()
        {
            var vm = new ListViewModel { NewItem = "  apple  " };

            vm.AddCommand.Execute(null);

            Assert.Equal(new[] { "apple" }, vm.Items);
            Assert.Equal("", vm.NewItem);
            Assert.Equal("共 1 项", vm.Summary);
        }

        [Fact]
        public void List_remove_needs_a_selection()
        {
            var vm = new ListViewModel();
            vm.Items.Add("a");
            Assert.False(vm.RemoveCommand.CanExecute(null));

            vm.Selected = "a";
            Assert.True(vm.RemoveCommand.CanExecute(null));

            vm.RemoveCommand.Execute(null);
            Assert.Empty(vm.Items);
            Assert.Equal("共 0 项", vm.Summary);
        }
    }
}
```

在 `hello-avalonia.slnx` 里 `Avalonia.TestingDemo` 那行之后加一行：

```xml
  <Project Path="Avalonia.TestingDemo.Tests/Avalonia.TestingDemo.Tests.csproj" />
```

Run: `dotnet test Avalonia.TestingDemo.Tests 2>&1 | grep -E "error|通过!|失败!"`
Expected: `通过:    12`，失败 0。

- [x] **Step 2: 写 UI 辅助与控件查找、交互测试**

**`Avalonia.TestingDemo.Tests/UiHelpers.cs`**

```csharp
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.TestingDemo.Tests
{
    // Shared helpers for the headless UI tests.
    internal static class UiHelpers
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

**`Avalonia.TestingDemo.Tests/ControlQueryTests.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestingDemo.Views.Pages;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class ControlQueryTests
    {
        [AvaloniaFact]
        public void Finds_a_control_by_Name()
        {
            var window = UiHelpers.Show(new CounterPage());

            var button = window.ByName<Button>("IncrementButton");

            Assert.Equal("加", button.Content);
        }

        [AvaloniaFact]
        public void Finds_a_control_by_AutomationId()
        {
            var window = UiHelpers.Show(new CounterPage());

            var button = window.ByAutomationId<Button>("increment-btn");

            Assert.Equal("IncrementButton", button.Name);
        }

        [AvaloniaFact]
        public void Every_form_control_is_reachable_by_its_AutomationId()
        {
            var window = UiHelpers.Show(new FormPage());

            Assert.NotNull(window.ByAutomationId<TextBox>("name-box"));
            Assert.NotNull(window.ByAutomationId<TextBox>("age-box"));
            Assert.NotNull(window.ByAutomationId<TextBlock>("name-error"));
            Assert.NotNull(window.ByAutomationId<TextBlock>("age-error"));
            Assert.NotNull(window.ByAutomationId<Button>("submit-btn"));
            Assert.NotNull(window.ByAutomationId<TextBlock>("status-text"));
        }

        [AvaloniaFact]
        public void Initial_state_is_readable_from_the_controls()
        {
            var window = UiHelpers.Show(new CounterPage());

            Assert.Equal("0", window.ByName<TextBlock>("CountText").Text);
            // Assert IsEffectivelyEnabled: a command-disabled button keeps IsEnabled = true on some paths.
            Assert.True(window.ByName<Button>("IncrementButton").IsEffectivelyEnabled);
            Assert.False(window.ByName<Button>("DecrementButton").IsEffectivelyEnabled);
            Assert.False(window.ByName<Button>("ResetButton").IsEffectivelyEnabled);
        }
    }
}
```

**`Avalonia.TestingDemo.Tests/InteractionTests.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestingDemo.Views.Pages;
using System.Linq;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class InteractionTests
    {
        [AvaloniaFact]
        public void Clicking_plus_then_minus_walks_the_counter_and_toggles_enabled_state()
        {
            var window = UiHelpers.Show(new CounterPage());
            var count = window.ByName<TextBlock>("CountText");
            var plus = window.ByName<Button>("IncrementButton");
            var minus = window.ByName<Button>("DecrementButton");

            window.Click(plus);
            Assert.Equal("1", count.Text);
            Assert.True(minus.IsEffectivelyEnabled);

            window.Click(minus);
            Assert.Equal("0", count.Text);
            Assert.False(minus.IsEffectivelyEnabled);
        }

        [AvaloniaFact]
        public void Reset_returns_the_counter_to_zero()
        {
            var window = UiHelpers.Show(new CounterPage());
            var plus = window.ByName<Button>("IncrementButton");

            window.Click(plus);
            window.Click(plus);
            window.Click(plus);
            window.Click(window.ByName<Button>("ResetButton"));

            Assert.Equal("0", window.ByName<TextBlock>("CountText").Text);
        }

        [AvaloniaFact]
        public void Typing_into_the_form_shows_and_clears_validation_errors()
        {
            var window = UiHelpers.Show(new FormPage());

            window.Type(window.ByName<TextBox>("NameBox"), "A");
            Assert.Equal("姓名至少 2 个字符", window.ByName<TextBlock>("NameError").Text);

            window.Type(window.ByName<TextBox>("NameBox"), "l");
            Assert.Equal("", window.ByName<TextBlock>("NameError").Text);

            window.Type(window.ByName<TextBox>("AgeBox"), "x");
            Assert.Equal("年龄必须是整数", window.ByName<TextBlock>("AgeError").Text);
        }

        [AvaloniaFact]
        public void Submit_is_enabled_only_when_the_whole_form_is_valid()
        {
            var window = UiHelpers.Show(new FormPage());
            var submit = window.ByName<Button>("SubmitButton");
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(window.ByName<TextBox>("NameBox"), "Alice");
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(window.ByName<TextBox>("AgeBox"), "30");
            Assert.True(submit.IsEffectivelyEnabled);

            window.Click(submit);
            Assert.Equal("已提交：Alice，30 岁", window.ByName<TextBlock>("StatusText").Text);
        }

        [AvaloniaFact]
        public void List_add_select_remove_round_trip()
        {
            var window = UiHelpers.Show(new ListPage());
            var summary = window.ByName<TextBlock>("SummaryText");
            var list = window.ByName<ListBox>("ItemsList");
            Assert.Equal("共 0 项", summary.Text);

            window.Type(window.ByName<TextBox>("NewItemBox"), "apple");
            window.Click(window.ByName<Button>("AddButton"));
            window.Type(window.ByName<TextBox>("NewItemBox"), "pear");
            window.Click(window.ByName<Button>("AddButton"));

            Assert.Equal("共 2 项", summary.Text);
            Assert.Equal("", window.ByName<TextBox>("NewItemBox").Text);

            list.SelectedIndex = 0;
            window.Click(window.ByName<Button>("RemoveButton"));

            Assert.Equal("共 1 项", summary.Text);
            Assert.Equal(new[] { "pear" }, list.Items.Cast<string>());
        }
    }
}
```

`InteractionTests.cs` 用到 `.Cast<string>()`，需要 `using System.Linq;`（已在上面的源码里）。

- [x] **Step 3: 写渲染快照测试**

**`Avalonia.TestingDemo.Tests/RenderSnapshotTests.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class RenderSnapshotTests
    {
        // Reads one pixel out of a captured frame. Needs Skia drawing (see TestAppBuilder), otherwise the bitmap is 1x1.
        private static Color PixelAt(WriteableBitmap frame, int x, int y)
        {
            using var fb = frame.Lock();
            var bytes = new byte[4];
            var offset = y * fb.RowBytes + x * 4;
            System.Runtime.InteropServices.Marshal.Copy(fb.Address + offset, bytes, 0, 4);
            // The byte order depends on the frame's own pixel format, so read it instead of assuming one.
            return fb.Format == PixelFormat.Bgra8888
                ? Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0])
                : Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }

        [AvaloniaFact]
        public void A_captured_frame_has_the_window_size()
        {
            var window = new Window { Width = 200, Height = 100, Content = new Border { Background = Brushes.Red } };
            window.Show();

            var frame = window.CaptureRenderedFrame();

            Assert.NotNull(frame);
            Assert.Equal(200, frame!.PixelSize.Width);
            Assert.Equal(100, frame.PixelSize.Height);
        }

        [AvaloniaFact]
        public void A_filled_border_renders_its_colour()
        {
            var window = new Window { Width = 200, Height = 100, Content = new Border { Background = Brushes.Red } };
            window.Show();

            var frame = window.CaptureRenderedFrame()!;

            Assert.Equal(Colors.Red, PixelAt(frame, 100, 50));
        }

        [AvaloniaFact]
        public void Two_halves_render_two_colours()
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            grid.Children.Add(new Border { Background = Brushes.Blue });
            var right = new Border { Background = Brushes.Lime };
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
            var window = new Window { Width = 200, Height = 100, Content = grid };
            window.Show();

            var frame = window.CaptureRenderedFrame()!;

            Assert.Equal(Colors.Blue, PixelAt(frame, 50, 50));
            Assert.Equal(Colors.Lime, PixelAt(frame, 150, 50));
        }
    }
}
```

- [x] **Step 4: 跑全部测试**

Run: `dotnet test Avalonia.TestingDemo.Tests 2>&1 | grep -E "error|通过!|失败!"`
Expected: `通过:    24`，失败 0。

- [x] **Step 5: 提交**

```bash
git add Avalonia.TestingDemo/ Avalonia.TestingDemo.Tests/ hello-avalonia.slnx
git commit -m "feat: demonstrate the Testing category" -m "Adds Avalonia.TestingDemo (counter, form and list panels built to be asserted against) and Avalonia.TestingDemo.Tests: 24 xunit v3 + Avalonia.Headless tests covering control queries, simulated input, plain ViewModel tests and rendered-pixel checks.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 6: README 与 spec 写回

**Files:**
- Modify: `README.md`、`docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

- [x] **Step 1: README 加 5 行并补测试命令**

在 `Avalonia.CustomControlsDemo` 行之后、`Avalonia.MusicStore` 行之前（官方分类顺序），加：

```markdown
| [Avalonia.ServicesDemo](Avalonia.ServicesDemo) | 剪贴板、文件对话框、StorageProvider、Launcher、平台设置，以及 InputPane / InsetsManager / IActivatableLifetime 的可用性检测 |
| [Avalonia.AppDevelopmentDemo](Avalonia.AppDevelopmentDemo) | 依赖注入、本地化、日志、未处理异常、资源查找、线程模型、窗口管理、无障碍 |
| [Avalonia.TestingDemo](Avalonia.TestingDemo) | 被测应用：计数器、表单、列表三个易于断言的面板 |
| [Avalonia.TestingDemo.Tests](Avalonia.TestingDemo.Tests) | xUnit v3 + Headless 测试：控件查找、模拟输入、ViewModel 单测、渲染像素断言（`dotnet test`） |
```

「构建与运行」代码块里 `dotnet run --project Avalonia.WebViewDemo` 之后加一行 `dotnet test Avalonia.TestingDemo.Tests`。

- [x] **Step 2: spec 的包清单更正**

把「依赖与技术选型」表里的 `xunit` 行改为：

```markdown
| `xunit.v3` | 测试框架（原写 `xunit`，见「应用服务层实测结论」） | #15 |
```

- [x] **Step 3: spec 追加「应用服务层实测结论」小节**

在 `## 交付顺序与验证标准` 之前追加 `### 应用服务层实测结论（2026-10-08）`，记录：技术风险第 1 点已解除但测试框架换成 xunit v3（`CS0433` 与 `OutputType=Exe`）；渲染快照要 Skia 且按 `Format` 读字节序；第 3 点的实测（headless 下 `StorageProvider`/`Launcher` 为 Noop，`InputPane`/`InsetsManager`/`IActivatableLifetime` 为 `null`）；功能点映射的出入（Services 8 个 Tab、App Development 10 个 Tab、Testing 并入 #15、**Appium 未做**）；API 形态（`GetPlatformSettings()`、12.x 剪贴板、`WindowDecorations`、`PlaceholderText`）；DI 作用域校验、资源查找差别、两套日志互不相通、`RadioButton` 事件顺序、`.resx` 要排除出 `AvaloniaResource`；探针条数（#12 为 36、#13 为 44，#15 自身 24 个测试）。

- [x] **Step 4: 全量验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx --no-incremental 2>&1 | grep warning | grep -E "ServicesDemo|AppDevelopmentDemo|TestingDemo" | grep -v MSB3884`
Expected: 无输出。

Run: `dotnet test hello-avalonia.slnx 2>&1 | grep -E "通过!|失败!"`
Expected: `通过:    24`，失败 0。

- [x] **Step 5: 提交**

```bash
git add README.md docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md
git commit -m "docs: register the app-services demos and record what they measured" -m "README gets the five new project rows and the dotnet test command. The spec gets an application-services findings section, and its package list now names xunit.v3 instead of xunit.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec 覆盖：** #12 Services（Task 2）、#13 AppDevelopment（Task 3）、#14 TestingDemo（Task 4）、#15 Tests（Task 5）、README / slnx / 包声明（Task 1、5、6）。spec 的「项目 #15 额外要求 `dotnet test` 全部通过」落在 Task 5 Step 4 与 Task 6 Step 4。官方文档里不做的页（XAML 预览、性能、原生互操作、跨平台搭建、Appium）已在「功能点映射的实测修正」与「Global Constraints」里说明。

**占位符扫描：** 无 TBD/TODO；每个代码步骤都有完整源码，源码由脚本从已通过探针与测试的仓库文件原样嵌入，不会与实际代码漂移。

**类型一致性：** Task 4 声明的 ViewModel 成员名（`IncrementCommand`、`NameError`、`Summary` 等）与 Task 5 的测试逐一对应；`AutomationId` 与 `Name` 在 Task 4 页面与 Task 5 `UiHelpers`/测试中一致。

## 执行记录（2026-10-08）

Task 2–5 里嵌入的源码，是执行后从仓库里已通过探针与测试的文件原样取回的，与仓库逐字一致（执行时先在草稿目录验证、再落地，之后才回填进 plan）。

验证结果：#12 探针 36 条、#13 探针 44 条全部通过；`dotnet test` 24 个测试全部通过；整个解决方案 0 错误。README 与 spec 的写回见提交 `docs: register the app-services demos and record what they measured`。

执行时对 plan 的更正：`TextBox.Watermark` 已过时，改用 `PlaceholderText`；渲染快照读像素时按帧的 `Format` 判断字节序；本地化页的 `RadioButton` 按 `sender` 判断；数据校验路标的目标 Tab 名为「校验」。
