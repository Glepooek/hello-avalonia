# ControlsDemo 02：Layout 20 个控件页与 3 篇实战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全 `Avalonia.ControlsDemo` 的 Layout 分类：Containers 9 页、Decorator 与 LayoutTransformControl 2 页、Panels 9 页，共 20 个 Control 页；加 Grid、ScrollViewer、Expander 3 篇 How-to 实战页。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份只新增页面文件、`PageCatalog.Layout.cs` 的登记行与探针 `Probe.Layout.cs`；不引入新包，不改 `App.axaml`。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2（仅 Expander 实战页用到 ViewModel）

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00、01 份已执行（`PageCatalog`、`Categories.Layout`、`Harness` 已存在）。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」与第 01 份「本份新增的已核实事实」之外的约定，**不重复贴**，执行者必须先读第 00 份。本份额外强调：

- 页面类名 `<控件>Page`，How-to 页 `<主题>HowToPage`；命名空间一律 `Avalonia.ControlsDemo.Views.Pages`，ViewModel 一律 `Avalonia.ControlsDemo.ViewModels`
- `DocPath` 已用官方 `sitemap.xml` 核对：控件页 `controls/layout/...`，How-to 页 `docs/how-to/<主题>-how-to`
- **事件接线放在构造函数里、`InitializeComponent()` 之后**（第 01 份同一条）；需要响应用户操作的事件一律在 code-behind 订阅
- **会被样式驱动的属性不在元素上写本地值**（规则 1）。`GridSplitter` 的外观通过页面级 `<Style Selector="GridSplitter">` 给，不在元素上写 `Background`
- 去重规则：Canvas、DockPanel、Grid、Panel、RelativePanel、StackPanel、UniformGrid、WrapPanel 在 `Avalonia.LayoutDemo`「布局面板」Tab 已有对比演示，Border 与对齐间距在「定位与间距」Tab；这些页末尾各加一行 `更深入的演示：Avalonia.LayoutDemo →「…」`。其余页面不写指向（未核实过的 Tab 名不写）
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时在 Avalonia 12.1.2 headless 实测）

| 控件 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `GridSplitter` | 鼠标按下—右移 60—抬起：第 0 列从 200 变 260，`ColumnDefinition.Width` 随之变成 260；键盘聚焦后按右方向键 +10（`KeyboardIncrement=10`） | 页面用读数显示列宽；探针既测拖动也测键盘 |
| `Viewbox` | `Stretch.Uniform`、300×300 容器、100×50 子元素：子元素在容器内的右边缘 x=300（缩放 3 倍）；`Stretch.None` 时为 100；**子元素的 `Bounds` 始终是 100×50，不反映缩放** | 缩放倍数要用 `TransformToVisual` 算，不能读 `Bounds` |
| `LayoutTransformControl` / `RenderTransform` | 同样旋转 90°：`LayoutTransformControl` 的布局盒变成 40×100（参与布局），`RenderTransform` 的宿主仍是 100×40（只画不排） | 页面并排摆两种，读数显示各自宿主的 `Bounds` |
| `Decorator` | `Padding=10` 包住 50×20 的子元素，自己占 70×40；**`Decorator` 本身不画任何东西** | 页面用外层 `Border` 描边来显示它占的盒子 |
| `ScrollViewer` | 新建时 `HorizontalScrollBarVisibility=Disabled`、`VerticalScrollBarVisibility=Auto`；`ScrollToEnd()` 后 `Offset=(0,1800)`（内容 2000、视口 200）；**`Offset=5000` 被夹取到 1800**；`ScrollToHome()` 回 `(0,0)`；`LineDown()` +16；每次写 `Offset` 触发 `ScrollChanged` 一次 | 页面的"回到顶部/底部/下一行"按钮直接调这些方法 |
| `RefreshContainer` | `RequestRefresh()` 触发 `RefreshRequested` 一次；`PullDirection` 默认 `TopToBottom`；`IsMouseEnabled` 默认 `False`（鼠标拖不动，只有触摸能下拉） | 页面提供"请求刷新"按钮，并说明鼠标默认不能下拉 |
| `PipsPager` | `SelectedPageIndex=3` 触发 `SelectedIndexChanged` 一次；模板里有 2 个 `Button`（上一页、下一页） | 页面读 `SelectedPageIndex` |
| `SplitView` | `CompactInline` 且关闭时窗格宽 = `CompactPaneLength`（48）；展开有过渡动画，短时间内读到的是中间值 | 探针断言 `IsPaneOpen` 与关闭宽度，不断言展开后的像素 |
| `Flyout` | `ShowAt(button)` 后 `IsOpen=True`、`Opened` 触发一次；`Hide()` 后 `IsOpen=False` | 可直接断言 |
| `Grid`（写完页面后补测） | 360 宽、`80,Auto,*,2*`：`ColumnDefinition.ActualWidth` 为 80/72/69/139；设 `ColumnSpacing=10` 后变成 90/82/69/129——**`ActualWidth` 含间距**，而子元素 `Bounds.Width` 是 80/72/59/119（比例仍是 1:2） | 断言星号比例要读子元素 `Bounds`，不读 `ActualWidth` |
| `RelativePanel`（写完页面后补测） | 跟随者 `RightOf=Anchor` 再加 `AlignRightWithPanel=True`：340 宽面板里 x=170（两条约束之间居中），**不贴右边**；只有 `AlignRightWithPanel` 的元素 `Right`=面板宽 | 页面另放一个只写 `AlignRightWithPanel` 的 `Edge` 作对照 |
| `Viewbox`（补测） | 外层 `Border` 320×140、`BorderThickness=1`：可用区 318×138，`Viewbox.Bounds` 为 276×138（`Uniform` 缩到 138/50=2.76，居中） | 缩放读数是 2.76 倍，不是 2.8 |
| `SplitView`（补测） | 关闭时模板里的窄条是 `Panel#PART_PaneRoot`（48 宽），不是 `Border`；`TemplateSettings.ClosedPaneWidth`=48 | 探针按 `PART_PaneRoot` 找 |
| `Expander` | 代码写 `IsExpanded=true`：**`Expanding` 触发 3 次**后 `Expanded` 触发 1 次；写 `false`：`Collapsing` 3 次、`Collapsed` 1 次；点内部 `ToggleButton`：`Expanding` 2 次、`Expanded` 1 次；**`IsEnabled=false` 时代码仍能写 `IsExpanded`** | 页面只数 `Expanded` / `Collapsed`；说明 `Expanding` / `Collapsing` 不要拿来计数 |

**成员核实（反射，Avalonia 12.1.2）**：`Border` 有 `BoxShadow`（`BoxShadows`）、`BackgroundSizing`、`ClipToBoundsRadius`；`Expander` 有 `ExpandDirection`（`Down/Up/Left/Right`）、`ContentTransition`、事件 `Expanding/Expanded/Collapsing/Collapsed`；`GroupBox` 没有任何自己的属性（继承 `HeaderedContentControl` 的 `Header`/`Content`）；`PipsPager` 有 `NumberOfPages`、`MaxVisiblePips`、`Orientation`、`SelectedPageIndex`、`IsPreviousButtonVisible`、`IsNextButtonVisible`；`RefreshContainer` 有 `PullDirection`、`IsMouseEnabled`、`RequestRefresh()`、`RefreshRequested`，事件参数 `RefreshRequestedEventArgs.GetDeferral()`；`SplitView` 有 `DisplayMode`（`Inline/CompactInline/Overlay/CompactOverlay`）、`PanePlacement`、`CompactPaneLength`、`OpenPaneLength`、`PaneBackground`、`UseLightDismissOverlayMode`、事件 `PaneOpening/PaneOpened/PaneClosing/PaneClosed`；`Viewbox` 有 `Stretch`、`StretchDirection`；`DockPanel` 有 `LastChildFill`、`HorizontalSpacing`、`VerticalSpacing`；`Grid` 有 `ShowGridLines`、`RowSpacing`、`ColumnSpacing`，附加属性 `Row/Column/RowSpan/ColumnSpan/IsSharedSizeScope`；`GridSplitter` 有 `ResizeDirection`（`Auto/Columns/Rows`）、`ResizeBehavior`（`BasedOnAlignment/CurrentAndNext/PreviousAndCurrent/PreviousAndNext`）、`ShowsPreview`、`KeyboardIncrement`、`DragIncrement`；`StackPanel` 有 `Spacing`、`Orientation`；`UniformGrid`（`Avalonia.Controls.Primitives`）有 `Rows`、`Columns`、`FirstColumn`、`RowSpacing`、`ColumnSpacing`；`WrapPanel` 有 `ItemSpacing`、`LineSpacing`、`Orientation`、`ItemsAlignment`（`Start/Center/End`）、`ItemWidth`、`ItemHeight`；`RelativePanel` 的 16 个附加属性有 `Above`、`Below`、`LeftOf`、`RightOf`、`AlignLeftWith`、`AlignLeftWithPanel` 等；`ScrollViewer` 有 `LineUp/LineDown/PageUp/PageDown/ScrollToHome/ScrollToEnd`、`IsScrollChainingEnabled`、`AllowAutoHide`。

**官方 How-to 要点（已抓取）**：Grid 指南讲行列定义（像素、`Auto`、星号）、`MinWidth/MaxWidth`、`SharedSizeGroup` + `Grid.IsSharedSizeScope`、`RowSpacing/ColumnSpacing`、`RowSpan/ColumnSpan`、同格叠放与 `ZIndex`、`ShowGridLines`；ScrollViewer 指南讲滚动条可见性、`Offset` 编程滚动、`ScrollChanged` 触底加载、嵌套与 `IsScrollChainingEnabled`、吸顶头部；Expander 指南讲 `IsExpanded` 绑定、`ExpandDirection`、自定义头部、手风琴（单开）、`ContentTransition`、`:disabled`。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Navigation/PageCatalog.Layout.cs      改：+20 控件页 +3 How-to 页
├── ViewModels/ExpanderHowToViewModel.cs  新
└── Views/Pages/Layout/                   新增 23 个页面（各含 .axaml 与 .axaml.cs）
    Border / Expander / Flyout / GroupBox / PipsPager / RefreshContainer / ScrollViewer / SplitView / Viewbox
    Decorator / LayoutTransformControl
    Canvas / DockPanel / Grid / GridSplitter / Panel / RelativePanel / StackPanel / UniformGrid / WrapPanel
    GridHowTo / ScrollViewerHowTo / ExpanderHowTo
```

探针（仓库外）：`C:\Temp\probe-controls\Probe.Layout.cs`（及 `Probe.Layout2.cs`）。

### 页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 页面骨架（`UserControl` + `ScrollViewer` + `StackPanel Margin="12"` + `DemoHeader`）与第 00 份相同；演示块用 `TextBlock.caption` + `Border.stage` + `TextBlock.hint`。
- 枚举下拉一律在 code-behind 构造函数里 `ItemsSource = Enum.GetValues<T>()`，先设 `SelectedItem` 再订阅 `SelectionChanged`。
- 滑块驱动的整数属性在处理器里强转，不走转换器。
- **页面里嵌套 `ScrollViewer` 的坑**：每个页面最外层已经是 `ScrollViewer`，演示用的 `ScrollViewer` 必须给固定 `Height`，否则它会撑满内容、永远不出滚动条。

---

### Task 1: Border、GroupBox、Decorator、LayoutTransformControl、Viewbox

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Layout/BorderPage.axaml`、`.axaml.cs`
- Create: `.../GroupBoxPage.axaml`、`.axaml.cs`
- Create: `.../DecoratorPage.axaml`、`.axaml.cs`
- Create: `.../LayoutTransformControlPage.axaml`、`.axaml.cs`
- Create: `.../ViewboxPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`
- Produces（探针依赖的元素名）：
  - `BorderPage`：`ThicknessSlider`、`RadiusSlider`、`Target`（`Border`）
  - `GroupBoxPage`：`Box`（`GroupBox`）
  - `DecoratorPage`：`PaddedDecorator`（`Decorator`）、`PaddingSlider`、`SizeText`
  - `LayoutTransformControlPage`：`LayoutHost`（`LayoutTransformControl`）、`RenderHost`（`Decorator`）、`AngleSlider`、`LayoutSize`、`RenderSize`
  - `ViewboxPage`：`Box`（`Viewbox`）、`Inner`（`Border`）、`StretchBox`（`ComboBox`）、`ScaleText`

页面里的数值滑块都在 code-behind 订阅 `PropertyChanged`，不走转换器（`Thickness` / `CornerRadius` 是结构体，规则 3）。

#### `Avalonia.ControlsDemo/Views/Pages/Layout/BorderPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.BorderPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Border：给单个子元素加边框与背景"
                               DocPath="controls/layout/containers/border" />

            <TextBlock Classes="caption" Text="1. BorderThickness、CornerRadius 与 Padding" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Border Name="Target" Width="260" Height="70" BorderBrush="Orange" Padding="12"
                            Background="#33FFA500">
                        <TextBlock VerticalAlignment="Center" Text="我被 Border 包着" />
                    </Border>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="110" VerticalAlignment="Center" Text="BorderThickness" />
                        <Slider Name="ThicknessSlider" Width="200" Minimum="0" Maximum="12" Value="2" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="110" VerticalAlignment="Center" Text="CornerRadius" />
                        <Slider Name="RadiusSlider" Width="200" Minimum="0" Maximum="35" Value="8" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Border 只能有一个子元素；多个元素先放进一个面板再交给它。" />

            <TextBlock Classes="caption" Text="2. 四边不同粗细、四角不同圆角" />
            <Border Classes="stage" Padding="12">
                <Border Width="260" Height="60" BorderBrush="SteelBlue" BorderThickness="1,4,1,0"
                        CornerRadius="20,0,20,0" Background="#334682B4" />
            </Border>

            <TextBlock Classes="caption" Text="3. BoxShadow：阴影" />
            <Border Classes="stage" Padding="24">
                <Border Width="200" Height="60" CornerRadius="8" Background="#2D2D30"
                        BoxShadow="0 4 16 0 #80000000" />
            </Border>
            <TextBlock Classes="hint" Text="阴影格式：水平偏移 垂直偏移 模糊半径 扩展半径 颜色，多个阴影用逗号分隔。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「定位与间距」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/BorderPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class BorderPage : UserControl
    {
        public BorderPage()
        {
            InitializeComponent();

            // Thickness and CornerRadius are structs, so a double slider cannot bind to them directly (rule 3).
            ThicknessSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Target.BorderThickness = new Thickness(ThicknessSlider.Value);
                }
            };
            RadiusSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Target.CornerRadius = new CornerRadius(RadiusSlider.Value);
                }
            };

            // Applied after wiring so the starting slider values are honoured once, from code.
            Target.BorderThickness = new Thickness(ThicknessSlider.Value);
            Target.CornerRadius = new CornerRadius(RadiusSlider.Value);
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GroupBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.GroupBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="GroupBox：带标题的分组框"
                               DocPath="controls/layout/containers/groupbox" />

            <TextBlock Classes="caption" Text="Header 与 Content" />
            <Border Classes="stage" Padding="12">
                <GroupBox Name="Box" Header="登录方式" Width="280">
                    <StackPanel Spacing="4">
                        <RadioButton GroupName="Login" Content="账号密码" IsChecked="True" />
                        <RadioButton GroupName="Login" Content="手机验证码" />
                        <RadioButton GroupName="Login" Content="扫码" />
                    </StackPanel>
                </GroupBox>
            </Border>
            <TextBlock Classes="hint" Text="GroupBox 没有自己的属性，只是 HeaderedContentControl：Header 可以是任意对象，Content 只能放一个子元素。" />

            <TextBlock Classes="caption" Text="Header 放自定义内容" />
            <Border Classes="stage" Padding="12">
                <GroupBox Width="280">
                    <GroupBox.Header>
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <TextBlock Text="⚙" />
                            <TextBlock FontWeight="SemiBold" Text="高级设置" />
                        </StackPanel>
                    </GroupBox.Header>
                    <CheckBox Content="启用实验功能" />
                </GroupBox>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GroupBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GroupBoxPage : UserControl
    {
        public GroupBoxPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/DecoratorPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DecoratorPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Decorator：只包一个子元素的基类"
                               DocPath="controls/layout/decorator" />

            <TextBlock Classes="caption" Text="Padding 让 Decorator 比子元素大一圈，但它自己什么都不画" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <!--  The outer Border only outlines the box the Decorator occupies; Decorator itself draws nothing.  -->
                    <Border BorderBrush="Gray" BorderThickness="1" HorizontalAlignment="Left">
                        <Decorator Name="PaddedDecorator">
                            <Border Width="120" Height="40" Background="SteelBlue" />
                        </Decorator>
                    </Border>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="60" VerticalAlignment="Center" Text="Padding" />
                        <Slider Name="PaddingSlider" Width="200" Minimum="0" Maximum="40" Value="10" />
                        <TextBlock Name="SizeText" VerticalAlignment="Center" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Border、Viewbox、LayoutTransformControl 都派生自 Decorator；自己写「只包一个子元素」的控件时从它继承最省事。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/DecoratorPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DecoratorPage : UserControl
    {
        public DecoratorPage()
        {
            InitializeComponent();

            PaddingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Apply();
                }
            };
            PaddedDecorator.LayoutUpdated += (_, _) => SizeText.Text = $"占 {PaddedDecorator.Bounds.Width:F0} × {PaddedDecorator.Bounds.Height:F0}";
            Apply();
        }

        private void Apply() => PaddedDecorator.Padding = new Thickness(PaddingSlider.Value);
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/LayoutTransformControlPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.LayoutTransformControlPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="LayoutTransformControl：参与布局的变换"
                               DocPath="controls/layout/layouttransformcontrol" />

            <TextBlock Classes="caption" Text="同样旋转：LayoutTransform 占据旋转后的空间，RenderTransform 只改画法" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="60" VerticalAlignment="Center" Text="角度" />
                        <Slider Name="AngleSlider" Width="240" Minimum="0" Maximum="90" Value="90" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="40">
                        <StackPanel Spacing="4">
                            <TextBlock Classes="hint" Text="LayoutTransformControl" />
                            <Border BorderBrush="Gray" BorderThickness="1" HorizontalAlignment="Left">
                                <LayoutTransformControl Name="LayoutHost">
                                    <Border Width="100" Height="40" Background="SeaGreen">
                                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="布局变换" />
                                    </Border>
                                </LayoutTransformControl>
                            </Border>
                            <TextBlock Name="LayoutSize" />
                        </StackPanel>
                        <StackPanel Spacing="4">
                            <TextBlock Classes="hint" Text="RenderTransform" />
                            <Border BorderBrush="Gray" BorderThickness="1" HorizontalAlignment="Left">
                                <Decorator Name="RenderHost">
                                    <Border Name="RenderChild" Width="100" Height="40" Background="Chocolate">
                                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="渲染变换" />
                                    </Border>
                                </Decorator>
                            </Border>
                            <TextBlock Name="RenderSize" />
                        </StackPanel>
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="灰色框是宿主占的空间：左边随旋转变高变窄，右边始终是 100×40。竖排标签、旋转的表头用 LayoutTransformControl。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.GraphicsDemo →「变换」（Render 与 Layout 的对照）" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/LayoutTransformControlPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class LayoutTransformControlPage : UserControl
    {
        public LayoutTransformControlPage()
        {
            InitializeComponent();

            AngleSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Apply();
                }
            };
            LayoutHost.LayoutUpdated += (_, _) => LayoutSize.Text = $"宿主 {LayoutHost.Bounds.Width:F0} × {LayoutHost.Bounds.Height:F0}";
            RenderHost.LayoutUpdated += (_, _) => RenderSize.Text = $"宿主 {RenderHost.Bounds.Width:F0} × {RenderHost.Bounds.Height:F0}";
            Apply();
        }

        private void Apply()
        {
            LayoutHost.LayoutTransform = new RotateTransform(AngleSlider.Value);
            RenderChild.RenderTransform = new RotateTransform(AngleSlider.Value);
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ViewboxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ViewboxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Viewbox：把子元素缩放到可用空间"
                               DocPath="controls/layout/containers/viewbox" />

            <TextBlock Classes="caption" Text="Stretch 与 StretchDirection" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Stretch" />
                        <ComboBox Name="StretchBox" Width="140" />
                        <TextBlock Name="ScaleText" VerticalAlignment="Center" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" Width="320" Height="140" HorizontalAlignment="Left">
                        <Viewbox Name="Box">
                            <Border Name="Inner" Width="100" Height="50" Background="SteelBlue">
                                <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="100 × 50" />
                            </Border>
                        </Viewbox>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="子元素的 Bounds 永远是 100×50，缩放发生在渲染变换里，所以倍数要靠它在容器里的右边缘位置算出来。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ViewboxPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ViewboxPage : UserControl
    {
        public ViewboxPage()
        {
            InitializeComponent();

            StretchBox.ItemsSource = Enum.GetValues<Stretch>();
            StretchBox.SelectedItem = Box.Stretch;
            StretchBox.SelectionChanged += (_, _) =>
            {
                if (StretchBox.SelectedItem is Stretch stretch)
                {
                    Box.Stretch = stretch;
                }
            };

            Box.LayoutUpdated += (_, _) =>
            {
                // The child's Bounds never change; the scale shows in where its right edge lands inside the Viewbox.
                var right = Inner.TransformToVisual(Box)?.Transform(new Point(Inner.Bounds.Width, 0)).X;
                ScaleText.Text = right is { } x ? $"水平缩放约 {x / Inner.Bounds.Width:F2} 倍" : string.Empty;
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
git commit -m "feat: demonstrate Border, GroupBox, Decorator, LayoutTransformControl and Viewbox

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: Expander、Flyout、PipsPager、RefreshContainer

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Layout/ExpanderPage.axaml`、`.axaml.cs`
- Create: `.../FlyoutPage.axaml`、`.axaml.cs`
- Create: `.../PipsPagerPage.axaml`、`.axaml.cs`
- Create: `.../RefreshContainerPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖的元素名）：
  - `ExpanderPage`：`Basic`、`Sideways`（`Expander`）、`DirectionBox`（`ComboBox`）、`BasicResult`、`EnabledCheck`
  - `FlyoutPage`：`OpenButton`（`Button`）、`FlyoutResult`
  - `PipsPagerPage`：`Pager`（`PipsPager`）、`PagesSlider`、`PageText`、`PreviousCheck`、`NextCheck`
  - `RefreshContainerPage`：`Container`（`RefreshContainer`）、`RefreshButton`、`RefreshResult`

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ExpanderPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ExpanderPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Expander：可折叠区域"
                               DocPath="controls/layout/containers/expander" />

            <TextBlock Classes="caption" Text="1. Header、IsExpanded 与 Expanded / Collapsed 事件" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Expander Name="Basic" Header="高级选项" Width="320">
                        <StackPanel Spacing="4">
                            <CheckBox Content="启用日志" />
                            <CheckBox Content="启用遥测" />
                        </StackPanel>
                    </Expander>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="EnabledCheck" Content="IsEnabled" IsChecked="True" />
                        <TextBlock Name="BasicResult" VerticalAlignment="Center" Text="已展开 0 次，已折叠 0 次" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="事件只数 Expanded / Collapsed：Expanding 与 Collapsing 在一次动作里会触发多次。IsEnabled=False 能挡住鼠标，挡不住代码写 IsExpanded。" />

            <TextBlock Classes="caption" Text="2. ExpandDirection：向四个方向展开" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <ComboBox Name="DirectionBox" Width="140" />
                    <Expander Name="Sideways" Header="方向" HorizontalAlignment="Left" VerticalAlignment="Top">
                        <TextBlock Margin="8" Text="展开的内容" />
                    </Expander>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="选 Up 时要给 Expander 留出向上生长的空间（VerticalAlignment=Bottom）；这里固定在上方，Up 会向外溢出，仅作演示。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ExpanderPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ExpanderPage : UserControl
    {
        private int _expanded, _collapsed;

        public ExpanderPage()
        {
            InitializeComponent();

            Basic.Expanded += (_, _) => { _expanded++; ShowCounts(); };
            Basic.Collapsed += (_, _) => { _collapsed++; ShowCounts(); };
            EnabledCheck.IsCheckedChanged += (_, _) => Basic.IsEnabled = EnabledCheck.IsChecked == true;

            DirectionBox.ItemsSource = Enum.GetValues<ExpandDirection>();
            DirectionBox.SelectedItem = Sideways.ExpandDirection;
            DirectionBox.SelectionChanged += (_, _) =>
            {
                if (DirectionBox.SelectedItem is ExpandDirection direction)
                {
                    Sideways.ExpandDirection = direction;
                }
            };
        }

        private void ShowCounts() => BasicResult.Text = $"已展开 {_expanded} 次，已折叠 {_collapsed} 次";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/FlyoutPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.FlyoutPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Flyout：附着在控件上的浮出层"
                               DocPath="controls/layout/containers/flyout" />

            <TextBlock Classes="caption" Text="1. Button.Flyout：点击按钮时弹出" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Button Name="OpenButton" Content="打开 Flyout">
                        <Button.Flyout>
                            <Flyout Placement="Bottom">
                                <StackPanel Spacing="6" Width="200">
                                    <TextBlock FontWeight="SemiBold" Text="确认删除？" />
                                    <Button Name="ConfirmButton" Content="确认" HorizontalAlignment="Stretch" />
                                </StackPanel>
                            </Flyout>
                        </Button.Flyout>
                    </Button>
                    <TextBlock Name="FlyoutResult" VerticalAlignment="Center" Text="尚未操作" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. Placement：在目标的哪一侧出现" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Button Content="上">
                        <Button.Flyout><Flyout Placement="Top"><TextBlock Text="在上方" /></Flyout></Button.Flyout>
                    </Button>
                    <Button Content="右">
                        <Button.Flyout><Flyout Placement="Right"><TextBlock Text="在右侧" /></Flyout></Button.Flyout>
                    </Button>
                    <Button Content="下">
                        <Button.Flyout><Flyout Placement="Bottom"><TextBlock Text="在下方" /></Flyout></Button.Flyout>
                    </Button>
                    <Button Content="左">
                        <Button.Flyout><Flyout Placement="Left"><TextBlock Text="在左侧" /></Flyout></Button.Flyout>
                    </Button>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Flyout 只能放一个子元素；需要菜单样式的用 MenuFlyout（见 Menus）。弹出层不在页面的可视树里，所以按钮只能靠 Button.Flyout 找到它。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.CustomControlsDemo →「自定义 Flyout」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/FlyoutPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class FlyoutPage : UserControl
    {
        public FlyoutPage()
        {
            InitializeComponent();

            if (OpenButton.Flyout is Flyout flyout)
            {
                flyout.Opened += (_, _) => FlyoutResult.Text = "Flyout 已打开";
                flyout.Closed += (_, _) => FlyoutResult.Text = "Flyout 已关闭";
            }
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/PipsPagerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.PipsPagerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="PipsPager：圆点分页指示器"
                               DocPath="controls/layout/containers/pipspager" />

            <TextBlock Classes="caption" Text="NumberOfPages、SelectedPageIndex、上一页 / 下一页按钮" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <PipsPager Name="Pager" NumberOfPages="5" MaxVisiblePips="5" HorizontalAlignment="Left" />
                    <TextBlock Name="PageText" Text="第 1 页" />
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="80" VerticalAlignment="Center" Text="NumberOfPages" />
                        <Slider Name="PagesSlider" Width="200" Minimum="2" Maximum="12" Value="5" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="16">
                        <CheckBox Name="PreviousCheck" Content="IsPreviousButtonVisible" />
                        <CheckBox Name="NextCheck" Content="IsNextButtonVisible" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="PipsPager 只显示「第几页」，不管页面内容；配合 Carousel 或自己的切换逻辑使用。上一页 / 下一页按钮默认隐藏。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/PipsPagerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PipsPagerPage : UserControl
    {
        public PipsPagerPage()
        {
            InitializeComponent();

            Pager.SelectedIndexChanged += (_, _) => PageText.Text = $"第 {Pager.SelectedPageIndex + 1} 页";
            PagesSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Pager.NumberOfPages = (int)PagesSlider.Value;
                    Pager.MaxVisiblePips = (int)PagesSlider.Value;
                }
            };
            PreviousCheck.IsCheckedChanged += (_, _) => Pager.IsPreviousButtonVisible = PreviousCheck.IsChecked == true;
            NextCheck.IsCheckedChanged += (_, _) => Pager.IsNextButtonVisible = NextCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/RefreshContainerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.RefreshContainerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="RefreshContainer：下拉刷新"
                               DocPath="controls/layout/containers/refreshcontainer" />

            <TextBlock Classes="caption" Text="RefreshRequested、RequestRefresh() 与 Deferral" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Border BorderBrush="Gray" BorderThickness="1" Width="320" Height="140" HorizontalAlignment="Left">
                        <RefreshContainer Name="Container">
                            <ScrollViewer>
                                <TextBlock Name="Body" Margin="8" Text="内容：尚未刷新" />
                            </ScrollViewer>
                        </RefreshContainer>
                    </Border>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Button Name="RefreshButton" Content="请求刷新" />
                        <TextBlock Name="RefreshResult" VerticalAlignment="Center" Text="刷新 0 次" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="手势下拉只对触摸有效：IsMouseEnabled 默认 False，鼠标拖不动。上面的按钮用 RequestRefresh() 走同一条路径。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/RefreshContainerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RefreshContainerPage : UserControl
    {
        private int _count;

        public RefreshContainerPage()
        {
            InitializeComponent();

            Container.RefreshRequested += async (_, e) =>
            {
                // The visualizer keeps spinning until the deferral is completed.
                var deferral = e.GetDeferral();
                await Task.Delay(300);
                _count++;
                RefreshResult.Text = $"刷新 {_count} 次";
                Body.Text = $"内容：{DateTime.Now:HH:mm:ss} 刷新";
                deferral.Complete();
            };
            RefreshButton.Click += (_, _) => Container.RequestRefresh();
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
git commit -m "feat: demonstrate Expander, Flyout, PipsPager and RefreshContainer

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: ScrollViewer 与 SplitView

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Layout/ScrollViewerPage.axaml`、`.axaml.cs`
- Create: `.../SplitViewPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖的元素名）：
  - `ScrollViewerPage`：`Scroller`（`ScrollViewer`）、`HBox`、`VBox`（`ComboBox`）、`HomeButton`、`EndButton`、`LineButton`、`OffsetText`
  - `SplitViewPage`：`Split`（`SplitView`）、`ToggleButton`（`Button`）、`ModeBox`、`PlacementBox`（`ComboBox`）、`PaneText`

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ScrollViewerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ScrollViewerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ScrollViewer：滚动容器"
                               DocPath="controls/layout/containers/scrollviewer" />

            <TextBlock Classes="caption" Text="1. 滚动条可见性与编程滚动" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Horizontal" />
                        <ComboBox Name="HBox" Width="110" />
                        <TextBlock VerticalAlignment="Center" Text="Vertical" />
                        <ComboBox Name="VBox" Width="110" />
                    </StackPanel>
                    <!--  The demo viewer needs a fixed Height, otherwise it grows to its content and never scrolls.  -->
                    <ScrollViewer Name="Scroller" Width="320" Height="140" HorizontalAlignment="Left"
                                  BorderBrush="Gray" BorderThickness="1">
                        <Border Width="600" Height="500">
                            <Border.Background>
                                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                    <GradientStop Color="SteelBlue" Offset="0" />
                                    <GradientStop Color="Orange" Offset="1" />
                                </LinearGradientBrush>
                            </Border.Background>
                            <TextBlock Margin="8" Text="600 × 500 的内容" />
                        </Border>
                    </ScrollViewer>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="HomeButton" Content="ScrollToHome" />
                        <Button Name="EndButton" Content="ScrollToEnd" />
                        <Button Name="LineButton" Content="LineDown" />
                    </StackPanel>
                    <TextBlock Name="OffsetText" Text="Offset = 0, 0" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="新建的 ScrollViewer 横向默认是 Disabled（宽内容被裁掉），纵向默认 Auto。直接写 Offset 越界时会被夹取到最大值。" />

            <TextBlock Classes="caption" Text="2. 嵌套：内层不处理的方向交给外层" />
            <Border Classes="stage" Padding="12">
                <ScrollViewer Width="320" Height="100" HorizontalAlignment="Left" BorderBrush="Gray" BorderThickness="1">
                    <StackPanel Spacing="4">
                        <TextBlock Text="外层内容 1" />
                        <ScrollViewer Height="50" IsScrollChainingEnabled="False" BorderBrush="Orange" BorderThickness="1">
                            <StackPanel>
                                <TextBlock Text="内层 1" />
                                <TextBlock Text="内层 2" />
                                <TextBlock Text="内层 3" />
                                <TextBlock Text="内层 4" />
                                <TextBlock Text="内层 5" />
                            </StackPanel>
                        </ScrollViewer>
                        <TextBlock Text="外层内容 2" />
                        <TextBlock Text="外层内容 3" />
                        <TextBlock Text="外层内容 4" />
                    </StackPanel>
                </ScrollViewer>
            </Border>
            <TextBlock Classes="hint" Text="内层 IsScrollChainingEnabled=False：内层滚到头后不会带动外层。更多场景见紧随其后的「实战：ScrollViewer」。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」（本项目每个页面都靠 ScrollViewer 承载）" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ScrollViewerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ScrollViewerPage : UserControl
    {
        public ScrollViewerPage()
        {
            InitializeComponent();

            HBox.ItemsSource = Enum.GetValues<ScrollBarVisibility>();
            HBox.SelectedItem = Scroller.HorizontalScrollBarVisibility;
            HBox.SelectionChanged += (_, _) =>
            {
                if (HBox.SelectedItem is ScrollBarVisibility v)
                {
                    Scroller.HorizontalScrollBarVisibility = v;
                }
            };
            VBox.ItemsSource = Enum.GetValues<ScrollBarVisibility>();
            VBox.SelectedItem = Scroller.VerticalScrollBarVisibility;
            VBox.SelectionChanged += (_, _) =>
            {
                if (VBox.SelectedItem is ScrollBarVisibility v)
                {
                    Scroller.VerticalScrollBarVisibility = v;
                }
            };

            HomeButton.Click += (_, _) => Scroller.ScrollToHome();
            EndButton.Click += (_, _) => Scroller.ScrollToEnd();
            LineButton.Click += (_, _) => Scroller.LineDown();
            Scroller.ScrollChanged += (_, _) => OffsetText.Text = $"Offset = {Scroller.Offset.X:F0}, {Scroller.Offset.Y:F0}";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/SplitViewPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SplitViewPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="SplitView：窗格加内容"
                               DocPath="controls/layout/containers/splitview" />

            <TextBlock Classes="caption" Text="DisplayMode、PanePlacement 与 IsPaneOpen（本项目的主窗口就是一个 SplitView）" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Button Name="ToggleButton" Content="切换窗格" />
                        <TextBlock VerticalAlignment="Center" Text="DisplayMode" />
                        <ComboBox Name="ModeBox" Width="150" />
                        <TextBlock VerticalAlignment="Center" Text="PanePlacement" />
                        <ComboBox Name="PlacementBox" Width="100" />
                    </StackPanel>
                    <TextBlock Name="PaneText" Classes="hint" Text="窗格：关闭" />
                    <SplitView Name="Split" Height="180" BorderBrush="Gray" BorderThickness="1"
                               DisplayMode="CompactInline" CompactPaneLength="48" OpenPaneLength="180"
                               PaneBackground="#33808080">
                        <SplitView.Pane>
                            <StackPanel Spacing="6" Margin="8">
                                <TextBlock Text="☰ 导航" />
                                <TextBlock Text="🏠 首页" />
                                <TextBlock Text="⚙ 设置" />
                            </StackPanel>
                        </SplitView.Pane>
                        <TextBlock Margin="12" Text="主内容区" />
                    </SplitView>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Inline 把内容挤开；Overlay 盖在内容上；Compact* 关闭时仍保留 CompactPaneLength 宽的窄条。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/SplitViewPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SplitViewPage : UserControl
    {
        public SplitViewPage()
        {
            InitializeComponent();

            ToggleButton.Click += (_, _) => Split.IsPaneOpen = !Split.IsPaneOpen;
            Split.PaneOpened += (_, _) => PaneText.Text = "窗格：打开";
            Split.PaneClosed += (_, _) => PaneText.Text = "窗格：关闭";

            ModeBox.ItemsSource = Enum.GetValues<SplitViewDisplayMode>();
            ModeBox.SelectedItem = Split.DisplayMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SplitViewDisplayMode mode)
                {
                    Split.DisplayMode = mode;
                }
            };

            PlacementBox.ItemsSource = Enum.GetValues<SplitViewPanePlacement>();
            PlacementBox.SelectedItem = Split.PanePlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is SplitViewPanePlacement placement)
                {
                    Split.PanePlacement = placement;
                }
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
git commit -m "feat: demonstrate ScrollViewer and SplitView

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: Canvas、DockPanel、Panel、StackPanel

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Layout/CanvasPage.axaml`、`.axaml.cs`
- Create: `.../DockPanelPage.axaml`、`.axaml.cs`
- Create: `.../PanelPage.axaml`、`.axaml.cs`
- Create: `.../StackPanelPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖的元素名）：
  - `CanvasPage`：`Dot`（`Border`）、`LeftSlider`、`TopSlider`
  - `DockPanelPage`：`Dock`（`DockPanel`）、`FillCheck`
  - `PanelPage`：`Stack`（`Panel`）、`Under`（`Border`）、`Over`（`Border`）
  - `StackPanelPage`：`Stack`（`StackPanel`）、`OrientationBox`（`ComboBox`）、`SpacingSlider`

四页都是**最小控件页**：只演示该控件自己的属性，对比与进阶留在 `Avalonia.LayoutDemo`「布局面板」Tab。

#### `Avalonia.ControlsDemo/Views/Pages/Layout/CanvasPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CanvasPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Canvas：按坐标绝对定位"
                               DocPath="controls/layout/panels/canvas" />

            <TextBlock Classes="caption" Text="Canvas.Left / Canvas.Top 与 ZIndex" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Canvas Width="320" Height="140" HorizontalAlignment="Left" Background="#22808080">
                        <Border Canvas.Left="20" Canvas.Top="20" Width="80" Height="60" Background="SteelBlue" />
                        <Border Canvas.Left="60" Canvas.Top="40" Width="80" Height="60" Background="Orange" Opacity="0.85" />
                        <Border Name="Dot" Canvas.Left="200" Canvas.Top="30" Width="40" Height="40"
                                CornerRadius="20" Background="SeaGreen" />
                    </Canvas>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="40" VerticalAlignment="Center" Text="Left" />
                        <Slider Name="LeftSlider" Width="220" Minimum="0" Maximum="280" Value="200" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="40" VerticalAlignment="Center" Text="Top" />
                        <Slider Name="TopSlider" Width="220" Minimum="0" Maximum="100" Value="30" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Canvas 不做任何自动排布：子元素的位置完全由附加属性决定，后声明的盖在先声明的上面。Canvas.Left 与 Canvas.Top 默认是 NaN。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/CanvasPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CanvasPage : UserControl
    {
        public CanvasPage()
        {
            InitializeComponent();

            // Attached properties cannot take an element-name binding as a source here, so move the dot from code.
            LeftSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Canvas.SetLeft(Dot, LeftSlider.Value);
                }
            };
            TopSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Canvas.SetTop(Dot, TopSlider.Value);
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/DockPanelPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DockPanelPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DockPanel：按边停靠"
                               DocPath="controls/layout/panels/dockpanel" />

            <TextBlock Classes="caption" Text="DockPanel.Dock 与 LastChildFill" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <CheckBox Name="FillCheck" Content="LastChildFill" IsChecked="True" />
                    <DockPanel Name="Dock" Width="320" Height="160" HorizontalAlignment="Left">
                        <Border DockPanel.Dock="Top" Height="30" Background="#E8564A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="Top" /></Border>
                        <Border DockPanel.Dock="Bottom" Height="30" Background="#E8974A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="Bottom" /></Border>
                        <Border DockPanel.Dock="Left" Width="60" Background="#E8D24A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="Left" /></Border>
                        <Border DockPanel.Dock="Right" Width="60" Background="#6FE84A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="Right" /></Border>
                        <Border Name="LastChild" Background="#4A9BE8"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="最后一个" /></Border>
                    </DockPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="先声明的先占边；关掉 LastChildFill 后，最后一个元素也按自己的 Dock（默认 Left）停靠，不再填满剩余空间。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/DockPanelPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DockPanelPage : UserControl
    {
        public DockPanelPage()
        {
            InitializeComponent();
            FillCheck.IsCheckedChanged += (_, _) => Dock.LastChildFill = FillCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/PanelPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.PanelPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Panel：所有面板的基类，子元素叠放在同一处"
                               DocPath="controls/layout/panels/panel" />

            <TextBlock Classes="caption" Text="不做任何排布：子元素都铺满面板，后声明的在上面" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Panel Name="Stack" Width="200" Height="100" HorizontalAlignment="Left" Background="#22808080">
                        <Border Name="Under" Background="SteelBlue">
                            <TextBlock HorizontalAlignment="Left" VerticalAlignment="Top" Margin="6" Text="下层" />
                        </Border>
                        <Border Name="Over" Margin="30" Background="Orange" Opacity="0.85">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="上层" />
                        </Border>
                    </Panel>
                    <Button Name="SwapButton" Content="交换 ZIndex" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="需要叠放（比如在图片上盖一个角标）时，用 Panel 比 Grid 更轻；Panel.Background 让面板自己也可点击。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/PanelPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PanelPage : UserControl
    {
        public PanelPage()
        {
            InitializeComponent();

            // ZIndex is a plain property on every Visual; the larger one is drawn on top.
            SwapButton.Click += (_, _) =>
            {
                var overOnTop = Over.ZIndex >= Under.ZIndex;
                Over.ZIndex = overOnTop ? -1 : 1;
                Under.ZIndex = overOnTop ? 1 : -1;
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/StackPanelPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.StackPanelPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="StackPanel：单行或单列排布"
                               DocPath="controls/layout/panels/stackpanel" />

            <TextBlock Classes="caption" Text="Orientation 与 Spacing" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Orientation" />
                        <ComboBox Name="OrientationBox" Width="130" />
                        <TextBlock VerticalAlignment="Center" Text="Spacing" />
                        <Slider Name="SpacingSlider" Width="160" Minimum="0" Maximum="30" Value="6" />
                    </StackPanel>
                    <StackPanel Name="Stack" Spacing="6">
                        <Border Width="60" Height="30" Background="#E8564A" />
                        <Border Width="60" Height="30" Background="#E8974A" />
                        <Border Width="60" Height="30" Background="#E8D24A" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="StackPanel 在排布方向上给子元素无限空间，所以把它放进 ScrollViewer 里内容不会自己出滚动条；需要滚动时给外层固定高度。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/StackPanelPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class StackPanelPage : UserControl
    {
        public StackPanelPage()
        {
            InitializeComponent();

            OrientationBox.ItemsSource = Enum.GetValues<Orientation>();
            OrientationBox.SelectedItem = Stack.Orientation;
            OrientationBox.SelectionChanged += (_, _) =>
            {
                if (OrientationBox.SelectedItem is Orientation orientation)
                {
                    Stack.Orientation = orientation;
                }
            };
            SpacingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Stack.Spacing = SpacingSlider.Value;
                }
            };
        }
    }
}
```

> **StackPanel 页的写法说明**：`Stack` 的 `Spacing="6"` 写在元素上，而 `SpacingSlider` 初值也是 6。这不是规则 1 的陷阱——这里是用 code-behind 直接写属性、没有任何样式在驱动 `Spacing`，写本地值就是正确做法。

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate Canvas, DockPanel, Panel and StackPanel

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: Grid、GridSplitter、RelativePanel、UniformGrid、WrapPanel

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Layout/GridPage.axaml`、`.axaml.cs`
- Create: `.../GridSplitterPage.axaml`、`.axaml.cs`
- Create: `.../RelativePanelPage.axaml`、`.axaml.cs`
- Create: `.../UniformGridPage.axaml`、`.axaml.cs`
- Create: `.../WrapPanelPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖的元素名）：
  - `GridPage`：`Demo`（`Grid`）、`LinesCheck`、`SpacingSlider`
  - `GridSplitterPage`：`Split`（`Grid`）、`Splitter`（`GridSplitter`）、`WidthText`、`PreviewCheck`
  - `RelativePanelPage`：`Rel`（`RelativePanel`）、`Anchor`、`Follower`、`Edge`（`Border`）、`PanelRightCheck`
  - `UniformGridPage`：`Uniform`（`UniformGrid`）、`ColumnsSlider`、`FirstSlider`
  - `WrapPanelPage`：`Wrap`（`WrapPanel`）、`OrientationBox`（`ComboBox`）、`ItemGapSlider`、`LineGapSlider`

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GridPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.GridPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Grid：行列网格"
                               DocPath="controls/layout/panels/grid" />

            <TextBlock Classes="caption" Text="1. 行列定义：像素、Auto、星号，加 ShowGridLines 与间距" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="LinesCheck" Content="ShowGridLines" />
                        <TextBlock VerticalAlignment="Center" Text="RowSpacing / ColumnSpacing" />
                        <Slider Name="SpacingSlider" Width="140" Minimum="0" Maximum="20" Value="0" />
                    </StackPanel>
                    <Grid Name="Demo" Width="360" Height="150" HorizontalAlignment="Left"
                          ColumnDefinitions="80,Auto,*,2*" RowDefinitions="40,*">
                        <Border Grid.Column="0" Grid.Row="0" Background="#E8564A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="80px" /></Border>
                        <Border Grid.Column="1" Grid.Row="0" Background="#E8974A"><TextBlock Margin="8,0" VerticalAlignment="Center" Text="Auto" /></Border>
                        <Border Grid.Column="2" Grid.Row="0" Background="#E8D24A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="*" /></Border>
                        <Border Grid.Column="3" Grid.Row="0" Background="#6FE84A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="2*" /></Border>
                        <Border Grid.Column="0" Grid.Row="1" Grid.ColumnSpan="4" Background="#4A9BE8">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="第二行，ColumnSpan=4，高度 *" />
                        </Border>
                    </Grid>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="星号按剩余空间的比例分：* 与 2* 就是 1:2。Auto 取内容所需的宽度。行列定义可以写成 ColumnDefinitions=&quot;80,Auto,*,2*&quot; 这样的简写。" />

            <TextBlock Classes="caption" Text="2. 同一格叠放：后声明的盖在上面" />
            <Border Classes="stage" Padding="12">
                <Grid Width="200" Height="80" HorizontalAlignment="Left">
                    <Border Background="SteelBlue" />
                    <Border Margin="30,20" Background="Orange" Opacity="0.85">
                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="叠在上层" />
                    </Border>
                </Grid>
            </Border>
            <TextBlock Classes="hint" Text="不写 Grid.Row / Grid.Column 就是第 0 行第 0 列；SharedSizeGroup、MinWidth / MaxWidth 等见紧随其后的「实战：Grid」。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GridPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GridPage : UserControl
    {
        public GridPage()
        {
            InitializeComponent();

            LinesCheck.IsCheckedChanged += (_, _) => Demo.ShowGridLines = LinesCheck.IsChecked == true;
            SpacingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Demo.RowSpacing = SpacingSlider.Value;
                    Demo.ColumnSpacing = SpacingSlider.Value;
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GridSplitterPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.GridSplitterPage">
    <UserControl.Styles>
        <!--  Appearance goes through a style, not a local Background on the element (rule 1).  -->
        <Style Selector="GridSplitter">
            <Setter Property="Background" Value="Orange" />
        </Style>
    </UserControl.Styles>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="GridSplitter：拖动调整行列大小"
                               DocPath="controls/layout/panels/gridsplitter" />

            <TextBlock Classes="caption" Text="拖动橙色分隔条，或点一下它再按左右方向键" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <CheckBox Name="PreviewCheck" Content="ShowsPreview（拖动时只移动预览线，松手才生效）" />
                    <Grid Name="Split" Width="360" Height="120" HorizontalAlignment="Left" ColumnDefinitions="200,Auto,*">
                        <Border Grid.Column="0" Background="#334682B4">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="左栏" />
                        </Border>
                        <GridSplitter Name="Splitter" Grid.Column="1" Width="6" />
                        <Border Grid.Column="2" Background="#33FFA500">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="右栏" />
                        </Border>
                    </Grid>
                    <TextBlock Name="WidthText" Text="左栏宽度 200" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="GridSplitter 要放在自己独占的 Auto 列（或行）里，它改的是相邻行列的 Width / Height。聚焦后方向键每次移动 KeyboardIncrement（默认 10）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GridSplitterPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GridSplitterPage : UserControl
    {
        public GridSplitterPage()
        {
            InitializeComponent();

            // Dragging rewrites ColumnDefinition.Width, so the readout reads it back after every layout pass.
            Split.LayoutUpdated += (_, _) => WidthText.Text = $"左栏宽度 {Split.ColumnDefinitions[0].Width.Value:F0}";
            PreviewCheck.IsCheckedChanged += (_, _) => Splitter.ShowsPreview = PreviewCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/RelativePanelPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.RelativePanelPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="RelativePanel：子元素互相参照定位"
                               DocPath="controls/layout/panels/relativepanel" />

            <TextBlock Classes="caption" Text="RightOf、Below 与 AlignRightWithPanel" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <CheckBox Name="PanelRightCheck" Content="给「跟随者」再加上 AlignRightWithPanel（与 RightOf 并存）" />
                    <RelativePanel Name="Rel" Width="340" Height="140" HorizontalAlignment="Left" Background="#22808080">
                        <Border Name="Anchor" Width="90" Height="40" Background="SteelBlue"
                                RelativePanel.AlignLeftWithPanel="True" RelativePanel.AlignTopWithPanel="True">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="锚点" />
                        </Border>
                        <Border Name="Follower" Width="90" Height="40" Background="Orange"
                                RelativePanel.RightOf="Anchor" RelativePanel.AlignTopWithPanel="True">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="跟随者" />
                        </Border>
                        <Border Width="190" Height="30" Background="SeaGreen"
                                RelativePanel.Below="Anchor" RelativePanel.AlignLeftWith="Anchor">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="在锚点下方，左对齐" />
                        </Border>
                        <Border Name="Edge" Width="90" Height="30" Background="MediumPurple"
                                RelativePanel.AlignRightWithPanel="True" RelativePanel.AlignBottomWithPanel="True">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="贴右下角" />
                        </Border>
                    </RelativePanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="参照对象写元素的 Name（字符串）。RelativePanel 共有 16 个附加属性：Above / Below / LeftOf / RightOf、AlignXxxWith、AlignXxxWithPanel 等。" />
            <TextBlock Classes="hint" Text="实测：左右两侧各有一条约束时（RightOf 加 AlignRightWithPanel），元素占据两条约束之间的空间，定宽元素会在中间居中，而不是贴边；只写 AlignRightWithPanel 的紫色块才真正贴右。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/RelativePanelPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RelativePanelPage : UserControl
    {
        public RelativePanelPage()
        {
            InitializeComponent();

            PanelRightCheck.IsCheckedChanged += (_, _) =>
                RelativePanel.SetAlignRightWithPanel(Follower, PanelRightCheck.IsChecked == true);
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/UniformGridPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.UniformGridPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="UniformGrid：等大小格子"
                               DocPath="controls/layout/panels/uniformgrid" />

            <TextBlock Classes="caption" Text="Columns 与 FirstColumn" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="80" VerticalAlignment="Center" Text="Columns" />
                        <Slider Name="ColumnsSlider" Width="160" Minimum="1" Maximum="6" Value="3" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="80" VerticalAlignment="Center" Text="FirstColumn" />
                        <Slider Name="FirstSlider" Width="160" Minimum="0" Maximum="5" Value="0" />
                    </StackPanel>
                    <UniformGrid Name="Uniform" Width="300" Height="140" HorizontalAlignment="Left" Columns="3">
                        <Border Margin="2" Background="#E8564A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="1" /></Border>
                        <Border Margin="2" Background="#E8974A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="2" /></Border>
                        <Border Margin="2" Background="#E8D24A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="3" /></Border>
                        <Border Margin="2" Background="#6FE84A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="4" /></Border>
                        <Border Margin="2" Background="#4A9BE8"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="5" /></Border>
                    </UniformGrid>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="每个格子一样大；Rows 与 Columns 只给一个，另一个按子元素个数自动算。FirstColumn 让第一行前面空出若干格。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/UniformGridPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class UniformGridPage : UserControl
    {
        public UniformGridPage()
        {
            InitializeComponent();

            // Columns and FirstColumn are ints; the handlers convert the slider's double.
            ColumnsSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Uniform.Columns = (int)ColumnsSlider.Value;
                }
            };
            FirstSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Uniform.FirstColumn = (int)FirstSlider.Value;
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/WrapPanelPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.WrapPanelPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="WrapPanel：放不下就换行"
                               DocPath="controls/layout/panels/wrappanel" />

            <TextBlock Classes="caption" Text="Orientation、ItemSpacing 与 LineSpacing" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="Orientation" />
                        <ComboBox Name="OrientationBox" Width="130" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="80" VerticalAlignment="Center" Text="ItemSpacing" />
                        <Slider Name="ItemGapSlider" Width="160" Minimum="0" Maximum="20" Value="4" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="80" VerticalAlignment="Center" Text="LineSpacing" />
                        <Slider Name="LineGapSlider" Width="160" Minimum="0" Maximum="20" Value="4" />
                    </StackPanel>
                    <WrapPanel Name="Wrap" Width="260" Height="150" HorizontalAlignment="Left">
                        <Border Width="70" Height="30" Background="#E8564A" />
                        <Border Width="50" Height="40" Background="#E8974A" />
                        <Border Width="80" Height="30" Background="#E8D24A" />
                        <Border Width="60" Height="50" Background="#6FE84A" />
                        <Border Width="90" Height="30" Background="#4A9BE8" />
                        <Border Width="50" Height="30" Background="#9B6FE8" />
                        <Border Width="70" Height="40" Background="#E86FB4" />
                    </WrapPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Horizontal 先排一行再换行，Vertical 先排一列再换列；面板的宽（或高）决定在哪里换。ItemWidth / ItemHeight 能把所有格子压成同样大小。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.LayoutDemo →「布局面板」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/WrapPanelPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WrapPanelPage : UserControl
    {
        public WrapPanelPage()
        {
            InitializeComponent();

            OrientationBox.ItemsSource = Enum.GetValues<Orientation>();
            OrientationBox.SelectedItem = Wrap.Orientation;
            OrientationBox.SelectionChanged += (_, _) =>
            {
                if (OrientationBox.SelectedItem is Orientation orientation)
                {
                    Wrap.Orientation = orientation;
                }
            };
            ItemGapSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Wrap.ItemSpacing = ItemGapSlider.Value;
                }
            };
            LineGapSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Wrap.LineSpacing = LineGapSlider.Value;
                }
            };
            Wrap.ItemSpacing = ItemGapSlider.Value;
            Wrap.LineSpacing = LineGapSlider.Value;
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
git commit -m "feat: demonstrate Grid, GridSplitter, RelativePanel, UniformGrid and WrapPanel

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 6: 三篇 How-to 实战页（Grid、ScrollViewer、Expander）

每篇取官方 How-to 里能在一个窗口内演示的场景，紧跟在控件页之后；`PageKind.HowTo`，树里显示为「实战：…」。

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/ExpanderHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Layout/GridHowToPage.axaml`、`.axaml.cs`
- Create: `.../ScrollViewerHowToPage.axaml`、`.axaml.cs`
- Create: `.../ExpanderHowToPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖）：
  - `ExpanderHowToViewModel : ObservableObject`：`int OpenSection`（-1 表示全收起）、`bool IsGeneralOpen` / `IsAdvancedOpen` / `IsAboutOpen`（可读写，写入 true 会收起其它节）
  - 元素名：`GridHowToPage`：`SizeScope`（`Grid`）、`FormA` / `FormB`（`Grid`）、`Bounded`（`Grid`）、`SpanGrid`（`Grid`）、`LinesCheck`、`Overlay`（`Border`）；`ScrollViewerHowToPage`：`FeedScroller`（`ScrollViewer`）、`FeedList`（`StackPanel`）、`FeedCount`、`LoadButton`、`JumpScroller`（`ScrollViewer`）、`JumpButton`、`JumpTarget`（`Border`）、`StickyGrid`（`Grid`）、`StickyHeader`（`Border`）、`StickyScroller`（`ScrollViewer`）、`ChainInner`（`ScrollViewer`）、`ChainCheck`；`ExpanderHowToPage`：`Section1` / `Section2` / `Section3`（`Expander`）、`Card`（`Expander`）、`DirectionDemo`（`Expander`）、`DirectionBox`（`ComboBox`）、`DisabledCheck`、`OpenLabel`

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GridHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.GridHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Grid 实战"
                               DocPath="docs/how-to/grid-how-to" />

            <TextBlock Classes="caption" Text="1. SharedSizeGroup：两行表单的标签列一样宽" />
            <Border Classes="stage" Padding="12">
                <!--  Without IsSharedSizeScope on the outer grid, the two Auto columns size independently.  -->
                <Grid Name="SizeScope" Grid.IsSharedSizeScope="True" Width="360" HorizontalAlignment="Left" RowDefinitions="Auto,Auto">
                    <Grid Name="FormA" Grid.Row="0" Margin="0,0,0,6">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" SharedSizeGroup="Label" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Margin="0,0,12,0" Text="名" />
                        <TextBox Grid.Column="1" PlaceholderText="王小明" />
                    </Grid>
                    <Grid Name="FormB" Grid.Row="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" SharedSizeGroup="Label" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Margin="0,0,12,0" Text="电子邮箱地址" />
                        <TextBox Grid.Column="1" PlaceholderText="name@example.com" />
                    </Grid>
                </Grid>
            </Border>
            <TextBlock Classes="hint" Text="两个 Auto 列同属 SharedSizeGroup=Label：它们取共同的最大宽度，所以标签长度不同也对得齐。共用名要落在同一个 Grid.IsSharedSizeScope 子树里。" />

            <TextBlock Classes="caption" Text="2. 星号列上加 MinWidth / MaxWidth" />
            <Border Classes="stage" Padding="12">
                <Grid Name="Bounded" Width="400" Height="56" HorizontalAlignment="Left">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" MinWidth="60" />
                        <ColumnDefinition Width="3*" MaxWidth="180" />
                    </Grid.ColumnDefinitions>
                    <Border Grid.Column="0" Background="#334682B4"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="MinWidth 60" /></Border>
                    <Border Grid.Column="1" Background="#33FFA500"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="MaxWidth 180" /></Border>
                </Grid>
            </Border>
            <TextBlock Classes="hint" Text="星号列在有富余空间时先按比例分，超出 MinWidth / MaxWidth 的部分会让给其它列；窗口够窄时 MinWidth 会造成水平方向的溢出。" />

            <TextBlock Classes="caption" Text="3. RowSpan / ColumnSpan、同格叠放与 ZIndex" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <CheckBox Name="LinesCheck" Content="ShowGridLines（调试用，显示行列线）" />
                    <Grid Name="SpanGrid" Width="360" Height="140" HorizontalAlignment="Left" ColumnDefinitions="*,*,*" RowDefinitions="*,*">
                        <Border Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="3" Background="#334682B4">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="表头：ColumnSpan=3" />
                        </Border>
                        <Border Grid.Row="1" Grid.Column="0" Background="#E8564A" />
                        <Border Grid.Row="1" Grid.Column="1" Background="#E8974A" />
                        <Border Grid.Row="1" Grid.Column="2" Background="#E8D24A" />
                        <Border Name="Overlay" Grid.Row="0" Grid.RowSpan="2" Grid.Column="2" ZIndex="1" Opacity="0.9" Background="Orange">
                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="覆盖&#10;RowSpan=2" TextAlignment="Center" />
                        </Border>
                    </Grid>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="同一个格子里放多个元素就叠起来，默认后声明的在上；ZIndex 可以显式指定谁在上面、谁在下面。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/GridHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GridHowToPage : UserControl
    {
        public GridHowToPage()
        {
            InitializeComponent();
            LinesCheck.IsCheckedChanged += (_, _) => SpanGrid.ShowGridLines = LinesCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ScrollViewerHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ScrollViewerHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ScrollViewer 实战"
                               DocPath="docs/how-to/scrollviewer-how-to" />

            <TextBlock Classes="caption" Text="1. 滚到底自动加载更多：ScrollChanged + Offset / Extent / Viewport" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <ScrollViewer Name="FeedScroller" Width="320" Height="120" HorizontalAlignment="Left"
                                  BorderBrush="Gray" BorderThickness="1">
                        <StackPanel Name="FeedList" Margin="8" Spacing="2" />
                    </ScrollViewer>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Button Name="LoadButton" Content="ScrollToEnd（触发加载）" />
                        <TextBlock Name="FeedCount" VerticalAlignment="Center" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="判断触底：Offset.Y + Viewport.Height ≥ Extent.Height。Extent 为 0（还没布局）时同样满足这个式子，所以要先确认内容已经比视口高。示例最多加载 60 项。" />

            <TextBlock Classes="caption" Text="2. 滚到指定元素：BringIntoView()" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <ScrollViewer Name="JumpScroller" Width="320" Height="100" HorizontalAlignment="Left"
                                  BorderBrush="Gray" BorderThickness="1">
                        <StackPanel Margin="8" Spacing="2">
                            <TextBlock Text="第 1 行" />
                            <TextBlock Text="第 2 行" />
                            <TextBlock Text="第 3 行" />
                            <TextBlock Text="第 4 行" />
                            <TextBlock Text="第 5 行" />
                            <TextBlock Text="第 6 行" />
                            <TextBlock Text="第 7 行" />
                            <TextBlock Text="第 8 行" />
                            <Border Name="JumpTarget" Background="Orange" Padding="6,2">
                                <TextBlock Text="★ 目标行" />
                            </Border>
                            <TextBlock Text="第 10 行" />
                            <TextBlock Text="第 11 行" />
                            <TextBlock Text="第 12 行" />
                        </StackPanel>
                    </ScrollViewer>
                    <Button Name="JumpButton" Content="BringIntoView 到目标行" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 吸顶表头：把表头放在 ScrollViewer 之外" />
            <Border Classes="stage" Padding="12">
                <Grid Name="StickyGrid" Width="320" RowDefinitions="Auto,100" HorizontalAlignment="Left">
                    <Border Name="StickyHeader" Grid.Row="0" Background="SteelBlue" Padding="8,4">
                        <TextBlock Text="表头永远可见" />
                    </Border>
                    <ScrollViewer Name="StickyScroller" Grid.Row="1" BorderBrush="Gray" BorderThickness="1">
                        <StackPanel Margin="8" Spacing="2">
                            <TextBlock Text="内容 1" />
                            <TextBlock Text="内容 2" />
                            <TextBlock Text="内容 3" />
                            <TextBlock Text="内容 4" />
                            <TextBlock Text="内容 5" />
                            <TextBlock Text="内容 6" />
                            <TextBlock Text="内容 7" />
                            <TextBlock Text="内容 8" />
                            <TextBlock Text="内容 9" />
                            <TextBlock Text="内容 10" />
                        </StackPanel>
                    </ScrollViewer>
                </Grid>
            </Border>
            <TextBlock Classes="hint" Text="最省事的吸顶做法就是版面上分成两行：表头在第一行，ScrollViewer 在第二行。表头需要跟着横向滚动时再考虑把它放进内容里。" />

            <TextBlock Classes="caption" Text="4. 嵌套滚动：IsScrollChainingEnabled" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <CheckBox Name="ChainCheck" Content="内层 IsScrollChainingEnabled（滚到头后把滚动交给外层）" IsChecked="True" />
                    <ScrollViewer Name="ChainOuter" Width="320" Height="110" HorizontalAlignment="Left"
                                  BorderBrush="Gray" BorderThickness="1">
                        <StackPanel Spacing="4" Margin="8">
                            <TextBlock Text="外层内容 1" />
                            <ScrollViewer Name="ChainInner" Height="50" BorderBrush="Orange" BorderThickness="1">
                                <StackPanel>
                                    <TextBlock Text="内层 1" />
                                    <TextBlock Text="内层 2" />
                                    <TextBlock Text="内层 3" />
                                    <TextBlock Text="内层 4" />
                                    <TextBlock Text="内层 5" />
                                </StackPanel>
                            </ScrollViewer>
                            <TextBlock Text="外层内容 2" />
                            <TextBlock Text="外层内容 3" />
                            <TextBlock Text="外层内容 4" />
                            <TextBlock Text="外层内容 5" />
                        </StackPanel>
                    </ScrollViewer>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="用滚轮在橙色内层上滚：勾选时内层到头后继续滚会带动外层；取消勾选，内层到头就停。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ScrollViewerHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ScrollViewerHowToPage : UserControl
    {
        private const int MaxItems = 60;
        private int _loaded;

        public ScrollViewerHowToPage()
        {
            InitializeComponent();

            AddItems(20);
            FeedScroller.ScrollChanged += (_, _) =>
            {
                // Extent is 0 before the first layout, which would also satisfy the "at bottom" test, so require real overflow.
                var overflows = FeedScroller.Extent.Height > FeedScroller.Viewport.Height;
                var atBottom = FeedScroller.Offset.Y + FeedScroller.Viewport.Height >= FeedScroller.Extent.Height - 1;
                if (overflows && atBottom && _loaded < MaxItems)
                {
                    AddItems(10);
                }
            };
            LoadButton.Click += (_, _) => FeedScroller.ScrollToEnd();

            JumpButton.Click += (_, _) => JumpTarget.BringIntoView();
            ChainCheck.IsCheckedChanged += (_, _) => ChainInner.IsScrollChainingEnabled = ChainCheck.IsChecked == true;
        }

        private void AddItems(int count)
        {
            for (var i = 0; i < count; i++)
            {
                FeedList.Children.Add(new TextBlock { Text = $"第 {++_loaded} 项" });
            }

            FeedCount.Text = $"已加载 {_loaded} 项";
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/ExpanderHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Backs the accordion block: one int says which section is open, -1 means all collapsed.</summary>
    public sealed class ExpanderHowToViewModel : ObservableObject
    {
        private int _openSection = -1;

        public int OpenSection
        {
            get => _openSection;
            set
            {
                if (SetProperty(ref _openSection, value))
                {
                    OnPropertyChanged(nameof(IsGeneralOpen));
                    OnPropertyChanged(nameof(IsAdvancedOpen));
                    OnPropertyChanged(nameof(IsAboutOpen));
                }
            }
        }

        // Only a "false" for the section that is currently open collapses it. Without that guard the
        // TwoWay binding of the section being closed by someone else would close the new one as well.
        public bool IsGeneralOpen
        {
            get => _openSection == 0;
            set => Apply(0, value);
        }

        public bool IsAdvancedOpen
        {
            get => _openSection == 1;
            set => Apply(1, value);
        }

        public bool IsAboutOpen
        {
            get => _openSection == 2;
            set => Apply(2, value);
        }

        private void Apply(int index, bool isOpen)
        {
            if (isOpen)
            {
                OpenSection = index;
            }
            else if (_openSection == index)
            {
                OpenSection = -1;
            }
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ExpanderHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ExpanderHowToPage"
             x:DataType="vm:ExpanderHowToViewModel">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Expander 实战"
                               DocPath="docs/how-to/expander-how-to" />

            <TextBlock Classes="caption" Text="1. 手风琴：一次只开一节" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="6">
                    <Expander Name="Section1" Width="340" Header="常规"
                              IsExpanded="{Binding IsGeneralOpen, Mode=TwoWay}">
                        <TextBlock Margin="8" TextWrapping="Wrap" Text="常规设置的内容。" />
                    </Expander>
                    <Expander Name="Section2" Width="340" Header="高级"
                              IsExpanded="{Binding IsAdvancedOpen, Mode=TwoWay}">
                        <TextBlock Margin="8" TextWrapping="Wrap" Text="高级设置的内容。" />
                    </Expander>
                    <Expander Name="Section3" Width="340" Header="关于"
                              IsExpanded="{Binding IsAboutOpen, Mode=TwoWay}">
                        <TextBlock Margin="8" TextWrapping="Wrap" Text="版本、许可等信息。" />
                    </Expander>
                    <TextBlock Name="OpenLabel" Classes="hint" Text="{Binding OpenSection, StringFormat='OpenSection = {0}（-1 表示全收起）'}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="三节都绑到同一个 ViewModel 的 IsExpanded：打开某一节就把 OpenSection 换成它的序号，其余 IsExpanded 自然变 false。" />

            <TextBlock Classes="caption" Text="2. 自定义头部与 ContentTransition" />
            <Border Classes="stage" Padding="12">
                <Expander Name="Card" Width="340">
                    <Expander.Header>
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <TextBlock Text="⚙" />
                            <TextBlock FontWeight="SemiBold" Text="外观设置" />
                            <Border Background="Orange" CornerRadius="8" Padding="6,0" VerticalAlignment="Center">
                                <TextBlock FontSize="11" Text="新" />
                            </Border>
                        </StackPanel>
                    </Expander.Header>
                    <Expander.ContentTransition>
                        <CrossFade Duration="0:0:0.3" />
                    </Expander.ContentTransition>
                    <TextBlock Margin="8" Text="Header 是任意对象，可以塞面板；ContentTransition 换成 CrossFade 后展开是淡入而不是滑出。" />
                </Expander>
            </Border>

            <TextBlock Classes="caption" Text="3. ExpandDirection 与 :disabled" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="ExpandDirection" />
                        <ComboBox Name="DirectionBox" Width="140" />
                        <CheckBox Name="DisabledCheck" Content="IsEnabled=False" />
                    </StackPanel>
                    <Expander Name="DirectionDemo" Header="方向" Width="300" HorizontalAlignment="Left">
                        <TextBlock Margin="8" Text="展开的内容" />
                    </Expander>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="选 Up 时要留出向上生长的空间，否则会溢出面板；IsEnabled=False 时整块变灰，鼠标点不动头部。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Layout/ExpanderHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ExpanderHowToPage : UserControl
    {
        public ExpanderHowToPage()
        {
            InitializeComponent();
            DataContext = new ExpanderHowToViewModel();

            DirectionBox.ItemsSource = Enum.GetValues<ExpandDirection>();
            DirectionBox.SelectedItem = DirectionDemo.ExpandDirection;
            DirectionBox.SelectionChanged += (_, _) =>
            {
                if (DirectionBox.SelectedItem is ExpandDirection direction)
                {
                    DirectionDemo.ExpandDirection = direction;
                }
            };
            DisabledCheck.IsCheckedChanged += (_, _) => DirectionDemo.IsEnabled = DisabledCheck.IsChecked != true;
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告（`CrossFade` 在默认 XML 命名空间里可直接写，已构建验证）。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the Grid, ScrollViewer and Expander how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 7: 登记全部页面、探针验证、收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Layout.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Layout.cs`、`Probe.Layout2.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）

**Interfaces:**
- Consumes: Task 1–6 的全部页面类；第 00 份的 `ControlPage<T>`、`HowToPage<T>`、`Harness`
- Produces: Layout 分类共 20 个 `Control` 页 + 3 个 `HowTo` 页，登记顺序为官方侧边栏顺序（containers → decorator → layouttransformcontrol → panels），HowTo 紧跟其控件

- [ ] **Step 1: 登记**

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Layout.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddLayout(List<PageEntry> list)
        {
            const string c = Categories.Layout;

            // Containers, in the order of the official sidebar.
            list.Add(ControlPage<BorderPage>("Border", c, "controls/layout/containers/border"));
            list.Add(ControlPage<ExpanderPage>("Expander", c, "controls/layout/containers/expander"));
            list.Add(HowToPage<ExpanderHowToPage>("Expander", c, "docs/how-to/expander-how-to"));
            list.Add(ControlPage<FlyoutPage>("Flyout", c, "controls/layout/containers/flyout"));
            list.Add(ControlPage<GroupBoxPage>("GroupBox", c, "controls/layout/containers/groupbox"));
            list.Add(ControlPage<PipsPagerPage>("PipsPager", c, "controls/layout/containers/pipspager"));
            list.Add(ControlPage<RefreshContainerPage>("RefreshContainer", c, "controls/layout/containers/refreshcontainer"));
            list.Add(ControlPage<ScrollViewerPage>("ScrollViewer", c, "controls/layout/containers/scrollviewer"));
            list.Add(HowToPage<ScrollViewerHowToPage>("ScrollViewer", c, "docs/how-to/scrollviewer-how-to"));
            list.Add(ControlPage<SplitViewPage>("SplitView", c, "controls/layout/containers/splitview"));
            list.Add(ControlPage<ViewboxPage>("Viewbox", c, "controls/layout/containers/viewbox"));

            // Decorator
            list.Add(ControlPage<DecoratorPage>("Decorator", c, "controls/layout/decorator"));

            // LayoutTransformControl
            list.Add(ControlPage<LayoutTransformControlPage>("LayoutTransformControl", c, "controls/layout/layouttransformcontrol"));

            // Panels
            list.Add(ControlPage<CanvasPage>("Canvas", c, "controls/layout/panels/canvas"));
            list.Add(ControlPage<DockPanelPage>("DockPanel", c, "controls/layout/panels/dockpanel"));
            list.Add(ControlPage<GridPage>("Grid", c, "controls/layout/panels/grid"));
            list.Add(HowToPage<GridHowToPage>("Grid", c, "docs/how-to/grid-how-to"));
            list.Add(ControlPage<GridSplitterPage>("GridSplitter", c, "controls/layout/panels/gridsplitter"));
            list.Add(ControlPage<PanelPage>("Panel", c, "controls/layout/panels/panel"));
            list.Add(ControlPage<RelativePanelPage>("RelativePanel", c, "controls/layout/panels/relativepanel"));
            list.Add(ControlPage<StackPanelPage>("StackPanel", c, "controls/layout/panels/stackpanel"));
            list.Add(ControlPage<UniformGridPage>("UniformGrid", c, "controls/layout/panels/uniformgrid"));
            list.Add(ControlPage<WrapPanelPage>("WrapPanel", c, "controls/layout/panels/wrappanel"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

**`C:\Temp\probe-controls\Probe.Layout.cs`**（目录完整性、容器与装饰器页、Task 1–3 的行为读数）

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeLayout
{
    public static void Run()
    {
        Catalog();
        Containers();
        Interactive();
        Panels();
        HowTos();
    }

    static void Catalog()
    {
        var layout = PageCatalog.All.Where(e => e.Category == Categories.Layout).ToList();
        Harness.Check("Layout: 20 Control pages", layout.Count(e => e.Kind == PageKind.Control) == 20, layout.Count(e => e.Kind == PageKind.Control));
        Harness.Check("Layout: 3 HowTo pages", layout.Count(e => e.Kind == PageKind.HowTo) == 3, layout.Count(e => e.Kind == PageKind.HowTo));

        // Each HowTo page sits directly after the control page with the same title.
        for (int i = 0; i < layout.Count; i++)
        {
            if (layout[i].Kind != PageKind.HowTo) continue;
            Harness.Check($"Layout: HowTo {layout[i].Title} follows its control",
                i > 0 && layout[i - 1].Kind == PageKind.Control && layout[i - 1].Title == layout[i].Title);
            Harness.Check($"Layout: HowTo {layout[i].Title} DocPath starts with docs/how-to/", layout[i].DocPath.StartsWith("docs/how-to/"));
        }

        foreach (var entry in layout)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Layout/{entry.Title}({entry.Kind}): renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"Layout/{entry.Title}({entry.Kind}): header matches catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            w.Close();
        }
    }

    static void Interactive()
    {
        // ---- Expander: only Expanded / Collapsed are countable; Expanding fires several times per action
        var ex = new ExpanderPage();
        var w = Harness.Show(ex);
        var basic = Harness.Find<Expander>(ex, "Basic");
        basic.IsExpanded = true;
        Harness.Pump();
        Harness.Check("Expander: code writes IsExpanded and reports one expansion",
            Harness.Find<TextBlock>(ex, "BasicResult").Text == "已展开 1 次，已折叠 0 次", Harness.Find<TextBlock>(ex, "BasicResult").Text);
        Harness.Find<CheckBox>(ex, "EnabledCheck").IsChecked = false;
        Harness.Pump();
        basic.IsExpanded = false;
        Harness.Pump();
        Harness.Check("Expander: IsEnabled=false does not block a code write to IsExpanded",
            !basic.IsEnabled && Harness.Find<TextBlock>(ex, "BasicResult").Text == "已展开 1 次，已折叠 1 次",
            Harness.Find<TextBlock>(ex, "BasicResult").Text);
        Harness.Find<ComboBox>(ex, "DirectionBox").SelectedItem = ExpandDirection.Right;
        Harness.Pump();
        Harness.Check("Expander: the direction box drives ExpandDirection", Harness.Find<Expander>(ex, "Sideways").ExpandDirection == ExpandDirection.Right);
        w.Close();

        // ---- Flyout: ShowAt opens it, Hide closes it
        var fl = new FlyoutPage();
        w = Harness.Show(fl);
        var openButton = Harness.Find<Button>(fl, "OpenButton");
        var flyout = (Flyout)openButton.Flyout!;
        flyout.ShowAt(openButton);
        Harness.Pump();
        Harness.Check("Flyout: ShowAt opens it", flyout.IsOpen && Harness.Find<TextBlock>(fl, "FlyoutResult").Text == "Flyout 已打开");
        flyout.Hide();
        Harness.Pump();
        Harness.Check("Flyout: Hide closes it", !flyout.IsOpen && Harness.Find<TextBlock>(fl, "FlyoutResult").Text == "Flyout 已关闭");
        w.Close();

        // ---- PipsPager: the readout follows SelectedPageIndex
        var pp = new PipsPagerPage();
        w = Harness.Show(pp);
        var pager = Harness.Find<PipsPager>(pp, "Pager");
        pager.SelectedPageIndex = 3;
        Harness.Pump();
        Harness.Check("PipsPager: the readout follows SelectedPageIndex", Harness.Find<TextBlock>(pp, "PageText").Text == "第 4 页",
            Harness.Find<TextBlock>(pp, "PageText").Text);
        Harness.Find<Slider>(pp, "PagesSlider").Value = 8;
        Harness.Pump();
        Harness.Check("PipsPager: the slider drives NumberOfPages and MaxVisiblePips",
            pager.NumberOfPages == 8 && pager.MaxVisiblePips == 8, $"{pager.NumberOfPages}/{pager.MaxVisiblePips}");
        Harness.Find<CheckBox>(pp, "PreviousCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("PipsPager: the checkbox shows the previous button", pager.IsPreviousButtonVisible);
        w.Close();

        // ---- RefreshContainer: RequestRefresh drives the same path the touch gesture would
        var rc = new RefreshContainerPage();
        w = Harness.Show(rc);
        Harness.Check("RefreshContainer: the mouse is off by default", !Harness.Find<RefreshContainer>(rc, "Container").IsMouseEnabled);
        Harness.Find<Button>(rc, "RefreshButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        System.Threading.Thread.Sleep(450);
        Harness.Pump();
        Harness.Check("RefreshContainer: the request goes through the deferral and reports one refresh",
            Harness.Find<TextBlock>(rc, "RefreshResult").Text == "刷新 1 次", Harness.Find<TextBlock>(rc, "RefreshResult").Text);
        Harness.Check("RefreshContainer: the body was rewritten", Harness.Find<TextBlock>(rc, "Body").Text?.EndsWith("刷新") == true,
            Harness.Find<TextBlock>(rc, "Body").Text);
        w.Close();

        // ---- ScrollViewer: the buttons call the scrolling API; offsets are read back rather than guessed
        var sv = new ScrollViewerPage();
        w = Harness.Show(sv);
        var scroller = Harness.Find<ScrollViewer>(sv, "Scroller");
        Harness.Check("ScrollViewer: a fresh one is Horizontal Disabled and Vertical Auto",
            scroller.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && scroller.VerticalScrollBarVisibility == ScrollBarVisibility.Auto,
            $"{scroller.HorizontalScrollBarVisibility}/{scroller.VerticalScrollBarVisibility}");
        var extent = scroller.Extent.Height;
        var viewport = scroller.Viewport.Height;
        Harness.Check("ScrollViewer: the 500-high content overflows the 140-high viewer", extent > viewport && viewport > 0, $"{extent}/{viewport}");
        Harness.Find<Button>(sv, "EndButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        var atEnd = scroller.Offset.Y;
        Harness.Check("ScrollViewer: ScrollToEnd lands on the bottom", Math.Abs(atEnd - (extent - viewport)) < 1.5, $"{atEnd} vs {extent - viewport}");
        Harness.Check("ScrollViewer: the readout follows the offset", Harness.Find<TextBlock>(sv, "OffsetText").Text == $"Offset = {scroller.Offset.X:F0}, {scroller.Offset.Y:F0}",
            Harness.Find<TextBlock>(sv, "OffsetText").Text);
        Harness.Find<Button>(sv, "HomeButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("ScrollViewer: ScrollToHome returns to the top", scroller.Offset.Y == 0, scroller.Offset.Y);
        Harness.Find<Button>(sv, "LineButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("ScrollViewer: LineDown moves down by one line", scroller.Offset.Y > 0, scroller.Offset.Y);
        w.Close();

        // ---- SplitView: assert IsPaneOpen and the closed pane width, not the animated open pixels
        var sp = new SplitViewPage();
        w = Harness.Show(sp);
        var split = Harness.Find<SplitView>(sp, "Split");
        Harness.Check("SplitView: starts closed and keeps a 48-wide compact strip",
            !split.IsPaneOpen && split.DisplayMode == SplitViewDisplayMode.CompactInline, $"{split.IsPaneOpen}/{split.DisplayMode}");
        var paneRoot = split.GetVisualDescendants().OfType<Panel>().First(x => x.Name == "PART_PaneRoot");
        Harness.Check("SplitView: the closed pane strip is CompactPaneLength wide", Math.Abs(paneRoot.Bounds.Width - 48) < 1.5, paneRoot.Bounds.Width);
        Harness.Find<Button>(sp, "ToggleButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("SplitView: the toggle button opens the pane", split.IsPaneOpen);
        Harness.Find<ComboBox>(sp, "ModeBox").SelectedItem = SplitViewDisplayMode.Overlay;
        Harness.Find<ComboBox>(sp, "PlacementBox").SelectedItem = SplitViewPanePlacement.Right;
        Harness.Pump();
        Harness.Check("SplitView: the boxes drive DisplayMode and PanePlacement",
            split.DisplayMode == SplitViewDisplayMode.Overlay && split.PanePlacement == SplitViewPanePlacement.Right,
            $"{split.DisplayMode}/{split.PanePlacement}");
        w.Close();
    }

    static void Containers()
    {
        // ---- Border: the sliders drive Thickness and CornerRadius
        var bp = new BorderPage();
        var w = Harness.Show(bp);
        var target = Harness.Find<Border>(bp, "Target");
        Harness.Check("Border: thickness starts at the slider value", target.BorderThickness == new Thickness(2), target.BorderThickness);
        Harness.Find<Slider>(bp, "ThicknessSlider").Value = 6;
        Harness.Pump();
        Harness.Check("Border: the thickness slider rewrites BorderThickness", target.BorderThickness == new Thickness(6), target.BorderThickness);
        Harness.Find<Slider>(bp, "RadiusSlider").Value = 12;
        Harness.Pump();
        Harness.Check("Border: the radius slider rewrites CornerRadius", target.CornerRadius == new CornerRadius(12), target.CornerRadius);
        w.Close();

        // ---- GroupBox: header only
        var gb = new GroupBoxPage();
        w = Harness.Show(gb);
        Harness.Check("GroupBox: header text", Harness.Find<GroupBox>(gb, "Box").Header?.ToString() == "登录方式");
        w.Close();

        // ---- Decorator: Padding grows the box the Decorator occupies, and it draws nothing itself
        var dp = new DecoratorPage();
        w = Harness.Show(dp);
        var dec = Harness.Find<Decorator>(dp, "PaddedDecorator");
        Harness.Check("Decorator: Padding 10 around a 120x40 child makes the box 140x60",
            Math.Abs(dec.Bounds.Width - 140) < 1.5 && Math.Abs(dec.Bounds.Height - 60) < 1.5, dec.Bounds);
        Harness.Find<Slider>(dp, "PaddingSlider").Value = 30;
        Harness.Pump();
        Harness.Check("Decorator: the padding slider grows it further",
            Math.Abs(dec.Bounds.Width - 180) < 1.5 && Math.Abs(dec.Bounds.Height - 100) < 1.5, dec.Bounds);
        Harness.Check("Decorator: the readout follows the bounds", Harness.Find<TextBlock>(dp, "SizeText").Text?.StartsWith("占 180") == true,
            Harness.Find<TextBlock>(dp, "SizeText").Text);
        w.Close();

        // ---- LayoutTransformControl vs RenderTransform: the same angle, a different layout box
        var lt = new LayoutTransformControlPage();
        w = Harness.Show(lt);
        var layoutHost = Harness.Find<LayoutTransformControl>(lt, "LayoutHost");
        var renderHost = Harness.Find<Decorator>(lt, "RenderHost");
        Harness.Check("LayoutTransformControl: a 90-degree layout transform swaps the host box to tall and narrow",
            layoutHost.Bounds.Width < 60 && layoutHost.Bounds.Height > 80, layoutHost.Bounds);
        Harness.Check("RenderTransform: the host box stays 100x40",
            Math.Abs(renderHost.Bounds.Width - 100) < 1.5 && Math.Abs(renderHost.Bounds.Height - 40) < 1.5, renderHost.Bounds);
        Harness.Find<Slider>(lt, "AngleSlider").Value = 0;
        Harness.Pump();
        Harness.Check("LayoutTransformControl: back at 0 degrees the host box is 100x40 again",
            Math.Abs(layoutHost.Bounds.Width - 100) < 1.5 && Math.Abs(layoutHost.Bounds.Height - 40) < 1.5, layoutHost.Bounds);
        w.Close();

        // ---- Viewbox: the child's Bounds never change; the scale shows in where its right edge lands
        var vb = new ViewboxPage();
        w = Harness.Show(vb);
        var box = Harness.Find<Viewbox>(vb, "Box");
        var inner = Harness.Find<Border>(vb, "Inner");
        Harness.Check("Viewbox: the child keeps its own 100x50 bounds",
            Math.Abs(inner.Bounds.Width - 100) < 1.5 && Math.Abs(inner.Bounds.Height - 50) < 1.5, inner.Bounds);
        // The 320x140 Border has a 1px border, so the Viewbox gets 318x138: 138/50 = 2.76 (the height is the limit).
        Harness.Check("Viewbox: the readout reports the fitted scale",
            Harness.Find<TextBlock>(vb, "ScaleText").Text?.Contains("2.76") == true, Harness.Find<TextBlock>(vb, "ScaleText").Text);
        box.Stretch = Stretch.None;
        Harness.Pump();
        Harness.Check("Viewbox: Stretch.None stops the scaling",
            Harness.Find<TextBlock>(vb, "ScaleText").Text?.Contains("1.00") == true, Harness.Find<TextBlock>(vb, "ScaleText").Text);
        Harness.Check("Viewbox: the Stretch box starts on the control's value", Harness.Find<ComboBox>(vb, "StretchBox").SelectedItem is Stretch.Uniform);
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Layout2.cs`**（Panels 与三篇 How-to；`Panels()` 里有 GridSplitter 的真实鼠标拖动与键盘）

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeLayout
{
    static void Panels()
    {
        // ---- Canvas: the sliders move the dot through the attached properties
        var cv = new CanvasPage();
        var w = Harness.Show(cv);
        var dot = Harness.Find<Border>(cv, "Dot");
        Harness.Find<Slider>(cv, "LeftSlider").Value = 100;
        Harness.Find<Slider>(cv, "TopSlider").Value = 50;
        Harness.Pump();
        Harness.Check("Canvas: Left and Top follow the sliders", Canvas.GetLeft(dot) == 100 && Canvas.GetTop(dot) == 50, $"{Canvas.GetLeft(dot)},{Canvas.GetTop(dot)}");
        w.Close();

        // ---- DockPanel: LastChildFill decides whether the last element fills the rest
        var dk = new DockPanelPage();
        w = Harness.Show(dk);
        var dock = Harness.Find<DockPanel>(dk, "Dock");
        var last = Harness.Find<Border>(dk, "LastChild");
        Harness.Check("DockPanel: by default the last child fills the middle", last.Bounds.Width > 100, last.Bounds);
        Harness.Find<CheckBox>(dk, "FillCheck").IsChecked = false;
        Harness.Pump();
        Harness.Check("DockPanel: LastChildFill=false is applied", !dock.LastChildFill);
        w.Close();

        // ---- Panel: swapping ZIndex flips the order
        var pn = new PanelPage();
        w = Harness.Show(pn);
        var under = Harness.Find<Border>(pn, "Under");
        var over = Harness.Find<Border>(pn, "Over");
        Harness.Find<Button>(pn, "SwapButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("Panel: the swap button puts Under above Over", under.ZIndex > over.ZIndex, $"{under.ZIndex}/{over.ZIndex}");
        w.Close();

        // ---- StackPanel
        var st = new StackPanelPage();
        w = Harness.Show(st);
        var stack = Harness.Find<StackPanel>(st, "Stack");
        Harness.Find<ComboBox>(st, "OrientationBox").SelectedItem = Orientation.Horizontal;
        Harness.Find<Slider>(st, "SpacingSlider").Value = 20;
        Harness.Pump();
        Harness.Check("StackPanel: the boxes drive Orientation and Spacing", stack.Orientation == Orientation.Horizontal && stack.Spacing == 20, $"{stack.Orientation}/{stack.Spacing}");
        w.Close();

        // ---- Grid
        var gr = new GridPage();
        w = Harness.Show(gr);
        var demo = Harness.Find<Grid>(gr, "Demo");
        Harness.Find<CheckBox>(gr, "LinesCheck").IsChecked = true;
        Harness.Find<Slider>(gr, "SpacingSlider").Value = 10;
        Harness.Pump();
        Harness.Check("Grid: ShowGridLines and spacing follow the controls", demo.ShowGridLines && demo.RowSpacing == 10 && demo.ColumnSpacing == 10);
        // ActualWidth includes the spacing, so the 1:2 split is read from the children: 59 and 119 at a gap of 10.
        var star = demo.Children[2].Bounds.Width;
        var twoStar = demo.Children[3].Bounds.Width;
        Harness.Check("Grid: * and 2* split the remainder 1:2", star > 0 && Math.Abs(twoStar / star - 2) < 0.05, $"{star}/{twoStar}");
        w.Close();

        // ---- GridSplitter: a mouse drag and the arrow keys both rewrite the column width
        var gs = new GridSplitterPage();
        w = Harness.Show(gs);
        var split = Harness.Find<Grid>(gs, "Split");
        var splitter = Harness.Find<GridSplitter>(gs, "Splitter");
        Harness.Check("GridSplitter: starts with a 200 left column", split.ColumnDefinitions[0].Width.Value == 200, split.ColumnDefinitions[0].Width);
        var start = splitter.TranslatePoint(new Point(splitter.Bounds.Width / 2, splitter.Bounds.Height / 2), w)!.Value;
        w.MouseDown(start, MouseButton.Left);
        w.MouseMove(new Point(start.X + 30, start.Y));
        w.MouseMove(new Point(start.X + 60, start.Y));
        w.MouseUp(new Point(start.X + 60, start.Y), MouseButton.Left);
        Harness.Pump();
        Harness.Check("GridSplitter: dragging 60 right widens the left column to 260", Math.Abs(split.ColumnDefinitions[0].Width.Value - 260) < 1.5, split.ColumnDefinitions[0].Width);
        Harness.Check("GridSplitter: the readout follows", Harness.Find<TextBlock>(gs, "WidthText").Text == "左栏宽度 260", Harness.Find<TextBlock>(gs, "WidthText").Text);
        splitter.Focus();
        Harness.Pump();
        w.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Harness.Pump();
        Harness.Check("GridSplitter: an arrow key moves it by KeyboardIncrement (10)", Math.Abs(split.ColumnDefinitions[0].Width.Value - 270) < 1.5, split.ColumnDefinitions[0].Width);
        w.Close();

        // ---- RelativePanel: Right-of puts the follower beside the anchor; aligning to the panel edge moves it
        var rp = new RelativePanelPage();
        w = Harness.Show(rp);
        var anchor = Harness.Find<Border>(rp, "Anchor");
        var follower = Harness.Find<Border>(rp, "Follower");
        Harness.Check("RelativePanel: the follower starts right of the anchor", Math.Abs(follower.Bounds.X - (anchor.Bounds.X + anchor.Bounds.Width)) < 1.5, follower.Bounds);
        Harness.Find<CheckBox>(rp, "PanelRightCheck").IsChecked = true;
        Harness.Pump();
        var rel = Harness.Find<RelativePanel>(rp, "Rel");
        // Two constraints on opposite sides: the fixed-width child centres between the anchor's edge and the panel's edge.
        var expectedX = anchor.Bounds.Right + (rel.Bounds.Width - anchor.Bounds.Right - follower.Bounds.Width) / 2;
        Harness.Check("RelativePanel: RightOf plus AlignRightWithPanel centres the follower between the two",
            Math.Abs(follower.Bounds.X - expectedX) < 1.5, $"{follower.Bounds.X} vs {expectedX}");
        Harness.Check("RelativePanel: AlignRightWithPanel alone does reach the right edge",
            Math.Abs(Harness.Find<Border>(rp, "Edge").Bounds.Right - rel.Bounds.Width) < 1.5, Harness.Find<Border>(rp, "Edge").Bounds);
        w.Close();

        // ---- UniformGrid
        var ug = new UniformGridPage();
        w = Harness.Show(ug);
        var uni = Harness.Find<UniformGrid>(ug, "Uniform");
        Harness.Find<Slider>(ug, "ColumnsSlider").Value = 5;
        Harness.Find<Slider>(ug, "FirstSlider").Value = 2;
        Harness.Pump();
        Harness.Check("UniformGrid: the sliders drive Columns and FirstColumn", uni.Columns == 5 && uni.FirstColumn == 2, $"{uni.Columns}/{uni.FirstColumn}");
        w.Close();

        // ---- WrapPanel
        var wp = new WrapPanelPage();
        w = Harness.Show(wp);
        var wrap = Harness.Find<WrapPanel>(wp, "Wrap");
        Harness.Find<ComboBox>(wp, "OrientationBox").SelectedItem = Orientation.Vertical;
        Harness.Find<Slider>(wp, "ItemGapSlider").Value = 12;
        Harness.Find<Slider>(wp, "LineGapSlider").Value = 9;
        Harness.Pump();
        Harness.Check("WrapPanel: the controls drive Orientation, ItemSpacing and LineSpacing",
            wrap.Orientation == Orientation.Vertical && wrap.ItemSpacing == 12 && wrap.LineSpacing == 9, $"{wrap.Orientation}/{wrap.ItemSpacing}/{wrap.LineSpacing}");
        w.Close();
    }

    static void HowTos()
    {
        // ---- Grid how-to: a shared size group makes the two label columns equally wide
        var gp = new GridHowToPage();
        var w = Harness.Show(gp);
        var a = Harness.Find<Grid>(gp, "FormA");
        var b = Harness.Find<Grid>(gp, "FormB");
        Harness.Check("Grid how-to: SharedSizeGroup gives both label columns the same width",
            a.ColumnDefinitions[0].ActualWidth > 0 && Math.Abs(a.ColumnDefinitions[0].ActualWidth - b.ColumnDefinitions[0].ActualWidth) < 0.5,
            $"{a.ColumnDefinitions[0].ActualWidth} vs {b.ColumnDefinitions[0].ActualWidth}");
        var bounded = Harness.Find<Grid>(gp, "Bounded");
        // 400 wide, * and 3*: the 3* column would be 300 but is capped at 180, and * takes what is left (220).
        Harness.Check("Grid how-to: MaxWidth caps the 3* column", Math.Abs(bounded.ColumnDefinitions[1].ActualWidth - 180) < 1.5, bounded.ColumnDefinitions[1].ActualWidth);
        Harness.Find<CheckBox>(gp, "LinesCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("Grid how-to: ShowGridLines follows the box", Harness.Find<Grid>(gp, "SpanGrid").ShowGridLines);
        Harness.Check("Grid how-to: the overlay sits above the cells it covers", Harness.Find<Border>(gp, "Overlay").ZIndex == 1);
        w.Close();

        // ---- ScrollViewer how-to
        var sp = new ScrollViewerHowToPage();
        w = Harness.Show(sp);
        var feed = Harness.Find<ScrollViewer>(sp, "FeedScroller");
        var list = Harness.Find<StackPanel>(sp, "FeedList");
        Harness.Check("ScrollViewer how-to: starts with 20 items", list.Children.Count == 20, list.Children.Count);
        Harness.Find<Button>(sp, "LoadButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump(); Harness.Pump();
        Harness.Check("ScrollViewer how-to: reaching the bottom loads 10 more", list.Children.Count >= 30, list.Children.Count);
        for (int i = 0; i < 10; i++) { feed.ScrollToEnd(); Harness.Pump(); }
        Harness.Check("ScrollViewer how-to: loading stops at the 60 item cap", list.Children.Count == 60, list.Children.Count);
        Harness.Check("ScrollViewer how-to: the counter matches", Harness.Find<TextBlock>(sp, "FeedCount").Text == "已加载 60 项", Harness.Find<TextBlock>(sp, "FeedCount").Text);

        var jump = Harness.Find<ScrollViewer>(sp, "JumpScroller");
        Harness.Check("ScrollViewer how-to: the jump list starts at the top", jump.Offset.Y == 0);
        Harness.Find<Button>(sp, "JumpButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("ScrollViewer how-to: BringIntoView scrolls the target into the viewport", jump.Offset.Y > 0, jump.Offset.Y);

        var grid = Harness.Find<Grid>(sp, "StickyGrid");
        var header = Harness.Find<Border>(sp, "StickyHeader");
        var sticky = Harness.Find<ScrollViewer>(sp, "StickyScroller");
        sticky.ScrollToEnd();
        Harness.Pump();
        Harness.Check("ScrollViewer how-to: the header stays put while the content scrolls", sticky.Offset.Y > 0 && header.Bounds.Y == 0, $"{sticky.Offset.Y}/{header.Bounds.Y}");

        var inner = Harness.Find<ScrollViewer>(sp, "ChainInner");
        Harness.Find<CheckBox>(sp, "ChainCheck").IsChecked = false;
        Harness.Pump();
        Harness.Check("ScrollViewer how-to: the chain box drives IsScrollChainingEnabled", !inner.IsScrollChainingEnabled);
        w.Close();

        // ---- Expander how-to: the accordion keeps exactly one section open
        var ep = new ExpanderHowToPage();
        w = Harness.Show(ep);
        var vm = (ExpanderHowToViewModel)ep.DataContext!;
        var s1 = Harness.Find<Expander>(ep, "Section1");
        var s2 = Harness.Find<Expander>(ep, "Section2");
        var s3 = Harness.Find<Expander>(ep, "Section3");
        Harness.Check("Expander how-to: starts with every section collapsed", !s1.IsExpanded && !s2.IsExpanded && !s3.IsExpanded && vm.OpenSection == -1);
        s1.IsExpanded = true;
        Harness.Pump();
        Harness.Check("Expander how-to: opening section 1 sets OpenSection 0", vm.OpenSection == 0 && s1.IsExpanded && !s2.IsExpanded, vm.OpenSection);
        s2.IsExpanded = true;
        Harness.Pump();
        Harness.Check("Expander how-to: opening section 2 closes section 1", vm.OpenSection == 1 && s2.IsExpanded && !s1.IsExpanded && !s3.IsExpanded,
            $"{vm.OpenSection} {s1.IsExpanded}/{s2.IsExpanded}/{s3.IsExpanded}");
        s2.IsExpanded = false;
        Harness.Pump();
        Harness.Check("Expander how-to: closing the open section returns to -1", vm.OpenSection == -1 && !s2.IsExpanded, vm.OpenSection);
        Harness.Check("Expander how-to: the label reads the model", Harness.Find<TextBlock>(ep, "OpenLabel").Text?.StartsWith("OpenSection = -1") == true,
            Harness.Find<TextBlock>(ep, "OpenLabel").Text);
        var card = Harness.Find<Expander>(ep, "Card");
        Harness.Check("Expander how-to: the card uses a CrossFade content transition", card.ContentTransition is Avalonia.Animation.CrossFade, card.ContentTransition);
        Harness.Find<ComboBox>(ep, "DirectionBox").SelectedItem = ExpandDirection.Left;
        Harness.Find<CheckBox>(ep, "DisabledCheck").IsChecked = true;
        Harness.Pump();
        var dd = Harness.Find<Expander>(ep, "DirectionDemo");
        Harness.Check("Expander how-to: the boxes drive ExpandDirection and IsEnabled", dd.ExpandDirection == ExpandDirection.Left && !dd.IsEnabled);
        w.Close();
    }
}
```

同时把 `C:\Temp\probe-controls\Program.cs` 的 `Main` 改为依次调用 `ProbeShell.Run(); ProbeButtons.Run(); ProbeInput.Run(); ProbeLayout.Run(); ProbePackages.Run(); ProbeNewPages.Run();`。探针目录里不要留下仅用于测量的临时 `.cs` 文件（它们会一起编译，与 `ProbeLayout` 无关却可能冲突）。

- [ ] **Step 3: 跑探针**

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "FAIL|error|Unhandled|passed"`
Expected: `0 failed`、`0 warning log(s)`（编写计划时实测为 **369 passed**，其中第 00、01 份的 245 条在内，本份新增 124 条）。

**失败时的排查顺序**：
1. `GridSplitter` 拖动两条失败：探针的 `MouseDown` 坐标要用分隔条中心相对窗口的点（`TranslatePoint(..., window)`），并且要分两次 `MouseMove`；
2. `Grid: * and 2* split…` 失败：别读 `ColumnDefinition.ActualWidth`（含间距），读子元素 `Bounds.Width`；
3. `SplitView: the closed pane strip…` 失败：窄条是 `Panel#PART_PaneRoot`，不是 `Border`；
4. `Expander` 计数不对：检查页面是不是订阅了 `Expanding` / `Collapsing`（会触发多次）；
5. `RefreshContainer` 刷新数为 0：探针里等待要大于页面里的 300 ms 延迟；
6. 任何页面 `renders` 失败且无异常：对照第 00 份「Buttons 页面的共同写法」检查骨架。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|warn|个错误|Build succeeded" | tail -5`
Expected: 0 个错误、无新增警告。

- [ ] **Step 5: 真实窗口目视（人工，一次）**

```bash
dotnet run --project Avalonia.ControlsDemo
```

确认：Layout 分类下 23 个条目都能切换；`GridSplitter` 页用鼠标拖橙色分隔条，读数跟着变；`Flyout` 页四个方向的按钮弹出位置正确；`ScrollViewer` 实战页用滚轮滚内层，勾选 / 取消勾选链式滚动表现不同；`RefreshContainer` 页点按钮后文字更新（鼠标拖不动是预期）；`Expander` 手风琴一次只开一节；`SplitView` 页切换 `Overlay` 后窗格盖在内容上。弹出层、滚轮与真实拖动是 headless 验证不了的部分。

- [ ] **Step 6: 回填 spec**

在 spec「待验证的技术风险」末尾的「批 1 实测结论」之后追加（若第 01 份尚未执行，则接在「批 0 实测结论」之后）：

```markdown
### 批 2 实测结论（填入执行日期）

- **容器的静默行为**：`ScrollViewer` 新建时横向 `Disabled`、纵向 `Auto`，直接写越界 `Offset` 会被夹取到最大值；`Expander` 一次展开触发 3 次 `Expanding`（点击触发 2 次），只有 `Expanded` / `Collapsed` 可以计数，且 `IsEnabled=False` 挡不住代码写 `IsExpanded`；`RefreshContainer.IsMouseEnabled` 默认 `False`，鼠标拖不动，只有触摸能下拉；`Viewbox` 子元素的 `Bounds` 不反映缩放，倍数要用 `TransformToVisual` 算。
- **布局读数的坑**：`Grid` 的 `ColumnDefinition.ActualWidth` 含 `ColumnSpacing`，子元素 `Bounds.Width` 才是真实内容宽；`RelativePanel` 中左右各有一条约束（`RightOf` 加 `AlignRightWithPanel`）时，定宽元素在两条约束之间居中而不是贴边；`SplitView` 关闭时的窄条是模板里的 `Panel#PART_PaneRoot`。
- **`LayoutTransformControl` 与 `RenderTransform`**：同样旋转 90°，前者的宿主变成 40×100（参与布局），后者宿主仍是 100×40。
- **官方 How-to 与 12.1.2 的出入**：无。
- **探针断言实际条数**：本批累计 369 条（含第 00、01 份），全部通过且零警告日志。
```

尖括号与"填入执行日期"必须替换为实际日期；数字以 Step 3 的真实输出为准。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: complete the Layout category with 20 control pages and 3 how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review（编写者自查记录）

**Spec 覆盖**：Layout 的 Containers 9、Decorator 1、LayoutTransformControl 1、Panels 9 共 20 页 → Task 1–5；3 篇 How-to（Grid、ScrollViewer、Expander）→ Task 6；官方侧边栏顺序与 HowTo 紧跟 → Task 7 Step 1 与探针的"紧跟"断言；与 `Avalonia.LayoutDemo` 的去重 → 各 Panels 页末尾的指向行。

**已实测，不是推断**：本份全部页面代码已从计划原文抽取到 `C:\Temp\plan-verify`，构建 0 错误 0 警告，探针 369 条通过。构建与实测时发现并已回写到上面代码里的有六处：`RefreshCompletionDeferral` 不是 `IDisposable`（用 `Complete()`）；`Name="Content"` 会让生成字段遮住 `ContentControl.Content`（改叫 `Body`）；`ScrollBarVisibility` 在 `Avalonia.Controls.Primitives`；`Grid` 的 `ActualWidth` 含间距；`RelativePanel` 双向约束是居中；`SplitView` 窄条是 `PART_PaneRoot`。

**类型一致性**：元素名与 Task 1–6 的 Interfaces 列表一致；`ExpanderHowToViewModel.OpenSection` / `IsGeneralOpen` / `IsAdvancedOpen` / `IsAboutOpen` 在 ViewModel、XAML 与探针里一致。

**未覆盖，已知**：`Flyout` 四个方向的实际弹出位置、滚轮驱动的嵌套滚动与链式滚动、`RefreshContainer` 的触摸下拉、`SplitView` 展开动画的像素值；这些在 Step 5 目视。

