# Avalonia AppDevelopmentDemo Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补齐 `Avalonia.AppDevelopmentDemo` 对官方 `app-development` 14 个顶层主题的覆盖，并为本地化、资源、异常处理和线程模型补充可稳定验证的关键示例。

**Architecture:** `MainWindow` 只负责按官方顺序导航；页面负责说明或触发局部 UI 示例；应用级异常处理继续放在 `App` 和服务层。新增设计时与性能数据使用独立 ViewModel，原生互操作只读取跨平台句柄抽象，不调用平台专用 API。

**Tech Stack:** .NET 10、Avalonia 12、AXAML、CommunityToolkit.Mvvm（沿用项目依赖）、Microsoft.Extensions.DependencyInjection、Microsoft.Extensions.Logging、xUnit v3、Avalonia.Headless.XUnit。

**Spec:** `docs/superpowers/specs/2026-10-25-app-development-demo-coverage-design.md`

## Global Constraints

- 主窗口必须按官方目录顺序提供 14 个标签页。
- Page 不创建 DI 容器、不替换全局日志接收器、不订阅应用级异常事件。
- 不新增桌面以外的平台启动项目，不增加 P/Invoke、`NativeControlHost` 或第三方原生库。
- 性能页不展示机器相关耗时，不把 UI 示例当作微基准。
- 新增代码与 AXAML 注释使用英文，用户可见文字使用中文。
- 使用 L0 与 L1 Headless 测试；不新增 Appium 测试。
- 每个任务完成后运行该任务指定测试并单独提交，提交主题使用英文 Conventional Commits。

## File Map

- `Views/MainWindow.axaml`：14 个主题的唯一导航顺序。
- `Views/Pages/CrossPlatformSetupPage.*`：跨平台项目职责说明，不包含运行时平台项目生成。
- `Views/Pages/XamlDesignTimePage.*`、`ViewModels/DesignTimeSampleViewModel.cs`：真实设计时 DataContext 和独立运行时数据。
- `Views/Pages/PerformancePage.*`、`ViewModels/PerformancePageViewModel.cs`：虚拟化列表、编译绑定和容器统计。
- `Views/Pages/NativeInteropPage.*`：读取 `TopLevel.TryGetPlatformHandle()` 并说明互操作边界。
- `LocalizationPage.*`、`Strings.ar-SA.resx`：文化、格式化与 RTL。
- `Themes/AppDevelopmentResources.axaml`、`ResourcesPage.*`：合并资源字典和主题变体。
- `App.axaml.cs`、`ApplicationExceptionReporter.cs`：应用级异常过滤阶段。
- `ThreadingPage.*`：`InvokeAsync` 与 `DispatcherPriority.Background`。
- `SmokeTests.cs`、`InteractionTests.cs`、`ServiceTests.cs`：L0/L1 回归测试。

---

### Task 1: 锁定 14 个主题的导航结构

**Files:**
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/CrossPlatformSetupPage.axaml`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/CrossPlatformSetupPage.axaml.cs`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/XamlDesignTimePage.axaml`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/XamlDesignTimePage.axaml.cs`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/PerformancePage.axaml`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/PerformancePage.axaml.cs`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/NativeInteropPage.axaml`
- Create: `src/Avalonia.AppDevelopmentDemo/Views/Pages/NativeInteropPage.axaml.cs`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/MainWindow.axaml`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/SmokeTests.cs`

**Interfaces:**
- Produces: `CrossPlatformSetupPage`, `XamlDesignTimePage`, `PerformancePage`, `NativeInteropPage` parameterless controls loadable by AXAML.
- Produces: the exact tab sequence used by all later tasks.

- [ ] **Step 1: Change the tab-walking test to the required order**

Replace the expected header array in `Main_window_walks_every_tab_without_errors` with:

```csharp
new[]
{
    "跨平台结构", "依赖注入", "本地化", "Web 内容", "日志", "未处理异常", "资源",
    "XAML 设计时", "数据校验", "线程模型", "窗口管理", "性能", "无障碍", "原生互操作"
}
```

In `Logging_tab_does_not_replace_the_application_log_sink`, change the logging and exception indices from `3/4` to `4/5`.

- [ ] **Step 2: Run the smoke tests and verify the new contract fails**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~SmokeTests"
```

Expected: `Main_window_walks_every_tab_without_errors` fails because only 10 headers exist; the logging-index assertion also fails until the new first tab is inserted.

- [ ] **Step 3: Add four loadable page shells**

Each AXAML file must use the existing shared header and a `ScrollViewer`. Use these titles and paths:

```xml
<shared:DemoHeader Title="跨平台解决方案结构：共享核心与平台启动项目"
                   DocPath="app-development/cross-platform-solution-setup" />
<shared:DemoHeader Title="XAML 预览与设计时设置"
                   DocPath="app-development/xaml-preview-and-design-settings" />
<shared:DemoHeader Title="性能：虚拟化、编译绑定与分析"
                   DocPath="app-development/performance" />
<shared:DemoHeader Title="原生互操作：句柄、平台服务与原生控件"
                   DocPath="app-development/native-interop" />
```

Use the normal code-behind shape for every page:

```csharp
using Avalonia.Controls;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class CrossPlatformSetupPage : UserControl
    {
        public CrossPlatformSetupPage()
        {
            InitializeComponent();
        }
    }
}
```

Use the matching class name for the other three files. At this stage, put one Chinese explanatory `TextBlock` below each header so all shells are valid and visible.

- [ ] **Step 4: Insert the tabs in official order**

Add `CrossPlatformSetupPage` before dependency injection, `XamlDesignTimePage` after resources, `PerformancePage` after window management, and `NativeInteropPage` after accessibility:

```xml
<TabItem Header="跨平台结构">
    <pages:CrossPlatformSetupPage />
</TabItem>
```

Repeat the same shape with headers `XAML 设计时`, `性能`, and `原生互操作`.

- [ ] **Step 5: Run smoke tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~SmokeTests"
```

Expected: all `SmokeTests` pass, including all 14 pages loading without binding warnings.

- [ ] **Step 6: Commit the navigation skeleton**

```powershell
git add src/Avalonia.AppDevelopmentDemo/Views tests/Avalonia.AppDevelopmentDemo.Tests/SmokeTests.cs
git commit -m "feat: cover all app development topics"
```

---

### Task 2: 完成跨平台结构与 XAML 设计时示例

**Files:**
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/CrossPlatformSetupPage.axaml`
- Create: `src/Avalonia.AppDevelopmentDemo/ViewModels/DesignTimeSampleViewModel.cs`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/XamlDesignTimePage.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/XamlDesignTimePage.axaml.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`

**Interfaces:**
- Produces: `DesignTimeSampleViewModel.CreateRuntime()` returning runtime-only sample text.
- Consumes: `XamlDesignTimePage` shell from Task 1.

- [ ] **Step 1: Add a failing runtime/design-data separation test**

Add to `InteractionTests`:

```csharp
[AvaloniaFact]
public void Xaml_design_time_page_uses_runtime_data_when_the_application_runs()
{
    var window = UiHelpers.Show(new XamlDesignTimePage());

    Assert.Equal("运行时数据", Text(window, "DesignTitle"));
    Assert.Equal("Design.DataContext 只服务预览器，不会覆盖运行时 DataContext。", Text(window, "DesignSummary"));
}
```

- [ ] **Step 2: Run the new test and verify it fails**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Xaml_design_time_page"
```

Expected: FAIL because the named text blocks and ViewModel do not exist.

- [ ] **Step 3: Add the focused design-time ViewModel**

Create `DesignTimeSampleViewModel.cs`:

```csharp
namespace Avalonia.AppDevelopmentDemo.ViewModels
{
    public sealed class DesignTimeSampleViewModel
    {
        public string Title { get; init; } = "设计器数据";

        public string Summary { get; init; } = "这段文字来自 Design.DataContext。";

        public static DesignTimeSampleViewModel CreateRuntime() => new()
        {
            Title = "运行时数据",
            Summary = "Design.DataContext 只服务预览器，不会覆盖运行时 DataContext。"
        };
    }
}
```

- [ ] **Step 4: Turn the design page into a real previewer example**

Set these root attributes on `XamlDesignTimePage.axaml`:

```xml
xmlns:viewModels="using:Avalonia.AppDevelopmentDemo.ViewModels"
x:DataType="viewModels:DesignTimeSampleViewModel"
d:DesignWidth="760"
d:DesignHeight="560"
```

Add design data and compiled bindings:

```xml
<Design.DataContext>
    <viewModels:DesignTimeSampleViewModel />
</Design.DataContext>

<Border Classes="stage" Padding="12">
    <StackPanel Spacing="6">
        <TextBlock Name="DesignTitle" FontSize="20" Text="{Binding Title}" />
        <TextBlock Name="DesignSummary" TextWrapping="Wrap" Text="{Binding Summary}" />
    </StackPanel>
</Border>
```

Set runtime data in the constructor:

```csharp
DataContext = DesignTimeSampleViewModel.CreateRuntime();
```

Complete `CrossPlatformSetupPage.axaml` with a read-only `Core -> Desktop / Android / iOS / Browser` layout and four concise responsibility rows. Do not add platform projects or buttons.

- [ ] **Step 5: Run the focused test and smoke tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Xaml_design_time_page|FullyQualifiedName~SmokeTests"
```

Expected: all selected tests pass and no binding warnings are recorded.

- [ ] **Step 6: Commit the documentation-oriented pages**

```powershell
git add src/Avalonia.AppDevelopmentDemo/ViewModels/DesignTimeSampleViewModel.cs src/Avalonia.AppDevelopmentDemo/Views/Pages/CrossPlatformSetupPage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/XamlDesignTimePage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/XamlDesignTimePage.axaml.cs tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs
git commit -m "feat: demonstrate cross-platform and design-time setup"
```

---

### Task 3: 增加可验证的虚拟化与编译绑定示例

**Files:**
- Create: `src/Avalonia.AppDevelopmentDemo/ViewModels/PerformancePageViewModel.cs`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/PerformancePage.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/PerformancePage.axaml.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs`

**Interfaces:**
- Produces: `PerformancePageViewModel.Items : IReadOnlyList<PerformanceItem>` containing exactly 1,000 items.
- Produces: `PerformanceItem(int Index, string Name)` for compiled item-template binding.

- [ ] **Step 1: Add failing tests for the data and realized-container result**

Add an L0 test to `ServiceTests.cs`:

```csharp
[Fact]
public void Performance_view_model_builds_a_stable_thousand_item_data_set()
{
    var viewModel = new PerformancePageViewModel();

    Assert.Equal(1000, viewModel.Items.Count);
    Assert.Equal(new PerformanceItem(1, "项目 0001"), viewModel.Items[0]);
    Assert.Equal(new PerformanceItem(1000, "项目 1000"), viewModel.Items[^1]);
}
```

Add `using Avalonia.AppDevelopmentDemo.ViewModels;` to `ServiceTests.cs`.

Add an L1 test to `InteractionTests.cs`:

```csharp
[AvaloniaFact]
public void Performance_page_reports_fewer_realized_containers_than_data_items()
{
    var window = UiHelpers.Show(new PerformancePage());

    Press(window, "MeasureRealizedButton");

    Assert.Equal("数据项：1000", Text(window, "ItemCountText"));
    Assert.StartsWith("已实现容器：", Text(window, "RealizedCountText"));
    Assert.DoesNotContain("已实现容器：1000", Text(window, "RealizedCountText"));
}
```

- [ ] **Step 2: Run both tests and verify failure**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Performance_"
```

Expected: FAIL because the ViewModel and named controls do not exist.

- [ ] **Step 3: Implement the data model**

Create:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.ViewModels
{
    public sealed record PerformanceItem(int Index, string Name);

    public sealed class PerformancePageViewModel
    {
        public IReadOnlyList<PerformanceItem> Items { get; } = Enumerable.Range(1, 1000)
            .Select(index => new PerformanceItem(index, $"项目 {index:0000}"))
            .ToArray();
    }
}
```

- [ ] **Step 4: Implement the virtualized list with compiled item bindings**

Use `x:DataType="viewModels:PerformancePageViewModel"`, set the page DataContext to a new ViewModel, and add:

```xml
<TextBlock Name="ItemCountText" Text="数据项：1000" />
<ListBox Name="VirtualizedList" Height="220" ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="viewModels:PerformanceItem">
            <Grid ColumnDefinitions="60,*">
                <TextBlock Text="{Binding Index}" />
                <TextBlock Grid.Column="1" Text="{Binding Name}" />
            </Grid>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
<Button Name="MeasureRealizedButton" Content="读取已实现容器数" Click="OnMeasureRealized" />
<TextBlock Name="RealizedCountText" Text="已实现容器：（未读取）" />
```

Implement the button without timing measurements:

```csharp
private void OnMeasureRealized(object? sender, RoutedEventArgs e)
{
    var realized = VirtualizedList.GetVisualDescendants().OfType<ListBoxItem>().Count();
    RealizedCountText.Text = $"已实现容器：{realized}";
}
```

Add a read-only checklist for shallow layout, compiled/one-time bindings, batched collection changes, async work and profiling.

- [ ] **Step 5: Run focused tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Performance_"
```

Expected: both tests pass. If Headless realizes zero containers, that is valid; it must still be less than 1,000.

- [ ] **Step 6: Commit the performance example**

```powershell
git add src/Avalonia.AppDevelopmentDemo/ViewModels/PerformancePageViewModel.cs src/Avalonia.AppDevelopmentDemo/Views/Pages/PerformancePage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/PerformancePage.axaml.cs tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs
git commit -m "feat: demonstrate Avalonia performance practices"
```

---

### Task 4: 增加跨平台安全的原生句柄示例

**Files:**
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/NativeInteropPage.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/NativeInteropPage.axaml.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`

**Interfaces:**
- Produces: `NativeInteropPage.RefreshPlatformHandle()` which displays either a handle descriptor/value or a supported unavailable state.

- [ ] **Step 1: Add a failing Headless-safe test**

```csharp
[AvaloniaFact]
public void Native_interop_page_reports_the_platform_handle_without_calling_native_code()
{
    var window = UiHelpers.Show(new NativeInteropPage());
    Dispatcher.UIThread.RunJobs();

    var text = Text(window, "PlatformHandleText");
    Assert.NotEqual("原生句柄：（尚未读取）", text);
    Assert.StartsWith("原生句柄：", text);
}
```

- [ ] **Step 2: Run the test and verify failure**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Native_interop_page"
```

Expected: FAIL because `PlatformHandleText` does not exist.

- [ ] **Step 3: Implement handle inspection at the control lifecycle boundary**

Add a named result and read-only explanations of `NativeControlHost`, P/Invoke/native library loading, and platform services through DI. Override `OnLoaded`:

```csharp
protected override void OnLoaded(RoutedEventArgs e)
{
    base.OnLoaded(e);
    RefreshPlatformHandle();
}

internal void RefreshPlatformHandle()
{
    var handle = TopLevel.GetTopLevel(this)?.TryGetPlatformHandle();
    PlatformHandleText.Text = handle is null
        ? "原生句柄：当前平台或测试宿主未提供"
        : $"原生句柄：{handle.HandleDescriptor} = 0x{handle.Handle.ToInt64():X}";
}
```

Do not add `[DllImport]`, `LibraryImport`, OS-specific compile constants or handle-dependent actions.

- [ ] **Step 4: Run the focused and smoke tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Native_interop_page|FullyQualifiedName~SmokeTests"
```

Expected: selected tests pass on Headless regardless of whether a handle exists.

- [ ] **Step 5: Commit**

```powershell
git add src/Avalonia.AppDevelopmentDemo/Views/Pages/NativeInteropPage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/NativeInteropPage.axaml.cs tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs
git commit -m "feat: demonstrate safe native handle access"
```

---

### Task 5: 扩展 RESX 本地化、格式化与 RTL

**Files:**
- Create: `src/Avalonia.AppDevelopmentDemo/Assets/Langs/Strings.ar-SA.resx`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/LocalizationPage.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/LocalizationPage.axaml.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`

**Interfaces:**
- Produces: language options `zh-CN`, `en-US`, `ar-SA`.
- Produces: `FormatText` and `DirectionText` output derived from the selected culture.

- [ ] **Step 1: Extend the localization interaction test first**

Wrap the existing test in saved-culture restoration and append Arabic assertions:

```csharp
var savedCulture = CultureInfo.CurrentCulture;
var savedUiCulture = CultureInfo.CurrentUICulture;
try
{
    var window = UiHelpers.Show(new LocalizationPage());
    window.Click(window.ByName<RadioButton>("ArabicRadio"));
    Dispatcher.UIThread.RunJobs();

    Assert.Equal("ar-SA", CultureInfo.CurrentCulture.Name);
    Assert.Equal("ar-SA", CultureInfo.CurrentUICulture.Name);
    Assert.Equal("اتجاه المعاينة: RightToLeft", Text(window, "DirectionText"));
    Assert.Contains("12", Text(window, "FormatText"));
}
finally
{
    CultureInfo.CurrentCulture = savedCulture;
    CultureInfo.CurrentUICulture = savedUiCulture;
}
```

Keep the existing Chinese and English assertions inside the same `try` block.

- [ ] **Step 2: Run the localization test and verify failure**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Switching_language"
```

Expected: FAIL because `ArabicRadio`, `DirectionText`, `FormatText`, and the Arabic resource do not exist.

- [ ] **Step 3: Add Arabic RESX values**

Create a normal `.resx` matching the existing files, with these data entries:

```xml
<data name="Greeting" xml:space="preserve">
  <value>مرحباً بالعالم</value>
</data>
<data name="Farewell" xml:space="preserve">
  <value>إلى اللقاء</value>
</data>
```

The existing `AvaloniaResource Remove="Assets\Langs\*.resx"` already prevents Avalonia from treating it as an asset.

- [ ] **Step 4: Implement culture, formatting and direction**

Add `ArabicRadio`, a named preview panel, `FormatText`, and `DirectionText`. Resolve the culture from the sender:

```csharp
var cultureName = ReferenceEquals(radio, EnglishRadio)
    ? "en-US"
    : ReferenceEquals(radio, ArabicRadio) ? "ar-SA" : "zh-CN";
Apply(new CultureInfo(cultureName));
```

Update `Apply`:

```csharp
CultureInfo.CurrentCulture = culture;
CultureInfo.CurrentUICulture = culture;
GreetingText.Text = Strings.GetString("Greeting", culture);
FarewellText.Text = Strings.GetString("Farewell", culture);
CultureText.Text = $"culture = {culture.Name}";
FormatText.Text = string.Format(culture, "{0:N2} · {1:D}", 12345.67, new DateTime(2026, 10, 25));
var direction = culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
LocalizedPreview.FlowDirection = direction;
DirectionText.Text = $"اتجاه المعاينة: {direction}";
```

Use a Chinese label such as `预览方向` outside the RTL preview; keep `DirectionText` inside the result area so the test reads a stable value.

- [ ] **Step 5: Run localization tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Switching_language"
```

Expected: the test passes and restores both cultures even when an assertion fails.

- [ ] **Step 6: Commit**

```powershell
git add src/Avalonia.AppDevelopmentDemo/Assets/Langs/Strings.ar-SA.resx src/Avalonia.AppDevelopmentDemo/Views/Pages/LocalizationPage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/LocalizationPage.axaml.cs tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs
git commit -m "feat: demonstrate culture formatting and RTL"
```

---

### Task 6: 增加合并字典与主题变体资源

**Files:**
- Create: `src/Avalonia.AppDevelopmentDemo/Themes/AppDevelopmentResources.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`

**Interfaces:**
- Produces: merged resource key `MergedAccentBrush`.
- Produces: theme resource key `ThemeAccentBrush` for Light and Dark variants.

- [ ] **Step 1: Add failing resource assertions**

Extend `Replacing_a_resource_moves_only_the_dynamic_swatch_but_mutating_it_moves_both`:

```csharp
Assert.Equal("合并字典：CornflowerBlue", Text(window, "MergedLine"));

Press(window, "LightThemeButton");
Assert.Equal("主题资源：#FF1D4ED8", Text(window, "ThemeLine"));

Press(window, "DarkThemeButton");
Assert.Equal("主题资源：#FF93C5FD", Text(window, "ThemeLine"));
```

- [ ] **Step 2: Run the resource test and verify failure**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Replacing_a_resource"
```

Expected: FAIL because the merged/theme controls and resource dictionary do not exist.

- [ ] **Step 3: Create the merged and theme-variant dictionary**

Create:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Light">
            <SolidColorBrush x:Key="ThemeAccentBrush" Color="#1D4ED8" />
        </ResourceDictionary>
        <ResourceDictionary x:Key="Dark">
            <SolidColorBrush x:Key="ThemeAccentBrush" Color="#93C5FD" />
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
    <SolidColorBrush x:Key="MergedAccentBrush" Color="CornflowerBlue" />
</ResourceDictionary>
```

- [ ] **Step 4: Merge and display the resources**

Change `UserControl.Resources` to a `ResourceDictionary` containing the existing `Brand` brush and:

```xml
<ResourceDictionary.MergedDictionaries>
    <MergeResourceInclude Source="avares://Avalonia.AppDevelopmentDemo/Themes/AppDevelopmentResources.axaml" />
</ResourceDictionary.MergedDictionaries>
```

Add a `ThemeVariantScope Name="ThemeScope"`, a swatch with `Background="{DynamicResource ThemeAccentBrush}"`, `MergedLine`, `ThemeLine`, and `LightThemeButton`/`DarkThemeButton`.

Implement theme switching:

```csharp
private void OnLightTheme(object? sender, RoutedEventArgs e)
{
    ThemeScope.RequestedThemeVariant = ThemeVariant.Light;
    Dispatcher.UIThread.Post(RefreshThemeLines, DispatcherPriority.Loaded);
}

private void OnDarkTheme(object? sender, RoutedEventArgs e)
{
    ThemeScope.RequestedThemeVariant = ThemeVariant.Dark;
    Dispatcher.UIThread.Post(RefreshThemeLines, DispatcherPriority.Loaded);
}
```

`RefreshThemeLines` must read the actual brushes and format them through the existing `Describe` helper:

```csharp
MergedLine.Text = $"合并字典：{Describe(MergedSwatch.Background)}";
ThemeLine.Text = $"主题资源：{Describe(ThemeSwatch.Background)}";
```

- [ ] **Step 5: Run the resource and smoke tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Replacing_a_resource|FullyQualifiedName~SmokeTests"
```

Expected: selected tests pass with no resource lookup or binding warnings.

- [ ] **Step 6: Commit**

```powershell
git add src/Avalonia.AppDevelopmentDemo/Themes/AppDevelopmentResources.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/ResourcesPage.axaml.cs tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs
git commit -m "feat: demonstrate merged and theme resources"
```

---

### Task 7: 在应用生命周期接入异常过滤阶段

**Files:**
- Modify: `src/Avalonia.AppDevelopmentDemo/Services/ApplicationExceptionReporter.cs`
- Modify: `src/Avalonia.AppDevelopmentDemo/App.axaml.cs`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/UnhandledExceptionsPage.axaml`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs`

**Interfaces:**
- Produces: `ApplicationExceptionReporter.ReportUiThreadExceptionFilter(Exception exception, bool requestCatch)`.
- Consumes: the existing singleton reporter resolved by `App`.

- [ ] **Step 1: Add a failing reporter test**

```csharp
[Fact]
public void Application_exception_reporter_records_the_ui_filter_stage()
{
    using var services = ApplicationServices.CreateServiceProvider();
    var reporter = services.GetRequiredService<ApplicationExceptionReporter>();
    var entries = services.GetRequiredService<ListLoggerProvider>().Entries;

    reporter.ReportUiThreadExceptionFilter(new InvalidOperationException("boom"), requestCatch: true);

    var entry = Assert.Single(entries);
    Assert.Equal("Debug:UI-thread exception filter (RequestCatch: True) | InvalidOperationException: boom", entry);
}
```

- [ ] **Step 2: Run the test and verify failure**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~records_the_ui_filter_stage"
```

Expected: compile failure because `ReportUiThreadExceptionFilter` is missing.

- [ ] **Step 3: Implement the reporter method**

```csharp
public void ReportUiThreadExceptionFilter(Exception exception, bool requestCatch)
{
    _logger.LogDebug(exception, "UI-thread exception filter (RequestCatch: {RequestCatch})", requestCatch);
}
```

- [ ] **Step 4: Subscribe and unsubscribe in `App`**

Subscribe immediately before the existing unhandled-exception subscription:

```csharp
Dispatcher.UIThread.UnhandledExceptionFilter += OnDispatcherUnhandledExceptionFilter;
Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
```

Add the handler without changing `RequestCatch`:

```csharp
private void OnDispatcherUnhandledExceptionFilter(object? sender, DispatcherUnhandledExceptionFilterEventArgs e)
{
    _exceptionReporter?.ReportUiThreadExceptionFilter(e.Exception, e.RequestCatch);
}
```

Unsubscribe in `OnDesktopExit` in reverse order. Update the page text to explain that the filter runs before the handler, can request whether Avalonia catches the exception, and does not itself mark the exception handled.

- [ ] **Step 5: Run service and smoke tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Application_exception_reporter|FullyQualifiedName~SmokeTests"
```

Expected: selected tests pass. No test should intentionally crash the dispatcher.

- [ ] **Step 6: Commit**

```powershell
git add src/Avalonia.AppDevelopmentDemo/App.axaml.cs src/Avalonia.AppDevelopmentDemo/Services/ApplicationExceptionReporter.cs src/Avalonia.AppDevelopmentDemo/Views/Pages/UnhandledExceptionsPage.axaml tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs
git commit -m "feat: register the UI exception filter"
```

---

### Task 8: 增加 `InvokeAsync` 和调度优先级示例

**Files:**
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/ThreadingPage.axaml`
- Modify: `src/Avalonia.AppDevelopmentDemo/Views/Pages/ThreadingPage.axaml.cs`
- Modify: `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`

**Interfaces:**
- Produces: `InvokeButton` action that awaits a result scheduled with `DispatcherPriority.Background`.

- [ ] **Step 1: Add the failing interaction test**

```csharp
[AvaloniaFact]
public async Task Invoke_async_returns_a_value_from_the_ui_thread()
{
    var window = UiHelpers.Show(new ThreadingPage());

    window.Click(window.ByName<Button>("InvokeButton"));
    await UiHelpers.WaitUntil(() => Text(window, "Target") == "written via InvokeAsync");

    Assert.Equal("InvokeAsync(Background) 的回调里 CheckAccess = True，已等待返回值", Text(window, "Result"));
}
```

- [ ] **Step 2: Run the test and verify failure**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~Invoke_async_returns"
```

Expected: FAIL because `InvokeButton` does not exist.

- [ ] **Step 3: Implement the example**

Add a fourth action section and button:

```xml
<Button Name="InvokeButton"
        Content="后台线程 InvokeAsync 并等待结果"
        Click="OnInvoke" />
```

Implement:

```csharp
private async void OnInvoke(object? sender, RoutedEventArgs e)
{
    var message = await Task.Run(async () =>
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Target.Text = "written via InvokeAsync";
            return $"InvokeAsync(Background) 的回调里 CheckAccess = {Dispatcher.UIThread.CheckAccess()}，已等待返回值";
        }, DispatcherPriority.Background);
    });

    Result.Text = message;
}
```

Renumber the result section and update the hint to distinguish `Post` fire-and-forget from awaitable `InvokeAsync` and to explain that priority is ordering, not a new thread.

- [ ] **Step 4: Run all threading tests**

Run:

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --filter "FullyQualifiedName~background_thread|FullyQualifiedName~Posting_back|FullyQualifiedName~Code_after_await|FullyQualifiedName~Invoke_async"
```

Expected: all four threading tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/Avalonia.AppDevelopmentDemo/Views/Pages/ThreadingPage.axaml src/Avalonia.AppDevelopmentDemo/Views/Pages/ThreadingPage.axaml.cs tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs
git commit -m "feat: demonstrate dispatcher invoke priority"
```

---

### Task 9: 完整验证与文档状态收尾

**Files:**
- Modify: `docs/superpowers/specs/2026-10-25-app-development-demo-coverage-design.md`
- Verify: `src/Avalonia.AppDevelopmentDemo/**`
- Verify: `tests/Avalonia.AppDevelopmentDemo.Tests/**`

**Interfaces:**
- Consumes: all deliverables from Tasks 1-8.
- Produces: a fully verified AppDevelopmentDemo with an updated design status.

- [ ] **Step 1: Run the complete AppDevelopmentDemo test project**

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --nologo --verbosity minimal
```

Expected: zero failed tests and zero unexpected skipped tests.

- [ ] **Step 2: Build the entire solution**

```powershell
dotnet build hello-avalonia.slnx --nologo --verbosity minimal
```

Expected: build succeeds with zero errors. Existing unrelated warnings may remain, but no new warning may point to files changed by this plan.

- [ ] **Step 3: Verify document paths and coverage mechanically**

Run:

```powershell
rg -n 'DocPath="app-development/' src/Avalonia.AppDevelopmentDemo/Views/Pages
rg -n 'Header="' src/Avalonia.AppDevelopmentDemo/Views/MainWindow.axaml
git status --short
```

Expected: the four new official document paths are present; `MainWindow.axaml` contains 14 `TabItem` headers; Git status contains only intentional implementation and documentation changes.

- [ ] **Step 4: Update the spec status**

Change:

```markdown
状态：已批准，待实施
```

to:

```markdown
状态：已实施并验证
```

Append the exact test count and build result observed in Steps 1-2 under a new `## 实施验证` section. Do not predict counts before running the tests.

- [ ] **Step 5: Commit verification metadata**

```powershell
git add docs/superpowers/specs/2026-10-25-app-development-demo-coverage-design.md
git commit -m "docs: record app development demo verification"
```

---

## Plan Self-Review

- Spec coverage: Tasks 1-4 cover all four missing top-level themes; Tasks 5-8 cover the approved high-value gaps; Task 9 covers focused tests, full project tests and solution build.
- Infrastructure boundary: only Task 7 touches application-level event subscriptions; Page tasks contain local UI behavior only.
- Cross-platform boundary: native interop reads `IPlatformHandle` but never invokes platform-specific code.
- Test-layer boundary: pure data/reporter behavior uses `[Fact]`; controls, resources, dispatcher and window handles use `[AvaloniaFact]`.
- No task adds Appium, new platform projects, microbenchmark timing, P/Invoke or third-party native dependencies.
