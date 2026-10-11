# ControlsDemo 06：Primitives 5 页、System 的 TrayIcon，以及 Window、Dialogs、数据持久化三篇实战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全 `Avalonia.ControlsDemo` 的 Primitives 与 System 两个分类：Primitives 5 页（ScrollBar、ThemeVariantScope、UserControl、Window、WindowDrawnDecorations）、System 1 页（TrayIcon），共 6 个 Control 页；加 Window、Dialogs、数据持久化 3 篇 How-to 实战页。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份新增页面文件、一个自定义 `UserControl`（`SwatchView`）、对话框辅助类与 `IDialogService`、数据持久化的 `AppSettings` / `SettingsStore` / `RecentFiles`、三个实战页 ViewModel、`PageCatalog.Primitives.cs` / `PageCatalog.System.cs` 的登记行，以及探针 `Probe.Prim.cs` / `Probe.Prim2.cs`。不引入新包。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2、`System.Text.Json`

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00–05 份已执行（`PageCatalog`、`Categories.Primitives` / `Categories.System`、`Harness` 已存在；`AddPrimitives`、`AddSystem` 目前是空方法；`Assets/avalonia-logo.ico` 已随第 00 份建立）。

> **编号说明：** 同第 05 份——第 00 份开头那张"9 份计划"表已作废，以 `docs/superpowers/plans/` 下实际存在的文件为准。本份是 06，下一份 07 是 Web / Premium / How-to 路标。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」，**不重复贴**，执行者必须先读第 00 份；第 01–05 份「页面的写法约定」同样适用。本份额外强调：

- 页面类名 `<控件>Page`（`ScrollBarPage`、`ThemeVariantScopePage`、`UserControlPage`、`WindowPage`、`WindowDrawnDecorationsPage`、`TrayIconPage`），How-to 页 `<主题>HowToPage`（`WindowHowToPage`、`DialogsHowToPage`、`DataPersistenceHowToPage`）；命名空间一律 `Avalonia.ControlsDemo.Views.Pages`，模型与 ViewModel 一律 `Avalonia.ControlsDemo.ViewModels`；页面目录 `Views/Pages/Primitives/` 与 `Views/Pages/System/`
- `DocPath` 已用官方 `sitemap.xml` 核对：`controls/primitives/<控件>`；`TrayIcon` 的官方路径是 `controls/navigation/trayicon`（官方 URL 在 navigation 下、侧边栏却归 System，按侧边栏分类登记）；How-to 页 `docs/how-to/window-how-to`、`dialogs-how-to`、`data-persistence-how-to`
- **事件接线放在构造函数里、`InitializeComponent()` 之后**；枚举下拉先设 `SelectedItem` 再订阅 `SelectionChanged`
- 绝不在元素上写属性的默认值（硬性规则 1）
- **页面开出来的窗口、托盘图标一律在页面 `DetachedFromVisualTree` 时关掉 / 还原**，否则页面每切一次就在桌面上多一个孤儿窗口
- **演示不能碰用户的真实文件**：数据持久化页的 `SettingsStore` 只写 `%AppData%\Avalonia.ControlsDemo\`，且只在用户操作时写；探针一律用临时目录
- 事件读数一律用**计数**，不读事件参数里未核实的成员
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时在 Avalonia 12.1.2 headless 实测）

| 控件 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `ScrollBar` | 默认 `Minimum=0`、`Maximum=100`、`SmallChange=1`、`LargeChange=10`、`Visibility=Visible`、`AllowAutoHide=True`；`LineDown()` 触发 `Scroll`（`SmallIncrement`，值 1）、`PageDown()`（`LargeIncrement`，值 11）、`LineUp()`（`SmallDecrement`，值 10）、`ScrollToEnd()`（`LargeIncrement`，值 100）、`ScrollToHome()`（`LargeDecrement`，值 0）；**直接给 `Value` 赋值不触发 `Scroll`**（它只在用户操作与上面这组方法里触发） | 页面用按钮调这组方法，另给一个"直接赋值 `Value`"的滑块，对比两种路径；`Scroll` 读数用计数加 `ScrollEventType` |
| `ThemeVariantScope` | 它是 `Decorator`；`RequestedThemeVariant` 设成 `Dark` 后 `ActualThemeVariant` 立即变 `Dark`，设回 `Default` 后继承外层（这里是 `Light`） | 页面并排放 Light / Dark 两个作用域，再放一个下拉驱动的 |
| `Window` | headless 里完整可用：`Show()`、`Show(owner)`（`owner.OwnedWindows` 随之增减）、`Closing` 里 `e.Cancel = true` 能拦住 `Close()`（窗口仍可见），再次不拦才真关；`CanMaximize` / `CanMinimize` / `Topmost` / `ShowInTaskbar` 可写；`WindowState` 切 `Maximized` 再切 `Normal` 都生效；新建窗口 `Position` 为 `50, 50`、`SizeToContent=Manual`、`CanResize=True` | 控件页与 How-to 页都能真开窗口并读回属性 |
| `Window.ShowDialog<T>` | 返回 `Task<T?>`；`dialog.Close(true)` → 结果 `true`；**用户直接关窗（`Close()` 无参）→ 结果 `null`**；`Close(Colors.Red)` → `Color?` 结果 `Red`；`IsDialog` 为 `True`；**headless 里所有者的 `IsEnabled` 仍是 `True`**（真实桌面上所有者会被挡住） | 对话框结果一律用 `bool?` / `Color?`，页面要区分 `False` 与 `null`；探针不要断言"所有者被禁用" |
| 窗口装饰 | `WindowDecorations`（`None / BorderOnly / Full`）三种取值下，**headless 里都不画装饰**：`IsExtendedIntoWindowDecorations` 恒为 `False`，`WindowDecorationMargin` 恒为 `0`，可视树里没有任何标题栏按钮；`ExtendClientAreaToDecorationsHint=True` 也不改变这些读数；`new WindowDrawnDecorations()` 的 `TitleBarHeight` 等读数全是 0（取值由主题决定） | 探针只能断言**属性读回**与 `ElementRole` 读回；标题栏的真实绘制要人工目视（Step 5） |
| `WindowDrawnDecorations` | 官方说明它是"逻辑元素"，不是视觉控件；**没有任何公开属性把它暴露出来**（`Window`、`TopLevel` 上都没有类型为它的属性），只能通过主题（`ControlTheme TargetType="WindowDrawnDecorations"`）与 `Window.WindowDecorationsTheme` 触达；`WindowDecorationProperties.SetElementRole` / `GetElementRole` 在 `Avalonia.Controls.Chrome`，枚举 `WindowDecorationsElementRole` 在 `Avalonia.Input`（`None / DecorationsElement / User / TitleBar / ResizeN…ResizeSW / CloseButton / MinimizeButton / MaximizeButton / FullScreenButton`） | 页面不去"拿"它，而是演示三件事：`WindowDecorations` 三档、`ExtendClientAreaToDecorationsHint`、给元素标 `ElementRole` 做自绘标题栏；属性清单写在说明里 |
| `TrayIcon` | headless 里完整可用：`TrayIcon.SetIcons(Application.Current, new TrayIcons { icon })` 后 `GetIcons` 读回同一个对象；`IsVisible` 可改；`NativeMenuExporter` 为 `null`（没有平台实现，**这就是页面要检测并如实显示的"是否导出"**）；`Dispose()` 后 `SetIcons(app, null)` 清掉；菜单必须是 `NativeMenu`（`NativeMenuItem` 有 `Header`、`Command`、`Click`、`ToggleType`、`IsChecked`、`Icon`） | 页面在 `AttachedToVisualTree` 里挂、`DetachedFromVisualTree` 里还原；`NativeMenuItem` 的点击探针走 `Command.Execute`，`TrayIcon.Clicked` 在 headless 里触发不了，只做读数 |
| `StorageProvider` | `TopLevel.StorageProvider` 非空，但 **headless 里 `CanOpen`、`CanSave`、`CanPickFolder` 全是 `False`**；`ApplicationLifetime` 为 `null` | 对话框页的"打开文件…"按钮按 `CanOpen` 启用，并如实显示三项读数 |
| `Environment.SpecialFolder` | `ApplicationData` 在本机解析为 `C:\Users\<用户>\AppData\Roaming`；`ApplicationData` 与 `LocalApplicationData` 是两个不同的目录 | 数据持久化页显示两条真实路径；设置放 `ApplicationData`，最近文件放 `LocalApplicationData`（与官方指南一致） |

**成员核实（反射，Avalonia 12.1.2）**：`ScrollBar` 有 `Orientation`、`ViewportSize`、`AllowAutoHide`、`HideDelay`、`ShowDelay`、`IsExpanded`、`Visibility`（`ScrollBarVisibility`：`Disabled / Auto / Hidden / Visible`）、事件 `Scroll`（`ScrollEventArgs` 有 `NewValue`、`ScrollEventType`：`SmallDecrement / SmallIncrement / LargeDecrement / LargeIncrement / ThumbTrack / EndScroll`）、方法 `LineUp / LineDown / LineLeft / LineRight / PageUp / PageDown / PageLeft / PageRight / ScrollToHome / ScrollToEnd / ScrollHere`；`ThemeVariantScope` 有 `RequestedThemeVariant`、`ActualThemeVariantProperty`；`Window` 有 `Show()`、`Show(Window)`、`ShowDialog<T>(Window)`、`Close()`、`Close(object)`、`Hide()`、`Title`、`Icon`、`Topmost`、`CanResize`、`CanMinimize`、`CanMaximize`、`ShowInTaskbar`、`SizeToContent`（`Manual / Width / Height / WidthAndHeight`）、`WindowState`（`Normal / Minimized / Maximized / FullScreen`）、`WindowStartupLocation`（`Manual / CenterScreen / CenterOwner`）、`WindowDecorations`、`ExtendClientAreaToDecorationsHint`、`ExtendClientAreaTitleBarHeightHint`、`IsExtendedIntoWindowDecorations`、`WindowDecorationMargin`、`WindowDecorationsTheme`、`TransparencyLevelHint`、`ActualTransparencyLevel`、`OwnedWindows`、`IsDialog`、事件 `Opened / Closing / Closed / Resized / PositionChanged`；`TopLevel` 有 `StorageProvider`（`CanOpen / CanSave / CanPickFolder`）、`Launcher`、`Clipboard`、`Screens`；`TrayIcon` 有 `Icon`（`WindowIcon`）、`ToolTipText`、`IsVisible`、`Menu`（`NativeMenu`）、`Command`、`CommandParameter`、`Clicked`、`NativeMenuExporter`、`Dispose()`，静态 `SetIcons` / `GetIcons`；`WindowIcon` 有构造函数接收 `Stream`。

**与官方 How-to 的出入**：官方 Window 指南的"透明窗口"（`TransparencyLevelHint=AcrylicBlur`）与"窗口图标"两节，本份实战页不做：前者依赖平台合成器（headless 读不到，真实效果因系统而异），后者与 `TrayIcon` 页里的 `WindowIcon` 用法相同；官方 Dialogs 指南的"文件与文件夹对话框"在 headless 里 `CanOpen=False`，只做能力检测，不真的弹选择器（要手动试）；官方数据持久化指南的"书签存储"（`SaveBookmarkAsync`，针对 iOS / Android / WebAssembly 沙盒）桌面端用不上，不做。

**范围缺口（已知，不在本份处理）**：spec 的 Primitives 分类只列 5 页（ScrollBar、ThemeVariantScope、UserControl、Window、WindowDrawnDecorations）；官方 `controls/primitives/` 下若还有别的条目，不在 spec 范围内。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Navigation/
│   ├── PageCatalog.Primitives.cs           改：+5 控件页 +2 How-to 页
│   └── PageCatalog.System.cs               改：+1 控件页 +1 How-to 页
├── ViewModels/
│   ├── IDialogService.cs                   新：对话框抽象（确认、选色）
│   ├── DialogsHowToViewModel.cs            新：命令只依赖 IDialogService
│   ├── AppSettings.cs                      新：设置的数据形状
│   ├── SettingsStore.cs                    新：JSON 读写（可指定目录）
│   ├── RecentFiles.cs                      新：最近文件列表（上限 10，落盘）
│   └── DataPersistenceHowToViewModel.cs    新：设置自动保存、重读、恢复默认
└── Views/Pages/
    ├── Primitives/  ScrollBarPage / ThemeVariantScopePage / SwatchView(UserControl) / UserControlPage
    │                WindowPage / WindowDrawnDecorationsPage / WindowHowToPage / DialogsHowToPage
    │                DemoDialogs.cs / WindowDialogService.cs
    └── System/      TrayIconPage / DataPersistenceHowToPage
```

每个页面含 `.axaml` 与 `.axaml.cs`（`DemoDialogs.cs`、`WindowDialogService.cs` 只有 `.cs`）。探针（仓库外）：`C:\Temp\probe-controls\Probe.Prim.cs`、`Probe.Prim2.cs`。

### 页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 页面骨架（`UserControl` + `ScrollViewer` + `StackPanel Margin="12"` + `DemoHeader`）与第 00 份相同；演示块用 `TextBlock.caption` + `Border.stage` + `TextBlock.hint`。
- 枚举下拉一律在 code-behind 构造函数里 `ItemsSource = Enum.GetValues<T>()`。
- 页面开出来的窗口用 `Window` 在代码里造（不为示例窗口建 `.axaml`）；页面类暴露一个只读属性（如 `Sample`）给探针读，**不为测试加公开的写入口**。

### Task 1: ScrollBar、ThemeVariantScope、UserControl

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/ScrollBarPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/ThemeVariantScopePage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/SwatchView.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/UserControlPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份的 `DemoHeader`、`SharedStyles` 里的 `caption` / `stage` / `hint`
- Produces: `ScrollBarPage`、`ThemeVariantScopePage`、`UserControlPage`（页面类，Task 5 登记）；`SwatchView`（自定义 `UserControl`，公开 `Title : string?`、`Fill : IBrush?`，只给 `UserControlPage` 用）

- [ ] **Step 1: ScrollBar**

`ScrollBar` 单独使用（不挂在 `ScrollViewer` 里）。页面对比两条路径：按钮调 `LineDown()` 这组方法会触发 `Scroll` 事件，滑块直接写 `Value` 不会。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/ScrollBarPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ScrollBarPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ScrollBar：独立的滚动条"
                               DocPath="controls/primitives/scrollbar" />

            <TextBlock Classes="caption" Text="方法触发 Scroll，直接赋值 Value 不触发" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="24">
                    <ScrollBar Name="Bar" Height="220" Maximum="100" ViewportSize="20" />
                    <StackPanel Spacing="8">
                        <WrapPanel>
                            <Button Name="LineUpButton" Margin="0,0,8,8" Content="LineUp" />
                            <Button Name="LineDownButton" Margin="0,0,8,8" Content="LineDown" />
                            <Button Name="PageUpButton" Margin="0,0,8,8" Content="PageUp" />
                            <Button Name="PageDownButton" Margin="0,0,8,8" Content="PageDown" />
                            <Button Name="HomeButton" Margin="0,0,8,8" Content="ScrollToHome" />
                            <Button Name="EndButton" Margin="0,0,8,8" Content="ScrollToEnd" />
                        </WrapPanel>
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <TextBlock VerticalAlignment="Center" Text="直接写 Value" />
                            <Slider Name="ValueSlider" Width="200" Minimum="0" Maximum="100" />
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <TextBlock VerticalAlignment="Center" Text="Visibility" />
                            <ComboBox Name="VisibilityBox" Width="120" />
                            <CheckBox Name="AutoHideCheck" Content="AllowAutoHide" />
                        </StackPanel>
                        <TextBlock Name="ValueText" />
                        <TextBlock Name="ScrollText" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="SmallChange 默认 1、LargeChange 默认 10：LineDown 一次加 1，PageDown 一次加 10；ScrollToEnd 与 ScrollToHome 跳到两端。这些方法都会触发 Scroll 事件（带 ScrollEventType 与 NewValue），拖动滑块直接写 Value 则只改值、不触发 Scroll。Visibility 的 Disabled / Hidden 会让滚动条不可用或隐藏，Auto 在内容放得下时隐藏。AllowAutoHide 默认开，只在鼠标靠近时展开成完整滚动条。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/ScrollBarPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ScrollBarPage : UserControl
    {
        private int _scrollCount;
        private ScrollEventType? _lastType;

        public ScrollBarPage()
        {
            InitializeComponent();

            LineUpButton.Click += (_, _) => Bar.LineUp();
            LineDownButton.Click += (_, _) => Bar.LineDown();
            PageUpButton.Click += (_, _) => Bar.PageUp();
            PageDownButton.Click += (_, _) => Bar.PageDown();
            HomeButton.Click += (_, _) => Bar.ScrollToHome();
            EndButton.Click += (_, _) => Bar.ScrollToEnd();

            Bar.Scroll += (_, e) =>
            {
                _scrollCount++;
                _lastType = e.ScrollEventType;
                Refresh();
            };
            Bar.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty) Refresh();
            };

            ValueSlider.ValueChanged += (_, e) => Bar.Value = e.NewValue;

            VisibilityBox.ItemsSource = Enum.GetValues<ScrollBarVisibility>();
            VisibilityBox.SelectedItem = Bar.Visibility;
            VisibilityBox.SelectionChanged += (_, _) =>
            {
                if (VisibilityBox.SelectedItem is ScrollBarVisibility value) Bar.Visibility = value;
            };

            AutoHideCheck.IsChecked = Bar.AllowAutoHide;
            AutoHideCheck.IsCheckedChanged += (_, _) => Bar.AllowAutoHide = AutoHideCheck.IsChecked == true;

            Refresh();
        }

        private void Refresh()
        {
            // The slider only mirrors the value; writing the same number back is a no-op.
            ValueSlider.Value = Bar.Value;
            ValueText.Text = $"Value = {Bar.Value:0.#}（Maximum = {Bar.Maximum:0.#}，ViewportSize = {Bar.ViewportSize:0.#}）";
            ScrollText.Text = $"Scroll 事件 {_scrollCount} 次，最近一次：{(_lastType?.ToString() ?? "（无）")}";
        }
    }
}
```

- [ ] **Step 2: ThemeVariantScope**

三块演示：并排的 Light / Dark 作用域、一个由下拉驱动的作用域。每个作用域里放同样的控件，颜色不同是因为它们各自解析资源。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/ThemeVariantScopePage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ThemeVariantScopePage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ThemeVariantScope：给一块区域指定主题变体"
                               DocPath="controls/primitives/themevariantscope" />

            <TextBlock Classes="caption" Text="并排的 Light 与 Dark 作用域" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <ThemeVariantScope Name="LightScope" RequestedThemeVariant="Light">
                        <Border Name="LightBorder" Width="200" Padding="12" CornerRadius="6"
                                Background="{DynamicResource SystemControlBackgroundAltHighBrush}">
                            <StackPanel Spacing="8">
                                <TextBlock Text="Light 作用域" />
                                <Button Content="按钮" />
                                <CheckBox Content="复选框" />
                            </StackPanel>
                        </Border>
                    </ThemeVariantScope>
                    <ThemeVariantScope Name="DarkScope" RequestedThemeVariant="Dark">
                        <Border Name="DarkBorder" Width="200" Padding="12" CornerRadius="6"
                                Background="{DynamicResource SystemControlBackgroundAltHighBrush}">
                            <StackPanel Spacing="8">
                                <TextBlock Text="Dark 作用域" />
                                <Button Content="按钮" />
                                <CheckBox Content="复选框" />
                            </StackPanel>
                        </Border>
                    </ThemeVariantScope>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ThemeVariantScope 是一个 Decorator：RequestedThemeVariant 只影响它的子树，窗口其余部分不变。两块里的控件与背景写法完全相同，颜色不同是因为 DynamicResource 在各自的作用域里解析出了不同变体的值。" />

            <TextBlock Classes="caption" Text="RequestedThemeVariant 与 ActualThemeVariant" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="RequestedThemeVariant" />
                        <ComboBox Name="VariantBox" Width="120" />
                    </StackPanel>
                    <TextBlock Name="VariantText" />
                    <ThemeVariantScope Name="LiveScope">
                        <Border Padding="12" CornerRadius="6" Background="{DynamicResource SystemControlBackgroundAltHighBrush}">
                            <TextBlock Text="由下拉驱动的作用域" />
                        </Border>
                    </ThemeVariantScope>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="选 Default 表示不指定，作用域跟随外层（这里是整个窗口当前的变体）；选 Light 或 Dark 则强制该变体。ActualThemeVariant 是最终生效的值，所以 Default 时它读到的是外层的变体，而不是 Default。更深入的演示：Avalonia.StylingDemo →「主题变体」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/ThemeVariantScopePage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ThemeVariantScopePage : UserControl
    {
        public ThemeVariantScopePage()
        {
            InitializeComponent();

            VariantBox.ItemsSource = new[] { ThemeVariant.Default, ThemeVariant.Light, ThemeVariant.Dark };
            VariantBox.SelectedItem = LiveScope.RequestedThemeVariant ?? ThemeVariant.Default;
            VariantBox.SelectionChanged += (_, _) =>
            {
                if (VariantBox.SelectedItem is ThemeVariant variant) LiveScope.RequestedThemeVariant = variant;
                // ActualThemeVariantChanged can fire before Requested has been stored, so refresh here too.
                Refresh();
            };
            LiveScope.ActualThemeVariantChanged += (_, _) => Refresh();

            Refresh();
        }

        private void Refresh()
        {
            VariantText.Text = $"Requested = {LiveScope.RequestedThemeVariant?.ToString() ?? "（空）"}，Actual = {LiveScope.ActualThemeVariant}";
        }
    }
}
```

- [ ] **Step 3: UserControl 与自定义 SwatchView**

`UserControl` 的用途是"把一块 XAML 封装成可复用的组件"。`SwatchView` 带两个 `StyledProperty`（`Title`、`Fill`），内部绑定通过 `RelativeSource AncestorType` 读它们，不依赖 `x:Name`。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/SwatchView.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SwatchView">
    <StackPanel Width="96" Spacing="4">
        <Border Height="56" CornerRadius="6"
                Background="{Binding Fill, RelativeSource={RelativeSource AncestorType=local:SwatchView}}" />
        <TextBlock HorizontalAlignment="Center"
                   Text="{Binding Title, RelativeSource={RelativeSource AncestorType=local:SwatchView}}" />
    </StackPanel>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/SwatchView.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>A small reusable UserControl: a colour block with a caption.</summary>
    public partial class SwatchView : UserControl
    {
        public static readonly StyledProperty<string?> TitleProperty =
            AvaloniaProperty.Register<SwatchView, string?>(nameof(Title));

        public static readonly StyledProperty<IBrush?> FillProperty =
            AvaloniaProperty.Register<SwatchView, IBrush?>(nameof(Fill));

        public SwatchView()
        {
            InitializeComponent();
        }

        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/UserControlPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.UserControlPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="UserControl：把一块 XAML 封装成组件"
                               DocPath="controls/primitives/usercontrol" />

            <TextBlock Classes="caption" Text="自定义的 SwatchView：Title 与 Fill 是它的两个属性" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <local:SwatchView Title="红" Fill="Crimson" />
                        <local:SwatchView Title="绿" Fill="SeaGreen" />
                        <local:SwatchView Title="蓝" Fill="SteelBlue" />
                        <local:SwatchView Name="Live" Title="可改的一块" Fill="Gray" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="给最右一块换色" />
                        <ComboBox Name="ColorBox" Width="120" />
                        <TextBox Name="TitleBox" Width="140" PlaceholderText="改它的 Title" />
                    </StackPanel>
                    <TextBlock Name="StateText" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="UserControl 就是一个带 .axaml 与 code-behind 的类：XAML 描述内部结构，StyledProperty 暴露可配置的部分，使用方像用普通控件一样写属性。它没有自己的模板，内部元素直接成为它的 Content。本项目所有演示页都是 UserControl，SwatchView 则展示了「可复用的小组件」这一半用法。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/UserControlPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class UserControlPage : UserControl
    {
        private static readonly Dictionary<string, IBrush> Choices = new()
        {
            ["Gray"] = Brushes.Gray,
            ["Orange"] = Brushes.Orange,
            ["Purple"] = Brushes.MediumPurple,
            ["Gold"] = Brushes.Gold
        };

        public UserControlPage()
        {
            InitializeComponent();

            ColorBox.ItemsSource = Choices.Keys;
            ColorBox.SelectedItem = "Gray";
            ColorBox.SelectionChanged += (_, _) =>
            {
                if (ColorBox.SelectedItem is string key) Live.Fill = Choices[key];
                Refresh();
            };
            TitleBox.TextChanged += (_, _) =>
            {
                Live.Title = string.IsNullOrEmpty(TitleBox.Text) ? "可改的一块" : TitleBox.Text;
                Refresh();
            };

            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = $"Live.Title = {Live.Title}，Live.Fill = {(Live.Fill as ISolidColorBrush)?.Color.ToString() ?? "（空）"}";
        }
    }
}
```

- [ ] **Step 4: 抽取并构建**

Run: `& 'C:\Temp\extract-plan.ps1' -Plan '<本计划>' -RepoRoot '<仓库根>'` 然后 `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|个错误"`
Expected: 0 个错误。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate ScrollBar, ThemeVariantScope and UserControl

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: Window 与 WindowDrawnDecorations

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowDrawnDecorationsPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份的 `DemoHeader`、`SharedStyles`
- Produces: `WindowPage`（公开只读 `Sample : Window?`、`ClosingCount : int`）、`WindowDrawnDecorationsPage`（公开只读 `Sample : Window?`），Task 5 登记并供探针读取；两页都在 `DetachedFromVisualTree` 时强制关掉自己开的窗口

**两个新验证过的细节**（编写计划时实测）：`WindowClosingEventArgs` 有 `CloseReason`（`Undefined / WindowClosing / OwnerWindowClosing / ApplicationShutdown / OSShutdown`）与 `IsProgrammatic`；**代码里调 `Close()` 的 `CloseReason` 也是 `WindowClosing`**。所以"拦截关闭"只拦 `WindowClosing`，放过所有者关闭与关机，否则用户关主窗口时示例窗口会拖住整个应用。

- [ ] **Step 1: Window**

页面在代码里造一个示例窗口（`Show(owner)`），用复选框与下拉实时改它的属性，读数显示 `WindowState`、`Position`、`Bounds`、所有者的 `OwnedWindows` 与 `Closing` 触发次数。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.WindowPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Window：顶层窗口"
                               DocPath="controls/primitives/window" />

            <TextBlock Classes="caption" Text="打开、关闭，以及在 Closing 里拦截关闭" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="OpenButton" Content="打开示例窗口" />
                        <Button Name="CloseButton" Content="关闭示例窗口" />
                        <CheckBox Name="BlockCheck" Content="拦截关闭（Closing 里 e.Cancel = true）" />
                    </StackPanel>
                    <TextBlock Name="OwnerText" />
                    <TextBlock Name="StateText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="示例窗口用 Show(owner) 打开，所以它是当前窗口的子窗口：所有者的 OwnedWindows 随之增减，所有者关闭时它也会被带着关掉。勾上「拦截关闭」后，点示例窗口的关闭按钮或页面上的「关闭示例窗口」都会触发 Closing 却关不掉；离开本页时页面会强制关掉它。Window.Closing 的参数带 CloseReason，所有者关闭与系统关机不会被拦。" />

            <TextBlock Classes="caption" Text="实时改窗口属性（要先打开示例窗口）" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <WrapPanel>
                        <CheckBox Name="ResizeCheck" Margin="0,0,12,0" Content="CanResize" />
                        <CheckBox Name="MinimizeCheck" Margin="0,0,12,0" Content="CanMinimize" />
                        <CheckBox Name="MaximizeCheck" Margin="0,0,12,0" Content="CanMaximize" />
                        <CheckBox Name="TopmostCheck" Margin="0,0,12,0" Content="Topmost" />
                        <CheckBox Name="TaskbarCheck" Content="ShowInTaskbar" />
                    </WrapPanel>
                    <WrapPanel>
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="WindowState" />
                        <ComboBox Name="StateBox" Width="120" Margin="0,0,12,0" />
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="SizeToContent" />
                        <ComboBox Name="SizeBox" Width="150" Margin="0,0,12,0" />
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="WindowStartupLocation（下次打开生效）" />
                        <ComboBox Name="LocationBox" Width="140" />
                    </WrapPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="CanResize 关掉后边框不能拖；CanMinimize / CanMaximize 关掉后对应的标题栏按钮变灰；Topmost 让窗口盖在其他窗口之上；ShowInTaskbar 关掉后任务栏不再有它的图标。WindowState 的值会跟着用户在标题栏上的操作同步回下拉框。SizeToContent 选 WidthAndHeight 后窗口收缩到刚好包住内容。要弹出模态对话框并取回结果，见「实战：对话框」。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WindowPage : UserControl
    {
        private Window? _sample;
        private bool _forceClose;
        private bool _syncing;

        public WindowPage()
        {
            InitializeComponent();

            ResizeCheck.IsChecked = true;
            MinimizeCheck.IsChecked = true;
            MaximizeCheck.IsChecked = true;
            TaskbarCheck.IsChecked = true;

            StateBox.ItemsSource = Enum.GetValues<WindowState>();
            StateBox.SelectedItem = WindowState.Normal;
            SizeBox.ItemsSource = Enum.GetValues<SizeToContent>();
            SizeBox.SelectedItem = SizeToContent.Manual;
            LocationBox.ItemsSource = Enum.GetValues<WindowStartupLocation>();
            LocationBox.SelectedItem = WindowStartupLocation.CenterOwner;

            OpenButton.Click += (_, _) => OpenSample();
            CloseButton.Click += (_, _) => _sample?.Close();

            ResizeCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            MinimizeCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            MaximizeCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            TopmostCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            TaskbarCheck.IsCheckedChanged += (_, _) => ApplyToSample();
            StateBox.SelectionChanged += (_, _) => ApplyToSample();
            SizeBox.SelectionChanged += (_, _) => ApplyToSample();

            // Never leave an orphan window on the desktop when the user leaves this page.
            DetachedFromVisualTree += (_, _) => CloseSample();

            Refresh();
        }

        /// <summary>The sample window while it is open, otherwise null.</summary>
        public Window? Sample => _sample;

        /// <summary>How many times the sample window raised Closing since it was last opened.</summary>
        public int ClosingCount { get; private set; }

        private void OpenSample()
        {
            if (_sample is not null)
            {
                _sample.Activate();
                return;
            }

            var window = new Window
            {
                Title = "示例窗口",
                Width = 320,
                Height = 200,
                WindowStartupLocation = LocationBox.SelectedItem is WindowStartupLocation location ? location : WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Text = "这是用 Show(owner) 打开的示例窗口。回到页面改它的属性。"
                }
            };
            ClosingCount = 0;
            _sample = window;

            window.Closing += OnSampleClosing;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_sample, window)) _sample = null;
                Refresh();
            };
            window.Opened += (_, _) => Refresh();
            window.PositionChanged += (_, _) => Refresh();
            window.Resized += (_, _) => Refresh();
            window.PropertyChanged += (_, e) =>
            {
                // The user can minimise or maximise from the title bar, so mirror it back into the box.
                if (e.Property == Window.WindowStateProperty)
                {
                    _syncing = true;
                    StateBox.SelectedItem = window.WindowState;
                    _syncing = false;
                    Refresh();
                }
            };

            ApplyToSample();
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
            Refresh();
        }

        private void OnSampleClosing(object? sender, WindowClosingEventArgs e)
        {
            ClosingCount++;
            // Only the user's own close is held back; closing the owner or shutting down must go through.
            if (BlockCheck.IsChecked == true && !_forceClose && e.CloseReason == WindowCloseReason.WindowClosing)
            {
                e.Cancel = true;
            }
            Refresh();
        }

        private void CloseSample()
        {
            _forceClose = true;
            _sample?.Close();
            _forceClose = false;
        }

        private void ApplyToSample()
        {
            if (_sample is null || _syncing) return;

            _sample.CanResize = ResizeCheck.IsChecked == true;
            _sample.CanMinimize = MinimizeCheck.IsChecked == true;
            _sample.CanMaximize = MaximizeCheck.IsChecked == true;
            _sample.Topmost = TopmostCheck.IsChecked == true;
            _sample.ShowInTaskbar = TaskbarCheck.IsChecked == true;
            if (StateBox.SelectedItem is WindowState state) _sample.WindowState = state;
            if (SizeBox.SelectedItem is SizeToContent size) _sample.SizeToContent = size;
            Refresh();
        }

        private void Refresh()
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            // Closed fires while the window is still in OwnedWindows, so count only the ones still showing.
            OwnerText.Text = $"所有者的 OwnedWindows 数：{owner?.OwnedWindows.Count(w => w.IsVisible) ?? 0}";

            if (_sample is null)
            {
                StateText.Text = ClosingCount == 0 ? "示例窗口：未打开" : $"示例窗口：已关闭（上次共触发 Closing {ClosingCount} 次）";
                return;
            }

            StateText.Text = $"示例窗口：IsVisible = {_sample.IsVisible}，WindowState = {_sample.WindowState}，Position = {_sample.Position}，" +
                             $"大小 = {_sample.Bounds.Width:0}×{_sample.Bounds.Height:0}，Closing 触发 {ClosingCount} 次";
        }
    }
}
```

- [ ] **Step 2: WindowDrawnDecorations**

`WindowDrawnDecorations` 是主题里的**逻辑元素**，没有任何公开属性能直接拿到它，所以页面不去"拿"它，而是演示它背后的三件事：`WindowDecorations` 三档、`ExtendClientAreaToDecorationsHint`，以及给自己画的元素标 `ElementRole` 做自绘标题栏。**headless 里装饰不会真的绘制**（`IsExtendedIntoWindowDecorations` 恒为 `False`），所以这一页的外观要在 Task 5 Step 5 里人工目视。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowDrawnDecorationsPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.WindowDrawnDecorationsPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="WindowDrawnDecorations：由 Avalonia 绘制的窗口装饰"
                               DocPath="controls/primitives/windowdrawndecorations" />

            <TextBlock Classes="caption" Text="WindowDecorations 与 ExtendClientAreaToDecorationsHint" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <WrapPanel>
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="WindowDecorations" />
                        <ComboBox Name="DecorationsBox" Width="120" Margin="0,0,12,0" />
                        <CheckBox Name="ExtendCheck" Margin="0,0,12,0" Content="ExtendClientAreaToDecorationsHint" />
                        <Button Name="OpenSystemButton" Margin="0,0,8,0" Content="打开示例窗口" />
                        <Button Name="CloseButton" Content="关闭示例窗口" />
                    </WrapPanel>
                    <TextBlock Name="StateText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="WindowDecorations 有三档：Full（标题栏、边框、阴影都要）、BorderOnly（只要边框与阴影）、None（什么都不要）。打开 ExtendClientAreaToDecorationsHint 后客户区延伸到标题栏下面，内容要用 WindowDecorationMargin 给标题栏让位。这些装饰由 Avalonia 自己绘制，外观来自主题里 TargetType 为 WindowDrawnDecorations 的 ControlTheme，可以通过 Window.WindowDecorationsTheme 换成自己的。" />

            <TextBlock Classes="caption" Text="自绘标题栏：用 ElementRole 告诉窗口哪个元素是标题栏与按钮" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Button Name="OpenCustomButton" HorizontalAlignment="Left" Content="打开自绘标题栏窗口" />
                    <TextBlock Name="RolesText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="示例窗口把 WindowDecorations 设成 None，再用 WindowDecorationProperties.SetElementRole 给自己画的元素标角色：TitleBar 让用户能拖它移动窗口，MinimizeButton / MaximizeButton / CloseButton 让对应的按钮拥有系统按钮的行为。WindowDecorationsElementRole 还有 ResizeN、ResizeSE 等八个方向，可以给自绘的边缘加上拖动改变大小。主题里 WindowDrawnDecorations 的可调项有 TitleBarHeight、FrameThickness、ShadowThickness，模板里的按钮部件叫 PART_CloseButton、PART_MinimizeButton、PART_MaximizeButton、PART_FullScreenButton，并带 :normal、:maximized、:minimized、:fullscreen、:has-shadow、:has-border、:has-titlebar 这些伪类。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowDrawnDecorationsPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WindowDrawnDecorationsPage : UserControl
    {
        private Window? _sample;
        private bool _isCustom;

        public WindowDrawnDecorationsPage()
        {
            InitializeComponent();

            DecorationsBox.ItemsSource = Enum.GetValues<WindowDecorations>();
            DecorationsBox.SelectedItem = WindowDecorations.Full;
            ExtendCheck.IsChecked = false;

            DecorationsBox.SelectionChanged += (_, _) => ApplyToSystemSample();
            ExtendCheck.IsCheckedChanged += (_, _) => ApplyToSystemSample();
            OpenSystemButton.Click += (_, _) => OpenSystem();
            OpenCustomButton.Click += (_, _) => OpenCustom();
            CloseButton.Click += (_, _) => _sample?.Close();

            // Never leave an orphan window on the desktop when the user leaves this page.
            DetachedFromVisualTree += (_, _) => _sample?.Close();

            Refresh();
        }

        /// <summary>The sample window while it is open, otherwise null.</summary>
        public Window? Sample => _sample;

        private void OpenSystem()
        {
            var window = new Window
            {
                Title = "系统装饰示例",
                Width = 360,
                Height = 220,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Text = "改页面上的 WindowDecorations 与 ExtendClientAreaToDecorationsHint，看这个窗口的标题栏与边框怎么变。"
                }
            };
            Attach(window, isCustom: false);
            ApplyToSystemSample();
            Show(window);
        }

        private void OpenCustom()
        {
            var title = new TextBlock
            {
                Text = "自绘标题栏",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0)
            };
            var minimize = new Button { Content = "—", Width = 44 };
            var maximize = new Button { Content = "□", Width = 44 };
            var close = new Button { Content = "✕", Width = 44 };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Children = { minimize, maximize, close } };
            DockPanel.SetDock(buttons, Dock.Right);

            var bar = new DockPanel { Height = 32, Background = Brushes.SteelBlue, Children = { buttons, title } };
            DockPanel.SetDock(bar, Dock.Top);

            WindowDecorationProperties.SetElementRole(bar, WindowDecorationsElementRole.TitleBar);
            WindowDecorationProperties.SetElementRole(minimize, WindowDecorationsElementRole.MinimizeButton);
            WindowDecorationProperties.SetElementRole(maximize, WindowDecorationsElementRole.MaximizeButton);
            WindowDecorationProperties.SetElementRole(close, WindowDecorationsElementRole.CloseButton);

            var window = new Window
            {
                Title = "自绘标题栏示例",
                Width = 360,
                Height = 220,
                WindowDecorations = WindowDecorations.None,
                Content = new DockPanel
                {
                    Children =
                    {
                        bar,
                        new TextBlock { Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap, Text = "拖蓝色的标题栏移动窗口，三个按钮分别最小化、最大化与关闭。" }
                    }
                }
            };
            Attach(window, isCustom: true);

            RolesText.Text = "已标记的角色（读回）：" + string.Join("、",
                new Control[] { bar, minimize, maximize, close }.Select(c => WindowDecorationProperties.GetElementRole(c).ToString()));
            Show(window);
        }

        private void Attach(Window window, bool isCustom)
        {
            _sample?.Close();
            _sample = window;
            _isCustom = isCustom;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_sample, window)) _sample = null;
                Refresh();
            };
            window.PropertyChanged += (_, _) => Refresh();
        }

        private void Show(Window window)
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
            Refresh();
        }

        private void ApplyToSystemSample()
        {
            if (_sample is null || _isCustom) return;

            if (DecorationsBox.SelectedItem is WindowDecorations decorations) _sample.WindowDecorations = decorations;
            _sample.ExtendClientAreaToDecorationsHint = ExtendCheck.IsChecked == true;
            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = _sample is null
                ? "示例窗口：未打开"
                : $"WindowDecorations = {_sample.WindowDecorations}，ExtendClientAreaToDecorationsHint = {_sample.ExtendClientAreaToDecorationsHint}，" +
                  $"IsExtendedIntoWindowDecorations = {_sample.IsExtendedIntoWindowDecorations}，WindowDecorationMargin = {_sample.WindowDecorationMargin}";
        }
    }
}
```

- [ ] **Step 3: 抽取并构建**

Run: 同 Task 1 Step 4。
Expected: 0 个错误。

- [ ] **Step 4: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate Window and WindowDrawnDecorations

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: 对话框基础设施、Dialogs 与 Window 两篇实战页

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/DemoDialogs.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/IDialogService.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowDialogService.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/DialogsHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/DialogsHowToPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowHowToPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份的 `DemoHeader`、`SharedStyles`
- Produces:
  - `DemoDialogs`（`internal static`）：`ConfirmAsync(Window owner, string title, string message) : Task<bool?>`、`PromptAsync(Window owner, string title, string prompt, bool required) : Task<string?>`、`PickColorAsync(Window owner, Color initial) : Task<Color?>`；对话框里的按钮 `Name` 固定为 `OkButton`、`CancelButton`、`Swatch_Red` / `Swatch_Green` / `Swatch_Blue` / `Swatch_Orange`，输入框 `Name` 为 `AnswerBox`，探针靠这些名字操作
  - `IDialogService`（`public`，`ViewModels` 命名空间）：`ConfirmAsync(string, string) : Task<bool?>`、`PromptAsync(string, string, bool) : Task<string?>`、`PickColorAsync(Color) : Task<Color?>`
  - `DialogsHowToViewModel(IDialogService)`：`ConfirmResult`、`NameResult`、`PickedColor`、`PickedBrush`、`ColorResult`，命令 `AskConfirmCommand`、`AskNameCommand`、`AskRequiredNameCommand`、`PickColorCommand`
  - `DialogsHowToPage`、`WindowHowToPage`（页面类，Task 5 登记）；`WindowHowToPage` 公开只读 `PlaceWindow : Window?`、`Editor : Window?`

- [ ] **Step 1: 对话框辅助类与服务接口**

官方 Dialogs 指南的核心写法：对话框是一个普通 `Window`，用 `ShowDialog<T>(owner)` 弹出并 `await` 结果，对话框里用 `Close(result)` 回传。**三种结果的含义要分清**：`Close(true)` / `Close(false)` 是用户的明确回答，直接关窗（标题栏的 ✕）得 `null`。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/DemoDialogs.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>Builds the small modal dialogs used by the Window and Dialogs how-to pages.</summary>
    internal static class DemoDialogs
    {
        public static Task<bool?> ConfirmAsync(Window owner, string title, string message)
        {
            var ok = new Button { Name = "OkButton", Content = "确定", MinWidth = 72 };
            var cancel = new Button { Name = "CancelButton", Content = "取消", MinWidth = 72 };
            var dialog = Create(title, new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 }, ok, cancel);

            ok.Click += (_, _) => dialog.Close(true);
            cancel.Click += (_, _) => dialog.Close(false);
            return dialog.ShowDialog<bool?>(owner);
        }

        /// <summary>
        /// Asks for a line of text. With <paramref name="required"/> the title bar's close button is held back
        /// (and Cancel is disabled) until the user has answered.
        /// </summary>
        public static Task<string?> PromptAsync(Window owner, string title, string prompt, bool required)
        {
            var box = new TextBox { Name = "AnswerBox", PlaceholderText = "在这里输入" };
            var ok = new Button { Name = "OkButton", Content = "确定", MinWidth = 72, IsEnabled = false };
            var cancel = new Button { Name = "CancelButton", Content = "取消", MinWidth = 72, IsEnabled = !required };
            var body = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = prompt }, box } };
            var dialog = Create(title, body, ok, cancel);

            bool answered = false;
            box.TextChanged += (_, _) => ok.IsEnabled = !string.IsNullOrWhiteSpace(box.Text);
            ok.Click += (_, _) =>
            {
                answered = true;
                dialog.Close(box.Text!.Trim());
            };
            cancel.Click += (_, _) =>
            {
                answered = true;
                dialog.Close(null);
            };
            if (required)
            {
                dialog.Closing += (_, e) =>
                {
                    if (!answered && e.CloseReason == WindowCloseReason.WindowClosing) e.Cancel = true;
                };
            }
            return dialog.ShowDialog<string?>(owner);
        }

        /// <summary>A "complex result" dialog: the answer is a Color, not a bool.</summary>
        public static Task<Color?> PickColorAsync(Window owner, Color initial)
        {
            Window? dialog = null;
            var swatches = new WrapPanel { Width = 240 };
            foreach (var (name, color) in new[]
            {
                ("Red", Colors.Crimson), ("Green", Colors.SeaGreen), ("Blue", Colors.SteelBlue), ("Orange", Colors.Orange)
            })
            {
                var swatch = new Button
                {
                    Name = "Swatch_" + name,
                    Margin = new Thickness(0, 0, 8, 8),
                    // The colour sits in the content so the button keeps its theme hover and pressed states.
                    Content = new Border { Width = 40, Height = 24, Background = new SolidColorBrush(color) }
                };
                swatch.Click += (_, _) => dialog!.Close(color);
                swatches.Children.Add(swatch);
            }

            var cancel = new Button { Name = "CancelButton", Content = "取消", MinWidth = 72 };
            cancel.Click += (_, _) => dialog!.Close(null);
            var body = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = $"当前颜色：{initial}" }, swatches } };
            dialog = Create("选一个颜色", body, cancel);
            return dialog.ShowDialog<Color?>(owner);
        }

        private static Window Create(string title, Control body, params Button[] buttons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            foreach (var button in buttons) row.Children.Add(button);

            return new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 280,
                CanResize = false,
                CanMinimize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel { Margin = new Thickness(16), Spacing = 12, Children = { body, row } }
            };
        }
    }
}
```

官方指南建议 ViewModel 不直接碰 `Window`，而是依赖一个对话框服务接口；接口放在 `ViewModels`，实现留在视图层。

#### `Avalonia.ControlsDemo/ViewModels/IDialogService.cs`

```csharp
using Avalonia.Media;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>What a view model may ask of the UI: show a dialog and await the user's answer.</summary>
    public interface IDialogService
    {
        /// <summary>True for OK, false for Cancel, null when the user simply closed the dialog.</summary>
        Task<bool?> ConfirmAsync(string title, string message);

        /// <summary>The trimmed text the user typed, or null when they backed out.</summary>
        Task<string?> PromptAsync(string title, string prompt, bool required);

        /// <summary>The chosen colour, or null when they cancelled.</summary>
        Task<Color?> PickColorAsync(Color initial);
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowDialogService.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>The real IDialogService: finds the owner window on demand and shows a DemoDialogs dialog over it.</summary>
    internal sealed class WindowDialogService : IDialogService
    {
        private readonly Func<Window?> _owner;

        public WindowDialogService(Func<Window?> owner)
        {
            _owner = owner;
        }

        public Task<bool?> ConfirmAsync(string title, string message) =>
            _owner() is { } owner ? DemoDialogs.ConfirmAsync(owner, title, message) : Task.FromResult<bool?>(null);

        public Task<string?> PromptAsync(string title, string prompt, bool required) =>
            _owner() is { } owner ? DemoDialogs.PromptAsync(owner, title, prompt, required) : Task.FromResult<string?>(null);

        public Task<Color?> PickColorAsync(Color initial) =>
            _owner() is { } owner ? DemoDialogs.PickColorAsync(owner, initial) : Task.FromResult<Color?>(null);
    }
}
```

- [ ] **Step 2: Dialogs 实战页**

ViewModel 的命令只依赖 `IDialogService`，所以不开窗口也能测试；`PickedBrush` 是 `IBrush`（引用类型），页面绑它而不是直接绑 `Color` 结构体（绑到结构体属性失败是静默的）。

#### `Avalonia.ControlsDemo/ViewModels/DialogsHowToViewModel.cs`

```csharp
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class DialogsHowToViewModel : ObservableObject
    {
        private readonly IDialogService _dialogs;

        public DialogsHowToViewModel(IDialogService dialogs)
        {
            _dialogs = dialogs;
        }

        [ObservableProperty]
        private string _confirmResult = "（还没问过）";

        [ObservableProperty]
        private string _nameResult = "（还没问过）";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PickedBrush))]
        private Color _pickedColor = Colors.Gray;

        [ObservableProperty]
        private string _colorResult = "（还没选过）";

        public IBrush PickedBrush => new SolidColorBrush(PickedColor);

        [RelayCommand]
        private async Task AskConfirmAsync()
        {
            bool? answer = await _dialogs.ConfirmAsync("确认", "要删除这个项目吗？");
            ConfirmResult = answer switch
            {
                true => "结果 true：用户点了「确定」",
                false => "结果 false：用户点了「取消」",
                _ => "结果 null：用户直接关掉了窗口，既没确定也没取消"
            };
        }

        [RelayCommand]
        private async Task AskNameAsync()
        {
            string? name = await _dialogs.PromptAsync("新建项目", "给它起个名字：", required: false);
            NameResult = name is null ? "没有名字（取消或直接关闭）" : $"名字：{name}";
        }

        [RelayCommand]
        private async Task AskRequiredNameAsync()
        {
            string? name = await _dialogs.PromptAsync("必须回答", "这个问题不答不能关：", required: true);
            NameResult = name is null ? "没有名字" : $"名字（必答）：{name}";
        }

        [RelayCommand]
        private async Task PickColorAsync()
        {
            Color? picked = await _dialogs.PickColorAsync(PickedColor);
            if (picked is Color color)
            {
                PickedColor = color;
                ColorResult = $"结果 Color：{color}";
            }
            else
            {
                ColorResult = "结果 null：没选，颜色保持不变";
            }
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/DialogsHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:DataType="vm:DialogsHowToViewModel"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DialogsHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="实战：对话框与它们的返回值"
                               DocPath="docs/how-to/dialogs-how-to" />

            <TextBlock Classes="caption" Text="确认对话框：ShowDialog&lt;bool?&gt;" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Button HorizontalAlignment="Left" Content="删除…" Command="{Binding AskConfirmCommand}" />
                    <TextBlock Text="{Binding ConfirmResult}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="对话框是一个普通 Window：用 ShowDialog&lt;T&gt;(owner) 弹出并 await 结果，里面的按钮调 Close(result) 回传。确定得 true，取消得 false，直接点标题栏的 ✕ 得 null——所以结果类型要写成 bool? 并分清这三种。真实桌面上，对话框打开期间所有者不能操作。" />

            <TextBlock Classes="caption" Text="输入对话框，以及「必须回答才能关」" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="起名字…" Command="{Binding AskNameCommand}" />
                        <Button Content="必答的问题…" Command="{Binding AskRequiredNameCommand}" />
                    </StackPanel>
                    <TextBlock Text="{Binding NameResult}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="「必答的问题」把取消按钮禁掉，并在对话框的 Closing 里拦住用户点 ✕（只拦 CloseReason 为 WindowClosing 的情况，且用一个 answered 标志放行代码里调用的 Close(result)）。确定按钮在输入框为空时也是灰的。" />

            <TextBlock Classes="caption" Text="复杂的返回值：ShowDialog&lt;Color?&gt;" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Button Content="选颜色…" Command="{Binding PickColorCommand}" />
                    <Border Width="80" Height="32" CornerRadius="4" Background="{Binding PickedBrush}" />
                    <TextBlock VerticalAlignment="Center" Text="{Binding ColorResult}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="返回值可以是任何类型，Close(color) 把它装箱传回来，ShowDialog&lt;Color?&gt; 再取出。这里的 ViewModel 只认 IDialogService，不知道 Window 的存在：页面在构造时把「用 TopLevel 找所有者再弹窗」的实现传给它，所以 ViewModel 不用开窗口就能测。预览色块绑的是 IBrush 而不是 Color 结构体，因为绑到结构体属性失败时没有任何提示。" />

            <TextBlock Classes="caption" Text="文件对话框：先问 StorageProvider 能做什么" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TextBlock Name="StorageText" />
                    <Button Name="OpenFileButton" HorizontalAlignment="Left" Content="打开文件…" />
                    <TextBlock Name="FileText" Text="（还没选过）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="文件与文件夹选择器来自 TopLevel.StorageProvider，不是 Window。并不是每个平台都支持，所以先看 CanOpen / CanSave / CanPickFolder，不支持时把按钮灰掉。这一块要在真实窗口里手动点一下试。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/DialogsHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DialogsHowToPage : UserControl
    {
        public DialogsHowToPage()
        {
            InitializeComponent();
            DataContext = new DialogsHowToViewModel(new WindowDialogService(() => TopLevel.GetTopLevel(this) as Window));

            // The storage provider belongs to the TopLevel, which is only known once the page is on screen.
            OpenFileButton.IsEnabled = false;
            AttachedToVisualTree += (_, _) => RefreshStorage();
            OpenFileButton.Click += async (_, _) => await OpenFileAsync();
        }

        private void RefreshStorage()
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            StorageText.Text = storage is null
                ? "StorageProvider：不可用"
                : $"CanOpen = {storage.CanOpen}，CanSave = {storage.CanSave}，CanPickFolder = {storage.CanPickFolder}";
            OpenFileButton.IsEnabled = storage?.CanOpen == true;
        }

        private async Task OpenFileAsync()
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage is null || !storage.CanOpen) return;

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选一个文件", AllowMultiple = false });
            FileText.Text = files.Count == 0 ? "没选文件" : $"选了：{files[0].Name}";
        }
    }
}
```

- [ ] **Step 3: Window 实战页**

对应官方 Window 指南里控件页没演示的三节：多窗口（独立窗口与子窗口）、位置与大小的保存和恢复、关闭前确认（"先取消、再询问、再关"这个经典写法）。尺寸、`WindowState`、`CanMinimize`、拦截关闭的基础用法已在 Window 控件页，不重复。

关闭前确认的关键顺序：`Closing` 处理器**必须同步**设 `e.Cancel = true`（事件返回后窗口就已经决定不关了），然后异步弹对话框；用户确认后置位 `_confirmedClose`，再调一次 `Close()`，这次处理器直接放行。

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.WindowHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="实战：多窗口、位置记忆与关闭前确认"
                               DocPath="docs/how-to/window-how-to" />

            <TextBlock Classes="caption" Text="多窗口：独立窗口与子窗口" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="FreeButton" Content="新建独立窗口 Show()" />
                        <Button Name="OwnedButton" Content="新建子窗口 Show(owner)" />
                        <Button Name="CloseAllButton" Content="全部关闭" />
                    </StackPanel>
                    <TextBlock Name="WindowsText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Show() 打开的是独立窗口，没有所有者，各自活着；Show(owner) 打开的是子窗口，登记在所有者的 OwnedWindows 里，所有者最小化时它跟着收起，所有者关闭时它被带着关掉。页面自己记着开过哪些窗口，离开本页时统一关掉。" />

            <TextBlock Classes="caption" Text="位置与大小：记下、移动、恢复" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <WrapPanel>
                        <Button Name="PlaceOpenButton" Margin="0,0,8,8" Content="打开示例窗口" />
                        <Button Name="MoveButton" Margin="0,0,8,8" Content="移到 (200, 120)，改成 420×260" />
                        <Button Name="SaveButton" Margin="0,0,8,8" Content="记下位置与大小" />
                        <Button Name="RestoreButton" Margin="0,0,8,8" Content="恢复记下的" />
                        <Button Name="PlaceCloseButton" Margin="0,0,8,8" Content="关闭示例窗口" />
                    </WrapPanel>
                    <TextBlock Name="PlaceText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Position 是屏幕上的像素坐标（PixelPoint），改它就是移动窗口；Width / Height 是设备无关单位下的客户区大小。先拖动或缩放示例窗口，再点「记下」，再点「移到…」，最后「恢复」就能回到记下的样子。这里只记在内存里；真正存到文件、下次启动再读回的写法，见 System 分类的「实战：数据持久化」。" />

            <TextBlock Classes="caption" Text="关闭前确认：有未保存的更改时拦下，问清楚再关" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Button Name="EditorButton" HorizontalAlignment="Left" Content="打开可编辑的窗口" />
                    <TextBlock Name="ConfirmText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="可编辑窗口里改了文字就有「未保存」标记，点「保存」清掉标记。有标记时点标题栏的 ✕：Closing 处理器先同步 e.Cancel = true 拦下，再 await 一个确认对话框；用户选「确定」才置位 confirmed 并重新 Close()。没有标记时直接关，不打扰。注意 Cancel 必须在处理器返回之前同步设置，不能 await 之后再设。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Primitives/WindowHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WindowHowToPage : UserControl
    {
        private readonly List<Window> _free = new();
        private readonly List<Window> _owned = new();
        private Window? _placeWindow;
        private Window? _editor;
        private (PixelPoint Position, Size Size)? _saved;
        private bool _dirty;
        private bool _confirmedClose;
        private int _blocked;
        private string _lastChoice = "（还没触发）";
        private int _counter;

        public WindowHowToPage()
        {
            InitializeComponent();

            FreeButton.Click += (_, _) => OpenFree();
            OwnedButton.Click += (_, _) => OpenOwned();
            CloseAllButton.Click += (_, _) => CloseAll();

            PlaceOpenButton.Click += (_, _) => OpenPlace();
            MoveButton.Click += (_, _) => MovePlace();
            SaveButton.Click += (_, _) => SavePlace();
            RestoreButton.Click += (_, _) => RestorePlace();
            PlaceCloseButton.Click += (_, _) => _placeWindow?.Close();

            EditorButton.Click += (_, _) => OpenEditor();

            // Never leave orphan windows on the desktop when the user leaves this page.
            DetachedFromVisualTree += (_, _) =>
            {
                _confirmedClose = true;
                CloseAll();
                _placeWindow?.Close();
                _editor?.Close();
            };

            RefreshWindows();
            RefreshPlace();
            RefreshConfirm();
        }

        /// <summary>The position-and-size sample window while it is open, otherwise null.</summary>
        public Window? PlaceWindow => _placeWindow;

        /// <summary>The editable window with the close confirmation while it is open, otherwise null.</summary>
        public Window? Editor => _editor;

        private Window? Owner => TopLevel.GetTopLevel(this) as Window;

        private static Window NewWindow(string title, string text) => new Window
        {
            Title = title,
            Width = 280,
            Height = 140,
            Content = new TextBlock { Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap, Text = text }
        };

        private void ShowOwned(Window window)
        {
            if (Owner is { } owner)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
        }

        // ---- multi-window ----

        private void OpenFree()
        {
            var window = NewWindow($"独立窗口 {++_counter}", "用 Show() 打开，没有所有者。");
            _free.Add(window);
            window.Closed += (_, _) =>
            {
                _free.Remove(window);
                RefreshWindows();
            };
            window.Show();
            RefreshWindows();
        }

        private void OpenOwned()
        {
            var window = NewWindow($"子窗口 {++_counter}", "用 Show(owner) 打开，随所有者一起关闭。");
            _owned.Add(window);
            window.Closed += (_, _) =>
            {
                _owned.Remove(window);
                RefreshWindows();
            };
            ShowOwned(window);
            RefreshWindows();
        }

        private void CloseAll()
        {
            foreach (var window in _free.ToArray()) window.Close();
            foreach (var window in _owned.ToArray()) window.Close();
        }

        private void RefreshWindows()
        {
            // Closed fires while the window is still in OwnedWindows, so count only the ones still showing.
            int owned = Owner?.OwnedWindows.Count(w => w.IsVisible) ?? 0;
            WindowsText.Text = $"独立窗口 {_free.Count} 个，子窗口 {_owned.Count} 个；所有者的 OwnedWindows 共 {owned} 个（含下面两块开的窗口）";
        }

        // ---- position and size ----

        private void OpenPlace()
        {
            if (_placeWindow is not null)
            {
                _placeWindow.Activate();
                return;
            }

            var window = NewWindow("位置与大小示例", "拖动或缩放它，再用页面上的按钮记下、移动、恢复。");
            _placeWindow = window;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_placeWindow, window)) _placeWindow = null;
                RefreshPlace();
            };
            window.PositionChanged += (_, _) => RefreshPlace();
            window.Resized += (_, _) => RefreshPlace();
            ShowOwned(window);
            RefreshPlace();
        }

        private void MovePlace()
        {
            if (_placeWindow is null) return;

            _placeWindow.Position = new PixelPoint(200, 120);
            _placeWindow.Width = 420;
            _placeWindow.Height = 260;
            RefreshPlace();
        }

        private void SavePlace()
        {
            if (_placeWindow is null) return;

            _saved = (_placeWindow.Position, _placeWindow.ClientSize);
            RefreshPlace();
        }

        private void RestorePlace()
        {
            if (_placeWindow is null || _saved is not { } saved) return;

            _placeWindow.Position = saved.Position;
            _placeWindow.Width = saved.Size.Width;
            _placeWindow.Height = saved.Size.Height;
            RefreshPlace();
        }

        private void RefreshPlace()
        {
            string current = _placeWindow is null
                ? "示例窗口：未打开"
                : $"示例窗口：Position = {_placeWindow.Position}，ClientSize = {_placeWindow.ClientSize.Width:0}×{_placeWindow.ClientSize.Height:0}";
            string saved = _saved is { } s
                ? $"已记下：Position = {s.Position}，ClientSize = {s.Size.Width:0}×{s.Size.Height:0}"
                : "已记下：（还没有）";
            PlaceText.Text = $"{current}；{saved}";
        }

        // ---- confirm before closing ----

        private void OpenEditor()
        {
            if (_editor is not null)
            {
                _editor.Activate();
                return;
            }

            _dirty = false;
            _confirmedClose = false;
            _blocked = 0;
            _lastChoice = "（还没触发）";

            var box = new TextBox { Name = "EditorBox", AcceptsReturn = true, Height = 70, PlaceholderText = "改点什么" };
            var save = new Button { Name = "EditorSaveButton", Content = "保存" };
            var state = new TextBlock { Name = "EditorState", Text = "没有未保存的更改" };
            box.TextChanged += (_, _) =>
            {
                _dirty = true;
                state.Text = "有未保存的更改";
                RefreshConfirm();
            };
            save.Click += (_, _) =>
            {
                _dirty = false;
                state.Text = "没有未保存的更改";
                RefreshConfirm();
            };

            var window = new Window
            {
                Title = "可编辑的窗口",
                Width = 320,
                Height = 220,
                Content = new StackPanel { Margin = new Thickness(16), Spacing = 8, Children = { box, save, state } }
            };
            _editor = window;
            window.Closing += OnEditorClosing;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_editor, window)) _editor = null;
                RefreshConfirm();
            };
            ShowOwned(window);
            RefreshConfirm();
        }

        // async void is fine here: this is an event handler, and Cancel is set before the first await.
        private async void OnEditorClosing(object? sender, WindowClosingEventArgs e)
        {
            if (!_dirty || _confirmedClose || e.CloseReason != WindowCloseReason.WindowClosing) return;

            e.Cancel = true;
            _blocked++;
            RefreshConfirm();

            var window = (Window)sender!;
            bool? discard = await DemoDialogs.ConfirmAsync(window, "放弃更改？", "窗口里的内容还没保存，确定要关闭吗？");
            _lastChoice = discard switch
            {
                true => "用户选了「确定」，窗口随后关闭",
                false => "用户选了「取消」，窗口留着",
                _ => "用户直接关掉了确认框，窗口留着"
            };
            RefreshConfirm();

            if (discard == true)
            {
                _confirmedClose = true;
                window.Close();
            }
        }

        private void RefreshConfirm()
        {
            string state = _editor is null ? "窗口未打开" : _dirty ? "窗口有未保存的更改" : "窗口没有未保存的更改";
            ConfirmText.Text = $"{state}；Closing 被拦下 {_blocked} 次；最近一次：{_lastChoice}";
        }
    }
}
```

- [ ] **Step 4: 抽取并构建**

Run: 同 Task 1 Step 4。
Expected: 0 个错误。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the Window and Dialogs how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: System 分类：TrayIcon 与数据持久化实战页

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/System/TrayIconPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/AppSettings.cs`、`SettingsStore.cs`、`RecentFiles.cs`、`DataPersistenceHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/System/DataPersistenceHowToPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份的 `DemoHeader`、`SharedStyles`；`Assets/avalonia-logo.ico`（已由 `<AvaloniaResource Include="Assets\**" />` 嵌入）
- Produces:
  - `TrayIconPage`：公开只读 `Tray : TrayIcon?`（页面在屏幕上时才有值）
  - `AppSettings`（`Username : string`、`FontSize : double`、`NotifyEnabled : bool`）
  - `SettingsStore(string folder)`：`FolderPath`、`FilePath`、`LastLoadFailed`、`Load() : AppSettings`、`Save(AppSettings)`、`Delete()`、`WriteRaw(string)`，静态 `DefaultDirectory`（`%AppData%\Avalonia.ControlsDemo`）
  - `RecentFiles(string folder)`：`Capacity = 10`、`Items : ObservableCollection<string>`、`FilePath`、`Add(string)`、`Clear()`，静态 `DefaultDirectory`（`%LocalAppData%\Avalonia.ControlsDemo`）
  - `DataPersistenceHowToViewModel(string settingsFolder, string recentFolder)`：`Username`、`FontSize`、`NotifyEnabled`、`SaveCount`、`StatusText`、`FileText`、`SettingsPath`、`RecentPath`、`RecentItems`，命令 `ReloadCommand`、`ResetCommand`、`DeleteFileCommand`、`CorruptFileCommand`、`AddRecentCommand`、`ClearRecentCommand`
  - `DataPersistenceHowToPage`（页面类，Task 5 登记）

- [ ] **Step 1: TrayIcon**

`TrayIcon` 不是可视树里的控件，要通过 `TrayIcon.SetIcons(Application.Current, new TrayIcons { icon })` 挂到应用上，所以页面在 `AttachedToVisualTree` 里挂、在 `DetachedFromVisualTree` 里摘（并还原挂之前的值），否则托盘里的图标会一直留着。菜单必须是 `NativeMenu`，菜单项的点击走 `Command`。**headless 里 `NativeMenuExporter` 为 `null`**（没有平台实现），页面如实显示这个检测结果，不假装有。

#### `Avalonia.ControlsDemo/Views/Pages/System/TrayIconPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TrayIconPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TrayIcon：系统托盘图标"
                               DocPath="controls/navigation/trayicon" />

            <TextBlock Classes="caption" Text="进入本页时挂上托盘图标，离开时摘掉" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="VisibleCheck" Content="IsVisible" />
                        <TextBlock VerticalAlignment="Center" Text="ToolTipText" />
                        <TextBox Name="TipBox" Width="220" />
                    </StackPanel>
                    <TextBlock Name="StateText" TextWrapping="Wrap" />
                    <TextBlock Name="MenuText" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="看任务栏右下角的托盘区：图标挂上后，右键能弹出「显示 / 开机提示 / 退出」三项原生菜单，左键点图标会触发 Clicked。「开机提示」是带勾选的菜单项（ToggleType = CheckBox），「退出」在这里只做计数，不会真的退出演示程序。IsVisible 改成 False 后图标从托盘消失但对象仍在。TrayIcon 依赖平台实现：NativeMenuExporter 为空说明当前平台没有原生托盘（比如 headless 测试环境），这时图标与菜单只是对象，不会真的出现。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/System/TrayIconPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TrayIconPage : UserControl
    {
        private TrayIcon? _tray;
        private TrayIcons? _previous;
        private NativeMenuItem? _toggleItem;
        private int _showClicks, _toggleClicks, _quitClicks, _iconClicks;

        public TrayIconPage()
        {
            InitializeComponent();

            VisibleCheck.IsChecked = true;
            TipBox.Text = "ControlsDemo 托盘图标";
            VisibleCheck.IsCheckedChanged += (_, _) =>
            {
                if (_tray is not null) _tray.IsVisible = VisibleCheck.IsChecked == true;
                Refresh();
            };
            TipBox.TextChanged += (_, _) =>
            {
                if (_tray is not null) _tray.ToolTipText = TipBox.Text;
                Refresh();
            };

            // Icons are attached to the Application, so tie their lifetime to this page being on screen.
            AttachedToVisualTree += (_, _) => Install();
            DetachedFromVisualTree += (_, _) => Uninstall();

            Refresh();
        }

        /// <summary>The tray icon while this page is on screen, otherwise null.</summary>
        public TrayIcon? Tray => _tray;

        private void Install()
        {
            if (_tray is not null || Application.Current is not { } app) return;

            var show = new NativeMenuItem { Header = "显示", Command = new RelayCommand(() => { _showClicks++; Refresh(); }) };
            _toggleItem = new NativeMenuItem
            {
                Header = "开机提示",
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = true,
                Command = new RelayCommand(() => { _toggleClicks++; Refresh(); })
            };
            var quit = new NativeMenuItem { Header = "退出（演示用，不会真的退出）", Command = new RelayCommand(() => { _quitClicks++; Refresh(); }) };

            using var stream = AssetLoader.Open(new Uri("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico"));
            _tray = new TrayIcon
            {
                Icon = new WindowIcon(stream),
                ToolTipText = TipBox.Text,
                IsVisible = VisibleCheck.IsChecked == true,
                Menu = new NativeMenu { Items = { show, new NativeMenuItemSeparator(), _toggleItem, quit } }
            };
            _tray.Clicked += (_, _) =>
            {
                _iconClicks++;
                Refresh();
            };

            _previous = TrayIcon.GetIcons(app);
            TrayIcon.SetIcons(app, new TrayIcons { _tray });
            Refresh();
        }

        private void Uninstall()
        {
            if (_tray is null) return;

            _tray.Dispose();
            if (Application.Current is { } app) TrayIcon.SetIcons(app, _previous);
            _tray = null;
            _toggleItem = null;
            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = _tray is null
                ? "托盘图标：未挂上（页面不在屏幕上）"
                : $"托盘图标：已挂上，IsVisible = {_tray.IsVisible}，ToolTipText = {_tray.ToolTipText}，菜单项 {_tray.Menu?.Items.Count} 个，" +
                  $"NativeMenuExporter = {(_tray.NativeMenuExporter is null ? "无（当前平台没有原生托盘实现）" : "有")}";
            MenuText.Text = $"菜单点击：显示 {_showClicks} 次，开机提示 {_toggleClicks} 次（IsChecked = {_toggleItem?.IsChecked.ToString() ?? "-"}），退出 {_quitClicks} 次；" +
                            $"图标本身被点击 {_iconClicks} 次（Clicked）";
        }
    }
}
```

- [ ] **Step 2: 数据持久化的模型与存储**

对应官方数据持久化指南的"JSON 设置文件""最近文件列表（上限 10）""各平台路径"三节：设置放 `ApplicationData`（漫游），最近文件放 `LocalApplicationData`（本机）。三条原则贯穿代码：**构造时只读不写**（页面一打开就写用户目录是不礼貌的）、**文件损坏时回退默认值而不是崩**、**目录由调用方传入**（页面用真实路径，探针用临时目录）。

#### `Avalonia.ControlsDemo/ViewModels/AppSettings.cs`

```csharp
namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>The shape of settings.json. Missing fields in an older file simply keep these defaults.</summary>
    public class AppSettings
    {
        public string Username { get; set; } = "";

        public double FontSize { get; set; } = 14;

        public bool NotifyEnabled { get; set; }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/SettingsStore.cs`

```csharp
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Reads and writes settings.json in a folder chosen by the caller.</summary>
    public class SettingsStore
    {
        // Relaxed escaping keeps Chinese text readable in the file instead of turning it into \uXXXX.
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public SettingsStore(string folder)
        {
            FolderPath = folder;
            FilePath = Path.Combine(folder, "settings.json");
        }

        /// <summary>Per-user, roaming: %AppData%\Avalonia.ControlsDemo on Windows.</summary>
        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.ControlsDemo");

        public string FolderPath { get; }

        public string FilePath { get; }

        /// <summary>True when the last Load found a file it could not parse and fell back to the defaults.</summary>
        public bool LastLoadFailed { get; private set; }

        public AppSettings Load()
        {
            LastLoadFailed = false;
            if (!File.Exists(FilePath)) return new AppSettings();

            try
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
            }
            catch (JsonException)
            {
                // A broken file must never stop the app from starting.
                LastLoadFailed = true;
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(FolderPath);

            // Write beside the target and swap, so a crash mid-write cannot leave half a file behind.
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
            File.Move(temp, FilePath, overwrite: true);
        }

        public void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        /// <summary>Writes text as-is, bypassing the serializer. Only the "corrupt file" demo uses it.</summary>
        public void WriteRaw(string text)
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, text);
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/RecentFiles.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A most-recently-used list of paths, newest first, capped at <see cref="Capacity"/> and kept in recent.json.</summary>
    public class RecentFiles
    {
        public const int Capacity = 10;

        private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public RecentFiles(string folder)
        {
            FolderPath = folder;
            FilePath = Path.Combine(folder, "recent.json");
            Load();
        }

        /// <summary>Per-machine, not roaming: %LocalAppData%\Avalonia.ControlsDemo on Windows.</summary>
        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Avalonia.ControlsDemo");

        public string FolderPath { get; }

        public string FilePath { get; }

        public ObservableCollection<string> Items { get; } = new();

        public void Add(string path)
        {
            // Opening a file that is already listed moves it to the top instead of listing it twice.
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Items[i], path, StringComparison.OrdinalIgnoreCase)) Items.RemoveAt(i);
            }

            Items.Insert(0, path);
            while (Items.Count > Capacity) Items.RemoveAt(Items.Count - 1);
            Save();
        }

        public void Clear()
        {
            Items.Clear();
            Save();
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;

            try
            {
                var saved = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(FilePath), Options);
                if (saved is null) return;
                foreach (string path in saved)
                {
                    if (Items.Count < Capacity) Items.Add(path);
                }
            }
            catch (JsonException)
            {
                // An unreadable list is just an empty list.
            }
        }

        private void Save()
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Items, Options));
        }
    }
}
```

- [ ] **Step 3: 数据持久化的 ViewModel**

三个设置项都靠 `OnXxxChanged` 钩子自动存盘；从文件读进来时先打开 `_loading`，免得"读文件"又触发"写文件"。

#### `Avalonia.ControlsDemo/ViewModels/DataPersistenceHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class DataPersistenceHowToViewModel : ObservableObject
    {
        private readonly SettingsStore _store;
        private readonly RecentFiles _recent;
        private bool _loading;
        private int _recentCounter;

        public DataPersistenceHowToViewModel(string settingsFolder, string recentFolder)
        {
            _store = new SettingsStore(settingsFolder);
            _recent = new RecentFiles(recentFolder);

            // Reading never writes: the first file appears only when the user changes something.
            Apply(_store.Load());
            _statusText = _store.LastLoadFailed ? "设置文件损坏，已回退到默认值"
                : File.Exists(_store.FilePath) ? "已从文件读到上次的设置"
                : "还没有设置文件，使用默认值";
            RefreshFileText();
        }

        public string SettingsPath => _store.FilePath;

        public string RecentPath => _recent.FilePath;

        public ObservableCollection<string> RecentItems => _recent.Items;

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private double _fontSize = 14;

        [ObservableProperty]
        private bool _notifyEnabled;

        [ObservableProperty]
        private int _saveCount;

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private string _fileText = "";

        partial void OnUsernameChanged(string value) => Save();

        partial void OnFontSizeChanged(double value) => Save();

        partial void OnNotifyEnabledChanged(bool value) => Save();

        [RelayCommand]
        private void Reload()
        {
            Apply(_store.Load());
            StatusText = _store.LastLoadFailed ? "设置文件损坏，已回退到默认值" : "已从文件重新读取";
            RefreshFileText();
        }

        [RelayCommand]
        private void Reset()
        {
            Apply(new AppSettings());
            Save();
            StatusText = "已恢复默认值并保存";
        }

        [RelayCommand]
        private void DeleteFile()
        {
            _store.Delete();
            StatusText = "设置文件已删除（界面上的值不变，下次启动会用默认值）";
            RefreshFileText();
        }

        [RelayCommand]
        private void CorruptFile()
        {
            _store.WriteRaw("{ this is not json");
            StatusText = "已把设置文件写坏，点「重新读取」看看会怎样";
            RefreshFileText();
        }

        [RelayCommand]
        private void AddRecent()
        {
            _recentCounter++;
            _recent.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"文档 {_recentCounter}.txt"));
        }

        [RelayCommand]
        private void ClearRecent() => _recent.Clear();

        private void Apply(AppSettings settings)
        {
            _loading = true;
            Username = settings.Username;
            FontSize = settings.FontSize;
            NotifyEnabled = settings.NotifyEnabled;
            _loading = false;
        }

        private void Save()
        {
            if (_loading) return;

            _store.Save(new AppSettings { Username = Username, FontSize = FontSize, NotifyEnabled = NotifyEnabled });
            SaveCount++;
            StatusText = $"已自动保存（第 {SaveCount} 次）";
            RefreshFileText();
        }

        private void RefreshFileText()
        {
            FileText = File.Exists(_store.FilePath) ? File.ReadAllText(_store.FilePath) : "（文件不存在）";
        }
    }
}
```

- [ ] **Step 4: 数据持久化实战页**

#### `Avalonia.ControlsDemo/Views/Pages/System/DataPersistenceHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:DataType="vm:DataPersistenceHowToViewModel"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DataPersistenceHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="实战：数据持久化（把设置存成 JSON）"
                               DocPath="docs/how-to/data-persistence-how-to" />

            <TextBlock Classes="caption" Text="设置：改一下就自动写进 settings.json" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="用户名" />
                        <TextBox Width="180" Text="{Binding Username}" />
                        <TextBlock VerticalAlignment="Center" Text="字号" />
                        <Slider Width="160" Minimum="10" Maximum="28" Value="{Binding FontSize}" />
                        <CheckBox Content="启用通知" IsChecked="{Binding NotifyEnabled}" />
                    </StackPanel>
                    <TextBlock FontSize="{Binding FontSize}" Text="{Binding Username, StringFormat='预览：你好，{0}'}" />
                    <WrapPanel>
                        <Button Margin="0,0,8,8" Content="重新读取" Command="{Binding ReloadCommand}" />
                        <Button Margin="0,0,8,8" Content="恢复默认" Command="{Binding ResetCommand}" />
                        <Button Margin="0,0,8,8" Content="删除文件" Command="{Binding DeleteFileCommand}" />
                        <Button Margin="0,0,8,8" Content="写坏文件" Command="{Binding CorruptFileCommand}" />
                    </WrapPanel>
                    <TextBlock Text="{Binding StatusText}" />
                    <TextBlock TextWrapping="Wrap" Text="{Binding SettingsPath, StringFormat='文件：{0}'}" />
                    <Border BorderBrush="Gray" BorderThickness="1" Padding="8">
                        <SelectableTextBlock FontFamily="Consolas, Menlo, monospace" Text="{Binding FileText}" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="设置是一个普通的 AppSettings 对象，SettingsStore 用 System.Text.Json 把它读写成 settings.json，放在 Environment.SpecialFolder.ApplicationData 下的应用子目录里（Windows 的 %AppData%，macOS 与 Linux 各有对应位置）。三个设置项在 ViewModel 里靠 OnXxxChanged 钩子自动存盘，下面框里是文件的真实内容。写文件时先写 .tmp 再替换，崩溃也不会留下半个文件；读文件时若 JSON 坏了就回退默认值，试试「写坏文件」再「重新读取」。页面一打开只读不写，所以没改过设置之前磁盘上不会多出文件。真实应用的文本框可以给自动保存加个防抖，这里为了看得见每次都写。" />

            <TextBlock Classes="caption" Text="最近文件：最新的在最上面，最多 10 条，存进 recent.json" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="假装打开一个文件" Command="{Binding AddRecentCommand}" />
                        <Button Content="清空" Command="{Binding ClearRecentCommand}" />
                        <TextBlock VerticalAlignment="Center" Text="{Binding RecentItems.Count, StringFormat='共 {0} 条，上限 10'}" />
                    </StackPanel>
                    <ListBox MaxHeight="160" ItemsSource="{Binding RecentItems}" />
                    <TextBlock TextWrapping="Wrap" Text="{Binding RecentPath, StringFormat='文件：{0}'}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="最近文件放在 LocalApplicationData（%LocalAppData%）而不是 ApplicationData：它是机器相关的路径，不该跟着账号漫游到别的电脑。重复打开同一个路径只会把它顶到最上面，不会出现两条；超过 10 条时最旧的被挤掉。这里点一次「假装打开」只是往列表里加一个路径，并没有真的创建文档。官方指南里针对 iOS、Android、WebAssembly 沙盒的「书签存储」桌面端用不上，这里不做。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/System/DataPersistenceHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DataPersistenceHowToPage : UserControl
    {
        public DataPersistenceHowToPage()
        {
            InitializeComponent();

            // The view model reads these folders but only writes when the user changes something.
            DataContext = new DataPersistenceHowToViewModel(SettingsStore.DefaultDirectory, RecentFiles.DefaultDirectory);
        }
    }
}
```

- [ ] **Step 5: 抽取并构建**

Run: 同 Task 1 Step 4。
Expected: 0 个错误。

- [ ] **Step 6: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate TrayIcon and add the data persistence how-to page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: 登记页面、探针验证、收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Primitives.cs`、`PageCatalog.System.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Prim.cs`、`Probe.Prim2.cs`、`Probe.Prim3.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）、`CLAUDE.md`（更新"进行中的工作"）

**Interfaces:**
- Consumes: Task 1–4 的全部页面类；第 00 份的 `ControlPage<T>`、`HowToPage<T>`、`Harness`
- Produces: Primitives 分类 5 个 `Control` 页 + 2 个 `HowTo` 页，System 分类 1 个 `Control` 页 + 1 个 `HowTo` 页

- [ ] **Step 1: 登记**

HowTo 紧跟的规则与第 05 份不同：Window 实战页紧跟 `Window` 控件页（同标题）；Dialogs 实战页**紧跟 Window 实战页**（spec：「紧随 window-how-to」），标题用 `Dialogs`；数据持久化实战页紧跟 `TrayIcon`（spec：System 分类里「follows TrayIcon」），标题用 `数据持久化`，导航树里显示为「实战：数据持久化」。三条"紧跟"关系都由探针逐条断言。

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Primitives.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddPrimitives(List<PageEntry> list)
        {
            const string c = Categories.Primitives;

            list.Add(ControlPage<ScrollBarPage>("ScrollBar", c, "controls/primitives/scrollbar"));
            list.Add(ControlPage<ThemeVariantScopePage>("ThemeVariantScope", c, "controls/primitives/themevariantscope"));
            list.Add(ControlPage<UserControlPage>("UserControl", c, "controls/primitives/usercontrol"));
            list.Add(ControlPage<WindowPage>("Window", c, "controls/primitives/window"));
            list.Add(HowToPage<WindowHowToPage>("Window", c, "docs/how-to/window-how-to"));
            list.Add(HowToPage<DialogsHowToPage>("Dialogs", c, "docs/how-to/dialogs-how-to"));
            list.Add(ControlPage<WindowDrawnDecorationsPage>("WindowDrawnDecorations", c, "controls/primitives/windowdrawndecorations"));
        }
    }
}
```

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.System.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddSystem(List<PageEntry> list)
        {
            const string c = Categories.System;

            // Official URL is under controls/navigation/, but the official sidebar files TrayIcon under System.
            list.Add(ControlPage<TrayIconPage>("TrayIcon", c, "controls/navigation/trayicon"));
            list.Add(HowToPage<DataPersistenceHowToPage>("数据持久化", c, "docs/how-to/data-persistence-how-to"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

三个文件，都是 `static partial class ProbePrim`：`Probe.Prim.cs`（目录完整性与 Primitives 的 5 个控件页）、`Probe.Prim2.cs`（TrayIcon 与两套对话框用例）、`Probe.Prim3.cs`（Window 实战页与数据持久化）。

**探针要守住的三条底线**：① 每个新开的窗口、托盘图标都在用例结尾关掉 / 还原；② 数据持久化的所有写操作都落在临时目录，而且最后断言"真实的 `%AppData%` 与 `%LocalAppData%` 里的文件没被碰过"（页面构造只读不写，这条断言把它钉死）；③ headless 里 `IsExtendedIntoWindowDecorations` 恒为 `False`，探针把它当作已记录的 headless 限制来断言，真实绘制只能目视。

**`C:\Temp\probe-controls\Probe.Prim.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Shared.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.IO;
using System.Linq;

static partial class ProbePrim
{
    public static void Run()
    {
        string before = Snapshot();

        Catalog();
        ScrollBarCases();
        ThemeScopeCases();
        UserControlCases();
        WindowCases();
        DecorationCases();
        Tray();
        DialogViewModel();
        DialogPage();
        WindowHowTo();
        Persistence();

        Harness.Check("Prim: the probes left the real settings and recent files alone", Snapshot() == before, $"{before} -> {Snapshot()}");
    }

    static void Click(Control root, string name) => Harness.Find<Button>(root, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    static void Settle()
    {
        // Window events, dialog results and template swaps all need more than one dispatcher round.
        for (int i = 0; i < 4; i++) Harness.Pump();
    }

    static string Text(Control root, string name) => Harness.Find<TextBlock>(root, name).Text ?? "";

    // Last-write time of the two real files the pages know about; "-" when a file does not exist.
    static string Snapshot()
    {
        static string Stat(string path) => File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks.ToString() : "-";
        return Stat(Path.Combine(SettingsStore.DefaultDirectory, "settings.json")) + "|" + Stat(Path.Combine(RecentFiles.DefaultDirectory, "recent.json"));
    }

    static void Follows(List<PageEntry> all, string title, PageKind kind, string previousTitle, PageKind previousKind)
    {
        int i = all.FindIndex(e => e.Title == title && e.Kind == kind);
        Harness.Check($"Prim: {kind} {title} follows {previousKind} {previousTitle}", i > 0 && all[i - 1].Title == previousTitle && all[i - 1].Kind == previousKind, i);
    }

    static void Catalog()
    {
        var all = PageCatalog.All.Where(e => e.Category == Categories.Primitives || e.Category == Categories.System).ToList();
        int Count(string category, PageKind kind) => all.Count(e => e.Category == category && e.Kind == kind);

        Harness.Check("Prim: Primitives has 5 Control pages", Count(Categories.Primitives, PageKind.Control) == 5, Count(Categories.Primitives, PageKind.Control));
        Harness.Check("Prim: Primitives has 2 HowTo pages", Count(Categories.Primitives, PageKind.HowTo) == 2, Count(Categories.Primitives, PageKind.HowTo));
        Harness.Check("Prim: System has 1 Control page", Count(Categories.System, PageKind.Control) == 1, Count(Categories.System, PageKind.Control));
        Harness.Check("Prim: System has 1 HowTo page", Count(Categories.System, PageKind.HowTo) == 1, Count(Categories.System, PageKind.HowTo));

        string order = string.Join(",", all.Where(e => e.Category == Categories.Primitives).Select(e => e.Title));
        Harness.Check("Prim: Primitives follows the official order, how-tos right after Window",
            order == "ScrollBar,ThemeVariantScope,UserControl,Window,Window,Dialogs,WindowDrawnDecorations", order);
        Follows(all, "Window", PageKind.HowTo, "Window", PageKind.Control);
        Follows(all, "Dialogs", PageKind.HowTo, "Window", PageKind.HowTo);
        Follows(all, "数据持久化", PageKind.HowTo, "TrayIcon", PageKind.Control);

        foreach (var entry in all.Where(e => e.Kind == PageKind.HowTo))
        {
            Harness.Check($"Prim: HowTo {entry.Title} DocPath starts with docs/how-to/", entry.DocPath.StartsWith("docs/how-to/"), entry.DocPath);
        }
        Harness.Check("Prim: TrayIcon keeps the official navigation URL", all.Single(e => e.Title == "TrayIcon").DocPath == "controls/navigation/trayicon");

        foreach (var entry in all)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Prim/{entry.Title}({entry.Kind}): renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"Prim/{entry.Title}({entry.Kind}): header matches catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            w.Close();
        }
    }

    static void ScrollBarCases()
    {
        var p = new ScrollBarPage();
        var w = Harness.Show(p);
        var bar = Harness.Find<ScrollBar>(p, "Bar");
        Harness.Check("ScrollBar: range 0..100, viewport 20, small 1, large 10", bar.Minimum == 0 && bar.Maximum == 100 && bar.ViewportSize == 20 && bar.SmallChange == 1 && bar.LargeChange == 10);
        Harness.Check("ScrollBar: starts at 0 with no Scroll events",
            Text(p, "ValueText") == "Value = 0（Maximum = 100，ViewportSize = 20）" && Text(p, "ScrollText") == "Scroll 事件 0 次，最近一次：（无）", $"{Text(p, "ValueText")} | {Text(p, "ScrollText")}");

        Click(p, "LineDownButton"); Settle();
        Harness.Check("ScrollBar: LineDown adds SmallChange and raises Scroll", bar.Value == 1 && Text(p, "ScrollText") == "Scroll 事件 1 次，最近一次：SmallIncrement", $"{bar.Value} | {Text(p, "ScrollText")}");
        Click(p, "PageDownButton"); Settle();
        Harness.Check("ScrollBar: PageDown adds LargeChange", bar.Value == 11 && Text(p, "ScrollText") == "Scroll 事件 2 次，最近一次：LargeIncrement", $"{bar.Value} | {Text(p, "ScrollText")}");
        Click(p, "LineUpButton"); Settle();
        Harness.Check("ScrollBar: LineUp subtracts SmallChange", bar.Value == 10 && Text(p, "ScrollText") == "Scroll 事件 3 次，最近一次：SmallDecrement", $"{bar.Value} | {Text(p, "ScrollText")}");
        Click(p, "EndButton"); Settle();
        Harness.Check("ScrollBar: ScrollToEnd jumps to Maximum", bar.Value == 100 && Text(p, "ScrollText").StartsWith("Scroll 事件 4 次"), $"{bar.Value} | {Text(p, "ScrollText")}");
        Click(p, "HomeButton"); Settle();
        Harness.Check("ScrollBar: ScrollToHome jumps to Minimum", bar.Value == 0 && Text(p, "ScrollText") == "Scroll 事件 5 次，最近一次：LargeDecrement", $"{bar.Value} | {Text(p, "ScrollText")}");

        Harness.Find<Slider>(p, "ValueSlider").Value = 50; Settle();
        Harness.Check("ScrollBar: writing Value directly changes it but raises no Scroll",
            bar.Value == 50 && Text(p, "ScrollText").StartsWith("Scroll 事件 5 次") && Text(p, "ValueText").StartsWith("Value = 50"), $"{bar.Value} | {Text(p, "ScrollText")}");
        Harness.Check("ScrollBar: a method call moves the slider along", Harness.Find<Slider>(p, "ValueSlider").Value == 50);
        Click(p, "HomeButton"); Settle();
        Harness.Check("ScrollBar: the slider mirrors the bar after a method call", Harness.Find<Slider>(p, "ValueSlider").Value == 0, Harness.Find<Slider>(p, "ValueSlider").Value);

        Harness.Check("ScrollBar: Visibility starts Visible and AllowAutoHide on", bar.Visibility == ScrollBarVisibility.Visible && bar.AllowAutoHide
            && Harness.Find<CheckBox>(p, "AutoHideCheck").IsChecked == true);
        Harness.Find<ComboBox>(p, "VisibilityBox").SelectedItem = ScrollBarVisibility.Hidden; Settle();
        Harness.Check("ScrollBar: the box drives Visibility", bar.Visibility == ScrollBarVisibility.Hidden && !bar.IsVisible, $"{bar.Visibility}/{bar.IsVisible}");
        Harness.Find<ComboBox>(p, "VisibilityBox").SelectedItem = ScrollBarVisibility.Visible; Settle();
        Harness.Check("ScrollBar: and brings it back", bar.IsVisible);
        Harness.Find<CheckBox>(p, "AutoHideCheck").IsChecked = false;
        Harness.Check("ScrollBar: the check box drives AllowAutoHide", !bar.AllowAutoHide);
        w.Close();
    }

    static void ThemeScopeCases()
    {
        var p = new ThemeVariantScopePage();
        var w = Harness.Show(p);
        var lightScope = Harness.Find<ThemeVariantScope>(p, "LightScope");
        var darkScope = Harness.Find<ThemeVariantScope>(p, "DarkScope");
        Harness.Check("ThemeVariantScope: Light and Dark scopes resolve to their own variants",
            lightScope.ActualThemeVariant == ThemeVariant.Light && darkScope.ActualThemeVariant == ThemeVariant.Dark, $"{lightScope.ActualThemeVariant}/{darkScope.ActualThemeVariant}");

        var lightColor = (Harness.Find<Border>(p, "LightBorder").Background as ISolidColorBrush)?.Color;
        var darkColor = (Harness.Find<Border>(p, "DarkBorder").Background as ISolidColorBrush)?.Color;
        Harness.Check("ThemeVariantScope: the same DynamicResource gives a lighter colour in Light than in Dark",
            lightColor is { } l && darkColor is { } d && l.R > d.R, $"{lightColor}/{darkColor}");

        var live = Harness.Find<ThemeVariantScope>(p, "LiveScope");
        var box = Harness.Find<ComboBox>(p, "VariantBox");
        Harness.Check("ThemeVariantScope: the live scope starts on Default and follows the window",
            box.SelectedItem is ThemeVariant chosen && chosen == ThemeVariant.Default && live.ActualThemeVariant == w.ActualThemeVariant
            && Text(p, "VariantText") == $"Requested = Default，Actual = {live.ActualThemeVariant}", Text(p, "VariantText"));

        box.SelectedItem = ThemeVariant.Dark; Settle();
        Harness.Check("ThemeVariantScope: choosing Dark forces Dark", live.RequestedThemeVariant == ThemeVariant.Dark && live.ActualThemeVariant == ThemeVariant.Dark
            && Text(p, "VariantText") == "Requested = Dark，Actual = Dark", Text(p, "VariantText"));
        box.SelectedItem = ThemeVariant.Light; Settle();
        Harness.Check("ThemeVariantScope: choosing Light forces Light", live.ActualThemeVariant == ThemeVariant.Light && Text(p, "VariantText") == "Requested = Light，Actual = Light", Text(p, "VariantText"));
        box.SelectedItem = ThemeVariant.Default; Settle();
        Harness.Check("ThemeVariantScope: back to Default it follows the window again", live.ActualThemeVariant == w.ActualThemeVariant, live.ActualThemeVariant);
        w.Close();
    }

    static void UserControlCases()
    {
        var p = new UserControlPage();
        var w = Harness.Show(p);
        var swatches = p.GetVisualDescendants().OfType<SwatchView>().ToList();
        Harness.Check("UserControl: four SwatchView instances", swatches.Count == 4, swatches.Count);
        Harness.Check("UserControl: Title is set from XAML", string.Join(",", swatches.Select(s => s.Title)) == "红,绿,蓝,可改的一块", string.Join(",", swatches.Select(s => s.Title)));

        static Border Block(SwatchView s) => s.GetVisualDescendants().OfType<Border>().First(b => b.Height == 56);
        static string Caption(SwatchView s) => s.GetVisualDescendants().OfType<TextBlock>().Single().Text ?? "";
        static Color? Fill(SwatchView s) => (Block(s).Background as ISolidColorBrush)?.Color;

        Harness.Check("UserControl: the inner block paints its own Fill", Fill(swatches[0]) == Colors.Crimson && Fill(swatches[1]) == Colors.SeaGreen && Fill(swatches[2]) == Colors.SteelBlue,
            $"{Fill(swatches[0])}/{Fill(swatches[1])}/{Fill(swatches[2])}");
        Harness.Check("UserControl: the inner caption shows Title", Caption(swatches[0]) == "红" && Caption(swatches[3]) == "可改的一块", $"{Caption(swatches[0])}/{Caption(swatches[3])}");

        var live = Harness.Find<SwatchView>(p, "Live");
        Harness.Check("UserControl: the live block starts grey", Fill(live) == Colors.Gray && Text(p, "StateText") == $"Live.Title = 可改的一块，Live.Fill = {Colors.Gray}", Text(p, "StateText"));
        Harness.Find<ComboBox>(p, "ColorBox").SelectedItem = "Orange"; Settle();
        Harness.Check("UserControl: changing the Fill property repaints only that block",
            Fill(live) == Colors.Orange && Fill(swatches[0]) == Colors.Crimson && Text(p, "StateText") == $"Live.Title = 可改的一块，Live.Fill = {Colors.Orange}", $"{Fill(live)} | {Text(p, "StateText")}");
        Harness.Find<TextBox>(p, "TitleBox").Text = "新标题"; Settle();
        Harness.Check("UserControl: changing Title updates the caption", live.Title == "新标题" && Caption(live) == "新标题" && Caption(swatches[0]) == "红", $"{live.Title}/{Caption(live)}");
        Harness.Find<TextBox>(p, "TitleBox").Text = ""; Settle();
        Harness.Check("UserControl: clearing the box restores the default caption", live.Title == "可改的一块" && Caption(live) == "可改的一块", live.Title);
        w.Close();
    }

    static void WindowCases()
    {
        var p = new WindowPage();
        var w = Harness.Show(p);
        Harness.Check("Window: nothing is open at first", p.Sample is null && Text(p, "StateText") == "示例窗口：未打开" && Text(p, "OwnerText") == "所有者的 OwnedWindows 数：0",
            $"{Text(p, "StateText")} | {Text(p, "OwnerText")}");

        Click(p, "OpenButton"); Settle();
        var s = p.Sample!;
        Harness.Check("Window: Show(owner) opens a visible child that the owner tracks", p.Sample is not null && s.IsVisible && w.OwnedWindows.Count == 1 && Text(p, "OwnerText") == "所有者的 OwnedWindows 数：1",
            $"{s.IsVisible}/{w.OwnedWindows.Count}/{Text(p, "OwnerText")}");
        Harness.Check("Window: the readout shows IsVisible and WindowState", Text(p, "StateText").StartsWith("示例窗口：IsVisible = True，WindowState = Normal"), Text(p, "StateText"));
        Click(p, "OpenButton"); Settle();
        Harness.Check("Window: opening again reuses the same window", ReferenceEquals(p.Sample, s) && w.OwnedWindows.Count == 1);

        Harness.Check("Window: a new window starts resizable, minimisable, maximisable and in the taskbar", s.CanResize && s.CanMinimize && s.CanMaximize && !s.Topmost && s.ShowInTaskbar);
        Harness.Find<CheckBox>(p, "ResizeCheck").IsChecked = false;
        Harness.Find<CheckBox>(p, "MinimizeCheck").IsChecked = false;
        Harness.Find<CheckBox>(p, "MaximizeCheck").IsChecked = false;
        Harness.Find<CheckBox>(p, "TopmostCheck").IsChecked = true;
        Harness.Find<CheckBox>(p, "TaskbarCheck").IsChecked = false;
        Settle();
        Harness.Check("Window: the check boxes drive the five bool properties", !s.CanResize && !s.CanMinimize && !s.CanMaximize && s.Topmost && !s.ShowInTaskbar,
            $"{s.CanResize}/{s.CanMinimize}/{s.CanMaximize}/{s.Topmost}/{s.ShowInTaskbar}");

        Harness.Find<ComboBox>(p, "StateBox").SelectedItem = WindowState.Maximized; Settle();
        Harness.Check("Window: the box drives WindowState", s.WindowState == WindowState.Maximized && Text(p, "StateText").Contains("WindowState = Maximized"), $"{s.WindowState} | {Text(p, "StateText")}");
        s.WindowState = WindowState.Normal; Settle();
        Harness.Check("Window: a state change made elsewhere is mirrored into the box", Harness.Find<ComboBox>(p, "StateBox").SelectedItem as WindowState? == WindowState.Normal, Harness.Find<ComboBox>(p, "StateBox").SelectedItem);
        Harness.Find<ComboBox>(p, "SizeBox").SelectedItem = SizeToContent.WidthAndHeight; Settle();
        Harness.Check("Window: the box drives SizeToContent", s.SizeToContent == SizeToContent.WidthAndHeight, s.SizeToContent);
        Harness.Find<ComboBox>(p, "SizeBox").SelectedItem = SizeToContent.Manual; Settle();

        Harness.Find<CheckBox>(p, "BlockCheck").IsChecked = true;
        Click(p, "CloseButton"); Settle();
        Harness.Check("Window: with blocking on, Close() raises Closing but the window stays", p.Sample is not null && s.IsVisible && p.ClosingCount == 1, $"{p.Sample is not null}/{s.IsVisible}/{p.ClosingCount}");
        Harness.Find<CheckBox>(p, "BlockCheck").IsChecked = false;
        Click(p, "CloseButton"); Settle();
        Harness.Check("Window: with blocking off, Close() really closes it", p.Sample is null && !s.IsVisible && p.ClosingCount == 2 && w.OwnedWindows.Count == 0
            && Text(p, "StateText") == "示例窗口：已关闭（上次共触发 Closing 2 次）" && Text(p, "OwnerText") == "所有者的 OwnedWindows 数：0", $"{p.ClosingCount}/{w.OwnedWindows.Count}/{Text(p, "StateText")}/{Text(p, "OwnerText")}");

        Click(p, "OpenButton"); Settle();
        var second = p.Sample!;
        Harness.Check("Window: reopening resets the Closing counter", p.ClosingCount == 0 && !ReferenceEquals(second, s), p.ClosingCount);
        Harness.Find<CheckBox>(p, "BlockCheck").IsChecked = true;
        w.Content = null; Settle();
        Harness.Check("Window: leaving the page closes the sample even while blocking", p.Sample is null && !second.IsVisible, $"{p.Sample is null}/{second.IsVisible}");
        w.Close();

        var p3 = new WindowPage();
        var w3 = Harness.Show(p3);
        Click(p3, "OpenButton"); Settle();
        var third = p3.Sample!;
        Harness.Find<CheckBox>(p3, "BlockCheck").IsChecked = true;
        w3.Close(); Settle();
        Harness.Check("Window: closing the owner takes the child with it even while blocking", !third.IsVisible, third.IsVisible);
    }

    static void DecorationCases()
    {
        var p = new WindowDrawnDecorationsPage();
        var w = Harness.Show(p);
        Harness.Check("Decorations: nothing is open at first, the box starts on Full",
            p.Sample is null && Text(p, "StateText") == "示例窗口：未打开" && Harness.Find<ComboBox>(p, "DecorationsBox").SelectedItem as WindowDecorations? == WindowDecorations.Full);

        Click(p, "OpenSystemButton"); Settle();
        var system = p.Sample!;
        Harness.Check("Decorations: the system sample opens with Full and no extension", system.IsVisible && system.WindowDecorations == WindowDecorations.Full && !system.ExtendClientAreaToDecorationsHint,
            $"{system.WindowDecorations}/{system.ExtendClientAreaToDecorationsHint}");
        Harness.Check("Decorations: the readout reports headless drawing no decorations", Text(p, "StateText").StartsWith("WindowDecorations = Full，ExtendClientAreaToDecorationsHint = False，IsExtendedIntoWindowDecorations = False"), Text(p, "StateText"));

        foreach (var mode in new[] { WindowDecorations.None, WindowDecorations.BorderOnly, WindowDecorations.Full })
        {
            Harness.Find<ComboBox>(p, "DecorationsBox").SelectedItem = mode; Settle();
            Harness.Check($"Decorations: the box drives WindowDecorations = {mode}", system.WindowDecorations == mode && Text(p, "StateText").StartsWith($"WindowDecorations = {mode}"), $"{system.WindowDecorations} | {Text(p, "StateText")}");
        }
        Harness.Find<CheckBox>(p, "ExtendCheck").IsChecked = true; Settle();
        Harness.Check("Decorations: the check box drives ExtendClientAreaToDecorationsHint", system.ExtendClientAreaToDecorationsHint && Text(p, "StateText").Contains("ExtendClientAreaToDecorationsHint = True"), Text(p, "StateText"));

        Click(p, "OpenCustomButton"); Settle();
        var custom = p.Sample!;
        Harness.Check("Decorations: the custom window replaces the system one", !system.IsVisible && custom.IsVisible && !ReferenceEquals(custom, system) && custom.WindowDecorations == WindowDecorations.None,
            $"{system.IsVisible}/{custom.IsVisible}/{custom.WindowDecorations}");

        var roles = custom.GetVisualDescendants().OfType<Control>().Select(c => WindowDecorationProperties.GetElementRole(c)).Where(r => r != WindowDecorationsElementRole.None)
            .Select(r => r.ToString()).OrderBy(r => r).ToList();
        Harness.Check("Decorations: exactly four elements carry a role", string.Join(",", roles) == "CloseButton,MaximizeButton,MinimizeButton,TitleBar", string.Join(",", roles));
        Harness.Check("Decorations: the page reports the roles it read back", Text(p, "RolesText") == "已标记的角色（读回）：TitleBar、MinimizeButton、MaximizeButton、CloseButton", Text(p, "RolesText"));
        Harness.Check("Decorations: the custom title bar has the three buttons", custom.GetVisualDescendants().OfType<Button>().Count() == 3, custom.GetVisualDescendants().OfType<Button>().Count());

        Harness.Find<ComboBox>(p, "DecorationsBox").SelectedItem = WindowDecorations.Full; Settle();
        Harness.Check("Decorations: the box only touches the system sample, not the custom one", custom.WindowDecorations == WindowDecorations.None, custom.WindowDecorations);

        w.Content = null; Settle();
        Harness.Check("Decorations: leaving the page closes the sample", p.Sample is null && !custom.IsVisible, $"{p.Sample is null}/{custom.IsVisible}");
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Prim2.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Linq;
using System.Threading.Tasks;

static partial class ProbePrim
{
    // A stand-in for the real dialogs: it answers instantly with whatever the case has set up.
    sealed class FakeDialogs : IDialogService
    {
        public bool? Confirm;
        public string? Name;
        public Color? Pick;
        public string? LastTitle;
        public bool? LastRequired;
        public Color? LastInitial;

        public Task<bool?> ConfirmAsync(string title, string message)
        {
            LastTitle = title;
            return Task.FromResult(Confirm);
        }

        public Task<string?> PromptAsync(string title, string prompt, bool required)
        {
            LastTitle = title;
            LastRequired = required;
            return Task.FromResult(Name);
        }

        public Task<Color?> PickColorAsync(Color initial)
        {
            LastInitial = initial;
            return Task.FromResult(Pick);
        }
    }

    static void Tray()
    {
        var app = Application.Current!;
        var previous = TrayIcon.GetIcons(app);
        var p = new TrayIconPage();
        Harness.Check("Tray: nothing is installed before the page is on screen", p.Tray is null && TrayIcon.GetIcons(app)?.Count == previous?.Count);

        var w = Harness.Show(p);
        var tray = p.Tray;
        Harness.Check("Tray: showing the page installs exactly one icon on the application",
            tray is not null && TrayIcon.GetIcons(app)?.Count == 1 && ReferenceEquals(TrayIcon.GetIcons(app)![0], tray));

        var items = tray!.Menu!.Items;
        Harness.Check("Tray: the menu is show, separator, check-box toggle, quit",
            items.Count == 4 && (items[0] as NativeMenuItem)?.Header == "显示" && items[1] is NativeMenuItemSeparator
            && items[2] is NativeMenuItem { ToggleType: MenuItemToggleType.CheckBox, IsChecked: true } && (items[3] as NativeMenuItem)?.Header == "退出（演示用，不会真的退出）", items.Count);
        Harness.Check("Tray: the icon is visible, has the default tooltip and a WindowIcon", tray.IsVisible && tray.ToolTipText == "ControlsDemo 托盘图标" && tray.Icon is not null);
        Harness.Check("Tray: headless has no NativeMenuExporter and the page says so",
            tray.NativeMenuExporter is null
            && Text(p, "StateText") == "托盘图标：已挂上，IsVisible = True，ToolTipText = ControlsDemo 托盘图标，菜单项 4 个，NativeMenuExporter = 无（当前平台没有原生托盘实现）", Text(p, "StateText"));

        ((NativeMenuItem)items[0]).Command!.Execute(null);
        ((NativeMenuItem)items[0]).Command!.Execute(null);
        ((NativeMenuItem)items[2]).Command!.Execute(null);
        ((NativeMenuItem)items[3]).Command!.Execute(null);
        Settle();
        Harness.Check("Tray: menu commands update the click counters",
            Text(p, "MenuText") == "菜单点击：显示 2 次，开机提示 1 次（IsChecked = True），退出 1 次；图标本身被点击 0 次（Clicked）", Text(p, "MenuText"));

        Harness.Find<CheckBox>(p, "VisibleCheck").IsChecked = false;
        Harness.Check("Tray: the check box drives IsVisible", !tray.IsVisible && Text(p, "StateText").StartsWith("托盘图标：已挂上，IsVisible = False"), Text(p, "StateText"));
        Harness.Find<TextBox>(p, "TipBox").Text = "新提示"; Settle();
        Harness.Check("Tray: the box drives ToolTipText", tray.ToolTipText == "新提示" && Text(p, "StateText").Contains("ToolTipText = 新提示"), Text(p, "StateText"));

        w.Content = null; Settle();
        Harness.Check("Tray: leaving the page removes the icon and restores what was there before",
            p.Tray is null && ReferenceEquals(TrayIcon.GetIcons(app), previous) && Text(p, "StateText") == "托盘图标：未挂上（页面不在屏幕上）", Text(p, "StateText"));

        w.Content = p; Settle();
        Harness.Check("Tray: coming back installs a fresh icon that keeps the page's settings",
            p.Tray is not null && !ReferenceEquals(p.Tray, tray) && p.Tray!.ToolTipText == "新提示" && !p.Tray.IsVisible && TrayIcon.GetIcons(app)?.Count == 1,
            $"{p.Tray?.ToolTipText}/{p.Tray?.IsVisible}");

        w.Content = null; Settle();
        w.Close();
        Harness.Check("Tray: nothing is left on the application afterwards", ReferenceEquals(TrayIcon.GetIcons(app), previous));
    }

    static void DialogViewModel()
    {
        var fake = new FakeDialogs();
        var vm = new DialogsHowToViewModel(fake);
        Harness.Check("DialogsVm: starts with nothing asked", vm.ConfirmResult == "（还没问过）" && vm.NameResult == "（还没问过）" && vm.ColorResult == "（还没选过）" && vm.PickedColor == Colors.Gray);

        fake.Confirm = true; vm.AskConfirmCommand.Execute(null);
        Harness.Check("DialogsVm: true reads as OK", vm.ConfirmResult == "结果 true：用户点了「确定」" && fake.LastTitle == "确认", vm.ConfirmResult);
        fake.Confirm = false; vm.AskConfirmCommand.Execute(null);
        Harness.Check("DialogsVm: false reads as Cancel", vm.ConfirmResult == "结果 false：用户点了「取消」", vm.ConfirmResult);
        fake.Confirm = null; vm.AskConfirmCommand.Execute(null);
        Harness.Check("DialogsVm: null reads as the window being closed", vm.ConfirmResult == "结果 null：用户直接关掉了窗口，既没确定也没取消", vm.ConfirmResult);

        fake.Name = "小明"; vm.AskNameCommand.Execute(null);
        Harness.Check("DialogsVm: a typed name is kept, and the prompt is not required", vm.NameResult == "名字：小明" && fake.LastRequired == false, vm.NameResult);
        fake.Name = null; vm.AskNameCommand.Execute(null);
        Harness.Check("DialogsVm: no name when the user backed out", vm.NameResult == "没有名字（取消或直接关闭）", vm.NameResult);
        fake.Name = "阿福"; vm.AskRequiredNameCommand.Execute(null);
        Harness.Check("DialogsVm: the required prompt passes required = true", vm.NameResult == "名字（必答）：阿福" && fake.LastRequired == true, vm.NameResult);

        fake.Pick = Colors.Orange; vm.PickColorCommand.Execute(null);
        Harness.Check("DialogsVm: a picked colour replaces PickedColor and its brush",
            vm.PickedColor == Colors.Orange && (vm.PickedBrush as ISolidColorBrush)?.Color == Colors.Orange && vm.ColorResult == $"结果 Color：{Colors.Orange}" && fake.LastInitial == Colors.Gray,
            $"{vm.PickedColor}/{vm.ColorResult}");
        fake.Pick = null; vm.PickColorCommand.Execute(null);
        Harness.Check("DialogsVm: a cancelled pick keeps the colour", vm.PickedColor == Colors.Orange && vm.ColorResult == "结果 null：没选，颜色保持不变" && fake.LastInitial == Colors.Orange,
            $"{vm.PickedColor}/{vm.ColorResult}");
    }

    static void DialogPage()
    {
        var p = new DialogsHowToPage();
        var w = Harness.Show(p);
        var vm = (DialogsHowToViewModel)p.DataContext!;

        Harness.Check("Dialogs: headless reports every storage capability as False and disables the picker button",
            Text(p, "StorageText") == "CanOpen = False，CanSave = False，CanPickFolder = False" && !Harness.Find<Button>(p, "OpenFileButton").IsEnabled, Text(p, "StorageText"));

        vm.AskConfirmCommand.Execute(null); Settle();
        var d = w.OwnedWindows.Single();
        Harness.Check("Dialogs: AskConfirm opens a modal dialog owned by the page's window", d.IsVisible && d.IsDialog && d.Title == "确认" && vm.ConfirmResult == "（还没问过）", $"{d.Title}/{d.IsDialog}");
        Click(d, "OkButton"); Settle();
        Harness.Check("Dialogs: OK gives true and the dialog goes away", vm.ConfirmResult == "结果 true：用户点了「确定」" && w.OwnedWindows.Count == 0 && !d.IsVisible, vm.ConfirmResult);

        vm.AskConfirmCommand.Execute(null); Settle();
        Click(w.OwnedWindows.Single(), "CancelButton"); Settle();
        Harness.Check("Dialogs: Cancel gives false", vm.ConfirmResult == "结果 false：用户点了「取消」", vm.ConfirmResult);

        vm.AskConfirmCommand.Execute(null); Settle();
        w.OwnedWindows.Single().Close(); Settle();
        Harness.Check("Dialogs: closing the window gives null, not false", vm.ConfirmResult == "结果 null：用户直接关掉了窗口，既没确定也没取消" && w.OwnedWindows.Count == 0, vm.ConfirmResult);

        vm.AskNameCommand.Execute(null); Settle();
        d = w.OwnedWindows.Single();
        Harness.Check("Dialogs: the prompt starts with OK disabled and Cancel enabled", !Harness.Find<Button>(d, "OkButton").IsEnabled && Harness.Find<Button>(d, "CancelButton").IsEnabled);
        Harness.Find<TextBox>(d, "AnswerBox").Text = "   "; Settle();
        Harness.Check("Dialogs: blank text keeps OK disabled", !Harness.Find<Button>(d, "OkButton").IsEnabled);
        Harness.Find<TextBox>(d, "AnswerBox").Text = "  小明 "; Settle();
        Harness.Check("Dialogs: real text enables OK", Harness.Find<Button>(d, "OkButton").IsEnabled);
        Click(d, "OkButton"); Settle();
        Harness.Check("Dialogs: the answer comes back trimmed", vm.NameResult == "名字：小明" && w.OwnedWindows.Count == 0, vm.NameResult);

        vm.AskNameCommand.Execute(null); Settle();
        Click(w.OwnedWindows.Single(), "CancelButton"); Settle();
        Harness.Check("Dialogs: Cancel on the prompt gives no name", vm.NameResult == "没有名字（取消或直接关闭）", vm.NameResult);

        vm.AskRequiredNameCommand.Execute(null); Settle();
        d = w.OwnedWindows.Single();
        Harness.Check("Dialogs: the required prompt disables Cancel", !Harness.Find<Button>(d, "CancelButton").IsEnabled);
        d.Close(); Settle();
        Harness.Check("Dialogs: the required prompt cannot be closed from the title bar", d.IsVisible && w.OwnedWindows.Count == 1 && vm.NameResult == "没有名字（取消或直接关闭）", $"{d.IsVisible}/{w.OwnedWindows.Count}");
        Harness.Find<TextBox>(d, "AnswerBox").Text = "阿福"; Settle();
        Click(d, "OkButton"); Settle();
        Harness.Check("Dialogs: answering lets it close with the answer", vm.NameResult == "名字（必答）：阿福" && w.OwnedWindows.Count == 0 && !d.IsVisible, vm.NameResult);

        vm.PickColorCommand.Execute(null); Settle();
        d = w.OwnedWindows.Single();
        Harness.Check("Dialogs: the colour dialog offers four swatches and Cancel",
            new[] { "Swatch_Red", "Swatch_Green", "Swatch_Blue", "Swatch_Orange", "CancelButton" }.All(n => d.GetVisualDescendants().OfType<Button>().Any(b => b.Name == n)));
        Click(d, "Swatch_Orange"); Settle();
        Harness.Check("Dialogs: choosing a swatch returns its colour", vm.PickedColor == Colors.Orange && vm.ColorResult == $"结果 Color：{Colors.Orange}" && w.OwnedWindows.Count == 0, $"{vm.PickedColor}/{vm.ColorResult}");
        Harness.Check("Dialogs: the preview block follows the picked colour",
            p.GetVisualDescendants().OfType<Border>().Any(b => b.Width == 80 && (b.Background as ISolidColorBrush)?.Color == Colors.Orange));

        vm.PickColorCommand.Execute(null); Settle();
        Click(w.OwnedWindows.Single(), "CancelButton"); Settle();
        Harness.Check("Dialogs: cancelling the colour dialog keeps the colour", vm.PickedColor == Colors.Orange && vm.ColorResult == "结果 null：没选，颜色保持不变", vm.ColorResult);
        Harness.Check("Dialogs: the page shows the view model's strings",
            p.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == vm.ConfirmResult) && p.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == vm.ColorResult));
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Prim3.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;

static partial class ProbePrim
{
    static void WindowHowTo()
    {
        var p = new WindowHowToPage();
        var w = Harness.Show(p);

        Harness.Check("WindowHowTo: starts with no windows", Text(p, "WindowsText") == "独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）"
            && Text(p, "PlaceText") == "示例窗口：未打开；已记下：（还没有）" && Text(p, "ConfirmText") == "窗口未打开；Closing 被拦下 0 次；最近一次：（还没触发）",
            $"{Text(p, "WindowsText")} | {Text(p, "PlaceText")} | {Text(p, "ConfirmText")}");

        // ---- multi-window ----
        Click(p, "FreeButton"); Click(p, "OwnedButton"); Click(p, "OwnedButton"); Settle();
        Harness.Check("WindowHowTo: Show() windows have no owner, Show(owner) windows are tracked",
            Text(p, "WindowsText") == "独立窗口 1 个，子窗口 2 个；所有者的 OwnedWindows 共 2 个（含下面两块开的窗口）" && w.OwnedWindows.Count == 2, $"{Text(p, "WindowsText")} / {w.OwnedWindows.Count}");
        Click(p, "CloseAllButton"); Settle();
        Harness.Check("WindowHowTo: close-all empties both lists", Text(p, "WindowsText") == "独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）" && w.OwnedWindows.Count == 0, Text(p, "WindowsText"));

        // ---- position and size ----
        Click(p, "RestoreButton");
        Harness.Check("WindowHowTo: restoring with no window open does nothing", p.PlaceWindow is null);
        Click(p, "PlaceOpenButton"); Settle();
        var place = p.PlaceWindow!;
        var originalPosition = place.Position;
        double originalWidth = place.Width, originalHeight = place.Height;
        Harness.Check("WindowHowTo: the sample opens as a child with its position in the readout", place.IsVisible && w.OwnedWindows.Contains(place) && Text(p, "PlaceText").StartsWith($"示例窗口：Position = {originalPosition}"), Text(p, "PlaceText"));

        Click(p, "SaveButton"); Settle();
        Harness.Check("WindowHowTo: Save remembers the current place", Text(p, "PlaceText").Contains($"已记下：Position = {originalPosition}"), Text(p, "PlaceText"));
        Click(p, "MoveButton"); Settle();
        Harness.Check("WindowHowTo: Move changes position and size", place.Position == new PixelPoint(200, 120) && place.Width == 420 && place.Height == 260, $"{place.Position}/{place.Width}x{place.Height}");
        Click(p, "RestoreButton"); Settle();
        Harness.Check("WindowHowTo: Restore brings back the remembered place", place.Position == originalPosition && place.Width == originalWidth && place.Height == originalHeight, $"{place.Position}/{place.Width}x{place.Height}");
        Click(p, "PlaceCloseButton"); Settle();
        Harness.Check("WindowHowTo: closing the sample clears it but keeps what was remembered", p.PlaceWindow is null && Text(p, "PlaceText").StartsWith("示例窗口：未打开；已记下：Position = "), Text(p, "PlaceText"));

        // ---- confirm before closing ----
        Click(p, "EditorButton"); Settle();
        var editor = p.Editor!;
        Harness.Check("WindowHowTo: the editor opens clean", editor.IsVisible && Text(p, "ConfirmText") == "窗口没有未保存的更改；Closing 被拦下 0 次；最近一次：（还没触发）", Text(p, "ConfirmText"));
        editor.Close(); Settle();
        Harness.Check("WindowHowTo: a clean editor closes without asking", p.Editor is null && !editor.IsVisible && w.OwnedWindows.Count == 0, $"{p.Editor is null}/{editor.IsVisible}");

        Click(p, "EditorButton"); Settle();
        editor = p.Editor!;
        Harness.Find<TextBox>(editor, "EditorBox").Text = "改了"; Settle();
        Harness.Check("WindowHowTo: typing marks the editor dirty, both inside and on the page",
            Text(editor, "EditorState") == "有未保存的更改" && Text(p, "ConfirmText").StartsWith("窗口有未保存的更改；Closing 被拦下 0 次"), $"{Text(editor, "EditorState")} | {Text(p, "ConfirmText")}");

        editor.Close(); Settle();
        Harness.Check("WindowHowTo: closing a dirty editor is held back and asks first",
            editor.IsVisible && editor.OwnedWindows.Count == 1 && editor.OwnedWindows[0].Title == "放弃更改？" && Text(p, "ConfirmText").Contains("被拦下 1 次"), $"{editor.IsVisible}/{editor.OwnedWindows.Count}");
        Click(editor.OwnedWindows[0], "CancelButton"); Settle();
        Harness.Check("WindowHowTo: Cancel on the question keeps the editor", editor.IsVisible && editor.OwnedWindows.Count == 0 && Text(p, "ConfirmText").EndsWith("最近一次：用户选了「取消」，窗口留着"), Text(p, "ConfirmText"));

        editor.Close(); Settle();
        editor.OwnedWindows.Single().Close(); Settle();
        Harness.Check("WindowHowTo: closing the question itself also keeps the editor", editor.IsVisible && Text(p, "ConfirmText").Contains("被拦下 2 次") && Text(p, "ConfirmText").EndsWith("最近一次：用户直接关掉了确认框，窗口留着"), Text(p, "ConfirmText"));

        Click(editor, "EditorSaveButton"); Settle();
        Harness.Check("WindowHowTo: Save clears the dirty mark", Text(editor, "EditorState") == "没有未保存的更改" && Text(p, "ConfirmText").StartsWith("窗口没有未保存的更改"), $"{Text(editor, "EditorState")} | {Text(p, "ConfirmText")}");
        Harness.Find<TextBox>(editor, "EditorBox").Text = "又改了"; Settle();

        editor.Close(); Settle();
        Click(editor.OwnedWindows.Single(), "OkButton"); Settle();
        Harness.Check("WindowHowTo: OK on the question lets the editor close", p.Editor is null && !editor.IsVisible && w.OwnedWindows.Count == 0
            && Text(p, "ConfirmText").StartsWith("窗口未打开") && Text(p, "ConfirmText").EndsWith("最近一次：用户选了「确定」，窗口随后关闭"), Text(p, "ConfirmText"));

        // ---- leaving the page ----
        Click(p, "FreeButton"); Click(p, "OwnedButton"); Click(p, "PlaceOpenButton"); Click(p, "EditorButton"); Settle();
        Harness.Find<TextBox>(p.Editor!, "EditorBox").Text = "没保存"; Settle();
        var open = new[] { p.PlaceWindow!, p.Editor! };
        w.Content = null; Settle();
        Harness.Check("WindowHowTo: leaving the page closes every window, even a dirty editor", p.PlaceWindow is null && p.Editor is null && open.All(o => !o.IsVisible) && w.OwnedWindows.Count == 0,
            $"{p.PlaceWindow is null}/{p.Editor is null}/{w.OwnedWindows.Count}");
        w.Close();
    }

    static void Persistence()
    {
        string root = Path.Combine(Path.GetTempPath(), "cd-probe-" + Guid.NewGuid().ToString("N"));
        string settings = Path.Combine(root, "settings"), recent = Path.Combine(root, "recent");
        try
        {
            var vm = new DataPersistenceHowToViewModel(settings, recent);
            Harness.Check("Persist: building the view model writes nothing", !Directory.Exists(settings) && !Directory.Exists(recent) && !File.Exists(vm.SettingsPath));
            Harness.Check("Persist: defaults and the first status", vm.Username == "" && vm.FontSize == 14 && !vm.NotifyEnabled && vm.SaveCount == 0
                && vm.StatusText == "还没有设置文件，使用默认值" && vm.FileText == "（文件不存在）", $"{vm.StatusText}/{vm.FileText}");
            Harness.Check("Persist: the paths point into the folders given", vm.SettingsPath == Path.Combine(settings, "settings.json") && vm.RecentPath == Path.Combine(recent, "recent.json"));

            vm.Username = "小明";
            Harness.Check("Persist: the first change creates the file", File.Exists(vm.SettingsPath) && vm.SaveCount == 1 && vm.StatusText == "已自动保存（第 1 次）", $"{vm.SaveCount}/{vm.StatusText}");
            Harness.Check("Persist: Chinese is written as-is, not as \\u escapes", File.ReadAllText(vm.SettingsPath).Contains("小明") && vm.FileText.Contains("小明"), vm.FileText);
            vm.FontSize = 20; vm.NotifyEnabled = true;
            Harness.Check("Persist: each setting change saves once", vm.SaveCount == 3 && vm.StatusText == "已自动保存（第 3 次）", $"{vm.SaveCount}/{vm.StatusText}");
            Harness.Check("Persist: no .tmp file is left behind", Directory.GetFiles(settings, "*.tmp").Length == 0);
            Harness.Check("Persist: the file text is what is on disk", vm.FileText == File.ReadAllText(vm.SettingsPath));

            var second = new DataPersistenceHowToViewModel(settings, recent);
            Harness.Check("Persist: a second view model reads the same values back without saving",
                second.Username == "小明" && second.FontSize == 20 && second.NotifyEnabled && second.SaveCount == 0 && second.StatusText == "已从文件读到上次的设置", $"{second.Username}/{second.FontSize}/{second.StatusText}");

            vm.ResetCommand.Execute(null);
            Harness.Check("Persist: Reset restores the defaults and saves once", vm.Username == "" && vm.FontSize == 14 && !vm.NotifyEnabled && vm.SaveCount == 4 && vm.StatusText == "已恢复默认值并保存", $"{vm.SaveCount}/{vm.StatusText}");

            vm.Username = "改过";
            vm.CorruptFileCommand.Execute(null);
            Harness.Check("Persist: Corrupt writes garbage the page can show", vm.FileText == "{ this is not json" && vm.StatusText == "已把设置文件写坏，点「重新读取」看看会怎样", vm.FileText);
            vm.ReloadCommand.Execute(null);
            Harness.Check("Persist: Reload on a broken file falls back to defaults instead of crashing",
                vm.Username == "" && vm.FontSize == 14 && !vm.NotifyEnabled && vm.StatusText == "设置文件损坏，已回退到默认值", $"{vm.Username}/{vm.StatusText}");
            var third = new DataPersistenceHowToViewModel(settings, recent);
            Harness.Check("Persist: a fresh view model over the broken file starts with defaults instead of crashing",
                third.Username == "" && third.StatusText == "设置文件损坏，已回退到默认值", third.StatusText);

            vm.DeleteFileCommand.Execute(null);
            Harness.Check("Persist: Delete removes the file but leaves the values on screen", !File.Exists(vm.SettingsPath) && vm.FileText == "（文件不存在）" && vm.StatusText.StartsWith("设置文件已删除"), vm.StatusText);
            Harness.Check("Persist: a view model over a missing file uses the defaults", new DataPersistenceHowToViewModel(settings, recent).StatusText == "还没有设置文件，使用默认值");

            File.WriteAllText(vm.SettingsPath, "{\"Username\":\"旧\"}");
            vm.ReloadCommand.Execute(null);
            Harness.Check("Persist: an older file with missing fields keeps the defaults for them", vm.Username == "旧" && vm.FontSize == 14 && !vm.NotifyEnabled && vm.StatusText == "已从文件重新读取", $"{vm.Username}/{vm.FontSize}/{vm.StatusText}");
            Harness.Check("Persist: reading never counts as saving", vm.SaveCount == 5, vm.SaveCount);

            // ---- recent files, through the view model ----
            Harness.Check("Persist: the recent list starts empty and nothing is written", vm.RecentItems.Count == 0 && !File.Exists(vm.RecentPath));
            for (int i = 0; i < 12; i++) vm.AddRecentCommand.Execute(null);
            Harness.Check("Persist: the recent list is capped at 10, newest first",
                vm.RecentItems.Count == 10 && Path.GetFileName(vm.RecentItems[0]) == "文档 12.txt" && Path.GetFileName(vm.RecentItems[9]) == "文档 3.txt", $"{vm.RecentItems.Count}/{Path.GetFileName(vm.RecentItems[0])}/{Path.GetFileName(vm.RecentItems[^1])}");
            Harness.Check("Persist: the recent list is on disk in readable form", File.Exists(vm.RecentPath) && File.ReadAllText(vm.RecentPath).Contains("文档 12.txt"));
            var reread = new RecentFiles(recent);
            Harness.Check("Persist: a new RecentFiles reads the same order back", reread.Items.SequenceEqual(vm.RecentItems), reread.Items.Count);
            vm.ClearRecentCommand.Execute(null);
            Harness.Check("Persist: Clear empties the list and the file", vm.RecentItems.Count == 0 && File.ReadAllText(vm.RecentPath).Trim() == "[]" && new RecentFiles(recent).Items.Count == 0);

            // ---- recent files, directly ----
            string direct = Path.Combine(root, "direct");
            var list = new RecentFiles(direct);
            list.Add("C:/a"); list.Add("C:/b"); list.Add("c:/A");
            Harness.Check("Persist: adding a path again, whatever the case, moves it to the top once", list.Items.SequenceEqual(new[] { "c:/A", "C:/b" }), string.Join(",", list.Items));
            Harness.Check("Persist: Capacity is 10", RecentFiles.Capacity == 10);
            File.WriteAllText(list.FilePath, "not json");
            Harness.Check("Persist: an unreadable recent file is just an empty list", new RecentFiles(direct).Items.Count == 0);

            Harness.Check("Persist: the default folders sit under different special folders", SettingsStore.DefaultDirectory != RecentFiles.DefaultDirectory
                && SettingsStore.DefaultDirectory.EndsWith("Avalonia.ControlsDemo") && RecentFiles.DefaultDirectory.EndsWith("Avalonia.ControlsDemo"), $"{SettingsStore.DefaultDirectory} | {RecentFiles.DefaultDirectory}");

            PersistencePage(root);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    static void PersistencePage(string root)
    {
        var vm = new DataPersistenceHowToViewModel(Path.Combine(root, "page-settings"), Path.Combine(root, "page-recent"));
        var p = new DataPersistenceHowToPage { DataContext = vm };
        var w = Harness.Show(p);

        var texts = p.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Harness.Check("PersistPage: the status line and both paths are bound", texts.Contains(vm.StatusText) && texts.Contains($"文件：{vm.SettingsPath}") && texts.Contains($"文件：{vm.RecentPath}"), vm.StatusText);

        p.GetVisualDescendants().OfType<TextBox>().First().Text = "页面输入"; Settle();
        Harness.Check("PersistPage: the user-name box writes the view model and the file", vm.Username == "页面输入" && File.Exists(vm.SettingsPath) && vm.SaveCount == 1, $"{vm.Username}/{vm.SaveCount}");
        p.GetVisualDescendants().OfType<Slider>().Single().Value = 22; Settle();
        Harness.Check("PersistPage: the slider writes FontSize", vm.FontSize == 22 && vm.SaveCount == 2, $"{vm.FontSize}/{vm.SaveCount}");
        p.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked = true; Settle();
        Harness.Check("PersistPage: the check box writes NotifyEnabled", vm.NotifyEnabled && vm.SaveCount == 3, $"{vm.NotifyEnabled}/{vm.SaveCount}");
        Harness.Check("PersistPage: the file box shows the real file", p.GetVisualDescendants().OfType<SelectableTextBlock>().Any(t => t.Text == File.ReadAllText(vm.SettingsPath) && t.Text!.Contains("页面输入")));
        Harness.Check("PersistPage: the preview uses the typed name", p.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "预览：你好，页面输入" && t.FontSize == 22));

        vm.AddRecentCommand.Execute(null); Settle();
        Harness.Check("PersistPage: the recent list box follows the collection", p.GetVisualDescendants().OfType<ListBox>().Single().ItemCount == 1 && p.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "共 1 条，上限 10"));
        w.Close();
    }
}
```

- [ ] **Step 3: 接入探针入口并运行**

在 `C:\Temp\probe-controls\Program.cs` 里，`ProbeNav.Run();` 之后加一行 `ProbePrim.Run();`。

Run: `cd C:\Temp\probe-controls && dotnet run 2>&1 | grep -E "FAIL|Unhandled|   at |warning log|failed|passed"`
Expected: 末行 `873 passed, 0 failed, 0 warning log(s)`（累计数，含第 00–05 份；本份新增 175 条）。出现 `FAIL` 时先判断是页面缺陷还是探针写错，改对应的代码块再重新抽取，不要放宽断言。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|个错误|Build succeeded"`
Expected: 0 个错误，且相比构建前没有新增警告（`Avalonia.ControlsDemo` 早在第 00 份就已加入 `hello-avalonia.slnx`）。

- [ ] **Step 5: 人工目视（headless 验证不了的部分）**

`dotnet run --project Avalonia.ControlsDemo`，在导航树依次点开下列页面：

- **ScrollBar**：六个按钮的数值变化与 `Scroll` 计数；滑块拖动只改值不加计数；Visibility 选 Hidden 滚动条消失
- **ThemeVariantScope**：并排的两块明显一浅一深；下拉切换时第三块跟着变
- **UserControl**：四块色块，改色与改标题只影响最右一块
- **Window**：打开示例窗口后改各复选框，看标题栏按钮变灰、任务栏图标消失、置顶生效；勾上"拦截关闭"后点示例窗口的 ✕ 关不掉
- **WindowDrawnDecorations**：Full / BorderOnly / None 三档的边框与标题栏差别；"打开自绘标题栏窗口"的蓝色标题栏能拖动窗口，三个按钮能最小化、最大化、关闭（**这一页 headless 画不出来，只能在这里看**）
- **TrayIcon**：任务栏右下角出现图标（可能折叠在"显示隐藏的图标"里）；右键菜单有四项，「开机提示」带勾；切走页面后图标消失
- **实战：Window**：独立窗口与子窗口的区别（最小化主窗口，子窗口跟着收起）；移动、记下、恢复；可编辑窗口里改字后点 ✕ 会弹确认
- **实战：Dialogs**：确认框的 true / false / null 三种读数；"必答的问题"点 ✕ 关不掉；选颜色后预览块变色；本机若支持文件选择器，"打开文件…"可点（与 headless 不同）
- **实战：数据持久化**：改用户名后 `%AppData%\Avalonia.ControlsDemo\settings.json` 出现且内容是可读的中文；"写坏文件"再"重新读取"回退默认值；"假装打开"点 12 次只剩 10 条；`%LocalAppData%\Avalonia.ControlsDemo\recent.json` 出现

目视完毕后，删掉这两个演示目录里的文件（它们是演示产物，不是项目的一部分）。

- [ ] **Step 6: 回填 spec 与 CLAUDE.md**

在 spec 末尾"批 4 实测结论"之后、"验证标准"之前追加下面这一节（`验证标准` 一节的上方）：

```markdown
### 批 5 实测结论（2026-10-11）

- **headless 画不出窗口装饰**：`WindowDecorations` 取 `None / BorderOnly / Full` 三档、`ExtendClientAreaToDecorationsHint` 开关，`IsExtendedIntoWindowDecorations` 都恒为 `False`，`WindowDecorationMargin` 恒为 0；`WindowDrawnDecorations` 没有任何公开属性能拿到它，只能经主题与 `Window.WindowDecorationsTheme` 触达。页面演示属性读回与 `ElementRole`，外观要人工目视。
- **`Window` 的关闭行为**：代码里调 `Close()` 的 `CloseReason` 也是 `WindowClosing`，拦截只应拦这一种，放过 `OwnerWindowClosing`，否则示例窗口会拖住主窗口；`Closed` 触发时窗口仍在所有者的 `OwnedWindows` 里，读数要数 `IsVisible` 的。`async void` 的 `Closing` 处理器必须在第一个 `await` 前同步设 `e.Cancel = true`。
- **`ShowDialog<T>` 的结果**：`Close(true)` 得 `true`，无参 `Close()`（含点 ✕）得 `null`，不是 `false`；headless 里所有者的 `IsEnabled` 仍是 `True`，不能断言"所有者被挡住"。
- **`TrayIcon`**：`NativeMenuExporter` 在 headless 里为 `null`，页面如实显示；`TrayIcon.Clicked` 在 headless 里触发不了，菜单项点击走 `Command.Execute`，且 `Command.Execute` 不会改 `IsChecked`（勾选是平台导出层的事）。图标挂在 `Application` 上，页面离开时必须摘掉并还原之前的 `TrayIcons`。
- **`StorageProvider`**：非空，但 `CanOpen`、`CanSave`、`CanPickFolder` 在 headless 里全是 `False`，文件按钮按能力启用。
- **`ScrollBar`**：`LineDown` 等方法触发 `Scroll`，直接给 `Value` 赋值不触发。
- **`ThemeVariantScope`**：`ActualThemeVariantChanged` 触发时 `RequestedThemeVariant` 可能还没写进去，读数要在设值之后再刷新一次。
- **JSON 持久化**：`System.Text.Json` 默认把中文转成 `\uXXXX`，设置文件要用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` 才可读；自动保存会覆盖刚写坏的文件，探针里"写坏再读"的顺序要先改值、后写坏。
- **范围缺口**：官方 Window 指南的"透明窗口"与"窗口图标"、Dialogs 指南的"文件对话框"实际弹出、数据持久化指南的"书签存储"，本份不做（原因见第 06 份计划开头）。
- **官方 How-to 与 12.1.2 的出入**：无。
- **探针断言实际条数**：本批累计 873 条（含第 00–05 份），全部通过且零警告日志。
```

然后把 `CLAUDE.md`「进行中的工作」一节更新为"第 00–06 份已写，第 00–04 份已执行；第 07 份待写"的现状（以执行时仓库的实际状态为准）。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs CLAUDE.md
git commit -m "feat: complete the Primitives and System categories with 6 control pages and 3 how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec 覆盖：**

| spec 要求 | 落点 |
|---|---|
| Primitives 5 页（ScrollBar、ThemeVariantScope、UserControl、Window、WindowDrawnDecorations） | Task 1（3 页）、Task 2（2 页）、Task 5 Step 1 登记 |
| System 1 页（TrayIcon，按官方侧边栏归 System、DocPath 保留 `controls/navigation/trayicon`） | Task 4 Step 1、Task 5 Step 1 |
| How-to：window-how-to（紧跟 Window） | Task 3 Step 3 |
| How-to：dialogs-how-to（紧随 window-how-to） | Task 3 Steps 1–2，探针 `Follows` 断言 |
| How-to：data-persistence-how-to 做成实战页放 System、紧跟 TrayIcon | Task 4 Steps 2–4 |
| ThemeVariantScope 页末"更深入的演示：StylingDemo →『主题变体』" | Task 1 Step 2 的 hint |
| 原生控件按"先检测、再使用"写（风险 2） | TrayIcon 检测 `NativeMenuExporter`；Dialogs 检测 `StorageProvider.CanOpen`；探针只断言检测逻辑，外观走 Step 5 目视 |
| 探针读回属性值、零警告日志 | Task 5 Step 2–3（873 条，0 警告日志） |
| 探针仓库外、不提交 | Step 2 |

**2. 占位符扫描：** 无 TBD / TODO；每个代码步骤都有完整代码块；探针的断言数以实际运行结果填写（873 / 175）。

**3. 类型一致性：** `WindowPage.Sample` / `ClosingCount`、`WindowDrawnDecorationsPage.Sample`、`TrayIconPage.Tray`、`WindowHowToPage.PlaceWindow` / `Editor` 在 Task 2–4 定义，探针读取时名字一致；`IDialogService` 三个方法与 `FakeDialogs`、`WindowDialogService` 一致；`DemoDialogs` 的按钮 `Name`（`OkButton`、`CancelButton`、`Swatch_*`、`AnswerBox`）与探针一致；`SettingsStore` / `RecentFiles` 的 `DefaultDirectory` 与探针的"真实文件未被碰"快照一致。

**4. 已知遗留（不在本份处理）：**
- 第 05、06 份两份计划的执行顺序：先 05、后 06（两份互不依赖对方的页面，但都往 `PageCatalog` 的分类文件里填登记行，按顺序执行最不容易冲突）。
- Web 与付费控件的路标页、9 篇横切 How-to 路标、README 的"控件目录对照表"收尾，留给第 07 份。

<!-- PLAN-END -->
