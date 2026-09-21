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
| `xunit` | 测试框架 | #15 |
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

- **容器查询**：可用。`Container.Name` / `Container.Sizing` 附加属性与
  `<ContainerQuery Name="..." Query="max-width:400">` 元素在 Avalonia 12.1.2 中直接
  编译通过、运行时无异常，未使用任何回退方案。
- **`Avalonia.Shared` 承载 XAML 控件**：可行，需增加 `Avalonia.Themes.Fluent` 包引用
  （因 ControlTheme 用到 Fluent 资源键 `SystemControlBackgroundListLowBrush` 和
  `SystemAccentColor`）。
- **元素名绑定**（额外发现，brief 未列出）：`{Binding #ElementName.Value}` 在
  `AvaloniaUseCompiledBindingsByDefault=true` 下无需 `x:DataType`，plan 里准备的三级
  回退未用上。
- **8 种布局面板语法**（额外发现，brief 未列出）：Avalonia 12.1.2 全部接受，含
  `RelativePanel` 的 8 个附加属性、裸 `<Panel>` 元素、`Width="NaN"` 覆盖 double 型
  样式属性。
- 风险 1（Headless 测试包）与风险 3（Services 桌面可用性）不在样板范围，留待对应
  plan 验证。
- **验证局限**：所有页面的视觉排布效果均未经目视确认，仅验证了"构建 0 错误 + 运行
  无异常堆栈"。

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


