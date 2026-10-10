# ControlsDemo 04：Feedback、Media、Menus 12 个控件页与 3 篇实战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全 `Avalonia.ControlsDemo` 的三个分类：Feedback 4 页（Notification、Popup、ProgressBar、ToolTip）、Media 3 页（Image、DrawingImage、PathIcon）、Menus 5 页（Menu、ContextMenu、MenuFlyout、NativeMenu、Separator），共 12 个 Control 页；加 Notifications、Image、Menu 3 篇 How-to 实战页。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份新增页面文件、3 个示例 ViewModel / 辅助类、`PageCatalog.Feedback.cs` / `PageCatalog.Media.cs` / `PageCatalog.Menus.cs` 的登记行，以及探针 `Probe.Fmm.cs`（Feedback / Media / Menus 的缩写）。不引入新包。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00–03 份已执行（`PageCatalog`、`Categories.Feedback/Media/Menus`、`Harness` 已存在；三个 `AddFeedback/AddMedia/AddMenus` 目前是空方法）。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」，**不重复贴**，执行者必须先读第 00 份；第 01–03 份「页面的写法约定」同样适用。本份额外强调：

- 页面类名 `<控件>Page`，How-to 页 `<主题>HowToPage`；命名空间一律 `Avalonia.ControlsDemo.Views.Pages`，模型与 ViewModel 一律 `Avalonia.ControlsDemo.ViewModels`；页面目录 `Views/Pages/Feedback/`、`Media/`、`Menus/`
- `DocPath` 已用官方 `sitemap.xml` 核对：控件页 `controls/<分类>/<控件>`，How-to 页 `docs/how-to/<主题>-how-to`（Notifications 的 How-to 路径是 `notifications-how-to`，复数）
- **事件接线放在构造函数里、`InitializeComponent()` 之后**；枚举下拉先设 `SelectedItem` 再订阅 `SelectionChanged`
- 绝不在元素上写属性的默认值（硬性规则 1：本地值会永久压制样式）；运行时由用户操作触发的代码赋值不受此限
- 需要 `TopLevel` 的控件（`WindowNotificationManager`）在 `AttachedToVisualTree` 里创建，`DetachedFromVisualTree` 里清理，否则页面每切一次就在窗口上叠一个管理器
- `MediaPlayer` 属于付费包，本份**不做**，第 06 份以 Premium 路标页处理；官方 `media/mediaplayer/**` 下的 4 个子页同归第 06 份
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时在 Avalonia 12.1.2 headless 实测）

| 控件 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `WindowNotificationManager` | 默认 `Position=TopRight`；`Show(new Notification(title, message, type, expiration, onClick))`；`MaxItems=3` 时发 6 条，**稳定后只剩 3 张活卡**（被挤出的先淡出，期间卡片总数短暂为 4）；`CloseAll()` 不处理**还在滑入的卡片**（约发出后 0.5 秒内），300 / 500 ms 时调用留下 3 张，800 ms 后调用清空 | 页面在 `AttachedToVisualTree` 里建管理器；探针按"非关闭中的卡片数"断言，`CloseAll` 前先等 1 秒 |
| `Popup` | `IsOpen` 驱动 `Opened` / `Closed`；`IsLightDismissEnabled` 默认 `False`；`PlacementTarget` 可绑 `#名字` | 页面用 `Opened` 计数，不靠轮询 |
| `ProgressBar` | **`Percentage` 在布局之前是 0**（`Value=40` 的新建控件读到 0，显示后才变 40，并触发一次 `PercentageProperty` 变更）；`Minimum=-50, Maximum=50, Value=-10` 得 40% | 读数监听 `PercentageProperty`，不在设 `Value` 后立即读 |
| `ToolTip` | 附加属性：`Tip`、`ShowDelay`、`Placement`、`IsOpen`（`ToolTip.SetIsOpen` 能代码打开）、`ShowOnDisabled` | 页面用 `ToolTip.SetShowDelay(...)` 等静态方法驱动 |
| `Image` | `Uniform` 在 260×140 框（含 1 px 边框，内部 258×138）里把 40×20 放大成 **258×129**；`Fill` 变形成 258×138；`None` 与 `DownOnly` 的 `DesiredSize` 都是 40×20；`WriteableBitmap` 可用 `Lock()` + `Marshal.Copy` 逐像素写；**headless 里 `Save` 出 0 字节、`new Bitmap(stream)` 得 1×1**，但 `Bitmap.DecodeToWidth` 与 `RenderTargetBitmap(PixelSize)` 的尺寸正确 | 示例位图用代码生成（`DemoImages.Gradient`），不依赖解码；`BitmapInterpolationMode` 在 `Avalonia.Media.Imaging` |
| `DrawingImage` | `DrawingImage.Size` 取绘制内容的边界（100×100 的图标得 100×100）；改 `GeometryDrawing.Brush` 后图自动重绘 | 页面直接改同一个 `GeometryDrawing` 的 `Brush` |
| `PathIcon` | `Data` 吃 `StreamGeometry`；`Width` / `Height` 决定大小；跟随 `Foreground` 与 `IsEnabled` 变色 | 页面从 `UserControl.Resources` 取几何 |
| `MenuItem` | 默认 `ToggleType=None`；`CheckBox` 型 `IsChecked` 可写；`Radio` + 同一 `GroupName` 后选中的清掉前一个；`InputGesture` 只是显示；**子菜单没打开时，项不在可视树里，`RaiseEvent(ClickEvent)` 到不了页面处理器**（先 `IsSubMenuOpen=true`） | 页面用 `AddHandler(MenuItem.ClickEvent, ...)` 一处接 |
| `ContextMenu` | `Open(control)` 触发 `Opened`、**不触发 `Opening`**（`Opening` 只在用户右键路径上到来）；`Close()` 触发 `Closing` | 页面分别计数三个事件并如实说明 |
| `MenuFlyout` | `ShowAt(control)` 触发 `Opened`，`Hide()` 触发 `Closed`，默认 `Placement=Bottom`；**作为 `Button.Flyout` 的子元素不进 XAML 命名范围**，写 `Name` 不生成字段，要从 `Trigger.Flyout` 取 | 页面用 `(MenuFlyout)Trigger.Flyout` |
| `NativeMenu` | `NativeMenu.SetMenu(topLevel, menu)` / `GetMenu` 可读写；headless 里 `GetIsNativeMenuExported` 为 `False`；`VisualTreeAttachmentEventArgs.Root` 已过时，改用 `RootVisual` | 页面如实显示"是否导出" |
| `RelayCommand` + 菜单样式 | 样式选择器 `MenuItem` 会命中**根菜单项自己**，对它套命令会把整个 ViewModel 当参数传入（`ArgumentException`）；要用 `MenuItem MenuItem` 排除根项，并让绑定类型是真实项与占位项的共同基类 | Menu 实战页的动态菜单 |
| `Button.Command` | `RaiseEvent(Button.ClickEvent)` 只触发 `Click`，**不会执行 `Command`**（命令走真实指针 / 键盘路径） | 探针对带命令的按钮直接 `Command.Execute` |

**成员核实（反射，Avalonia 12.1.2）**：`WindowNotificationManager` 有 `Position`、`MaxItems`、`Show(...)`、`CloseAll()`；`Notification(title, message, type, expiration, onClick, onClose)`；`NotificationType` 有 `Information / Success / Warning / Error`；`NotificationPosition` 有 `TopLeft / TopRight / BottomLeft / BottomRight`；`ToolTip` 附加属性有 `Tip / Placement / ShowDelay / IsOpen / ShowOnDisabled / HorizontalOffset / VerticalOffset`；`MenuItem` 有 `ToggleType`（`None / CheckBox / Radio`）、`GroupName`、`InputGesture`、`Icon`、`IsSubMenuOpen`；`ContextMenu` 有 `Open(Control)`、`Close()`、事件 `Opening / Opened / Closing / Closed`；`MenuFlyout` 有 `ItemsSource`、`ShowAt(Control)`、`Hide()`、`Placement`；`NativeMenu` / `NativeMenuItem` / `NativeMenuItemSeparator`、附加方法 `SetMenu / GetMenu / GetIsNativeMenuExported`；`PathIcon.Data`；`DrawingImage(Drawing)`；`Bitmap.DecodeToWidth(Stream, int)`；`RenderTargetBitmap(PixelSize)` 与 `Render(Visual)`。

**范围缺口（已知，不在本份处理）**：官方 `controls/media/mediaplayer/**`（`MediaPlayer`、`mediasource`、`media-playback` 等 4 个子页）依赖付费包，归第 06 份的 Premium 路标页。

**与官方 How-to 的出入**：官方 Notifications 指南称「Avalonia 不含内置通知控件」，但 12.1.2 里 `WindowNotificationManager` / `Notification` 存在且可用（本份 Task 1 的控件页已演示）；实战页保留官方指南的"自己搭"思路，因为它支持着色、常驻、状态栏和横幅，是内置管理器没有的那一半。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Navigation/
│   ├── PageCatalog.Feedback.cs           改：+4 控件页 +1 How-to 页
│   ├── PageCatalog.Media.cs              改：+3 控件页 +1 How-to 页
│   └── PageCatalog.Menus.cs              改：+5 控件页 +1 How-to 页
├── ViewModels/
│   ├── DemoImages.cs                     新：用代码生成示例位图（不依赖资源文件）
│   ├── NotificationsHowToViewModel.cs    新：应用内通知的数据与命令
│   ├── ImageHowToViewModel.cs            新：头像 / 占位 / 流加载
│   └── MenuHowToViewModel.cs             新：最近文件、快捷键、右键命令
└── Views/Pages/
    ├── Feedback/   NotificationPage / PopupPage / ProgressBarPage / ToolTipPage / NotificationsHowToPage
    ├── Media/      ImagePage / DrawingImagePage / PathIconPage / ImageHowToPage
    └── Menus/      MenuPage / ContextMenuPage / MenuFlyoutPage / NativeMenuPage / SeparatorPage / MenuHowToPage
```

每个页面含 `.axaml` 与 `.axaml.cs`。探针（仓库外）：`C:\Temp\probe-controls\Probe.Fmm.cs`、`Probe.Fmm2.cs`。

### 页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 页面骨架（`UserControl` + `ScrollViewer` + `StackPanel Margin="12"` + `DemoHeader`）与第 00 份相同；演示块用 `TextBlock.caption` + `Border.stage` + `TextBlock.hint`。
- 枚举下拉一律在 code-behind 构造函数里 `ItemsSource = Enum.GetValues<T>()`；滑块驱动的整数属性在处理器里强转。

### Task 1: Feedback（Notification、Popup、ProgressBar、ToolTip）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Feedback/NotificationPage.axaml`、`.axaml.cs`
- Create: `.../PopupPage.axaml`、`.axaml.cs`
- Create: `.../ProgressBarPage.axaml`、`.axaml.cs`
- Create: `.../ToolTipPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`
- Produces（探针依赖）：
  - `NotificationPage`：`public WindowNotificationManager? Manager`；元素 `TypeBox`、`PositionBox`（`ComboBox`）、`MaxItemsSlider`、`SecondsSlider`（`Slider`）、`ShowButton`、`CloseAllButton`、`ClickableButton`、`SentText`
  - `PopupPage`：`Pop`（`Popup`）、`ToggleButton`、`PlacementBox`（`ComboBox`）、`LightDismissCheck`、`StateText`
  - `ProgressBarPage`：`Bar`（`ProgressBar`）、`ValueSlider`、`IndeterminateCheck`、`TextCheck`、`VerticalCheck`、`PercentText`
  - `ToolTipPage`：`Target`（`Button`）、`DelaySlider`、`PlacementBox`（`ComboBox`）、`OpenButton`、`StateText`

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/NotificationPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NotificationPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Notification：窗口内的消息通知"
                               DocPath="controls/feedback/notification" />

            <TextBlock Classes="caption" Text="WindowNotificationManager：位置、同时显示条数与停留时间" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="类型" />
                        <ComboBox Name="TypeBox" Width="130" />
                        <TextBlock VerticalAlignment="Center" Text="Position" />
                        <ComboBox Name="PositionBox" Width="140" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="110" VerticalAlignment="Center" Text="MaxItems" />
                        <Slider Name="MaxItemsSlider" Width="160" Minimum="1" Maximum="5" Value="3" />
                        <TextBlock Name="MaxItemsText" VerticalAlignment="Center" Text="3" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="110" VerticalAlignment="Center" Text="停留（秒）" />
                        <Slider Name="SecondsSlider" Width="160" Minimum="1" Maximum="10" Value="4" />
                        <TextBlock Name="SecondsText" VerticalAlignment="Center" Text="4" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="ShowButton" Content="发送一条通知" />
                        <Button Name="ClickableButton" Content="发送可点击的通知" />
                        <Button Name="CloseAllButton" Content="全部关闭" />
                    </StackPanel>
                    <TextBlock Name="SentText" Text="已发送 0 条，点击 0 次" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="通知画在窗口的装饰层上，不占布局。MaxItems 满了以后，最老的一条被挤掉。Avalonia 本身只提供这个窗口内的管理器；系统级通知（Windows 通知中心等）需要平台代码，见实战页。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/NotificationPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NotificationPage : UserControl
    {
        private int _sent;
        private int _clicked;

        public NotificationPage()
        {
            InitializeComponent();

            TypeBox.ItemsSource = Enum.GetValues<NotificationType>();
            TypeBox.SelectedItem = NotificationType.Information;
            PositionBox.ItemsSource = Enum.GetValues<NotificationPosition>();
            PositionBox.SelectedItem = NotificationPosition.TopRight;
            PositionBox.SelectionChanged += (_, _) =>
            {
                if (Manager is not null && PositionBox.SelectedItem is NotificationPosition position)
                {
                    Manager.Position = position;
                }
            };

            MaxItemsSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    MaxItemsText.Text = ((int)MaxItemsSlider.Value).ToString();
                    if (Manager is not null)
                    {
                        Manager.MaxItems = (int)MaxItemsSlider.Value;
                    }
                }
            };
            SecondsSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    SecondsText.Text = ((int)SecondsSlider.Value).ToString();
                }
            };

            ShowButton.Click += (_, _) => Send(null);
            ClickableButton.Click += (_, _) => Send(() =>
            {
                _clicked++;
                ShowCounts();
            });
            CloseAllButton.Click += (_, _) => Manager?.CloseAll();

            AttachedToVisualTree += (_, _) =>
            {
                // The manager needs the window, so it can only be made once the page is on screen.
                if (Manager is null && TopLevel.GetTopLevel(this) is { } top)
                {
                    Manager = new WindowNotificationManager(top)
                    {
                        Position = (NotificationPosition)PositionBox.SelectedItem!,
                        MaxItems = (int)MaxItemsSlider.Value,
                    };
                }
            };
            DetachedFromVisualTree += (_, _) =>
            {
                Manager?.CloseAll();
            };
        }

        /// <summary>The window-level manager; null until the page is attached.</summary>
        public WindowNotificationManager? Manager { get; private set; }

        private void Send(Action? onClick)
        {
            if (Manager is null)
            {
                return;
            }

            var type = (NotificationType)TypeBox.SelectedItem!;
            _sent++;
            Manager.Show(new Notification($"第 {_sent} 条", $"这是一条 {type} 通知", type,
                TimeSpan.FromSeconds((int)SecondsSlider.Value), onClick));
            ShowCounts();
        }

        private void ShowCounts() => SentText.Text = $"已发送 {_sent} 条，点击 {_clicked} 次";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/PopupPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.PopupPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Popup：浮在内容上的小窗口"
                               DocPath="controls/feedback/popup" />

            <TextBlock Classes="caption" Text="PlacementTarget、Placement 与 IsLightDismissEnabled" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Placement" />
                        <ComboBox Name="PlacementBox" Width="170" />
                        <CheckBox Name="LightDismissCheck" Content="IsLightDismissEnabled（点外面关闭）" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Button Name="ToggleButton" Content="打开 / 关闭 Popup" />
                        <TextBlock Name="StateText" VerticalAlignment="Center" Text="IsOpen = False，打开 0 次" />
                    </StackPanel>
                    <Popup Name="Pop" PlacementTarget="{Binding #ToggleButton}" Placement="Bottom">
                        <Border Padding="12" CornerRadius="6" BorderThickness="1" BorderBrush="Gray" Background="#FF2F4F6F">
                            <TextBlock Foreground="White" Text="我是 Popup 里的内容" />
                        </Border>
                    </Popup>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Popup 本身不画任何外观，Child 画成什么样全由你决定。默认不会点外面自动关闭，要这个行为就勾上 IsLightDismissEnabled。需要现成样式的浮层（带阴影、随焦点关闭）用 Flyout，见 Primitives。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/PopupPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PopupPage : UserControl
    {
        private int _opened;

        public PopupPage()
        {
            InitializeComponent();

            PlacementBox.ItemsSource = new[]
            {
                PlacementMode.Bottom, PlacementMode.Top, PlacementMode.Left, PlacementMode.Right, PlacementMode.Pointer,
            };
            PlacementBox.SelectedItem = PlacementMode.Bottom;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is PlacementMode mode)
                {
                    Pop.Placement = mode;
                }
            };

            LightDismissCheck.IsCheckedChanged += (_, _) => Pop.IsLightDismissEnabled = LightDismissCheck.IsChecked == true;
            ToggleButton.Click += (_, _) => Pop.IsOpen = !Pop.IsOpen;
            Pop.Opened += (_, _) =>
            {
                _opened++;
                Show();
            };
            Pop.Closed += (_, _) => Show();
        }

        private void Show() => StateText.Text = $"IsOpen = {Pop.IsOpen}，打开 {_opened} 次";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/ProgressBarPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ProgressBarPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ProgressBar：进度条"
                               DocPath="controls/feedback/progressbar" />

            <TextBlock Classes="caption" Text="Value、IsIndeterminate、ShowProgressText 与方向" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="90" VerticalAlignment="Center" Text="Value" />
                        <Slider Name="ValueSlider" Width="200" Minimum="0" Maximum="100" Value="40" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="IndeterminateCheck" Content="IsIndeterminate" />
                        <CheckBox Name="TextCheck" Content="ShowProgressText" />
                        <CheckBox Name="VerticalCheck" Content="Vertical" />
                    </StackPanel>
                    <TextBlock Name="PercentText" />
                    <Border Width="320" Height="120" HorizontalAlignment="Left" BorderBrush="Gray" BorderThickness="1" Padding="8">
                        <ProgressBar Name="Bar" Minimum="0" Maximum="100" HorizontalAlignment="Left" VerticalAlignment="Top" Width="280" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="IsIndeterminate 为 True 时忽略 Value，只播放循环动画。Percentage 是 (Value - Minimum) / (Maximum - Minimum) 算出的只读百分比，不受 Maximum 取值影响。竖向要同时改 Orientation 和宽高。" />

            <TextBlock Classes="caption" Text="自定义范围：Minimum=-50、Maximum=50" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="4">
                    <ProgressBar Name="RangeBar" Minimum="-50" Maximum="50" Value="0" Width="280" HorizontalAlignment="Left" />
                    <TextBlock Name="RangeText" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/ProgressBarPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ProgressBarPage : UserControl
    {
        public ProgressBarPage()
        {
            InitializeComponent();

            ValueSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Bar.Value = ValueSlider.Value;
                    RangeBar.Value = ValueSlider.Value - 50;
                }
            };
            IndeterminateCheck.IsCheckedChanged += (_, _) => Bar.IsIndeterminate = IndeterminateCheck.IsChecked == true;
            TextCheck.IsCheckedChanged += (_, _) => Bar.ShowProgressText = TextCheck.IsChecked == true;
            VerticalCheck.IsCheckedChanged += (_, _) =>
            {
                var vertical = VerticalCheck.IsChecked == true;
                Bar.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
                Bar.Width = vertical ? 20 : 280;
                Bar.Height = vertical ? 100 : double.NaN;
            };

            // Percentage is only recomputed once the bar has been laid out, so the readout listens to it
            // instead of reading it right after Value is set (that would show the stale 0%).
            Bar.PropertyChanged += (_, e) =>
            {
                if (e.Property == ProgressBar.PercentageProperty)
                {
                    Show();
                }
            };
            RangeBar.PropertyChanged += (_, e) =>
            {
                if (e.Property == ProgressBar.PercentageProperty)
                {
                    Show();
                }
            };

            Bar.Value = ValueSlider.Value;
            RangeBar.Value = ValueSlider.Value - 50;
            Show();
        }

        private void Show()
        {
            PercentText.Text = $"Value = {Bar.Value:0}，Percentage = {Bar.Percentage:0.#}%";
            RangeText.Text = $"Value = {RangeBar.Value:0}，Percentage = {RangeBar.Percentage:0.#}%";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/ToolTipPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ToolTipPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ToolTip：悬停提示"
                               DocPath="controls/feedback/tooltip" />

            <TextBlock Classes="caption" Text="ToolTip.Tip、ShowDelay、Placement 与代码打开" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="90" VerticalAlignment="Center" Text="ShowDelay(ms)" />
                        <Slider Name="DelaySlider" Width="200" Minimum="0" Maximum="2000" Value="400" />
                        <TextBlock Name="DelayText" VerticalAlignment="Center" Text="400" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Placement" />
                        <ComboBox Name="PlacementBox" Width="140" />
                        <Button Name="OpenButton" Content="代码打开 / 关闭" />
                    </StackPanel>
                    <Button Name="Target" Content="把鼠标停在这里" ToolTip.Tip="我是简单的文字提示" />
                    <Button Content="富内容提示">
                        <ToolTip.Tip>
                            <StackPanel>
                                <TextBlock FontWeight="Bold" Text="带结构的提示" />
                                <TextBlock Text="Tip 可以放任意控件。" />
                            </StackPanel>
                        </ToolTip.Tip>
                    </Button>
                    <Button Content="被禁用的按钮也能提示" IsEnabled="False" ToolTip.Tip="ShowOnDisabled 让禁用控件也显示提示" ToolTip.ShowOnDisabled="True" />
                    <TextBlock Name="StateText" Text="IsOpen = False" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ToolTip 是附加属性，写在任意控件上；ShowDelay 是悬停多久才出现。禁用控件默认不显示提示，需要 ShowOnDisabled。键盘焦点到达控件时提示不会自动出现，重要信息不要只放在 ToolTip 里。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/ToolTipPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ToolTipPage : UserControl
    {
        public ToolTipPage()
        {
            InitializeComponent();

            DelaySlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    var delay = (int)DelaySlider.Value;
                    DelayText.Text = delay.ToString();
                    ToolTip.SetShowDelay(Target, delay);
                }
            };

            PlacementBox.ItemsSource = new[] { PlacementMode.Bottom, PlacementMode.Top, PlacementMode.Left, PlacementMode.Right, PlacementMode.Pointer };
            PlacementBox.SelectedItem = ToolTip.GetPlacement(Target);
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is PlacementMode mode)
                {
                    ToolTip.SetPlacement(Target, mode);
                }
            };

            OpenButton.Click += (_, _) =>
            {
                ToolTip.SetIsOpen(Target, !ToolTip.GetIsOpen(Target));
                StateText.Text = $"IsOpen = {ToolTip.GetIsOpen(Target)}";
            };
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate Notification, Popup, ProgressBar and ToolTip

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: Media（Image、DrawingImage、PathIcon）与示例位图

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/DemoImages.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Media/ImagePage.axaml`、`.axaml.cs`
- Create: `.../DrawingImagePage.axaml`、`.axaml.cs`
- Create: `.../PathIconPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`
- Produces:
  - `DemoImages.Gradient(int width, int height) : WriteableBitmap`（蓝到橙的横向渐变，纯代码生成，不依赖资源文件，Task 5 的 How-to 也用它）
  - `ImagePage`：`Pic`（`Image`）、`StretchBox`、`DirectionBox`、`InterpolationBox`（`ComboBox`）、`SizeText`
  - `DrawingImagePage`：`Badge`（`Image`，XAML 资源）、`Built`（`Image`，代码构建）、`RecolorButton`、`ColorText`
  - `PathIconPage`：`Icon`（`PathIcon`）、`SizeSlider`、`ColorBox`（`ComboBox`）、`GeometryBox`（`ComboBox`）、`SizeText`

#### `Avalonia.ControlsDemo/ViewModels/DemoImages.cs`

```csharp
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Builds sample bitmaps in code, so the Image pages need no binary asset and nothing to decode.</summary>
    public static class DemoImages
    {
        /// <summary>A horizontal blue-to-orange gradient, so Stretch and interpolation changes are easy to see.</summary>
        public static WriteableBitmap Gradient(int width, int height)
        {
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            var pixels = new int[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var t = width <= 1 ? 0 : x / (double)(width - 1);
                    var r = (int)(0x33 + t * (0xE8 - 0x33));
                    var g = (int)(0x66 + t * (0x97 - 0x66));
                    var b = (int)(0xCC + t * (0x4A - 0xCC));
                    // A dark diagonal makes the aspect ratio visible when the image is stretched.
                    if (x * height / width == y)
                    {
                        r = g = b = 0x20;
                    }

                    pixels[y * width + x] = unchecked((int)(0xFF000000 | (uint)(r << 16) | (uint)(g << 8) | (uint)b));
                }
            }

            using var frame = bitmap.Lock();
            Marshal.Copy(pixels, 0, frame.Address, pixels.Length);
            return bitmap;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/ImagePage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ImagePage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Image：显示位图与矢量图"
                               DocPath="controls/media/image" />

            <TextBlock Classes="caption" Text="Stretch、StretchDirection 与位图插值" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Stretch" />
                        <ComboBox Name="StretchBox" Width="140" />
                        <TextBlock VerticalAlignment="Center" Text="StretchDirection" />
                        <ComboBox Name="DirectionBox" Width="120" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="BitmapInterpolationMode" />
                        <ComboBox Name="InterpolationBox" Width="150" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" Width="260" Height="140" HorizontalAlignment="Left">
                        <Image Name="Pic" />
                    </Border>
                    <TextBlock Name="SizeText" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="源图是 40×20 的小位图，外框 260×140。Uniform 等比放大到一边顶满；UniformToFill 等比放大到填满、多出部分被裁掉；Fill 拉伸变形；None 保持原大小。StretchDirection 限定只放大还是只缩小。小图放大时，插值模式决定是糊（HighQuality）还是马赛克（None）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/ImagePage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ImagePage : UserControl
    {
        public ImagePage()
        {
            InitializeComponent();

            Pic.Source = DemoImages.Gradient(40, 20);

            StretchBox.ItemsSource = Enum.GetValues<Stretch>();
            StretchBox.SelectedItem = Pic.Stretch;
            StretchBox.SelectionChanged += (_, _) =>
            {
                if (StretchBox.SelectedItem is Stretch stretch)
                {
                    Pic.Stretch = stretch;
                }
            };

            DirectionBox.ItemsSource = Enum.GetValues<StretchDirection>();
            DirectionBox.SelectedItem = Pic.StretchDirection;
            DirectionBox.SelectionChanged += (_, _) =>
            {
                if (DirectionBox.SelectedItem is StretchDirection direction)
                {
                    Pic.StretchDirection = direction;
                }
            };

            InterpolationBox.ItemsSource = Enum.GetValues<BitmapInterpolationMode>();
            InterpolationBox.SelectedItem = RenderOptions.GetBitmapInterpolationMode(Pic);
            InterpolationBox.SelectionChanged += (_, _) =>
            {
                if (InterpolationBox.SelectedItem is BitmapInterpolationMode mode)
                {
                    RenderOptions.SetBitmapInterpolationMode(Pic, mode);
                }
            };

            // The rendered size only changes through layout, so the readout follows LayoutUpdated.
            Pic.LayoutUpdated += (_, _) =>
            {
                var text = $"源图 {Pic.Source!.Size.Width:0}×{Pic.Source.Size.Height:0}，Image 占位 {Pic.Bounds.Width:0}×{Pic.Bounds.Height:0}，期望尺寸 {Pic.DesiredSize.Width:0}×{Pic.DesiredSize.Height:0}";
                if (SizeText.Text != text)
                {
                    SizeText.Text = text;
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/DrawingImagePage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DrawingImagePage">
    <UserControl.Resources>
        <DrawingImage x:Key="BadgeImage">
            <DrawingGroup>
                <GeometryDrawing Brush="#FF3366CC" Geometry="M0,0 L100,0 L100,100 L0,100 Z" />
                <GeometryDrawing Brush="White" Geometry="M50,12 L61,38 L90,40 L68,58 L75,86 L50,71 L25,86 L32,58 L10,40 L39,38 Z" />
            </DrawingGroup>
        </DrawingImage>
    </UserControl.Resources>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DrawingImage：矢量绘制当图片用"
                               DocPath="controls/media/drawingimage" />

            <TextBlock Classes="caption" Text="1. XAML 里描述的矢量图，任意大小都清晰" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="16">
                    <Image Name="Badge" Source="{StaticResource BadgeImage}" Width="24" Height="24" />
                    <Image Source="{StaticResource BadgeImage}" Width="64" Height="64" />
                    <Image Source="{StaticResource BadgeImage}" Width="128" Height="128" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="DrawingImage 是 IImage，所以能放进任何要 Source 的地方（Image、MenuItem.Icon、按钮内容）。它按矢量重绘，放大不糊，和位图的插值无关。" />

            <TextBlock Classes="caption" Text="2. 代码里构建并改色：GeometryDrawing.Brush 变了，图跟着重绘" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="16">
                    <Image Name="Built" Width="64" Height="64" />
                    <Button Name="RecolorButton" Content="换个颜色" VerticalAlignment="Center" />
                    <TextBlock Name="ColorText" VerticalAlignment="Center" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/DrawingImagePage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DrawingImagePage : UserControl
    {
        private static readonly IBrush[] Palette = [Brushes.OrangeRed, Brushes.SeaGreen, Brushes.SteelBlue, Brushes.Goldenrod];
        private readonly GeometryDrawing _circle;
        private int _next;

        public DrawingImagePage()
        {
            InitializeComponent();

            _circle = new GeometryDrawing
            {
                Brush = Palette[0],
                Geometry = new EllipseGeometry(new Rect(5, 5, 90, 90)),
            };
            Built.Source = new DrawingImage(_circle);
            ColorText.Text = $"Brush = {Palette[0]}";

            RecolorButton.Click += (_, _) =>
            {
                _next = (_next + 1) % Palette.Length;
                _circle.Brush = Palette[_next];
                ColorText.Text = $"Brush = {Palette[_next]}";
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/PathIconPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.PathIconPage">
    <UserControl.Resources>
        <StreamGeometry x:Key="HeartGeometry">M12,21 C12,21 3,14 3,8.5 C3,5.5 5.4,3.5 8,3.5 C9.7,3.5 11.2,4.4 12,5.8 C12.8,4.4 14.3,3.5 16,3.5 C18.6,3.5 21,5.5 21,8.5 C21,14 12,21 12,21 Z</StreamGeometry>
        <StreamGeometry x:Key="CheckGeometry">M9,16.2 L4.8,12 L3.4,13.4 L9,19 L21,7 L19.6,5.6 Z</StreamGeometry>
    </UserControl.Resources>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="PathIcon：用路径画的单色图标"
                               DocPath="controls/media/pathicon" />

            <TextBlock Classes="caption" Text="Data、Foreground 与大小" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Data" />
                        <ComboBox Name="GeometryBox" Width="110" />
                        <TextBlock VerticalAlignment="Center" Text="Foreground" />
                        <ComboBox Name="ColorBox" Width="130" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="60" VerticalAlignment="Center" Text="大小" />
                        <Slider Name="SizeSlider" Width="200" Minimum="12" Maximum="96" Value="32" />
                        <TextBlock Name="SizeText" VerticalAlignment="Center" />
                    </StackPanel>
                    <PathIcon Name="Icon" HorizontalAlignment="Left" Data="{StaticResource HeartGeometry}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="PathIcon 把一段几何数据按 Foreground 单色填充，大小由 Width / Height 决定，放大不糊。它没有自己的颜色，会随 Foreground 变，所以放在按钮、菜单里能自动跟随主题；要多色就用 DrawingImage。" />

            <TextBlock Classes="caption" Text="放进按钮和菜单项" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Button>
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <PathIcon Width="16" Height="16" Data="{StaticResource CheckGeometry}" />
                            <TextBlock Text="确认" />
                        </StackPanel>
                    </Button>
                    <Button IsEnabled="False">
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <PathIcon Width="16" Height="16" Data="{StaticResource CheckGeometry}" />
                            <TextBlock Text="禁用时图标一起变灰" />
                        </StackPanel>
                    </Button>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/PathIconPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PathIconPage : UserControl
    {
        private static readonly (string Name, IBrush Brush)[] Colors =
        [
            ("OrangeRed", Brushes.OrangeRed),
            ("SeaGreen", Brushes.SeaGreen),
            ("SteelBlue", Brushes.SteelBlue),
            ("Gray", Brushes.Gray),
        ];

        public PathIconPage()
        {
            InitializeComponent();

            GeometryBox.ItemsSource = new[] { "Heart", "Check" };
            GeometryBox.SelectedIndex = 0;
            GeometryBox.SelectionChanged += (_, _) =>
                Icon.Data = (Geometry)this.FindResource(GeometryBox.SelectedIndex == 1 ? "CheckGeometry" : "HeartGeometry")!;

            ColorBox.ItemsSource = new[] { Colors[0].Name, Colors[1].Name, Colors[2].Name, Colors[3].Name };
            ColorBox.SelectionChanged += (_, _) =>
            {
                if (ColorBox.SelectedIndex >= 0)
                {
                    Icon.Foreground = Colors[ColorBox.SelectedIndex].Brush;
                }
            };

            SizeSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Icon.Width = Icon.Height = SizeSlider.Value;
                    SizeText.Text = $"{SizeSlider.Value:0}";
                }
            };
            Icon.Width = Icon.Height = SizeSlider.Value;
            SizeText.Text = $"{SizeSlider.Value:0}";
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate Image, DrawingImage and PathIcon

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: Menus（Menu、ContextMenu、MenuFlyout、NativeMenu、Separator）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Menus/MenuPage.axaml`、`.axaml.cs`
- Create: `.../ContextMenuPage.axaml`、`.axaml.cs`
- Create: `.../MenuFlyoutPage.axaml`、`.axaml.cs`
- Create: `.../NativeMenuPage.axaml`、`.axaml.cs`
- Create: `.../SeparatorPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`
- Produces（探针依赖）：
  - `MenuPage`：`BoldItem`、`LeftItem`、`CenterItem`、`RightItem`、`NewItem`（`MenuItem`）、`LastText`、`StateText`
  - `ContextMenuPage`：`Area`（`Border`，持有 `ContextMenu`）、`Menu`（`ContextMenu`，通过 `x:Name` 访问）、`CancelCheck`、`OpenButton`、`LogText`
  - `MenuFlyoutPage`：`Trigger`（`Button`）、`ItemsFlyout`（属性，取自 `Trigger.Flyout`；`Button.Flyout` 里的元素不进 XAML 命名范围，写 `Name` 不会生成字段）、`PlacementBox`（`ComboBox`）、`ShowButton`、`LogText`
  - `NativeMenuPage`：`AttachCheck`、`ExportText`、`ItemText`、`public NativeMenu Built`
  - `SeparatorPage`：`Standalone`（`Separator`）

**本任务实测事实**：`MenuItem` 默认 `ToggleType=None`；设为 `CheckBox` 后 `IsChecked` 可写；`Radio` + 同一 `GroupName` 时后选中的把前一个清掉；对 `MenuItem` 手动 `RaiseEvent(ClickEvent)` 只触发 `Click`、**不会**翻转 `IsChecked`（翻转发生在真实的指针 / 键盘路径）；`ContextMenu.Open(control)` 触发 `Opened`、**不触发** `Opening`（`Opening` 只在用户右键时来，且可取消）；`MenuFlyout.ShowAt(control)` 触发 `Opened`，`Placement` 默认 `Bottom`；headless 里 `NativeMenu.GetIsNativeMenuExported` 为 `False`。

#### `Avalonia.ControlsDemo/Views/Pages/Menus/MenuPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MenuPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Menu：窗口里的菜单栏"
                               DocPath="controls/menus/menu" />

            <TextBlock Classes="caption" Text="子菜单、InputGesture、勾选项与单选组" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Menu HorizontalAlignment="Left">
                        <MenuItem Header="_文件">
                            <MenuItem Name="NewItem" Header="_新建" InputGesture="Ctrl+N" />
                            <MenuItem Header="_打开…" InputGesture="Ctrl+O" />
                            <Separator />
                            <MenuItem Header="最近的文件">
                                <MenuItem Header="报告.docx" />
                                <MenuItem Header="预算.xlsx" />
                            </MenuItem>
                            <MenuItem Header="不可用项" IsEnabled="False" />
                        </MenuItem>
                        <MenuItem Header="_格式">
                            <MenuItem Name="BoldItem" Header="_粗体" ToggleType="CheckBox" InputGesture="Ctrl+B" />
                            <Separator />
                            <MenuItem Name="LeftItem" Header="左对齐" ToggleType="Radio" GroupName="align" IsChecked="True" />
                            <MenuItem Name="CenterItem" Header="居中" ToggleType="Radio" GroupName="align" />
                            <MenuItem Name="RightItem" Header="右对齐" ToggleType="Radio" GroupName="align" />
                        </MenuItem>
                    </Menu>
                    <TextBlock Name="LastText" Text="最近点击：（无）" />
                    <TextBlock Name="StateText" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Header 里 _ 后面的字母是访问键（Alt+字母）。InputGesture 只是菜单右侧显示的提示文字，并不会自己注册快捷键；真正响应 Ctrl+B 要靠窗口的 KeyBindings 或 HotKey（见「实战：Menu」）。ToggleType=Radio 的几项靠同一个 GroupName 互斥。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/MenuPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MenuPage : UserControl
    {
        public MenuPage()
        {
            InitializeComponent();

            // Click bubbles up from every MenuItem, so one handler on the page is enough.
            AddHandler(MenuItem.ClickEvent, (_, e) =>
            {
                if (e.Source is MenuItem item)
                {
                    LastText.Text = $"最近点击：{item.Header}";
                    ShowState();
                }
            });
            ShowState();
        }

        private void ShowState()
        {
            var align = LeftItem.IsChecked ? "左" : CenterItem.IsChecked ? "中" : RightItem.IsChecked ? "右" : "（无）";
            StateText.Text = $"粗体 = {BoldItem.IsChecked}，对齐 = {align}";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/ContextMenuPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ContextMenuPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ContextMenu：右键菜单"
                               DocPath="controls/menus/contextmenu" />

            <TextBlock Classes="caption" Text="Opening 可取消、Closing 与代码打开" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="CancelCheck" Content="在 Opening 里取消（菜单不再弹出）" />
                        <Button Name="OpenButton" Content="代码打开 / 关闭" />
                    </StackPanel>
                    <Border Name="Area" Width="320" Height="90" HorizontalAlignment="Left" CornerRadius="6" Background="#334682B4">
                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="在这块区域里点右键" />
                        <Border.ContextMenu>
                            <ContextMenu Name="Menu">
                                <MenuItem Header="复制" InputGesture="Ctrl+C" />
                                <MenuItem Header="粘贴" InputGesture="Ctrl+V" />
                                <Separator />
                                <MenuItem Header="属性" />
                            </ContextMenu>
                        </Border.ContextMenu>
                    </Border>
                    <TextBlock Name="LogText" Text="Opening 0 次，Opened 0 次，Closing 0 次" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="实测：用代码调用 ContextMenu.Open() 只触发 Opened，不触发 Opening；Opening 只在用户右键（或按菜单键）时到来，而且只有这条路径上取消才有效。一个控件不能同时有 ContextMenu 和 ContextFlyout，只会有一个生效。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/ContextMenuPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ContextMenuPage : UserControl
    {
        private int _opening;
        private int _opened;
        private int _closing;

        public ContextMenuPage()
        {
            InitializeComponent();

            Menu.Opening += (_, e) =>
            {
                _opening++;
                e.Cancel = CancelCheck.IsChecked == true;
                Show();
            };
            Menu.Opened += (_, _) =>
            {
                _opened++;
                Show();
            };
            Menu.Closing += (_, _) =>
            {
                _closing++;
                Show();
            };

            OpenButton.Click += (_, _) =>
            {
                if (Menu.IsOpen)
                {
                    Menu.Close();
                }
                else
                {
                    Menu.Open(Area);
                }
            };
        }

        private void Show() => LogText.Text = $"Opening {_opening} 次，Opened {_opened} 次，Closing {_closing} 次";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/MenuFlyoutPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MenuFlyoutPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="MenuFlyout：点按钮弹出的菜单"
                               DocPath="controls/menus/menuflyout" />

            <TextBlock Classes="caption" Text="Button.Flyout、Placement 与 ShowAt" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Placement" />
                        <ComboBox Name="PlacementBox" Width="170" />
                        <Button Name="ShowButton" Content="用代码 ShowAt" />
                    </StackPanel>
                    <Button Name="Trigger" Content="点我弹出菜单 ▾" HorizontalAlignment="Left">
                        <Button.Flyout>
                            <MenuFlyout>
                                <MenuItem Header="重命名" />
                                <MenuItem Header="复制链接" />
                                <Separator />
                                <MenuItem Header="删除" />
                            </MenuFlyout>
                        </Button.Flyout>
                    </Button>
                    <TextBlock Name="LogText" Text="Opened 0 次，Closed 0 次" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="MenuFlyout 的内容就是 MenuItem 列表，和 ContextMenu 一样，但它挂在 Button.Flyout 上，左键点击弹出，位置用 Placement 控制。需要弹出任意内容（表单、说明）用普通 Flyout；需要右键触发用 ContextMenu。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/MenuFlyoutPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MenuFlyoutPage : UserControl
    {
        private int _opened;
        private int _closed;

        public MenuFlyoutPage()
        {
            InitializeComponent();

            PlacementBox.ItemsSource = new[]
            {
                PlacementMode.Bottom, PlacementMode.Top, PlacementMode.Right, PlacementMode.Left, PlacementMode.BottomEdgeAlignedLeft,
            };
            PlacementBox.SelectedItem = ItemsFlyout.Placement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is PlacementMode mode)
                {
                    ItemsFlyout.Placement = mode;
                }
            };

            ItemsFlyout.Opened += (_, _) => { _opened++; Show(); };
            ItemsFlyout.Closed += (_, _) => { _closed++; Show(); };
            ShowButton.Click += (_, _) => ItemsFlyout.ShowAt(Trigger);
        }

        // An element set as Button.Flyout is not in the XAML name scope, so no field is generated for it.
        private MenuFlyout ItemsFlyout => (MenuFlyout)Trigger.Flyout!;

        private void Show() => LogText.Text = $"Opened {_opened} 次，Closed {_closed} 次";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/NativeMenuPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NativeMenuPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="NativeMenu：系统原生菜单栏"
                               DocPath="controls/menus/nativemenu" />

            <TextBlock Classes="caption" Text="把 NativeMenu 挂到当前窗口，看平台是否真的导出" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <CheckBox Name="AttachCheck" Content="挂到当前窗口（NativeMenu.SetMenu）" />
                    <TextBlock Name="ExportText" />
                    <TextBlock Name="ItemText" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="NativeMenu 只在有「全局菜单栏」的平台上被导出：macOS 的屏幕顶部菜单、部分 Linux 桌面。Windows 上挂了也不显示，所以在 Windows 与其它平台都要保留窗口内的 Menu，两者可以并存。取消勾选页面会把菜单从窗口摘掉，不留痕迹。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/NativeMenuPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NativeMenuPage : UserControl
    {
        private int _clicks;

        public NativeMenuPage()
        {
            InitializeComponent();

            var hello = new NativeMenuItem("演示：你好");
            hello.Click += (_, _) =>
            {
                _clicks++;
                ShowItems();
            };
            var root = new NativeMenuItem("演示")
            {
                Menu = new NativeMenu
                {
                    Items = { hello, new NativeMenuItemSeparator(), new NativeMenuItem("演示：关于") },
                },
            };
            Built = new NativeMenu { Items = { root } };

            AttachCheck.IsCheckedChanged += (_, _) =>
            {
                if (TopLevel.GetTopLevel(this) is { } top)
                {
                    NativeMenu.SetMenu(top, AttachCheck.IsChecked == true ? Built : null);
                    ShowExport();
                }
            };
            DetachedFromVisualTree += (_, e) =>
            {
                // Leave the window as it was, whatever the box says.
                if (AttachCheck.IsChecked == true && e.RootVisual is TopLevel top)
                {
                    NativeMenu.SetMenu(top, null);
                }
            };
            AttachedToVisualTree += (_, _) => ShowExport();
            ShowItems();
        }

        /// <summary>The menu the check box attaches; kept public so it can be inspected.</summary>
        public NativeMenu Built { get; }

        private void ShowExport()
        {
            var top = TopLevel.GetTopLevel(this);
            ExportText.Text = top is null
                ? "尚未加入窗口"
                : $"NativeMenu.GetIsNativeMenuExported = {NativeMenu.GetIsNativeMenuExported(top)}";
        }

        private void ShowItems() => ItemText.Text = $"菜单含 {Built.Items.Count} 个顶层项，点击「演示：你好」{_clicks} 次";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/SeparatorPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SeparatorPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Separator：菜单里的分隔线"
                               DocPath="controls/menus/separator" />

            <TextBlock Classes="caption" Text="在 Menu 和 ContextMenu 里分组" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Menu HorizontalAlignment="Left">
                        <MenuItem Header="编辑">
                            <MenuItem Header="撤销" />
                            <MenuItem Header="重做" />
                            <Separator />
                            <MenuItem Header="剪切" />
                            <MenuItem Header="复制" />
                            <MenuItem Header="粘贴" />
                        </MenuItem>
                    </Menu>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="也可以单独放在普通布局里当一条线" />
            <Border Classes="stage" Padding="12">
                <StackPanel Width="240" HorizontalAlignment="Left" Spacing="6">
                    <TextBlock Text="上面" />
                    <Separator Name="Standalone" />
                    <TextBlock Text="下面" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Separator 是个没有内容的控件，Fluent 主题给它画一条 1 像素的横线，用在 Menu、ContextMenu 和 MenuFlyout 里，也能放进别的面板。NativeMenu 里对应的是 NativeMenuItemSeparator，两者不是同一个类型。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/SeparatorPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SeparatorPage : UserControl
    {
        public SeparatorPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate Menu, ContextMenu, MenuFlyout, NativeMenu and Separator

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: How-to 实战页（Notifications、Image、Menu）

官方 How-to 的要点（已抓取）：**Notifications** 指南强调 Avalonia 的应用内通知要自己用 `ItemsControl` + `Border` 搭，讲了浮层、状态栏、横幅、按类型着色和托盘图标；**Image** 指南讲 `avares://` 资源、从流创建 `Bitmap`、`Stretch` / `StretchDirection`、`BitmapInterpolationMode`、圆形头像（`CornerRadius` + `ClipToBounds`）、占位（`ObjectConverters.IsNull`）、`DrawingImage` / `PathIcon`、`RenderTargetBitmap` 截图；**Menu** 指南讲 `InputGesture` 只是显示、真正的快捷键要 `KeyBindings`、`MenuItem.Icon`、勾选项、`ItemsSource` + `ItemContainerTheme` 动态菜单、`ContextMenu` 的 `Opening` 取消、`CommandParameter` 用 `$parent[...]` 取点中的项。

> 与官方文字的出入：Notifications 指南称「没有内置通知控件」，但 12.1.2 里 `WindowNotificationManager` / `Notification` 确实存在（Task 1 已实测）。本任务的实战页演示的是它没覆盖的另一半：**可自定义外观、能带操作按钮的应用内通知**，与控件页互补。

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/NotificationsHowToViewModel.cs`（含 `ToastItem`）
- Create: `Avalonia.ControlsDemo/ViewModels/ImageHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/MenuHowToViewModel.cs`（含 `RecentFile`）
- Create: `Avalonia.ControlsDemo/Views/Pages/Feedback/NotificationsHowToPage.axaml`、`.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Media/ImageHowToPage.axaml`、`.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Menus/MenuHowToPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: Task 2 的 `DemoImages.Gradient`
- Produces（探针依赖）：
  - `ToastItem`：`string Message`、`string Kind`（`info` / `success` / `error`）、`bool IsInfo/IsSuccess/IsError`
  - `NotificationsHowToViewModel`：`ObservableCollection<ToastItem> Toasts`、`string StatusMessage`、`bool ShowBanner`、`void Show(string message, string kind, int durationMs)`、`DismissCommand`（参数 `ToastItem`）、`ShowStatusCommand`、`ShowBannerCommand`、`DismissBannerCommand`
  - `NotificationsHowToPage` 元素名：`InfoButton`、`SuccessButton`、`StickyButton`、`StatusButton`、`BannerButton`、`ToastList`（`ItemsControl`）
  - `ImageHowToViewModel`：`Bitmap? Photo`、`string Source`、`LoadCommand`、`ClearCommand`
  - `ImageHowToPage` 元素名：`Avatar`（`Image`）、`PhotoImage`（`Image`）、`PlaceholderText`、`LoadButton`、`ClearButton`、`ShotButton`、`ShotImage`、`ShotText`、`Capture`（`Border`）
  - `RecentEntry`（抽象基类）、`RecentFile : RecentEntry`（`string Name`）、`EmptyRecent : RecentEntry`；`MenuHowToViewModel`：`ObservableCollection<RecentFile> RecentFiles`、`IEnumerable<RecentEntry> RecentMenuItems`、`ObservableCollection<string> Items`、`bool IsBold`、`string LastAction`、`OpenRecentCommand`（参数 `RecentEntry`）、`SaveCommand`、`DeleteCommand`（参数 `string`）、`ClearRecentCommand`
  - `MenuHowToPage` 元素名：`Items`（`ListBox`）、`RecentMenu`（`MenuItem`）、`HotKeyText`

#### `Avalonia.ControlsDemo/ViewModels/NotificationsHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>One toast; the three Is* flags exist so XAML can switch a style class without a converter.</summary>
    public sealed class ToastItem
    {
        public ToastItem(string message, string kind)
        {
            Message = message;
            Kind = kind;
        }

        public string Message { get; }

        public string Kind { get; }

        public bool IsInfo => Kind == "info";

        public bool IsSuccess => Kind == "success";

        public bool IsError => Kind == "error";
    }

    public sealed partial class NotificationsHowToViewModel : ObservableObject
    {
        private CancellationTokenSource? _statusCts;

        public ObservableCollection<ToastItem> Toasts { get; } = [];

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private bool _showBanner;

        /// <summary>Adds a toast; a duration of 0 keeps it until the user dismisses it.</summary>
        public void Show(string message, string kind, int durationMs)
        {
            var toast = new ToastItem(message, kind);
            Toasts.Add(toast);
            if (durationMs > 0)
            {
                _ = RemoveLater(toast, durationMs);
            }
        }

        // await resumes on the UI thread here, so the collection is never touched from another thread.
        private async Task RemoveLater(ToastItem toast, int durationMs)
        {
            await Task.Delay(durationMs);
            Toasts.Remove(toast);
        }

        [RelayCommand]
        private void Dismiss(ToastItem toast) => Toasts.Remove(toast);

        [RelayCommand]
        private async Task ShowStatus()
        {
            // A newer message must not be wiped by the older one's timer, so each call cancels the previous wait.
            _statusCts?.Cancel();
            var cts = _statusCts = new CancellationTokenSource();
            StatusMessage = $"已保存（{DateTime.Now:HH:mm:ss}）";
            try
            {
                await Task.Delay(2500, cts.Token);
                StatusMessage = "就绪";
            }
            catch (TaskCanceledException)
            {
                // Replaced by a newer message; that call owns the reset now.
            }
        }

        [RelayCommand]
        private void ShowBannerNow() => ShowBanner = true;

        [RelayCommand]
        private void DismissBanner() => ShowBanner = false;
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/NotificationsHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NotificationsHowToPage"
             x:DataType="vm:NotificationsHowToViewModel">
    <UserControl.Styles>
        <!--  The colour comes from a style on a class the template sets, so no local Background is involved (rule 1).  -->
        <Style Selector="Border.toast">
            <Setter Property="Background" Value="#CC404040" />
        </Style>
        <Style Selector="Border.toast.success">
            <Setter Property="Background" Value="#CC2E7D32" />
        </Style>
        <Style Selector="Border.toast.error">
            <Setter Property="Background" Value="#CCC62828" />
        </Style>
    </UserControl.Styles>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Notifications 实战"
                               DocPath="docs/how-to/notifications-how-to" />

            <TextBlock Classes="caption" Text="1. 自己搭的浮层通知：可着色、可手动关闭、可常驻" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="InfoButton" Content="信息（3 秒）" />
                        <Button Name="SuccessButton" Content="成功（3 秒）" />
                        <Button Name="StickyButton" Content="错误（常驻）" />
                    </StackPanel>
                    <!--  The overlay is the last child of a Panel, so it draws over the content without taking layout space.  -->
                    <Panel Width="360" Height="170" HorizontalAlignment="Left">
                        <Border Background="#11808080" />
                        <TextBlock HorizontalAlignment="Left" VerticalAlignment="Bottom" Margin="8" Foreground="Gray" Text="页面内容区" />
                        <ItemsControl Name="ToastList" ItemsSource="{Binding Toasts}" HorizontalAlignment="Right" VerticalAlignment="Top" Width="240" Margin="8">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate x:DataType="vm:ToastItem">
                                    <Border Classes="toast" Classes.success="{Binding IsSuccess}" Classes.error="{Binding IsError}"
                                            Margin="0,0,0,4" Padding="10,6" CornerRadius="4">
                                        <Grid ColumnDefinitions="*,Auto">
                                            <TextBlock VerticalAlignment="Center" Foreground="White" TextWrapping="Wrap" Text="{Binding Message}" />
                                            <Button Grid.Column="1" Padding="6,0" Content="✕"
                                                    Command="{Binding $parent[ItemsControl].((vm:NotificationsHowToViewModel)DataContext).DismissCommand}"
                                                    CommandParameter="{Binding}" />
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </Panel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="服务层只管往 Toasts 里加项、到时间移除；界面层用 ItemsControl 画。Classes.success=&quot;{Binding IsSuccess}&quot; 把布尔属性变成样式类，颜色全在样式里。常驻的通知必须有关闭按钮，否则用户没法把它去掉。" />

            <TextBlock Classes="caption" Text="2. 状态栏：后一条消息不会被前一条的计时器清掉" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Button Name="StatusButton" Content="保存（多点几次试试）" Command="{Binding ShowStatusCommand}" />
                    <Border BorderBrush="Gray" BorderThickness="0,1,0,0" Width="360" HorizontalAlignment="Left" Padding="8,4">
                        <TextBlock Name="StatusText" Text="{Binding StatusMessage}" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="每次发消息先取消上一次的等待（CancellationTokenSource），否则连点两次，第一次的 2.5 秒到点会把第二次的消息提前清掉。" />

            <TextBlock Classes="caption" Text="3. 可关闭的横幅" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Button Name="BannerButton" Content="显示横幅" Command="{Binding ShowBannerNowCommand}" />
                    <Border Name="Banner" IsVisible="{Binding ShowBanner}" Width="360" HorizontalAlignment="Left" Padding="10,6" CornerRadius="4" Background="#33FFA000">
                        <Grid ColumnDefinitions="*,Auto">
                            <TextBlock VerticalAlignment="Center" Text="新版本可用，重启后生效。" />
                            <Button Grid.Column="1" Padding="6,0" Content="✕" Command="{Binding DismissBannerCommand}" />
                        </Grid>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="横幅是常驻的页面内消息，位置固定，不会自己消失；浮层通知是临时的。两者别混用。系统托盘通知（TrayIcon）只在桌面平台可用，移动端与浏览器要靠页面内的这几种方式。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Feedback/NotificationsHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NotificationsHowToPage : UserControl
    {
        public NotificationsHowToPage()
        {
            InitializeComponent();
            var model = new NotificationsHowToViewModel();
            DataContext = model;

            var count = 0;
            InfoButton.Click += (_, _) => model.Show($"第 {++count} 条信息", "info", 3000);
            SuccessButton.Click += (_, _) => model.Show($"第 {++count} 条：已完成", "success", 3000);
            StickyButton.Click += (_, _) => model.Show($"第 {++count} 条：出错了，需要手动关闭", "error", 0);
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/ImageHowToViewModel.cs`

```csharp
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Avalonia.ControlsDemo.ViewModels
{
    public sealed partial class ImageHowToViewModel : ObservableObject
    {
        [ObservableProperty]
        private Bitmap? _photo;

        [ObservableProperty]
        private string _source = "（未加载）";

        /// <summary>Loads an embedded resource through its avares:// URI, decoding it down to 64 px wide.</summary>
        [RelayCommand]
        private void Load()
        {
            using var stream = AssetLoader.Open(new Uri("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico"));
            // DecodeToWidth keeps only the decoded pixels needed for the target size, which saves memory for big photos.
            Photo = Bitmap.DecodeToWidth(stream, 64);
            Source = "avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico（DecodeToWidth 64）";
        }

        [RelayCommand]
        private void Clear()
        {
            Photo = null;
            Source = "（未加载）";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/ImageHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ImageHowToPage"
             x:DataType="vm:ImageHowToViewModel">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Image 实战"
                               DocPath="docs/how-to/image-how-to" />

            <TextBlock Classes="caption" Text="1. 圆形头像：Border 裁圆，Image 用 UniformToFill 填满" />
            <Border Classes="stage" Padding="12">
                <Border Width="80" Height="80" CornerRadius="40" ClipToBounds="True" HorizontalAlignment="Left">
                    <Image Name="Avatar" Stretch="UniformToFill" />
                </Border>
            </Border>
            <TextBlock Classes="hint" Text="圆角是 Border 的，裁剪也是 Border 的（ClipToBounds）；Image 自己没有圆角属性。图不是正方形时 UniformToFill 保证不留空白，代价是边缘被裁掉。" />

            <TextBlock Classes="caption" Text="2. 从资源加载，没图时显示占位" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="LoadButton" Content="加载资源图" Command="{Binding LoadCommand}" />
                        <Button Name="ClearButton" Content="清除" Command="{Binding ClearCommand}" />
                    </StackPanel>
                    <Panel Width="120" Height="80" HorizontalAlignment="Left">
                        <Border Background="#22808080" CornerRadius="4" IsVisible="{Binding Photo, Converter={x:Static ObjectConverters.IsNull}}">
                            <TextBlock Name="PlaceholderText" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="Gray" Text="暂无图片" />
                        </Border>
                        <Image Name="PhotoImage" Source="{Binding Photo}" />
                    </Panel>
                    <TextBlock Name="SourceText" Text="{Binding Source}" TextWrapping="Wrap" Width="360" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Source 绑定到 Bitmap 属性；占位用 ObjectConverters.IsNull 控制显隐，不需要任何代码。Bitmap.DecodeToWidth 只解码到目标宽度，大图列表里能明显省内存。从文件或网络加载把流交给 new Bitmap(stream) 即可，流读完后可以立刻释放。" />

            <TextBlock Classes="caption" Text="3. 截图：RenderTargetBitmap.Render 把控件画成位图" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Border Name="Capture" Width="160" Height="60" HorizontalAlignment="Left" CornerRadius="6" Background="SteelBlue">
                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="White" Text="被截图的区域" />
                    </Border>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="ShotButton" Content="截图" />
                        <TextBlock Name="ShotText" VerticalAlignment="Center" Text="尚未截图" />
                    </StackPanel>
                    <Image Name="ShotImage" Width="160" Height="60" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="被截的控件必须已经布局过（在可见窗口里），否则尺寸是 0；RenderTargetBitmap 要先 new 出指定像素尺寸，再 Render(control)，得到的位图可以直接当 Image.Source，也可以 Save 成 PNG。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Media/ImageHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media.Imaging;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ImageHowToPage : UserControl
    {
        public ImageHowToPage()
        {
            InitializeComponent();
            DataContext = new ImageHowToViewModel();

            Avatar.Source = DemoImages.Gradient(60, 40);

            ShotButton.Click += (_, _) =>
            {
                var size = new PixelSize((int)Capture.Bounds.Width, (int)Capture.Bounds.Height);
                var shot = new RenderTargetBitmap(size);
                shot.Render(Capture);
                ShotImage.Source = shot;
                ShotText.Text = $"已截图 {shot.PixelSize.Width}×{shot.PixelSize.Height}";
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/MenuHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Common base of a real entry and the empty-list placeholder, so one style can bind to either.</summary>
    public abstract class RecentEntry
    {
    }

    public sealed class RecentFile : RecentEntry
    {
        public RecentFile(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public sealed partial class MenuHowToViewModel : ObservableObject
    {
        public ObservableCollection<RecentFile> RecentFiles { get; } = [new("报告.docx"), new("预算.xlsx"), new("方案.pptx")];

        public ObservableCollection<string> Items { get; } = ["苹果", "面包", "樱桃"];

        /// <summary>The recent list, or one disabled placeholder entry when it is empty.</summary>
        public IEnumerable<RecentEntry> RecentMenuItems => RecentFiles.Count > 0 ? RecentFiles : [new EmptyRecent()];

        [ObservableProperty]
        private bool _isBold;

        [ObservableProperty]
        private string _lastAction = "（无）";

        public MenuHowToViewModel()
        {
            RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(RecentMenuItems));
        }

        // The placeholder goes through the same command, but CanOpen is false for it, so its menu item is greyed out.
        [RelayCommand(CanExecute = nameof(CanOpen))]
        private void OpenRecent(RecentEntry? entry)
        {
            if (entry is RecentFile file)
            {
                LastAction = $"打开 {file.Name}";
            }
        }

        private static bool CanOpen(RecentEntry? entry) => entry is RecentFile;

        [RelayCommand]
        private void Save() => LastAction = "保存（Ctrl+S 或菜单都会走到这里）";

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete(string? item)
        {
            if (item is not null)
            {
                Items.Remove(item);
                LastAction = $"删除 {item}";
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanDelete(string? item) => item is not null && Items.Count > 1;

        [RelayCommand]
        private void ClearRecent() => RecentFiles.Clear();
    }

    /// <summary>Stands in for the list when it is empty; its template draws one disabled line.</summary>
    public sealed class EmptyRecent : RecentEntry
    {
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/MenuHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MenuHowToPage"
             x:DataType="vm:MenuHowToViewModel">
    <UserControl.Resources>
        <StreamGeometry x:Key="SaveGeometry">M17,3 H5 C3.9,3 3,3.9 3,5 V19 C3,20.1 3.9,21 5,21 H19 C20.1,21 21,20.1 21,19 V7 L17,3 Z M12,19 C10.3,19 9,17.7 9,16 C9,14.3 10.3,13 12,13 C13.7,13 15,14.3 15,16 C15,17.7 13.7,19 12,19 Z M15,9 H5 V5 H15 Z</StreamGeometry>
    </UserControl.Resources>
    <UserControl.KeyBindings>
        <!--  InputGesture on a MenuItem is only a label; this binding is what makes Ctrl+S actually fire.  -->
        <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
    </UserControl.KeyBindings>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Menu 实战"
                               DocPath="docs/how-to/menu-how-to" />

            <TextBlock Classes="caption" Text="1. 命令 + 快捷键 + 图标 + 勾选项" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Menu HorizontalAlignment="Left">
                        <MenuItem Header="_文件">
                            <MenuItem Header="_保存" Command="{Binding SaveCommand}" InputGesture="Ctrl+S">
                                <MenuItem.Icon>
                                    <PathIcon Data="{StaticResource SaveGeometry}" />
                                </MenuItem.Icon>
                            </MenuItem>
                        </MenuItem>
                        <MenuItem Header="_格式">
                            <MenuItem Header="_粗体" ToggleType="CheckBox" IsChecked="{Binding IsBold, Mode=TwoWay}" />
                        </MenuItem>
                    </Menu>
                    <TextBlock Name="HotKeyText" Text="{Binding LastAction, StringFormat='最近操作：{0}'}" />
                    <TextBlock Text="{Binding IsBold, StringFormat='粗体 = {0}'}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="菜单项和快捷键绑到同一个命令，保证两条路径行为一致；点中菜单项或在页面上按 Ctrl+S 都会更新「最近操作」。勾选项的 IsChecked 用 TwoWay 绑定到模型。" />

            <TextBlock Classes="caption" Text="2. 动态菜单：最近的文件来自集合，空了显示一条灰色占位" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Menu HorizontalAlignment="Left">
                        <MenuItem Name="RecentMenu" Header="最近的文件" ItemsSource="{Binding RecentMenuItems}">
                            <MenuItem.DataTemplates>
                                <DataTemplate DataType="vm:RecentFile">
                                    <TextBlock Text="{Binding Name}" />
                                </DataTemplate>
                                <DataTemplate DataType="vm:EmptyRecent">
                                    <TextBlock Foreground="Gray" Text="（没有最近的文件）" />
                                </DataTemplate>
                            </MenuItem.DataTemplates>
                            <MenuItem.Styles>
                                <!--  "MenuItem MenuItem" skips the root item itself, which would otherwise get the command with the whole view model as parameter.  -->
                                <Style Selector="MenuItem MenuItem" x:DataType="vm:RecentEntry">
                                    <Setter Property="Command" Value="{Binding $parent[UserControl].((vm:MenuHowToViewModel)DataContext).OpenRecentCommand}" />
                                    <Setter Property="CommandParameter" Value="{Binding}" />
                                </Style>
                            </MenuItem.Styles>
                        </MenuItem>
                    </Menu>
                    <Button Content="清空最近的文件" Command="{Binding ClearRecentCommand}" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ItemsSource 指向模型里的集合，菜单项用数据模板生成；统一的命令和参数用样式设置到每一个生成的 MenuItem 上。空集合时让属性改返回一个占位对象，由另一个数据模板画成灰色一行，就不用写任何显隐代码。" />

            <TextBlock Classes="caption" Text="3. 右键菜单取到「点中的那一项」" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <ListBox Name="Items" ItemsSource="{Binding Items}" Width="240" Height="120" HorizontalAlignment="Left">
                        <ListBox.ContextMenu>
                            <ContextMenu>
                                <MenuItem Header="删除"
                                          Command="{Binding DeleteCommand}"
                                          CommandParameter="{Binding $parent[ListBox].SelectedItem}" />
                            </ContextMenu>
                        </ListBox.ContextMenu>
                    </ListBox>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="右键之前先左键选中一项；菜单项的 CommandParameter 用 $parent[ListBox].SelectedItem 取当前选中项。DeleteCommand 带 CanExecute：没选中或只剩一项时菜单项自动变灰。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Menus/MenuHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MenuHowToPage : UserControl
    {
        public MenuHowToPage()
        {
            InitializeComponent();
            DataContext = new MenuHowToViewModel();
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the Notifications, Image and Menu how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: 登记页面、探针验证、收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Feedback.cs`、`PageCatalog.Media.cs`、`PageCatalog.Menus.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Fmm.cs`、`Probe.Fmm2.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）

**Interfaces:**
- Consumes: Task 1–4 的全部页面类；第 00 份的 `ControlPage<T>`、`HowToPage<T>`、`Harness`
- Produces: 三个分类共 12 个 `Control` 页 + 3 个 `HowTo` 页，登记顺序为官方侧边栏顺序，HowTo 紧跟其控件

- [ ] **Step 1: 登记**

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Feedback.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddFeedback(List<PageEntry> list)
        {
            const string c = Categories.Feedback;

            list.Add(ControlPage<NotificationPage>("Notification", c, "controls/feedback/notification"));
            list.Add(HowToPage<NotificationsHowToPage>("Notification", c, "docs/how-to/notifications-how-to"));
            list.Add(ControlPage<PopupPage>("Popup", c, "controls/feedback/popup"));
            list.Add(ControlPage<ProgressBarPage>("ProgressBar", c, "controls/feedback/progressbar"));
            list.Add(ControlPage<ToolTipPage>("ToolTip", c, "controls/feedback/tooltip"));
        }
    }
}
```

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Media.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddMedia(List<PageEntry> list)
        {
            const string c = Categories.Media;

            list.Add(ControlPage<DrawingImagePage>("DrawingImage", c, "controls/media/drawingimage"));
            list.Add(ControlPage<ImagePage>("Image", c, "controls/media/image"));
            list.Add(HowToPage<ImageHowToPage>("Image", c, "docs/how-to/image-how-to"));
            list.Add(ControlPage<PathIconPage>("PathIcon", c, "controls/media/pathicon"));
        }
    }
}
```

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Menus.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddMenus(List<PageEntry> list)
        {
            const string c = Categories.Menus;

            list.Add(ControlPage<ContextMenuPage>("ContextMenu", c, "controls/menus/contextmenu"));
            list.Add(ControlPage<MenuPage>("Menu", c, "controls/menus/menu"));
            list.Add(HowToPage<MenuHowToPage>("Menu", c, "docs/how-to/menu-how-to"));
            list.Add(ControlPage<MenuFlyoutPage>("MenuFlyout", c, "controls/menus/menuflyout"));
            list.Add(ControlPage<NativeMenuPage>("NativeMenu", c, "controls/menus/nativemenu"));
            list.Add(ControlPage<SeparatorPage>("Separator", c, "controls/menus/separator"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

探针两个文件，都是 `static partial class ProbeFmm`：`Probe.Fmm.cs`（目录完整性、Feedback、Media）、`Probe.Fmm2.cs`（Menus 与三篇 How-to）。

**`C:\Temp\probe-controls\Probe.Fmm.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeFmm
{
    public static void Run()
    {
        Catalog();
        Feedback();
        Media();
        Menus();
        HowTos();
    }

    static void Click(Control root, string name) => Harness.Find<Button>(root, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    static void Catalog()
    {
        var cats = new[] { Categories.Feedback, Categories.Media, Categories.Menus };
        var all = PageCatalog.All.Where(e => cats.Contains(e.Category)).ToList();
        Harness.Check("Fmm: 12 Control pages", all.Count(e => e.Kind == PageKind.Control) == 12, all.Count(e => e.Kind == PageKind.Control));
        Harness.Check("Fmm: 3 HowTo pages", all.Count(e => e.Kind == PageKind.HowTo) == 3, all.Count(e => e.Kind == PageKind.HowTo));
        Harness.Check("Fmm: Feedback 4 + Media 3 + Menus 5 controls",
            PageCatalog.All.Count(e => e.Category == Categories.Feedback && e.Kind == PageKind.Control) == 4
            && PageCatalog.All.Count(e => e.Category == Categories.Media && e.Kind == PageKind.Control) == 3
            && PageCatalog.All.Count(e => e.Category == Categories.Menus && e.Kind == PageKind.Control) == 5);

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Kind != PageKind.HowTo) continue;
            Harness.Check($"Fmm: HowTo {all[i].Title} follows its control", i > 0 && all[i - 1].Kind == PageKind.Control && all[i - 1].Title == all[i].Title);
            Harness.Check($"Fmm: HowTo {all[i].Title} DocPath starts with docs/how-to/", all[i].DocPath.StartsWith("docs/how-to/"));
        }

        foreach (var entry in all)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Fmm/{entry.Title}({entry.Kind}): renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"Fmm/{entry.Title}({entry.Kind}): header matches catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            w.Close();
        }
    }

    static void Feedback()
    {
        // ---- Notification: the manager exists once the page is on screen, MaxItems caps what is shown
        var np = new NotificationPage();
        Harness.Check("Notification: no manager before the page is attached", np.Manager is null);
        var w = Harness.Show(np);
        var mgr = np.Manager;
        Harness.Check("Notification: attaching creates the manager with TopRight and 3 items",
            mgr is not null && mgr.Position == NotificationPosition.TopRight && mgr.MaxItems == 3, $"{mgr?.Position}/{mgr?.MaxItems}");
        Click(np, "ShowButton"); Harness.Pump();
        Harness.Check("Notification: one Show draws one card", mgr!.GetVisualDescendants().OfType<NotificationCard>().Count() == 1, mgr.GetVisualDescendants().OfType<NotificationCard>().Count());
        for (int i = 0; i < 5; i++) Click(np, "ShowButton");
        Harness.Pump();
                for (int i = 0; i < 8; i++) { System.Threading.Thread.Sleep(100); Harness.Pump(); }
        // The oldest card fades out before it is removed, so count the cards that are not closing.
        var live = mgr.GetVisualDescendants().OfType<NotificationCard>().Count(c => !c.IsClosing);
        Harness.Check("Notification: MaxItems=3 keeps 3 live cards after 6 are sent", live == 3
            && Harness.Find<TextBlock>(np, "SentText").Text == "已发送 6 条，点击 0 次",
            $"{live}/{Harness.Find<TextBlock>(np, "SentText").Text}");
        Harness.Find<ComboBox>(np, "PositionBox").SelectedItem = NotificationPosition.BottomLeft;
        Harness.Find<Slider>(np, "MaxItemsSlider").Value = 5;
        Harness.Pump();
        Harness.Check("Notification: the box and slider drive Position and MaxItems", mgr.Position == NotificationPosition.BottomLeft && mgr.MaxItems == 5, $"{mgr.Position}/{mgr.MaxItems}");
        // CloseAll skips cards that are still sliding in (about the first 0.5 s), so let them settle first.
        for (int i = 0; i < 10; i++) { System.Threading.Thread.Sleep(100); Harness.Pump(); }
        Click(np, "CloseAllButton");
        for (int i = 0; i < 6; i++) { System.Threading.Thread.Sleep(100); Harness.Pump(); }
        Harness.Check("Notification: CloseAll empties the stack (after the fade-out)", mgr.GetVisualDescendants().OfType<NotificationCard>().Count() == 0, mgr.GetVisualDescendants().OfType<NotificationCard>().Count());
        w.Close();

        // ---- Popup: open state and events follow IsOpen
        var pp = new PopupPage();
        w = Harness.Show(pp);
        var pop = Harness.Find<Popup>(pp, "Pop");
        Harness.Check("Popup: starts closed and not light-dismiss", !pop.IsOpen && !pop.IsLightDismissEnabled);
        Click(pp, "ToggleButton"); Harness.Pump();
        Harness.Check("Popup: the button opens it and Opened is counted", pop.IsOpen && Harness.Find<TextBlock>(pp, "StateText").Text == "IsOpen = True，打开 1 次", Harness.Find<TextBlock>(pp, "StateText").Text);
        Click(pp, "ToggleButton"); Harness.Pump();
        Harness.Check("Popup: the button closes it again", !pop.IsOpen && Harness.Find<TextBlock>(pp, "StateText").Text == "IsOpen = False，打开 1 次", Harness.Find<TextBlock>(pp, "StateText").Text);
        Harness.Find<ComboBox>(pp, "PlacementBox").SelectedItem = PlacementMode.Right;
        Harness.Find<CheckBox>(pp, "LightDismissCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("Popup: the controls drive Placement and IsLightDismissEnabled", pop.Placement == PlacementMode.Right && pop.IsLightDismissEnabled);
        w.Close();

        // ---- ProgressBar
        var bp = new ProgressBarPage();
        w = Harness.Show(bp);
        var bar = Harness.Find<ProgressBar>(bp, "Bar");
        Harness.Check("ProgressBar: starts at 40 and reports 40%", bar.Value == 40 && Math.Abs(bar.Percentage - 40) < 0.01
            && Harness.Find<TextBlock>(bp, "PercentText").Text == "Value = 40，Percentage = 40%", Harness.Find<TextBlock>(bp, "PercentText").Text);
        var range = Harness.Find<ProgressBar>(bp, "RangeBar");
        Harness.Check("ProgressBar: -50..50 range at Value=-10 is 40%", Math.Abs(range.Percentage - 40) < 0.01, $"{range.Value}/{range.Percentage}");
        Harness.Find<Slider>(bp, "ValueSlider").Value = 75;
        Harness.Pump();
        Harness.Check("ProgressBar: the slider drives both bars", bar.Value == 75 && Math.Abs(bar.Percentage - 75) < 0.01 && Math.Abs(range.Percentage - 75) < 0.01, $"{bar.Value}/{range.Percentage}");
        Harness.Find<CheckBox>(bp, "IndeterminateCheck").IsChecked = true;
        Harness.Find<CheckBox>(bp, "TextCheck").IsChecked = true;
        Harness.Find<CheckBox>(bp, "VerticalCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("ProgressBar: the boxes drive IsIndeterminate, ShowProgressText and Orientation",
            bar.IsIndeterminate && bar.ShowProgressText && bar.Orientation == Avalonia.Layout.Orientation.Vertical && bar.Width == 20, $"{bar.IsIndeterminate}/{bar.ShowProgressText}/{bar.Orientation}");
        w.Close();

        // ---- ToolTip
        var tp = new ToolTipPage();
        w = Harness.Show(tp);
        var target = Harness.Find<Button>(tp, "Target");
        Harness.Check("ToolTip: the text tip is set on the target", ToolTip.GetTip(target) as string == "我是简单的文字提示", ToolTip.GetTip(target));
        Harness.Find<Slider>(tp, "DelaySlider").Value = 1200;
        Harness.Find<ComboBox>(tp, "PlacementBox").SelectedItem = PlacementMode.Top;
        Harness.Pump();
        Harness.Check("ToolTip: the slider and box drive ShowDelay and Placement", ToolTip.GetShowDelay(target) == 1200 && ToolTip.GetPlacement(target) == PlacementMode.Top,
            $"{ToolTip.GetShowDelay(target)}/{ToolTip.GetPlacement(target)}");
        Click(tp, "OpenButton"); Harness.Pump();
        Harness.Check("ToolTip: the button opens it from code", ToolTip.GetIsOpen(target) && Harness.Find<TextBlock>(tp, "StateText").Text == "IsOpen = True", Harness.Find<TextBlock>(tp, "StateText").Text);
        Click(tp, "OpenButton"); Harness.Pump();
        Harness.Check("ToolTip: pressing again closes it", !ToolTip.GetIsOpen(target));
        w.Close();
    }

    static void Media()
    {
        // ---- Image
        var ip = new ImagePage();
        var w = Harness.Show(ip);
        var pic = Harness.Find<Image>(ip, "Pic");
        Harness.Check("Image: the 40x20 source is set and starts Uniform / Both", pic.Source is not null && pic.Source.Size.Width == 40 && pic.Source.Size.Height == 20
            && pic.Stretch == Stretch.Uniform && pic.StretchDirection == StretchDirection.Both, $"{pic.Source?.Size}/{pic.Stretch}/{pic.StretchDirection}");
        // The frame is 260x140 with a 1px border, so the room inside is 258x138 and a 2:1 source becomes 258x129.
        Harness.Check("Image: Uniform enlarges 40x20 to the 258 inner width (258x129)", Math.Abs(pic.Bounds.Width - 258) < 1.5 && Math.Abs(pic.Bounds.Height - 129) < 1.5, pic.Bounds);
        Harness.Find<ComboBox>(ip, "StretchBox").SelectedItem = Stretch.None;
        Harness.Pump();
        Harness.Check("Image: Stretch=None draws at the source size", Math.Abs(pic.DesiredSize.Width - 40) < 1.5 && Math.Abs(pic.DesiredSize.Height - 20) < 1.5, pic.DesiredSize);
        Harness.Find<ComboBox>(ip, "StretchBox").SelectedItem = Stretch.Fill;
        Harness.Pump();
        Harness.Check("Image: Stretch=Fill distorts to the full inner 258x138", Math.Abs(pic.Bounds.Width - 258) < 1.5 && Math.Abs(pic.Bounds.Height - 138) < 1.5, pic.Bounds);
        Harness.Find<ComboBox>(ip, "StretchBox").SelectedItem = Stretch.Uniform;
        Harness.Find<ComboBox>(ip, "DirectionBox").SelectedItem = StretchDirection.DownOnly;
        Harness.Pump();
        Harness.Check("Image: DownOnly never enlarges the 40x20 source", Math.Abs(pic.DesiredSize.Width - 40) < 1.5, pic.DesiredSize);
        Harness.Find<ComboBox>(ip, "InterpolationBox").SelectedItem = BitmapInterpolationMode.None;
        Harness.Pump();
        Harness.Check("Image: the box sets the interpolation mode", RenderOptions.GetBitmapInterpolationMode(pic) == BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode(pic));
        Harness.Check("Image: the readout reports the sizes", Harness.Find<TextBlock>(ip, "SizeText").Text!.StartsWith("源图 40×20"), Harness.Find<TextBlock>(ip, "SizeText").Text);
        w.Close();

        // ---- DrawingImage
        var dp = new DrawingImagePage();
        w = Harness.Show(dp);
        var badge = Harness.Find<Image>(dp, "Badge");
        Harness.Check("DrawingImage: the XAML resource is a 100x100 DrawingImage", badge.Source is DrawingImage di && Math.Abs(di.Size.Width - 100) < 1 && Math.Abs(badge.Bounds.Width - 24) < 1.5, badge.Source);
        var built = Harness.Find<Image>(dp, "Built");
        var drawing = ((DrawingImage)built.Source!).Drawing as GeometryDrawing;
        Harness.Check("DrawingImage: the code-built image starts OrangeRed", Equals(drawing!.Brush, Brushes.OrangeRed) && Harness.Find<TextBlock>(dp, "ColorText").Text!.StartsWith("Brush ="), drawing.Brush);
        Click(dp, "RecolorButton"); Harness.Pump();
        Harness.Check("DrawingImage: recolouring changes the same drawing's Brush", Equals(drawing.Brush, Brushes.SeaGreen), drawing.Brush);
        w.Close();

        // ---- PathIcon
        var pi = new PathIconPage();
        w = Harness.Show(pi);
        var icon = Harness.Find<PathIcon>(pi, "Icon");
        Harness.Check("PathIcon: the heart geometry is set and the size follows the slider", icon.Data is not null && icon.Width == 32 && icon.Height == 32, $"{icon.Width}");
        Harness.Find<Slider>(pi, "SizeSlider").Value = 64;
        Harness.Find<ComboBox>(pi, "ColorBox").SelectedIndex = 1;
        Harness.Find<ComboBox>(pi, "GeometryBox").SelectedIndex = 1;
        Harness.Pump();
        Harness.Check("PathIcon: the controls drive size, foreground and data", icon.Width == 64 && icon.Height == 64 && Equals(icon.Foreground, Brushes.SeaGreen)
            && Harness.Find<TextBlock>(pi, "SizeText").Text == "64", $"{icon.Width}/{icon.Foreground}");
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Fmm2.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeFmm
{
    static void Menus()
    {
        // ---- Menu: Click bubbles to the page, Radio groups are exclusive
        var mp = new MenuPage();
        var w = Harness.Show(mp);
        var bold = mp.FindControl<MenuItem>("BoldItem")!;
        var left = mp.FindControl<MenuItem>("LeftItem")!;
        var center = mp.FindControl<MenuItem>("CenterItem")!;
        var right = mp.FindControl<MenuItem>("RightItem")!;
        Harness.Check("Menu: Bold is a CheckBox item, alignment items are Radio", bold.ToggleType == MenuItemToggleType.CheckBox
            && left.ToggleType == MenuItemToggleType.Radio && left.IsChecked && !center.IsChecked, $"{bold.ToggleType}/{left.ToggleType}");
        Harness.Check("Menu: InputGesture is only the displayed hint", bold.InputGesture?.ToString() == "Ctrl+B", bold.InputGesture);
        bold.IsChecked = true;
        center.IsChecked = true;
        Harness.Pump();
        Harness.Check("Menu: choosing Centre clears Left (same GroupName)", center.IsChecked && !left.IsChecked && !right.IsChecked, $"{left.IsChecked}/{center.IsChecked}/{right.IsChecked}");
        // A closed sub-menu is not in the visual tree, so open it first, as a real click would have.
        ((MenuItem)mp.FindControl<MenuItem>("NewItem")!.Parent!).IsSubMenuOpen = true;
        Harness.Pump();
        mp.FindControl<MenuItem>("NewItem")!.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Harness.Pump();
        Harness.Check("Menu: a Click bubbles up to the page handler", Harness.Find<TextBlock>(mp, "LastText").Text == "最近点击：_新建", Harness.Find<TextBlock>(mp, "LastText").Text);
        w.Close();

        // ---- ContextMenu: Open() raises Opened but not Opening; Opening cancels only from the user path
        var cp = new ContextMenuPage();
        w = Harness.Show(cp);
        var area = Harness.Find<Border>(cp, "Area");
        var menu = area.ContextMenu!;
        Harness.Check("ContextMenu: attached to the area with 4 entries including a separator", menu.ItemCount == 4 && menu.Items.OfType<Separator>().Count() == 1, menu.ItemCount);
        Click(cp, "OpenButton"); Harness.Pump();
        Harness.Check("ContextMenu: Open() shows it and counts Opened, not Opening", menu.IsOpen
            && Harness.Find<TextBlock>(cp, "LogText").Text == "Opening 0 次，Opened 1 次，Closing 0 次", Harness.Find<TextBlock>(cp, "LogText").Text);
        Click(cp, "OpenButton"); Harness.Pump();
        Harness.Check("ContextMenu: Close() counts Closing", !menu.IsOpen && Harness.Find<TextBlock>(cp, "LogText").Text == "Opening 0 次，Opened 1 次，Closing 1 次", Harness.Find<TextBlock>(cp, "LogText").Text);
        w.Close();

        // ---- MenuFlyout
        var fp = new MenuFlyoutPage();
        w = Harness.Show(fp);
        var trigger = Harness.Find<Button>(fp, "Trigger");
        var mf = (MenuFlyout)trigger.Flyout!;
        Harness.Check("MenuFlyout: 3 entries plus a separator and Bottom placement by default", mf.Items.Count == 4 && mf.Placement == PlacementMode.Bottom, $"{mf.Items.Count}/{mf.Placement}");
        Click(fp, "ShowButton"); Harness.Pump();
        Harness.Check("MenuFlyout: ShowAt opens it and counts Opened", mf.IsOpen && Harness.Find<TextBlock>(fp, "LogText").Text == "Opened 1 次，Closed 0 次", Harness.Find<TextBlock>(fp, "LogText").Text);
        mf.Hide(); Harness.Pump();
        Harness.Check("MenuFlyout: Hide counts Closed", !mf.IsOpen && Harness.Find<TextBlock>(fp, "LogText").Text == "Opened 1 次，Closed 1 次", Harness.Find<TextBlock>(fp, "LogText").Text);
        Harness.Find<ComboBox>(fp, "PlacementBox").SelectedItem = PlacementMode.Right;
        Harness.Pump();
        Harness.Check("MenuFlyout: the box drives Placement", mf.Placement == PlacementMode.Right, mf.Placement);
        w.Close();

        // ---- NativeMenu: attaching and detaching leaves the window as it was
        var nm = new NativeMenuPage();
        w = Harness.Show(nm);
        var top = (TopLevel)w;
        Harness.Check("NativeMenu: nothing is attached to the window before the box is ticked", NativeMenu.GetMenu(top) is null);
        Harness.Check("NativeMenu: the built menu has one top-level item", nm.Built.Items.Count == 1, nm.Built.Items.Count);
        Harness.Find<CheckBox>(nm, "AttachCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("NativeMenu: ticking attaches it, and headless reports it is not exported",
            ReferenceEquals(NativeMenu.GetMenu(top), nm.Built) && Harness.Find<TextBlock>(nm, "ExportText").Text == "NativeMenu.GetIsNativeMenuExported = False", Harness.Find<TextBlock>(nm, "ExportText").Text);
        Harness.Find<CheckBox>(nm, "AttachCheck").IsChecked = false;
        Harness.Pump();
        Harness.Check("NativeMenu: unticking removes it again", NativeMenu.GetMenu(top) is null);
        w.Close();

        // ---- Separator
        var sp = new SeparatorPage();
        w = Harness.Show(sp);
        Harness.Check("Separator: a stand-alone one in a layout draws a line with width", Harness.Find<Separator>(sp, "Standalone").Bounds.Width > 100, Harness.Find<Separator>(sp, "Standalone").Bounds);
        w.Close();
    }

    static void HowTos()
    {
        // ---- Notifications how-to
        var nh = new NotificationsHowToPage();
        var w = Harness.Show(nh);
        var vm = (NotificationsHowToViewModel)nh.DataContext!;
        var list = Harness.Find<ItemsControl>(nh, "ToastList");
        Click(nh, "InfoButton"); Click(nh, "SuccessButton"); Click(nh, "StickyButton");
        Harness.Pump();
        Harness.Check("Notifications how-to: three toasts are drawn", vm.Toasts.Count == 3 && list.ItemCount == 3 && list.GetRealizedContainers().Count() == 3, $"{vm.Toasts.Count}/{list.ItemCount}");
        var borders = list.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("toast")).ToList();
        Harness.Check("Notifications how-to: the success and error toasts carry their classes",
            borders.Count == 3 && borders[1].Classes.Contains("success") && borders[2].Classes.Contains("error") && !borders[0].Classes.Contains("success"),
            string.Join("|", borders.Select(b => string.Join(",", b.Classes))));
        var paint = borders[1].Background as ISolidColorBrush;
        Harness.Check("Notifications how-to: the style, not a local value, paints the success toast green", paint?.Color == Color.Parse("#CC2E7D32"), paint?.Color);
        var dismiss = borders[2].GetVisualDescendants().OfType<Button>().First();
        dismiss.Command!.Execute(dismiss.CommandParameter);
        Harness.Pump();
        Harness.Check("Notifications how-to: the dismiss button removes only the sticky one", vm.Toasts.Count == 2 && vm.Toasts.All(t => t.Kind != "error"), vm.Toasts.Count);
        vm.Toasts.Clear();
        vm.Show("short", "info", 60);
        for (int i = 0; i < 6; i++) { System.Threading.Thread.Sleep(50); Harness.Pump(); }
        Harness.Check("Notifications how-to: a timed toast removes itself", vm.Toasts.Count == 0, vm.Toasts.Count);

        vm.ShowStatusCommand.Execute(null);
        Harness.Pump();
        var first = vm.StatusMessage;
        vm.ShowStatusCommand.Execute(null);
        Harness.Pump();
        Harness.Check("Notifications how-to: the status text shows a saved message", first.StartsWith("已保存") && Harness.Find<TextBlock>(nh, "StatusText").Text!.StartsWith("已保存"), first);
        var banner = Harness.Find<Border>(nh, "Banner");
        Harness.Check("Notifications how-to: the banner starts hidden", !banner.IsVisible);
        // RaiseEvent(ClickEvent) does not run Command, so check the wiring and run the command itself.
        var bannerButton = Harness.Find<Button>(nh, "BannerButton");
        bannerButton.Command!.Execute(null); Harness.Pump();
        Harness.Check("Notifications how-to: the button is wired to the show command and shows it", ReferenceEquals(bannerButton.Command, vm.ShowBannerNowCommand) && banner.IsVisible);
        vm.DismissBannerCommand.Execute(null); Harness.Pump();
        Harness.Check("Notifications how-to: the dismiss command hides it", !banner.IsVisible);
        w.Close();

        // ---- Image how-to
        var ih = new ImageHowToPage();
        w = Harness.Show(ih);
        var placeholder = Harness.Find<TextBlock>(ih, "PlaceholderText");
        Harness.Check("Image how-to: the avatar is set and the placeholder is visible at the start", Harness.Find<Image>(ih, "Avatar").Source is not null && placeholder.GetVisualAncestors().OfType<Border>().First().IsVisible);
        var loadButton = Harness.Find<Button>(ih, "LoadButton");
        loadButton.Command!.Execute(null); Harness.Pump();
        var photo = Harness.Find<Image>(ih, "PhotoImage");
        Harness.Check("Image how-to: Load puts a Bitmap on the image and hides the placeholder", photo.Source is Bitmap && !placeholder.GetVisualAncestors().OfType<Border>().First().IsVisible, photo.Source);
        Harness.Find<Button>(ih, "ClearButton").Command!.Execute(null); Harness.Pump();
        Harness.Check("Image how-to: Clear brings the placeholder back", photo.Source is null && placeholder.GetVisualAncestors().OfType<Border>().First().IsVisible);
        Click(ih, "ShotButton"); Harness.Pump();
        var cap = Harness.Find<Border>(ih, "Capture");
        var shot = Harness.Find<Image>(ih, "ShotImage").Source as RenderTargetBitmap;
        Harness.Check("Image how-to: the screenshot has the captured control's pixel size", shot is not null && shot.PixelSize.Width == (int)cap.Bounds.Width && shot.PixelSize.Height == (int)cap.Bounds.Height
            && Harness.Find<TextBlock>(ih, "ShotText").Text == $"已截图 {shot.PixelSize.Width}×{shot.PixelSize.Height}", shot?.PixelSize);
        w.Close();

        // ---- Menu how-to
        var mh = new MenuHowToPage();
        w = Harness.Show(mh);
        var mvm = (MenuHowToViewModel)mh.DataContext!;
        var recent = mh.FindControl<MenuItem>("RecentMenu")!;
        recent.IsSubMenuOpen = true; Harness.Pump();
        Harness.Check("Menu how-to: the recent menu lists 3 files", recent.ItemCount == 3, recent.ItemCount);
        var firstItem = (MenuItem)recent.ContainerFromIndex(0)!;
        Harness.Check("Menu how-to: each generated item carries the command and its own file as parameter",
            firstItem.Command == mvm.OpenRecentCommand && firstItem.CommandParameter == mvm.RecentFiles[0], $"{firstItem.Command}/{firstItem.CommandParameter}");
        firstItem.Command!.Execute(firstItem.CommandParameter);
        Harness.Check("Menu how-to: running it reports the file", mvm.LastAction == "打开 报告.docx", mvm.LastAction);
        mvm.ClearRecentCommand.Execute(null); Harness.Pump();
        Harness.Check("Menu how-to: an empty list shows one placeholder entry, greyed out by CanExecute",
            recent.ItemCount == 1 && recent.Items.Cast<object>().First() is EmptyRecent && !((MenuItem)recent.ContainerFromIndex(0)!).IsEffectivelyEnabled, recent.ItemCount);
        recent.IsSubMenuOpen = false;

        mh.KeyBindings.Count.ToString();
        Harness.Check("Menu how-to: the page registers a Ctrl+S key binding bound to Save", mh.KeyBindings.Count == 1 && mh.KeyBindings[0].Gesture.ToString() == "Ctrl+S", mh.KeyBindings.Count);
        mh.KeyBindings[0].Command!.Execute(null);
        Harness.Check("Menu how-to: the binding runs the same command as the menu item", mvm.LastAction.StartsWith("保存"), mvm.LastAction);

        var items = Harness.Find<ListBox>(mh, "Items");
        var cm = items.ContextMenu!;
        var del = (MenuItem)cm.Items.Cast<object>().First();
        cm.Open(items); Harness.Pump();
        Harness.Check("Menu how-to: with nothing selected the delete item is disabled", !del.IsEffectivelyEnabled, del.IsEffectivelyEnabled);
        cm.Close();
        items.SelectedItem = "面包"; Harness.Pump();
        cm.Open(items); Harness.Pump();
        Harness.Check("Menu how-to: the parameter is the selected item and the item is enabled", del.CommandParameter as string == "面包" && del.IsEffectivelyEnabled, $"{del.CommandParameter}/{del.IsEffectivelyEnabled}");
        del.Command!.Execute(del.CommandParameter);
        Harness.Check("Menu how-to: deleting removes it from the list", !mvm.Items.Contains("面包") && mvm.Items.Count == 2, mvm.Items.Count);
        cm.Close();
        w.Close();
    }
}
```

同时把 `C:\Temp\probe-controls\Program.cs` 的 `Main` 改为依次调用 `ProbeShell.Run(); ProbeButtons.Run(); ProbeInput.Run(); ProbeLayout.Run(); ProbeData.Run(); ProbeFmm.Run(); ProbePackages.Run(); ProbeNewPages.Run();`。

- [ ] **Step 3: 跑探针**

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "FAIL|error|Unhandled|passed"`
Expected: `0 failed`、`0 warning log(s)`（编写计划时实测为 **591 passed**，其中第 00–03 份的 484 条在内，本份新增 107 条）。

**失败时的排查顺序**：
1. `Notification` 的卡片数不对：被挤出的卡片先淡出，要等约 0.8 秒，并只数 `!IsClosing` 的；`CloseAll()` 对还在滑入的卡片无效，调用前先等 1 秒；
2. `ProgressBar` 读到 `Percentage = 0%`：它在布局之后才算出，读数要监听 `PercentageProperty`；
3. `MenuPage` 的 `Click` 没冒泡到页面：子菜单没打开时项不在可视树里，探针要先 `IsSubMenuOpen = true`；
4. `MenuFlyoutPage` 编译报 `CS0103` / `CS0120`：`Button.Flyout` 里的元素不进命名范围，字段要自己从 `Trigger.Flyout` 取，且别把字段叫 `Flyout`（会和类型同名）；
5. 动态菜单抛 `ArgumentException ... requires an argument of type RecentEntry`：样式选择器命中了根菜单项，用 `MenuItem MenuItem`；
6. 带 `Command` 的按钮在探针里"点了没反应"：`RaiseEvent(ClickEvent)` 不执行命令，直接调 `Command.Execute`；
7. 任何页面 `renders` 失败且无异常：对照第 00 份「Buttons 页面的共同写法」检查骨架。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|warn|个错误|Build succeeded" | tail -5`
Expected: 0 个错误、无新增警告。

- [ ] **Step 5: 真实窗口目视（人工，一次）**

```bash
dotnet run --project Avalonia.ControlsDemo
```

确认：Feedback、Media、Menus 三个分类下共 15 个条目都能切换；`Notification` 页窗口右上角确实弹出通知，换 `Position` 后位置变化，点击"可点击的通知"后计数加一；`Popup` 页打开后点外面，勾了 `IsLightDismissEnabled` 才会关；`ToolTip` 页悬停约 `ShowDelay` 毫秒后出现提示，禁用按钮也能提示；`Menu` 页 Alt+F 打开文件菜单，`ContextMenu` 页在区域内右键能弹出，并且勾上"取消"后右键不再弹；`MenuFlyout` 页点按钮弹出菜单；`Menu` 实战页按 Ctrl+S 会更新"最近操作"，`Notifications` 实战页的常驻通知需要手动关闭，状态栏连点两次后第二条不会被提前清掉。鼠标悬停、右键、访问键、动画与真实窗口里的通知位置是 headless 验证不了的部分。

- [ ] **Step 6: 回填 spec**

在 spec「待验证的技术风险」末尾的「批 3 实测结论」之后追加（若「批 2」「批 3」两段还没回填，先按第 02、03 份 Step 6 补上）：

```markdown
### 批 4 实测结论（填入执行日期）

- **`WindowNotificationManager` 的两个时间坑**：超过 `MaxItems` 时，被挤出的卡片先淡出再移除，探针只能数"非关闭中"的卡片；`CloseAll()` 对还在滑入的卡片（发出后约 0.5 秒内）无效，要等稳定后调用。管理器必须在 `AttachedToVisualTree` 里用 `TopLevel` 创建，离开页面时 `CloseAll()`。
- **`ProgressBar.Percentage` 在布局前是 0**：读数要监听 `PercentageProperty`，不要在设 `Value` 后立即读。
- **headless 的位图限制**：`WriteableBitmap` 能写像素，但 `Save` 得 0 字节、`new Bitmap(stream)` 得 1×1；示例图一律用代码生成，不依赖解码。`Bitmap.DecodeToWidth` 与 `RenderTargetBitmap` 的尺寸是对的。
- **命令不走 `Click` 事件**：`RaiseEvent(Button.ClickEvent)` 只触发 `Click`，不执行 `Command`；菜单项的 `ClickEvent` 在子菜单没打开时到不了页面。探针对命令直接 `Execute`，对菜单项先 `IsSubMenuOpen = true`。
- **菜单样式会命中根项**：给 `MenuItem` 写套命令的样式会命中根菜单项自己，要写 `MenuItem MenuItem`；绑定类型要用真实项与占位项的共同基类。
- **`Button.Flyout` 里的元素不进命名范围**：写 `Name` 不生成字段，要从 `Trigger.Flyout` 取。`ContextMenu.Open()` 不触发 `Opening`（只有用户右键才触发）。
- **范围缺口**：官方 `media/mediaplayer/**` 属付费，归第 06 份的 Premium 路标页。
- **官方 How-to 与 12.1.2 的出入**：Notifications 指南称没有内置通知控件，但 `WindowNotificationManager` / `Notification` 存在；实战页演示自建的另一半。
- **探针断言实际条数**：本批累计 591 条（含第 00–03 份），全部通过且零警告日志。
```

尖括号与"填入执行日期"必须替换为实际日期；数字以 Step 3 的真实输出为准。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: complete Feedback, Media and Menus with 12 control pages and 3 how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review（编写者自查记录）

**Spec 覆盖**：Feedback 4（Notification、Popup、ProgressBar、ToolTip）、Media 3（Image、DrawingImage、PathIcon）、Menus 5（Menu、ContextMenu、MenuFlyout、NativeMenu、Separator）共 12 页 → Task 1–3；3 篇 How-to（Notifications、Image、Menu）→ Task 4；官方侧边栏顺序与 HowTo 紧跟 → Task 5 Step 1 与探针的"紧跟"断言；`MediaPlayer` → 明确归第 06 份。

**已实测，不是推断**：本份全部页面、模型与 ViewModel 已从计划原文抽取到 `C:\Temp\plan-verify`，构建 0 错误 0 警告，探针 591 条通过、零警告日志。构建与实测时发现并已回写到上面代码里的有：`BitmapInterpolationMode` 的命名空间是 `Avalonia.Media.Imaging`；`Button.Flyout` 里的 `MenuFlyout` 不进命名范围，且字段名不能叫 `Flyout`；`VisualTreeAttachmentEventArgs.Root` 已过时；`ProgressBar.Percentage` 布局前为 0；`CloseAll()` 对滑入中的卡片无效；动态菜单的样式会命中根项；`RaiseEvent(ClickEvent)` 不执行命令。

**类型一致性**：元素名与 Task 1–4 的 Interfaces 列表一致；`RecentEntry` / `RecentFile` / `EmptyRecent`、`ToastItem`、`DemoImages.Gradient` 在 ViewModel、XAML 与探针里一致；`ItemsFlyout` 是页面的私有属性，探针经 `Trigger.Flyout` 取，不依赖它。

**未覆盖，已知**：真实窗口里的通知位置与滑入动画、`ToolTip` 的悬停延时、右键弹出与访问键、`NativeMenu` 在 macOS 上的实际导出、`Image` 从文件 / 网络加载与 `Save` 成 PNG（headless 的位图编解码不可用）；这些在 Step 5 目视或留给真机。
