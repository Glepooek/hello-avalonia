# ControlsDemo 00：导航壳与 Input/Buttons 样板 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新建 `Avalonia.ControlsDemo` 项目：侧边导航壳、`PageCatalog` 单点登记、仓库外的 headless 探针骨架，并以 Input 分类下 Buttons 子类的 8 个控件页作为样板。本份是 9 份计划中的第一份，**后 8 份全部依赖本份定义的类型与约定**。

**Architecture:** `MainWindow` 是 `SplitView`：左侧 `TreeView` 显示「分类 → 页面」，右侧 `ContentControl` 显示当前页。`PageCatalog`（静态 partial 类）持有全部 `PageEntry`，每个官方分类一个 partial 文件，各份计划只改自己分类的文件，互不冲突。选中某个 `PageEntry` 时由 `Activator.CreateInstance(entry.PageType)` 实例化页面。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2、中央包管理；探针用 `Avalonia.Headless` 12.1.2

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`

**本系列 9 份计划（按此顺序执行）：**

| # | 文件 | Control 页 | HowTo 页 | Signpost 页 |
|---|---|---|---|---|
| 00 | `2026-10-10-controls-demo-00-shell-and-buttons.md`（本份） | 8 | 0 | 0 |
| 01 | `2026-10-10-controls-demo-01-input.md`（Input 其余） | 14 | 4 | 0 |
| 02 | `2026-10-10-controls-demo-02-layout.md` | 20 | 3 | 0 |
| 03 | `2026-10-10-controls-demo-03-data-display.md` | 12 | 4 | 0 |
| 04 | `2026-10-10-controls-demo-04-feedback-media.md` | 7 | 2 | 0 |
| 05 | `2026-10-10-controls-demo-05-menus.md` | 5 | 1 | 0 |
| 06 | `2026-10-10-controls-demo-06-navigation.md` | 8 | 2 | 0 |
| 07 | `2026-10-10-controls-demo-07-primitives-system.md` | 6 | 3 | 0 |
| 08 | `2026-10-10-controls-demo-08-web-premium-signposts.md` | 0 | 0 | 20 |
| 合计 | | **80** | **19** | **20** |

## Global Constraints

以下约束适用于本系列每一份计划的每一个任务（取自 spec，数值原样照抄）：

- **Avalonia 版本统一为 12.1.2**，不降级到 11
- **TargetFramework 为 `net10.0`**，`Nullable` 为 `enable`，`AvaloniaUseCompiledBindingsByDefault` 为 `true`
- **包版本只在 `Directory.Packages.props` 声明**，`.csproj` 的 `PackageReference` 不带 `Version`
- **不引用 `Avalonia.Diagnostics`**；**不引入 ReactiveUI、Prism 等第三方 UI/MVVM 框架**
- **不为演示项目写自动化测试**；验证用仓库外的 headless 探针，跑完即弃，不提交
- **C# 与 XAML 注释用英文；界面文字（说明条、按钮文案、导航标题除外）用中文；标识符用英文**
- **每个页面顶部必须有 `Avalonia.Shared` 的 `DemoHeader`**（`Title` 中文 + `DocPath`）。`DocPath` 写官方路径，不带域名、不带前导斜杠，如 `controls/input/buttons/button`；How-to 页写 `docs/how-to/xxx-how-to`
- **每个控件一个 `UserControl`**，页面文件放 `Views/Pages/<分类目录>/` 下
- **去重规则**：已被现有项目深度演示的控件仍做控件页，但页面末尾加一行 `更深入的演示：Avalonia.XxxDemo →「某 Tab」`（`TextBlock Classes="hint"`）；路标页不写代码
- **付费控件（Pro / Enterprise）只做路标页，不做可运行演示**
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本系列的命名与结构约定（所有计划共同遵守）

1. **页面类名**：`<控件名>Page`（`ButtonPage`、`NumericUpDownPage`）。控件名本身以 `Page` 结尾的（`NavigationPage`、`DrawerPage`、`TabbedPage`、`CarouselPage`、`ContentPage`）写成 `<控件名>DemoPage`（`NavigationDemoPage`）。How-to 页：`<主题>HowToPage`（`TreeViewHowToPage`）。路标页：`<主题>SignpostPage`。
2. **命名空间一律扁平**：所有页面的 C# 命名空间都是 `Avalonia.ControlsDemo.Views.Pages`，所有 ViewModel 都是 `Avalonia.ControlsDemo.ViewModels`，**与所在子目录无关**。原因：按目录取命名空间会得到 `...Pages.Input`、`...Pages.Layout`、`...Pages.Media`，与 `Avalonia.Input`、`Avalonia.Layout`、`Avalonia.Media` 同名，页面代码里 `using Avalonia.Layout;` 后会出现二义性。XAML 的 `x:Class` 同样写扁平命名空间。
3. **页面子目录**：`Views/Pages/Input/`、`Layout/`、`DataDisplay/`、`Feedback/`、`Media/`、`Menus/`、`Navigation/`、`Primitives/`、`System/`、`Web/`、`Premium/`、`HowTo/`（与 spec 一致，只是目录，不影响命名空间）。How-to **实战页**放在它所属控件分类的目录里，不放 `HowTo/`；`HowTo/` 只放横切主题的路标页。
4. **登记**：新增页面 = 建页面文件 + 在 `Navigation/PageCatalog.<分类>.cs` 对应方法里加一行。How-to 页登记在它所属控件之后。
5. **页面状态**：无状态页用 code-behind + 命名元素；有状态（需要命令、可观察属性）才建 ViewModel（`CommunityToolkit.Mvvm`），页面在构造函数里 `DataContext = new XxxViewModel()`，并写 `x:DataType`。
6. **元素命名**：探针靠 `Name` 找元素，页面里每个被断言的交互元素都要有 `Name`，且不与继承属性重名（规则 7）。

### 硬性规则（前几组实测，Avalonia 12.1.2；每一条都对应一次"构建通过、行为却错"）

1. **打算被样式/过渡驱动的属性，不在元素上写本地值**（本地值优先级最高，永久压过 Style Setter，且无日志）。
2. **`StringFormat` 里字面花括号双写；以 `{0}` 开头的格式串写成 `'{}{0} …'`**（否则 `AVLN2000` 构建失败）；以字面文字开头的不受影响。单层花括号会让格式化**静默产出空字符串**。
3. **`double` 绑定到 `Thickness` / `CornerRadius` 等结构体属性必须走转换器**（`Avalonia.Shared/Converters/DoubleToThicknessConverter.cs` 已有），否则静默失败、只记一条 binding error。
4. **元素名绑定 `{Binding #Name.Prop}` 在 `AvaloniaUseCompiledBindingsByDefault=true` 下无需 `x:DataType`。**
5. **`Watermark` 已过时（`AVLN5001`），用 `PlaceholderText`。** `Window.SystemDecorations` 已过时，用 `WindowDecorations`。
6. **派生自现有控件的自定义控件要覆盖 `StyleKeyOverride`**，否则找不到主题、不渲染、点不了，且无报错（自带 ControlTheme 的除外）。
7. **页面里元素 `Name` 不能与继承属性重名**（如 `Opacity`），否则生成字段遮住继承成员，`CS0108`。
8. **XAML 里 `HotKey="Ctrl+1"` 的数字键被解析成枚举数值**（`Key.Back`），要写 `Ctrl+D1`。
9. **被 Transition 驱动的附加属性要先写显式起始值**（`Canvas.Left` 默认 `NaN`，没有起点可插值）。
10. **`ThemeDictionaries` 里的键用 `StaticResource` 取得 null**，要用 `DynamicResource`。
11. **非控件对象（如 `Flyout`、`MenuFlyout`、`Transition`）不能写 `Name`**（`AVLN2000`）；元素名绑定可以绑它们的属性，但要从拥有者控件出发。
12. **`RadioButton.IsCheckedChanged` 触发时，同组另一个按钮还没取消选中**，要按 `sender` 判断，不能读兄弟按钮状态。
13. **`PopupFlyoutBase` 要 `using Avalonia.Controls.Primitives`。**
14. **LSP 对 `InitializeComponent`、命名元素的 `CS0103` 是误报**，以 `dotnet build` 为准。

### 探针写法（所有计划共用）

- 探针建在 **`C:\Temp\probe-controls\`**，**跨 9 份计划复用**，到第 08 份收尾时才删。第 00 份建立骨架，其余各份只新增 `Probe.<分类>.cs` 并在 `Main` 里加一行调用。
- 探针 csproj 要写 `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>`，否则 `NU1008`；包版本写死。
- 直接 `new` 真实页面类，放进 `Window` → `Show()` → `UpdateLayout()` → `Pump()`，读回属性值断言；挂 `ILogSink` 自动捕获 ≥Warning 的日志，断言"零警告"。
- **`RaiseEvent(Button.ClickEvent)` 只触发 `Click` 处理器，不执行 `Command`**；要测命令用 `Command.Execute(CommandParameter)`。命令禁用时断言 **`IsEffectivelyEnabled`**，`IsEnabled` 不变。
- **headless 默认后端里所有 `Bitmap` 的 `PixelSize` 都是 `1×1`**；`KeyPressQwerty` 只发 KeyDown/KeyUp，要输入字符补 `KeyTextInput`。
- 弹出层（`Flyout`、`ContextMenu`、`Popup`、`ToolTip`）的内容在独立的弹出层里，不在页面的可视树下，要从 `Flyout.Content` / `MenuFlyout.Items` 等入口取。
- 命名颜色读回是名字（`Red`）而非十六进制；未绑定 `DataContext` 的 `TextBlock.Text` 读回 `null` 而非 `""`。

### 本系列新增的已核实事实（编写计划时实测）

- **Buttons 枚举与类型**：`ButtonSpinner.ButtonSpinnerLocation` 的类型是 `Avalonia.Controls.Location`（`Left`、`Right`）；`SpinDirection` 是 `Increase`、`Decrease`；`ClickMode` 是 `Release`、`Press`；`RepeatButton.Interval` 与 `Delay` 是毫秒 `int`；`HyperlinkButton.NavigateUri` 是 `Uri`；`SplitButton.Flyout` 的类型是 `FlyoutBase`。`Spinner` 的路由事件 `SpinEvent` 对应 CLR 事件 `Spin`。

- **`Avalonia.Controls.DataGrid` 12.1.2 与 `Avalonia.Controls.ColorPicker` 12.1.2 存在，且是独立包**（`DataGrid`、`ColorPicker`、`ColorView` 不在主包 `Avalonia.Controls.dll` 里）。它们依赖 `Avalonia 12.1.0`。这是 spec「依赖与技术选型」"不新增包"的**唯一例外**，本份 Task 1 在 `Directory.Packages.props` 声明版本，第 01、03 份各自在 csproj 里引用。**两者都必须额外 `StyleInclude` 才会渲染，否则静默为空**，路径与实测数据见本份 Task 7 Step 1。
- **以下控件在主包里存在**（类型已用反射确认）：`Carousel`、`NumericUpDown`、`PipsPager`、`TabStrip`、`CommandBar`、`NavigationPage`、`DrawerPage`、`TabbedPage`、`CarouselPage`、`ContentPage`、`TableView`、`RefreshContainer`、`SplitButton`、`ToggleSplitButton`、`GroupBox`、`MaskedTextBox`、`Viewbox`、`TrayIcon`、`LayoutTransformControl`、`Label`、`TransitioningContentControl`、`CalendarDatePicker`、`TimePicker`、`AutoCompleteBox`、`ButtonSpinner`、`HyperlinkButton`、`Decorator`、`WindowDrawnDecorations`（`Avalonia.Controls.Chrome`）、`Expander`、`SplitView`、`GridSplitter`、`NativeMenu`、`MenuFlyout`、`WindowNotificationManager`（`Avalonia.Controls.Notifications`）、`SelectableTextBlock`。`TextTrimming` 在 `Avalonia.Media`。
- **Buttons 相关成员**（反射核实）：`ButtonSpinner` 有 `AllowSpin`、`ShowButtonSpinner`、`ButtonSpinnerLocation`，基类 `Spinner` 提供 `Spin` 事件与 `SpinEventArgs(Direction, UsingMouseWheel)`；`SplitButton` 有 `Command`、`CommandParameter`、`Flyout`、`HotKey` 与 `Click` 事件；`ToggleSplitButton` 继承 `SplitButton`，多 `IsChecked` 与 `IsCheckedChanged`；`RepeatButton` 有 `Interval`、`Delay`；`HyperlinkButton` 有 `IsVisited`、`NavigateUri`；`ToggleButton` 有 `IsChecked`、`IsThreeState`、`IsCheckedChanged`。

---

## File Structure（本份创建）

```
Avalonia.ControlsDemo/
├── Avalonia.ControlsDemo.csproj
├── app.manifest                      复制自 LayoutDemo
├── Assets/avalonia-logo.ico          复制自 LayoutDemo
├── Program.cs
├── App.axaml / App.axaml.cs
├── Navigation/
│   ├── PageKind.cs                   enum：Control / HowTo / Signpost
│   ├── PageEntry.cs                  一个页面的元数据
│   ├── CategoryNode.cs               导航树的分类节点
│   ├── PageCatalog.cs                登记处：All、Categories、辅助方法、分类名常量
│   └── PageCatalog.<分类>.cs × 12    每分类一个 partial，本份先建空的
├── ViewModels/
│   ├── MainViewModel.cs
│   └── ButtonViewModel.cs
└── Views/
    ├── MainWindow.axaml(.cs)
    └── Pages/Input/                  8 个 Buttons 页面
```

另改：`Directory.Packages.props`（+2 个包版本）、`hello-avalonia.slnx`（+1 个项目）。

---

### Task 1: 项目脚手架与包声明

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `hello-avalonia.slnx`
- Create: `Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj`
- Create: `Avalonia.ControlsDemo/Program.cs`
- Create: `Avalonia.ControlsDemo/App.axaml`、`App.axaml.cs`
- Copy: `Avalonia.LayoutDemo/app.manifest`、`Avalonia.LayoutDemo/Assets/avalonia-logo.ico`

**Interfaces:**
- Produces: 可构建的空壳项目；`Avalonia.ControlsDemo.App`；`Directory.Packages.props` 里 `Avalonia.Controls.DataGrid` 与 `Avalonia.Controls.ColorPicker`（均 12.1.2）两个版本声明，供第 01、03 份引用

- [ ] **Step 1: 声明两个新增包版本**

在 `Directory.Packages.props` 中 `Avalonia.Fonts.Inter` 那行之后加入：

```xml
        <!--NOTE: DataGrid, ColorPicker and ColorView ship as separate packages in v12 (they are not
            in the main Avalonia assembly). They back the ControlsDemo Input and Data display pages.-->
        <PackageVersion Include="Avalonia.Controls.DataGrid" Version="12.1.2" />
        <PackageVersion Include="Avalonia.Controls.ColorPicker" Version="12.1.2" />
```

- [ ] **Step 2: 复制资源并建 csproj**

```bash
mkdir -p Avalonia.ControlsDemo/Assets
cp Avalonia.LayoutDemo/app.manifest Avalonia.ControlsDemo/app.manifest
cp Avalonia.LayoutDemo/Assets/avalonia-logo.ico Avalonia.ControlsDemo/Assets/avalonia-logo.ico
```

`Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj`（与 `Avalonia.LayoutDemo` 相同，本份不引用两个新包，第 01 / 03 份再加）：

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

- [ ] **Step 3: 写 Program.cs、App.axaml、App.axaml.cs**

`Program.cs`：

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.ControlsDemo
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

`App.axaml`：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.ControlsDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader and the shared page-level styles.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>
</Application>
```

`App.axaml.cs`（`MainWindow` 在 Task 3 才建，本步先让它指向它，Task 3 之前构建会失败，所以 Step 5 的构建放在 Task 3 之后。为让本任务可独立验证，先注释掉窗口创建）：

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Avalonia.ControlsDemo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                // MainWindow is wired in Task 3, once the navigation shell exists.
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

- [ ] **Step 4: 把项目加入解决方案**

在 `hello-avalonia.slnx` 里 `Avalonia.AppDevelopmentDemo` 那行之后插入（按字母序，`ControlsDemo` 排在 `CustomControlsDemo` 之前）：

```xml
  <Project Path="Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj" />
```

放置位置：`Avalonia.AppDevelopmentDemo` 与 `Avalonia.CustomControlsDemo` 两行之间。

- [ ] **Step 5: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|警告|Warn|个错误|Build succeeded"`
Expected: 0 个错误，无新增警告。

- [ ] **Step 6: 提交**

```bash
git add Directory.Packages.props hello-avalonia.slnx Avalonia.ControlsDemo
git commit -m "feat: scaffold the ControlsDemo project

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: 导航数据模型与 PageCatalog

**Files:**
- Create: `Avalonia.ControlsDemo/Navigation/PageKind.cs`、`PageEntry.cs`、`NavNode.cs`、`Categories.cs`、`PageCatalog.cs`
- Create: 12 个空的 `Avalonia.ControlsDemo/Navigation/PageCatalog.<分类>.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（把 `CategoryNode` 改成 `NavNode`）

**Interfaces:**
- Produces（后 8 份计划全部依赖，签名不得改动）：
  - `enum PageKind { Control, HowTo, Signpost }`
  - `sealed record PageEntry(string Title, string Category, string DocPath, Type PageType, PageKind Kind)`，方法 `Control CreatePage()`
  - `sealed partial class NavNode : ObservableObject`：构造 `NavNode(string title, PageEntry? entry = null, IReadOnlyList<NavNode>? children = null)`；属性 `PageEntry? Entry`、`IReadOnlyList<NavNode> Children`、`string DisplayTitle`、`bool IsExpanded`（可观察）
  - `static class Categories`：常量 `DataDisplay`、`Feedback`、`Input`、`Layout`、`Media`、`Menus`、`Navigation`、`Primitives`、`System`、`Web`、`Premium`、`HowToSignposts`
  - `static partial class PageCatalog`：`IReadOnlyList<PageEntry> All`；私有辅助 `ControlPage<TPage>(title, category, docPath)`、`HowToPage<TPage>(...)`、`SignpostPage<TPage>(...)`；每个分类一个 `private static void Add<分类>(List<PageEntry> list)`，**登记顺序即导航树顺序**
- Consumes: 无

**为什么 `Categories` 单独成类**：`PageCatalog` 里若直接放名为 `System`、`Input`、`Layout` 的常量，类内任何 `System.Type` 的写法都会被解析成那个字符串常量而编译失败。放进独立的 `Categories` 类，冲突只限于它自己的几行。

**导航树的显示规则**：`Control` 页显示原标题；`HowTo` 页显示 `实战：<标题>`；`Signpost` 页显示 `↗ <标题>`。因此登记时标题一律写**纯控件名或主题名**（`TreeView`，不是 `TreeView 实战`）。

- [ ] **Step 1: 写 PageKind、PageEntry、NavNode、Categories**

`Navigation/PageKind.cs`：

```csharp
namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>How a page relates to the official documentation, and how it is marked in the tree.</summary>
    public enum PageKind
    {
        /// <summary>A runnable demonstration of one control.</summary>
        Control,

        /// <summary>A hands-on page for one official How-to guide, placed next to its control.</summary>
        HowTo,

        /// <summary>No code: a note pointing at another project or at the official docs.</summary>
        Signpost,
    }
}
```

`Navigation/PageEntry.cs`：

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>Metadata of one demo page. <see cref="PageCatalog"/> is the only place these are created.</summary>
    /// <param name="Title">Plain control or topic name; the tree adds the "实战：" / "↗ " marks.</param>
    /// <param name="Category">One of the <see cref="Categories"/> constants.</param>
    /// <param name="DocPath">Official docs path without domain or leading slash, e.g. controls/input/buttons/button.</param>
    /// <param name="PageType">A <see cref="Control"/> subclass with a public parameterless constructor.</param>
    public sealed record PageEntry(string Title, string Category, string DocPath, Type PageType, PageKind Kind)
    {
        public Control CreatePage() => (Control)Activator.CreateInstance(PageType)!;
    }
}
```

`Navigation/NavNode.cs`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>
    /// One row of the navigation tree: either a category (no <see cref="Entry"/>, has children)
    /// or a page (has an entry, no children). A single type keeps the TreeView template and the
    /// IsExpanded binding uniform across both levels.
    /// </summary>
    public sealed partial class NavNode : ObservableObject
    {
        public NavNode(string title, PageEntry? entry = null, IReadOnlyList<NavNode>? children = null)
        {
            Entry = entry;
            Children = children ?? Array.Empty<NavNode>();
            DisplayTitle = entry?.Kind switch
            {
                PageKind.HowTo => $"实战：{title}",
                PageKind.Signpost => $"↗ {title}",
                _ => title,
            };
        }

        public PageEntry? Entry { get; }

        public IReadOnlyList<NavNode> Children { get; }

        public string DisplayTitle { get; }

        [ObservableProperty]
        private bool _isExpanded;
    }
}
```

`Navigation/Categories.cs`（顺序即官方侧边栏顺序，末尾两个是本项目额外的集中分类）：

```csharp
namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>Category names, in the order of the official Controls sidebar.</summary>
    public static class Categories
    {
        public const string DataDisplay = "Data display";
        public const string Feedback = "Feedback";
        public const string Input = "Input";
        public const string Layout = "Layout";
        public const string Media = "Media";
        public const string Menus = "Menus";
        public const string Navigation = "Navigation";
        public const string Primitives = "Primitives";
        public const string System = "System";
        public const string Web = "Web";

        /// <summary>Pro / Enterprise controls, gathered so nobody opens a page before learning it needs a licence.</summary>
        public const string Premium = "付费控件";

        /// <summary>Cross-cutting How-to guides that belong to no single control.</summary>
        public const string HowToSignposts = "How-to 路标";
    }
}
```

- [ ] **Step 2: 写 PageCatalog 主文件**

`Navigation/PageCatalog.cs`：

```csharp
using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>
    /// The single registry of demo pages. Adding a page is two steps: create the page file, then
    /// add one line to the matching PageCatalog.&lt;Category&gt;.cs. The navigation tree and the
    /// probe's completeness counts are both derived from <see cref="All"/>.
    /// </summary>
    public static partial class PageCatalog
    {
        public static IReadOnlyList<PageEntry> All { get; } = Build();

        // Registration order is the tree order, so these calls follow the official sidebar.
        private static IReadOnlyList<PageEntry> Build()
        {
            var list = new List<PageEntry>();
            AddDataDisplay(list);
            AddFeedback(list);
            AddInput(list);
            AddLayout(list);
            AddMedia(list);
            AddMenus(list);
            AddNavigation(list);
            AddPrimitives(list);
            AddSystem(list);
            AddWeb(list);
            AddPremium(list);
            AddHowToSignposts(list);
            return list;
        }

        private static PageEntry ControlPage<TPage>(string title, string category, string docPath)
            where TPage : Control
            => new(title, category, docPath, typeof(TPage), PageKind.Control);

        private static PageEntry HowToPage<TPage>(string title, string category, string docPath)
            where TPage : Control
            => new(title, category, docPath, typeof(TPage), PageKind.HowTo);

        private static PageEntry SignpostPage<TPage>(string title, string category, string docPath)
            where TPage : Control
            => new(title, category, docPath, typeof(TPage), PageKind.Signpost);
    }
}
```

- [ ] **Step 3: 建 12 个空的分类 partial**

每个文件结构相同，只有分类名不同。用脚本一次生成，避免手写出错：

```bash
cd Avalonia.ControlsDemo/Navigation
for c in DataDisplay Feedback Input Layout Media Menus Navigation Primitives System Web Premium HowToSignposts; do
cat > PageCatalog.$c.cs <<EOF
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void Add$c(List<PageEntry> list)
        {
        }
    }
}
EOF
done
ls
```

Expected: 5 个基础文件加 12 个 `PageCatalog.*.cs`，共 17 个。

- [ ] **Step 4: 把 spec 里的 CategoryNode 改成 NavNode**

`docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md` 的项目骨架里，目录树那行：

```
│   ├── CategoryNode.cs         导航树的分类节点（含子 PageEntry）
```

改为：

```
│   ├── NavNode.cs              导航树的一行：分类（有子节点）或页面（有 PageEntry）
```

并把"导航机制"一段里 `PageCatalog` 之后补一句"`PageCatalog` 按分类拆成 12 个 partial 文件（`PageCatalog.Input.cs` 等），各份实现计划只改自己分类的文件"。

- [ ] **Step 5: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|警告|个错误|Build succeeded"`
Expected: 0 个错误。

- [ ] **Step 6: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: add the page registry that drives the ControlsDemo navigation

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: 导航壳（MainViewModel 与 MainWindow）

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/MainViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/MainWindow.axaml`、`MainWindow.axaml.cs`
- Modify: `Avalonia.ControlsDemo/App.axaml.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（去掉"搜索关键字"）

**Interfaces:**
- Consumes: Task 2 的 `PageCatalog.All`、`NavNode`、`PageEntry.CreatePage()`
- Produces: `MainViewModel`：`IReadOnlyList<NavNode> Nodes`、`NavNode? SelectedNode`（可观察，双向）、`Control? CurrentPage`（可观察，随 `SelectedNode` 变化）；`MainWindow`（无参构造，自己创建 `MainViewModel`）

**设计取舍**：不做搜索框。spec 的骨架里提过 `MainViewModel` 含搜索关键字，但 80 个页面有分类树就够找，搜索是投机性功能，这里去掉并同步改 spec。

- [ ] **Step 1: 写 MainViewModel**

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    public sealed partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Nodes = BuildTree(PageCatalog.All);

            // Open on the first page so the window is never an empty shell.
            var firstCategory = Nodes.FirstOrDefault(n => n.Children.Count > 0);
            if (firstCategory is not null)
            {
                firstCategory.IsExpanded = true;
                SelectedNode = firstCategory.Children[0];
            }
        }

        /// <summary>Category nodes, each holding its page nodes.</summary>
        public IReadOnlyList<NavNode> Nodes { get; }

        [ObservableProperty]
        private NavNode? _selectedNode;

        /// <summary>
        /// The page shown on the right. A fresh instance per selection, so every visit starts from
        /// the page's initial state. Null while a category row is selected.
        /// </summary>
        [ObservableProperty]
        private Control? _currentPage;

        partial void OnSelectedNodeChanged(NavNode? value) => CurrentPage = value?.Entry?.CreatePage();

        // GroupBy keeps first-appearance order, and PageCatalog registers categories in sidebar order.
        private static IReadOnlyList<NavNode> BuildTree(IReadOnlyList<PageEntry> entries)
            => entries
                .GroupBy(e => e.Category)
                .Select(g => new NavNode(
                    g.Key,
                    children: g.Select(e => new NavNode(e.Title, e)).ToList()))
                .ToList();
    }
}
```

- [ ] **Step 2: 写 MainWindow.axaml**

`SplitView` 的 `Pane` 放 `TreeView`，`Content` 放页面。`TreeViewItem.IsExpanded` 用 Style 里的 Setter 绑到 `NavNode.IsExpanded`，**不在元素上写本地值**（规则 1）。Style 里的绑定要写 `x:DataType`，因为编译绑定在 Style 中拿不到模板的数据类型。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:nav="using:Avalonia.ControlsDemo.Navigation"
        xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
        mc:Ignorable="d"
        d:DesignWidth="1100"
        d:DesignHeight="700"
        x:Class="Avalonia.ControlsDemo.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Controls Demo"
        Width="1100"
        Height="700"
        WindowStartupLocation="CenterScreen">

    <!--  Left: the official Controls sidebar, category by category. Right: the selected page.  -->
    <SplitView DisplayMode="Inline"
               IsPaneOpen="True"
               OpenPaneLength="260">
        <SplitView.Pane>
            <TreeView Name="NavTree"
                      ItemsSource="{Binding Nodes}"
                      SelectedItem="{Binding SelectedNode, Mode=TwoWay}">
                <TreeView.Styles>
                    <!--  Expansion lives on the node so the view model can open the first category.  -->
                    <Style Selector="TreeViewItem" x:DataType="nav:NavNode">
                        <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
                    </Style>
                </TreeView.Styles>
                <TreeView.DataTemplates>
                    <TreeDataTemplate DataType="nav:NavNode" ItemsSource="{Binding Children}">
                        <TextBlock Text="{Binding DisplayTitle}" />
                    </TreeDataTemplate>
                </TreeView.DataTemplates>
            </TreeView>
        </SplitView.Pane>

        <Grid>
            <ContentControl Name="PageHost" Content="{Binding CurrentPage}" />
            <TextBlock Classes="hint"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center"
                       IsVisible="{Binding CurrentPage, Converter={x:Static ObjectConverters.IsNull}}"
                       Text="请在左侧选择一个控件" />
        </Grid>
    </SplitView>
</Window>
```

- [ ] **Step 3: 写 MainWindow.axaml.cs，接入 App**

`Views/MainWindow.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
```

`App.axaml.cs` 把窗口创建恢复：

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ControlsDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.ControlsDemo
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
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

- [ ] **Step 4: 同步 spec，去掉搜索**

`docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md` 里目录树的这一行：

```
│   ├── MainViewModel.cs        导航树、当前选中页、搜索关键字
```

改为：

```
│   ├── MainViewModel.cs        导航树、选中节点、当前页
```

- [ ] **Step 5: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|警告|个错误|Build succeeded"`
Expected: 0 个错误。若报 `AVLN2100`（Style 内绑定缺 `x:DataType`），确认 `<Style>` 上写了 `x:DataType="nav:NavNode"`。

- [ ] **Step 6: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: add the ControlsDemo navigation shell

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### 本系列各现有项目的实际 Tab 名（用于「更深入的演示」指向，已对照各 `MainWindow.axaml` 核实）

| 项目 | Tab |
|---|---|
| LayoutDemo | 布局面板 / 定位与间距 / 响应式布局 |
| StylingDemo | 选择器 / 样式类 / 伪类 / ControlTheme / 主题变体 / 容器查询 / 字体 / 样式共享 / 优先级 |
| FundamentalsDemo | 纯代码 UI / Code-behind / MVVM / TopLevel / 视觉树与逻辑树 / 生命周期与资源 |
| InputDemo | 指针 / 焦点 / 手势 / 键盘与 HotKey / 交互的写法 / 拖放 / 文本输入 / 路由事件 |
| ServicesDemo | 剪贴板 / 文件对话框 / StorageProvider / 焦点管理 / Launcher / 平台设置 / 移动端服务 / 可激活生命周期 |
| AppDevelopmentDemo | 依赖注入 / 本地化 / Web 内容 / 日志 / 未处理异常 / 资源 / 数据校验 / 线程模型 / 窗口管理 / 无障碍 |
| EventsDemo | 生命周期 / 输入事件 / 路由三阶段 / Handled / 自定义路由事件 |
| DataBindingDemo | 绑定语法 / 编译绑定 / 集合 / 主从视图 / 多值绑定 / 命令 / 转换器 / 校验 / 集合视图 / 异步与图片 / 绑定调试 / 标记扩展 |
| CustomControlsDemo | 用户控件 / 模板化控件 / 自绘控件 / 属性与事件 / 控件树 / 自定义面板 / 自定义 Flyout |
| GraphicsDemo | 画刷与渐变 / 变换 / 形状与几何 / 自定义绘制 / 特效 / 裁剪与命中 / 图标 / 渲染选项 / 关键帧动画 / 控件过渡 / 页面过渡 / 缓动函数 / 合成动画 |
| DataTemplatesDemo | 控件内容 / 内联模板 / 模板集合 / 选择器 / 代码建模板 / 复用 / ViewLocator / 面板与树 / 对比 ControlTemplate |
| PropertySystemDemo | StyledProperty / DirectProperty / 附加属性 / 值优先级 / 元数据与回调 |
| XamlDemo | 命名空间 / x: 指令 / 标记扩展 / 类型转换器 / 泛型 / XAML 编译 / 平台相关 XAML |
| WebViewDemo | 独立窗口应用，无 Tab |

---

## Buttons 页面的共同写法

8 个页面都按同一骨架写，差别只在中间的演示区。骨架（以 `ButtonPage` 为例，后面各页只给出**变化的部分**，骨架不重复贴）：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="800"
             d:DesignHeight="500"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ButtonPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Button：最基本的可点击控件"
                               DocPath="controls/input/buttons/button" />
            <!-- demo blocks: TextBlock.caption + Border.stage + TextBlock.hint -->
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ButtonPage : UserControl
    {
        public ButtonPage()
        {
            InitializeComponent();
        }
    }
}
```

**无需 `x:DataType` 的页面**：用 code-behind 与命名元素的页面不设 `DataContext`，也就不写 `x:DataType`（编译绑定只在有绑定表达式时才要求）。页面里的"读数"用元素名绑定（规则 4）：`{Binding #CountBlock.Text}`，或直接在事件处理器里改 `TextBlock.Text`。

**页面里的小节**：每个演示块是 `TextBlock Classes="caption"`（小节标题）+ `Border Classes="stage" Padding="12"`（演示区）+ 可选 `TextBlock Classes="hint"`（说明）。这三个样式来自 `Avalonia.Shared/Themes/SharedStyles.axaml`，不用自己写。

---

### Task 4: Button、ToggleButton、RepeatButton、HyperlinkButton

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/ButtonPage.axaml`、`.axaml.cs`
- Create: `.../ToggleButtonPage.axaml`、`.axaml.cs`
- Create: `.../RepeatButtonPage.axaml`、`.axaml.cs`
- Create: `.../HyperlinkButtonPage.axaml`、`.axaml.cs`
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Input.cs`

**Interfaces:**
- Consumes: Task 2 的 `ControlPage<TPage>`、`Categories.Input`
- Produces: 4 个页面类，命名空间 `Avalonia.ControlsDemo.Views.Pages`，无参构造；页面内可断言的元素名见各页

每个页面用上面的骨架，`x:Class` 与类名换成各自的名字，`DemoHeader` 换成下面的标题与路径。

- [ ] **Step 1: ButtonPage**

`Title="Button：最基本的可点击控件"`，`DocPath="controls/input/buttons/button"`。三个演示块：

1. **Click 事件**：一个按钮，`Click` 处理器累加计数。`Command` 绑定需要 ViewModel，本页刻意不引入（保持无 `DataContext`），命令的完整演示在 `Avalonia.DataBindingDemo`。
2. **ClickMode**：`ClickMode="Release"`（默认，松开才触发）与 `ClickMode="Press"`（按下即触发）各一个按钮，都更新各自的计数。
3. **IsDefault / IsCancel**：只写说明文字，不做交互（它们只在 `Window` 里生效，页面里演示不了）。

```xml
<TextBlock Classes="caption" Text="1. Click 事件" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <Button Name="ClickButton" Content="点我" Click="OnClickButtonClick" />
        <TextBlock Name="ClickCount" VerticalAlignment="Center" Text="点击次数：0" />
    </StackPanel>
</Border>

<TextBlock Classes="caption" Text="2. ClickMode：Release 与 Press" />
<Border Classes="stage" Padding="12">
    <StackPanel Spacing="8">
        <StackPanel Orientation="Horizontal" Spacing="12">
            <Button Name="ReleaseButton" ClickMode="Release" Content="Release（松开触发）" Click="OnReleaseClick" />
            <TextBlock Name="ReleaseCount" VerticalAlignment="Center" Text="0" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" Spacing="12">
            <Button Name="PressButton" ClickMode="Press" Content="Press（按下触发）" Click="OnPressClick" />
            <TextBlock Name="PressCount" VerticalAlignment="Center" Text="0" />
        </StackPanel>
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="IsDefault 与 IsCancel 只在 Window 中生效：按回车等于点击 IsDefault 的按钮，按 Esc 等于点击 IsCancel 的按钮。" />
<TextBlock Classes="hint" Text="更深入的演示：Avalonia.InputDemo →「交互的写法」" />
```

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ButtonPage : UserControl
    {
        private int _clicks, _releases, _presses;

        public ButtonPage()
        {
            InitializeComponent();
        }

        private void OnClickButtonClick(object? sender, RoutedEventArgs e) => ClickCount.Text = $"点击次数：{++_clicks}";

        private void OnReleaseClick(object? sender, RoutedEventArgs e) => ReleaseCount.Text = (++_releases).ToString();

        private void OnPressClick(object? sender, RoutedEventArgs e) => PressCount.Text = (++_presses).ToString();
    }
}
```

- [ ] **Step 2: ToggleButtonPage**

`Title="ToggleButton：有选中状态的按钮"`，`DocPath="controls/input/buttons/togglebutton"`。两个演示块：双态与 `IsThreeState`。

```xml
<TextBlock Classes="caption" Text="1. 双态：IsChecked 在 True 与 False 之间切换" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <ToggleButton Name="TwoState" Content="开关" />
        <TextBlock VerticalAlignment="Center"
                   Text="{Binding #TwoState.IsChecked, StringFormat='IsChecked = {0}'}" />
    </StackPanel>
</Border>

<TextBlock Classes="caption" Text="2. 三态：IsThreeState 多出 null（不确定）" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <ToggleButton Name="ThreeState" IsThreeState="True" IsChecked="{x:Null}" Content="三态" />
        <TextBlock VerticalAlignment="Center"
                   Text="{Binding #ThreeState.IsChecked, StringFormat='IsChecked = {0}', FallbackValue='IsChecked = null'}" />
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="更深入的演示：Avalonia.StylingDemo →「伪类」（:checked 与 :indeterminate）" />
```

注意：`StringFormat='IsChecked = {0}'` 以字面文字开头，不会触发规则 2 的 `AVLN2000`。`IsChecked="{x:Null}"` 是显式的"不确定"初值，**写在元素上不违反规则 1**，因为本页没有任何样式去驱动 `IsChecked`。`FallbackValue` 兜住 `null` 时格式化结果为空的情况。

code-behind 只有 `InitializeComponent()`，与骨架相同。

- [ ] **Step 3: RepeatButtonPage**

`Title="RepeatButton：按住不放持续触发"`，`DocPath="controls/input/buttons/repeatbutton"`。一个演示块：`Delay` 与 `Interval` 两个滑块实时调节，按住按钮时计数持续增加。

```xml
<TextBlock Classes="caption" Text="按住按钮：先等待 Delay，再每隔 Interval 触发一次 Click" />
<Border Classes="stage" Padding="12">
    <StackPanel Spacing="10">
        <StackPanel Orientation="Horizontal" Spacing="12">
            <RepeatButton Name="Repeater"
                          Delay="{Binding #DelaySlider.Value, Converter={x:Static shared:NumberConverters.DoubleToInt}}"
                          Interval="{Binding #IntervalSlider.Value, Converter={x:Static shared:NumberConverters.DoubleToInt}}"
                          Content="按住我"
                          Click="OnRepeaterClick" />
            <TextBlock Name="RepeatCount" VerticalAlignment="Center" Text="触发次数：0" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock Width="120" VerticalAlignment="Center" Text="Delay（毫秒）" />
            <Slider Name="DelaySlider" Width="240" Minimum="0" Maximum="1000" Value="300" />
            <TextBlock VerticalAlignment="Center" Text="{Binding #DelaySlider.Value, StringFormat='{}{0:F0}'}" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock Width="120" VerticalAlignment="Center" Text="Interval（毫秒）" />
            <Slider Name="IntervalSlider" Width="240" Minimum="20" Maximum="500" Value="100" />
            <TextBlock VerticalAlignment="Center" Text="{Binding #IntervalSlider.Value, StringFormat='{}{0:F0}'}" />
        </StackPanel>
    </StackPanel>
</Border>
```

**`Delay`、`Interval` 是 `int`，`Slider.Value` 是 `double`**：绑定需要转换器，否则静默失败（与规则 3 同类）。`Avalonia.Shared` 目前没有 `double → int` 转换器，所以本步在共享库里新增一个，后面 Input 的 `NumericUpDown` 页也会用。

新建 `Avalonia.Shared/Converters/NumberConverters.cs`：

```csharp
using Avalonia.Data.Converters;

namespace Avalonia.Shared.Converters
{
    /// <summary>Numeric conversions the demo pages need to wire a double-valued control to an int property.</summary>
    public static class NumberConverters
    {
        /// <summary>double → int by truncation; anything that is not a double yields 0.</summary>
        public static readonly IValueConverter DoubleToInt = new FuncValueConverter<double, int>(v => (int)v);
    }
}
```

页面里的 XAML 命名空间前缀要用转换器所在的命名空间，所以 `RepeatButtonPage.axaml` 额外声明 `xmlns:conv="using:Avalonia.Shared.Converters"`，并把上面两处 `shared:NumberConverters.DoubleToInt` 改成 `conv:NumberConverters.DoubleToInt`。

> 说明：`FuncValueConverter<TIn, TOut>` 是 Avalonia 自带的类型，同时实现 `IValueConverter`。`ConvertBack` 不支持，这里是单向绑定，不需要。

code-behind：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RepeatButtonPage : UserControl
    {
        private int _count;

        public RepeatButtonPage()
        {
            InitializeComponent();
        }

        private void OnRepeaterClick(object? sender, RoutedEventArgs e) => RepeatCount.Text = $"触发次数：{++_count}";
    }
}
```

- [ ] **Step 4: HyperlinkButtonPage**

`Title="HyperlinkButton：看起来像链接的按钮"`，`DocPath="controls/input/buttons/hyperlinkbutton"`。两个演示块：带 `NavigateUri` 的链接按钮（点击由系统浏览器打开）、不带 `NavigateUri` 只响应 `Click` 的链接按钮。

```xml
<TextBlock Classes="caption" Text="1. NavigateUri：点击后交给系统浏览器打开" />
<Border Classes="stage" Padding="12">
    <HyperlinkButton Name="DocsLink"
                     Content="Avalonia 官方文档"
                     NavigateUri="https://docs.avaloniaui.net" />
</Border>

<TextBlock Classes="caption" Text="2. 没有 NavigateUri：只当作外观是链接的普通按钮" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <HyperlinkButton Name="PlainLink" Content="点击计数" Click="OnPlainLinkClick" />
        <TextBlock Name="PlainCount" VerticalAlignment="Center" Text="0" />
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="真实打开链接依赖平台的 Launcher；更深入的演示：Avalonia.ServicesDemo →「Launcher」" />
```

code-behind：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class HyperlinkButtonPage : UserControl
    {
        private int _count;

        public HyperlinkButtonPage()
        {
            InitializeComponent();
        }

        private void OnPlainLinkClick(object? sender, RoutedEventArgs e) => PlainCount.Text = (++_count).ToString();
    }
}
```

- [ ] **Step 5: 登记四个页面**

`Navigation/PageCatalog.Input.cs` 的 `AddInput` 里写入（顺序为官方侧边栏的 Buttons 顺序，本份只登记自己建的页面，**Buttons 的其余 4 个在 Task 5 补，位置按官方顺序穿插**，所以这里先用占位的最终顺序注释说明）：

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddInput(List<PageEntry> list)
        {
            const string c = Categories.Input;

            // Buttons, in the order of the official sidebar. Task 5 inserts the other four.
            list.Add(ControlPage<ButtonPage>("Button", c, "controls/input/buttons/button"));
            list.Add(ControlPage<HyperlinkButtonPage>("HyperlinkButton", c, "controls/input/buttons/hyperlinkbutton"));
            list.Add(ControlPage<RepeatButtonPage>("RepeatButton", c, "controls/input/buttons/repeatbutton"));
            list.Add(ControlPage<ToggleButtonPage>("ToggleButton", c, "controls/input/buttons/togglebutton"));
        }
    }
}
```

- [ ] **Step 6: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|警告|个错误|Build succeeded"`
Expected: 0 个错误。若报 `AVLN2000` 且指向 `StringFormat`，对照规则 2。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo Avalonia.Shared
git commit -m "feat: demonstrate Button, ToggleButton, RepeatButton and HyperlinkButton

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: ButtonSpinner、RadioButton、SplitButton、ToggleSplitButton

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/ButtonSpinnerPage.axaml`、`.axaml.cs`
- Create: `.../RadioButtonPage.axaml`、`.axaml.cs`
- Create: `.../SplitButtonPage.axaml`、`.axaml.cs`
- Create: `.../ToggleSplitButtonPage.axaml`、`.axaml.cs`
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Input.cs`

**Interfaces:**
- Consumes: Task 4 的登记方式；`Avalonia.Shared.Converters.NumberConverters`（本任务不用）
- Produces: 4 个页面类；`PageCatalog.Input` 里 Buttons 子类的 8 行登记完整，顺序为官方侧边栏顺序

骨架同 Task 4。

- [ ] **Step 1: ButtonSpinnerPage**

`Title="ButtonSpinner：给任意内容加上下微调按钮"`，`DocPath="controls/input/buttons/buttonspinner"`。`ButtonSpinner` 本身不持有数值，它只在点击微调按钮时触发 `Spin` 事件，由使用者决定如何响应。三个演示块：

1. 用 `Spin` 事件调整一个计数（`SpinDirection.Increase` 加、`Decrease` 减）。
2. `ButtonSpinnerLocation`：`Left` 与 `Right` 对比。
3. `AllowSpin` 与 `ShowButtonSpinner`：关闭微调、隐藏按钮。

```xml
<TextBlock Classes="caption" Text="1. Spin 事件：控件只报告方向，数值由你维护" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <ButtonSpinner Name="CounterSpinner" Width="140" Spin="OnCounterSpin">
            <TextBlock Name="CounterText" HorizontalAlignment="Center" VerticalAlignment="Center" Text="0" />
        </ButtonSpinner>
        <TextBlock Name="SpinLog" VerticalAlignment="Center" Text="尚未微调" />
    </StackPanel>
</Border>

<TextBlock Classes="caption" Text="2. ButtonSpinnerLocation：按钮在左侧还是右侧" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <ButtonSpinner Name="RightSpinner" ButtonSpinnerLocation="Right" Width="140">
            <TextBlock HorizontalAlignment="Center" Text="Right（默认）" />
        </ButtonSpinner>
        <ButtonSpinner Name="LeftSpinner" ButtonSpinnerLocation="Left" Width="140">
            <TextBlock HorizontalAlignment="Center" Text="Left" />
        </ButtonSpinner>
    </StackPanel>
</Border>

<TextBlock Classes="caption" Text="3. AllowSpin 与 ShowButtonSpinner" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <ButtonSpinner Name="NoSpinSpinner" AllowSpin="False" Width="140">
            <TextBlock HorizontalAlignment="Center" Text="AllowSpin=False" />
        </ButtonSpinner>
        <ButtonSpinner Name="HiddenSpinner" ShowButtonSpinner="False" Width="140">
            <TextBlock HorizontalAlignment="Center" Text="按钮已隐藏" />
        </ButtonSpinner>
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="带数值的微调框是 NumericUpDown，它内部就是 ButtonSpinner 加 TextBox。" />
```

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ButtonSpinnerPage : UserControl
    {
        private int _value;

        public ButtonSpinnerPage()
        {
            InitializeComponent();
        }

        // ButtonSpinner only reports a direction; keeping the number is the caller's job.
        private void OnCounterSpin(object? sender, SpinEventArgs e)
        {
            _value += e.Direction == SpinDirection.Increase ? 1 : -1;
            CounterText.Text = _value.ToString();
            SpinLog.Text = $"最近一次：{e.Direction}";
        }
    }
}
```

- [ ] **Step 2: RadioButtonPage**

`Title="RadioButton：同组互斥的选项"`，`DocPath="controls/input/buttons/radiobutton"`。两个演示块：`GroupName` 分组、不分组时按父容器自动成组。

按**规则 12**，`IsCheckedChanged` 里要用 `sender` 判断是谁被选中，不要读兄弟按钮。

```xml
<TextBlock Classes="caption" Text="1. GroupName：同名的按钮互斥" />
<Border Classes="stage" Padding="12">
    <StackPanel Spacing="6">
        <!--  The result label comes first on purpose: XAML creates elements in order, and the
              IsChecked="True" below raises IsCheckedChanged while the page is still loading.
              A label declared after the buttons would not exist yet.  -->
        <TextBlock Name="SizeResult" Classes="hint" Text="当前选择：中" />
        <RadioButton Name="SizeSmall" GroupName="Size" Content="小" IsCheckedChanged="OnSizeChanged" />
        <RadioButton Name="SizeMedium" GroupName="Size" Content="中" IsChecked="True" IsCheckedChanged="OnSizeChanged" />
        <RadioButton Name="SizeLarge" GroupName="Size" Content="大" IsCheckedChanged="OnSizeChanged" />
    </StackPanel>
</Border>

<TextBlock Classes="caption" Text="2. 不写 GroupName：同一个父容器里的按钮自动成组" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="24">
        <StackPanel Name="GroupA" Spacing="6">
            <RadioButton Name="A1" Content="A1" IsChecked="True" />
            <RadioButton Name="A2" Content="A2" />
        </StackPanel>
        <StackPanel Name="GroupB" Spacing="6">
            <RadioButton Name="B1" Content="B1" IsChecked="True" />
            <RadioButton Name="B2" Content="B2" />
        </StackPanel>
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="两个 StackPanel 里各有一个被选中，互不影响。" />
<TextBlock Classes="hint" Text="更深入的演示：Avalonia.AppDevelopmentDemo →「本地化」（语言切换用到了 RadioButton）" />
```

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RadioButtonPage : UserControl
    {
        public RadioButtonPage()
        {
            InitializeComponent();
        }

        // IsCheckedChanged fires for the button being unchecked too, and at that moment its
        // sibling is not yet checked, so act only on the sender that became checked.
        private void OnSizeChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { IsChecked: true } button)
            {
                SizeResult.Text = $"当前选择：{button.Content}";
            }
        }
    }
}
```

- [ ] **Step 3: SplitButtonPage**

`Title="SplitButton：主操作加下拉菜单"`，`DocPath="controls/input/buttons/splitbutton"`。左半是主操作（触发 `Click`），右半的箭头打开 `Flyout`。两个演示块：`Flyout` 里放 `MenuFlyout`、`Command` 属性。

`SplitButton.Flyout` 是 `FlyoutBase`，用 `MenuFlyout` 即可，其项在 code-behind 里不需要访问，所以不命名（规则 11）。

```xml
<TextBlock Classes="caption" Text="1. 主按钮触发 Click，箭头弹出 Flyout" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <SplitButton Name="SaveSplit" Content="保存" Click="OnSaveClick">
            <SplitButton.Flyout>
                <MenuFlyout>
                    <MenuItem Header="另存为…" Click="OnSaveAsClick" />
                    <MenuItem Header="保存全部" Click="OnSaveAllClick" />
                </MenuFlyout>
            </SplitButton.Flyout>
        </SplitButton>
        <TextBlock Name="SplitResult" VerticalAlignment="Center" Text="尚未操作" />
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="主按钮代表最常用的操作，下拉里放同类的其它操作。" />
```

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SplitButtonPage : UserControl
    {
        public SplitButtonPage()
        {
            InitializeComponent();
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e) => SplitResult.Text = "执行：保存";

        private void OnSaveAsClick(object? sender, RoutedEventArgs e) => SplitResult.Text = "执行：另存为";

        private void OnSaveAllClick(object? sender, RoutedEventArgs e) => SplitResult.Text = "执行：保存全部";
    }
}
```

同样的构造期顺序问题不存在：这些处理器只在用户操作时触发，`SplitResult` 早已创建。

- [ ] **Step 4: ToggleSplitButtonPage**

`Title="ToggleSplitButton：带选中状态的 SplitButton"`，`DocPath="controls/input/buttons/togglesplitbutton"`。继承 `SplitButton`，多了 `IsChecked`。一个演示块：主按钮切换选中，箭头弹出 Flyout，读数显示 `IsChecked`。

```xml
<TextBlock Classes="caption" Text="主按钮切换 IsChecked，箭头弹出 Flyout" />
<Border Classes="stage" Padding="12">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <ToggleSplitButton Name="BoldSplit" Content="粗体">
            <ToggleSplitButton.Flyout>
                <MenuFlyout>
                    <MenuItem Header="加粗并加下划线" />
                    <MenuItem Header="清除格式" />
                </MenuFlyout>
            </ToggleSplitButton.Flyout>
        </ToggleSplitButton>
        <TextBlock VerticalAlignment="Center"
                   Text="{Binding #BoldSplit.IsChecked, StringFormat='IsChecked = {0}'}" />
    </StackPanel>
</Border>
<TextBlock Classes="hint" Text="适合格式工具栏：主按钮是开关，下拉里放它的扩展选项。" />
```

code-behind 只有 `InitializeComponent()`。

- [ ] **Step 5: 补全 Buttons 的 8 行登记**

`PageCatalog.Input.cs` 的 Buttons 部分改为官方顺序（Button、ButtonSpinner、HyperlinkButton、RadioButton、RepeatButton、SplitButton、ToggleButton、ToggleSplitButton），并删掉 Task 4 写的"Task 5 inserts the other four"注释：

```csharp
            // Buttons, in the order of the official sidebar.
            list.Add(ControlPage<ButtonPage>("Button", c, "controls/input/buttons/button"));
            list.Add(ControlPage<ButtonSpinnerPage>("ButtonSpinner", c, "controls/input/buttons/buttonspinner"));
            list.Add(ControlPage<HyperlinkButtonPage>("HyperlinkButton", c, "controls/input/buttons/hyperlinkbutton"));
            list.Add(ControlPage<RadioButtonPage>("RadioButton", c, "controls/input/buttons/radiobutton"));
            list.Add(ControlPage<RepeatButtonPage>("RepeatButton", c, "controls/input/buttons/repeatbutton"));
            list.Add(ControlPage<SplitButtonPage>("SplitButton", c, "controls/input/buttons/splitbutton"));
            list.Add(ControlPage<ToggleButtonPage>("ToggleButton", c, "controls/input/buttons/togglebutton"));
            list.Add(ControlPage<ToggleSplitButtonPage>("ToggleSplitButton", c, "controls/input/buttons/togglesplitbutton"));
```

- [ ] **Step 6: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|警告|个错误|Build succeeded"`
Expected: 0 个错误。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate ButtonSpinner, RadioButton, SplitButton and ToggleSplitButton

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 6: 探针骨架与样板验证

**Files:**（全部在仓库外，不提交）
- Create: `C:\Temp\probe-controls\probe-controls.csproj`
- Create: `C:\Temp\probe-controls\Harness.cs`（`CaptureSink`、`Check`、`Pump`、`Find`、`Show`）
- Create: `C:\Temp\probe-controls\Probe.Shell.cs`（导航壳与目录完整性）
- Create: `C:\Temp\probe-controls\Probe.Buttons.cs`（8 个 Buttons 页）
- Create: `C:\Temp\probe-controls\Program.cs`（入口，后面各份往里加调用）

**Interfaces:**
- Consumes: `PageCatalog.All`、`MainWindow`、`MainViewModel`、各页面类
- Produces: 探针骨架 `Harness`（静态类），后 8 份计划**原样复用**：
  - `Harness.Check(string name, bool ok, object? detail = null)`
  - `Harness.Pump()`：反复 `Dispatcher.UIThread.RunJobs()` 共 5 次
  - `Harness.Find<T>(Visual root, string name)`：按 `Name` 找后代，找不到抛异常
  - `Harness.Show(Control page)` → `Window`：把页面放进 800×600 窗口，`Show()`、`UpdateLayout()`、`Pump()`
  - `Harness.Sink`：`CaptureSink`，`Harness.Sink.Hits` 是 ≥Warning 的日志条目
  - `Harness.Exit()`：打印通过/失败条数，返回进程退出码

- [ ] **Step 1: 建探针 csproj**

```bash
mkdir -p /c/Temp/probe-controls
```

`C:\Temp\probe-controls\probe-controls.csproj`（`ProjectReference` 路径按实际仓库位置写；仓库在 `E:\ProjectxPlex\WPFCodePlex\hello-avalonia`）：

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
    <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.ControlsDemo\Avalonia.ControlsDemo.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 写 Harness.cs**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

// Remembers every warning-or-worse message, so "no binding warnings" is asserted, not eyeballed.
class CaptureSink : ILogSink
{
    public List<string> Hits { get; } = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Hits.Add($"{level}/{area}: {messageTemplate}");
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) => Hits.Add($"{level}/{area}: {messageTemplate}");
}

static class Harness
{
    static int _pass, _fail;

    public static CaptureSink Sink { get; } = new();

    public static void Check(string name, bool ok, object? detail = null)
    {
        if (ok) { _pass++; Console.WriteLine($"PASS  {name}"); }
        else { _fail++; Console.WriteLine($"FAIL  {name}  [{detail}]"); }
    }

    public static void Pump()
    {
        for (int i = 0; i < 5; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(5); }
    }

    public static T Find<T>(Visual root, string name) where T : Control
        => root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    public static Window Show(Control page)
    {
        var window = new Window { Width = 800, Height = 600, Content = page };
        window.Show();
        window.UpdateLayout();
        Pump();
        return window;
    }

    public static void Setup()
    {
        AppBuilder.Configure<Avalonia.ControlsDemo.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
        Logger.Sink = Sink;
    }

    public static int Exit()
    {
        Console.WriteLine($"\n{_pass} passed, {_fail} failed, {Sink.Hits.Count} warning log(s)");
        foreach (var h in Sink.Hits) Console.WriteLine("  LOG " + h);
        return _fail == 0 && Sink.Hits.Count == 0 ? 0 : 1;
    }
}
```

- [ ] **Step 3: 写 Probe.Shell.cs 与 Program.cs，先让探针在空目录上跑通**

`Probe.Shell.cs` 断言导航壳：窗口能创建、树的第一个分类已展开、选中第一个页面后 `CurrentPage` 非空且是 `ButtonPage`、选中分类节点时页面清空、目录完整性（本份只有 8 个 Control 页，且无重复类型、无重复 DocPath）。

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views;
using Avalonia.ControlsDemo.Views.Pages;
using System.Linq;

static class ProbeShell
{
    public static void Run()
    {
        var all = PageCatalog.All;

        // ---- catalog integrity
        Harness.Check("catalog: every PageType is unique", all.Select(e => e.PageType).Distinct().Count() == all.Count);
        Harness.Check("catalog: every DocPath is unique", all.Select(e => e.DocPath).Distinct().Count() == all.Count);
        Harness.Check("catalog: no DocPath has a leading slash or domain",
            all.All(e => !e.DocPath.StartsWith('/') && !e.DocPath.Contains("://")));
        Harness.Check("catalog: every PageType is a Control with a parameterless constructor",
            all.All(e => typeof(Control).IsAssignableFrom(e.PageType) && e.PageType.GetConstructor(System.Type.EmptyTypes) is not null));

        // ---- shell
        var window = new MainWindow();
        window.Show();
        window.UpdateLayout();
        Harness.Pump();
        var vm = (MainViewModel)window.DataContext!;

        Harness.Check("shell: tree has one node per category", vm.Nodes.Count == all.Select(e => e.Category).Distinct().Count(), vm.Nodes.Count);
        Harness.Check("shell: first category is expanded", vm.Nodes[0].IsExpanded);
        Harness.Check("shell: first page is selected on start", vm.SelectedNode?.Entry is not null);
        Harness.Check("shell: CurrentPage is the first registered page", vm.CurrentPage?.GetType() == all[0].PageType, vm.CurrentPage?.GetType().Name);

        var host = Harness.Find<ContentControl>(window, "PageHost");
        Harness.Check("shell: PageHost shows CurrentPage", ReferenceEquals(host.Content, vm.CurrentPage));
        Harness.Check("shell: the first node is a category, not a page", vm.Nodes[0].Entry is null && vm.Nodes[0].Children.Count > 0);

        var first = vm.CurrentPage;
        vm.SelectedNode = vm.Nodes[0].Children[1];
        Harness.Pump();
        Harness.Check("shell: selecting another page swaps CurrentPage", !ReferenceEquals(vm.CurrentPage, first));

        vm.SelectedNode = vm.Nodes[0].Children[0];
        Harness.Pump();
        Harness.Check("shell: re-visiting a page builds a fresh instance", !ReferenceEquals(vm.CurrentPage, first));

        vm.SelectedNode = vm.Nodes[0];
        Harness.Pump();
        Harness.Check("shell: selecting a category clears the page", vm.CurrentPage is null);

        // ---- display titles
        var howTo = new NavNode("X", new PageEntry("X", "c", "p", typeof(ButtonPage), PageKind.HowTo));
        var sign = new NavNode("X", new PageEntry("X", "c", "p", typeof(ButtonPage), PageKind.Signpost));
        Harness.Check("nav: HowTo title is prefixed", howTo.DisplayTitle == "实战：X", howTo.DisplayTitle);
        Harness.Check("nav: Signpost title is prefixed", sign.DisplayTitle == "↗ X", sign.DisplayTitle);

        window.Close();
    }
}
```

`Program.cs`：

```csharp
using System;

static class Program
{
    [STAThread]
    static int Main()
    {
        Harness.Setup();
        ProbeShell.Run();
        // Each later plan adds one line here: ProbeInput.Run(); ProbeLayout.Run(); ...
        return Harness.Exit();
    }
}
```

此时 `ProbeButtons` 还不存在，先不写调用。

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | tail -30`
Expected: 全部 PASS，`0 warning log(s)`。**若 "tree has one node per category" 失败**，说明 `Nodes` 的分组与登记不一致，先查 `BuildTree`。**若 `first category is expanded` 失败**，查 `IsExpanded` 初始化。

- [ ] **Step 4: 写 Probe.Buttons.cs**

断言每个页面：能在窗口里渲染（`Bounds` 非零）、有 `DemoHeader` 且 `DocPath` 与登记一致，并读回交互结果。

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System.Linq;

static class ProbeButtons
{
    public static void Run()
    {
        // ---- every Input/Button page: renders, and its header points at its own registered DocPath
        foreach (var entry in PageCatalog.All.Where(e => e.Category == Categories.Input && e.Kind == PageKind.Control))
        {
            var page = entry.CreatePage();
            var window = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"{entry.Title}: renders with non-zero bounds", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"{entry.Title}: has a DemoHeader", header is not null);
            Harness.Check($"{entry.Title}: header DocPath matches the catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            window.Close();
        }

        Button_();
        ToggleButton_();
        RepeatButton_();
        HyperlinkButton_();
        ButtonSpinner_();
        RadioButton_();
        SplitButton_();
        ToggleSplitButton_();
    }

    static void Button_()
    {
        var page = new ButtonPage();
        var w = Harness.Show(page);
        var click = Harness.Find<Button>(page, "ClickButton");
        click.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        click.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Check("Button: two clicks count 2", Harness.Find<TextBlock>(page, "ClickCount").Text == "点击次数：2",
            Harness.Find<TextBlock>(page, "ClickCount").Text);
        Harness.Check("Button: ClickMode defaults to Release", click.ClickMode == ClickMode.Release);
        Harness.Check("Button: PressButton has ClickMode.Press", Harness.Find<Button>(page, "PressButton").ClickMode == ClickMode.Press);
        w.Close();
    }

    static void ToggleButton_()
    {
        var page = new ToggleButtonPage();
        var w = Harness.Show(page);
        var two = Harness.Find<ToggleButton>(page, "TwoState");
        Harness.Check("ToggleButton: two-state starts unchecked", two.IsChecked == false, two.IsChecked);
        two.IsChecked = true;
        Harness.Pump();
        var label = page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text?.StartsWith("IsChecked =") == true);
        Harness.Check("ToggleButton: readout follows IsChecked", label.Text == "IsChecked = True", label.Text);
        var three = Harness.Find<ToggleButton>(page, "ThreeState");
        Harness.Check("ToggleButton: three-state starts at null", three.IsThreeState && three.IsChecked is null, three.IsChecked);
        w.Close();
    }

    static void RepeatButton_()
    {
        var page = new RepeatButtonPage();
        var w = Harness.Show(page);
        var repeater = Harness.Find<RepeatButton>(page, "Repeater");
        Harness.Check("RepeatButton: Delay is bound from its slider (300)", repeater.Delay == 300, repeater.Delay);
        Harness.Check("RepeatButton: Interval is bound from its slider (100)", repeater.Interval == 100, repeater.Interval);
        Harness.Find<Slider>(page, "DelaySlider").Value = 640;
        Harness.Pump();
        Harness.Check("RepeatButton: moving the slider changes Delay", repeater.Delay == 640, repeater.Delay);
        repeater.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Check("RepeatButton: Click updates the count", Harness.Find<TextBlock>(page, "RepeatCount").Text == "触发次数：1",
            Harness.Find<TextBlock>(page, "RepeatCount").Text);
        w.Close();
    }

    static void HyperlinkButton_()
    {
        var page = new HyperlinkButtonPage();
        var w = Harness.Show(page);
        Harness.Check("HyperlinkButton: NavigateUri is set",
            Harness.Find<HyperlinkButton>(page, "DocsLink").NavigateUri?.Host == "docs.avaloniaui.net");
        Harness.Find<HyperlinkButton>(page, "PlainLink").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Check("HyperlinkButton: plain link counts clicks", Harness.Find<TextBlock>(page, "PlainCount").Text == "1");
        w.Close();
    }

    static void ButtonSpinner_()
    {
        var page = new ButtonSpinnerPage();
        var w = Harness.Show(page);
        var spinner = Harness.Find<ButtonSpinner>(page, "CounterSpinner");
        spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, SpinDirection.Increase));
        spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, SpinDirection.Increase));
        spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, SpinDirection.Decrease));
        Harness.Check("ButtonSpinner: +1 +1 -1 leaves 1", Harness.Find<TextBlock>(page, "CounterText").Text == "1",
            Harness.Find<TextBlock>(page, "CounterText").Text);
        Harness.Check("ButtonSpinner: log shows the last direction", Harness.Find<TextBlock>(page, "SpinLog").Text == "最近一次：Decrease",
            Harness.Find<TextBlock>(page, "SpinLog").Text);
        Harness.Check("ButtonSpinner: Left spinner has Location.Left", Harness.Find<ButtonSpinner>(page, "LeftSpinner").ButtonSpinnerLocation == Location.Left);
        Harness.Check("ButtonSpinner: AllowSpin=False is honoured", !Harness.Find<ButtonSpinner>(page, "NoSpinSpinner").AllowSpin);
        Harness.Check("ButtonSpinner: ShowButtonSpinner=False is honoured", !Harness.Find<ButtonSpinner>(page, "HiddenSpinner").ShowButtonSpinner);
        w.Close();
    }

    static void RadioButton_()
    {
        var page = new RadioButtonPage();
        var w = Harness.Show(page);
        Harness.Check("RadioButton: starts on 中", Harness.Find<TextBlock>(page, "SizeResult").Text == "当前选择：中",
            Harness.Find<TextBlock>(page, "SizeResult").Text);
        Harness.Find<RadioButton>(page, "SizeLarge").IsChecked = true;
        Harness.Pump();
        Harness.Check("RadioButton: choosing 大 updates the label", Harness.Find<TextBlock>(page, "SizeResult").Text == "当前选择：大",
            Harness.Find<TextBlock>(page, "SizeResult").Text);
        Harness.Check("RadioButton: 中 was unchecked by the group", Harness.Find<RadioButton>(page, "SizeMedium").IsChecked == false);
        Harness.Find<RadioButton>(page, "A2").IsChecked = true;
        Harness.Pump();
        Harness.Check("RadioButton: A2 unchecks A1", Harness.Find<RadioButton>(page, "A1").IsChecked == false);
        Harness.Check("RadioButton: panel B is unaffected", Harness.Find<RadioButton>(page, "B1").IsChecked == true);
        w.Close();
    }

    static void SplitButton_()
    {
        var page = new SplitButtonPage();
        var w = Harness.Show(page);
        var split = Harness.Find<SplitButton>(page, "SaveSplit");
        split.RaiseEvent(new RoutedEventArgs(SplitButton.ClickEvent));
        Harness.Check("SplitButton: main part runs Click", Harness.Find<TextBlock>(page, "SplitResult").Text == "执行：保存",
            Harness.Find<TextBlock>(page, "SplitResult").Text);
        var flyout = split.Flyout as MenuFlyout;
        Harness.Check("SplitButton: Flyout is a MenuFlyout with 2 items", flyout?.Items.Count == 2, flyout?.Items.Count);
        w.Close();
    }

    static void ToggleSplitButton_()
    {
        var page = new ToggleSplitButtonPage();
        var w = Harness.Show(page);
        var split = Harness.Find<ToggleSplitButton>(page, "BoldSplit");
        Harness.Check("ToggleSplitButton: starts unchecked", split.IsChecked == false, split.IsChecked);
        split.IsChecked = true;
        Harness.Pump();
        var label = page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text?.StartsWith("IsChecked =") == true);
        Harness.Check("ToggleSplitButton: readout follows IsChecked", label.Text == "IsChecked = True", label.Text);
        w.Close();
    }
}
```

在 `Program.cs` 的 `ProbeShell.Run();` 后加一行 `ProbeButtons.Run();`。

- [ ] **Step 5: 跑探针**

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | tail -70`
Expected: 全部 PASS，`0 warning log(s)`。

**失败时的排查顺序**（每一条都对应前几组踩过的坑）：
1. `RepeatButton: Delay is bound…` 失败且无异常：转换器没生效，查 `RepeatButtonPage.axaml` 的 `xmlns:conv` 与引用的是 `conv:NumberConverters.DoubleToInt`；
2. `SplitButton`、`ToggleSplitButton` 渲染但 `Bounds` 为零：查是否覆盖了 `StyleKeyOverride`（规则 6，本页用的是原生控件，不应出现）；
3. `readout follows IsChecked` 读回为空串：`StringFormat` 写法（规则 2）；
4. `RadioButton: starts on 中` 失败：`SizeResult` 没排在三个 `RadioButton` 之前（见 Task 5 Step 2 的注释）。

- [ ] **Step 6: 真实窗口目视（人工，仅一次）**

```bash
dotnet run --project Avalonia.ControlsDemo
```

确认：左侧树第一个分类（Input）已展开并选中 Button；点击其它 7 个页面右侧都能切换；点击分类名行右侧显示「请在左侧选择一个控件」。`RepeatButton` 页**真的按住鼠标**，计数应持续增加（headless 无法验证按住行为，这一步补上）；`HyperlinkButton` 页点击第一个链接应打开系统浏览器。

### Task 7: 风险验证、收尾与回填

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填风险验证结果）

**Interfaces:**
- Consumes: Task 6 的探针骨架
- Produces: 风险 1、3、4 的实测结论写入 spec，供第 01、03、06 份计划引用

本份只验证**会改变后面计划形态**的三个风险，其余风险（2 号原生控件）由用到它的第 05、07 份各自验证。

- [ ] **Step 1: 验证风险 3 与 4——DataGrid / ColorPicker 能否在 12.1.2 渲染（编写计划时已实测，此步为回归确认）**

**实测结论（Avalonia 12.1.2 headless，读回可视后代数与行数）：两个包都必须显式 `StyleInclude`，否则构造成功、能布局、但什么都不画，且不报错不记日志。**

| 控件 | 不加 StyleInclude | 加 StyleInclude |
|---|---|---|
| `DataGrid`（2 列 2 行） | `Bounds` 800×600，列数 2，**行 0、单元格 0、可视后代 0** | `Themes/Fluent.xaml`：行 2、单元格 6、后代 129；`Themes/Simple.xaml`：行 2、单元格 6、后代 86 |
| `ColorPicker` / `ColorView` | **picker 后代 0、view 后代 0** | `Themes/Fluent/Fluent.xaml`：picker 12、view 101 |

**本项目用 Fluent 主题，所以只用下面两个**（均已用 `StyleInclude` 实际加载并读回过）：

```xml
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml" />
<StyleInclude Source="avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml" />
```

`DataGrid` 另有 `Themes/Simple.xaml`（行 2、后代 86，同样可用），配 Simple 主题时才需要，本项目不用。

**反例（不要写）**：`avares://Avalonia.Controls.DataGrid/Themes/Generic.xaml` 与 `avares://Avalonia.Controls.ColorPicker/Themes/Fluent.xaml` 都抛 `XamlLoadException: No precompiled XAML found`。ColorPicker 的主题文件在 `Themes/Fluent/` 子目录下，**少一层目录就找不到**。

**这是又一种"构建通过、运行无异常、界面空白"的静默失败**，与 spec 里记录的本地值压制样式同属一类。第 01 份（ColorPicker、ColorView）与第 03 份（DataGrid）各自在引用包的同时，必须在 `Avalonia.ControlsDemo/App.axaml` 的 `Application.Styles` 里加上对应的 `StyleInclude`，并在探针里用"后代数大于零"断言它生效。

探针 csproj 加两个包引用：

```xml
    <PackageReference Include="Avalonia.Controls.DataGrid" Version="12.1.2" />
    <PackageReference Include="Avalonia.Controls.ColorPicker" Version="12.1.2" />
```

`Probe.Packages.cs`（回归确认：先断言"不加样式时为空"，证明静默失败确实存在，再断言"加了之后有内容"）：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

static class ProbePackages
{
    record Row(string Name, int Age);

    static int RealisedRows()
    {
        var grid = new DataGrid { ItemsSource = new List<Row> { new("A", 1), new("B", 2) }, AutoGenerateColumns = true };
        var w = Harness.Show(grid);
        int rows = grid.GetVisualDescendants().OfType<DataGridRow>().Count();
        w.Close();
        return rows;
    }

    static int PickerVisuals()
    {
        var picker = new ColorPicker();
        var view = new ColorView();
        var w = Harness.Show(new StackPanel { Children = { picker, view } });
        int n = picker.GetVisualDescendants().Count() + view.GetVisualDescendants().Count();
        w.Close();
        return n;
    }

    static StyleInclude Include(string source)
    {
        var inc = new StyleInclude(new Uri("avares://probe/")) { Source = new Uri(source) };
        Application.Current!.Styles.Add(inc);
        return inc;
    }

    public static void Run()
    {
        // Silent failure first: the control exists and lays out, but draws nothing.
        Harness.Check("DataGrid without a StyleInclude realises no rows", RealisedRows() == 0);
        Harness.Check("ColorPicker without a StyleInclude has no visuals", PickerVisuals() == 0);

        var dg = Include("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml");
        Harness.Check("DataGrid with Themes/Fluent.xaml realises 2 rows", RealisedRows() == 2);
        Application.Current!.Styles.Remove(dg);

        var cp = Include("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml");
        Harness.Check("ColorPicker + ColorView with Themes/Fluent/Fluent.xaml have visuals", PickerVisuals() > 50);
        Application.Current!.Styles.Remove(cp);
    }
}
```

`Program.cs` 加 `ProbePackages.Run();`。

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "DataGrid|ColorPicker|passed"`
Expected: 4 条全 PASS。

**若有失败**：`without a StyleInclude` 两条失败说明新版本改成了自带样式，那后面的 `StyleInclude` 反而多余，记入 spec 并从第 01、03 份里去掉；`with Themes/...` 失败说明路径变了，用 `Application.Current.Styles` 逐个试，不要猜。**包不兼容**（`NU1xxx` 或 `FileNotFoundException`）则停下回报用户：ColorPicker / ColorView 与 DataGrid 页降级为路标页，并改 spec 页数。

把结论原样记下，用于 Step 3。

- [ ] **Step 2: 验证风险 1——新页面类型在桌面上的可用性**

在探针里新增 `Probe.Pages.cs`。判定标准直接取自 spec：**能实例化、`Measure`/`Arrange` 后 `Bounds` 非零、可见后代数大于零**。

```csharp
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.Linq;

static class ProbeNewPages
{
    public static void Run()
    {
        Try("NavigationPage", () => new NavigationPage { Content = new ContentPage { Header = "首页", Content = new TextBlock { Text = "hi" } } });
        Try("ContentPage", () => new ContentPage { Header = "页", Content = new TextBlock { Text = "hi" } });
        Try("DrawerPage", () => new DrawerPage { Content = new ContentPage { Content = new TextBlock { Text = "hi" } }, Drawer = new ContentPage { Content = new TextBlock { Text = "d" } } });
        Try("TabbedPage", () => { var t = new TabbedPage(); t.Pages = new[] { new ContentPage { Header = "a", Content = new TextBlock { Text = "A" } } }; return t; });
        Try("CarouselPage", () => { var c = new CarouselPage(); c.Pages = new[] { new ContentPage { Content = new TextBlock { Text = "A" } } }; return c; });
        Try("CommandBar", () => new CommandBar());
    }

    static void Try(string name, Func<Control> make)
    {
        try
        {
            var c = make();
            var w = Harness.Show(c);
            var n = c.GetVisualDescendants().Count();
            Harness.Check($"{name}: instantiates, non-zero bounds, has visuals", c.Bounds.Width > 0 && c.Bounds.Height > 0 && n > 0,
                $"bounds={c.Bounds} visuals={n}");
            w.Close();
        }
        catch (Exception ex)
        {
            Harness.Check($"{name}: instantiates", false, ex.GetType().Name + ": " + ex.Message);
        }
    }
}
```

**实测结论（Avalonia 12.1.2 headless，探针已跑过）：6 个控件全部能实例化、`Bounds` 非零、有可视后代，风险 1 解除，第 06 份无需降级任何页面。** 下面的探针代码里用到的成员名**已全部用反射核实存在**：`TabbedPage.Pages` 与 `CarouselPage.Pages` 来自抽象基类 `MultiPage`（`Pages`、`ItemsSource`、`PageTemplate`），`SelectingMultiPage` 再加 `SelectedIndex`、`SelectedPage`；`DrawerPage.Drawer` 与 `DrawerPage.Content` 存在；`Page.Header` 存在。

**第 06 份可以直接依赖的成员**（反射核实，类型在括号里）：

| 类型 | 成员 |
|---|---|
| `Page`（基类，`TemplatedControl`） | `Header`(object)、`Icon`(object)、`CurrentPage`、`Navigation`、`IsInNavigationPage`；事件 `NavigatedTo`、`Navigating`、`NavigatedFrom` |
| `ContentPage` | `Content`、`TopCommandBar`、`BottomCommandBar`（均为 object）、`HorizontalContentAlignment`、`VerticalContentAlignment` |
| `NavigationPage`（`MultiPage`） | `Content`、`PageTransition`、`ModalTransition`、`CanGoBack`、`StackDepth`、`NavigationStack`、`ModalStack`、`IsBackButtonVisible`、`IsGestureEnabled`、`BarHeight`、`HasShadow`；方法 `PushAsync`、`PopAsync`、`PopToRootAsync`、`PopToPageAsync`、`PushModalAsync`、`PopModalAsync`、`PopAllModalsAsync`、`RemovePage`、`InsertPage`、`ReplaceAsync`；事件 `Pushed`、`Popped`、`PoppedToRoot`、`ModalPushed`、`ModalPopped` |
| `DrawerPage` | `Drawer`、`Content`、`IsOpen`、`DrawerLength`、`CompactDrawerLength`、`DrawerBehavior`、`DrawerLayoutBehavior`、`DrawerPlacement`、`DrawerHeader`、`DrawerFooter`、`DisplayMode`(`SplitViewDisplayMode`)；事件 `Opened`、`Closing`、`Closed` |
| `TabbedPage`（`SelectingMultiPage`） | `Pages`、`SelectedIndex`、`TabPlacement`、`PageTransition`、`IsGestureEnabled`、`IsKeyboardNavigationEnabled` |
| `CarouselPage`（`SelectingMultiPage`） | `Pages`、`SelectedIndex`、`PageTransition`、`IsGestureEnabled`、`IsKeyboardNavigationEnabled`、`ItemsPanel` |
| `CommandBar` | `PrimaryCommands`、`SecondaryCommands`（`IList`）、`Content`、`DefaultLabelPosition`、`IsDynamicOverflowEnabled`、`OverflowButtonVisibility`、`IsOpen`、`IsSticky`；事件 `Opening`、`Opened`、`Closing`、`Closed` |
| `CommandBarButton`（`Button`）、`CommandBarToggleButton`（`ToggleButton`） | `Label`、`Icon`、`IsCompact`、`DynamicOverflowOrder`、`LabelPosition`、`IsInOverflow` |

枚举：`DrawerBehavior` = `Auto`/`Flyout`/`Locked`/`Disabled`；`DrawerPlacement` = `Left`/`Right`/`Top`/`Bottom`；`DrawerLayoutBehavior` = `Overlay`/`Split`/`CompactOverlay`/`CompactInline`；`TabPlacement` = `Auto`/`Top`/`Bottom`/`Left`/`Right`；`CommandBarDefaultLabelPosition` = `Bottom`/`Right`/`Collapsed`。

本步仍然要跑一遍探针，作为回归确认（与上面的实测对照，防止包版本变动）。

`Program.cs` 加 `ProbeNewPages.Run();`。

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "NavigationPage|ContentPage|DrawerPage|TabbedPage|CarouselPage|CommandBar|passed"`

**判定与分支：**

| 结果 | 动作 |
|---|---|
| 某控件 PASS | 第 06 份对它做可运行页 |
| 某控件 FAIL 且是异常 | 该页降级为路标页；spec 的 Control 页数 −1、Signpost 页数 +1；第 06 份对应任务改写 |
| 某控件实例化成功但 `Bounds` 为零 | 先试把它放进 `Grid` 并给固定 `Width`/`Height`；仍为零则同上降级 |

- [ ] **Step 3: 把三个风险的结果回填到 spec**

在 `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md` 的「待验证的技术风险」末尾追加一小节：

```markdown
### 批 0 实测结论（填入执行日期）

- **风险 3（DataGrid 包）**：<实测结果：包名、版本、是否需要 StyleInclude 及准确的 Source>。这是"不新增依赖"的唯一例外，已在 `Directory.Packages.props` 声明。
- **风险 4（ColorPicker / ColorView 包）**：<同上>。
- **风险 1（新页面类型）**：NavigationPage、ContentPage、DrawerPage、TabbedPage、CarouselPage、CommandBar 六个控件在桌面 headless 下全部能实例化、`Bounds` 非零、有可视后代；无需降级，第 06 份全部做可运行页。成员清单见本计划 Task 7 Step 2。（若回归探针与此不符，以回归结果为准并改本条。）
- **Buttons 样板中确认的写法**：<执行中新发现的任何静默失败或 API 出入，没有则写"无">。
```

尖括号里的内容**必须替换为 Step 1、Step 2 的真实结果**，不能留着。若有控件降级，同时把 spec「控件映射」表与批次表里对应的页数改掉，并回头改第 01–08 份计划文件头部的页数表。

- [ ] **Step 4: 在 README 登记新项目**

先看 README 里项目表的现有格式：

Run: `grep -n "Avalonia\.\(LayoutDemo\|AppDevelopmentDemo\|CustomControlsDemo\)" README.md`

按相同的列结构，在项目表里新增一行 `Avalonia.ControlsDemo`，说明写「按官方 Controls 目录组织的控件演示（10 个分类，侧边导航）；Pro / Enterprise 控件只有路标页」。位置放在 `Avalonia.CustomControlsDemo` 之前（与 slnx 的字母序一致）。**此时只登记项目，不写控件数字**：页数要到第 08 份全部完成后才是最终值，现在写会过时。

- [ ] **Step 5: 全量构建与探针**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|警告|个错误|Build succeeded" | tail -5`
Expected: 0 个错误，无新增警告。

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | tail -8`
Expected: `0 failed`，`0 warning log(s)`；Step 2 中降级的控件除外（其 FAIL 是预期结果，已记入 spec，探针里把这几条的 `Harness.Check` 改成只打印不计失败，或直接删掉这几行，**探针里不能留着会失败的断言**）。

- [ ] **Step 6: 提交**

```bash
git add README.md docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "docs: record the ControlsDemo pilot findings and register the project

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

**探针目录保留**：`C:\Temp\probe-controls\` 不删，第 01–08 份继续使用。

---

## Self-Review（编写者自查记录）

**Spec 覆盖**：
- 项目骨架（目录树、`PageCatalog`、`PageKind`、`NavNode`）→ Task 1–3；
- Input / Buttons 8 页 → Task 4–5；
- 验证标准的前三条（构建、探针、目录完整性）→ Task 6；第 4 条（人工启动）→ Task 6 Step 6；
- 风险 1、3、4 → Task 7；风险 2（原生控件）→ 留给第 05、07 份；
- spec 的"不新增依赖"：本份新增 2 个包版本，已在"已核实事实"与 Task 7 Step 3 注明是唯一例外。

**与 spec 的两处偏离（已同步改 spec）**：`CategoryNode` → `NavNode`（Task 2 Step 4）；去掉搜索关键字（Task 3 Step 4）。

**类型一致性**：`PageEntry` 的五个成员、`ControlPage<TPage>(title, category, docPath)` 的参数顺序、`Categories` 常量名、`NavNode.DisplayTitle` 在 Task 2、3、6 里一致；`Harness` 的五个成员在 Task 6、7 里一致。

**留给执行者的两处核实**（计划里已写明做法）：Task 7 Step 1 的 StyleInclude 路径、Step 2 的新页面成员名。这两处无法在写计划时确定，因为它们取决于执行时的实测，不是遗漏。
