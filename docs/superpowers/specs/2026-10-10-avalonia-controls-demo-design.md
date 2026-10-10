# 按官方控件目录构建 ControlsDemo 设计

日期：2026-10-10
状态：待审阅

## 背景

`hello-avalonia` 已按官方文档的 14 个能力分类建立了演示项目（见
`2026-09-21-avalonia-docs-category-demos-design.md`），但官方文档的另一棵树——
**Controls 控件目录**（<https://docs.avaloniaui.net/controls>）——没有对应的演示。
扫描现有 17 个项目的 `.axaml` 后确认，下列控件从未出现过：`NumericUpDown`、
`DatePicker`、`Calendar`、`TimePicker`、`DataGrid`、`TreeView`（仅作为附带出现）、
`Expander`、`SplitView`、`ContextMenu`、`NativeMenu`、`TrayIcon`、`ColorPicker`、
`AutoCompleteBox`、`MaskedTextBox`、`GridSplitter`、`Viewbox`、`TabStrip`、
`Carousel`、`Popup`、`ScrollBar` 等。

官方 Welcome 页的 **How-to Guides** 共 28 篇，此前 spec 把它列为"内容分散在各分类中，
不单独成项目"，但并没有真正把这些实战指南落到演示里。

## 目标

1. 新建单个项目 `Avalonia.ControlsDemo`，按官方 Controls 目录的 10 个分类组织，
   为每个**免费**控件提供一个可运行的演示页
2. 把 28 篇 How-to 按所属分类整合进来：18 篇做成并入对应分类的实战页，
   10 篇横切主题做成指向现有项目的路标页
3. 付费控件（Pro / Enterprise）只做路标页，说明用途、授权类型与官方链接

## 非目标

- 不实现任何付费控件的可运行演示（Charts、PdfViewer、Markdown、TreeDataGrid、
  RichTextEditor、MediaPlayer、VirtualKeyboard），这些需要商业授权包
- 不重复实现已被现有项目完整演示的主题（见"去重规则"），只做最小控件页加路标
- 不重构现有 17 个项目
- 不为演示项目写自动化测试（沿用既有约定，用仓库外的 headless 探针验证）
- 不引入第三方包。只用 `Avalonia`、`Avalonia.Desktop`、`Avalonia.Themes.Fluent`、`Avalonia.Fonts.Inter`、
  `CommunityToolkit.Mvvm`，加上两个**官方**独立包 `Avalonia.Controls.DataGrid` 与
  `Avalonia.Controls.ColorPicker`（见「依赖与技术选型」）

## 范围核实

范围来自对官方站点地图（`sitemap.xml`）的完整抓取，付费边界以官方的
`/controls/tags/avalonia-pro` 与 `/controls/tags/avalonia-enterprise` 标签页为准，
而不是 `/controls` 首页——首页只列了约 35 张卡片，漏掉了绝大多数控件。

- `/controls/` 下共 274 个文档页，其中 Charts 一组就占约 125 页
- 带 Pro / Enterprise 标签的 7 组：Charts、PdfViewer、Markdown、TreeDataGrid、
  RichTextEditor、MediaPlayer、VirtualKeyboard
- 去掉这 7 组及其子页后，**免费控件页共 84 个**；其中 Web 分类的 4 页由已有的
  `Avalonia.WebViewDemo` 承担，只做路标，所以**可运行的控件页为 80 个**

## 项目骨架

沿用现有项目形态（WinExe + net10.0 + Fluent + 引用 `Avalonia.Shared`，csproj 与
`Avalonia.LayoutDemo` 相同），区别只在导航外壳：80 个控件页放不进 `TabControl`
（Layout 一个分类就有 20 个），所以顶层改用**侧边导航**。

```
Avalonia.ControlsDemo/
├── App.axaml(.cs)              Fluent 主题 + SharedStyles
├── Program.cs
├── app.manifest
├── Assets/avalonia-logo.ico
├── Navigation/
│   ├── PageEntry.cs            一个页面的元数据：标题、分类、官方路径、页面类型、种类
│   ├── CategoryNode.cs         导航树的分类节点（含子 PageEntry）
│   └── PageCatalog.cs          全部 PageEntry 的唯一登记处
├── ViewModels/
│   ├── MainViewModel.cs        导航树、当前选中页、搜索关键字
│   └── <Page>ViewModel.cs      仅有状态的页面才建，与页面一一对应
├── Views/
│   ├── MainWindow.axaml(.cs)   SplitView：左 TreeView，右 ContentControl
│   └── Pages/
│       ├── Input/              ButtonPage、NumericUpDownPage、DatePickerPage ...
│       ├── Layout/
│       ├── DataDisplay/
│       ├── Feedback/ Media/ Menus/ Navigation/ Primitives/ System/ Web/
│       ├── Premium/            七个付费控件的路标页
│       └── HowTo/              横切主题的路标页（见"How-to 整合"）
└── Avalonia.ControlsDemo.csproj
```

**导航机制**：`PageCatalog` 持有全部 `PageEntry`，`MainViewModel` 从它构建分类树。
`PageEntry.PageType` 是页面的 `Type`，选中时由 `Activator.CreateInstance` 实例化后放进
右侧 `ContentControl.Content`。不使用 `ViewLocator`，也不给每个页面注册 `DataTemplate`——
80 个页面写 80 条模板是纯样板，而 `PageCatalog` 本身就是索引。

**为什么用 `PageCatalog` 单点登记**：新增一个控件页只需要两步（建页面文件、在
`PageCatalog` 加一行）。导航树、页面总数断言、README 的控件清单都从这一处派生，
不会出现"页面写了但菜单里没有"的漏登记。

**页面种类**（`PageKind` 枚举，导航树上用不同标记区分）：

| 种类 | 含义 | 数量 |
|---|---|---|
| `Control` | 控件的可运行演示页 | 约 80 |
| `HowTo` | 并入控件旁的 How-to 实战页 | 19 |
| `Signpost` | 路标页：一句话说明加指向的项目或官方链接（Web 4 + 付费 7 + 横切 How-to 9） | 20 |

**页面说明条**：沿用 `Avalonia.Shared` 的 `DemoHeader`（`Title` + `DocPath`）。
`DocPath` 写官方路径（如 `controls/input/selectors/numericupdown`），读者可以直接对照原文。
How-to 页的说明条多一行，标出对应的 How-to 篇名与路径（如 `docs/how-to/treeview-how-to`）。

**语言约定**：与现有项目一致——C# 与 XAML 注释英文、界面文字中文、标识符英文。

## 控件映射

官方路径均相对 `https://docs.avaloniaui.net`。"已演示"列指该控件已在哪个现有项目里出现过；
这类控件仍做一个最小控件页（保证目录完整），但深度内容指向原项目（见"去重规则"）。

### Input（22 页）

| 子类 | 控件 | 已演示 |
|---|---|---|
| Buttons | Button、ButtonSpinner、HyperlinkButton、RadioButton、RepeatButton、SplitButton、ToggleButton、ToggleSplitButton | Button 处处可见 |
| Date and time | Calendar、CalendarDatePicker、DatePicker、TimePicker | 无 |
| Selectors | CheckBox、ColorPicker、ColorView、ComboBox、NumericUpDown、Slider、ToggleSwitch | CheckBox、Slider 等 |
| Text input | AutoCompleteBox、MaskedTextBox、TextBox | TextBox |

### Layout（20 页）

| 子类 | 控件 | 已演示 |
|---|---|---|
| Containers | Border、Expander、Flyout、GroupBox、PipsPager、RefreshContainer、ScrollViewer、SplitView、Viewbox | Border、ScrollViewer、Flyout |
| 独立页 | Decorator、LayoutTransformControl | 无 |
| Panels | Canvas、DockPanel、Grid、GridSplitter、Panel、RelativePanel、StackPanel、UniformGrid、WrapPanel | 除 GridSplitter 外均在 LayoutDemo |

### Data display（12 页）

| 子类 | 控件 | 已演示 |
|---|---|---|
| Collections | Carousel、ItemsControl、ListBox | ItemsControl、ListBox |
| Structured data | DataGrid、TableView、TreeView | TreeView 作为附带出现 |
| Text display | Label、SelectableTextBlock、TextBlock、TextTrimming | TextBlock |
| 独立页 | ContentControl、TransitioningContentControl | 无 |

### 其余分类（共 30 页，含 Web 路标）

| 分类 | 控件 | 页面数 |
|---|---|---|
| Feedback | Notification、Popup、ProgressBar、ToolTip | 4 |
| Media | Image、DrawingImage、PathIcon | 3 |
| Menus | Menu、ContextMenu、MenuFlyout、NativeMenu、Separator | 5 |
| Navigation | TabControl、TabStrip、CommandBar、NavigationPage、DrawerPage、TabbedPage、CarouselPage、ContentPage | 8 |
| Primitives | ScrollBar、ThemeVariantScope、UserControl、Window、WindowDrawnDecorations | 5 |
| System | TrayIcon | 1 |
| Web | NativeWebView、NativeWebDialog、WebAuthenticationBroker、WebView 环境配置 | 4（路标，指向 `Avalonia.WebViewDemo`） |

说明：官方侧边栏有独立的 System 分类，其唯一入口 TrayIcon 的 URL 却在 `/controls/navigation/trayicon`
下。`PageCatalog` 按**官方侧边栏分类**登记，所以 TrayIcon 归 System，便于与官方目录逐项对照。
导航树因此是 10 个分类。

**页面数核对**：Input 22 + Layout 20 + Data display 12 + Feedback 4 + Media 3 +
Menus 5 + Navigation 8 + Primitives 5 + System 1 + Web 4 = **84 页**，
其中 Web 4 页为路标，可运行控件页 **80 页**，与"范围核实"一节一致。

## How-to 整合

官方 How-to 共 28 篇（站点地图 `/docs/how-to/*`，与 Welcome 页侧边栏一致）。
分流标准：**这篇指南的主角是不是某个控件**。是则并入该控件所在分类，否则做路标。

### 并入控件分类（18 篇，`PageKind.HowTo`）

每篇做成一个独立页面，紧跟在对应控件页之后，标题加"实战："前缀，内容是官方指南里
**能在一个窗口内演示的场景**，而非简单重复控件页。

| How-to | 并入分类 | 紧跟的控件页 |
|---|---|---|
| treeview-how-to | Data display | TreeView |
| listbox-how-to | Data display | ListBox |
| itemscontrol-how-to | Data display | ItemsControl |
| datagrid-how-to | Data display | DataGrid |
| textbox-how-to | Input | TextBox |
| combobox-how-to | Input | ComboBox |
| slider-how-to | Input | Slider |
| datepicker-how-to | Input | DatePicker |
| grid-how-to | Layout | Grid |
| scrollviewer-how-to | Layout | ScrollViewer |
| expander-how-to | Layout | Expander |
| tabcontrol-how-to | Navigation | TabControl |
| navigation-how-to | Navigation | NavigationPage |
| menu-how-to | Menus | Menu |
| image-how-to | Media | Image |
| notifications-how-to | Feedback | Notification |
| window-how-to | Primitives | Window |
| dialogs-how-to | Primitives | Window（紧随 window-how-to） |

### 横切主题路标（9 篇，`PageKind.Signpost`）

这些主题不属于任何控件，且现有项目已有对应演示（Tab 名已对照各项目 `MainWindow.axaml`
的实际 `Header` 核实）。放在导航树的"How-to 路标"分类下，每页写一句话说明加指向，
不重复实现。

| How-to | 指向 |
|---|---|
| responsive-layout-how-to | `Avalonia.LayoutDemo` →「响应式布局」 |
| styling-controls-how-to | `Avalonia.StylingDemo` →「选择器」「样式类」「ControlTheme」 |
| theme-switching-how-to | `Avalonia.StylingDemo` →「主题变体」 |
| custom-font-how-to | `Avalonia.StylingDemo` →「字体」 |
| mvvm-how-to | `Avalonia.FundamentalsDemo` →「MVVM」 |
| coded-ui-how-to | `Avalonia.FundamentalsDemo` →「纯代码 UI」 |
| drag-and-drop-how-to | `Avalonia.InputDemo` →「拖放」 |
| clipboard-how-to | `Avalonia.ServicesDemo` →「剪贴板」 |
| debugging-how-to | `Avalonia.AppDevelopmentDemo` →「日志」「未处理异常」 |

### data-persistence-how-to：现有项目未覆盖，改为实战页

在全部演示项目中搜索 `ApplicationData`、`SpecialFolder`、`JsonSerializer`、
`File.Write/Read`，只有 `WebViewDemo` 有一处无关的文件操作，没有任何应用设置持久化演示。
所以这一篇**不做路标，做成真正的实战页**，放在 System 分类：把设置写入
`%AppData%` 下的 JSON 文件并在下次启动时读回。因此最终数字是 **19 篇并入、9 篇路标**，
合计 28 篇。

页面数随之调整：System 分类由 1 页变为 2 页（TrayIcon + 设置持久化），
`HowTo` 种类共 19 页。

## 去重规则

1. **一个知识点只完整实现一次。** 控件页关注"这个控件有什么属性、事件、常见用法"，
   不重复现有项目里按主题深入的内容。
2. **控件页只演示控件自身的属性与事件。** 例如 `Grid` 页演示行列定义、`Star` / `Auto` 尺寸、
   `Grid.RowSpan`；布局系统的测量排列原理、响应式留在 `LayoutDemo`。
3. **已被现有项目深度演示的控件**（Button、TextBox、Border、ScrollViewer、Canvas、
   DockPanel、WrapPanel、UniformGrid、RelativePanel、Flyout、Image、PathIcon、
   ThemeVariantScope 等）仍做控件页以保证目录完整，但页面说明条末尾加一行
   "更深入的演示：`Avalonia.XxxDemo` →「某 Tab」"。
4. **路标页不写代码**，只有 `DemoHeader`、一段说明和指向。

## 付费控件路标

七组付费控件各一页（`Premium/` 目录），每页固定四项内容：

| 项 | 内容 |
|---|---|
| 用途 | 一句话说明控件解决什么问题 |
| 授权 | Pro 或 Enterprise（以官方标签页为准：Charts 为 Pro；PdfViewer、Markdown、TreeDataGrid、RichTextEditor、MediaPlayer、VirtualKeyboard 同时带 Pro 与 Enterprise 标签） |
| 子页 | 官方文档下的子页数量与主题（如 TreeDataGrid 的排序、过滤、选择模式） |
| 链接 | 官方路径，可点击在默认浏览器中打开（`Launcher`） |

付费控件在导航树的"付费控件"分类下集中展示，而不是散落在各分类里，
避免读者点进去才发现需要授权。

## 依赖与技术选型

只新增两个**官方**包，均为 12.1.2，在 `Directory.Packages.props` 声明：

| 包 | 用途 | 引用它的页面 |
|---|---|---|
| `Avalonia.Controls.DataGrid` | `DataGrid` | Data display 的 DataGrid 与 DataGrid How-to |
| `Avalonia.Controls.ColorPicker` | `ColorPicker`、`ColorView` | Input 的 ColorPicker、ColorView |

两者都**必须在 `App.axaml` 里额外 `StyleInclude` 才会渲染**，否则构造成功、能布局、但什么都不画，
且不报错不记日志（实测：`DataGrid` 行数 0、`ColorPicker` 可视后代数 0）。路径：

```xml
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml" />
<StyleInclude Source="avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml" />
```

其余控件（`NumericUpDown`、`TableView`、`NavigationPage` 等）均在主包里，已用反射确认。
`Avalonia.ControlsDemo.csproj` 的其余包引用与 `Avalonia.LayoutDemo` 相同。

`NativeMenu`、`TrayIcon`、`NativeWebView` 等原生控件依赖平台实现，
仅在 Windows 桌面验证。

## 待验证的技术风险

以下几点无法从文档确定，在样板阶段先验证，结果回填到本节：

1. **新页面类型在 Avalonia 12.1.2 桌面上的可用性——已解除（编写计划时实测）：六个控件全部通过判定标准，无需降级。** `NavigationPage`、`DrawerPage`、
   `TabbedPage`、`CarouselPage`、`ContentPage`、`CommandBar` 是较新的控件，文档偏向移动端场景，
   在桌面窗口里可能无法正常显示。**判定标准**：能在 headless 下实例化、`Measure` / `Arrange`
   后 `Bounds` 非零、可见子元素数大于零。不满足的降级为路标页，并在 spec 里记录原因。
2. **原生控件的 headless 行为。** `NativeMenu`、`TrayIcon`、`WindowDrawnDecorations` 在
   headless 下多半是 Noop 实现。这些页面按"先检测、再使用"写，探针只断言检测逻辑，
   真实效果需要真机目视，spec 里如实记录。
3. **`DataGrid` 的可用性——已解除（编写计划时实测）。** 它不在主包里，是独立包
   `Avalonia.Controls.DataGrid` 12.1.2（依赖 Avalonia 12.1.0）。不加 `StyleInclude` 时行数为 0，
   加 `Themes/Fluent.xaml` 后行数 2、可视后代 129。这是"不新增依赖"的唯一例外（两个包都是 Avalonia 官方发布），**待用户审阅本 spec 时确认**；若不接受，DataGrid 与 ColorPicker / ColorView 相关页面降级为路标页。
4. **`ColorPicker` / `ColorView` 的包归属——已解除（编写计划时实测）。** 同样是独立包
   `Avalonia.Controls.ColorPicker` 12.1.2；不加样式时后代数 0，加 `Themes/Fluent/Fluent.xaml` 后
   picker 12、view 101。注意主题文件在 `Fluent/` 子目录下，少一层目录会抛 `XamlLoadException`。
5. **既有规则继续适用：** 样板阶段已确认的静默失败规则（被样式驱动的属性不写本地值、
   `double` 绑定 `Thickness` 必须用转换器、`StringFormat` 以 `{0}` 开头要写 `{}` 前缀、
   派生自现有控件要覆盖 `StyleKeyOverride`、`Watermark` 已过时改用 `PlaceholderText`）
   全部沿用，不在这里重复展开，实现计划的"规则"一节会逐条列出。

## 验证标准

1. `dotnet build hello-avalonia.slnx` 通过，且无新增警告
2. **headless 探针**（仓库外、跑完即弃）对每个页面断言：
   - 页面能 `new` 出来并 `Measure` / `Arrange`，`Bounds` 非零
   - 全程挂 `LogArea.Binding` 的 `ILogSink`，零绑定警告
   - 有交互的页面，读回关键属性值（如点击后计数、选中项、`Value`），不依赖目视
3. **目录完整性断言**：`PageCatalog` 中 `Control` 页恒为 80、`HowTo` 页恒为 19、
   `Signpost` 页恒为 20；每个 `PageEntry` 的 `DocPath` 都在本 spec 的页面清单里；
   没有两个 `PageEntry` 指向同一个 `Type`
4. 每个批次结束后启动一次真实窗口，确认导航树能切换到该批的每个页面
   （这一步由人执行，因为 headless 验证不了原生控件与弹出层）

**探针局限**：sample 阶段已证明"构建 0 错误加运行无异常"检验不出静默失败，
所以探针必须读回属性值。弹出层（`Flyout`、`ContextMenu`、`Popup`、`ToolTip`）的内容在独立
的弹出层里，要从 `Flyout.Content` 之类的入口向上找，不能走可视树从页面向下找。

## 交付顺序

**批 0 至批 6**：每批结束即可构建、探针、启动验证并提交。批 0 是样板，交付后由用户 review
风格，再批量推进。

| 批 | 内容 | Control 页 | HowTo 页 | Signpost 页 |
|---|---|---|---|---|
| 0 | 导航壳、`PageCatalog`、Input 的 Buttons 子类；同时验证风险 1、3、4 | 8 | 0 | 0 |
| 1 | Input 其余：Date and time、Selectors、Text input；How-to：TextBox、ComboBox、Slider、DatePicker | 14 | 4 | 0 |
| 2 | Layout 全部；How-to：Grid、ScrollViewer、Expander | 20 | 3 | 0 |
| 3 | Data display 全部；How-to：TreeView、ListBox、ItemsControl、DataGrid | 12 | 4 | 0 |
| 4 | Feedback、Media、Menus；How-to：Notifications、Image、Menu | 12 | 3 | 0 |
| 5 | Navigation、Primitives、System；How-to：TabControl、Navigation、Window、Dialogs、数据持久化 | 14 | 5 | 0 |
| 6 | Web 与付费路标、9 篇横切 How-to 路标；README / slnx 收尾 | 0 | 0 | 20 |
| 合计 | | **80** | **19** | **20** |

**收尾**：更新 `hello-avalonia.slnx`、`README.md`（项目表新增一行，并加一节
"控件目录对照表"）。各批次的实测结论追加到本 spec 末尾的"实测结论"小节，
格式沿用前一份 spec。