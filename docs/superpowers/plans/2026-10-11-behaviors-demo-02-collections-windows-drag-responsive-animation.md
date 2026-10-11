# BehaviorsDemo 02：第 6–10 页（集合、窗口弹出通知、拖拽、响应式、动画）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Avalonia.BehaviorsDemo` 里加入第 6–10 页：集合与 ItemsControl、窗口/弹出/通知、拖拽、响应式、动画，并把 `MainWindow` 扩成 10 个标签。

**Architecture:** 延续计划 01：每页一个 `UserControl` + 一个 ViewModel，行为全部写在 XAML 的 `Interaction.Behaviors` 里；仅在行为库做不到的地方（打开子窗口、调用 `AttentionAnimations`）用最少的 code-behind。验证用仓库外的 headless 探针（`C:\Temp\probe-bdemo`，沿用计划 01 的 `Harness`）。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Xaml.Behaviors.Avalonia 12.0.7、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md`

**前置：** 计划 01 已执行（`Avalonia.BehaviorsDemo` 项目、5 个页面、`C:\Temp\probe-bdemo` 里的 `Harness.cs` 与 `Probe.Mech/Event/Data/Focus/Valid/Shell.cs` 都在）。

## Global Constraints

- 新写的代码与 XAML 注释用**英文**；界面文字用**中文**。
- 中文属性值里不要用 ASCII 双引号（用「」），不用 `&` 与 `<`（AVLN1001）。
- 版本只写在根目录 `Directory.Packages.props`，csproj 里的 `PackageReference` 不带 `Version`；本份不新增任何包。
- MVVM 用 CommunityToolkit.Mvvm；默认开启编译绑定，页面根元素写 `x:DataType`。
- 页面类名 `<主题>Page`，命名空间 `Avalonia.BehaviorsDemo.Views.Pages`；ViewModel 命名空间 `Avalonia.BehaviorsDemo.ViewModels`。
- `DemoHeader.DocPath` 写被演示类型的完整名。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`；探针放仓库外，不提交。

### 本份已核实的事实（编写计划时在仓库外的临时项目里 headless 实测）

| 事项 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `AddItemToItemsControlAction` / `ClearItemsControlAction` | 作用在 `ItemsControl.ItemsSource` 绑定的集合上：`ItemsSource` 是 `ObservableCollection` 时，添加与清空都生效；`ItemsControl` 没绑 `ItemsSource`、只有 XAML 里手写子项时**什么都不会发生**（也没有日志） | 页面里的列表一律绑 `ItemsSource`；页面上注明这一点 |
| `RemoveItemInItemsControlAction` | 没有任何属性：放在列表项模板里的按钮上，会把**这一项的 DataContext** 从所属 `ItemsControl` 的 `ItemsSource` 里删掉 | 放进 `ItemTemplate` 的按钮里，不需要 `Item` |
| `ScrollToItemBehavior` | `Item` 是 `IObservable<object>` 类型的属性：往里推一个元素，列表就滚到该元素（滚动偏移 0 → 5487）。项目里没有 System.Reactive，ViewModel 自己写一个最小的 `IObservable<object>` | 新增 `ScrollRequest` 类 |
| `ListBoxSelectAllBehavior` | 挂在 `SelectionMode="Multiple"` 的 `ListBox` 上，焦点在列表里按 Ctrl+A 全选 | 页面上提示要先点一下列表 |
| `ShowFlyoutAction` / `ShowPopupAction` / `ShowToolTipAction` | 点击后 `Flyout.IsOpen`、`Popup.IsOpen`、`ToolTip.GetIsOpen` 都变 true | 三者都用 `TargetControl` / `Popup` 绑到元素名 |
| `NotificationManagerBehavior` + `ShowSuccessNotificationAction` | 行为挂在宿主控件上，运行时它的 `NotificationManager` 是 `WindowNotificationManager`；Action 的 `NotificationManager` 必须绑定到它（`{Binding #NM.NotificationManager}`，行为要写 `x:Name`），点击后窗口里出现 `NotificationCard`。**不绑定就什么都不显示，也没有日志** | 四种通知 Action（Success / Information / Warning / Error）都显式绑定 |
| `CloseWindowAction` | 不指定 `TargetWindow` 时关闭按钮所在的窗口 | 放在子窗口里的按钮上，**不要放在主窗口里** |
| `CanvasDragBehavior` | `Canvas` 里的元素真实拖动后 `Canvas.Left/Top` 改变（10,10 → 70,35，等于鼠标位移）。headless 里 `MouseMove` 必须带 `RawInputModifiers.LeftMouseButton` | 探针的拖动助手统一带该修饰键 |
| `MouseDragElementBehavior` | 拖动后元素的 `RenderTransform` 变成 `TranslateTransform`（位置变化 +50,+20） | 与 `CanvasDragBehavior` 对比：前者改变换，后者改 Canvas 附加属性 |
| `ListReorderDragBehavior` | **必须用 `BehaviorCollectionTemplate` 通过 `ListBoxItem` 的样式挂到每个列表项上**（`<Setter Property="(Interaction.Behaviors)">`）。直接写进 `DataTemplate` 的子元素里拖动无效。挂对后拖动 A 越过两项，`ItemsSource` 变成 B,C,A,D | 页面用样式挂载 |
| `AdaptiveBehavior` | 按宿主宽度加减样式类：宽 200 → `narrow`，400 → `wide`，80 → `narrow`；宽度变化后立即切换 | 页面用 `Border.narrow` / `Border.wide` 样式换颜色 |
| `AspectRatioBehavior` | 200×100 → `flat`；80×100 → `tall` | 同上 |
| `FadeInBehavior` | 挂载时不透明度走 1 → 0 → … → 1，真实时间约 0.3 秒；只在**挂载**时播放一次 | 探针在 `Show` 之前挂监听记录采样 |
| `PlayAnimationBehavior` / `BeginAnimationAction` | `Animation` **不能直接写在行为或 Action 里面**（编译报 AVLN2200「找不到 Setter 的目标类型」）；写成某个 `Style` 的 `Style.Resources` 里的 `<Animation x:Key=…>`（Setter 用 `Property="Opacity"`），再用 `Animation="{DynamicResource Key}"` 引用即可，动画播完后 `FillMode="Forward"` 使不透明度停在末值 | 页面把 `Animation` 放进 `UserControl.Styles` 的 `Style.Resources` |
| `AttentionAnimations.SetShakeX(Control, double milliseconds)` | 在 code-behind 里直接调用，不抛异常；`EntranceAnimations` / `AttentionAnimations` / `ExitAnimations` 都是 `SetXxx(Control, double)` 形态 | 页面用一个按钮的 `Click` 处理器调用 |
| `TiltEffectBehavior`、`ParallaxBehavior` | 挂载成功；headless 里没有可读回的 `RenderTransform`，真实效果需目视 | 探针只验证挂载 |
| 计时器 | `DispatcherTimer` 在 headless 里只有跑 `Dispatcher.UIThread.MainLoop` 才会走（计划 01 的 `Harness.RunLoop` / `WaitUntil` 已封装） | 动画读回用 `RunLoop` |

---

## File Structure（本份创建 / 修改）

```
Avalonia.BehaviorsDemo/
├── Views/
│   ├── MainWindow.axaml                    改：5 个标签 → 10 个标签
│   ├── ChildWindow.axaml / .axaml.cs       新：页面 7 的子窗口（里面有 CloseWindowAction）
│   └── Pages/
│       ├── CollectionPage                  新：页面 6
│       ├── WindowPopupPage                 新：页面 7
│       ├── DragPage                        新：页面 8
│       ├── ResponsivePage                  新：页面 9
│       └── AnimationPage                   新：页面 10
└── ViewModels/
    ├── ScrollRequest.cs                    新：最小的 IObservable<object>
    ├── CollectionViewModel.cs              新
    ├── DragViewModel.cs                    新
    └── ResponsiveViewModel.cs              新
README.md                                   改：项目行页数 5 → 10
docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md   改：追加「批 2 实测结论」
```

### 抽取与验证的约定

- 同计划 01：代码块前的 `#### \`相对路径\`` 标题是目标文件（相对仓库根），`**\`C:\绝对路径\`**` 标题是仓库外的探针文件；同一路径出现多次时后者覆盖前者。抽取脚本见计划 01 的附录 A（`C:\Temp\extract-plan.ps1`）。
- "抽取并构建"指：`powershell -NoProfile -File C:\Temp\extract-plan.ps1 -Plan <本计划> -RepoRoot <仓库根>`，然后 `dotnet build Avalonia.BehaviorsDemo 2>&1 | grep -E " error |个错误"`，期望 0 个错误。
- 探针累计：计划 01 结束时是 74 条；本份每个任务末尾给出新增条数与累计数。

## Task 1: 页面 6 集合与 ItemsControl

**Files:**
- Create: `ViewModels/ScrollRequest.cs`、`ViewModels/CollectionViewModel.cs`、`Views/Pages/CollectionPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Collection.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `ScrollRequest : IObservable<object>`（`Publish(object)`）；`CollectionViewModel`：`Fruits`、`Todos`（`ObservableCollection<string>`）、`Numbers`（200 项）、`Colors`（8 项）、`ScrollRequests`、命令 `ScrollToCommand(string?)`、`ResetTodosCommand`。控件名：`FruitList`、`AddFruitButton`、`ClearFruitsButton`、`TodoList`、`BigList`、`ScrollButton`、`MultiList`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Collection.cs`**

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeCollection
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 6: collections ==");
        var page = new CollectionPage();
        var w = Show(page, 900, 1300);
        var vm = (CollectionViewModel)page.DataContext!;

        var add = Find<Button>(page, "AddFruitButton");
        Click(add); Click(add); Pump();
        Check("AddItemToItemsControlAction appended to the bound collection twice", vm.Fruits.Count == 5 && vm.Fruits[3] == "葡萄");
        Click(Find<Button>(page, "ClearFruitsButton")); Pump();
        Check("ClearItemsControlAction emptied the bound collection", vm.Fruits.Count == 0);

        var todoList = Find<ListBox>(page, "TodoList");
        var removeButton = todoList.GetVisualDescendants().OfType<Button>().First(b => (string?)b.DataContext == "跑探针");
        Click(removeButton); Pump();
        Check("RemoveItemInItemsControlAction removed exactly the clicked item", vm.Todos.Count == 2 && !vm.Todos.Contains("跑探针"));

        var scrollButton = Find<Button>(page, "ScrollButton");
        Check("the scroll button is wired to ScrollToCommand", scrollButton.Command == vm.ScrollToCommand);
        var viewer = Find<ListBox>(page, "BigList").GetVisualDescendants().OfType<ScrollViewer>().First();
        Check("BigList starts at the top", viewer.Offset.Y == 0);
        vm.ScrollToCommand.Execute("150"); Pump(20);
        Check("ScrollToItemBehavior scrolled the list after an item was published", viewer.Offset.Y > 0);

        var multi = Find<ListBox>(page, "MultiList");
        multi.Focus(); Pump();
        w.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control); Pump();
        Check("ListBoxSelectAllBehavior: Ctrl+A selected all 8 items", multi.SelectedItems!.Count == vm.Colors.Count && vm.Colors.Count == 8);
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
ProbeCollection.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/ScrollRequest.cs`

```csharp
using System;
using System.Collections.Generic;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    /// <summary>
    /// A minimal push source for ScrollToItemBehavior.Item, which wants an IObservable.
    /// The project has no System.Reactive, and one Publish method is all the page needs.
    /// </summary>
    public sealed class ScrollRequest : IObservable<object>
    {
        private readonly List<IObserver<object>> _observers = new();

        public IDisposable Subscribe(IObserver<object> observer)
        {
            _observers.Add(observer);
            return new Subscription(this, observer);
        }

        public void Publish(object item)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(item);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ScrollRequest _owner;
            private readonly IObserver<object> _observer;

            public Subscription(ScrollRequest owner, IObserver<object> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Dispose() => _owner._observers.Remove(_observer);
        }
    }
}
```

#### `Avalonia.BehaviorsDemo/ViewModels/CollectionViewModel.cs`

```csharp
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class CollectionViewModel : ViewModelBase
    {
        // The collection actions work on whatever ItemsSource is bound to, so these must be real collections.
        public ObservableCollection<string> Fruits { get; } = new() { "苹果", "香蕉", "橙子" };

        public ObservableCollection<string> Todos { get; } = new() { "写计划", "跑探针", "提交" };

        public IReadOnlyList<string> Numbers { get; } = Enumerable.Range(0, 200).Select(i => $"第 {i} 项").ToList();

        public IReadOnlyList<string> Colors { get; } = new[] { "红", "橙", "黄", "绿", "青", "蓝", "紫", "灰" };

        public ScrollRequest ScrollRequests { get; } = new();

        [RelayCommand]
        private void ScrollTo(string? index) => ScrollRequests.Publish(Numbers[int.Parse(index ?? "0")]);

        [RelayCommand]
        private void ResetTodos()
        {
            Todos.Clear();
            foreach (var todo in new[] { "写计划", "跑探针", "提交" })
            {
                Todos.Add(todo);
            }
        }
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/CollectionPage.axaml`

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
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.CollectionPage"
             x:DataType="vm:CollectionViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="集合与 ItemsControl：增、删、清空、滚动、全选"
                               DocPath="Avalonia.Xaml.Interactions.Custom.AddItemToItemsControlAction" />

            <TextBlock Classes="caption" Text="1. AddItemToItemsControlAction 与 ClearItemsControlAction" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ListBox Name="FruitList" Width="200" Height="120" ItemsSource="{Binding Fruits}" />
                <StackPanel Spacing="8">
                    <Button Name="AddFruitButton" Content="添加「葡萄」">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <AddItemToItemsControlAction ItemsControl="{Binding #FruitList}" Item="葡萄" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                    <Button Name="ClearFruitsButton" Content="清空">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <ClearItemsControlAction ItemsControl="{Binding #FruitList}" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="这两个 Action 改的是 ItemsSource 绑定的集合（这里是 ObservableCollection），所以列表必须绑 ItemsSource。如果列表只是在 XAML 里手写了几个子项，点按钮什么都不会发生，也不会有任何报错。" />

            <TextBlock Classes="caption" Text="2. RemoveItemInItemsControlAction：放在列表项模板里，删掉自己这一项" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ListBox Name="TodoList" Width="260" Height="130" ItemsSource="{Binding Todos}">
                    <ListBox.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <Grid ColumnDefinitions="*,Auto">
                                <TextBlock VerticalAlignment="Center" Text="{Binding}" />
                                <Button Grid.Column="1" Content="删除">
                                    <Interaction.Behaviors>
                                        <EventTriggerBehavior EventName="Click">
                                            <RemoveItemInItemsControlAction />
                                        </EventTriggerBehavior>
                                    </Interaction.Behaviors>
                                </Button>
                            </Grid>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
                <Button VerticalAlignment="Top" Content="恢复三项" Command="{Binding ResetTodosCommand}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="RemoveItemInItemsControlAction 没有任何属性：它把所在列表项的 DataContext 从所属 ItemsControl 的 ItemsSource 里移除，所以必须放在 ItemTemplate 里面。" />

            <TextBlock Classes="caption" Text="3. ScrollToItemBehavior：推一个元素进去，列表就滚到它" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ListBox Name="BigList" Width="200" Height="120" ItemsSource="{Binding Numbers}">
                    <Interaction.Behaviors>
                        <ScrollToItemBehavior Item="{Binding ScrollRequests}" />
                    </Interaction.Behaviors>
                </ListBox>
                <Button Name="ScrollButton" VerticalAlignment="Top" Content="滚到第 150 项"
                        Command="{Binding ScrollToCommand}" CommandParameter="150" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="Item 属性的类型是 IObservable，行为订阅它，每收到一个元素就滚动到该元素。ViewModel 里的 ScrollRequest 是一个只有 Publish 方法的最小实现，项目没有引入 System.Reactive。" />

            <TextBlock Classes="caption" Text="4. ListBoxSelectAllBehavior：Ctrl+A 全选" />
            <ListBox Name="MultiList" Width="200" Height="130" HorizontalAlignment="Left"
                     SelectionMode="Multiple" ItemsSource="{Binding Colors}">
                <Interaction.Behaviors>
                    <ListBoxSelectAllBehavior />
                </Interaction.Behaviors>
            </ListBox>
            <TextBlock Classes="hint" Text="先点一下列表让它获得焦点，再按 Ctrl+A。列表的 SelectionMode 要是 Multiple。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/CollectionPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class CollectionPage : UserControl
    {
        public CollectionPage()
        {
            InitializeComponent();
            DataContext = new CollectionViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 7 条断言。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo collection page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 2: 页面 7 窗口、弹出与通知

**Files:**
- Create: `Views/ChildWindow.axaml(.cs)`、`Views/Pages/WindowPopupPage.axaml(.cs)`（本页不需要 ViewModel）
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Popup.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `ChildWindow`（按钮 `CloseChildButton` 上挂 `CloseWindowAction`）；`WindowPopupPage.Child`（最近一次打开的子窗口）。控件名：`FlyoutOpener`、`FlyoutAnchor`、`PopupOpener`、`DemoPopup`、`TipOpener`、`TipTarget`、`SuccessButton`、`InfoButton`、`WarningButton`、`ErrorButton`、`UnboundButton`、`OpenChildButton`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Popup.cs`**

```csharp
using Avalonia.BehaviorsDemo.Views;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbePopup
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 7: windows, popups, notifications ==");
        var page = new WindowPopupPage();
        var w = Show(page, 900, 1000);

        Click(Find<Button>(page, "FlyoutOpener")); Pump();
        Check("ShowFlyoutAction opened the flyout attached to another control",
            FlyoutBase.GetAttachedFlyout(Find<Border>(page, "FlyoutAnchor"))!.IsOpen);

        Click(Find<Button>(page, "PopupOpener")); Pump();
        Check("ShowPopupAction opened the popup", Find<Popup>(page, "DemoPopup").IsOpen);

        Click(Find<Button>(page, "TipOpener")); Pump();
        Check("ShowToolTipAction opened the tooltip of another control", ToolTip.GetIsOpen(Find<Border>(page, "TipTarget")));

        int Cards() => w.GetVisualDescendants().OfType<NotificationCard>().Count();
        Check("no notification cards before any click", Cards() == 0);
        Click(Find<Button>(page, "UnboundButton")); Pump(20);
        Check("an action without a bound NotificationManager shows nothing", Cards() == 0);
        Click(Find<Button>(page, "SuccessButton")); Pump(20);
        Check("ShowSuccessNotificationAction: one card", Cards() == 1);
        Click(Find<Button>(page, "InfoButton")); Click(Find<Button>(page, "WarningButton")); Click(Find<Button>(page, "ErrorButton")); Pump(20);
        Check("information, warning and error add three more cards", Cards() == 4);

        Check("no child window yet", page.Child is null);
        Click(Find<Button>(page, "OpenChildButton")); Pump();
        var child = page.Child!;
        Check("the child window opened", child is not null && child.IsVisible);
        var closed = false;
        child!.Closed += (_, _) => closed = true;
        Click(Find<Button>(child, "CloseChildButton")); Pump();
        Check("CloseWindowAction closed the window that hosts the button", closed && !child.IsVisible);
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
ProbeCollection.Run();
ProbePopup.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写子窗口**

#### `Avalonia.BehaviorsDemo/Views/ChildWindow.axaml`

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="Avalonia.BehaviorsDemo.Views.ChildWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="子窗口"
        Width="320"
        Height="160"
        WindowStartupLocation="CenterOwner">

    <StackPanel Margin="16" Spacing="12" VerticalAlignment="Center">
        <TextBlock TextWrapping="Wrap" Text="这个按钮上没有任何 code-behind：关闭窗口完全由 CloseWindowAction 完成。" />
        <Button Name="CloseChildButton" Content="关闭我" HorizontalAlignment="Center">
            <Interaction.Behaviors>
                <EventTriggerBehavior EventName="Click">
                    <!-- No TargetWindow: it closes the window that hosts the button. -->
                    <CloseWindowAction />
                </EventTriggerBehavior>
            </Interaction.Behaviors>
        </Button>
    </StackPanel>
</Window>
```

#### `Avalonia.BehaviorsDemo/Views/ChildWindow.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views
{
    public partial class ChildWindow : Window
    {
        public ChildWindow()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/WindowPopupPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="900"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.WindowPopupPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="窗口、弹出与通知：ShowFlyoutAction、ShowPopupAction、ShowToolTipAction、通知、CloseWindowAction"
                               DocPath="Avalonia.Xaml.Interactions.Custom.ShowFlyoutAction" />

            <TextBlock Classes="caption" Text="1. 按钮触发，弹出的却是另一个控件上的 Flyout" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="FlyoutOpener" Content="打开 Flyout">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <ShowFlyoutAction TargetControl="{Binding #FlyoutAnchor}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Border Name="FlyoutAnchor" Classes="stage" Padding="12,8">
                    <FlyoutBase.AttachedFlyout>
                        <Flyout>
                            <TextBlock Text="我是挂在右边这个方块上的 Flyout" />
                        </Flyout>
                    </FlyoutBase.AttachedFlyout>
                    <TextBlock Text="Flyout 的锚点" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="不写 TargetControl 时，用的是触发控件自己身上挂的 Flyout；写了就可以让任何控件去打开任何控件上的 Flyout。" />

            <TextBlock Classes="caption" Text="2. ShowPopupAction：打开页面里的 Popup" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="PopupOpener" Content="打开 Popup">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <ShowPopupAction Popup="{Binding #DemoPopup}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Popup Name="DemoPopup" PlacementTarget="{Binding #PopupOpener}" Placement="Bottom" IsLightDismissEnabled="True">
                    <Border Background="#303040" BorderBrush="#808090" BorderThickness="1" Padding="12">
                        <TextBlock Text="点 Popup 之外的任何地方就会关闭它" />
                    </Border>
                </Popup>
            </StackPanel>

            <TextBlock Classes="caption" Text="3. ShowToolTipAction：不用悬停，直接弹出另一个控件的 ToolTip" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="TipOpener" Content="显示 ToolTip">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <ShowToolTipAction TargetControl="{Binding #TipTarget}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Border Name="TipTarget" Classes="stage" Padding="12,8" ToolTip.Tip="我是被远程唤出的 ToolTip">
                    <TextBlock Text="ToolTip 的宿主" />
                </Border>
            </StackPanel>

            <TextBlock Classes="caption" Text="4. NotificationManagerBehavior 与四种通知 Action" />
            <Border Classes="stage" Padding="12">
                <Interaction.Behaviors>
                    <!-- Name it so the actions can bind to its NotificationManager. -->
                    <NotificationManagerBehavior x:Name="NM" />
                </Interaction.Behaviors>
                <WrapPanel>
                    <Button Name="SuccessButton" Content="成功" Margin="0,0,8,0">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <ShowSuccessNotificationAction NotificationManager="{Binding #NM.NotificationManager}" Title="成功" Message="保存完成" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                    <Button Name="InfoButton" Content="信息" Margin="0,0,8,0">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <ShowInformationNotificationAction NotificationManager="{Binding #NM.NotificationManager}" Title="信息" Message="有一条新消息" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                    <Button Name="WarningButton" Content="警告" Margin="0,0,8,0">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <ShowWarningNotificationAction NotificationManager="{Binding #NM.NotificationManager}" Title="警告" Message="磁盘空间不足" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                    <Button Name="ErrorButton" Content="错误" Margin="0,0,8,0">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <ShowErrorNotificationAction NotificationManager="{Binding #NM.NotificationManager}" Title="错误" Message="连接失败" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                    <Button Name="UnboundButton" Content="没绑定 NotificationManager 的成功通知">
                        <Interaction.Behaviors>
                            <EventTriggerBehavior EventName="Click">
                                <ShowSuccessNotificationAction Title="不会出现" Message="没有绑定 NotificationManager" />
                            </EventTriggerBehavior>
                        </Interaction.Behaviors>
                    </Button>
                </WrapPanel>
            </Border>
            <TextBlock Classes="hint"
                       Text="通知卡片出现在窗口右上角。NotificationManagerBehavior 在宿主控件挂载时创建窗口级的通知管理器；每个 Action 的 NotificationManager 必须显式绑到它（行为要有 x:Name）。最后一个按钮故意不绑：什么都不会出现，也没有任何报错。" />

            <TextBlock Classes="caption" Text="5. CloseWindowAction：关闭按钮所在的窗口" />
            <Button Name="OpenChildButton" Content="打开子窗口" HorizontalAlignment="Left" Click="OnOpenChild" />
            <TextBlock Classes="hint"
                       Text="子窗口里的「关闭我」按钮没有任何 code-behind。CloseWindowAction 不写 TargetWindow 时关闭所在窗口，所以它只能放在子窗口里，放在主窗口里就会把整个程序关掉。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/WindowPopupPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class WindowPopupPage : UserControl
    {
        public WindowPopupPage()
        {
            InitializeComponent();
        }

        // The most recently opened child window; exposed so a probe can reach it.
        public ChildWindow? Child { get; private set; }

        private void OnOpenChild(object? sender, RoutedEventArgs e)
        {
            Child = new ChildWindow();
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                Child.Show(owner);
            }
            else
            {
                Child.Show();
            }
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 10 条断言，累计 91 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo window, popup and notification page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 3: 页面 8 拖拽

**Files:**
- Create: `ViewModels/DragViewModel.cs`、`Views/Pages/DragPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Drag.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `DragViewModel`：`Order`（`ObservableCollection<string>`，初值 A、B、C、D）、`OrderText`、`DropLog`（`EventLog`）、命令 `TextDroppedCommand(object?)`、`FilesDroppedCommand(object?)`。控件名：`CanvasChip`、`MouseChip`、`OrderList`、`TextDropZone`、`FilesDropZone`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Drag.cs`**

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Avalonia.Xaml.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeDrag
{
    // Real mouse drag: headless MouseMove must carry the left-button modifier or the behaviors see a hover.
    static void Drag(Window w, Control c, Point from, Point delta, int steps = 2)
    {
        var start = c.TranslatePoint(from, w)!.Value;
        w.MouseDown(start, MouseButton.Left);
        for (var i = 1; i <= steps; i++)
        {
            w.MouseMove(start + new Point(delta.X * i / steps, delta.Y * i / steps), RawInputModifiers.LeftMouseButton);
        }
        w.MouseUp(start + delta, MouseButton.Left);
        Pump(20);
    }

    public static void Run()
    {
        Console.WriteLine("== page 8: drag ==");
        var page = new DragPage();
        var w = Show(page, 900, 1100);
        var vm = (DragViewModel)page.DataContext!;

        var chip = Find<Border>(page, "CanvasChip");
        var left0 = Canvas.GetLeft(chip);
        var top0 = Canvas.GetTop(chip);
        Drag(w, chip, new Point(10, 10), new Point(60, 25));
        Check("CanvasDragBehavior moved Canvas.Left/Top by the mouse delta", Canvas.GetLeft(chip) == left0 + 60 && Canvas.GetTop(chip) == top0 + 25);

        var mouse = Find<Border>(page, "MouseChip");
        var origin0 = mouse.TranslatePoint(new Point(0, 0), w)!.Value;
        Drag(w, mouse, new Point(10, 10), new Point(50, 20));
        var origin1 = mouse.TranslatePoint(new Point(0, 0), w)!.Value;
        Check("MouseDragElementBehavior moved the element through a transform", origin1 - origin0 == new Point(50, 20) && mouse.RenderTransform is TranslateTransform);
        Check("it left Canvas.Left untouched (the two behaviors differ)", Canvas.GetLeft(mouse) == 10);

        Check("the list starts as A,B,C,D", string.Join(",", vm.Order) == "A,B,C,D");
        var list = Find<ListBox>(page, "OrderList");
        var itemA = list.GetVisualDescendants().OfType<ListBoxItem>().First(i => (string?)i.DataContext == "A");
        Drag(w, itemA, new Point(10, 10), new Point(0, 72), steps: 6);
        Check("ListReorderDragBehavior moved A two places down in the ItemsSource", string.Join(",", vm.Order) == "B,C,A,D");
        Check("OrderText follows the collection", vm.OrderText.Contains("B") && vm.OrderText.IndexOf('B') < vm.OrderText.IndexOf('A'));

        // Real file and text drops need the OS drag loop; headless can only prove the behaviors attached.
        Check("TextDropBehavior is attached to its zone",
            Interaction.GetBehaviors(Find<Border>(page, "TextDropZone")).OfType<TextDropBehavior>().Count() == 1);
        Check("FilesDropBehavior is attached to its zone",
            Interaction.GetBehaviors(Find<Border>(page, "FilesDropZone")).OfType<FilesDropBehavior>().Count() == 1);
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
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/DragViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class DragViewModel : ViewModelBase
    {
        public DragViewModel()
        {
            // ListReorderDragBehavior reorders the ItemsSource in place; mirror it as text so the result is readable.
            Order.CollectionChanged += (_, _) => OnPropertyChanged(nameof(OrderText));
        }

        public ObservableCollection<string> Order { get; } = new() { "A", "B", "C", "D" };

        public string OrderText => string.Join(" → ", Order);

        public EventLog DropLog { get; } = new();

        [RelayCommand]
        private void TextDropped(object? payload) => DropLog.Write($"文字投放：参数类型 {payload?.GetType().Name ?? "null"}，内容 {payload}");

        [RelayCommand]
        private void FilesDropped(object? payload) => DropLog.Write($"文件投放：参数类型 {payload?.GetType().Name ?? "null"}");
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/DragPage.axaml`

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
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.DragPage"
             x:DataType="vm:DragViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="拖拽：CanvasDragBehavior、MouseDragElementBehavior、ListReorderDragBehavior，以及文字与文件的投放"
                               DocPath="Avalonia.Xaml.Interactions.Draggable.CanvasDragBehavior" />

            <TextBlock Classes="caption" Text="1. CanvasDragBehavior：在 Canvas 里拖动，改的是 Canvas.Left 与 Canvas.Top" />
            <Border Classes="stage" Width="360" Height="120" HorizontalAlignment="Left" ClipToBounds="True">
                <Canvas>
                    <Border Name="CanvasChip" Canvas.Left="10" Canvas.Top="10" Width="40" Height="40"
                            Background="DarkOrange" CornerRadius="4">
                        <Interaction.Behaviors>
                            <CanvasDragBehavior />
                        </Interaction.Behaviors>
                    </Border>
                </Canvas>
            </Border>

            <TextBlock Classes="caption" Text="2. MouseDragElementBehavior：拖动靠 RenderTransform，不改布局属性" />
            <Border Classes="stage" Width="360" Height="120" HorizontalAlignment="Left" ClipToBounds="True">
                <Canvas>
                    <Border Name="MouseChip" Canvas.Left="10" Canvas.Top="10" Width="40" Height="40"
                            Background="SteelBlue" CornerRadius="4">
                        <Interaction.Behaviors>
                            <MouseDragElementBehavior />
                        </Interaction.Behaviors>
                    </Border>
                </Canvas>
            </Border>
            <TextBlock Classes="hint"
                       Text="两者看起来都是「能拖」，区别在内部：第一个把新位置写回 Canvas.Left / Canvas.Top，第二个给元素加一个 TranslateTransform，Canvas.Left 始终是 10。第二种不要求父容器是 Canvas。" />

            <TextBlock Classes="caption" Text="3. ListReorderDragBehavior：拖动列表项重新排序" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ListBox Name="OrderList" Width="200" ItemsSource="{Binding Order}">
                    <ListBox.Styles>
                        <Style Selector="ListBoxItem">
                            <Setter Property="(Interaction.Behaviors)">
                                <BehaviorCollectionTemplate>
                                    <BehaviorCollection>
                                        <ListReorderDragBehavior Orientation="Vertical" />
                                    </BehaviorCollection>
                                </BehaviorCollectionTemplate>
                            </Setter>
                        </Style>
                    </ListBox.Styles>
                </ListBox>
                <TextBlock VerticalAlignment="Top" Text="{Binding OrderText, StringFormat={}当前顺序：{0}}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="行为必须通过 ListBoxItem 的样式（BehaviorCollectionTemplate）挂到每一项上；直接写进 DataTemplate 里面拖不动。它改的是 ItemsSource 本身，所以要绑 ObservableCollection。" />

            <TextBlock Classes="caption" Text="4. TextDropBehavior 与 FilesDropBehavior：接收从别处拖进来的内容（需目视）" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Name="TextDropZone" Classes="stage" Width="240" Height="80" DragDrop.AllowDrop="True" Background="#20FFFFFF">
                    <Interaction.Behaviors>
                        <TextDropBehavior Command="{Binding TextDroppedCommand}" />
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="把选中的文字拖到这里" />
                </Border>
                <Border Name="FilesDropZone" Classes="stage" Width="240" Height="80" DragDrop.AllowDrop="True" Background="#20FFFFFF">
                    <Interaction.Behaviors>
                        <FilesDropBehavior Command="{Binding FilesDroppedCommand}" />
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="把文件拖到这里" />
                </Border>
            </StackPanel>
            <Border Classes="stage" Padding="8" Height="110" Margin="0,8,0,0">
                <ListBox ItemsSource="{Binding DropLog.Entries}" />
            </Border>
            <TextBlock Classes="hint"
                       Text="这两个投放行为只演示读到了什么：下面的记录里写着命令收到的参数类型。真实的拖放需要操作系统的拖放循环，自动化测试只能验证行为已经挂上，所以这一块要手动试：从记事本拖一段文字、从资源管理器拖一个文件进来。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/DragPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class DragPage : UserControl
    {
        public DragPage()
        {
            InitializeComponent();
            DataContext = new DragViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 8 条断言，累计 99 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo drag page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 4: 页面 9 响应式

**Files:**
- Create: `ViewModels/ResponsiveViewModel.cs`、`Views/Pages/ResponsivePage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Responsive.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `ResponsiveViewModel.PanelWidth`（double，初值 360）。控件名：`AdaptiveBox`、`AdaptiveLabel`、`RatioBox`、`RatioLabel`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Responsive.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeResponsive
{
    public static void Run()
    {
        Console.WriteLine("== page 9: responsive ==");
        var page = new ResponsivePage();
        Show(page, 900, 700);
        var vm = (ResponsiveViewModel)page.DataContext!;
        var adaptive = Find<Border>(page, "AdaptiveBox");
        var ratio = Find<Border>(page, "RatioBox");

        vm.PanelWidth = 200; Pump();
        Check("width 200: AdaptiveBehavior adds narrow and not wide", adaptive.Classes.Contains("narrow") && !adaptive.Classes.Contains("wide"));
        Check("width 200 x height 100: AspectRatioBehavior adds flat", ratio.Classes.Contains("flat") && !ratio.Classes.Contains("tall"));
        Check("a style keyed on the class changed the label (no local value on it)", Find<TextBlock>(page, "AdaptiveLabel").Text == "窄：宽度小于 300");

        vm.PanelWidth = 480; Pump();
        Check("width 480: narrow is removed and wide added", adaptive.Classes.Contains("wide") && !adaptive.Classes.Contains("narrow"));

        vm.PanelWidth = 80; Pump();
        Check("width 80 x height 100: the ratio drops below 1 and tall replaces flat", ratio.Classes.Contains("tall") && !ratio.Classes.Contains("flat"));
        Check("the ratio label follows", Find<TextBlock>(page, "RatioLabel").Text == "高：宽高比小于 1");
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
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/ResponsiveViewModel.cs`

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class ResponsiveViewModel : ViewModelBase
    {
        // The slider drives the width of both boxes; the behaviors only ever see the resulting Bounds.
        [ObservableProperty]
        private double _panelWidth = 360;
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/ResponsivePage.axaml`

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
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.ResponsivePage"
             x:DataType="vm:ResponsiveViewModel">

    <UserControl.Styles>
        <!-- The behaviors only toggle style classes; these selectors turn each class into something visible. -->
        <!-- The labels must not set Text locally, or a local value would suppress these setters. -->
        <Style Selector="Border.narrow">
            <Setter Property="Background" Value="DarkOrange" />
        </Style>
        <Style Selector="Border.narrow > TextBlock.label">
            <Setter Property="Text" Value="窄：宽度小于 300" />
        </Style>
        <Style Selector="Border.wide">
            <Setter Property="Background" Value="SteelBlue" />
        </Style>
        <Style Selector="Border.wide > TextBlock.label">
            <Setter Property="Text" Value="宽：宽度不小于 300" />
        </Style>
        <Style Selector="Border.tall">
            <Setter Property="Background" Value="MediumPurple" />
        </Style>
        <Style Selector="Border.tall > TextBlock.label">
            <Setter Property="Text" Value="高：宽高比小于 1" />
        </Style>
        <Style Selector="Border.flat">
            <Setter Property="Background" Value="SeaGreen" />
        </Style>
        <Style Selector="Border.flat > TextBlock.label">
            <Setter Property="Text" Value="扁：宽高比不小于 1" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="响应式：AdaptiveBehavior 与 AspectRatioBehavior"
                               DocPath="Avalonia.Xaml.Interactions.Responsive.AdaptiveBehavior" />

            <TextBlock Classes="caption" Text="拖动滑块改变下面两个盒子的宽度" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Width="320" Minimum="60" Maximum="600" Value="{Binding PanelWidth}" />
                <TextBlock VerticalAlignment="Center" Text="{Binding PanelWidth, StringFormat={}宽度 {0:0}}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="1. AdaptiveBehavior：按宽度加减样式类" />
            <Border Name="AdaptiveBox" Width="{Binding PanelWidth}" Height="60" HorizontalAlignment="Left" CornerRadius="4">
                <Interaction.Behaviors>
                    <AdaptiveBehavior>
                        <AdaptiveBehavior.Setters>
                            <AdaptiveClassSetter MinWidth="0" MaxWidth="300" ClassName="narrow" />
                            <AdaptiveClassSetter MinWidth="300" ClassName="wide" />
                        </AdaptiveBehavior.Setters>
                    </AdaptiveBehavior>
                </Interaction.Behaviors>
                <TextBlock Name="AdaptiveLabel" Classes="label" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>
            <TextBlock Classes="hint"
                       Text="每个 AdaptiveClassSetter 描述一个宽度（或高度）区间和一个类名；盒子的尺寸落进哪个区间，行为就给它加上哪个类，离开区间就摘掉。颜色与文字都由样式表按类名决定，行为本身不碰它们。" />

            <TextBlock Classes="caption" Text="2. AspectRatioBehavior：按宽高比加减样式类（高度固定 100）" />
            <Border Name="RatioBox" Width="{Binding PanelWidth}" Height="100" HorizontalAlignment="Left" CornerRadius="4">
                <Interaction.Behaviors>
                    <AspectRatioBehavior>
                        <AspectRatioBehavior.Setters>
                            <AspectRatioClassSetter MaxRatio="1" ClassName="tall" />
                            <AspectRatioClassSetter MinRatio="1" ClassName="flat" />
                        </AspectRatioBehavior.Setters>
                    </AspectRatioBehavior>
                </Interaction.Behaviors>
                <TextBlock Name="RatioLabel" Classes="label" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>
            <TextBlock Classes="hint" Text="宽度小于 100 时宽高比小于 1，盒子变紫色；否则是绿色。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/ResponsivePage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ResponsivePage : UserControl
    {
        public ResponsivePage()
        {
            InitializeComponent();
            DataContext = new ResponsiveViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 6 条断言，累计 105 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo responsive page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 5: 页面 10 动画

**Files:**
- Create: `Views/Pages/AnimationPage.axaml(.cs)`（本页不需要 ViewModel）
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Animation.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: 控件名 `FadeBox`、`PlayBox`、`BeginTarget`、`BeginButton`、`ShakeBox`、`ShakeButton`、`TiltBox`、`ParallaxBox`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Animation.cs`**

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeAnimation
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 10: animation ==");
        var page = new AnimationPage();

        // FadeInBehavior plays once on attach, so the listener must exist before the window shows the page.
        var fade = page.FindControl<Border>("FadeBox")!;
        var samples = new List<double>();
        fade.PropertyChanged += (_, e) => { if (e.Property == Visual.OpacityProperty) samples.Add((double)e.NewValue!); };
        Show(page, 900, 900);
        RunLoop(700);
        Check("FadeInBehavior animated the opacity on attach (it dipped below 0.5)", samples.Count > 0 && samples.Min() < 0.5);
        Check("FadeInBehavior ended fully opaque", fade.Opacity > 0.99);

        var play = Find<Border>(page, "PlayBox");
        Check("PlayAnimationBehavior ran the referenced Animation on attach (opacity settled at 0.4)", WaitUntil(() => Math.Abs(play.Opacity - 0.4) < 0.01, 2000));

        var target = Find<Border>(page, "BeginTarget");
        Check("BeginTarget is untouched before the click", target.Opacity == 1);
        Click(Find<Button>(page, "BeginButton"));
        Check("BeginAnimationAction played the Animation on another control", WaitUntil(() => Math.Abs(target.Opacity - 0.4) < 0.01, 2000));

        var shakeThrew = false;
        try { Click(Find<Button>(page, "ShakeButton")); RunLoop(100); } catch { shakeThrew = true; }
        Check("AttentionAnimations.SetShakeX from code-behind ran without throwing", !shakeThrew);

        // Real tilt and parallax need pointer motion and a scrolling source; headless can only prove they attached.
        Check("TiltEffectBehavior is attached", Interaction.GetBehaviors(Find<Border>(page, "TiltBox")).OfType<TiltEffectBehavior>().Count() == 1);
        Check("ParallaxBehavior is attached", Interaction.GetBehaviors(Find<Border>(page, "ParallaxBox")).OfType<ParallaxBehavior>().Count() == 1);
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
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
ProbeAnimation.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/AnimationPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="900"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.AnimationPage">

    <UserControl.Styles>
        <!-- An Animation cannot be written inline inside a behavior or action (AVLN2200), -->
        <!-- so it lives in a style's resources and the behaviors reference it with DynamicResource. -->
        <Style Selector="Border.anim">
            <Style.Resources>
                <Animation x:Key="Dim" Duration="0:0:0.2" FillMode="Forward">
                    <KeyFrame Cue="0%">
                        <Setter Property="Opacity" Value="1" />
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Opacity" Value="0.4" />
                    </KeyFrame>
                </Animation>
            </Style.Resources>
        </Style>
    </UserControl.Styles>

    <ScrollViewer Name="PageScroller">
        <StackPanel Margin="12">
            <shared:DemoHeader Title="动画：FadeInBehavior、PlayAnimationBehavior、BeginAnimationAction、内置动画与倾斜视差"
                               DocPath="Avalonia.Xaml.Interactions.Custom.FadeInBehavior" />

            <TextBlock Classes="caption" Text="1. FadeInBehavior：控件挂载时淡入（切换标签页再回来可以重新看到）" />
            <Border Name="FadeBox" Classes="stage" Width="240" Height="60" HorizontalAlignment="Left" Background="SteelBlue">
                <Interaction.Behaviors>
                    <FadeInBehavior InitialDelay="0:0:0.1" Duration="0:0:0.4" />
                </Interaction.Behaviors>
                <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="我在挂载时淡入" />
            </Border>
            <TextBlock Classes="hint" Text="只在控件挂到可视树的那一刻播放一次，不是每次属性变化都播。" />

            <TextBlock Classes="caption" Text="2. PlayAnimationBehavior：挂载时播放一个资源里定义好的 Animation" />
            <Border Name="PlayBox" Classes="stage anim" Width="240" Height="60" HorizontalAlignment="Left" Background="DarkOrange">
                <Interaction.Behaviors>
                    <PlayAnimationBehavior Animation="{DynamicResource Dim}" />
                </Interaction.Behaviors>
                <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="挂载后变暗并停在 0.4" />
            </Border>

            <TextBlock Classes="caption" Text="3. BeginAnimationAction：事件触发，让另一个控件播放同一个 Animation" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="BeginButton" Content="让右边的方块变暗">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <BeginAnimationAction Animation="{DynamicResource Dim}" TargetControl="{Binding #BeginTarget}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Border Name="BeginTarget" Classes="stage anim" Width="160" Height="60" Background="SeaGreen">
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="目标方块" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="Animation 必须放进样式的 Style.Resources 再用 DynamicResource 引用；直接写在行为或 Action 里面会编译失败。动画结束后 FillMode=Forward 让不透明度停在末值，刷新页面才会还原。" />

            <TextBlock Classes="caption" Text="4. 内置动画库：AttentionAnimations.SetShakeX 等一行调用" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="ShakeButton" Content="摇一摇" Click="OnShake" />
                <Border Name="ShakeBox" Classes="stage" Width="160" Height="60" Background="MediumPurple">
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="我会左右晃动" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="EntranceAnimations、AttentionAnimations、ExitAnimations 三组都是 SetXxx(控件, 毫秒) 的形态，没有对应的 XAML 行为，所以这里用按钮的 Click 事件在 code-behind 里调用。" />

            <TextBlock Classes="caption" Text="5. TiltEffectBehavior 与 ParallaxBehavior（需目视）" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Name="TiltBox" Classes="stage" Width="200" Height="100" Background="#304060">
                    <Interaction.Behaviors>
                        <TiltEffectBehavior TiltStrength="12" />
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="鼠标在上面移动，看它倾斜" />
                </Border>
                <Border Name="ParallaxBox" Classes="stage" Width="200" Height="100" Background="#403060">
                    <Interaction.Behaviors>
                        <ParallaxBehavior SourceScrollViewer="{Binding #PageScroller}" ParallaxRatio="0.3" />
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="滚动页面，看它错位跟随" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="这两个效果靠指针位置和滚动偏移驱动，自动化测试只能验证行为已挂上，真实效果请手动试。视差需要页面能滚动：把窗口调矮一些。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/AnimationPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.Custom;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class AnimationPage : UserControl
    {
        public AnimationPage()
        {
            InitializeComponent();
        }

        private void OnShake(object? sender, RoutedEventArgs e) => AttentionAnimations.SetShakeX(ShakeBox, 500);
    }
}
```

- [ ] **Step 3: 抽取、构建并运行探针**

Run: `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 8 条断言，累计 113 条。

- [ ] **Step 4: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo animation page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 6: 登记全部页面、整体探针、README 与 spec 回填

**Files:**
- 覆盖: `Avalonia.BehaviorsDemo/Views/MainWindow.axaml`（5 个标签 → 10 个标签）
- Modify: `README.md`（项目行页数 5 → 10）、`docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md`（追加「批 2 实测结论」）
- Create（仓库外）: 覆盖 `C:\Temp\probe-bdemo\Probe.Shell.cs`

- [ ] **Step 1: 改写 Shell 探针（先于 MainWindow）**

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
        Check("MainWindow lists 10 tabs", tabs.Items.Count == 10);
        var headers = tabs.Items.OfType<TabItem>().Select(t => t.Header?.ToString()).ToArray();
        Check("tab headers match the page order",
            string.Join("|", headers) == "机制|事件转命令|数据驱动|焦点键盘指针|表单验证|集合|窗口弹出通知|拖拽|响应式|动画");
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

- [ ] **Step 2: 写完整的 MainWindow（覆盖计划 01 的 5 标签版本）**

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
        <TabItem Header="集合">
            <pages:CollectionPage />
        </TabItem>
        <TabItem Header="窗口弹出通知">
            <pages:WindowPopupPage />
        </TabItem>
        <TabItem Header="拖拽">
            <pages:DragPage />
        </TabItem>
        <TabItem Header="响应式">
            <pages:ResponsivePage />
        </TabItem>
        <TabItem Header="动画">
            <pages:AnimationPage />
        </TabItem>
    </TabControl>
</Window>
```

- [ ] **Step 3: 抽取、构建、运行全部探针**

Run: `dotnet build Avalonia.BehaviorsDemo 2>&1 | grep -E " error |个错误"` 然后 `dotnet run --project C:\Temp\probe-bdemo 2>&1 | grep -E "error|FAIL|passed|\[log"`
Expected: 构建 0 个错误；探针无 `FAIL`，末行 `118 passed, 0 failed, 0 warning log(s)`（计划 01 的 74 条里 Shell 的 7 条换成 12 条，再加页面 6–10 的 7 + 10 + 8 + 6 + 8 = 39 条）。

- [ ] **Step 4: 更新 README 的项目行**

把计划 01 写入的那一行（`| [Avalonia.BehaviorsDemo]…`）整行替换为：

```markdown
| [Avalonia.BehaviorsDemo](Avalonia.BehaviorsDemo) | Xaml.Behaviors.Avalonia：Behavior / Trigger / Action 机制、事件转命令（防抖、节流、延时）、数据触发与条件、焦点键盘指针、表单验证、集合、窗口弹出通知、拖拽、响应式、动画（侧边导航，当前 10 页；副作用动作与自定义见后续批次） |
```

- [ ] **Step 5: 回填 spec 的「批 2 实测结论」**

在 spec 末尾（「批 1 实测结论」小节之后）追加下面这段：

```markdown

## 批 2 实测结论（计划 02 执行后）

- **集合 Action 作用在 `ItemsSource` 绑定的集合上**：`AddItemToItemsControlAction`、`ClearItemsControlAction` 对手写子项的列表什么都不做，也没有日志；`RemoveItemInItemsControlAction` 没有属性，删的是所在列表项的 DataContext，只能放在 `ItemTemplate` 里。
- **`ScrollToItemBehavior.Item` 是 `IObservable<object>`**：项目没有引入 System.Reactive，用一个只有 `Publish` 的 `ScrollRequest` 即可。
- **通知必须显式绑定管理器**：`NotificationManagerBehavior` 要有 `x:Name`，每个 `Show*NotificationAction.NotificationManager` 绑 `{Binding #NM.NotificationManager}`；不绑就不显示，也没有日志。
- **`CloseWindowAction` 不写 `TargetWindow` 时关掉所在窗口**，所以只能放在子窗口里。
- **`ListReorderDragBehavior` 必须通过 `ListBoxItem` 样式里的 `BehaviorCollectionTemplate` 挂载**；直接写进 `DataTemplate` 的子元素里拖不动。`CanvasDragBehavior` 改 `Canvas.Left/Top`，`MouseDragElementBehavior` 加 `TranslateTransform`。headless 里拖动的 `MouseMove` 必须带 `RawInputModifiers.LeftMouseButton`。
- **文字与文件投放只能验证行为已挂载**：真实投放要操作系统的拖放循环，页面上注明需手动试。
- **`AdaptiveBehavior` / `AspectRatioBehavior` 只加减样式类**，颜色与文字由样式表按类名决定；标签不能在本地写 `Text`，否则会压制样式里的 Setter。
- **`Animation` 不能直接写在行为或 Action 里**（AVLN2200）：放进某个 `Style` 的 `Style.Resources`，用 `DynamicResource` 引用；`PlayAnimationBehavior` 与 `BeginAnimationAction` 都能让不透明度落到 0.4。`FadeInBehavior` 只在挂载时播一次。`AttentionAnimations.SetShakeX` 等是 `SetXxx(Control, double)` 形态，没有 XAML 行为。`TiltEffectBehavior` 与 `ParallaxBehavior` 只能验证已挂载，真实效果需目视。
- 探针累计 118 条断言全部通过，0 条警告日志。
```

- [ ] **Step 6: 全解决方案构建，提交**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E " error |个错误"`
Expected: 0 个错误。

```bash
git add Avalonia.BehaviorsDemo README.md docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md
git commit -m "feat: register all ten BehaviorsDemo pages and record batch 2 findings

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

- **Spec 覆盖**：页面 6–10（Task 1–5）、10 标签导航与 README、spec 回填（Task 6）。页面 11–15 属于计划 03。
- **占位扫描**：无 TBD/TODO；每个代码步骤都有完整代码，每个探针断言都有明确期望。
- **类型一致**：各页 ViewModel 的命令名由 `[RelayCommand]` 方法名加 `Command` 生成（`ScrollTo`→`ScrollToCommand`、`ResetTodos`→`ResetTodosCommand`、`TextDropped`→`TextDroppedCommand`、`FilesDropped`→`FilesDroppedCommand`）；控件名在探针与 XAML 中逐一对照过；`Program.cs` 在每个任务里都是累积版本，后者覆盖前者。
- **实测过程**：本计划的所有代码块都用计划 01 附录 A 的脚本抽取、构建并跑通了探针（见 Task 6 Step 3 的期望数）。
