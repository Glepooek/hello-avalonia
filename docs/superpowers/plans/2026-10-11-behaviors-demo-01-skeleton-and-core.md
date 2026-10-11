# BehaviorsDemo 01：项目骨架与第 1–5 页（机制、事件转命令、数据驱动、焦点键盘指针、表单验证）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新建独立可运行的 `Avalonia.BehaviorsDemo` 项目（左侧 TabControl 导航），并落地前 5 个页面：机制、事件转命令、数据驱动与条件、焦点键盘指针、表单验证。

**Architecture:** 沿用 `Avalonia.InputDemo` 的结构：`App` + `Program` + `MainWindow`（左侧 `TabControl`）+ `Views/Pages/*`（每页一个 `UserControl`，页面自己持有 ViewModel）+ `ViewModels/`。行为一律写在 XAML 的 `Interaction.Behaviors` 里；code-behind 只做 `InitializeComponent()`、`DataContext = new …ViewModel()`，以及第 1 页读行为集合的一行展示。验证用仓库外的 headless 探针（`C:\Temp\probe-bdemo`，不入库）：每页都"触发 → 读回"。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、Xaml.Behaviors.Avalonia 12.0.7、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md`

## Global Constraints

- 新写的代码与 XAML 注释用**英文**；界面文字（标签页标题、说明、按钮文案）用**中文**。
- 中文属性值里不要用 ASCII 双引号（用「」），不用 `&` 与 `<`（会让 XAML 编译失败，AVLN1001）。
- 版本只写在根目录 `Directory.Packages.props`；csproj 里的 `PackageReference` **不带 `Version`**。`Xaml.Behaviors.Avalonia` 12.0.7 已登记，不要换成旧的 `Avalonia.Xaml.Behaviors`（与 v12 二进制不兼容）。
- MVVM 用 CommunityToolkit.Mvvm（`[ObservableProperty]`、`[RelayCommand]`）；默认开启编译绑定，页面根元素写 `x:DataType`。
- 页面类名 `<主题>Page`，命名空间 `Avalonia.BehaviorsDemo.Views.Pages`；ViewModel 命名空间 `Avalonia.BehaviorsDemo.ViewModels`。
- `DemoHeader.DocPath` 在本项目里写**被演示类型的完整名**（如 `Avalonia.Xaml.Interactions.Core.EventTriggerBehavior`），因为这个库没有 docs.avaloniaui.net 上的页面。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`。
- 探针放在仓库外，**不提交**。

### 本份已核实的事实（编写计划时在仓库外的临时项目里 headless 实测）

| 事项 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| XAML 命名空间 | 8 个子包的类型都映射进默认的 `https://github.com/avaloniaui` | 页面里不加任何 `xmlns` 前缀，直接写 `<Interaction.Behaviors>`、`<EventTriggerBehavior>` |
| `EventTriggerBehavior` + `InvokeCommandAction` | 编译绑定下 `Command="{Binding XxxCommand}"`、`CommandParameter="7"` 可用，命令执行 | 命令名是 `[RelayCommand]` 生成的 `XxxCommand` |
| `PassEventArgsToCommand="True"` | 命令收到的是真实的事件参数对象（`Click` 得到 `RoutedEventArgs`，`PointerPressed` 得到 `PointerPressedEventArgs`） | 命令参数类型要写成能接收该对象的类型 |
| `ChangePropertyAction` | 不写 `TargetObject` 时改的是挂载的控件；写 `TargetObject="{Binding #Name}"` 能按元素名指到别的控件 | 两种写法都用得上 |
| 给行为自己的 `IsEnabled` 绑定 | `EventTriggerBehavior IsEnabled="{Binding Gate}"` 生效：关闭后点击不执行，重新打开后恢复 | 行为能像普通对象一样被开关 |
| `EventTriggerBehavior SourceObject` | `SourceObject="{Binding #Src}"` 让触发器监听**另一个**控件的事件 | 页面 2 演示 |
| `ThrottleAction` | 5 次连点只执行 1 次；过了间隔再点又执行 | 页面 2 演示节流 |
| `DebounceAction` | 内部用 `DispatcherTimer`。**headless 里 `Dispatcher.UIThread.RunJobs()` 永远不会让任何 `DispatcherTimer` 走一格**（实测：普通 100ms 计时器泵 0.4 秒仍是 0 次），只有跑 `Dispatcher.UIThread.MainLoop(token)` 才会走；真实应用有主循环，不受影响 | 探针的 `WaitUntil` / `RunLoop` 用 `MainLoop` 切片，不用 `Pump` 去等计时器 |
| `DelayAction` | **单独放在动作序列里不会让后面的动作等待**：延时后面的命令立刻执行 | 必须放进 `AsyncActionGroup Mode="Sequence"` 才真的等待；页面 2 把两种写法并排演示 |
| `DataTriggerBehavior` | `ComparisonCondition`（`Equal`、`NotEqual`、`GreaterThan`、`GreaterThanOrEqual`、`LessThan`、`LessThanOrEqual`）；`Value="3"` 这类字符串会被转成绑定值的类型再比较；`RevertOnFalse="True"` 在条件变假时还原被改的属性 | 页面 3 演示 |
| `ConditionalBehavior` | `Condition="{Binding Flag}"` + `ElseActions`；**只在条件值变化时触发，初始值不触发** | 初始文字要在 XAML 里直接写好，页面上注明 |
| `MultiDataTriggerBehavior` | 子元素 `Conditions` 里放多个 `Condition`（`Binding`、`ComparisonCondition`、`Value`）；全部成立才触发，`RevertOnFalse` 同上 | 页面 3 演示 |
| `TimerTrigger` | `MillisecondsPerTick="50" TotalTicks="3"` 在控件挂载后开始走，同样是 `DispatcherTimer`，要跑 `MainLoop` 才会走 | 探针用 `WaitUntil` |
| `TaskCompletedTrigger` | `Task="{Binding Work}"`：给属性赋一个新的 `Task`，**完成时触发，重新赋值再触发** | 页面 3 的"开始任务"按钮每点一次赋一个新 `Task` |
| `KeyTrigger` 的路由 | 默认 `EventRoutingStrategy` 是 `Direct`：对 `TextBox` 按 Enter **完全不触发**（`TextBox` 在冒泡阶段把 Enter 标成已处理）；写 `EventRoutingStrategy="Tunnel"` 后用真实按键触发成功；默认只响应 `KeyDown` | 页面 4 把"没写路由"与"写了 Tunnel"两个输入框并排对比 |
| `RoutedEventTriggerBase` 的默认路由 | `PointerPressedTrigger` 等基于它的触发器，默认 `EventRoutingStrategy` 是 `Tunnel, Bubble`：**按一次触发两次**；写 `EventRoutingStrategy="Bubble"` 后触发一次 | 页面 4 并排演示 |
| `KeyGestureTrigger` / `ExecuteCommandOnKeyDownBehavior` | `Gesture="Ctrl+S" EventRoutingStrategy="Tunnel"` 与 `Key="F5"` 都能用真实按键触发 | 页面 4 演示 |
| `FocusBehavior` | `IsFocused="{Binding WantFocus, Mode=TwoWay}"`：设 `true` 控件获得焦点；焦点被别的控件拿走时，绑定的属性自动变回 `false` | 页面 4 的复选框会自己取消勾选 |
| `AutoFocusBehavior` / `LoseFocusOnEnterBehavior` | 控件挂载后获得焦点 / 按 Enter 后失去焦点 | 页面 4 演示 |
| `FocusTrapBehavior` | `IsActive="True"` 时 Tab 键在容器内的两个输入框之间循环 | 页面 4 演示 |
| `TextBoxValidationBehavior` | 子元素 `Rules` 里放 `RequiredTextValidationRule`、`MinLengthValidationRule`（`Length`）、`RegexValidationRule`（`Pattern`），各带 `ErrorMessage`；**挂载时就校验一次**（初始为无效）；多条规则同时失败时 `Error` 用换行拼接；`IsValid` 与 `Error` 是可写的 Avalonia 属性 | 页面 5 用 `Mode=OneWayToSource` 把它们写进 ViewModel |
| 真实输入模拟 | headless 里用 `window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None)`（接收者必须是窗口，不是控件）与 `window.MouseDown(point, MouseButton.Left)` / `MouseUp` | 探针用它们，而不是手造事件对象 |

---

## File Structure（本份创建 / 修改）

```
Avalonia.BehaviorsDemo/
├── Avalonia.BehaviorsDemo.csproj           新
├── app.manifest                            新（复制 InputDemo 并改名）
├── Assets/avalonia-logo.ico                新（复制 InputDemo 的）
├── App.axaml / App.axaml.cs                新
├── Program.cs                              新
├── Views/
│   ├── MainWindow.axaml / .axaml.cs        新（Task 1 先空壳，Task 7 整文件替换成 5 个标签）
│   └── Pages/
│       ├── MechanismPage                   新：页面 1
│       ├── EventToCommandPage              新：页面 2
│       ├── DataTriggerPage                 新：页面 3
│       ├── FocusKeyboardPage               新：页面 4
│       └── ValidationPage                  新：页面 5
└── ViewModels/
    ├── MechanismViewModel.cs               新
    ├── EventToCommandViewModel.cs          新
    ├── DataTriggerViewModel.cs             新
    ├── FocusKeyboardViewModel.cs           新
    └── ValidationViewModel.cs              新
hello-avalonia.slnx                         改：登记项目
README.md                                   改：项目表加一行
docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md   改：回填「批 1 实测结论」
```

每个页面含 `.axaml` 与 `.axaml.cs`。探针（仓库外）：`C:\Temp\probe-bdemo\`。

### 抽取与验证的约定

- 下文每个代码块前的 `#### \`相对路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文；探针文件用 `**\`C:\绝对路径\`**` 标题。同一路径出现多次时，按文档顺序写入，**后者覆盖前者**（Task 7 的 `MainWindow.axaml` 就是这样替换 Task 1 的版本）。
- 抽取脚本见文末「附录 A」，保存为 `C:\Temp\extract-plan.ps1`。
- 每个任务的"抽取并构建"指：`powershell -NoProfile -File C:\Temp\extract-plan.ps1 -Plan <本计划> -RepoRoot <仓库根>`，然后 `dotnet build Avalonia.BehaviorsDemo 2>&1 | grep -E " error |个错误"`。期望 0 个错误。

## Task 1: 项目骨架与探针 Harness

**Files:**
- Create: `Avalonia.BehaviorsDemo/` 下的 csproj、manifest、`App`、`Program`、`MainWindow`（空壳）、图标
- Modify: `hello-avalonia.slnx`、`README.md`
- Create（仓库外）: `C:\Temp\probe-bdemo\` 探针项目与 `Harness.cs`

**Interfaces:**
- Produces: 命名空间 `Avalonia.BehaviorsDemo`、`Avalonia.BehaviorsDemo.Views`；探针的 `Harness` 静态类（`Setup()`、`Show<T>()`、`Find<T>()`、`Pump()`、`WaitUntil()`、`Check()`、`Exit()`），后续任务的探针都用它。

- [ ] **Step 1: 创建项目文件**

#### `Avalonia.BehaviorsDemo/Avalonia.BehaviorsDemo.csproj`

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
        <PackageReference Include="Xaml.Behaviors.Avalonia" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

#### `Avalonia.BehaviorsDemo/app.manifest`

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <!-- This manifest is used on Windows only.
       Don't remove it as it might cause problems with window transparency and embedded controls.
       For more details visit https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests -->
  <assemblyIdentity version="1.0.0.0" name="Avalonia.BehaviorsDemo"/>

  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <!-- Windows 10 -->
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
</assembly>
```

#### `Avalonia.BehaviorsDemo/App.axaml`

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.BehaviorsDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader plus the shared caption, hint and stage styles.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>
</Application>
```

#### `Avalonia.BehaviorsDemo/App.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.Views;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Avalonia.BehaviorsDemo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // No window-level DataContext: each page owns its own state.
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

#### `Avalonia.BehaviorsDemo/Program.cs`

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.BehaviorsDemo
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace(LogEventLevel.Debug);
    }
}
```

#### `Avalonia.BehaviorsDemo/Views/MainWindow.axaml`

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="Avalonia.BehaviorsDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Behaviors Demo"
        Width="1000"
        Height="720"
        WindowStartupLocation="CenterScreen">

    <!-- Placeholder shell; Task 7 replaces this file with the full tab list. -->
    <TabControl Margin="12" TabStripPlacement="Left" />
</Window>
```

#### `Avalonia.BehaviorsDemo/Views/MainWindow.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 2: 复制图标，登记到 slnx 与 README**

```bash
mkdir -p Avalonia.BehaviorsDemo/Assets
cp Avalonia.InputDemo/Assets/avalonia-logo.ico Avalonia.BehaviorsDemo/Assets/avalonia-logo.ico
```

用 Edit 在 `hello-avalonia.slnx` 的 `Avalonia.AppDevelopmentDemo` 行后插入（按字母序，`BehaviorsDemo` 排在 `AppDevelopmentDemo` 与 `ControlsDemo` 之间）：

```xml
  <Project Path="Avalonia.BehaviorsDemo/Avalonia.BehaviorsDemo.csproj" />
```

用 Edit 在 `README.md` 的 `Avalonia.ControlsDemo` 行后插入一行（页数等 Task 7 完成后再改成实数，此时只写已落地的 5 页）：

```markdown
| [Avalonia.BehaviorsDemo](Avalonia.BehaviorsDemo) | Xaml.Behaviors.Avalonia：Behavior / Trigger / Action 机制、事件转命令、数据触发、焦点键盘指针、表单验证（后续批次补集合、弹出通知、拖拽、响应式、动画、副作用动作与自定义） |
```

- [ ] **Step 3: 构建骨架**

Run: `dotnet build Avalonia.BehaviorsDemo 2>&1 | grep -E " error |个错误|Warn|Error"`
Expected: `0 个错误`（或 `0 Error(s)`）。

- [ ] **Step 4: 创建探针项目（仓库外，不提交）**

**`C:\Temp\probe-bdemo\probe-bdemo.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <NoWarn>$(NoWarn);CS8981</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.1.2" />
    <PackageReference Include="Avalonia.Headless" Version="12.1.2" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="12.1.2" />
    <PackageReference Include="Xaml.Behaviors.Avalonia" Version="12.0.7" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.BehaviorsDemo\Avalonia.BehaviorsDemo.csproj" />
    <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.Shared\Avalonia.Shared.csproj" />
  </ItemGroup>
</Project>
```

**`C:\Temp\probe-bdemo\Harness.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ProbeBdemo;

public class ProbeApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
    }
}

// Counts Warning-and-above log events so silent binding failures surface in the summary.
sealed class CountingSink : ILogSink
{
    public int Count;
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    { if (IsEnabled(level, area)) { Count++; Console.WriteLine($"  [log {level}/{area}] {messageTemplate}"); } }
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    { if (IsEnabled(level, area)) { Count++; Console.WriteLine($"  [log {level}/{area}] {messageTemplate} :: {string.Join(", ", propertyValues)}"); } }
}

public static class Harness
{
    static int _pass, _fail;
    static readonly CountingSink Sink = new();

    public static void Setup()
    {
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
        Logger.Sink = Sink;
        // Canary: a deliberate warning proves the sink is wired before we trust a zero count.
        Logger.TryGet(LogEventLevel.Warning, "Probe")?.Log(null, "canary");
        var canary = Sink.Count;
        Sink.Count = 0;
        Check("log sink canary received the deliberate warning", canary == 1);
    }

    public static void Pump(int n = 8)
    {
        for (var i = 0; i < n; i++) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
    }

    // DispatcherTimer (DebounceAction, TimerTrigger) never ticks under RunJobs() in headless mode;
    // only a running dispatcher loop advances it. Run the loop in short slices.
    public static void RunLoop(int ms)
    {
        using var cts = new CancellationTokenSource(ms);
        Dispatcher.UIThread.MainLoop(cts.Token);
    }

    public static bool WaitUntil(Func<bool> cond, int timeoutMs = 3000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < end)
        {
            if (cond()) return true;
            RunLoop(20);
        }
        return cond();
    }

    public static Window Show(Control page, double width = 900, double height = 700)
    {
        var w = new Window { Content = page, Width = width, Height = height };
        w.Show();
        Pump();
        return w;
    }

    public static T Find<T>(Control root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().FirstOrDefault(x => x.Name == name)
        ?? throw new InvalidOperationException($"{typeof(T).Name} '{name}' not found");

    public static void Check(string what, bool ok)
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}");
    }

    public static int Exit()
    {
        Console.WriteLine($"\n{_pass} passed, {_fail} failed, {Sink.Count} warning log(s)");
        return _fail == 0 && Sink.Count == 0 ? 0 : 1;
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
// Later tasks add their Probe.RunXxx() calls here, before Exit.
return Harness.Exit();
```

- [ ] **Step 5: 构建并运行探针，确认金丝雀有效**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | tail -5`
Expected: `PASS  log sink canary received the deliberate warning` 与 `1 passed, 0 failed, 0 warning log(s)`。若金丝雀 FAIL，说明日志汇没接上，后面所有"0 条警告"都不可信——先修 Harness 再继续。

- [ ] **Step 6: 提交**

```bash
git add Avalonia.BehaviorsDemo hello-avalonia.slnx README.md
git commit -m "feat: scaffold Avalonia.BehaviorsDemo with an empty tab shell

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 2: 页面 1 机制（Behavior / Trigger / Action）

**Files:**
- Create: `ViewModels/MechanismViewModel.cs`、`Views/Pages/MechanismPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Mech.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `MechanismViewModel`（`Log`、`BehaviorEnabled`、`Runs`、命令 `StepCommand(string?)`、`FinishCommand`、`ClearLogCommand`）；页面控件名 `RunButton`、`Readout`、`CodeReadout`。

- [ ] **Step 1: 写探针（先于页面，预期此时构建失败：页面类不存在）**

**`C:\Temp\probe-bdemo\Probe.Mech.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeMech
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 1: mechanism ==");
        var page = new MechanismPage();
        Show(page);
        var vm = (MechanismViewModel)page.DataContext!;
        var run = Find<Button>(page, "RunButton");
        var readout = Find<TextBlock>(page, "Readout");

        // The three base types really are the contract the library is built on.
        Check("EventTriggerBehavior is an IBehavior", typeof(IBehavior).IsAssignableFrom(typeof(EventTriggerBehavior)));
        Check("InvokeCommandAction is an IAction", typeof(IAction).IsAssignableFrom(typeof(InvokeCommandAction)));

        var attached = Interaction.GetBehaviors(run);
        Check("RunButton carries exactly one behavior", attached.Count == 1);
        Check("CodeReadout names that behavior", Find<TextBlock>(page, "CodeReadout").Text!.Contains("EventTriggerBehavior"));

        var before = readout.Text;
        Click(run); Pump();
        Check("click ran all three log steps in order", vm.Log.Entries.Count == 3 && vm.Log.Entries[0].Contains("Trigger") && vm.Log.Entries[2].Contains("结束"));
        Check("ChangePropertyAction rewrote another control by element name", readout.Text != before);
        Check("Runs == 1 after the first click", vm.Runs == 1);

        vm.BehaviorEnabled = false; Pump();
        Click(run); Pump();
        Check("a disabled behavior does nothing (log unchanged, Runs still 1)", vm.Log.Entries.Count == 3 && vm.Runs == 1);

        vm.BehaviorEnabled = true; Pump();
        Click(run); Pump();
        Check("re-enabled behavior fires again (Runs == 2)", vm.Runs == 2);

        vm.ClearLogCommand.Execute(null); Pump();
        Check("ClearLog empties the trail", vm.Log.Entries.Count == 0);
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeMech.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/MechanismViewModel.cs`

```csharp
using Avalonia.Shared.Helpers;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class MechanismViewModel : ViewModelBase
    {
        public EventLog Log { get; } = new();

        // Bound to the trigger's own IsEnabled: a behavior is an object, so it can be switched like one.
        [ObservableProperty]
        private bool _behaviorEnabled = true;

        [ObservableProperty]
        private int _runs;

        [RelayCommand]
        private void Step(string? text) => Log.Write(text ?? "?");

        [RelayCommand]
        private void Finish()
        {
            Runs++;
            Log.Write($"-- 第 {Runs} 次触发结束 --");
        }

        [RelayCommand]
        private void ClearLog() => Log.Clear();
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/MechanismPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="640"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.MechanismPage"
             x:DataType="vm:MechanismViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="机制：Behavior 挂在控件上，Trigger 监听，Action 执行"
                               DocPath="Avalonia.Xaml.Interactivity.Interaction" />

            <TextBlock Classes="caption" Text="1. 一个按钮上挂一个 EventTriggerBehavior，里面串着四个 Action" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Button Name="RunButton" Content="点我：触发 Click">
                        <Interaction.Behaviors>
                            <!-- Trigger: listens to Click. IsEnabled is bound, so a checkbox can switch the whole behavior off. -->
                            <EventTriggerBehavior EventName="Click" IsEnabled="{Binding BehaviorEnabled}">
                                <InvokeCommandAction Command="{Binding StepCommand}" CommandParameter="① Trigger 收到 Click" />
                                <!-- Action that edits another control: TargetObject points at it by element name. -->
                                <ChangePropertyAction TargetObject="{Binding #Readout}" PropertyName="Text" Value="② ChangePropertyAction 改写了这行文字" />
                                <InvokeCommandAction Command="{Binding StepCommand}" CommandParameter="③ 第二个 InvokeCommandAction 紧跟着执行" />
                                <InvokeCommandAction Command="{Binding FinishCommand}" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                    <CheckBox Content="行为已启用（绑定到 EventTriggerBehavior.IsEnabled）" IsChecked="{Binding BehaviorEnabled}" />
                    <TextBlock Name="Readout" Text="（尚未触发）" />
                    <TextBlock Name="CodeReadout" Classes="hint" Text="" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint"
                       Text="三件套：Behavior 是挂在控件上的对象（附加属性 Interaction.Behaviors）；Trigger 是特殊的 Behavior，等某件事发生；Action 是 Trigger 里的一串步骤，按顺序执行。关掉复选框再点按钮，什么都不会发生。" />

            <TextBlock Classes="caption" Text="2. 触发记录" />
            <Border Classes="stage" Padding="8" Height="180">
                <ListBox ItemsSource="{Binding Log.Entries}" />
            </Border>
            <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,6,0,0">
                <Button Content="清空记录" Command="{Binding ClearLogCommand}" />
                <TextBlock VerticalAlignment="Center" Text="{Binding Runs, StringFormat={}触发次数：{0}}" />
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/MechanismPage.axaml.cs`

```csharp
using System.Linq;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class MechanismPage : UserControl
    {
        public MechanismPage()
        {
            InitializeComponent();
            DataContext = new MechanismViewModel();

            // The XAML above is just sugar over this collection: read it back to see what is attached.
            var behaviors = Interaction.GetBehaviors(RunButton);
            var names = behaviors.Select(b =>
                b is Trigger trigger ? $"{b.GetType().Name}（{trigger.Actions.Count} 个 Action）" : b.GetType().Name);
            CodeReadout.Text = $"Interaction.GetBehaviors(RunButton) 读到 {behaviors.Count} 个：{string.Join("、", names)}";
        }
    }
}
```

- [ ] **Step 4: 抽取并构建，运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | tail -15`
Expected: 全部 `PASS`，末行 `… 0 failed, 0 warning log(s)`（累计数此时 = 1 个金丝雀 + 本页断言）。若 `ChangePropertyAction` 那条 FAIL，先查 `{Binding #Readout}` 是否因元素名拼错而静默失败。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo mechanism page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 3: 页面 2 事件转命令

**Files:**
- Create: `ViewModels/EventToCommandViewModel.cs`、`Views/Pages/EventToCommandPage.axaml(.cs)`
- Create（仓库外）: `Probe.Event.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `EventToCommandViewModel`：属性 `Total`、`LastArgs`、`HelloCount`、`SourceHits`、`ThrottleAttempts`、`ThrottleRuns`、`DebounceAttempts`、`DebounceRuns`、`Log`；命令 `AddCommand(string?)`、`InspectCommand(object?)`、`SourceHitCommand`、`ThrottleAttemptCommand`、`ThrottleRunCommand`、`DebounceAttemptCommand`、`DebounceRunCommand`、`MarkCommand(string?)`、`ClearLogCommand`；公开方法 `Hello()`（给 `CallMethodAction` 用）。控件名见下方探针。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Event.cs`**

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeEvent
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 2: event to command ==");
        var page = new EventToCommandPage();
        var w = Show(page, 900, 1400);
        var vm = (EventToCommandViewModel)page.DataContext!;

        Click(Find<Button>(page, "AddOneButton")); Click(Find<Button>(page, "AddTenButton")); Click(Find<Button>(page, "AddHundredButton")); Pump();
        Check("CommandParameter reaches the command (1 + 10 + 100 = 111)", vm.Total == 111);

        var press = Find<Border>(page, "PressBorder");
        var pt = press.TranslatePoint(new Point(5, 5), w)!.Value;
        w.MouseDown(pt, MouseButton.Left); w.MouseUp(pt, MouseButton.Left); Pump();
        Check("PassEventArgsToCommand delivers the real PointerPressedEventArgs", vm.LastArgs == "PointerPressedEventArgs");

        Click(Find<Button>(page, "HelloButton")); Pump();
        Check("CallMethodAction invoked Hello() on the view model", vm.HelloCount == 1);

        Click(Find<Button>(page, "SourceButton")); Pump();
        Check("SourceObject: a trigger on another control heard the button click", vm.SourceHits == 1);

        var thr = Find<Button>(page, "ThrottleButton");
        for (var i = 0; i < 5; i++) Click(thr);
        Pump();
        Check("Throttle: 5 rapid clicks = 5 attempts, 1 execution", vm.ThrottleAttempts == 5 && vm.ThrottleRuns == 1);
        Thread.Sleep(1000);
        Click(thr); Pump();
        Check("Throttle: after the interval the next click executes again", vm.ThrottleRuns == 2);

        var deb = Find<Button>(page, "DebounceButton");
        for (var i = 0; i < 5; i++) Click(deb);
        Pump();
        Check("Debounce: 5 rapid clicks = 5 attempts, still 0 executions at once", vm.DebounceAttempts == 5 && vm.DebounceRuns == 0);
        Check("Debounce: exactly 1 execution once the quiet period passes", WaitUntil(() => vm.DebounceRuns == 1));
        RunLoop(700);
        Check("Debounce: it does not fire a second time", vm.DebounceRuns == 1);

        vm.ClearLogCommand.Execute(null); Pump();
        Click(Find<Button>(page, "BareDelayButton")); Pump();
        Check("bare DelayAction: the action after it ran immediately (both entries at once)", vm.Log.Entries.Count == 2);
        vm.ClearLogCommand.Execute(null); Pump();
        Click(Find<Button>(page, "GroupDelayButton")); Pump();
        Check("AsyncActionGroup Sequence: only the first entry right after the click", vm.Log.Entries.Count == 1);
        Check("AsyncActionGroup Sequence: the second entry arrives after the delay", WaitUntil(() => vm.Log.Entries.Count == 2));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeMech.Run();
ProbeEvent.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/EventToCommandViewModel.cs`

```csharp
using System;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class EventToCommandViewModel : ViewModelBase
    {
        public EventLog Log { get; } = new();

        [ObservableProperty] private int _total;
        [ObservableProperty] private string _lastArgs = "（还没点过）";
        [ObservableProperty] private int _helloCount;
        [ObservableProperty] private int _sourceHits;
        [ObservableProperty] private int _throttleAttempts;
        [ObservableProperty] private int _throttleRuns;
        [ObservableProperty] private int _debounceAttempts;
        [ObservableProperty] private int _debounceRuns;

        // CommandParameter arrives as the literal string from XAML.
        [RelayCommand]
        private void Add(string? amount) => Total += int.Parse(amount ?? "0");

        [RelayCommand]
        private void Inspect(object? args) => LastArgs = args?.GetType().Name ?? "null";

        // CallMethodAction needs a public, parameterless method on the target object.
        public void Hello() => HelloCount++;

        [RelayCommand] private void SourceHit() => SourceHits++;
        [RelayCommand] private void ThrottleAttempt() => ThrottleAttempts++;
        [RelayCommand] private void ThrottleRun() => ThrottleRuns++;
        [RelayCommand] private void DebounceAttempt() => DebounceAttempts++;
        [RelayCommand] private void DebounceRun() => DebounceRuns++;

        [RelayCommand]
        private void Mark(string? text) => Log.Write($"{DateTime.Now:HH:mm:ss.fff}  {text}");

        [RelayCommand]
        private void ClearLog() => Log.Clear();
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/EventToCommandPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="1200"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.EventToCommandPage"
             x:DataType="vm:EventToCommandViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="事件转命令：EventTriggerBehavior + InvokeCommandAction，以及防抖、节流、延时"
                               DocPath="Avalonia.Xaml.Interactions.Core.EventTriggerBehavior" />

            <TextBlock Classes="caption" Text="1. CommandParameter：同一个命令，三个按钮传不同的数" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="AddOneButton" Content="+1">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding AddCommand}" CommandParameter="1" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="AddTenButton" Content="+10">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding AddCommand}" CommandParameter="10" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="AddHundredButton" Content="+100">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding AddCommand}" CommandParameter="100" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock VerticalAlignment="Center" Text="{Binding Total, StringFormat={}合计：{0}}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="Button 自带 Command 属性，但 Border、TextBlock 这类没有。EventTriggerBehavior 让任何控件的任何事件都能转成命令。" />

            <TextBlock Classes="caption" Text="2. PassEventArgsToCommand：把真实的事件参数交给命令" />
            <Border Name="PressBorder" Classes="stage" Padding="12" Background="#30FFFFFF">
                <Interaction.Behaviors>
                    <EventTriggerBehavior EventName="PointerPressed">
                        <InvokeCommandAction Command="{Binding InspectCommand}" PassEventArgsToCommand="True" />
                    </EventTriggerBehavior>
                </Interaction.Behaviors>
                <TextBlock Text="{Binding LastArgs, StringFormat={}在这块区域按下鼠标。命令收到的参数类型：{0}}" />
            </Border>

            <TextBlock Classes="caption" Text="3. CallMethodAction：直接调用对象上的方法" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="HelloButton" Content="调用 Hello()">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <CallMethodAction TargetObject="{Binding}" MethodName="Hello" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock VerticalAlignment="Center" Text="{Binding HelloCount, StringFormat={}Hello 被调用了 {0} 次}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="方法必须是 public 且无参数；不需要写 ICommand。" />

            <TextBlock Classes="caption" Text="4. SourceObject：触发器挂在 A 上，监听的却是 B 的事件" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="SourceButton" Content="我是被监听的按钮" />
                <TextBlock Name="ListenerBlock" VerticalAlignment="Center"
                           Text="{Binding SourceHits, StringFormat={}旁边这行文字听到了按钮的 Click：{0} 次}">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click" SourceObject="{Binding #SourceButton}">
                            <InvokeCommandAction Command="{Binding SourceHitCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </TextBlock>
            </StackPanel>

            <TextBlock Classes="caption" Text="5. ThrottleAction：第一次立刻执行，间隔内的后续点击被忽略" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="ThrottleButton" Content="连点我（间隔 0.8 秒）">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding ThrottleAttemptCommand}" />
                        </EventTriggerBehavior>
                        <EventTriggerBehavior EventName="Click">
                            <ThrottleAction Interval="0:0:0.8">
                                <InvokeCommandAction Command="{Binding ThrottleRunCommand}" />
                            </ThrottleAction>
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock VerticalAlignment="Center"
                           Text="{Binding ThrottleAttempts, StringFormat={}点击 {0} 次}" />
                <TextBlock VerticalAlignment="Center"
                           Text="{Binding ThrottleRuns, StringFormat={}实际执行 {0} 次}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="6. DebounceAction：每点一次就重新计时，安静下来之后才执行一次" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="DebounceButton" Content="连点我（安静 0.5 秒后执行）">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding DebounceAttemptCommand}" />
                        </EventTriggerBehavior>
                        <EventTriggerBehavior EventName="Click">
                            <DebounceAction Delay="0:0:0.5">
                                <InvokeCommandAction Command="{Binding DebounceRunCommand}" />
                            </DebounceAction>
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock VerticalAlignment="Center"
                           Text="{Binding DebounceAttempts, StringFormat={}点击 {0} 次}" />
                <TextBlock VerticalAlignment="Center"
                           Text="{Binding DebounceRuns, StringFormat={}实际执行 {0} 次}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="节流用于「按住不放也只响应一次」，防抖用于「输入停下来再去搜索」。两者都把子 Action 包在里面。" />

            <TextBlock Classes="caption" Text="7. DelayAction 的陷阱：单独放着不会让后面的 Action 等" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="BareDelayButton" Content="A：DelayAction 单独放">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding MarkCommand}" CommandParameter="A 前" />
                            <DelayAction Duration="0:0:0.8" />
                            <InvokeCommandAction Command="{Binding MarkCommand}" CommandParameter="A 后（没有等）" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="GroupDelayButton" Content="B：放进 AsyncActionGroup Sequence">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <InvokeCommandAction Command="{Binding MarkCommand}" CommandParameter="B 前" />
                            <AsyncActionGroup Mode="Sequence">
                                <DelayAction Duration="0:0:0.8" />
                                <InvokeCommandAction Command="{Binding MarkCommand}" CommandParameter="B 后（等了 0.8 秒）" />
                            </AsyncActionGroup>
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Content="清空" Command="{Binding ClearLogCommand}" />
            </StackPanel>
            <Border Classes="stage" Padding="8" Height="130" Margin="0,6,0,0">
                <ListBox ItemsSource="{Binding Log.Entries}" />
            </Border>
            <TextBlock Classes="hint"
                       Text="看每行前面的时间戳：A 的两行几乎同一毫秒，B 的两行相差约 0.8 秒。DelayAction 只有在 AsyncActionGroup 里才真的让后续步骤排队等待。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/EventToCommandPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class EventToCommandPage : UserControl
    {
        public EventToCommandPage()
        {
            InitializeComponent();
            DataContext = new EventToCommandViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|PASS|FAIL|passed|\[log"`
Expected: 页面 2 的 12 条断言全部 `PASS`，累计 `23 passed, 0 failed, 0 warning log(s)`（1 条金丝雀 + 页面 1 的 10 条 + 页面 2 的 12 条）。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo event-to-command page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 4: 页面 3 数据驱动与条件

**Files:**
- Create: `ViewModels/DataTriggerViewModel.cs`、`Views/Pages/DataTriggerPage.axaml(.cs)`
- Create（仓库外）: `Probe.Data.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `DataTriggerViewModel`：`IsOnline`(bool, 初值 true)、`Score`(double, 初值 40)、`Agreed`(bool)、`Flag`(bool)、`TimerTicks`(int)、`Work`(Task?)、`WorkStatus`(string)、`DoneCount`(int)；命令 `TickCommand`、`StartWorkCommand`、`WorkDoneCommand`。控件名：`StatusText`、`GradeText`、`PassText`、`FlagText`、`StartWorkButton`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Data.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeData
{
    public static void Run()
    {
        Console.WriteLine("== page 3: data triggers ==");
        var page = new DataTriggerPage();
        Show(page, 900, 1100);
        var vm = (DataTriggerViewModel)page.DataContext!;
        var status = Find<TextBlock>(page, "StatusText");
        var grade = Find<TextBlock>(page, "GradeText");
        var pass = Find<TextBlock>(page, "PassText");
        var flag = Find<TextBlock>(page, "FlagText");

        Check("DataTrigger applies at attach for the initial value (online)", status.Text == "在线");
        vm.IsOnline = false; Pump();
        Check("RevertOnFalse restores the original text (offline)", status.Text == "离线");
        vm.IsOnline = true; Pump();
        Check("condition true again re-applies the change", status.Text == "在线");

        Check("Score 40: the less-than branch shows the original text", grade.Text == "不及格");
        vm.Score = 75; Pump();
        Check("Score 75: GreaterThanOrEqual 60 changes the text", grade.Text == "及格");
        vm.Score = 50; Pump();
        Check("Score 50: reverts", grade.Text == "不及格");

        Check("MultiDataTrigger starts unmet", pass.Text == "条件未满足");
        vm.Score = 75; Pump();
        Check("only one of two conditions met: still unmet", pass.Text == "条件未满足");
        vm.Agreed = true; Pump();
        Check("both conditions met: text changes", pass.Text == "可以提交");
        vm.Agreed = false; Pump();
        Check("one condition lost: reverts", pass.Text == "条件未满足");

        Check("ConditionalBehavior does not fire for the initial value", flag.Text == "（初始值不触发）");
        vm.Flag = true; Pump();
        Check("ConditionalBehavior: Flag true runs the main actions", flag.Text == "Flag 变成了 True");
        vm.Flag = false; Pump();
        Check("ConditionalBehavior: Flag false runs ElseActions", flag.Text == "Flag 变成了 False");

        Check("TimerTrigger ticks 5 times and stops", WaitUntil(() => vm.TimerTicks == 5, 5000));
        RunLoop(500);
        Check("TimerTrigger does not tick a sixth time", vm.TimerTicks == 5);

        // Raising Click by hand does not run Button.Command (that happens in OnClick), so drive the bound command.
        var start = Find<Button>(page, "StartWorkButton");
        Check("the button is wired to StartWorkCommand", start.Command == vm.StartWorkCommand);
        vm.StartWorkCommand.Execute(null); Pump();
        Check("task started", vm.WorkStatus == "运行中");
        Check("TaskCompletedTrigger fired when the task finished", WaitUntil(() => vm.DoneCount == 1));
        vm.StartWorkCommand.Execute(null); Pump();
        Check("assigning a new Task fires the trigger again", WaitUntil(() => vm.DoneCount == 2));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/DataTriggerViewModel.cs`

```csharp
using System.Threading.Tasks;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class DataTriggerViewModel : ViewModelBase
    {
        [ObservableProperty] private bool _isOnline = true;
        [ObservableProperty] private double _score = 40;
        [ObservableProperty] private bool _agreed;
        [ObservableProperty] private bool _flag;
        [ObservableProperty] private int _timerTicks;
        [ObservableProperty] private int _doneCount;
        [ObservableProperty] private string _workStatus = "未开始";

        // TaskCompletedTrigger watches this property: every new Task assigned here is observed again.
        [ObservableProperty] private Task? _work;

        [RelayCommand]
        private void Tick() => TimerTicks++;

        [RelayCommand]
        private void StartWork()
        {
            WorkStatus = "运行中";
            Work = Task.Delay(600);
        }

        [RelayCommand]
        private void WorkDone()
        {
            DoneCount++;
            WorkStatus = "已完成";
        }
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/DataTriggerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="1000"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.DataTriggerPage"
             x:DataType="vm:DataTriggerViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="数据驱动与条件：DataTriggerBehavior、MultiDataTriggerBehavior、ConditionalBehavior、TimerTrigger、TaskCompletedTrigger"
                               DocPath="Avalonia.Xaml.Interactions.Core.DataTriggerBehavior" />

            <TextBlock Classes="caption" Text="1. DataTriggerBehavior + RevertOnFalse：绑定值满足条件时改属性，不满足时自动还原" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <CheckBox Content="在线" IsChecked="{Binding IsOnline}" />
                <TextBlock Name="StatusText" VerticalAlignment="Center" Text="离线">
                    <Interaction.Behaviors>
                        <DataTriggerBehavior Binding="{Binding IsOnline}" ComparisonCondition="Equal" Value="True" RevertOnFalse="True">
                            <ChangePropertyAction PropertyName="Text" Value="在线" />
                            <ChangePropertyAction PropertyName="Foreground" Value="LimeGreen" />
                        </DataTriggerBehavior>
                    </Interaction.Behaviors>
                </TextBlock>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="XAML 里写的 Text 是「离线」。条件成立时被改成「在线」，条件不成立时 RevertOnFalse 把它改回写在 XAML 里的原值，不需要再写一个反向的触发器。" />

            <TextBlock Classes="caption" Text="2. ComparisonCondition：数值比较（GreaterThanOrEqual 等六种）" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Width="240" Minimum="0" Maximum="100" Value="{Binding Score}" />
                <TextBlock VerticalAlignment="Center" Text="{Binding Score, StringFormat={}{0:0} 分}" />
                <TextBlock Name="GradeText" VerticalAlignment="Center" Text="不及格">
                    <Interaction.Behaviors>
                        <DataTriggerBehavior Binding="{Binding Score}" ComparisonCondition="GreaterThanOrEqual" Value="60" RevertOnFalse="True">
                            <ChangePropertyAction PropertyName="Text" Value="及格" />
                        </DataTriggerBehavior>
                    </Interaction.Behaviors>
                </TextBlock>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="Value 在 XAML 里是字符串「60」，库会按绑定值的类型（double）转换后再比较。" />

            <TextBlock Classes="caption" Text="3. MultiDataTriggerBehavior：多个条件同时成立才触发" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <CheckBox Content="已阅读条款" IsChecked="{Binding Agreed}" />
                <TextBlock VerticalAlignment="Center" Text="并且上面的分数 ≥ 60，才能提交：" />
                <TextBlock Name="PassText" VerticalAlignment="Center" Text="条件未满足">
                    <Interaction.Behaviors>
                        <MultiDataTriggerBehavior RevertOnFalse="True">
                            <MultiDataTriggerBehavior.Conditions>
                                <Condition Binding="{Binding Agreed}" ComparisonCondition="Equal" Value="True" />
                                <Condition Binding="{Binding Score}" ComparisonCondition="GreaterThanOrEqual" Value="60" />
                            </MultiDataTriggerBehavior.Conditions>
                            <ChangePropertyAction PropertyName="Text" Value="可以提交" />
                        </MultiDataTriggerBehavior>
                    </Interaction.Behaviors>
                </TextBlock>
            </StackPanel>

            <TextBlock Classes="caption" Text="4. ConditionalBehavior：if / else 两组 Action" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <CheckBox Content="Flag" IsChecked="{Binding Flag}" />
                <TextBlock Name="FlagText" VerticalAlignment="Center" Text="（初始值不触发）">
                    <Interaction.Behaviors>
                        <ConditionalBehavior Condition="{Binding Flag}">
                            <ChangePropertyAction PropertyName="Text" Value="Flag 变成了 True" />
                            <ConditionalBehavior.ElseActions>
                                <ChangePropertyAction PropertyName="Text" Value="Flag 变成了 False" />
                            </ConditionalBehavior.ElseActions>
                        </ConditionalBehavior>
                    </Interaction.Behaviors>
                </TextBlock>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="和 DataTriggerBehavior 不同：ConditionalBehavior 只在条件值发生变化时才执行，页面刚打开时的初始值不会触发，所以初始文字要自己写在 XAML 里。" />

            <TextBlock Classes="caption" Text="5. TimerTrigger：挂载后每隔一段时间触发，走完 TotalTicks 次就停" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Width="24" Height="24" Background="DodgerBlue" CornerRadius="12">
                    <Interaction.Behaviors>
                        <TimerTrigger MillisecondsPerTick="300" TotalTicks="5">
                            <InvokeCommandAction Command="{Binding TickCommand}" />
                        </TimerTrigger>
                    </Interaction.Behaviors>
                </Border>
                <TextBlock VerticalAlignment="Center" Text="{Binding TimerTicks, StringFormat={}页面打开后已走 {0} / 5 格}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="6. TaskCompletedTrigger：绑定的 Task 完成时触发；换一个新 Task 又会再触发" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="StartWorkButton" Content="开始任务（0.6 秒）" Command="{Binding StartWorkCommand}" />
                <Border Width="24" Height="24" Background="Orange" CornerRadius="12">
                    <Interaction.Behaviors>
                        <TaskCompletedTrigger Task="{Binding Work}">
                            <InvokeCommandAction Command="{Binding WorkDoneCommand}" />
                        </TaskCompletedTrigger>
                    </Interaction.Behaviors>
                </Border>
                <TextBlock VerticalAlignment="Center" Text="{Binding WorkStatus}" />
                <TextBlock VerticalAlignment="Center" Text="{Binding DoneCount, StringFormat={}完成 {0} 次}" />
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/DataTriggerPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class DataTriggerPage : UserControl
    {
        public DataTriggerPage()
        {
            InitializeComponent();
            DataContext = new DataTriggerViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`。累计断言数记下来，填进 Task 7 的验收清单。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo data-trigger page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 5: 页面 4 焦点、键盘、指针

**Files:**
- Create: `ViewModels/FocusKeyboardViewModel.cs`、`Views/Pages/FocusKeyboardPage.axaml(.cs)`
- Create（仓库外）: `Probe.Focus.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `FocusKeyboardViewModel`：`WantFocus`(bool)、计数属性 `EnterDirectCount`、`EnterTunnelCount`、`SaveCount`、`F5Count`、`PressDefaultCount`、`PressBubbleCount`、`TapCount`；对应命令 `EnterDirectCommand`、`EnterTunnelCommand`、`SaveCommand`、`F5Command`、`PressDefaultCommand`、`PressBubbleCommand`、`TapCommand`。控件名：`AutoBox`、`SelectBox`、`FocusTarget`、`NoRouteBox`、`TunnelBox`、`GestureBox`、`LoseBox`、`Trap1`、`Trap2`、`PressDefaultBorder`、`PressBubbleBorder`、`TapBorder`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Focus.cs`**

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeFocus
{
    static void Press(Window w, PhysicalKey key, RawInputModifiers mods = RawInputModifiers.None)
    {
        w.KeyPressQwerty(key, mods);
        Pump();
    }

    static void ClickAt(Window w, Control c)
    {
        var pt = c.TranslatePoint(new Point(5, 5), w)!.Value;
        w.MouseDown(pt, MouseButton.Left);
        w.MouseUp(pt, MouseButton.Left);
        Pump();
    }

    public static void Run()
    {
        Console.WriteLine("== page 4: focus, keyboard, pointer ==");
        var page = new FocusKeyboardPage();
        var w = Show(page, 900, 1400);
        var vm = (FocusKeyboardViewModel)page.DataContext!;

        var auto = Find<TextBox>(page, "AutoBox");
        var select = Find<TextBox>(page, "SelectBox");
        Check("AutoFocusBehavior focused the box when the page attached", auto.IsFocused);
        select.Focus(); Pump();
        Check("SelectAllOnFocusBehavior selected the whole text", select.SelectionEnd - select.SelectionStart == select.Text!.Length && select.Text.Length > 0);

        var target = Find<TextBox>(page, "FocusTarget");
        vm.WantFocus = true; Pump();
        Check("FocusBehavior: setting the bound property true moves focus", target.IsFocused);
        auto.Focus(); Pump();
        Check("FocusBehavior: focus moving away writes false back (two-way)", vm.WantFocus == false);

        var noRoute = Find<TextBox>(page, "NoRouteBox");
        noRoute.Focus(); Pump();
        Press(w, PhysicalKey.Enter);
        Check("KeyTrigger with the default Direct routing misses Enter inside a TextBox", vm.EnterDirectCount == 0);
        var tunnel = Find<TextBox>(page, "TunnelBox");
        tunnel.Focus(); Pump();
        Press(w, PhysicalKey.Enter);
        Check("KeyTrigger with EventRoutingStrategy=Tunnel catches Enter", vm.EnterTunnelCount == 1);

        Find<TextBox>(page, "GestureBox").Focus(); Pump();
        Press(w, PhysicalKey.S, RawInputModifiers.Control);
        Check("KeyGestureTrigger Ctrl+S", vm.SaveCount == 1);
        Press(w, PhysicalKey.F5);
        Check("ExecuteCommandOnKeyDownBehavior F5", vm.F5Count == 1);

        var lose = Find<TextBox>(page, "LoseBox");
        lose.Focus(); Pump();
        Check("precondition: LoseBox has focus", lose.IsFocused);
        Press(w, PhysicalKey.Enter);
        Check("LoseFocusOnEnterBehavior dropped the focus", !lose.IsFocused);

        var t1 = Find<TextBox>(page, "Trap1");
        var t2 = Find<TextBox>(page, "Trap2");
        t1.Focus(); Pump();
        Press(w, PhysicalKey.Tab);
        Check("FocusTrapBehavior: Tab moves Trap1 to Trap2", t2.IsFocused);
        Press(w, PhysicalKey.Tab);
        Check("FocusTrapBehavior: the next Tab wraps back to Trap1 instead of leaving", t1.IsFocused);

        ClickAt(w, Find<Border>(page, "PressDefaultBorder"));
        Check("PointerPressedTrigger with the default routing fires twice for one press", vm.PressDefaultCount == 2);
        ClickAt(w, Find<Border>(page, "PressBubbleBorder"));
        Check("PointerPressedTrigger with EventRoutingStrategy=Bubble fires once", vm.PressBubbleCount == 1);
        ClickAt(w, Find<Border>(page, "TapBorder"));
        Check("ExecuteCommandOnTappedBehavior ran the command on a real tap", vm.TapCount == 1);
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/FocusKeyboardViewModel.cs`

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class FocusKeyboardViewModel : ViewModelBase
    {
        // Two-way target of FocusBehavior.IsFocused.
        [ObservableProperty] private bool _wantFocus;

        [ObservableProperty] private int _enterDirectCount;
        [ObservableProperty] private int _enterTunnelCount;
        [ObservableProperty] private int _saveCount;
        [ObservableProperty] private int _f5Count;
        [ObservableProperty] private int _pressDefaultCount;
        [ObservableProperty] private int _pressBubbleCount;
        [ObservableProperty] private int _tapCount;

        [RelayCommand] private void EnterDirect() => EnterDirectCount++;
        [RelayCommand] private void EnterTunnel() => EnterTunnelCount++;
        [RelayCommand] private void Save() => SaveCount++;
        [RelayCommand] private void F5() => F5Count++;
        [RelayCommand] private void PressDefault() => PressDefaultCount++;
        [RelayCommand] private void PressBubble() => PressBubbleCount++;
        [RelayCommand] private void Tap() => TapCount++;
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/FocusKeyboardPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="1300"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.FocusKeyboardPage"
             x:DataType="vm:FocusKeyboardViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="焦点、键盘与指针：FocusBehavior、KeyTrigger、KeyGestureTrigger、FocusTrapBehavior、PointerPressedTrigger"
                               DocPath="Avalonia.Xaml.Interactions.Custom.FocusBehavior" />

            <TextBlock Classes="caption" Text="1. AutoFocusBehavior 与 SelectAllOnFocusBehavior" />
            <StackPanel Spacing="6">
                <TextBox Name="AutoBox" Text="页面一打开，这个输入框就自动获得焦点">
                    <Interaction.Behaviors>
                        <AutoFocusBehavior />
                    </Interaction.Behaviors>
                </TextBox>
                <TextBox Name="SelectBox" Text="点进来或 Tab 进来，整段文字立刻被选中">
                    <Interaction.Behaviors>
                        <SelectAllOnFocusBehavior />
                    </Interaction.Behaviors>
                </TextBox>
            </StackPanel>

            <TextBlock Classes="caption" Text="2. FocusBehavior：用绑定的布尔值控制焦点，而且是双向的" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <CheckBox Content="让右边的输入框获得焦点" IsChecked="{Binding WantFocus}" />
                <TextBox Name="FocusTarget" Width="260" Text="被控制的输入框">
                    <Interaction.Behaviors>
                        <FocusBehavior IsFocused="{Binding WantFocus, Mode=TwoWay}" />
                    </Interaction.Behaviors>
                </TextBox>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="勾选复选框，焦点跳到输入框；再点别的地方，焦点离开时复选框会自己取消勾选——因为绑定是双向的。" />

            <TextBlock Classes="caption" Text="3. KeyTrigger 的路由陷阱：同一个 Enter，两种写法" />
            <StackPanel Spacing="6">
                <TextBox Name="NoRouteBox" Text="没写 EventRoutingStrategy：按 Enter 没反应">
                    <Interaction.Behaviors>
                        <KeyTrigger Key="Enter">
                            <InvokeCommandAction Command="{Binding EnterDirectCommand}" />
                        </KeyTrigger>
                    </Interaction.Behaviors>
                </TextBox>
                <TextBox Name="TunnelBox" Text="EventRoutingStrategy=Tunnel：按 Enter 有反应">
                    <Interaction.Behaviors>
                        <KeyTrigger Key="Enter" EventRoutingStrategy="Tunnel">
                            <InvokeCommandAction Command="{Binding EnterTunnelCommand}" />
                        </KeyTrigger>
                    </Interaction.Behaviors>
                </TextBox>
                <StackPanel Orientation="Horizontal" Spacing="16">
                    <TextBlock Text="{Binding EnterDirectCount, StringFormat={}默认路由收到 Enter：{0} 次}" />
                    <TextBlock Text="{Binding EnterTunnelCount, StringFormat={}Tunnel 路由收到 Enter：{0} 次}" />
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="KeyTrigger 默认是 Direct 路由，只有事件直接发给该控件才算。TextBox 自己会把 Enter 标记为已处理，所以第一个输入框永远收不到；改成 Tunnel 后，触发器在 TextBox 之前就看到了按键。" />

            <TextBlock Classes="caption" Text="4. KeyGestureTrigger 与 ExecuteCommandOnKeyDownBehavior" />
            <TextBox Name="GestureBox" Text="在这里按 Ctrl+S 或 F5">
                <Interaction.Behaviors>
                    <KeyGestureTrigger Gesture="Ctrl+S" EventRoutingStrategy="Tunnel">
                        <InvokeCommandAction Command="{Binding SaveCommand}" />
                    </KeyGestureTrigger>
                    <ExecuteCommandOnKeyDownBehavior Key="F5" Command="{Binding F5Command}" />
                </Interaction.Behaviors>
            </TextBox>
            <StackPanel Orientation="Horizontal" Spacing="16" Margin="0,6,0,0">
                <TextBlock Text="{Binding SaveCount, StringFormat={}Ctrl+S：{0} 次}" />
                <TextBlock Text="{Binding F5Count, StringFormat={}F5：{0} 次}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="5. LoseFocusOnEnterBehavior 与 FocusTrapBehavior" />
            <TextBox Name="LoseBox" Text="在这里按 Enter，焦点会离开">
                <Interaction.Behaviors>
                    <LoseFocusOnEnterBehavior />
                </Interaction.Behaviors>
            </TextBox>
            <Border Classes="stage" Padding="8" Margin="0,8,0,0">
                <StackPanel Spacing="6">
                    <Interaction.Behaviors>
                        <FocusTrapBehavior IsActive="True" />
                    </Interaction.Behaviors>
                    <TextBlock Classes="hint" Text="焦点陷阱：点进下面任一输入框，再按 Tab，焦点只会在这两个框之间循环，出不去。" />
                    <TextBox Name="Trap1" Text="陷阱里的输入框 1" />
                    <TextBox Name="Trap2" Text="陷阱里的输入框 2" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="6. 指针触发器：默认路由会触发两次" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Name="PressDefaultBorder" Classes="stage" Width="200" Height="60" Background="#30FFFFFF">
                    <Interaction.Behaviors>
                        <PointerPressedTrigger>
                            <InvokeCommandAction Command="{Binding PressDefaultCommand}" />
                        </PointerPressedTrigger>
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="按下（默认路由）" />
                </Border>
                <Border Name="PressBubbleBorder" Classes="stage" Width="200" Height="60" Background="#30FFFFFF">
                    <Interaction.Behaviors>
                        <PointerPressedTrigger EventRoutingStrategy="Bubble">
                            <InvokeCommandAction Command="{Binding PressBubbleCommand}" />
                        </PointerPressedTrigger>
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="按下（Bubble）" />
                </Border>
                <Border Name="TapBorder" Classes="stage" Width="200" Height="60" Background="#30FFFFFF">
                    <Interaction.Behaviors>
                        <ExecuteCommandOnTappedBehavior Command="{Binding TapCommand}" />
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="点击（Tapped）" />
                </Border>
            </StackPanel>
            <StackPanel Orientation="Horizontal" Spacing="16" Margin="0,6,0,0">
                <TextBlock Text="{Binding PressDefaultCount, StringFormat={}默认路由：{0} 次}" />
                <TextBlock Text="{Binding PressBubbleCount, StringFormat={}Bubble：{0} 次}" />
                <TextBlock Text="{Binding TapCount, StringFormat={}Tapped：{0} 次}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="基于 RoutedEventTriggerBase 的触发器默认同时监听 Tunnel 和 Bubble 两个阶段，一次按下就被数了两次；明确写 Bubble 就只数一次。需要「只一次」时务必写明路由。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/FocusKeyboardPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class FocusKeyboardPage : UserControl
    {
        public FocusKeyboardPage()
        {
            InitializeComponent();
            DataContext = new FocusKeyboardViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo focus, keyboard and pointer page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 6: 页面 5 表单验证

**Files:**
- Create: `ViewModels/ValidationViewModel.cs`、`Views/Pages/ValidationPage.axaml(.cs)`
- Create（仓库外）: `Probe.Valid.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `ValidationViewModel`：`UsernameValid`/`UsernameError`、`EmailValid`/`EmailError`（均由行为用 `OneWayToSource` 写入）、`CanSubmit`、`SubmitResult`、`SubmitCommand`。控件名：`UsernameBox`、`EmailBox`、`SubmitButton`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Valid.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeValid
{
    public static void Run()
    {
        Console.WriteLine("== page 5: validation ==");
        var page = new ValidationPage();
        Show(page);
        var vm = (ValidationViewModel)page.DataContext!;
        var user = Find<TextBox>(page, "UsernameBox");
        var email = Find<TextBox>(page, "EmailBox");
        var submit = Find<Button>(page, "SubmitButton");

        Check("validation runs at attach: empty username is already invalid", !vm.UsernameValid && (vm.UsernameError ?? "").Contains("不能为空"));
        Check("submit starts disabled", !vm.CanSubmit && !submit.IsEffectivelyEnabled);

        user.Text = "ab"; Pump();
        Check("too short: MinLength error replaces Required error", !vm.UsernameValid && vm.UsernameError!.Contains("至少 3") && !vm.UsernameError.Contains("不能为空"));

        user.Text = "a!"; Pump();
        Check("two rules failing: Error joins both messages with a newline", vm.UsernameError!.Contains("至少 3") && vm.UsernameError.Contains("只能包含") && vm.UsernameError.Contains('\n'));

        user.Text = "alice_1"; Pump();
        Check("valid username clears the error", vm.UsernameValid && string.IsNullOrEmpty(vm.UsernameError));
        Check("submit still disabled while the email is invalid", !vm.CanSubmit);

        email.Text = "x"; Pump();
        Check("malformed email is invalid", !vm.EmailValid && !string.IsNullOrEmpty(vm.EmailError));

        email.Text = "alice@example.com"; Pump();
        Check("valid email", vm.EmailValid && string.IsNullOrEmpty(vm.EmailError));
        Check("both valid: CanSubmit true and the button enables", vm.CanSubmit && submit.IsEffectivelyEnabled);

        vm.SubmitCommand.Execute(null); Pump();
        Check("submit ran", vm.SubmitResult.Contains("已提交"));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/ValidationViewModel.cs`

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class ValidationViewModel : ViewModelBase
    {
        // The four properties below are written by TextBoxValidationBehavior (Mode=OneWayToSource).
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private bool _usernameValid;

        [ObservableProperty] private string? _usernameError;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private bool _emailValid;

        [ObservableProperty] private string? _emailError;

        [ObservableProperty] private string _submitResult = "";

        public bool CanSubmit => UsernameValid && EmailValid;

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private void Submit() => SubmitResult = "已提交：两个字段都通过了校验";
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/ValidationPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="640"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.ValidationPage"
             x:DataType="vm:ValidationViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="表单验证：TextBoxValidationBehavior 与一组 ValidationRule"
                               DocPath="Avalonia.Xaml.Interactions.Custom.TextBoxValidationBehavior" />

            <TextBlock Classes="caption" Text="1. 用户名：必填 + 至少 3 个字符 + 只能含字母数字下划线" />
            <TextBox Name="UsernameBox" Width="320" HorizontalAlignment="Left" PlaceholderText="请输入用户名">
                <Interaction.Behaviors>
                    <TextBoxValidationBehavior IsValid="{Binding UsernameValid, Mode=OneWayToSource}"
                                               Error="{Binding UsernameError, Mode=OneWayToSource}">
                        <TextBoxValidationBehavior.Rules>
                            <RequiredTextValidationRule ErrorMessage="用户名不能为空" />
                            <MinLengthValidationRule Length="3" ErrorMessage="至少 3 个字符" />
                            <RegexValidationRule Pattern="^[A-Za-z0-9_]*$" ErrorMessage="只能包含字母、数字、下划线" />
                        </TextBoxValidationBehavior.Rules>
                    </TextBoxValidationBehavior>
                </Interaction.Behaviors>
            </TextBox>
            <TextBlock Foreground="OrangeRed" Text="{Binding UsernameError}" />

            <TextBlock Classes="caption" Text="2. 邮箱：必填 + 正则" />
            <TextBox Name="EmailBox" Width="320" HorizontalAlignment="Left" PlaceholderText="name@example.com">
                <Interaction.Behaviors>
                    <TextBoxValidationBehavior IsValid="{Binding EmailValid, Mode=OneWayToSource}"
                                               Error="{Binding EmailError, Mode=OneWayToSource}">
                        <TextBoxValidationBehavior.Rules>
                            <RequiredTextValidationRule ErrorMessage="邮箱不能为空" />
                            <RegexValidationRule Pattern="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="邮箱格式不对" />
                        </TextBoxValidationBehavior.Rules>
                    </TextBoxValidationBehavior>
                </Interaction.Behaviors>
            </TextBox>
            <TextBlock Foreground="OrangeRed" Text="{Binding EmailError}" />

            <StackPanel Orientation="Horizontal" Spacing="12" Margin="0,16,0,0">
                <Button Name="SubmitButton" Content="提交" Command="{Binding SubmitCommand}" />
                <TextBlock VerticalAlignment="Center" Text="{Binding SubmitResult}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="行为只负责判断：IsValid 与 Error 通过 OneWayToSource 绑定写进 ViewModel，「提交」按钮是否可用由 ViewModel 决定。多条规则同时不满足时，Error 把各条消息用换行拼在一起。页面一打开就已经校验过一次，所以空输入框一开始就是无效状态。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/ValidationPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ValidationPage : UserControl
    {
        public ValidationPage()
        {
            InitializeComponent();
            DataContext = new ValidationViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo form validation page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 7: 登记全部页面、整体探针、README 与 spec 回填

**Files:**
- 覆盖: `Avalonia.BehaviorsDemo/Views/MainWindow.axaml`（Task 1 的空壳 → 5 个标签）
- Modify: `README.md`（项目行改成实数）、`docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md`（追加「批 1 实测结论」）
- Create（仓库外）: `Probe.Shell.cs`；覆盖 `Program.cs`

- [ ] **Step 1: 写探针（先于 MainWindow）**

**`C:\Temp\probe-bdemo\Probe.Shell.cs`**

```csharp
using Avalonia.BehaviorsDemo.Views;
using Avalonia.Controls;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeShell
{
    public static void Run()
    {
        Console.WriteLine("== shell: MainWindow ==");
        var window = new MainWindow();
        window.Show();
        Pump();
        var tabs = Find<TabControl>(window, "MainTabs");
        Check("MainWindow lists 5 tabs", tabs.Items.Count == 5);
        var headers = tabs.Items.OfType<TabItem>().Select(t => t.Header?.ToString()).ToArray();
        Check("tab headers match the page order", string.Join("|", headers) == "机制|事件转命令|数据驱动|焦点键盘指针|表单验证");
        for (var i = 0; i < tabs.Items.Count; i++)
        {
            tabs.SelectedIndex = i;
            Pump();
            var item = (TabItem)tabs.Items[i]!;
            Check($"tab {i + 1} ({headers[i]}) renders a page", item.Content is UserControl);
        }
        window.Close();
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeShell.Run();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写完整的 MainWindow（覆盖 Task 1 的空壳）**

#### `Avalonia.BehaviorsDemo/Views/MainWindow.axaml`

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:pages="using:Avalonia.BehaviorsDemo.Views.Pages"
        mc:Ignorable="d"
        d:DesignWidth="1000"
        d:DesignHeight="720"
        x:Class="Avalonia.BehaviorsDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Behaviors Demo"
        Width="1000"
        Height="720"
        WindowStartupLocation="CenterScreen">

    <TabControl Name="MainTabs" Margin="12" TabStripPlacement="Left">
        <TabItem Header="机制">
            <pages:MechanismPage />
        </TabItem>
        <TabItem Header="事件转命令">
            <pages:EventToCommandPage />
        </TabItem>
        <TabItem Header="数据驱动">
            <pages:DataTriggerPage />
        </TabItem>
        <TabItem Header="焦点键盘指针">
            <pages:FocusKeyboardPage />
        </TabItem>
        <TabItem Header="表单验证">
            <pages:ValidationPage />
        </TabItem>
    </TabControl>
</Window>
```

- [ ] **Step 3: 抽取、构建、运行全部探针**

Run: `dotnet build Avalonia.BehaviorsDemo 2>&1 | grep -E " error |个错误"` 然后 `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 构建 0 个错误；探针无 `FAIL`，末行 `74 passed, 0 failed, 0 warning log(s)`（1 金丝雀 + Shell 7 + 页面 1 的 10 + 页面 2 的 12 + 页面 3 的 19 + 页面 4 的 15 + 页面 5 的 10）。

- [ ] **Step 4: 更新 README 的项目行**

把 Task 1 写入的那一行（`| [Avalonia.BehaviorsDemo]…`）整行替换为：

```markdown
| [Avalonia.BehaviorsDemo](Avalonia.BehaviorsDemo) | Xaml.Behaviors.Avalonia：Behavior / Trigger / Action 机制、事件转命令（防抖、节流、延时）、数据触发与条件、焦点键盘指针、表单验证（侧边导航，当前 5 页；集合、弹出通知、拖拽、响应式、动画、副作用动作、自定义见后续批次） |
```

- [ ] **Step 5: 回填 spec 的「批 1 实测结论」**

在 spec 末尾（「待确认」小节之后）追加下面这段：

```markdown

## 批 1 实测结论（计划 01 执行后）

- **计时器类行为在 headless 里需要跑主循环**：`DebounceAction`、`TimerTrigger` 内部都是 `DispatcherTimer`，`Dispatcher.UIThread.RunJobs()` 不会让它们走一格，必须用 `Dispatcher.UIThread.MainLoop(token)` 切片。探针的 `WaitUntil` / `RunLoop` 据此实现；此前 spike 里"Sleep 之后再泵就能读到结果"的印象是错的。
- **`Button.Command` 不会被手工 `RaiseEvent(Click)` 触发**：命令在 `OnClick` 里执行，探针对绑了 `Command` 的按钮直接调用 `vm.XxxCommand.Execute`，并另外断言 `button.Command == vm.XxxCommand`。
- **`DelayAction` 单独放着不会让后面的 Action 等**，只有放进 `AsyncActionGroup Mode="Sequence"` 才排队（页面 2 并排演示）。
- **`KeyTrigger` 默认 Direct 路由收不到 `TextBox` 里的 Enter**，要写 `EventRoutingStrategy="Tunnel"`；`PointerPressedTrigger` 默认 `Tunnel, Bubble` 会触发两次，写 `Bubble` 才是一次（页面 4 并排演示）。
- **`ConditionalBehavior` 不对初始值触发**，`DataTriggerBehavior` 与 `MultiDataTriggerBehavior` 会在挂载时按当前值执行一次。
- **`TextBoxValidationBehavior` 在挂载时就校验一次**，多条规则失败时 `Error` 用换行拼接，`IsValid` / `Error` 可用 `OneWayToSource` 写进 ViewModel。
- 全部 8 个子包的类型都在默认 XAML 命名空间里，5 个页面没有加任何 `xmlns` 前缀，也没有遇到与 Avalonia 自带类型的同名歧义。
- 探针累计 74 条断言全部通过，0 条警告日志（金丝雀已确认日志汇有效）。
```

- [ ] **Step 6: 全解决方案构建，提交**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E " error |个错误"`
Expected: 0 个错误。

```bash
git add Avalonia.BehaviorsDemo README.md docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md
git commit -m "feat: register all five BehaviorsDemo pages and record batch 1 findings

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

- **Spec 覆盖**：骨架（Task 1）、页面 1–5（Task 2–6）、slnx 与 README 登记（Task 1、7）、spec 回填（Task 7）。页面 6–15 属于计划 02、03，不在本份范围。
- **占位扫描**：无 TBD/TODO；每个代码步骤都有完整代码；每个探针断言都有明确期望。
- **类型一致**：各 ViewModel 的命令名由 `[RelayCommand]` 方法名加 `Command` 生成，XAML 里引用的名字与方法一一对应（`Step`→`StepCommand`、`Add`→`AddCommand`、`Tick`→`TickCommand`、`Save`→`SaveCommand`、`F5`→`F5Command` 等）；控件名在探针与 XAML 中逐一对照过。
- **实测过程**：本计划的所有代码块都用附录 A 的脚本抽取、构建并跑通了探针（见 Task 7 Step 3 的期望数）。

## 附录 A：抽取脚本 `C:\Temp\extract-plan.ps1`

按文档顺序把「`#### \`路径\``」或「`**\`绝对路径\`**`」标题后的第一个代码块写成文件；同一路径后者覆盖前者。

```powershell
param(
    [Parameter(Mandatory)] [string] $Plan,
    [Parameter(Mandatory)] [string] $RepoRoot
)

# Extract files from a plan: "#### `rel/path`" (repo-relative) or "**`C:\abs\path`**" (absolute),
# each followed by a fenced code block. Writes in document order, so a later block overwrites an earlier one.
$lines = [System.IO.File]::ReadAllLines($Plan, [System.Text.Encoding]::UTF8)
$utf8 = New-Object System.Text.UTF8Encoding($false)
$count = 0
$target = $null
$inFence = $false
$buf = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines) {
    if (-not $inFence) {
        if ($line -match '^####\s+`(?<p>[^`]+)`\s*$') {
            $target = Join-Path $RepoRoot $Matches['p']
        }
        elseif ($line -match '^\*\*`(?<p>[A-Za-z]:\\[^`]+)`\*\*\s*$') {
            $target = $Matches['p']
        }
        elseif ($line -match '^```' -and $target) {
            $inFence = $true
            $buf.Clear()
        }
        elseif ($line -match '^#{1,3}\s' -or $line -match '^- \[') {
            $target = $null
        }
    }
    else {
        if ($line -match '^```\s*$') {
            $dir = Split-Path $target -Parent
            if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
            [System.IO.File]::WriteAllText($target, (($buf -join "`n") + "`n"), $utf8)
            $count++
            $inFence = $false
            $target = $null
        }
        else { $buf.Add($line) }
    }
}
Write-Output "-- $count files"
```
