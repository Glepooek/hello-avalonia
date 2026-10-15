# 演示测试 04：AppDevelopment、Services、Behaviors 的 Headless 测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `AppDevelopmentDemo`、`ServicesDemo`、`BehaviorsDemo` 各建一个 Headless 测试项目，覆盖 spec 的 H1 冒烟（含 `LogCollector`）、H2 逻辑、H3 交互三层；修好共享库 `UiHelpers.Click` 的一个时序缺陷并新增 `UiHelpers.WaitUntil`；修复探针发现的 `ActivatableLifetimePage` 重复订阅缺陷。

**Architecture:** 沿用批 1–3：每个应用一个 `Avalonia.<X>Demo.Tests`（xUnit v3 + `Avalonia.Headless.XUnit`），引用演示项目与 `tests/Avalonia.Tests.Common`，`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。本批的新难点是页面会**改进程级状态**（`Logger.Sink`、dispatcher 的静态事件）或**等真实时钟**（`Task.Run`、`DispatcherTimer`、HTTP 回环、文件监视器）：前者用 `try/finally` 关窗还原，后者统一用 `UiHelpers.WaitUntil` 轮询，超时抛异常而不是返回 `false`。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、Avalonia.Skia、xUnit v3（3.2.2）、CommunityToolkit.Mvvm、Xaml.Behaviors.Avalonia 12.0.7、Microsoft.Extensions.DependencyInjection / Logging

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 4）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文。
- 版本只写在根目录 `Directory.Packages.props`；csproj 的 `PackageReference` **不带 `Version`**。本批用到的包都已登记，不改 `Directory.Packages.props`。
- 代码风格沿用 `tests/Avalonia.TestingDemo.Tests`：块作用域命名空间、测试方法名用英文下划线句式。
- 每个测试项目的 `TestAppBuilder` 只写 `HeadlessApp.Build<App>()`，不复制 `Avalonia.Tests.Common` 里的辅助代码。
- 目录约定：测试项目放 `tests/`，并登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`。
- `src/` 只允许一处改动：Task 3 修 `src/Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs` 的重复订阅（用户 2026-10-15 确认"计划里顺手修"）。其余"破坏检查"临时改 `src/` 的行必须 `git checkout --` 还原；每个任务提交前 `git status --short src` 只允许出现这一个文件（且只在 Task 3 提交之前）。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`。
- 探针放在仓库外（`C:\Temp\probe-*`），**永不提交**。
- 此前各批的 195 个测试始终保持通过（Task 1 之后为 198）。
- 进程级状态必须还原：显示过 `LoggingPage` 或 `UnhandledExceptionsPage` 的窗口，测试结束前在 `finally` 里 `Close()`（两页都在 `OnUnloaded` 里还原 `Logger.Sink` / 退订 `Dispatcher.UIThread.UnhandledException`）；打开的子窗口同样关掉。
- 异步结果一律用 `await UiHelpers.WaitUntil(...)` 等，**不要** `Thread.Sleep`、不要固定 `Task.Delay` 之后再断言（唯一例外见 Task 4 `TimerTrigger` 的"停住"断言，已写明理由）。
- 本批**不对动画中间态做断言**：BehaviorsDemo 的 `AnimationPage` 只靠 H1。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. **页面加载后自己滚动，第一下点击静默落空**：BehaviorsDemo 的 `CollectionPage` 里 `MultiList`（`SelectionMode="Multiple"` + `ListBoxSelectAllBehavior`）在 `Show` 之后的第一次 `RunJobs` 里把首项滚进视口，页面被带下 305px。旧 `Click` 的顺序是"BringIntoView → RunJobs → 算坐标"，排队的滚动恰好在中间执行，坐标落到窗口外，**连窗口级隧道处理器都收不到按下**。Task 1 先 `RunJobs` 再 `BringIntoView`，回归测试先红后绿；Task 4 的"添加『葡萄』"是它在真实页面上的用例。
2. **`LoggingPage` 接管 `Logger.Sink`，冒烟测试在这一页是瞎的**：停在「日志」页时 `LogCollector` 收到 0 条（实测），"0 条警告"在这一页是假通过；窗口不关还会把 `PageSink` 泄漏给同进程后面的测试。Task 2 的两条测试把盲区和还原都钉住。
3. **异步结果读得太早**：线程页、剪贴板、HTTP、文件监视器、防抖、`TaskCompletedTrigger` 都不在点击时完成。立即断言要么假失败，要么在"断言没变化"时假通过。`WaitUntil` 超时**抛异常**（Task 1 有测试），"立即为 0"的断言后面都跟着一个 `WaitUntil` 证明它后来确实变了（Task 4 防抖、`AsyncActionGroup`）。
4. **测试碰到真实外部资源**：`Launcher` 在 Headless 下是 `NoopLauncher`，若将来换成真实实现，点「LaunchUriAsync」会真的打开浏览器。Task 3 在点击前先断言 `Launcher` 的类型名，类型变了就在点击**之前**失败。剪贴板实测是内存桩 `HeadlessClipboardImplStub`，不碰系统剪贴板。
5. **重访标签页让处理器越挂越多**：`ActivatableLifetimePage` 每次 `Loaded` 都订阅 `window.Activated`，从不退订；实测第 1/2/3 次访问后，一次激活让计数分别 +1/+2/+3。Task 3 先写会红的测试，再改 `src/`。

另有两条与区域设置、输入模拟相关的坑，在各任务里处理：`Greeter` 用 `HH:mm:ss`，其中 `:` 是区域设置的时间分隔符，测试包在 `CultureScope.Invariant()` 里；拖拽行为只认带 `RawInputModifiers.LeftMouseButton` 的 `MouseMove`，不带就纹丝不动、也不报错。

---

## 本份已核实的事实（编写计划时在仓库外的探针里实测）

探针在 `C:\Temp\probe-b4-services`、`probe-b4-appdev`、`probe-b4-behaviors`、`probe-b4-common`。**与批 3 不同**：本份计划只有 Task 1 的代码是实跑过的成品（已在工作区，见 Task 1）；Task 2–4 的测试代码按下表的实测事实编写，**没有整份跑过**，执行时以各任务"运行"步骤的真实输出为准，与预期不符时先查测试、再查 `src/`，把差异写进 Task 5 的回填。

| 事项 | 实测结果 | 对计划的影响 |
|---|---|---|
| 三个应用的 `App`、`MainWindow` 都是 `public`，`MainWindow` 有无参构造 | 探针可直接 `new MainWindow()`、`HeadlessApp.Build<App>()` | 不需要 `InternalsVisibleTo` |
| 标签页标题 | AppDevelopment：`依赖注入`、`本地化`、`Web 内容`、`日志`、`未处理异常`、`资源`、`数据校验`、`线程模型`、`窗口管理`、`无障碍`；Services：`剪贴板`、`文件对话框`、`StorageProvider`、`焦点管理`、`Launcher`、`平台设置`、`移动端服务`、`可激活生命周期`；Behaviors：`机制`、`事件转命令`、`数据驱动`、`焦点键盘指针`、`表单验证`、`集合`、`窗口弹出通知`、`拖拽`、`响应式`、`动画`、`文件系统`、`网络`、`剪贴板`、`截图`、`自定义` | 冒烟测试的页头断言 |
| `TabWalker` + `LogCollector` 走完三个应用 | 各 0 个页面异常、0 条日志 | 三个项目都用 `Assert.Empty` |
| 旧 `UiHelpers.Click` 在 `CollectionPage` | 第一下点击 `AddFruitButton`：`Click` 事件 0 次；按钮在 `Show` 后 y=119，第一次 `RunJobs` 后页面偏移 305、按钮 y=-186；发起滚动的是 `MultiList` 的首项「红」 | Review Focus 第 1 条；Task 1 |
| `GetVisualsAt(中心点)` | 对能正常点击的按钮也返回空 | **不能**用它判断"点没点到"；用 `Click` 事件计数或业务结果 |
| `LoggingPage` | 选中「日志」后 `Logger.Sink` 是 `PageSink`，此时的绑定警告 `LogCollector` 收到 0 条；切到别的页后 `Logger.Sink` 回到 `LogCollector`，再触发收到 1 条；关窗后还原成打开前的值；应用日志三条为 `Debug:d 1`、`Warning:w`、`Error:e`；框架日志一条，以 `Warning/Binding:` 开头、含 `Missing` | Review Focus 第 2 条；Task 2 |
| `UnhandledExceptionsPage` | 点击后 `RunJobs` **不抛**；`CountLine` = `处理器被调用：1 次`，`MessageLine` = `最近一次异常：InvalidOperationException：thrown from a posted callback` | Task 2 |
| `ThreadingPage` | 直接写：`CheckAccess = False，InvalidOperationException：…`，`Target` 不变；Post：`written via Post`；await：`written after await`，结果含 `CheckAccess = True` | Task 2，需 `WaitUntil` |
| `DependencyInjectionPage` | Transient 行以 `不同实例` 结尾，Scoped、Singleton 以 `同一个实例` 结尾；Id 来自静态计数器，**数值随测试顺序变**；根容器取 Scoped：`InvalidOperationException：Cannot resolve scoped service 'Avalonia.AppDevelopmentDemo.Services.Counter' from root provider.` | Task 2 只断言关系，不断言 Id 数值 |
| `LocalizationPage` | zh-CN：`你好，世界`、`再见`、`culture = zh-CN`；en-US：`Hello, world`、`Goodbye`、`culture = en-US` | Task 2 |
| `ResourcesPage` | 初始两行都是 `Red`；替换后 `Red`/`Blue`；还原后改同一画刷 `Green`/`Green`；**先替换再改**时只有动态的变 | Task 2 |
| `AccessibilityPage` | `ButtonAutomationPeer`、`Confirm`、`ok-btn`、`Confirms the form`、`Button`；`ViewLine` = `Decoration 的 AccessibilityView = Raw` | Task 2 |
| `WindowManagementPage`（页面放在宿主窗口里） | `Show(owner)`：`OwnedWindows` 1 个，结果 `…child.Owner 是主窗口：True`；`ShowDialog` 在 Headless 下不阻塞，点对话框里的按钮后结果 `ShowDialog 返回：accepted`；拦截窗口第一次 `Close()` 后仍 `IsVisible`，结果以 `Closing 里 e.Cancel = true` 开头；勾上复选框再关则关闭 | Task 2 |
| Services 的平台服务 | `Clipboard` 底层 `HeadlessClipboardImplStub`（内存）；`StorageProvider` 是 `NoopStorageProvider`（三个 `Can*` 都是 `False`）；`Launcher` 是 `NoopLauncher`（返回 `False`）；`InputPane`、`InsetsManager` 为 null | Task 3 |
| `NoopStorageProvider.TryGetFolderFromPathAsync` | **读真实文件系统**：Temp 目录列出了 50 项 | Task 3 自建临时目录再断言内容（不断言顺序） |
| `PlatformSettingsPage` | `Copy：Ctrl+C、Ctrl+Insert`、`Paste：Ctrl+V、Shift+Insert`、`Undo：Ctrl+Z`；`ThemeVariant = Light` | Task 3 只断言快捷键三行与主题行前缀 |
| `ActivatableLifetimePage` | `IActivatableLifetime：null（当前平台不支持，见下表）`；`ActivationKind 取值：File、OpenUri、Reopen、Background`；重访标签页后一次 `Activate()` 的增量依次为 1、2、3 | Review Focus 第 5 条；Task 3 |
| Behaviors：机制页 | 一次点击：日志三条（`① …`、`③ …`、`-- 第 1 次触发结束 --`，`ChangePropertyAction` 不写日志），`Readout` 被改写；`BehaviorEnabled=false` 后再点无变化；`CodeReadout` = `…读到 1 个：EventTriggerBehavior` | Task 4 |
| 事件转命令页 | +1/+10/+100 → 111；按下 `PressBorder` → `PointerPressedEventArgs`；节流连点三下 → 尝试 3、执行 1；防抖连点三下 → 立即 0，约 470 ms 后 1；`DelayAction` 单独放：两条立即出现；放进 `AsyncActionGroup`：先一条，约 800 ms 后第二条 | Task 4 |
| 数据驱动页 | 初始 `在线`/`不及格`/`条件未满足`/`（初始值不触发）`；离线 → `离线`；60 分 → `及格`；再勾条款 → `可以提交`；59 → 两者还原；Flag 真/假 → `Flag 变成了 True/False`；`TimerTrigger` 约 1.3 s 走到 5 并停住；`TaskCompletedTrigger` 约 0.6 s 后 `已完成`，再点一次再完成一次 | Task 4 |
| 焦点键盘指针页 | 打开后焦点在 `AutoBox`；`WantFocus=true` → `FocusTarget`；焦点移到别处后 `WantFocus` 变回 false；Tab 进 `SelectBox` 全选；Enter：默认路由 0、Tunnel 1；`Ctrl+S` 1、`F5` 1；`LoseBox` 按 Enter 后无焦点；`Trap2` 按 Tab 回到 `Trap1`；按下默认路由 2 次、Bubble 1 次、Tapped 1 次 | Task 4 |
| 表单验证页 | 初始用户名错误三条（`用户名不能为空`、`至少 3 个字符`、`只能包含字母、数字、下划线`，换行分隔）、邮箱两条；`ab` → `至少 3 个字符`；`ab!` → `只能包含字母、数字、下划线`；置空 → 前两条；`abc_1` → 有效、提交仍灰；邮箱 `a@b` → `邮箱格式不对`；`a@b.co` → 有效、提交可用；点提交 → `已提交：两个字段都通过了校验` | Task 4 |
| 集合页（新 `Click`） | 添加 → `苹果,香蕉,橙子,葡萄`；清空 → 0；删第二项 → `写计划,提交`；恢复三项；滚到第 150 项 → 内部偏移 5920、第 150 项已实现、第 0 项已回收；`MultiList` 点首项再 `Ctrl+A` → 8 项全选 | Task 4 |
| 窗口弹出通知页 | Flyout、Popup、ToolTip 点击后都打开；四个通知按钮 → 窗口视觉树里 4 个 `NotificationCard`，类型依次 `Success,Information,Warning,Error`；没绑 `NotificationManager` 的按钮 → 仍是 4 个；打开子窗口 → `Owner` 是主窗口，点「关闭我」→ 子窗口关闭、主窗口仍可见 | Task 4 |
| 拖拽页 | `MouseMove` **不带** `LeftMouseButton` 时三种拖拽都不动；带上后：`CanvasChip` 拖 (60,40) → `Canvas.Left/Top` = 70/50；`MouseChip` → `Canvas.Left` 仍 10、`TranslateTransform` = (60,40)；列表把 A 拖过 C → `B → C → A → D` | Task 4 |
| 响应式页 | 宽 360/300 → `wide`、`宽：宽度不小于 300`；200/100 → `narrow`；宽高比：360/200/100 → `flat`，60 → `tall`、`高：宽高比小于 1` | Task 4 |
| 文件系统页 | 沙箱在 `%TEMP%\avalonia-behaviors-demo\files`，ViewModel 构造时清空；未建文件夹就写 → 没有文件；建文件夹 → `notes`；写入 → 列表 `notes`、`notes\hello.txt`，内容 `你好，Behaviors\n`；追加后两行；监视器触发次数**时序不稳**（写入后立刻已是 2，追加后的增量一次没等到） | Task 4 只断言"写入后最终 ≥ 1" |
| 网络页 | 初始状态码行 `状态码 0`；POST → `pong:ping`、`状态码 201`；GET → `pong`、`状态码 200` | Task 4，需 `WaitUntil` |
| 剪贴板页 | 先读 → `""`；复制再读 → 与 `Input` 相同；清空再读 → `""` | Task 4 |
| 截图页 | 加载后 `Renderer.Calls` = 1，`Image.Source` 是 `RenderTargetBitmap`；点「再画一次」后 `Calls` 增加、整帧像素变化；`ScreenshotAction` 不产出文件 | Task 4；`ScreenshotAction` 只靠 H1 |
| 自定义页 | 输入 `abc` → `ABC`；程序赋值 `hello` → `HELLO`；按 1 下 → 0 次；2 下 → 1 次、`星星：★`；5 下 → 2 次、`星星：★★` | Task 4 |
| 只靠 H1 的页面 | AppDevelopment：`WebContentPage`、`DataValidationPage`（静态指引页）；Services：`FocusManagerPage`（静态指引页）；Behaviors：`AnimationPage`（动画）、`DragPage` 的文字/文件投放（真实系统拖放）、`NetworkPage` 的 `NetworkInformationTrigger`（无法制造网络变化）、`ScreenshotPage` 的 `ScreenshotAction`、`AnimationPage` 的倾斜与视差 | 不写 H3 |

## 文件结构

**新增**（`<X>` 为 `AppDevelopment`、`Services`、`Behaviors`）

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.<X>Demo.Tests/Avalonia.<X>Demo.Tests.csproj` | 测试项目工程 |
| `tests/Avalonia.<X>Demo.Tests/TestAppBuilder.cs` | 一行：`HeadlessApp.Build<App>()` |
| `tests/Avalonia.<X>Demo.Tests/SmokeTests.cs` | H1：走完全部标签页、断言页头与无异常、断言日志 |
| `tests/Avalonia.<X>Demo.Tests/InteractionTests.cs` | H3："操作 → 断言界面变化" |
| `tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs` | H2：`Greeter`、`Counter`、`ListLoggerProvider`（普通 `[Fact]`） |
| `tests/Avalonia.BehaviorsDemo.Tests/ViewModelTests.cs` | H2：各 ViewModel（普通 `[Fact]`） |

**修改**

| 文件 | 改动 |
|---|---|
| `tests/Avalonia.Tests.Common/UiHelpers.cs` | `Click` 先 `RunJobs`；新增 `WaitUntil`（Task 1，**已在工作区**） |
| `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs` | 3 条回归测试（Task 1，**已在工作区**） |
| `src/Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs` | `OnUnloaded` 退订窗口激活事件（Task 3） |
| `hello-avalonia.slnx` | `/tests/` 加入三个新项目 |
| `README.md` | 测试项目一行补上三个应用（Task 5） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | `UiHelpers` 一行、src 改动范围、回填「批 4 实测结论」（Task 5） |

**不碰**：`src/` 下其他文件、`Directory.Packages.props`、`CLAUDE.md`。

**执行前提**：`CLAUDE.md` 提到的 `C:\Temp\extract-plan.ps1` 目前不存在。执行者要么按 `#### \`路径\`` 标注手动用 Write 写入每个代码块，要么先重建该脚本；两种方式结果相同。

---

### Task 1: 共享库：`Click` 先清空排队的工作，新增 `WaitUntil`

**Files:**
- Modify: `tests/Avalonia.Tests.Common/UiHelpers.cs`、`tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs`

**这两份改动在编写计划时已经写好、跑过，现在留在工作区未提交**（用户 2026-10-15 决定"保留"）。本任务只需核对内容与下面一致、跑测试、提交；**不要**重写。若 `git diff` 与下面不符，以下面为准改回来。

**Interfaces:**
- Consumes（批 0–3 已交付）：`UiHelpers.Show(Control) : Window`（600×480）、`UiHelpers.Click(this Window, Control)`。
- Produces：
  - `UiHelpers.Click(this Window window, Control control)`：签名不变，行为改为 `RunJobs → BringIntoView → RunJobs → 按下/抬起`。Task 4 的集合页依赖它。
  - `UiHelpers.WaitUntil(Func<bool> condition, int timeoutMs = 5000) : Task`——每 10 ms `RunJobs()` 一次并检查条件；超时抛 `TimeoutException`。Task 2–4 所有异步断言依赖它。测试方法因此写成 `public async Task ...()`，`[AvaloniaFact]` 支持异步测试，`await` 之后仍在 UI 线程上。

- [ ] **Step 1: 核对工作区的改动**

Run: `git diff --stat`
Expected: 只有 `tests/Avalonia.Tests.Common/UiHelpers.cs` 与 `tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs` 两个文件。

`UiHelpers.cs` 里的 `Click` 应为：

```csharp
        // A real left click at the centre of the control: unlike RaiseEvent(Click) this runs the bound Command.
        // Demo pages sit in ScrollViewers, so scroll the target into view first or the click lands on something else.
        // Flush queued work before that: a page that scrolls itself after Show (a ListBox bringing its first item
        // into view) would otherwise scroll the target away again between BringIntoView and the click.
        public static void Click(this Window window, Control control)
        {
            Dispatcher.UIThread.RunJobs();
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
        }
```

`CaptureFrame` 之后应有（文件头新增 `using System;`、`using System.Diagnostics;`、`using System.Threading.Tasks;`）：

```csharp
        // Pumps the dispatcher until the condition holds, for work that finishes on its own clock (Task.Run, timers,
        // HTTP, file watchers). Throws rather than returning false, so a forgotten assert cannot turn a timeout into a pass.
        public static async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                Dispatcher.UIThread.RunJobs();
                if (condition())
                    return;
                if (watch.ElapsedMilliseconds > timeoutMs)
                    throw new TimeoutException($"Condition not met within {timeoutMs} ms.");
                await Task.Delay(10);
            }
        }
```

`UiHelpersTests.cs` 在第一个测试之后应有三个新测试（文件头新增 `using Avalonia.Threading;`、`using System.Threading.Tasks;`）：

```csharp
        // BehaviorsDemo's CollectionPage does this: a ListBox scrolls its first item into view on the first RunJobs
        // after Show, which used to carry the target off screen between BringIntoView and the click.
        [AvaloniaFact]
        public void Click_reaches_a_control_when_the_page_scrolls_itself_after_show()
        {
            var clicks = 0;
            var top = new Button { Content = "top", Height = 40 };
            top.Click += (_, _) => clicks++;
            var bottom = new Border { Height = 40 };
            var panel = new StackPanel { Children = { top, new Border { Height = 2000 }, bottom } };
            var window = UiHelpers.Show(new ScrollViewer { Content = panel });
            Dispatcher.UIThread.Post(() => bottom.BringIntoView());

            window.Click(top);

            Assert.Equal(1, clicks);
        }

        [AvaloniaFact]
        public async Task WaitUntil_returns_once_background_work_lands_on_the_ui_thread()
        {
            var done = false;
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                Dispatcher.UIThread.Post(() => done = true);
            });

            Assert.False(done);
            await UiHelpers.WaitUntil(() => done);
            Assert.True(done);
        }

        [AvaloniaFact]
        public async Task WaitUntil_throws_when_the_condition_never_holds()
        {
            await Assert.ThrowsAsync<TimeoutException>(() => UiHelpers.WaitUntil(() => false, 100));
        }
```

- [ ] **Step 2: 证明回归测试能抓到旧行为（临时改，必须还原）**

用 Edit 删掉 `Click` 里**第一行** `Dispatcher.UIThread.RunJobs();`（`BringIntoView` 之前那一行）。

Run: `dotnet test tests/Avalonia.TestingDemo.Tests --filter "FullyQualifiedName~scrolls_itself_after_show"`
Expected: **失败**，`Assert.Equal() Failure: Expected 1, Actual 0`（编写计划时实测如此）。

把那一行加回去，然后：

Run: `git diff tests/Avalonia.Tests.Common/UiHelpers.cs`
Expected: 与 Step 1 的 `Click` 完全一致。

- [ ] **Step 3: 记录基线并确认通过**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)"`
Expected: 0 错误。记下警告数；Task 5 要求结束时不高于这个数。

Run: `dotnet test tests/Avalonia.TestingDemo.Tests`
Expected: `通过: 37，失败: 0`（34 + 新增 3）。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 198 个通过（195 + 3；编写计划时实测如此）。

- [ ] **Step 4: Commit**

```bash
git add tests/Avalonia.Tests.Common/UiHelpers.cs tests/Avalonia.TestingDemo.Tests/UiHelpersTests.cs
git status --short
git commit -m "test: flush queued work before clicking and add WaitUntil to the shared helpers"
```

`git status --short` 里已暂存的只应是上面两个文件（本计划文件按批 3 的做法，在执行开始前单独提交为 `docs: add batch 4 plan ...`）。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 2: `Avalonia.AppDevelopmentDemo.Tests`

**Files:**
- Create: `tests/Avalonia.AppDevelopmentDemo.Tests/Avalonia.AppDevelopmentDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`ServiceTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click/WaitUntil`、`TabWalker.Walk`、`LogCollector`、`CultureScope.Invariant()`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.AppDevelopmentDemo.App`、`Views.MainWindow`、`Views.Pages.*`（全是 code-behind，没有 ViewModel）、`Services.{Greeter(IClock), IClock, Counter}`、`Logging.ListLoggerProvider`（`Entries : ObservableCollection<string>`，条目形如 `Warning:w`）。
- Produces：测试项目 `Avalonia.AppDevelopmentDemo.Tests`（20 个测试）。

本项目的页面全部写在 code-behind 里，元素用 `Name` 取，**同名元素在不同页面里重复出现**（`Result` 至少出现在线程页和窗口管理页），所以 H3 测试每次只把**一个**页面放进窗口（`UiHelpers.Show(new XPage())`），不在 `MainWindow` 上按名字找。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.AppDevelopmentDemo.Tests/Avalonia.AppDevelopmentDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.AppDevelopmentDemo\Avalonia.AppDevelopmentDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

`Microsoft.Extensions.Logging` 经演示项目的引用传递过来，`ServiceTests` 直接用，不另加 `PackageReference`。

#### `tests/Avalonia.AppDevelopmentDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.AppDevelopmentDemo;
using Avalonia.AppDevelopmentDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 `CustomControlsDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.AppDevelopmentDemo.Tests/Avalonia.AppDevelopmentDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试（含 `LoggingPage` 的盲区与还原）**

#### `tests/Avalonia.AppDevelopmentDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.AppDevelopmentDemo.Views;
using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
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
                new[] { "依赖注入", "本地化", "Web 内容", "日志", "未处理异常", "资源", "数据校验", "线程模型", "窗口管理", "无障碍" },
                visits.Select(v => v.Header));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        // Blind spot: while the 日志 tab is selected, LoggingPage owns Logger.Sink and this collector hears nothing
        // (see the next test), so a warning raised on that page alone would not fail this test.
        [AvaloniaFact]
        public void Walking_every_tab_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();

            TabWalker.Walk(window);

            Assert.Empty(logs.Messages);
            Assert.Same(logs, Logger.Sink);
        }

        // Logger.Sink is process-wide: if LoggingPage did not hand it back on unload, every later test in the
        // process would report to the page instead of to its own LogCollector.
        [AvaloniaFact]
        public void Logging_tab_owns_the_log_sink_only_while_it_is_selected()
        {
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();
            var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
            try
            {
                Assert.Equal("日志", (tabs.Items[3] as TabItem)?.Header);

                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Assert.NotSame(logs, Logger.Sink);

                tabs.SelectedIndex = 4;
                Dispatcher.UIThread.RunJobs();
                Assert.Same(logs, Logger.Sink);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Closing_the_window_on_the_logging_page_restores_the_previous_sink()
        {
            using var logs = new LogCollector();
            var window = UiHelpers.Show(new LoggingPage());
            Dispatcher.UIThread.RunJobs();
            Assert.NotSame(logs, Logger.Sink);

            window.Close();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(logs, Logger.Sink);
        }
    }
}
```

- [ ] **Step 3: H2 服务测试（普通 `[Fact]`，不需要 UI）**

#### `tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs`

```csharp
using Avalonia.AppDevelopmentDemo.Logging;
using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.Tests.Common;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public class ServiceTests
    {
        private sealed class FixedClock : IClock
        {
            public DateTime Now { get; } = new(2026, 10, 15, 9, 5, 7);
        }

        // HH:mm:ss uses the culture's time separator, so pin the culture before asserting on ':'.
        [Fact]
        public void Greeter_formats_the_name_and_the_time_from_the_injected_clock()
        {
            using var culture = CultureScope.Invariant();

            Assert.Equal("你好，Avalonia。现在是 09:05:07", new Greeter(new FixedClock()).Greet("Avalonia"));
        }

        // The id comes from a static counter shared by every test in the process, so only the order is stable.
        [Fact]
        public void Each_counter_takes_a_later_id_than_the_one_before()
        {
            var first = new Counter();
            var second = new Counter();

            Assert.True(second.Id > first.Id, $"{second.Id} should be greater than {first.Id}");
        }

        [Fact]
        public void List_logger_writes_level_and_message_and_ignores_level_none()
        {
            using var provider = new ListLoggerProvider();
            var logger = provider.CreateLogger("Demo");

            logger.LogWarning("w {Index}", 1);
            logger.LogError(new InvalidOperationException("boom"), "e");

            Assert.Equal(new[] { "Warning:w 1", "Error:e" }, provider.Entries);
            Assert.False(logger.IsEnabled(LogLevel.None));
        }
    }
}
```

`LogError(exception, "e")` 的条目只有 `Error:e`：`ListLogger` 只用 `formatter(state, exception)`，默认格式化器不把异常拼进消息（`LoggingPage` 的实测条目同样是 `Error:e`）。

- [ ] **Step 4: H3 页面交互测试**

#### `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.AppDevelopmentDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Window window, string name) => window.ByName<TextBlock>(name).Text ?? "";

        private static void Press(Window window, string buttonName)
        {
            window.Click(window.ByName<Button>(buttonName));
            Dispatcher.UIThread.RunJobs();
        }

        // Ids come from a static counter, so compare lines across clicks instead of asserting numbers.
        [AvaloniaFact]
        public void Resolving_twice_shows_new_scoped_instances_and_the_same_singleton()
        {
            var window = UiHelpers.Show(new DependencyInjectionPage());

            Press(window, "ResolveButton");
            var transient = Text(window, "TransientLine");
            var scoped = Text(window, "ScopedLine");
            var singleton = Text(window, "SingletonLine");
            Press(window, "ResolveButton");

            Assert.StartsWith("Transient：", transient);
            Assert.EndsWith("不同实例", transient);
            Assert.EndsWith("同一个实例", scoped);
            Assert.EndsWith("同一个实例", singleton);
            // A new scope per click: the scoped id moves on, the singleton id does not.
            Assert.NotEqual(scoped, Text(window, "ScopedLine"));
            Assert.Equal(singleton, Text(window, "SingletonLine"));
        }

        [AvaloniaFact]
        public void Greeter_comes_from_the_container_and_a_scoped_service_cannot_come_from_the_root()
        {
            using var culture = CultureScope.Invariant();
            var window = UiHelpers.Show(new DependencyInjectionPage());

            Press(window, "GreetButton");
            Press(window, "RootScopedButton");

            Assert.Matches(new Regex(@"^你好，Avalonia。现在是 \d{2}:\d{2}:\d{2}$"), Text(window, "GreetLine"));
            Assert.Equal(
                "InvalidOperationException：Cannot resolve scoped service 'Avalonia.AppDevelopmentDemo.Services.Counter' from root provider.",
                Text(window, "ScopeErrorLine"));
        }

        [AvaloniaFact]
        public void Switching_language_reloads_the_strings_from_the_resource_file()
        {
            var window = UiHelpers.Show(new LocalizationPage());
            Assert.Equal(new[] { "你好，世界", "再见", "culture = zh-CN" },
                new[] { Text(window, "GreetingText"), Text(window, "FarewellText"), Text(window, "CultureText") });

            window.Click(window.ByName<RadioButton>("EnglishRadio"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "Hello, world", "Goodbye", "culture = en-US" },
                new[] { Text(window, "GreetingText"), Text(window, "FarewellText"), Text(window, "CultureText") });

            window.Click(window.ByName<RadioButton>("ChineseRadio"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("你好，世界", Text(window, "GreetingText"));
        }

        // The page's point: a one-shot lookup keeps the brush it got, DynamicResource follows the key, and both
        // see a change made to the brush object they share.
        [AvaloniaFact]
        public void Replacing_a_resource_moves_only_the_dynamic_swatch_but_mutating_it_moves_both()
        {
            var window = UiHelpers.Show(new ResourcesPage());
            string[] Lines() => new[] { Text(window, "StaticLine"), Text(window, "DynamicLine") };
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Red" }, Lines());

            Press(window, "ReplaceButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Blue" }, Lines());

            // The key now points at the blue brush, which the static swatch never saw.
            Press(window, "MutateButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Green" }, Lines());

            Press(window, "ResetButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Red", "DynamicResource 的：Red" }, Lines());

            Press(window, "MutateButton");
            Assert.Equal(new[] { "TryFindResource 取到的：Green", "DynamicResource 的：Green" }, Lines());
        }

        [AvaloniaFact]
        public void Reading_the_peer_shows_the_automation_properties_set_in_xaml()
        {
            var window = UiHelpers.Show(new AccessibilityPage());

            Press(window, "ReadButton");

            Assert.Equal("Peer 类型：ButtonAutomationPeer", Text(window, "PeerType"));
            Assert.Equal("Name：Confirm", Text(window, "PeerName"));
            Assert.Equal("AutomationId：ok-btn", Text(window, "PeerId"));
            Assert.Equal("HelpText：Confirms the form", Text(window, "PeerHelp"));
            Assert.Equal("ControlType：Button", Text(window, "PeerControl"));
            Assert.Equal("Decoration 的 AccessibilityView = Raw", Text(window, "ViewLine"));
        }

        // The framework entry goes through Dispatcher.Post, so wait for it instead of reading right after the click.
        [AvaloniaFact]
        public async Task Logging_page_lists_app_log_entries_and_the_framework_binding_warning()
        {
            var window = UiHelpers.Show(new LoggingPage());
            try
            {
                var app = window.ByName<ItemsControl>("AppEntries");
                var framework = window.ByName<ItemsControl>("AvaloniaEntries");

                Press(window, "AppLogButton");
                Press(window, "AvaloniaLogButton");
                await UiHelpers.WaitUntil(() => framework.ItemCount > 0);

                Assert.Equal(new[] { "Debug:d 1", "Warning:w", "Error:e" }, app.Items.Cast<string>());
                var entry = Assert.Single(framework.Items.Cast<string>());
                Assert.StartsWith("Warning/Binding:", entry);
                Assert.Contains("Missing", entry);

                Press(window, "AppClearButton");
                Press(window, "AvaloniaClearButton");
                Assert.Equal(0, app.ItemCount);
                Assert.Equal(0, framework.ItemCount);
            }
            finally
            {
                window.Close();
            }
        }

        // The handler marks the exception handled, so RunJobs must not rethrow it into the test.
        [AvaloniaFact]
        public void A_throwing_posted_callback_reaches_the_page_handler_instead_of_crashing()
        {
            var window = UiHelpers.Show(new UnhandledExceptionsPage());
            try
            {
                Press(window, "ThrowButton");

                Assert.Equal("处理器被调用：1 次", Text(window, "CountLine"));
                Assert.Equal("最近一次异常：InvalidOperationException：thrown from a posted callback", Text(window, "MessageLine"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Writing_from_a_background_thread_throws_and_leaves_the_control_unchanged()
        {
            var window = UiHelpers.Show(new ThreadingPage());

            window.Click(window.ByName<Button>("DirectButton"));
            await UiHelpers.WaitUntil(() => Text(window, "Result").StartsWith("CheckAccess"));

            Assert.StartsWith("CheckAccess = False，InvalidOperationException：", Text(window, "Result"));
            Assert.Equal("（被写的控件）", Text(window, "Target"));
        }

        [AvaloniaFact]
        public async Task Posting_back_to_the_ui_thread_writes_the_control()
        {
            var window = UiHelpers.Show(new ThreadingPage());

            window.Click(window.ByName<Button>("PostButton"));
            await UiHelpers.WaitUntil(() => Text(window, "Target") == "written via Post");

            Assert.Equal("Post 的回调里 CheckAccess = True，写入成功", Text(window, "Result"));
        }

        [AvaloniaFact]
        public async Task Code_after_await_resumes_on_the_ui_thread()
        {
            var window = UiHelpers.Show(new ThreadingPage());

            window.Click(window.ByName<Button>("AwaitButton"));
            await UiHelpers.WaitUntil(() => Text(window, "Target") == "written after await");

            Assert.Equal("await 之后 CheckAccess = True，写入成功", Text(window, "Result"));
        }

        // The page needs a real Window as its TopLevel to act as owner; UiHelpers.Show provides one.
        [AvaloniaFact]
        public void Show_with_owner_makes_the_page_window_the_owner()
        {
            var window = UiHelpers.Show(new WindowManagementPage());
            try
            {
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("WindowState = Normal，CanResize = True，SizeToContent = Manual，Topmost = False", Text(window, "StateLine"));

                Press(window, "ShowButton");

                Assert.Equal("Show(owner) 之后 child.Owner 是主窗口：True", Text(window, "Result"));
                var child = Assert.Single(window.OwnedWindows);
                Assert.Equal("非模态子窗口", child.Title);
            }
            finally
            {
                window.Close();
            }
        }

        // Headless ShowDialog does not block the test: the dialog is just another owned window to click in.
        [AvaloniaFact]
        public async Task Show_dialog_returns_the_value_passed_to_close()
        {
            var window = UiHelpers.Show(new WindowManagementPage());
            try
            {
                Press(window, "DialogButton");
                var dialog = Assert.Single(window.OwnedWindows);
                Assert.Equal("模态对话框", dialog.Title);

                dialog.Click(dialog.GetVisualDescendants().OfType<Button>().Single());
                await UiHelpers.WaitUntil(() => Text(window, "Result") == "ShowDialog 返回：accepted");

                Assert.False(dialog.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Closing_handler_keeps_the_window_open_until_closing_is_allowed()
        {
            var window = UiHelpers.Show(new WindowManagementPage());
            try
            {
                Press(window, "GuardButton");
                var guard = Assert.Single(window.OwnedWindows);
                Assert.Equal("拦截关闭", guard.Title);

                guard.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(guard.IsVisible);
                Assert.StartsWith("Closing 里 e.Cancel = true", Text(window, "Result"));

                guard.Click(guard.GetVisualDescendants().OfType<CheckBox>().Single());
                guard.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.False(guard.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

和探针的差别（执行时留意）：探针把 `WindowManagementPage` 放在 900×640 的宿主窗口里；这里用 `UiHelpers.Show` 的 600×480。页面上三个按钮在一行里，若 `Click` 没有触发（结果行仍是 `（还没有操作）`），先在 `try` 第一行加 `window.Width = 900;` 再试（批 3 `CustomPanelPage` 同样的坑），并把这一点写进 Task 5 的回填。

- [ ] **Step 5: 运行**

Run: `dotnet test tests/Avalonia.AppDevelopmentDemo.Tests`
Expected: `通过: 20，失败: 0`（4 + 3 + 13）。

Run: `dotnet build tests/Avalonia.AppDevelopmentDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

某条测试失败时，先对照「本份已核实的事实」：文字与实测不一致就以界面真实输出为准改测试，并记进 Task 5 回填；若界面行为本身与页面说明栏描述的不符，停下来报告，不要改 `src/`。

- [ ] **Step 6: 破坏检查（临时改 `src/`，必须还原）**

**(a)** 证明 sink 测试真的在看还原。用 Edit 在 `src/Avalonia.AppDevelopmentDemo/Views/Pages/LoggingPage.axaml.cs` 里：

旧：

```csharp
            if (ReferenceEquals(Logger.Sink, _sink))
```

新：

```csharp
            if (false && ReferenceEquals(Logger.Sink, _sink))
```

Run: `dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~SmokeTests"`
Expected: **3 个失败**（`Walking_every_tab_logs_no_warnings` 的 `Assert.Same`、`Logging_tab_owns...`、`Closing_the_window...`）。会多一条"不可访问代码"警告，正常。

立刻还原：

```bash
git checkout -- src/Avalonia.AppDevelopmentDemo/Views/Pages/LoggingPage.axaml.cs
git status --short src
```

**(b)** 证明资源测试分得清"换资源"和"改画刷"。用 Edit 在 `src/Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml.cs` 的 `OnReplace` 里：

旧：

```csharp
            Resources["Brand"] = new SolidColorBrush(Colors.Blue);
```

新：

```csharp
            if (Resources["Brand"] is SolidColorBrush shared)
                shared.Color = Colors.Blue;
```

Run: `dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Replacing_a_resource"`
Expected: **失败**（替换之后静态一行也变成 `Blue`）。

立刻还原：

```bash
git checkout -- src/Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml.cs
git status --short src
```

**(c)** 证明未处理异常测试依赖 `Handled`。用 Edit 在 `src/Avalonia.AppDevelopmentDemo/Views/Pages/UnhandledExceptionsPage.axaml.cs` 里：

旧：

```csharp
            e.Handled = true;
```

新：

```csharp
            e.Handled = false;
```

Run: `dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~A_throwing_posted_callback"`
Expected: **失败**（`RunJobs` 把 `InvalidOperationException: thrown from a posted callback` 抛进测试）。

立刻还原：

```bash
git checkout -- src/Avalonia.AppDevelopmentDemo/Views/Pages/UnhandledExceptionsPage.axaml.cs
git status --short src
```

Expected: 三次 `git status --short src` 都无输出。再跑一次 `dotnet test tests/Avalonia.AppDevelopmentDemo.Tests`，`通过: 20`。若改了之后测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 7: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.AppDevelopmentDemo.Tests
git status --short
git commit -m "test: add Headless tests for AppDevelopmentDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.AppDevelopmentDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 3: `Avalonia.ServicesDemo.Tests`，并修复 `ActivatableLifetimePage` 的重复订阅

**Files:**
- Create: `tests/Avalonia.ServicesDemo.Tests/Avalonia.ServicesDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`、`src/Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/Click/WaitUntil`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码）：`Avalonia.ServicesDemo.App`、`Views.MainWindow`、`Views.Pages.*`（全是 code-behind；平台服务一律经 `TopLevel.GetTopLevel(this)` 取）。
- Produces：测试项目 `Avalonia.ServicesDemo.Tests`（12 个测试）；修好的 `ActivatableLifetimePage`（`OnUnloaded` 退订窗口的 `Activated`/`Deactivated`）。

Headless 平台的服务：`Clipboard` 是内存桩，`StorageProvider` 不能弹选择器但**按路径取文件夹会读真实磁盘**，`Launcher` 是永远返回 `False` 的 `NoopLauncher`，`InputPane`/`InsetsManager` 为 null。测试只碰自己建的临时目录。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.ServicesDemo.Tests/Avalonia.ServicesDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.ServicesDemo\Avalonia.ServicesDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.ServicesDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.ServicesDemo;
using Avalonia.ServicesDemo.Tests;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.ServicesDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 Task 2 刚加的 `AppDevelopmentDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.ServicesDemo.Tests/Avalonia.ServicesDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.ServicesDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.ServicesDemo.Views;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.ServicesDemo.Tests
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
                new[] { "剪贴板", "文件对话框", "StorageProvider", "焦点管理", "Launcher", "平台设置", "移动端服务", "可激活生命周期" },
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

- [ ] **Step 3: H3 页面交互测试（最后一条会先红）**

#### `tests/Avalonia.ServicesDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.ServicesDemo.Views;
using Avalonia.ServicesDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ServicesDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Visual root, string name) => root.ByName<TextBlock>(name).Text ?? "";

        // Every service call on these pages is async, so click and then wait for the result line to change.
        private static async Task ClickAndWait(Window window, string buttonName, Func<string, bool> resultIs)
        {
            window.Click(window.ByName<Button>(buttonName));
            await UiHelpers.WaitUntil(() => resultIs(Text(window, "Result")));
        }

        [AvaloniaFact]
        public async Task Clipboard_text_round_trips_and_clear_empties_it()
        {
            var window = UiHelpers.Show(new ClipboardPage());

            await ClickAndWait(window, "SetTextButton", r => r == "已用 SetTextAsync 写入");
            await ClickAndWait(window, "ReadButton", r => r.StartsWith("TryGetTextAsync"));
            Assert.Equal("TryGetTextAsync = \"来自 Avalonia 的文本\"", Text(window, "Result"));

            await ClickAndWait(window, "ClearButton", r => r == "已清空");
            await ClickAndWait(window, "ReadButton", r => r.StartsWith("TryGetTextAsync"));
            Assert.Equal("TryGetTextAsync 返回 null（剪贴板里没有文本）", Text(window, "Result"));
        }

        // Headless has no picker UI: the page must say so instead of awaiting a dialog that never comes.
        [AvaloniaFact]
        public async Task File_dialog_buttons_report_that_the_platform_cannot_show_pickers()
        {
            var window = UiHelpers.Show(new FileDialogsPage());

            await ClickAndWait(window, "OpenButton", r => r.Contains("CanOpen"));
            Assert.Equal("当前平台的 StorageProvider.CanOpen 为 False，无法弹出打开对话框", Text(window, "Result"));
            await ClickAndWait(window, "SaveButton", r => r.Contains("CanSave"));
            Assert.Equal("当前平台的 StorageProvider.CanSave 为 False，无法弹出保存对话框", Text(window, "Result"));
            await ClickAndWait(window, "FolderButton", r => r.Contains("CanPickFolder"));
            Assert.Equal("当前平台的 StorageProvider.CanPickFolder 为 False，无法弹出文件夹对话框", Text(window, "Result"));
        }

        [AvaloniaFact]
        public void Storage_page_shows_the_capabilities_of_the_platform()
        {
            var window = UiHelpers.Show(new StorageProviderPage());
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("CanOpen = False，CanSave = False，CanPickFolder = False", Text(window, "Capabilities"));
        }

        // TryGetFolderFromPathAsync reads the real disk even without picker support, so give it a folder whose
        // contents the test controls. Enumeration order is the file system's, so compare as sets.
        [AvaloniaFact]
        public async Task Folder_from_path_lists_the_items_of_a_real_folder()
        {
            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "services-demo-tests-" + Guid.NewGuid().ToString("N")));
            try
            {
                File.WriteAllText(Path.Combine(dir.FullName, "a.txt"), "a");
                dir.CreateSubdirectory("sub");
                var window = UiHelpers.Show(new StorageProviderPage());
                window.ByName<TextBox>("PathBox").Text = dir.FullName;

                await ClickAndWait(window, "PathButton", r => r.StartsWith("TryGetFolderFromPathAsync"));

                Assert.Equal($"TryGetFolderFromPathAsync → {dir.Name}，前 2 项", Text(window, "Result"));
                Assert.Equal(
                    new[] { "[夹] sub", "[文件] a.txt" }.OrderBy(s => s),
                    window.ByName<ListBox>("Items").Items.Cast<string>().OrderBy(s => s));
            }
            finally
            {
                dir.Delete(recursive: true);
            }
        }

        [AvaloniaFact]
        public async Task Folder_from_a_missing_path_reports_null_instead_of_throwing()
        {
            var window = UiHelpers.Show(new StorageProviderPage());
            window.ByName<TextBox>("PathBox").Text = Path.Combine(Path.GetTempPath(), "services-demo-missing-" + Guid.NewGuid().ToString("N"));

            await ClickAndWait(window, "PathButton", r => r.StartsWith("TryGetFolderFromPathAsync"));

            Assert.Equal("TryGetFolderFromPathAsync 返回 null（路径不存在，或平台不提供）", Text(window, "Result"));
            Assert.Equal(0, window.ByName<ListBox>("Items").ItemCount);
        }

        // The type check runs before any click: if the headless platform ever ships a real launcher, this test must
        // fail here rather than open a browser on the machine running the tests.
        [AvaloniaFact]
        public async Task Launcher_buttons_report_the_result_of_the_noop_launcher()
        {
            var window = UiHelpers.Show(new LauncherPage());
            Assert.Equal("NoopLauncher", TopLevel.GetTopLevel(window)!.Launcher!.GetType().Name);

            await ClickAndWait(window, "UriButton", r => r.StartsWith("LaunchUriAsync"));
            Assert.Equal("LaunchUriAsync 返回 False", Text(window, "Result"));

            window.ByName<TextBox>("UriBox").Text = "not a uri";
            await ClickAndWait(window, "UriButton", r => r == "不是合法的绝对 URI");

            await ClickAndWait(window, "DirButton", r => r.StartsWith("LaunchDirectoryInfoAsync"));
            Assert.Equal("LaunchDirectoryInfoAsync 返回 False", Text(window, "Result"));
        }

        [AvaloniaFact]
        public void Mobile_services_are_reported_as_missing_on_the_desktop()
        {
            var window = UiHelpers.Show(new MobileServicesPage());
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("InputPane：null（当前平台没有软键盘服务）", Text(window, "InputPaneLine"));
            Assert.Equal("InsetsManager：null（当前平台没有安全区服务）", Text(window, "InsetsLine"));
        }

        [AvaloniaFact]
        public void Platform_settings_page_lists_the_hotkeys_and_the_theme()
        {
            var window = UiHelpers.Show(new PlatformSettingsPage());
            Dispatcher.UIThread.RunJobs();

            Assert.StartsWith("ThemeVariant = ", Text(window, "ThemeLine"));
            Assert.Equal("Copy：Ctrl+C、Ctrl+Insert", Text(window, "CopyLine"));
            Assert.Equal("Paste：Ctrl+V、Shift+Insert", Text(window, "PasteLine"));
            Assert.Equal("Undo：Ctrl+Z", Text(window, "UndoLine"));
        }

        private static int Activations(Visual root)
            => int.Parse(Regex.Match(Text(root, "ActivationCounts"), @"Window\.Activated 触发 (\d+) 次").Groups[1].Value);

        [AvaloniaFact]
        public void Activation_page_counts_window_activation_without_a_platform_lifetime()
        {
            var window = UiHelpers.Show(new ActivatableLifetimePage());
            Dispatcher.UIThread.RunJobs();
            var before = Activations(window);

            window.Activate();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("IActivatableLifetime：null（当前平台不支持，见下表）", Text(window, "AvailabilityLine"));
            Assert.Equal("ActivationKind 取值：File、OpenUri、Reopen、Background", Text(window, "WindowLine"));
            Assert.Equal(before + 1, Activations(window));
        }

        // Loaded runs on every visit to the tab; before the fix each visit added another Activated handler,
        // so one activation counted once per visit so far.
        [AvaloniaFact]
        public void Revisiting_the_activation_tab_still_counts_one_activation_once()
        {
            var window = new MainWindow();
            window.Show();
            var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
            void Select(int index)
            {
                tabs.SelectedIndex = index;
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("可激活生命周期", (tabs.Items[7] as TabItem)?.Header);

            Select(7);
            Select(0);
            Select(7);
            var before = Activations(window);
            window.Activate();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(before + 1, Activations(window));
        }
    }
}
```

两条未在探针里跑过的断言（执行时以真实输出为准）：清空后再读的文字 `TryGetTextAsync 返回 null（剪贴板里没有文本）`，以及不存在路径的 `… 返回 null（…）`。若清空后桩返回的是空串而不是 null，结果会是 `TryGetTextAsync = ""`——把断言改成实际文字，并写进 Task 5 回填。

- [ ] **Step 4: 运行，确认只有重访测试失败**

Run: `dotnet test tests/Avalonia.ServicesDemo.Tests`
Expected: `通过: 11，失败: 1`。失败的是 `Revisiting_the_activation_tab_still_counts_one_activation_once`：`Expected: n+1, Actual: n+2`（编写计划时实测，第二次访问后一次激活加 2）。其余 11 个若有失败，先修测试（同 Task 2 Step 5 的处理原则），再继续。

- [ ] **Step 5: 修复 `ActivatableLifetimePage`**

`lifetime.Activated`/`Deactivated` 的两个 lambda 有同样的形状，但桌面与 Headless 下 `lifetime` 为 null，测不到，也不在本次范围内；记进 Task 5 的回填，留作遗留项。

用 Edit 在 `src/Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs` 里做三处改动：

旧：

```csharp
        private int _deactivated;
```

新：

```csharp
        private int _deactivated;
        private Window? _window;
```

旧：

```csharp
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Activated += (_, _) => { _activated++; ShowCounts(); };
                window.Deactivated += (_, _) => { _deactivated++; ShowCounts(); };
            }
```

新：

```csharp
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                _window = window;
                window.Activated += OnWindowActivated;
                window.Deactivated += OnWindowDeactivated;
            }
```

旧：

```csharp
        private void ShowCounts() =>
```

新：

```csharp
        // Loaded runs again every time the tab is revisited, so drop the handlers here or each visit adds another pair.
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            if (_window is not null)
            {
                _window.Activated -= OnWindowActivated;
                _window.Deactivated -= OnWindowDeactivated;
                _window = null;
            }

            base.OnUnloaded(e);
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            _activated++;
            ShowCounts();
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            _deactivated++;
            ShowCounts();
        }

        private void ShowCounts() =>
```

文件已有 `using System;`（`EventArgs`）与 `using Avalonia.Interactivity;`（`RoutedEventArgs`），不需要加 using。`OnUnloaded` 的写法照搬同项目 `PlatformSettingsPage`（先退订，最后调 `base`）。

- [ ] **Step 6: 确认通过**

Run: `dotnet test tests/Avalonia.ServicesDemo.Tests`
Expected: `通过: 12，失败: 0`。

Run: `dotnet build src/Avalonia.ServicesDemo`
Expected: 0 错误，没有新增警告。

Run: `git diff --stat src`
Expected: 只有 `src/Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs`。

- [ ] **Step 7: 破坏检查（临时改测试，必须还原）**

修复本身已由 Step 4→6 的先红后绿证明。这里再证明 Launcher 的类型守卫有效：用 Edit 把测试里的 `"NoopLauncher"` 临时改成 `"RealLauncher"`。

Run: `dotnet test tests/Avalonia.ServicesDemo.Tests --filter "FullyQualifiedName~Launcher_buttons"`
Expected: **失败**，且失败行是 `Assert.Equal("RealLauncher", ...)`，在任何点击之前。改回 `"NoopLauncher"`，再跑一次整个项目，`通过: 12`。

- [ ] **Step 8: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.ServicesDemo.Tests src/Avalonia.ServicesDemo/Views/Pages/ActivatableLifetimePage.axaml.cs
git status --short
git commit -m "test: add Headless tests for ServicesDemo and stop ActivatableLifetimePage stacking window handlers"
```

`git status --short` 里只应出现 `hello-avalonia.slnx`、`tests/Avalonia.ServicesDemo.Tests/` 下的文件和那一个 `src/` 文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 4: `Avalonia.BehaviorsDemo.Tests`

**Files:**
- Create: `tests/Avalonia.BehaviorsDemo.Tests/Avalonia.BehaviorsDemo.Tests.csproj`、`TestAppBuilder.cs`、`SmokeTests.cs`、`ViewModelTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（Task 1 之后）：`UiHelpers.Show/ByName/ButtonWithText/Click/Type/CaptureFrame/WaitUntil`、`TabWalker.Walk`、`LogCollector`、`HeadlessApp.Build<TApp>()`。
- Consumes（被测代码，只读）：`Avalonia.BehaviorsDemo.App`、`Views.MainWindow`、`Views.ChildWindow`、`Views.Pages.*`（每页在构造函数里 `DataContext = new XViewModel()`，测试用 `(XViewModel)page.DataContext!` 取）、`ViewModels.*`、`Avalonia.Shared.Helpers.EventLog`（`Entries` 条目形如 `01  文字`，序号两位、后跟两个空格）。
- Produces：测试项目 `Avalonia.BehaviorsDemo.Tests`（2 + 7 + 31 = 40 个测试）。

本项目的行为全写在 XAML 里，测试**不碰行为对象本身**，只做真实输入（`Click`、`KeyPress`、带左键的 `MouseMove`）再读 ViewModel 或界面。读 ViewModel 比读 `TextBlock` 更稳：`StringFormat` 的文字改了不该让行为测试失败。

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.BehaviorsDemo.Tests/Avalonia.BehaviorsDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.BehaviorsDemo\Avalonia.BehaviorsDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.BehaviorsDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo;
using Avalonia.BehaviorsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.BehaviorsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

用 Edit 在 `hello-avalonia.slnx` 的 `/tests/` 文件夹里，紧挨着 Task 3 刚加的 `ServicesDemo.Tests` 那一行**之后**加一行：

```xml
    <Project Path="tests/Avalonia.BehaviorsDemo.Tests/Avalonia.BehaviorsDemo.Tests.csproj" />
```

- [ ] **Step 2: H1 冒烟测试**

#### `tests/Avalonia.BehaviorsDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.BehaviorsDemo.Views;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System.Linq;
using Xunit;

namespace Avalonia.BehaviorsDemo.Tests
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
                    "机制", "事件转命令", "数据驱动", "焦点键盘指针", "表单验证", "集合", "窗口弹出通知", "拖拽",
                    "响应式", "动画", "文件系统", "网络", "剪贴板", "截图", "自定义",
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

- [ ] **Step 3: H2 ViewModel 测试（普通 `[Fact]`，不需要 UI）**

#### `tests/Avalonia.BehaviorsDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using System;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.BehaviorsDemo.Tests
{
    public class ViewModelTests
    {
        private sealed class ListObserver : IObserver<object>
        {
            public List<object> Items { get; } = new();
            public void OnNext(object value) => Items.Add(value);
            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }

        // The page binds the two Valid flags OneWayToSource from the validation behaviors; the VM only combines them.
        [Fact]
        public void Submit_is_allowed_only_when_both_fields_are_valid()
        {
            var vm = new ValidationViewModel();
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.UsernameValid = true;
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.EmailValid = true;
            Assert.True(vm.CanSubmit);
            Assert.True(vm.SubmitCommand.CanExecute(null));
            vm.SubmitCommand.Execute(null);
            Assert.Equal("已提交：两个字段都通过了校验", vm.SubmitResult);
        }

        [Fact]
        public void Reset_todos_restores_the_three_items_in_order()
        {
            var vm = new CollectionViewModel();
            vm.Todos.RemoveAt(1);
            vm.Todos.Add("别的");

            vm.ResetTodosCommand.Execute(null);

            Assert.Equal(new[] { "写计划", "跑探针", "提交" }, vm.Todos);
        }

        [Fact]
        public void Scroll_to_publishes_the_numbered_item()
        {
            var vm = new CollectionViewModel();
            var observer = new ListObserver();
            using var subscription = vm.ScrollRequests.Subscribe(observer);

            vm.ScrollToCommand.Execute("150");

            Assert.Equal(new object[] { "第 150 项" }, observer.Items);
        }

        // ScrollToItemBehavior subscribes on attach and disposes on detach; a leaked observer would keep scrolling
        // a list that is no longer on screen.
        [Fact]
        public void Scroll_request_stops_publishing_after_the_subscription_is_disposed()
        {
            var requests = new ScrollRequest();
            var observer = new ListObserver();
            var subscription = requests.Subscribe(observer);
            requests.Publish("a");

            subscription.Dispose();
            requests.Publish("b");

            Assert.Equal(new object[] { "a" }, observer.Items);
        }

        [Fact]
        public void Order_text_follows_moves_in_the_collection()
        {
            var vm = new DragViewModel();
            var changed = new List<string?>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.Order.Move(0, 2);

            Assert.Equal("B → C → A → D", vm.OrderText);
            Assert.Contains(nameof(DragViewModel.OrderText), changed);
        }

        [Fact]
        public void Start_work_hands_the_trigger_a_new_task_each_time()
        {
            var vm = new DataTriggerViewModel();

            vm.StartWorkCommand.Execute(null);
            var first = vm.Work;
            vm.StartWorkCommand.Execute(null);

            Assert.Equal("运行中", vm.WorkStatus);
            Assert.NotNull(first);
            Assert.NotSame(first, vm.Work);

            vm.WorkDoneCommand.Execute(null);
            Assert.Equal("已完成", vm.WorkStatus);
            Assert.Equal(1, vm.DoneCount);
        }

        [Fact]
        public void Finish_counts_the_run_and_logs_after_the_steps()
        {
            var vm = new MechanismViewModel();

            vm.StepCommand.Execute("x");
            vm.FinishCommand.Execute(null);

            Assert.Equal(1, vm.Runs);
            Assert.Equal(new[] { "01  x", "02  -- 第 1 次触发结束 --" }, vm.Log.Entries);
        }
    }
}
```

- [ ] **Step 4: H3 页面交互测试**

#### `tests/Avalonia.BehaviorsDemo.Tests/InteractionTests.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.BehaviorsDemo.Tests
{
    public class InteractionTests
    {
        private static (Window Window, TPage Page, TViewModel Vm) Open<TPage, TViewModel>()
            where TPage : UserControl, new()
        {
            var page = new TPage();
            var window = UiHelpers.Show(page);
            Dispatcher.UIThread.RunJobs();
            return (window, page, (TViewModel)page.DataContext!);
        }

        private static string Text(Window window, string name) => window.ByName<TextBlock>(name).Text ?? "";

        private static void Press(Window window, string name)
        {
            window.Click(window.ByName<Control>(name));
            Dispatcher.UIThread.RunJobs();
        }

        // Focus first: the key goes to the focused element, and the triggers on this page listen on the TextBox.
        private static void SendKey(Window window, Control target, Key key, PhysicalKey physical,
            RawInputModifiers modifiers = RawInputModifiers.None, string? symbol = null)
        {
            target.Focus();
            Dispatcher.UIThread.RunJobs();
            window.KeyPress(key, modifiers, physical, symbol);
            window.KeyRelease(key, modifiers, physical, symbol);
            Dispatcher.UIThread.RunJobs();
        }

        private static Point Center(Window window, Control control)
            => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        // The drag behaviors read the button state from the move itself: a MouseMove without LeftMouseButton is a
        // hover, and nothing moves.
        private static void Drag(Window window, Point from, Point to, RawInputModifiers held = RawInputModifiers.LeftMouseButton)
        {
            window.MouseDown(from, MouseButton.Left);
            for (var i = 1; i <= 10; i++)
            {
                window.MouseMove(new Point(from.X + (to.X - from.X) * i / 10, from.Y + (to.Y - from.Y) * i / 10), held);
                Dispatcher.UIThread.RunJobs();
            }
            window.MouseUp(to, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        private static IInputElement? Focused(Window window) => window.FocusManager!.GetFocusedElement();

        // ---- 机制 ----

        [AvaloniaFact]
        public void One_click_runs_the_trigger_actions_in_declaration_order()
        {
            var (window, _, vm) = Open<MechanismPage, MechanismViewModel>();
            Assert.Equal("Interaction.GetBehaviors(RunButton) 读到 1 个：EventTriggerBehavior", Text(window, "CodeReadout"));

            Press(window, "RunButton");

            Assert.Equal(new[] { "01  ① Trigger 收到 Click", "02  ③ 第二个 InvokeCommandAction 紧跟着执行", "03  -- 第 1 次触发结束 --" },
                vm.Log.Entries);
            Assert.Equal("② ChangePropertyAction 改写了这行文字", Text(window, "Readout"));
        }

        [AvaloniaFact]
        public void A_disabled_trigger_runs_none_of_its_actions()
        {
            var (window, _, vm) = Open<MechanismPage, MechanismViewModel>();
            vm.BehaviorEnabled = false;
            Dispatcher.UIThread.RunJobs();

            Press(window, "RunButton");

            Assert.Empty(vm.Log.Entries);
            Assert.Equal(0, vm.Runs);
            Assert.Equal("（尚未触发）", Text(window, "Readout"));
        }

        // ---- 事件转命令 ----

        [AvaloniaFact]
        public void Event_triggers_pass_parameters_event_args_and_call_methods()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            Press(window, "AddOneButton");
            Press(window, "AddTenButton");
            Press(window, "AddHundredButton");
            Press(window, "PressBorder");
            Press(window, "HelloButton");
            Press(window, "SourceButton");

            Assert.Equal(111, vm.Total);
            Assert.Equal("PointerPressedEventArgs", vm.LastArgs);
            Assert.Equal(1, vm.HelloCount);
            // The trigger sits on ListenerBlock but listens to SourceButton through SourceObject.
            Assert.Equal(1, vm.SourceHits);
        }

        [AvaloniaFact]
        public void Throttle_runs_the_first_click_and_drops_the_rest_of_the_burst()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            for (var i = 0; i < 3; i++)
                Press(window, "ThrottleButton");

            Assert.Equal(3, vm.ThrottleAttempts);
            Assert.Equal(1, vm.ThrottleRuns);
        }

        // "Nothing yet" alone would also pass if the debounce never ran, so wait for the run that must follow.
        [AvaloniaFact]
        public async Task Debounce_runs_once_after_the_burst_goes_quiet()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            for (var i = 0; i < 3; i++)
                Press(window, "DebounceButton");
            Assert.Equal(3, vm.DebounceAttempts);
            Assert.Equal(0, vm.DebounceRuns);

            await UiHelpers.WaitUntil(() => vm.DebounceRuns == 1);
        }

        // The page's trap: a bare DelayAction does not hold back the actions after it; inside AsyncActionGroup it does.
        [AvaloniaFact]
        public async Task Delay_only_waits_inside_an_async_action_group()
        {
            var (window, _, vm) = Open<EventToCommandPage, EventToCommandViewModel>();

            Press(window, "BareDelayButton");
            Assert.Equal(2, vm.Log.Entries.Count);
            Assert.EndsWith("A 前", vm.Log.Entries[0]);
            Assert.EndsWith("A 后（没有等）", vm.Log.Entries[1]);

            vm.ClearLogCommand.Execute(null);
            Press(window, "GroupDelayButton");
            Assert.EndsWith("B 前", Assert.Single(vm.Log.Entries));

            await UiHelpers.WaitUntil(() => vm.Log.Entries.Count == 2);
            Assert.EndsWith("B 后（等了 0.8 秒）", vm.Log.Entries[1]);
        }

        // ---- 数据驱动 ----

        [AvaloniaFact]
        public void Data_triggers_follow_the_view_model()
        {
            var (window, _, vm) = Open<DataTriggerPage, DataTriggerViewModel>();
            Assert.Equal("在线", Text(window, "StatusText"));
            Assert.Equal(Colors.LimeGreen, ((ISolidColorBrush)window.ByName<TextBlock>("StatusText").Foreground!).Color);
            Assert.Equal("不及格", Text(window, "GradeText"));
            Assert.Equal("条件未满足", Text(window, "PassText"));
            // ConditionalBehavior ignores the initial value, unlike DataTriggerBehavior.
            Assert.Equal("（初始值不触发）", Text(window, "FlagText"));

            vm.IsOnline = false;
            vm.Score = 60;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("离线", Text(window, "StatusText"));
            Assert.Equal("及格", Text(window, "GradeText"));
            Assert.Equal("条件未满足", Text(window, "PassText"));

            vm.Agreed = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("可以提交", Text(window, "PassText"));

            vm.Score = 59;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("不及格", Text(window, "GradeText"));
            Assert.Equal("条件未满足", Text(window, "PassText"));

            vm.Flag = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Flag 变成了 True", Text(window, "FlagText"));
            vm.Flag = false;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Flag 变成了 False", Text(window, "FlagText"));
        }

        // The one fixed wait in this batch: "stops at five" is a claim about the absence of a sixth tick, and no
        // condition can wait for something that must not happen. Two more tick intervals (300 ms each) is the margin.
        [AvaloniaFact]
        public async Task Timer_trigger_stops_after_its_total_ticks()
        {
            var (_, _, vm) = Open<DataTriggerPage, DataTriggerViewModel>();

            await UiHelpers.WaitUntil(() => vm.TimerTicks == 5);
            await Task.Delay(700);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(5, vm.TimerTicks);
        }

        [AvaloniaFact]
        public async Task Task_completed_trigger_fires_again_for_every_new_task()
        {
            var (window, _, vm) = Open<DataTriggerPage, DataTriggerViewModel>();

            Press(window, "StartWorkButton");
            Assert.Equal("运行中", vm.WorkStatus);
            await UiHelpers.WaitUntil(() => vm.DoneCount == 1);
            Assert.Equal("已完成", vm.WorkStatus);

            Press(window, "StartWorkButton");
            await UiHelpers.WaitUntil(() => vm.DoneCount == 2);
        }

        // ---- 焦点键盘指针 ----

        [AvaloniaFact]
        public void Focus_behaviors_move_focus_and_report_it_back_two_way()
        {
            var (window, _, vm) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();
            Assert.Same(window.ByName<TextBox>("AutoBox"), Focused(window));

            vm.WantFocus = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(window.ByName<TextBox>("FocusTarget"), Focused(window));

            var select = window.ByName<TextBox>("SelectBox");
            select.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();
            Assert.False(vm.WantFocus);
            Assert.Equal(0, select.SelectionStart);
            Assert.Equal(select.Text!.Length, select.SelectionEnd);
        }

        // TextBox handles Enter itself, so a Direct-routed KeyTrigger never sees it; Tunnel sees it first.
        [AvaloniaFact]
        public void Key_triggers_need_tunnel_routing_to_see_keys_a_text_box_handles()
        {
            var (window, _, vm) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();

            SendKey(window, window.ByName<TextBox>("NoRouteBox"), Key.Enter, PhysicalKey.Enter);
            SendKey(window, window.ByName<TextBox>("TunnelBox"), Key.Enter, PhysicalKey.Enter);
            SendKey(window, window.ByName<TextBox>("GestureBox"), Key.S, PhysicalKey.S, RawInputModifiers.Control, "s");
            SendKey(window, window.ByName<TextBox>("GestureBox"), Key.F5, PhysicalKey.F5);

            Assert.Equal(0, vm.EnterDirectCount);
            Assert.Equal(1, vm.EnterTunnelCount);
            Assert.Equal(1, vm.SaveCount);
            Assert.Equal(1, vm.F5Count);
        }

        [AvaloniaFact]
        public void Enter_drops_focus_and_tab_cannot_leave_the_focus_trap()
        {
            var (window, _, _) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();

            SendKey(window, window.ByName<TextBox>("LoseBox"), Key.Enter, PhysicalKey.Enter);
            Assert.Null(Focused(window));

            SendKey(window, window.ByName<TextBox>("Trap2"), Key.Tab, PhysicalKey.Tab);
            Assert.Same(window.ByName<TextBox>("Trap1"), Focused(window));
        }

        // The page's caption says it: with the default routing the pressed trigger fires twice for one press.
        [AvaloniaFact]
        public void Default_routed_pointer_trigger_fires_twice_for_one_press()
        {
            var (window, _, vm) = Open<FocusKeyboardPage, FocusKeyboardViewModel>();

            Press(window, "PressDefaultBorder");
            Press(window, "PressBubbleBorder");
            Press(window, "TapBorder");

            Assert.Equal(2, vm.PressDefaultCount);
            Assert.Equal(1, vm.PressBubbleCount);
            Assert.Equal(1, vm.TapCount);
        }

        // ---- 表单验证 ----

        [AvaloniaFact]
        public void Validation_rules_report_every_failure_and_gate_the_submit_button()
        {
            var (window, _, vm) = Open<ValidationPage, ValidationViewModel>();
            var username = window.ByName<TextBox>("UsernameBox");
            var email = window.ByName<TextBox>("EmailBox");
            var submit = window.ByName<Button>("SubmitButton");
            Assert.False(vm.UsernameValid);
            Assert.Equal("用户名不能为空\n至少 3 个字符\n只能包含字母、数字、下划线", vm.UsernameError);
            Assert.Equal("邮箱不能为空\n邮箱格式不对", vm.EmailError);
            // A command button stays IsEnabled while CanExecute is false; IsEffectivelyEnabled is what the user sees.
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(username, "ab");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("至少 3 个字符", vm.UsernameError);
            window.Type(username, "!");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("只能包含字母、数字、下划线", vm.UsernameError);
            username.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("用户名不能为空\n至少 3 个字符", vm.UsernameError);

            window.Type(username, "abc_1");
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.UsernameValid);
            Assert.Null(vm.UsernameError);
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(email, "a@b");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("邮箱格式不对", vm.EmailError);
            window.Type(email, ".co");
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.EmailValid);
            Assert.True(submit.IsEffectivelyEnabled);

            window.Click(submit);
            Assert.Equal("已提交：两个字段都通过了校验", vm.SubmitResult);
        }

        // ---- 集合 ----

        // Review Focus 1: this is the first click on a freshly shown CollectionPage, the case where MultiList
        // scrolls the page after Show. With the old Click it silently missed and the fruit count stayed at 3.
        [AvaloniaFact]
        public void The_first_click_on_the_page_adds_an_item_and_clear_empties_the_list()
        {
            var (window, _, vm) = Open<CollectionPage, CollectionViewModel>();

            Press(window, "AddFruitButton");
            Assert.Equal(new[] { "苹果", "香蕉", "橙子", "葡萄" }, vm.Fruits);

            Press(window, "ClearFruitsButton");
            Assert.Empty(vm.Fruits);
        }

        [AvaloniaFact]
        public void A_delete_button_inside_the_template_removes_its_own_item()
        {
            var (window, _, vm) = Open<CollectionPage, CollectionViewModel>();
            var deletes = window.ByName<ListBox>("TodoList").GetVisualDescendants().OfType<Button>()
                .Where(b => "删除".Equals(b.Content)).ToList();
            Assert.Equal(3, deletes.Count);

            window.Click(deletes[1]);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "写计划", "提交" }, vm.Todos);

            window.Click(window.ButtonWithText("恢复三项"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(3, vm.Todos.Count);
        }

        // Virtualization is the proof: item 150 only gets a container once it has really been scrolled into view.
        [AvaloniaFact]
        public void Scroll_to_item_brings_a_virtualized_item_into_view()
        {
            var (window, _, _) = Open<CollectionPage, CollectionViewModel>();
            var big = window.ByName<ListBox>("BigList");
            Assert.Null(big.ContainerFromIndex(150));

            Press(window, "ScrollButton");
            window.UpdateLayout();

            Assert.True(big.GetVisualDescendants().OfType<ScrollViewer>().First().Offset.Y > 0);
            Assert.NotNull(big.ContainerFromIndex(150));
            Assert.Null(big.ContainerFromIndex(0));
        }

        [AvaloniaFact]
        public void Ctrl_a_selects_every_item_of_the_multi_select_list()
        {
            var (window, _, _) = Open<CollectionPage, CollectionViewModel>();
            var multi = window.ByName<ListBox>("MultiList");

            window.Click(multi.ContainerFromIndex(0)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, multi.SelectedItems!.Count);
            window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
            window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(8, multi.SelectedItems!.Count);
        }

        // ---- 窗口弹出通知 ----

        // The opener buttons sit in rows wider than the 600px test window; widen it so the clicks land (batch 3,
        // CustomPanelPage). The probe ran at 1000.
        [AvaloniaFact]
        public void Show_actions_open_a_flyout_a_popup_and_a_tooltip_on_other_controls()
        {
            var (window, _, _) = Open<WindowPopupPage, object>();
            window.Width = 1000;
            Dispatcher.UIThread.RunJobs();
            var flyout = FlyoutBase.GetAttachedFlyout(window.ByName<Border>("FlyoutAnchor"))!;
            var popup = window.ByName<Popup>("DemoPopup");
            var tipTarget = window.ByName<Border>("TipTarget");
            Assert.False(flyout.IsOpen);
            Assert.False(popup.IsOpen);
            Assert.False(ToolTip.GetIsOpen(tipTarget));

            Press(window, "FlyoutOpener");
            Assert.True(flyout.IsOpen);
            flyout.Hide();
            Press(window, "PopupOpener");
            Assert.True(popup.IsOpen);
            popup.Close();
            Press(window, "TipOpener");
            Assert.True(ToolTip.GetIsOpen(tipTarget));
            ToolTip.SetIsOpen(tipTarget, false);
        }

        [AvaloniaFact]
        public async Task Notification_buttons_show_one_card_each_with_its_type()
        {
            var (window, _, _) = Open<WindowPopupPage, object>();
            window.Width = 1000;
            NotificationCard[] Cards() => window.GetVisualDescendants().OfType<NotificationCard>().ToArray();
            Assert.Empty(Cards());

            foreach (var name in new[] { "SuccessButton", "InfoButton", "WarningButton", "ErrorButton" })
                Press(window, name);
            await UiHelpers.WaitUntil(() => Cards().Length == 4);

            Assert.Equal(
                new[] { NotificationType.Success, NotificationType.Information, NotificationType.Warning, NotificationType.Error },
                Cards().Select(c => c.NotificationType));

            // The unbound action has no NotificationManager to show through, so it must add nothing.
            Press(window, "UnboundButton");
            Assert.Equal(4, Cards().Length);
        }

        [AvaloniaFact]
        public void Close_window_action_closes_only_the_child_window()
        {
            var (window, page, _) = Open<WindowPopupPage, object>();
            try
            {
                Press(window, "OpenChildButton");
                var child = page.Child!;
                Assert.True(child.IsVisible);
                Assert.Same(window, child.Owner);

                child.Click(child.ByName<Button>("CloseChildButton"));
                Dispatcher.UIThread.RunJobs();

                Assert.False(child.IsVisible);
                Assert.Empty(window.OwnedWindows);
                Assert.True(window.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- 拖拽 ----

        [AvaloniaFact]
        public void Drag_behaviors_ignore_moves_without_the_left_button_held()
        {
            var (window, _, vm) = Open<DragPage, DragViewModel>();
            var chip = window.ByName<Border>("CanvasChip");
            var from = Center(window, chip);

            Drag(window, from, new Point(from.X + 60, from.Y + 40), held: RawInputModifiers.None);

            Assert.Equal(10, Canvas.GetLeft(chip));
            Assert.Equal(10, Canvas.GetTop(chip));
            Assert.Equal("A → B → C → D", vm.OrderText);
        }

        // Canvas drag moves Canvas.Left/Top; MouseDragElementBehavior leaves them alone and adds a TranslateTransform.
        [AvaloniaFact]
        public void Dragging_a_chip_moves_it_by_the_pointer_delta()
        {
            var (window, _, _) = Open<DragPage, DragViewModel>();
            var canvasChip = window.ByName<Border>("CanvasChip");
            var from = Center(window, canvasChip);
            Drag(window, from, new Point(from.X + 60, from.Y + 40));
            Assert.Equal(70, Canvas.GetLeft(canvasChip));
            Assert.Equal(50, Canvas.GetTop(canvasChip));

            var mouseChip = window.ByName<Border>("MouseChip");
            mouseChip.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            from = Center(window, mouseChip);
            Drag(window, from, new Point(from.X + 60, from.Y + 40));
            Assert.Equal(10, Canvas.GetLeft(mouseChip));
            var shift = Assert.IsType<TranslateTransform>(mouseChip.RenderTransform);
            Assert.Equal(60, shift.X);
            Assert.Equal(40, shift.Y);
        }

        [AvaloniaFact]
        public void Dragging_a_list_item_past_another_reorders_the_source_collection()
        {
            var (window, _, vm) = Open<DragPage, DragViewModel>();
            var list = window.ByName<ListBox>("OrderList");
            list.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var first = Center(window, list.ContainerFromIndex(0)!);
            var third = Center(window, list.ContainerFromIndex(2)!);

            Drag(window, first, new Point(first.X, third.Y + 4));

            Assert.Equal("B → C → A → D", vm.OrderText);
        }

        // ---- 响应式 ----

        [AvaloniaFact]
        public void Adaptive_and_aspect_ratio_setters_switch_classes_as_the_width_changes()
        {
            var (window, _, vm) = Open<ResponsivePage, ResponsiveViewModel>();
            var adaptive = window.ByName<Border>("AdaptiveBox");
            var ratio = window.ByName<Border>("RatioBox");

            foreach (var (width, size, shape) in new[]
            {
                (360.0, "wide", "flat"), (200.0, "narrow", "flat"), (300.0, "wide", "flat"), (100.0, "narrow", "flat"), (60.0, "narrow", "tall"),
            })
            {
                vm.PanelWidth = width;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.True(adaptive.Classes.Contains(size), $"width {width}: AdaptiveBox classes are [{string.Join(",", adaptive.Classes)}]");
                Assert.True(ratio.Classes.Contains(shape), $"width {width}: RatioBox classes are [{string.Join(",", ratio.Classes)}]");
            }
            Assert.Equal("窄：宽度小于 300", Text(window, "AdaptiveLabel"));
            Assert.Equal("高：宽高比小于 1", Text(window, "RatioLabel"));
        }

        // ---- 文件系统 ----

        // The view model wipes %TEMP%\avalonia-behaviors-demo\files on construction, so this test always starts empty
        // (and would also wipe the sandbox of a running copy of the app). Watcher counts are timing-dependent
        // (a new file raises one or two events), so only "at least one" is asserted.
        [AvaloniaFact]
        public async Task File_actions_stay_in_the_sandbox_and_the_watcher_sees_the_write()
        {
            var (window, _, vm) = Open<FileSystemPage, FileSystemViewModel>();
            Assert.Equal("（沙箱是空的）", vm.Listing);

            Press(window, "WriteButton");
            Assert.False(File.Exists(vm.FilePath));

            Press(window, "MkdirButton");
            Assert.Equal("notes", vm.Listing);

            Press(window, "WriteButton");
            Assert.Equal("你好，Behaviors\n", File.ReadAllText(vm.FilePath));
            await UiHelpers.WaitUntil(() => vm.WatchHits >= 1);
            Assert.Equal("notes\n" + Path.Combine("notes", "hello.txt"), vm.Listing);

            Press(window, "AppendButton");
            Assert.Equal("你好，Behaviors\n你好，Behaviors\n", File.ReadAllText(vm.FilePath));

            Press(window, "DeleteFileButton");
            Press(window, "DeleteDirButton");
            Assert.False(Directory.Exists(vm.SubDir));
        }

        // ---- 网络 ----

        // The page talks to its own loopback server, so this needs no network access.
        [AvaloniaFact]
        public async Task Http_request_action_posts_and_gets_from_the_loopback_server()
        {
            var (window, _, _) = Open<NetworkPage, NetworkViewModel>();
            Assert.Equal("状态码 0", Text(window, "PostStatus"));

            Press(window, "PostButton");
            await UiHelpers.WaitUntil(() => Text(window, "PostBody") != "");
            Assert.Equal("pong:ping", Text(window, "PostBody"));
            Assert.Equal("状态码 201", Text(window, "PostStatus"));

            Press(window, "GetButton");
            await UiHelpers.WaitUntil(() => Text(window, "GetBody") != "");
            Assert.Equal("pong", Text(window, "GetBody"));
            Assert.Equal("状态码 200", Text(window, "GetStatus"));
        }

        // ---- 剪贴板 ----

        // The headless clipboard is an in-memory stub that lives for the whole test process, so start by clearing it.
        [AvaloniaFact]
        public async Task Clipboard_actions_copy_read_and_clear()
        {
            var (window, _, vm) = Open<ClipboardPage, ClipboardViewModel>();
            Press(window, "ClearButton");

            Press(window, "CopyButton");
            Press(window, "PasteButton");
            await UiHelpers.WaitUntil(() => vm.Pasted == vm.Input);

            Press(window, "ClearButton");
            Press(window, "PasteButton");
            await UiHelpers.WaitUntil(() => vm.Pasted == "");
        }

        // ---- 截图 ----

        [AvaloniaFact]
        public async Task Render_target_bitmap_behavior_redraws_into_the_image_on_demand()
        {
            var (window, _, vm) = Open<ScreenshotPage, ScreenshotViewModel>();
            await UiHelpers.WaitUntil(() => vm.Renderer.Calls >= 1);
            Assert.IsType<RenderTargetBitmap>(window.ByName<Image>("PreviewImage").Source);
            var calls = vm.Renderer.Calls;
            var before = window.CaptureFrame();

            Press(window, "RerenderButton");
            await UiHelpers.WaitUntil(() => vm.Renderer.Calls > calls);

            // The renderer changes colour on every call, so a real redraw changes pixels.
            Assert.NotEqual(before, window.CaptureFrame());
        }

        // ---- 自定义 ----

        [AvaloniaFact]
        public void Upper_case_behavior_rewrites_typed_and_assigned_text()
        {
            var (window, _, _) = Open<CustomPage, CustomViewModel>();
            var box = window.ByName<TextBox>("UpperBox");

            window.Type(box, "abc");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("ABC", box.Text);

            box.Text = "hello";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("HELLO", box.Text);
        }

        [AvaloniaFact]
        public void Click_count_trigger_fires_on_every_second_press()
        {
            var (window, _, vm) = Open<CustomPage, CustomViewModel>();

            Press(window, "ClickPad");
            Assert.Equal(0, vm.Fires);
            Assert.Equal("星星：", Text(window, "Output"));

            Press(window, "ClickPad");
            Assert.Equal(1, vm.Fires);
            Assert.Equal("星星：★", Text(window, "Output"));

            for (var i = 0; i < 3; i++)
                Press(window, "ClickPad");
            Assert.Equal(2, vm.Fires);
            Assert.Equal("星星：★★", Text(window, "Output"));
        }
    }
}
```

与探针相比，本文件里**没有原样跑过**的写法（执行时若失败先查这几处）：`Drag` 辅助把探针里的两段移动改成了十步；`Open<WindowPopupPage, object>` 在页面没有 ViewModel 时取到的是 null，测试不使用它；`WindowPopupPage` 的三个测试沿用探针的 `Width = 1000`，没有验证 600 宽时是否也能点中；剪贴板测试先清空再复制，探针是先读空剪贴板。

- [ ] **Step 5: 运行**

Run: `dotnet test tests/Avalonia.BehaviorsDemo.Tests`
Expected: `通过: 40，失败: 0`（2 + 7 + 31）。整份大约需要 10 秒：防抖、`AsyncActionGroup`、`TimerTrigger`、`TaskCompletedTrigger` 各要等 0.5–2 秒。

Run: `dotnet build tests/Avalonia.BehaviorsDemo.Tests`
Expected: 0 错误，0 个来自新项目的警告。

失败时的处理同 Task 2 Step 5。特别是：某条测试因为**点击没落到控件上**而失败（计数仍为 0、ViewModel 没变），不要改用 `RaiseEvent` 或直接 `Execute` 命令绕过去——那样测的就不是行为了；先查控件是否在 600 宽的窗口之外（放宽 `window.Width`），或页面在 `Show` 之后是否还会自己滚动（Review Focus 第 1 条）。

- [ ] **Step 6: 破坏检查（临时改 `src/`，必须还原）**

**(a)** 用 Edit 在 `src/Avalonia.BehaviorsDemo/ViewModels/ValidationViewModel.cs` 里：

旧：

```csharp
        public bool CanSubmit => UsernameValid && EmailValid;
```

新：

```csharp
        public bool CanSubmit => UsernameValid || EmailValid;
```

Run: `dotnet test tests/Avalonia.BehaviorsDemo.Tests --filter "FullyQualifiedName~Submit_is_allowed|FullyQualifiedName~Validation_rules"`
Expected: **2 个失败**（用户名合法之后提交就可用了）。

立刻还原：

```bash
git checkout -- src/Avalonia.BehaviorsDemo/ViewModels/ValidationViewModel.cs
git status --short src
```

**(b)** 用 Edit 在 `src/Avalonia.BehaviorsDemo/Views/Pages/FocusKeyboardPage.axaml` 里去掉 Tunnel：

旧：

```xml
                        <KeyTrigger Key="Enter" EventRoutingStrategy="Tunnel">
```

新：

```xml
                        <KeyTrigger Key="Enter">
```

Run: `dotnet test tests/Avalonia.BehaviorsDemo.Tests --filter "FullyQualifiedName~Key_triggers"`
Expected: **失败**（`EnterTunnelCount` 为 0）。

立刻还原：

```bash
git checkout -- src/Avalonia.BehaviorsDemo/Views/Pages/FocusKeyboardPage.axaml
git status --short src
```

**(c)** 用 Edit 在 `src/Avalonia.BehaviorsDemo/Views/Pages/EventToCommandPage.axaml` 里把 B 按钮的延迟从组里拿出来（删掉 `AsyncActionGroup` 的开闭标签，保留中间两行）：

旧：

```xml
                            <AsyncActionGroup Mode="Sequence">
                                <DelayAction Duration="0:0:0.8" />
                                <InvokeCommandAction Command="{Binding MarkCommand}" CommandParameter="B 后（等了 0.8 秒）" />
                            </AsyncActionGroup>
```

新：

```xml
                                <DelayAction Duration="0:0:0.8" />
                                <InvokeCommandAction Command="{Binding MarkCommand}" CommandParameter="B 后（等了 0.8 秒）" />
```

Run: `dotnet test tests/Avalonia.BehaviorsDemo.Tests --filter "FullyQualifiedName~Delay_only_waits"`
Expected: **失败**（点 B 之后立刻就有两条，`Assert.Single` 不成立）。

立刻还原：

```bash
git checkout -- src/Avalonia.BehaviorsDemo/Views/Pages/EventToCommandPage.axaml
git status --short src
```

Expected: 三次 `git status --short src` 都无输出。再跑一次 `dotnet test tests/Avalonia.BehaviorsDemo.Tests`，`通过: 40`。若改了之后测试**仍然通过**，说明测试没测到点上，先修测试再继续。

- [ ] **Step 7: Commit**

```bash
git add hello-avalonia.slnx tests/Avalonia.BehaviorsDemo.Tests
git status --short
git commit -m "test: add Headless tests for BehaviorsDemo"
```

`git status --short` 里只应出现 `hello-avalonia.slnx` 与 `tests/Avalonia.BehaviorsDemo.Tests/` 下的文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

### Task 5: 文档同步、回填 spec、整体验证

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`

**Interfaces:**
- Consumes: Task 1–4 交付的共享库改动、3 个测试项目（本批 72 + 3 个新测试）、`ActivatableLifetimePage` 的修复、Task 1 Step 3 记下的构建警告基线。
- Produces: 与现状一致的 `README.md`；回填后的 spec（批 5 起的计划以它为准）。

- [ ] **Step 1: `README.md` 的测试项目行补上本批三个应用**

用 Edit 替换这一行里的括号部分：

旧：

```markdown
Events、Input、Graphics、CustomControls）
```

新：

```markdown
Events、Input、Graphics、CustomControls、AppDevelopment、Services、Behaviors）
```

- [ ] **Step 2: spec 里 `UiHelpers` 一行与 Task 1 的改动一致**

用 Edit 替换：

旧：

```markdown
并新增 `CultureScope.Invariant()`（固定 `CultureInfo.CurrentCulture` 并在 `Dispose` 时还原） |
```

新：

```markdown
并新增 `CultureScope.Invariant()`（固定 `CultureInfo.CurrentCulture` 并在 `Dispose` 时还原）；批 4 起 `Click` 在 `BringIntoView` **之前**也先 `RunJobs()`（页面在 `Show` 之后自己滚动时，旧顺序会把目标滚走，点击静默落空），并新增 `WaitUntil(condition, timeoutMs = 5000)`（轮询 dispatcher 直到条件成立，超时抛 `TimeoutException`，用于 `Task.Run`、计时器、HTTP、文件监视器等自带时钟的结果） |
```

- [ ] **Step 3: spec 里 `src/` 改动范围的两处说明**

用 Edit 替换（约束一节）：

旧：

```markdown
- 不改演示项目的业务代码；仅在 Appium 确需定位时，给被测控件补 `AutomationProperties.AutomationId`（见「Appium 定位」）。
```

新：

```markdown
- 不改演示项目的业务代码；仅在 Appium 确需定位时，给被测控件补 `AutomationProperties.AutomationId`（见「Appium 定位」）。例外：测试发现的真实缺陷，经用户确认后可在该批计划里单列任务修复，先写会失败的测试再改 `src/`（批 4 的 `ActivatableLifetimePage`，见「批 4 实测结论」）。
```

用 Edit 替换（验证标准一节）：

旧：

```markdown
且 `src/` 下的改动只限于 `AutomationProperties.AutomationId`。
```

新：

```markdown
且 `src/` 下的改动只限于 `AutomationProperties.AutomationId`，以及该批计划里经用户确认的缺陷修复。
```

- [ ] **Step 4: 回填「批 4 实测结论」**

把 spec 里这一行：

```markdown
## 批 4 实测结论（执行后回填）
```

替换为下面的整段。「执行时的实测」一栏按本次执行的真实输出填写；Task 2–4 在执行中若改过断言（与「本份已核实的事实」不符），在"对后续批的决定与发现"末尾逐条写明改了什么、真实输出是什么，不要照抄下面的预期。

```markdown
## 批 4 实测结论（计划 04 执行后）

| 项目 | 测试数 | H1 页数 | 说明 |
|---|---|---|---|
| `Avalonia.AppDevelopmentDemo.Tests` | 20 | 10 | H1 含 `LoggingPage` 接管 `Logger.Sink` 的盲区与还原；H2：`Greeter`（注入假时钟、固定区域设置）、`Counter`、`ListLoggerProvider`；H3：DI 生命周期、本地化切换、静态与动态资源、应用日志与框架日志、未处理异常、三种跨线程写法、`Show(owner)`/`ShowDialog`/拦截关闭、自动化 Peer |
| `Avalonia.ServicesDemo.Tests` | 12 | 8 | 剪贴板往返、三种选择器在不支持时的提示、按路径列真实文件夹（自建临时目录）、`NoopLauncher`（点击前先断言类型）、移动端服务为 null、快捷键配置、窗口激活计数（含重访标签页） |
| `Avalonia.BehaviorsDemo.Tests` | 40 | 15 | H2：7 个 ViewModel 测试；H3：触发器与 Action 的顺序与开关、事件转命令（节流、防抖、`DelayAction` 与 `AsyncActionGroup`）、数据触发器、计时与任务触发器、焦点与键盘路由、校验规则、集合 Action、弹出层与通知、三种拖拽、自适应类名、文件系统沙箱与监视器、回环 HTTP、剪贴板 Action、`RenderTargetBitmapBehavior`、两个自定义行为 |
| `Avalonia.TestingDemo.Tests`（新增） | 3 | — | `UiHelpersTests`：页面自己滚动后仍能点中、`WaitUntil` 等到后台结果、`WaitUntil` 超时抛异常 |

执行时的实测：构建警告数 <填实际值>（批开始前 <基线>）；`dotnet test hello-avalonia.slnx` 共 <填实际值> 个测试通过（批 3 结束时的 195 + 本批 3 + 72）；破坏检查：`Click` 去掉第一个 `RunJobs` → <填个数> 个失败；`LoggingPage` 不还原 sink → <填个数> 个；`OnReplace` 改成改画刷颜色 → <填个数> 个；`e.Handled = false` → <填个数> 个；Launcher 类型守卫改名 → 在点击前失败；`CanSubmit` 改成 `||` → <填个数> 个；KeyTrigger 去掉 Tunnel → <填个数> 个；拆掉 `AsyncActionGroup` → <填个数> 个，均已还原。

**`src/` 改动（用户 2026-10-15 确认）**：`ActivatableLifetimePage` 每次 `Loaded` 都给窗口挂一对 `Activated`/`Deactivated` lambda、从不退订，重访标签页 N 次后一次激活计 N 次。改为具名处理器 + `OnUnloaded` 退订；先写的 `Revisiting_the_activation_tab_still_counts_one_activation_once` 修复前失败（<填实际值：Expected/Actual>）、修复后通过。遗留：同一方法里 `IActivatableLifetime.Activated`/`Deactivated` 的 lambda 形状相同，但桌面与 Headless 下该服务为 null，测不到，未改。

**对后续批的决定与发现**（计划阶段在探针里实测，执行阶段原样复现）：

- **页面在 `Show` 之后自己滚动，第一下点击会静默落空**：`ListBox`（`SelectionMode="Multiple"`）在第一次 `RunJobs` 里把首项滚进视口，把整页带下 305px。连窗口级隧道处理器都收不到按下，`GetVisualsAt` 对能正常点击的按钮也返回空，所以"点没点中"只能用业务结果判断。`UiHelpers.Click` 已改为先清空排队的工作；以后若再遇到"第一下点击没反应、第二下才有"，先怀疑这一类。
- **页面会改进程级状态**：`LoggingPage` 在显示期间接管 `Logger.Sink`，`LogCollector` 在这一页收不到任何东西（冒烟测试的盲区）；`UnhandledExceptionsPage` 订阅 dispatcher 的异常事件。两页都在 `OnUnloaded` 里还原，测试结束前必须 `Close()` 窗口。批 5–6 里凡是改静态或进程级状态的页面，照此加"还原"测试。
- **异步结果一律 `WaitUntil`**：它超时抛异常，所以忘了断言也不会把超时变成通过。"立即为 0"之类的断言后面要跟一个"后来确实变了"的等待，否则行为永远不跑也能通过。唯一的固定等待是 `TimerTrigger`"停在 5 格"——等不出"不会发生的事"。
- **Headless 的平台服务**：剪贴板是进程内存桩；`StorageProvider` 不能弹选择器，但按路径取文件夹**读真实磁盘**；`Launcher` 是 `NoopLauncher`；`InputPane`、`InsetsManager`、`IActivatableLifetime` 为 null；`ShowDialog` 不阻塞，对话框就是又一个 `OwnedWindows`。碰外部资源的测试先断言服务类型，再操作。
- **拖拽要带按键状态**：`MouseMove` 不带 `RawInputModifiers.LeftMouseButton` 时，拖拽行为把它当悬停，什么都不动、也不报错。
- **Behaviors 测试读 ViewModel，不读 `StringFormat` 出来的文字**：页面上的计数文字是给人看的，改文案不该让行为测试失败。
- **风险表里"Headless 下构造即抛"的页面，本批实测都不抛**：`WindowManagementPage`、`StorageProviderPage` 在 `TabWalker` 下 0 个异常，并且都有 H3；本批没有跳过清单。
- **只靠 H1 的页面**：AppDevelopment 的 `WebContentPage`、`DataValidationPage`；Services 的 `FocusManagerPage`；Behaviors 的 `AnimationPage`、`DragPage` 的文字/文件投放、`NetworkPage` 的 `NetworkInformationTrigger`、`ScreenshotPage` 的 `ScreenshotAction`（Headless 下不产出文件）。
```

- [ ] **Step 5: 整体验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | Select-String "个警告|Warning\(s\)|个错误|Error\(s\)"`
Expected: 0 错误；警告数不高于 Task 1 Step 3 记下的基线。

Run: `dotnet test hello-avalonia.slnx`
Expected: 失败 0，共 270 个通过（195 + 3 + 20 + 12 + 40）。

Run: `git status --short`
Expected: 只有 Step 1–4 改动的 `README.md` 与 spec 文件，没有 `C:\Temp\probe-*` 的文件；`git status --short src` 无输出。

- [ ] **Step 6: Commit**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git status --short
git commit -m "docs: record batch 4 results and list the new test projects"
```

`git status --short` 里只应出现上面两个文件。提交信息末尾按 Global Constraints 附 Co-Authored-By 行。

---

## Self-Review

**1. Spec 覆盖（批 4）：**

| spec 要求 | 对应 |
|---|---|
| AppDevelopment、Services、Behaviors 各一个 `Avalonia.<X>Demo.Tests` 项目 | Task 2–4 |
| H1：走完全部标签页、断言页数与无异常 | 每个项目的 `Main_window_walks_every_tab_without_errors`（10 / 8 / 15 页） |
| H1：接入 `LogCollector` | 每个项目的 `Walking_every_tab_logs_no_warnings`；AppDevelopment 另有 2 条测试钉住 `LoggingPage` 造成的盲区与还原 |
| H2：有 `ViewModels/` 的项目测 ViewModel | Behaviors 的 `ViewModelTests`（7）；AppDevelopment 与 Services 没有 `ViewModels/`，AppDevelopment 由 `ServiceTests`（3）顶替，Services 只有 code-behind，不补 |
| H3：含可交互控件的页面至少一条"操作 → 断言界面变化" | 三个项目的 `InteractionTests`（13 / 10 / 31）；只靠 H1 的页面在「本份已核实的事实」末行列出理由 |
| 动画页只做 H1，不断言中间态 | Global Constraints 末条；`AnimationPage` 无 H3 |
| 风险表"窗口管理页、`StorageProvider` 在 Headless 下可能构造即抛"→ 建立带理由的跳过清单 | 探针实测两页都不抛（`TabWalker` 0 个异常），本批跳过清单为空；`WindowManagementPage`、`StorageProviderPage` 都有 H3 |
| 共享辅助放 `Avalonia.Tests.Common`，不在测试项目里复制 | Task 1（`Click` 时序、`WaitUntil`）；各项目的私有辅助（`Text`、`Press`、`SendKey`、`Drag`）只服务于本项目的页面 |
| `src/` 改动只限于 `AutomationId` | 本批例外一处，用户已确认；Global Constraints 第 6 条、Task 3 Step 5、Task 5 Step 3 修订 spec 的两处表述 |
| 回填 spec「批 4 实测结论」、更新 README | Task 5 |

spec 第 126–127 行（`ServicesDemo` 文件对话框取消、`AppDevelopmentDemo` 无障碍页对照 UIA 树）属于 Appium 批，不在本批。

**2. 占位符扫描：** 没有 TBD/TODO/"类似 Task N"。每个任务的 csproj、`TestAppBuilder` 都完整重复了一遍（执行者可能乱序阅读）。Task 5 Step 4 里的 `<填实际值>`、`<填个数>`、`<基线>` 是写给执行者的填空，与批 2、批 3 做法相同。

**3. 类型一致性：**
- `UiHelpers.WaitUntil(Func<bool>, int = 5000) : Task`（Task 1）→ Task 2 的 5 处、Task 3 的 `ClickAndWait`、Task 4 的 13 处都写成 `await UiHelpers.WaitUntil(...)`，所在测试方法都是 `async Task`。
- `UiHelpers.Click(this Window, Control)` 签名未变；Task 4 用它点 `Border`（`ClickPad`、`PressBorder`）与 `ListBoxItem`，都是 `Control`。
- Task 4 的 `Open<TPage, TViewModel>()` 与各页的 ViewModel 类型一一对应：`MechanismPage`→`MechanismViewModel`、`EventToCommandPage`→`EventToCommandViewModel`、`DataTriggerPage`→`DataTriggerViewModel`、`FocusKeyboardPage`→`FocusKeyboardViewModel`、`ValidationPage`→`ValidationViewModel`、`CollectionPage`→`CollectionViewModel`、`DragPage`→`DragViewModel`、`ResponsivePage`→`ResponsiveViewModel`、`FileSystemPage`→`FileSystemViewModel`、`NetworkPage`→`NetworkViewModel`、`ClipboardPage`→`ClipboardViewModel`、`ScreenshotPage`→`ScreenshotViewModel`、`CustomPage`→`CustomViewModel`；`WindowPopupPage` 没有 ViewModel，用 `object`。
- 生成的命令名按 CommunityToolkit 规则（方法名 + `Command`）：`SubmitCommand`、`ResetTodosCommand`、`ScrollToCommand`、`StartWorkCommand`、`WorkDoneCommand`、`StepCommand`、`FinishCommand`、`ClearLogCommand`，都在被测 ViewModel 里有对应的 `[RelayCommand]` 方法。
- 测试数：Task 1 +3、Task 2 20（4 + 3 + 13）、Task 3 12（2 + 10）、Task 4 40（2 + 7 + 31）；累计 195 → 198 → 218 → 230 → 270，与各 Step 的 `Expected` 以及 Task 5 Step 5 一致。
- slnx 插入顺序：`CustomControlsDemo.Tests` 之后依次是 AppDevelopment、Services、Behaviors，每个任务的锚点都是前一个任务刚加的那行，`TestingDemo.Tests` 保持在最后。

**4. Review Focus 覆盖：**

| Review Focus | 测试 |
|---|---|
| 1. 页面加载后自己滚动，第一下点击落空 | Task 1 `Click_reaches_a_control_when_the_page_scrolls_itself_after_show`（Step 2 证明旧行为下失败）；Task 4 `The_first_click_on_the_page_adds_an_item...` |
| 2. `LoggingPage` 接管 `Logger.Sink` | Task 2 `Logging_tab_owns_the_log_sink_only_while_it_is_selected`、`Closing_the_window_on_the_logging_page_restores_the_previous_sink`、`Walking_every_tab_logs_no_warnings` 的 `Assert.Same`（破坏检查 (a) 期望 3 个失败） |
| 3. 异步结果读得太早 | Task 1 两条 `WaitUntil` 测试；Task 2 线程页三条；Task 4 `Debounce_runs_once...`、`Delay_only_waits...`（"立即"断言之后都跟着等待；破坏检查 (c)） |
| 4. 测试碰到真实外部资源 | Task 3 `Launcher_buttons_report...` 在点击前断言 `NoopLauncher`（Step 7 证明守卫在点击前失败）；`Folder_from_path...` 只读自建临时目录 |
| 5. 重访标签页让处理器越挂越多 | Task 3 `Revisiting_the_activation_tab_still_counts_one_activation_once`（Step 4 先红，Step 6 转绿） |
