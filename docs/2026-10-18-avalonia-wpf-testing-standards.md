# Avalonia / WPF 项目测试规范

> 适用范围：本仓库（`hello-avalonia`）以及同类型的桌面端 MVVM 项目；WPF 部分单独标注差异。
>
> 证据来源：Avalonia 官方测试文档（primary）、Microsoft EF Core 测试文档（primary）、WireMock.Net / NSubstitute / MockHttp 官方文档（primary）、Xunit.StaFact 文档（primary）、Avalonia XPF 文档（primary）。每条结论后面的方括号是出处编号，见文末「证据与置信度」。
>
> 性能基准（BenchmarkDotNet）不在本文范围，见 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md)。

---

## 0. 一页结论

| 问题 | 结论 |
| --- | --- |
| 测试策略选一种还是分层？ | **分层**。官方文档明确说 "layered" 且 "Start with unit tests" |
| UI 测试默认用哪个？ | **Headless**。跑完整控件树 / 布局 / 样式 / 绑定，无窗口、无显示器，CI 随处可跑 |
| Appium 什么时候上？ | 只测 **真实窗口、原生菜单/焦点、平台特性、无障碍**，以及少量关键用户流程 |
| ViewModel 测试要不要启动 Avalonia？ | **不需要**。纯 xUnit `[Fact]`，除非 VM 内部碰了 `Dispatcher.UIThread` / `Application.Current` |
| 接口请求怎么 mock？ | 优先 **接口替身**；其次 **替换 `HttpMessageHandler`**；需要验证真实 HTTP 行为时用 **WireMock.Net** |
| SQLite 怎么测？ | **SQLite in-memory（连接保持打开）**；禁用 EF Core in-memory provider，禁止手写 fake `DbSet` |
| Benchmark 放哪？ | **不在本文范围**。独立的 `*.Benchmarks` 项目 + 独立 CI 阶段，见 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md) |
| WPF 有没有 headless？ | 原生 WPF **没有**官方 headless；用 `[WpfFact]`（Xunit.StaFact）+ FlaUI/UIA 替代。付费的 Avalonia XPF 可以用 Avalonia headless 跑 WPF UI（需许可证） |

**核心判断标准（一句话版）**：*这个问题在真实窗口里才会出现吗？* 不会 → 单测或 headless；会 → Appium/FlaUI。

---

## 1. 分层模型

### 1.1 四层

| 层 | 测什么 | 工具 | 速度 | 依赖显示器 | 本仓库对应 |
| --- | --- | --- | --- | --- | --- |
| L0 单元测试 | ViewModel、Service、Converter、纯算法 | xUnit `[Fact]` / `[Theory]` | 快 | 否 | `tests/Avalonia.*.Tests/ViewModelTests.cs` |
| L1 Headless UI | 控件行为、布局、样式、绑定、输入、路由 | `Avalonia.Headless.XUnit` + `[AvaloniaFact]` | 快 | 否 | `tests/Avalonia.*.Tests/*Tests.cs` + `Avalonia.Tests.Common` |
| L2 视觉回归 | 渲染出的像素、自定义控件外观、主题 | Skia + `CaptureRenderedFrame()` | 中 | 否 | `UiHelpers.CaptureFrame()` |
| L3 端到端 | 完整应用、原生窗口、菜单、焦点、无障碍 | Appium + WinAppDriver（WPF 可用 FlaUI） | 慢 | **是** | `tests/Avalonia.*.AppiumTests` |

官方原文把四层写成 "Choosing the right approach"：单元测试先行，headless 覆盖依赖 Avalonia 属性系统/布局/输入的行为，视觉回归仅在你拥有自定义控件或主题、像素正确性重要时才做，Appium 留给关键流程、平台特性和无障碍。

### 1.2 为什么是这个顺序

- L0/L1 单次运行在毫秒级，可以每个提交都跑；
- L2 需要维护基线图片，成本高、易受抗锯齿和字体差异影响，只覆盖「外观即正确性」的少数场景；
- L3 每次启动真实进程、占用鼠标键盘、需要 WinAppDriver 和桌面会话，**必须是少数**。

**反模式**：把所有 UI 验证都塞进 Appium（慢、脆、只能跑 Windows 桌面会话）；或者完全不做 L1，只靠 L0 单测（绑定、样式、模板错误全部漏网）。

### 1.3 本仓库的目录映射

```
src/Avalonia.<X>Demo/                 被测应用
tests/Avalonia.<X>Demo.Tests/         L0 + L1（默认每个提交都跑）
tests/Avalonia.<X>Demo.AppiumTests/   L3（环境变量门控，默认跳过）
tests/Avalonia.Tests.Common/          L1 共用辅助（HeadlessApp、UiHelpers、TabWalker、CultureScope）
tests/Avalonia.Appium.Common/         L3 共用辅助（AppSession、KeyChord、UnicodeKeyboard）
```


## 2. UI 测试：Headless 还是 Appium

### 2.1 判断表

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

### 2.2 Headless 的确切能力边界

官方文档：headless 平台「provides the full Avalonia control tree, layout, styling, and data binding」「replaces the real windowing and rendering backends with in-memory implementations」，因此不需要显示器，CI 随处可跑。

**它没有的**：

- 原生窗口与平台窗口管理；
- 原生菜单、焦点管理、系统输入法；
- 真实无障碍树；
- 默认绘制后端**不产生真实像素**——要做视觉回归必须 `UseSkia()` + `UseHeadlessDrawing = false`（本仓库 `HeadlessApp.Build<TApp>()` 已经这样配）。

### 2.3 Headless 项目配置（硬性）

1. 包：`Avalonia.Headless.XUnit` + 一个主题包（如 `Avalonia.Themes.Fluent`）——headless 控件同样需要主题才能正确测量和套用样式。
2. 程序集级注册：

```csharp
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
```

测试项目没有 `Program.cs`，所以 `AppBuilder` 必须由这个属性暴露。本仓库统一写成一行 `HeadlessApp.Build<App>()`，不要在各测试项目里复制配置。

3. **UI 测试一律用 `[AvaloniaFact]` / `[AvaloniaTheory]`**。差别不只是名字：这两个特性挂的是 `AvaloniaFactDiscoverer` → `AvaloniaTestCase`，它把测试体派发到 UI 线程并泵 `RunJobs()`；普通 `[Fact]` 走的是标准 `XunitTestCase`，**永远不会**做这件事，测试会随机失败或死锁。
4. 纯 ViewModel 测试保留普通 `[Fact]`（见第 3 节）。

### 2.4 Headless 输入：必须走路由输入管线

用 `Window` 的扩展方法模拟输入：`MouseDown` / `MouseUp` / `MouseMove` / `MouseWheel` / `KeyPress` / `KeyRelease` / `KeyPressQwerty` / `KeyTextInput` / `DragDrop`。

**禁止** `button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))`：它只触发事件处理器，不会调用 `OnClick()`，因此绑定的 `ICommand` **不会执行**，测试会假绿。

本仓库的 `UiHelpers` 已经封装了这几条踩过的坑：

- `Click()`：先 `RunJobs()` → `BringIntoView()` → 再 `RunJobs()` → 在控件中心发 `MouseDown`/`MouseUp`。页面在 `ScrollViewer` 里，不先滚进视野就会点偏。
- `Type()`：先 `Focus()` 再 `KeyTextInput()`；只发 `KeyPressQwerty` 不会产生 `TextInput`。
- `WaitUntil()`：泵调度器直到条件成立，超时**抛异常**而不是返回 `false`——防止忘记断言时超时被当成通过。

### 2.5 异步与渲染同步

headless 不会自动替你把排队的工作跑完。下面三种情况必须显式冲刷：

- 布局/尺寸变化后：`window.UpdateLayout()`；
- 调度器上排队的回调：`Dispatcher.UIThread.RunJobs()`；
- 动画/渲染：`AvaloniaHeadlessPlatform.ForceRenderTimerTick()`。

输入辅助方法和 `CaptureRenderedFrame()` 内部会做一部分，**普通测试代码不会**。规则：凡是断言依赖「刚才发生的事已经生效」，就先冲刷，再断言。

### 2.6 Appium 的使用条件（硬性）

- Windows 上 Appium 依赖 **WinAppDriver 作为服务端**，需预装并先启动；WinAppDriver 要求 **Windows 10+ 且开启开发者模式**。
- **需要真实显示器或虚拟显示器**；Linux 没有稳定的桌面驱动，官方明确建议 Linux CI 用 headless。
- 因此 Appium **必须是可选层**：用环境变量门控，默认跳过（本仓库 `HELLO_AVALONIA_APPIUM=1` + `AppiumFactAttribute`）。
- 仓库实测补充：中文输入法会吞掉 WinAppDriver 的 `SendKeys`，方向键也送不到；一律用 `AppSession.TypeInto` / `KeyChord.Press`。不要按 Ctrl+Shift 组合键（Windows 切换输入法热键，结果时有时无）。
- 子窗口用 `AppSession.FindWindow` 按标题找，`WindowHandles` 不会列出所有者的窗口。
- Appium 项目会**接管鼠标键盘**，多个项目并行要串行化（`AppSession` 用跨进程命名 Mutex 做到这一点）。

**Appium 里不要做的事**：重复 headless 已覆盖的断言、验证样式细节、跑大量参数化用例、依赖固定端口或固定窗口位置。

### 2.7 Appium 里保留什么

每个应用 **1–3 条**关键旅程即可，例如：

1. 启动 → 主窗口出现 → 关键控件可被自动化 ID 找到；
2. 一次完整的输入 → 提交 → 结果可见；
3. 原生有差异的行为：文件对话框、窗口关闭确认、托盘、无障碍名称。

控件要能被稳定定位，优先给关键控件设 `AutomationProperties.AutomationId`（`Name` 只在不依赖本地化时可用）。这不是所有控件的硬要求，但**关键流程上的控件必须有**——这条是本仓库的约定，不是官方强制的。


---

## 3. ViewModel 与服务层测试

### 3.1 默认形态：纯 xUnit，不启动 Avalonia

官方文档专门有一节 "Testing view models without UI"，原话是「You only need the headless platform when your test involves Avalonia controls, layout, or input」，示例注释也写着 `// No [AvaloniaTest] needed, just a regular [Fact]`。

规则：

- `ViewModelTests.cs` 里直接 `new XxxViewModel()`，用 `[Fact]` / `[Theory]`；
- 断言属性值、`CanExecute`、`Execute` 的副作用、`INotifyPropertyChanged` / `ObservableCollection` 通知；
- 需要 `DataValidationException` 之类的行为时直接断言异常和旧值保留；
- 只有 VM **内部**碰了 `Dispatcher.UIThread`、`Application.Current`、`AssetLoader`、`Bitmap`、`StreamGeometry.Parse` 等平台服务时，才升级为 `[AvaloniaFact]`。

**判断方法**：把 `new` 出来的 VM 单独跑，失败信息里出现 `Dispatcher` / `Platform` / `AssetLoader` 字样 → 升级到 headless；否则保持纯单测。

### 3.2 被测单元是 VM，边界是「它的协作者」

VM 测试不需要模拟 UI，但需要替换 VM 依赖的三类协作者：

| 协作者类型 | 推荐做法 | 理由 |
| --- | --- | --- |
| 你自己定义的接口（`IUserService`、`IDialogService`） | 接口替身（NSubstitute / Moq / 手写 fake） | 简单、快、无网络 |
| `HttpClient` / HTTP API | 替换 `HttpMessageHandler` 或用 WireMock.Net | 保留真实序列化与状态码处理 |
| 数据库 / 仓储 | SQLite in-memory 或仓储接口替身 | 见第 4 节 |
| 文件系统、时钟、随机数 | 包装成接口（`IClock`、`IFileSystem`） | 否则测试不确定、不可重复 |

### 3.3 替身必须替在接口上（否则会静默跑真代码）

NSubstitute 的创建机制是 Castle DynamicProxy 生成子类，所以：

- 只拦截**可从测试程序集重写**的成员：`public/protected virtual`，或 `internal virtual` + `InternalsVisibleTo`；
- **非 virtual 成员会真的执行基类实现**，构造函数也会执行；
- 官方原文警告：「real code could be inadvertently executed in your test」「any non-virtual code in the class will actually execute!」

C# 方法默认非 virtual，所以**默认情况下替身具体类是危险的**。规则：

1. 被替身的服务优先定义接口；
2. 需要替身具体类时，成员必须 `virtual`，并显式写一个测试确认「替身没有跑真代码」（例如真实现会抛异常/写日志，断言没有发生）；
3. 不要用「部分替身」去绕过不可测的设计——那是设计问题，不是测试问题。

### 3.4 HTTP mock 的三档策略（由内向外）

#### 档 1：替换 `HttpMessageHandler`（首选，零网络）

适用：客户端代码由你控制，Service 通过构造函数注入 `HttpClient`（或 `IHttpClientFactory`）。

- `RichardSzalay.MockHttp` 提供替换的 `HttpMessageHandler`，Service 层代码原样执行，不需要网络；
- 用 `IHttpClientFactory` 时，可以在测试 setup 里**再次** `AddHttpClient("name")` 覆写 primary handler：同名注册是**追加**配置而非替换，Microsoft 文档明确说这用途就是「mocking a primary handler for tests」。

前提：Service 必须**注入** `HttpClient` / handler，不能在内部 `new HttpClient()`。

```csharp
// 概念示意
var mockHttp = new MockHttpMessageHandler();
mockHttp.When("https://api.example.com/users").RespondJson("""[{"id":1,"name":"张三"}]""");
var client = mockHttp.ToHttpClient();
client.BaseAddress = new Uri("https://api.example.com");
var vm = new UserListViewModel(new UserService(client));
```

#### 档 2：WireMock.Net（真实 HTTP 语义）

适用：需要验证**真实的 HTTP 行为**——序列化、状态码、响应头、重试、超时，或 API 是外部服务、尚不存在。

- WireMock.Net 启动进程内 HTTP 桩，客户端发出真实请求；
- **不要用固定端口**：让 WireMock 自选端口（端口 0），再从 `server.Urls[0]` 读回地址传给客户端。CI 上固定端口可能被占用。

```csharp
using var server = WireMockServer.Start();
var baseUrl = server.Urls[0];
```

#### 档 3：替换服务接口（最外层，谨慎）

适用：只测 VM 的编排逻辑（加载状态、错误提示、命令可用性），HTTP 细节已有其他测试覆盖。这时替换 `IUserService` 是合理的；但如果用它来「测」HTTP 处理，就会漏掉序列化和状态码分支。

#### 选择规则

| 你要证明的事 | 用哪档 |
| --- | --- |
| VM 在成功/失败/加载中三种状态下表现正确 | 档 3 |
| Service 正确构造请求、解析响应、处理 4xx/5xx | 档 1 |
| 真实网络栈、header、重试、超时、TLS 之外的传输语义 | 档 2 |
| 外部 API 契约 | 档 2（或契约测试） |

**禁止**：在单测里请求真实公网 API；用 `Thread.Sleep` 等网络；把 `HttpClient` 写成静态单例导致无法替换。

### 3.5 异步 VM 测试

- 用 `async Task` 测试方法，**不要** `.Result` / `.Wait()`（`AvaloniaFact` 下可能死锁）；
- CommunityToolkit.Mvvm 的 `[RelayCommand]` 生成的 `AsyncRelayCommand` 要等它完成：`await vm.LoadCommand.ExecuteAsync(null)`；
- 如果要验证「执行中按钮禁用」，在命令返回的 `Task` 上做中间断言，或注入一个可手动完成的 `TaskCompletionSource`；
- 时间相关逻辑（定时器、节流）把时间源抽象成接口，测试里用可控时钟；不要靠真实 `Task.Delay` 加宽容超时。


---

## 4. 本地存储（SQLite）怎么测

### 4.1 三条禁令

1. **不要**用 EF Core 的 in-memory provider 测查询逻辑。
   Microsoft 原文：它「will not behave like your real database in many important ways」，事务和原生 SQL **完全无法测**，大小写敏感性等行为也不同，并且明确写着「we discourage its use」；该 provider 已进入仅维护状态。
2. **不要**手写 fake `DbSet` / 自己实现 `IQueryable`。
   Microsoft 同样劝阻：「complex and difficult, and suffers from the same disadvantages」。
3. **不要**把 SQLite 当成生产数据库的证明。
   它是「最接近生产的测试替身」，不是生产本身：大小写、部分查询的翻译、`EF.Functions` 的 provider 专有方法（如 SQL Server 的 `DateDiffDay`）依然测不到。

### 4.2 推荐：SQLite in-memory，连接保持打开

关键机制：**每次打开底层连接都会新建一个内存数据库，连接关闭时数据库被删除**。`DbContext` 默认会按查询开关连接，所以必须自己先打开连接、交给 EF Core，并在测试结束时才关闭。

```csharp
public class OrderStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public OrderStoreTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();                       // 数据库此刻创建，随连接存活
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();         // 或 Migrate()，见 4.4
        context.Orders.Add(new Order { Id = 1, Total = 100m });
        context.SaveChanges();
    }

    private AppDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();   // 数据库随之删除
}
```

要点：

- **一个测试一个实例**：xUnit 每个 `[Fact]` 都会新建测试类实例，天然隔离；不要共享静态连接；
- **每个测试一个新 `DbContext`**：模拟真实的「短生命周期上下文」；
- 需要验证事务/约束/原生 SQL 时就用这个方式——它们会真实执行；
- 复杂 schema（视图、触发器）在 `EnsureCreated()` 之后用 `GetDbConnection().CreateCommand()` 执行 DDL。

### 4.3 分层：大部分用 SQLite，少量用生产 provider

| 层 | 测什么 | 工具 |
| --- | --- | --- |
| 仓储单元测试 | 查询组合、排序、分页、映射 | SQLite in-memory |
| 迁移测试 | schema 演进、`Migrate()` 是否可前进 | SQLite in-memory（或临时文件库） |
| 生产 provider 测试 | provider 专有函数、大小写规则、真实 SQL 方言 | 真实数据库或 Testcontainers，**少量**、可选、CI 门控 |

判断标准：**如果这条查询的语义依赖某个 provider 的实现，就必须有一条针对该 provider 的测试**；其余全部走 SQLite。

### 4.4 迁移与文件型数据库

- 只测 `EnsureCreated()` 会漏掉迁移本身的错误。至少有一条测试执行 `context.Database.Migrate()` 并断言表/列存在；
- 当需要测 WAL、连接池、多连接并发时，用**临时文件数据库**（`Path.GetTempFileName()` + `Dispose` 删除），而不是 `:memory:`；
- 临时文件路径要保证测试结束删除，且不要写进仓库目录。

### 4.5 应用程序边缘：不要重复测 EF Core

如果应用只是通过仓储读写，**VM 测试里用仓储接口替身**即可，不要在 VM 测试里启动数据库。数据库行为在仓储层测一次就够。

### 4.6 本仓库现状

本仓库目前没有 SQLite 依赖；上面的规范是为将来引入 `Microsoft.Data.Sqlite` / EF Core 的项目准备的。如果只用 `Microsoft.Data.Sqlite` 而不上 EF Core，做法一样：每个测试一个 `:memory:` 连接并保持打开，DDL 直接执行 SQL。


---

## 5. WPF 的差异（不能照搬 Avalonia）

### 5.1 关键事实

**原生 WPF 没有 Avalonia 那样的官方 in-process headless 平台。** 因此分层名字可以沿用，但机制必须换：

| 层 | Avalonia | 原生 WPF |
| --- | --- | --- |
| L0 单测 | xUnit `[Fact]` | xUnit `[Fact]`（与 Avalonia 相同） |
| L1 UI 测试 | `[AvaloniaFact]` + headless 平台 | `[WpfFact]`（Xunit.StaFact）+ 进程内创建真实 `Window`/控件，不 Show 或 Show 在虚拟屏上 |
| L2 视觉回归 | Skia headless 截帧 | `RenderTargetBitmap` 渲染控件树后比对；同样需要 STA 线程 |
| L3 端到端 | Appium + WinAppDriver | Appium + WinAppDriver，或 FlaUI（UIA3） |

**例外：Avalonia XPF**（商业的 WPF 兼容层）。官方文档建议「try Headless testing first as it's fast and portable」，做法与 Avalonia 基本一致，但有几处差别 [12]：

- 引用 `Avalonia.Headless.XUnit`（或 NUnit 版本），版本取 `$(XpfAvaloniaVersion)`；测试项目需要 `Xpf.Sdk`，TFM 用 `net8.0-windows`（Linux/macOS 上加 `EnableWindowsTargeting=true`）；
- **CI 里也必须有有效的 XPF 许可证**，否则运行时校验失败；
- AppBuilder 用 `Configure<DefaultXpfAvaloniaApplication>().WithAvaloniaXpf().UseSkia().UseHeadless(...)`；
- headless 输入扩展**只能用在 Avalonia `Window` 上**，要先 `XpfWpfAbstraction.GetAvaloniaWindowForWindow(wpfWindow)` 拿到对应窗口；
- 普通 `net8.0-windows` 的 WPF 控件库也能被这样的测试项目引用并测试；
- 测试间状态泄漏或 `TaskScheduler` 报错时，可设 `AvaloniaTestIsolationLevel.PerAssembly`。

如果团队已经有 XPF 许可证，WPF 项目的 L1 首选 XPF headless；没有的话按下面的 `[WpfFact]` 方案走。

### 5.2 WPF 的 L1：`[WpfFact]` + 进程内控件

`Xunit.StaFact` 提供 `[WpfFact]` / `[WpfTheory]`：把测试体放到 **STA 线程**并安装 WPF 的 `DispatcherSynchronizationContext`，效果等同于 GUI 主线程。官方描述为「Run your xunit-based tests on an STA thread with the WPF Dispatcher」。

规则：

- 任何 `new Window()` / `new Button()` / 依赖 `Dispatcher` 的测试都用 `[WpfFact]`；
- 非 Windows 平台会自动 **Skipped**（Windows-only 特性），所以 WPF 测试项目天然只能在 Windows 上跑；
- 需要参数化用 `[WpfTheory]`；
- 版本要与 xUnit 大版本匹配（Xunit.StaFact 2.x 对应 xUnit v3 1.x 等）。

**WPF L1 能覆盖的**：绑定、命令、转换器、模板应用（`ApplyTemplate()` 后查 `Template.FindName`）、依赖属性、样式触发器（需要 `IsLoaded` 或手动 raise）。
**WPF L1 覆盖不了的**：真实输入管线（键盘/鼠标要显式构造 `RoutedEventArgs` 或走 UIA）、窗口动画、DPI 与多显示器——这些仍归 L3。

### 5.3 WPF 的输入模拟

WPF 没有 Avalonia 那套 `Window.KeyTextInput` 扩展。可选：

1. 直接设 `TextBox.Text` + 手动 raise `TextChanged` —— **只测逻辑，不测输入管线**，不要用它冒充 UI 测试；
2. `InputManager`/`Keyboard` 的编程式输入在进程内不可靠；
3. 真正验证键盘路由、焦点、输入法 → **放到 L3**（FlaUI/UIA 或 Appium）。

**与 Avalonia 相同的教训**：在 WPF 里 `button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))` 同样**不会**执行 `OnClick` 里的 `Command`；要用 `button.Command.Execute(...)` 测命令、或用 UIA/自动化测点击。

### 5.4 WPF 的 L3

- **FlaUI（UIA3）**：进程外驱动，支持 WPF 原生；不需要 WinAppDriver，适合纯 Windows 桌面 CI（仍需桌面会话）。
- **Appium + WinAppDriver**：与 Avalonia 相同的前置条件（Windows 10+、开发者模式、显示器）。
- 选择：已有 Appium 基础设施就沿用；只做 WPF 且不想装 WinAppDriver，用 FlaUI。

### 5.5 共享的部分

以下内容 WPF 与 Avalonia 完全一致，规则可直接复用：

- ViewModel 纯单测（第 3 节）；
- 接口替身优先、HTTP mock 三档（第 3.3–3.4 节）；
- SQLite 测试（第 4 节）；
- Benchmark 集成（见 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md)）；
- 命名、断言、测试数据、CI 组织（第 6–8 节）。

---

## 6. 视觉回归测试（L2）

### 6.1 何时做

满足**全部**条件才做：

1. 你拥有自定义控件、主题或自绘渲染；
2. 外观本身就是需求（品牌色、圆角、图标对齐）；
3. 已经有一个 headless UI 测试流程可以复用。

**不要**给所有页面做像素基线：字体渲染、抗锯齿、SKU 差异会让测试长期红，维护成本超过收益。

### 6.2 做法（Avalonia）

```csharp
// 需要 UseSkia() + UseHeadlessDrawing = false，否则截到的是 1x1 位图
var png = window.CaptureFrame();   // 内部含 RunJobs + ForceRenderTimerTick
```

- 基线图片随测试项目提交，文件名包含用例名；
- 比对时给出**容差**（像素差异比例），不要做逐字节比较；
- 失败时输出「实际 / 预期 / 差异图」三张，方便人工判断；
- 基线更新必须是显式操作（如环境变量 `UPDATE_BASELINES=1`），不能在普通测试里自动覆盖。

### 6.3 什么时候跳过

- 动画中间帧（不稳定）；
- 依赖系统字体的文本排版；
- 高 DPI 下的亚像素渲染。

---

## 7. 测试代码的组织与命名

### 7.1 文件划分

```
tests/Avalonia.<X>Demo.Tests/
  TestAppBuilder.cs       // 只写 HeadlessApp.Build<App>() 一行
  ViewModelTests.cs       // L0：纯 xUnit
  InteractionTests.cs     // L1：headless + [AvaloniaFact]
  SmokeTests.cs           // L1：每个页面能构造、每个 Tab 能切换
  VisualTests.cs          // L2（可选）
```

**一个测试类只服务一种模式**：`[Fact]` 和 `[AvaloniaFact]` 不要混在同一个类里，否则读者无法判断某个测试需不需要 UI 线程。

### 7.2 命名

- 测试方法名描述**行为**而不是实现：`Decrement_is_disabled_at_zero_and_notified_when_count_changes`、`Bad_email_is_reported_and_blocks_submit`；
- 命中多个输入用 `[Theory] + [InlineData]`；
- 测试类内不写 `[Trait]` 分类也能通过目录区分层级（`*.Tests` vs `*.AppiumTests`）。

### 7.3 共享辅助

- 通用辅助放 `tests/Avalonia.Tests.Common` / `tests/Avalonia.Appium.Common`，**不要**在各测试项目复制；
- 辅助方法失败时**抛异常**，不要返回 `bool`（参考 `UiHelpers.WaitUntil`）；
- 辅助方法内部要处理本仓库已知的时序坑（`RunJobs`、`BringIntoView`、`Focus`）。

### 7.4 断言

- 断言用户可见结果，不断言私有实现细节；
- 集合断言顺序敏感时写清楚（`Assert.Equal(expected, actual)`）；
- 异常断言同时校验异常类型和消息（本仓库校验中文消息）；
- 绑定到**结构体属性**的情况必须在实时控件上断言——绑定失败是**静默**的，构建和冒烟测试都检不出（见记忆 `avalonia-silent-binding-failures`）。

---

## 8. 覆盖率与冒烟测试

### 8.1 冒烟测试（必做）

每个应用至少有一条：

- 主窗口能构造并 `Show()`；
- 每个 Tab / 页面能选中且不抛异常（本仓库 `TabWalker.Walk`，单个页面失败不会掩盖后面的页面）；
- 主 ViewModel 能构造。

这条测试能在几十毫秒内抓住 XAML 解析错误、缺资源、构造函数异常——价值远高于覆盖率数字。

### 8.2 覆盖率

- 不追求数字目标；用覆盖率找**没被测到的分支**，而不是当 KPI；
- 优先覆盖：`CanExecute` 的每个分支、错误路径、边界值、空集合；
- 不覆盖：纯 UI 修饰（颜色、间距）、样板属性、为演示而存在的代码。

### 8.3 测试数量配额（经验值）

| 层 | 每个应用的典型数量 | 说明 |
| --- | --- | --- |
| L0 单测 | 5–20 | 主要价值所在 |
| L1 headless | 5–15 | 覆盖绑定、布局、输入、关键交互 |
| L2 视觉 | 0–3 | 只给自定义控件 |
| L3 Appium | 1–3 | 只给关键旅程 |

---

## 9. CI 与执行顺序

```yaml
# 顺序即优先级：先快后慢，先阻断后告警
1. dotnet build hello-avalonia.slnx
2. dotnet test hello-avalonia.slnx            # L0 + L1，必须通过
3. （可选，Windows 桌面会话）dotnet test tests/Avalonia.*.AppiumTests
4. （独立阶段，见性能基准规范）dotnet run -c Release benchmarks/... -- --exporters json
5. 基准结果上传/比较（告警或门禁）
```

规则：

- **L0/L1 必须在每次提交上跑**，它们是唯一的强制门禁；
- Appium 用环境变量门控，默认跳过；CI 上显式开启时要确认 WinAppDriver 已启动、桌面会话可用；
- 关闭测试并行化时要显式配置（Appium 会接管鼠标键盘，本仓库已用 Mutex 串行化；xUnit 默认按测试类并行，headless 测试要避免共享静态状态；本仓库未单独验证 headless 下的并行行为）；
- 测试中的区域设置要固定（`CultureScope.Invariant()`），否则格式化断言会随机器失败；
- 渲染/时序相关的测试要接受「重跑一次」策略，但**不允许**用重试掩盖真实失败。


---

## 10. 证据与置信度

### 10.1 一手来源与结论对应

| # | 来源 | 支撑的规则 | 置信度 |
| --- | --- | --- | --- |
| [1] | https://docs.avaloniaui.net/docs/testing/ | 分层策略、四层职责、Appium 的定位 | 高（官方原文） |
| [2] | https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform | headless 能力边界、输入辅助方法、`RaiseEvent(Click)` 不执行命令、`RunJobs`/`ForceRenderTimerTick`、VM 测试不需要 headless | 高 |
| [3] | https://docs.avaloniaui.net/docs/testing/headless-xunit | `[AvaloniaFact]`/`[AvaloniaTheory]`、`AvaloniaTestApplication`、主题包要求 | 高 |
| [4] | https://docs.avaloniaui.net/docs/testing/ui-testing-with-appium | WinAppDriver 前置条件、Linux 无稳定驱动、Appium 覆盖范围 | 高 |
| [5] | https://learn.microsoft.com/en-us/ef/core/testing/ 及 choosing-a-testing-strategy | in-memory provider 与 fake DbSet 被劝阻、SQLite 作为关系型替身 | 高 |
| [6] | https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database | SQLite in-memory 连接保持打开的写法 | 高 |
| [7] | https://wiremock.org/dotnet/ 及 using-wiremock-in-unittests | WireMock 定位、动态端口 | 高 |
| [8] | https://www.nuget.org/packages/RichardSzalay.MockHttp | 替换 `HttpMessageHandler`、无需网络 | 高 |
| [9] | https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting | 同名 `AddHttpClient` 追加配置、可用于 mock primary handler | 高 |
| [10] | https://nsubstitute.github.io/help/creating-a-substitute/ 及 how-nsub-works | 非 virtual 成员执行真代码、Castle 子类代理 | 高 |
| [11] | https://github.com/AArnott/Xunit.StaFact | `[WpfFact]`/`[WpfTheory]`、STA + WPF Dispatcher 同步上下文、非 Windows 标记 Skipped | 高（官方 README） |
| [12] | https://docs.avaloniaui.net/xpf/testing/headless-testing | XPF headless 的包、许可证、AppBuilder、窗口桥接、隔离级别 | 高（官方原文） |

### 10.2 未充分核实的部分（使用时需自行验证）

- **Avalonia XPF headless**：已读官方页 [12]，但未在本仓库实测；页面没有列出 XPF headless 测不了的 WPF 功能。
- **WPF 进程内 UI 测试的边界**：`[WpfFact]` + `Window` 能测什么、不能测什么，是本报告基于机制推断的结论，没有 WPF 官方文档直接背书；输入模拟部分尤其如此。
- **FlaUI 与 UIA3**：作为 WPF 的 L3 替代方案是合理选择，本次未逐一核验其文档。
- **覆盖率阈值、测试数量配额（第 8.3 节）**：经验值，不是官方结论。

### 10.3 被排除的结论

深度调研过程中有 3 条候选结论被 3 票核验否决（如「所有 Appium 定位都必须依赖 AutomationId」「WireMock 一定是外进程」），已从本文删除。另有一条被部分否决：WireMock 是**进程内** HTTP 桩，不是外进程服务——文中已按正确表述书写。

---

## 11. 与本仓库既有规则的对照

| 本文规则 | 仓库现状 | 行动 |
| --- | --- | --- |
| Headless 为默认 UI 层 | `tests/Avalonia.Tests.Common/HeadlessApp.cs` 已用 Skia + headless | 保持 |
| 统一 `HeadlessApp.Build<App>()` | 各测试项目 `TestAppBuilder` 一行调用 | 保持 |
| 输入走 `UiHelpers` | `Click`/`Type`/`WaitUntil` 已封装 | 保持，新增能力先加到 Common |
| Appium 环境变量门控 | `HELLO_AVALONIA_APPIUM=1` + `AppiumFactAttribute` | 保持 |
| 区域设置固定 | `CultureScope.Invariant()` | 保持 |
| 冒烟测试 | `SmokeTests.cs` + `TabWalker.Walk` | 保持 |
| SQLite 测试 | 暂无 SQLite | 引入时按第 4 节执行 |
| Benchmark | 已有 `benchmarks/Avalonia.Benchmarks`，`dotnet test` 不执行它（已实测） | 保持，按 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md) 扩展 |
| WPF 项目 | 本仓库无 WPF 项目 | 新增 WPF 项目时按第 5 节执行 |
