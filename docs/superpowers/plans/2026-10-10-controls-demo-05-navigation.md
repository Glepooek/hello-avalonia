# ControlsDemo 05：Navigation 8 个控件页与 TabControl、Navigation 两篇实战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全 `Avalonia.ControlsDemo` 的 Navigation 分类：8 个 Control 页（CarouselPage、CommandBar、ContentPage、DrawerPage、NavigationPage、TabbedPage、TabControl、TabStrip）加 TabControl、Navigation 两篇 How-to 实战页，共 10 页。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份新增页面文件、一个页面样例辅助类 `DemoPages`、两个实战页 ViewModel、`PageCatalog.Navigation.cs` 的登记行，以及探针 `Probe.Nav.cs` / `Probe.Nav2.cs`。不引入新包。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00–04 份已执行（`PageCatalog`、`Categories.Navigation`、`Harness` 已存在；`AddNavigation` 目前是空方法）。

> **编号说明：** 第 00 份开头那张"9 份计划"表在编写第 04 份时已经合并过（04 实际包含 Feedback + Media + Menus），表里 04–08 的文件名与分工已作废。**以 `docs/superpowers/plans/` 下实际存在的文件为准**：本份是 05（Navigation），06 是 Primitives + System，07 是 Web / Premium / How-to 路标。第 00、04 份正文里"第 06 份做 Premium 路标页"的说法，实际指第 07 份。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」，**不重复贴**，执行者必须先读第 00 份；第 01–04 份「页面的写法约定」同样适用。本份额外强调：

- 页面类名：控件名本身以 `Page` 结尾的写成 `<控件名>DemoPage`（`CarouselDemoPage`、`ContentDemoPage`、`DrawerDemoPage`、`NavigationDemoPage`、`TabbedDemoPage`），其余 `<控件>Page`（`CommandBarPage`、`TabControlPage`、`TabStripPage`）；How-to 页 `<主题>HowToPage`。命名空间一律 `Avalonia.ControlsDemo.Views.Pages`，ViewModel 一律 `Avalonia.ControlsDemo.ViewModels`；页面目录 `Views/Pages/Navigation/`
- `DocPath` 已用官方 `sitemap.xml` 核对：控件页 `controls/navigation/<控件>`，How-to 页 `docs/how-to/tabcontrol-how-to`、`docs/how-to/navigation-how-to`
- **事件接线放在构造函数里、`InitializeComponent()` 之后**；枚举下拉先设 `SelectedItem` 再订阅 `SelectionChanged`
- 绝不在元素上写属性的默认值（硬性规则 1）；运行时由用户操作触发的代码赋值不受此限
- 事件读数一律用**计数**（`Pushed 2 次`），不读事件参数里的类型，避免依赖未核实的 `EventArgs` 成员
- 页面样例（`ContentPage`）一律用 `DemoPages.Make` 在代码里造，不要为每个样例页单独建 `.axaml`
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时在 Avalonia 12.1.2 headless 实测）

| 控件 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `NavigationPage` | 根页放进 `Content` 后 `StackDepth = 1`、`CanGoBack = False`；`PushAsync` 后 `StackDepth = 2`、`CanGoBack = True`、`Pushed` 触发 1 次；`PopAsync` 返回弹出的页、`Popped` 触发 1 次；`PopToRootAsync` 回到深度 1；**在根页上 `PopAsync` 返回 `null` 且深度不变**；**`PageTransition` 与 `ModalTransition` 构造后读是 `null`，但窗口显示后主题样式把两者都设成 `PageSlide`**；`IsBackButtonVisible` 默认 `True` | 页面在 `Loaded` 时按真实值同步过渡下拉；根页上的 Pop 不需要额外保护；**headless 里带过渡的 `PushAsync` 一直等动画，探针必须先选"无"** |
| `NavigationPage` 模态 | `PushModalAsync` 后 `ModalStack.Count = 1`，**不计入 `StackDepth`**；**只有 `PageTransition` 与 `ModalTransition` 都是 `null` 时，headless 里 `PushModalAsync` 才会完成**（带过渡的任务一直不完成，但栈已变） | 过渡下拉同时改 `PageTransition` 与 `ModalTransition`；探针先选"无" |
| `DrawerPage` | 默认 `IsOpen=False`、`DrawerLength=320`、`DrawerBehavior=Auto`、`DrawerPlacement=Left`、`DrawerLayoutBehavior=Overlay`、`DisplayMode=Overlay`；`IsOpen=true` 触发 `Opened`、`false` 触发 `Closed`；**切到 `DrawerBehavior=Locked` 时抽屉被打开（`IsOpen=True`）且 `DisplayMode` 变成 `Inline`；之后代码把 `IsOpen` 设回 `false` 仍然生效，换回 `Auto` 时 `IsOpen` 保持当时的值、`DisplayMode` 回到 `Overlay`**；再改 `DrawerLayoutBehavior=Split` 不变 | 页面在每次改动后重读 `IsOpen` 与 `DisplayMode`，不假定复选框就是真值 |
| `TabbedPage` | `Pages` 用代码赋值（`IEnumerable<Page>`）；`SelectedIndex` 默认 0，`SelectedPage` 同步；**但 `SelectedPage` 比 `SelectedIndex` 晚一步更新**（`SelectedIndex` 的变更通知到来时 `SelectedPage` 还是旧页，首次挂载时甚至为空），读 `SelectedPage` 要监听 `SelectedPageProperty`；`TabPlacement` 默认 `Auto`；`IsGestureEnabled` 默认 `False`、`IsKeyboardNavigationEnabled` 默认 `True` | 页面在构造函数里 `Demo.Pages = new Page[] {...}`，XAML 里写不了 |
| `CarouselPage` | `Pages` 同上；`SelectedIndex` 默认 0；`PageTransition` 默认 `null`；赋 `SelectedIndex = 1` 立即生效 | 同上；过渡下拉默认"无" |
| `CommandBar` | `IsDynamicOverflowEnabled` **默认 `False`**，此时再窄也**不会**溢出（90 宽时 0 个溢出）；打开后 500 宽 0 个、200 宽 1 个、90 宽 2 个主命令进入溢出（`ICommandBarElement.IsInOverflow`）；`DefaultLabelPosition` 默认 `Bottom`，改成 `Right` 后每个按钮的 `LabelPosition` 跟着变；`IsOpen` 可写；`OverflowButtonVisibility` 默认 `Auto`、`IsSticky` 默认 `False`；主命令按钮的 `Click` 能冒泡到 `CommandBar`，`e.Source` 就是那个 `CommandBarButton` | 溢出读数要先勾 `IsDynamicOverflowEnabled`，并在布局后读（`Dispatcher.Post` 到 `Loaded`） |
| `ContentPage` | `HorizontalContentAlignment` / `VerticalContentAlignment` 默认 `Stretch`；`TopCommandBar` / `BottomCommandBar` 是 `object`，可直接放 `CommandBar` | 页面从 `Demo.BottomCommandBar` 取出备份，用复选框切换 |
| `TabControl` | `TabStripPlacement` 的类型是 `Dock`，默认 `Top`；**改 `TabStripPlacement` 会换模板并把 `SelectedIndex` 重置为 0**（即使之前选的是 2），等布局完成后再赋值才能恢复；代码可以选中 `IsEnabled=False` 的 `TabItem` | 页面改位置前记下选中项，用 `Dispatcher.Post(..., Loaded)` 恢复；How-to 的 `SelectedItem` 双向绑定同样会被重置，也要恢复 |
| `TabStrip` | `TabStrip : SelectingItemsControl`（在 `Avalonia.Controls.Primitives`），只有标签条、没有内容区；`SelectedIndex` 默认 0，可写 | 页面自己用 `SelectionChanged` 驱动一块内容 |

**命名碰撞**：`Views.Pages` 里已有第 03 份的 `CarouselPage`（Carousel 控件的页面），与 `Avalonia.Controls.CarouselPage` 同名。本份的 `CarouselDemoPage` 因此不受影响（XAML 与生成的字段都用完整类型），但探针同时 `using` 两个命名空间时会 `CS0104`，要写 `Avalonia.Controls.CarouselPage`。

**成员核实（反射，Avalonia 12.1.2）**：`MultiPage` 有 `Pages`（`IEnumerable<Page>`）、`ItemsSource`、`PageTemplate`；`SelectingMultiPage` 加 `SelectedIndex`、`SelectedPage`；`NavigationPage` 有 `PushAsync(Page)` / `PushAsync(Page, IPageTransition)`、`PopAsync()`（返回 `Task<Page?>`）、`PopToRootAsync()`、`PushModalAsync(Page)`、`PopModalAsync()`、`StackDepth`、`CanGoBack`、`ModalStack`、`PageTransition`、`ModalTransition`、事件 `Pushed` / `Popped` / `ModalPushed` / `ModalPopped`；`DrawerPage` 的 `Drawer`、`Content` 是 `object`，`DrawerBehavior`（`Auto / Flyout / Locked / Disabled`）、`DrawerPlacement`（`Left / Right / Top / Bottom`）、`DrawerLayoutBehavior`（`Overlay / Split / CompactOverlay / CompactInline`）；`TabPlacement`（`Auto / Top / Bottom / Left / Right`）；`CommandBarDefaultLabelPosition`（`Bottom / Right / Collapsed`）；`CommandBar.PrimaryCommands` / `SecondaryCommands` 是 `IList`；`TabItem` 有 `Icon`、`IsSelected`、`TabStripPlacement`；`TabControl` 有 `ContentTemplate`、`PageTransition`、`SelectedContent`、`TabStripPlacement`。

**与官方 How-to 的出入**：官方 TabControl 指南最后一节讲用 `Styles` 改标签外观；本份实战页不重复，样式选择器已在 `Avalonia.StylingDemo` 里演示（页面末尾加一行指向）。官方 Navigation 指南的 "View locator" 与 "Sidebar navigation" 两节，本项目的主窗口就是实例（`MainWindow` 的 `SplitView` + `ContentControl`），实战页只演示"视图切换 + 过渡 + 前进后退栈"三节。

**范围缺口（已知，不在本份处理）**：官方 `controls/navigation/` 下除上述 8 页外还有 `trayicon`，按 spec 归 System 分类，由第 06 份处理。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Navigation/
│   └── PageCatalog.Navigation.cs           改：+8 控件页 +2 How-to 页
├── ViewModels/
│   ├── DocumentTab.cs                      新：TabControl 实战页的一个标签
│   ├── WizardStep.cs                       新：向导的一步（内容懒创建）
│   ├── TabControlHowToViewModel.cs         新：动态标签、关闭、向导与懒加载
│   ├── NavPageViewModels.cs                新：Navigation 实战页的三个页面 ViewModel
│   └── NavigationHowToViewModel.cs         新：当前页、后退栈、前进栈
└── Views/Pages/Navigation/
    ├── DemoPages.cs                        新：造样例 ContentPage 的小工具
    ├── CommandBarPage / ContentDemoPage / NavigationDemoPage / DrawerDemoPage
    ├── TabbedDemoPage / CarouselDemoPage / TabControlPage / TabStripPage
    └── TabControlHowToPage / NavigationHowToPage
```

每个页面含 `.axaml` 与 `.axaml.cs`。探针（仓库外）：`C:\Temp\probe-controls\Probe.Nav.cs`、`Probe.Nav2.cs`。

### 页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 页面骨架（`UserControl` + `ScrollViewer` + `StackPanel Margin="12"` + `DemoHeader`）与第 00 份相同；演示块用 `TextBlock.caption` + `Border.stage` + `TextBlock.hint`。
- 枚举下拉一律在 code-behind 构造函数里 `ItemsSource = Enum.GetValues<T>()`。

### Task 1: DemoPages、CommandBar、ContentPage

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/DemoPages.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/CommandBarPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/ContentDemoPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份的 `DemoHeader`、`SharedStyles` 里的 `caption` / `stage` / `hint`
- Produces: `DemoPages.Make(string header, string body) : ContentPage`，Task 2、3 的页面都用它造样例页

- [ ] **Step 1: 样例页辅助类**

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/DemoPages.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>Builds the small sample pages that the navigation demos switch between.</summary>
    internal static class DemoPages
    {
        public static ContentPage Make(string header, string body)
        {
            return new ContentPage
            {
                Header = header,
                Content = new TextBlock
                {
                    Text = body,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }
    }
}
```

- [ ] **Step 2: CommandBar**

两块演示：上块看 `PrimaryCommands` / `SecondaryCommands`、标签位置与 `Click` 冒泡；下块看动态溢出（先勾 `IsDynamicOverflowEnabled`，再拖窄）。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/CommandBarPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CommandBarPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="CommandBar：命令栏与动态溢出"
                               DocPath="controls/navigation/commandbar" />

            <TextBlock Classes="caption" Text="PrimaryCommands、SecondaryCommands 与标签位置" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="DefaultLabelPosition" />
                        <ComboBox Name="LabelBox" Width="120" />
                    </StackPanel>
                    <Border HorizontalAlignment="Left" BorderBrush="Gray" BorderThickness="1">
                        <CommandBar Name="Bar">
                            <CommandBar.PrimaryCommands>
                                <CommandBarButton Name="NewButton" Label="新建">
                                    <CommandBarButton.Icon>
                                        <PathIcon Data="M10,4 H14 V10 H20 V14 H14 V20 H10 V14 H4 V10 H10 Z" />
                                    </CommandBarButton.Icon>
                                </CommandBarButton>
                                <CommandBarToggleButton Name="BoldButton" Label="加粗" />
                                <CommandBarButton Name="SaveButton" Label="保存" />
                            </CommandBar.PrimaryCommands>
                            <CommandBar.SecondaryCommands>
                                <CommandBarButton Name="ExportButton" Label="导出…" />
                            </CommandBar.SecondaryCommands>
                        </CommandBar>
                    </Border>
                    <TextBlock Name="ClickText" Text="最近点击：（无）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="主命令直接显示在栏上，次要命令收在右侧的「…」菜单里。每个按钮的 Click 都会冒泡到 CommandBar，页面在 Bar 上用一个处理器接住。DefaultLabelPosition 改成 Right 后标签挪到图标右边，Collapsed 则只剩图标。" />

            <TextBlock Classes="caption" Text="IsDynamicOverflowEnabled：栏太窄时把主命令挪进溢出菜单" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="OverflowCheck" Content="IsDynamicOverflowEnabled" />
                        <TextBlock VerticalAlignment="Center" Text="宽度" />
                        <Slider Name="WidthSlider" Width="200" Minimum="80" Maximum="520" Value="520" />
                    </StackPanel>
                    <Border Name="OverflowHost" Width="520" HorizontalAlignment="Left" BorderBrush="Gray" BorderThickness="1">
                        <CommandBar Name="OverflowBar">
                            <CommandBar.PrimaryCommands>
                                <CommandBarButton Label="新建" />
                                <CommandBarButton Label="打开" />
                                <CommandBarButton Label="保存" />
                                <CommandBarButton Label="导出" />
                            </CommandBar.PrimaryCommands>
                        </CommandBar>
                    </Border>
                    <TextBlock Name="OverflowText" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="IsDynamicOverflowEnabled 默认是 False，这时栏再窄也不会溢出，命令只会被裁掉。勾上之后，拖窄宽度，排在后面的主命令会依次进入溢出菜单（ICommandBarElement.IsInOverflow 变成 True）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/CommandBarPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CommandBarPage : UserControl
    {
        public CommandBarPage()
        {
            InitializeComponent();

            LabelBox.ItemsSource = Enum.GetValues<CommandBarDefaultLabelPosition>();
            LabelBox.SelectedItem = Bar.DefaultLabelPosition;
            LabelBox.SelectionChanged += (_, _) =>
            {
                if (LabelBox.SelectedItem is CommandBarDefaultLabelPosition position)
                {
                    Bar.DefaultLabelPosition = position;
                }
            };

            Bar.AddHandler(Button.ClickEvent, OnBarClick);

            OverflowCheck.IsChecked = OverflowBar.IsDynamicOverflowEnabled;
            OverflowCheck.IsCheckedChanged += (_, _) =>
            {
                OverflowBar.IsDynamicOverflowEnabled = OverflowCheck.IsChecked == true;
                RefreshOverflow();
            };
            WidthSlider.ValueChanged += (_, e) =>
            {
                OverflowHost.Width = e.NewValue;
                RefreshOverflow();
            };
            RefreshOverflow();
        }

        private void OnBarClick(object? sender, RoutedEventArgs e)
        {
            string? label = (e.Source as CommandBarButton)?.Label ?? (e.Source as CommandBarToggleButton)?.Label;
            if (label is not null)
            {
                ClickText.Text = $"最近点击：{label}";
            }
        }

        // Overflow is decided during layout, so read it after the layout pass.
        private void RefreshOverflow()
        {
            Dispatcher.UIThread.Post(() =>
            {
                int overflowed = OverflowBar.PrimaryCommands.OfType<ICommandBarElement>().Count(c => c.IsInOverflow);
                OverflowText.Text = $"IsDynamicOverflowEnabled = {OverflowBar.IsDynamicOverflowEnabled}，进入溢出菜单的主命令：{overflowed} 个";
            }, DispatcherPriority.Loaded);
        }
    }
}
```

- [ ] **Step 3: ContentPage**

`ContentPage` 是单页容器：`Header`、一块内容，外加顶部与底部各一个命令栏插槽。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/ContentDemoPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ContentDemoPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ContentPage：带命令栏插槽的单页"
                               DocPath="controls/navigation/contentpage" />

            <TextBlock Classes="caption" Text="Header、TopCommandBar、BottomCommandBar 与内容对齐" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="HorizontalContentAlignment" />
                        <ComboBox Name="HAlignBox" Width="120" />
                        <TextBlock VerticalAlignment="Center" Text="VerticalContentAlignment" />
                        <ComboBox Name="VAlignBox" Width="120" />
                        <CheckBox Name="BottomCheck" Content="显示 BottomCommandBar" />
                    </StackPanel>
                    <Border Height="280" BorderBrush="Gray" BorderThickness="1">
                        <ContentPage Name="Demo" Header="收件箱">
                            <ContentPage.TopCommandBar>
                                <CommandBar>
                                    <CommandBar.PrimaryCommands>
                                        <CommandBarButton Name="RefreshButton" Label="刷新" />
                                    </CommandBar.PrimaryCommands>
                                </CommandBar>
                            </ContentPage.TopCommandBar>
                            <ContentPage.BottomCommandBar>
                                <CommandBar>
                                    <CommandBar.PrimaryCommands>
                                        <CommandBarButton Name="ReplyButton" Label="回复" />
                                    </CommandBar.PrimaryCommands>
                                </CommandBar>
                            </ContentPage.BottomCommandBar>
                            <Border Width="160" Height="60" Background="#3300AAFF">
                                <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="页面内容" />
                            </Border>
                        </ContentPage>
                    </Border>
                    <TextBlock Name="ClickText" Text="最近点击：（无）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ContentPage 的 Content 默认拉伸填满；内容比页面小时，用两个 ContentAlignment 决定它停在哪。TopCommandBar 与 BottomCommandBar 是 object 类型的插槽，通常放 CommandBar，设成 null 就整条消失。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/ContentDemoPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ContentDemoPage : UserControl
    {
        // The slot is cleared by setting it to null, so keep the original bar to put it back.
        private readonly object? _bottomBar;

        public ContentDemoPage()
        {
            InitializeComponent();
            _bottomBar = Demo.BottomCommandBar;

            HAlignBox.ItemsSource = Enum.GetValues<HorizontalAlignment>();
            HAlignBox.SelectedItem = Demo.HorizontalContentAlignment;
            HAlignBox.SelectionChanged += (_, _) =>
            {
                if (HAlignBox.SelectedItem is HorizontalAlignment value) Demo.HorizontalContentAlignment = value;
            };

            VAlignBox.ItemsSource = Enum.GetValues<VerticalAlignment>();
            VAlignBox.SelectedItem = Demo.VerticalContentAlignment;
            VAlignBox.SelectionChanged += (_, _) =>
            {
                if (VAlignBox.SelectedItem is VerticalAlignment value) Demo.VerticalContentAlignment = value;
            };

            BottomCheck.IsChecked = true;
            BottomCheck.IsCheckedChanged += (_, _) =>
            {
                Demo.BottomCommandBar = BottomCheck.IsChecked == true ? _bottomBar : null;
            };

            Demo.AddHandler(Button.ClickEvent, OnDemoClick);
        }

        private void OnDemoClick(object? sender, RoutedEventArgs e)
        {
            if (e.Source is CommandBarButton button)
            {
                ClickText.Text = $"最近点击：{button.Label}";
            }
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
git commit -m "feat: demonstrate CommandBar and ContentPage

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: NavigationPage 与 DrawerPage

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationDemoPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/DrawerDemoPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `DemoPages.Make(string, string) : ContentPage`
- Produces: `NavigationDemoPage`、`DrawerDemoPage`（页面类，Task 5 登记）

- [ ] **Step 1: NavigationPage**

页面把 `NavigationPage` 放进固定高度的框里，上方五个按钮对应 `PushAsync`、`PopAsync`、`PopToRootAsync`、`PushModalAsync`、`PopModalAsync`。过渡下拉同时改 `PageTransition` 与 `ModalTransition`（见"已核实事实"：只有两者都为 `null`，headless 里模态才会完成）。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationDemoPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NavigationDemoPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="NavigationPage：带返回栈的页面导航"
                               DocPath="controls/navigation/navigationpage" />

            <TextBlock Classes="caption" Text="PushAsync、PopAsync、PopToRootAsync 与模态页" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="PushButton" Content="Push 新页" />
                        <Button Name="PopButton" Content="Pop" />
                        <Button Name="RootButton" Content="PopToRoot" />
                        <Button Name="ModalButton" Content="PushModal" />
                        <Button Name="CloseModalButton" Content="PopModal" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="过渡动画" />
                        <ComboBox Name="TransitionBox" Width="140" />
                    </StackPanel>
                    <TextBlock Name="StateText" />
                    <TextBlock Name="EventText" />
                    <Border Height="260" BorderBrush="Gray" BorderThickness="1">
                        <NavigationPage Name="Nav" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="根页算进 StackDepth（所以起始是 1），此时 CanGoBack 为 False；Push 之后顶部出现返回按钮。在根页上 Pop 什么也不会发生。模态页另有一个栈（ModalStack），不计入 StackDepth，要用 PopModal 关掉。PageTransition 与 ModalTransition 的默认值由主题设成 PageSlide（窗口显示后才生效），下拉框选「无」可以关掉动画。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationDemoPage.axaml.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NavigationDemoPage : UserControl
    {
        private int _pageNumber = 1;
        private int _pushed, _popped, _modalPushed, _modalPopped;

        public NavigationDemoPage()
        {
            InitializeComponent();
            Nav.Content = DemoPages.Make("首页", "这是栈底的根页。点「Push 新页」压入下一页。");

            TransitionBox.ItemsSource = new[] { "无", "PageSlide", "CrossFade" };
            // The theme only fills in the default transition once the page is on screen, so sync the box then.
            Loaded += OnFirstLoaded;
            Nav.Pushed += (_, _) => { _pushed++; Refresh(); };
            Nav.Popped += (_, _) => { _popped++; Refresh(); };
            Nav.ModalPushed += (_, _) => { _modalPushed++; Refresh(); };
            Nav.ModalPopped += (_, _) => { _modalPopped++; Refresh(); };

            PushButton.Click += async (_, _) =>
            {
                _pageNumber++;
                await Nav.PushAsync(DemoPages.Make($"第 {_pageNumber} 页", $"这是第 {_pageNumber} 页，用左上角的返回按钮或「Pop」回去。"));
            };
            PopButton.Click += async (_, _) => await Nav.PopAsync();
            RootButton.Click += async (_, _) => await Nav.PopToRootAsync();
            ModalButton.Click += async (_, _) => await Nav.PushModalAsync(DemoPages.Make("模态页", "模态页盖在整个导航栈上面，用「PopModal」关掉。"));
            CloseModalButton.Click += async (_, _) => await Nav.PopModalAsync();

            Refresh();
        }

        private void OnFirstLoaded(object? sender, RoutedEventArgs e)
        {
            Loaded -= OnFirstLoaded;
            TransitionBox.SelectedIndex = Nav.PageTransition switch
            {
                PageSlide => 1,
                CrossFade => 2,
                _ => 0
            };
            TransitionBox.SelectionChanged += (_, _) => ApplyTransition();
        }

        private void ApplyTransition()
        {
            IPageTransition? transition = TransitionBox.SelectedItem switch
            {
                "PageSlide" => new PageSlide(TimeSpan.FromMilliseconds(250)),
                "CrossFade" => new CrossFade(TimeSpan.FromMilliseconds(250)),
                _ => null
            };
            Nav.PageTransition = transition;
            Nav.ModalTransition = transition;
        }

        private void Refresh()
        {
            StateText.Text = $"StackDepth = {Nav.StackDepth}，CanGoBack = {Nav.CanGoBack}，ModalStack = {Nav.ModalStack.Count}";
            EventText.Text = $"Pushed {_pushed} 次，Popped {_popped} 次，ModalPushed {_modalPushed} 次，ModalPopped {_modalPopped} 次";
        }
    }
}
```

- [ ] **Step 2: DrawerPage**

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/DrawerDemoPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DrawerDemoPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DrawerPage：抽屉式页面"
                               DocPath="controls/navigation/drawerpage" />

            <TextBlock Classes="caption" Text="IsOpen、DrawerBehavior、DrawerPlacement 与 DrawerLayoutBehavior" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <WrapPanel>
                        <CheckBox Name="OpenCheck" Margin="0,0,12,0" Content="IsOpen" />
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="DrawerBehavior" />
                        <ComboBox Name="BehaviorBox" Width="110" Margin="0,0,12,0" />
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="DrawerPlacement" />
                        <ComboBox Name="PlacementBox" Width="100" Margin="0,0,12,0" />
                        <TextBlock VerticalAlignment="Center" Margin="0,0,4,0" Text="DrawerLayoutBehavior" />
                        <ComboBox Name="LayoutBox" Width="150" />
                    </WrapPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="DrawerLength" />
                        <Slider Name="LengthSlider" Width="200" Minimum="120" Maximum="400" />
                    </StackPanel>
                    <TextBlock Name="StateText" />
                    <TextBlock Name="EventText" />
                    <Border Height="300" BorderBrush="Gray" BorderThickness="1">
                        <DrawerPage Name="Demo">
                            <DrawerPage.Drawer>
                                <StackPanel Margin="12" Spacing="8">
                                    <TextBlock FontWeight="Bold" Text="抽屉内容" />
                                    <Button Name="InDrawerButton" Content="从抽屉里关闭" />
                                </StackPanel>
                            </DrawerPage.Drawer>
                            <DrawerPage.Content>
                                <ContentPage Header="主页面">
                                    <TextBlock Margin="16" TextWrapping="Wrap" Text="主页面内容。勾上 IsOpen 看抽屉滑出来。" />
                                </ContentPage>
                            </DrawerPage.Content>
                        </DrawerPage>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="DrawerBehavior 切到 Locked 时抽屉会被打开，DisplayMode 变成 Inline（与内容并排，不再盖在内容上）；页面每次改动后都重读 IsOpen 与 DisplayMode，复选框只是显示读回的值。DrawerLayoutBehavior 决定抽屉是盖在内容上（Overlay）还是把内容挤开（Split）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/DrawerDemoPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DrawerDemoPage : UserControl
    {
        private int _opened, _closed;

        public DrawerDemoPage()
        {
            InitializeComponent();

            OpenCheck.IsChecked = Demo.IsOpen;
            OpenCheck.IsCheckedChanged += (_, _) =>
            {
                Demo.IsOpen = OpenCheck.IsChecked == true;
                Refresh();
            };
            InDrawerButton.Click += (_, _) =>
            {
                Demo.IsOpen = false;
                Refresh();
            };

            BehaviorBox.ItemsSource = Enum.GetValues<DrawerBehavior>();
            BehaviorBox.SelectedItem = Demo.DrawerBehavior;
            BehaviorBox.SelectionChanged += (_, _) =>
            {
                if (BehaviorBox.SelectedItem is DrawerBehavior value) Demo.DrawerBehavior = value;
                Refresh();
            };

            PlacementBox.ItemsSource = Enum.GetValues<DrawerPlacement>();
            PlacementBox.SelectedItem = Demo.DrawerPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is DrawerPlacement value) Demo.DrawerPlacement = value;
            };

            LayoutBox.ItemsSource = Enum.GetValues<DrawerLayoutBehavior>();
            LayoutBox.SelectedItem = Demo.DrawerLayoutBehavior;
            LayoutBox.SelectionChanged += (_, _) =>
            {
                if (LayoutBox.SelectedItem is DrawerLayoutBehavior value) Demo.DrawerLayoutBehavior = value;
                Refresh();
            };

            LengthSlider.Value = Demo.DrawerLength;
            LengthSlider.ValueChanged += (_, e) => Demo.DrawerLength = e.NewValue;

            Demo.Opened += (_, _) => { _opened++; Refresh(); };
            Demo.Closed += (_, _) => { _closed++; Refresh(); };

            Refresh();
        }

        private void Refresh()
        {
            // Locked opens the drawer on its own, so read the real value back instead of trusting the box.
            OpenCheck.IsChecked = Demo.IsOpen;
            StateText.Text = $"IsOpen = {Demo.IsOpen}，DisplayMode = {Demo.DisplayMode}";
            EventText.Text = $"Opened {_opened} 次，Closed {_closed} 次";
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
git commit -m "feat: demonstrate NavigationPage and DrawerPage

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: TabbedPage、CarouselPage、TabControl、TabStrip

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/TabbedDemoPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/CarouselDemoPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/TabControlPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/TabStripPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `DemoPages.Make(string, string) : ContentPage`
- Produces: `TabbedDemoPage`、`CarouselDemoPage`、`TabControlPage`、`TabStripPage`（页面类，Task 5 登记）

- [ ] **Step 1: TabbedPage**

`Pages` 是 `IEnumerable<Page>`，XAML 里没法填，所以页面在构造函数里赋值。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabbedDemoPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TabbedDemoPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TabbedPage：标签式多页"
                               DocPath="controls/navigation/tabbedpage" />

            <TextBlock Classes="caption" Text="Pages、SelectedIndex 与 TabPlacement" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="PrevButton" Content="上一页" />
                        <Button Name="NextButton" Content="下一页" />
                        <TextBlock VerticalAlignment="Center" Text="TabPlacement" />
                        <ComboBox Name="PlacementBox" Width="100" />
                        <CheckBox Name="GestureCheck" Content="IsGestureEnabled" />
                    </StackPanel>
                    <TextBlock Name="StateText" />
                    <Border Height="260" BorderBrush="Gray" BorderThickness="1">
                        <TabbedPage Name="Demo" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TabbedPage 与 TabControl 的区别：它的每个标签都是一个完整的 Page（带自己的 Header 与命令栏），适合整页切换；TabControl 的标签只是普通内容。TabPlacement 默认是 Auto，由平台决定标签条放哪。IsGestureEnabled 默认关，打开后触摸滑动也能换页。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabbedDemoPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabbedDemoPage : UserControl
    {
        public TabbedDemoPage()
        {
            InitializeComponent();

            Demo.Pages = new Page[]
            {
                DemoPages.Make("收件箱", "收件箱的页面内容。"),
                DemoPages.Make("已发送", "已发送的页面内容。"),
                DemoPages.Make("草稿", "草稿的页面内容。")
            };

            PrevButton.Click += (_, _) => Demo.SelectedIndex = Math.Max(0, Demo.SelectedIndex - 1);
            NextButton.Click += (_, _) => Demo.SelectedIndex = Math.Min(2, Demo.SelectedIndex + 1);

            PlacementBox.ItemsSource = Enum.GetValues<TabPlacement>();
            PlacementBox.SelectedItem = Demo.TabPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is TabPlacement value) Demo.TabPlacement = value;
            };

            GestureCheck.IsChecked = Demo.IsGestureEnabled;
            GestureCheck.IsCheckedChanged += (_, _) => Demo.IsGestureEnabled = GestureCheck.IsChecked == true;

            Demo.PropertyChanged += (_, e) =>
            {
                // SelectedPage is updated after SelectedIndex, so listen for it to read the right header.
                if (e.Property == SelectingMultiPage.SelectedIndexProperty || e.Property == SelectingMultiPage.SelectedPageProperty) Refresh();
            };
            Refresh();
        }

        private void Refresh()
        {
            string header = (Demo.SelectedPage as ContentPage)?.Header?.ToString() ?? "（无）";
            StateText.Text = $"SelectedIndex = {Demo.SelectedIndex}，SelectedPage.Header = {header}";
        }
    }
}
```

- [ ] **Step 2: CarouselPage**

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/CarouselDemoPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CarouselDemoPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="CarouselPage：轮播式多页"
                               DocPath="controls/navigation/carouselpage" />

            <TextBlock Classes="caption" Text="Pages、SelectedIndex 与 PageTransition" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="PrevButton" Content="上一页" />
                        <Button Name="NextButton" Content="下一页" />
                        <TextBlock VerticalAlignment="Center" Text="PageTransition" />
                        <ComboBox Name="TransitionBox" Width="140" />
                    </StackPanel>
                    <TextBlock Name="StateText" />
                    <Border Height="220" BorderBrush="Gray" BorderThickness="1">
                        <CarouselPage Name="Demo" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="CarouselPage 一次只显示一页，没有标签条，换页靠代码或手势。PageTransition 默认是 null，即直接切换；选 PageSlide 或 CrossFade 后切页带动画（headless 里 PageSlide 不会报完成，真实窗口没有这个问题）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/CarouselDemoPage.axaml.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CarouselDemoPage : UserControl
    {
        public CarouselDemoPage()
        {
            InitializeComponent();

            Demo.Pages = new Page[]
            {
                DemoPages.Make("第 1 页", "轮播的第 1 页。"),
                DemoPages.Make("第 2 页", "轮播的第 2 页。"),
                DemoPages.Make("第 3 页", "轮播的第 3 页。")
            };

            PrevButton.Click += (_, _) => Demo.SelectedIndex = Math.Max(0, Demo.SelectedIndex - 1);
            NextButton.Click += (_, _) => Demo.SelectedIndex = Math.Min(2, Demo.SelectedIndex + 1);

            TransitionBox.ItemsSource = new[] { "无", "PageSlide", "CrossFade" };
            TransitionBox.SelectedIndex = 0;
            TransitionBox.SelectionChanged += (_, _) =>
            {
                Demo.PageTransition = TransitionBox.SelectedItem switch
                {
                    "PageSlide" => new PageSlide(TimeSpan.FromMilliseconds(300)),
                    "CrossFade" => new CrossFade(TimeSpan.FromMilliseconds(300)),
                    _ => null
                };
            };

            Demo.PropertyChanged += (_, e) =>
            {
                if (e.Property == SelectingMultiPage.SelectedIndexProperty) Refresh();
            };
            Refresh();
        }

        private void Refresh()
        {
            StateText.Text = $"SelectedIndex = {Demo.SelectedIndex}，共 {Demo.Pages?.Count() ?? 0} 页";
        }
    }
}
```

- [ ] **Step 3: TabControl**

页面改 `TabStripPlacement` 前先记下选中项，布局完成后再恢复（见"已核实事实"：换模板会把选中重置为 0）。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabControlPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TabControlPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TabControl：标签页容器"
                               DocPath="controls/navigation/tabcontrol" />

            <TextBlock Classes="caption" Text="TabItem、TabStripPlacement 与 SelectedIndex" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="TabStripPlacement" />
                        <ComboBox Name="PlacementBox" Width="100" />
                        <Button Name="DisabledButton" Content="用代码选中被禁用的标签" />
                    </StackPanel>
                    <TextBlock Name="StateText" />
                    <Border Height="180" BorderBrush="Gray" BorderThickness="1">
                        <TabControl Name="Tabs">
                            <TabItem Header="概览">
                                <TextBlock Margin="12" Text="概览标签的内容。" />
                            </TabItem>
                            <TabItem Header="详情" IsEnabled="False">
                                <TextBlock Margin="12" Text="详情标签被禁用，鼠标点不了。" />
                            </TabItem>
                            <TabItem Header="设置">
                                <TextBlock Margin="12" Text="设置标签的内容。" />
                            </TabItem>
                        </TabControl>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TabStripPlacement 的类型是 Dock（Top / Bottom / Left / Right），默认 Top；改它会换一套模板，选中项被重置为第一个，页面在布局后把它恢复。IsEnabled=False 的标签用户点不了，但代码仍可以通过 SelectedIndex 选中它。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.StylingDemo →「选择器」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabControlPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabControlPage : UserControl
    {
        public TabControlPage()
        {
            InitializeComponent();

            PlacementBox.ItemsSource = Enum.GetValues<Dock>();
            PlacementBox.SelectedItem = Tabs.TabStripPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is not Dock dock) return;

                // Changing the placement swaps the template and resets the selection to 0.
                int selected = Tabs.SelectedIndex;
                Tabs.TabStripPlacement = dock;
                Dispatcher.UIThread.Post(() => Tabs.SelectedIndex = selected, DispatcherPriority.Loaded);
            };

            DisabledButton.Click += (_, _) => Tabs.SelectedIndex = 1;
            Tabs.SelectionChanged += (_, _) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            string header = (Tabs.SelectedItem as TabItem)?.Header?.ToString() ?? "（无）";
            StateText.Text = $"SelectedIndex = {Tabs.SelectedIndex}，选中的标签 = {header}";
        }
    }
}
```

- [ ] **Step 4: TabStrip**

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabStripPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TabStripPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TabStrip：只有标签条的选择器"
                               DocPath="controls/navigation/tabstrip" />

            <TextBlock Classes="caption" Text="TabStrip 驱动一块自己管理的内容" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TabStrip Name="Strip">
                        <TabStripItem Content="全部" />
                        <TabStripItem Content="未读" />
                        <TabStripItem Content="已标星" />
                    </TabStrip>
                    <TextBlock Name="ContentText" />
                    <Button Name="LastButton" HorizontalAlignment="Left" Content="选中最后一项" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TabStrip 只有标签条，没有内容区：它是 SelectingItemsControl，选中变化时由页面自己决定换什么内容，适合做过滤器或视图切换。要内容区跟着走，用 TabControl。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabStripPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabStripPage : UserControl
    {
        public TabStripPage()
        {
            InitializeComponent();

            Strip.SelectionChanged += (_, _) => Refresh();
            LastButton.Click += (_, _) => Strip.SelectedIndex = Strip.ItemCount - 1;
            Refresh();
        }

        private void Refresh()
        {
            string label = (Strip.SelectedItem as TabStripItem)?.Content?.ToString() ?? "（无）";
            ContentText.Text = $"SelectedIndex = {Strip.SelectedIndex}，当前显示「{label}」的内容";
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
git commit -m "feat: demonstrate TabbedPage, CarouselPage, TabControl and TabStrip

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: TabControl 与 Navigation 两篇实战页

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/DocumentTab.cs`、`WizardStep.cs`、`TabControlHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/TabControlHowToPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/NavPageViewModels.cs`、`NavigationHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 无（本任务自成一体）
- Produces: `TabControlHowToPage`、`NavigationHowToPage`（页面类，Task 5 登记）；探针会用到的公开成员：`TabControlHowToViewModel.Tabs`、`SelectedTab`、`AddTabCommand`、`CloseTabCommand`、`WizardIndex`、`CreatedCount`、`NextStepCommand`、`PreviousStepCommand`；`NavigationHowToViewModel.Current`、`GoHomeCommand`、`GoSettingsCommand`、`GoAboutCommand`、`GoBackCommand`、`GoForwardCommand`、`HistoryText`

- [ ] **Step 1: TabControl 实战的 ViewModel**

对应官方 TabControl 指南的"动态标签 / 可关闭标签 / 带图标的标签 / 绑定 SelectedIndex / 懒加载内容"五节。懒加载靠 `WizardStep.Body` 的 `??=`：`TabControl` 只为选中的标签生成内容，所以没访问过的步骤不会调用工厂。

#### `Avalonia.ControlsDemo/ViewModels/DocumentTab.cs`

```csharp
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>One closable tab in the dynamic TabControl.</summary>
    public partial class DocumentTab : ObservableObject
    {
        public DocumentTab(string title, StreamGeometry icon, string body)
        {
            Title = title;
            Icon = icon;
            _body = body;
        }

        public string Title { get; }

        public StreamGeometry Icon { get; }

        [ObservableProperty]
        private string _body;
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/WizardStep.cs`

```csharp
using System;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A wizard step whose content is only built the first time it is shown.</summary>
    public class WizardStep
    {
        private readonly Func<string> _factory;
        private string? _body;

        public WizardStep(string title, Func<string> factory)
        {
            Title = title;
            _factory = factory;
        }

        public string Title { get; }

        public string Body => _body ??= _factory();
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/TabControlHowToViewModel.cs`

```csharp
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class TabControlHowToViewModel : ObservableObject
    {
        private static readonly StreamGeometry DocIcon = StreamGeometry.Parse("M6,2 H14 L18,6 V22 H6 Z");
        private static readonly StreamGeometry LogIcon = StreamGeometry.Parse("M4,5 H20 V7 H4 Z M4,11 H20 V13 H4 Z M4,17 H20 V19 H4 Z");
        private static readonly StreamGeometry GearIcon = StreamGeometry.Parse("M12,2 L14,6 L19,6 L17,10 L19,14 L14,14 L12,18 L10,14 L5,14 L7,10 L5,6 L10,6 Z");

        private int _nextNumber = 4;

        public TabControlHowToViewModel()
        {
            Tabs.Add(new DocumentTab("说明", DocIcon, "这是第一个标签。"));
            Tabs.Add(new DocumentTab("日志", LogIcon, "这是日志标签。"));
            Tabs.Add(new DocumentTab("设置", GearIcon, "这是设置标签。"));
            _selectedTab = Tabs[0];

            WizardSteps = new[]
            {
                new WizardStep("账号", () => Build("账号")),
                new WizardStep("资料", () => Build("资料")),
                new WizardStep("确认", () => Build("确认"))
            };
        }

        public ObservableCollection<DocumentTab> Tabs { get; } = new();

        [ObservableProperty]
        private DocumentTab? _selectedTab;

        [RelayCommand]
        private void AddTab()
        {
            var tab = new DocumentTab($"文档 {_nextNumber++}", DocIcon, "新建的文档，可以直接编辑。");
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        [RelayCommand]
        private void CloseTab(DocumentTab? tab)
        {
            // Never close the last tab: the TabControl would have nothing left to show.
            if (tab is null || Tabs.Count <= 1) return;

            int index = Tabs.IndexOf(tab);
            bool wasSelected = ReferenceEquals(tab, SelectedTab);
            Tabs.Remove(tab);
            if (wasSelected)
            {
                SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
            }
        }

        public WizardStep[] WizardSteps { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
        private int _wizardIndex;

        [ObservableProperty]
        private int _createdCount;

        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private void PreviousStep() => WizardIndex--;

        private bool CanGoPrevious() => WizardIndex > 0;

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void NextStep() => WizardIndex++;

        private bool CanGoNext() => WizardIndex < WizardSteps.Length - 1;

        private string Build(string name)
        {
            CreatedCount++;
            return $"「{name}」步骤的内容（第一次显示时才创建）。";
        }
    }
}
```

- [ ] **Step 2: TabControl 实战页**

关闭按钮的命令要从标签头里"够到"页面的 ViewModel：`$parent[TabControl]` 找到所属的 `TabControl`，再把它的 `DataContext` 强转成 ViewModel 类型。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabControlHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:DataType="vm:TabControlHowToViewModel"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TabControlHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="实战：TabControl 的动态标签、关闭与向导"
                               DocPath="docs/how-to/tabcontrol-how-to" />

            <TextBlock Classes="caption" Text="动态标签：ItemsSource + ItemTemplate + ContentTemplate，带图标和关闭按钮" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="新建标签" Command="{Binding AddTabCommand}" />
                        <TextBlock VerticalAlignment="Center" Text="TabStripPlacement" />
                        <ComboBox Name="PlacementBox" Width="100" />
                        <TextBlock VerticalAlignment="Center" Text="{Binding Tabs.Count, StringFormat='共 {0} 个标签'}" />
                        <TextBlock VerticalAlignment="Center" Text="{Binding SelectedTab.Title, StringFormat='选中：{0}'}" />
                    </StackPanel>
                    <Border Height="200" BorderBrush="Gray" BorderThickness="1">
                        <TabControl Name="Tabs" ItemsSource="{Binding Tabs}" SelectedItem="{Binding SelectedTab}">
                            <TabControl.ItemTemplate>
                                <DataTemplate x:DataType="vm:DocumentTab">
                                    <StackPanel Orientation="Horizontal" Spacing="6">
                                        <PathIcon Width="12" Height="12" Data="{Binding Icon}" />
                                        <TextBlock VerticalAlignment="Center" Text="{Binding Title}" />
                                        <Button Padding="4,0" Content="✕"
                                                Command="{Binding $parent[TabControl].((vm:TabControlHowToViewModel)DataContext).CloseTabCommand}"
                                                CommandParameter="{Binding .}" />
                                    </StackPanel>
                                </DataTemplate>
                            </TabControl.ItemTemplate>
                            <TabControl.ContentTemplate>
                                <DataTemplate x:DataType="vm:DocumentTab">
                                    <TextBox Margin="8" AcceptsReturn="True" Text="{Binding Body}" />
                                </DataTemplate>
                            </TabControl.ContentTemplate>
                        </TabControl>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="标签头由 ItemTemplate 画（图标 + 标题 + 关闭按钮），内容由 ContentTemplate 画。关闭命令挂在 ViewModel 上，标签头用 $parent[TabControl] 够到它，并把当前标签作为 CommandParameter 传入；最后一个标签不会被关掉。改 TabStripPlacement 会让 TabControl 重置选中项，页面在布局后把它恢复。" />

            <TextBlock Classes="caption" Text="向导：SelectedIndex 双向绑定，内容懒创建" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Border Height="110" BorderBrush="Gray" BorderThickness="1">
                        <TabControl Name="Wizard" ItemsSource="{Binding WizardSteps}" SelectedIndex="{Binding WizardIndex}">
                            <TabControl.ItemTemplate>
                                <DataTemplate x:DataType="vm:WizardStep">
                                    <TextBlock Text="{Binding Title}" />
                                </DataTemplate>
                            </TabControl.ItemTemplate>
                            <TabControl.ContentTemplate>
                                <DataTemplate x:DataType="vm:WizardStep">
                                    <TextBlock Margin="8" TextWrapping="Wrap" Text="{Binding Body}" />
                                </DataTemplate>
                            </TabControl.ContentTemplate>
                        </TabControl>
                    </Border>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="上一步" Command="{Binding PreviousStepCommand}" />
                        <Button Content="下一步" Command="{Binding NextStepCommand}" />
                        <TextBlock VerticalAlignment="Center" Text="{Binding CreatedCount, StringFormat='已创建内容的步骤：{0} 个'}" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="SelectedIndex 绑定到整数，向导按钮只改这个数字。步骤内容放在 WizardStep.Body 的 ??= 里，TabControl 只为选中的标签取内容，所以没到过的步骤不会被创建，回头再看也不会重建。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.StylingDemo →「选择器」（官方指南最后一节的 TabItem 样式）" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/TabControlHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Threading;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabControlHowToPage : UserControl
    {
        public TabControlHowToPage()
        {
            InitializeComponent();
            var viewModel = new TabControlHowToViewModel();
            DataContext = viewModel;

            PlacementBox.ItemsSource = Enum.GetValues<Dock>();
            PlacementBox.SelectedItem = Tabs.TabStripPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is not Dock dock) return;

                // Changing the placement swaps the template, which resets (and writes back) the selection.
                var keep = viewModel.SelectedTab;
                Tabs.TabStripPlacement = dock;
                Dispatcher.UIThread.Post(() => viewModel.SelectedTab = keep, DispatcherPriority.Loaded);
            };
        }
    }
}
```

- [ ] **Step 3: Navigation 实战的 ViewModel**

对应官方 Navigation 指南的"视图切换 / 过渡 / 带后退栈的导航"三节。页面 ViewModel 的实例直接压进后退栈，所以回到设置页时，之前改过的开关还在。

#### `Avalonia.ControlsDemo/ViewModels/NavPageViewModels.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    public abstract class NavPageViewModel : ObservableObject
    {
        protected NavPageViewModel(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }

    public class HomeNavViewModel : NavPageViewModel
    {
        public HomeNavViewModel() : base("首页")
        {
        }

        public string Welcome => "欢迎回来。每个页面是一个 ViewModel，DataTemplate 决定它长什么样。";
    }

    public partial class SettingsNavViewModel : NavPageViewModel
    {
        public SettingsNavViewModel() : base("设置")
        {
        }

        [ObservableProperty]
        private bool _notifyEnabled;

        [ObservableProperty]
        private double _volume = 50;
    }

    public class AboutNavViewModel : NavPageViewModel
    {
        public AboutNavViewModel() : base("关于")
        {
        }

        public string Version => "ControlsDemo · Avalonia 12.1.2";
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/NavigationHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class NavigationHowToViewModel : ObservableObject
    {
        private readonly Stack<NavPageViewModel> _back = new();
        private readonly Stack<NavPageViewModel> _forward = new();

        public NavigationHowToViewModel()
        {
            _current = new HomeNavViewModel();
            _historyText = Describe();
        }

        [ObservableProperty]
        private NavPageViewModel _current;

        [ObservableProperty]
        private string _historyText;

        [RelayCommand]
        private void GoHome() => NavigateTo(new HomeNavViewModel());

        [RelayCommand]
        private void GoSettings() => NavigateTo(new SettingsNavViewModel());

        [RelayCommand]
        private void GoAbout() => NavigateTo(new AboutNavViewModel());

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void GoBack()
        {
            _forward.Push(Current);
            Current = _back.Pop();
            Refresh();
        }

        private bool CanGoBack() => _back.Count > 0;

        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void GoForward()
        {
            _back.Push(Current);
            Current = _forward.Pop();
            Refresh();
        }

        private bool CanGoForward() => _forward.Count > 0;

        private void NavigateTo(NavPageViewModel page)
        {
            // Going to the page you are already on would only fill the back stack with duplicates.
            if (page.GetType() == Current.GetType()) return;

            _back.Push(Current);
            _forward.Clear();
            Current = page;
            Refresh();
        }

        private void Refresh()
        {
            HistoryText = Describe();
            GoBackCommand.NotifyCanExecuteChanged();
            GoForwardCommand.NotifyCanExecuteChanged();
        }

        private string Describe() => $"后退栈 {_back.Count} 项，前进栈 {_forward.Count} 项";
    }
}
```

- [ ] **Step 4: Navigation 实战页**

三个页面 ViewModel 各配一个 `DataTemplate`，`TransitioningContentControl` 绑定 `Current`，换页时按所选过渡动画。

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:DataType="vm:NavigationHowToViewModel"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NavigationHowToPage">
    <UserControl.DataTemplates>
        <DataTemplate DataType="vm:HomeNavViewModel">
            <Border Padding="16" Background="#2200AA66">
                <TextBlock TextWrapping="Wrap" Text="{Binding Welcome}" />
            </Border>
        </DataTemplate>
        <DataTemplate DataType="vm:SettingsNavViewModel">
            <Border Padding="16" Background="#220066FF">
                <StackPanel Spacing="8">
                    <CheckBox Content="启用通知" IsChecked="{Binding NotifyEnabled}" />
                    <Slider Width="200" Minimum="0" Maximum="100" Value="{Binding Volume}" />
                </StackPanel>
            </Border>
        </DataTemplate>
        <DataTemplate DataType="vm:AboutNavViewModel">
            <Border Padding="16" Background="#22FF8800">
                <TextBlock Text="{Binding Version}" />
            </Border>
        </DataTemplate>
    </UserControl.DataTemplates>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="实战：用 ContentControl 做视图切换与后退栈"
                               DocPath="docs/how-to/navigation-how-to" />

            <TextBlock Classes="caption" Text="视图切换 + 过渡 + 后退 / 前进栈" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="◀ 后退" Command="{Binding GoBackCommand}" />
                        <Button Content="前进 ▶" Command="{Binding GoForwardCommand}" />
                        <Button Content="首页" Command="{Binding GoHomeCommand}" />
                        <Button Content="设置" Command="{Binding GoSettingsCommand}" />
                        <Button Content="关于" Command="{Binding GoAboutCommand}" />
                        <TextBlock VerticalAlignment="Center" Text="过渡" />
                        <ComboBox Name="TransitionBox" Width="120" />
                    </StackPanel>
                    <TextBlock Text="{Binding HistoryText}" />
                    <TextBlock Text="{Binding Current.Title, StringFormat='当前页：{0}'}" />
                    <Border Height="130" BorderBrush="Gray" BorderThickness="1">
                        <TransitioningContentControl Name="Host" Content="{Binding Current}" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="每个页面是一个 ViewModel，页面里的 DataTemplate 按类型决定怎么画它。后退栈存的是 ViewModel 实例本身：在设置页拨动开关，再去关于页，后退回来开关还在。跳到当前所在的页不会入栈；有新的跳转时前进栈清空。" />
            <TextBlock Classes="hint" Text="官方指南里的「侧栏导航」和「ViewLocator」两节，这个项目的主窗口就是实例：MainWindow 用 SplitView 放侧栏，右边的 ContentControl 显示当前页。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NavigationHowToPage : UserControl
    {
        public NavigationHowToPage()
        {
            InitializeComponent();
            DataContext = new NavigationHowToViewModel();

            TransitionBox.ItemsSource = new[] { "无", "CrossFade", "PageSlide" };
            TransitionBox.SelectedIndex = Host.PageTransition switch
            {
                CrossFade => 1,
                PageSlide => 2,
                _ => 0
            };
            TransitionBox.SelectionChanged += (_, _) =>
            {
                Host.PageTransition = TransitionBox.SelectedItem switch
                {
                    "CrossFade" => new CrossFade(TimeSpan.FromMilliseconds(250)),
                    "PageSlide" => new PageSlide(TimeSpan.FromMilliseconds(250)),
                    _ => null
                };
            };
        }
    }
}
```

- [ ] **Step 5: 抽取并构建**

Run: 同 Task 1 Step 4。
Expected: 0 个错误。若 `$parent[TabControl].((vm:TabControlHowToViewModel)DataContext)` 编译失败，把它改成 `$parent[UserControl].((vm:TabControlHowToViewModel)DataContext)`，再不行就退回到 code-behind 的 `Click` 处理器（`((Button)sender).DataContext as DocumentTab`）并如实记入 spec。

- [ ] **Step 6: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the TabControl and Navigation how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: 登记页面、探针验证、收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Navigation.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Nav.cs`、`Probe.Nav2.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）

**Interfaces:**
- Consumes: Task 1–4 的全部页面类；第 00 份的 `ControlPage<T>`、`HowToPage<T>`、`Harness`
- Produces: Navigation 分类共 8 个 `Control` 页 + 2 个 `HowTo` 页，登记顺序按控件名字母序，HowTo 紧跟其控件

- [ ] **Step 1: 登记**

HowTo 的标题与它所属控件页的标题相同（探针的"紧跟"断言按标题比对）：Navigation 实战页紧跟 `NavigationPage`，TabControl 实战页紧跟 `TabControl`。

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Navigation.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddNavigation(List<PageEntry> list)
        {
            const string c = Categories.Navigation;

            list.Add(ControlPage<CarouselDemoPage>("CarouselPage", c, "controls/navigation/carouselpage"));
            list.Add(ControlPage<CommandBarPage>("CommandBar", c, "controls/navigation/commandbar"));
            list.Add(ControlPage<ContentDemoPage>("ContentPage", c, "controls/navigation/contentpage"));
            list.Add(ControlPage<DrawerDemoPage>("DrawerPage", c, "controls/navigation/drawerpage"));
            list.Add(ControlPage<NavigationDemoPage>("NavigationPage", c, "controls/navigation/navigationpage"));
            list.Add(HowToPage<NavigationHowToPage>("NavigationPage", c, "docs/how-to/navigation-how-to"));
            list.Add(ControlPage<TabbedDemoPage>("TabbedPage", c, "controls/navigation/tabbedpage"));
            list.Add(ControlPage<TabControlPage>("TabControl", c, "controls/navigation/tabcontrol"));
            list.Add(HowToPage<TabControlHowToPage>("TabControl", c, "docs/how-to/tabcontrol-how-to"));
            list.Add(ControlPage<TabStripPage>("TabStrip", c, "controls/navigation/tabstrip"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

探针两个文件，都是 `static partial class ProbeNav`：`Probe.Nav.cs`（目录完整性与 8 个控件页）、`Probe.Nav2.cs`（两篇 How-to）。

**`C:\Temp\probe-controls\Probe.Nav.cs`**

```csharp
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Interactivity;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System.Linq;

static partial class ProbeNav
{
    public static void Run()
    {
        Catalog();
        CommandBarCases();
        ContentPageCases();
        NavigationPageCases();
        DrawerPageCases();
        TabbedPageCases();
        CarouselPageCases();
        TabControlCases();
        TabStripCases();
        HowTos();
    }

    static void Click(Control root, string name) => Harness.Find<Button>(root, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    static void Settle()
    {
        // Overflow, template swaps and page transitions all need more than one dispatcher round.
        for (int i = 0; i < 4; i++) Harness.Pump();
    }

    static string Text(Control root, string name) => Harness.Find<TextBlock>(root, name).Text ?? "";

    static void Catalog()
    {
        var all = PageCatalog.All.Where(e => e.Category == Categories.Navigation).ToList();
        Harness.Check("Nav: 8 Control pages", all.Count(e => e.Kind == PageKind.Control) == 8, all.Count(e => e.Kind == PageKind.Control));
        Harness.Check("Nav: 2 HowTo pages", all.Count(e => e.Kind == PageKind.HowTo) == 2, all.Count(e => e.Kind == PageKind.HowTo));

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Kind != PageKind.HowTo) continue;
            Harness.Check($"Nav: HowTo {all[i].Title} follows its control", i > 0 && all[i - 1].Kind == PageKind.Control && all[i - 1].Title == all[i].Title);
            Harness.Check($"Nav: HowTo {all[i].Title} DocPath starts with docs/how-to/", all[i].DocPath.StartsWith("docs/how-to/"));
        }

        foreach (var entry in all)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Nav/{entry.Title}({entry.Kind}): renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"Nav/{entry.Title}({entry.Kind}): header matches catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            w.Close();
        }
    }

    static void CommandBarCases()
    {
        var p = new CommandBarPage();
        var w = Harness.Show(p);
        var bar = Harness.Find<CommandBar>(p, "Bar");
        Harness.Check("CommandBar: 3 primary and 1 secondary command", bar.PrimaryCommands.Count == 3 && bar.SecondaryCommands.Count == 1, $"{bar.PrimaryCommands.Count}/{bar.SecondaryCommands.Count}");
        Harness.Check("CommandBar: labels sit at the Bottom by default", bar.DefaultLabelPosition == CommandBarDefaultLabelPosition.Bottom, bar.DefaultLabelPosition);

        Harness.Find<ComboBox>(p, "LabelBox").SelectedItem = CommandBarDefaultLabelPosition.Right;
        Settle();
        Harness.Check("CommandBar: the box drives DefaultLabelPosition and the buttons follow",
            bar.DefaultLabelPosition == CommandBarDefaultLabelPosition.Right && p.FindControl<CommandBarButton>("NewButton")!.LabelPosition == CommandBarDefaultLabelPosition.Right,
            p.FindControl<CommandBarButton>("NewButton")!.LabelPosition);

        p.FindControl<CommandBarButton>("NewButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Check("CommandBar: a Click bubbles to the bar handler", Text(p, "ClickText") == "最近点击：新建", Text(p, "ClickText"));
        p.FindControl<CommandBarToggleButton>("BoldButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Check("CommandBar: a toggle button bubbles Click too", Text(p, "ClickText") == "最近点击：加粗", Text(p, "ClickText"));

        var overflowBar = Harness.Find<CommandBar>(p, "OverflowBar");
        Harness.Check("CommandBar: dynamic overflow is off by default", !overflowBar.IsDynamicOverflowEnabled);
        Harness.Find<Slider>(p, "WidthSlider").Value = 90;
        Settle();
        Harness.Check("CommandBar: a narrow bar overflows nothing while the switch is off",
            Text(p, "OverflowText") == "IsDynamicOverflowEnabled = False，进入溢出菜单的主命令：0 个", Text(p, "OverflowText"));
        Harness.Find<CheckBox>(p, "OverflowCheck").IsChecked = true;
        Settle();
        int overflowed = overflowBar.PrimaryCommands.OfType<ICommandBarElement>().Count(c => c.IsInOverflow);
        Harness.Check("CommandBar: switching it on moves commands into the overflow menu", overflowBar.IsDynamicOverflowEnabled && overflowed > 0
            && Text(p, "OverflowText") == $"IsDynamicOverflowEnabled = True，进入溢出菜单的主命令：{overflowed} 个", $"{overflowed}/{Text(p, "OverflowText")}");
        Harness.Find<Slider>(p, "WidthSlider").Value = 520;
        Settle();
        Harness.Check("CommandBar: a wide bar brings them all back",
            overflowBar.PrimaryCommands.OfType<ICommandBarElement>().Count(c => c.IsInOverflow) == 0
            && Text(p, "OverflowText") == "IsDynamicOverflowEnabled = True，进入溢出菜单的主命令：0 个", Text(p, "OverflowText"));
        w.Close();
    }

    static void ContentPageCases()
    {
        var p = new ContentDemoPage();
        var w = Harness.Show(p);
        var demo = Harness.Find<ContentPage>(p, "Demo");
        Harness.Check("ContentPage: Header and both command bar slots are set", demo.Header?.ToString() == "收件箱" && demo.TopCommandBar is not null && demo.BottomCommandBar is not null);
        Harness.Check("ContentPage: content alignment defaults to Stretch", demo.HorizontalContentAlignment == Avalonia.Layout.HorizontalAlignment.Stretch
            && demo.VerticalContentAlignment == Avalonia.Layout.VerticalAlignment.Stretch);

        Harness.Find<ComboBox>(p, "HAlignBox").SelectedItem = Avalonia.Layout.HorizontalAlignment.Left;
        Harness.Find<ComboBox>(p, "VAlignBox").SelectedItem = Avalonia.Layout.VerticalAlignment.Top;
        Settle();
        Harness.Check("ContentPage: the boxes drive both content alignments", demo.HorizontalContentAlignment == Avalonia.Layout.HorizontalAlignment.Left
            && demo.VerticalContentAlignment == Avalonia.Layout.VerticalAlignment.Top);

        Harness.Find<CheckBox>(p, "BottomCheck").IsChecked = false;
        Harness.Check("ContentPage: unticking clears the bottom slot", demo.BottomCommandBar is null);
        Harness.Find<CheckBox>(p, "BottomCheck").IsChecked = true;
        Harness.Check("ContentPage: ticking puts the same bar back", demo.BottomCommandBar is not null);

        p.FindControl<CommandBarButton>("RefreshButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Check("ContentPage: a top bar Click bubbles to the page", Text(p, "ClickText") == "最近点击：刷新", Text(p, "ClickText"));
        w.Close();
    }

    static void NavigationPageCases()
    {
        var p = new NavigationDemoPage();
        var w = Harness.Show(p);
        var nav = Harness.Find<NavigationPage>(p, "Nav");
        Harness.Check("NavigationPage: the root page counts, so depth starts at 1 and cannot go back",
            Text(p, "StateText") == "StackDepth = 1，CanGoBack = False，ModalStack = 0", Text(p, "StateText"));
        Harness.Check("NavigationPage: once on screen the theme has set PageSlide for both transitions, and the box shows it",
            nav.PageTransition is PageSlide && nav.ModalTransition is PageSlide && Harness.Find<ComboBox>(p, "TransitionBox").SelectedItem as string == "PageSlide",
            $"{nav.PageTransition?.GetType().Name}/{Harness.Find<ComboBox>(p, "TransitionBox").SelectedItem}");
        // Animated pushes never finish under headless, so switch the animation off before navigating.
        Harness.Find<ComboBox>(p, "TransitionBox").SelectedItem = "无"; Settle();

        Click(p, "PushButton"); Settle();
        Harness.Check("NavigationPage: Push raises depth to 2 and counts Pushed",
            Text(p, "StateText") == "StackDepth = 2，CanGoBack = True，ModalStack = 0"
            && Text(p, "EventText") == "Pushed 1 次，Popped 0 次，ModalPushed 0 次，ModalPopped 0 次", $"{Text(p, "StateText")} | {Text(p, "EventText")}");
        Click(p, "PopButton"); Settle();
        Harness.Check("NavigationPage: Pop returns to depth 1 and counts Popped",
            nav.StackDepth == 1 && Text(p, "EventText") == "Pushed 1 次，Popped 1 次，ModalPushed 0 次，ModalPopped 0 次", Text(p, "EventText"));
        Click(p, "PopButton"); Settle();
        Harness.Check("NavigationPage: Pop on the root page changes nothing", nav.StackDepth == 1 && Text(p, "EventText").Contains("Popped 1 次"), Text(p, "EventText"));

        Click(p, "PushButton"); Settle();
        Click(p, "PushButton"); Settle();
        Harness.Check("NavigationPage: two more pushes reach depth 3", nav.StackDepth == 3, nav.StackDepth);
        Click(p, "RootButton"); Settle();
        Harness.Check("NavigationPage: PopToRoot goes straight back to depth 1", nav.StackDepth == 1 && !nav.CanGoBack, nav.StackDepth);

        Click(p, "ModalButton"); Settle();
        Harness.Check("NavigationPage: PushModal fills the modal stack but not StackDepth",
            nav.ModalStack.Count == 1 && nav.StackDepth == 1 && Text(p, "StateText") == "StackDepth = 1，CanGoBack = False，ModalStack = 1", Text(p, "StateText"));
        Click(p, "CloseModalButton"); Settle();
        Harness.Check("NavigationPage: PopModal empties it", nav.ModalStack.Count == 0 && Text(p, "StateText").EndsWith("ModalStack = 0"), Text(p, "StateText"));

        var box = Harness.Find<ComboBox>(p, "TransitionBox");
        box.SelectedItem = "PageSlide"; Settle();
        Harness.Check("NavigationPage: PageSlide drives both transitions", nav.PageTransition is PageSlide && nav.ModalTransition is PageSlide);
        box.SelectedItem = "无"; Settle();
        Harness.Check("NavigationPage: choosing none clears both", nav.PageTransition is null && nav.ModalTransition is null);
        w.Close();
    }

    static void DrawerPageCases()
    {
        var p = new DrawerDemoPage();
        var w = Harness.Show(p);
        var demo = Harness.Find<DrawerPage>(p, "Demo");
        Harness.Check("DrawerPage: starts closed with Overlay display", !demo.IsOpen && Text(p, "StateText") == "IsOpen = False，DisplayMode = Overlay", Text(p, "StateText"));
        Harness.Check("DrawerPage: defaults are Auto, Left, Overlay and 320",
            demo.DrawerBehavior == DrawerBehavior.Auto && demo.DrawerPlacement == DrawerPlacement.Left
            && demo.DrawerLayoutBehavior == DrawerLayoutBehavior.Overlay && demo.DrawerLength == 320);

        Harness.Find<CheckBox>(p, "OpenCheck").IsChecked = true; Settle();
        Harness.Check("DrawerPage: ticking IsOpen opens it and counts Opened", demo.IsOpen && Text(p, "EventText") == "Opened 1 次，Closed 0 次", Text(p, "EventText"));
        p.FindControl<Button>("InDrawerButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Settle();
        Harness.Check("DrawerPage: the button inside the drawer closes it", !demo.IsOpen && Text(p, "EventText") == "Opened 1 次，Closed 1 次"
            && Harness.Find<CheckBox>(p, "OpenCheck").IsChecked == false, Text(p, "EventText"));

        Harness.Find<ComboBox>(p, "BehaviorBox").SelectedItem = DrawerBehavior.Locked; Settle();
        Harness.Check("DrawerPage: Locked forces it open and switches to Inline", demo.IsOpen && Text(p, "StateText") == "IsOpen = True，DisplayMode = Inline"
            && Harness.Find<CheckBox>(p, "OpenCheck").IsChecked == true, Text(p, "StateText"));
        Harness.Find<CheckBox>(p, "OpenCheck").IsChecked = false; Settle();
        Harness.Check("DrawerPage: code can still close it, and the box reads the real value back", !demo.IsOpen && Harness.Find<CheckBox>(p, "OpenCheck").IsChecked == false);
        Harness.Find<ComboBox>(p, "BehaviorBox").SelectedItem = DrawerBehavior.Auto; Settle();
        Harness.Check("DrawerPage: back to Auto the display goes back to Overlay", Text(p, "StateText") == "IsOpen = False，DisplayMode = Overlay", Text(p, "StateText"));

        Harness.Find<ComboBox>(p, "PlacementBox").SelectedItem = DrawerPlacement.Right;
        Harness.Find<ComboBox>(p, "LayoutBox").SelectedItem = DrawerLayoutBehavior.Split;
        Harness.Find<Slider>(p, "LengthSlider").Value = 200; Settle();
        Harness.Check("DrawerPage: placement, layout and length are driven by the controls",
            demo.DrawerPlacement == DrawerPlacement.Right && demo.DrawerLayoutBehavior == DrawerLayoutBehavior.Split && demo.DrawerLength == 200,
            $"{demo.DrawerPlacement}/{demo.DrawerLayoutBehavior}/{demo.DrawerLength}");
        w.Close();
    }

    static void TabbedPageCases()
    {
        var p = new TabbedDemoPage();
        var w = Harness.Show(p);
        var demo = Harness.Find<TabbedPage>(p, "Demo");
        Harness.Check("TabbedPage: 3 pages, first selected", demo.Pages!.Count() == 3 && demo.SelectedIndex == 0
            && Text(p, "StateText") == "SelectedIndex = 0，SelectedPage.Header = 收件箱", Text(p, "StateText"));
        Click(p, "NextButton"); Settle();
        Harness.Check("TabbedPage: Next selects the second page", demo.SelectedIndex == 1 && Text(p, "StateText") == "SelectedIndex = 1，SelectedPage.Header = 已发送", Text(p, "StateText"));
        Click(p, "NextButton"); Click(p, "NextButton"); Settle();
        Harness.Check("TabbedPage: Next stops at the last page", demo.SelectedIndex == 2 && Text(p, "StateText").EndsWith("草稿"), Text(p, "StateText"));
        Click(p, "PrevButton"); Settle();
        Harness.Check("TabbedPage: Prev goes back one", demo.SelectedIndex == 1, demo.SelectedIndex);

        Harness.Find<ComboBox>(p, "PlacementBox").SelectedItem = TabPlacement.Left; Settle();
        Harness.Check("TabbedPage: the box drives TabPlacement", demo.TabPlacement == TabPlacement.Left, demo.TabPlacement);
        Harness.Check("TabbedPage: gestures are off by default", !demo.IsGestureEnabled);
        Harness.Find<CheckBox>(p, "GestureCheck").IsChecked = true;
        Harness.Check("TabbedPage: the box turns gestures on", demo.IsGestureEnabled);
        w.Close();
    }

    static void CarouselPageCases()
    {
        var p = new CarouselDemoPage();
        var w = Harness.Show(p);
        // Views.Pages already has a CarouselPage (the page for the Carousel control), so spell out the control.
        var demo = Harness.Find<Avalonia.Controls.CarouselPage>(p, "Demo");
        Harness.Check("CarouselPage: 3 pages, first selected, no transition",
            Text(p, "StateText") == "SelectedIndex = 0，共 3 页" && demo.PageTransition is null, Text(p, "StateText"));
        Click(p, "NextButton"); Settle();
        Harness.Check("CarouselPage: Next selects the second page", demo.SelectedIndex == 1 && Text(p, "StateText") == "SelectedIndex = 1，共 3 页", Text(p, "StateText"));
        Click(p, "NextButton"); Click(p, "NextButton"); Settle();
        Harness.Check("CarouselPage: Next stops at the last page", demo.SelectedIndex == 2, demo.SelectedIndex);
        Click(p, "PrevButton"); Settle();
        Harness.Check("CarouselPage: Prev goes back one", demo.SelectedIndex == 1, demo.SelectedIndex);

        var box = Harness.Find<ComboBox>(p, "TransitionBox");
        box.SelectedItem = "PageSlide"; Settle();
        Harness.Check("CarouselPage: PageSlide sets the transition", demo.PageTransition is PageSlide);
        box.SelectedItem = "CrossFade"; Settle();
        Harness.Check("CarouselPage: CrossFade replaces it", demo.PageTransition is CrossFade);
        box.SelectedItem = "无"; Settle();
        Harness.Check("CarouselPage: none clears it", demo.PageTransition is null);
        w.Close();
    }

    static void TabControlCases()
    {
        var p = new TabControlPage();
        var w = Harness.Show(p);
        var tabs = Harness.Find<TabControl>(p, "Tabs");
        Harness.Check("TabControl: 3 tabs, first selected, placement Top",
            tabs.ItemCount == 3 && tabs.SelectedIndex == 0 && tabs.TabStripPlacement == Dock.Top && Text(p, "StateText") == "SelectedIndex = 0，选中的标签 = 概览", Text(p, "StateText"));
        tabs.SelectedIndex = 2; Settle();
        Harness.Check("TabControl: the readout follows SelectedIndex", Text(p, "StateText") == "SelectedIndex = 2，选中的标签 = 设置", Text(p, "StateText"));

        Harness.Find<ComboBox>(p, "PlacementBox").SelectedItem = Dock.Left; Settle();
        Harness.Check("TabControl: changing the placement keeps the selected tab (the page restores it)",
            tabs.TabStripPlacement == Dock.Left && tabs.SelectedIndex == 2, $"{tabs.TabStripPlacement}/{tabs.SelectedIndex}");

        Click(p, "DisabledButton"); Settle();
        var disabled = (TabItem)tabs.Items[1]!;
        Harness.Check("TabControl: code can select a disabled tab", !disabled.IsEnabled && tabs.SelectedIndex == 1
            && Text(p, "StateText") == "SelectedIndex = 1，选中的标签 = 详情", Text(p, "StateText"));
        w.Close();
    }

    static void TabStripCases()
    {
        var p = new TabStripPage();
        var w = Harness.Show(p);
        var strip = Harness.Find<TabStrip>(p, "Strip");
        Harness.Check("TabStrip: 3 items, first selected", strip.ItemCount == 3 && Text(p, "ContentText") == "SelectedIndex = 0，当前显示「全部」的内容", Text(p, "ContentText"));
        Click(p, "LastButton"); Settle();
        Harness.Check("TabStrip: the button selects the last item", strip.SelectedIndex == 2 && Text(p, "ContentText") == "SelectedIndex = 2，当前显示「已标星」的内容", Text(p, "ContentText"));
        strip.SelectedIndex = 1; Settle();
        Harness.Check("TabStrip: the readout follows SelectedIndex", Text(p, "ContentText") == "SelectedIndex = 1，当前显示「未读」的内容", Text(p, "ContentText"));
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Nav2.cs`**

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.VisualTree;
using System.Linq;

static partial class ProbeNav
{
    static void HowTos()
    {
        TabControlHowTo();
        NavigationHowTo();
    }

    static void TabControlHowTo()
    {
        var p = new TabControlHowToPage();
        var w = Harness.Show(p);
        var vm = (TabControlHowToViewModel)p.DataContext!;
        var tabs = Harness.Find<TabControl>(p, "Tabs");
        Harness.Check("TabControl how-to: 3 tabs from the collection, first selected", tabs.ItemCount == 3 && ReferenceEquals(vm.SelectedTab, vm.Tabs[0]) && ReferenceEquals(tabs.SelectedItem, vm.Tabs[0]));
        Harness.Check("TabControl how-to: every tab header has a close button",
            tabs.GetVisualDescendants().OfType<Button>().Count(b => b.Content?.ToString() == "✕") == 3,
            tabs.GetVisualDescendants().OfType<Button>().Count(b => b.Content?.ToString() == "✕"));

        vm.AddTabCommand.Execute(null); Settle();
        Harness.Check("TabControl how-to: adding a tab selects it", vm.Tabs.Count == 4 && ReferenceEquals(vm.SelectedTab, vm.Tabs[3]) && tabs.ItemCount == 4 && ReferenceEquals(tabs.SelectedItem, vm.Tabs[3]), vm.Tabs.Count);

        var closeButtons = tabs.GetVisualDescendants().OfType<Button>().Where(b => b.Content?.ToString() == "✕").ToList();
        closeButtons[1].Command!.Execute(closeButtons[1].CommandParameter); Settle();
        Harness.Check("TabControl how-to: the header close button removes its own tab and keeps the selection",
            vm.Tabs.Count == 3 && vm.Tabs.All(t => t.Title != "日志") && vm.SelectedTab!.Title == "文档 4", $"{vm.Tabs.Count}/{vm.SelectedTab?.Title}");

        var selected = vm.SelectedTab;
        vm.CloseTabCommand.Execute(selected); Settle();
        Harness.Check("TabControl how-to: closing the selected tab selects a neighbour", vm.Tabs.Count == 2 && vm.SelectedTab is not null && !ReferenceEquals(vm.SelectedTab, selected));
        vm.CloseTabCommand.Execute(vm.Tabs[0]); Settle();
        vm.CloseTabCommand.Execute(vm.Tabs[0]); Settle();
        Harness.Check("TabControl how-to: the last tab cannot be closed", vm.Tabs.Count == 1 && tabs.ItemCount == 1 && vm.SelectedTab is not null, vm.Tabs.Count);
        vm.AddTabCommand.Execute(null); vm.AddTabCommand.Execute(null); Settle();

        var keep = vm.SelectedTab;
        Harness.Find<ComboBox>(p, "PlacementBox").SelectedItem = Dock.Bottom; Settle();
        Harness.Check("TabControl how-to: changing placement keeps the view model selection",
            tabs.TabStripPlacement == Dock.Bottom && ReferenceEquals(vm.SelectedTab, keep) && ReferenceEquals(tabs.SelectedItem, keep), $"{tabs.TabStripPlacement}/{vm.SelectedTab?.Title}");

        var box = tabs.GetVisualDescendants().OfType<TextBox>().First();
        box.Text = "edited";
        Harness.Check("TabControl how-to: the tab body is two-way bound", vm.SelectedTab!.Body == "edited", vm.SelectedTab.Body);

        var wizard = Harness.Find<TabControl>(p, "Wizard");
        Harness.Check("TabControl how-to: the wizard builds only the first step so far", vm.WizardIndex == 0 && vm.CreatedCount == 1 && wizard.SelectedIndex == 0, $"{vm.WizardIndex}/{vm.CreatedCount}");
        Harness.Check("TabControl how-to: Previous is disabled on step 1, Next is enabled", !vm.PreviousStepCommand.CanExecute(null) && vm.NextStepCommand.CanExecute(null));
        vm.NextStepCommand.Execute(null); Settle();
        Harness.Check("TabControl how-to: Next selects step 2 and builds it", vm.WizardIndex == 1 && wizard.SelectedIndex == 1 && vm.CreatedCount == 2, $"{vm.WizardIndex}/{vm.CreatedCount}");
        vm.PreviousStepCommand.Execute(null); Settle();
        Harness.Check("TabControl how-to: going back does not rebuild step 1", vm.WizardIndex == 0 && vm.CreatedCount == 2, vm.CreatedCount);
        vm.NextStepCommand.Execute(null); vm.NextStepCommand.Execute(null); Settle();
        Harness.Check("TabControl how-to: the last step disables Next", vm.WizardIndex == 2 && vm.CreatedCount == 3 && !vm.NextStepCommand.CanExecute(null), $"{vm.WizardIndex}/{vm.CreatedCount}");
        wizard.SelectedIndex = 0; Settle();
        Harness.Check("TabControl how-to: SelectedIndex is two-way, clicking a header moves the view model", vm.WizardIndex == 0, vm.WizardIndex);
        w.Close();
    }

    static void NavigationHowTo()
    {
        var p = new NavigationHowToPage();
        var w = Harness.Show(p);
        var vm = (NavigationHowToViewModel)p.DataContext!;
        var host = Harness.Find<TransitioningContentControl>(p, "Host");
        Harness.Check("Navigation how-to: starts on Home with empty stacks", vm.Current is HomeNavViewModel && vm.HistoryText == "后退栈 0 项，前进栈 0 项"
            && !vm.GoBackCommand.CanExecute(null) && !vm.GoForwardCommand.CanExecute(null), vm.HistoryText);
        Harness.Check("Navigation how-to: the Home template draws its text", host.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == ((HomeNavViewModel)vm.Current).Welcome));

        vm.GoHomeCommand.Execute(null);
        Harness.Check("Navigation how-to: going to the page you are on is ignored", vm.HistoryText == "后退栈 0 项，前进栈 0 项", vm.HistoryText);

        vm.GoSettingsCommand.Execute(null); Settle();
        Harness.Check("Navigation how-to: Settings pushes Home onto the back stack", vm.Current is SettingsNavViewModel && vm.HistoryText == "后退栈 1 项，前进栈 0 项" && vm.GoBackCommand.CanExecute(null), vm.HistoryText);
        var check = host.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        Harness.Check("Navigation how-to: the Settings template draws a check box", check is not null);
        check!.IsChecked = true;
        var settings = (SettingsNavViewModel)vm.Current;
        Harness.Check("Navigation how-to: the check box writes to the page view model", settings.NotifyEnabled);

        vm.GoAboutCommand.Execute(null); Settle();
        vm.GoBackCommand.Execute(null); Settle();
        Harness.Check("Navigation how-to: Back returns the very same Settings instance with its state",
            ReferenceEquals(vm.Current, settings) && settings.NotifyEnabled && vm.HistoryText == "后退栈 1 项，前进栈 1 项", vm.HistoryText);
        Harness.Check("Navigation how-to: Forward is now available", vm.GoForwardCommand.CanExecute(null));
        vm.GoForwardCommand.Execute(null); Settle();
        Harness.Check("Navigation how-to: Forward goes to About", vm.Current is AboutNavViewModel && vm.HistoryText == "后退栈 2 项，前进栈 0 项", vm.HistoryText);
        vm.GoBackCommand.Execute(null); Settle();
        vm.GoHomeCommand.Execute(null); Settle();
        Harness.Check("Navigation how-to: a new navigation clears the forward stack", vm.Current is HomeNavViewModel && !vm.GoForwardCommand.CanExecute(null) && vm.HistoryText.EndsWith("前进栈 0 项"), vm.HistoryText);

        var box = Harness.Find<ComboBox>(p, "TransitionBox");
        box.SelectedItem = "PageSlide"; Settle();
        Harness.Check("Navigation how-to: PageSlide is applied to the host", host.PageTransition is PageSlide);
        box.SelectedItem = "无"; Settle();
        Harness.Check("Navigation how-to: none clears it", host.PageTransition is null);
        w.Close();
    }
}
```

同时把 `C:\Temp\probe-controls\Program.cs` 的 `Main` 改为依次调用 `ProbeShell.Run(); ProbeButtons.Run(); ProbeInput.Run(); ProbeLayout.Run(); ProbeData.Run(); ProbeFmm.Run(); ProbeNav.Run(); ProbePackages.Run(); ProbeNewPages.Run();`。

- [ ] **Step 3: 跑探针**

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "FAIL|error|Unhandled|passed"`
Expected: `0 failed`、`0 warning log(s)`（编写计划时实测为 **698 passed**，其中第 00–04 份的 591 条在内，本份新增 107 条）。

**失败时的排查顺序**：
1. `NavigationPage` 的 Push / Pop / 模态不生效或读数不变：过渡动画在 headless 里不会完成。先选"无"（`PageTransition` 与 `ModalTransition` 都要是 `null`）再操作；
2. `TabbedPage` 读到上一页的标题：读 `SelectedPage` 要等 `SelectedPageProperty` 变更，不能在 `SelectedIndex` 变更里读；
3. `TabControl` 改位置后选中项回到第一个：这是预期行为，页面要在 `Dispatcher.Post(..., Loaded)` 里恢复，探针要 `Settle()` 多泵几轮；
4. `CommandBar` 溢出数始终为 0：`IsDynamicOverflowEnabled` 默认 `False`，要先勾上，并且在布局之后读；
5. `CS0104`：`CarouselPage` 在 `Views.Pages` 与 `Avalonia.Controls` 里各有一个，探针里写 `Avalonia.Controls.CarouselPage`；
6. 带命令的按钮在探针里"点了没反应"：`RaiseEvent(ClickEvent)` 不执行命令，直接调 `Command.Execute`；
7. 任何页面 `renders` 失败且无异常：对照第 00 份「Buttons 页面的共同写法」检查骨架。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|warn|个错误|Build succeeded" | tail -5`
Expected: 0 个错误、无新增警告。

- [ ] **Step 5: 真实窗口目视（人工，一次）**

```bash
dotnet run --project Avalonia.ControlsDemo
```

确认：Navigation 分类下共 10 个条目都能切换；`CommandBar` 页勾上 `IsDynamicOverflowEnabled` 后拖窄宽度，命令会收进「…」菜单；`ContentPage` 页的两个命令栏在页面顶部与底部；`NavigationPage` 页 Push 时有滑动动画、左上角出现返回按钮，换成「无」后立即切换，模态页盖在上面；`DrawerPage` 页勾 `IsOpen` 抽屉从左边滑出，改 `DrawerPlacement` 后换边；`TabbedPage` 与 `CarouselPage` 页切换时标题随之变化，`CarouselPage` 选 `PageSlide` 有动画；`TabControl` 页改 `TabStripPlacement` 后选中的标签不丢，被禁用的标签鼠标点不了；`TabControl` 实战页新建 / 关闭标签、向导的"上一步 / 下一步"在边界处按钮变灰；`Navigation` 实战页的后退 / 前进按钮在栈空时变灰，在设置页拨动开关后去别的页再后退，开关还在。动画、鼠标点击、手势与真实窗口里的返回按钮外观是 headless 验证不了的部分。

- [ ] **Step 6: 回填 spec**

在 spec「待验证的技术风险」末尾的「批 4 实测结论」之后追加（若前面的批次还没回填，先按各份 Step 6 补上）：

```markdown
### 批 5（第 05 份：Navigation）实测结论（填入执行日期）

- **`NavigationPage` 的默认过渡是主题给的**：新建后 `PageTransition` / `ModalTransition` 读到 `null`，窗口显示后主题样式把两者都设成 `PageSlide`。headless 里带过渡的 `PushAsync` 一直等动画，只有把两者都设成 `null`，推页与模态页才能完成；页面要在 `Loaded` 时按真实值同步过渡下拉。根页算进 `StackDepth`；在根页上 `PopAsync` 返回 `null`；模态栈 `ModalStack` 不计入 `StackDepth`。
- **`TabControl` 改 `TabStripPlacement` 会重置选中项**：换模板后 `SelectedIndex` 回到 0（`SelectedItem` 双向绑定也会被写回第一个），要在布局后恢复；代码可以选中 `IsEnabled=False` 的标签。
- **`TabbedPage.SelectedPage` 比 `SelectedIndex` 晚一步更新**：读标题要监听 `SelectedPageProperty`。`TabbedPage` / `CarouselPage` 的 `Pages` 只能用代码赋值。
- **`CommandBar.IsDynamicOverflowEnabled` 默认 `False`**：这时再窄也不溢出；打开后 4 个主命令的栏拖窄会依次进入溢出菜单（`ICommandBarElement.IsInOverflow`）。
- **`DrawerPage`**：`DrawerBehavior=Locked` 会把抽屉打开并让 `DisplayMode` 变成 `Inline`，但代码仍能把 `IsOpen` 设回 `False`；复选框要读回真实值。
- **命名碰撞**：`Views.Pages` 里第 03 份的 `CarouselPage`（Carousel 控件的页面）与 `Avalonia.Controls.CarouselPage` 同名，页面本身不受影响，探针里要写全名。
- **官方 How-to 与 12.1.2 的出入**：无。TabControl 指南的"样式"一节、Navigation 指南的"侧栏导航"与"ViewLocator"两节不重复实现，分别指向 `StylingDemo` 与本项目的 `MainWindow`。
- **探针断言实际条数**：本批累计 698 条（含第 00–04 份），全部通过且零警告日志。
```

尖括号与"填入执行日期"必须替换为实际日期；数字以 Step 3 的真实输出为准。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: complete the Navigation category with 8 control pages and 2 how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review（编写者自查记录）

**Spec 覆盖**：Navigation 8 页（CarouselPage、CommandBar、ContentPage、DrawerPage、NavigationPage、TabbedPage、TabControl、TabStrip）→ Task 1–3；How-to 的 TabControl、Navigation 两篇 → Task 4；官方分类内顺序与 HowTo 紧跟 → Task 5 Step 1 与探针的"紧跟"断言；`TrayIcon` → 明确归第 06 份（System 分类）。spec 批 5 表里的另外三篇（Window、Dialogs、数据持久化）也在第 06 份。

**已实测，不是推断**：本份全部页面、模型与 ViewModel 已从计划原文抽取到 `C:\Temp\plan-verify`，构建 0 错误，探针 698 条通过、零警告日志。构建与实测时发现并已回写到上面代码里的有：`NavigationPage` 默认过渡由主题在显示后才设上；`TabbedPage.SelectedPage` 晚于 `SelectedIndex`；`TabControl` 改位置会重置选中项；`CommandBar` 的动态溢出默认关闭；探针里 `CarouselPage` 的命名碰撞。`$parent[TabControl].((vm:...)DataContext).Command` 这种强转绑定在 12.1.2 的编译绑定下直接可用。
