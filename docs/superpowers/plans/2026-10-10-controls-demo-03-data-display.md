# ControlsDemo 03：Data display 12 个控件页与 4 篇实战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全 `Avalonia.ControlsDemo` 的 Data display 分类：Collections 3 页、Structured data 3 页、Text display 4 页、独立页 2 页，共 12 个 Control 页；加 ItemsControl、ListBox、DataGrid、TreeView 4 篇 How-to 实战页；同时引入 `Avalonia.Controls.DataGrid` 包并启用它的样式。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份新增页面文件、3 个示例模型（`DemoProduct`、`DemoTreeNode`、`TreeViewHowToViewModel` 里的节点类）、`PageCatalog.DataDisplay.cs` 的登记行、csproj 的一个包引用、`App.axaml` 的一条 `StyleInclude`，以及探针 `Probe.Data.cs`。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2（树节点与 How-to 的 ViewModel）、`Avalonia.Controls.DataGrid` 12.1.2

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00、01、02 份已执行（`PageCatalog`、`Categories.DataDisplay`、`Harness`、`Directory.Packages.props` 里的 `Avalonia.Controls.DataGrid` 版本声明已存在）。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」，**不重复贴**，执行者必须先读第 00 份；第 01、02 份「页面的写法约定」同样适用。本份额外强调：

- 页面类名 `<控件>Page`，How-to 页 `<主题>HowToPage`；命名空间一律 `Avalonia.ControlsDemo.Views.Pages`，模型与 ViewModel 一律 `Avalonia.ControlsDemo.ViewModels`
- `DocPath` 已用官方 `sitemap.xml` 核对：控件页 `controls/data-display/...`，How-to 页 `docs/how-to/<主题>-how-to`
- **事件接线放在构造函数里、`InitializeComponent()` 之后**；枚举下拉先设 `SelectedItem` 再订阅 `SelectionChanged`
- **`DataGrid` 必须有 `StyleInclude`**（本份 Task 1），否则构建通过、运行无异常、界面空白（第 00 份 Task 7 已实测）；`TableView` 在主包里，**不需要**任何额外样式
- 页面里嵌套 `ScrollViewer` 的坑（第 02 份）：`DataGrid`、`ListBox`、`TreeView`、`TableView` 自带滚动，放进页面时必须给固定 `Height`，否则撑满内容
- 编译绑定默认开启：**`DataGrid` 列、`TableViewColumn` 的 `Binding` 不在可视树里**，必须在列上写 `x:DataType`（本份各页已写），否则报 AVLN2100
- 去重规则：`TreeView` 在 `Avalonia.DataTemplatesDemo`「面板与树」与 `Avalonia.FundamentalsDemo` 的 TreesPage 有模板对照；`ListBox` / `ItemsControl` 在 `Avalonia.DataBindingDemo`「集合」「集合视图」有绑定对照。对应页末尾各加一行 `更深入的演示：…`；其余页面不写指向（未核实过的 Tab 名不写）
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时在 Avalonia 12.1.2 headless 实测）

| 控件 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `TableView` | **在主包里**，`TableView : ListBox`；无 `StyleInclude` 即可渲染（400×200：3 行、6 个单元格、2 个表头）；默认 `SelectionMode=Single`；列用 `TableViewColumn`（`Header`、`Width`（`GridLength`）、`Binding`、`CanUserResize`），**没有 `TableViewTextColumn`** | 页面直接写 `TableView.Columns`；不引用 DataGrid 包也能演示 |
| `DataGrid` | 默认 `IsReadOnly=False`、`GridLinesVisibility=None`、`HeadersVisibility=Column`、`SelectionMode=Extended`；`AutoGenerateColumns` 按属性声明顺序生成列（`Name,Category,Price,InStock,Description`）；`SelectedIndex=1` 后 `SelectedItem` 是第二项、`CurrentColumn` 是第一列 | 页面读 `SelectedItem`，不依赖列顺序以外的假设 |
| `DataGrid`（列宽） | 600 宽、列 `2*`、`CheckBox`（默认宽度）、`80`：实际宽 391 / 129 / 80，合计 600 | 页面的列宽演示以读数展示，不断言 `CheckBox` 列的像素 |
| `DataGrid`（分组与排序） | `DataGridCollectionView` 加 `DataGridPathGroupDescription("Category")`：2 个组、2 个组头、3 行；`SortDescriptions` 按 `Name` 降序时视图第一项是 `Cherry` | 分组、排序都走 `DataGridCollectionView`，不改源集合 |
| `TreeView` | 新建时节点 `IsExpanded=False`；写 `IsExpanded=true` 后多出 2 个已实现节点；`ExpandSubTree` 把整棵子树展开；**`CollapseSubTree` 期间 `Collapsed` 事件触发 4 次**（子节点的事件向上冒泡）；选中一项触发 1 次 `SelectionChanged` | 计数展开 / 折叠次数不要用 `Collapsed`；页面只显示"已展开节点数"（从模型数） |
| `Carousel` | 默认 `SelectedIndex=0`、`PageTransition=null`、`IsSwipeEnabled=False`；`Next()` → 1；再 `Next()` 两次 → 2（3 项时），**到末项不会回到首项**；`Previous()` → 1 | 页面的"下一张"在末项后停住，并显示该事实 |
| `ListBox` | `Multiple` 模式 `SelectAll()` 选中全部 4 项；切到 `Single` 后只剩 1 项 | 页面切换模式后读 `SelectedItems.Count` |
| `ItemsControl` | 默认 `ItemsPanel` 是 `StackPanel`；设置 2 项后实现 2 个容器 | 页面用 `ItemCount` 与 `GetRealizedContainers()` 读数 |
| `TransitioningContentControl` | 改 `Content` 后 `TransitionCompleted` 触发 1 次，`Content` 已是新值 | 页面计数完成次数 |
| `SelectableTextBlock` | `SelectAll()` → `SelectionStart=0`、`SelectionEnd=14`（`Hello Avalonia`）、`CanCopy=True`；设 `SelectionStart=0`、`SelectionEnd=5` 后 `SelectedText=Hello` | 页面用按钮写选区，读 `SelectedText` |
| `Label` | `Label` 是 `ContentControl`，有 `Target`（`IInputElement`）；`Content="_Name"` 的 `_` 是访问键标记 | 页面把 `Target` 指到旁边的 `TextBox` |
| `TextBlock`（裁剪） | 宽 80、`CharacterEllipsis`、`NoWrap`：`Bounds` 宽 80、`DesiredSize` 宽 80，`Text` 长度仍是 43（**只改画法，不改 `Text`**） | 页面显示"Text 未变，只是画出来被截" |
| `ContentControl` | 模板里的 `ContentPresenter.Content` 等于 `ContentControl.Content` | 页面读 `Content.GetType()` |

**成员核实（反射，Avalonia 12.1.2）**：`Carousel` 有 `PageTransition`、`IsSwipeEnabled`、`ViewportFraction`、`Next()`、`Previous()`；页面过渡类型有 `PageSlide`、`CrossFade`、`Rotate3DTransition`、`CompositePageTransition`；`ItemsControl` 有 `ItemsSource`、`ItemTemplate`、`ItemsPanel`、`DisplayMemberBinding`、`ItemContainerTheme`、`ItemCount`、`GetRealizedContainers()`、`ContainerFromIndex`、`ScrollIntoView`、事件 `PreparingContainer` / `ContainerPrepared` / `ContainerClearing`；`ListBox` 有 `SelectionMode`（`Single` / `Multiple` / `Toggle` / `AlwaysSelected`）、`SelectedItems`、`SelectAll()`、`UnselectAll()`、`Scroll`；`DataGrid` 有 `CanUserReorderColumns` / `CanUserResizeColumns` / `CanUserSortColumns`、`GridLinesVisibility`（`None/Horizontal/Vertical/All`）、`HeadersVisibility`、`SelectionMode`（`Extended/Single`）、`FrozenColumnCount`、`RowDetailsTemplate`、`RowDetailsVisibilityMode`（`VisibleWhenSelected/Visible/Collapsed`）、`ColumnWidth`、事件 `AutoGeneratingColumn` / `BeginningEdit` / `CellEditEnded` / `SelectionChanged` / `Sorting` / `LoadingRow`，列类型 `DataGridTextColumn` / `DataGridCheckBoxColumn` / `DataGridTemplateColumn`（`CellTemplate`、`CellEditingTemplate`）；`TreeView` 有 `SelectionMode`、`SelectedItem`、`SelectedItems`、`ExpandSubTree(TreeViewItem)`、`CollapseSubTree(TreeViewItem)`、`SelectAll()`、`GetRealizedTreeContainers()`、`TreeContainerFromItem`；`TreeViewItem` 有 `IsExpanded`、`Level`、事件 `Expanded` / `Collapsed`；`SelectableTextBlock` 有 `SelectionBrush`、`SelectionStart`、`SelectionEnd`、`SelectedText`、`CanCopy`、事件 `CopyingToClipboard`；`TransitioningContentControl` 有 `PageTransition`、`IsTransitionReversed`、事件 `TransitionCompleted`；`TextTrimming` 的静态成员有 `None`、`CharacterEllipsis`、`WordEllipsis`、`PrefixCharacterEllipsis`、`LeadingCharacterEllipsis`、`PathSegmentEllipsis`。

**官方 How-to 要点（已抓取）**：DataGrid 指南讲包与 `StyleInclude`、`AutoGenerateColumns`、三种列、`SortMemberPath`、`DataGridCollectionView` 分组、`CellEditingTemplate` 编辑、列宽 `Auto` / `*` / 像素 / `SizeToCells` / `SizeToHeader`、`RowDetailsTemplate`、`FrozenColumnCount`；TreeView 指南讲 `TreeDataTemplate` + `ItemsSource`、按 `DataType` 选模板、`SelectedItem(s)`、用 `IsExpanded` 绑定做展开与懒加载（占位子项）；ListBox 指南讲 `SelectionMode`、`ScrollIntoView`、`ItemsPanel` 横向排布、`ListBoxItem:selected` 样式、`CheckBox` 多选；ItemsControl 指南讲无选择无虚拟化的定位、`ItemsPanel` 替换、用 `x:Int32` + `ObjectConverters.Equal` 做空状态、`VirtualizingStackPanel`、`PreparingContainer` 与 `e.Container.Classes`。

**范围缺口（已知，不在本份处理）**：官方 sitemap 的 `data-display` 下还有 `charts/**`（约 100 个图表页）、`pdfviewer/**` 与 `structured-data/treedatagrid/**`，spec 的 12 页列表里没有它们。它们依赖额外（多为付费）包，按 spec 的"付费控件只给导向页"原则，归到第 06 份的 Premium 路标页处理；本份不新增。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Avalonia.ControlsDemo.csproj          改：+DataGrid 包引用
├── App.axaml                             改：+DataGrid 的 StyleInclude
├── Navigation/PageCatalog.DataDisplay.cs 改：+12 控件页 +4 How-to 页
├── ViewModels/
│   ├── DemoProduct.cs                    新：DataGrid / TableView / ContentControl 共用的示例行
│   ├── DemoTreeNode.cs                   新：TreeView 页用的节点（带 IsExpanded）
│   ├── ListBoxHowToViewModel.cs          新
│   ├── DataGridHowToViewModel.cs         新
│   └── TreeViewHowToViewModel.cs         新：FileNode / FolderNode / LazyNode 与页面 VM
└── Views/Pages/DataDisplay/              新增 16 个页面（各含 .axaml 与 .axaml.cs）
    Carousel / ItemsControl / ListBox
    ContentControl / TransitioningContentControl
    DataGrid / TableView / TreeView
    Label / SelectableTextBlock / TextBlock / TextTrimming
    ItemsControlHowTo / ListBoxHowTo / DataGridHowTo / TreeViewHowTo
```

探针（仓库外）：`C:\Temp\probe-controls\Probe.Data.cs`（及 `Probe.Data2.cs`）。

### 页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 页面骨架（`UserControl` + `ScrollViewer` + `StackPanel Margin="12"` + `DemoHeader`）与第 00 份相同；演示块用 `TextBlock.caption` + `Border.stage` + `TextBlock.hint`。
- 枚举下拉一律在 code-behind 构造函数里 `ItemsSource = Enum.GetValues<T>()`，先设 `SelectedItem` 再订阅 `SelectionChanged`。
- 滑块驱动的整数属性在处理器里强转，不走转换器。

---

### Task 1: 引入 DataGrid 包、启用样式，并建共用示例模型

**Files:**
- Modify: `Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj`
- Modify: `Avalonia.ControlsDemo/App.axaml`
- Create: `Avalonia.ControlsDemo/ViewModels/DemoProduct.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/DemoTreeNode.cs`

**Interfaces:**
- Consumes: 第 00 份 Task 1 已在 `Directory.Packages.props` 声明 `Avalonia.Controls.DataGrid` 12.1.2
- Produces:
  - `DemoProduct`：`string Name`、`string Category`、`decimal Price`、`bool InStock`、`string Description`（都有 setter，`DataGrid` 才能编辑）；静态方法 `DemoProduct.Sample()` 返回 6 项（`Apple`/`Cherry`/`Banana` 属 `Fruit`，`Bread`/`Bagel`/`Croissant` 属 `Bakery`）
  - `DemoTreeNode`：`string Label`、`IReadOnlyList<DemoTreeNode> Children`；静态方法 `DemoTreeNode.Sample()` 返回 2 个根（`文档` 下 2 个子项，其中 `报告` 又有 2 个子项；`图片` 下 1 个子项），共 8 个节点

**为什么必须两处都改**：只加包引用，`DataGrid` 构造成功、能布局、但**什么都不画，且不报错不记日志**（第 00 份 Task 7 已实测：不加样式时行数为 0）。包引用和 `StyleInclude` 是一个原子改动，探针里要断言行数大于零。

- [ ] **Step 1: csproj 加包引用**

在 `Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj` 的 `Avalonia.Controls.ColorPicker` 那行之后加一行：

```xml
        <PackageReference Include="Avalonia.Controls.DataGrid" />
```

- [ ] **Step 2: App.axaml 加样式**

`App.axaml` 的 `Application.Styles` 里，ColorPicker 那行之后加（`Themes/Fluent.xaml`，**不是** `Themes/Generic.xaml`，后者会抛 `XamlLoadException`）：

```xml
        <!--  DataGrid draws nothing without this; there is no error and no log.  -->
        <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml" />
```

- [ ] **Step 3: 建示例模型**

#### `Avalonia.ControlsDemo/ViewModels/DemoProduct.cs`

```csharp
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A plain editable row shared by the DataGrid, TableView and ContentControl pages.</summary>
    public sealed class DemoProduct
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool InStock { get; set; }
        public string Description { get; set; } = string.Empty;

        public static List<DemoProduct> Sample() =>
        [
            new() { Name = "Apple", Category = "Fruit", Price = 1.2m, InStock = true, Description = "fresh" },
            new() { Name = "Bread", Category = "Bakery", Price = 3.5m, InStock = false, Description = "daily" },
            new() { Name = "Cherry", Category = "Fruit", Price = 5.0m, InStock = true, Description = "sweet" },
            new() { Name = "Bagel", Category = "Bakery", Price = 2.2m, InStock = true, Description = "seeded" },
            new() { Name = "Banana", Category = "Fruit", Price = 0.8m, InStock = true, Description = "ripe" },
            new() { Name = "Croissant", Category = "Bakery", Price = 2.9m, InStock = false, Description = "buttery" },
        ];
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/DemoTreeNode.cs`

```csharp
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A read-only tree node; TreeDataTemplate walks <see cref="Children" /> to build the next level.</summary>
    public sealed class DemoTreeNode
    {
        public DemoTreeNode(string label, params DemoTreeNode[] children)
        {
            Label = label;
            Children = children;
        }

        public string Label { get; }

        public IReadOnlyList<DemoTreeNode> Children { get; }

        public static List<DemoTreeNode> Sample() =>
        [
            new("文档", new DemoTreeNode("报告", new DemoTreeNode("2025 年报"), new DemoTreeNode("2026 季报")), new DemoTreeNode("合同")),
            new("图片", new DemoTreeNode("旅行")),
        ];
    }
}
```

- [ ] **Step 4: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the DataGrid package, its theme and the shared sample models to ControlsDemo

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: Collections（Carousel、ItemsControl、ListBox）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/DataDisplay/CarouselPage.axaml`、`.axaml.cs`
- Create: `.../ItemsControlPage.axaml`、`.axaml.cs`
- Create: `.../ListBoxPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`
- Produces（探针依赖的元素名）：
  - `CarouselPage`：`Slides`（`Carousel`）、`PreviousButton`、`NextButton`（`Button`）、`TransitionBox`（`ComboBox`）、`SwipeCheck`、`IndexText`
  - `ItemsControlPage`：`Items`（`ItemsControl`）、`PanelBox`（`ComboBox`）、`AddButton`、`RemoveButton`、`CountText`
  - `ListBoxPage`：`List`（`ListBox`）、`ModeBox`（`ComboBox`）、`SelectAllButton`、`ClearButton`、`JumpButton`、`SelectionText`

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/CarouselPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CarouselPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Carousel：一次只显示一项"
                               DocPath="controls/data-display/collections/carousel" />

            <TextBlock Classes="caption" Text="Next() / Previous()、PageTransition 与 IsSwipeEnabled" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="PreviousButton" Content="上一张" />
                        <Button Name="NextButton" Content="下一张" />
                        <TextBlock Name="IndexText" VerticalAlignment="Center" Text="第 1 / 3 张" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="PageTransition" />
                        <ComboBox Name="TransitionBox" Width="150" />
                        <CheckBox Name="SwipeCheck" Content="IsSwipeEnabled（触摸滑动）" />
                    </StackPanel>
                    <Carousel Name="Slides" Width="320" Height="120" HorizontalAlignment="Left">
                        <Border Background="#E8564A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="第 1 张" /></Border>
                        <Border Background="#E8974A"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="第 2 张" /></Border>
                        <Border Background="#4A9BE8"><TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="第 3 张" /></Border>
                    </Carousel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Carousel 是只有 Next() / Previous() 的 SelectingItemsControl，没有内置的指示器，需要圆点时配 PipsPager（见 Layout）。实测：到最后一张后 Next() 不会回到第一张。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/CarouselPage.axaml.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CarouselPage : UserControl
    {
        private static readonly string[] TransitionNames = ["无", "PageSlide", "CrossFade"];

        public CarouselPage()
        {
            InitializeComponent();

            PreviousButton.Click += (_, _) => Slides.Previous();
            NextButton.Click += (_, _) => Slides.Next();
            Slides.SelectionChanged += (_, _) => IndexText.Text = $"第 {Slides.SelectedIndex + 1} / {Slides.ItemCount} 张";

            TransitionBox.ItemsSource = TransitionNames;
            TransitionBox.SelectedIndex = 0;
            TransitionBox.SelectionChanged += (_, _) =>
            {
                var duration = TimeSpan.FromMilliseconds(300);
                Slides.PageTransition = TransitionBox.SelectedIndex switch
                {
                    1 => new PageSlide(duration),
                    2 => new CrossFade(duration),
                    _ => null,
                };
            };
            SwipeCheck.IsCheckedChanged += (_, _) => Slides.IsSwipeEnabled = SwipeCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ItemsControlPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ItemsControlPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ItemsControl：最基础的集合展示"
                               DocPath="controls/data-display/collections/itemscontrol" />

            <TextBlock Classes="caption" Text="ItemsSource、ItemTemplate 与 ItemsPanel" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="AddButton" Content="添加一项" />
                        <Button Name="RemoveButton" Content="删除最后一项" />
                        <TextBlock VerticalAlignment="Center" Text="ItemsPanel" />
                        <ComboBox Name="PanelBox" Width="130" />
                        <TextBlock Name="CountText" VerticalAlignment="Center" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" Width="320" Height="130" HorizontalAlignment="Left" Padding="6">
                        <ItemsControl Name="Items">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Border Margin="2" Padding="8,2" CornerRadius="4" Background="#334682B4">
                                        <TextBlock Text="{Binding}" />
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ItemsControl 没有选择、没有键盘导航，也默认不虚拟化（ItemsPanel 默认是 StackPanel），所以适合项数不多、只展示不点选的场景。需要选择用 ListBox。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.DataBindingDemo →「集合」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ItemsControlPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ItemsControlPage : UserControl
    {
        private readonly ObservableCollection<string> _source = ["Apple", "Bread", "Cherry"];
        private int _next = 1;

        public ItemsControlPage()
        {
            InitializeComponent();

            Items.ItemsSource = _source;
            Show();

            AddButton.Click += (_, _) => { _source.Add($"新项 {_next++}"); Show(); };
            RemoveButton.Click += (_, _) =>
            {
                if (_source.Count > 0)
                {
                    _source.RemoveAt(_source.Count - 1);
                    Show();
                }
            };

            PanelBox.ItemsSource = new[] { "StackPanel", "WrapPanel" };
            PanelBox.SelectedIndex = 0;
            PanelBox.SelectionChanged += (_, _) =>
                Items.ItemsPanel = PanelBox.SelectedIndex == 1
                    ? new FuncTemplate<Panel?>(() => new WrapPanel())
                    : new FuncTemplate<Panel?>(() => new StackPanel());
        }

        private void Show() => CountText.Text = $"ItemCount = {Items.ItemCount}";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ListBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ListBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ListBox：可选择的列表"
                               DocPath="controls/data-display/collections/listbox" />

            <TextBlock Classes="caption" Text="SelectionMode、SelectedItems 与 ScrollIntoView" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock VerticalAlignment="Center" Text="SelectionMode" />
                        <ComboBox Name="ModeBox" Width="140" />
                        <Button Name="SelectAllButton" Content="SelectAll()" />
                        <Button Name="ClearButton" Content="UnselectAll()" />
                        <Button Name="JumpButton" Content="ScrollIntoView 末项" />
                    </StackPanel>
                    <TextBlock Name="SelectionText" Text="已选 0 项" />
                    <!--  A ListBox scrolls by itself; inside this page it needs a fixed Height.  -->
                    <ListBox Name="List" Width="240" Height="140" HorizontalAlignment="Left" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Single 只留 1 项；Multiple 点哪项选哪项；Toggle 再点取消；AlwaysSelected 至少保留 1 项。实测：Multiple 下 SelectAll() 选中全部，切回 Single 后只剩 1 项。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.DataBindingDemo →「集合视图」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ListBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ListBoxPage : UserControl
    {
        public ListBoxPage()
        {
            InitializeComponent();

            List.ItemsSource = Enumerable.Range(1, 20).Select(i => $"第 {i} 项").ToList();
            List.SelectionChanged += (_, _) => SelectionText.Text = $"已选 {List.SelectedItems?.Count ?? 0} 项";

            ModeBox.ItemsSource = Enum.GetValues<SelectionMode>();
            ModeBox.SelectedItem = List.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SelectionMode mode)
                {
                    List.SelectionMode = mode;
                }
            };

            SelectAllButton.Click += (_, _) => List.SelectAll();
            ClearButton.Click += (_, _) => List.UnselectAll();
            JumpButton.Click += (_, _) => List.ScrollIntoView(19);
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
git commit -m "feat: demonstrate Carousel, ItemsControl and ListBox

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: Structured data（DataGrid、TableView、TreeView）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/DataDisplay/DataGridPage.axaml`、`.axaml.cs`
- Create: `.../TableViewPage.axaml`、`.axaml.cs`
- Create: `.../TreeViewPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `DemoProduct.Sample()`、`DemoTreeNode.Sample()`
- Produces（探针依赖的元素名）：
  - `DataGridPage`：`Grid`（`DataGrid`）、`LinesBox`、`SelectionBox`（`ComboBox`）、`ReadOnlyCheck`、`ReorderCheck`、`SelectedText`
  - `TableViewPage`：`Table`（`TableView`）、`ModeBox`（`ComboBox`）、`ResizeCheck`、`SelectedText`
  - `TreeViewPage`：`Tree`（`TreeView`）、`ExpandAllButton`、`CollapseAllButton`、`ModeBox`（`ComboBox`）、`SelectedText`

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/DataGridPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DataGridPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DataGrid：可排序、可编辑的表格"
                               DocPath="controls/data-display/structured-data/datagrid" />

            <TextBlock Classes="caption" Text="三种列、网格线、选择模式与编辑" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="GridLines" />
                        <ComboBox Name="LinesBox" Width="130" />
                        <TextBlock VerticalAlignment="Center" Text="SelectionMode" />
                        <ComboBox Name="SelectionBox" Width="110" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="ReadOnlyCheck" Content="IsReadOnly" />
                        <CheckBox Name="ReorderCheck" Content="CanUserReorderColumns" />
                    </StackPanel>
                    <!--  A DataGrid scrolls by itself; inside this page it needs a fixed Height.  -->
                    <DataGrid Name="Grid" Width="520" Height="200" HorizontalAlignment="Left"
                              AutoGenerateColumns="False" CanUserSortColumns="True" CanUserResizeColumns="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn x:DataType="vm:DemoProduct" Header="名称" Binding="{Binding Name}" Width="2*" />
                            <DataGridTextColumn x:DataType="vm:DemoProduct" Header="分类" Binding="{Binding Category}" Width="*" />
                            <DataGridTextColumn x:DataType="vm:DemoProduct" Header="价格" Binding="{Binding Price}" Width="80" />
                            <DataGridCheckBoxColumn x:DataType="vm:DemoProduct" Header="有货" Binding="{Binding InStock}" />
                        </DataGrid.Columns>
                    </DataGrid>
                    <TextBlock Name="SelectedText" Text="选中：（无）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="点列头排序；双击单元格编辑，Enter 提交、Esc 取消；拖列头边缘调宽。DataGrid 在独立包 Avalonia.Controls.DataGrid 里，必须配合 App.axaml 的 StyleInclude，否则整块空白。" />
            <TextBlock Classes="hint" Text="分组、行详情、冻结列、按模板编辑见紧随其后的「实战：DataGrid」。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/DataGridPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            InitializeComponent();

            Grid.ItemsSource = DemoProduct.Sample();
            Grid.SelectionChanged += (_, _) =>
                SelectedText.Text = Grid.SelectedItem is DemoProduct p ? $"选中：{p.Name}（{p.Category}，{p.Price}）" : "选中：（无）";

            LinesBox.ItemsSource = Enum.GetValues<DataGridGridLinesVisibility>();
            LinesBox.SelectedItem = Grid.GridLinesVisibility;
            LinesBox.SelectionChanged += (_, _) =>
            {
                if (LinesBox.SelectedItem is DataGridGridLinesVisibility lines)
                {
                    Grid.GridLinesVisibility = lines;
                }
            };

            SelectionBox.ItemsSource = Enum.GetValues<DataGridSelectionMode>();
            SelectionBox.SelectedItem = Grid.SelectionMode;
            SelectionBox.SelectionChanged += (_, _) =>
            {
                if (SelectionBox.SelectedItem is DataGridSelectionMode mode)
                {
                    Grid.SelectionMode = mode;
                }
            };

            ReadOnlyCheck.IsCheckedChanged += (_, _) => Grid.IsReadOnly = ReadOnlyCheck.IsChecked == true;
            ReorderCheck.IsCheckedChanged += (_, _) => Grid.CanUserReorderColumns = ReorderCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TableViewPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TableViewPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TableView：主包内的轻量表格"
                               DocPath="controls/data-display/structured-data/tableview" />

            <TextBlock Classes="caption" Text="Columns、SelectionMode 与 CanUserResizeColumns" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="SelectionMode" />
                        <ComboBox Name="ModeBox" Width="140" />
                        <CheckBox Name="ResizeCheck" Content="CanUserResizeColumns" />
                    </StackPanel>
                    <!--  TableView derives from ListBox, so it also scrolls by itself and needs a fixed Height here.  -->
                    <TableView Name="Table" Width="420" Height="170" HorizontalAlignment="Left">
                        <TableView.Columns>
                            <TableViewColumn x:DataType="vm:DemoProduct" Header="名称" Width="2*" Binding="{Binding Name}" />
                            <TableViewColumn x:DataType="vm:DemoProduct" Header="分类" Width="*" Binding="{Binding Category}" />
                            <TableViewColumn x:DataType="vm:DemoProduct" Header="价格" Width="80" Binding="{Binding Price}" />
                        </TableView.Columns>
                    </TableView>
                    <TextBlock Name="SelectedText" Text="选中：（无）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TableView 派生自 ListBox，在主包里，不需要 StyleInclude；列只有 TableViewColumn 一种（没有 TableViewTextColumn），单元格内容靠 Binding 或 CellTemplate。它没有排序、编辑和分组，需要这些时用 DataGrid。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TableViewPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TableViewPage : UserControl
    {
        public TableViewPage()
        {
            InitializeComponent();

            Table.ItemsSource = DemoProduct.Sample();
            Table.SelectionChanged += (_, _) =>
                SelectedText.Text = Table.SelectedItem is DemoProduct p ? $"选中：{p.Name}（{p.Category}，{p.Price}）" : "选中：（无）";

            ModeBox.ItemsSource = Enum.GetValues<SelectionMode>();
            ModeBox.SelectedItem = Table.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SelectionMode mode)
                {
                    Table.SelectionMode = mode;
                }
            };

            ResizeCheck.IsChecked = Table.CanUserResizeColumns;
            ResizeCheck.IsCheckedChanged += (_, _) => Table.CanUserResizeColumns = ResizeCheck.IsChecked == true;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TreeViewPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TreeViewPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TreeView：层级数据"
                               DocPath="controls/data-display/structured-data/treeview" />

            <TextBlock Classes="caption" Text="TreeDataTemplate、ExpandSubTree / CollapseSubTree 与选择" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="ExpandAllButton" Content="展开全部" />
                        <Button Name="CollapseAllButton" Content="折叠全部" />
                        <TextBlock VerticalAlignment="Center" Text="SelectionMode" />
                        <ComboBox Name="ModeBox" Width="140" />
                    </StackPanel>
                    <!--  A TreeView scrolls by itself; inside this page it needs a fixed Height.  -->
                    <TreeView Name="Tree" Width="260" Height="170" HorizontalAlignment="Left">
                        <TreeView.ItemTemplate>
                            <TreeDataTemplate x:DataType="vm:DemoTreeNode" ItemsSource="{Binding Children}">
                                <TextBlock Text="{Binding Label}" />
                            </TreeDataTemplate>
                        </TreeView.ItemTemplate>
                    </TreeView>
                    <TextBlock Name="SelectedText" Text="选中：（无）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TreeDataTemplate 的 ItemsSource 指向「子节点」属性，TreeView 就会一层层向下生成。展开 / 折叠全部要对每个根调用 ExpandSubTree / CollapseSubTree，TreeView 本身没有「全部展开」属性。" />
            <TextBlock Classes="hint" Text="按 DataType 选模板、懒加载与筛选见紧随其后的「实战：TreeView」。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.DataTemplatesDemo →「面板与树」；Avalonia.FundamentalsDemo → 树页" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TreeViewPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TreeViewPage : UserControl
    {
        public TreeViewPage()
        {
            InitializeComponent();

            Tree.ItemsSource = DemoTreeNode.Sample();
            Tree.SelectionChanged += (_, _) =>
                SelectedText.Text = Tree.SelectedItem is DemoTreeNode node ? $"选中：{node.Label}" : "选中：（无）";

            // The roots are TreeViewItem containers only after layout, so expand through GetRealizedTreeContainers.
            ExpandAllButton.Click += (_, _) =>
            {
                foreach (var item in Tree.GetRealizedTreeContainers().OfType<TreeViewItem>())
                {
                    Tree.ExpandSubTree(item);
                }
            };
            CollapseAllButton.Click += (_, _) =>
            {
                foreach (var item in Tree.GetRealizedTreeContainers().OfType<TreeViewItem>())
                {
                    Tree.CollapseSubTree(item);
                }
            };

            ModeBox.ItemsSource = Enum.GetValues<SelectionMode>();
            ModeBox.SelectedItem = Tree.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SelectionMode mode)
                {
                    Tree.SelectionMode = mode;
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
git commit -m "feat: demonstrate DataGrid, TableView and TreeView

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: Text display（Label、SelectableTextBlock、TextBlock、TextTrimming）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/DataDisplay/LabelPage.axaml`、`.axaml.cs`
- Create: `.../SelectableTextBlockPage.axaml`、`.axaml.cs`
- Create: `.../TextBlockPage.axaml`、`.axaml.cs`
- Create: `.../TextTrimmingPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`
- Produces（探针依赖的元素名）：
  - `LabelPage`：`NameLabel`（`Label`）、`NameBox`（`TextBox`）、`FocusText`
  - `SelectableTextBlockPage`：`Selectable`、`SelectAllButton`、`FirstWordButton`、`ClearButton`、`SelectionText`
  - `TextBlockPage`：`Sample`（`TextBlock`）、`SizeSlider`、`WrapBox`、`AlignBox`（`ComboBox`）、`MaxLinesSlider`
  - `TextTrimmingPage`：`Trimmed`（`TextBlock`）、`WidthSlider`、`TrimBox`（`ComboBox`）、`LengthText`

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/LabelPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.LabelPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Label：带访问键的标签"
                               DocPath="controls/data-display/text-display/label" />

            <TextBlock Classes="caption" Text="Target 与访问键：按 Alt+N 把焦点交给输入框" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Label Name="NameLabel" Content="_Name" Target="{Binding #NameBox}" VerticalAlignment="Center" />
                        <TextBox Name="NameBox" Width="200" PlaceholderText="点击这里，或按 Alt+N" />
                    </StackPanel>
                    <TextBlock Name="FocusText" Text="输入框：未聚焦" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="Content 里 _ 后面的字母就是访问键。Label 自己不能聚焦，按下访问键时把焦点转给 Target。它是 ContentControl，Content 也可以是任意控件；只需要显示文字、没有访问键时用 TextBlock。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/LabelPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class LabelPage : UserControl
    {
        public LabelPage()
        {
            InitializeComponent();

            NameBox.GotFocus += (_, _) => FocusText.Text = "输入框：已聚焦";
            NameBox.LostFocus += (_, _) => FocusText.Text = "输入框：未聚焦";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/SelectableTextBlockPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SelectableTextBlockPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="SelectableTextBlock：可选中、可复制的文字"
                               DocPath="controls/data-display/text-display/selectabletextblock" />

            <TextBlock Classes="caption" Text="SelectionStart / SelectionEnd 与 SelectedText" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <SelectableTextBlock Name="Selectable" Width="320" HorizontalAlignment="Left" TextWrapping="Wrap"
                                         Text="Hello Avalonia：用鼠标拖选这段文字，按 Ctrl+C 复制。" />
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="SelectAllButton" Content="SelectAll()" />
                        <Button Name="FirstWordButton" Content="选中前 5 个字符" />
                        <Button Name="ClearButton" Content="清除选区" />
                    </StackPanel>
                    <TextBlock Name="SelectionText" Text="SelectedText：（空）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="普通 TextBlock 不能选中。SelectableTextBlock 继承 TextBlock，属性完全一样，只多了选区；CanCopy 为 True 时才有可复制的内容，右键菜单会出现「复制」。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/SelectableTextBlockPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SelectableTextBlockPage : UserControl
    {
        public SelectableTextBlockPage()
        {
            InitializeComponent();

            // The selection is not a plain property change notification, so the readout is refreshed after each button.
            SelectAllButton.Click += (_, _) => { Selectable.SelectAll(); Show(); };
            FirstWordButton.Click += (_, _) =>
            {
                Selectable.SelectionStart = 0;
                Selectable.SelectionEnd = 5;
                Show();
            };
            ClearButton.Click += (_, _) =>
            {
                Selectable.SelectionStart = 0;
                Selectable.SelectionEnd = 0;
                Show();
            };
            Selectable.PointerReleased += (_, _) => Show();
        }

        private void Show() =>
            SelectionText.Text = string.IsNullOrEmpty(Selectable.SelectedText) ? "SelectedText：（空）" : $"SelectedText：{Selectable.SelectedText}";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TextBlockPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TextBlockPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TextBlock：显示文字"
                               DocPath="controls/data-display/text-display/textblock" />

            <TextBlock Classes="caption" Text="1. 字号、换行、对齐与 MaxLines" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="90" VerticalAlignment="Center" Text="FontSize" />
                        <Slider Name="SizeSlider" Width="160" Minimum="10" Maximum="32" Value="14" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="90" VerticalAlignment="Center" Text="MaxLines" />
                        <Slider Name="MaxLinesSlider" Width="160" Minimum="0" Maximum="5" Value="0" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="TextWrapping" />
                        <ComboBox Name="WrapBox" Width="130" />
                        <TextBlock VerticalAlignment="Center" Text="TextAlignment" />
                        <ComboBox Name="AlignBox" Width="110" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" Width="300" HorizontalAlignment="Left" Padding="6">
                        <TextBlock Name="Sample" Text="The quick brown fox jumps over the lazy dog. 敏捷的棕色狐狸跳过了那只懒狗。" />
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TextWrapping 默认是 NoWrap，宽度不够时直接截断；MaxLines=0 表示不限行数。TextBlock 只显示、不能选中，要选中用 SelectableTextBlock。" />

            <TextBlock Classes="caption" Text="2. Inlines：同一个 TextBlock 里混排样式" />
            <Border Classes="stage" Padding="12">
                <TextBlock TextWrapping="Wrap" Width="320">
                    <Run Text="普通文字，" />
                    <Run FontWeight="Bold" Text="粗体，" />
                    <Run FontStyle="Italic" Text="斜体，" />
                    <Run Foreground="OrangeRed" Text="橙红色，" />
                    <Run TextDecorations="Underline" Text="下划线。" />
                    <LineBreak />
                    <Run FontSize="11" Text="LineBreak 之后的第二行小字。" />
                </TextBlock>
            </Border>
            <TextBlock Classes="hint" Text="Run 和 LineBreak 写在 TextBlock 里就是 Inlines；绑定文字用 Text 属性，混排样式才用 Inlines，二者不要同时写。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TextBlockPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextBlockPage : UserControl
    {
        public TextBlockPage()
        {
            InitializeComponent();

            SizeSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Sample.FontSize = SizeSlider.Value;
                }
            };
            MaxLinesSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Sample.MaxLines = (int)MaxLinesSlider.Value;
                }
            };

            WrapBox.ItemsSource = Enum.GetValues<TextWrapping>();
            WrapBox.SelectedItem = Sample.TextWrapping;
            WrapBox.SelectionChanged += (_, _) =>
            {
                if (WrapBox.SelectedItem is TextWrapping wrapping)
                {
                    Sample.TextWrapping = wrapping;
                }
            };

            AlignBox.ItemsSource = Enum.GetValues<TextAlignment>();
            AlignBox.SelectedItem = Sample.TextAlignment;
            AlignBox.SelectionChanged += (_, _) =>
            {
                if (AlignBox.SelectedItem is TextAlignment alignment)
                {
                    Sample.TextAlignment = alignment;
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TextTrimmingPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TextTrimmingPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TextTrimming：文字放不下时怎么截"
                               DocPath="controls/data-display/text-display/texttrimming" />

            <TextBlock Classes="caption" Text="拖动宽度，换不同的截断方式" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="60" VerticalAlignment="Center" Text="宽度" />
                        <Slider Name="WidthSlider" Width="200" Minimum="40" Maximum="360" Value="120" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="60" VerticalAlignment="Center" Text="TextTrimming" />
                        <ComboBox Name="TrimBox" Width="200" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" HorizontalAlignment="Left" Padding="4">
                        <TextBlock Name="Trimmed" Width="120" TextWrapping="NoWrap"
                                   Text="C:\Users\Administrator\Documents\Reports\2026\summary-final.docx" />
                    </Border>
                    <TextBlock Name="LengthText" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TextTrimming 只改画出来的样子，Text 本身一个字符都没少，复制、绑定读到的仍是完整内容。必须配合 NoWrap（默认值）才会生效，换行状态下文字是折行而不是截断。" />
            <TextBlock Classes="hint" Text="PathSegmentEllipsis 专为路径设计：优先保留文件名，从中间的目录段开始省略。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TextTrimmingPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextTrimmingPage : UserControl
    {
        // TextTrimming is a class with static instances rather than an enum, so the box lists names and maps them back.
        private static readonly (string Name, TextTrimming Value)[] Options =
        [
            ("None", TextTrimming.None),
            ("CharacterEllipsis", TextTrimming.CharacterEllipsis),
            ("WordEllipsis", TextTrimming.WordEllipsis),
            ("PrefixCharacterEllipsis", TextTrimming.PrefixCharacterEllipsis),
            ("LeadingCharacterEllipsis", TextTrimming.LeadingCharacterEllipsis),
            ("PathSegmentEllipsis", TextTrimming.PathSegmentEllipsis),
        ];

        public TextTrimmingPage()
        {
            InitializeComponent();

            var names = new List<string>();
            foreach (var option in Options)
            {
                names.Add(option.Name);
            }

            TrimBox.ItemsSource = names;
            TrimBox.SelectedIndex = 1;
            Trimmed.TextTrimming = Options[1].Value;
            TrimBox.SelectionChanged += (_, _) =>
            {
                if (TrimBox.SelectedIndex >= 0)
                {
                    Trimmed.TextTrimming = Options[TrimBox.SelectedIndex].Value;
                }
            };

            WidthSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Trimmed.Width = WidthSlider.Value;
                }
            };
            LengthText.Text = $"Text 长度 = {Trimmed.Text!.Length}（无论怎么截都不变）";
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
git commit -m "feat: demonstrate Label, SelectableTextBlock, TextBlock and TextTrimming

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: ContentControl 与 TransitioningContentControl

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ContentControlPage.axaml`、`.axaml.cs`
- Create: `.../TransitioningContentControlPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `DemoProduct`
- Produces（探针依赖的元素名）：
  - `ContentControlPage`：`Host`（`ContentControl`）、`TextButton`、`ControlButton`、`ModelButton`（`Button`）、`TypeText`
  - `TransitioningContentControlPage`：`Host`（`TransitioningContentControl`）、`NextButton`、`ReverseCheck`（`CheckBox`）、`TransitionBox`（`ComboBox`）、`DoneText`

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ContentControlPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ContentControlPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ContentControl：只放一个内容"
                               DocPath="controls/data-display/contentcontrol" />

            <TextBlock Classes="caption" Text="Content 是字符串、控件，还是数据对象，决定了它怎么画" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="TextButton" Content="Content = 字符串" />
                        <Button Name="ControlButton" Content="Content = 控件" />
                        <Button Name="ModelButton" Content="Content = 数据对象" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" Width="320" Height="70" HorizontalAlignment="Left">
                        <ContentControl Name="Host" HorizontalAlignment="Center" VerticalAlignment="Center">
                            <ContentControl.DataTemplates>
                                <DataTemplate x:DataType="vm:DemoProduct">
                                    <StackPanel Orientation="Horizontal" Spacing="8">
                                        <TextBlock FontWeight="SemiBold" Text="{Binding Name}" />
                                        <TextBlock Text="{Binding Price, StringFormat={}{0:C}}" />
                                    </StackPanel>
                                </DataTemplate>
                            </ContentControl.DataTemplates>
                        </ContentControl>
                    </Border>
                    <TextBlock Name="TypeText" Text="Content 的类型：（未设置）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="字符串直接显示；控件原样放进去；其它对象找数据模板（ContentTemplate，或树上任一层的 DataTemplates）渲染，找不到就退回调用 ToString()。Button、Border 等的 Content 都是这套规则。" />
            <TextBlock Classes="hint" Text="更深入的演示：Avalonia.DataTemplatesDemo →「控件内容」" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ContentControlPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ContentControlPage : UserControl
    {
        public ContentControlPage()
        {
            InitializeComponent();

            TextButton.Click += (_, _) => Apply("你好，ContentControl");
            ControlButton.Click += (_, _) => Apply(new Border
            {
                Padding = new Thickness(10, 4),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.SteelBlue,
                Child = new TextBlock { Text = "我是一个 Border", Foreground = Brushes.White },
            });
            ModelButton.Click += (_, _) => Apply(new DemoProduct { Name = "Apple", Price = 1.2m });
        }

        private void Apply(object content)
        {
            Host.Content = content;
            TypeText.Text = $"Content 的类型：{content.GetType().Name}";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TransitioningContentControlPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TransitioningContentControlPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TransitioningContentControl：换内容时带过渡"
                               DocPath="controls/data-display/transitioningcontentcontrol" />

            <TextBlock Classes="caption" Text="PageTransition 与 IsTransitionReversed" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Button Name="NextButton" Content="换下一页" />
                        <TextBlock VerticalAlignment="Center" Text="PageTransition" />
                        <ComboBox Name="TransitionBox" Width="150" />
                        <CheckBox Name="ReverseCheck" Content="IsTransitionReversed" />
                    </StackPanel>
                    <TextBlock Name="DoneText" Text="已完成 0 次过渡" />
                    <Border BorderBrush="Gray" BorderThickness="1" Width="320" Height="100" HorizontalAlignment="Left" ClipToBounds="True">
                        <TransitioningContentControl Name="Host" Content="第 1 页">
                            <TransitioningContentControl.ContentTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Border Background="#334682B4">
                                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" FontSize="20" Text="{Binding}" />
                                    </Border>
                                </DataTemplate>
                            </TransitioningContentControl.ContentTemplate>
                        </TransitioningContentControl>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="它只是在改 Content 的瞬间播放 PageTransition，其余和 ContentControl 一样。外层 Border 要 ClipToBounds，否则滑动过渡会画到框外。每次换内容完成后触发一次 TransitionCompleted。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TransitioningContentControlPage.axaml.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TransitioningContentControlPage : UserControl
    {
        private static readonly string[] TransitionNames = ["PageSlide", "CrossFade", "Rotate3D"];
        private int _page = 1;
        private int _done;
        private bool _initialSeen;

        public TransitioningContentControlPage()
        {
            InitializeComponent();

            Host.TransitionCompleted += (_, _) =>
            {
                // The control also reports once for the content it was created with; skip that one so only real changes count.
                if (!_initialSeen)
                {
                    _initialSeen = true;
                    return;
                }

                DoneText.Text = $"已完成 {++_done} 次过渡";
            };
            NextButton.Click += (_, _) => Host.Content = $"第 {++_page} 页";

            TransitionBox.ItemsSource = TransitionNames;
            TransitionBox.SelectedIndex = 0;
            Apply();
            TransitionBox.SelectionChanged += (_, _) => Apply();
            ReverseCheck.IsCheckedChanged += (_, _) => Host.IsTransitionReversed = ReverseCheck.IsChecked == true;
        }

        private void Apply()
        {
            var duration = TimeSpan.FromMilliseconds(400);
            Host.PageTransition = TransitionBox.SelectedIndex switch
            {
                1 => new CrossFade(duration),
                2 => new Rotate3DTransition(duration),
                _ => new PageSlide(duration),
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
git commit -m "feat: demonstrate ContentControl and TransitioningContentControl

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 6: 四篇 How-to 实战页（ItemsControl、ListBox、DataGrid、TreeView）

每篇取官方 How-to 里能在一个窗口内演示的场景，紧跟在控件页之后；`PageKind.HowTo`，树里显示为「实战：…」。本任务分 6a（ItemsControl、ListBox）与 6b（DataGrid、TreeView）两段。

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/ListBoxHowToViewModel.cs`（含 `TodoItem`）
- Create: `Avalonia.ControlsDemo/ViewModels/DataGridHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/TreeViewHowToViewModel.cs`（含 `FolderNode`、`FileNode`、`LazyNode`）
- Create: `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ItemsControlHowToPage.axaml`、`.axaml.cs`
- Create: `.../ListBoxHowToPage.axaml`、`.axaml.cs`
- Create: `.../DataGridHowToPage.axaml`、`.axaml.cs`
- Create: `.../TreeViewHowToPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `DemoProduct.Sample()`
- Produces（探针依赖）：
  - `TodoItem : ObservableObject`：`string Title`、`bool IsDone`；`ListBoxHowToViewModel : ObservableObject`：`IReadOnlyList<TodoItem> Todos`（4 项）、`int DoneCount`
  - `ItemsControlHowToPage` 元素名：`Tags`（`ItemsControl`）、`AddTagButton`、`ClearTagsButton`、`EmptyText`（`TextBlock`）、`Striped`（`ItemsControl`）、`BigScroller`（`ScrollViewer`）、`Big`（`ItemsControl`）、`RealizedText`
  - `ListBoxHowToPage` 元素名：`Todos`（`ListBox`）、`DoneText`、`Strip`（`ListBox`）、`Accent`（`ListBox`）

#### `Avalonia.ControlsDemo/ViewModels/ListBoxHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>One row of the checkbox list; IsDone is the state, so no ListBox selection is involved.</summary>
    public sealed class TodoItem : ObservableObject
    {
        private bool _isDone;

        public TodoItem(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public bool IsDone
        {
            get => _isDone;
            set => SetProperty(ref _isDone, value);
        }
    }

    public sealed class ListBoxHowToViewModel : ObservableObject
    {
        public ListBoxHowToViewModel()
        {
            Todos = [new TodoItem("写计划"), new TodoItem("跑探针"), new TodoItem("目视检查"), new TodoItem("提交")];
            foreach (var todo in Todos)
            {
                todo.PropertyChanged += (_, _) => OnPropertyChanged(nameof(DoneCount));
            }
        }

        public IReadOnlyList<TodoItem> Todos { get; }

        public int DoneCount => Todos.Count(t => t.IsDone);
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ItemsControlHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ItemsControlHowToPage">
    <UserControl.Styles>
        <!--  The stripe comes from a style on a class the code sets per container, never a local Background (rule 1).  -->
        <Style Selector="ItemsControl.striped ContentPresenter.odd">
            <Setter Property="Background" Value="#22808080" />
        </Style>
    </UserControl.Styles>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ItemsControl 实战"
                               DocPath="docs/how-to/itemscontrol-how-to" />

            <TextBlock Classes="caption" Text="1. 换 ItemsPanel 排成标签云，并在列表为空时显示提示" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="AddTagButton" Content="添加标签" />
                        <Button Name="ClearTagsButton" Content="清空" />
                    </StackPanel>
                    <Border BorderBrush="Gray" BorderThickness="1" Width="320" MinHeight="60" HorizontalAlignment="Left" Padding="6">
                        <Panel>
                            <ItemsControl Name="Tags">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <WrapPanel />
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate x:DataType="x:String">
                                        <Border Margin="2" Padding="8,2" CornerRadius="10" Background="#334682B4">
                                            <TextBlock Text="{Binding}" />
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                            <!--  The parameter must be a real int: ConverterParameter=0 would be the string "0", and {x:Int32 0} is not a markup extension.  -->
                            <TextBlock Name="EmptyText" Foreground="Gray" HorizontalAlignment="Center" VerticalAlignment="Center"
                                       Text="还没有标签">
                                <TextBlock.IsVisible>
                                    <Binding Path="#Tags.ItemCount" Converter="{x:Static ObjectConverters.Equal}">
                                        <Binding.ConverterParameter>
                                            <x:Int32>0</x:Int32>
                                        </Binding.ConverterParameter>
                                    </Binding>
                                </TextBlock.IsVisible>
                            </TextBlock>
                        </Panel>
                    </Border>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ItemsPanel 决定子项怎么排：默认 StackPanel，换成 WrapPanel 就成了标签云。空状态不用写代码，让提示的 IsVisible 绑定到 ItemCount 是否等于 0 即可。" />

            <TextBlock Classes="caption" Text="2. 用 ContainerPrepared 给每个容器打标记，做斑马纹" />
            <Border Classes="stage" Padding="12">
                <ItemsControl Name="Striped" Classes="striped" Width="240" HorizontalAlignment="Left">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <TextBlock Margin="8,3" Text="{Binding}" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </Border>
            <TextBlock Classes="hint" Text="每个条目生成容器时触发 ContainerPrepared，事件参数带容器和序号；在这里给奇数序号的容器加上 odd 样式类，页面级样式负责涂色。事件必须先订阅、再设 ItemsSource。" />

            <TextBlock Classes="caption" Text="3. 大数据量：VirtualizingStackPanel 只实现看得见的部分" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TextBlock Name="RealizedText" />
                    <ScrollViewer Name="BigScroller" Width="240" Height="120" HorizontalAlignment="Left"
                                  BorderBrush="Gray" BorderThickness="1">
                        <ItemsControl Name="Big">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <VirtualizingStackPanel />
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                        </ItemsControl>
                    </ScrollViewer>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ItemsControl 默认不虚拟化；项数很多时把 ItemsPanel 换成 VirtualizingStackPanel，并把它放进 ScrollViewer，容器数就只和视口有关，和总项数无关。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ItemsControlHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ItemsControlHowToPage : UserControl
    {
        private const int BigCount = 1000;
        private readonly ObservableCollection<string> _tags = ["Avalonia", "XAML"];
        private int _nextTag = 1;

        public ItemsControlHowToPage()
        {
            InitializeComponent();

            Tags.ItemsSource = _tags;
            AddTagButton.Click += (_, _) => _tags.Add($"标签 {_nextTag++}");
            ClearTagsButton.Click += (_, _) => _tags.Clear();

            // Subscribe first, then assign ItemsSource, so the first containers are not missed.
            Striped.ContainerPrepared += (_, e) =>
            {
                if (e.Index % 2 == 1)
                {
                    e.Container.Classes.Add("odd");
                }
                else
                {
                    e.Container.Classes.Remove("odd");
                }
            };
            Striped.ItemsSource = Enumerable.Range(1, 6).Select(i => $"第 {i} 行").ToList();

            Big.ItemsSource = Enumerable.Range(1, BigCount).Select(i => $"第 {i} 项").ToList();
            Big.LayoutUpdated += (_, _) =>
                RealizedText.Text = $"共 {Big.ItemCount} 项，已实现 {Big.GetRealizedContainers().Count()} 个容器";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ListBoxHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ListBoxHowToPage"
             x:DataType="vm:ListBoxHowToViewModel">
    <UserControl.Styles>
        <!--  Colour the selected row through the template part; setting Background on ListBoxItem is overridden by the theme.  -->
        <Style Selector="ListBox.accent ListBoxItem:selected /template/ ContentPresenter">
            <Setter Property="Background" Value="Orange" />
        </Style>
    </UserControl.Styles>
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ListBox 实战"
                               DocPath="docs/how-to/listbox-how-to" />

            <TextBlock Classes="caption" Text="1. 用 CheckBox 做多选：状态放在数据上，不靠 ListBox 的选择" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <ListBox Name="Todos" ItemsSource="{Binding Todos}" Width="240" Height="130" HorizontalAlignment="Left">
                        <ListBox.ItemTemplate>
                            <DataTemplate x:DataType="vm:TodoItem">
                                <CheckBox IsChecked="{Binding IsDone}" Content="{Binding Title}" />
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>
                    <TextBlock Name="DoneText" Text="{Binding DoneCount, StringFormat='已完成 {0} 项'}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="每行的 CheckBox 绑到数据的 IsDone，勾选就是改数据，所以不用开 SelectionMode=Multiple，也不用处理 SelectedItems。" />

            <TextBlock Classes="caption" Text="2. 横向排列：ItemsPanel 换成水平 StackPanel" />
            <Border Classes="stage" Padding="12">
                <ListBox Name="Strip" Width="360" Height="80" HorizontalAlignment="Left"
                         ScrollViewer.HorizontalScrollBarVisibility="Auto">
                    <ListBox.ItemsPanel>
                        <ItemsPanelTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="4" />
                        </ItemsPanelTemplate>
                    </ListBox.ItemsPanel>
                    <ListBoxItem Width="80" Content="卡片 1" />
                    <ListBoxItem Width="80" Content="卡片 2" />
                    <ListBoxItem Width="80" Content="卡片 3" />
                    <ListBoxItem Width="80" Content="卡片 4" />
                    <ListBoxItem Width="80" Content="卡片 5" />
                    <ListBoxItem Width="80" Content="卡片 6" />
                </ListBox>
            </Border>
            <TextBlock Classes="hint" Text="横向时别忘了 ScrollViewer.HorizontalScrollBarVisibility，默认是 Disabled，内容会被裁掉而不是出现滚动条。" />

            <TextBlock Classes="caption" Text="3. 自定义选中外观：ListBoxItem:selected /template/ ContentPresenter" />
            <Border Classes="stage" Padding="12">
                <ListBox Name="Accent" Classes="accent" Width="240" Height="110" HorizontalAlignment="Left" SelectedIndex="1">
                    <ListBoxItem Content="苹果" />
                    <ListBoxItem Content="面包" />
                    <ListBoxItem Content="樱桃" />
                </ListBox>
            </Border>
            <TextBlock Classes="hint" Text="选中背景画在模板里的 ContentPresenter 上，直接给 ListBoxItem 设 Background 会被主题盖掉，必须用 /template/ 选到那个部件。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/ListBoxHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ListBoxHowToPage : UserControl
    {
        public ListBoxHowToPage()
        {
            InitializeComponent();
            DataContext = new ListBoxHowToViewModel();
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/DataGridHowToViewModel.cs`

```csharp
using Avalonia.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Two DataGridCollectionViews over the same rows: one grouped by Category, one sorted by Name.</summary>
    public sealed class DataGridHowToViewModel
    {
        public DataGridHowToViewModel()
        {
            var rows = DemoProduct.Sample();

            Grouped = new DataGridCollectionView(rows);
            Grouped.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(DemoProduct.Category)));

            Sorted = new DataGridCollectionView(DemoProduct.Sample());
            Sorted.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(DemoProduct.Name), ListSortDirection.Descending));

            Details = DemoProduct.Sample();
        }

        public DataGridCollectionView Grouped { get; }

        public DataGridCollectionView Sorted { get; }

        public List<DemoProduct> Details { get; }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/DataGridHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DataGridHowToPage"
             x:DataType="vm:DataGridHowToViewModel">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DataGrid 实战"
                               DocPath="docs/how-to/datagrid-how-to" />

            <TextBlock Classes="caption" Text="1. 分组：DataGridCollectionView + DataGridPathGroupDescription" />
            <Border Classes="stage" Padding="12">
                <DataGrid Name="GroupGrid" ItemsSource="{Binding Grouped}" Width="480" Height="200" HorizontalAlignment="Left"
                          AutoGenerateColumns="False" IsReadOnly="True">
                    <DataGrid.Columns>
                        <DataGridTextColumn x:DataType="vm:DemoProduct" Header="名称" Binding="{Binding Name}" Width="*" />
                        <DataGridTextColumn x:DataType="vm:DemoProduct" Header="价格" Binding="{Binding Price}" Width="80" />
                    </DataGrid.Columns>
                </DataGrid>
            </Border>
            <TextBlock Classes="hint" Text="分组不改源集合：把集合包进 DataGridCollectionView，再往 GroupDescriptions 里加一条按属性名分组的描述，DataGrid 自己生成组头（点组头可折叠）。排序同理走 SortDescriptions。" />

            <TextBlock Classes="caption" Text="2. 预设排序与 SortMemberPath" />
            <Border Classes="stage" Padding="12">
                <DataGrid Name="SortGrid" ItemsSource="{Binding Sorted}" Width="480" Height="170" HorizontalAlignment="Left"
                          AutoGenerateColumns="False" IsReadOnly="True" CanUserSortColumns="True">
                    <DataGrid.Columns>
                        <DataGridTextColumn x:DataType="vm:DemoProduct" Header="名称（预设降序）" Binding="{Binding Name}" Width="*" />
                        <!--  A template column has no Binding to sort by, so SortMemberPath names the property to use.  -->
                        <DataGridTemplateColumn Header="价格（模板列）" Width="140" SortMemberPath="Price">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate x:DataType="vm:DemoProduct">
                                    <TextBlock Margin="8,0" VerticalAlignment="Center" Foreground="SeaGreen"
                                               Text="{Binding Price, StringFormat={}{0:C}}" />
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Border>
            <TextBlock Classes="hint" Text="模板列没有 Binding，点列头时 DataGrid 不知道按什么排；SortMemberPath 就是告诉它「按 Price 这个属性排」。" />

            <TextBlock Classes="caption" Text="3. 行详情与冻结列" />
            <Border Classes="stage" Padding="12">
                <DataGrid Name="DetailGrid" ItemsSource="{Binding Details}" Width="480" Height="200" HorizontalAlignment="Left"
                          AutoGenerateColumns="False" IsReadOnly="True" FrozenColumnCount="1"
                          RowDetailsVisibilityMode="VisibleWhenSelected" HorizontalScrollBarVisibility="Auto">
                    <DataGrid.Columns>
                        <DataGridTextColumn x:DataType="vm:DemoProduct" Header="名称" Binding="{Binding Name}" Width="120" />
                        <DataGridTextColumn x:DataType="vm:DemoProduct" Header="分类" Binding="{Binding Category}" Width="160" />
                        <DataGridTextColumn x:DataType="vm:DemoProduct" Header="价格" Binding="{Binding Price}" Width="160" />
                        <DataGridCheckBoxColumn x:DataType="vm:DemoProduct" Header="有货" Binding="{Binding InStock}" Width="120" />
                    </DataGrid.Columns>
                    <DataGrid.RowDetailsTemplate>
                        <DataTemplate x:DataType="vm:DemoProduct">
                            <TextBlock Margin="12,4" Foreground="Gray" Text="{Binding Description, StringFormat='说明：{0}'}" />
                        </DataTemplate>
                    </DataGrid.RowDetailsTemplate>
                </DataGrid>
            </Border>
            <TextBlock Classes="hint" Text="选中行下方展开详情（RowDetailsVisibilityMode=VisibleWhenSelected）；FrozenColumnCount=1 把名称列钉在左边，横向滚动时不动。四列加起来比 480 宽，才有横向滚动。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/DataGridHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DataGridHowToPage : UserControl
    {
        public DataGridHowToPage()
        {
            InitializeComponent();
            DataContext = new DataGridHowToViewModel();
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/TreeViewHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>
    /// Every node in the mixed tree derives from this, so one TreeViewItem style can bind IsExpanded for folders and
    /// files alike. A style selector matches every TreeViewItem, so a binding typed to FolderNode would fail on files.
    /// </summary>
    public abstract partial class TreeNodeBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isExpanded;
    }

    public sealed class FolderNode : TreeNodeBase
    {
        public FolderNode(string name, params TreeNodeBase[] children)
        {
            Name = name;
            Children = new ObservableCollection<TreeNodeBase>(children);
        }

        public string Name { get; }

        public ObservableCollection<TreeNodeBase> Children { get; }
    }

    public sealed class FileNode : TreeNodeBase
    {
        public FileNode(string name, int sizeKb)
        {
            Name = name;
            SizeKb = sizeKb;
        }

        public string Name { get; }

        public int SizeKb { get; }
    }

    /// <summary>
    /// Loads its children the first time it is expanded. It starts with one placeholder child so the
    /// TreeView draws an expander arrow; the placeholder is replaced by the real children on first expand.
    /// </summary>
    public sealed partial class LazyNode : ObservableObject
    {
        private bool _loaded;

        [ObservableProperty]
        private bool _isExpanded;

        public LazyNode(string name)
        {
            Name = name;
            Children = [new LazyNode("加载中…", isLeaf: true)];
        }

        private LazyNode(string name, bool isLeaf)
        {
            Name = name;
            Children = [];
        }

        public string Name { get; }

        public int LoadCount { get; private set; }

        public ObservableCollection<LazyNode> Children { get; }

        partial void OnIsExpandedChanged(bool value)
        {
            if (!value || _loaded)
            {
                return;
            }

            _loaded = true;
            LoadCount++;
            Children.Clear();
            for (var i = 1; i <= 3; i++)
            {
                Children.Add(new LazyNode($"子项 {i}", isLeaf: true));
            }
        }
    }

    public sealed partial class TreeViewHowToViewModel : ObservableObject
    {
        public TreeViewHowToViewModel()
        {
            Roots =
            [
                new FolderNode("项目",
                    new FolderNode("src", new FileNode("App.axaml", 4), new FileNode("Program.cs", 2)),
                    new FileNode("README.md", 1)),
                new FolderNode("素材", new FileNode("logo.png", 38)),
            ];
            Lazy = [new LazyNode("远程目录")];
        }

        public ObservableCollection<TreeNodeBase> Roots { get; }

        public ObservableCollection<LazyNode> Lazy { get; }

        [ObservableProperty]
        private string _selectionText = "选中：（无）";

        public void ExpandAll(IEnumerable<TreeNodeBase> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is FolderNode folder)
                {
                    folder.IsExpanded = true;
                    ExpandAll(folder.Children);
                }
            }
        }

        public void CollapseAll(IEnumerable<TreeNodeBase> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is FolderNode folder)
                {
                    folder.IsExpanded = false;
                    CollapseAll(folder.Children);
                }
            }
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TreeViewHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TreeViewHowToPage"
             x:DataType="vm:TreeViewHowToViewModel">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TreeView 实战"
                               DocPath="docs/how-to/treeview-how-to" />

            <TextBlock Classes="caption" Text="1. 一棵树里放不同类型的节点：每种类型一个模板，并从模型控制展开" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="ExpandAllButton" Content="展开全部" />
                        <Button Name="CollapseAllButton" Content="折叠全部" />
                    </StackPanel>
                    <TreeView Name="Mixed" ItemsSource="{Binding Roots}" Width="280" Height="170" HorizontalAlignment="Left">
                        <TreeView.Styles>
                            <!--  Bind each item's expansion to the model, so the code can open or close the tree without touching containers.  -->
                            <Style Selector="TreeViewItem" x:DataType="vm:TreeNodeBase">
                                <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
                            </Style>
                        </TreeView.Styles>
                        <TreeView.DataTemplates>
                            <TreeDataTemplate DataType="vm:FolderNode" ItemsSource="{Binding Children}">
                                <TextBlock FontWeight="SemiBold" Text="{Binding Name, StringFormat='📁 {0}'}" />
                            </TreeDataTemplate>
                            <DataTemplate DataType="vm:FileNode">
                                <TextBlock Text="{Binding Name, StringFormat='📄 {0}'}" />
                            </DataTemplate>
                        </TreeView.DataTemplates>
                    </TreeView>
                    <TextBlock Name="MixedText" Text="选中：（无）" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="TreeView.DataTemplates 按节点的 DataType 选模板：文件夹用 TreeDataTemplate（带 ItemsSource），文件用普通 DataTemplate。把 TreeViewItem 的 IsExpanded 绑到模型，「展开全部」就变成递归改 IsExpanded，不必去找容器。样式选择器会匹配每一个 TreeViewItem（文件节点也算），所以样式里的绑定类型要写两种节点的共同基类，写成 FolderNode 会在文件节点上报强转失败。" />

            <TextBlock Classes="caption" Text="2. 懒加载：第一次展开时才生成子节点" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TreeView Name="LazyTree" ItemsSource="{Binding Lazy}" Width="280" Height="130" HorizontalAlignment="Left">
                        <TreeView.Styles>
                            <Style Selector="TreeViewItem" x:DataType="vm:LazyNode">
                                <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
                            </Style>
                        </TreeView.Styles>
                        <TreeView.DataTemplates>
                            <TreeDataTemplate DataType="vm:LazyNode" ItemsSource="{Binding Children}">
                                <TextBlock Text="{Binding Name}" />
                            </TreeDataTemplate>
                        </TreeView.DataTemplates>
                    </TreeView>
                    <TextBlock Name="LazyText" Text="远程目录已加载 0 次" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="节点一开始只带一个「加载中…」占位子项，这样 TreeView 才会画出展开箭头；IsExpanded 第一次变成 true 时，模型把占位换成真正的子节点。实际项目里把这一步换成异步读取，换完前先显示占位。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/DataDisplay/TreeViewHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TreeViewHowToPage : UserControl
    {
        public TreeViewHowToPage()
        {
            InitializeComponent();
            var model = new TreeViewHowToViewModel();
            DataContext = model;

            ExpandAllButton.Click += (_, _) => model.ExpandAll(model.Roots);
            CollapseAllButton.Click += (_, _) => model.CollapseAll(model.Roots);

            Mixed.SelectionChanged += (_, _) =>
                MixedText.Text = Mixed.SelectedItem switch
                {
                    FolderNode folder => $"选中文件夹：{folder.Name}（{folder.Children.Count} 项）",
                    FileNode file => $"选中文件：{file.Name}（{file.SizeKb} KB）",
                    _ => "选中：（无）",
                };

            // Expanding the root runs the model's loader; show how many times it actually loaded.
            LazyTree.LayoutUpdated += (_, _) => LazyText.Text = $"远程目录已加载 {model.Lazy[0].LoadCount} 次";
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
git commit -m "feat: add the ItemsControl, ListBox, DataGrid and TreeView how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 7: 登记全部页面、探针验证、收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.DataDisplay.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Data.cs`、`Probe.Data2.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）

**Interfaces:**
- Consumes: Task 1–6 的全部页面类；第 00 份的 `ControlPage<T>`、`HowToPage<T>`、`Harness`
- Produces: Data display 分类共 12 个 `Control` 页 + 4 个 `HowTo` 页，登记顺序为官方侧边栏顺序（collections → contentcontrol → structured-data → text-display → transitioningcontentcontrol），HowTo 紧跟其控件

- [ ] **Step 1: 登记**

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.DataDisplay.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddDataDisplay(List<PageEntry> list)
        {
            const string c = Categories.DataDisplay;

            // Collections, in the order of the official sidebar.
            list.Add(ControlPage<CarouselPage>("Carousel", c, "controls/data-display/collections/carousel"));
            list.Add(ControlPage<ItemsControlPage>("ItemsControl", c, "controls/data-display/collections/itemscontrol"));
            list.Add(HowToPage<ItemsControlHowToPage>("ItemsControl", c, "docs/how-to/itemscontrol-how-to"));
            list.Add(ControlPage<ListBoxPage>("ListBox", c, "controls/data-display/collections/listbox"));
            list.Add(HowToPage<ListBoxHowToPage>("ListBox", c, "docs/how-to/listbox-how-to"));

            // Stand-alone pages directly under the category.
            list.Add(ControlPage<ContentControlPage>("ContentControl", c, "controls/data-display/contentcontrol"));

            // Structured data
            list.Add(ControlPage<DataGridPage>("DataGrid", c, "controls/data-display/structured-data/datagrid"));
            list.Add(HowToPage<DataGridHowToPage>("DataGrid", c, "docs/how-to/datagrid-how-to"));
            list.Add(ControlPage<TableViewPage>("TableView", c, "controls/data-display/structured-data/tableview"));
            list.Add(ControlPage<TreeViewPage>("TreeView", c, "controls/data-display/structured-data/treeview"));
            list.Add(HowToPage<TreeViewHowToPage>("TreeView", c, "docs/how-to/treeview-how-to"));

            // Text display
            list.Add(ControlPage<LabelPage>("Label", c, "controls/data-display/text-display/label"));
            list.Add(ControlPage<SelectableTextBlockPage>("SelectableTextBlock", c, "controls/data-display/text-display/selectabletextblock"));
            list.Add(ControlPage<TextBlockPage>("TextBlock", c, "controls/data-display/text-display/textblock"));
            list.Add(ControlPage<TextTrimmingPage>("TextTrimming", c, "controls/data-display/text-display/texttrimming"));

            list.Add(ControlPage<TransitioningContentControlPage>("TransitioningContentControl", c, "controls/data-display/transitioningcontentcontrol"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

探针分三个文件，都是 `static partial class ProbeData`：`Probe.Data.cs`（目录完整性、Collections、独立页）、`Probe.Data2.cs`（文字页与 Structured data）、`Probe.Data3.cs`（四篇 How-to）。

**`C:\Temp\probe-controls\Probe.Data.cs`**

```csharp
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;

static partial class ProbeData
{
    public static void Run()
    {
        Catalog();
        Collections();
        Content();
        Text();
        Structured();
        HowTos();
    }

    static void Click(Control root, string name) => Harness.Find<Button>(root, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    static void Catalog()
    {
        var data = PageCatalog.All.Where(e => e.Category == Categories.DataDisplay).ToList();
        Harness.Check("Data: 12 Control pages", data.Count(e => e.Kind == PageKind.Control) == 12, data.Count(e => e.Kind == PageKind.Control));
        Harness.Check("Data: 4 HowTo pages", data.Count(e => e.Kind == PageKind.HowTo) == 4, data.Count(e => e.Kind == PageKind.HowTo));

        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].Kind != PageKind.HowTo) continue;
            Harness.Check($"Data: HowTo {data[i].Title} follows its control",
                i > 0 && data[i - 1].Kind == PageKind.Control && data[i - 1].Title == data[i].Title);
            Harness.Check($"Data: HowTo {data[i].Title} DocPath starts with docs/how-to/", data[i].DocPath.StartsWith("docs/how-to/"));
        }

        foreach (var entry in data)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Data/{entry.Title}({entry.Kind}): renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"Data/{entry.Title}({entry.Kind}): header matches catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            w.Close();
        }
    }

    static void Collections()
    {
        // ---- Carousel: Next stops at the last item instead of wrapping
        var cp = new CarouselPage();
        var w = Harness.Show(cp);
        var slides = Harness.Find<Carousel>(cp, "Slides");
        Harness.Check("Carousel: starts at 0 with no transition and no swipe",
            slides.SelectedIndex == 0 && slides.PageTransition is null && !slides.IsSwipeEnabled, $"{slides.SelectedIndex}/{slides.PageTransition}/{slides.IsSwipeEnabled}");
        Click(cp, "NextButton"); Harness.Pump();
        Harness.Check("Carousel: Next moves to 1 and the readout follows", slides.SelectedIndex == 1 && Harness.Find<TextBlock>(cp, "IndexText").Text == "第 2 / 3 张",
            Harness.Find<TextBlock>(cp, "IndexText").Text);
        Click(cp, "NextButton"); Click(cp, "NextButton"); Harness.Pump();
        Harness.Check("Carousel: Next does not wrap past the last item", slides.SelectedIndex == 2 && Harness.Find<TextBlock>(cp, "IndexText").Text == "第 3 / 3 张",
            $"{slides.SelectedIndex}/{Harness.Find<TextBlock>(cp, "IndexText").Text}");
        Click(cp, "PreviousButton"); Harness.Pump();
        Harness.Check("Carousel: Previous goes back to 1", slides.SelectedIndex == 1, slides.SelectedIndex);
        Harness.Find<ComboBox>(cp, "TransitionBox").SelectedIndex = 1;
        Harness.Find<CheckBox>(cp, "SwipeCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("Carousel: the boxes drive PageTransition and IsSwipeEnabled", slides.PageTransition is PageSlide && slides.IsSwipeEnabled);
        w.Close();

        // ---- ItemsControl: the source drives the count, the box swaps the panel
        var ip = new ItemsControlPage();
        w = Harness.Show(ip);
        var items = Harness.Find<ItemsControl>(ip, "Items");
        Harness.Check("ItemsControl: the default panel is a StackPanel with 3 containers",
            items.ItemCount == 3 && items.GetVisualDescendants().OfType<StackPanel>().Any() && items.GetRealizedContainers().Count() == 3,
            $"{items.ItemCount}/{items.GetRealizedContainers().Count()}");
        Click(ip, "AddButton"); Harness.Pump();
        Harness.Check("ItemsControl: adding an item raises ItemCount and the readout",
            items.ItemCount == 4 && Harness.Find<TextBlock>(ip, "CountText").Text == "ItemCount = 4", Harness.Find<TextBlock>(ip, "CountText").Text);
        Harness.Find<ComboBox>(ip, "PanelBox").SelectedIndex = 1;
        Harness.Pump();
        Harness.Check("ItemsControl: switching ItemsPanel puts a WrapPanel under it", items.GetVisualDescendants().OfType<WrapPanel>().Any());
        Click(ip, "RemoveButton"); Harness.Pump();
        Harness.Check("ItemsControl: removing brings it back to 3", items.ItemCount == 3, items.ItemCount);
        w.Close();

        // ---- ListBox: the mode decides how many items stay selected
        var lp = new ListBoxPage();
        w = Harness.Show(lp);
        var list = Harness.Find<ListBox>(lp, "List");
        Harness.Check("ListBox: starts in Single mode with nothing selected", list.SelectionMode == SelectionMode.Single && (list.SelectedItems?.Count ?? 0) == 0, list.SelectionMode);
        Harness.Find<ComboBox>(lp, "ModeBox").SelectedItem = SelectionMode.Multiple;
        Harness.Pump();
        Click(lp, "SelectAllButton"); Harness.Pump();
        Harness.Check("ListBox: Multiple + SelectAll selects all 20", list.SelectedItems!.Count == 20 && Harness.Find<TextBlock>(lp, "SelectionText").Text == "已选 20 项",
            $"{list.SelectedItems!.Count}/{Harness.Find<TextBlock>(lp, "SelectionText").Text}");
        Harness.Find<ComboBox>(lp, "ModeBox").SelectedItem = SelectionMode.Single;
        Harness.Pump();
        Harness.Check("ListBox: switching back to Single keeps just one", list.SelectedItems!.Count == 1, list.SelectedItems!.Count);
        Click(lp, "ClearButton"); Harness.Pump();
        Harness.Check("ListBox: UnselectAll empties the selection", list.SelectedItems!.Count == 0 && Harness.Find<TextBlock>(lp, "SelectionText").Text == "已选 0 项");
        Click(lp, "JumpButton"); Harness.Pump(); Harness.Pump();
        var viewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
        Harness.Check("ListBox: ScrollIntoView scrolls the list down", viewer.Offset.Y > 0, viewer.Offset.Y);
        w.Close();
    }

    static void Content()
    {
        // ---- ContentControl: the same property takes a string, a control or a data object
        var cc = new ContentControlPage();
        var w = Harness.Show(cc);
        var host = Harness.Find<ContentControl>(cc, "Host");
        Click(cc, "TextButton"); Harness.Pump();
        Harness.Check("ContentControl: a string is shown as text", Harness.Find<TextBlock>(cc, "TypeText").Text == "Content 的类型：String"
            && host.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "你好，ContentControl"), Harness.Find<TextBlock>(cc, "TypeText").Text);
        Click(cc, "ControlButton"); Harness.Pump();
        Harness.Check("ContentControl: a control is placed as it is", host.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "我是一个 Border")
            && Harness.Find<TextBlock>(cc, "TypeText").Text == "Content 的类型：Border", Harness.Find<TextBlock>(cc, "TypeText").Text);
        Click(cc, "ModelButton"); Harness.Pump();
        Harness.Check("ContentControl: a data object goes through its DataTemplate", host.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Apple"),
            string.Join("|", host.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text)));
        w.Close();

        // ---- TransitioningContentControl
        var tp = new TransitioningContentControlPage();
        w = Harness.Show(tp);
        var tcc = Harness.Find<TransitioningContentControl>(tp, "Host");
        var before = Harness.Find<TextBlock>(tp, "DoneText").Text;
        Harness.Check("TransitioningContentControl: starts with a PageSlide", tcc.PageTransition is PageSlide, tcc.PageTransition);
        Harness.Check("TransitioningContentControl: the initial content is not counted", Harness.Find<TextBlock>(tp, "DoneText").Text == "已完成 0 次过渡", before);
        Click(tp, "NextButton");
        Harness.Pump();
        Harness.Check("TransitioningContentControl: the content changes at once, whatever the transition", tcc.Content as string == "第 2 页", tcc.Content);
        // Under PageSlide the headless clock never reports completion within 1.2s; CrossFade does, so count with CrossFade.
        Harness.Find<ComboBox>(tp, "TransitionBox").SelectedIndex = 1;
        Harness.Pump();
        Click(tp, "NextButton");
        for (int i = 0; i < 8; i++) { System.Threading.Thread.Sleep(100); Harness.Pump(); }
        Harness.Check("TransitioningContentControl: a CrossFade change reports TransitionCompleted", tcc.Content as string == "第 3 页"
            && Harness.Find<TextBlock>(tp, "DoneText").Text == "已完成 1 次过渡", $"{tcc.Content}/{Harness.Find<TextBlock>(tp, "DoneText").Text}");
        Harness.Find<ComboBox>(tp, "TransitionBox").SelectedIndex = 2;
        Harness.Find<CheckBox>(tp, "ReverseCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("TransitioningContentControl: the boxes drive PageTransition and IsTransitionReversed", tcc.PageTransition is Rotate3DTransition && tcc.IsTransitionReversed);
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Data2.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

static partial class ProbeData
{
    static void Text()
    {
        // ---- Label: the access key hands focus to the Target
        var lb = new LabelPage();
        var w = Harness.Show(lb);
        var label = Harness.Find<Label>(lb, "NameLabel");
        var box = Harness.Find<TextBox>(lb, "NameBox");
        Harness.Check("Label: Target is the text box", ReferenceEquals(label.Target, box), label.Target);
        box.Focus();
        Harness.Pump();
        Harness.Check("Label: focusing the target updates the readout", Harness.Find<TextBlock>(lb, "FocusText").Text == "输入框：已聚焦", Harness.Find<TextBlock>(lb, "FocusText").Text);
        w.Close();

        // ---- SelectableTextBlock: the buttons write the selection and the readout reads SelectedText
        var sp = new SelectableTextBlockPage();
        w = Harness.Show(sp);
        var stb = Harness.Find<SelectableTextBlock>(sp, "Selectable");
        Click(sp, "FirstWordButton"); Harness.Pump();
        Harness.Check("SelectableTextBlock: 0..5 selects the first five characters", stb.SelectedText == "Hello", stb.SelectedText);
        Harness.Check("SelectableTextBlock: the readout shows the selection", Harness.Find<TextBlock>(sp, "SelectionText").Text == "SelectedText：Hello", Harness.Find<TextBlock>(sp, "SelectionText").Text);
        Click(sp, "SelectAllButton"); Harness.Pump();
        Harness.Check("SelectableTextBlock: SelectAll covers the whole text", stb.SelectedText == stb.Text && stb.CanCopy, $"{stb.SelectedText?.Length}/{stb.Text?.Length}");
        Click(sp, "ClearButton"); Harness.Pump();
        Harness.Check("SelectableTextBlock: clearing empties the readout", Harness.Find<TextBlock>(sp, "SelectionText").Text == "SelectedText：（空）", Harness.Find<TextBlock>(sp, "SelectionText").Text);
        w.Close();

        // ---- TextBlock: the controls drive the sample
        var tb = new TextBlockPage();
        w = Harness.Show(tb);
        var sample = Harness.Find<TextBlock>(tb, "Sample");
        Harness.Check("TextBlock: wrapping is off by default", sample.TextWrapping == TextWrapping.NoWrap, sample.TextWrapping);
        var noWrapHeight = sample.Bounds.Height;
        Harness.Find<ComboBox>(tb, "WrapBox").SelectedItem = TextWrapping.Wrap;
        Harness.Pump();
        Harness.Check("TextBlock: Wrap makes the 300-wide sample taller", sample.Bounds.Height > noWrapHeight, $"{noWrapHeight} -> {sample.Bounds.Height}");
        var wrappedHeight = sample.Bounds.Height;
        Harness.Find<Slider>(tb, "MaxLinesSlider").Value = 1;
        Harness.Pump();
        Harness.Check("TextBlock: MaxLines=1 cuts it back to one line", sample.MaxLines == 1 && sample.Bounds.Height < wrappedHeight, $"{sample.MaxLines}/{sample.Bounds.Height}");
        Harness.Find<Slider>(tb, "SizeSlider").Value = 24;
        Harness.Find<ComboBox>(tb, "AlignBox").SelectedItem = TextAlignment.Center;
        Harness.Pump();
        Harness.Check("TextBlock: the controls drive FontSize and TextAlignment", sample.FontSize == 24 && sample.TextAlignment == TextAlignment.Center, $"{sample.FontSize}/{sample.TextAlignment}");
        w.Close();

        // ---- TextTrimming: the picture is trimmed, the string is not
        var tt = new TextTrimmingPage();
        w = Harness.Show(tt);
        var trimmed = Harness.Find<TextBlock>(tt, "Trimmed");
        var fullLength = trimmed.Text!.Length;
        Harness.Check("TextTrimming: starts on CharacterEllipsis", trimmed.TextTrimming == TextTrimming.CharacterEllipsis, trimmed.TextTrimming);
        Harness.Check("TextTrimming: the text keeps its width of 120", Math.Abs(trimmed.Bounds.Width - 120) < 1.5, trimmed.Bounds.Width);
        Harness.Find<ComboBox>(tt, "TrimBox").SelectedIndex = 5;
        Harness.Pump();
        Harness.Check("TextTrimming: the box picks PathSegmentEllipsis", trimmed.TextTrimming == TextTrimming.PathSegmentEllipsis, trimmed.TextTrimming);
        Harness.Find<Slider>(tt, "WidthSlider").Value = 300;
        Harness.Pump();
        Harness.Check("TextTrimming: the slider widens the block", Math.Abs(trimmed.Bounds.Width - 300) < 1.5, trimmed.Bounds.Width);
        Harness.Check("TextTrimming: Text is never shortened", trimmed.Text!.Length == fullLength
            && Harness.Find<TextBlock>(tt, "LengthText").Text == $"Text 长度 = {fullLength}（无论怎么截都不变）", Harness.Find<TextBlock>(tt, "LengthText").Text);
        w.Close();
    }

    static void Structured()
    {
        // ---- DataGrid: the StyleInclude is what makes it render at all
        var dg = new DataGridPage();
        var w = Harness.Show(dg);
        var grid = Harness.Find<DataGrid>(dg, "Grid");
        // 6 rows x (4 columns + 1 filler cell the grid appends to every row) = 30 cells.
        Harness.Check("DataGrid: the StyleInclude makes it draw rows, cells and headers",
            grid.GetVisualDescendants().OfType<DataGridRow>().Count() == 6
            && grid.GetVisualDescendants().OfType<DataGridCell>().Count() == 30
            && grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Any(),
            $"{grid.GetVisualDescendants().OfType<DataGridRow>().Count()}/{grid.GetVisualDescendants().OfType<DataGridCell>().Count()}");
        Harness.Check("DataGrid: 4 manual columns, Extended selection, no grid lines by default",
            grid.Columns.Count == 4 && grid.SelectionMode == DataGridSelectionMode.Extended && grid.GridLinesVisibility == DataGridGridLinesVisibility.None,
            $"{grid.Columns.Count}/{grid.SelectionMode}/{grid.GridLinesVisibility}");
        grid.SelectedIndex = 1;
        Harness.Pump();
        Harness.Check("DataGrid: selecting row 1 reports Bread", Harness.Find<TextBlock>(dg, "SelectedText").Text == "选中：Bread（Bakery，3.5）", Harness.Find<TextBlock>(dg, "SelectedText").Text);
        Harness.Find<ComboBox>(dg, "LinesBox").SelectedItem = DataGridGridLinesVisibility.All;
        Harness.Find<ComboBox>(dg, "SelectionBox").SelectedItem = DataGridSelectionMode.Single;
        Harness.Find<CheckBox>(dg, "ReadOnlyCheck").IsChecked = true;
        Harness.Find<CheckBox>(dg, "ReorderCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("DataGrid: the controls drive lines, selection mode, read-only and reorder",
            grid.GridLinesVisibility == DataGridGridLinesVisibility.All && grid.SelectionMode == DataGridSelectionMode.Single && grid.IsReadOnly && grid.CanUserReorderColumns);
        w.Close();

        // ---- TableView: in the main package, so it renders without any StyleInclude
        var tv = new TableViewPage();
        w = Harness.Show(tv);
        var table = Harness.Find<TableView>(tv, "Table");
        // The 170-high table is virtualised: only the rows that fit are realised, not all 6.
        Harness.Check("TableView: rows and 3 headers drawn without a StyleInclude",
            table.GetVisualDescendants().OfType<TableViewRow>().Count() is > 0 and < 6 && table.Columns.Count == 3 && table.GetVisualDescendants().OfType<TableViewColumnHeader>().Count() == 3,
            $"{table.GetVisualDescendants().OfType<TableViewRow>().Count()}/{table.Columns.Count}");
        Harness.Check("TableView: the first cell reads Apple", table.GetVisualDescendants().OfType<TableViewCell>().First().GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Apple"));
        table.SelectedIndex = 2;
        Harness.Pump();
        Harness.Check("TableView: selecting row 2 reports Cherry", Harness.Find<TextBlock>(tv, "SelectedText").Text == "选中：Cherry（Fruit，5.0）", Harness.Find<TextBlock>(tv, "SelectedText").Text);
        w.Close();

        // ---- TreeView: ExpandSubTree opens everything, and CollapseSubTree bubbles Collapsed
        var tp = new TreeViewPage();
        w = Harness.Show(tp);
        var tree = Harness.Find<TreeView>(tp, "Tree");
        Harness.Check("TreeView: the roots are collapsed, only the 2 roots are realised", tree.GetRealizedTreeContainers().OfType<TreeViewItem>().Count() == 2
            && tree.GetRealizedTreeContainers().OfType<TreeViewItem>().All(i => !i.IsExpanded), tree.GetRealizedTreeContainers().Count());
        Click(tp, "ExpandAllButton"); Harness.Pump(); Harness.Pump();
        var expanded = tree.GetVisualDescendants().OfType<TreeViewItem>().ToList();
        Harness.Check("TreeView: Expand all reveals all 7 nodes, every parent open", expanded.Count == 7 && expanded.Where(i => i.ItemCount > 0).All(i => i.IsExpanded),
            $"{expanded.Count}/{expanded.Count(i => i.IsExpanded)}");
        Click(tp, "CollapseAllButton"); Harness.Pump();
        Harness.Check("TreeView: Collapse all closes every parent", tree.GetVisualDescendants().OfType<TreeViewItem>().Where(i => i.ItemCount > 0).All(i => !i.IsExpanded));
        Click(tp, "ExpandAllButton"); Harness.Pump(); Harness.Pump();
        var leaf = tree.GetVisualDescendants().OfType<TreeViewItem>().First(i => (i.DataContext as DemoTreeNode)?.Label == "合同");
        tree.SelectedItem = leaf.DataContext;
        Harness.Pump();
        Harness.Check("TreeView: selecting a node reports it", Harness.Find<TextBlock>(tp, "SelectedText").Text == "选中：合同", Harness.Find<TextBlock>(tp, "SelectedText").Text);
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Data3.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeData
{
    static void HowTos()
    {
        // ---- ItemsControl how-to
        var ih = new ItemsControlHowToPage();
        var w = Harness.Show(ih);
        var tags = Harness.Find<ItemsControl>(ih, "Tags");
        var empty = Harness.Find<TextBlock>(ih, "EmptyText");
        Harness.Check("ItemsControl how-to: tags sit in a WrapPanel", tags.GetVisualDescendants().OfType<WrapPanel>().Any() && tags.ItemCount == 2);
        Harness.Check("ItemsControl how-to: the empty hint is hidden while there are tags", !empty.IsVisible);
        Click(ih, "ClearTagsButton"); Harness.Pump();
        Harness.Check("ItemsControl how-to: clearing shows the empty hint", empty.IsVisible, empty.IsVisible);
        Click(ih, "AddTagButton"); Harness.Pump();
        Harness.Check("ItemsControl how-to: adding a tag hides it again", !empty.IsVisible);

        var striped = Harness.Find<ItemsControl>(ih, "Striped");
        var odd = striped.GetRealizedContainers().Count(c => c.Classes.Contains("odd"));
        Harness.Check("ItemsControl how-to: 3 of the 6 containers got the odd class", striped.GetRealizedContainers().Count() == 6 && odd == 3, $"{striped.GetRealizedContainers().Count()}/{odd}");
        var stripedBrush = striped.GetRealizedContainers().OfType<ContentPresenter>().First(c => c.Classes.Contains("odd")).Background;
        Harness.Check("ItemsControl how-to: the page style paints the odd containers", stripedBrush is not null, stripedBrush);

        var big = Harness.Find<ItemsControl>(ih, "Big");
        var realized = big.GetRealizedContainers().Count();
        Harness.Check("ItemsControl how-to: 1000 items realise only the visible handful", big.ItemCount == 1000 && realized > 0 && realized < 50, $"{big.ItemCount}/{realized}");
        Harness.Check("ItemsControl how-to: the readout shows both numbers", Harness.Find<TextBlock>(ih, "RealizedText").Text == $"共 1000 项，已实现 {realized} 个容器", Harness.Find<TextBlock>(ih, "RealizedText").Text);
        w.Close();

        // ---- ListBox how-to
        var lh = new ListBoxHowToPage();
        w = Harness.Show(lh);
        var vm = (ListBoxHowToViewModel)lh.DataContext!;
        var todos = Harness.Find<ListBox>(lh, "Todos");
        Harness.Check("ListBox how-to: the visible rows hold check boxes (the list is virtualised)", todos.GetVisualDescendants().OfType<CheckBox>().Count() is >= 3 and <= 4, todos.GetVisualDescendants().OfType<CheckBox>().Count());
        todos.GetVisualDescendants().OfType<CheckBox>().ElementAt(1).IsChecked = true;
        todos.GetVisualDescendants().OfType<CheckBox>().ElementAt(2).IsChecked = true;
        Harness.Pump();
        Harness.Check("ListBox how-to: ticking writes IsDone on the model and not the selection",
            vm.DoneCount == 2 && vm.Todos[1].IsDone && vm.Todos[2].IsDone && Harness.Find<TextBlock>(lh, "DoneText").Text == "已完成 2 项", $"{vm.DoneCount}/{Harness.Find<TextBlock>(lh, "DoneText").Text}");
        var strip = Harness.Find<ListBox>(lh, "Strip");
        Harness.Check("ListBox how-to: the horizontal strip lays items out left to right",
            strip.GetVisualDescendants().OfType<ListBoxItem>().Take(2).Select(i => i.Bounds.X).Distinct().Count() == 2
            && strip.GetVisualDescendants().OfType<ListBoxItem>().Take(2).Select(i => i.Bounds.Y).Distinct().Count() == 1);
        var accent = Harness.Find<ListBox>(lh, "Accent");
        var selected = accent.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.IsSelected);
        var brush = selected.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter").Background as ISolidColorBrush;
        Harness.Check("ListBox how-to: the selected row is painted orange through the template part", brush?.Color == Avalonia.Media.Colors.Orange, brush?.Color);
        w.Close();

        // ---- DataGrid how-to
        var dh = new DataGridHowToPage();
        w = Harness.Show(dh);
        var model = (DataGridHowToViewModel)dh.DataContext!;
        var group = Harness.Find<DataGrid>(dh, "GroupGrid");
        Harness.Check("DataGrid how-to: grouping by Category makes 2 groups with 2 headers", model.Grouped.Groups!.Count == 2
            && group.GetVisualDescendants().OfType<DataGridRowGroupHeader>().Count() == 2,
            $"{model.Grouped.Groups!.Count}/{group.GetVisualDescendants().OfType<DataGridRowGroupHeader>().Count()}");
        Harness.Check("DataGrid how-to: the sorted view starts with Croissant (Name descending)", model.Sorted.Cast<DemoProduct>().First().Name == "Croissant", model.Sorted.Cast<DemoProduct>().First().Name);
        var sortGrid = Harness.Find<DataGrid>(dh, "SortGrid");
        Harness.Check("DataGrid how-to: the template column sorts by Price", sortGrid.Columns[1].SortMemberPath == "Price", sortGrid.Columns[1].SortMemberPath);
        var detail = Harness.Find<DataGrid>(dh, "DetailGrid");
        Harness.Check("DataGrid how-to: one column is frozen", detail.FrozenColumnCount == 1);
        var before = detail.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text?.StartsWith("说明：") == true);
        detail.SelectedIndex = 0;
        Harness.Pump(); Harness.Pump();
        var after = detail.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text?.StartsWith("说明：") == true);
        Harness.Check("DataGrid how-to: selecting a row opens its detail", before == 0 && after == 1 && detail.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "说明：fresh"), $"{before}->{after}");
        w.Close();

        // ---- TreeView how-to
        var th = new TreeViewHowToPage();
        w = Harness.Show(th);
        var tm = (TreeViewHowToViewModel)th.DataContext!;
        var mixed = Harness.Find<TreeView>(th, "Mixed");
        Harness.Check("TreeView how-to: starts collapsed with 2 root folders", mixed.GetVisualDescendants().OfType<TreeViewItem>().Count() == 2, mixed.GetVisualDescendants().OfType<TreeViewItem>().Count());
        Click(th, "ExpandAllButton"); Harness.Pump(); Harness.Pump();
        Harness.Check("TreeView how-to: Expand all from the model opens all 7 nodes", mixed.GetVisualDescendants().OfType<TreeViewItem>().Count() == 7
            && ((FolderNode)tm.Roots[0]).IsExpanded, mixed.GetVisualDescendants().OfType<TreeViewItem>().Count());
        Harness.Check("TreeView how-to: folders and files use different templates",
            mixed.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "📁 src") && mixed.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "📄 App.axaml"));
        mixed.SelectedItem = tm.Roots.OfType<FolderNode>().First().Children.OfType<FileNode>().First();
        Harness.Pump();
        Harness.Check("TreeView how-to: selecting a file reports its size", Harness.Find<TextBlock>(th, "MixedText").Text == "选中文件：README.md（1 KB）", Harness.Find<TextBlock>(th, "MixedText").Text);
        Click(th, "CollapseAllButton"); Harness.Pump();
        Harness.Check("TreeView how-to: Collapse all from the model closes the roots", !((FolderNode)tm.Roots[0]).IsExpanded && !((FolderNode)tm.Roots[1]).IsExpanded);

        var lazy = tm.Lazy[0];
        Harness.Check("TreeView how-to: the lazy root starts with 1 placeholder and 0 loads", lazy.Children.Count == 1 && lazy.LoadCount == 0 && lazy.Children[0].Name == "加载中…");
        lazy.IsExpanded = true;
        Harness.Pump(); Harness.Pump();
        Harness.Check("TreeView how-to: the first expansion replaces the placeholder with 3 real children",
            lazy.LoadCount == 1 && lazy.Children.Count == 3 && lazy.Children.All(c => c.Name.StartsWith("子项")), $"{lazy.LoadCount}/{lazy.Children.Count}");
        lazy.IsExpanded = false; lazy.IsExpanded = true;
        Harness.Check("TreeView how-to: expanding again does not load again", lazy.LoadCount == 1, lazy.LoadCount);
        Harness.Check("TreeView how-to: the readout shows the load count", Harness.Find<TextBlock>(th, "LazyText").Text == "远程目录已加载 1 次", Harness.Find<TextBlock>(th, "LazyText").Text);
        w.Close();
    }
}
```

同时把 `C:\Temp\probe-controls\Program.cs` 的 `Main` 改为依次调用 `ProbeShell.Run(); ProbeButtons.Run(); ProbeInput.Run(); ProbeLayout.Run(); ProbeData.Run(); ProbePackages.Run(); ProbeNewPages.Run();`。

**第 00 份留下的 `Probe.Packages.cs` 要做一处调整**：`App.axaml` 现在已经包含 DataGrid 样式，"不加样式时没有行"的那条断言不再成立。把

```csharp
Harness.Check("DataGrid without a StyleInclude realises no rows", RealisedRows() == 0);
```

换成

```csharp
// ControlsDemo App.axaml already includes the DataGrid theme (plan 03 Task 1), so it renders by default.
Harness.Check("DataGrid renders under the app styles", RealisedRows() == 2);
```

并删掉其后 `var dg = Include(...)` 到 `Application.Current!.Styles.Remove(dg);` 的三行（它们会把同一份样式再加一遍）。

- [ ] **Step 3: 跑探针**

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "FAIL|error|Unhandled|passed"`
Expected: `0 failed`、`0 warning log(s)`（编写计划时实测为 **484 passed**，其中第 00–02 份的 369 条在内，本份新增 115 条）。

**失败时的排查顺序**：
1. `Binding` 警告里出现 `Unable to cast object of type 'X' to type 'Y'`：样式里 `TreeViewItem` 的绑定类型写成了某一种节点，要写所有节点的共同基类；
2. `TransitioningContentControl` 完成次数不对：`PageSlide` 在 headless 里 1.2 秒内不报完成，要用 `CrossFade` 计数；初始内容也会报一次，页面已跳过它；
3. `DataGrid` / `TableView` 行数不是 6：二者都虚拟化，只实现视口里放得下的行；`DataGrid` 的单元格数还包含每行末尾的一个填充格（6 行 × 5 = 30）；
4. `ListBox how-to` 复选框数不是 4：同样是虚拟化，130 高的列表只实现 3～4 行；
5. 任何页面 `renders` 失败且无异常：对照第 00 份「Buttons 页面的共同写法」检查骨架；`DataGrid` 页空白先查 `App.axaml` 的 `StyleInclude`。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|warn|个错误|Build succeeded" | tail -5`
Expected: 0 个错误、无新增警告。

- [ ] **Step 5: 真实窗口目视（人工，一次）**

```bash
dotnet run --project Avalonia.ControlsDemo
```

确认：Data display 分类下 16 个条目都能切换；`DataGrid` 页点列头排序、双击单元格编辑、拖列头调宽；`Carousel` 页换成 `PageSlide` / `CrossFade` 后切换有动画；`TreeView` 页「展开全部」把整棵树打开；`SelectableTextBlock` 页能用鼠标拖选并 Ctrl+C 复制；`Label` 页按 Alt+N 把焦点交给输入框；`TextTrimming` 页拖动宽度时省略号位置随之变化；`TransitioningContentControl` 页换 `PageSlide` 时能看到滑动。鼠标拖选、访问键、动画与真实排序是 headless 验证不了的部分。

- [ ] **Step 6: 回填 spec**

在 spec「待验证的技术风险」末尾的「批 2 实测结论」之后追加：

```markdown
### 批 3 实测结论（填入执行日期）

- **`TableView` 在主包里**：`TableView : ListBox`，12.1.2 无需任何包与 `StyleInclude` 就能渲染；列类型只有 `TableViewColumn`（`Header`、`Width`、`Binding`），没有 `TableViewTextColumn`。`DataGrid` 仍是独立包，必须 `StyleInclude Themes/Fluent.xaml`。
- **虚拟化让"行数"断言不可靠**：`DataGrid`、`TableView`、`ListBox` 只实现视口放得下的行，探针断言要写范围而不是总数；`DataGrid` 每行还会多一个填充单元格。
- **`TreeView` 的两个坑**：样式里给 `TreeViewItem` 绑 `IsExpanded` 时，选择器会匹配所有节点，绑定类型必须是所有节点类型的共同基类，否则文件节点报强转警告；`CollapseSubTree` 期间 `Collapsed` 会被子节点冒泡触发多次，不能拿来计数。
- **编译绑定的参数**：`ConverterParameter={x:Int32 0}` 编译不过（`x:Int32` 不是标记扩展），要用 `<Binding.ConverterParameter><x:Int32>0</x:Int32></Binding.ConverterParameter>` 的元素语法；`TransitioningContentControl` 对初始内容也报一次 `TransitionCompleted`，且 headless 里 `PageSlide` 不报完成。
- **范围缺口**：官方 Data display 下还有 `charts/**`、`pdfviewer/**`、`structured-data/treedatagrid/**`，spec 的 12 页没有它们，转入第 06 份的 Premium 路标页。
- **官方 How-to 与 12.1.2 的出入**：无。
- **探针断言实际条数**：本批累计 484 条（含第 00–02 份），全部通过且零警告日志。
```

尖括号与"填入执行日期"必须替换为实际日期；数字以 Step 3 的真实输出为准。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: complete the Data display category with 12 control pages and 4 how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review（编写者自查记录）

**Spec 覆盖**：Data display 的 Collections 3、Structured data 3、Text display 4、独立页 2 共 12 页 → Task 2–5；4 篇 How-to（ItemsControl、ListBox、DataGrid、TreeView）→ Task 6；`DataGrid` 包与样式 → Task 1；官方侧边栏顺序与 HowTo 紧跟 → Task 7 Step 1 与探针的"紧跟"断言；与既有演示的去重 → ItemsControl、ListBox、TreeView、ContentControl 页末尾的指向行。

**已实测，不是推断**：本份全部页面与模型代码已从计划原文抽取到 `C:\Temp\plan-verify`，构建 0 错误，探针 484 条通过、零警告日志。构建与实测时发现并已回写到上面代码里的有五处：`GetRealizedTreeContainers()` 返回 `Control`，要 `OfType<TreeViewItem>()`；`ConverterParameter={x:Int32 0}` 编译不过；样式里 `TreeViewItem` 的 `IsExpanded` 绑定类型必须是节点共同基类；`TransitioningContentControl` 的初始内容也报完成；`DataGrid` 每行多一个填充单元格、`TableView`/`ListBox` 虚拟化。

**类型一致性**：元素名与 Task 2–6 的 Interfaces 列表一致；`TreeNodeBase.IsExpanded`、`FolderNode.Children`、`FileNode.SizeKb`、`LazyNode.LoadCount` 在 ViewModel、XAML 与探针里一致；`DemoProduct.Sample()` 的 6 项顺序（Apple、Bread、Cherry、Bagel、Banana、Croissant）与探针里的 `Bread` / `Cherry` / `fresh` / `Croissant` 断言一致。

**未覆盖，已知**：`DataGrid` 的真实排序与单元格编辑、`SelectableTextBlock` 的鼠标拖选与 Ctrl+C、`Label` 的访问键、`PageSlide` / `Rotate3D` 的动画观感；这些在 Step 5 目视。