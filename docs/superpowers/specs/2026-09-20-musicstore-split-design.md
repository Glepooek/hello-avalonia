# Avalonia.MusicStore 迁移与拆分设计

日期：2026-09-20
状态：已批准，待实施

## 背景

`Avalonia.MusicStore` 原先位于 `hello-dotnet/AvaloniaSamples/` 下，是该仓库中唯一的
Avalonia 项目。它刚完成 Avalonia 11 → 12 的升级（CefGlue 替换为官方
`Avalonia.Controls.WebView`）。

问题在于这个项目承载了过多互不相关的演示主题：专辑搜索与缓存、WebView 与 JS 互操作、
HtmlPanel 富文本、字体图标、数据模版选择器、Flyout 与 ControlTheme 样式。`MainWindow`
成了一个杂项演示大杂烩，与"每个示例演示一个特定技术"的学习仓库定位相悖。

`hello-avalonia` 是一个新建的独立仓库（已连 GitHub remote，仅有 Initial commit），
用于集中存放 Avalonia 示例。

## 目标

1. 将 `Avalonia.MusicStore` 迁移到 `hello-avalonia` 仓库
2. 按演示主题拆分为 4 个独立项目 + 1 个共享类库
3. 迁移后源仓库删除 `AvaloniaSamples/` 目录

## 非目标

- 不保留 git 历史（该项目的历史大多混在"上传一些demo"这类大提交中，抽取价值有限）
- 不重构业务逻辑，不修复既有代码风格问题（本次是迁移与拆分，不是重写）
- 不引入自动化测试（GUI 演示项目，与仓库既有约定一致）

## 目标结构

```
hello-avalonia/
├── hello-avalonia.slnx              # 统一解决方案（.slnx 新格式）
├── Directory.Packages.props         # 集中式包管理
├── Avalonia.Shared/                 # 共享类库（非可执行）
│   ├── Helpers/AvaloniaHelper.cs
│   ├── Helpers/NativeMethodHelper.cs
│   ├── Messages/MessageParam.cs
│   └── ViewModels/ViewModelBase.cs
├── Avalonia.MusicStore/             # ① 专辑搜索 / 购买 / 本地缓存
├── Avalonia.WebViewDemo/            # ② NativeWebView + NativeWebDialog + JS 桥
├── Avalonia.HtmlRendererDemo/       # ③ HtmlPanel 富文本 + IconFont
└── Avalonia.DataTemplateDemo/       # ④ 模版选择器 + Flyout + ControlTheme
```

依赖方向是单向的：四个演示项目各自引用 `Avalonia.Shared`，共享库不反向引用任何演示
项目。这保证每个演示都能独立打开运行——学习仓库的核心诉求。

## 内容划分

| 项目 | 从原项目取走 | 演示主题 |
|---|---|---|
| **Shared** | `AvaloniaHelper`、`NativeMethodHelper`、`MessageParam`、`ViewModelBase` | — |
| **MusicStore** | `Models/Album`、`AlbumViewModel`、`MusicStoreViewModel`、`AlbumView`、`MusicStoreView`、`MusicStoreWindow`、`Assets/Langs` | 搜索/购买/缓存、i18n |
| **WebViewDemo** | `WebViewWindow.axaml(.cs)`、`ShowNativeWebDialog`、`TestWeb/` | 嵌入式 WebView、原生弹窗、JS↔C# 双向桥 |
| **HtmlRendererDemo** | `HtmlPanel` 用法、`Assets/iconfont.ttf`、`Resources/Icons.axaml` | 富文本渲染、字体图标 |
| **DataTemplateDemo** | `PersonDataTemplateSelector`、`Models/Person`、`Resources/ButtonStyles.axaml`、Flyout/ControlTheme | 模版选择器、样式系统 |

每个演示项目都需新建自己的 `App.axaml(.cs)`、`Program.cs`、`MainWindow`、
`app.manifest`、`Assets/avalonia-logo.ico`。

## 关键决策

### 为什么用集中式包管理

四个演示项目都引用 Avalonia 12.1.2。版本散落在各自 csproj 中意味着升级要改四处，
漏掉一个就会造成版本错配——Avalonia 对混版极其敏感，表现为运行时类型加载异常而非
编译失败，排查成本高。`Directory.Packages.props` 把版本收敛到一处，也与 hello-dotnet
的 `WpfTest/Directory.Packages.props` 约定一致。

### 共享库的边界

`MessageParam` 与 `ViewModelBase` 都下沉到 Shared。

`ViewModelBase` 目前是空类（`class ViewModelBase : ObservableObject {}`），单看收益
有限。`MessageParam` 是弱类型载体，MusicStore 用其 `Data` 字段传 `AlbumViewModel`，
WebViewDemo 用其 `Reult` 字段传触发信号——两者实际用的是不同字段。

下沉的理由是：`WeakReferenceMessenger` 按**类型**路由消息，共享同一个 `MessageParam`
类型恰好演示了"跨窗口消息总线"这一模式本身，这对学习仓库有直接价值。代价是两个项目
的消息契约被绑定，日后一方加字段会影响另一方——在演示项目的规模下可以接受。

### 保留 `Reult` 拼写

`MessageParam.Reult` 是 `Result` 的拼写错误。本次不修正，因为这是迁移而非重构，改名
会让新仓库与 hello-dotnet 的历史代码产生无谓差异。

### WebViewDemo 的主窗

原先 WebView 是从 MusicStore 主窗点按钮弹出的。拆分后它是主角，主窗直接做成演示菜单，
三个入口分别演示：`NativeWebView`（嵌入式控件）、`NativeWebDialog`（原生窗口）、
`C# 调用 JS`。JS→C# 的 JSON 信封桥（`{ action, requestId, payload }`）保持现状。

### 命名空间

各项目使用自己的根命名空间（`Avalonia.WebViewDemo.*` 等），共享库用 `Avalonia.Shared.*`。

## 验证标准

本仓库无自动化测试（GUI 演示项目），按以下标准逐项验证：

| # | 步骤 | 验证 |
|---|---|---|
| 1 | `dotnet build hello-avalonia.slnx` | 0 error |
| 2 | 逐个启动 4 个演示 exe | 主窗正常显示，stderr 无异常 |
| 3 | MusicStore 截图 | 专辑封面加载（首次运行 Cache 为空属正常，需经商店购买或从旧输出目录拷入缓存文件验证） |
| 4 | WebViewDemo 截图 | 网页渲染正常，JS 桥双向调用可用 |
| 5 | HtmlRendererDemo 截图 | 富文本与图标字体渲染正常 |
| 6 | DataTemplateDemo 截图 | 男/女模版分色，Flyout 可弹出 |
| 7 | hello-dotnet 删除后构建 | 无残留引用 |

## 提交划分

新仓库：

1. `chore:` 解决方案脚手架 + `Directory.Packages.props`（沿用仓库现有 `.gitignore`，
   它已是标准 VS 模板并覆盖 bin/obj）
2. `feat:` `Avalonia.Shared` 共享类库
3. `feat:` `Avalonia.MusicStore`
4. `feat:` `Avalonia.WebViewDemo`
5. `feat:` `Avalonia.HtmlRendererDemo`
6. `feat:` `Avalonia.DataTemplateDemo`
7. `docs:` 更新 README

源仓库 `hello-dotnet`：

1. `chore:` 删除 `AvaloniaSamples/`（已迁至 hello-avalonia）

## 风险

- **`iTunesSearch 1.0.44` 是 .NET Framework 包**，在 net10.0 下产生 NU1701 警告。
  实测功能正常（搜索返回 100 条结果），迁移后需重新确认。
- **`Avalonia.Diagnostics` 尚无 12.x 版本**，当前固定在 11.3.22，仅 Debug 配置引用。
- **WebView2 运行时依赖**：`Avalonia.Controls.WebView` 在 Windows 上依赖系统 WebView2
  Runtime，目标机器缺失时 WebViewDemo 无法运行。
