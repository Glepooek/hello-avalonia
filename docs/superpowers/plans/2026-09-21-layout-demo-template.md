# Avalonia.LayoutDemo 样板项目 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建成 `Avalonia.LayoutDemo`，演示官方 Layout 分类的 3 个功能点，并确立后续 14 个分类演示项目共用的目录结构、说明条格式与注释风格。

**Architecture:** WinExe 项目，`MainWindow` 只承载 `TabControl` 外壳，每个功能点是 `Views/Pages/` 下一个独立 `UserControl`。页面顶部统一用共享的 `DemoHeader` 控件显示中文功能点说明与官方文档路径。无外部依赖，无网络调用。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm、中央包管理（`Directory.Packages.props`）

**Spec:** `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

## Global Constraints

以下约束适用于本 plan 的每一个任务，也适用于后续所有分类演示项目：

- **Avalonia 版本统一为 12.1.2**，不为任何项目降级到 Avalonia 11
- **TargetFramework 为 `net10.0`**，`Nullable` 为 `enable`
- **包版本只在 `Directory.Packages.props` 声明**，`.csproj` 里的 `PackageReference` 不带 `Version` 属性
- **不引入 ReactiveUI、Prism 等第三方 MVVM/UI 框架**，只用官方 API + `CommunityToolkit.Mvvm`
- **C# 与 XAML 注释用英文**；**界面文字（Tab 标题、说明条、按钮文案）用中文**；**标识符（类名、属性名、`x:Name`）用英文**
- **每个演示页顶部必须有说明条**，含中文功能点描述 + 对应官方文档路径（形如 `docs/layout/choosing-a-layout-panel`）
- **每个功能点一个 `UserControl`**，放在 `Views/Pages/` 下，不把多个功能点塞进同一个 axaml
- **不为演示项目写自动化测试**（`Avalonia.TestingDemo.Tests` 除外，不在本 plan 范围内）
- **`AvaloniaUseCompiledBindingsByDefault` 设为 `true`**，与现有项目一致
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`

## 本 plan 的范围

本 plan 只实现 spec 交付顺序中的**第一阶段（样板）**。其余 14 个项目分 4 个后续 plan，在本样板通过用户 review 后再编写——样板未定型就复制结构，等于把未验证的决策复制 14 遍。

## File Structure

| 文件 | 职责 |
|---|---|
| `Avalonia.Shared/Controls/DemoHeader.cs` | 说明条控件（TemplatedControl），两个属性：`Title`、`DocPath`。所有分类演示项目共用 |
| `Avalonia.Shared/Controls/DemoHeader.axaml` | `DemoHeader` 的 ControlTheme |
| `Avalonia.Shared/Themes/SharedStyles.axaml` | 汇总共享控件样式，供各演示项目 `StyleInclude` 引用 |
| `Avalonia.Shared/Avalonia.Shared.csproj` | 增加 `Avalonia.Themes.Fluent` 包引用（`DemoHeader.axaml` 用到 Fluent 资源键） |
| `Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj` | 项目文件 |
| `Avalonia.LayoutDemo/Program.cs` | 入口 |
| `Avalonia.LayoutDemo/App.axaml(.cs)` | Fluent 主题 + 引用 `SharedStyles.axaml` |
| `Avalonia.LayoutDemo/app.manifest` | Windows manifest |
| `Avalonia.LayoutDemo/Assets/avalonia-logo.ico` | 窗口图标（从现有项目复制） |
| `Avalonia.LayoutDemo/Views/MainWindow.axaml(.cs)` | TabControl 外壳，3 个 Tab |
| `Avalonia.LayoutDemo/Views/Pages/PanelsPage.axaml(.cs)` | 功能点 1：8 种布局面板对照 |
| `Avalonia.LayoutDemo/Views/Pages/PositioningPage.axaml(.cs)` | 功能点 2：对齐、Margin、Padding |
| `Avalonia.LayoutDemo/Views/Pages/ResponsivePage.axaml(.cs)` | 功能点 3：响应式布局 |
| `Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.cs` | 功能点 3 的断点 ViewModel（前两个功能点是纯 XAML，无 ViewModel） |
| `hello-avalonia.slnx` | 注册新项目 |
| `README.md` | 项目表格加一行 |

**为什么 `DemoHeader` 放在 `Avalonia.Shared`**：15 个项目、约 120 个演示页都要用同一个说明条。放在样板项目里意味着后续 14 个项目各复制一份，改格式要改 15 处。`Avalonia.Shared` 已是所有演示项目的共同依赖，是它的自然归属。

**为什么前两个功能点没有 ViewModel**：布局面板对照和对齐演示是纯静态 XAML，不需要状态。为了"结构统一"给它们各配一个空 ViewModel 属于投机性设计。

## Task 1: 共享说明条控件 DemoHeader

**Files:**
- Create: `Avalonia.Shared/Controls/DemoHeader.cs`
- Create: `Avalonia.Shared/Controls/DemoHeader.axaml`
- Create: `Avalonia.Shared/Themes/SharedStyles.axaml`
- Modify: `Avalonia.Shared/Avalonia.Shared.csproj`

**Interfaces:**
- Consumes: 无（本任务是起点）
- Produces: `Avalonia.Shared.Controls.DemoHeader`，一个 `TemplatedControl`，含两个
  `StyledProperty<string?>`：`Title`（中文功能点描述）和 `DocPath`（官方文档路径）。
  样式入口为 `avares://Avalonia.Shared/Themes/SharedStyles.axaml`，后续所有演示项目在
  `App.axaml` 里 `StyleInclude` 这一个地址。

**为什么用 TemplatedControl 而不是 UserControl**：`DemoHeader` 只有两个字符串属性、无内部
交互逻辑，`TemplatedControl` + `ControlTheme` 是 Avalonia 对这类"可换肤的纯展示控件"的
标准做法，也让它自身成为 CustomControls 分类的一个真实用例。

- [x] **Step 1: 创建 DemoHeader 控件类**

创建 `Avalonia.Shared/Controls/DemoHeader.cs`：

```csharp
using Avalonia.Controls.Primitives;

namespace Avalonia.Shared.Controls
{
    /// <summary>
    /// A header bar shown at the top of every demo page. It states which
    /// documentation topic the page demonstrates and where to find it upstream.
    /// </summary>
    public class DemoHeader : TemplatedControl
    {
        public static readonly StyledProperty<string?> TitleProperty =
            AvaloniaProperty.Register<DemoHeader, string?>(nameof(Title));

        public static readonly StyledProperty<string?> DocPathProperty =
            AvaloniaProperty.Register<DemoHeader, string?>(nameof(DocPath));

        /// <summary>The feature being demonstrated, written in Chinese.</summary>
        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>Path of the matching page on docs.avaloniaui.net.</summary>
        public string? DocPath
        {
            get => GetValue(DocPathProperty);
            set => SetValue(DocPathProperty, value);
        }
    }
}
```

- [x] **Step 2: 创建 DemoHeader 的 ControlTheme**

创建 `Avalonia.Shared/Controls/DemoHeader.axaml`：

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:controls="using:Avalonia.Shared.Controls">

    <ControlTheme x:Key="{x:Type controls:DemoHeader}" TargetType="controls:DemoHeader">
        <Setter Property="Margin" Value="0,0,0,12" />
        <Setter Property="Template">
            <ControlTemplate TargetType="controls:DemoHeader">
                <Border Background="{DynamicResource SystemControlBackgroundListLowBrush}"
                        BorderBrush="{DynamicResource SystemAccentColor}"
                        BorderThickness="4,0,0,0"
                        CornerRadius="0,4,4,0"
                        Padding="12,8">
                    <StackPanel Spacing="2">
                        <TextBlock FontSize="15"
                                   FontWeight="SemiBold"
                                   Text="{TemplateBinding Title}" />
                        <!--  The doc path is dimmed: it is a reference, not the message.  -->
                        <TextBlock FontFamily="Consolas, monospace"
                                   FontSize="12"
                                   Opacity="0.65"
                                   Text="{TemplateBinding DocPath}" />
                    </StackPanel>
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
</ResourceDictionary>
```

- [x] **Step 3: 创建共享样式汇总入口**

创建 `Avalonia.Shared/Themes/SharedStyles.axaml`：

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!--  Single entry point for demo projects: include this one file, get every shared control.  -->
    <Styles.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://Avalonia.Shared/Controls/DemoHeader.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Styles.Resources>
</Styles>
```

- [x] **Step 4: 让 Avalonia.Shared 支持编译 XAML**

`Avalonia.Shared` 目前是纯 C# 类库，没有引用 XAML 编译所需的包。修改
`Avalonia.Shared/Avalonia.Shared.csproj` 为：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>
</Project>
```

新增 `Avalonia.Themes.Fluent` 的原因：`DemoHeader.axaml` 里用了
`SystemControlBackgroundListLowBrush` 和 `SystemAccentColor` 这两个 Fluent 主题资源键。

注意：`.axaml` 文件不需要显式的 `<AvaloniaXaml Include>` 条目，Avalonia 的 MSBuild
targets 会自动包含项目目录下的 `**/*.axaml`。

- [x] **Step 5: 构建验证**

Run: `dotnet build Avalonia.Shared/Avalonia.Shared.csproj`
Expected: 构建成功，无错误无警告。

若报 `AVLN:0004` 之类的 XAML 错误，说明资源键名或命名空间有误，按错误信息定位到具体行修正。

- [x] **Step 6: 提交**

```bash
git add Avalonia.Shared/
git commit -m "$(cat <<'EOF'
feat: add shared DemoHeader control for demo pages

Every demo page states which documentation topic it covers and where to
find it upstream. Putting the header in Avalonia.Shared keeps that format
in one place across the 15 category projects.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

## Task 2: LayoutDemo 项目骨架与 TabControl 外壳

**Files:**
- Create: `Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj`
- Create: `Avalonia.LayoutDemo/Program.cs`
- Create: `Avalonia.LayoutDemo/App.axaml`
- Create: `Avalonia.LayoutDemo/App.axaml.cs`
- Create: `Avalonia.LayoutDemo/app.manifest`
- Create: `Avalonia.LayoutDemo/Assets/avalonia-logo.ico`（从现有项目复制）
- Create: `Avalonia.LayoutDemo/Views/MainWindow.axaml`
- Create: `Avalonia.LayoutDemo/Views/MainWindow.axaml.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 1 的 `avares://Avalonia.Shared/Themes/SharedStyles.axaml`
- Produces: 可运行的空壳窗口。`MainWindow.axaml` 内有一个 `TabControl`，Task 3–5 各自
  往里加一个 `TabItem`。命名空间 `Avalonia.LayoutDemo.Views.Pages` 为页面预留。

- [x] **Step 1: 创建项目文件**

创建 `Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj`：

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
        <PackageReference Condition="'$(Configuration)' == 'Debug'" Include="Avalonia.Diagnostics" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

- [x] **Step 2: 复制图标与 manifest**

```bash
mkdir -p Avalonia.LayoutDemo/Assets
cp Avalonia.DataTemplateDemo/Assets/avalonia-logo.ico Avalonia.LayoutDemo/Assets/
cp Avalonia.DataTemplateDemo/app.manifest Avalonia.LayoutDemo/
```

然后编辑 `Avalonia.LayoutDemo/app.manifest`，把 `assemblyIdentity` 的 `name` 从
`Avalonia.MusicStore.Desktop` 改为 `Avalonia.LayoutDemo`：

```xml
  <assemblyIdentity version="1.0.0.0" name="Avalonia.LayoutDemo"/>
```

（现有项目的 manifest 都还留着 MusicStore 的名字，那是拆分时的遗留；新项目不沿袭这个错误，
但也不回头去改现有项目——那超出本次范围。）

- [x] **Step 3: 创建 Program.cs**

创建 `Avalonia.LayoutDemo/Program.cs`：

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.LayoutDemo
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

- [x] **Step 4: 创建 App.axaml 与 App.axaml.cs**

创建 `Avalonia.LayoutDemo/App.axaml`：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.LayoutDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader and any future shared control.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>
</Application>
```

创建 `Avalonia.LayoutDemo/App.axaml.cs`：

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LayoutDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.LayoutDemo
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

- [x] **Step 5: 创建 MainWindow 外壳**

创建 `Avalonia.LayoutDemo/Views/MainWindow.axaml`：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.LayoutDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Layout Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <!--  Tabs are added by later tasks, one per documentation topic.  -->
    <TabControl Margin="12">
    </TabControl>
</Window>
```

创建 `Avalonia.LayoutDemo/Views/MainWindow.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.LayoutDemo.Views
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

- [x] **Step 6: 注册到解决方案**

修改 `hello-avalonia.slnx`，在项目列表中按字母序插入一行（`Avalonia.HtmlRendererDemo`
之后、`Avalonia.MusicStore` 之前）：

```xml
  <Project Path="Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj" />
```

- [x] **Step 7: 构建并运行验证**

Run: `dotnet build hello-avalonia.slnx`
Expected: 构建成功，无错误。

Run: `dotnet run --project Avalonia.LayoutDemo`
Expected: 弹出 900×640 的深色窗口，标题 "Avalonia Layout Demo"，内容区是空的
TabControl。确认后关闭窗口。

- [x] **Step 8: 提交**

```bash
git add Avalonia.LayoutDemo/ hello-avalonia.slnx
git commit -m "$(cat <<'EOF'
feat: scaffold Avalonia.LayoutDemo

Empty TabControl shell wired to the shared styles. Pages land in the
following commits, one per documentation topic.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

## Task 3: 功能点 1 — 8 种布局面板对照

**Files:**
- Create: `Avalonia.LayoutDemo/Views/Pages/PanelsPage.axaml`
- Create: `Avalonia.LayoutDemo/Views/Pages/PanelsPage.axaml.cs`
- Modify: `Avalonia.LayoutDemo/Views/MainWindow.axaml`（加第 1 个 TabItem）

**Interfaces:**
- Consumes: Task 1 的 `DemoHeader`；Task 2 的 `MainWindow` TabControl 外壳
- Produces: `Avalonia.LayoutDemo.Views.Pages.PanelsPage`，一个无参构造的 `UserControl`

**对照设计**：8 个面板各放一个小卡片，每张卡片里塞**完全相同的 5 个色块**，让读者一眼看出
同样内容在不同面板下的排布差异——这比 8 段互不相干的示例更能说明"该选哪个面板"。

- [x] **Step 1: 创建 PanelsPage.axaml**

创建 `Avalonia.LayoutDemo/Views/Pages/PanelsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.LayoutDemo.Views.Pages.PanelsPage">

    <UserControl.Styles>
        <!--  Every panel below hosts these same five blocks, so differences come from the panel alone.  -->
        <Style Selector="Border.block">
            <Setter Property="Width" Value="52" />
            <Setter Property="Height" Value="32" />
            <Setter Property="Margin" Value="2" />
            <Setter Property="CornerRadius" Value="3" />
        </Style>
        <Style Selector="Border.card">
            <Setter Property="BorderBrush" Value="#40FFFFFF" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="CornerRadius" Value="4" />
            <Setter Property="Padding" Value="8" />
            <Setter Property="Height" Value="150" />
        </Style>
        <Style Selector="TextBlock.caption">
            <Setter Property="FontWeight" Value="SemiBold" />
            <Setter Property="Margin" Value="0,0,0,4" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="布局面板对照：同样的 5 个色块，8 种排布结果"
                               DocPath="docs/layout/choosing-a-layout-panel" />

            <UniformGrid Columns="2">

                <!--  Grid: rows and columns sized independently. The only panel with proportional sizing.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="Grid — 行列独立定尺，支持 * 比例分配" />
                    <Border Classes="card">
                        <Grid ColumnDefinitions="Auto,*,Auto" RowDefinitions="Auto,*">
                            <Border Grid.Row="0" Grid.Column="0" Classes="block" Background="#E8564A" />
                            <Border Grid.Row="0" Grid.Column="1" Classes="block" Background="#E8974A" Width="NaN" />
                            <Border Grid.Row="0" Grid.Column="2" Classes="block" Background="#E8D24A" />
                            <Border Grid.Row="1" Grid.Column="0" Classes="block" Background="#6FE84A" />
                            <Border Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="2" Classes="block" Background="#4AC7E8" Width="NaN" />
                        </Grid>
                    </Border>
                </StackPanel>

                <!--  DockPanel: earlier children win space; the last one fills what is left.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="DockPanel — 先声明先占边，最后一个填满剩余" />
                    <Border Classes="card">
                        <DockPanel>
                            <Border Classes="block" Background="#E8564A" DockPanel.Dock="Top" Width="NaN" />
                            <Border Classes="block" Background="#E8974A" DockPanel.Dock="Bottom" Width="NaN" />
                            <Border Classes="block" Background="#E8D24A" DockPanel.Dock="Left" Height="NaN" />
                            <Border Classes="block" Background="#6FE84A" DockPanel.Dock="Right" Height="NaN" />
                            <Border Classes="block" Background="#4AC7E8" Width="NaN" Height="NaN" />
                        </DockPanel>
                    </Border>
                </StackPanel>

                <!--  StackPanel: one direction, no wrapping, children get unlimited space along it.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="StackPanel — 单方向排列，不换行" />
                    <Border Classes="card">
                        <StackPanel Orientation="Horizontal">
                            <Border Classes="block" Background="#E8564A" />
                            <Border Classes="block" Background="#E8974A" />
                            <Border Classes="block" Background="#E8D24A" />
                            <Border Classes="block" Background="#6FE84A" />
                            <Border Classes="block" Background="#4AC7E8" />
                        </StackPanel>
                    </Border>
                </StackPanel>

                <!--  WrapPanel: same as StackPanel until it runs out of room, then breaks to a new line.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="WrapPanel — 排满即换行，行与行独立对齐" />
                    <Border Classes="card" Width="220" HorizontalAlignment="Left">
                        <WrapPanel Orientation="Horizontal">
                            <Border Classes="block" Background="#E8564A" />
                            <Border Classes="block" Background="#E8974A" />
                            <Border Classes="block" Background="#E8D24A" />
                            <Border Classes="block" Background="#6FE84A" />
                            <Border Classes="block" Background="#4AC7E8" />
                        </WrapPanel>
                    </Border>
                </StackPanel>

                <!--  UniformGrid: every cell identical, regardless of content size.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="UniformGrid — 等分单元格，与内容尺寸无关" />
                    <Border Classes="card">
                        <UniformGrid Columns="3">
                            <Border Classes="block" Background="#E8564A" Width="NaN" Height="NaN" />
                            <Border Classes="block" Background="#E8974A" Width="NaN" Height="NaN" />
                            <Border Classes="block" Background="#E8D24A" Width="NaN" Height="NaN" />
                            <Border Classes="block" Background="#6FE84A" Width="NaN" Height="NaN" />
                            <Border Classes="block" Background="#4AC7E8" Width="NaN" Height="NaN" />
                        </UniformGrid>
                    </Border>
                </StackPanel>

                <!--  RelativePanel: each child positioned against a sibling or a panel edge.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="RelativePanel — 相对兄弟元素或面板边缘定位" />
                    <Border Classes="card">
                        <RelativePanel>
                            <Border Name="Anchor" Classes="block" Background="#E8564A"
                                    RelativePanel.AlignHorizontalCenterWithPanel="True"
                                    RelativePanel.AlignVerticalCenterWithPanel="True" />
                            <Border Classes="block" Background="#E8974A" RelativePanel.Above="Anchor" RelativePanel.AlignHorizontalCenterWith="Anchor" />
                            <Border Classes="block" Background="#E8D24A" RelativePanel.Below="Anchor" RelativePanel.AlignHorizontalCenterWith="Anchor" />
                            <Border Classes="block" Background="#6FE84A" RelativePanel.LeftOf="Anchor" RelativePanel.AlignVerticalCenterWith="Anchor" />
                            <Border Classes="block" Background="#4AC7E8" RelativePanel.RightOf="Anchor" RelativePanel.AlignVerticalCenterWith="Anchor" />
                        </RelativePanel>
                    </Border>
                </StackPanel>

                <!--  Canvas: absolute coordinates. The only panel that ignores the available size.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="Canvas — 绝对坐标，不随窗口尺寸调整" />
                    <Border Classes="card">
                        <Canvas>
                            <Border Classes="block" Background="#E8564A" Canvas.Left="0" Canvas.Top="0" />
                            <Border Classes="block" Background="#E8974A" Canvas.Left="30" Canvas.Top="24" />
                            <Border Classes="block" Background="#E8D24A" Canvas.Left="60" Canvas.Top="48" />
                            <Border Classes="block" Background="#6FE84A" Canvas.Left="90" Canvas.Top="72" />
                            <Border Classes="block" Background="#4AC7E8" Canvas.Left="120" Canvas.Top="96" />
                        </Canvas>
                    </Border>
                </StackPanel>

                <!--  Panel: the base class itself. Children stack in Z order, declaration order = bottom to top.  -->
                <StackPanel Margin="4">
                    <TextBlock Classes="caption" Text="Panel — 按声明顺序层叠（后声明的在上层）" />
                    <Border Classes="card">
                        <Panel>
                            <Border Classes="block" Background="#E8564A" Width="120" Height="90" />
                            <Border Classes="block" Background="#E8974A" Width="100" Height="74" />
                            <Border Classes="block" Background="#E8D24A" Width="80" Height="58" />
                            <Border Classes="block" Background="#6FE84A" Width="60" Height="42" />
                            <Border Classes="block" Background="#4AC7E8" Width="40" Height="26" />
                        </Panel>
                    </Border>
                </StackPanel>

            </UniformGrid>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

说明 `Width="NaN"`：`Border.block` 样式统一设了固定宽高，个别面板（Grid 的拉伸列、
DockPanel 的填充区、UniformGrid 的等分格）需要让色块随格子伸展，用 `Width="NaN"` 在
控件实例上覆盖样式设的固定值——这是 Avalonia 中"取消已设置尺寸"的写法，直接写
`Width="Auto"` 在 `double` 类型属性上不合法。

- [x] **Step 2: 创建 PanelsPage.axaml.cs**

创建 `Avalonia.LayoutDemo/Views/Pages/PanelsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.LayoutDemo.Views.Pages
{
    public partial class PanelsPage : UserControl
    {
        public PanelsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 3: 挂到 MainWindow 的 TabControl**

修改 `Avalonia.LayoutDemo/Views/MainWindow.axaml`——在根 `Window` 元素上补一个命名空间
声明：

```xml
        xmlns:pages="using:Avalonia.LayoutDemo.Views.Pages"
```

并把空的 TabControl 替换为：

```xml
    <TabControl Margin="12">
        <TabItem Header="布局面板">
            <pages:PanelsPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 4: 构建并运行验证**

Run: `dotnet build hello-avalonia.slnx`
Expected: 构建成功。

Run: `dotnet run --project Avalonia.LayoutDemo`
Expected: 窗口显示"布局面板"标签页，内有 8 张卡片。逐一核对：
- Grid 卡片中间色块横向拉伸
- DockPanel 卡片呈"上下左右 + 中心填充"的回字形
- StackPanel 卡片 5 个色块一字排开
- WrapPanel 卡片色块折成两行
- UniformGrid 卡片 3 列 2 行等分
- RelativePanel 卡片呈十字形
- Canvas 卡片色块阶梯状斜排
- Panel 卡片色块层层叠套
- 页面顶部说明条显示标题与 `docs/layout/choosing-a-layout-panel`

拉伸窗口宽度，确认 Canvas 卡片内色块位置不变、其余卡片跟随调整。确认后关闭窗口。

- [x] **Step 5: 提交**

```bash
git add Avalonia.LayoutDemo/
git commit -m "$(cat <<'EOF'
feat: compare the eight layout panels side by side

Each card hosts the same five blocks, so the arrangement difference comes
from the panel and nothing else.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

## Task 4: 功能点 2 — 对齐、Margin 与 Padding

**Files:**
- Create: `Avalonia.LayoutDemo/Views/Pages/PositioningPage.axaml`
- Create: `Avalonia.LayoutDemo/Views/Pages/PositioningPage.axaml.cs`
- Modify: `Avalonia.LayoutDemo/Views/MainWindow.axaml`（加第 2 个 TabItem）

**Interfaces:**
- Consumes: Task 1 的 `DemoHeader`；Task 2 的 TabControl 外壳
- Produces: `Avalonia.LayoutDemo.Views.Pages.PositioningPage`，无参构造的 `UserControl`

**设计要点**：这一页要讲清三个官方文档明确点出、但初学者最容易踩的坑：
1. `Stretch` 是默认值，但显式设了 `Width`/`Height` 后 `Stretch` 会被忽略
2. `Margin` 是元素**外**的间距，`Padding` 是元素**内**的间距，且 `Padding` 只有少数控件有
3. `Margin` 可以为负，会导致元素重叠或越界

这三点用可交互的滑块比静态截图更有说服力，所以本页用 `Slider` + `{Binding #name.Value}`
的元素到元素绑定（不需要 ViewModel）。

- [x] **Step 1: 创建 PositioningPage.axaml**

创建 `Avalonia.LayoutDemo/Views/Pages/PositioningPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.LayoutDemo.Views.Pages.PositioningPage">

    <UserControl.Styles>
        <Style Selector="Border.stage">
            <Setter Property="Background" Value="#20FFFFFF" />
            <Setter Property="BorderBrush" Value="#40FFFFFF" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="CornerRadius" Value="4" />
        </Style>
        <Style Selector="TextBlock.caption">
            <Setter Property="FontWeight" Value="SemiBold" />
            <Setter Property="Margin" Value="0,16,0,6" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="定位控件：对齐、外边距与内边距"
                               DocPath="docs/layout/positioning-controls" />

            <TextBlock Classes="caption" Text="1. HorizontalAlignment / VerticalAlignment — 子元素在父级分配空间内的落点" />
            <UniformGrid Columns="4" Height="120">
                <Border Classes="stage" Margin="3">
                    <Button HorizontalAlignment="Left" VerticalAlignment="Top" Content="Left / Top" />
                </Border>
                <Border Classes="stage" Margin="3">
                    <Button HorizontalAlignment="Center" VerticalAlignment="Center" Content="Center" />
                </Border>
                <Border Classes="stage" Margin="3">
                    <Button HorizontalAlignment="Right" VerticalAlignment="Bottom" Content="Right / Bottom" />
                </Border>
                <Border Classes="stage" Margin="3">
                    <!--  Stretch is the default for both axes, hence no attribute needed here.  -->
                    <Button Content="Stretch（默认值）" />
                </Border>
            </UniformGrid>

            <TextBlock Classes="caption" Text="2. 显式尺寸会压过 Stretch —— 两个按钮都是 Stretch，右边那个设了 Width" />
            <UniformGrid Columns="2" Height="70">
                <Border Classes="stage" Margin="3">
                    <Button Content="无 Width：撑满" />
                </Border>
                <Border Classes="stage" Margin="3">
                    <!--  An explicit Width wins: the Stretch request is silently ignored.  -->
                    <Button Width="150" Content="Width=150：不撑满" />
                </Border>
            </UniformGrid>

            <TextBlock Classes="caption" Text="3. Margin 是元素外的间距，Padding 是元素内的间距（拖动滑块对比）" />
            <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto">
                <StackPanel Grid.Row="0" Grid.Column="0" Margin="3">
                    <TextBlock Text="{Binding #MarginSlider.Value, StringFormat='Margin = {0:F0}'}" />
                    <Slider Name="MarginSlider" Minimum="-20" Maximum="40" Value="8" />
                </StackPanel>
                <StackPanel Grid.Row="0" Grid.Column="1" Margin="3">
                    <TextBlock Text="{Binding #PaddingSlider.Value, StringFormat='Padding = {0:F0}'}" />
                    <Slider Name="PaddingSlider" Minimum="0" Maximum="40" Value="8" />
                </StackPanel>

                <Border Grid.Row="1" Grid.Column="0" Classes="stage" Margin="3" Height="110">
                    <!--  Margin pushes the child away from the stage edge; negative values let it spill out.  -->
                    <Border Background="#E8564A" CornerRadius="3" Margin="{Binding #MarginSlider.Value}">
                        <TextBlock Margin="8" Text="我有 Margin" VerticalAlignment="Center" />
                    </Border>
                </Border>
                <Border Grid.Row="1" Grid.Column="1" Classes="stage" Margin="3" Height="110">
                    <!--  Padding grows the element itself: only Border, TemplatedControl and TextBlock expose it.  -->
                    <Border Background="#4AC7E8" CornerRadius="3" Padding="{Binding #PaddingSlider.Value}"
                            HorizontalAlignment="Center" VerticalAlignment="Center">
                        <TextBlock Text="我有 Padding" VerticalAlignment="Center" />
                    </Border>
                </Border>
            </Grid>

            <TextBlock Classes="caption" Text="4. Margin 的四值写法：左、上、右、下（顺时针从左开始）" />
            <Border Classes="stage" Height="110">
                <Border Background="#6FE84A" CornerRadius="3" Margin="40,10,5,25">
                    <TextBlock Margin="8" Text='Margin="40,10,5,25"' VerticalAlignment="Center" />
                </Border>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [x] **Step 2: 创建 PositioningPage.axaml.cs**

创建 `Avalonia.LayoutDemo/Views/Pages/PositioningPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.LayoutDemo.Views.Pages
{
    public partial class PositioningPage : UserControl
    {
        public PositioningPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 3: 挂到 TabControl**

修改 `Avalonia.LayoutDemo/Views/MainWindow.axaml`，在"布局面板"TabItem 之后加：

```xml
        <TabItem Header="定位与间距">
            <pages:PositioningPage />
        </TabItem>
```

- [x] **Step 4: 构建并运行验证**

Run: `dotnet build hello-avalonia.slnx`
Expected: 构建成功。

Run: `dotnet run --project Avalonia.LayoutDemo`
Expected: 切到"定位与间距"标签页，核对：
- 第 1 组 4 个方框中按钮分别位于左上、正中、右下、撑满
- 第 2 组左侧按钮撑满、右侧按钮宽 150 居中
- 拖动 Margin 滑块，红色块与外框的间距变化；拖到负值时红块越出外框边界
- 拖动 Padding 滑块，蓝色块自身尺寸变大，文字与块边缘间距变化
- 第 4 组绿色块四边间距明显不等

确认后关闭窗口。

若 `{Binding #MarginSlider.Value}` 报编译绑定错误，原因是编译绑定默认需要 `x:DataType`；
元素名绑定不依赖 DataContext，应当可用。若确实报错，把该绑定改为
`{Binding #MarginSlider.Value, Mode=OneWay}`；仍报错则在 `UserControl` 上加
`x:CompileBindings="False"` 并在注释中说明原因。

- [x] **Step 5: 提交**

```bash
git add Avalonia.LayoutDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate alignment, margin and padding

Sliders drive Margin and Padding live, which makes the outside-vs-inside
distinction and the negative-margin overflow visible rather than described.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

## Task 5: 功能点 3 — 响应式布局

**Files:**
- Create: `Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.cs`
- Create: `Avalonia.LayoutDemo/Views/Pages/ResponsivePage.axaml`
- Create: `Avalonia.LayoutDemo/Views/Pages/ResponsivePage.axaml.cs`
- Modify: `Avalonia.LayoutDemo/Views/MainWindow.axaml`（加第 3 个 TabItem）

**Interfaces:**
- Consumes: Task 1 的 `DemoHeader`；Task 2 的 TabControl 外壳；`Avalonia.Shared.ViewModels.ViewModelBase`
- Produces: `Avalonia.LayoutDemo.ViewModels.ResponsiveViewModel`，含
  `bool IsCompact`、`bool IsWide` 两个 observable 属性和 `void UpdateLayout(double width)`
  方法；`Avalonia.LayoutDemo.Views.Pages.ResponsivePage`，无参构造的 `UserControl`

官方 responsive-layouts 页列了 4 种手段，本页全部覆盖：容器查询、`OnFormFactor`/`OnPlatform`
标记扩展、换行面板、断点 ViewModel。

**已知风险**：容器查询（`ContainerQuery`）是 Avalonia 12 的较新特性，spec 风险条目 2 记录
了这一点。若 Step 3 构建失败，按该步骤内的回退说明处理。

- [x] **Step 1: 创建断点 ViewModel**

创建 `Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.cs`：

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.LayoutDemo.ViewModels
{
    /// <summary>
    /// Turns a raw width into named breakpoints. Use this approach when the
    /// decision depends on more than size, or drives logic rather than styling.
    /// </summary>
    public partial class ResponsiveViewModel : ViewModelBase
    {
        private const double CompactThreshold = 500d;

        [ObservableProperty]
        private bool _isCompact = true;

        [ObservableProperty]
        private bool _isWide;

        [ObservableProperty]
        private double _currentWidth;

        public void UpdateLayout(double width)
        {
            CurrentWidth = width;
            IsCompact = width < CompactThreshold;
            IsWide = !IsCompact;
        }
    }
}
```

注意 `ViewModelBase` 继承自 `CommunityToolkit.Mvvm` 的 `ObservableObject`，所以
`[ObservableProperty]` 的源生成器可以正常工作；类必须声明为 `partial`。

- [x] **Step 2: 创建 ResponsivePage.axaml.cs**

先写 code-behind，因为 XAML 里要绑定它建立的 DataContext。创建
`Avalonia.LayoutDemo/Views/Pages/ResponsivePage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.LayoutDemo.ViewModels;

namespace Avalonia.LayoutDemo.Views.Pages
{
    public partial class ResponsivePage : UserControl
    {
        private readonly ResponsiveViewModel _viewModel = new();

        public ResponsivePage()
        {
            InitializeComponent();
            DataContext = _viewModel;

            // SizeChanged is the hook for the breakpoint-view-model technique:
            // container queries cannot express non-size conditions, this can.
            SizeChanged += (_, e) => _viewModel.UpdateLayout(e.NewSize.Width);
        }
    }
}
```

- [x] **Step 3: 创建 ResponsivePage.axaml**

创建 `Avalonia.LayoutDemo/Views/Pages/ResponsivePage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.LayoutDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.LayoutDemo.Views.Pages.ResponsivePage"
             x:DataType="vm:ResponsiveViewModel">

    <UserControl.Styles>
        <Style Selector="TextBlock.caption">
            <Setter Property="FontWeight" Value="SemiBold" />
            <Setter Property="Margin" Value="0,16,0,6" />
        </Style>
        <Style Selector="Border.tile">
            <Setter Property="Background" Value="#4A7BE8" />
            <Setter Property="CornerRadius" Value="4" />
            <Setter Property="Height" Value="44" />
            <Setter Property="Margin" Value="3" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="响应式布局：横向拉伸窗口观察四种手段的反应"
                               DocPath="docs/layout/responsive-layouts" />

            <TextBlock Classes="caption" Text="1. 容器查询 —— 样式随祖先控件尺寸触发，类似 CSS media query" />
            <Border Name="QueryHost"
                    Container.Name="ResponsiveHost"
                    Container.Sizing="Width"
                    BorderBrush="#40FFFFFF"
                    BorderThickness="1"
                    CornerRadius="4"
                    Padding="8">
                <Border.Styles>
                    <!--  Tiered breakpoints: each query sets a different column count.  -->
                    <ContainerQuery Name="ResponsiveHost" Query="max-width:400">
                        <Style Selector="UniformGrid#TileGrid">
                            <Setter Property="Columns" Value="1" />
                        </Style>
                    </ContainerQuery>
                    <ContainerQuery Name="ResponsiveHost" Query="min-width:400 and max-width:700">
                        <Style Selector="UniformGrid#TileGrid">
                            <Setter Property="Columns" Value="2" />
                        </Style>
                    </ContainerQuery>
                    <ContainerQuery Name="ResponsiveHost" Query="min-width:700">
                        <Style Selector="UniformGrid#TileGrid">
                            <Setter Property="Columns" Value="4" />
                        </Style>
                    </ContainerQuery>
                </Border.Styles>
                <UniformGrid Name="TileGrid" Columns="1">
                    <Border Classes="tile" />
                    <Border Classes="tile" />
                    <Border Classes="tile" />
                    <Border Classes="tile" />
                </UniformGrid>
            </Border>

            <TextBlock Classes="caption" Text="2. OnFormFactor / OnPlatform —— 启动时解析一次，不响应运行时缩放" />
            <StackPanel Spacing="4">
                <TextBlock Text="{OnFormFactor Desktop='当前按桌面版式渲染', Mobile='当前按移动版式渲染', Default='未知设备类型'}" />
                <TextBlock Text="{OnPlatform Windows='运行于 Windows', macOS='运行于 macOS', Linux='运行于 Linux', Default='运行于其他平台'}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. 换行面板 —— 内容装不下就折行，最省事的响应式手段" />
            <WrapPanel>
                <Border Classes="tile" Width="120" />
                <Border Classes="tile" Width="120" />
                <Border Classes="tile" Width="120" />
                <Border Classes="tile" Width="120" />
                <Border Classes="tile" Width="120" />
                <Border Classes="tile" Width="120" />
            </WrapPanel>

            <TextBlock Classes="caption" Text="4. 断点 ViewModel —— 在代码里判定，可组合尺寸以外的条件" />
            <StackPanel Spacing="6">
                <TextBlock Text="{Binding CurrentWidth, StringFormat='当前页面宽度：{0:F0} px（阈值 500）'}" />
                <Border Background="#E8974A" CornerRadius="4" Padding="10" IsVisible="{Binding IsCompact}">
                    <TextBlock Text="窄版式：宽度小于 500 px 时显示这一块" />
                </Border>
                <Border Background="#6FE84A" CornerRadius="4" Padding="10" IsVisible="{Binding IsWide}">
                    <TextBlock Text="宽版式：宽度不小于 500 px 时显示这一块" Foreground="Black" />
                </Border>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**容器查询回退方案**：若构建时报 `ContainerQuery`、`Container.Name` 或 `Container.Sizing`
未找到，说明该 API 在 12.1.2 中形态与文档不符。此时：
1. 先执行 `dotnet build Avalonia.LayoutDemo 2>&1 | head -30` 记录确切错误
2. 把第 1 组整段（`<Border Name="QueryHost">` 到其 `</Border>`）替换为一个说明块：

```xml
            <Border BorderBrush="#40FFFFFF" BorderThickness="1" CornerRadius="4" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="容器查询在当前 Avalonia 12.1.2 中不可用（构建报错），待上游修复后补齐。其余三种手段见下方。" />
            </Border>
```
3. 在 spec 的"待验证的技术风险"第 2 条下追加一行实测结论
4. 向用户报告这一偏差，不要静默降级

- [x] **Step 4: 挂到 TabControl**

修改 `Avalonia.LayoutDemo/Views/MainWindow.axaml`，在"定位与间距"TabItem 之后加：

```xml
        <TabItem Header="响应式布局">
            <pages:ResponsivePage />
        </TabItem>
```

- [x] **Step 5: 构建并运行验证**

Run: `dotnet build hello-avalonia.slnx`
Expected: 构建成功。若容器查询报错，按 Step 3 的回退方案处理后重新构建。

Run: `dotnet run --project Avalonia.LayoutDemo`
Expected: 切到"响应式布局"标签页，把窗口从最窄拖到最宽，核对：
- 第 1 组方块列数依次为 1 → 2 → 4（若已回退则显示说明文字）
- 第 2 组显示"当前按桌面版式渲染"和"运行于 Windows"
- 第 3 组方块随宽度折行数变化
- 第 4 组宽度数字实时更新，跨过 500 px 时橙色块与绿色块互换显示

确认后关闭窗口。

- [x] **Step 6: 提交**

```bash
git add Avalonia.LayoutDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the four responsive layout techniques

Container queries, OnFormFactor/OnPlatform, wrapping panels and a
breakpoint view model, each reacting to the same window resize.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: 收尾 — README 与样板固化

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**Interfaces:**
- Consumes: Task 2–5 完成的 `Avalonia.LayoutDemo`
- Produces: 无代码产物。本任务把样板落成的实际结构回写到文档，供后续 4 个 plan 引用。

- [x] **Step 1: 更新 README 项目表格**

修改 `README.md`，在项目表格中 `Avalonia.DataTemplateDemo` 一行之后插入：

```markdown
| [Avalonia.LayoutDemo](Avalonia.LayoutDemo) | 8 种布局面板对照、对齐与 Margin/Padding、四种响应式手段 |
```

并把 `Avalonia.Shared` 一行的描述更新为（它现在多了共享控件）：

```markdown
| [Avalonia.Shared](Avalonia.Shared) | 共享类库：演示页说明条控件、窗口 Helper、Win32 互操作、消息载体、ViewModel 基类 |
```

- [x] **Step 2: 更新 README 约定小节**

修改 `README.md` 的"约定"小节，把语言约定一条扩展为界面/注释分离的表述：

```markdown
- 新增代码与 XAML 注释用英文，界面文字（Tab 标题、说明条、按钮文案）用中文；从 hello-dotnet 迁移来的既有中文注释保持原样
```

- [x] **Step 3: 回写实测结论到 spec**

在 spec 的"待验证的技术风险"小节末尾追加一段，如实记录本次样板中验证到的结果：

```markdown
### 样板阶段实测结论（2026-09-21）

- 容器查询：<填写实际结果——可用 / 不可用及错误信息>
- `Avalonia.Shared` 承载 XAML 控件：可行，需增加 `Avalonia.Themes.Fluent` 包引用
- 风险 1（Headless 测试包）与风险 3（Services 桌面可用性）不在样板范围，留待对应 plan 验证
```

把尖括号占位替换为 Task 5 的实际结果。

- [x] **Step 4: 全量构建与运行验证**

Run: `dotnet build hello-avalonia.slnx`
Expected: 全部 6 个项目构建成功，无错误。

Run: `dotnet run --project Avalonia.LayoutDemo`
Expected: 三个标签页都能切换且内容正常。

Run: `git status`
Expected: 工作区干净（除本任务待提交的文档改动外无遗留文件）。

- [x] **Step 5: 提交**

```bash
git add README.md docs/
git commit -m "$(cat <<'EOF'
docs: register LayoutDemo and record template findings

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [x] **Step 6: 交回用户 review**

样板已完成。向用户报告：
- 三个标签页各演示了什么
- 容器查询的实测结果
- `DemoHeader` 的最终样子与用法
- 请用户确认这套结构可作为后续 14 个项目的模板

用户确认后，再为 spec 的第二阶段 4 个分组分别编写 plan。**不要在用户确认前开始下一个
项目**——样板的意义就在于先定型。
