# 桌面端 MVVM 项目测试规范（通用部分）

> 适用范围：本仓库（`hello-avalonia`）以及同类型的桌面端 MVVM 项目。本文只写**与 UI 框架无关**的规则：分层模型、ViewModel / 服务层、HTTP mock、SQLite、组织与命名、覆盖率、CI。
>
> 框架专属规则见：
>
> - [`2026-10-18-avalonia-testing-standards.md`](2026-10-18-avalonia-testing-standards.md)：Avalonia 的 Headless / Appium、视觉回归、本仓库的测试项目布局；
> - [`2026-10-18-wpf-testing-standards.md`](2026-10-18-wpf-testing-standards.md)：WPF 的 `[WpfFact]`、Avalonia XPF、FlaUI。
>
> 性能基准（BenchmarkDotNet）不在本文范围，见 [`2026-10-18-benchmarkdotnet-standards.md`](2026-10-18-benchmarkdotnet-standards.md)。
>
> 证据来源：Avalonia 测试文档、Microsoft EF Core 测试文档、WireMock.Net / NSubstitute / MockHttp 官方文档（均为一手）。正文方括号是本文自己的出处编号，见文末「证据与置信度」。

---

## 0. 一页结论

| 问题 | 结论 |
| --- | --- |
| 测试策略选一种还是分层？ | **分层**，单元测试先行 [1] |
| UI 测试怎么选？ | 见框架专属文档：Avalonia 默认 **Headless**，WPF 默认 **`[WpfFact]`** |
| ViewModel 测试要不要启动 UI 框架？ | **不需要**。纯 xUnit `[Fact]`，除非 VM 内部碰了 `Dispatcher` / `Application.Current` 等平台服务 [2] |
| 接口请求怎么 mock？ | 按要证明的事选档：Service 的请求与解析 → **替换 `HttpMessageHandler`**（首选）；VM 编排 → **接口替身**；真实 HTTP 语义 → **WireMock.Net**（见 2.4） |
| SQLite 怎么测？ | **SQLite in-memory（连接保持打开）**；禁用 EF Core in-memory provider，禁止手写 fake `DbSet` |

---

## 1. 分层模型

### 1.1 四层

| 层 | 测什么 | Avalonia 工具 | WPF 工具 | 速度 | 依赖显示器 |
| --- | --- | --- | --- | --- | --- |
| L0 单元测试 | ViewModel、Service、Converter、纯算法 | xUnit `[Fact]` / `[Theory]` | 同左 | 快 | 否 |
| L1 进程内 UI | 控件行为、布局、样式、绑定、输入、路由 | `Avalonia.Headless.XUnit` + `[AvaloniaFact]` | `[WpfFact]`（Xunit.StaFact），或 XPF headless | 快 | 否 |
| L2 视觉回归 | 渲染出的像素、自定义控件外观、主题 | Skia + `CaptureRenderedFrame()` | `RenderTargetBitmap` | 中 | 否 |
| L3 端到端 | 完整应用、原生窗口、菜单、焦点、无障碍 | Appium + WinAppDriver（仅 Windows） | Appium + WinAppDriver，或 FlaUI（UIA3） | 慢 | **是** |

这是三份文档里唯一一张完整的分层表，框架文档只补「本仓库落在哪」。

Avalonia 官方原文把四层写成 "Choosing the right approach"：单元测试先行，headless 覆盖依赖 Avalonia 属性系统/布局/输入的行为，视觉回归仅在你拥有自定义控件或主题、像素正确性重要时才做，Appium 留给关键流程、平台特性和无障碍 [1]。

选层口诀与逐项判断表见 Avalonia 文档「Headless 还是 Appium」一节。

### 1.2 为什么是这个顺序

- L0/L1 单次运行在毫秒级，可以每个提交都跑；
- L2 需要维护基线图片，成本高、易受抗锯齿和字体差异影响，只覆盖「外观即正确性」的少数场景；
- L3 每次启动真实进程、占用鼠标键盘、需要 WinAppDriver 和桌面会话，**必须是少数**。

**反模式**：把所有 UI 验证都塞进 Appium（慢、脆、只能跑 Windows 桌面会话）；或者完全不做 L1，只靠 L0 单测（绑定、样式、模板错误全部漏网）。

---

## 2. ViewModel 与服务层测试

> 本节和第 3 节推荐的 NSubstitute、RichardSzalay.MockHttp、WireMock.Net、Microsoft.Data.Sqlite 目前**都没有**登记在 `Directory.Packages.props`。使用前先在那里登记版本，csproj 的 `PackageReference` 不写 `Version`（中央包管理）。

### 2.1 默认形态：纯 xUnit，不启动 UI 框架

Avalonia 官方文档专门有一节 "Testing view models without UI" [2]，原话是「You only need the headless platform when your test involves Avalonia controls, layout, or input」，示例注释也写着 `// No [AvaloniaTest] needed, just a regular [Fact]` [2]。

规则：

- `ViewModelTests.cs` 里直接 `new XxxViewModel()`，用 `[Fact]` / `[Theory]`；
- 断言属性值、`CanExecute`、`Execute` 的副作用、`INotifyPropertyChanged` / `ObservableCollection` 通知；
- 需要 `DataValidationException` 之类的行为时直接断言异常和旧值保留；
- 只有 VM **内部**碰了 `Dispatcher.UIThread`、`Application.Current`、`AssetLoader`、`Bitmap`、`StreamGeometry.Parse` 等平台服务时，才升级为框架的 UI 测试特性（Avalonia 的 `[AvaloniaFact]`、WPF 的 `[WpfFact]`）。

**判断方法**：把 `new` 出来的 VM 单独跑，失败信息里出现 `Dispatcher` / `Platform` / `AssetLoader` 字样 → 升级到 UI 测试层；否则保持纯单测。

### 2.2 被测单元是 VM，边界是「它的协作者」

VM 测试不需要模拟 UI，但需要替换 VM 依赖的四类协作者：

| 协作者类型 | 推荐做法 | 理由 |
| --- | --- | --- |
| 你自己定义的接口（`IUserService`、`IDialogService`） | 接口替身（NSubstitute / Moq / 手写 fake） | 简单、快、无网络 |
| `HttpClient` / HTTP API | 替换 `HttpMessageHandler` 或用 WireMock.Net | 保留真实序列化与状态码处理 |
| 数据库 / 仓储 | SQLite in-memory 或仓储接口替身 | 见第 3 节 |
| 文件系统、时钟、随机数 | 包装成接口（`IClock`、`IFileSystem`） | 否则测试不确定、不可重复 |

### 2.3 替身必须替在接口上（否则会静默跑真代码）

NSubstitute 的创建机制是 Castle DynamicProxy 生成子类，所以：

- 只拦截**可从测试程序集重写**的成员：`public/protected virtual`，或 `internal virtual` + `InternalsVisibleTo`；
- **非 virtual 成员会真的执行基类实现**，构造函数也会执行；
- 官方原文警告：「real code could be inadvertently executed in your test」「any non-virtual code in the class will actually execute!」[8]

C# 方法默认非 virtual，所以**默认情况下替身具体类是危险的**。规则：

1. 被替身的服务优先定义接口；
2. 需要替身具体类时，成员必须 `virtual`，并显式写一个测试确认「替身没有跑真代码」（例如真实现会抛异常/写日志，断言没有发生）；
3. 不要用「部分替身」去绕过不可测的设计——那是设计问题，不是测试问题。

### 2.4 HTTP mock 的三档策略（由内向外）

#### 档 1：替换 `HttpMessageHandler`（首选，零网络）

适用：客户端代码由你控制，Service 通过构造函数注入 `HttpClient`（或 `IHttpClientFactory`）。

- `RichardSzalay.MockHttp` 提供替换的 `HttpMessageHandler`，Service 层代码原样执行，不需要网络 [6]；
- 用 `IHttpClientFactory` 时，可以在测试 setup 里**再次** `AddHttpClient("name")` 覆写 primary handler：同名注册是**追加**配置而非替换，Microsoft 文档明确说这用途就是「mocking a primary handler for tests」[7]。

前提：Service 必须**注入** `HttpClient` / handler，不能在内部 `new HttpClient()`。

```csharp
// 概念示意
var mockHttp = new MockHttpMessageHandler();
mockHttp.When("https://api.example.com/users")
        .Respond("application/json", """[{"id":1,"name":"张三"}]""");
var client = mockHttp.ToHttpClient();
client.BaseAddress = new Uri("https://api.example.com");
var vm = new UserListViewModel(new UserService(client));
```

#### 档 2：WireMock.Net（真实 HTTP 语义）

适用：需要验证**真实的 HTTP 行为**——序列化、状态码、响应头、重试、超时，或 API 是外部服务、尚不存在。

- WireMock.Net 启动进程内 HTTP 桩，客户端发出真实请求 [5]；
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

### 2.5 异步 VM 测试

- 用 `async Task` 测试方法，**不要** `.Result` / `.Wait()`（在 `[AvaloniaFact]` / `[WpfFact]` 的单线程调度器下可能死锁）；
- CommunityToolkit.Mvvm 的 `[RelayCommand]` 生成的 `AsyncRelayCommand` 要等它完成：`await vm.LoadCommand.ExecuteAsync(null)`；
- 如果要验证「执行中按钮禁用」，在命令返回的 `Task` 上做中间断言，或注入一个可手动完成的 `TaskCompletionSource`；
- 时间相关逻辑（定时器、节流）把时间源抽象成接口，测试里用可控时钟；不要靠真实 `Task.Delay` 加宽容超时。


---

## 3. 本地存储（SQLite）怎么测

### 3.1 三条禁令

1. **不要**用 EF Core 的 in-memory provider 测查询逻辑。
   Microsoft 原文：它「will not behave like your real database in many important ways」，事务和原生 SQL **完全无法测**，大小写敏感性等行为也不同，并且明确写着「we discourage its use」；该 provider 已进入仅维护状态 [3]。
2. **不要**手写 fake `DbSet` / 自己实现 `IQueryable`。
   Microsoft 同样劝阻：「complex and difficult, and suffers from the same disadvantages」[3]。
3. **不要**把 SQLite 当成生产数据库的证明。
   它是「最接近生产的测试替身」，不是生产本身：大小写、部分查询的翻译、`EF.Functions` 的 provider 专有方法（如 SQL Server 的 `DateDiffDay`）依然测不到。

### 3.2 推荐：SQLite in-memory，连接保持打开

关键机制：**每次打开底层连接都会新建一个内存数据库，连接关闭时数据库被删除**。`DbContext` 默认会按查询开关连接，所以必须自己先打开连接、交给 EF Core，并在测试结束时才关闭 [4]。

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
        context.Database.EnsureCreated();         // 或 Migrate()，见 3.4
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

### 3.3 分层：大部分用 SQLite，少量用生产 provider

| 层 | 测什么 | 工具 |
| --- | --- | --- |
| 仓储单元测试 | 查询组合、排序、分页、映射 | SQLite in-memory |
| 迁移测试 | schema 演进、`Migrate()` 是否可前进 | SQLite in-memory（或临时文件库） |
| 生产 provider 测试 | provider 专有函数、大小写规则、真实 SQL 方言 | 真实数据库或 Testcontainers，**少量**、可选、CI 门控 |

判断标准：**如果这条查询的语义依赖某个 provider 的实现，就必须有一条针对该 provider 的测试**；其余全部走 SQLite。

### 3.4 迁移与文件型数据库

- 只测 `EnsureCreated()` 会漏掉迁移本身的错误。至少有一条测试执行 `context.Database.Migrate()` 并断言表/列存在；
- 当需要测 WAL、连接池、多连接并发时，用**临时文件数据库**（`Path.GetTempFileName()` + `Dispose` 删除），而不是 `:memory:`；
- 临时文件路径要保证测试结束删除，且不要写进仓库目录。

### 3.5 应用程序边缘：不要重复测 EF Core

如果应用只是通过仓储读写，**VM 测试里用仓储接口替身**即可，不要在 VM 测试里启动数据库。数据库行为在仓储层测一次就够。

### 3.6 本仓库现状

本仓库目前没有 SQLite 依赖；上面的规范是为将来引入 `Microsoft.Data.Sqlite` / EF Core 的项目准备的。如果只用 `Microsoft.Data.Sqlite` 而不上 EF Core，做法一样：每个测试一个 `:memory:` 连接并保持打开，DDL 直接执行 SQL。


---

## 4. 测试代码的组织与命名

### 4.1 命名

- 测试方法名描述**行为**而不是实现：`Decrement_is_disabled_at_zero_and_notified_when_count_changes`、`Bad_email_is_reported_and_blocks_submit`；
- 命中多个输入用 `[Theory] + [InlineData]`；
- 层级按项目区分：`*.Tests` 是 L0 + L1，`*.AppiumTests` 是 L3；不用 `[Trait]` 再分类。

### 4.2 共享辅助

- 通用辅助放共用库，**不要**在各测试项目复制（本仓库的共用库见 Avalonia 文档的「测试代码的组织」）；
- 辅助方法失败时**抛异常**，不要返回 `bool`（参考 `UiHelpers.WaitUntil`）：超时被当成通过是最隐蔽的假绿；
- 辅助方法内部要处理已知的时序坑，让调用方不必每次重复。

### 4.3 断言

- 断言用户可见结果，不断言私有实现细节；
- 集合整体比较用 `Assert.Equal(expected, actual)`，它对顺序敏感；顺序无关时先排序再比较，不要逐项 `Assert.Contains` 漏掉多余元素；
- 异常断言同时校验异常类型和消息（本仓库校验中文消息）。

---

## 5. 覆盖率与冒烟测试

### 5.1 冒烟测试（必做）

每个应用至少有一条：

- 主窗口能构造并 `Show()`；
- 每个 Tab / 页面能选中且不抛异常（Avalonia 项目见其文档中的 `TabWalker.Walk`）；
- 主 ViewModel 能构造。

这条测试能在几十毫秒内抓住 XAML 解析错误、缺资源、构造函数异常——价值远高于覆盖率数字。

### 5.2 覆盖率

- 不追求数字目标；用覆盖率找**没被测到的分支**，而不是当 KPI；
- 优先覆盖：`CanExecute` 的每个分支、错误路径、边界值、空集合；
- 不覆盖：纯 UI 修饰（颜色、间距）、样板属性、为演示而存在的代码。

### 5.3 测试数量配额（经验值）

| 层 | 每个应用的典型数量 | 说明 |
| --- | --- | --- |
| L0 单测 | 5–20 | 主要价值所在 |
| L1 headless | 5–15 | 覆盖绑定、布局、输入、关键交互 |
| L2 视觉 | 0–3 | 只给自定义控件 |
| L3 Appium | 1–3 | 只给关键旅程 |

---

## 6. CI 与执行顺序

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
- 并行：xUnit 默认按测试类并行，测试之间不要共享静态状态；Appium 会接管鼠标键盘，本仓库已由 `AppSession` 的跨进程 Mutex 串行化，无需额外配置；
- 测试中的区域设置要固定（`CultureScope.Invariant()`），否则格式化断言会随机器失败；
- CI 不配自动重试。时序类测试偶发失败时，先按框架文档补冲刷（Avalonia 见「异步与渲染同步」），不要靠重跑变绿。

---

## 7. 证据与置信度

### 7.1 一手来源与结论对应

| # | 来源 | 支撑的规则 | 置信度 |
| --- | --- | --- | --- |
| [1] | https://docs.avaloniaui.net/docs/testing/ | 分层策略、单元测试先行 | 高 |
| [2] | https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform | VM 测试不需要 headless | 高 |
| [3] | https://learn.microsoft.com/en-us/ef/core/testing/ 及 choosing-a-testing-strategy | in-memory provider 与 fake DbSet 被劝阻、SQLite 作为关系型替身 | 高 |
| [4] | https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database | SQLite in-memory 连接保持打开的写法 | 高 |
| [5] | https://wiremock.org/dotnet/ 及 using-wiremock-in-unittests | WireMock 定位（进程内桩）、动态端口 | 高 |
| [6] | https://www.nuget.org/packages/RichardSzalay.MockHttp | 替换 `HttpMessageHandler`、无需网络 | 高 |
| [7] | https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting | 同名 `AddHttpClient` 追加配置、可用于 mock primary handler | 高 |
| [8] | https://nsubstitute.github.io/help/creating-a-substitute/ 及 how-nsub-works | 非 virtual 成员执行真代码、Castle 子类代理 | 高 |

### 7.2 未充分核实的部分（使用时需自行验证）

- **覆盖率阈值、测试数量配额（第 5.3 节）**：经验值，不是官方结论。
- 框架专属的未核实项见各自文档。
