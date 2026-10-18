# hello-avalonia

Avalonia UI 学习示例集合。每个项目演示一个独立主题，可单独打开运行；每个应用都配有 Headless 测试与 Appium 测试。

## 项目

演示项目都在 `src/`，按建议的学习顺序排列。

| 项目 | 演示内容 |
|---|---|
| [Avalonia.StarterTutorialDemo](src/Avalonia.StarterTutorialDemo) | 官方入门教程：添加控件、Grid 布局、自定义窗口、Click 事件、摄氏转华氏（code-behind）及三个练习 |
| [Avalonia.FundamentalsDemo](src/Avalonia.FundamentalsDemo) | 纯代码 UI、code-behind 与 MVVM 对照、TopLevel、视觉树与逻辑树、应用生命周期 |
| [Avalonia.XamlDemo](src/Avalonia.XamlDemo) | 命名空间、x: 指令、标记扩展、类型转换器、泛型、XAML 编译、平台相关 XAML（OnPlatform / OnFormFactor） |
| [Avalonia.LayoutDemo](src/Avalonia.LayoutDemo) | 8 种布局面板对照、对齐与 Margin/Padding、四种响应式手段 |
| [Avalonia.StylingDemo](src/Avalonia.StylingDemo) | 选择器语法、样式类、伪类、ControlTheme、主题变体、嵌入字体、样式共享 |
| [Avalonia.DataBindingDemo](src/Avalonia.DataBindingDemo) | 绑定语法与模式、编译绑定、集合与主从、多值绑定、命令、转换器、校验、集合视图、异步绑定、绑定调试 |
| [Avalonia.DataTemplatesDemo](src/Avalonia.DataTemplatesDemo) | 内联模板、按类型匹配、模板选择器、代码建模板、复用、ViewLocator、面板与树模板 |
| [Avalonia.PropertySystemDemo](src/Avalonia.PropertySystemDemo) | StyledProperty / DirectProperty / 附加属性、值优先级、元数据与回调 |
| [Avalonia.EventsDemo](src/Avalonia.EventsDemo) | 生命周期事件、输入事件、路由事件的隧道/冒泡/直接、Handled 与 handledEventsToo、自定义路由事件 |
| [Avalonia.InputDemo](src/Avalonia.InputDemo) | 指针、焦点与导航、手势、键盘与 HotKey、事件/命令/手势三种交互写法、拖放、文本输入过滤 |
| [Avalonia.GraphicsDemo](src/Avalonia.GraphicsDemo) | 画刷与渐变、变换、形状与几何、自定义绘制、特效、裁剪与命中、图标、渲染选项、关键帧动画、控件过渡、页面过渡、缓动函数、合成动画 |
| [Avalonia.ControlsDemo](src/Avalonia.ControlsDemo) | 按官方 Controls 目录组织的控件演示（119 页，侧边导航，见下方对照表） |
| [Avalonia.CustomControlsDemo](src/Avalonia.CustomControlsDemo) | UserControl、TemplatedControl 与 ControlTheme、自绘控件、控件树、自定义 Panel、自定义 Flyout |
| [Avalonia.BehaviorsDemo](src/Avalonia.BehaviorsDemo) | Xaml.Behaviors.Avalonia：Behavior / Trigger / Action 机制、事件转命令、数据触发、焦点键盘指针、表单验证、集合、窗口弹出通知、拖拽、响应式、动画，以及文件系统、网络、剪贴板、截图（均限制在沙箱内）和自定义 Behavior（侧边导航，共 15 页） |
| [Avalonia.ServicesDemo](src/Avalonia.ServicesDemo) | 剪贴板、文件对话框、StorageProvider、Launcher、平台设置，以及 InputPane / InsetsManager / IActivatableLifetime 的可用性检测 |
| [Avalonia.AppDevelopmentDemo](src/Avalonia.AppDevelopmentDemo) | 依赖注入、本地化、日志、未处理异常、资源查找、线程模型、窗口管理、无障碍 |
| [Avalonia.TestingDemo](src/Avalonia.TestingDemo) | 被测应用：计数器、表单、列表三个易于断言的面板（测试写法的基准样例，见「项目测试」） |
| [Avalonia.MusicStore](src/Avalonia.MusicStore) | 完整应用：专辑搜索（iTunes API）、购买、本地缓存、RESX 多语言 |
| [Avalonia.WebViewDemo](src/Avalonia.WebViewDemo) | NativeWebView 嵌入控件、NativeWebDialog 原生窗口、JS ↔ C# 双向调用 |
| [Avalonia.HtmlRendererDemo](src/Avalonia.HtmlRendererDemo) | HtmlPanel 富文本渲染、IconFont 与 PathIcon 图标 |
| [Avalonia.Shared](src/Avalonia.Shared) | 共享类库：演示页说明条控件、窗口 Helper、Win32 互操作、消息载体、ViewModel 基类 |

### 控件目录对照表（Avalonia.ControlsDemo）

| 分类 | 页面数 | 说明 |
|---|---|---|
| Input | 26 | 22 个控件页 + 4 个实战页 |
| Layout | 23 | 20 个控件页 + 3 个实战页 |
| Data display | 16 | 12 个控件页 + 4 个实战页 |
| Feedback | 5 | 4 个控件页 + 1 个实战页 |
| Media | 4 | 3 个控件页 + 1 个实战页 |
| Menus | 6 | 5 个控件页 + 1 个实战页 |
| Navigation | 10 | 8 个控件页 + 2 个实战页 |
| Primitives | 7 | 5 个控件页 + 2 个实战页 |
| System | 2 | 1 个控件页 + 1 个实战页 |
| Web | 4 | 4 个路标页（指向 `Avalonia.WebViewDemo`） |
| 付费控件 | 7 | 7 个路标页（Pro / Enterprise，只在浏览器里打开官方文档） |
| How-to 路标 | 9 | 9 个路标页（指向本仓库其他演示项目） |
| **合计** | **119** | 80 个可运行控件页 + 19 个实战页 + 20 个路标页 |

导航树里，实战页标题带「实战：」前缀，路标页带「↗」前缀；路标页只说明用途与指向，不重复实现演示。

## 环境

- .NET 10.0 SDK
- Windows 上运行 WebViewDemo 需要 WebView2 Runtime
- 跑 Appium 测试需要 [WinAppDriver 1.2.1](https://github.com/microsoft/WinAppDriver/releases) 与 Windows 开发人员模式

主要依赖（版本统一在 [Directory.Packages.props](Directory.Packages.props)）：

| 用途 | 包 |
|---|---|
| UI 框架 | Avalonia 12.1.2（含 Desktop、Fluent 主题、DataGrid、ColorPicker） |
| 扩展控件 | Avalonia.Controls.WebView、Avalonia.HtmlRenderer、MessageBox.Avalonia、AsyncImageLoader.Avalonia |
| 行为与 MVVM | Xaml.Behaviors.Avalonia、CommunityToolkit.Mvvm |
| 测试 | xUnit v3、Avalonia.Headless.XUnit、Appium.WebDriver 4.4.5、BenchmarkDotNet |

## 构建与运行

```bash
dotnet build hello-avalonia.slnx
dotnet run --project src/Avalonia.LayoutDemo      # 换成任意演示项目
```

所有项目统一输出到各自的 `bin\Debug\`（路径里没有 `net10.0`）。

## 项目测试

测试项目都在 `tests/`，每个应用各有一个 Headless 项目和一个 Appium 项目。

| 项目 | 内容 |
|---|---|
| `Avalonia.<应用>.Tests` ×20 | xUnit v3 + Avalonia Headless，不开窗口、不占桌面：走完全部标签页（无警告日志）、ViewModel 单测、关键页面的点击与输入；`TestingDemo.Tests` 另含渲染像素断言 |
| `Avalonia.<应用>.AppiumTests` ×20 | WinAppDriver + Appium 驱动真实进程，**默认跳过**：全部应用的启动、16 个 TabControl 应用的逐页导航，以及只有真实进程才能验证的流程——TestingDemo 三个面板、真实键鼠事件（Input、Events）、文件对话框取消（Services）、UIA 自动化属性（AppDevelopment）、网页窗口与 JS ↔ C# 调用（WebView）、商店窗口（MusicStore） |
| [Avalonia.Tests.Common](tests/Avalonia.Tests.Common) | Headless 共用辅助：`HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`、`CultureScope` |
| [Avalonia.Appium.Common](tests/Avalonia.Appium.Common) | Appium 共用辅助：`AppSession`、`AppiumFactAttribute`、`AppPaths`、`UnicodeKeyboard`、`KeyChord` |

`<应用>` 是去掉 `Avalonia.` 前缀的项目名，如 `Avalonia.LayoutDemo.Tests`、`Avalonia.MusicStore.AppiumTests`。

运行：

```powershell
dotnet test hello-avalonia.slnx                   # 全部；Appium 测试显示为跳过
dotnet test tests/Avalonia.LayoutDemo.Tests       # 单个应用

# Appium：先在另一个终端启动 WinAppDriver（需开启 Windows 开发人员模式）
& "C:\Program Files\Windows Application Driver\WinAppDriver.exe"
$env:HELLO_AVALONIA_APPIUM = "1"
dotnet test tests/Avalonia.TestingDemo.AppiumTests
```

Appium 测试会接管鼠标键盘，运行期间不要操作机器、不要锁屏。WinAppDriver 的安装、门控条件与常见问题见 [测试设计文档](docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md) 的「Appium 环境配置」；键入、按键、子窗口的使用规则见 [Avalonia 测试规范](docs/2026-10-18-avalonia-testing-standards.md) §3.6。

写测试：

- 新建测试项目、写第一个测试，照 [Avalonia 测试规范](docs/2026-10-18-avalonia-testing-standards.md) §1「5 分钟上手」；点击、键入、等待异步等辅助方法见同文 §3.4 的 `UiHelpers` 速查表；
- 选层、ViewModel、HTTP mock、SQLite 等与框架无关的规则见 [通用测试规范](docs/2026-10-18-testing-common-standards.md)，WPF 项目见 [WPF 测试规范](docs/2026-10-18-wpf-testing-standards.md)。

## 性能基准

[benchmarks/Avalonia.Benchmarks](benchmarks/Avalonia.Benchmarks) 用 BenchmarkDotNet 测转换器与 ViewModel 的热路径。它不是测试项目，`dotnet test` 不会运行它；要用 Release、在命令行下运行，不挂调试器：

```powershell
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*Celsius*'
```

加一个基准照 [BenchmarkDotNet 规范](docs/2026-10-18-benchmarkdotnet-standards.md) §1「快速上手」；写法规则与 CI 门禁见同文 §3、§4。
