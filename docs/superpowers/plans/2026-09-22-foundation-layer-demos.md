# Avalonia 基础层演示项目（#1 Fundamentals / #2 XAML / #7 PropertySystem）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建成 `Avalonia.FundamentalsDemo`、`Avalonia.XamlDemo`、`Avalonia.PropertySystemDemo` 三个演示项目，覆盖官方文档 Fundamentals、XAML Reference、Property System 三个分类的可演示功能点，全部沿用 `Avalonia.LayoutDemo` 样板确立的结构。

**Architecture:** 三个独立 WinExe 项目，各自 `MainWindow` 只承载 `TabControl` 外壳，每个功能点是 `Views/Pages/` 下一个独立 `UserControl`，页面顶部统一用 `Avalonia.Shared` 的 `DemoHeader` 显示中文说明与官方文档路径。无外部依赖，无网络调用。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm、中央包管理（`Directory.Packages.props`）

**Spec:** `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**样板参考:** `docs/superpowers/plans/2026-09-21-layout-demo-template.md` 与已落成的 `Avalonia.LayoutDemo/`

## Global Constraints

以下约束适用于本 plan 的每一个任务：

- **Avalonia 版本统一为 12.1.2**，不为任何项目降级到 Avalonia 11
- **TargetFramework 为 `net10.0`**，`Nullable` 为 `enable`
- **包版本只在 `Directory.Packages.props` 声明**，`.csproj` 里的 `PackageReference` 不带 `Version` 属性
- **不引用 `Avalonia.Diagnostics`**（停在 11.3.22，v12 的 DevTools 已内置于主包）
- **不引入 ReactiveUI、Prism 等第三方 MVVM/UI 框架**，只用官方 API + `CommunityToolkit.Mvvm`
- **C# 与 XAML 注释用英文**；**界面文字（Tab 标题、说明条、按钮文案）用中文**；**标识符（类名、属性名、`x:Name`）用英文**
- **每个演示页顶部必须有 `DemoHeader`**，含中文功能点描述 + 对应官方文档路径
- **每个功能点一个 `UserControl`**，放在 `Views/Pages/` 下
- **不为演示项目写自动化测试**
- **`AvaloniaUseCompiledBindingsByDefault` 设为 `true`**
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`

### 样板阶段已确立的硬性规则（违反会导致静默失败）

这三条来自 LayoutDemo 样板的实测教训，spec「样板阶段实测结论」有完整记录：

1. **数值绑到结构体属性必须走转换器。** `double` → `Thickness`/`CornerRadius` 没有内置转换，
   失败只记一条 binding warning、不抛异常。用 `Avalonia.Shared/Converters/DoubleToThicknessConverter.cs`。
2. **打算被 Style / 伪类 / ContainerQuery 驱动的属性，不在元素上写本地值。**
   XAML 元素属性是 `BindingPriority.LocalValue`（值 0，最高），永久压过所有样式 Setter，
   且**连警告都没有**。需要兜底就写进普通 `<Style>`。
3. **验证用 headless 探针读回属性值，不靠目视。** 建在仓库外、跑完即弃，不违反
   「不为演示项目写自动化测试」约束。做法见本 plan 每组的验证任务。

## 本 plan 的范围

spec 第二阶段分 4 组，本 plan 只实现**第一组「基础层」**：#1 Fundamentals、#2 XAML、
#7 PropertySystem。其余 3 组在本组通过用户 review 后再编写。

第 4 组的两个技术风险（`Avalonia.Headless.XUnit` 是否有 12.x 版本、Services 在桌面端的
可用性）与本组无关，留待对应 plan 验证。

## 功能点映射的实测修正

编写本 plan 时核对了官方文档侧边栏的真实子页，与 spec 的功能点列表有三处出入，
本 plan 按实际文档结构执行：

| 分类 | spec 的列法 | 官方实际 | 本 plan 的处理 |
|---|---|---|---|
| Fundamentals | 8 个 Tab | 12 个子页 | 去掉纯说明性的 `architecture` / `cross-platform-architecture`；`avalonia-xaml` 归项目 #2；补上 spec 漏掉的 `main-window`，与 `top-level` 合并成一个 Tab |
| Property System | 6 个 Tab，含三种属性的定义 | 本分类只有 3 个子页 | `StyledProperty`/`DirectProperty`/`AttachedProperty` 的定义方式在 `custom-controls/defining-properties`，说明条如实指向该路径 |
| XAML Reference | 6 个 Tab | 6 个子页，完全吻合 | 不调整 |

## File Structure

三个项目结构同构，均照搬 `Avalonia.LayoutDemo`。下表只列每个项目**独有**的文件；
`Program.cs`、`App.axaml(.cs)`、`app.manifest`、`Assets/avalonia-logo.ico`、
`Views/MainWindow.axaml(.cs)`、`.csproj` 六件套每个项目都有一份，内容除项目名外一致。

### 项目 #1 `Avalonia.FundamentalsDemo`（6 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/CodedUiPage.axaml(.cs)` | 纯代码建 UI 与 XAML 等价对照 | `fundamentals/coded-ui` |
| `Views/Pages/CodeBehindPage.axaml(.cs)` | code-behind 事件处理与 `x:Name` 访问 | `fundamentals/code-behind` |
| `Views/Pages/MvvmPage.axaml(.cs)` | MVVM 三层职责划分 | `fundamentals/the-mvvm-pattern` |
| `Views/Pages/TopLevelPage.axaml(.cs)` | `TopLevel` 与 `Window` 的关系、屏幕与缩放信息 | `fundamentals/top-level` |
| `Views/Pages/TreesPage.axaml(.cs)` | 视觉树与逻辑树的实时对照 | `fundamentals/visual-and-logical-trees` |
| `Views/Pages/LifetimesPage.axaml(.cs)` | 应用生命周期与 Assets 加载 | `fundamentals/application-lifetimes` |
| `ViewModels/MvvmViewModel.cs` | MVVM 页的 ViewModel | — |
| `ViewModels/TreesViewModel.cs` | 树结构快照的 ViewModel | — |
| `Models/TreeNodeInfo.cs` | 树节点展示模型 | — |

### 项目 #2 `Avalonia.XamlDemo`（6 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/NamespacesPage.axaml(.cs)` | XAML 命名空间声明与 `using:` 前缀 | `xaml/namespaces` |
| `Views/Pages/DirectivesPage.axaml(.cs)` | `x:Name` / `x:Key` / `x:Static` 等指令 | `xaml/directives` |
| `Views/Pages/MarkupExtensionsPage.axaml(.cs)` | 内置标记扩展与自定义标记扩展 | `xaml/markup-extensions` |
| `Views/Pages/TypeConvertersPage.axaml(.cs)` | 字符串到强类型的隐式转换 | `xaml/type-converters` |
| `Views/Pages/GenericsPage.axaml(.cs)` | `x:TypeArguments` 泛型实例化 | `xaml/generics` |
| `Views/Pages/CompilationPage.axaml(.cs)` | 编译型 XAML 与运行时加载对照 | `xaml/compilation` |
| `MarkupExtensions/RepeatTextExtension.cs` | 自定义标记扩展示例 | — |
| `Models/PriorityLevel.cs` | 供 `x:Static` 与泛型页使用的枚举与集合类型 | — |

### 项目 #7 `Avalonia.PropertySystemDemo`（5 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/StyledPropertyPage.axaml(.cs)` | `StyledProperty` 定义与样式驱动 | `custom-controls/defining-properties` |
| `Views/Pages/DirectPropertyPage.axaml(.cs)` | `DirectProperty` 与 CLR 字段的关系 | `custom-controls/defining-properties` |
| `Views/Pages/AttachedPropertyPage.axaml(.cs)` | 附加属性的定义与消费 | `custom-controls/defining-properties#attached-properties` |
| `Views/Pages/PrecedencePage.axaml(.cs)` | 属性值优先级实战（含样板踩过的本地值坑） | `properties/value-precedence` |
| `Views/Pages/MetadataPage.axaml(.cs)` | 默认值、coerce、validate、变更回调、值继承 | `properties/metadata-and-callbacks` |
| `Controls/GaugeControl.cs` | 演示三种属性的自定义控件 | — |
| `Controls/GaugeControl.axaml` | `GaugeControl` 的 ControlTheme | — |
| `Controls/HighlightBehavior.cs` | 附加属性宿主 | — |
| `ViewModels/PrecedenceViewModel.cs` | 优先级页的状态 | — |

### 共享文件的改动

| 文件 | 改动 |
|---|---|
| `hello-avalonia.slnx` | 注册三个新项目 |
| `README.md` | 项目表格加三行 |
| `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md` | 回写本组实测结论 |

**为什么 `GaugeControl` 放在项目 #7 而不是 `Avalonia.Shared`**：它是 Property System 分类的
教学道具，三种属性定义方式都要在它身上体现，其它项目不会用。`DemoHeader` 进 `Avalonia.Shared`
是因为 15 个项目都要用——判断标准是复制次数，不是"看起来像个控件"。

**为什么 `TopLevel` 与 `MainWindow` 合成一个 Tab**：官方 `main-window` 页讲的是"怎么设置
启动窗口"，`top-level` 页讲的是"Window/Popup/嵌入宿主的共同基类"。前者的可演示内容（改
`desktop.MainWindow`）在应用启动后已无法交互展示，附在 `TopLevelPage` 里作为说明更实在。

---

## Task 1: 三个项目的骨架与 TabControl 外壳

**Files:**
- Create: `Avalonia.FundamentalsDemo/` 下的 `Avalonia.FundamentalsDemo.csproj`、`Program.cs`、`App.axaml`、`App.axaml.cs`、`app.manifest`、`Assets/avalonia-logo.ico`、`Views/MainWindow.axaml`、`Views/MainWindow.axaml.cs`
- Create: `Avalonia.XamlDemo/` 下同名八件套
- Create: `Avalonia.PropertySystemDemo/` 下同名八件套
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: `avares://Avalonia.Shared/Themes/SharedStyles.axaml`（已存在，提供 `DemoHeader`、`TextBlock.caption`、`Border.stage`）
- Produces: 三个可运行的空壳窗口。命名空间分别为 `Avalonia.FundamentalsDemo.Views.Pages`、
  `Avalonia.XamlDemo.Views.Pages`、`Avalonia.PropertySystemDemo.Views.Pages`，供 Task 2–4 挂页面。

三份骨架除项目名外完全一致，因此合成一个任务——拆开会让审查者连看三遍同样的 diff。

- [x] **Step 1: 用脚本批量生成三份骨架的目录与二进制文件**

在仓库根目录执行。图标与 manifest 从样板项目复制，避免手工重建二进制文件：

```bash
for p in FundamentalsDemo XamlDemo PropertySystemDemo; do
  mkdir -p "Avalonia.$p/Assets" "Avalonia.$p/Views/Pages"
  cp Avalonia.LayoutDemo/Assets/avalonia-logo.ico "Avalonia.$p/Assets/"
  sed "s/Avalonia\.LayoutDemo/Avalonia.$p/" Avalonia.LayoutDemo/app.manifest > "Avalonia.$p/app.manifest"
done
grep -l "Avalonia.LayoutDemo" Avalonia.*Demo/app.manifest || echo "manifest names rewritten"
```

最后一行应输出 `manifest names rewritten`。若输出了文件路径，说明 `sed` 替换没生效，
检查样板 manifest 里的 `assemblyIdentity` 名称是否确实是 `Avalonia.LayoutDemo`。

- [x] **Step 2: 创建三个 .csproj**

三份内容除项目名外一致。`Avalonia.FundamentalsDemo/Avalonia.FundamentalsDemo.csproj`：

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

`Avalonia.XamlDemo/Avalonia.XamlDemo.csproj` 与
`Avalonia.PropertySystemDemo/Avalonia.PropertySystemDemo.csproj` 内容逐字相同——
本文件没有项目名出现，直接复制即可。

注意**不要**加 `Avalonia.Diagnostics` 引用：它停在 11.3.22，v12 的 DevTools 已内置于主包，
样板阶段已把这条定为规则（见 Global Constraints）。

- [x] **Step 3: 创建三个 Program.cs**

`Avalonia.FundamentalsDemo/Program.cs`：

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.FundamentalsDemo
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

另两个项目同此，只把 `namespace` 换成 `Avalonia.XamlDemo` /
`Avalonia.PropertySystemDemo`。

- [x] **Step 4: 创建三个 App.axaml 与 App.axaml.cs**

`Avalonia.FundamentalsDemo/App.axaml`：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.FundamentalsDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader plus the shared caption and stage styles.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>
</Application>
```

`Avalonia.FundamentalsDemo/App.axaml.cs`：

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.FundamentalsDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.FundamentalsDemo
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

另两个项目同此，把 `x:Class`、`namespace`、`using` 里的 `Avalonia.FundamentalsDemo`
换成对应项目名。

- [x] **Step 5: 创建三个 MainWindow**

`Avalonia.FundamentalsDemo/Views/MainWindow.axaml`：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.FundamentalsDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Fundamentals Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <TabControl Margin="12">
    </TabControl>
</Window>
```

`Avalonia.FundamentalsDemo/Views/MainWindow.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.FundamentalsDemo.Views
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

另两个项目的 `Title` 分别为 `Avalonia XAML Demo` 和
`Avalonia Property System Demo`，`x:Class` 与 `namespace` 随项目名变化。

- [x] **Step 6: 注册到解决方案**

修改 `hello-avalonia.slnx`，按字母序插入三行。`Avalonia.FundamentalsDemo` 在
`Avalonia.DataTemplateDemo` 之后，`Avalonia.PropertySystemDemo` 在 `Avalonia.MusicStore`
之后，`Avalonia.XamlDemo` 在 `Avalonia.WebViewDemo` 之后：

```xml
<Solution>
  <Project Path="Avalonia.DataTemplateDemo/Avalonia.DataTemplateDemo.csproj" />
  <Project Path="Avalonia.FundamentalsDemo/Avalonia.FundamentalsDemo.csproj" />
  <Project Path="Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj" />
  <Project Path="Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj" />
  <Project Path="Avalonia.MusicStore/Avalonia.MusicStore.csproj" />
  <Project Path="Avalonia.PropertySystemDemo/Avalonia.PropertySystemDemo.csproj" />
  <Project Path="Avalonia.Shared/Avalonia.Shared.csproj" />
  <Project Path="Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj" />
  <Project Path="Avalonia.XamlDemo/Avalonia.XamlDemo.csproj" />
</Solution>
```

- [x] **Step 7: 构建验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -5`
Expected: `0 个错误`。三个新项目均出现在构建输出中。

已知的既有警告（不是本任务引入的，不要去修）：`Avalonia.MusicStore` 的 NU1701 与
`Bitmap.Save` 过时警告、`Avalonia.HtmlRendererDemo` 的 MSB3245 `System.Xaml`、
几个项目的 CS8618/CS8604。

- [x] **Step 8: 提交**

```bash
git add Avalonia.FundamentalsDemo/ Avalonia.XamlDemo/ Avalonia.PropertySystemDemo/ hello-avalonia.slnx
git commit -m "$(cat <<'EOF'
feat: scaffold the three foundation-layer demo projects

Empty TabControl shells wired to the shared styles, following the
LayoutDemo template. Pages land in the following commits, one category
per commit.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: 项目 #1 Fundamentals 的 6 个页面

**Files:**
- Create: `Avalonia.FundamentalsDemo/Models/TreeNodeInfo.cs`
- Create: `Avalonia.FundamentalsDemo/ViewModels/MvvmViewModel.cs`
- Create: `Avalonia.FundamentalsDemo/ViewModels/TreesViewModel.cs`
- Create: `Avalonia.FundamentalsDemo/Views/Pages/CodedUiPage.axaml(.cs)`
- Create: `Avalonia.FundamentalsDemo/Views/Pages/CodeBehindPage.axaml(.cs)`
- Create: `Avalonia.FundamentalsDemo/Views/Pages/MvvmPage.axaml(.cs)`
- Create: `Avalonia.FundamentalsDemo/Views/Pages/TopLevelPage.axaml(.cs)`
- Create: `Avalonia.FundamentalsDemo/Views/Pages/TreesPage.axaml(.cs)`
- Create: `Avalonia.FundamentalsDemo/Views/Pages/LifetimesPage.axaml(.cs)`
- Modify: `Avalonia.FundamentalsDemo/Views/MainWindow.axaml`

**Interfaces:**
- Consumes: Task 1 的 `MainWindow` 外壳；`Avalonia.Shared.Controls.DemoHeader`；
  `Avalonia.Shared.ViewModels.ViewModelBase`（继承自 `ObservableObject`）
- Produces: 六个无参构造的 `UserControl`：`CodedUiPage`、`CodeBehindPage`、`MvvmPage`、
  `TopLevelPage`、`TreesPage`、`LifetimesPage`。
  `MvvmViewModel` 含 `string Input`、`string Greeting`、`IRelayCommand GreetCommand`；
  `TreesViewModel` 含 `ObservableCollection<TreeNodeInfo> VisualTree`、
  `ObservableCollection<TreeNodeInfo> LogicalTree`、`void Refresh(Control root)`。

- [x] **Step 1: 创建树节点模型**

创建 `Avalonia.FundamentalsDemo/Models/TreeNodeInfo.cs`：

```csharp
using System.Collections.ObjectModel;

namespace Avalonia.FundamentalsDemo.Models
{
    /// <summary>
    /// One node in a snapshot of either tree. The snapshot is a plain model
    /// rather than the live control, so the TreeView cannot accidentally
    /// re-parent the controls it is displaying.
    /// </summary>
    public sealed class TreeNodeInfo
    {
        public TreeNodeInfo(string label)
        {
            Label = label;
        }

        public string Label { get; }

        public ObservableCollection<TreeNodeInfo> Children { get; } = new();
    }
}
```

**为什么快照而不是直接绑活控件**：`TreeView` 会把 item 作为内容承载，直接把活控件塞进去
等于把它从原来的视觉树上摘下来——演示视觉树的页面反而会破坏视觉树。

- [x] **Step 2: 创建 MVVM 页的 ViewModel**

创建 `Avalonia.FundamentalsDemo/ViewModels/MvvmViewModel.cs`：

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.FundamentalsDemo.ViewModels
{
    /// <summary>
    /// The view model knows nothing about the view: no control types, no
    /// event handlers. That is what makes the same state testable and
    /// reusable across views.
    /// </summary>
    public partial class MvvmViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GreetCommand))]
        private string _input = string.Empty;

        [ObservableProperty]
        private string _greeting = "（还没有问候）";

        private bool CanGreet() => !string.IsNullOrWhiteSpace(Input);

        [RelayCommand(CanExecute = nameof(CanGreet))]
        private void Greet()
        {
            Greeting = $"你好，{Input.Trim()}！";
        }
    }
}
```

注意 `[NotifyCanExecuteChangedFor]`：没有它，`Input` 变化时按钮的可用状态不会刷新——
这是 CommunityToolkit 源生成器里最常被漏掉的一环。

- [x] **Step 3: 创建树快照的 ViewModel**

创建 `Avalonia.FundamentalsDemo/ViewModels/TreesViewModel.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.FundamentalsDemo.Models;
using Avalonia.LogicalTree;
using Avalonia.Shared.ViewModels;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.FundamentalsDemo.ViewModels
{
    /// <summary>
    /// Walks both trees from the same root so the two panes can be compared
    /// side by side. The visual tree includes every part a control template
    /// expanded into; the logical tree stops at what the XAML author wrote.
    /// </summary>
    public sealed class TreesViewModel : ViewModelBase
    {
        public ObservableCollection<TreeNodeInfo> VisualTree { get; } = new();

        public ObservableCollection<TreeNodeInfo> LogicalTree { get; } = new();

        public void Refresh(Control root)
        {
            VisualTree.Clear();
            LogicalTree.Clear();
            VisualTree.Add(BuildVisual(root));
            LogicalTree.Add(BuildLogical(root));
        }

        private static TreeNodeInfo BuildVisual(Visual visual)
        {
            var node = new TreeNodeInfo(Describe(visual));
            foreach (var child in visual.GetVisualChildren())
            {
                node.Children.Add(BuildVisual(child));
            }
            return node;
        }

        private static TreeNodeInfo BuildLogical(ILogical logical)
        {
            var node = new TreeNodeInfo(Describe(logical));
            foreach (var child in logical.LogicalChildren)
            {
                node.Children.Add(BuildLogical(child));
            }
            return node;
        }

        private static string Describe(object node)
        {
            var typeName = node.GetType().Name;
            var name = (node as Control)?.Name;
            return string.IsNullOrEmpty(name) ? typeName : $"{typeName} \"{name}\"";
        }
    }
}
```

以上 API 形态（`Visual.GetVisualChildren()`、`ILogical.LogicalChildren`）已在
Avalonia 12.1.2 上编译验证过，可直接使用。

- [x] **Step 4: 创建 CodedUiPage**

创建 `Avalonia.FundamentalsDemo/Views/Pages/CodedUiPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.FundamentalsDemo.Views.Pages.CodedUiPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="纯代码建 UI：同一个界面，XAML 与 C# 两种写法"
                               DocPath="fundamentals/coded-ui" />

            <TextBlock Classes="caption" Text="左边由 XAML 声明，右边由 C# 在构造函数里搭建，两者渲染结果一致" />
            <Grid ColumnDefinitions="*,*">
                <Border Grid.Column="0" Classes="stage" Margin="3" Padding="10">
                    <StackPanel Spacing="6">
                        <TextBlock FontWeight="SemiBold" Text="XAML 声明" />
                        <TextBox Watermark="输入点什么" />
                        <Button Content="提交" HorizontalAlignment="Stretch"
                                HorizontalContentAlignment="Center" />
                    </StackPanel>
                </Border>

                <!--  Filled in by the constructor; see the code-behind.  -->
                <Border Grid.Column="1" Name="CodeBuiltHost" Classes="stage" Margin="3" Padding="10" />
            </Grid>

            <TextBlock Classes="caption" Text="什么时候用纯代码" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="控件数量或层级在编译期无法确定时（例如按运行时数据生成表单），纯代码比 XAML 更直接。反之，静态布局用 XAML 更易读，也能享受编译型绑定与设计时预览。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.FundamentalsDemo/Views/Pages/CodedUiPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class CodedUiPage : UserControl
    {
        public CodedUiPage()
        {
            InitializeComponent();

            // The same three controls the XAML side declares, built by hand.
            // Property setters map one-to-one onto XAML attributes.
            CodeBuiltHost.Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { FontWeight = FontWeight.SemiBold, Text = "C# 构建" },
                    new TextBox { Watermark = "输入点什么" },
                    new Button
                    {
                        Content = "提交",
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                    },
                },
            };
        }
    }
}
```

- [x] **Step 5: 创建 CodeBehindPage**

创建 `Avalonia.FundamentalsDemo/Views/Pages/CodeBehindPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.FundamentalsDemo.Views.Pages.CodeBehindPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Code-behind：x:Name 让 XAML 元素成为字段"
                               DocPath="fundamentals/code-behind" />

            <TextBlock Classes="caption" Text="1. x:Name 生成的字段可以在 code-behind 里直接访问" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <TextBox Name="NameBox" Watermark="你的名字" />
                    <Button Name="GreetButton" Content="打招呼" Click="OnGreetClick" />
                    <TextBlock Name="ResultText" Text="（结果显示在这里）" TextWrapping="Wrap" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 计数器：状态存在 code-behind 的字段里" />
            <Border Classes="stage" Padding="10">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Button Content="点我 +1" Click="OnCountClick" />
                    <TextBlock Name="CountText" Text="0 次" VerticalAlignment="Center" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="代价" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="状态和逻辑长在控件上，换一个界面就得重写，也无法脱离 UI 测试。功能点 3 的 MVVM 页演示如何把这两段逻辑搬出去。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.FundamentalsDemo/Views/Pages/CodeBehindPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class CodeBehindPage : UserControl
    {
        // State living on the control is exactly what the MVVM page moves out.
        private int _count;

        public CodeBehindPage()
        {
            InitializeComponent();
        }

        private void OnGreetClick(object? sender, RoutedEventArgs e)
        {
            var name = NameBox.Text;
            ResultText.Text = string.IsNullOrWhiteSpace(name)
                ? "请先输入名字"
                : $"你好，{name.Trim()}！";
        }

        private void OnCountClick(object? sender, RoutedEventArgs e)
        {
            _count++;
            CountText.Text = $"{_count} 次";
        }
    }
}
```

- [x] **Step 6: 创建 MvvmPage**

创建 `Avalonia.FundamentalsDemo/Views/Pages/MvvmPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.FundamentalsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.FundamentalsDemo.Views.Pages.MvvmPage"
             x:DataType="vm:MvvmViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="MVVM：把上一页的逻辑搬进 ViewModel"
                               DocPath="fundamentals/the-mvvm-pattern" />

            <TextBlock Classes="caption" Text="界面与上一页的第 1 组一模一样，但没有一行事件处理代码" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <TextBox Text="{Binding Input}" Watermark="你的名字" />
                    <!--  The command supplies both the action and its enabled state.  -->
                    <Button Command="{Binding GreetCommand}" Content="打招呼" />
                    <TextBlock Text="{Binding Greeting}" TextWrapping="Wrap" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="三层各自的职责" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap" Text="Model — 数据与业务规则，不知道界面存在" />
                    <TextBlock TextWrapping="Wrap" Text="ViewModel — 界面状态与命令，不引用任何控件类型" />
                    <TextBlock TextWrapping="Wrap" Text="View — 只做展示与绑定，不存状态" />
                    <TextBlock TextWrapping="Wrap" Opacity="0.75"
                               Text="判断 ViewModel 写对了没有：它的 using 里若出现 Avalonia.Controls，多半越界了。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="按钮为什么一开始是灰的" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="GreetCommand 的 CanExecute 要求输入非空。输入框一变化，[NotifyCanExecuteChangedFor] 就通知命令重新求值，按钮随之启用——这一步若漏写，按钮会一直保持初始状态。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.FundamentalsDemo/Views/Pages/MvvmPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.FundamentalsDemo.ViewModels;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class MvvmPage : UserControl
    {
        public MvvmPage()
        {
            InitializeComponent();
            DataContext = new MvvmViewModel();
        }
    }
}
```

- [x] **Step 7: 创建 TopLevelPage**

创建 `Avalonia.FundamentalsDemo/Views/Pages/TopLevelPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.FundamentalsDemo.Views.Pages.TopLevelPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TopLevel：桌面窗口与嵌入宿主的共同基类"
                               DocPath="fundamentals/top-level" />

            <TextBlock Classes="caption" Text="1. 本页所在 TopLevel 的运行时信息（拖动窗口到不同显示器可观察缩放变化）" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Name="TopLevelTypeText" />
                    <TextBlock Name="ScalingText" />
                    <TextBlock Name="ScreenText" />
                    <TextBlock Name="ServiceText" TextWrapping="Wrap" />
                    <Button Content="刷新" Click="OnRefreshClick" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 为什么要经由 TopLevel 取服务" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="剪贴板、文件选择器、屏幕信息都属于某一个顶层窗口，而不属于整个应用。用 TopLevel.GetTopLevel(control) 取当前控件所在的顶层，代码在桌面窗口、浏览器页面、移动端视图里都能跑。" />
            </Border>

            <TextBlock Classes="caption" Text="3. 启动窗口是怎么设置的" />
            <Border Classes="stage" Padding="10">
                <SelectableTextBlock TextWrapping="Wrap"
                                     Text="App.axaml.cs 的 OnFrameworkInitializationCompleted 里：if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow();&#10;桌面之外的生命周期（单视图）用 ISingleViewApplicationLifetime.MainView，那里没有窗口概念。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.FundamentalsDemo/Views/Pages/TopLevelPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class TopLevelPage : UserControl
    {
        public TopLevelPage()
        {
            InitializeComponent();
        }

        // The TopLevel only exists once the control is attached, so the first
        // read happens here rather than in the constructor.
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Describe();
        }

        private void OnRefreshClick(object? sender, RoutedEventArgs e) => Describe();

        private void Describe()
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null)
            {
                TopLevelTypeText.Text = "尚未附加到任何 TopLevel";
                return;
            }

            TopLevelTypeText.Text = $"TopLevel 实际类型：{top.GetType().Name}";
            ScalingText.Text = $"渲染缩放：{top.RenderScaling:F2}（客户区 {top.ClientSize.Width:F0}×{top.ClientSize.Height:F0}）";
            ScreenText.Text = $"检测到显示器：{top.Screens?.ScreenCount.ToString() ?? "不可用"} 台";
            ServiceText.Text = $"剪贴板：{Availability(top.Clipboard is not null)}；"
                + $"存储服务：{Availability(top.StorageProvider is not null)}；"
                + $"输入法面板：{Availability(top.InputPane is not null)}";
        }

        private static string Availability(bool available) => available ? "可用" : "本平台不提供";
    }
}
```

**为什么在 `OnAttachedToVisualTree` 里读**：构造函数执行时控件还没挂上视觉树，
`TopLevel.GetTopLevel(this)` 必然返回 `null`。这是 Avalonia 里"取宿主信息"的标准时机。

以上 API（`RenderScaling`、`ClientSize`、`Screens.ScreenCount`、`Clipboard`、
`StorageProvider`、`InputPane`、`OnAttachedToVisualTree` 签名）已在 12.1.2 编译验证。

- [x] **Step 8: 创建 TreesPage**

创建 `Avalonia.FundamentalsDemo/Views/Pages/TreesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.FundamentalsDemo.Models"
             xmlns:vm="using:Avalonia.FundamentalsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.FundamentalsDemo.Views.Pages.TreesPage"
             x:DataType="vm:TreesViewModel">

    <Grid Margin="12" RowDefinitions="Auto,Auto,Auto,*">
        <shared:DemoHeader Grid.Row="0"
                           Title="视觉树与逻辑树：同一个控件，两种父子关系"
                           DocPath="fundamentals/visual-and-logical-trees" />

        <TextBlock Grid.Row="1" Classes="caption"
                   Text="下面这个按钮是被观察的对象，点『生成快照』查看它展开成了什么" />

        <Border Grid.Row="2" Classes="stage" Padding="10">
            <StackPanel Spacing="8">
                <Button Name="SampleButton" Content="我是一个普通按钮" HorizontalAlignment="Left" />
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Button Content="生成快照" Click="OnSnapshotClick" />
                    <TextBlock Name="HintText" VerticalAlignment="Center" Opacity="0.75"
                               Text="逻辑树只有你写的那一层；视觉树里是控件模板展开后的每个零件。" />
                </StackPanel>
            </StackPanel>
        </Border>

        <Grid Grid.Row="3" ColumnDefinitions="*,*" Margin="0,8,0,0">
            <Border Grid.Column="0" Classes="stage" Margin="3" Padding="8">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Classes="caption" Margin="0,0,0,6"
                               Text="逻辑树 LogicalChildren" />
                    <TreeView ItemsSource="{Binding LogicalTree}">
                        <TreeView.ItemTemplate>
                            <TreeDataTemplate DataType="models:TreeNodeInfo" ItemsSource="{Binding Children}">
                                <TextBlock Text="{Binding Label}" />
                            </TreeDataTemplate>
                        </TreeView.ItemTemplate>
                    </TreeView>
                </DockPanel>
            </Border>

            <Border Grid.Column="1" Classes="stage" Margin="3" Padding="8">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Classes="caption" Margin="0,0,0,6"
                               Text="视觉树 GetVisualChildren" />
                    <TreeView ItemsSource="{Binding VisualTree}">
                        <TreeView.ItemTemplate>
                            <TreeDataTemplate DataType="models:TreeNodeInfo" ItemsSource="{Binding Children}">
                                <TextBlock Text="{Binding Label}" />
                            </TreeDataTemplate>
                        </TreeView.ItemTemplate>
                    </TreeView>
                </DockPanel>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

创建 `Avalonia.FundamentalsDemo/Views/Pages/TreesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.FundamentalsDemo.ViewModels;
using Avalonia.Interactivity;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class TreesPage : UserControl
    {
        private readonly TreesViewModel _viewModel = new();

        public TreesPage()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private void OnSnapshotClick(object? sender, RoutedEventArgs e)
        {
            // Snapshot after the template has expanded, otherwise the visual
            // tree side would show nothing below the button itself.
            _viewModel.Refresh(SampleButton);
            HintText.Text = "快照已生成——展开左右两棵树对比层级深度。";
        }
    }
}
```

**为什么用 `Grid` 而不是外层 `ScrollViewer`+`StackPanel`**：两个 `TreeView` 需要占满剩余
高度并各自滚动。放进 `StackPanel` 会让它们按内容无限伸展，外层滚动条接管一切，树本身
反而不能独立滚动。

- [x] **Step 9: 创建 LifetimesPage**

创建 `Avalonia.FundamentalsDemo/Views/Pages/LifetimesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.FundamentalsDemo.Views.Pages.LifetimesPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="应用生命周期与 Assets：当前进程跑在哪种生命周期下"
                               DocPath="fundamentals/application-lifetimes" />

            <TextBlock Classes="caption" Text="1. 运行时探测到的生命周期" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Name="LifetimeText" TextWrapping="Wrap" />
                    <TextBlock Name="WindowCountText" TextWrapping="Wrap" />
                    <Button Content="再开一个窗口" Click="OnOpenWindowClick" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 三种生命周期的区别" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap" Text="IClassicDesktopStyleApplicationLifetime — 桌面：有多窗口、有退出码、可控制何时退出" />
                    <TextBlock TextWrapping="Wrap" Text="ISingleViewApplicationLifetime — 移动端与浏览器：只有一个 MainView，没有窗口概念" />
                    <TextBlock TextWrapping="Wrap" Text="无生命周期 — 设计器预览与 headless 测试宿主里 ApplicationLifetime 为 null" />
                    <TextBlock TextWrapping="Wrap" Opacity="0.75"
                               Text="因此 OnFrameworkInitializationCompleted 里那个 is 判断不是防御性代码，是真的会走另一条分支。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. Assets：用 avares:// 加载嵌入资源" />
            <Border Classes="stage" Padding="10">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <!--  Loaded through the same avares scheme the code-behind uses.  -->
                    <Image Width="48" Height="48" Source="/Assets/avalonia-logo.ico" />
                    <StackPanel Spacing="4" VerticalAlignment="Center">
                        <TextBlock Name="AssetText" TextWrapping="Wrap" />
                        <TextBlock Opacity="0.75" TextWrapping="Wrap"
                                   Text="XAML 里写相对路径即可；代码里要用完整的 avares://程序集名/路径，并通过 AssetLoader.Open 读取。" />
                    </StackPanel>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.FundamentalsDemo/Views/Pages/LifetimesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using System;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class LifetimesPage : UserControl
    {
        public LifetimesPage()
        {
            InitializeComponent();
            DescribeLifetime();
            DescribeAsset();
        }

        private void DescribeLifetime()
        {
            LifetimeText.Text = Application.Current?.ApplicationLifetime switch
            {
                IClassicDesktopStyleApplicationLifetime => "当前：桌面生命周期（IClassicDesktopStyleApplicationLifetime）",
                ISingleViewApplicationLifetime => "当前：单视图生命周期（ISingleViewApplicationLifetime）",
                null => "当前：没有生命周期（设计器或测试宿主）",
                var other => $"当前：{other.GetType().Name}",
            };

            UpdateWindowCount();
        }

        private void UpdateWindowCount()
        {
            WindowCountText.Text =
                Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? $"已打开窗口数：{desktop.Windows.Count}"
                    : "非桌面生命周期，没有窗口列表";
        }

        private void OnOpenWindowClick(object? sender, RoutedEventArgs e)
        {
            var extra = new Window
            {
                Title = "多开的窗口",
                Width = 320,
                Height = 200,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Text = "桌面生命周期允许任意多个窗口。关掉它再点『刷新』，窗口数会变回去。",
                },
            };

            extra.Closed += (_, _) => UpdateWindowCount();
            extra.Show();
            UpdateWindowCount();
        }

        private void DescribeAsset()
        {
            // Assets are embedded in the assembly, addressed by the avares scheme.
            var uri = new Uri("avares://Avalonia.FundamentalsDemo/Assets/avalonia-logo.ico");
            using var stream = AssetLoader.Open(uri);
            AssetText.Text = $"已通过 AssetLoader 打开图标资源，共 {stream.Length} 字节。";
        }
    }
}
```

以上 API（`AssetLoader.Open`、`desktop.Windows.Count`、`ApplicationLifetime` 的模式匹配）
已在 12.1.2 编译验证。

- [x] **Step 10: 挂到 MainWindow 的 TabControl**

修改 `Avalonia.FundamentalsDemo/Views/MainWindow.axaml`——在根 `Window` 元素上补命名空间
声明：

```xml
        xmlns:pages="using:Avalonia.FundamentalsDemo.Views.Pages"
```

把空的 TabControl 替换为：

```xml
    <TabControl Margin="12">
        <TabItem Header="纯代码 UI">
            <pages:CodedUiPage />
        </TabItem>
        <TabItem Header="Code-behind">
            <pages:CodeBehindPage />
        </TabItem>
        <TabItem Header="MVVM">
            <pages:MvvmPage />
        </TabItem>
        <TabItem Header="TopLevel">
            <pages:TopLevelPage />
        </TabItem>
        <TabItem Header="视觉树与逻辑树">
            <pages:TreesPage />
        </TabItem>
        <TabItem Header="生命周期与资源">
            <pages:LifetimesPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 11: 构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -5`
Expected: `0 个错误`，且没有新增警告（既有警告清单见 Task 1 Step 7）。

- [x] **Step 12: 用 headless 探针断言页面行为**

**不要用目视核对代替这一步。** 样板阶段的教训：plan 里写着"核对列数依次为 1 → 2 → 4"，
执行时标记为通过，实际上那个演示在任何宽度下都是 1 列——目视在"整页看着是活的"时最容易
自我欺骗（详见 spec「样板阶段实测结论」）。

在**仓库外**建探针，跑完即弃。创建 `C:\Temp\fundcheck\fundcheck.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" Version="12.1.2" />
        <PackageReference Include="Avalonia.Headless" Version="12.1.2" />
        <PackageReference Include="Avalonia.Themes.Fluent" Version="12.1.2" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.FundamentalsDemo\Avalonia.FundamentalsDemo.csproj" />
    </ItemGroup>
</Project>
```

创建 `C:\Temp\fundcheck\Program.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.FundamentalsDemo.Views.Pages;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
    }
}

internal sealed class WarnSink : ILogSink
{
    public readonly List<string> Entries = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel l, string a, object? s, string m) => Entries.Add($"[{l}] ({a}) {m}");
    public void Log(LogEventLevel l, string a, object? s, string m, params object?[] v)
        => Entries.Add($"[{l}] ({a}) {m} :: {string.Join(", ", v)}");
}

internal static class Probe
{
    private static T Find<T>(Visual root, string name) where T : Visual
    {
        foreach (var d in root.GetVisualDescendants())
        {
            if (d is T hit && (d as Control)?.Name == name) return hit;
        }
        throw new InvalidOperationException($"not found: {name}");
    }

    private static void Layout(Control c)
    {
        var w = new Window { Content = c, Width = 900, Height = 700 };
        w.Show();
        w.Measure(new Size(900, 700));
        w.Arrange(new Rect(0, 0, 900, 700));
        Dispatcher.UIThread.RunJobs();
    }

    private static void Check(string label, bool ok, string detail)
        => Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-34} {detail}");

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        // CodedUiPage: the C# half must have produced the same three controls.
        var coded = new CodedUiPage();
        Layout(coded);
        var host = Find<Border>(coded, "CodeBuiltHost");
        var built = (host.Child as StackPanel)?.Children.Count ?? 0;
        Check("CodedUi: C# side built", built == 3, $"children={built} (expect 3)");

        // CodeBehindPage: the click handler must write into ResultText.
        var cb = new CodeBehindPage();
        Layout(cb);
        var box = Find<TextBox>(cb, "NameBox");
        var result = Find<TextBlock>(cb, "ResultText");
        box.Text = "小明";
        Find<Button>(cb, "GreetButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Check("CodeBehind: greeting updates", result.Text?.Contains("小明") == true, $"text={result.Text}");

        // MvvmPage: CanExecute must gate the button, and flip when input arrives.
        var mvvm = new MvvmPage();
        Layout(mvvm);
        var mvvmButton = mvvm.GetVisualDescendants().OfType<Button>().First();
        var before = mvvmButton.IsEffectivelyEnabled;
        mvvm.GetVisualDescendants().OfType<TextBox>().First().Text = "小红";
        Dispatcher.UIThread.RunJobs();
        Check("Mvvm: CanExecute gates button", !before && mvvmButton.IsEffectivelyEnabled,
            $"before={before} after={mvvmButton.IsEffectivelyEnabled}");

        // TreesPage: the visual tree must be strictly deeper than the logical one.
        var trees = new TreesPage();
        Layout(trees);
        var vm = (Avalonia.FundamentalsDemo.ViewModels.TreesViewModel)trees.DataContext!;
        vm.Refresh(Find<Button>(trees, "SampleButton"));
        static int Depth(Avalonia.FundamentalsDemo.Models.TreeNodeInfo n)
            => n.Children.Count == 0 ? 1 : 1 + n.Children.Max(Depth);
        var vd = Depth(vm.VisualTree[0]);
        var ld = Depth(vm.LogicalTree[0]);
        Check("Trees: visual deeper than logical", vd > ld, $"visual={vd} logical={ld}");

        // LifetimesPage: AssetLoader must have found the embedded icon.
        var life = new LifetimesPage();
        Layout(life);
        var assetText = Find<TextBlock>(life, "AssetText").Text ?? "";
        Check("Lifetimes: asset loaded", assetText.Contains("字节"), assetText);

        // TopLevelPage: attaching must populate the scaling readout.
        var tl = new TopLevelPage();
        Layout(tl);
        var scaling = Find<TextBlock>(tl, "ScalingText").Text ?? "";
        Check("TopLevel: scaling read", scaling.Contains("渲染缩放"), scaling);

        Console.WriteLine($"\nwarning-or-worse log entries: {sink.Entries.Count}");
        foreach (var e in sink.Entries.Distinct()) Console.WriteLine("  " + e);
    }
}
```

Run: `dotnet run --project C:\Temp\fundcheck\fundcheck.csproj`

Expected: 六行全部 `PASS`，且最后一行 `warning-or-worse log entries: 0`。

任何一行 `FAIL` 或出现 binding warning 都要先修好再提交——**不要**把 FAIL 解释成
"探针写得不对"就跳过。若确认是探针本身的问题（例如控件名拼错），修探针后重跑。

- [x] **Step 13: 清理探针并提交**

```bash
rm -rf /c/Temp/fundcheck
git add Avalonia.FundamentalsDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Fundamentals category

Six pages covering coded UI, code-behind, MVVM, TopLevel, the two trees
and application lifetimes. The code-behind and MVVM pages render the same
interface, so the difference between them is the code, not the result.

Verified with a throwaway headless probe: the C# half of the coded-UI page
builds three controls, the greeting handler fires, CanExecute gates the
MVVM button, the visual tree comes out deeper than the logical one, and
the asset loads. No binding warnings.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: 项目 #2 XAML Reference 的 6 个页面

**Files:**
- Create: `Avalonia.XamlDemo/Models/PriorityLevel.cs`
- Create: `Avalonia.XamlDemo/MarkupExtensions/RepeatTextExtension.cs`
- Create: `Avalonia.XamlDemo/Views/Pages/NamespacesPage.axaml(.cs)`
- Create: `Avalonia.XamlDemo/Views/Pages/DirectivesPage.axaml(.cs)`
- Create: `Avalonia.XamlDemo/Views/Pages/MarkupExtensionsPage.axaml(.cs)`
- Create: `Avalonia.XamlDemo/Views/Pages/TypeConvertersPage.axaml(.cs)`
- Create: `Avalonia.XamlDemo/Views/Pages/GenericsPage.axaml(.cs)`
- Create: `Avalonia.XamlDemo/Views/Pages/CompilationPage.axaml(.cs)`
- Modify: `Avalonia.XamlDemo/Views/MainWindow.axaml`

**Interfaces:**
- Consumes: Task 1 的 `MainWindow` 外壳；`Avalonia.Shared.Controls.DemoHeader`
- Produces: 六个无参构造的 `UserControl`。
  `Avalonia.XamlDemo.Models.PriorityLevel` 为 `enum { Low, Normal, High }`；
  `Avalonia.XamlDemo.Models.DemoConstants` 提供 `const string AppTitle`；
  `Avalonia.XamlDemo.Models.PriorityList : List<PriorityLevel>`；
  `RepeatTextExtension` 含 `string Text`、`int Count`、`object ProvideValue(IServiceProvider)`。

### 本任务的实测结论（写 plan 时已在 12.1.2 上跑过）

下面这些形态都经过带 XAML 编译的 headless 探针验证，可直接使用：

| 写法 | 实测结果 |
|---|---|
| `{x:Static local:DemoConstants.AppTitle}` → `Text` | 正常，输出常量值 |
| `{x:Static local:PriorityLevel.High}` → `Text` | **编译失败 `AVLN3000`** |
| `{Binding Source={x:Static local:PriorityLevel.High}}` → `Text` | 正常，输出 `High` |
| `{Binding Source={x:Type Button}}` → `Text` | **编译失败 `AVLN2100`**，加 `Path=` 也不行 |
| `{local:RepeatText 喵, Count=3}` | 正常，输出 `喵喵喵` |
| `<gen:List x:TypeArguments="sys:String">` | 正常，`ItemsSource` 拿到 2 个元素 |
| `<local:PriorityList>` 内嵌 `<local:PriorityLevel>Low</...>` | 正常，集合拿到 2 个枚举元素 |
| `<sys:Double x:Key="BigFont">20</sys:Double>` → `FontSize` | 正常，字号为 20 |
| `{Binding #SourceBox.Text}` | 正常，无需 `x:DataType` |
| `StringFormat='{}{Binding} …{0:F0}'`（单层花括号） | **静默失败，文本为空** |
| `StringFormat='{}{{Binding}} …{0:F0}'`（双写） | 正常，输出 `{Binding} …42` |
| `Text="{}{以大括号开头的字面量}"` | 正常 |
| `Background="#FF3366"` / `BorderThickness="1,2,3,4"` / `CornerRadius="5"` | 正常，分别转成 `#ffff3366`、`1,2,3,4`、`5,5,5,5` |

**三条实测发现，前两条响亮、第三条静默**：

- `{x:Static}` 返回**枚举**、目标属性是 `string` 时，编译型 XAML **不做隐式 `ToString()`**，
  报 `AVLN3000` 并列出它能接受的类型。包一层 `{Binding Source=...}` 即可——绑定管线里有
  转换步骤。
- `{x:Type}` **不能当 `Binding` 的 `Source`**，报 `AVLN2100` 要求 `x:DataType`。
  对照之下 `{x:Static}` 作 `Source` 却能通过，差别在于其返回值类型编译期可知。
  `x:Type` 的正当位置是 `ControlTheme` 的 `x:Key`/`TargetType`。
- **`StringFormat` 里的字面花括号必须双写。** `{}` 转义只对整个属性值的开头有效；
  `StringFormat` 内部走 .NET 复合格式化，单层 `{Binding}` 会被当成占位符，解析失败后
  **静默产出空字符串**——编译通过、运行无异常、无日志，只有文本莫名其妙不见了。
  这条与样板阶段的两条规则同属一类，实现时务必用 headless 断言文本非空。

前两条是编译期失败，改不对就构建不过；第三条才是危险的那个。**XAML 的类型装配是响亮
失败的，值的流转（绑定、格式化、样式优先级）才是静默失败的重灾区。**

- [x] **Step 1: 创建模型与常量**

创建 `Avalonia.XamlDemo/Models/PriorityLevel.cs`：

```csharp
using System.Collections.Generic;

namespace Avalonia.XamlDemo.Models
{
    public enum PriorityLevel
    {
        Low,
        Normal,
        High,
    }

    /// <summary>Static members reachable from XAML through x:Static.</summary>
    public static class DemoConstants
    {
        public const string AppTitle = "XAML 参考演示";

        public static readonly PriorityLevel DefaultPriority = PriorityLevel.Normal;
    }

    /// <summary>
    /// A closed generic type. XAML can also spell the open form with
    /// x:TypeArguments; this named subclass is the alternative.
    /// </summary>
    public sealed class PriorityList : List<PriorityLevel>
    {
    }
}
```

- [x] **Step 2: 创建自定义标记扩展**

创建 `Avalonia.XamlDemo/MarkupExtensions/RepeatTextExtension.cs`：

```csharp
using Avalonia.Metadata;
using System;
using System.Linq;

namespace Avalonia.XamlDemo.MarkupExtensions
{
    /// <summary>
    /// Repeats a string. The point is the shape, not the feature: any class
    /// with a ProvideValue method can be used as {local:RepeatText ...}.
    /// The "Extension" suffix is optional in the XAML usage.
    /// </summary>
    public sealed class RepeatTextExtension
    {
        public RepeatTextExtension()
        {
        }

        public RepeatTextExtension(string text) => Text = text;

        // Marks which property the positional argument fills.
        [ConstructorArgument("text")]
        public string Text { get; set; } = string.Empty;

        public int Count { get; set; } = 2;

        public object ProvideValue(IServiceProvider serviceProvider)
            => string.Concat(Enumerable.Repeat(Text, Math.Max(1, Count)));
    }
}
```

- [x] **Step 3: 创建 NamespacesPage**

创建 `Avalonia.XamlDemo/Views/Pages/NamespacesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:sys="using:System"
             xmlns:models="using:Avalonia.XamlDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.XamlDemo.Views.Pages.NamespacesPage">

    <UserControl.Resources>
        <!--  Types from other namespaces need a prefix before they can be named.  -->
        <sys:String x:Key="PlainString">来自 System 命名空间的字符串</sys:String>
        <models:PriorityList x:Key="Priorities">
            <models:PriorityLevel>Low</models:PriorityLevel>
            <models:PriorityLevel>High</models:PriorityLevel>
        </models:PriorityList>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="XAML 命名空间：每个前缀对应一个 CLR 命名空间"
                               DocPath="xaml/namespaces" />

            <TextBlock Classes="caption" Text="1. 本页文件头声明的五个前缀" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap" Text="默认（无前缀）https://github.com/avaloniaui — Avalonia 的全部控件" />
                    <TextBlock TextWrapping="Wrap" Text="x: — XAML 语言本身的指令，如 x:Class、x:Key、x:Name" />
                    <TextBlock TextWrapping="Wrap" Text="d: 与 mc: — 设计时专用；mc:Ignorable 让运行时忽略 d: 前缀" />
                    <TextBlock TextWrapping="Wrap" Text="sys: 与 models: — 用 using: 语法指向 CLR 命名空间" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. using: 语法引入的类型可以直接实例化" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Text="{DynamicResource PlainString}" />
                    <ItemsControl ItemsSource="{DynamicResource Priorities}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="8" />
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                    </ItemsControl>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 引用其它程序集的类型" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <!--  DemoHeader itself comes from Avalonia.Shared, declared as shared: above.  -->
                    <TextBlock TextWrapping="Wrap"
                               Text="本页顶部那条说明条就来自另一个程序集：xmlns:shared=&quot;using:Avalonia.Shared.Controls&quot;。" />
                    <TextBlock TextWrapping="Wrap" Opacity="0.75"
                               Text="跨程序集时 using: 后面写命名空间即可，Avalonia 会在已引用的程序集里查找；只有同名命名空间冲突时才需要 assembly= 限定。" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.XamlDemo/Views/Pages/NamespacesPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class NamespacesPage : UserControl
    {
        public NamespacesPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 4: 创建 DirectivesPage**

创建 `Avalonia.XamlDemo/Views/Pages/DirectivesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:sys="using:System"
             xmlns:models="using:Avalonia.XamlDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.XamlDemo.Views.Pages.DirectivesPage">

    <UserControl.Resources>
        <!--  x:Key is what makes a resource addressable.  -->
        <sys:Double x:Key="BigFont">20</sys:Double>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="x: 指令：XAML 语言自身的关键字"
                               DocPath="xaml/directives" />

            <TextBlock Classes="caption" Text="1. x:Class — 把这个 XAML 文件和一个 C# 分部类绑定起来" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="本文件根元素上写着 x:Class=&quot;Avalonia.XamlDemo.Views.Pages.DirectivesPage&quot;，编译器据此生成 InitializeComponent，code-behind 才能是 partial class。" />
            </Border>

            <TextBlock Classes="caption" Text="2. x:Name — 生成字段，让 code-behind 和绑定都能找到这个元素" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <TextBox Name="SourceBox" Text="改我试试" />
                    <!--  #SourceBox is element-name binding; it needs no DataContext.  -->
                    <TextBlock Text="{Binding #SourceBox.Text, StringFormat='元素名绑定读到：{0}'}" />
                    <Button Content="从 code-behind 读取" Click="OnReadClick" HorizontalAlignment="Left" />
                    <TextBlock Name="ReadResult" Opacity="0.75" Text="（还没读）" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. x:Key — 资源字典里的索引键" />
            <Border Classes="stage" Padding="10">
                <TextBlock FontSize="{DynamicResource BigFont}" Text="这行字的字号来自资源 BigFont" />
            </Border>

            <TextBlock Classes="caption" Text="4. x:Static — 读取静态成员" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Text="{x:Static models:DemoConstants.AppTitle}" />
                    <!--
                        A compiled-XAML gotcha: x:Static returning an enum cannot fill a
                        string property directly — the build fails with AVLN3000. Routing
                        it through a binding gives the value a conversion step.
                    -->
                    <TextBlock Text="{Binding Source={x:Static models:PriorityLevel.High}, StringFormat='枚举要包一层 Binding：{0}'}" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="5. x:Type — 取类型对象而不是实例" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Name="TypeText" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="x:Type 的日常位置是 ControlTheme 的 x:Key 与 TargetType（见 Avalonia.Shared/Controls/DemoHeader.axaml）。它不能直接当 Binding 的 Source——编译型 XAML 会要求 x:DataType，报 AVLN2100。" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.XamlDemo/Views/Pages/DirectivesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class DirectivesPage : UserControl
    {
        public DirectivesPage()
        {
            InitializeComponent();

            // The C# equivalent of x:Type. Shown here because a Binding cannot
            // take {x:Type} as its Source under compiled bindings.
            TypeText.Text = $"typeof(Button) 得到：{typeof(Button).FullName}";
        }

        private void OnReadClick(object? sender, RoutedEventArgs e)
        {
            // SourceBox is the field x:Name generated.
            ReadResult.Text = $"code-behind 读到：{SourceBox.Text}";
        }
    }
}
```

**实测补充**：`{Binding Source={x:Type Button}}` 在 `AvaloniaUseCompiledBindingsByDefault=true`
下编译失败（`AVLN2100`，要求 `x:DataType`），加 `Path=` 也不行。而
`{Binding Source={x:Static ...}}` 却能通过——两者都带 `Source=`，差别在于 `x:Static`
的返回值类型编译期可知。所以 `x:Type` 在本页用 code-behind 展示，XAML 里指向它的
真实用途（`ControlTheme.TargetType`）。

- [x] **Step 5: 创建 MarkupExtensionsPage**

创建 `Avalonia.XamlDemo/Views/Pages/MarkupExtensionsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:ext="using:Avalonia.XamlDemo.MarkupExtensions"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.XamlDemo.Views.Pages.MarkupExtensionsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="标记扩展：花括号里的都是它"
                               DocPath="xaml/markup-extensions" />

            <TextBlock Classes="caption" Text="1. 内置标记扩展" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <!--
                        Braces meant as literals inside a StringFormat must be doubled:
                        a single {Binding} is parsed as a composite-format placeholder,
                        fails, and silently yields an empty string.
                    -->
                    <TextBlock Text="{Binding #Slider.Value, StringFormat='{}{{Binding}} 读另一个元素：{0:F0}'}" />
                    <Slider Name="Slider" Minimum="0" Maximum="100" Value="42" />
                    <TextBlock Foreground="{DynamicResource SystemAccentColor}"
                               Text="{}{DynamicResource} — 这行字的颜色随主题变化，换主题会重新求值" />
                    <TextBlock Text="{}{StaticResource} — 只在加载时求值一次，之后不再跟随" />
                    <TextBlock Text="{OnPlatform Windows='{}{OnPlatform} — 运行于 Windows', macOS='运行于 macOS', Default='运行于其它平台'}" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 自定义标记扩展：任何带 ProvideValue 的类都可以" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <!--  Positional argument fills Text; Count is a named property.  -->
                    <TextBlock Text="{ext:RepeatText 喵}" />
                    <TextBlock Text="{ext:RepeatText 汪, Count=5}" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="类名以 Extension 结尾时，XAML 里可以省略这个后缀：RepeatTextExtension 写成 {ext:RepeatText ...}。第一个位置参数由 [ConstructorArgument] 指明对应哪个属性。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 大括号的转义：两种场合，两套规则" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Text="{}{这段文字以大括号开头，前面的 {} 是转义标记}" />
                    <TextBlock Text="{Binding #Slider.Value, StringFormat='{}{{双写}} 才是 StringFormat 里的字面括号：{0:F0}'}" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="属性值以 { 开头时，前置一对空的 {} 告诉解析器这不是标记扩展。但 StringFormat 内部走的是 .NET 复合格式化，那里的字面括号必须双写成 {{ }}——只写单层会被当成占位符，解析失败后静默产出空字符串，既不报错也无日志。" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.XamlDemo/Views/Pages/MarkupExtensionsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class MarkupExtensionsPage : UserControl
    {
        public MarkupExtensionsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 6: 创建 TypeConvertersPage**

创建 `Avalonia.XamlDemo/Views/Pages/TypeConvertersPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.XamlDemo.Views.Pages.TypeConvertersPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="类型转换器：XAML 里写的都是字符串"
                               DocPath="xaml/type-converters" />

            <TextBlock Classes="caption" Text="1. 同一个 Border，六个属性各自走了不同的转换器" />
            <Border Classes="stage" Padding="10">
                <StackPanel Orientation="Horizontal" Spacing="16">
                    <Border Name="Sample"
                            Background="#FF3366"
                            BorderBrush="White"
                            BorderThickness="1,2,3,4"
                            CornerRadius="8"
                            Width="140"
                            Height="70" />
                    <StackPanel Name="ReadoutHost" Spacing="4" VerticalAlignment="Center" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 常见的简写形式" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap" Text="Thickness：&quot;4&quot; 四边相同；&quot;4,8&quot; 横向/纵向；&quot;1,2,3,4&quot; 左上右下" />
                    <TextBlock TextWrapping="Wrap" Text="CornerRadius：同上，但四个值的顺序是左上、右上、右下、左下" />
                    <TextBlock TextWrapping="Wrap" Text="Brush：颜色名、#RGB、#RRGGBB、#AARRGGBB 都接受" />
                    <TextBlock TextWrapping="Wrap" Text="GridLength：&quot;Auto&quot;、&quot;*&quot;、&quot;2*&quot;、&quot;120&quot;" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 转换器不存在时会怎样" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="XAML 属性里写字面量走的是编译期转换，缺转换器会直接编译失败。但绑定不同：把 double 绑到 Thickness 没有可用转换，运行时只记一条 binding warning，界面纹丝不动——本仓库的 Avalonia.Shared/Converters/DoubleToThicknessConverter.cs 就是为此而生。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.XamlDemo/Views/Pages/TypeConvertersPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class TypeConvertersPage : UserControl
    {
        public TypeConvertersPage()
        {
            InitializeComponent();

            // Read back what the string literals actually became.
            foreach (var line in new[]
                     {
                         $"Background \"#FF3366\" → {Sample.Background}",
                         $"BorderBrush \"White\" → {Sample.BorderBrush}",
                         $"BorderThickness \"1,2,3,4\" → {Sample.BorderThickness}",
                         $"CornerRadius \"8\" → {Sample.CornerRadius}",
                         $"Width \"140\" → {Sample.Width} ({Sample.Width.GetType().Name})",
                     })
            {
                ReadoutHost.Children.Add(new TextBlock { Text = line });
            }
        }
    }
}
```

- [x] **Step 7: 创建 GenericsPage**

创建 `Avalonia.XamlDemo/Views/Pages/GenericsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:sys="using:System"
             xmlns:gen="using:System.Collections.Generic"
             xmlns:models="using:Avalonia.XamlDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.XamlDemo.Views.Pages.GenericsPage">

    <UserControl.Resources>
        <!--  Open generic closed at the XAML level.  -->
        <gen:List x:TypeArguments="sys:String" x:Key="Names">
            <sys:String>甲</sys:String>
            <sys:String>乙</sys:String>
            <sys:String>丙</sys:String>
        </gen:List>

        <!--  The alternative: a named subclass that is already closed.  -->
        <models:PriorityList x:Key="Priorities">
            <models:PriorityLevel>Low</models:PriorityLevel>
            <models:PriorityLevel>Normal</models:PriorityLevel>
            <models:PriorityLevel>High</models:PriorityLevel>
        </models:PriorityList>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="XAML 泛型：x:TypeArguments 与具名子类两条路"
                               DocPath="xaml/generics" />

            <TextBlock Classes="caption" Text="1. x:TypeArguments 在 XAML 里闭合开放泛型" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <ItemsControl ItemsSource="{DynamicResource Names}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="10" />
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                    </ItemsControl>
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="写法：&lt;gen:List x:TypeArguments=&quot;sys:String&quot;&gt;。类型实参本身也要带命名空间前缀。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 具名子类：把闭合动作放在 C# 里" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <ItemsControl ItemsSource="{DynamicResource Priorities}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="10" />
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                    </ItemsControl>
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="PriorityList : List&lt;PriorityLevel&gt; 在 XAML 里就是个普通类型，不需要 x:TypeArguments。多处复用同一个闭合类型时这样更省事。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 泛型控件怎么写" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="控件类型同样可以用 x:TypeArguments 实例化，但 Avalonia 内置控件几乎都是非泛型的——泛型主要出现在集合与 ViewModel 上。若自定义了泛型控件，根元素的 x:Class 那一侧需要用具名子类，因为 x:Class 不支持类型实参。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.XamlDemo/Views/Pages/GenericsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class GenericsPage : UserControl
    {
        public GenericsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 8: 创建 CompilationPage**

创建 `Avalonia.XamlDemo/Views/Pages/CompilationPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.XamlDemo.Views.Pages.CompilationPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="XAML 编译：本项目开了编译型绑定，代价与收益"
                               DocPath="xaml/compilation" />

            <TextBlock Classes="caption" Text="1. 本程序集的编译设置" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Name="CompiledFlagText" TextWrapping="Wrap" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="AvaloniaUseCompiledBindingsByDefault=true 写在 .csproj 里。单个绑定可用 x:CompileBindings 局部覆盖。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 编译型绑定拦下了什么" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap" Text="属性名写错 → 编译期报错，而不是运行时悄悄绑不上" />
                    <TextBlock TextWrapping="Wrap" Text="类型不匹配 → 编译期报错（如 x:Static 返回枚举填 string，报 AVLN3000）" />
                    <TextBlock TextWrapping="Wrap" Text="缺 x:DataType → 编译期报错 AVLN2100，逼你声明数据类型" />
                    <TextBlock TextWrapping="Wrap" Text="性能上少了一次反射查找" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 它拦不住什么" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap" Text="值在运行时转换失败 — 只记一条 binding warning，界面不动" />
                    <TextBlock TextWrapping="Wrap" Text="元素上的本地值压过样式 Setter — 连日志都没有" />
                    <TextBlock TextWrapping="Wrap" Text="StringFormat 里的花括号写错 — 静默产出空串" />
                    <TextBlock TextWrapping="Wrap" Opacity="0.75"
                               Text="所以「构建 0 错误」永远不等于「界面是对的」。这三类都要靠运行时读回属性值才能发现。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="4. 元素名绑定不需要 x:DataType" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBox Name="Probe" Text="随便改" />
                    <TextBlock Text="{Binding #Probe.Text, StringFormat='读到：{0}'}" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="#name 形式在编译期就能确定目标元素的类型，不依赖 DataContext，因此本页没有声明 x:DataType 也能编译。" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.XamlDemo/Views/Pages/CompilationPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using System.Linq;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class CompilationPage : UserControl
    {
        public CompilationPage()
        {
            InitializeComponent();

            // Compiled XAML turns each .axaml into generated code in this same
            // assembly, so the populate methods show up as compiler-generated types.
            var assembly = typeof(CompilationPage).Assembly;
            var xamlTypes = assembly.GetTypes()
                .Count(t => t.Namespace?.StartsWith("CompiledAvaloniaXaml") == true);

            CompiledFlagText.Text =
                $"本程序集：{assembly.GetName().Name}，"
                + $"编译期生成的 XAML 类型 {xamlTypes} 个。"
                + "若关闭编译型 XAML，这些类型不会存在，界面改由运行时解析器现场构建。";
        }
    }
}
```

**注意**：不要用 `BindingPlugins.PropertyAccessors` 之类的 API 来做这个展示——
它在 12.1.2 中不是公开类型（实测 `CS0122`）。上面这种反射程序集自身的写法不依赖内部 API，
已实测可用：开启编译型 XAML 的程序集里确实存在 `CompiledAvaloniaXaml` 命名空间。

- [x] **Step 9: 挂到 MainWindow 的 TabControl**

修改 `Avalonia.XamlDemo/Views/MainWindow.axaml`——在根 `Window` 元素上补命名空间声明：

```xml
        xmlns:pages="using:Avalonia.XamlDemo.Views.Pages"
```

把空的 TabControl 替换为：

```xml
    <TabControl Margin="12">
        <TabItem Header="命名空间">
            <pages:NamespacesPage />
        </TabItem>
        <TabItem Header="x: 指令">
            <pages:DirectivesPage />
        </TabItem>
        <TabItem Header="标记扩展">
            <pages:MarkupExtensionsPage />
        </TabItem>
        <TabItem Header="类型转换器">
            <pages:TypeConvertersPage />
        </TabItem>
        <TabItem Header="泛型">
            <pages:GenericsPage />
        </TabItem>
        <TabItem Header="XAML 编译">
            <pages:CompilationPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 10: 构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -5`
Expected: `0 个错误`，无新增警告。

若报 `AVLN3000` 或 `AVLN2100`，对照本任务开头的实测结论表——多半是某处把 `{x:Static}`
枚举或 `{x:Type}` 直接填进了字符串属性。

- [x] **Step 11: 用 headless 探针断言页面行为**

把 Task 2 Step 12 的探针工程复制一份到 `C:\Temp\xamlcheck`，`ProjectReference` 改指
`Avalonia.XamlDemo`，`Program.cs` 的断言部分替换为：

```csharp
        // NamespacesPage: the typed collection resource must have materialised.
        var ns = new NamespacesPage();
        Layout(ns);
        var priorities = ns.GetVisualDescendants().OfType<ItemsControl>().First().ItemsSource;
        Check("Namespaces: typed resource", priorities?.Cast<object>().Count() == 2,
            $"count={priorities?.Cast<object>().Count()}");

        // DirectivesPage: x:Static, x:Key and element-name binding all resolve.
        var dir = new DirectivesPage();
        Layout(dir);
        var typeText = Find<TextBlock>(dir, "TypeText").Text ?? "";
        Check("Directives: x:Type readout", typeText.Contains("Button"), typeText);
        Find<TextBox>(dir, "SourceBox").Text = "新值";
        Dispatcher.UIThread.RunJobs();
        var echoed = dir.GetVisualDescendants().OfType<TextBlock>()
            .Any(t => t.Text?.Contains("新值") == true);
        Check("Directives: element-name binding", echoed, $"echoed={echoed}");

        // MarkupExtensionsPage: every TextBlock must be non-empty.
        // An empty one means a StringFormat brace was written single instead of doubled.
        var mx = new MarkupExtensionsPage();
        Layout(mx);
        var blanks = mx.GetVisualDescendants().OfType<TextBlock>()
            .Where(t => string.IsNullOrEmpty(t.Text)).Count();
        Check("MarkupExtensions: no blank text", blanks == 0, $"blank TextBlocks={blanks}");

        // TypeConvertersPage: the readout lines must have been generated.
        var tc = new TypeConvertersPage();
        Layout(tc);
        var lines = Find<StackPanel>(tc, "ReadoutHost").Children.Count;
        Check("TypeConverters: readout built", lines == 5, $"lines={lines}");

        // GenericsPage: both the x:TypeArguments list and the named subclass.
        var gen = new GenericsPage();
        Layout(gen);
        var lists = gen.GetVisualDescendants().OfType<ItemsControl>()
            .Select(i => i.ItemsSource?.Cast<object>().Count() ?? 0).ToList();
        Check("Generics: both collections", lists.Count == 2 && lists[0] == 3 && lists[1] == 3,
            $"counts=[{string.Join(", ", lists)}]");

        // CompilationPage: the generated-type count must be non-zero.
        var comp = new CompilationPage();
        Layout(comp);
        var flag = Find<TextBlock>(comp, "CompiledFlagText").Text ?? "";
        Check("Compilation: generated types found", !flag.Contains("XAML 类型 0 个"), flag);
```

**`MarkupExtensions: no blank text` 这条是本任务最关键的断言**：空文本正是
`StringFormat` 花括号写错的唯一表征，而它不报错、不记日志。

Run: `dotnet run --project C:\Temp\xamlcheck\xamlcheck.csproj`

Expected: 六行全部 `PASS`，`warning-or-worse log entries: 0`。

- [x] **Step 12: 清理探针并提交**

```bash
rm -rf /c/Temp/xamlcheck
git add Avalonia.XamlDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the XAML Reference category

Six pages covering namespaces, x: directives, markup extensions, type
converters, generics and compilation. Three compiled-XAML traps are
demonstrated rather than described: an enum from x:Static cannot fill a
string property, x:Type cannot be a binding Source, and a single brace
inside StringFormat silently yields an empty string.

Verified with a throwaway headless probe, which asserts among other things
that no TextBlock on the markup-extensions page renders empty.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: 项目 #7 Property System 的 5 个页面

**Files:**
- Create: `Avalonia.PropertySystemDemo/Controls/GaugeControl.cs`
- Create: `Avalonia.PropertySystemDemo/Controls/GaugeControl.axaml`
- Create: `Avalonia.PropertySystemDemo/Controls/HighlightBehavior.cs`
- Create: `Avalonia.PropertySystemDemo/ViewModels/PrecedenceViewModel.cs`
- Create: `Avalonia.PropertySystemDemo/Views/Pages/StyledPropertyPage.axaml(.cs)`
- Create: `Avalonia.PropertySystemDemo/Views/Pages/DirectPropertyPage.axaml(.cs)`
- Create: `Avalonia.PropertySystemDemo/Views/Pages/AttachedPropertyPage.axaml(.cs)`
- Create: `Avalonia.PropertySystemDemo/Views/Pages/PrecedencePage.axaml(.cs)`
- Create: `Avalonia.PropertySystemDemo/Views/Pages/MetadataPage.axaml(.cs)`
- Modify: `Avalonia.PropertySystemDemo/App.axaml`（引入 `GaugeControl` 的 ControlTheme）
- Modify: `Avalonia.PropertySystemDemo/Views/MainWindow.axaml`

**Interfaces:**
- Consumes: Task 1 的 `MainWindow` 外壳与 `App.axaml`；`Avalonia.Shared.Controls.DemoHeader`；
  `Avalonia.Shared.ViewModels.ViewModelBase`
- Produces: `GaugeControl`（`StyledProperty<double> Value`、`StyledProperty<string?> Caption`、
  `DirectProperty<GaugeControl, string> Readout`）；
  `HighlightBehavior.IsHighlightedProperty`（`AttachedProperty<bool>`）与
  `HighlightBehavior.TagLineProperty`（`AttachedProperty<string?>`，`inherits: true`）；
  `PrecedenceViewModel` 含 `double BoundWidth`；五个无参构造的 `UserControl`。

### 本任务的实测结论（写 plan 时已在 12.1.2 上跑过）

| 行为 | 实测结果 |
|---|---|
| `coerce: (_, v) => Math.Clamp(v, 0, 100)`，赋 150 | 得 `100` |
| 同上，赋 −20 | 得 `0` |
| `RegisterAttached<Control, string?>(..., inherits: true)` | 值沿视觉树向下传递三层，全部读到 |
| `AttachedProperty<bool>` 默认值 | `False`，`SetIsHighlighted(btn, true)` 后为 `True` |
| `Button { Width = 111 }` + 样式 Setter `Width = 999` | **得 `111`** |

最后一行就是样板阶段那个缺陷的最小复现，本任务的 `PrecedencePage` 把它做成可交互演示。

- [x] **Step 1: 创建 GaugeControl**

创建 `Avalonia.PropertySystemDemo/Controls/GaugeControl.cs`：

```csharp
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.PropertySystemDemo.Controls
{
    /// <summary>
    /// One control carrying all three property kinds, so the pages can point
    /// at the same object and talk about different registration styles.
    /// </summary>
    public class GaugeControl : TemplatedControl
    {
        /// <summary>
        /// A styled property: reachable from styles, and able to coerce.
        /// The coerce callback runs on every write, whatever the source.
        /// </summary>
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<GaugeControl, double>(
                nameof(Value),
                defaultValue: 0d,
                coerce: static (_, v) => Math.Clamp(v, 0d, 100d));

        public static readonly StyledProperty<string?> CaptionProperty =
            AvaloniaProperty.Register<GaugeControl, string?>(nameof(Caption));

        private string _readout = "0%";

        /// <summary>
        /// A direct property: a plain CLR field with change notification bolted
        /// on. Cheaper to read than a styled property, but styles cannot set it.
        /// </summary>
        public static readonly DirectProperty<GaugeControl, string> ReadoutProperty =
            AvaloniaProperty.RegisterDirect<GaugeControl, string>(
                nameof(Readout),
                o => o._readout);

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string? Caption
        {
            get => GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public string Readout
        {
            get => _readout;
            private set => SetAndRaise(ReadoutProperty, ref _readout, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // The change callback is where a derived value gets recomputed.
            if (change.Property == ValueProperty)
            {
                Readout = $"{Value:F0}%";
            }
        }
    }
}
```

创建 `Avalonia.PropertySystemDemo/Controls/GaugeControl.axaml`：

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:controls="using:Avalonia.PropertySystemDemo.Controls">

    <ControlTheme x:Key="{x:Type controls:GaugeControl}" TargetType="controls:GaugeControl">
        <Setter Property="Template">
            <ControlTemplate TargetType="controls:GaugeControl">
                <StackPanel Spacing="4">
                    <TextBlock FontWeight="SemiBold" Text="{TemplateBinding Caption}" />
                    <Border Background="#20FFFFFF" CornerRadius="3" Height="18">
                        <!--  Width driven by Value through the ProgressBar-like fill.  -->
                        <ProgressBar Maximum="100"
                                     Minimum="0"
                                     Value="{TemplateBinding Value}" />
                    </Border>
                    <TextBlock FontSize="12" Opacity="0.75" Text="{TemplateBinding Readout}" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
</ResourceDictionary>
```

- [x] **Step 2: 创建附加属性宿主**

创建 `Avalonia.PropertySystemDemo/Controls/HighlightBehavior.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.PropertySystemDemo.Controls
{
    /// <summary>
    /// Attached properties let one type add state to controls it does not own.
    /// The owner here is a static class that is never instantiated.
    /// </summary>
    public static class HighlightBehavior
    {
        /// <summary>Non-inheriting: each control carries its own value.</summary>
        public static readonly AttachedProperty<bool> IsHighlightedProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                "IsHighlighted", typeof(HighlightBehavior));

        /// <summary>
        /// Inheriting: setting it on an ancestor makes every descendant read
        /// the same value, without any of them declaring it.
        /// </summary>
        public static readonly AttachedProperty<string?> TagLineProperty =
            AvaloniaProperty.RegisterAttached<Control, string?>(
                "TagLine", typeof(HighlightBehavior), inherits: true);

        public static bool GetIsHighlighted(Control target)
            => target.GetValue(IsHighlightedProperty);

        public static void SetIsHighlighted(Control target, bool value)
            => target.SetValue(IsHighlightedProperty, value);

        public static string? GetTagLine(Control target)
            => target.GetValue(TagLineProperty);

        public static void SetTagLine(Control target, string? value)
            => target.SetValue(TagLineProperty, value);
    }
}
```

- [x] **Step 3: 在 App.axaml 里引入 GaugeControl 的主题**

修改 `Avalonia.PropertySystemDemo/App.axaml`，在 `StyleInclude` 之后补一段
`Application.Resources`：

```xml
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://Avalonia.PropertySystemDemo/Controls/GaugeControl.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
```

`Application.Styles` 部分保持 Task 1 的原样不动。

- [x] **Step 4: 创建 StyledPropertyPage**

创建 `Avalonia.PropertySystemDemo/Views/Pages/StyledPropertyPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.PropertySystemDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.PropertySystemDemo.Views.Pages.StyledPropertyPage">

    <UserControl.Styles>
        <!--  Styles can reach a StyledProperty. This is the whole point of the kind.  -->
        <Style Selector="controls|GaugeControl.warning">
            <Setter Property="Value" Value="90" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="StyledProperty：可被样式设置、支持强制与继承的属性"
                               DocPath="custom-controls/defining-properties" />

            <TextBlock Classes="caption" Text="1. 滑块直接写 Value（本地值）" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <Slider Name="ValueSlider" Minimum="-50" Maximum="150" Value="30" />
                    <controls:GaugeControl Caption="本地值驱动"
                                           Value="{Binding #ValueSlider.Value}" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="滑块范围是 -50 到 150，但仪表永远停在 0 到 100 之间——coerce 回调在每次写入时都会夹取。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 样式类设置同一个属性" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <!--
                        No Value written on the element: a local value would outrank
                        the style setter permanently and the class would do nothing.
                    -->
                    <controls:GaugeControl Name="StyledGauge" Caption="样式类驱动" />
                    <CheckBox Name="WarningToggle" Content="加上 warning 样式类（Value=90）"
                              IsCheckedChanged="OnWarningToggled" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="注意这个 GaugeControl 元素上没有写 Value。写了的话它就是本地值，样式类将永远不起作用——且没有任何报错或日志。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 注册一个 StyledProperty 要写什么" />
            <Border Classes="stage" Padding="10">
                <SelectableTextBlock FontFamily="Consolas, monospace" TextWrapping="Wrap"
                                     Text="public static readonly StyledProperty&lt;double&gt; ValueProperty =&#10;    AvaloniaProperty.Register&lt;GaugeControl, double&gt;(&#10;        nameof(Value),&#10;        defaultValue: 0d,&#10;        coerce: static (_, v) =&gt; Math.Clamp(v, 0d, 100d));" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.PropertySystemDemo/Views/Pages/StyledPropertyPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class StyledPropertyPage : UserControl
    {
        public StyledPropertyPage()
        {
            InitializeComponent();
        }

        private void OnWarningToggled(object? sender, RoutedEventArgs e)
        {
            if (WarningToggle.IsChecked == true)
            {
                StyledGauge.Classes.Add("warning");
            }
            else
            {
                StyledGauge.Classes.Remove("warning");
            }
        }
    }
}
```

- [x] **Step 5: 创建 DirectPropertyPage**

创建 `Avalonia.PropertySystemDemo/Views/Pages/DirectPropertyPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.PropertySystemDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.PropertySystemDemo.Views.Pages.DirectPropertyPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DirectProperty：包了通知的普通字段"
                               DocPath="custom-controls/defining-properties" />

            <TextBlock Classes="caption" Text="1. Readout 是 DirectProperty，由 Value 变化时算出" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <Slider Name="Driver" Minimum="0" Maximum="100" Value="25" />
                    <controls:GaugeControl Name="Gauge" Caption="观察下方读数"
                                           Value="{Binding #Driver.Value}" />
                    <TextBlock Text="{Binding #Gauge.Readout, StringFormat='从外部绑定读到 Readout：{0}'}" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 两种属性的取舍" />
            <Border Classes="stage" Padding="10">
                <Grid ColumnDefinitions="Auto,*,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto">
                    <TextBlock Grid.Row="0" Grid.Column="0" FontWeight="SemiBold" Margin="0,0,16,4" Text="能力" />
                    <TextBlock Grid.Row="0" Grid.Column="1" FontWeight="SemiBold" Margin="0,0,16,4" Text="StyledProperty" />
                    <TextBlock Grid.Row="0" Grid.Column="2" FontWeight="SemiBold" Margin="0,0,0,4" Text="DirectProperty" />

                    <TextBlock Grid.Row="1" Grid.Column="0" Margin="0,0,16,2" Text="样式可设置" />
                    <TextBlock Grid.Row="1" Grid.Column="1" Margin="0,0,16,2" Text="可以" />
                    <TextBlock Grid.Row="1" Grid.Column="2" Margin="0,0,0,2" Text="不可以" />

                    <TextBlock Grid.Row="2" Grid.Column="0" Margin="0,0,16,2" Text="值继承 / 优先级仲裁" />
                    <TextBlock Grid.Row="2" Grid.Column="1" Margin="0,0,16,2" Text="支持" />
                    <TextBlock Grid.Row="2" Grid.Column="2" Margin="0,0,0,2" Text="不支持" />

                    <TextBlock Grid.Row="3" Grid.Column="0" Margin="0,0,16,2" Text="读取开销" />
                    <TextBlock Grid.Row="3" Grid.Column="1" Margin="0,0,16,2" Text="查优先级表" />
                    <TextBlock Grid.Row="3" Grid.Column="2" Margin="0,0,0,2" Text="直接读字段" />

                    <TextBlock Grid.Row="4" Grid.Column="0" Margin="0,0,16,0" Text="适合" />
                    <TextBlock Grid.Row="4" Grid.Column="1" Margin="0,0,16,0" Text="外观类、可换肤的属性" />
                    <TextBlock Grid.Row="4" Grid.Column="2" Margin="0,0,0,0" Text="高频读写的内部状态、只读派生值" />
                </Grid>
            </Border>

            <TextBlock Classes="caption" Text="3. 注册一个 DirectProperty 要写什么" />
            <Border Classes="stage" Padding="10">
                <SelectableTextBlock FontFamily="Consolas, monospace" TextWrapping="Wrap"
                                     Text="private string _readout = &quot;0%&quot;;&#10;&#10;public static readonly DirectProperty&lt;GaugeControl, string&gt; ReadoutProperty =&#10;    AvaloniaProperty.RegisterDirect&lt;GaugeControl, string&gt;(&#10;        nameof(Readout), o =&gt; o._readout);&#10;&#10;// 写入必须走 SetAndRaise，否则绑定收不到通知&#10;private set =&gt; SetAndRaise(ReadoutProperty, ref _readout, value);" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.PropertySystemDemo/Views/Pages/DirectPropertyPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class DirectPropertyPage : UserControl
    {
        public DirectPropertyPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 6: 创建 AttachedPropertyPage**

创建 `Avalonia.PropertySystemDemo/Views/Pages/AttachedPropertyPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.PropertySystemDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.PropertySystemDemo.Views.Pages.AttachedPropertyPage">

    <UserControl.Styles>
        <!--  A style can select on an attached property the target never declared.  -->
        <Style Selector="Button[(controls|HighlightBehavior.IsHighlighted)=True]">
            <Setter Property="Background" Value="#E8974A" />
            <Setter Property="Foreground" Value="Black" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="附加属性：给不属于你的控件挂状态"
                               DocPath="custom-controls/defining-properties#attached-properties" />

            <TextBlock Classes="caption" Text="1. 非继承型：每个控件各自持有" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="TargetA" Content="按钮 A" />
                        <Button Name="TargetB" Content="按钮 B" />
                    </StackPanel>
                    <CheckBox Content="给按钮 A 挂上 IsHighlighted" IsCheckedChanged="OnHighlightToggled" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="Button 类里没有 IsHighlighted 这个属性，是 HighlightBehavior 挂上去的。上面的样式选择器据此改变外观。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 内置附加属性就是这么工作的" />
            <Border Classes="stage" Padding="10">
                <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto" Height="80">
                    <Border Grid.Row="0" Grid.Column="0" Background="#4A7BE8" CornerRadius="3" Margin="2" />
                    <Border Grid.Row="0" Grid.Column="1" Background="#6FE84A" CornerRadius="3" Margin="2" />
                    <Border Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
                            Background="#E8564A" CornerRadius="3" Margin="2" />
                </Grid>
            </Border>
            <TextBlock Opacity="0.75" TextWrapping="Wrap" Margin="0,4,0,0"
                       Text="Grid.Row / Grid.Column / Grid.ColumnSpan 都是 Grid 挂到子元素上的附加属性——Border 自己并不知道行列是什么。" />

            <TextBlock Classes="caption" Text="3. 继承型：设在祖先上，后代全部读到" />
            <Border Name="InheritRoot" Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <Border Padding="8" BorderBrush="#40FFFFFF" BorderThickness="1" CornerRadius="3">
                        <StackPanel Spacing="4">
                            <TextBlock Name="Level1Text" />
                            <Border Padding="8" BorderBrush="#40FFFFFF" BorderThickness="1" CornerRadius="3">
                                <TextBlock Name="Level2Text" />
                            </Border>
                        </StackPanel>
                    </Border>
                    <Button Content="在最外层设置 TagLine" Click="OnSetTagLineClick" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.PropertySystemDemo/Views/Pages/AttachedPropertyPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.PropertySystemDemo.Controls;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class AttachedPropertyPage : UserControl
    {
        public AttachedPropertyPage()
        {
            InitializeComponent();
            ShowInherited();
        }

        private void OnHighlightToggled(object? sender, RoutedEventArgs e)
        {
            var on = (sender as CheckBox)?.IsChecked == true;
            HighlightBehavior.SetIsHighlighted(TargetA, on);
        }

        private void OnSetTagLineClick(object? sender, RoutedEventArgs e)
        {
            // Set once, on the outermost Border only.
            HighlightBehavior.SetTagLine(InheritRoot, "我在最外层被设置");
            ShowInherited();
        }

        private void ShowInherited()
        {
            Level1Text.Text = $"第 1 层读到：{Describe(HighlightBehavior.GetTagLine(Level1Text))}";
            Level2Text.Text = $"第 2 层读到：{Describe(HighlightBehavior.GetTagLine(Level2Text))}";
        }

        private static string Describe(string? value)
            => string.IsNullOrEmpty(value) ? "（尚未设置）" : value;
    }
}
```

**选择器语法要点**：按附加属性筛选写作
`Selector="Button[(controls|HighlightBehavior.IsHighlighted)=True]"`——
命名空间前缀用 `|` 而不是 `:`，整个属性名要用圆括号包住。

- [x] **Step 7: 创建 PrecedenceViewModel 与 PrecedencePage**

这是本组最重要的一页：它把样板阶段那个静默缺陷做成可交互的演示。

创建 `Avalonia.PropertySystemDemo/ViewModels/PrecedenceViewModel.cs`：

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.PropertySystemDemo.ViewModels
{
    public partial class PrecedenceViewModel : ViewModelBase
    {
        [ObservableProperty]
        private double _boundWidth = 220d;
    }
}
```

创建 `Avalonia.PropertySystemDemo/Views/Pages/PrecedencePage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.PropertySystemDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.PropertySystemDemo.Views.Pages.PrecedencePage"
             x:DataType="vm:PrecedenceViewModel">

    <UserControl.Styles>
        <Style Selector="Border.probe">
            <Setter Property="Background" Value="#4A7BE8" />
            <Setter Property="CornerRadius" Value="3" />
            <Setter Property="Height" Value="32" />
            <!--  Style priority: 3. Loses to anything written on the element.  -->
            <Setter Property="Width" Value="300" />
        </Style>
        <Style Selector="Border.probe:pointerover">
            <!--  Pseudo-class trigger priority: 1. Beats a plain style, loses to local.  -->
            <Setter Property="Width" Value="380" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="属性值优先级：谁赢了，为什么"
                               DocPath="properties/value-precedence" />

            <TextBlock Classes="caption" Text="1. 三个方块用同一套样式，差别只在有没有写本地值" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <TextBlock Text="A：只有样式（Width=300），鼠标悬停时伪类接管（380）" />
                    <Border Name="ProbeA" Classes="probe" HorizontalAlignment="Left" />

                    <TextBlock Text="B：元素上写了 Width=150，样式与伪类都失效" />
                    <Border Name="ProbeB" Classes="probe" Width="150" HorizontalAlignment="Left" />

                    <TextBlock Text="C：绑定到 ViewModel，绑定与本地值同级，后写的赢" />
                    <Border Name="ProbeC" Classes="probe" Width="{Binding BoundWidth}" HorizontalAlignment="Left" />
                    <Slider Minimum="80" Maximum="400" Value="{Binding BoundWidth}" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 实测读回来的宽度" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Name="ReadoutA" />
                    <TextBlock Name="ReadoutB" />
                    <TextBlock Name="ReadoutC" />
                    <Button Content="重新读取" Click="OnReadClick" HorizontalAlignment="Left" Margin="0,4,0,0" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 优先级表（数值越小越优先）" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Text="0  Animation — 动画运行期间压过一切" />
                    <TextBlock Text="0  LocalValue — 元素上直接写的属性、代码里的 SetValue" />
                    <TextBlock Text="1  StyleTrigger — 伪类与容器查询命中时的 Setter" />
                    <TextBlock Text="2  TemplatedParent — 控件模板里 TemplateBinding 带来的值" />
                    <TextBlock Text="3  Style — 普通样式 Setter" />
                    <TextBlock Text="4  Inherited — 从祖先继承" />
                    <TextBlock Text="5  Default — 属性注册时的默认值" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="4. 这条规则坑过本仓库一次" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="Avalonia.LayoutDemo 的容器查询演示里，UniformGrid 元素上写了 Columns=&quot;1&quot; 作为兜底，结果三个 ContainerQuery 的 Setter 全部失效，任何窗口宽度下都只渲染一列。它不报错、不记日志、不抛异常——因为从框架视角看这不是错误，是按设计工作的优先级规则。凡打算被样式驱动的属性，元素上就不要写值。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.PropertySystemDemo/Views/Pages/PrecedencePage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.PropertySystemDemo.ViewModels;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class PrecedencePage : UserControl
    {
        public PrecedencePage()
        {
            InitializeComponent();
            DataContext = new PrecedenceViewModel();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Read();
        }

        private void OnReadClick(object? sender, RoutedEventArgs e) => Read();

        private void Read()
        {
            ReadoutA.Text = $"A 实际宽度：{ProbeA.Bounds.Width:F0}（样式给的 300）";
            ReadoutB.Text = $"B 实际宽度：{ProbeB.Bounds.Width:F0}（本地值 150 压过样式 300）";
            ReadoutC.Text = $"C 实际宽度：{ProbeC.Bounds.Width:F0}（跟随滑块）";
        }
    }
}
```

- [x] **Step 8: 创建 MetadataPage**

创建 `Avalonia.PropertySystemDemo/Views/Pages/MetadataPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.PropertySystemDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.PropertySystemDemo.Views.Pages.MetadataPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="元数据与回调：默认值、强制、变更通知"
                               DocPath="properties/metadata-and-callbacks" />

            <TextBlock Classes="caption" Text="1. coerce：每次写入都会夹取，来源不限" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="写入 150" Click="OnWriteHighClick" />
                        <Button Content="写入 -20" Click="OnWriteLowClick" />
                        <Button Content="写入 55" Click="OnWriteMidClick" />
                    </StackPanel>
                    <controls:GaugeControl Name="Target" Caption="看写进去和存下来的差别" />
                    <TextBlock Name="CoerceLog" TextWrapping="Wrap" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 变更回调：派生值在 OnPropertyChanged 里重算" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Text="{Binding #Target.Readout, StringFormat='Readout 当前是：{0}'}" />
                    <TextBlock Opacity="0.75" TextWrapping="Wrap"
                               Text="GaugeControl 重写了 OnPropertyChanged，发现 ValueProperty 变化就更新 Readout。这是让一个属性跟随另一个属性的标准位置。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 值继承：设一次，后代全读到" />
            <Border Name="InheritHost" Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock Text="下面两行的字号没有各自设置，是从这个 Border 继承的：" />
                    <TextBlock Text="第一行" />
                    <Border Padding="8">
                        <TextBlock Text="第二行（又嵌了一层，仍然继承）" />
                    </Border>
                    <Slider Name="FontSlider" Minimum="10" Maximum="28" Value="14" />
                </StackPanel>
            </Border>
            <TextBlock Opacity="0.75" TextWrapping="Wrap" Margin="0,4,0,0"
                       Text="FontSize 是注册时带 inherits 的属性之一。Avalonia 里只有少量属性参与继承：FontSize、FontFamily、Foreground、FlowDirection 等。" />

            <TextBlock Classes="caption" Text="4. 注册时能提供哪些元数据" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Text="defaultValue — 谁也没设置时的值（优先级最低）" />
                    <TextBlock Text="inherits — 是否沿视觉树向下传递" />
                    <TextBlock Text="defaultBindingMode — 不写 Mode 时的默认绑定方向" />
                    <TextBlock Text="coerce — 每次写入的夹取/规整回调" />
                    <TextBlock Text="validate — 值非法时直接抛异常，与 coerce 的静默纠正相对" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

创建 `Avalonia.PropertySystemDemo/Views/Pages/MetadataPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class MetadataPage : UserControl
    {
        public MetadataPage()
        {
            InitializeComponent();

            // FontSize inherits, so setting it on the host reaches both labels.
            InheritHost.Bind(FontSizeProperty, FontSlider.GetObservable(Slider.ValueProperty));
        }

        private void OnWriteHighClick(object? sender, RoutedEventArgs e) => Write(150);

        private void OnWriteLowClick(object? sender, RoutedEventArgs e) => Write(-20);

        private void OnWriteMidClick(object? sender, RoutedEventArgs e) => Write(55);

        private void Write(double requested)
        {
            Target.Value = requested;
            CoerceLog.Text = $"写入 {requested:F0}，实际存下 {Target.Value:F0}"
                + (System.Math.Abs(requested - Target.Value) > 0.01 ? "（被 coerce 夹取了）" : "（在范围内，原样保留）");
        }
    }
}
```

**`InitializeComponent()` 之后 `x:Name` 字段即可用**，与视觉树是否建立无关——所以这里在
构造函数里 `Bind` 是可以的。对比 `TopLevelPage`：那里必须等 `OnAttachedToVisualTree`，
因为 `TopLevel.GetTopLevel(this)` 依赖的是视觉树而非字段。

以上写法均已实测：附加属性选择器 `Button[(controls|HighlightBehavior.IsHighlighted)=True]`
生效（背景确实变为 `#ffe8974a`）；`ProbeA`/`ProbeB` 分别读回 300 / 150；
`FontSize` 继承随滑块从 14 联动到 26。

- [x] **Step 9: 挂到 MainWindow 的 TabControl**

修改 `Avalonia.PropertySystemDemo/Views/MainWindow.axaml`——补命名空间声明：

```xml
        xmlns:pages="using:Avalonia.PropertySystemDemo.Views.Pages"
```

把空的 TabControl 替换为：

```xml
    <TabControl Margin="12">
        <TabItem Header="StyledProperty">
            <pages:StyledPropertyPage />
        </TabItem>
        <TabItem Header="DirectProperty">
            <pages:DirectPropertyPage />
        </TabItem>
        <TabItem Header="附加属性">
            <pages:AttachedPropertyPage />
        </TabItem>
        <TabItem Header="值优先级">
            <pages:PrecedencePage />
        </TabItem>
        <TabItem Header="元数据与回调">
            <pages:MetadataPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 10: 构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -5`
Expected: `0 个错误`，无新增警告。

- [x] **Step 11: 用 headless 探针断言页面行为**

复制 Task 2 Step 12 的探针工程到 `C:\Temp\propcheck`，`ProjectReference` 改指
`Avalonia.PropertySystemDemo`，`ProbeApp.Initialize` 里额外合并 `GaugeControl` 的主题
（否则控件没有模板，内部零件读不到）：

```csharp
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Avalonia.PropertySystemDemo/"))
        {
            Source = new Uri("avares://Avalonia.PropertySystemDemo/Controls/GaugeControl.axaml")
        });
    }
```

断言部分：

```csharp
        // GaugeControl: the coerce callback must clamp both ends.
        var gauge = new Avalonia.PropertySystemDemo.Controls.GaugeControl();
        gauge.Value = 150;
        var high = gauge.Value;
        gauge.Value = -20;
        var low = gauge.Value;
        Check("Gauge: coerce clamps", high == 100 && low == 0, $"150→{high}, -20→{low}");
        Check("Gauge: readout follows", gauge.Readout == "0%", $"readout={gauge.Readout}");

        // StyledPropertyPage: the style class must reach Value, because the
        // element carries no local value.
        var sp = new StyledPropertyPage();
        Layout(sp);
        var styledGauge = Find<Avalonia.PropertySystemDemo.Controls.GaugeControl>(sp, "StyledGauge");
        var beforeClass = styledGauge.Value;
        styledGauge.Classes.Add("warning");
        Dispatcher.UIThread.RunJobs();
        Check("StyledProperty: class sets Value", beforeClass == 0 && styledGauge.Value == 90,
            $"before={beforeClass} after={styledGauge.Value}");

        // AttachedPropertyPage: the selector must repaint the button.
        var ap = new AttachedPropertyPage();
        Layout(ap);
        var targetA = Find<Button>(ap, "TargetA");
        var bgBefore = targetA.Background?.ToString();
        Avalonia.PropertySystemDemo.Controls.HighlightBehavior.SetIsHighlighted(targetA, true);
        Dispatcher.UIThread.RunJobs();
        Check("Attached: selector repaints", targetA.Background?.ToString() != bgBefore,
            $"{bgBefore} → {targetA.Background}");

        // PrecedencePage: this is the LayoutDemo defect in miniature.
        var pp = new PrecedencePage();
        Layout(pp);
        var a = Find<Border>(pp, "ProbeA").Bounds.Width;
        var b = Find<Border>(pp, "ProbeB").Bounds.Width;
        Check("Precedence: style vs local", a == 300 && b == 150, $"A={a} B={b}");

        // MetadataPage: FontSize must inherit down through the nested Border.
        var mp = new MetadataPage();
        Layout(mp);
        var slider = mp.GetVisualDescendants().OfType<Slider>().First();
        slider.Value = 26;
        Dispatcher.UIThread.RunJobs();
        var host = Find<Border>(mp, "InheritHost");
        Check("Metadata: FontSize inherits", Math.Abs(host.FontSize - 26) < 0.01, $"host={host.FontSize}");
```

Run: `dotnet run --project C:\Temp\propcheck\propcheck.csproj`

Expected: 七行全部 `PASS`，`warning-or-worse log entries: 0`。

`StyledProperty: class sets Value` 这条最关键：它验证的正是"元素上没写本地值，样式才能
生效"。若这条 FAIL 且 `after=0`，回去检查 `StyledGauge` 元素上是不是多写了 `Value`。

- [x] **Step 12: 清理探针并提交**

```bash
rm -rf /c/Temp/propcheck
git add Avalonia.PropertySystemDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Property System category

Five pages built around one GaugeControl that carries all three property
kinds. The precedence page reproduces the defect that bit LayoutDemo: a
local value on the element silently outranks every style setter, with no
error and no log entry.

Verified with a throwaway headless probe asserting that coerce clamps both
ends, that a style class reaches Value only when no local value is present,
that the attached-property selector repaints, and that FontSize inherits
through a nested Border.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: 收尾 — README、slnx 校验与 spec 回写

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**Interfaces:**
- Consumes: Task 1–4 完成的三个项目
- Produces: 无代码产物。把本组的实测结论回写到 spec，供后续三组 plan 引用。

- [x] **Step 1: 更新 README 项目表格**

修改 `README.md`，在 `Avalonia.LayoutDemo` 一行之后插入三行（按官方文档分类顺序，
Fundamentals 与 XAML 在 Layout 之前，但表格目前是按加入时间排的，所以追加在后面即可）：

```markdown
| [Avalonia.FundamentalsDemo](Avalonia.FundamentalsDemo) | 纯代码 UI、code-behind 与 MVVM 对照、TopLevel、视觉树与逻辑树、应用生命周期 |
| [Avalonia.XamlDemo](Avalonia.XamlDemo) | 命名空间、x: 指令、标记扩展、类型转换器、泛型、XAML 编译 |
| [Avalonia.PropertySystemDemo](Avalonia.PropertySystemDemo) | StyledProperty / DirectProperty / 附加属性、值优先级、元数据与回调 |
```

- [x] **Step 2: 回写实测结论到 spec**

在 spec 的「样板阶段实测结论」小节之后，新增一节：

```markdown
### 基础层实测结论（2026-09-22）

三个项目（#1 Fundamentals、#2 XAML、#7 PropertySystem）落地过程中验证到的结果：

- **官方文档子页与 spec 功能点映射有三处出入**（已按实际调整）：Fundamentals 官方有 12 个
  子页而非 8 个，其中 `architecture` / `cross-platform-architecture` 无可交互内容、
  `avalonia-xaml` 归项目 #2；Property System 分类只有 3 个子页，三种属性的定义方式实际在
  `custom-controls/defining-properties`；XAML Reference 的 6 个子页与 spec 完全吻合。
- **编译型 XAML 的两条响亮失败**：`{x:Static}` 返回枚举时不能直接填 `string` 属性
  （`AVLN3000`），包一层 `{Binding Source=...}` 可解；`{x:Type}` 不能当 `Binding` 的
  `Source`（`AVLN2100` 要求 `x:DataType`），它的正当位置是 `ControlTheme.TargetType`。
- **`StringFormat` 里的字面花括号必须双写**（新增的静默失败类型）：单层 `{Binding}` 会被
  .NET 复合格式化当成占位符，解析失败后**静默产出空字符串**，无报错无日志。后续项目凡用
  `StringFormat` 的，headless 断言里要检查文本非空。
- **`BindingPlugins` 在 12.1.2 不是公开 API**（`CS0122`）。需要展示编译型 XAML 的产物时，
  改为反射程序集自身的 `CompiledAvaloniaXaml` 命名空间。
- **属性系统行为确认**：`coerce` 在每次写入时生效（150→100、−20→0）；
  `RegisterAttached(..., inherits: true)` 的值沿视觉树传递多层；
  附加属性选择器语法为 `Button[(ns|Owner.Prop)=True]`（前缀用 `|`，属性名加圆括号）；
  **本地值 111 确实压过样式 Setter 999**——样板阶段那个缺陷的最小复现。
- **验证方式**：三个项目各用一个仓库外的 headless 探针断言关键属性值，跑完即弃。
  这套做法在本组共拦下 3 个写 plan 阶段的错误（枚举填 string、`x:Type` 作 Source、
  `StringFormat` 花括号），全部在编写期解决，未进入实现。
```

- [x] **Step 3: 全量构建与最终验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -5`
Expected: 全部 9 个项目构建成功，`0 个错误`。

Run: `git status --short`
Expected: 只有本任务待提交的 `README.md` 与 `docs/` 改动，无遗留的探针目录或临时文件。

逐个启动三个项目确认能打开（这一步是烟雾测试，不替代 Step 11 那类断言）：

```bash
dotnet run --project Avalonia.FundamentalsDemo
dotnet run --project Avalonia.XamlDemo
dotnet run --project Avalonia.PropertySystemDemo
```

- [x] **Step 4: 提交**

```bash
git add README.md docs/
git commit -m "$(cat <<'EOF'
docs: register the foundation-layer demos and record findings

Three compiled-XAML traps found while writing the plan are now in the
spec, so the remaining three groups inherit them: an enum from x:Static
cannot fill a string property, x:Type cannot be a binding Source, and a
single brace inside StringFormat silently yields an empty string.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [x] **Step 5: 交回用户 review**

本组完成。向用户报告：

- 三个项目各有哪些 Tab，分别演示了什么
- 写 plan 阶段实测拦下的三个错误，以及 `StringFormat` 那条静默失败
- 官方文档子页与 spec 映射的三处出入
- 请用户确认后，再编写第二组「样式绑定层」（#4 Styling、#5 DataBinding、#6 DataTemplates，
  同时删除旧 `Avalonia.DataTemplateDemo`）的 plan

**不要在用户确认前开始下一组**——与样板阶段同理，先让一组定型。


















