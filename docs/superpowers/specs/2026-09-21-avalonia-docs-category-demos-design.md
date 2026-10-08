# 按官方文档分类构建演示项目集设计

日期：2026-09-21
状态：已批准，待实施

## 背景

`hello-avalonia` 目前有 4 个演示项目 + 1 个共享类库，它们来自 `Avalonia.MusicStore` 的
主题拆分（见 `2026-09-20-musicstore-split-design.md`）。这批项目的划分依据是"原项目里
恰好有哪些功能"，而不是一套可对照的知识体系——学习者无法据此判断"Avalonia 有哪些能力、
我学到哪了"。

Avalonia 官方文档（<https://docs.avaloniaui.net/docs/welcome>）的侧边栏已经给出了一套
成熟的能力分类。以它为骨架建立演示项目集，可以让仓库从"零散示例堆"变成"可对照官方文档
逐项验证的学习路径"。

## 目标

1. 为官方文档中 14 个可演示分类各建一个独立可运行的演示项目
2. 每个项目以 TabControl 组织该分类下的功能点，每个功能点配中文说明与英文代码注释
3. 拆分现有 `Avalonia.DataTemplateDemo`，消除与新分类项目的内容重叠

## 非目标

- 不覆盖纯说明性分类（Welcome、Supported platforms、Getting Started、Deployment、
  Migration、Breaking changes、Samples & Tutorials、How-To Guides）
- 不重构现有的 MusicStore / WebViewDemo / HtmlRendererDemo 三个项目
- 不为演示项目引入自动化测试（`Avalonia.TestingDemo.Tests` 例外，它本身就是测试演示）
- 不引入 ReactiveUI 等第三方 MVVM 框架，演示官方分类只用官方 API + 已有的
  CommunityToolkit.Mvvm

## 分类取舍

官方侧边栏共 22 个顶层分类。筛选标准是"能否做成可交互的运行时演示"：

**纳入（14 个）**：Fundamentals、XAML Reference、Layout、Styling、Data Binding、
Data Templates、Property System、Events、Input & Interaction、Graphics and Animation、
Custom controls、Services、App Development、Testing

**排除（8 个）**：Welcome、Supported platforms（平台说明表）、Getting Started（安装
与 IDE 配置）、Deployment（打包发布流程）、Migration（WPF 对照速查）、Breaking changes
（版本变更列表）、Samples & Tutorials（已由 `Avalonia.MusicStore` 承担）、
How-To Guides（其内容分散在上述分类中，不单独成项目）

## 统一项目骨架

每个演示项目沿用现有 `Avalonia.DataTemplateDemo` 的形态（WinExe + net10.0 + Fluent
主题 + 引用 `Avalonia.Shared`），在此基础上增加页面分层。项目 #15 是例外：它是测试项目
（非 WinExe），引用 #14 而非 `Avalonia.Shared`。

```
Avalonia.XxxDemo/
├── App.axaml(.cs)              Fluent 主题 + 全局 StyleInclude
├── Program.cs                  BuildAvaloniaApp
├── app.manifest
├── Assets/avalonia-logo.ico
├── Views/
│   ├── MainWindow.axaml(.cs)   仅承载 TabControl 外壳
│   └── Pages/                  每个功能点一个 UserControl
│       ├── XxxPage.axaml(.cs)
│       └── ...
├── ViewModels/                 与 Pages 一一对应（无状态页可省略）
├── Models/                     该分类演示所需的数据模型
└── Avalonia.XxxDemo.csproj
```

**为什么每个功能点单独成 UserControl**：Data Binding 分类有 24 个官方子页、Graphics
and Animation 有 21 个。若全部塞进单个 `MainWindow.axaml`，文件会膨胀到上千行，既难以
阅读，也让"想看某一个功能点怎么写"的读者必须在长文件里翻找。一个功能点一个文件，文件名
即索引。

**页面说明条**：每个 Page 顶部固定一个说明区域，内容为中文的功能点描述 + 对应官方文档
路径，格式如：

```
布局面板选择 — docs/layout/choosing-a-layout-panel
Avalonia 提供 8 种内置面板，本页对照它们在相同内容下的排布差异。
```

这让演示页与官方文档可双向对照，读者看到某个效果能立刻回查原文。

**功能点粒度**：官方子页多的分类按主题归并，单个项目控制在 3–13 个 Tab。不做成一个子页
一个 Tab——那样 Data Binding 会有 24 个 Tab，横向滚动反而难用。下限由分类本身决定：
Layout 官方只有 3 个子页，就是 3 个 Tab，不为凑数拆分。

## 项目清单与功能点映射

| # | 项目 | Tab 分组（功能点） |
|---|---|---|
| 1 | `Avalonia.FundamentalsDemo` | Code-only UI / Code-behind / MVVM 模式 / TopLevel / UI 组合 / 视觉树与逻辑树 / 应用生命周期 / Assets 资源 |
| 2 | `Avalonia.XamlDemo` | XAML 命名空间 / x: 指令 / 标记扩展 / 类型转换器 / XAML 泛型 / 编译型 XAML |
| 3 | `Avalonia.LayoutDemo` | 布局面板对照（Grid/DockPanel/StackPanel/WrapPanel/UniformGrid/RelativePanel/Canvas/Panel）/ 定位对齐与 Margin-Padding / 响应式布局 |
| 4 | `Avalonia.StylingDemo` | Style 与选择器语法 / 样式类 / 伪类 / ControlTheme / 主题与 ThemeVariant / 容器查询 / 自定义字体与排版 / 样式共享 / 属性值优先级 |
| 5 | `Avalonia.DataBindingDemo` | 绑定语法与 DataContext / 编译绑定 / 集合绑定 / 主从绑定 / MultiBinding / 命令与 CanExecute / 值转换器 / 数据校验 / 排序筛选分组 / 绑定调试 |
| 6 | `Avalonia.DataTemplatesDemo` | DataTemplate 基础 / DataType 隐式匹配 / IDataTemplate 选择器 / ItemsPanelTemplate / TreeDataTemplate / ControlTemplate 对照 |
| 7 | `Avalonia.PropertySystemDemo` | StyledProperty / DirectProperty / AttachedProperty / 值优先级 / 元数据与变更回调 / 值继承 |
| 8 | `Avalonia.EventsDemo` | 生命周期事件 / 输入事件 / 路由事件三阶段（Tunnel-Bubble-Direct）/ Handled 拦截 / 自定义路由事件 |
| 9 | `Avalonia.InputDemo` | 指针设备 / 焦点与 FocusManager / 手势 / 键盘与 HotKey / 命令绑定 / 拖放 / 文本输入与 IME |
| 10 | `Avalonia.GraphicsDemo` | 画刷 / 渐变 / 变换（Render vs Layout）/ 形状与几何 / DrawingContext 自定义绘制 / 关键帧动画 / 过渡 / 页面过渡 / 缓动函数 / 特效 / 裁剪遮罩 / 命中测试 / 图标 |
| 11 | `Avalonia.CustomControlsDemo` | UserControl / TemplatedControl / 自定义绘制控件 / 定义属性 / 定义事件 / 自定义 Panel / 自定义 Flyout |
| 12 | `Avalonia.ServicesDemo` | 剪贴板 / 文件对话框 / StorageProvider / FocusManager / Launcher / PlatformSettings / InputPane |
| 13 | `Avalonia.AppDevelopmentDemo` | 依赖注入 / ResX 本地化 / 嵌入 Web 内容（指路页）/ 日志 / 未处理异常 / 资源字典 / 线程模型与 Dispatcher / 窗口管理 / 无障碍 |
| 14 | `Avalonia.TestingDemo` | 被测应用：计数器、表单校验、列表操作（刻意设计为易于断言的 UI） |
| 15 | `Avalonia.TestingDemo.Tests` | Headless xUnit：控件查询、模拟点击、模拟键盘、渲染快照断言 |

### 与现有项目的边界

**`Avalonia.DataTemplateDemo` 拆分后删除**：它的 Style/ControlTheme/Flyout 内容并入
项目 #4，`PersonDataTemplateSelector` 及其数据模型并入项目 #6。这两块在新分类项目里都
会被扩充到覆盖官方子页，保留旧项目会造成同一知识点两处实现、两处维护。

**不重复实现已覆盖的功能点**：
- App Development 的 "Embedding web content" → `Avalonia.WebViewDemo` 已完整演示
- App Development 的 "Localizing using ResX" → `Avalonia.MusicStore` 已有实现，
  项目 #13 中做一个最小版本以保证分类完整，并在说明条中指向 MusicStore 的完整用法

这些页面保留 Tab 位置（以免读者以为分类漏了），内容为一句话说明 + 指向对应项目的路径。

**分类之间的去重规则**：一个功能点只在最贴合的分类里完整实现，其他分类若也列出该页，
做成指路页。已识别的跨分类重复：

- "Data validation" 同时出现在 Data Binding 和 App Development → 完整实现放 #5
- "Property value precedence" 同时出现在 Styling 和 Property System → 完整实现放 #7，
  #4 中做指路页
- "Markup extensions" 同时出现在 XAML Reference 和 Data Binding → 完整实现放 #2
- "Focus Manager" 同时出现在 Services 和 Input & Interaction → 完整实现放 #9

**App Development 中不做成 Tab 的子页**：XAML live previewer（IDE 功能，非运行时行为）、
Performance optimization（优化建议清单，无可交互演示）、Native platform interop
（`Avalonia.Shared/Helpers/NativeMethodHelper.cs` 已有实际用例）。

## 依赖与技术选型

新增到 `Directory.Packages.props` 的包：

| 包 | 用途 | 使用项目 |
|---|---|---|
| `Microsoft.Extensions.DependencyInjection` | DI 容器演示 | #13 |
| `Microsoft.Extensions.Logging.Console` | 日志演示 | #13 |
| `Avalonia.Headless.XUnit` | 无头 UI 测试 | #15 |
| `xunit.v3` | 测试框架（原写 `xunit`，见「应用服务层实测结论」） | #15 |
| `xunit.runner.visualstudio` | 测试运行器 | #15 |
| `Microsoft.NET.Test.Sdk` | 测试宿主 | #15 |

复用已有的 `CommunityToolkit.Mvvm`（ViewModel 与命令）和 `Avalonia.Diagnostics`
（DevTools，仅 Debug）。

**不引入第三方 UI/MVVM 框架**。本项目集的目的是演示官方文档描述的能力，引入
ReactiveUI、Prism 等会让读者分不清"这是 Avalonia 的能力"还是"这是框架的能力"。

### 语言约定

延续 README 现有约定并补充界面部分：

- **C# 与 XAML 注释**：英文
- **界面文字**（Tab 标题、说明条、按钮文案）：中文
- **标识符**（类名、属性名、x:Name）：英文

## 待验证的技术风险

以下几点无法从文档确定，在样板项目阶段先行验证，结果反馈后再批量推进：

1. **`Avalonia.Headless.XUnit` 是否有 12.1.2 版本**。Avalonia 12 发布不久，配套测试包
   可能滞后（`Avalonia.Diagnostics` 就停在 11.3.22）。**全部项目统一使用 Avalonia 12，
   不为任何项目降级到 11。** 回退顺序：先试 12.x 最新版；不可用则试与 Avalonia 12 二进制
   兼容的最近版本；仍不可用则项目 #15 不建，#14 保留为普通演示项目，并在 README 中说明
   该分类待上游发布 12.x 测试包后补齐。
2. **Container Queries 在 12.1.2 的可用性**。该特性在文档中列于 Styling 分类，但属较新
   特性，需确认 API 形态与文档一致。
3. **Services 分类的桌面平台可用性**。InputPane、InsetsManager 主要面向移动端，在
   Windows 桌面上可能返回 null。这类页面演示"如何检测服务是否可用"本身也是有价值的内容。

验证方式：在样板项目（#3 Layout，零外部依赖）跑通后，单独建一个最小 probe 验证第 1、2 点，
再决定 #4 和 #15 的最终形态。

### 样板阶段实测结论（2026-09-21）

- **容器查询**：语法可用，但有两个运行时约束，都属于"不报错、不记日志、不抛异常"的静默
  失败，必须当成硬性规则传给后续项目。`Container.Name` / `Container.Sizing` 附加属性与
  `<ContainerQuery Name="..." Query="max-width:400">` 元素在 Avalonia 12.1.2 中编译通过、
  运行时无异常，未使用任何回退方案；但初版写法实测**列数在任何宽度下恒为 1**，两个缺陷叠加：
  - **目标属性上不能写本地值。** XAML 元素属性（如 `<UniformGrid Columns="1">`）是
    `BindingPriority.LocalValue`（值 0，最高），永久压过查询里的 Setter
    （`StyleTrigger=1` / `Style=3`）。**凡打算被 Style / ContainerQuery / 伪类驱动的属性，
    一律不在元素上写"兜底默认值"**——那个本能动作会悄悄禁用你为它写的全部样式。需要兜底就
    写进普通 `<Style>`（同为 Style 优先级、声明在前，会被 trigger 覆盖）。
  - **`and` 组合符解析有缺陷，只有右侧条件生效。** 实测
    `min-width:400 and max-width:700` 的行为等同于单条 `max-width:700`，左侧被整个丢弃；
    调换顺序则等同于单条 `min-width:400`。加括号、加 `px`、写小数、大写 `AND` 五种替代写法
    均在解析阶段抛 `InvalidOperationException`。**12.1.2 没有任何可用的 `and` 写法**，区间
    必须用单条件按声明顺序层叠表达（后声明覆盖先声明，CSS 式级联）。这个"层叠而非区间"的
    模式本身就是容器查询值得演示的知识点。
  - 修复后实测（headless，读回 `UniformGrid.Columns`）：容器 375→1 列、526→2 列、
    876→4 列。注意断点落在**容器**尺寸而非窗口尺寸上——`QueryHost` 比窗口窄 24 px，这正是
    容器查询区别于 CSS media query 的地方。
- **`Avalonia.Shared` 承载 XAML 控件**：可行，需增加 `Avalonia.Themes.Fluent` 包引用
  （因 ControlTheme 用到 Fluent 资源键 `SystemControlBackgroundListLowBrush` 和
  `SystemAccentColor`）。
- **元素名绑定**（额外发现，brief 未列出）：`{Binding #ElementName.Value}` 在
  `AvaloniaUseCompiledBindingsByDefault=true` 下无需 `x:DataType`，plan 里准备的三级
  回退未用上。
- **`double` 绑定到 `Thickness` 必须显式转换器**（最终审查发现的缺陷，已修复）：
  `Thickness` 既无 `TypeConverter` 也无 `double` 转换操作符，`TargetTypeConverter`
  的转换链会一路落到 `IConvertible.ToType` 并失败。失败**不抛异常**，只记一条 binding
  error，所以"构建 0 错误 + 运行无异常堆栈"检验不出来——表现是滑块拖动时数字在变、
  被绑定的元素纹丝不动。已加 `Avalonia.Shared/Converters/DoubleToThicknessConverter.cs`
  解决。**后续项目凡是把数值绑到 `Thickness`/`CornerRadius` 等结构体属性的，都要走
  转换器。**
- **8 种布局面板语法**（额外发现，brief 未列出）：Avalonia 12.1.2 全部接受，含
  `RelativePanel` 的 8 个附加属性、裸 `<Panel>` 元素、`Width="NaN"` 覆盖 double 型
  样式属性。
- **共享的不只是控件，还有页面级样式**：`TextBlock.caption`（小节标题）与 `Border.stage`
  （演示区边框）已提到 `Avalonia.Shared/Themes/SharedStyles.axaml`。样板初版让三个页面各写
  一份，三份里就已经有一份 Margin 分叉了——按 15 项目约 120 页的规模，这类"只有几行"的样式
  会长出上百份互不一致的副本。**判断是否该共享，看的是复制次数而非代码行数。** 页面专属的
  装饰样式（如 `Border.block` / `Border.tile`）留在页面里。注意 `SharedStyles.axaml` 中普通
  `<Style>` 要作为 `<Styles>` 的直接子元素，放在 `Styles.Resources` 之外。
- **不要引用 `Avalonia.Diagnostics`**：该包停在 11.3.22，12.x 的 DevTools 已内置于主包
  （见 `Directory.Packages.props` 注释）。四个早期项目引用了它，但全仓库零处
  `AttachDevTools` 调用，只是把一个 11.x 程序集复制进 12.x 应用的输出目录。样板不沿袭，
  后续 14 个项目一律不加；早期项目按外科手术式改动原则暂不回头修改。
- 风险 1（Headless 测试包）与风险 3（Services 桌面可用性）不在样板范围，留待对应
  plan 验证。
- **验证局限与应对**：样板初版仅验证了"构建 0 错误 + 运行无异常堆栈"，**这套方法已被两次
  证明存在盲区**——`double`→`Thickness` 缺陷由最终审查静态分析发现，容器查询的两个缺陷由
  代码审查阶段的 headless 实测发现，两者都顺利通过了构建，后者连 binding 日志都没有。
  **后续项目一律改用可读回属性值的 headless 探针验证，不再依赖人工目视。** 做法很轻：
  `Avalonia.Headless` + 直接 `new` 真实页面类 + `Measure`/`Arrange` + 读回属性值，
  再挂一个过滤 `LogArea.Binding` 的 `ILogSink` 自动捕获静默绑定失败，不到 40 行。
  探针建在仓库外、跑完即弃，因此不违反"不为演示项目写自动化测试"这条约束。
  plan 里"人工目视核对"一类的验证步骤应当替换为这种可断言的检查——样板阶段
  plan Task 5 Step 5 写的"核对列数依次为 1 → 2 → 4"若真执行了，当场就会暴露上述缺陷。

### 基础层实测结论（2026-09-22）

三个项目（#1 Fundamentals、#2 XAML、#7 PropertySystem）落地过程中验证到的结果：

- **官方文档子页与 spec 功能点映射有三处出入**（已按实际调整）：Fundamentals 官方有 12 个
  子页而非 8 个，其中 `architecture` / `cross-platform-architecture` 无可交互内容、
  `avalonia-xaml` 归项目 #2；Property System 分类只有 3 个子页，三种属性的定义方式实际在
  `custom-controls/defining-properties`；XAML Reference 的 6 个子页与 spec 完全吻合。
- **编译型 XAML 的两条响亮失败**：`{x:Static}` 返回枚举时不能直接填 `string` 属性
  （`AVLN3000`），包一层 `{Binding Source=...}` 可解；`{x:Type}` 不能当 `Binding` 的
  `Source`（`AVLN2100` 要求 `x:DataType`），它的正当位置是 `ControlTheme.TargetType`。
- **`StringFormat` 里的字面花括号必须双写**（新增的静默失败类型）：单层 `{Binding}` 会被
  .NET 复合格式化当成占位符，解析失败后**静默产出空字符串**，无报错无日志。后续项目凡用
  `StringFormat` 的，headless 断言里要检查文本非空。
- **`BindingPlugins` 在 12.1.2 不是公开 API**（`CS0122`）。需要展示编译型 XAML 的产物时，
  改为反射程序集自身的 `CompiledAvaloniaXaml` 命名空间。
- **属性系统行为确认**：`coerce` 在每次写入时生效（150→100、−20→0）；
  `RegisterAttached(..., inherits: true)` 的值沿视觉树传递多层；
  附加属性选择器语法为 `Button[(ns|Owner.Prop)=True]`（前缀用 `|`，属性名加圆括号）；
  **本地值 111 确实压过样式 Setter 999**——样板阶段那个缺陷的最小复现。
- **验证方式**：三个项目各用一个仓库外的 headless 探针断言关键属性值，跑完即弃。
  这套做法在本组共拦下 3 个写 plan 阶段的错误（枚举填 string、`x:Type` 作 Source、
  `StringFormat` 花括号），全部在编写期解决，未进入实现。

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
- **`StringFormat` 以 `{0}` 开头会构建失败**（执行期发现，`AVLN2000`）：`'{0} 岁'`、
  `'{0:F1} °C'` 被 XAML 当成标记扩展，要写成 `'{}{0} 岁'`。以字面文字开头的格式串不受影响。
  这是响亮失败，与规则 3 的静默失败是两件事。
- **`Watermark` 在 12.1.2 已过时**（`AVLN5001`），统一改用 `PlaceholderText`。
- **转换器不适合报错**：`ConvertBack` 返回 `BindingNotification` 或抛异常，界面看到的都是框架
  生成的 `InvalidCastException` 文本。转换失败返回 `BindingOperations.DoNothing`，校验放进 ViewModel。
- **`^` 绑定的时序**：替换 `Task` 属性后，新任务完成前界面停在旧结果，不回到 `FallbackValue`；
  `IObservable` 订阅只在 `DataContext` 变化时释放，页面离开可视树时不释放。
- **探针写法的坑**：`RaiseEvent(ClickEvent)` 不执行 `Command`（要 `Command.Execute(CommandParameter)`）；
  headless 默认后端的 `Bitmap` 一律 1×1；命名颜色读回是名字（`Yellow`）而非十六进制；
  Flyout 的 Presenter 在独立的弹出层里，要从 `Flyout.Content` 向上找。
  进程级的日志 sink 会被「切到绑定调试页」这一动作触发，探针里遍历全部 Tab 的步骤必须排在
  统计「普通页零警告」之后。
- **页面级日志 sink 可以串联**：装一个只截 `LogArea.Binding` 的 sink、其余转交原 sink，
  `LogToTrace` 的输出不受影响。绑定调试页用这个把错误显示在界面上。
- **探针断言实际条数**：#4 为 28 条、#5 为 50 条、#6 为 31 条，全部通过且零警告日志；
  plan 里写的 19 / 38 / 22 是漏数，以实际为准。

### 交互图形层实测结论（2026-10-08）

四个项目（#8 Events、#9 Input、#10 Graphics、#11 CustomControls）落地过程中验证到的结果：

- **功能点映射的出入**：Events 官方子页合成 5 个 Tab；Input 8 个；Graphics 13 个；CustomControls 7 个，
  其中「属性与事件」是指向 #7 与 #8 的路标页。
- **去重规则落地**：路由事件主体在 #8（#9 只留路标）；焦点管理完整在 #9；定义属性在 #7
  （#11 路标）；自定义路由事件在 #8（#11 路标）。
- **新增静默失败**（plan 规则 26–28 及执行时发现的几条）：非控件对象不能命名（`AVLN2000`），
  但元素名绑定可以绑它们的属性；`GeometryCombineMode` 只有四个值；`coerce` 回调的第一个参数是
  `AvaloniaObject`；`PopupFlyoutBase` 要 `using Avalonia.Controls.Primitives`。
- **派生自现有控件要覆盖 `StyleKeyOverride`**：`Notifier : Button` 不覆盖时按自身类型找主题，
  找不到就没有模板，不渲染、点不了，既不报错也不记日志。有自己 ControlTheme 的控件（`Meter`）
  则相反，不要覆盖。
- **XAML 里的数字键会被解析成枚举数值**：`HotKey="Ctrl+2"` 注册的是 `Ctrl+Back`，`Ctrl+3` 是
  `Ctrl+Tab`；要写 `Ctrl+D2`。菜单项的 `InputGesture` 仍只显示文字、不注册按键。
- **`Canvas.Left` 默认是 `NaN`**：`DoubleTransition` 没有起点可插值，小球原地不动，不报错。
  被 Transition 驱动的附加属性要先写显式起始值（它是起点，不是会压住样式的本地值）。
- **页面里元素名不能与继承属性重名**：`Name="Opacity"` 让生成的字段遮住 `Visual.Opacity`（`CS0108`）。
- **`Handled` 只拦 `KeyDown`**：之后的 `TextInput` 仍会写进 `TextBox`，页面说明里的
  「不再收到字符」只对 KeyDown 阶段成立。
- **headless 的边界**：`Render` 会被调用，自绘控件可以断言重绘次数；无限动画只前进 1–2 帧，
  `Animation.RunAsync` 不推进，动画页的断言只落在类的切换与 `Transitions` 的终值上；
  `DoDragDropAsync` 的发起端无法验证，只测了接收端，需真机目视。
- **探针写法的坑**：`KeyPressQwerty` 只发 KeyDown/KeyUp、不发 TextInput，要补 `KeyTextInput`；
  带修饰键的组合会被 HotKey/KeyBinding 拦截，到不了控件的 KeyDown；命令禁用要断言
  `IsEffectivelyEnabled` 而非 `IsEnabled`；未绑定 `DataContext` 的 `TextBlock.Text` 读回 `null`
  而非 `""`；`TransformOperations` 在 `Avalonia.Media.Transformation` 命名空间。
- **探针断言实际条数**：#8 为 23 条、#9 为 44 条、#10 为 70 条、#11 为 53 条，全部通过且零警告日志。
- **执行时更正的 plan 错误**：
  - Task 2：`Notifier` 缺 `StyleKeyOverride`；探针的 `Handled off` 断言漏了 `KeyTextInput`。
  - Task 3：菜单 `Ctrl+1/2/3` 改为 `Ctrl+D1/D2/D3`；`IsEnabled` 断言改 `IsEffectivelyEnabled`；
    键盘读数探针先发不带修饰键的 K。
  - Task 4：`EasingPage` 小球补 `Canvas.Left="0"`；探针补 `using`。
  - Task 5：`LabeledSlider` 的 `Name="Opacity"` 改为 `Fade`；`Child.Text == ""` 改为
    `string.IsNullOrEmpty`。

### 应用服务层实测结论（2026-10-08）

四个项目（#12 Services、#13 AppDevelopment、#14 TestingDemo、#15 TestingDemo.Tests）落地过程中验证到的结果：

- **技术风险第 1 点已解除，但测试框架要换成 xunit v3**：`Avalonia.Headless.XUnit` 12.1.2 存在且可用，
  它依赖 `xunit.v3.extensibility.core`。同时引用 `xunit` 2.9.3 会让 `[InlineData]` 报 `CS0433`。
  实际用 `xunit.v3` 3.2.2，测试项目写 `<OutputType>Exe</OutputType>`，配 `xunit.runner.visualstudio`
  3.1.5 与 `Microsoft.NET.Test.Sdk` 18.10.1。上文包清单已据此更正。
- **渲染快照要显式启用 Skia**：默认 headless 绘图下 `CaptureRenderedFrame` 的位图是 1×1。
  `TestAppBuilder` 里写 `.UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })`。
  读像素时字节序要看帧的 `Format`，不能假设 BGRA（本次实测红蓝互换才暴露）。
- **技术风险第 3 点的实测**：headless 下 `StorageProvider` 与 `Launcher` 是 Noop 实现
  （`CanOpen`/`CanSave`/`CanPickFolder` 全为 `False`，`LaunchUriAsync` 返回 `False`），`InputPane`、
  `InsetsManager`、`IActivatableLifetime` 为 `null`。因此这几页做成"先检测、再使用"的演示。
  `TryGetFolderFromPathAsync` 与 `TryGetWellKnownFolderAsync` 在 headless 里仍能拿到真实目录。
- **功能点映射的出入**：Services 官方 9 页，做成 8 个 Tab（spec 漏了 `insets-manager` 与
  `activatable-lifetime`；Focus Manager 是指向 #9 的路标）；App Development 官方 14 页，做成 10 个 Tab
  （Web 内容与数据校验是路标）；Testing 官方 2 页并入 #15 的测试类。**`ui-testing-with-appium` 未做**：
  它需要外部驱动与真实应用进程，无法在仓库内自洽运行。
- **API 形态**：`PlatformSettings` 要用 `this.GetPlatformSettings()`（`using Avalonia.VisualTree;`），
  `TopLevel.PlatformSettings` 不存在；剪贴板用 12.x 的 `SetTextAsync` / `TryGetTextAsync` / `DataTransfer`，
  不用旧的 `IDataObject`；`Window.SystemDecorations` 已过时，用 `WindowDecorations`；
  `TextBox.Watermark` 已过时，用 `PlaceholderText`（`AVLN5001`）。
- **DI 作用域校验**：`ValidateScopes = true` 时，从根容器解析 `Scoped` 服务立刻抛
  `InvalidOperationException`，不会悄悄当单例用。
- **资源查找的差别**：替换字典里的项后，`TryFindResource` 一次性取出的值不更新，`DynamicResource` 会更新；
  直接改同一个画刷的 `Color` 则两者都更新（拿的是同一个对象）。
- **两套日志互不相通**：`Logger.Sink` 接框架内部消息（绑定、布局），`ILogger` 是应用自己的。`WinExe` 没有
  控制台，`AddConsole()` 看不到输出，所以用自定义 `ILoggerProvider` / `ILogSink` 把消息收进列表。
  `Logger.Sink` 是进程级的，页面在 `OnLoaded` 保存旧 sink、`OnUnloaded` 还原。
- **RadioButton 的事件顺序**：`IsCheckedChanged` 触发时，另一个按钮还没取消选中。要按 `sender` 判断，
  不能读兄弟按钮的状态（本地化页最初因此在切回中文时仍显示英文）。
- **`.resx` 要排除出 `AvaloniaResource`**，否则运行时 `ResourceManager` 找不到；清单资源名以程序集名为前缀。
- **探针断言实际条数**：#12 为 36 条、#13 为 44 条，全部通过；#15 自身 24 个测试全部通过。
- **执行时更正的错误**：`DataValidationPage` 的路标把目标 Tab 写成「数据校验」，实际叫「校验」；
  探针里资源色值的比较串改用颜色名（`Red`/`Blue`/`Green`）而非十六进制。

## 交付顺序与验证标准

**第一阶段（样板）**：完成 `Avalonia.LayoutDemo`。选它作样板的理由——纯 UI、零外部依赖、
功能点数量适中（3 个 Tab），能最快定型目录结构、说明条格式、注释风格和 TabControl 组织
方式。交付后由用户 review 确认风格。

**第二阶段（批量）**：样板确认后，按下列分组推进，每组完成即可构建验证：

- 基础层：#1 Fundamentals、#2 XAML、#7 PropertySystem
- 样式绑定层：#4 Styling、#5 DataBinding、#6 DataTemplates（同时删除旧 DataTemplateDemo）
- 交互图形层：#8 Events、#9 Input、#10 Graphics、#11 CustomControls
- 应用服务层：#12 Services、#13 AppDevelopment、#14/#15 Testing

**每个项目的验证标准**：

1. `dotnet build hello-avalonia.slnx` 通过且无新增警告
2. `dotnet run --project Avalonia.XxxDemo` 能启动窗口
3. 每个 Tab 可切换，交互元素（按钮、输入、动画）响应正常
4. 项目 #15 额外要求 `dotnet test` 全部通过

**收尾**：更新 `hello-avalonia.slnx` 项目列表、`README.md` 项目表格（按官方分类顺序
重排）、`Directory.Packages.props` 包声明。


