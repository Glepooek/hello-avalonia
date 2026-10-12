# 演示项目测试覆盖设计：Headless + Appium

> 状态：待审阅。参考实现：`Glepooek/Avalonia` 的 `tests/`（Headless 系列项目 + `Avalonia.IntegrationTests.Appium`）。

## 目标

为 `src/` 下全部 20 个可运行演示应用补充两层自动化测试，并让后续新增演示按同一套模板接入：

1. **Headless 测试**：不开真实窗口，快、稳，可在 CI 上跑；验证页面能构造、能布局、ViewModel 逻辑正确、关键交互有效。
2. **Appium 测试**：驱动真实进程与真实窗口，验证 Headless 覆盖不到的部分（真实启动、真实 UIA 无障碍树、原生 WebView、跨窗口行为）。

成功标准：

- 每个演示项目都有对应的 Headless 测试项目，`dotnet test` 全绿。
- 每个演示项目都有 Appium 测试类；在本机满足前置条件时全绿，不满足时**跳过而不是失败**。
- 默认的 `dotnet build hello-avalonia.slnx` 与 `dotnet test hello-avalonia.slnx` 不依赖 WinAppDriver，也不会接管用户的鼠标键盘。

## 范围与非目标

范围：

- 20 个应用：`AppDevelopmentDemo`、`BehaviorsDemo`、`ControlsDemo`、`CustomControlsDemo`、`DataBindingDemo`、`DataTemplatesDemo`、`EventsDemo`、`FundamentalsDemo`、`GraphicsDemo`、`HtmlRendererDemo`、`InputDemo`、`LayoutDemo`、`MusicStore`、`PropertySystemDemo`、`ServicesDemo`、`StarterTutorialDemo`、`StylingDemo`、`TestingDemo`、`WebViewDemo`、`XamlDemo`。
- `Avalonia.Shared` 是类库，不单独建测试项目，由各应用的测试间接覆盖。
- `TestingDemo` 已有 24 个 Headless 测试，保留，并把其公共辅助代码迁到共享测试库；不重写测试本身。

非目标：

- 不追求逐控件、逐属性的 100% 行为覆盖。覆盖契约见「测试分层」。
- 不做 macOS / Linux 的 Appium（参考库做了，本库只有 Windows 目标需求，YAGNI）。
- 不改演示项目的业务代码；仅在 Appium 确需定位时，给被测控件补 `AutomationProperties.AutomationId`（见「Appium 定位」）。
- 不接 CI 配置、不做视频录制、不做性能基准。
- 不为 `WebViewDemo` 里网页的内容本身写断言，只验证窗口能弹出、按钮能触发。
- 不为 `MusicStore` 的 iTunes 网络结果写断言（见「风险」）。

## 已核实的事实

### 参考库（Glepooek/Avalonia `tests/`）的做法

- 约 30 个测试项目，**Headless 是主力**，Appium 只有一个项目 `Avalonia.IntegrationTests.Appium`，专测需要真实 OS 的行为（原生菜单、托盘、拖放、弹出层）。
- Appium 项目的结构：`DefaultAppFixture`（启动被测应用并建会话）→ `TestBase`（构造时点击侧边 `Pager` 切到某页）→ 各 `*Tests` 类用 `[Collection("Default")]` 共享同一会话。
- Windows 走 WinAppDriver，会话地址 `http://127.0.0.1:4723`，用 `AppiumOptions` 的 `app` 能力指定 exe 路径；元素统一用 `FindElementByAccessibilityId` 定位。
- 客户端包为 `Appium.WebDriver` 4.4.5（Appium 1 协议），并有 `APPIUM1/APPIUM2` 编译开关；用 `PlatformFactAttribute` 按操作系统跳过用例。
- 被测应用是一个专门的 `IntegrationTestApp`，每个控件都带 `AutomationId`。

### 本库现状（子代理盘点，grep 级别，未运行）

| 事实 | 对设计的影响 |
|---|---|
| 20 个应用的 `Program` 是 `internal`，无 `InternalsVisibleTo` | 测试不能复用 `Program.BuildAvaloniaApp()`，要自己 `AppBuilder.Configure<App>()`（现有 TestingDemo.Tests 就是这样） |
| 各应用的 `App`、`MainWindow` 都是 `public`，`MainWindow` 都有无参构造 | Headless 可直接 `new MainWindow()` |
| `[assembly: AvaloniaTestApplication]` 每个程序集只能有一个，且指向一个 `App` | 各应用的 `App.axaml` 样式/资源不同，**每个应用需要独立的测试程序集** |
| 16 个应用用顶层 `TabControl` 导航；`ControlsDemo` 用 `SplitView + TreeView`（121 个页面）；`HtmlRendererDemo`、`MusicStore`、`WebViewDemo` 无页面导航 | 通用"遍历全部标签页"的冒烟测试可覆盖 16 个，其余 4 个单独处理 |
| 只有 `TestingDemo` 给控件设了 `AutomationId`（16 处）；其余应用有 `Name` 但几乎没有 `AutomationId` | Appium 定位是最大的不确定项，见「Appium 定位」 |
| 有大量时间/平台相关页面：`DispatcherTimer`、`Task.Delay`、动画、`StorageProvider`、剪贴板、`Launcher`、`WebView`、窗口管理 | 这些页面在 Headless 里只断言"能构造并布局"，不断言副作用 |
| `Directory.Packages.props` 已有 `xunit.v3` 3.2.2、`Avalonia.Headless.XUnit` 12.1.2、`Microsoft.NET.Test.Sdk`；**没有任何 Appium/Selenium 包** | 需要新增 `Appium.WebDriver` 登记 |

### 本机环境（已确认）

- `C:\Program Files\Windows Application Driver\WinAppDriver.exe` 已安装。
- Windows 开发者模式已开启（`AllowDevelopmentWithoutDevLicense = 1`）。
- 已装 Node.js，但未装 `appium` 命令；Appium 1 + WinAppDriver 的方案不需要它。
- 批 0 已实测（见「批 0 实测结论」）：WinAppDriver 能驱动 Avalonia 12 窗口，`Appium.WebDriver` 4.4.5 能在 net10.0 上工作，但点击后读取状态需要等待。

## 架构

```
tests/
├─ Avalonia.Tests.Common/            共享测试库（新）：UiHelpers、标签页遍历、Headless AppBuilder 工厂
├─ Avalonia.TestingDemo.Tests/       已有；改为引用 Common
├─ Avalonia.<X>Demo.Tests/           新增 ×19，每个应用一个 Headless 测试项目
└─ Avalonia.Demos.AppiumTests/       新增 ×1，所有应用的 Appium 测试集中在一个项目
```

### 为什么 Headless 每个应用一个项目

`[assembly: AvaloniaTestApplication]` 每个程序集只能指向一个 `App`，而各应用的 `App.axaml` 带着自己的主题、样式与资源。合并成一个项目就得用一个中立的 `Application`，页面里的 `StaticResource`、自定义样式会找不到。代价是项目数多（20 个），但每个项目只有三五个文件，由模板生成。

### 为什么 Appium 只用一个项目

Appium 驱动的是外部进程，测试程序集不引用任何应用，也就没有"一个程序集一个 App"的限制。集中后共享一个 `AppSession` 夹具，只维护一份驱动配置。

### `Avalonia.Tests.Common`（共享测试库）

普通类库（非 Exe），引用 `Avalonia.Headless` 与 `Avalonia.Skia`，不引用 xunit，也不引用任何应用。内容：

| 类型 | 职责 |
|---|---|
| `UiHelpers` | 从 `TestingDemo.Tests` 迁入并改为 `public`：`Show`、`ByName`、`ByAutomationId`、`Click`、`Type`，签名与行为不变 |
| `HeadlessApp.Build<TApp>()` | 返回 `AppBuilder.Configure<TApp>().UseSkia().UseHeadless(UseHeadlessDrawing=false)`，各项目的 `TestAppBuilder` 只剩一行调用 |
| `TabWalker` | 对任意 `Window` 找到第一个 `TabControl`，逐个选中每个 `TabItem` 并强制布局，返回每页的 `Header` 与异常（单页抛错不中断后面的页）；冒烟测试在一个 `[AvaloniaFact]` 里断言页头列表与无异常 |
| `LogCollector` | 收集 Warning 及以上的 Avalonia 日志，`Dispose` 时还原 `Logger.Sink`；独立提供，是否接入冒烟测试由批 1 逐页确认后定 |

现有 `TestingDemo.Tests` 迁移后 24 个测试必须原样通过；这是批 0 的回归基线。

## 测试分层与覆盖契约

"全部演示项目都有测试"按以下契约衡量，而不是按行覆盖率。

### Headless 层（每个应用）

| 层 | 内容 | 适用 |
|---|---|---|
| **H1 冒烟** | `new MainWindow()` 能显示；每个标签页/导航节点都能选中并布局而不抛异常；页面数等于预期值（防止漏页或误删页） | 全部 20 个 |
| **H2 ViewModel** | 每个带逻辑的 ViewModel 用普通 `[Fact]`/`[Theory]` 测命令可执行状态、属性联动、校验；不起 Avalonia | 有 `ViewModels/` 的项目 |
| **H3 交互** | 在含可交互控件的页面里，至少一条"点击/输入 → 断言界面变化"的用例，沿用 `UiHelpers.Click/Type` | 有交互页面的项目 |

H1 对 16 个 `TabControl` 应用共用 `TabWalker`；`ControlsDemo` 遍历 `PageCatalog` 的 121 个节点；其余 3 个单独写。

H3 的"至少一条"是下限。哪些页面值得测，由各批 plan 在读过页面代码后列出，spec 不预先枚举。

不进入 Headless 的内容（只断言"能构造"）：`StorageProvider` 文件对话框、剪贴板、`Launcher`、`NativeWebView`、窗口管理弹窗、依赖真实时间的动画。

### Appium 层

| 层 | 内容 | 适用 |
|---|---|---|
| **A1 启动** | 启动 exe，主窗口在 N 秒内出现，标题非空 | 全部 20 个 |
| **A2 导航** | 通过 UIA 枚举并逐个点击标签页，每页都有可见元素 | 16 个 `TabControl` 应用 |
| **A3 流程** | 1–2 条只有真实进程才能验证的流程（真实键鼠、真实 UIA 属性、原生窗口） | 按应用挑选，见下 |

A3 的候选（由对应批的 plan 确认）：

- `TestingDemo`：计数器、表单、列表三个面板的完整操作，作为 Appium 基准样例，因为它已有全套 `AutomationId`。
- `WebViewDemo`：点击按钮后 `NativeWebView`/`NativeWebDialog` 窗口真的弹出——Headless 做不到。
- `InputDemo` / `EventsDemo`：真实鼠标与键盘事件、焦点移动。
- `ServicesDemo`：文件对话框打开后能取消（验证不崩溃，不选文件）。
- `AppDevelopmentDemo`：无障碍页，对照 `ok-btn` 的 `AutomationId` 与 UIA 树。

Appium 不重复 H2；ViewModel 逻辑只在 Headless 测一次。

## Appium 设计

### 技术栈（沿用参考库）

- 客户端：`Appium.WebDriver` 4.4.5（Appium 1 协议），在 `Directory.Packages.props` 登记；只写 Windows，不带参考库的 `APPIUM2` 开关与 macOS 驱动。
- 服务端：WinAppDriver，监听 `http://127.0.0.1:4723`。**夹具不负责启动它**，由使用者手动运行；没运行就跳过（见「运行门控」）。
- 会话：每个应用一个 `AppSession`（`IClassFixture` 级别），用 `app` 能力指向 `src\<proj>\bin\Debug\<proj>.exe`，会话关闭即结束进程。路径通过向上查找 `hello-avalonia.slnx` 定位仓库根，再找 `Debug`、`Release` 中较新的 exe。
- 构建顺序：Appium 项目对 20 个应用做 `ProjectReference ... ReferenceOutputAssembly="false"`，保证测试前 exe 已构建。
- 全局串行：`[assembly: CollectionBehavior(DisableTestParallelization = true)]`，因为同一时刻只能有一个会话控制桌面。

### 运行门控

Appium 会接管真实鼠标键盘，所以必须显式开启。自定义 `AppiumFactAttribute : FactAttribute`（结构同参考库的 `PlatformFactAttribute`，在构造函数里计算 `Skip`），满足以下全部条件才执行，否则**跳过并给出原因**：

1. 操作系统是 Windows。
2. 环境变量 `HELLO_AVALONIA_APPIUM=1`。
3. 本机 TCP 端口 4723 能连上（不要依赖 `/status` 的返回内容，见「批 0 实测结论」）。
4. 目标应用的 exe 存在。

这样 `dotnet test hello-avalonia.slnx` 在没有 WinAppDriver 时全绿（Appium 部分显示为跳过），不会误控桌面。

### 元素定位

沿用参考库的 `FindElementByAccessibilityId`。两个假设已在批 0 实测，**均成立**：

- **H-A（成立，仅对可交互控件已验证）**：Avalonia 的 UIA 节点在没有显式 `AutomationProperties.AutomationId` 时，会回退使用控件的 `Name`（即 `x:Name`）。批 0 只实测了一个 `Slider`；`Border`、`Grid`、`StackPanel` 这类布局元素是否出现在 UIA 树里**没有测**，所以"549 处 `x:Name` 都可用"不成立，只能说带 `Name` 的**可交互控件**可以直接按 `Name` 定位。
- **H-B（成立）**：`TabItem` 的 UIA `Name` 等于其 `Header` 文本，可以按"中文标签页标题"点击。

只有在某个控件**既没有 `Name` 也没有 `AutomationId`** 时，才给它补 `AutomationProperties.AutomationId`，命名沿用 `TestingDemo` 的 kebab-case（如 `increment-btn`），并且只补对应测试实际用到的控件。点击后读取状态必须带等待（见「批 0 实测结论」）。

## Appium 环境配置（安装 WinAppDriver 之后）

> 来源：`microsoft/WinAppDriver` 的 README 与 Releases（2026-10-12 查询）。标"未实测"的步骤由批 0 探针 P1/P2 验证。

### 先说清楚：本设计里的 "Appium" 指什么

本设计用的是 **Appium 1 协议**：测试里的 `Appium.WebDriver` 客户端**直接连接 WinAppDriver**（默认 `http://127.0.0.1:4723`），**中间没有 Appium Server，也不需要装 Node.js 的 `appium`**。所以"配置 Appium"在这里等于"配置 WinAppDriver + 配置测试项目里的客户端"。

| 组件 | 角色 | 是否需要安装 |
|---|---|---|
| WinAppDriver | 服务端，把 WebDriver 请求翻译成 Windows UI Automation 调用 | 是（本机已装） |
| `Appium.WebDriver` 4.4.5 | 测试代码里的 NuGet 客户端 | 否，随 `dotnet restore` 拉取 |
| Appium Server 2.x + `appium-windows-driver` | 另一种服务端（内部仍调用 WinAppDriver） | **否**，仅作备选，见末尾 |

### 步骤 1：安装 WinAppDriver

1. 从 `https://github.com/microsoft/WinAppDriver/releases` 下载最新版 `WindowsApplicationDriver_1.2.1.msi`（v1.2.1，发布于 2020-11-05，目前仍是最新；仓库未归档，最近一次推送是 2025-04）。
2. 在要跑测试的 Windows 10/11 机器上运行安装包。本机装在 `C:\Program Files\Windows Application Driver\`（README 示例是 `Program Files (x86)`，以实际路径为准）。
3. 确认 `WinAppDriver.exe` 存在：
   ```powershell
   Test-Path "C:\Program Files\Windows Application Driver\WinAppDriver.exe"
   ```

### 步骤 2：开启 Windows 开发者模式

WinAppDriver 要求开启。路径：**设置 → 隐私和安全性 → 开发者选项 → 开发人员模式**（Windows 10 为 设置 → 更新和安全 → 开发者选项）。

校验（`1` 表示已开启，本机当前即为 `1`）：

```powershell
(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock').AllowDevelopmentWithoutDevLicense
```

### 步骤 3：启动 WinAppDriver 服务

测试**之前**，在一个**单独的终端**里手动运行，并保持它开着：

```powershell
& "C:\Program Files\Windows Application Driver\WinAppDriver.exe"
```

- 默认监听 `127.0.0.1:4723`，与本设计的客户端地址一致，**不要改**。
- 要换端口或地址（`WinAppDriver.exe 4727`、`WinAppDriver.exe 10.0.0.10 4725`），README 说明必须**以管理员身份**运行；本设计不需要，所以默认用普通权限启动即可。
- 夹具不会替你启动它。没启动时 Appium 用例会被跳过（见「运行门控」），而不是失败。

校验服务是否起来（批 0 实测：监听正常时 `/status` 也返回 `{"status":13,"value":{"error":"unknown error",…}}`，所以**只看能不能连上，不看返回内容**）：

```powershell
Test-NetConnection 127.0.0.1 -Port 4723 -InformationLevel Quiet
```

返回 `True` 说明服务在监听；`False` 说明没起来。

### 步骤 4：配置测试项目（由批 0、批 7 落地）

1. `Directory.Packages.props` 登记：`<PackageVersion Include="Appium.WebDriver" Version="4.4.5" />`。
2. `Avalonia.Demos.AppiumTests.csproj`：`OutputType=Exe`（xunit v3）、引用 `Appium.WebDriver`、`xunit.v3`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk`，并对 20 个应用做 `ProjectReference ... ReferenceOutputAssembly="false"` 以保证 exe 先被构建。
3. `AppSession` 创建会话时的能力（与参考库 Windows 分支一致）：

   | 能力 | 值 |
   |---|---|
   | `app` | 被测 exe 的**绝对路径**，如 `E:\...\src\Avalonia.TestingDemo\bin\Debug\Avalonia.TestingDemo.exe` |
   | `platformName` | `Windows` |
   | `deviceName` | `WindowsPC` |

   服务地址 `http://127.0.0.1:4723`（Windows 不加 `/wd/hub`；参考库只在 macOS 分支加）。
4. exe 路径规则：`Directory.Build.props` 把输出统一到 `bin\Debug\`（没有 `net10.0` 层），所以是 `src\<项目名>\bin\Debug\<项目名>.exe`。

### 步骤 5：运行

先 `dotnet build hello-avalonia.slnx`，然后在**另一个终端**：

```powershell
$env:HELLO_AVALONIA_APPIUM = "1"
dotnet test tests/Avalonia.Demos.AppiumTests
```

不设环境变量，或 WinAppDriver 没起，整个项目显示为"跳过"，退出码仍为 0。

### 运行期间的注意事项

- WinAppDriver 通过 UI Automation 操作**真实的交互式桌面**：运行期间不要动鼠标键盘，不要锁屏；用远程桌面时不要最小化或断开会话，否则用例会找不到元素。（常见经验，未实测。）
- 同一时刻只允许一个会话，所以 Appium 项目全局串行，不要同时开两个 `dotnet test`。
- 每个用例结束后夹具会关闭会话并结束被测进程；如果测试被强杀，手动结束遗留的 `Avalonia.*.exe` 与 `WinAppDriver.exe`。

### 常见问题

| 现象 | 可能原因 | 处理 |
|---|---|---|
| 全部用例"跳过" | 未设 `HELLO_AVALONIA_APPIUM=1`；WinAppDriver 没起；exe 未构建 | 逐项对照「运行门控」四个条件 |
| 创建会话报 `UnknownError` / 找不到应用 | `app` 路径不是绝对路径，或 Debug 版本未构建 | 先 `dotnet build`，核对 `bin\Debug` 下的 exe |
| 启动 WinAppDriver 报开发者模式相关错误 | 未开启开发人员模式 | 回到步骤 2 |
| 能连上但按名字找不到元素 | 该控件不是可交互控件（例如布局容器可能不在 UIA 树里），或既没有 `Name` 也没有 `AutomationId` | 改为定位它内部的可交互控件或文本；仍不行才给该控件补 `AutomationProperties.AutomationId` |
| 端口 4723 被占用 | 上一次的 `WinAppDriver.exe` 还在 | `Get-Process WinAppDriver \| Stop-Process` |

### 备选方案：Appium 2 + appium-windows-driver（未实测，仅在探针失败时启用）

只有批 0 的 P1/P2 证明 WinAppDriver 直连行不通时才考虑。思路是装 Node.js 版 Appium 2 与 `appium-windows-driver`（它内部仍调用 WinAppDriver，所以本节前三步仍然需要），客户端从 `Appium.WebDriver` 4.4.5 升到 5.x 并启用参考库的 `APPIUM2` 分支。具体的安装命令、驱动版本、服务地址和能力名**以届时官方文档为准**，这里不预先固定，避免写下未经验证的步骤。如果连这条路也不通，再评估 FlaUI（直接走 UIA，无需服务端）。

## 交付拆分

按「spec → 分批 plan → 执行」推进。计划文件命名 `docs/superpowers/plans/2026-10-12-demo-tests-NN-<名称>.md`，每批执行后把实测结论回填到本文末尾。分组沿用已有演示计划的分层，便于同一批里复用对页面的了解。

| 批 | 计划 | 内容 | 应用数 |
|---|---|---|---|
| 0 | 00-foundation-and-probes | `Avalonia.Tests.Common`；迁移 `TestingDemo.Tests` 到 Common 并保持 24 通过；登记 Appium 包；跑 6 个探针并回填 | 0（基建） |
| 1 | 01-headless-foundation | Fundamentals、Xaml、Layout、StarterTutorial | 4 |
| 2 | 02-headless-styling-binding | Styling、DataBinding、DataTemplates、PropertySystem | 4 |
| 3 | 03-headless-interaction-graphics | Events、Input、Graphics、CustomControls | 4 |
| 4 | 04-headless-app-services | AppDevelopment、Services、Behaviors | 3 |
| 5 | 05-headless-controls | ControlsDemo（121 页，按目录分类，必要时拆 05a/05b） | 1 |
| 6 | 06-headless-exceptions | MusicStore、WebViewDemo、HtmlRendererDemo（TestingDemo 的 H1 已在批 0 随 `TabWalkerTests` 完成） | 3 |
| 7 | 07-appium-foundation | Appium 项目骨架、`AppSession`、`AppiumFactAttribute`；`TestingDemo` 的 A1–A3 作为基准 | 1 |
| 8 | 08-appium-tabbed | 其余 15 个 `TabControl` 应用的 A1 + A2（数据驱动，一份代码） | 15 |
| 9 | 09-appium-special | ControlsDemo、HtmlRendererDemo、MusicStore、WebViewDemo 的 A1，以及 WebViewDemo 的 A3 | 4 |
| 10 | 10-appium-flows | 其余应用的 A3 流程（Input、Events、Services、AppDevelopment 等，按实测挑选） | 若干 |

批 0 必须先完成：它的探针结论决定批 7–10 能否按本设计走。批 1–6（Headless）与批 7–10（Appium）互相独立，批 0 之后可交替推进。

## 验证

每批完成的判据：

1. `dotnet build hello-avalonia.slnx` 0 错误，警告数不高于该批开始前。
2. 该批新增的每个测试项目 `dotnet test tests/<项目>` 失败 0；Appium 批在 `HELLO_AVALONIA_APPIUM=1` 且 WinAppDriver 已运行时失败 0，否则只允许"跳过"。
3. `TestingDemo.Tests` 的 24 个测试始终保持通过。
4. `git status` 确认没有探针文件（`C:\Temp\probe-*`）被提交，且 `src/` 下的改动只限于 `AutomationProperties.AutomationId`。
5. 把该批的实测结论（含意外发现）回填到本文对应的「批 N 实测结论」。

## 风险

| 风险 | 影响 | 应对 |
|---|---|---|
| WinAppDriver 最新发布仍是 2020-11 的 v1.2.1（仓库未归档，2025-04 仍有推送），可能驱动不了 Avalonia 12 / net10 应用 | 整个 Appium 层 | 批 0 探针 P2 先验证；失败则改用 Appium 2 + appium-windows-driver（本机已有 Node），或 FlaUI（直接走 UIA，无需服务端），并回头改本设计 |
| `Appium.WebDriver` 4.4.5 依赖 Selenium 3，可能与 net10 不兼容 | Appium 客户端 | 批 0 探针 P1 |
| 被测控件既没有 `Name` 也没有 `AutomationId`，或是不在 UIA 树里的布局容器 | 个别测试需要给 `src/` 补 `AutomationId` | 只补用到的控件；每批 plan 列出具体控件清单；每批执行后确认对应页面的样式与绑定未受影响 |
| 页面依赖真实时间/动画，Headless 断言不稳 | 偶发失败 | 只断言结构与状态，不断言动画中间态；动画页只做 H1 |
| 部分页面在 Headless 下构造即抛异常（`WebView` 页、窗口管理页、`StorageProvider`） | H1 误报 | 在各批 plan 里读代码后建立带理由的"跳过清单"，不无声吞异常 |
| `MusicStore` 的搜索走 iTunes 网络，没有可替换的接缝 | H3/A3 不稳 | 不改 `src/`；Headless 只测窗口与 ViewModel 的离线部分，Appium 只做 A1 |
| 20 个测试项目拖慢整体构建与测试 | 开发体验 | 批 0 记录基线（现状：全解决方案构建约 70 秒，`TestingDemo.Tests` 运行约 4 秒），每批报告增量，批 6 后复评是否需要把测试分组 |
| Appium 运行时接管桌面 | 干扰使用者 | 环境变量开关 + 全局串行 + 文档说明运行期间不要操作机器 |
| 绑定到结构体属性失败不抛异常 | 冒烟测试发现不了 | 批 0 探针 P6 评估能否通过 Avalonia 日志收集绑定错误；能则加入 `TabWalker`，不能则作罢，不阻塞 |

## 批 0 探针清单

在仓库外的 `C:\Temp\probe-appium` 里做，结论回填到「批 0 实测结论」。每项只改一个变量，并先写好成败判据。

| 编号 | 问题 | 成功判据 |
|---|---|---|
| P1 | `Appium.WebDriver` 4.4.5 能否在 net10 的 xunit v3 项目里创建 `WindowsDriver` | 项目编译通过，能连上 WinAppDriver 并返回会话 |
| P2 | WinAppDriver 能否启动 `TestingDemo.exe` 并按 `AutomationId` 找到 `increment-btn`，点击后 `count-text` 变为 `1` | 断言通过 |
| P3 | H-A：对没有 `AutomationId` 的 `LayoutDemo`，能否用 `x:Name` 作为 `AccessibilityId` 找到控件 | 找到元素 |
| P4 | H-B：能否用 `TabItem` 的 `Header` 文本找到并点击标签页 | 点击后 `IsSelected` 为真 |
| P5 | `TabWalker` 能否在 `LayoutDemo` 的 Headless 窗口里遍历全部标签页而不抛异常 | 3 个标签页全部选中并布局 |
| P6 | 能否通过 `Logger.Sink` 收集到一条故意写错的绑定的警告 | 收集到该警告；收集不到则本能力作罢 |

## 已确认的决定（2026-10-12，用户确认）

1. **Appium 技术栈**：WinAppDriver + `Appium.WebDriver` 4.4.5（与参考库一致、本机已装）。WinAppDriver 最新版仍是 2020-11 的 v1.2.1，批 0 探针失败时再换，见「备选方案」。
2. **Headless 项目组织**：每个应用一个测试项目（共 20 个，含已有的 `TestingDemo.Tests`）。
3. **Appium 范围**：只做 Windows，集中在一个项目里，用环境变量显式开启。

## 批 0 实测结论（计划 00 执行后）

| 探针 | 结论 | 说明 |
|---|---|---|
| P1 编译 | 通过 | `Appium.WebDriver` 4.4.5 在 net10 的 xunit v3 项目里还原、编译均无警告 |
| P1 运行 | 通过 | 客户端能连上 WinAppDriver 并创建会话、启动被测 exe。注意 `GET /status` 返回 `{"status":13,"value":{"error":"unknown error",…}}`，**不能**用它做健康检查，见下 |
| P2 | 失败（字面判据），已诊断 | P2 的判据是「点击后读回 `1`」，该断言失败过一次：点击后立刻读 `count-text` 得到 `0`。随后的诊断显示点击本身生效——按 `AutomationId` 找到了 `increment-btn`、`count-text`，等 1.5 秒后读到 `1`，减号按钮由禁用变启用。所以**定位、点击、驱动能力都已验证，缺的是等待**；诊断见下 |
| P3（H-A） | 通过 | 只有 `Name`、没有 `AutomationId` 的 `LayoutDemo` `MarginSlider`，能用 `FindElementByAccessibilityId("MarginSlider")` 找到 |
| P4（H-B） | 通过 | `FindElementByName("定位与间距")` 找到 `TabItem`，点击后 `Selected` 为真 |
| P5 | 通过 | `TabWalker` 对 `LayoutDemo`（用其自己的 `App`）依次得到 `布局面板`、`定位与间距`、`响应式布局`，均无异常 |
| P6 | 部分通过 | `LogCollector` 能收集运行时绑定到不存在属性的警告；编译绑定下"绑定到结构体属性"的静默失败**未验证**，所以没有接入 `TabWalker` |

### 对批 7–10 的决定

- **H-A 在可交互控件上成立**：带 `Name` 的 `Slider` 可直接按 `Name` 定位，不需要 `AutomationId`；是否所有控件类型都如此见上面的限定。
- **H-B 成立**：批 8 按 `TabItem` 的 `Header` 文本定位标签页，不必按序号。
- **所有 Appium 断言必须带等待**：点击之后读取状态要轮询到期望值或超时（建议每 200 ms 重试、上限 5 秒），不能点完立刻读。批 7 的 `AppSession` 要提供 `WaitFor(Func<bool>)` 之类的统一等待，各测试不要自己 `Thread.Sleep`。
- **`AppiumFactAttribute` 的"WinAppDriver 可达"判断不能用 `/status` 的返回内容**：它对健康的服务也回 `status=13`。「运行门控」第 3 条已改为 TCP 端口 4723 能连上（`TcpClient.Connect`）。
- P2 的诊断（一个变量一次，各只跑了一次）：点击前 `count-text`=`0`、`decrement-btn.Enabled`=`False`；点击后等 1.5 秒，重新查找 `count-text` 为 `1`，旧句柄再读也是 `1`，`decrement-btn.Enabled`=`True`。「点击后立刻读到 `0`」只来自 P2 那一次运行，**没有重复测过**，所以不知道是否每次都会读到旧值。**原因没有查明**：可能是 WinAppDriver 读到的是 UI Automation 的缓存，也可能是应用异步处理点击；P4 点击后立刻读 `Selected` 就拿到了新值，说明并非所有属性都有延迟。需要等多久同样没有测，只知道 1.5 秒足够，上面 5 秒的上限是保守估计，批 7 应实测后收紧。

## 批 1 实测结论（执行后回填）

## 批 2 实测结论（执行后回填）

## 批 3 实测结论（执行后回填）

## 批 4 实测结论（执行后回填）

## 批 5 实测结论（执行后回填）

## 批 6 实测结论（执行后回填）

## 批 7 实测结论（执行后回填）

## 批 8 实测结论（执行后回填）

## 批 9 实测结论（执行后回填）

## 批 10 实测结论（执行后回填）
