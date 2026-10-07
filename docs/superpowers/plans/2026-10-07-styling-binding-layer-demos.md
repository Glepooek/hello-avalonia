# Avalonia 样式与绑定层演示项目（#4 Styling / #5 DataBinding / #6 DataTemplates）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建成 `Avalonia.StylingDemo`、`Avalonia.DataBindingDemo`、`Avalonia.DataTemplatesDemo` 三个演示项目，覆盖官方文档 Styling、Data Binding、Data Templates 三个分类的可演示功能点，并拆除与之重叠的旧项目 `Avalonia.DataTemplateDemo`。

**Architecture:** 三个独立 WinExe 项目，沿用 `Avalonia.LayoutDemo` 样板确立的结构——`MainWindow` 只承载 `TabControl` 外壳，每个功能点是 `Views/Pages/` 下一个独立 `UserControl`，页面顶部统一用 `Avalonia.Shared` 的 `DemoHeader` 显示中文说明与官方文档路径。无外部依赖，无网络调用。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm、中央包管理（`Directory.Packages.props`）

**Spec:** `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**样板参考:** `docs/superpowers/plans/2026-09-21-layout-demo-template.md`；**前一组的写法参考:** `docs/superpowers/plans/2026-09-22-foundation-layer-demos.md` 与已落成的 `Avalonia.LayoutDemo/`、`Avalonia.XamlDemo/`

## Global Constraints

以下约束适用于本 plan 的每一个任务：

- **Avalonia 版本统一为 12.1.2**，不为任何项目降级到 Avalonia 11
- **TargetFramework 为 `net10.0`**，`Nullable` 为 `enable`
- **包版本只在 `Directory.Packages.props` 声明**，`.csproj` 里的 `PackageReference` 不带 `Version` 属性
- **不引用 `Avalonia.Diagnostics`**（停在 11.3.22，v12 的 DevTools 已内置于主包）
- **不引入 ReactiveUI、Prism 等第三方 MVVM/UI 框架**，只用官方 API + `CommunityToolkit.Mvvm`
- **C# 与 XAML 注释用英文**；**界面文字（Tab 标题、说明条、按钮文案）用中文**；**标识符（类名、属性名、`x:Name`）用英文**
- **每个演示页顶部必须有 `DemoHeader`**，含中文功能点描述 + 对应官方文档路径。`DocPath` **不带 `docs/` 前缀**，写成 `分类名/子页名`（如 `styling/style-selector-syntax`、`data-binding/master-detail`、`data-templates/view-locator`），与 `Avalonia.FundamentalsDemo`、`Avalonia.XamlDemo`、`Avalonia.PropertySystemDemo` 三个已交付项目一致；`Avalonia.LayoutDemo/Views/Pages/PanelsPage.axaml:31` 那处带前缀的是样板期的旧写法，不沿袭
- **每个功能点一个 `UserControl`**，放在 `Views/Pages/` 下
- **不为演示项目写自动化测试**
- **`AvaloniaUseCompiledBindingsByDefault` 设为 `true`**
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`

### 前两组已确立的硬性规则（违反会导致静默失败）

以下规则来自 LayoutDemo 样板与基础层三个项目的实测教训，spec「样板阶段实测结论」与
「基础层实测结论」有完整记录。**每一条都对应一次"构建 0 错误、运行无异常、行为却是错的"**：

1. **数值绑到结构体属性必须走转换器。** `double` → `Thickness`/`CornerRadius` 没有内置
   转换，失败只记一条 binding warning、不抛异常。用
   `Avalonia.Shared/Converters/DoubleToThicknessConverter.cs`。
2. **打算被 Style / 伪类 / ContainerQuery 驱动的属性，不在元素上写本地值。**
   XAML 元素属性是 `BindingPriority.LocalValue`（值 0，最高），永久压过所有样式 Setter，
   且**连警告都没有**。需要兜底就写进同优先级的普通 `<Style>`，靠声明顺序被覆盖。
3. **`StringFormat` 里的字面花括号必须双写**（`{{`/`}}`）。单层会导致 .NET 复合格式化
   解析失败并**静默产出空字符串**，无报错无日志。
4. **验证用 headless 探针读回属性值，不靠目视。** 探针建在仓库外、跑完即弃，不违反
   「不为演示项目写自动化测试」约束。

### 本组新增的硬性规则（编写 plan 时已实测）

5. **headless 探针里发鼠标事件前，先确认坐标真的落在目标上。**
   控件会被父面板居中、拉伸或偏移，凭感觉写的坐标常常落在空白处——**什么都不发生且不报错**。
   实测：按钮被居中到 `Bounds = (0,80,100,40)`，点 `(10,10)` 时 `Click` 计数恒为 0；
   改点 `(20,100)` 后计数为 1，`:pointerover` 也随之生效。
   做法：用 `target.TranslatePoint(new Point(5, 5), window)` 换算出窗口坐标，或先调
   `window.InputHitTest(point)` 确认命中的是目标（或其模板内部元素），再发鼠标事件。
   能用 `RaiseEvent` / 直接设属性验证的，就不要走鼠标。
6. **XAML 的 `DataTemplate` / `TreeDataTemplate` 不能在纯代码里 new。**
   `Avalonia.Markup.Xaml.Templates.DataTemplate` 的 `Content` 期望
   `ITemplateResult<T>`，塞一个 `Control` 进去会在运行时抛
   `InvalidCastException: Unable to cast object of type 'TextBlock' to type 'TemplateResult\`1[Control]'`。
   代码里构造模板要用 `FuncDataTemplate<T>` / `FuncTreeDataTemplate<T>`（`Avalonia.Controls.Templates`）。
7. **`TreeDataTemplate` 的 `ItemsSource` 是绑定，不是集合。** 写 `ItemsSource="{Binding Children}"`，
   配合 `DataType`。单独给 `Content` 而不给 `ItemsSource` 只会渲染根节点。
8. **`x:DataType` 是编译绑定的前提。** 页面根元素上声明 `x:DataType="vm:XxxViewModel"`，
   内部所有 `{Binding}` 按编译期解析；写错属性名会在**构建期**报 `AVLN` 错误——这正是
   功能点「绑定调试」要演示的内容，本条规则是为了让其他页面的错误不混进来。
9. **`ThemeDictionaries` 里的资源只能用 `{DynamicResource}` 取。** 实测同一个键：
   `DynamicResource` 在 Light/Dark 两个 `ThemeVariantScope` 下分别得到 `#FFEEEEEE` /
   `#FF222222`，切换 scope 后实时更新；`StaticResource` 得到 **`null`**——背景直接消失，
   不抛异常。主题页要把这个对照做成演示内容本身。
10. **`?.` 空条件路径会吞掉绑定警告，同时也不触发 `FallbackValue`。** 实测
    `{Binding Selected.Name, FallbackValue='（未选择）'}` 在 `Selected` 为 null 时显示回退值
    并记一条 `Value is null` 警告；改成 `Selected?.Name` 后**警告消失、文本变成空串**，回退值
    不再生效。要显示占位文本就配 `TargetNullValue`。调试页演示这一对照。
11. **类型选择器只匹配确切类型，不匹配子类。** 实测 `StackPanel Button` 只命中 `Button`，
    同一面板里的 `ToggleButton`、`RepeatButton` 都没命中；`:is(Button)` 三者全中。
    选择器页要把这一对照摆出来——它是从 CSS 转过来的人最常踩的坑。
12. **子类控件要复用父类外观，用 `ControlTheme BasedOn`，不要用 `StyleKeyOverride`。**
    实测 `class Chip : Button` 若重写 `StyleKeyOverride => typeof(Button)`，模板是有了，但
    `local|Chip:on` 选择器**完全失效**（样式系统按 Button 匹配它），而 `Button:on` 反而命中。
    改为 `<ControlTheme x:Key="{x:Type local:Chip}" TargetType="local:Chip" BasedOn="{StaticResource {x:Type Button}}" />`
    后两者都正确。
13. **多个样式类同时命中时，比的是样式的声明顺序，不是 `Classes` 里的书写顺序。**
    实测 `.a` 绿、`.b` 黄、`.b` 声明在后：`Classes="a b"` 与 `Classes="b a"` **都是黄色**。
    样式类页把这一对照做成演示。

14. **转换器里报错，用户看到的是框架的类型转换文本，不是你的消息。** 实测 `ConvertBack`
    返回 `BindingNotification(new DataValidationException("“abc”不是数字"), DataValidationError)` 时，
    `DataValidationErrors.GetErrors` 读回 `InvalidCastException: Could not convert '{DataValidationError: ...}' to System.Double`；
    改为 `throw` 则读回 `Could not convert '(unset)' ...`。转换器在无法转换时返回
    `BindingOperations.DoNothing`（源值保持不动、不报错）；要报错就放进 ViewModel。
15. **在属性 setter 里报错，要抛 `DataValidationException`，不要抛普通异常。** 实测
    `throw new ArgumentException("年龄不能为负")` 时 TextBox 下方显示的是
    `System.ArgumentException: 年龄不能为负`（带类型名前缀）；改抛
    `Avalonia.Data.DataValidationException("年龄不能为负")` 后显示的就是 `年龄不能为负`。
    两种情况源属性都保持旧值。

16. **探针里 `RaiseEvent(new RoutedEventArgs(Button.ClickEvent))` 不会执行 `Command`。**
    它只触发 `Click` 路由事件——`Click="OnXxx"` 的处理器会跑，绑定的命令不会。实测
    `Command="{Binding AddCommand}" CommandParameter="5"` 的按钮 raise 之后 `Count` 仍为 0。
    验证命令要写 `button.Command!.Execute(button.CommandParameter)`——这同时验证了
    `Command` 与 `CommandParameter` 两个绑定。

17. **`^` 绑定的两个时序细节与直觉相反。** 实测：把 `Task<string>` 属性换成一个新的未完成任务后，
    文本**停在上一次的结果**，不会回到 `FallbackValue`；`IObservable` 订阅在页面离开可视树
    （`window.Content = null`）后**仍然存活**，只有 `DataContext` 换掉时才 `Dispose`。
    异步页的说明文字按实测写，不按直觉写。
18. **headless 默认绘图后端里，所有 `Bitmap` 的 `PixelSize` 都是 `1×1`。** 探针只能断言
    `Image.Source is Bitmap`，不能用尺寸判断图片是否加载对了。

19. **标记扩展参数里的单引号字符串，若以 `{` 开头仍会被解析成嵌套标记扩展。** 实测
    `FallbackValue='{ReflectionBinding X} …'` 显示为 `Avalonia.Data.ReflectionBinding`；
    写成 `FallbackValue='{}{ReflectionBinding X} …'` 才显示字面文本。与规则 3（`StringFormat`
    双写花括号）是两件事：规则 3 管 .NET 格式化，本条管 XAML 解析。

20. **`required` 属性对 XAML 不设防。** 实测 XAML 里实例化带 `required` 成员的类型却漏写成员，
    构建 0 错误、运行时取默认值。需要 `required` 保护的模型只在 C# 里构造。
21. **`DataTemplate DataType` 匹配子类——与规则 11 的样式类型选择器正好相反。** 实测
    `DataType="local:Shape"` 的模板渲染了 `Shape` 的子类 `Tri`（"通用 t"）；同一集合里
    `DataType="local:Circle"` 声明在前，`Circle` 便走它。模板集合按**声明顺序**取第一个匹配的，
    因此具体类型的模板要写在基类模板前面。模板集合页把这一对照做成演示。

### 编写本 plan 时已实测通过的写法（直接照用，不必再试）

在仓库外的 headless 探针里逐项读回属性值确认过，Avalonia 12.1.2：

| 写法 | 读回结果 |
|---|---|
| `StackPanel#Root > TextBlock` / `StackPanel#Root Border TextBlock` | 子代与后代分别命中 |
| `TextBlock.a.b`、`TextBlock:not(.a)`、`:is(TextBlock).big` | 均按预期命中/排除 |
| `CheckBox[IsChecked=True]` | 属性选择器生效 |
| `Classes.hot="{Binding IsHot}"` | VM 翻转后类名同步增删 |
| 嵌套 `<Style Selector="^:disabled">` | 生效 |
| `Button.tpl /template/ ContentPresenter#PART_ContentPresenter` | 能改到 Fluent 模板内部的 `CornerRadius` |
| `ListBoxItem:nth-child(2n)` | 第 2、4 项命中 |
| `ControlTheme BasedOn="{StaticResource {x:Type Button}}"` 再被二级 `BasedOn` 继承 | 两级 Setter 叠加，且保留 Fluent 模板 |
| `<FontFamily x:Key>avares://程序集/Assets/Fonts#家族名</FontFamily>` | `FontManager` 解析出家族名 |
| `{Binding Task^}`、`{Binding Observable^}`、`{Binding !Flag}` | 分别取到结果值与取反 |
| `{Binding 方法名}` 绑到 `Command` + `CommandParameter` | 方法被调用并收到参数 |
| `ObservableValidator` + `[NotifyDataErrorInfo]` + DataAnnotations | `DataValidationErrors.GetHasErrors` 为 `True`，错误文本为特性里的中文消息 |
| `<ItemsPanelTemplate><WrapPanel /></ItemsPanelTemplate>` | `ItemsPanelRoot` 为 `WrapPanel` |
| `TreeDataTemplate DataType=... ItemsSource="{Binding Children}"` | 展开后子节点出现 |
| `TabControl ItemsSource` + `ItemTemplate` + `ContentTemplate` | 标题与内容分别套模板，切换生效 |
| 实现 `IDataTemplate` 的 ViewLocator 放进 `DataTemplates` | `XxxViewModel` 被解析成 `XxxView` |
| `{Binding $parent[Window].Title}` | 取到窗口标题 |
| `UserControl.Styles` 里的 `FlyoutPresenter` 样式 | 弹出后背景读回 `#FFE8564A`（Presenter 的逻辑父级是 `Popup`） |

## 本 plan 的范围

spec 第二阶段分 4 组，本 plan 只实现**第二组「样式绑定层」**：#4 Styling、#5 DataBinding、
#6 DataTemplates，含拆除旧 `Avalonia.DataTemplateDemo`。基础层三项目已交付，交互图形层
与应用服务层留待后续 plan。

## 功能点映射的实测修正

编写本 plan 时逐个核对了三个分类的官方侧边栏（2026-10-07 抓取），与 spec 的功能点列表
有以下出入，本 plan 按实际文档结构执行：

| 分类 | 官方子页数 | spec 的列法 | 本 plan 的处理 |
|---|---|---|---|
| Styling | 16 | 9 个 Tab | 保留 9 个 Tab。`styles` / `style-selectors` / `style-selector-syntax` / `property-setters` 四页讲的是同一件事的不同侧面，合成「选择器语法」；`themes` 与 `theme-variants` 合成「主题与变体」；`control-themes` 与 `control-template-walkthrough` 合成「ControlTheme」；`custom-fonts` 与 `typography` 合成「字体与排版」。`style-best-practices` 是建议清单，无可交互内容，不做 Tab |
| Styling | — | 「容器查询」完整实现 | **改为指路页**。`Avalonia.LayoutDemo` 的 `ResponsivePage` 已完整演示容器查询（含 `and` 缺陷与层叠写法），按 spec 去重规则不再重做 |
| Data Binding | 24 | 10 个 Tab | 12 个 Tab。spec 漏了「异步与图片」（`task-result` / `observable` / `image-files` 三页）；`markup-extensions` 子页按 spec 去重规则做成指路页指向 #2 |
| Data Binding | — | — | `binding-classes`（绑定样式类）完整实现放在 #4「样式类」页，#5 只在语法页留一行提示——它的知识重心是样式类而不是绑定 |
| Data Templates | 7 | 6 个 Tab | 9 个 Tab。spec 漏了 `control-content`、`creating-data-templates-in-code`、`reusing-data-templates`、`view-locator` 四页；spec 的「ItemsPanelTemplate」「TreeDataTemplate」不在本分类官方子页里，但属于模板体系，合成一个 Tab 保留 |

## File Structure

三个项目结构同构，均照搬 `Avalonia.LayoutDemo`。下表只列每个项目**独有**的文件；
`Program.cs`、`App.axaml(.cs)`、`app.manifest`、`Assets/avalonia-logo.ico`、
`Views/MainWindow.axaml(.cs)`、`.csproj` 六件套每个项目都有一份，内容除项目名外一致。

### 项目 #4 `Avalonia.StylingDemo`（9 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/SelectorsPage.axaml(.cs)` | 选择器语法全览：类型/名称/类/子代/后代/属性/`:not`/`:is`/`/template/`/`:nth-child`/嵌套 `^` | `styling/style-selector-syntax` |
| `Views/Pages/StyleClassesPage.axaml(.cs)` | 样式类的增删、多类组合、`Classes.xxx` 绑定 | `styling/style-classes` |
| `Views/Pages/PseudoClassesPage.axaml(.cs)` | 内置伪类与 `PseudoClasses.Set` 自定义伪类 | `styling/pseudoclasses` |
| `Views/Pages/ControlThemesPage.axaml(.cs)` | `ControlTheme` 的两种用法（重写模板 / `BasedOn` 继承） | `styling/control-themes` |
| `Views/Pages/ThemesPage.axaml(.cs)` | `ThemeVariant` 切换、`ThemeVariantScope`、`ThemeDictionaries` | `styling/theme-variants` |
| `Views/Pages/ContainerQueriesPage.axaml(.cs)` | 指路页 → `Avalonia.LayoutDemo` 响应式布局 | `styling/container-queries` |
| `Views/Pages/FontsPage.axaml(.cs)` | 嵌入字体文件、`FontFamily` 资源、排版属性 | `styling/custom-fonts` |
| `Views/Pages/SharingStylesPage.axaml(.cs)` | `StyleInclude` / `ResourceInclude` / 合并字典 | `styling/sharing-styles` |
| `Views/Pages/PrecedencePage.axaml(.cs)` | 指路页 → `Avalonia.PropertySystemDemo` 值优先级 | `properties/value-precedence` |
| `Controls/ToggleChip.cs` | 演示自定义伪类 `:on` 的最小控件 | — |
| `Styles/ButtonStyles.axaml` | 被 `StyleInclude` 引入的样式文件（迁移自旧 DataTemplateDemo） | — |
| `Styles/Palette.axaml` | 被 `ResourceInclude` 引入的资源字典 | — |
| `Assets/Fonts/iconfont.ttf` | 嵌入的图标字体（从 `Avalonia.HtmlRendererDemo` 复制） | — |

### 项目 #5 `Avalonia.DataBindingDemo`（12 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/SyntaxPage.axaml(.cs)` | 绑定语法、`DataContext` 继承、`#元素名`、`$parent`、绑定模式 | `data-binding/data-binding-syntax` |
| `Views/Pages/CompiledBindingsPage.axaml(.cs)` | `x:DataType`、`ReflectionBinding`、代码里 `Bind()` | `data-binding/compiled-bindings` |
| `Views/Pages/CollectionsPage.axaml(.cs)` | `ObservableCollection` 增删、绑定 `TabControl` | `data-binding/how-to-bind-to-a-collection` |
| `Views/Pages/MasterDetailPage.axaml(.cs)` | `SelectedItem` 驱动详情区 | `data-binding/master-detail` |
| `Views/Pages/MultiBindingPage.axaml(.cs)` | `MultiBinding` + `IMultiValueConverter` | `data-binding/multi-binding` |
| `Views/Pages/CommandsPage.axaml(.cs)` | `RelayCommand`、`CanExecute`、方法绑定、`CommandParameter` | `data-binding/binding-to-commands` |
| `Views/Pages/ConvertersPage.axaml(.cs)` | 内置转换器与自定义 `IValueConverter` | `data-binding/how-to-create-a-custom-data-binding-converter` |
| `Views/Pages/ValidationPage.axaml(.cs)` | DataAnnotations、`INotifyDataErrorInfo`、异常校验 | `data-binding/binding-validation` |
| `Views/Pages/CollectionViewsPage.axaml(.cs)` | ViewModel 侧排序、筛选、分组 | `data-binding/collection-views` |
| `Views/Pages/AsyncPage.axaml(.cs)` | `Task^`、`Observable^`、绑定图片 | `data-binding/how-to-bind-to-a-task-result` |
| `Views/Pages/DebuggingPage.axaml(.cs)` | `FallbackValue` / `TargetNullValue` / `?.` 与绑定日志 | `data-binding/binding-debugging` |
| `Views/Pages/MarkupExtensionsPage.axaml(.cs)` | 指路页 → `Avalonia.XamlDemo` 标记扩展 | `data-binding/markup-extensions` |
| `ViewModels/*.cs` | 与有状态的 Page 一一对应（见各 Task） | — |
| `Models/Contact.cs` | 主从、集合、筛选页共用的联系人模型 | — |
| `Converters/*.cs` | 自定义转换器（见 Task 3） | — |

### 项目 #6 `Avalonia.DataTemplatesDemo`（9 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/ControlContentPage.axaml(.cs)` | 非控件对象放进 `Content` 时的默认行为（`ToString`） | `data-templates/control-content` |
| `Views/Pages/ContentTemplatesPage.axaml(.cs)` | `ContentTemplate` / `ItemTemplate` 内联模板 | `data-templates/content-templates` |
| `Views/Pages/TemplateCollectionPage.axaml(.cs)` | `DataTemplates` 集合按 `DataType` 隐式匹配 | `data-templates/data-template-collection` |
| `Views/Pages/SelectorPage.axaml(.cs)` | `IDataTemplate` 选择器（迁移自旧 DataTemplateDemo） | `data-templates/data-template-collection` |
| `Views/Pages/CodeTemplatesPage.axaml(.cs)` | `FuncDataTemplate<T>` 纯代码建模板 | `data-templates/creating-data-templates-in-code` |
| `Views/Pages/ReusePage.axaml(.cs)` | 模板放进资源后按键复用 | `data-templates/reusing-data-templates` |
| `Views/Pages/ViewLocatorPage.axaml(.cs)` | 约定式 ViewLocator：`XxxViewModel` → `XxxView` | `data-templates/view-locator` |
| `Views/Pages/PanelsAndTreesPage.axaml(.cs)` | `ItemsPanelTemplate` 与 `TreeDataTemplate` | `data-templates/introduction-to-data-templates` |
| `Views/Pages/VersusControlTemplatePage.axaml(.cs)` | 同一份数据，DataTemplate 与 ControlTemplate 的分工对照 | `data-templates/introduction-to-data-templates` |
| `Models/Person.cs` | 选择器页的数据模型（迁移自旧项目，补 nullable 修正） | — |
| `Models/Shapes.cs` | 隐式匹配页的多态模型 | — |
| `Models/Folder.cs` | 树形模板页的递归模型 | — |
| `DataTemplates/PersonDataTemplateSelector.cs` | 迁移自旧项目 | — |
| `ViewLocator.cs` | 约定式视图定位器 | — |
| `ViewModels/*.cs` / `Views/Located/*.axaml(.cs)` | 供 ViewLocator 页解析的两组 VM 与 View | — |

### 共享文件的改动

| 文件 | 改动 |
|---|---|
| `hello-avalonia.slnx` | 注册三个新项目；移除 `Avalonia.DataTemplateDemo` |
| `Avalonia.DataTemplateDemo/` | **整个目录删除**（内容已迁入 #4 与 #6） |
| `README.md` | 项目表格加三行、删一行 |
| `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md` | 回写本组实测结论 |

**为什么旧项目在本组最后一个任务才删**：Task 2（Styling）与 Task 4（DataTemplates）
都要从它迁移文件。先删再迁意味着执行者要去 git 历史里翻原件；放到最后，迁移时原件就在
旁边，迁完一并核对、一并删除。

**为什么 `iconfont.ttf` 复制而不是跨项目引用**：`avares://` URI 只能指向本项目或被引用
程序集里的资源。让 StylingDemo 引用 HtmlRendererDemo 的程序集只为借一个字体，会把整个
HtmlRenderer 依赖拖进来。字体文件 1 份复制的代价远小于这个耦合。

---

## Task 1: 三个项目的骨架与 TabControl 外壳

**Files:**
- Create: `Avalonia.StylingDemo/` 下的 `Avalonia.StylingDemo.csproj`、`Program.cs`、`App.axaml`、`App.axaml.cs`、`app.manifest`、`Assets/avalonia-logo.ico`、`Views/MainWindow.axaml`、`Views/MainWindow.axaml.cs`
- Create: `Avalonia.DataBindingDemo/` 下同名八件套
- Create: `Avalonia.DataTemplatesDemo/` 下同名八件套
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: `avares://Avalonia.Shared/Themes/SharedStyles.axaml`（已存在，提供 `DemoHeader`、`TextBlock.caption`、`TextBlock.hint`、`Border.stage`）
- Produces: 三个可运行的空壳窗口。页面命名空间分别为 `Avalonia.StylingDemo.Views.Pages`、
  `Avalonia.DataBindingDemo.Views.Pages`、`Avalonia.DataTemplatesDemo.Views.Pages`，供 Task 2–4 挂页面。

注意项目名：新项目是 `Avalonia.DataTemplatesDemo`（复数，与官方分类名 Data Templates 一致），
旧项目是 `Avalonia.DataTemplateDemo`（单数）。两者在 Task 5 之前会共存，**不要把文件写进旧目录**。

- [ ] **Step 1: 用脚本批量生成三份骨架的目录与二进制文件**

在仓库根目录执行：

```bash
for p in StylingDemo DataBindingDemo DataTemplatesDemo; do
  mkdir -p "Avalonia.$p/Assets" "Avalonia.$p/Views/Pages"
  cp Avalonia.LayoutDemo/Assets/avalonia-logo.ico "Avalonia.$p/Assets/"
  sed "s/Avalonia\.LayoutDemo/Avalonia.$p/" Avalonia.LayoutDemo/app.manifest > "Avalonia.$p/app.manifest"
done
grep -l "Avalonia.LayoutDemo" Avalonia.StylingDemo/app.manifest Avalonia.DataBindingDemo/app.manifest Avalonia.DataTemplatesDemo/app.manifest || echo "manifest names rewritten"
```

Expected: 最后一行输出 `manifest names rewritten`。

- [ ] **Step 2: 创建三个 .csproj**

三份内容逐字相同（文件里没有项目名）。`Avalonia.StylingDemo/Avalonia.StylingDemo.csproj`：

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

复制为 `Avalonia.DataBindingDemo/Avalonia.DataBindingDemo.csproj` 与
`Avalonia.DataTemplatesDemo/Avalonia.DataTemplatesDemo.csproj`。

StylingDemo 的 `Styles/*.axaml` 也要被编译成 avares 资源。Avalonia SDK 默认把项目内所有
`*.axaml` 当作 `AvaloniaXaml` 编译，不需要额外 `ItemGroup`；`Assets\**` 那一行只管非 XAML
资源（图标、字体）。

- [ ] **Step 3: 创建三个 Program.cs**

`Avalonia.StylingDemo/Program.cs`：

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.StylingDemo
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

另两个项目只把 `namespace` 换成 `Avalonia.DataBindingDemo` / `Avalonia.DataTemplatesDemo`。

DataBindingDemo 的日志级别**不要**改成 `Verbose`：调试页（Task 3）会在页面里自己挂一个
只收 `LogArea.Binding` 的 sink，把错误显示在界面上；全局 Verbose 会让 Output 窗口被布局
日志淹没，反而看不到绑定错误。

- [ ] **Step 4: 创建三个 App.axaml 与 App.axaml.cs**

`Avalonia.StylingDemo/App.axaml`：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.StylingDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader plus the shared caption, hint and stage styles.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>
</Application>
```

`Avalonia.StylingDemo/App.axaml.cs`：

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.StylingDemo.Views;

namespace Avalonia.StylingDemo
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

另两个项目把 `x:Class`、`namespace`、`using` 里的 `Avalonia.StylingDemo` 换成对应项目名。

- [ ] **Step 5: 创建三个 MainWindow**

`Avalonia.StylingDemo/Views/MainWindow.axaml`：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.StylingDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Styling Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <TabControl Margin="12">
    </TabControl>
</Window>
```

`Avalonia.StylingDemo/Views/MainWindow.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.StylingDemo.Views
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

另两个项目的 `Title` 分别为 `Avalonia Data Binding Demo` 和 `Avalonia Data Templates Demo`，
`x:Class` 与 `namespace` 随项目名变化。

DataBindingDemo 有 12 个 Tab，900 宽放不下一行标题。它的 `TabControl` 改为竖排：

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
    </TabControl>
```

这是唯一一处偏离样板的地方。横排 12 个 Tab 会出现横向滚动，spec 明确把这当作要避免的
体验问题（「功能点粒度」一节）。

- [ ] **Step 6: 注册到解决方案**

修改 `hello-avalonia.slnx`，按字母序插入三行（旧 `Avalonia.DataTemplateDemo` 暂时保留，
Task 5 再删）：

```xml
<Solution>
  <Project Path="Avalonia.DataBindingDemo/Avalonia.DataBindingDemo.csproj" />
  <Project Path="Avalonia.DataTemplateDemo/Avalonia.DataTemplateDemo.csproj" />
  <Project Path="Avalonia.DataTemplatesDemo/Avalonia.DataTemplatesDemo.csproj" />
  <Project Path="Avalonia.FundamentalsDemo/Avalonia.FundamentalsDemo.csproj" />
  <Project Path="Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj" />
  <Project Path="Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj" />
  <Project Path="Avalonia.MusicStore/Avalonia.MusicStore.csproj" />
  <Project Path="Avalonia.PropertySystemDemo/Avalonia.PropertySystemDemo.csproj" />
  <Project Path="Avalonia.Shared/Avalonia.Shared.csproj" />
  <Project Path="Avalonia.StylingDemo/Avalonia.StylingDemo.csproj" />
  <Project Path="Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj" />
  <Project Path="Avalonia.XamlDemo/Avalonia.XamlDemo.csproj" />
</Solution>
```

- [ ] **Step 7: 构建验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误"`
Expected: `0 个错误`。

不要用警告总数做判据：仓库里有一批既有警告（每个项目一条 MSB3884，来自
`Directory.Build.props` 引用的不存在的 `MinimumRecommendedRules.ruleset`；另有 MusicStore 的
NU1701/CS0618、若干 CS8604/CS8618、HtmlRendererDemo 的 MSB3245），新增项目必然让 MSB3884
条数增加。判据是**新项目没有引入 MSB3884 以外的警告**：

```bash
dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "StylingDemo|DataBindingDemo|DataTemplatesDemo" | grep -v MSB3884
```

Expected: 无输出。后续每个任务的「构建」步骤都用这条命令。

- [ ] **Step 8: 提交**

```bash
git add Avalonia.StylingDemo/ Avalonia.DataBindingDemo/ Avalonia.DataTemplatesDemo/ hello-avalonia.slnx
git commit -m "$(cat <<'EOF'
feat: scaffold the three styling-and-binding demo projects

Empty TabControl shells wired to the shared styles, following the
LayoutDemo template. DataBindingDemo stacks its tabs vertically because
twelve headers do not fit across a 900px window.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: 项目 #4 Styling 的 9 个页面

**Files:**
- Create: `Avalonia.StylingDemo/Controls/ToggleChip.cs`
- Create: `Avalonia.StylingDemo/Controls/ToggleChip.axaml`
- Create: `Avalonia.StylingDemo/Styles/ButtonStyles.axaml`（迁移自 `Avalonia.DataTemplateDemo/Resources/ButtonStyles.axaml`）
- Create: `Avalonia.StylingDemo/Styles/Palette.axaml`
- Create: `Avalonia.StylingDemo/Assets/Fonts/iconfont.ttf`（复制自 `Avalonia.HtmlRendererDemo/Assets/iconfont.ttf`）
- Create: `Avalonia.StylingDemo/Views/Pages/` 下 9 组 `XxxPage.axaml(.cs)`：`SelectorsPage`、`StyleClassesPage`、`PseudoClassesPage`、`ControlThemesPage`、`ThemesPage`、`ContainerQueriesPage`、`FontsPage`、`SharingStylesPage`、`PrecedencePage`
- Modify: `Avalonia.StylingDemo/App.axaml`（引入 `ToggleChip` 的 ControlTheme）
- Modify: `Avalonia.StylingDemo/Views/MainWindow.axaml`（挂 9 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳；`Avalonia.Shared` 的 `DemoHeader` 与 `caption`/`hint`/`stage` 样式
- Produces: 无跨任务产物。`ToggleChip` 只在本项目使用。

本项目所有页面都是**无状态**的（交互状态全在控件上，或用 `#元素名` 绑定互相驱动），
因此没有 ViewModel 目录。这与 spec 「ViewModels/ 与 Pages 一一对应（无状态页可省略）」一致。

- [ ] **Step 1: 复制字体并迁移按钮样式**

```bash
mkdir -p Avalonia.StylingDemo/Assets/Fonts Avalonia.StylingDemo/Styles Avalonia.StylingDemo/Controls
cp Avalonia.HtmlRendererDemo/Assets/iconfont.ttf Avalonia.StylingDemo/Assets/Fonts/
```

`iconfont.ttf` 的家族名是 `iconfont`，码位从 `U+E601` 起（已用 fontTools 读过 cmap：
`E601 moveDown`、`E603 rotate`、`E604 checkIn`、`E60C duigou`、`E610 play`…）。

创建 `Avalonia.StylingDemo/Styles/ButtonStyles.axaml`。Setter 原样迁移自旧项目，只改两处：
类名改为小写，并补一段说明注释：

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!--  Pulled in by StyleInclude on SharingStylesPage. Nothing else references this file,
          so a button carrying these classes anywhere else in the app stays unstyled.  -->
    <Style Selector="Button.call">
        <Setter Property="Width" Value="120" />
        <Setter Property="Height" Value="40" />
        <Setter Property="HorizontalContentAlignment" Value="Center" />
        <Setter Property="VerticalContentAlignment" Value="Center" />
    </Style>
    <Style Selector="Button.icon">
        <Setter Property="Width" Value="50" />
        <Setter Property="Height" Value="40" />
        <Setter Property="TextBlock.TextAlignment" Value="Center" />
    </Style>
</Styles>
```

类名改为小写 `call` / `icon`，与仓库其余样式类（`caption`、`stage`、`hint`）的命名一致。

- [ ] **Step 2: 创建资源字典 Palette.axaml**

`Avalonia.StylingDemo/Styles/Palette.axaml`：

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!--  A resource dictionary carries values, not rules: merging it changes nothing on screen
          until something looks a key up. Contrast with ButtonStyles.axaml, which restyles
          matching controls the moment it is included.  -->
    <SolidColorBrush x:Key="PaletteAccent" Color="#E8974A" />
    <SolidColorBrush x:Key="PaletteMuted" Color="#556070" />
    <CornerRadius x:Key="PaletteRadius">8</CornerRadius>
</ResourceDictionary>
```

- [ ] **Step 3: 创建自定义伪类控件 ToggleChip**

`Avalonia.StylingDemo/Controls/ToggleChip.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avalonia.StylingDemo.Controls
{
    /// <summary>
    /// A button that latches. Its only purpose is to own a custom pseudo-class,
    /// <c>:on</c>, so the styling page can show that pseudo-classes are not a
    /// fixed list but something any control can publish.
    /// </summary>
    [PseudoClasses(":on")]
    public class ToggleChip : Button
    {
        public static readonly StyledProperty<bool> IsOnProperty =
            AvaloniaProperty.Register<ToggleChip, bool>(nameof(IsOn));

        public bool IsOn
        {
            get => GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        protected override void OnClick()
        {
            base.OnClick();
            IsOn = !IsOn;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // The attribute above only documents the pseudo-class for tooling;
            // this call is what actually turns it on and off.
            if (change.Property == IsOnProperty)
            {
                PseudoClasses.Set(":on", IsOn);
            }
        }
    }
}
```

`Avalonia.StylingDemo/Controls/ToggleChip.axaml`：

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:controls="using:Avalonia.StylingDemo.Controls">

    <!--
        Borrow the Fluent Button look through BasedOn rather than overriding StyleKeyOverride.
        StyleKeyOverride would make the style system treat every ToggleChip as a plain Button,
        so a "controls|ToggleChip:on" selector would silently never match.
    -->
    <ControlTheme x:Key="{x:Type controls:ToggleChip}"
                  TargetType="controls:ToggleChip"
                  BasedOn="{StaticResource {x:Type Button}}" />
</ResourceDictionary>
```

修改 `Avalonia.StylingDemo/App.axaml`，在 `</Application.Styles>` 之后加：

```xml
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://Avalonia.StylingDemo/Controls/ToggleChip.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
```

这与 `Avalonia.PropertySystemDemo/App.axaml` 引入 `GaugeControl` 主题的写法一致。

- [ ] **Step 4: 创建 SelectorsPage**

`Avalonia.StylingDemo/Views/Pages/SelectorsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.SelectorsPage">

    <UserControl.Styles>
        <!--  Every rule below paints one property, so each row shows exactly which elements it caught.  -->
        <Style Selector="Border.cell">
            <Setter Property="Background" Value="#30FFFFFF" />
            <Setter Property="Width" Value="56" />
            <Setter Property="Height" Value="28" />
            <Setter Property="Margin" Value="3" />
            <Setter Property="CornerRadius" Value="3" />
        </Style>

        <!--  1. Child (>) versus descendant (space).  -->
        <Style Selector="StackPanel#ChildRow > Border.cell">
            <Setter Property="Background" Value="#E8564A" />
        </Style>
        <Style Selector="StackPanel#DescendantRow Border.cell">
            <Setter Property="Background" Value="#4AC7E8" />
        </Style>

        <!--  2. A type selector matches the exact type only; :is() also takes subclasses.  -->
        <Style Selector="WrapPanel#ExactRow Button">
            <Setter Property="Background" Value="#E8564A" />
        </Style>
        <Style Selector="WrapPanel#IsRow :is(Button)">
            <Setter Property="Background" Value="#6FE84A" />
        </Style>

        <!--  3. Attribute selector on a live property value.  -->
        <Style Selector="CheckBox[IsChecked=True]">
            <Setter Property="Foreground" Value="#6FE84A" />
        </Style>

        <!--  4. :not() and :nth-child().  -->
        <Style Selector="StackPanel#NotRow > Border.cell:not(.keep)">
            <Setter Property="Opacity" Value="0.25" />
        </Style>
        <Style Selector="StackPanel#NthRow > Border.cell:nth-child(2n)">
            <Setter Property="Background" Value="#E8D24A" />
        </Style>

        <!--  5. /template/ reaches inside a control's template. Without it, PART_ContentPresenter
              is invisible to the selector: it lives in the visual tree, not the logical one.  -->
        <Style Selector="Button.round /template/ ContentPresenter#PART_ContentPresenter">
            <Setter Property="CornerRadius" Value="16" />
        </Style>

        <!--  6. Nesting: ^ stands for the enclosing selector.  -->
        <Style Selector="Button.nested">
            <Setter Property="Width" Value="160" />
            <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
                <Setter Property="Background" Value="#4A7BE8" />
            </Style>
        </Style>

        <!--  Migrated from the old DataTemplateDemo. The flyout opens in a separate popup root,
              yet these styles still reach it: a flyout's presenter is a logical child of its owner.  -->
        <Style Selector="FlyoutPresenter">
            <Setter Property="Background" Value="#E8564A" />
            <Setter Property="Margin" Value="0,5,0,0" />
            <Style Selector="^:pointerover">
                <Setter Property="Background" Value="#4A7BE8" />
            </Style>
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="选择器语法：每一行只有一条规则在起作用"
                               DocPath="styling/style-selector-syntax" />

            <TextBlock Classes="caption" Text="1. 子代 &gt; 与后代（空格）：中间隔了一层 Border 的那两个，只被后代选择器选中" />
            <StackPanel Name="ChildRow" Orientation="Horizontal">
                <Border Classes="cell" />
                <Border Classes="cell" />
                <Border Padding="4" BorderBrush="#40FFFFFF" BorderThickness="1">
                    <Border Classes="cell" />
                </Border>
            </StackPanel>
            <StackPanel Name="DescendantRow" Orientation="Horizontal">
                <Border Classes="cell" />
                <Border Classes="cell" />
                <Border Padding="4" BorderBrush="#40FFFFFF" BorderThickness="1">
                    <Border Classes="cell" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint" Text="上行用 StackPanel#ChildRow &gt; Border.cell（红），下行用 StackPanel#DescendantRow Border.cell（蓝）。" />

            <TextBlock Classes="caption" Text="2. 类型选择器不匹配子类 —— 这一条与 CSS 的直觉相反" />
            <WrapPanel Name="ExactRow">
                <Button Margin="3" Content="Button" />
                <ToggleButton Margin="3" Content="ToggleButton" />
                <RepeatButton Margin="3" Content="RepeatButton" />
            </WrapPanel>
            <WrapPanel Name="IsRow">
                <Button Margin="3" Content="Button" />
                <ToggleButton Margin="3" Content="ToggleButton" />
                <RepeatButton Margin="3" Content="RepeatButton" />
            </WrapPanel>
            <TextBlock Classes="hint" Text="上行规则写 Button，只染红了第一个；下行写 :is(Button)，三个都染绿——ToggleButton 和 RepeatButton 都派生自 Button。" />

            <TextBlock Classes="caption" Text="3. 属性选择器 [Property=Value]：勾选时文字变绿" />
            <CheckBox Content="点我切换 IsChecked" />

            <TextBlock Classes="caption" Text="4. :not() 与 :nth-child()" />
            <StackPanel Name="NotRow" Orientation="Horizontal">
                <Border Classes="cell keep" />
                <Border Classes="cell" />
                <Border Classes="cell keep" />
                <Border Classes="cell" />
            </StackPanel>
            <StackPanel Name="NthRow" Orientation="Horizontal">
                <Border Classes="cell" />
                <Border Classes="cell" />
                <Border Classes="cell" />
                <Border Classes="cell" />
            </StackPanel>
            <TextBlock Classes="hint" Text="上行：没有 .keep 类的变淡。下行：偶数位（第 2、4 个）变黄，计数从 1 开始。" />

            <TextBlock Classes="caption" Text="5. /template/ 改写控件模板内部的元素" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="普通按钮" />
                <Button Classes="round" Content="模板里的 ContentPresenter 被改成了圆角" />
            </StackPanel>

            <TextBlock Classes="caption" Text="6. 嵌套样式：^ 代表外层选择器（悬停看效果）" />
            <Button Classes="nested" Content="悬停我" />

            <TextBlock Classes="caption" Text="7. 样式也能管到弹出层：FlyoutPresenter 红底，悬停变蓝" />
            <Button Name="FlyoutOwner" HorizontalAlignment="Left" Content="打开 Flyout">
                <Button.Flyout>
                    <Flyout ShowMode="Standard" Placement="BottomEdgeAlignedLeft">
                        <TextBlock Text="这是弹出层" />
                    </Flyout>
                </Button.Flyout>
            </Button>
            <TextBlock Classes="hint" Text="弹出层是另一个顶层窗口，视觉树和页面断开；但它在逻辑树上挂在按钮下面，所以写在本页 UserControl.Styles 里的规则照样生效。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.StylingDemo/Views/Pages/SelectorsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.StylingDemo.Views.Pages
{
    public partial class SelectorsPage : UserControl
    {
        public SelectorsPage()
        {
            InitializeComponent();
        }
    }
}
```

后续每个无状态页面的 `.axaml.cs` 都是这个形状，只换类名——下文不再重复列出，
**但每个都必须创建**，否则 `x:Class` 找不到 partial 类会构建失败。

- [ ] **Step 5: 创建 StyleClassesPage**

`Avalonia.StylingDemo/Views/Pages/StyleClassesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.StyleClassesPage">

    <UserControl.Styles>
        <Style Selector="Border.swatch">
            <Setter Property="Width" Value="140" />
            <Setter Property="Height" Value="36" />
            <Setter Property="CornerRadius" Value="4" />
            <Setter Property="Background" Value="#30FFFFFF" />
        </Style>
        <Style Selector="Border.green">
            <Setter Property="Background" Value="#6FE84A" />
        </Style>
        <!--  Declared after .green, so it wins whenever both classes are present —
              regardless of the order they appear in the Classes attribute.  -->
        <Style Selector="Border.yellow">
            <Setter Property="Background" Value="#E8D24A" />
        </Style>
        <Style Selector="Border.warn">
            <Setter Property="BorderBrush" Value="#E8564A" />
            <Setter Property="BorderThickness" Value="3" />
        </Style>
        <Style Selector="Border.swatch.green.warn">
            <!--  A compound class selector: needs all three at once.  -->
            <Setter Property="Opacity" Value="0.5" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="样式类：用名字给元素打标签，样式按标签挑元素"
                               DocPath="styling/style-classes" />

            <TextBlock Classes="caption" Text="1. Classes 里写多个类，用空格分隔" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Border Classes="swatch" />
                <Border Classes="swatch green" />
                <Border Classes="swatch green warn" />
            </StackPanel>
            <TextBlock Classes="hint" Text="第三个同时有 swatch、green、warn，因此额外命中复合选择器 Border.swatch.green.warn（半透明）。" />

            <TextBlock Classes="caption" Text="2. 冲突时比的是样式声明顺序，不是 Classes 里的书写顺序" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Border Classes="swatch green yellow">
                    <TextBlock Margin="8,0" VerticalAlignment="Center" Foreground="Black" Text='"green yellow"' />
                </Border>
                <Border Classes="swatch yellow green">
                    <TextBlock Margin="8,0" VerticalAlignment="Center" Foreground="Black" Text='"yellow green"' />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint" Text="两块都是黄色：Border.yellow 在样式表里声明在 Border.green 之后。" />

            <TextBlock Classes="caption" Text="3. Classes.类名 绑定到布尔值：开关驱动类名增删" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ToggleSwitch Name="WarnSwitch" OffContent="正常" OnContent="警告" />
                <Border Name="BoundSwatch" Classes="swatch" Classes.warn="{Binding #WarnSwitch.IsChecked}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="对应官方 data-binding/binding-classes 一页。IsChecked 是 bool?，绑定照样生效。" />

            <TextBlock Classes="caption" Text="4. 在代码里增删类" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="切换 green" Click="OnToggleGreen" />
                <Border Name="CodeSwatch" Classes="swatch" />
                <TextBlock Name="CodeClassesText" VerticalAlignment="Center" />
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.StylingDemo/Views/Pages/StyleClassesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.StylingDemo.Views.Pages
{
    public partial class StyleClassesPage : UserControl
    {
        public StyleClassesPage()
        {
            InitializeComponent();
            ShowClasses();
        }

        private void OnToggleGreen(object? sender, RoutedEventArgs e)
        {
            // Classes.Set adds or removes in one call; Add/Remove are the long-hand form.
            CodeSwatch.Classes.Set("green", !CodeSwatch.Classes.Contains("green"));
            ShowClasses();
        }

        private void ShowClasses()
        {
            CodeClassesText.Text = $"当前 Classes：{string.Join(" ", CodeSwatch.Classes)}";
        }
    }
}
```

- [ ] **Step 6: 创建 PseudoClassesPage**

`Avalonia.StylingDemo/Views/Pages/PseudoClassesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.StylingDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.PseudoClassesPage">

    <UserControl.Styles>
        <Style Selector="Border.target">
            <Setter Property="Width" Value="200" />
            <Setter Property="Height" Value="40" />
            <Setter Property="CornerRadius" Value="4" />
            <Setter Property="Background" Value="#30FFFFFF" />
        </Style>
        <Style Selector="Border.target:pointerover">
            <Setter Property="Background" Value="#4A7BE8" />
        </Style>
        <Style Selector="TextBox.watch:focus">
            <Setter Property="BorderBrush" Value="#6FE84A" />
        </Style>
        <Style Selector="Button.watch:disabled">
            <Setter Property="Opacity" Value="0.3" />
        </Style>
        <Style Selector="controls|ToggleChip:on">
            <Setter Property="Background" Value="#E8974A" />
            <Setter Property="Foreground" Value="Black" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="伪类：控件自己报告的状态，样式按状态切换"
                               DocPath="styling/pseudoclasses" />

            <TextBlock Classes="caption" Text="1. 内置伪类：:pointerover / :focus / :disabled" />
            <StackPanel Spacing="8">
                <Border Classes="target">
                    <TextBlock Margin="8,0" VerticalAlignment="Center" Text="鼠标移上来" />
                </Border>
                <TextBox Classes="watch" Width="200" HorizontalAlignment="Left" Watermark="点进来获得焦点" />
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <CheckBox Name="EnableBox" IsChecked="True" Content="启用右边的按钮" />
                    <Button Classes="watch" Content="被控制的按钮" IsEnabled="{Binding #EnableBox.IsChecked}" />
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint" Text="伪类不能在 XAML 里手动设置，只能由控件自己根据状态打上。Classes 里看得到它们（带冒号前缀）。" />

            <TextBlock Classes="caption" Text="2. 自定义伪类 :on —— ToggleChip 点一下就锁定" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <controls:ToggleChip Name="Chip" Content="点我切换 :on" />
                <TextBlock VerticalAlignment="Center"
                           Text="{Binding #Chip.IsOn, StringFormat='IsOn = {0}'}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="ToggleChip 在 IsOn 变化时调用 PseudoClasses.Set(&quot;:on&quot;, IsOn)。样式 controls|ToggleChip:on 就是按这个状态挑元素的。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`PseudoClassesPage.axaml.cs`：无状态页标准形状（见 Step 4）。

- [ ] **Step 7: 创建 ControlThemesPage**

旧 `DataTemplateDemo` 里的 `ButtonStyle` ControlTheme 迁移到本页的第 1 节，`x:Key` 改为
`YellowButtonTheme`（旧名叫 Style 却是 ControlTheme，正是本页要澄清的混淆）。

`Avalonia.StylingDemo/Views/Pages/ControlThemesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.ControlThemesPage">

    <UserControl.Resources>
        <!--  Replaces the whole template: none of Fluent's visuals survive.  -->
        <ControlTheme x:Key="YellowButtonTheme" TargetType="Button">
            <Setter Property="Width" Value="200" />
            <Setter Property="Height" Value="40" />
            <Setter Property="Background" Value="Yellow" />
            <Setter Property="Foreground" Value="Black" />
            <Setter Property="BorderBrush" Value="Blue" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="Template">
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="4">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"
                                          Content="{TemplateBinding Content}"
                                          Foreground="{TemplateBinding Foreground}" />
                    </Border>
                </ControlTemplate>
            </Setter>
            <!--  Pseudo-class styles live inside the theme and use ^ for the themed control.  -->
            <Style Selector="^:pointerover">
                <Setter Property="Background" Value="#4A7BE8" />
            </Style>
        </ControlTheme>

        <!--  Keeps Fluent's template and adds setters on top.  -->
        <ControlTheme x:Key="WideButtonTheme"
                      TargetType="Button"
                      BasedOn="{StaticResource {x:Type Button}}">
            <Setter Property="Width" Value="200" />
        </ControlTheme>

        <!--  Inherits a theme that already inherits Fluent: setters stack up both levels.  -->
        <ControlTheme x:Key="TallWideButtonTheme"
                      TargetType="Button"
                      BasedOn="{StaticResource WideButtonTheme}">
            <Setter Property="Height" Value="56" />
        </ControlTheme>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ControlTheme：控件的整套外观，一个控件同时只用一个"
                               DocPath="styling/control-themes" />

            <TextBlock Classes="caption" Text="1. 重写模板：Fluent 的外观全部丢弃" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="Fluent 默认" />
                <Button Name="YellowButton" Theme="{StaticResource YellowButtonTheme}" Content="自定义模板" />
            </StackPanel>
            <TextBlock Classes="hint" Text="悬停时变蓝的规则写在 ControlTheme 内部的 ^:pointerover 里——主题自带的状态样式随主题一起换掉。" />

            <TextBlock Classes="caption" Text="2. BasedOn：保留 Fluent 模板，只追加 Setter" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="WideButton" Theme="{StaticResource WideButtonTheme}" Content="继承一层：Width=200" />
                <Button Name="TallWideButton" Theme="{StaticResource TallWideButtonTheme}" Content="继承两层：再加 Height=56" />
            </StackPanel>
            <TextBlock Classes="hint" Text="{}{StaticResource {x:Type Button}} 取到的就是 Fluent 给 Button 的默认 ControlTheme。" />

            <TextBlock Classes="caption" Text="3. ControlTheme 与 Style 的分工" />
            <Border Classes="stage" Padding="10">
                <TextBlock TextWrapping="Wrap"
                           Text="Style 按选择器批量挑元素，可以层层叠加；ControlTheme 通过 Theme 属性指定给单个控件，一次只生效一个，并且优先级低于所有 Style——所以用 Style 去覆盖主题里的 Setter 总能成功，反过来不行。WPF 的 Style 在 Avalonia 里被拆成了这两样东西。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`ControlThemesPage.axaml.cs`：无状态页标准形状。

迁移时去掉了旧版模板里 `Border.Styles` 中 `Border:pointerover` 那条规则：它把悬停样式写在
模板内部的 Border 上，与 `^:pointerover` 写法表达的是同一件事，留两份会让读者以为二者有别。

- [ ] **Step 8: 创建 ThemesPage**

`Avalonia.StylingDemo/Views/Pages/ThemesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.ThemesPage">

    <UserControl.Resources>
        <ResourceDictionary>
            <!--  One key, two values: the theme variant in effect decides which one a lookup gets.  -->
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                    <SolidColorBrush x:Key="CardBrush" Color="#FFEEEEEE" />
                    <SolidColorBrush x:Key="CardText" Color="#FF202020" />
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                    <SolidColorBrush x:Key="CardBrush" Color="#FF222222" />
                    <SolidColorBrush x:Key="CardText" Color="#FFEEEEEE" />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <UserControl.Styles>
        <Style Selector="Border.card">
            <Setter Property="Width" Value="220" />
            <Setter Property="Height" Value="60" />
            <Setter Property="CornerRadius" Value="6" />
            <Setter Property="Padding" Value="10" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="主题与变体：Light / Dark 切换，以及局部换肤"
                               DocPath="styling/theme-variants" />

            <TextBlock Classes="caption" Text="1. 切换整个应用的主题变体" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="Light" Click="OnLight" />
                <Button Content="Dark" Click="OnDark" />
                <Button Content="跟随系统（Default）" Click="OnDefault" />
                <TextBlock Name="ActualText" VerticalAlignment="Center" />
            </StackPanel>
            <TextBlock Classes="hint" Text="改的是 Application.RequestedThemeVariant。Fluent 的所有控件都通过 DynamicResource 取色，因此整窗即时换肤。" />

            <TextBlock Classes="caption" Text="2. ThemeVariantScope：只给一块区域换主题" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ThemeVariantScope RequestedThemeVariant="Light">
                    <Border Classes="card" Background="{DynamicResource CardBrush}">
                        <StackPanel Spacing="4">
                            <TextBlock Foreground="{DynamicResource CardText}" Text="这块永远是 Light" />
                            <Button Content="Fluent 按钮也跟着变" />
                        </StackPanel>
                    </Border>
                </ThemeVariantScope>
                <ThemeVariantScope RequestedThemeVariant="Dark">
                    <Border Classes="card" Background="{DynamicResource CardBrush}">
                        <StackPanel Spacing="4">
                            <TextBlock Foreground="{DynamicResource CardText}" Text="这块永远是 Dark" />
                            <Button Content="Fluent 按钮也跟着变" />
                        </StackPanel>
                    </Border>
                </ThemeVariantScope>
            </StackPanel>

            <TextBlock Classes="caption" Text="3. ThemeDictionaries 必须配 DynamicResource" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Name="DynamicCard" Classes="card" Background="{DynamicResource CardBrush}">
                    <TextBlock Foreground="{DynamicResource CardText}" Text="DynamicResource：随主题换色" />
                </Border>
                <Border Name="StaticCard" Classes="card" BorderBrush="#40FFFFFF" BorderThickness="1"
                        Background="{StaticResource CardBrush}">
                    <TextBlock Text="StaticResource：背景是 null" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint" Text="右边那块没有背景色：StaticResource 在加载时查找一次，而 ThemeDictionaries 里的键要等知道当前变体后才能解析，于是取到 null——不报错，也没有日志。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.StylingDemo/Views/Pages/ThemesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.StylingDemo.Views.Pages
{
    public partial class ThemesPage : UserControl
    {
        public ThemesPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ShowActual();
        }

        private void OnLight(object? sender, RoutedEventArgs e) => Apply(ThemeVariant.Light);

        private void OnDark(object? sender, RoutedEventArgs e) => Apply(ThemeVariant.Dark);

        // Default means "no preference": the platform's setting decides.
        private void OnDefault(object? sender, RoutedEventArgs e) => Apply(ThemeVariant.Default);

        private void Apply(ThemeVariant variant)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = variant;
            }

            ShowActual();
        }

        private void ShowActual()
        {
            var app = Application.Current;
            ActualText.Text = $"请求：{app?.RequestedThemeVariant}　实际：{app?.ActualThemeVariant}";
        }
    }
}
```

- [ ] **Step 9: 创建 ContainerQueriesPage（指路页）**

按 spec「分类之间的去重规则」，容器查询已在 `Avalonia.LayoutDemo` 完整实现，这里只做
指路。`Avalonia.StylingDemo/Views/Pages/ContainerQueriesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.ContainerQueriesPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="容器查询：完整演示在 Avalonia.LayoutDemo"
                           DocPath="styling/container-queries" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="容器查询让样式随某个祖先控件的尺寸生效，官方文档把它列在 Styling 分类下，但它最常见的用途是响应式布局，因此完整演示放在布局项目里。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.LayoutDemo   →  「响应式布局」标签页第 1 节" />
                <TextBlock Classes="hint"
                           Text="那一节还记录了 Avalonia 12.1.2 的两个静默陷阱：目标属性上写本地值会永久压制查询；min-width:a and max-width:b 只有右侧条件生效，区间要用单条件层叠表达。" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

`ContainerQueriesPage.axaml.cs`：无状态页标准形状。

- [ ] **Step 10: 创建 FontsPage**

`Avalonia.StylingDemo/Views/Pages/FontsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.FontsPage">

    <UserControl.Resources>
        <!--  "<assembly>/<folder>#<family name>". The family name comes from the font file's
              own name table, not from the file name — here they happen to coincide.  -->
        <FontFamily x:Key="IconFont">avares://Avalonia.StylingDemo/Assets/Fonts#iconfont</FontFamily>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="字体与排版：嵌入字体文件，调整字距行高"
                               DocPath="styling/custom-fonts" />

            <TextBlock Classes="caption" Text="1. 嵌入的图标字体：每个字符是一个图标" />
            <StackPanel Orientation="Horizontal" Spacing="16">
                <TextBlock Name="IconSample" FontFamily="{StaticResource IconFont}" FontSize="32" Text="&#xe603;" />
                <TextBlock FontFamily="{StaticResource IconFont}" FontSize="32" Text="&#xe604;" />
                <TextBlock FontFamily="{StaticResource IconFont}" FontSize="32" Text="&#xe60c;" />
                <TextBlock FontFamily="{StaticResource IconFont}" FontSize="32" Text="&#xe610;" />
            </StackPanel>
            <TextBlock Classes="hint" Text="字体文件放在 Assets/Fonts/ 下，由 csproj 里的 AvaloniaResource Include=&quot;Assets\**&quot; 打进程序集。码位 U+E603、E604、E60C、E610。" />

            <TextBlock Classes="caption" Text="2. 字体回退链：前一个找不到就用下一个" />
            <TextBlock FontFamily="不存在的字体, Consolas, monospace" Text="FontFamily=&quot;不存在的字体, Consolas, monospace&quot;" />

            <TextBlock Classes="caption" Text="3. 排版属性（拖动滑块）" />
            <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto" Margin="0,0,0,6">
                <TextBlock Grid.Row="0" Grid.Column="0" VerticalAlignment="Center" Margin="0,0,8,0"
                           Text="{Binding #SpacingSlider.Value, StringFormat='LetterSpacing {0:F1}'}" />
                <Slider Grid.Row="0" Grid.Column="1" Name="SpacingSlider" Minimum="0" Maximum="10" Value="0" />
                <TextBlock Grid.Row="1" Grid.Column="0" VerticalAlignment="Center" Margin="0,0,8,0"
                           Text="{Binding #LineSlider.Value, StringFormat='LineHeight {0:F0}'}" />
                <Slider Grid.Row="1" Grid.Column="1" Name="LineSlider" Minimum="16" Maximum="48" Value="20" />
            </Grid>
            <Border Classes="stage" Padding="10">
                <TextBlock FontSize="16"
                           LetterSpacing="{Binding #SpacingSlider.Value}"
                           LineHeight="{Binding #LineSlider.Value}"
                           TextWrapping="Wrap"
                           Text="Avalonia 的 TextBlock 支持字距、行高、下划线、删除线与 OpenType 特性。The quick brown fox jumps over the lazy dog." />
            </Border>
            <StackPanel Orientation="Horizontal" Spacing="16" Margin="0,8,0,0">
                <TextBlock TextDecorations="Underline" Text="Underline" />
                <TextBlock TextDecorations="Strikethrough" Text="Strikethrough" />
                <TextBlock FontStyle="Italic" Text="Italic" />
                <TextBlock FontWeight="Black" Text="Black" />
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`FontsPage.axaml.cs`：无状态页标准形状。

`LetterSpacing` 与 `LineHeight` 都是 `double`，直接绑 `Slider.Value` 不需要转换器——与
硬性规则 1 的 `Thickness` 情形不同，这里没有结构体。

- [ ] **Step 11: 创建 SharingStylesPage**

`Avalonia.StylingDemo/Views/Pages/SharingStylesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.SharingStylesPage">

    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!--  ResourceInclude merges values; nothing changes until a key is looked up.  -->
                <ResourceInclude Source="avares://Avalonia.StylingDemo/Styles/Palette.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <UserControl.Styles>
        <!--  StyleInclude brings in rules: matching buttons on this page restyle immediately.  -->
        <StyleInclude Source="avares://Avalonia.StylingDemo/Styles/ButtonStyles.axaml" />
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="样式共享：StyleInclude 带进规则，ResourceInclude 带进值"
                               DocPath="styling/sharing-styles" />

            <TextBlock Classes="caption" Text="1. StyleInclude：引入 Styles/ButtonStyles.axaml" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="CallButton" Classes="call" Content="call" />
                <Button Name="IconButton" Classes="icon" Content="i" />
                <Button Content="无类名" />
            </StackPanel>
            <TextBlock Classes="hint" Text="样式文件的作用域取决于它被 Include 到哪里：这里放在 UserControl.Styles，别的页面上带 call 类的按钮不受影响。要全局生效就放进 App.axaml 的 Application.Styles。" />

            <TextBlock Classes="caption" Text="2. ResourceInclude：引入 Styles/Palette.axaml 里的画刷与圆角" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Border Name="AccentSwatch" Width="120" Height="40"
                        Background="{StaticResource PaletteAccent}"
                        CornerRadius="{StaticResource PaletteRadius}" />
                <Border Width="120" Height="40"
                        Background="{StaticResource PaletteMuted}"
                        CornerRadius="{StaticResource PaletteRadius}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. 本仓库就是一个跨程序集共享的例子" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock TextWrapping="Wrap"
                               Text="页面顶部的说明条、小节标题（caption）、提示文字（hint）、演示边框（stage）都来自 Avalonia.Shared 程序集，每个演示项目在 App.axaml 里用一行 StyleInclude 引入：" />
                    <SelectableTextBlock FontFamily="Consolas, monospace"
                                         Text="&lt;StyleInclude Source=&quot;avares://Avalonia.Shared/Themes/SharedStyles.axaml&quot; /&gt;" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`SharingStylesPage.axaml.cs`：无状态页标准形状。

- [ ] **Step 12: 创建 PrecedencePage（指路页）**

`Avalonia.StylingDemo/Views/Pages/PrecedencePage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.StylingDemo.Views.Pages.PrecedencePage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="属性值优先级：完整演示在 Avalonia.PropertySystemDemo"
                           DocPath="properties/value-precedence" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="官方文档在 Styling 与 Property System 两个分类下都列了这一页。它讲的是属性系统如何在多个来源之间选值，样式只是来源之一，因此完整演示放在属性系统项目里。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.PropertySystemDemo   →  「值优先级」标签页" />
                <TextBlock Classes="hint"
                           Text="与样式最相关的一条：写在元素上的值（LocalValue）永远压过所有 Style Setter，连伪类也救不回来。ControlTheme 的 Setter 则低于 Style——本项目「ControlTheme」页第 3 节有说明。" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

`PrecedencePage.axaml.cs`：无状态页标准形状。

- [ ] **Step 13: 在 MainWindow 挂 9 个 Tab**

把 `Avalonia.StylingDemo/Views/MainWindow.axaml` 的 `<TabControl>` 一段替换为（根元素加
`xmlns:pages`）：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:pages="using:Avalonia.StylingDemo.Views.Pages"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.StylingDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Styling Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <TabControl Margin="12">
        <TabItem Header="选择器">
            <pages:SelectorsPage />
        </TabItem>
        <TabItem Header="样式类">
            <pages:StyleClassesPage />
        </TabItem>
        <TabItem Header="伪类">
            <pages:PseudoClassesPage />
        </TabItem>
        <TabItem Header="ControlTheme">
            <pages:ControlThemesPage />
        </TabItem>
        <TabItem Header="主题变体">
            <pages:ThemesPage />
        </TabItem>
        <TabItem Header="容器查询">
            <pages:ContainerQueriesPage />
        </TabItem>
        <TabItem Header="字体">
            <pages:FontsPage />
        </TabItem>
        <TabItem Header="样式共享">
            <pages:SharingStylesPage />
        </TabItem>
        <TabItem Header="优先级">
            <pages:PrecedencePage />
        </TabItem>
    </TabControl>
</Window>
```

- [ ] **Step 14: 构建**

Run: `dotnet build Avalonia.StylingDemo/Avalonia.StylingDemo.csproj 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`，无 `error` 行。

再跑 Task 1 Step 7 的警告检查命令，Expected: 无输出。

- [ ] **Step 15: 用 headless 探针断言页面行为**

**不要用目视核对代替这一步**（理由见 foundation plan Task 2 Step 12）。在**仓库外**建探针。
创建 `C:\Temp\stylecheck\stylecheck.csproj`：

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
        <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.StylingDemo\Avalonia.StylingDemo.csproj" />
    </ItemGroup>
</Project>
```

创建 `C:\Temp\stylecheck\Program.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.StylingDemo.Controls;
using Avalonia.StylingDemo.Views.Pages;
using Avalonia.StylingDemo.Views;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
        // Mirrors App.axaml: without this, ToggleChip has no template at all.
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Avalonia.StylingDemo/"))
        {
            Source = new Uri("avares://Avalonia.StylingDemo/Controls/ToggleChip.axaml")
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
        Dispatcher.UIThread.RunJobs();
    }

    private static string Color(IBrush? b) => (b as ISolidColorBrush)?.Color.ToString() ?? "null";

    // The cells of a row, in visual order, including ones nested one Border deeper.
    private static List<Border> Cells(Visual row)
        => row.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("cell")).ToList();

    private static void Check(string label, bool ok, string detail)
        => Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-36} {detail}");

    // The one artefact no page probe touches: the TabControl wiring in MainWindow.axaml.
    // A tab pointing at the wrong page still builds with zero errors.
    private static void RunShell()
    {
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        Check("Shell: tab count and headers", tabs.Count == 9
            && tabs.Select(t => t.Header as string).SequenceEqual(new[]
            { "选择器", "样式类", "伪类", "ControlTheme", "主题变体", "容器查询", "字体", "样式共享", "优先级" }),
            string.Join(",", tabs.Select(t => t.Header)));
        for (var i = 0; i < tabs.Count && i < 9; i++)
        {
            tabs[i].IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            var page = tabs[i].Content?.GetType().Name ?? "<null>";
            Check($"Shell: tab {i} renders its page", page == PageNames[i], page);
        }
    }

    private static readonly string[] PageNames =
    { "SelectorsPage", "StyleClassesPage", "PseudoClassesPage", "ControlThemesPage", "ThemesPage",
      "ContainerQueriesPage", "FontsPage", "SharingStylesPage", "PrecedencePage" };

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        RunShell();

        // SelectorsPage
        var sel = new SelectorsPage();
        Layout(sel);
        var child = Cells(Find<StackPanel>(sel, "ChildRow")).Select(b => Color(b.Background)).ToList();
        Check("Selectors: > skips the nested cell", child[0] == "#ffe8564a" && child[2] != "#ffe8564a",
            string.Join(",", child));
        var desc = Cells(Find<StackPanel>(sel, "DescendantRow")).Select(b => Color(b.Background)).ToList();
        Check("Selectors: space reaches the nested cell", desc.All(c => c == "#ff4ac7e8"), string.Join(",", desc));
        var exact = Find<WrapPanel>(sel, "ExactRow").Children.Select(c => Color(((TemplatedControl)c).Background)).ToList();
        Check("Selectors: type selector exact only", exact[0] == "#ffe8564a" && exact[1] != "#ffe8564a" && exact[2] != "#ffe8564a",
            string.Join(",", exact));
        var isRow = Find<WrapPanel>(sel, "IsRow").Children.Select(c => Color(((TemplatedControl)c).Background)).ToList();
        Check("Selectors: :is() takes subclasses", isRow.All(c => c == "#ff6fe84a"), string.Join(",", isRow));
        var nth = Cells(Find<StackPanel>(sel, "NthRow")).Select(b => Color(b.Background)).ToList();
        Check("Selectors: nth-child(2n)", nth[1] == "#ffe8d24a" && nth[3] == "#ffe8d24a" && nth[0] != "#ffe8d24a",
            string.Join(",", nth));
        var not = Cells(Find<StackPanel>(sel, "NotRow")).Select(b => b.Opacity).ToList();
        Check("Selectors: :not(.keep)", not[0] == 1 && not[1] == 0.25, string.Join(",", not));
        var owner = Find<Button>(sel, "FlyoutOwner");
        owner.Flyout!.ShowAt(owner);
        Dispatcher.UIThread.RunJobs();
        // The presenter lives in a popup root, so a search down from the window never finds it.
        // Walk up from the flyout's own content instead.
        var presenter = (((Flyout)owner.Flyout).Content as Visual)?.FindAncestorOfType<FlyoutPresenter>();
        var flyBg = Color(presenter?.Background);
        Check("Selectors: style reaches FlyoutPresenter", flyBg == "#ffe8564a", flyBg);
        owner.Flyout.Hide();

        // StyleClassesPage
        var sc = new StyleClassesPage();
        Layout(sc);
        var swatches = sc.GetVisualDescendants().OfType<Border>()
            .Where(b => b.Classes.Contains("green") && b.Classes.Contains("yellow")).Select(b => Color(b.Background)).ToList();
        Check("Classes: declaration order wins", swatches.Count == 2 && swatches.All(c => c == "#ffe8d24a"),
            string.Join(",", swatches));
        Find<ToggleSwitch>(sc, "WarnSwitch").IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Check("Classes: Classes.warn binding", Find<Border>(sc, "BoundSwatch").Classes.Contains("warn"), "");

        // PseudoClassesPage
        var pc = new PseudoClassesPage();
        Layout(pc);
        var chip = Find<ToggleChip>(pc, "Chip");
        var hasTemplate = chip.GetVisualDescendants().Any(v => (v as Control)?.Name == "PART_ContentPresenter");
        chip.IsOn = true;
        Dispatcher.UIThread.RunJobs();
        Check("Pseudo: ToggleChip :on styled", hasTemplate && Color(chip.Background) == "#ffe8974a",
            $"template={hasTemplate} bg={Color(chip.Background)}");

        // ControlThemesPage
        var ct = new ControlThemesPage();
        Layout(ct);
        var tall = Find<Button>(ct, "TallWideButton");
        var tallFluent = tall.GetVisualDescendants().Any(v => (v as Control)?.Name == "PART_ContentPresenter");
        Check("Theme: two-level BasedOn", tall.Width == 200 && tall.Height == 56 && tallFluent,
            $"{tall.Width}x{tall.Height} fluent={tallFluent}");
        Check("Theme: custom template replaces Fluent", Color(Find<Button>(ct, "YellowButton").Background) == "#ffffff00",
            Color(Find<Button>(ct, "YellowButton").Background));

        // ThemesPage
        var th = new ThemesPage();
        Layout(th);
        var dyn = Find<Border>(th, "DynamicCard");
        var darkColor = Color(dyn.Background);
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        var lightColor = Color(dyn.Background);
        Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();
        Check("Themes: DynamicResource follows variant", darkColor == "#ff222222" && lightColor == "#ffeeeeee",
            $"dark={darkColor} light={lightColor}");
        Check("Themes: StaticResource gets null", Find<Border>(th, "StaticCard").Background is null,
            Color(Find<Border>(th, "StaticCard").Background));

        // FontsPage
        var fp = new FontsPage();
        Layout(fp);
        var icon = Find<TextBlock>(fp, "IconSample");
        var family = FontManager.Current.TryGetGlyphTypeface(new Typeface(icon.FontFamily), out var g) ? g.FamilyName : "NOT RESOLVED";
        Check("Fonts: embedded iconfont resolves", family == "iconfont", family);

        // SharingStylesPage
        var ss = new SharingStylesPage();
        Layout(ss);
        Check("Sharing: StyleInclude applied", Find<Button>(ss, "CallButton").Width == 120,
            $"width={Find<Button>(ss, "CallButton").Width}");
        Check("Sharing: ResourceInclude applied", Color(Find<Border>(ss, "AccentSwatch").Background) == "#ffe8974a",
            Color(Find<Border>(ss, "AccentSwatch").Background));

        // The two signpost pages only need to load.
        Layout(new ContainerQueriesPage());
        Layout(new PrecedencePage());
        Check("Signposts: load", true, "");

        Console.WriteLine($"\nwarning-or-worse log entries: {sink.Entries.Count}");
        foreach (var e in sink.Entries.Distinct()) Console.WriteLine("  " + e);
    }
}
```

Run: `dotnet run --project C:\Temp\stylecheck\stylecheck.csproj`

Expected: 19 行全部 `PASS`，且 `warning-or-worse log entries: 0`。`StaticCard` 取到 null 时
**不产生任何日志**（编写 plan 时已实测），所以这里没有例外——出现任何一条都要修。

任何一行 `FAIL` 都要先修好再提交，**不要**把 FAIL 解释成"探针写得不对"就跳过。若确认是探针
本身的问题（例如控件名拼错），修探针后重跑。

- [ ] **Step 16: 清理探针并提交**

```bash
rm -rf /c/Temp/stylecheck
git add Avalonia.StylingDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Styling category

Nine pages covering selector syntax, style classes, pseudo-classes,
control themes, theme variants, fonts and shared style files, plus two
signpost pages for topics already demonstrated elsewhere. The styling
content of the old DataTemplateDemo moves here: the shared button styles,
the yellow ControlTheme, and the styled Flyout in the selector page.

Verified with a throwaway headless probe: type selectors skip subclasses
while :is() catches them, class conflicts resolve by declaration order,
the custom :on pseudo-class styles ToggleChip, two-level BasedOn keeps the
Fluent template, ThemeDictionaries resolve through DynamicResource but
come back null through StaticResource, and the embedded icon font loads.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: 项目 #5 Data Binding 的 12 个页面

**Files:**
- Create: `Avalonia.DataBindingDemo/Models/Contact.cs`、`Models/ContactGroupHeader.cs`
- Create: `Avalonia.DataBindingDemo/Converters/RgbToBrushConverter.cs`、`Converters/CelsiusToFahrenheitConverter.cs`、`Converters/InlineConverters.cs`
- Create: `Avalonia.DataBindingDemo/Diagnostics/BindingLogSink.cs`
- Create: `Avalonia.DataBindingDemo/ViewModels/` 下 `SyntaxViewModel`、`CollectionsViewModel`、`MasterDetailViewModel`、`CommandsViewModel`、`ValidationViewModel`、`CollectionViewsViewModel`、`AsyncViewModel`、`DebuggingViewModel`
- Create: `Avalonia.DataBindingDemo/Views/Pages/` 下 12 组 `XxxPage.axaml(.cs)`：`SyntaxPage`、`CompiledBindingsPage`、`CollectionsPage`、`MasterDetailPage`、`MultiBindingPage`、`CommandsPage`、`ConvertersPage`、`ValidationPage`、`CollectionViewsPage`、`AsyncPage`、`DebuggingPage`、`MarkupExtensionsPage`
- Modify: `Avalonia.DataBindingDemo/Views/MainWindow.axaml`（挂 12 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳；`Avalonia.Shared.ViewModels.ViewModelBase`（继承自 `ObservableObject`）
- Produces: 无跨任务产物。`Contact` 只在本项目使用（DataTemplatesDemo 用自己的 `Person`）。

有状态页沿用基础层写法：**在页面构造函数里** `DataContext = new XxxViewModel();`，页面根元素声明
`x:DataType="vm:XxxViewModel"`。`CompiledBindingsPage`、`MultiBindingPage`、`ConvertersPage`、
`MarkupExtensionsPage` 无状态（只用 `#元素名` 绑定），不建 ViewModel。

本任务比 Task 2 多一类静默失败：**编译绑定写错属性名会在构建期报错，`ReflectionBinding` 写错却只记一条
warning**。只有 `DebuggingPage` 故意写错绑定；其他页面若在探针里出现 binding warning，就是 bug。

- [ ] **Step 1: 创建模型**

`Avalonia.DataBindingDemo/Models/Contact.cs`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.DataBindingDemo.Models
{
    /// <summary>
    /// Observable so that editing a contact in a detail pane updates every
    /// list showing it — the list items and the detail bind to the same object.
    /// </summary>
    public partial class Contact : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _city;

        [ObservableProperty]
        private int _age;

        public Contact(string name, string city, int age)
        {
            _name = name;
            _city = city;
            _age = age;
        }

        // ListBox falls back to ToString when no template is given; make that readable.
        public override string ToString() => $"{Name}（{City}）";

        public static Contact[] Samples() =>
        [
            new("张三", "北京", 28),
            new("李四", "上海", 35),
            new("王五", "北京", 22),
            new("赵六", "广州", 41),
            new("钱七", "上海", 30),
            new("孙八", "深圳", 26),
        ];
    }
}
```

`Avalonia.DataBindingDemo/Models/ContactGroupHeader.cs`：

```csharp
namespace Avalonia.DataBindingDemo.Models
{
    /// <summary>
    /// A row that is not a contact. Grouping is done by flattening groups into one
    /// list of headers and contacts; a DataTemplate per type renders each kind.
    /// </summary>
    public sealed record ContactGroupHeader(string Key, int Count);
}
```

- [ ] **Step 2: 创建两个自定义转换器**

`Avalonia.DataBindingDemo/Converters/RgbToBrushConverter.cs`：

```csharp
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.DataBindingDemo.Converters
{
    /// <summary>
    /// Three slider values in, one brush out. A MultiBinding hands the converter
    /// every source value at once, in the order the bindings were declared.
    /// </summary>
    public sealed class RgbToBrushConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 3 && values[0] is double r && values[1] is double g && values[2] is double b)
            {
                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }

            // While a source is still unresolved, keep whatever the target already shows.
            return BindingOperations.DoNothing;
        }
    }
}
```

`Avalonia.DataBindingDemo/Converters/CelsiusToFahrenheitConverter.cs`：

```csharp
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Avalonia.DataBindingDemo.Converters
{
    /// <summary>
    /// A two-way converter: ConvertBack runs when the user types into the
    /// Fahrenheit box and pushes the value back to the Celsius slider.
    /// </summary>
    public sealed class CelsiusToFahrenheitConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is double c ? (c * 9 / 5 + 32).ToString("F1", culture) : BindingOperations.DoNothing;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Half-typed input such as "-" is normal while editing: leave the source alone
            // rather than flag an error. Reporting errors is ValidationPage's job.
            return value is string s && double.TryParse(s, NumberStyles.Float, culture, out var f)
                ? (f - 32) * 5 / 9
                : BindingOperations.DoNothing;
        }
    }
}
```

- [ ] **Step 3: 创建 SyntaxPage 与 SyntaxViewModel**

`Avalonia.DataBindingDemo/ViewModels/SyntaxViewModel.cs`：

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class SyntaxViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _message = "改我试试";

        // Only ever written by the view (OneWayToSource); the page echoes it back for proof.
        [ObservableProperty]
        private string _lastTyped = string.Empty;
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/SyntaxPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.SyntaxPage"
             x:DataType="vm:SyntaxViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="绑定语法：源从哪里来，值往哪边流"
                               DocPath="data-binding/data-binding-syntax" />

            <TextBlock Classes="caption" Text="1. 默认源是 DataContext，并沿逻辑树向下继承" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="4">
                    <TextBlock Text="{Binding Message, StringFormat='页面的 DataContext.Message = {0}'}" />
                    <Border Padding="8" BorderBrush="#40FFFFFF" BorderThickness="1">
                        <TextBlock Name="InheritedText" Text="{Binding Message, StringFormat='嵌套两层的子元素也读到：{0}'}" />
                    </Border>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 绑定模式" />
            <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto,Auto" RowSpacing="6" ColumnSpacing="8">
                <TextBlock Grid.Row="0" VerticalAlignment="Center" Text="TwoWay（TextBox 默认）" />
                <TextBox Grid.Row="0" Grid.Column="1" Name="TwoWayBox" Text="{Binding Message}" />
                <TextBlock Grid.Row="1" VerticalAlignment="Center" Text="OneWay（TextBlock 默认）" />
                <TextBlock Grid.Row="1" Grid.Column="1" Name="OneWayText" VerticalAlignment="Center" Text="{Binding Message}" />
                <TextBlock Grid.Row="2" VerticalAlignment="Center" Text="OneTime：只取初值" />
                <TextBlock Grid.Row="2" Grid.Column="1" Name="OneTimeText" VerticalAlignment="Center" Text="{Binding Message, Mode=OneTime}" />
                <TextBlock Grid.Row="3" VerticalAlignment="Center" Text="OneWayToSource：只写回" />
                <StackPanel Grid.Row="3" Grid.Column="1" Orientation="Horizontal" Spacing="8">
                    <TextBox Name="ToSourceBox" Width="200" Text="{Binding LastTyped, Mode=OneWayToSource}" Watermark="在这里输入" />
                    <TextBlock VerticalAlignment="Center" Text="{Binding LastTyped, StringFormat='ViewModel 收到：{0}'}" />
                </StackPanel>
            </Grid>
            <TextBlock Classes="hint" Text="在第一行改文字：OneWay 行跟着变，OneTime 行停在初值。" />

            <TextBlock Classes="caption" Text="3. 换一个源：#元素名 与 $parent" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="Source" Width="200" Minimum="0" Maximum="100" Value="40" />
                <TextBlock Name="ElementText" VerticalAlignment="Center"
                           Text="{Binding #Source.Value, StringFormat='#Source.Value = {0:F0}'}" />
            </StackPanel>
            <TextBlock Name="ParentText" Margin="0,6,0,0"
                       Text="{Binding $parent[Window].Title, StringFormat='$parent[Window].Title = {0}'}" />
            <TextBlock Classes="hint" Text="#Name 等价于 ElementName；$parent[类型] 沿视觉树向上找第一个该类型的祖先，$parent[类型;1] 跳过第一个。$self 指元素自己。" />

            <TextBlock Classes="caption" Text="4. 绑定样式类" />
            <TextBlock Classes="hint" Text="Classes.类名=&quot;{Binding 布尔值}&quot; 的完整演示在 Avalonia.StylingDemo 的「样式类」页第 3 节。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/SyntaxPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class SyntaxPage : UserControl
    {
        public SyntaxPage()
        {
            InitializeComponent();
            DataContext = new SyntaxViewModel();
        }
    }
}
```

有状态页的 `.axaml.cs` 都是这个形状，只换类名与 ViewModel 名——下文对有状态页**仍完整列出**
这几行（因为 ViewModel 名各不相同），无状态页则引用 Task 2 Step 4 的标准形状（命名空间换成
`Avalonia.DataBindingDemo.Views.Pages`）。

`Grid` 的 `RowSpacing` / `ColumnSpacing` 是 Avalonia 11.1 起才有的属性，12.1.2 可用。

- [ ] **Step 4: 创建 CompiledBindingsPage（无状态）**

`Avalonia.DataBindingDemo/Views/Pages/CompiledBindingsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.CompiledBindingsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="编译绑定：路径在构建时检查，写错就编不过"
                               DocPath="data-binding/compiled-bindings" />

            <TextBlock Classes="caption" Text="1. 本项目开启了 AvaloniaUseCompiledBindingsByDefault" />
            <Border Classes="stage" Padding="10">
                <StackPanel Spacing="6">
                    <TextBlock TextWrapping="Wrap"
                               Text="所以每个 {Binding} 都按 x:DataType 声明的类型在构建期解析。把 SyntaxPage.axaml 里的 Message 改成 Mesage 再构建，会得到 AVLN 开头的编译错误，而不是运行时一条容易漏看的警告。" />
                    <SelectableTextBlock FontFamily="Consolas, monospace"
                                         Text="&lt;AvaloniaUseCompiledBindingsByDefault&gt;true&lt;/AvaloniaUseCompiledBindingsByDefault&gt;" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 编译绑定的路径必须能静态确定类型" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="Source" Width="200" Minimum="0" Maximum="10" Value="3" />
                <TextBlock Name="CompiledText" VerticalAlignment="Center"
                           Text="{Binding #Source.Value, StringFormat='编译绑定 #Source.Value = {0:F1}'}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="#元素名 的类型由 XAML 编译器从元素声明里推出来，因此本页没有 x:DataType 也能编译。" />

            <TextBlock Classes="caption" Text="3. 逃生口：ReflectionBinding 与 x:CompileBindings=&quot;False&quot;" />
            <StackPanel Spacing="4">
                <TextBlock Name="ReflectionText"
                           Text="{ReflectionBinding #Source.Value, StringFormat='ReflectionBinding 同样读到 {0:F1}'}" />
                <StackPanel x:CompileBindings="False">
                    <TextBlock Name="UncompiledText"
                               Text="{Binding #Source.Value, StringFormat='整块关闭编译绑定后也读到 {0:F1}'}" />
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint" Text="两者都在运行时用反射解析：写错路径不会编译失败，只会在日志里留一条 Binding 警告——「绑定调试」页演示怎么把它找出来。只有 DataContext 类型在编译期确实未知时才用。" />

            <TextBlock Classes="caption" Text="4. 在代码里建绑定" />
            <TextBlock Name="CodeBoundText" />
            <TextBlock Classes="hint" Text="见 CompiledBindingsPage.axaml.cs：control.Bind(属性, new Binding(...))。代码里的 Binding 永远是反射绑定。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/CompiledBindingsPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CompiledBindingsPage : UserControl
    {
        public CompiledBindingsPage()
        {
            InitializeComponent();

            // The code-side equivalent of Text="{Binding #Source.Value, StringFormat=...}".
            CodeBoundText.Bind(TextBlock.TextProperty, new Binding("Value")
            {
                Source = Source,
                StringFormat = "代码里 Bind() 读到 {0:F1}",
            });
        }
    }
}
```

- [ ] **Step 5: 创建 CollectionsPage 与 CollectionsViewModel**

`Avalonia.DataBindingDemo/ViewModels/CollectionsViewModel.cs`：

```csharp
using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class CollectionsViewModel : ViewModelBase
    {
        private int _added;

        // Raises CollectionChanged, so bound lists insert and remove rows themselves.
        public ObservableCollection<Contact> Contacts { get; } = new(Contact.Samples()[..3]);

        // A plain List never tells anyone it changed: the bound list stays frozen.
        public List<Contact> FrozenContacts { get; } = new(Contact.Samples()[..3]);

        [RelayCommand]
        private void Add()
        {
            _added++;
            Contacts.Add(new Contact($"新人{_added}", "杭州", 20 + _added));
            FrozenContacts.Add(new Contact($"新人{_added}", "杭州", 20 + _added));
        }

        [RelayCommand]
        private void RemoveLast()
        {
            if (Contacts.Count > 0)
            {
                Contacts.RemoveAt(Contacts.Count - 1);
            }
        }
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/CollectionsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             xmlns:models="using:Avalonia.DataBindingDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.CollectionsPage"
             x:DataType="vm:CollectionsViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="绑定到集合：ObservableCollection 让列表自己增删行"
                               DocPath="data-binding/how-to-bind-to-a-collection" />

            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="两边各加一人" Command="{Binding AddCommand}" />
                <Button Content="删左边最后一人" Command="{Binding RemoveLastCommand}" />
            </StackPanel>

            <Grid ColumnDefinitions="*,*" ColumnSpacing="12" Margin="0,8,0,0">
                <StackPanel Grid.Column="0">
                    <TextBlock Classes="caption" Text="ObservableCollection" />
                    <TextBlock Name="LiveCount" Text="{Binding Contacts.Count, StringFormat='共 {0} 人'}" />
                    <ListBox Name="LiveList" Height="200" ItemsSource="{Binding Contacts}" />
                </StackPanel>
                <StackPanel Grid.Column="1">
                    <TextBlock Classes="caption" Text="List（不通知）" />
                    <TextBlock Name="FrozenCount" Text="{Binding FrozenContacts.Count, StringFormat='共 {0} 人'}" />
                    <ListBox Name="FrozenList" Height="200" ItemsSource="{Binding FrozenContacts}" />
                </StackPanel>
            </Grid>
            <TextBlock Classes="hint" Text="右边的 List 里其实也加进去了，但它不发 CollectionChanged，列表与计数都停在 3。没有给 ItemTemplate 时，ListBox 显示每项的 ToString()。" />

            <TextBlock Classes="caption" Text="同一个集合也能绑到 TabControl：每个联系人一页" />
            <TabControl Name="ContactTabs" Height="120" ItemsSource="{Binding Contacts}">
                <TabControl.ItemTemplate>
                    <DataTemplate x:DataType="models:Contact">
                        <TextBlock Text="{Binding Name}" />
                    </DataTemplate>
                </TabControl.ItemTemplate>
                <TabControl.ContentTemplate>
                    <DataTemplate x:DataType="models:Contact">
                        <TextBlock Margin="8" Text="{Binding Age, StringFormat='年龄 {0}'}" />
                    </DataTemplate>
                </TabControl.ContentTemplate>
            </TabControl>
            <TextBlock Classes="hint" Text="ItemTemplate 决定标签头，ContentTemplate 决定内容区——这是官方 how-to-bind-to-a-collection 一页的例子。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/CollectionsPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CollectionsPage : UserControl
    {
        public CollectionsPage()
        {
            InitializeComponent();
            DataContext = new CollectionsViewModel();
        }
    }
}
```

`Contact.Samples()[..3]` 用的是数组的范围运算符，C# 8 起可用，返回新数组——两个集合
各拿一份互不共享的 `Contact` 对象。

- [ ] **Step 6: 创建 MasterDetailPage 与 MasterDetailViewModel**

`Avalonia.DataBindingDemo/ViewModels/MasterDetailViewModel.cs`：

```csharp
using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class MasterDetailViewModel : ViewModelBase
    {
        public ObservableCollection<Contact> Contacts { get; } = new(Contact.Samples());

        [ObservableProperty]
        private Contact? _selected;
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/MasterDetailPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             xmlns:models="using:Avalonia.DataBindingDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.MasterDetailPage"
             x:DataType="vm:MasterDetailViewModel">

    <DockPanel Margin="12">
        <shared:DemoHeader DockPanel.Dock="Top"
                           Title="主从视图：列表的 SelectedItem 就是详情区的 DataContext"
                           DocPath="data-binding/master-detail" />

        <TextBlock DockPanel.Dock="Bottom" Classes="hint"
                   Text="在右边改名字，左边列表同一行立刻更新：两边绑的是同一个 Contact 对象，而 Contact 实现了 INotifyPropertyChanged。" />

        <Grid ColumnDefinitions="220,*" ColumnSpacing="12">
            <ListBox Grid.Column="0" Name="MasterList"
                     ItemsSource="{Binding Contacts}"
                     SelectedItem="{Binding Selected}">
                <ListBox.ItemTemplate>
                    <DataTemplate x:DataType="models:Contact">
                        <TextBlock Text="{Binding Name}" />
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>

            <Border Grid.Column="1" Classes="stage" Padding="12">
                <Panel>
                    <TextBlock Name="EmptyHint" Text="← 选一个联系人"
                               IsVisible="{Binding Selected, Converter={x:Static ObjectConverters.IsNull}}" />
                    <!--  Re-rooting the DataContext lets every binding below use short paths.  -->
                    <StackPanel Name="DetailPanel" Spacing="8"
                                DataContext="{Binding Selected}"
                                x:DataType="models:Contact"
                                IsVisible="{Binding $self.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}">
                        <TextBlock Text="姓名" />
                        <TextBox Name="DetailName" Text="{Binding Name}" />
                        <TextBlock Text="城市" />
                        <TextBox Text="{Binding City}" />
                        <TextBlock Text="{Binding Age, StringFormat='年龄 {0}'}" />
                    </StackPanel>
                </Panel>
            </Border>
        </Grid>
    </DockPanel>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/MasterDetailPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class MasterDetailPage : UserControl
    {
        public MasterDetailPage()
        {
            InitializeComponent();
            DataContext = new MasterDetailViewModel();
        }
    }
}
```

详情面板的 `IsVisible` 绑 `$self.DataContext` 而不是 `Selected`：面板上的 `x:DataType` 已经换成
`Contact`，在它身上写 `{Binding Selected}` 会在构建期报"Contact 没有 Selected 属性"。

- [ ] **Step 7: 创建 MultiBindingPage（无状态）**

`Avalonia.DataBindingDemo/Views/Pages/MultiBindingPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:conv="using:Avalonia.DataBindingDemo.Converters"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.MultiBindingPage">

    <UserControl.Resources>
        <conv:RgbToBrushConverter x:Key="RgbToBrush" />
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="多值绑定：几个源合成一个值"
                               DocPath="data-binding/multi-binding" />

            <TextBlock Classes="caption" Text="1. 自定义 IMultiValueConverter：三个滑块合成一种颜色" />
            <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto" ColumnSpacing="8">
                <TextBlock Grid.Row="0" VerticalAlignment="Center" Text="R" />
                <Slider Grid.Row="0" Grid.Column="1" Name="Red" Maximum="255" Value="232" />
                <TextBlock Grid.Row="1" VerticalAlignment="Center" Text="G" />
                <Slider Grid.Row="1" Grid.Column="1" Name="Green" Maximum="255" Value="151" />
                <TextBlock Grid.Row="2" VerticalAlignment="Center" Text="B" />
                <Slider Grid.Row="2" Grid.Column="1" Name="Blue" Maximum="255" Value="74" />
            </Grid>
            <Border Name="Mixed" Height="48" CornerRadius="6">
                <Border.Background>
                    <MultiBinding Converter="{StaticResource RgbToBrush}">
                        <Binding Path="#Red.Value" />
                        <Binding Path="#Green.Value" />
                        <Binding Path="#Blue.Value" />
                    </MultiBinding>
                </Border.Background>
            </Border>

            <TextBlock Classes="caption" Text="2. 不写转换器：MultiBinding 的 StringFormat" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBox Name="Family" Width="100" Text="欧阳" />
                <TextBox Name="Given" Width="100" Text="修" />
                <TextBlock Name="FullName" VerticalAlignment="Center">
                    <TextBlock.Text>
                        <MultiBinding StringFormat="{}全名：{0}{1}">
                            <Binding Path="#Family.Text" />
                            <Binding Path="#Given.Text" />
                        </MultiBinding>
                    </TextBlock.Text>
                </TextBlock>
            </StackPanel>
            <TextBlock Classes="hint" Text="开头的 {} 是转义：告诉 XAML 后面的花括号不是标记扩展。" />

            <TextBlock Classes="caption" Text="3. 内置的 BoolConverters.And / Or：两个勾都打上才能点" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <CheckBox Name="Agree" Content="同意条款" />
                <CheckBox Name="Adult" Content="已满 18 岁" />
                <Button Name="SubmitButton" Content="提交">
                    <Button.IsEnabled>
                        <MultiBinding Converter="{x:Static BoolConverters.And}">
                            <Binding Path="#Agree.IsChecked" />
                            <Binding Path="#Adult.IsChecked" />
                        </MultiBinding>
                    </Button.IsEnabled>
                </Button>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`MultiBindingPage.axaml.cs`：无状态页标准形状。

- [ ] **Step 8: 创建 CommandsPage 与 CommandsViewModel**

`Avalonia.DataBindingDemo/ViewModels/CommandsViewModel.cs`：

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class CommandsViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DecrementCommand))]
        private int _count;

        [ObservableProperty]
        private string _lastAction = "（还没点过）";

        [RelayCommand]
        private void Increment() => Count++;

        // CanExecute is re-queried whenever Count changes, via the attribute on _count.
        private bool CanDecrement() => Count > 0;

        [RelayCommand(CanExecute = nameof(CanDecrement))]
        private void Decrement() => Count--;

        // The generated command reads the parameter from CommandParameter.
        [RelayCommand]
        private void Add(string step) => Count += int.Parse(step);

        // An async command disables its button while running and exposes IsRunning.
        [RelayCommand]
        private async Task SlowReset()
        {
            await Task.Delay(1500);
            Count = 0;
        }

        // Not a command at all: Avalonia can bind Command straight to a public method.
        public void SayHello(object? who) => LastAction = $"方法绑定被调用，参数 = {who}";
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/CommandsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.CommandsPage"
             x:DataType="vm:CommandsViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="命令：按钮不认识事件处理器，只认识 ICommand"
                               DocPath="data-binding/binding-to-commands" />

            <TextBlock Name="CountText" FontSize="28" Text="{Binding Count, StringFormat='Count = {0}'}" />

            <TextBlock Classes="caption" Text="1. [RelayCommand] 生成命令，CanExecute 控制按钮可用" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="+1" Command="{Binding IncrementCommand}" />
                <Button Name="DecrementButton" Content="-1（为 0 时禁用）" Command="{Binding DecrementCommand}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. CommandParameter 把参数带给命令" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="+5" Command="{Binding AddCommand}" CommandParameter="5" />
                <Button Content="+10" Command="{Binding AddCommand}" CommandParameter="10" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. 异步命令：执行期间按钮自动禁用" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="SlowButton" Content="1.5 秒后清零" Command="{Binding SlowResetCommand}" />
                <TextBlock VerticalAlignment="Center"
                           Text="{Binding SlowResetCommand.IsRunning, StringFormat='IsRunning = {0}'}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="4. 直接绑方法（Avalonia 特有）" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="MethodButton" Content="调用 SayHello" Command="{Binding SayHello}" CommandParameter="小明" />
                <TextBlock Name="LastActionText" VerticalAlignment="Center" Text="{Binding LastAction}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="方法绑定省掉了命令属性，但没有 CanExecute：按钮永远可点。需要禁用逻辑时用 [RelayCommand]。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/CommandsPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CommandsPage : UserControl
    {
        public CommandsPage()
        {
            InitializeComponent();
            DataContext = new CommandsViewModel();
        }
    }
}
```

- [ ] **Step 9: 创建 ConvertersPage（无状态）**

`Avalonia.DataBindingDemo/Views/Pages/ConvertersPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:conv="using:Avalonia.DataBindingDemo.Converters"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.ConvertersPage">

    <UserControl.Resources>
        <conv:CelsiusToFahrenheitConverter x:Key="CelsiusToFahrenheit" />
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="转换器：源与目标类型不一致时，中间插一层翻译"
                               DocPath="data-binding/how-to-create-a-custom-data-binding-converter" />

            <TextBlock Classes="caption" Text="1. 内置转换器：不用写代码" />
            <StackPanel Spacing="6">
                <TextBox Name="Input" Width="260" HorizontalAlignment="Left" Watermark="输入点什么" />
                <TextBlock Name="EmptyWarning" Foreground="#E8564A" Text="StringConverters.IsNullOrEmpty：框是空的"
                           IsVisible="{Binding #Input.Text, Converter={x:Static StringConverters.IsNullOrEmpty}}" />
                <TextBlock Name="NotEmptyNote" Foreground="#6FE84A" Text="StringConverters.IsNotNullOrEmpty：有内容了"
                           IsVisible="{Binding #Input.Text, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                <TextBlock Text="{Binding !#Input.IsFocused, StringFormat='! 取反：输入框没有焦点 = {0}'}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="还有 ObjectConverters.IsNull / IsNotNull、BoolConverters.And / Or / Not。! 和 !! 是取反与转 bool 的简写，直接写在路径前面。" />

            <TextBlock Classes="caption" Text="2. 自定义 IValueConverter：摄氏 ⇄ 华氏，双向" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="Celsius" Width="240" Minimum="-40" Maximum="100" Value="20" />
                <TextBlock VerticalAlignment="Center" Text="{Binding #Celsius.Value, StringFormat='{0:F1} °C  ='}" />
                <TextBox Name="Fahrenheit" Width="80"
                         Text="{Binding #Celsius.Value, Converter={StaticResource CelsiusToFahrenheit}, Mode=TwoWay}" />
                <TextBlock VerticalAlignment="Center" Text="°F" />
            </StackPanel>
            <TextBlock Classes="hint" Text="拖滑块走 Convert；在右边输入 212 走 ConvertBack，滑块跳到 100。输入非数字时 ConvertBack 返回 DoNothing，滑块不动。" />

            <TextBlock Classes="caption" Text="3. 一次性转换器：FuncValueConverter" />
            <TextBlock Name="FuncText" Text="{Binding #Celsius.Value, Converter={x:Static conv:InlineConverters.Thermometer}}" />
            <TextBlock Classes="hint" Text="FuncValueConverter&lt;TIn, TOut&gt; 用一个 lambda 就能建单向转换器，放在静态字段里用 x:Static 引用。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`ConvertersPage.axaml.cs`：无状态页标准形状。

再创建 `Avalonia.DataBindingDemo/Converters/InlineConverters.cs`：

```csharp
using Avalonia.Data.Converters;
using System;

namespace Avalonia.DataBindingDemo.Converters
{
    public static class InlineConverters
    {
        // One-way only: FuncValueConverter has no ConvertBack.
        public static readonly FuncValueConverter<double, string> Thermometer =
            new(c => c switch
            {
                < 0 => "❄ 结冰",
                < 25 => "🙂 舒适",
                _ => "🔥 炎热",
            } + $"（{Math.Round(c)} °C）");
    }
}
```

- [ ] **Step 10: 创建 ValidationPage 与 ValidationViewModel**

`Avalonia.DataBindingDemo/ViewModels/ValidationViewModel.cs`：

```csharp
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;

namespace Avalonia.DataBindingDemo.ViewModels
{
    /// <summary>
    /// Derives from ObservableValidator rather than ViewModelBase: the validator
    /// base class is what implements INotifyDataErrorInfo, which is the channel
    /// Avalonia reads errors from.
    /// </summary>
    public partial class ValidationViewModel : ObservableValidator
    {
        // 1. DataAnnotations, surfaced through INotifyDataErrorInfo.
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        [Required(ErrorMessage = "用户名必填")]
        [MinLength(3, ErrorMessage = "用户名至少 3 个字符")]
        private string _userName = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        [EmailAddress(ErrorMessage = "邮箱格式不对")]
        private string _email = "someone@example.com";

        [ObservableProperty]
        private string _submitResult = string.Empty;

        // 2. Exception-based: the setter refuses the value and the binding shows the message.
        private int _age = 18;

        public int Age
        {
            get => _age;
            set
            {
                if (value is < 0 or > 150)
                {
                    // DataValidationException shows its bare message; any other exception
                    // type is shown with its type name in front (see rule 15).
                    throw new DataValidationException("年龄必须在 0 到 150 之间");
                }

                SetProperty(ref _age, value);
            }
        }

        private bool CanSubmit() => !HasErrors && !string.IsNullOrEmpty(UserName);

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private void Submit() => SubmitResult = $"已提交：{UserName} / {Email} / {Age} 岁";
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/ValidationPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.ValidationPage"
             x:DataType="vm:ValidationViewModel">

    <ScrollViewer>
        <StackPanel Margin="12" Spacing="4">
            <shared:DemoHeader Title="绑定校验：错误从 ViewModel 流回界面，显示在输入框下方"
                               DocPath="data-binding/binding-validation" />

            <TextBlock Classes="caption" Text="1. DataAnnotations + ObservableValidator" />
            <TextBlock Text="用户名（必填，至少 3 个字符）" />
            <TextBox Name="UserNameBox" Width="300" HorizontalAlignment="Left" Text="{Binding UserName}" />
            <TextBlock Text="邮箱" />
            <TextBox Name="EmailBox" Width="300" HorizontalAlignment="Left" Text="{Binding Email}" />
            <TextBlock Classes="hint" Text="[NotifyDataErrorInfo] 让生成的 setter 在赋值后跑一遍特性校验，并通过 INotifyDataErrorInfo.ErrorsChanged 通知界面。" />

            <TextBlock Classes="caption" Text="2. setter 里抛异常" />
            <TextBlock Text="年龄（0–150）" />
            <TextBox Name="AgeBox" Width="300" HorizontalAlignment="Left" Text="{Binding Age}" />
            <TextBlock Classes="hint" Text="输入 200：setter 抛 DataValidationException，Age 保持旧值。输入 abc：根本到不了 setter，是 string → int 的转换失败，框架同样把它显示成错误。" />

            <TextBlock Classes="caption" Text="3. 有错误时提交按钮不可用" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="SubmitButton" Content="提交" Command="{Binding SubmitCommand}" />
                <TextBlock VerticalAlignment="Center" Text="{Binding SubmitResult}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="年龄错误不会让按钮变灰：异常校验发生在绑定层，ViewModel 的 HasErrors 并不知道。这是两种校验方式最大的区别。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/ValidationPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class ValidationPage : UserControl
    {
        public ValidationPage()
        {
            InitializeComponent();
            DataContext = new ValidationViewModel();
        }
    }
}
```

- [ ] **Step 11: 创建 CollectionViewsPage 与 CollectionViewsViewModel**

Avalonia 没有 WPF 的 `CollectionViewSource` / `ICollectionView`。官方 `collection-views` 一页给出的
做法就是：**排序、筛选、分组都在 ViewModel 里做，界面只绑结果集合**。分组用"把组头和成员
拍平成一个列表、每种类型一个 DataTemplate"的写法，不需要任何第三方控件。

`Avalonia.DataBindingDemo/ViewModels/CollectionViewsViewModel.cs`：

```csharp
using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class CollectionViewsViewModel : ViewModelBase
    {
        private readonly Contact[] _all = Contact.Samples();

        // Holds Contact and ContactGroupHeader rows side by side; templates tell them apart.
        public ObservableCollection<object> Rows { get; } = new();

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private bool _sortByAge;

        [ObservableProperty]
        private bool _groupByCity;

        public CollectionViewsViewModel() => Rebuild();

        partial void OnFilterChanged(string value) => Rebuild();

        partial void OnSortByAgeChanged(bool value) => Rebuild();

        partial void OnGroupByCityChanged(bool value) => Rebuild();

        private void Rebuild()
        {
            var query = _all.Where(c => c.Name.Contains(Filter.Trim()));
            query = SortByAge ? query.OrderBy(c => c.Age) : query.OrderBy(c => c.Name);

            Rows.Clear();
            if (!GroupByCity)
            {
                foreach (var c in query)
                {
                    Rows.Add(c);
                }

                return;
            }

            foreach (var group in query.GroupBy(c => c.City))
            {
                Rows.Add(new ContactGroupHeader(group.Key, group.Count()));
                foreach (var c in group)
                {
                    Rows.Add(c);
                }
            }
        }
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/CollectionViewsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             xmlns:models="using:Avalonia.DataBindingDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.CollectionViewsPage"
             x:DataType="vm:CollectionViewsViewModel">

    <DockPanel Margin="12">
        <shared:DemoHeader DockPanel.Dock="Top"
                           Title="集合视图：排序、筛选、分组都在 ViewModel 里做"
                           DocPath="data-binding/collection-views" />

        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="12" Margin="0,0,0,8">
            <TextBox Name="FilterBox" Width="180" Watermark="按姓名筛选" Text="{Binding Filter}" />
            <CheckBox Name="SortBox" Content="按年龄排序" IsChecked="{Binding SortByAge}" />
            <CheckBox Name="GroupBox" Content="按城市分组" IsChecked="{Binding GroupByCity}" />
        </StackPanel>

        <TextBlock DockPanel.Dock="Bottom" Classes="hint"
                   Text="Avalonia 没有 CollectionViewSource。每次条件变化 ViewModel 重算一遍 Rows；分组时组头也是 Rows 里的一项，靠 DataTemplate 的 DataType 区分渲染方式。" />

        <ListBox Name="RowList" ItemsSource="{Binding Rows}">
            <ListBox.DataTemplates>
                <DataTemplate DataType="models:ContactGroupHeader">
                    <TextBlock FontWeight="Bold" Foreground="#E8974A"
                               Text="{Binding Key, StringFormat='【{0}】'}" />
                </DataTemplate>
                <DataTemplate DataType="models:Contact">
                    <StackPanel Orientation="Horizontal" Spacing="12" Margin="16,0,0,0">
                        <TextBlock Width="60" Text="{Binding Name}" />
                        <TextBlock Width="60" Text="{Binding City}" />
                        <TextBlock Text="{Binding Age, StringFormat='{0} 岁'}" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.DataTemplates>
        </ListBox>
    </DockPanel>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/CollectionViewsPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CollectionViewsPage : UserControl
    {
        public CollectionViewsPage()
        {
            InitializeComponent();
            DataContext = new CollectionViewsViewModel();
        }
    }
}
```

官方页面对大集合推荐 DynamicData（`SourceList` + `.Filter().Sort().Bind()`）。它是第三方库，
按 Global Constraints 不引入；本页的"清空重建"是官方页面给出的另一种写法，6 条数据足够。

组头也能被选中——这是拍平写法的已知代价。演示里保留它，hint 不展开；需要不可选的组头时，
用 `ItemsControl` 代替 `ListBox` 即可。

- [ ] **Step 12: 创建 AsyncPage 与 AsyncViewModel**

`Avalonia.DataBindingDemo/ViewModels/AsyncViewModel.cs`：

```csharp
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Shared.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class AsyncViewModel : ViewModelBase
    {
        // Replaced (not mutated) on reload, so the ^ binding re-subscribes to the new task.
        // Until the new task finishes, the target keeps the previous result.
        [ObservableProperty]
        private Task<string> _greeting = LoadGreetingAsync();

        public IObservable<string> Clock { get; } = new ClockObservable();

        // A Bitmap is just a property value: load it once, bind Image.Source to it.
        public Bitmap Logo { get; } = new(AssetLoader.Open(new Uri("avares://Avalonia.DataBindingDemo/Assets/avalonia-logo.ico")));

        [RelayCommand]
        private void Reload() => Greeting = LoadGreetingAsync();

        private static async Task<string> LoadGreetingAsync()
        {
            await Task.Delay(2000);
            return $"加载完成于 {DateTime.Now:HH:mm:ss}";
        }

        /// <summary>
        /// A minimal IObservable without pulling in System.Reactive: it pushes the
        /// current time every second for as long as someone is subscribed.
        /// </summary>
        private sealed class ClockObservable : IObservable<string>
        {
            public IDisposable Subscribe(IObserver<string> observer)
            {
                observer.OnNext(DateTime.Now.ToString("HH:mm:ss"));
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (_, _) => observer.OnNext(DateTime.Now.ToString("HH:mm:ss"));
                timer.Start();
                return new Stopper(timer);
            }

            private sealed class Stopper(DispatcherTimer timer) : IDisposable
            {
                // Called when the binding drops its subscription, which happens when the
                // DataContext changes — not merely when the page leaves the visual tree.
                public void Dispose() => timer.Stop();
            }
        }
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/AsyncPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.AsyncPage"
             x:DataType="vm:AsyncViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="异步与图片：^ 运算符直接绑 Task 与 IObservable"
                               DocPath="data-binding/how-to-bind-to-a-task-result" />

            <TextBlock Classes="caption" Text="1. Task^：任务完成后显示结果，之前显示 FallbackValue" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <TextBlock Name="GreetingText" VerticalAlignment="Center"
                           Text="{Binding Greeting^, FallbackValue='加载中……（2 秒）'}" />
                <Button Content="重新加载" Command="{Binding ReloadCommand}" />
            </StackPanel>
            <TextBlock Classes="hint" Text="不写 FallbackValue 的话，任务完成前文本是空的。注意「重新加载」时并不会回到 FallbackValue：新任务完成前，界面一直停在上一次的结果上。要显示加载状态，就得另设 IsLoading 属性（或用异步命令的 IsRunning，见「命令」页）。" />

            <TextBlock Classes="caption" Text="2. IObservable^：每推一个值界面更新一次" />
            <TextBlock Name="ClockText" FontSize="24" Text="{Binding Clock^}" />
            <TextBlock Classes="hint" Text="ClockObservable 是十几行的手写实现，没有引入 System.Reactive。绑定只在 DataContext 换掉时 Dispose 订阅——切到别的标签页时页面离开了可视树，计时器却仍在跑。" />

            <TextBlock Classes="caption" Text="3. 绑定图片" />
            <StackPanel Orientation="Horizontal" Spacing="16">
                <StackPanel Spacing="4">
                    <Image Name="BoundImage" Width="64" Height="64" Source="{Binding Logo}" />
                    <TextBlock Classes="hint" Text="绑定 Bitmap 属性" />
                </StackPanel>
                <StackPanel Spacing="4">
                    <Image Name="UriImage" Width="64" Height="64" Source="/Assets/avalonia-logo.ico" />
                    <TextBlock Classes="hint" Text="XAML 里直接写路径" />
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint" Text="XAML 的字符串路径由类型转换器变成 Bitmap；ViewModel 里要自己用 AssetLoader.Open 打开资源流。图片要从网络加载时，把 Bitmap 换成 Task&lt;Bitmap&gt; 再用 ^ 绑定。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/AsyncPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class AsyncPage : UserControl
    {
        public AsyncPage()
        {
            InitializeComponent();
            DataContext = new AsyncViewModel();
        }
    }
}
```

- [ ] **Step 13: 创建 BindingLogSink、DebuggingPage 与 DebuggingViewModel**

调试页要把绑定错误**显示在界面上**，而不是让用户去翻 Output 窗口。做法是装一个只截
`LogArea.Binding` 的 sink，并把其余日志转交给原来的 sink（`Program.cs` 里 `LogToTrace` 装的那个），
不破坏全局日志。实测串联后原 sink 照常收到每一条。

创建 `Avalonia.DataBindingDemo/Diagnostics/BindingLogSink.cs`：

```csharp
using Avalonia.Logging;
using Avalonia.Threading;
using System.Collections.ObjectModel;

namespace Avalonia.DataBindingDemo.Diagnostics
{
    /// <summary>
    /// Captures binding warnings into a collection the debugging page can show,
    /// and forwards everything to whichever sink was installed before it.
    /// </summary>
    public sealed class BindingLogSink : ILogSink
    {
        private readonly ILogSink? _inner;

        private BindingLogSink(ILogSink? inner) => _inner = inner;

        public static BindingLogSink? Current { get; private set; }

        public ObservableCollection<string> Entries { get; } = new();

        // Idempotent: the page may be constructed more than once (designer, re-navigation).
        public static BindingLogSink Install()
        {
            if (Current is null)
            {
                Current = new BindingLogSink(Logger.Sink);
                Logger.Sink = Current;
            }

            return Current;
        }

        public bool IsEnabled(LogEventLevel level, string area)
            => IsBindingWarning(level, area) || (_inner?.IsEnabled(level, area) ?? false);

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            Capture(level, area, messageTemplate);
            _inner?.Log(level, area, source, messageTemplate);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            // The template is "{Property} to {Expression} ... {Message}"; the values alone read better.
            Capture(level, area, propertyValues.Length > 0 ? string.Join(" | ", propertyValues) : messageTemplate);
            _inner?.Log(level, area, source, messageTemplate, propertyValues);
        }

        private static bool IsBindingWarning(LogEventLevel level, string area)
            => area == LogArea.Binding && level >= LogEventLevel.Warning;

        private void Capture(LogEventLevel level, string area, string text)
        {
            if (IsBindingWarning(level, area))
            {
                // Bindings may log from inside layout; never mutate a bound collection re-entrantly.
                Dispatcher.UIThread.Post(() => Entries.Add(text));
            }
        }
    }
}
```

`Avalonia.DataBindingDemo/ViewModels/DebuggingViewModel.cs`：

```csharp
using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class DebuggingViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Contact? _selected;

        [RelayCommand]
        private void Toggle() => Selected = Selected is null ? new Contact("张三", "北京", 28) : null;
    }
}
```

`Avalonia.DataBindingDemo/Views/Pages/DebuggingPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataBindingDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.DebuggingPage"
             x:DataType="vm:DebuggingViewModel">

    <DockPanel Margin="12">
        <shared:DemoHeader DockPanel.Dock="Top"
                           Title="绑定调试：绑定失败不抛异常，只在日志里留一行"
                           DocPath="data-binding/binding-debugging" />

        <StackPanel DockPanel.Dock="Top">
            <TextBlock Classes="caption" Text="1. 中间节点为 null 时的三种写法（Selected 当前为 null）" />
            <Button Content="切换 Selected（null ⇄ 张三）" Command="{Binding ToggleCommand}" />
            <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto" ColumnSpacing="12" RowSpacing="4" Margin="0,6,0,0">
                <TextBlock Grid.Row="0" FontFamily="Consolas, monospace" Text="Selected.Name, FallbackValue=…" />
                <TextBlock Grid.Row="0" Grid.Column="1" Name="FallbackText"
                           Text="{Binding Selected.Name, FallbackValue='（未选择）'}" />
                <TextBlock Grid.Row="1" FontFamily="Consolas, monospace" Text="Selected?.Name" />
                <TextBlock Grid.Row="1" Grid.Column="1" Name="NullCondText"
                           Text="{Binding Selected?.Name}" />
                <TextBlock Grid.Row="2" FontFamily="Consolas, monospace" Text="Selected?.Name, TargetNullValue=…" />
                <TextBlock Grid.Row="2" Grid.Column="1" Name="TargetNullText"
                           Text="{Binding Selected?.Name, TargetNullValue='（未选择）'}" />
            </Grid>
            <TextBlock Classes="hint" Text="第一行显示回退值，但下方日志多一条 Value is null。第二行没有日志，可文本是空的——?. 让 FallbackValue 失效。第三行既安静又有占位文本：?. 配 TargetNullValue。" />

            <TextBlock Classes="caption" Text="2. 拼错的反射绑定：编译通过，运行时静默失败" />
            <TextBlock Name="TypoText" FontFamily="Consolas, monospace"
                       Text="{ReflectionBinding Selectd.Name, FallbackValue='{}{ReflectionBinding Selectd.Name} → 看下方日志'}" />

            <TextBlock Classes="caption" Text="3. 本页截获的 Binding 警告（实时）" />
        </StackPanel>

        <TextBlock DockPanel.Dock="Bottom" Classes="hint"
                   Text="其他排查手段：运行时按 F12 打开 DevTools 查看任一控件的 DataContext 与绑定值；Program.cs 的 LogToTrace(LogEventLevel.Debug) 让同样的日志出现在 IDE 输出窗口。" />

        <Border Classes="stage" Padding="6">
            <ScrollViewer>
                <ItemsControl Name="LogList">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <SelectableTextBlock FontFamily="Consolas, monospace" FontSize="12"
                                                 TextWrapping="Wrap" Text="{Binding}" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Border>
    </DockPanel>
</UserControl>
```

`Avalonia.DataBindingDemo/Views/Pages/DebuggingPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataBindingDemo.Diagnostics;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class DebuggingPage : UserControl
    {
        public DebuggingPage()
        {
            // Install before InitializeComponent so the page's own broken bindings are caught.
            var sink = BindingLogSink.Install();
            InitializeComponent();
            LogList.ItemsSource = sink.Entries;
            DataContext = new DebuggingViewModel();
        }
    }
}
```

`FallbackValue` 里的 `{}` 不能省：单引号**不能**阻止花括号被当成标记扩展。实测去掉 `{}` 后文本
变成 `Avalonia.Data.ReflectionBinding`——XAML 把引号里的内容又解析成了一个嵌套绑定对象。

日志列表**不**走 ViewModel：sink 是进程级单例，与"每页一个 ViewModel"的状态无关；在
code-behind 里直接赋 `ItemsSource` 比给 ViewModel 塞一个全局对象更诚实。

`DataContext` 在 `InitializeComponent` **之后**才赋值，于是加载瞬间所有 `{Binding}` 都会先对着 null
求值一次。这不产生警告（`DataContext` 为 null 时 Avalonia 不报错），因此不影响"日志只来自本页
故意写错的两处"这一判断。

- [ ] **Step 14: 创建 MarkupExtensionsPage（指路页）**

`Avalonia.DataBindingDemo/Views/Pages/MarkupExtensionsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="760"
             d:DesignHeight="560"
             x:Class="Avalonia.DataBindingDemo.Views.Pages.MarkupExtensionsPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="标记扩展：完整演示在 Avalonia.XamlDemo"
                           DocPath="data-binding/markup-extensions" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="{}{Binding} 本身就是一个标记扩展。官方文档在 Data Binding 与 XAML 两个分类下都讲了标记扩展；内置扩展一览与自定义 MarkupExtension 的写法放在 XAML 项目里。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.XamlDemo   →  「标记扩展」标签页" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

`MarkupExtensionsPage.axaml.cs`：无状态页标准形状。

- [ ] **Step 15: 在 MainWindow 挂 12 个 Tab**

`Avalonia.DataBindingDemo/Views/MainWindow.axaml`：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:pages="using:Avalonia.DataBindingDemo.Views.Pages"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.DataBindingDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Data Binding Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <TabControl Margin="12" TabStripPlacement="Left">
        <TabItem Header="绑定语法">
            <pages:SyntaxPage />
        </TabItem>
        <TabItem Header="编译绑定">
            <pages:CompiledBindingsPage />
        </TabItem>
        <TabItem Header="集合">
            <pages:CollectionsPage />
        </TabItem>
        <TabItem Header="主从视图">
            <pages:MasterDetailPage />
        </TabItem>
        <TabItem Header="多值绑定">
            <pages:MultiBindingPage />
        </TabItem>
        <TabItem Header="命令">
            <pages:CommandsPage />
        </TabItem>
        <TabItem Header="转换器">
            <pages:ConvertersPage />
        </TabItem>
        <TabItem Header="校验">
            <pages:ValidationPage />
        </TabItem>
        <TabItem Header="集合视图">
            <pages:CollectionViewsPage />
        </TabItem>
        <TabItem Header="异步与图片">
            <pages:AsyncPage />
        </TabItem>
        <TabItem Header="绑定调试">
            <pages:DebuggingPage />
        </TabItem>
        <TabItem Header="标记扩展">
            <pages:MarkupExtensionsPage />
        </TabItem>
    </TabControl>
</Window>
```

- [ ] **Step 16: 构建**

Run: `dotnet build Avalonia.DataBindingDemo/Avalonia.DataBindingDemo.csproj 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`，无 `error` 行。再跑 Task 1 Step 7 的警告检查命令，Expected: 无输出。

若出现 `AVLN` 开头的错误，说明某处 `{Binding}` 的路径在 `x:DataType` 上找不到——这正是编译绑定
的价值所在，按报错的行号修正路径，**不要**改成 `ReflectionBinding` 绕过。

- [ ] **Step 17: 用 headless 探针断言页面行为**

创建 `C:\Temp\bindcheck\bindcheck.csproj`：

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
        <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.DataBindingDemo\Avalonia.DataBindingDemo.csproj" />
    </ItemGroup>
</Project>
```

创建 `C:\Temp\bindcheck\Program.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.DataBindingDemo.Diagnostics;
using Avalonia.DataBindingDemo.Models;
using Avalonia.DataBindingDemo.ViewModels;
using Avalonia.DataBindingDemo.Views.Pages;
using Avalonia.DataBindingDemo.Views;
using Avalonia.Headless;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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

    private static Window Layout(Control c)
    {
        var w = new Window { Title = "ProbeWindow", Content = c, Width = 900, Height = 700 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        return w;
    }

    private static void Pump() => Dispatcher.UIThread.RunJobs();

    private static string Texts(Visual root)
        => string.Join(",", root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrEmpty(t)));

    private static string Errors(Control c)
        => string.Join("|", DataValidationErrors.GetErrors(c)?.Cast<object>() ?? []);

    // Executes the bound command with the bound parameter. RaiseEvent(ClickEvent) would
    // fire Click handlers only and leave the command untouched (rule 16).
    private static void Invoke(Button b)
    {
        b.Command!.Execute(b.CommandParameter);
        Pump();
    }

    private static void Check(string label, bool ok, string detail)
        => Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-38} {detail}");

    // The one artefact no page probe touches: the TabControl wiring in MainWindow.axaml.
    // A tab pointing at the wrong page still builds with zero errors.
    private static void RunShell()
    {
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        Check("Shell: tab count and headers", tabs.Count == 12
            && tabs.Select(t => t.Header as string).SequenceEqual(new[] { "绑定语法", "编译绑定", "集合", "主从视图", "多值绑定", "命令", "转换器", "校验", "集合视图", "异步与图片", "绑定调试", "标记扩展" }),
            string.Join(",", tabs.Select(t => t.Header)));
        for (var i = 0; i < tabs.Count && i < 12; i++)
        {
            tabs[i].IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            var page = tabs[i].Content?.GetType().Name ?? "<null>";
            Check($"Shell: tab {i} renders its page", page == PageNames[i], page);
        }
    }

    private static readonly string[] PageNames = { "SyntaxPage", "CompiledBindingsPage", "CollectionsPage", "MasterDetailPage", "MultiBindingPage", "CommandsPage", "ConvertersPage", "ValidationPage", "CollectionViewsPage", "AsyncPage", "DebuggingPage", "MarkupExtensionsPage" };

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        RunShell();
        RunPages();

        // Only DebuggingPage breaks bindings on purpose; it runs last so its two warnings are
        // the only ones allowed. Snapshot the count first.
        var beforeDebugging = sink.Entries.Count;
        RunDebugging();

        Console.WriteLine($"\nwarnings from the eleven ordinary pages: {beforeDebugging}");
        foreach (var e in sink.Entries.Take(beforeDebugging).Distinct()) Console.WriteLine("  " + e);
    }

    private static void RunPages()
    {
        // SyntaxPage
        var syn = new SyntaxPage();
        Layout(syn);
        Find<TextBox>(syn, "TwoWayBox").Text = "新值";
        Pump();
        Check("Syntax: inherited DataContext", Find<TextBlock>(syn, "InheritedText").Text?.Contains("新值") == true,
            Find<TextBlock>(syn, "InheritedText").Text ?? "");
        Check("Syntax: OneTime keeps initial", Find<TextBlock>(syn, "OneTimeText").Text == "改我试试",
            Find<TextBlock>(syn, "OneTimeText").Text ?? "");
        Find<TextBox>(syn, "ToSourceBox").Text = "写回";
        Pump();
        Check("Syntax: OneWayToSource writes back", ((SyntaxViewModel)syn.DataContext!).LastTyped == "写回",
            ((SyntaxViewModel)syn.DataContext!).LastTyped);
        Check("Syntax: $parent[Window]", Find<TextBlock>(syn, "ParentText").Text?.EndsWith("ProbeWindow") == true,
            Find<TextBlock>(syn, "ParentText").Text ?? "");

        // CompiledBindingsPage
        var cb = new CompiledBindingsPage();
        Layout(cb);
        Find<Slider>(cb, "Source").Value = 7;
        Pump();
        var four = new[] { "CompiledText", "ReflectionText", "UncompiledText", "CodeBoundText" }
            .Select(n => Find<TextBlock>(cb, n).Text ?? "").ToList();
        Check("Compiled: four binding styles agree", four.All(t => t.Contains("7.0")), string.Join(" / ", four));

        // CollectionsPage
        var col = new CollectionsPage();
        Layout(col);
        var addButton = col.GetVisualDescendants().OfType<Button>().First();
        Invoke(addButton);
        Check("Collections: Observable grows", Find<TextBlock>(col, "LiveCount").Text == "共 4 人",
            Find<TextBlock>(col, "LiveCount").Text ?? "");
        Check("Collections: List stays frozen", Find<TextBlock>(col, "FrozenCount").Text == "共 3 人",
            Find<TextBlock>(col, "FrozenCount").Text ?? "");
        var tabCount = Find<TabControl>(col, "ContactTabs").GetVisualDescendants().OfType<TabItem>().Count();
        Check("Collections: one tab per contact", tabCount == 4, $"tabs={tabCount}");

        // MasterDetailPage
        var md = new MasterDetailPage();
        Layout(md);
        var detailBefore = Find<StackPanel>(md, "DetailPanel").IsVisible;
        Find<ListBox>(md, "MasterList").SelectedIndex = 1;
        Pump();
        Find<TextBox>(md, "DetailName").Text = "李四改";
        Pump();
        var masterTexts = Texts(Find<ListBox>(md, "MasterList"));
        Check("MasterDetail: detail hidden until selection", !detailBefore && Find<StackPanel>(md, "DetailPanel").IsVisible,
            $"before={detailBefore}");
        Check("MasterDetail: edit flows back to list", masterTexts.Contains("李四改"), masterTexts);

        // MultiBindingPage
        var mb = new MultiBindingPage();
        Layout(mb);
        var mixed = (Find<Border>(mb, "Mixed").Background as ISolidColorBrush)?.Color.ToString();
        Check("Multi: RGB converter", mixed == "#ffe8974a", mixed ?? "null");
        Check("Multi: StringFormat", Find<TextBlock>(mb, "FullName").Text == "全名：欧阳修", Find<TextBlock>(mb, "FullName").Text ?? "");
        var submit = Find<Button>(mb, "SubmitButton");
        var andBefore = submit.IsEnabled;
        Find<CheckBox>(mb, "Agree").IsChecked = true;
        Find<CheckBox>(mb, "Adult").IsChecked = true;
        Pump();
        Check("Multi: BoolConverters.And", !andBefore && submit.IsEnabled, $"before={andBefore} after={submit.IsEnabled}");

        // CommandsPage
        var cmd = new CommandsPage();
        Layout(cmd);
        var cvm = (CommandsViewModel)cmd.DataContext!;
        var decBefore = Find<Button>(cmd, "DecrementButton").IsEffectivelyEnabled;
        var plusFive = cmd.GetVisualDescendants().OfType<Button>().First(b => b.CommandParameter as string == "5");
        Invoke(plusFive);
        Check("Commands: CanExecute gates -1", !decBefore && Find<Button>(cmd, "DecrementButton").IsEffectivelyEnabled,
            $"before={decBefore}");
        Check("Commands: CommandParameter", cvm.Count == 5, $"count={cvm.Count}");
        Invoke(Find<Button>(cmd, "MethodButton"));
        Check("Commands: method binding", Find<TextBlock>(cmd, "LastActionText").Text?.Contains("小明") == true,
            Find<TextBlock>(cmd, "LastActionText").Text ?? "");

        // ConvertersPage
        var cv = new ConvertersPage();
        Layout(cv);
        var emptyBefore = Find<TextBlock>(cv, "EmptyWarning").IsVisible;
        Find<TextBox>(cv, "Input").Text = "x";
        Pump();
        Check("Converters: StringConverters", emptyBefore && !Find<TextBlock>(cv, "EmptyWarning").IsVisible
            && Find<TextBlock>(cv, "NotEmptyNote").IsVisible, $"emptyBefore={emptyBefore}");
        Check("Converters: Convert C→F", Find<TextBox>(cv, "Fahrenheit").Text == "68.0", Find<TextBox>(cv, "Fahrenheit").Text ?? "");
        Find<TextBox>(cv, "Fahrenheit").Text = "212";
        Pump();
        Check("Converters: ConvertBack F→C", Math.Abs(Find<Slider>(cv, "Celsius").Value - 100) < 0.01,
            $"celsius={Find<Slider>(cv, "Celsius").Value}");
        Find<TextBox>(cv, "Fahrenheit").Text = "abc";
        Pump();
        Check("Converters: DoNothing leaves source", Math.Abs(Find<Slider>(cv, "Celsius").Value - 100) < 0.01
            && !DataValidationErrors.GetHasErrors(Find<TextBox>(cv, "Fahrenheit")), $"celsius={Find<Slider>(cv, "Celsius").Value}");
        Check("Converters: FuncValueConverter", Find<TextBlock>(cv, "FuncText").Text?.Contains("炎热") == true,
            Find<TextBlock>(cv, "FuncText").Text ?? "");

        // ValidationPage
        var val = new ValidationPage();
        Layout(val);
        var user = Find<TextBox>(val, "UserNameBox");
        user.Text = "ab";
        Pump();
        Check("Validation: DataAnnotations message", Errors(user) == "用户名至少 3 个字符", Errors(user));
        Check("Validation: submit disabled on error", !Find<Button>(val, "SubmitButton").IsEffectivelyEnabled, "");
        user.Text = "abcd";
        var age = Find<TextBox>(val, "AgeBox");
        age.Text = "200";
        Pump();
        Check("Validation: exception message is bare", Errors(age) == "年龄必须在 0 到 150 之间", Errors(age));
        Check("Validation: age error does not gate submit", Find<Button>(val, "SubmitButton").IsEffectivelyEnabled,
            "hint on the page claims this");

        // CollectionViewsPage
        var cvp = new CollectionViewsPage();
        Layout(cvp);
        var cvvm = (CollectionViewsViewModel)cvp.DataContext!;
        cvvm.GroupByCity = true;
        Pump();
        var grouped = Texts(Find<ListBox>(cvp, "RowList"));
        Check("CollectionViews: group headers rendered", grouped.Contains("【北京】") && grouped.Contains("【上海】"), grouped);
        cvvm.Filter = "王";
        Pump();
        var filtered = cvvm.Rows.OfType<Contact>().Select(c => c.Name).ToList();
        Check("CollectionViews: filter", filtered.SequenceEqual(new[] { "王五" }), string.Join(",", filtered));

        // AsyncPage
        var asy = new AsyncPage();
        Layout(asy);
        Check("Async: Task^ shows fallback first", Find<TextBlock>(asy, "GreetingText").Text?.StartsWith("加载中") == true,
            Find<TextBlock>(asy, "GreetingText").Text ?? "");
        Check("Async: Observable^ ticks", Find<TextBlock>(asy, "ClockText").Text?.Length == 8,
            Find<TextBlock>(asy, "ClockText").Text ?? "");
        // Headless bitmaps are always 1x1 (rule 18): assert the type, not the size.
        Check("Async: bound Bitmap", Find<Image>(asy, "BoundImage").Source is Bitmap, "");
        Check("Async: URI Bitmap", Find<Image>(asy, "UriImage").Source is Bitmap, "");

        // MarkupExtensionsPage only needs to load.
        Layout(new MarkupExtensionsPage());
    }

    private static void RunDebugging()
    {
        var dbg = new DebuggingPage();
        Layout(dbg);
        Check("Debugging: FallbackValue", Find<TextBlock>(dbg, "FallbackText").Text == "（未选择）", "");
        Check("Debugging: ?. swallows fallback", string.IsNullOrEmpty(Find<TextBlock>(dbg, "NullCondText").Text), "");
        Check("Debugging: ?. + TargetNullValue", Find<TextBlock>(dbg, "TargetNullText").Text == "（未选择）", "");
        Check("Debugging: escaped fallback text", Find<TextBlock>(dbg, "TypoText").Text?.StartsWith("{ReflectionBinding") == true,
            Find<TextBlock>(dbg, "TypoText").Text ?? "");
        Pump();
        var captured = BindingLogSink.Current?.Entries.ToList() ?? new();
        Check("Debugging: page sink caught both", captured.Count == 2
            && captured.Any(e => e.Contains("Value is null")) && captured.Any(e => e.Contains("Selectd")),
            string.Join(" // ", captured));
        var rows = Find<ItemsControl>(dbg, "LogList").GetVisualDescendants().OfType<SelectableTextBlock>().Count();
        Check("Debugging: log rendered on page", rows == 2, $"rows={rows}");
    }
}
```

Run: `dotnet run --project C:\Temp\bindcheck\bindcheck.csproj`

Expected: 38 行全部 `PASS`，且 `warnings from the eleven ordinary pages: 0`。

`RunDebugging` 必须最后跑：它装的 `BindingLogSink` 是进程级的，提前装上会把其他页面的警告也收进
`captured`，让「page sink caught both」一行失真。

任何一行 `FAIL` 都要先修好再提交。若某一行是因为页面里的 `Name` 拼错导致 `Find` 抛异常，修页面
（页面与探针中的名字以本 plan 为准）。

- [ ] **Step 18: 清理探针并提交**

```bash
rm -rf /c/Temp/bindcheck
git add Avalonia.DataBindingDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Data Binding category

Twelve pages covering binding syntax and modes, compiled bindings,
collections, master-detail, multi-binding, commands, converters,
validation, ViewModel-side collection views, Task and IObservable
bindings, and binding diagnostics, plus a signpost to the XAML demo
for markup extensions.

The debugging page installs a chained log sink that surfaces binding
warnings on screen without stealing them from the trace output.

Verified with a throwaway headless probe: OneTime holds its first value,
a plain List never updates its view, edits in the detail pane flow back
to the master list, CanExecute gates buttons, DataValidationException
surfaces its bare message, ?. suppresses FallbackValue, and the eleven
ordinary pages log no binding warnings at all.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: 项目 #6 Data Templates 的 9 个页面

**Files:**
- Create: `Avalonia.DataTemplatesDemo/Models/Person.cs`（迁移自 `Avalonia.DataTemplateDemo/Models/Person.cs`）
- Create: `Avalonia.DataTemplatesDemo/Models/Shapes.cs`、`Models/Folder.cs`
- Create: `Avalonia.DataTemplatesDemo/DataTemplates/PersonDataTemplateSelector.cs`（迁移）
- Create: `Avalonia.DataTemplatesDemo/ViewLocator.cs`
- Create: `Avalonia.DataTemplatesDemo/ViewModels/` 下 `ViewLocatorViewModel.cs`、`DashboardViewModel.cs`、`SettingsViewModel.cs`
- Create: `Avalonia.DataTemplatesDemo/Views/Located/` 下 `DashboardView.axaml(.cs)`、`SettingsView.axaml(.cs)`
- Create: `Avalonia.DataTemplatesDemo/Views/Pages/` 下 9 组 `XxxPage.axaml(.cs)`：`ControlContentPage`、`ContentTemplatesPage`、`TemplateCollectionPage`、`SelectorPage`、`CodeTemplatesPage`、`ReusePage`、`ViewLocatorPage`、`PanelsAndTreesPage`、`VersusControlTemplatePage`
- Modify: `Avalonia.DataTemplatesDemo/Views/MainWindow.axaml`（挂 9 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳
- Produces: 无跨任务产物

除 `ViewLocatorPage` 外，本项目的页面都**不设 DataContext**：数据要么在 XAML 里直接实例化
（`<models:Circle Radius="3" />`），要么在 code-behind 里赋给 `ItemsSource` / `Content`。这样做是因为
本分类讲的是"拿到一个对象之后怎么画它"，数据从哪儿来不是重点，绕过 ViewModel 让每页 XAML
自成一体。

实测（headless，Avalonia 12.1.2）的默认行为，ControlContentPage 以此为准：
`ContentControl.Content = new Circle()` 且无匹配模板时显示 **`XProbe.Circle`**（即 `ToString()`，
默认是类型全名）；`Button.Content` 同样如此；字符串直接显示。

- [ ] **Step 1: 创建模型（含迁移的 Person）**

`Avalonia.DataTemplatesDemo/Models/Person.cs`——迁移自旧项目，改动三处：命名空间；属性加
`required` 修掉旧项目的 4 条 CS8618 警告；`Id` 从 `string` 改为 `int`，因为模板里把它当序号
大字显示：

```csharp
namespace Avalonia.DataTemplatesDemo.Models
{
    public enum Sex
    {
        Male,
        Female,
    }

    public class Person
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required string Address { get; init; }

        public required Sex Sex { get; init; }
    }
}
```

`required` 只约束 C#：对象初始化器漏写会得到 CS9035 编译错误。**XAML 编译器不检查它**——实测
`<local:Req Name="x" />` 漏了 `required int Id`，构建 0 错误，运行时 `Id` 静默为 0。所以本项目的
`Person` 一律在 code-behind 里用对象初始化器创建，让 `required` 真正起作用。

`Avalonia.DataTemplatesDemo/Models/Shapes.cs`：

```csharp
namespace Avalonia.DataTemplatesDemo.Models
{
    // A small hierarchy so that implicit templates have something to choose between.
    public abstract class Shape
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public class Square : Shape
    {
        public double Side { get; set; }
    }

    // Deliberately has no template of its own: shows how DataType matching falls back.
    public class Triangle : Shape
    {
        public double Base { get; set; }
    }
}
```

`Avalonia.DataTemplatesDemo/Models/Folder.cs`：

```csharp
using System.Collections.ObjectModel;

namespace Avalonia.DataTemplatesDemo.Models
{
    public class Folder
    {
        public Folder(string title, params Folder[] children)
        {
            Title = title;
            Children = new ObservableCollection<Folder>(children);
        }

        public string Title { get; }

        public ObservableCollection<Folder> Children { get; }
    }
}
```

- [ ] **Step 2: 迁移 PersonDataTemplateSelector**

`Avalonia.DataTemplatesDemo/DataTemplates/PersonDataTemplateSelector.cs`——迁移自旧项目，修正
两处：两个模板属性改为可空（修 CS8618），以及 `Build` 末尾多余的 `;` 和"非 Person 返回空
`Control`"的写法（`Match` 已经保证只会收到 `Person`）：

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.DataTemplates
{
    /// <summary>
    /// Picks a template per item at runtime. Avalonia has no DataTemplateSelector
    /// base class like WPF; any IDataTemplate whose Build looks at the data is one.
    /// </summary>
    public class PersonDataTemplateSelector : IDataTemplate
    {
        public IDataTemplate? MaleDataTemplate { get; set; }

        public IDataTemplate? FemaleDataTemplate { get; set; }

        public Control? Build(object? param)
        {
            var person = (Person)param!;
            var template = person.Sex == Sex.Male ? MaleDataTemplate : FemaleDataTemplate;
            return template?.Build(param);
        }

        // Asked first, for every item. Returning false lets the next template in the collection try.
        public bool Match(object? data) => data is Person;
    }
}
```

- [ ] **Step 3: 创建 ControlContentPage**

`Avalonia.DataTemplatesDemo/Views/Pages/ControlContentPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.ControlContentPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="控件内容：Content 放进去的不是控件时，Avalonia 怎么画它"
                               DocPath="data-templates/control-content" />

            <TextBlock Classes="caption" Text="1. 放控件：原样显示" />
            <ContentControl Name="ControlContent">
                <Button Content="我本身就是控件" />
            </ContentControl>

            <TextBlock Classes="caption" Text="2. 放字符串：自动包一个 TextBlock" />
            <ContentControl Name="StringContent" Content="一段普通字符串" />

            <TextBlock Classes="caption" Text="3. 放普通对象、又没有模板：显示 ToString()" />
            <ContentControl Name="ObjectContent" />
            <Button Name="ObjectButton" HorizontalAlignment="Left" Margin="0,6,0,0" />
            <TextBlock Classes="hint" Text="上面两处都放了一个 Circle 对象，显示的是它的类型全名——object.ToString() 的默认实现。Button 也是 ContentControl，规则相同。" />

            <TextBlock Classes="caption" Text="4. 给它一个 DataTemplate" />
            <ContentControl Name="TemplatedContent">
                <ContentControl.ContentTemplate>
                    <DataTemplate x:DataType="x:Object">
                        <Border Classes="stage" Padding="8">
                            <TextBlock Text="现在由 ContentTemplate 决定怎么画。" />
                        </Border>
                    </DataTemplate>
                </ContentControl.ContentTemplate>
            </ContentControl>
            <TextBlock Classes="hint" Text="后面几页讲的就是这一步：模板写在哪里、怎样按类型自动挑选、怎样在代码里建。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/ControlContentPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ControlContentPage : UserControl
    {
        public ControlContentPage()
        {
            InitializeComponent();

            // Same kind of object into two ContentControls; neither has a template for it.
            ObjectContent.Content = new Circle { Name = "圆", Radius = 3 };
            ObjectButton.Content = new Circle { Name = "圆", Radius = 3 };
            TemplatedContent.Content = new Circle { Name = "圆", Radius = 3 };
        }
    }
}
```

- [ ] **Step 4: 创建 ContentTemplatesPage**

`Avalonia.DataTemplatesDemo/Views/Pages/ContentTemplatesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.DataTemplatesDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.ContentTemplatesPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="内联模板：ContentTemplate 画一个对象，ItemTemplate 画每一项"
                               DocPath="data-templates/content-templates" />

            <TextBlock Classes="caption" Text="1. ContentControl.ContentTemplate" />
            <ContentControl Name="SingleCircle">
                <ContentControl.ContentTemplate>
                    <DataTemplate x:DataType="models:Circle">
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <Ellipse Width="{Binding Radius}" Height="{Binding Radius}" Fill="#E8974A" />
                            <TextBlock VerticalAlignment="Center" Text="{Binding Radius, StringFormat='半径 {0}'}" />
                        </StackPanel>
                    </DataTemplate>
                </ContentControl.ContentTemplate>
            </ContentControl>

            <TextBlock Classes="caption" Text="2. ListBox.ItemTemplate：同一个模板套在每一项上" />
            <ListBox Name="CircleList" Height="180">
                <ListBox.ItemTemplate>
                    <DataTemplate x:DataType="models:Circle">
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <Ellipse Width="{Binding Radius}" Height="{Binding Radius}" Fill="#4AC7E8" />
                            <TextBlock VerticalAlignment="Center" Text="{Binding Name}" />
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
            <TextBlock Classes="hint" Text="模板内部的 DataContext 就是当前这一项，所以 {Binding Radius} 读的是 Circle.Radius。x:DataType 让这些绑定在编译期检查。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/ContentTemplatesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ContentTemplatesPage : UserControl
    {
        public ContentTemplatesPage()
        {
            InitializeComponent();

            SingleCircle.Content = new Circle { Name = "大圆", Radius = 40 };
            CircleList.ItemsSource = new[]
            {
                new Circle { Name = "小", Radius = 12 },
                new Circle { Name = "中", Radius = 24 },
                new Circle { Name = "大", Radius = 36 },
            };
        }
    }
}
```

- [ ] **Step 5: 创建 TemplateCollectionPage**

`Avalonia.DataTemplatesDemo/Views/Pages/TemplateCollectionPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.DataTemplatesDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.TemplateCollectionPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="模板集合：按 DataType 自动挑模板，从上往下取第一个匹配的"
                               DocPath="data-templates/data-template-collection" />

            <TextBlock Classes="caption" Text="1. 具体类型在前：圆、方各走各的，三角形落到基类模板" />
            <ItemsControl Name="SpecificFirst">
                <ItemsControl.DataTemplates>
                    <DataTemplate DataType="models:Circle">
                        <TextBlock Foreground="#E8974A" Text="{Binding Radius, StringFormat='● 圆，半径 {0}'}" />
                    </DataTemplate>
                    <DataTemplate DataType="models:Square">
                        <TextBlock Foreground="#4AC7E8" Text="{Binding Side, StringFormat='■ 方，边长 {0}'}" />
                    </DataTemplate>
                    <DataTemplate DataType="models:Shape">
                        <TextBlock Opacity="0.6" Text="{Binding Name, StringFormat='（通用 Shape 模板）{0}'}" />
                    </DataTemplate>
                </ItemsControl.DataTemplates>
            </ItemsControl>

            <TextBlock Classes="caption" Text="2. 基类模板放在最前：所有形状都被它截走" />
            <ItemsControl Name="BaseFirst">
                <ItemsControl.DataTemplates>
                    <DataTemplate DataType="models:Shape">
                        <TextBlock Opacity="0.6" Text="{Binding Name, StringFormat='（通用 Shape 模板）{0}'}" />
                    </DataTemplate>
                    <DataTemplate DataType="models:Circle">
                        <TextBlock Foreground="#E8974A" Text="{Binding Radius, StringFormat='● 圆，半径 {0}'}" />
                    </DataTemplate>
                </ItemsControl.DataTemplates>
            </ItemsControl>
            <TextBlock Classes="hint" Text="DataType 匹配子类（Circle 也是 Shape），而且不比较「谁更具体」，只看声明顺序。这一点和样式的类型选择器正好相反：Style Selector=&quot;Shape&quot; 不会命中子类。" />

            <TextBlock Classes="caption" Text="3. 模板集合沿逻辑树向上查找" />
            <TextBlock Classes="hint" Text="ItemsControl.DataTemplates 找不到匹配时，会继续查父元素、页面、Window，最后是 App.axaml 里的 Application.DataTemplates。「复用模板」页利用的正是这一点。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/TemplateCollectionPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class TemplateCollectionPage : UserControl
    {
        public TemplateCollectionPage()
        {
            InitializeComponent();

            Shape[] shapes =
            [
                new Circle { Name = "圆", Radius = 3 },
                new Square { Name = "方", Side = 4 },
                new Triangle { Name = "三角形", Base = 5 },
            ];

            SpecificFirst.ItemsSource = shapes;
            BaseFirst.ItemsSource = shapes;
        }
    }
}
```

- [ ] **Step 6: 创建 SelectorPage（迁移旧 DataTemplateDemo 的主体）**

旧项目 `MainWindow.axaml` 第 1 节（男红女黄的横排 ListBox）原样迁移，只把模板选择器从
`Window.DataTemplates` 挪到 `ListBox.DataTemplates`——作用域缩小到用它的那个控件。

`Avalonia.DataTemplatesDemo/Views/Pages/SelectorPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.DataTemplatesDemo.Models"
             xmlns:templates="using:Avalonia.DataTemplatesDemo.DataTemplates"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.SelectorPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="模板选择器：同一类型的数据，按属性值换模板"
                           DocPath="data-templates/data-template-collection" />

        <TextBlock Classes="caption" Text="男性用红色模板，女性用黄色模板" />
        <ListBox Name="PeopleList">
            <ListBox.DataTemplates>
                <templates:PersonDataTemplateSelector>
                    <templates:PersonDataTemplateSelector.MaleDataTemplate>
                        <DataTemplate x:DataType="models:Person">
                            <StackPanel Classes="male" Orientation="Horizontal" Background="#E8564A" Spacing="8">
                                <TextBlock VerticalAlignment="Center" FontSize="40" Text="{Binding Id}" />
                                <StackPanel>
                                    <TextBlock FontSize="20" Text="{Binding Name}" />
                                    <TextBlock FontSize="20" Text="{Binding Address}" />
                                </StackPanel>
                            </StackPanel>
                        </DataTemplate>
                    </templates:PersonDataTemplateSelector.MaleDataTemplate>
                    <templates:PersonDataTemplateSelector.FemaleDataTemplate>
                        <DataTemplate x:DataType="models:Person">
                            <StackPanel Classes="female" Orientation="Horizontal" Background="#E8D24A" Spacing="8">
                                <TextBlock VerticalAlignment="Center" FontSize="40" Foreground="Black" Text="{Binding Id}" />
                                <StackPanel>
                                    <TextBlock FontSize="20" Foreground="Black" Text="{Binding Name}" />
                                    <TextBlock FontSize="20" Foreground="Black" Text="{Binding Address}" />
                                </StackPanel>
                            </StackPanel>
                        </DataTemplate>
                    </templates:PersonDataTemplateSelector.FemaleDataTemplate>
                </templates:PersonDataTemplateSelector>
            </ListBox.DataTemplates>
            <ListBox.Styles>
                <Style Selector="ListBoxItem">
                    <Setter Property="Height" Value="80" />
                </Style>
            </ListBox.Styles>
            <ListBox.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal" />
                </ItemsPanelTemplate>
            </ListBox.ItemsPanel>
        </ListBox>

        <Border Classes="stage" Padding="10" Margin="0,12,0,0">
            <TextBlock TextWrapping="Wrap"
                       Text="WPF 有专门的 DataTemplateSelector 基类，Avalonia 没有：任何实现 IDataTemplate 的类都可以放进 DataTemplates 集合，Match 决定接不接这一项，Build 决定画成什么。PersonDataTemplateSelector 在 Build 里看 Sex 属性，把活转交给两个普通 DataTemplate。" />
        </Border>
    </StackPanel>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/SelectorPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class SelectorPage : UserControl
    {
        public SelectorPage()
        {
            InitializeComponent();

            // Same two people as the old DataTemplateDemo, plus one more so each template shows twice or more.
            PeopleList.ItemsSource = new[]
            {
                new Person { Id = 1, Name = "anyu", Address = "Beijing", Sex = Sex.Male },
                new Person { Id = 2, Name = "lff", Address = "Beijing", Sex = Sex.Female },
                new Person { Id = 3, Name = "小明", Address = "Shanghai", Sex = Sex.Male },
            };
        }
    }
}
```

旧项目的 `MainWindowViewModel` 只为承载这两条 `People` 而存在；迁移后数据直接在 code-behind
赋值，ViewModel 不迁。

- [ ] **Step 7: 创建 CodeTemplatesPage**

`Avalonia.DataTemplatesDemo/Views/Pages/CodeTemplatesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.CodeTemplatesPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="在代码里建模板：FuncDataTemplate&lt;T&gt;"
                           DocPath="data-templates/creating-data-templates-in-code" />

        <TextBlock Classes="caption" Text="1. 构造函数里用 lambda 建控件树" />
        <ListBox Name="CodeList" Height="150" />

        <TextBlock Classes="caption" Text="2. 带匹配条件：只接半径大于 20 的圆，其余落到下一个模板" />
        <ItemsControl Name="FilteredList" />

        <Border Classes="stage" Padding="10" Margin="0,12,0,0">
            <TextBlock TextWrapping="Wrap"
                       Text="不要在代码里 new DataTemplate：那是给 XAML 编译器用的类型，它的 Content 期望编译器生成的模板结果，塞一个控件进去会在运行时抛 InvalidCastException。代码里一律用 Avalonia.Controls.Templates 下的 FuncDataTemplate&lt;T&gt;。" />
        </Border>
    </StackPanel>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/CodeTemplatesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.DataTemplatesDemo.Models;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class CodeTemplatesPage : UserControl
    {
        public CodeTemplatesPage()
        {
            InitializeComponent();

            Circle[] circles =
            [
                new() { Name = "小", Radius = 12 },
                new() { Name = "中", Radius = 24 },
                new() { Name = "大", Radius = 36 },
            ];

            // The second lambda argument is the name scope; unused here.
            CodeList.ItemTemplate = new FuncDataTemplate<Circle>((circle, _) => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new Ellipse
                    {
                        Fill = Brushes.Orange,
                        // Indexer syntax creates a binding against the item, like {Binding Radius}.
                        [!WidthProperty] = new Binding(nameof(Circle.Radius)),
                        [!HeightProperty] = new Binding(nameof(Circle.Radius)),
                    },
                    new TextBlock
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        [!TextBlock.TextProperty] = new Binding(nameof(Circle.Name)) { StringFormat = "代码模板：{0}" },
                    },
                },
            });
            CodeList.ItemsSource = circles;

            // A match predicate turns the template into a filter; the collection keeps looking on false.
            FilteredList.DataTemplates.Add(new FuncDataTemplate<Circle>(
                c => c.Radius > 20,
                (_, _) => new TextBlock
                {
                    Foreground = Brushes.Orange,
                    [!TextBlock.TextProperty] = new Binding(nameof(Circle.Name)) { StringFormat = "大圆模板：{0}" },
                }));
            FilteredList.DataTemplates.Add(new FuncDataTemplate<Circle>((_, _) => new TextBlock
            {
                Opacity = 0.6,
                [!TextBlock.TextProperty] = new Binding(nameof(Circle.Name)) { StringFormat = "兜底模板：{0}" },
            }));
            FilteredList.ItemsSource = circles;
        }
    }
}
```

- [ ] **Step 8: 创建 ReusePage**

`Avalonia.DataTemplatesDemo/Views/Pages/ReusePage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.DataTemplatesDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.ReusePage">

    <UserControl.Resources>
        <!--  A keyed template is not applied automatically; every user names it explicitly.  -->
        <DataTemplate x:Key="CircleCard" x:DataType="models:Circle">
            <Border Classes="card" Padding="8" CornerRadius="6" Background="#30E8974A">
                <TextBlock Text="{Binding Name, StringFormat='共享卡片：{0}'}" />
            </Border>
        </DataTemplate>
    </UserControl.Resources>

    <UserControl.DataTemplates>
        <!--  An unkeyed template in DataTemplates applies to every Square under this page.  -->
        <DataTemplate DataType="models:Square">
            <TextBlock Foreground="#4AC7E8" Text="{Binding Side, StringFormat='页面级隐式模板：边长 {0}'}" />
        </DataTemplate>
    </UserControl.DataTemplates>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="复用模板：放进资源按键引用，或放进 DataTemplates 按类型共享"
                               DocPath="data-templates/reusing-data-templates" />

            <TextBlock Classes="caption" Text="1. x:Key + StaticResource：同一个模板给三个不同的控件用" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ContentControl Name="ReuseContent" ContentTemplate="{StaticResource CircleCard}" />
                <ListBox Name="ReuseList" Width="220" ItemTemplate="{StaticResource CircleCard}" />
                <ComboBox Name="ReuseCombo" Width="220" ItemTemplate="{StaticResource CircleCard}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 不带 x:Key、放进 UserControl.DataTemplates：页内所有 Square 自动套用" />
            <StackPanel Spacing="4">
                <ContentControl Name="ImplicitA" />
                <Border Padding="8" BorderBrush="#40FFFFFF" BorderThickness="1">
                    <ContentControl Name="ImplicitB" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint" Text="第二个 Square 嵌在 Border 里，照样被找到：模板沿逻辑树向上查找。要整个应用共享，就把模板放进 App.axaml 的 Application.DataTemplates，或放进一个 ResourceDictionary 文件再 ResourceInclude。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/ReusePage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ReusePage : UserControl
    {
        public ReusePage()
        {
            InitializeComponent();

            Circle[] circles = [new() { Name = "甲" }, new() { Name = "乙" }];
            ReuseContent.Content = circles[0];
            ReuseList.ItemsSource = circles;
            ReuseCombo.ItemsSource = circles;
            ReuseCombo.SelectedIndex = 0;

            ImplicitA.Content = new Square { Name = "方 A", Side = 4 };
            ImplicitB.Content = new Square { Name = "方 B", Side = 6 };
        }
    }
}
```

- [ ] **Step 9: 创建 ViewLocator 与两组被定位的 View/ViewModel**

`Avalonia.DataTemplatesDemo/ViewLocator.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;

namespace Avalonia.DataTemplatesDemo
{
    /// <summary>
    /// Convention-based lookup: Avalonia.DataTemplatesDemo.ViewModels.FooViewModel resolves to
    /// Avalonia.DataTemplatesDemo.Views.Located.FooView. This is the same idea as the ViewLocator
    /// the Avalonia MVVM template generates, narrowed to one folder so it cannot accidentally
    /// claim the page view models used elsewhere.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            var name = data!.GetType().FullName!
                .Replace(".ViewModels.", ".Views.Located.")
                .Replace("ViewModel", "View");
            var type = Type.GetType(name);

            // A visible miss is better than a blank area: the user sees which name was tried.
            return type is null
                ? new TextBlock { Text = $"找不到视图：{name}" }
                : (Control)Activator.CreateInstance(type)!;
        }

        public bool Match(object? data) => data?.GetType().Name.EndsWith("ViewModel") == true;
    }
}
```

`Avalonia.DataTemplatesDemo/ViewModels/DashboardViewModel.cs`：

```csharp
namespace Avalonia.DataTemplatesDemo.ViewModels
{
    public class DashboardViewModel
    {
        public string Summary => "今日待办 3 项，已完成 1 项";
    }
}
```

`Avalonia.DataTemplatesDemo/ViewModels/SettingsViewModel.cs`：

```csharp
namespace Avalonia.DataTemplatesDemo.ViewModels
{
    public class SettingsViewModel
    {
        public bool DarkMode { get; set; } = true;
    }
}
```

`Avalonia.DataTemplatesDemo/Views/Located/DashboardView.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Avalonia.DataTemplatesDemo.ViewModels"
             x:Class="Avalonia.DataTemplatesDemo.Views.Located.DashboardView"
             x:DataType="vm:DashboardViewModel">
    <Border Classes="stage" Padding="16">
        <StackPanel Spacing="6">
            <TextBlock FontSize="18" Text="📊 DashboardView" />
            <TextBlock Name="SummaryText" Text="{Binding Summary}" />
        </StackPanel>
    </Border>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Located/DashboardView.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.DataTemplatesDemo.Views.Located
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }
    }
}
```

`Avalonia.DataTemplatesDemo/Views/Located/SettingsView.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Avalonia.DataTemplatesDemo.ViewModels"
             x:Class="Avalonia.DataTemplatesDemo.Views.Located.SettingsView"
             x:DataType="vm:SettingsViewModel">
    <Border Classes="stage" Padding="16">
        <StackPanel Spacing="6">
            <TextBlock FontSize="18" Text="⚙ SettingsView" />
            <CheckBox Name="DarkModeBox" Content="深色模式" IsChecked="{Binding DarkMode}" />
        </StackPanel>
    </Border>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Located/SettingsView.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.DataTemplatesDemo.Views.Located
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }
    }
}
```

被定位的 View **不**设 `DataContext`：`ContentControl` 用模板建出 View 后，会把 Content（即那个
ViewModel）自动设为 View 的 `DataContext`。

- [ ] **Step 10: 创建 ViewLocatorPage 与 ViewLocatorViewModel**

`Avalonia.DataTemplatesDemo/ViewModels/ViewLocatorViewModel.cs`：

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.DataTemplatesDemo.ViewModels
{
    /// <summary>
    /// Navigation without the view model knowing any view type: it only swaps
    /// which view model is current, and the ViewLocator picks the view.
    /// </summary>
    public partial class ViewLocatorViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboard = new();
        private readonly SettingsViewModel _settings = new();

        [ObservableProperty]
        private object _current;

        public ViewLocatorViewModel() => _current = _dashboard;

        [RelayCommand]
        private void ShowDashboard() => Current = _dashboard;

        [RelayCommand]
        private void ShowSettings() => Current = _settings;

        // No matching view exists for this one: shows the locator's fallback text.
        [RelayCommand]
        private void ShowMissing() => Current = new MissingViewModel();
    }

    public class MissingViewModel
    {
    }
}
```

`Avalonia.DataTemplatesDemo/Views/Pages/ViewLocatorPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.DataTemplatesDemo.ViewModels"
             xmlns:local="using:Avalonia.DataTemplatesDemo"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.ViewLocatorPage"
             x:DataType="vm:ViewLocatorViewModel">

    <UserControl.DataTemplates>
        <local:ViewLocator />
    </UserControl.DataTemplates>

    <StackPanel Margin="12">
        <shared:DemoHeader Title="ViewLocator：按命名约定从 ViewModel 找到 View"
                           DocPath="data-templates/view-locator" />

        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="DashboardViewModel" Command="{Binding ShowDashboardCommand}" />
            <Button Content="SettingsViewModel" Command="{Binding ShowSettingsCommand}" />
            <Button Content="MissingViewModel（没有对应视图）" Command="{Binding ShowMissingCommand}" />
        </StackPanel>

        <ContentControl Name="Host" Margin="0,12,0,0" Content="{Binding Current}" />

        <TextBlock Classes="hint" Margin="0,12,0,0"
                   Text="ViewModel 只换 Current，不引用任何 View 类型。ContentControl 拿到一个对象后查 DataTemplates，ViewLocator.Match 认出名字以 ViewModel 结尾，Build 把 ViewModels. 换成 Views.Located.、ViewModel 换成 View，再反射创建。" />
        <TextBlock Classes="hint"
                   Text="反射查找与 AOT 裁剪不兼容。正式项目若要发布为 NativeAOT，应改成显式的类型映射表。" />
    </StackPanel>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/ViewLocatorPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.ViewModels;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ViewLocatorPage : UserControl
    {
        public ViewLocatorPage()
        {
            InitializeComponent();
            DataContext = new ViewLocatorViewModel();
        }
    }
}
```

`ViewLocatorViewModel` 自己也以 `ViewModel` 结尾，但它是页面的 `DataContext`，从不作为 `Content`
交给模板系统，所以不会被 `ViewLocator` 误认。

- [ ] **Step 11: 创建 PanelsAndTreesPage**

`Avalonia.DataTemplatesDemo/Views/Pages/PanelsAndTreesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.DataTemplatesDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.PanelsAndTreesPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="另外两种模板：ItemsPanelTemplate 换容器，TreeDataTemplate 画层级"
                               DocPath="data-templates/introduction-to-data-templates" />

            <TextBlock Classes="caption" Text="1. ItemsPanelTemplate：同一组数据，换一个面板来排" />
            <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                <StackPanel Grid.Column="0">
                    <TextBlock Classes="hint" Text="默认：竖排 StackPanel（实际是虚拟化面板）" />
                    <ListBox Name="DefaultPanelList" Height="140" />
                </StackPanel>
                <StackPanel Grid.Column="1">
                    <TextBlock Classes="hint" Text="换成 WrapPanel：自动换行" />
                    <ListBox Name="WrapPanelList" Height="140">
                        <ListBox.ItemsPanel>
                            <ItemsPanelTemplate>
                                <WrapPanel />
                            </ItemsPanelTemplate>
                        </ListBox.ItemsPanel>
                    </ListBox>
                </StackPanel>
            </Grid>
            <TextBlock Classes="hint" Text="ItemsPanelTemplate 只决定「装项的那个面板」是什么，不管每项长什么样——那是 ItemTemplate 的事。换成非虚拟化面板会失去虚拟化，几千项时要注意。" />

            <TextBlock Classes="caption" Text="2. TreeDataTemplate：ItemsSource 指向子节点集合" />
            <TreeView Name="FolderTree" Height="180">
                <TreeView.ItemTemplate>
                    <TreeDataTemplate DataType="models:Folder" ItemsSource="{Binding Children}">
                        <TextBlock Text="{Binding Title, StringFormat='📁 {0}'}" />
                    </TreeDataTemplate>
                </TreeView.ItemTemplate>
            </TreeView>
            <TextBlock Classes="hint" Text="TreeDataTemplate 比 DataTemplate 多一个 ItemsSource：它是一个绑定，对每个节点求值得到它的子节点。同一个模板递归套用到每一层。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/PanelsAndTreesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;
using System.Linq;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class PanelsAndTreesPage : UserControl
    {
        public PanelsAndTreesPage()
        {
            InitializeComponent();

            var labels = Enumerable.Range(1, 12).Select(i => $"项 {i}").ToArray();
            DefaultPanelList.ItemsSource = labels;
            WrapPanelList.ItemsSource = labels;

            FolderTree.ItemsSource = new[]
            {
                new Folder("文档",
                    new Folder("工作", new Folder("2026 年报")),
                    new Folder("个人")),
                new Folder("图片",
                    new Folder("旅行")),
            };
        }
    }
}
```

- [ ] **Step 12: 创建 VersusControlTemplatePage**

`Avalonia.DataTemplatesDemo/Views/Pages/VersusControlTemplatePage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:models="using:Avalonia.DataTemplatesDemo.Models"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.DataTemplatesDemo.Views.Pages.VersusControlTemplatePage">

    <UserControl.Resources>
        <!--  ControlTemplate: the button's own chrome. Knows nothing about Circle.  -->
        <ControlTheme x:Key="PillButton" TargetType="Button">
            <Setter Property="Padding" Value="16,6" />
            <Setter Property="Template">
                <ControlTemplate TargetType="Button">
                    <Border Name="Chrome" Background="#30E8974A" BorderBrush="#E8974A" BorderThickness="2"
                            CornerRadius="16" Padding="{TemplateBinding Padding}">
                        <ContentPresenter Content="{TemplateBinding Content}"
                                          ContentTemplate="{TemplateBinding ContentTemplate}" />
                    </Border>
                </ControlTemplate>
            </Setter>
        </ControlTheme>

        <!--  DataTemplate: how a Circle looks. Knows nothing about buttons.  -->
        <DataTemplate x:Key="CircleData" x:DataType="models:Circle">
            <StackPanel Orientation="Horizontal" Spacing="6">
                <Ellipse Width="14" Height="14" Fill="#4AC7E8" />
                <TextBlock Text="{Binding Name}" />
            </StackPanel>
        </DataTemplate>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DataTemplate 与 ControlTemplate：一个画数据，一个画控件"
                               DocPath="data-templates/introduction-to-data-templates" />

            <TextBlock Classes="caption" Text="四个按钮，Content 都是同一个 Circle 对象" />
            <Grid ColumnDefinitions="Auto,Auto" RowDefinitions="Auto,Auto" ColumnSpacing="24" RowSpacing="12">
                <StackPanel Grid.Row="0" Grid.Column="0" Spacing="4">
                    <TextBlock Classes="hint" Text="两样都不换" />
                    <Button Name="Neither" />
                </StackPanel>
                <StackPanel Grid.Row="0" Grid.Column="1" Spacing="4">
                    <TextBlock Classes="hint" Text="只换 DataTemplate" />
                    <Button Name="DataOnly" ContentTemplate="{StaticResource CircleData}" />
                </StackPanel>
                <StackPanel Grid.Row="1" Grid.Column="0" Spacing="4">
                    <TextBlock Classes="hint" Text="只换 ControlTemplate" />
                    <Button Name="ControlOnly" Theme="{StaticResource PillButton}" />
                </StackPanel>
                <StackPanel Grid.Row="1" Grid.Column="1" Spacing="4">
                    <TextBlock Classes="hint" Text="两样都换" />
                    <Button Name="Both" Theme="{StaticResource PillButton}" ContentTemplate="{StaticResource CircleData}" />
                </StackPanel>
            </Grid>

            <Border Classes="stage" Padding="10" Margin="0,12,0,0">
                <TextBlock TextWrapping="Wrap"
                           Text="ControlTemplate 决定按钮的外框（Fluent 的方角灰底 → 药丸形橙边），对谁都一样；DataTemplate 决定框里的内容怎么画（类型全名 → 圆点加名字）。二者通过 ControlTemplate 里的 ContentPresenter 接头：它把 Content 交给 ContentTemplate 去画。自定义 ControlTemplate 时漏写 ContentTemplate=&quot;{TemplateBinding ContentTemplate}&quot;，DataTemplate 就会失效。" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.DataTemplatesDemo/Views/Pages/VersusControlTemplatePage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class VersusControlTemplatePage : UserControl
    {
        public VersusControlTemplatePage()
        {
            InitializeComponent();

            var circle = new Circle { Name = "同一个圆", Radius = 7 };
            foreach (var button in new[] { Neither, DataOnly, ControlOnly, Both })
            {
                button.Content = circle;
            }
        }
    }
}
```

- [ ] **Step 13: 在 MainWindow 挂 9 个 Tab**

`Avalonia.DataTemplatesDemo/Views/MainWindow.axaml`：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:pages="using:Avalonia.DataTemplatesDemo.Views.Pages"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.DataTemplatesDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Data Templates Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <TabControl Margin="12">
        <TabItem Header="控件内容">
            <pages:ControlContentPage />
        </TabItem>
        <TabItem Header="内联模板">
            <pages:ContentTemplatesPage />
        </TabItem>
        <TabItem Header="模板集合">
            <pages:TemplateCollectionPage />
        </TabItem>
        <TabItem Header="选择器">
            <pages:SelectorPage />
        </TabItem>
        <TabItem Header="代码建模板">
            <pages:CodeTemplatesPage />
        </TabItem>
        <TabItem Header="复用">
            <pages:ReusePage />
        </TabItem>
        <TabItem Header="ViewLocator">
            <pages:ViewLocatorPage />
        </TabItem>
        <TabItem Header="面板与树">
            <pages:PanelsAndTreesPage />
        </TabItem>
        <TabItem Header="对比 ControlTemplate">
            <pages:VersusControlTemplatePage />
        </TabItem>
    </TabControl>
</Window>
```

- [ ] **Step 14: 构建**

Run: `dotnet build Avalonia.DataTemplatesDemo/Avalonia.DataTemplatesDemo.csproj 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`，无 `error` 行。再跑 Task 1 Step 7 的警告检查命令，Expected: 无输出——
尤其确认旧项目带过来的 CS8618 已经被 `required` / 可空属性修掉。

- [ ] **Step 15: 用 headless 探针断言页面行为**

创建 `C:\Temp\tplcheck\tplcheck.csproj`：

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
        <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.DataTemplatesDemo\Avalonia.DataTemplatesDemo.csproj" />
    </ItemGroup>
</Project>
```

创建 `C:\Temp\tplcheck\Program.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.DataTemplatesDemo.ViewModels;
using Avalonia.DataTemplatesDemo.Views.Pages;
using Avalonia.DataTemplatesDemo.Views;
using Avalonia.Headless;
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
        var w = new Window { Content = c, Width = 900, Height = 900 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Pump() => Dispatcher.UIThread.RunJobs();

    private static string Texts(Visual root)
        => string.Join(",", root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrEmpty(t)));

    private static void Check(string label, bool ok, string detail)
        => Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-38} {detail}");

    // The one artefact no page probe touches: the TabControl wiring in MainWindow.axaml.
    // A tab pointing at the wrong page still builds with zero errors.
    private static void RunShell()
    {
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        Check("Shell: tab count and headers", tabs.Count == 9
            && tabs.Select(t => t.Header as string).SequenceEqual(new[] { "控件内容", "内联模板", "模板集合", "选择器", "代码建模板", "复用", "ViewLocator", "面板与树", "对比 ControlTemplate" }),
            string.Join(",", tabs.Select(t => t.Header)));
        for (var i = 0; i < tabs.Count && i < 9; i++)
        {
            tabs[i].IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            var page = tabs[i].Content?.GetType().Name ?? "<null>";
            Check($"Shell: tab {i} renders its page", page == PageNames[i], page);
        }
    }

    private static readonly string[] PageNames = { "ControlContentPage", "ContentTemplatesPage", "TemplateCollectionPage", "SelectorPage", "CodeTemplatesPage", "ReusePage", "ViewLocatorPage", "PanelsAndTreesPage", "VersusControlTemplatePage" };

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        RunShell();

        // ControlContentPage
        var cc = new ControlContentPage();
        Layout(cc);
        Check("Content: string becomes text", Texts(Find<ContentControl>(cc, "StringContent")) == "一段普通字符串", "");
        var plain = Texts(Find<ContentControl>(cc, "ObjectContent"));
        Check("Content: object shows ToString()", plain == "Avalonia.DataTemplatesDemo.Models.Circle", plain);
        Check("Content: Button same rule", Texts(Find<Button>(cc, "ObjectButton")) == plain, Texts(Find<Button>(cc, "ObjectButton")));
        Check("Content: ContentTemplate takes over", Texts(Find<ContentControl>(cc, "TemplatedContent")).Contains("ContentTemplate"), "");

        // ContentTemplatesPage
        var ct = new ContentTemplatesPage();
        Layout(ct);
        var ellipseW = Find<ContentControl>(ct, "SingleCircle").GetVisualDescendants().OfType<Ellipse>().First().Width;
        Check("Inline: ContentTemplate binds Radius", ellipseW == 40, $"ellipse={ellipseW}");
        Check("Inline: ItemTemplate per item", Texts(Find<ListBox>(ct, "CircleList")) == "小,中,大", Texts(Find<ListBox>(ct, "CircleList")));

        // TemplateCollectionPage
        var tc = new TemplateCollectionPage();
        Layout(tc);
        var specific = Texts(Find<ItemsControl>(tc, "SpecificFirst"));
        Check("Collection: specific-first", specific.StartsWith("● 圆") && specific.Contains("■ 方") && specific.Contains("（通用 Shape 模板）三角形"),
            specific);
        var baseFirst = Texts(Find<ItemsControl>(tc, "BaseFirst"));
        Check("Collection: base-first swallows all", !baseFirst.Contains("●"), baseFirst);

        // SelectorPage
        var sp = new SelectorPage();
        Layout(sp);
        var list = Find<ListBox>(sp, "PeopleList");
        var male = list.GetVisualDescendants().OfType<StackPanel>().Count(p => p.Classes.Contains("male"));
        var female = list.GetVisualDescendants().OfType<StackPanel>().Count(p => p.Classes.Contains("female"));
        Check("Selector: male/female split", male == 2 && female == 1, $"male={male} female={female}");
        Check("Selector: horizontal panel", list.ItemsPanelRoot is StackPanel { Orientation: Avalonia.Layout.Orientation.Horizontal },
            list.ItemsPanelRoot?.GetType().Name ?? "null");

        // CodeTemplatesPage
        var cp = new CodeTemplatesPage();
        Layout(cp);
        Check("Code: FuncDataTemplate", Texts(Find<ListBox>(cp, "CodeList")) == "代码模板：小,代码模板：中,代码模板：大",
            Texts(Find<ListBox>(cp, "CodeList")));
        Check("Code: match predicate", Texts(Find<ItemsControl>(cp, "FilteredList")) == "兜底模板：小,大圆模板：中,大圆模板：大",
            Texts(Find<ItemsControl>(cp, "FilteredList")));

        // ReusePage
        var rp = new ReusePage();
        Layout(rp);
        Check("Reuse: keyed template in ContentControl", Texts(Find<ContentControl>(rp, "ReuseContent")) == "共享卡片：甲", "");
        Check("Reuse: keyed template in ListBox", Texts(Find<ListBox>(rp, "ReuseList")) == "共享卡片：甲,共享卡片：乙",
            Texts(Find<ListBox>(rp, "ReuseList")));
        Check("Reuse: implicit reaches nested", Texts(Find<ContentControl>(rp, "ImplicitB")).Contains("边长 6"),
            Texts(Find<ContentControl>(rp, "ImplicitB")));

        // ViewLocatorPage
        var vl = new ViewLocatorPage();
        Layout(vl);
        var host = Find<ContentControl>(vl, "Host");
        Check("Locator: Dashboard resolved", Texts(host).Contains("今日待办"), Texts(host));
        var vlvm = (ViewLocatorViewModel)vl.DataContext!;
        vlvm.ShowSettingsCommand.Execute(null);
        Pump();
        Check("Locator: Settings resolved", host.GetVisualDescendants().OfType<CheckBox>().Any(b => b.Name == "DarkModeBox"), Texts(host));
        vlvm.ShowMissingCommand.Execute(null);
        Pump();
        Check("Locator: missing view message", Texts(host).StartsWith("找不到视图"), Texts(host));

        // PanelsAndTreesPage
        var pt = new PanelsAndTreesPage();
        Layout(pt);
        Check("Panels: WrapPanel swapped in", Find<ListBox>(pt, "WrapPanelList").ItemsPanelRoot is WrapPanel,
            Find<ListBox>(pt, "WrapPanelList").ItemsPanelRoot?.GetType().Name ?? "null");
        var tree = Find<TreeView>(pt, "FolderTree");
        tree.GetVisualDescendants().OfType<TreeViewItem>().First().IsExpanded = true;
        Pump();
        Check("Trees: children after expand", Texts(tree).Contains("📁 工作"), Texts(tree));

        // VersusControlTemplatePage
        var vs = new VersusControlTemplatePage();
        Layout(vs);
        string Shape(string n)
        {
            var b = Find<Button>(vs, n);
            var chrome = b.GetVisualDescendants().Any(x => (x as Control)?.Name == "Chrome");
            var data = b.GetVisualDescendants().OfType<Ellipse>().Any();
            return $"{(chrome ? "C" : "-")}{(data ? "D" : "-")}";
        }
        var quad = string.Join(" ", new[] { "Neither", "DataOnly", "ControlOnly", "Both" }.Select(Shape));
        Check("Versus: four quadrants", quad == "-- -D C- CD", quad);

        Console.WriteLine($"\nwarning-or-worse log entries: {sink.Entries.Count}");
        foreach (var e in sink.Entries.Distinct()) Console.WriteLine("  " + e);
    }
}
```

Run: `dotnet run --project C:\Temp\tplcheck\tplcheck.csproj`

Expected: 22 行全部 `PASS`，且 `warning-or-worse log entries: 0`。

- [ ] **Step 16: 清理探针并提交**

```bash
rm -rf /c/Temp/tplcheck
git add Avalonia.DataTemplatesDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Data Templates category

Nine pages covering default content rendering, inline templates,
DataType-matched template collections, an IDataTemplate selector,
FuncDataTemplate in code, keyed and implicit template reuse, a
convention-based ViewLocator, items panels and tree templates, and a
side-by-side comparison with ControlTemplate.

The selector page migrates the old DataTemplateDemo's main example;
Person now uses required members, clearing its CS8618 warnings.

Verified with a throwaway headless probe: templates match subclasses
and resolve in declaration order, the selector splits by Sex, the
locator resolves both views and reports the missing one, and all four
DataTemplate/ControlTemplate combinations render as described.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: 收尾 — 删除旧项目、README、spec 回写

**Files:**
- Delete: `Avalonia.DataTemplateDemo/`（整个目录）
- Modify: `hello-avalonia.slnx`
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**Interfaces:**
- Consumes: Task 2 与 Task 4 已完成迁移
- Produces: 无代码产物

- [ ] **Step 1: 核对迁移完整性，再删除旧项目**

旧项目的每一块内容都必须在新项目里有去处。逐条核对：

| 旧项目里的内容 | 新去处 |
|---|---|
| `Resources/ButtonStyles.axaml`（`Button.Call` / `Button.Icon`） | `Avalonia.StylingDemo/Styles/ButtonStyles.axaml`（类名改小写） |
| `MainWindow.axaml` 的 `ButtonStyle` ControlTheme | StylingDemo「ControlTheme」页第 1 节 `YellowButtonTheme` |
| `MainWindow.axaml` 的 `SharedFlyout` + `FlyoutPresenter` 嵌套样式 | StylingDemo「选择器」页第 7 节 |
| `DataTemplates/PersonDataTemplateSelector.cs` | `Avalonia.DataTemplatesDemo/DataTemplates/PersonDataTemplateSelector.cs` |
| `Models/Person.cs` | `Avalonia.DataTemplatesDemo/Models/Person.cs`（`required` 修 CS8618） |
| `MainWindow.axaml` 的选择器 ListBox（横排、Height=80） | DataTemplatesDemo「选择器」页 |
| `ViewModels/MainWindowViewModel.cs` 的 `People` | `SelectorPage.axaml.cs` 里直接赋值（不再需要 ViewModel） |

```bash
grep -q "YellowButtonTheme" Avalonia.StylingDemo/Views/Pages/ControlThemesPage.axaml && \
grep -q "FlyoutPresenter" Avalonia.StylingDemo/Views/Pages/SelectorsPage.axaml && \
grep -q "Button.call" Avalonia.StylingDemo/Styles/ButtonStyles.axaml && \
test -f Avalonia.DataTemplatesDemo/DataTemplates/PersonDataTemplateSelector.cs && \
grep -q "PersonDataTemplateSelector" Avalonia.DataTemplatesDemo/Views/Pages/SelectorPage.axaml && \
echo "migration complete"
```

Expected: `migration complete`。任何一项缺失就回到对应任务补上，**不要**先删。

```bash
git rm -r -q Avalonia.DataTemplateDemo
git status --short
```

**只跑 `git rm`，不要追加 `rm -rf Avalonia.DataTemplateDemo`。** 受版本控制的内容由 `git rm`
删除，这一步在 git 里完全可恢复（`git checkout HEAD~1 -- Avalonia.DataTemplateDemo`）；
目录里剩下的 `bin/`、`obj/` 不在版本控制内，属于本地残留，留给用户手动清理。
`rm -rf` 不可恢复，用户此前已明确拒绝过这类命令，不要用别的写法绕过。

修改 `hello-avalonia.slnx`，删掉 `Avalonia.DataTemplateDemo` 那一行：

```xml
<Solution>
  <Project Path="Avalonia.DataBindingDemo/Avalonia.DataBindingDemo.csproj" />
  <Project Path="Avalonia.DataTemplatesDemo/Avalonia.DataTemplatesDemo.csproj" />
  <Project Path="Avalonia.FundamentalsDemo/Avalonia.FundamentalsDemo.csproj" />
  <Project Path="Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj" />
  <Project Path="Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj" />
  <Project Path="Avalonia.MusicStore/Avalonia.MusicStore.csproj" />
  <Project Path="Avalonia.PropertySystemDemo/Avalonia.PropertySystemDemo.csproj" />
  <Project Path="Avalonia.Shared/Avalonia.Shared.csproj" />
  <Project Path="Avalonia.StylingDemo/Avalonia.StylingDemo.csproj" />
  <Project Path="Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj" />
  <Project Path="Avalonia.XamlDemo/Avalonia.XamlDemo.csproj" />
</Solution>
```

- [ ] **Step 2: 更新 README 项目表格，按官方分类顺序重排**

spec 的收尾要求是「`README.md` 项目表格（按官方分类顺序重排）」，不是只增删行。当前表格
的顺序是历史堆积的结果（MusicStore / WebViewDemo / HtmlRendererDemo / DataTemplateDemo /
Layout / Fundamentals / Xaml / PropertySystem / Shared），既不是分类顺序，也没有把工具类项目
和分类类项目分开。本步一次做完增、删、重排三件事。

**重排后的顺序**，即 spec 项目清单表里 #1–#6、#7 的官方分类次序，随后是三个工具类项目，
`Avalonia.Shared` 固定排在最后：

```markdown
| 项目 | 演示内容 |
|---|---|
| [Avalonia.FundamentalsDemo](Avalonia.FundamentalsDemo) | 纯代码 UI、code-behind 与 MVVM 对照、TopLevel、视觉树与逻辑树、应用生命周期 |
| [Avalonia.XamlDemo](Avalonia.XamlDemo) | 命名空间、x: 指令、标记扩展、类型转换器、泛型、XAML 编译 |
| [Avalonia.LayoutDemo](Avalonia.LayoutDemo) | 8 种布局面板对照、对齐与 Margin/Padding、四种响应式手段 |
| [Avalonia.StylingDemo](Avalonia.StylingDemo) | 选择器语法、样式类、伪类、ControlTheme、主题变体、嵌入字体、样式共享 |
| [Avalonia.DataBindingDemo](Avalonia.DataBindingDemo) | 绑定语法与模式、编译绑定、集合与主从、多值绑定、命令、转换器、校验、集合视图、异步绑定、绑定调试 |
| [Avalonia.DataTemplatesDemo](Avalonia.DataTemplatesDemo) | 内联模板、按类型匹配、模板选择器、代码建模板、复用、ViewLocator、面板与树模板 |
| [Avalonia.PropertySystemDemo](Avalonia.PropertySystemDemo) | StyledProperty / DirectProperty / 附加属性、值优先级、元数据与回调 |
| [Avalonia.MusicStore](Avalonia.MusicStore) | 专辑搜索（iTunes API）、购买、本地缓存、RESX 多语言 |
| [Avalonia.WebViewDemo](Avalonia.WebViewDemo) | NativeWebView 嵌入控件、NativeWebDialog 原生窗口、JS ↔ C# 双向调用 |
| [Avalonia.HtmlRendererDemo](Avalonia.HtmlRendererDemo) | HtmlPanel 富文本渲染、IconFont 与 PathIcon 图标 |
| [Avalonia.Shared](Avalonia.Shared) | 共享类库：演示页说明条控件、窗口 Helper、Win32 互操作、消息载体、ViewModel 基类 |
```

`Avalonia.DataTemplateDemo` 那一行随之消失（它已被拆分）。表格本身的行内容除新增三行外
**逐字不动**——「按分类顺序重排」改的是行的次序，不是描述文案。

后续两个 plan（交互图形层 #8–#11、应用服务层 #12–#15）追加新项目时，按同一顺序插到
`Avalonia.PropertySystemDemo` 与其后之间，而不是继续往表尾堆。

Run: `grep -c "DataTemplateDemo" README.md hello-avalonia.slnx`
Expected: 两个文件都是 `0`（新项目名 `DataTemplatesDemo` 在 `Template` 后多一个 `s`，不含这个子串）。

Run: `grep -n "^| \[" README.md`
Expected: 输出顺序与上表一致，共 11 行。

- [ ] **Step 3: 回写实测结论到 spec**

在 spec 的「基础层实测结论（2026-09-22）」小节之后，新增一节。**把 Task 2–4 探针的实际输出
与这里的说法逐条对照**；若执行中有任何一条与下文不符，以探针输出为准改写，并在该条末尾注明
"（执行期修正）"：

```markdown
### 样式绑定层实测结论（2026-10-07）

三个项目（#4 Styling、#5 DataBinding、#6 DataTemplates）落地过程中验证到的结果：

- **功能点映射的出入**：Styling 官方 16 个子页合成 9 个 Tab，容器查询与值优先级改为指路页；
  Data Binding 官方 24 个子页，spec 漏了「异步与图片」，`binding-classes` 放进 #4；
  Data Templates 官方 7 个子页，spec 漏了 `control-content`、`creating-data-templates-in-code`、
  `reusing-data-templates`、`view-locator` 四页。旧 `Avalonia.DataTemplateDemo` 已拆入 #4 与 #6 并删除。
- **两条"类型匹配"规则方向相反**：样式的类型选择器**不**匹配子类（`Button` 不命中
  `ToggleButton`，要用 `:is(Button)`）；`DataTemplate DataType` **匹配**子类，并按声明顺序取
  第一个——基类模板写在前面会截走所有子类。
- **样式优先级再确认**：多个类冲突时比样式声明顺序，与 `Classes` 书写顺序无关；ControlTheme
  的 Setter 低于任何 Style Setter；子类控件复用父类外观要用 `ControlTheme BasedOn`，
  `StyleKeyOverride` 会让 `子类:伪类` 选择器静默失效。
- **新增五种静默失败**：`ThemeDictionaries` 里的键用 `StaticResource` 取得 null；
  `?.` 吞掉绑定警告的同时让 `FallbackValue` 失效（改配 `TargetNullValue`）；
  标记扩展参数里以 `{` 开头的单引号字符串仍被当成嵌套扩展（要加 `{}`）；
  `required` 成员在 XAML 实例化时不受检查；setter 抛普通异常时界面显示带类型名前缀的错误
  文本（要抛 `DataValidationException`）。
- **转换器不适合报错**：`ConvertBack` 返回 `BindingNotification` 或抛异常，界面看到的都是框架
  生成的 `InvalidCastException` 文本。转换失败返回 `BindingOperations.DoNothing`，校验放进 ViewModel。
- **`^` 绑定的时序**：替换 `Task` 属性后，新任务完成前界面停在旧结果，不回到 `FallbackValue`；
  `IObservable` 订阅只在 `DataContext` 变化时释放，页面离开可视树时不释放。
- **探针写法的两个坑**：`RaiseEvent(ClickEvent)` 不执行 `Command`（要 `Command.Execute(CommandParameter)`）；
  headless 默认后端的 `Bitmap` 一律 1×1。Flyout 的 Presenter 在独立的弹出层里，要从
  `Flyout.Content` 向上找，从窗口向下找不到。
- **页面级日志 sink 可以串联**：装一个只截 `LogArea.Binding` 的 sink、其余转交原 sink，
  `LogToTrace` 的输出不受影响。绑定调试页用这个把错误显示在界面上。
```

- [ ] **Step 4: 全量构建与最终验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "StylingDemo|DataBindingDemo|DataTemplatesDemo" | grep -v MSB3884`
Expected: 无输出。

Run: `git status --short`
Expected: 只有本任务的删除与 `README.md`、`hello-avalonia.slnx`、`docs/` 改动，无遗留探针目录或临时文件。

逐个启动三个项目确认能打开（烟雾测试，不替代各任务的探针断言）。三个都是 WinExe，
`dotnet run` 会**阻塞在 GUI 进程上直到窗口关闭**，所以每条都要加超时，并且必须真的关掉窗口
再跑下一条——否则命令永远不返回：

```bash
timeout 60 dotnet run --project Avalonia.StylingDemo
timeout 60 dotnet run --project Avalonia.DataBindingDemo
timeout 60 dotnet run --project Avalonia.DataTemplatesDemo
```

判定标准（每条都逐项确认，不要只看"没报错"）：

1. 窗口真的出现，不是启动即退出——标题栏分别应为 `Avalonia Styling Demo`、
   `Avalonia Data Binding Demo`、`Avalonia Data Templates Demo`
2. 逐个点一遍全部 Tab（9 / 12 / 9 个），每个都切得动、都有内容，没有空白页
3. 每页标识区应显示中文说明与官方文档路径

`timeout` 返回 `124` 表示窗口一直开着直到超时被杀——这是**正常**的（说明它没崩溃），
不代表失败；返回其他非零值、或根本没有窗口出现，才要停下来查。

窗口标题以 Task 1 写进 `MainWindow.axaml` 的为准（三个项目的 `Title` 分别是
`Avalonia Styling Demo`、`Avalonia Data Binding Demo`、`Avalonia Data Templates Demo`）。
若标题对不上，说明 Task 1 的文件没照 brief 写，回去修 Task 1 而不是改这里的期望值。

- [ ] **Step 5: 提交**

```bash
git add -A Avalonia.DataTemplateDemo README.md hello-avalonia.slnx docs/
git commit -m "$(cat <<'EOF'
docs: register the styling-and-binding demos and retire DataTemplateDemo

Everything the old DataTemplateDemo showed now lives in StylingDemo
(button styles, the custom ControlTheme, the styled Flyout) or
DataTemplatesDemo (the Person template selector), so the old project
is removed from the solution and the README.

The spec gains this group's findings, chiefly that type selectors in
styles skip subclasses while DataTemplate DataType matching includes
them, and five more failure modes that compile and run without a word.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
)"
```

完成后停下，交回用户 review。交互图形层（#8–#11）与应用服务层（#12–#15）各写独立 plan。
