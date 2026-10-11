# ControlsDemo 07：Web、付费控件与横切 How-to 的 20 个路标页，以及整套目录的收尾 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全 `Avalonia.ControlsDemo` 的最后三个分类——Web（4 页）、付费控件（7 页）、How-to 路标（9 页），共 20 个路标页；用一个共用的 `SignpostView` 承载全部路标页的正文；再对整套 119 页目录做总数、顺序、可导航性的收尾断言，并更新 README、spec 与 CLAUDE.md。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份新增一个共用的 `SignpostView`（`UserControl`，6 个字符串属性 + 一个"在浏览器中打开"按钮），20 个路标页各自只是 `DemoHeader` 加一个 `SignpostView` 的薄壳；`PageCatalog.Web.cs` / `Premium.cs` / `HowToSignposts.cs` 三个空桩各填登记行；探针 `Probe.Sign.cs` / `Probe.Sign2.cs` 校验整套目录。不引入新包，不写任何付费控件的可运行代码。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00–06 份已执行（`PageCatalog`、`Categories.Web` / `Premium` / `HowToSignposts`、`SignpostPage<TPage>` 辅助方法、`Harness` 已存在；`AddWeb`、`AddPremium`、`AddHowToSignposts` 目前是空方法）。

> **编号说明：** 同第 05、06 份——第 00 份开头那张"9 份计划"表已作废，以 `docs/superpowers/plans/` 下实际存在的文件为准。本份是 07，也是最后一份。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」，**不重复贴**，执行者必须先读第 00 份；第 01–06 份「页面的写法约定」同样适用。本份额外强调：

- 路标页类名 `<主题>SignpostPage`，命名空间一律 `Avalonia.ControlsDemo.Views.Pages`（与所在子目录无关）；页面目录 `Views/Pages/Web/`、`Views/Pages/Premium/`、`Views/Pages/HowTo/`；共用的 `SignpostView` 放在 `Views/Pages/` 根下
- **路标页不写演示代码**（spec 去重规则 4）：只有 `DemoHeader`、一个 `SignpostView`；付费控件不引用任何商业授权包（spec 非目标）
- `DocPath` 一律是官方路径：Web 与付费控件用 `controls/...`，How-to 路标用 `docs/how-to/<slug>-how-to`；`SignpostView.DocPath` 必须与同页 `DemoHeader.DocPath`、`PageCatalog` 登记的 `DocPath` 三处一致，探针逐页断言
- **路标里每一条"指向"都必须是真的**：指向的项目与 Tab 名由探针直接读对应 `MainWindow.axaml` 里的 `Header="…"` 核对，防止以后改名后路标悬空；现有项目没有覆盖的主题，如实写"暂无可运行演示"，不假装有
- 标题登记时写纯主题名（`NativeWebView`、`响应式布局`），导航树自动加 `↗ ` 前缀
- 界面文字用中文，注释用英文；属性值里不用 ASCII 双引号（用「」或『』）、不用 `&` 与 `<`
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时核实）

| 事项 | 核实结果 | 对页面写法的影响 |
|---|---|---|
| headless 的 `Launcher` | `TopLevel.Launcher` 非空，类型是 `NoopLauncher`；`LaunchUriAsync` 同步完成并返回 `False`，**没有副作用**（不会真的启动浏览器） | "在浏览器中打开"按钮可以放心在探针里点；页面按返回值显示结果，探针断言 headless 下的"没有被接受"文案 |
| `StringConverters` | 不依赖它：`SignpostView` 用代码在 `Refresh()` 里设行的 `IsVisible`，不走绑定转换器 | 少一个静默失败点 |
| 官方 Web 分类 | 站点地图下共 4 页：`controls/web/nativewebview`、`nativewebdialog`、`webauthenticationbroker`、`webview-environment` | Web 路标 4 页，与 spec 一致 |
| `NativeWebView` | Windows 用 WebView2、macOS 与 iOS 用 WKWebView、Linux 用 WPE WebKit（WebKitGTK 备用）、Android 也支持；浏览器（WebAssembly）尚未实现；有 `Source`、`Navigate`、`GoBack`、`NavigationStarted`、`WebMessageReceived`、`InvokeScript`、`EnvironmentRequested` 等 | 说明文字照此写 |
| `NativeWebDialog` | 独立的原生浏览器窗口；支持 Windows、macOS、Linux，不支持 iOS、Android、浏览器 | 同上 |
| `WebAuthenticationBroker` | `AuthenticateAsync(topLevel, options)`，`WebAuthenticatorOptions` 的 `Mode` 可选 `Auto`、`System`、`NativeWebDialog`、`Browser` | 同上 |
| WebView 环境配置 | 用 `EnvironmentRequested` 事件在引擎启动前定制；共享选项 `EnableDevTools`，之后按平台分别设 WebView2 / WKWebView / WPE WebKit / WebKitGTK 的专属选项 | 同上 |
| `Avalonia.WebViewDemo` 的覆盖面 | 只有三个按钮：`NativeWebView`（嵌入窗口）、`NativeWebDialog`（独立窗口）、从 C# 调 JS；**没有** `WebAuthenticationBroker`，**没有** `EnvironmentRequested` | 前两页指向它；后两页写"暂无可运行演示" |
| 付费控件的官方位置 | **都不在 `controls/primitives` 等本项目分类下**：Charts 在 `controls/data-display/charts`（128 页）、PdfViewer 在 `controls/data-display/pdfviewer`（7 页）、TreeDataGrid 在 `controls/data-display/structured-data/treedatagrid`（7 页）、Markdown 在 `controls/data-display/text-display/markdown`（4 页）、RichTextEditor 在 `controls/input/text-input/richtexteditor`（11 页）、VirtualKeyboard 在 `controls/input/text-input/virtualkeyboard`（3 页）、MediaPlayer 在 `controls/media/mediaplayer`（4 页）；页数含总览页 | 这就是前几批记录的"范围缺口"：它们统一收进「付费控件」分类，`DocPath` 保留官方原路径；页数写进路标的"官方子页"一行 |
| 付费控件的授权 | 官方标签页：`avalonia-pro` 标签下 158 页，`avalonia-enterprise` 标签下 36 页，后者恰好是除 Charts 外的六组；各控件页都写"available as part of Avalonia Pro or higher"并要求在项目文件里写授权密钥；Charts 只标 Pro | Charts 写"Avalonia Pro"；其余六组写"Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）" |
| 9 个横切指向的 Tab | 已逐个在对应 `MainWindow.axaml` 里核对存在：LayoutDemo「响应式布局」；StylingDemo「选择器」「样式类」「ControlTheme」「主题变体」「字体」；FundamentalsDemo「MVVM」「纯代码 UI」；InputDemo「拖放」；ServicesDemo「剪贴板」；AppDevelopmentDemo「日志」「未处理异常」 | 路标的"本仓库里"一行照写；探针重新读文件核对 |
| 项目与 slnx、README | `Avalonia.ControlsDemo` 早已在 `hello-avalonia.slnx` 与 README 项目表里；README 里还**没有**"控件目录对照表"一节，项目表那一行写的是"10 个分类" | 本份只补那一节，并改那一行的描述 |

**官方 How-to 共 28 篇的 slug**（探针用它做集合相等断言）：并入控件分类的 19 篇 `treeview`、`listbox`、`itemscontrol`、`datagrid`、`textbox`、`combobox`、`slider`、`datepicker`、`grid`、`scrollviewer`、`expander`、`tabcontrol`、`navigation`、`menu`、`image`、`notifications`、`window`、`dialogs`、`data-persistence`；做成路标的 9 篇 `responsive-layout`、`styling-controls`、`theme-switching`、`custom-font`、`mvvm`、`coded-ui`、`drag-and-drop`、`clipboard`、`debugging`；`DocPath` 都是 `docs/how-to/<slug>-how-to`。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Navigation/
│   ├── PageCatalog.Web.cs                       改：+4 路标
│   ├── PageCatalog.Premium.cs                   改：+7 路标
│   └── PageCatalog.HowToSignposts.cs            改：+9 路标
└── Views/Pages/
    ├── SignpostView.axaml / .axaml.cs           新：路标页共用的正文
    ├── Web/        NativeWebViewSignpostPage / NativeWebDialogSignpostPage
    │               WebAuthenticationBrokerSignpostPage / WebViewEnvironmentSignpostPage
    ├── Premium/    ChartsSignpostPage / PdfViewerSignpostPage / MarkdownSignpostPage
    │               TreeDataGridSignpostPage / RichTextEditorSignpostPage
    │               MediaPlayerSignpostPage / VirtualKeyboardSignpostPage
    └── HowTo/      ResponsiveLayoutSignpostPage / StylingControlsSignpostPage / ThemeSwitchingSignpostPage
                    CustomFontSignpostPage / MvvmSignpostPage / CodedUiSignpostPage
                    DragAndDropSignpostPage / ClipboardSignpostPage / DebuggingSignpostPage
README.md                                        改：项目表一行 + 新增「控件目录对照表」
docs/superpowers/specs/…-design.md               改：回填「批 6 实测结论」
CLAUDE.md                                        改：「进行中的工作」收尾
```

每个路标页含 `.axaml` 与 `.axaml.cs`（后者只有构造函数里的 `InitializeComponent()`）。探针（仓库外）：`C:\Temp\probe-controls\Probe.Sign.cs`、`Probe.Sign2.cs`。

### 页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 路标页骨架固定：`ScrollViewer` → `StackPanel Margin="12"` → `DemoHeader` → 一行 `caption` → 一个 `local:SignpostView`。页面里**不放任何别的控件**。
- `SignpostView` 的 6 个属性都是 `string?`：`Summary`（一段话，必填）、`License`、`SubPages`、`Target`、`RunLine`、`DocPath`（必填）。空的属性对应的行自动隐藏。

### Task 1: SignpostView 与 Web 的 4 个路标页

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/SignpostView.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Web/NativeWebViewSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Web/NativeWebDialogSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Web/WebAuthenticationBrokerSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Web/WebViewEnvironmentSignpostPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份的 `DemoHeader`、`SharedStyles` 里的 `caption` / `stage` / `hint`
- Produces: `SignpostView`（`public`，`UserControl`；`StyledProperty<string?>`：`Summary`、`License`、`SubPages`、`Target`、`RunLine`、`DocPath`；内部元素 `Name`：`SummaryText`、`LicenseRow` / `LicenseText`、`SubPagesRow` / `SubPagesText`、`TargetRow` / `TargetText`、`RunRow` / `RunText`、`UrlText`、`OpenButton`、`LaunchText`，探针靠这些名字读写）；4 个 Web 路标页类（Task 4 登记）

- [ ] **Step 1: SignpostView**

`SignpostView` 的行显隐与文字都在代码里的 `Refresh()` 设置，不走绑定转换器。`_ready` 标志保证 `InitializeComponent()` 之前触发的属性变更不会碰到还没创建的元素。"在浏览器中打开"按钮用 `TopLevel.Launcher`，按 `LaunchUriAsync` 的返回值显示结果——headless 里它恒为 `False`。

#### `Avalonia.ControlsDemo/Views/Pages/SignpostView.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SignpostView">
    <Border Classes="stage" Padding="12">
        <StackPanel Spacing="10">
            <TextBlock Name="SummaryText" TextWrapping="Wrap" />

            <StackPanel Name="LicenseRow" Spacing="2">
                <TextBlock Classes="caption" Text="授权" />
                <TextBlock Name="LicenseText" TextWrapping="Wrap" />
            </StackPanel>

            <StackPanel Name="SubPagesRow" Spacing="2">
                <TextBlock Classes="caption" Text="官方子页" />
                <TextBlock Name="SubPagesText" TextWrapping="Wrap" />
            </StackPanel>

            <StackPanel Name="TargetRow" Spacing="2">
                <TextBlock Classes="caption" Text="本仓库里" />
                <TextBlock Name="TargetText" TextWrapping="Wrap" />
            </StackPanel>

            <StackPanel Name="RunRow" Spacing="2">
                <TextBlock Classes="caption" Text="运行" />
                <SelectableTextBlock Name="RunText" FontFamily="Consolas, Menlo, monospace" />
            </StackPanel>

            <StackPanel Spacing="2">
                <TextBlock Classes="caption" Text="官方文档" />
                <SelectableTextBlock Name="UrlText" FontFamily="Consolas, Menlo, monospace" />
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Button Name="OpenButton" Content="在浏览器中打开" />
                    <TextBlock Name="LaunchText" Classes="hint" VerticalAlignment="Center" />
                </StackPanel>
            </StackPanel>
        </StackPanel>
    </Border>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/SignpostView.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>
    /// The body shared by every signpost page: a summary, optional fact rows, and a link to the
    /// official docs. A row whose value is empty is hidden, so each page only shows what it has.
    /// </summary>
    public partial class SignpostView : UserControl
    {
        public const string DocBase = "https://docs.avaloniaui.net/";

        public static readonly StyledProperty<string?> SummaryProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(Summary));

        public static readonly StyledProperty<string?> LicenseProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(License));

        public static readonly StyledProperty<string?> SubPagesProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(SubPages));

        public static readonly StyledProperty<string?> TargetProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(Target));

        public static readonly StyledProperty<string?> RunLineProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(RunLine));

        public static readonly StyledProperty<string?> DocPathProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(DocPath));

        // Property changes can arrive before InitializeComponent has created the named elements.
        private readonly bool _ready;

        public SignpostView()
        {
            InitializeComponent();
            OpenButton.Click += OnOpenClick;
            _ready = true;
            Refresh();
        }

        public string? Summary
        {
            get => GetValue(SummaryProperty);
            set => SetValue(SummaryProperty, value);
        }

        public string? License
        {
            get => GetValue(LicenseProperty);
            set => SetValue(LicenseProperty, value);
        }

        public string? SubPages
        {
            get => GetValue(SubPagesProperty);
            set => SetValue(SubPagesProperty, value);
        }

        public string? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public string? RunLine
        {
            get => GetValue(RunLineProperty);
            set => SetValue(RunLineProperty, value);
        }

        public string? DocPath
        {
            get => GetValue(DocPathProperty);
            set => SetValue(DocPathProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SummaryProperty || change.Property == LicenseProperty || change.Property == SubPagesProperty
                || change.Property == TargetProperty || change.Property == RunLineProperty || change.Property == DocPathProperty)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!_ready) return;

            SummaryText.Text = Summary;
            ShowRow(LicenseRow, LicenseText, License);
            ShowRow(SubPagesRow, SubPagesText, SubPages);
            ShowRow(TargetRow, TargetText, Target);
            ShowRow(RunRow, RunText, RunLine);

            bool hasPath = !string.IsNullOrEmpty(DocPath);
            UrlText.Text = hasPath ? DocBase + DocPath : "";
            OpenButton.IsEnabled = hasPath;
        }

        private static void ShowRow(Control row, TextBlock text, string? value)
        {
            text.Text = value;
            row.IsVisible = !string.IsNullOrEmpty(value);
        }

        // async void is fine here: this is an event handler, and nothing awaits it.
        private async void OnOpenClick(object? sender, RoutedEventArgs e)
        {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            if (launcher is null || string.IsNullOrEmpty(DocPath))
            {
                LaunchText.Text = "没有可用的 Launcher";
                return;
            }

            bool accepted = await launcher.LaunchUriAsync(new Uri(DocBase + DocPath));
            LaunchText.Text = accepted
                ? "已请求系统用默认浏览器打开"
                : "系统没有接受这个请求（没有可用的浏览器，或当前平台不支持 Launcher）";
        }
    }
}
```

- [ ] **Step 2: NativeWebView 与 NativeWebDialog**

这两页指向 `Avalonia.WebViewDemo`：它的主窗口有三个按钮（原生按钮文案是英文），`1. NativeWebView (embedded in a Window)`、`2. NativeWebDialog (standalone native window)`、`3. Call JS from C# (open #1 first)`。

#### `Avalonia.ControlsDemo/Views/Pages/Web/NativeWebViewSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NativeWebViewSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="NativeWebView：嵌入布局的原生浏览器"
                               DocPath="controls/web/nativewebview" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.WebViewDemo" />
            <local:SignpostView DocPath="controls/web/nativewebview"
                                Summary="把各平台自带的浏览器引擎包成一个能放进布局的控件：Windows 用 WebView2，macOS 与 iOS 用 WKWebView，Linux 用 WPE WebKit（WebKitGTK 备用），Android 也支持，浏览器（WebAssembly）尚未实现。有 Source、Navigate、GoBack 与导航事件，WebMessageReceived 加 InvokeScript 让 JS 与 C# 双向通信，EnvironmentRequested 可以在引擎启动前定制环境。"
                                Target="Avalonia.WebViewDemo →「1. NativeWebView (embedded in a Window)」按钮（嵌入窗口），连同「3. Call JS from C# (open #1 first)」按钮（JS 与 C# 互调）"
                                RunLine="dotnet run --project Avalonia.WebViewDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Web/NativeWebViewSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NativeWebViewSignpostPage : UserControl
    {
        public NativeWebViewSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Web/NativeWebDialogSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NativeWebDialogSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="NativeWebDialog：独立的原生浏览器窗口"
                               DocPath="controls/web/nativewebdialog" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.WebViewDemo" />
            <local:SignpostView DocPath="controls/web/nativewebdialog"
                                Summary="一个单独的原生浏览器窗口，适合在布局之外显示网页，或者嵌入式的 NativeWebView 用不了的地方。支持 Windows、macOS 与 Linux（Linux 上所有者窗口的行为随窗口管理器而异），不支持 iOS、Android 与浏览器。常用成员有 Title、CanUserResize、Source、Show、Close、Navigate、InvokeScript，以及 Closing、NavigationStarted、WebMessageReceived 等事件。"
                                Target="Avalonia.WebViewDemo →「2. NativeWebDialog (standalone native window)」按钮（独立的原生窗口）"
                                RunLine="dotnet run --project Avalonia.WebViewDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Web/NativeWebDialogSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NativeWebDialogSignpostPage : UserControl
    {
        public NativeWebDialogSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: WebAuthenticationBroker 与 WebView 环境配置**

这两项现有项目都没有演示（核实过：`Avalonia.WebViewDemo` 与 `Avalonia.AppDevelopmentDemo` 里都搜不到 `WebAuthenticationBroker` 与 `EnvironmentRequested`），所以"本仓库里"一行如实写"暂无可运行演示"，`RunLine` 留空，运行行自动隐藏。

#### `Avalonia.ControlsDemo/Views/Pages/Web/WebAuthenticationBrokerSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.WebAuthenticationBrokerSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="WebAuthenticationBroker：网页登录流程"
                               DocPath="controls/web/webauthenticationbroker" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/web/webauthenticationbroker"
                                Summary="处理 OAuth 一类基于网页的登录流程：调用 AuthenticateAsync(topLevel, options) 打开起始地址，等服务端重定向到回调地址后把结果交还给代码，适合接入 Google、Microsoft 账号或 Facebook Login 之类的授权。WebAuthenticatorOptions 里除了 RequestUri 与 RedirectUri，还有 Mode 可选 Auto、System、NativeWebDialog 与 Browser，不同模式覆盖的平台不同。"
                                Target="暂无可运行演示：Avalonia.WebViewDemo 只涵盖 NativeWebView、NativeWebDialog 与 JS 互调。真正跑通它需要一个 OAuth 服务端的应用注册，不适合做成离线可跑的演示，请直接看官方文档。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Web/WebAuthenticationBrokerSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WebAuthenticationBrokerSignpostPage : UserControl
    {
        public WebAuthenticationBrokerSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Web/WebViewEnvironmentSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.WebViewEnvironmentSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="WebView 环境配置：在引擎启动前定制它"
                               DocPath="controls/web/webview-environment" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/web/webview-environment"
                                Summary="在浏览器引擎创建之前，用 EnvironmentRequested 事件定制它：先设各平台共享的 EnableDevTools，再按当前平台设专属选项。Windows 的 WebView2 有 UserDataFolder、ProfileName、BrowserExecutableFolder、AdditionalBrowserArguments、Language、IsInPrivateModeEnabled；macOS 与 iOS 的 WKWebView 有 NonPersistentDataStore、DataStoreIdentifier；Linux 的 WPE WebKit 与 WebKitGTK 各有自己的数据目录、渲染模式等选项。"
                                Target="暂无可运行演示：Avalonia.WebViewDemo 没有订阅 EnvironmentRequested。选项依赖具体平台的浏览器引擎，请直接看官方文档里每个平台的代码示例。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Web/WebViewEnvironmentSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WebViewEnvironmentSignpostPage : UserControl
    {
        public WebViewEnvironmentSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 4: 抽取并构建**

Run: `& 'C:\Temp\extract-plan.ps1' -Plan '<本计划>' -RepoRoot '<仓库根>'` 然后 `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|个错误"`
Expected: 0 个错误。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the shared SignpostView and the four Web signpost pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 2: 付费控件的 7 个路标页

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/ChartsSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/PdfViewerSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/MarkdownSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/TreeDataGridSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/RichTextEditorSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/MediaPlayerSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Premium/VirtualKeyboardSignpostPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `SignpostView`、第 00 份的 `DemoHeader` 与 `SharedStyles`
- Produces: 7 个付费控件路标页类（Task 4 登记）

- [ ] **Step 1: Charts 与 PdfViewer**

`Charts` 只标 Pro；其余六组 Pro 与 Enterprise 都标。`License` 一行据此写。`Target` 一律如实写"暂无可运行演示"（核实过：全部仓库里没有任何项目引用这些包，也没有 `AvaloniaUILicenseKey`），`RunLine` 留空，运行行自动隐藏。

#### `Avalonia.ControlsDemo/Views/Pages/Premium/ChartsSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ChartsSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Charts：数据可视化"
                               DocPath="controls/data-display/charts" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/data-display/charts"
                                License="Avalonia Pro（官方只标 Pro，不含 Enterprise）"
                                SubPages="官方子页约 128 页，覆盖成百种图表：笛卡尔系的柱状、折线、面积、散点、堆叠，圆环系的饼图、环形、半环，财务系的 K 线、OHLC、Renko，以及桑基图、树状图、甘特图、雷达图、热力图、词云等"
                                Summary="付费的数据可视化组件。所有图表都继承 ChartBase，数据系列继承 ChartSeries：系列有 Title、ItemsSource、ValuePath、Fill、Stroke，笛卡尔系列另有 CategoryPath 与 EmptyPointMode。宿主有 CartesianChart、SankeyChart、FinancialChart 与 FlowChart，共享图例、提示、十字准线、数据标签、趋势线与导出（PNG / JPEG）。动画用 IsAnimationEnabled、AnimationDuration 与 Easing 控制，自定义系列走 RenderSeries(in SeriesRenderContext)。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 授权，项目文件里要写 AvaloniaUILicenseKey 并引用 Avalonia.Controls.Charts 包。本仓库演示的是免费控件，请直接看官方文档。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/ChartsSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ChartsSignpostPage : UserControl
    {
        public ChartsSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/PdfViewerSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.PdfViewerSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="PdfViewer：内置的 PDF 阅读器"
                               DocPath="controls/data-display/pdfviewer" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/data-display/pdfviewer"
                                License="Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）"
                                SubPages="官方子页 7 页：控件总览、显示与缩放、侧边栏与工具条、文本选择与搜索、批注、权限与只读、打印与共享"
                                Summary="一个完整的 PDF 阅读与批注控件，渲染基于 PDFium。Document 相关有 Source、DocumentSource、Password、CurrentPage；显示有 ZoomLevel、ZoomMode、ViewMode（SinglePage / Continuous / TwoPages / TwoPagesContinuous）、SidebarMode、IsToolbarVisible。方法与命令覆盖 LoadDocumentAsync、SaveDocumentAsync、GoToPage、FitWidth、SearchAsync、HighlightSelectionAsync，以及 OpenCommand、PrintCommand、ZoomInCommand 等可绑定的命令。事件有 DocumentLoaded、LoadError、PageChanged、ZoomChanged、SearchCompleted、LinkClicked 等。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 或 Enterprise 授权，项目文件里要写 AvaloniaUILicenseKey 并引用 Avalonia.Controls.PdfViewer 包（用 PDFium 还要带第三方署名）。请在真实项目里按官方文档接入。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/PdfViewerSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PdfViewerSignpostPage : UserControl
    {
        public PdfViewerSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 2: Markdown 与 TreeDataGrid**

#### `Avalonia.ControlsDemo/Views/Pages/Premium/MarkdownSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MarkdownSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Markdown：渲染 Markdown 文本"
                               DocPath="controls/data-display/text-display/markdown" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/data-display/text-display/markdown"
                                License="Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）"
                                SubPages="官方子页 4 页：控件总览、Markdown 样式、CodeHighlighter 代码高亮、ImageLoader 图片加载"
                                Summary="把一段 Markdown 源码渲染成带样式的富文本，主要属性只有 Text，另有 SelectionBrush、CaretBrush、只读的 SelectedText 与 CanCopy，以及流式输出时保持视口在底部的 AutoScrollToEnd。选中的文字可以像普通文本控件一样复制。样式、代码高亮与图片加载各自可替换，对应官方文档里的三个子页。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 或更高授权，项目文件里要写 AvaloniaUILicenseKey 并引用对应的 Markdown 包。官方子页里有样式、代码高亮与图片加载的写法。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/MarkdownSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MarkdownSignpostPage : UserControl
    {
        public MarkdownSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/TreeDataGridSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TreeDataGridSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TreeDataGrid：树与表格合一"
                               DocPath="controls/data-display/structured-data/treedatagrid" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/data-display/structured-data/treedatagrid"
                                License="Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）"
                                SubPages="官方子页 7 页：控件总览、列类型、选择模式、展开与折叠、排序、过滤、v12 破坏性变更"
                                Summary="同时具备 TreeView 的层级与 DataGrid 的列：数据源分两种，扁平用 FlatTreeDataGridSource〈T〉（二维表格），层级用 HierarchicalTreeDataGridSource〈T〉（可展开的树加列）。列有 TreeDataGridTextColumn 与 TreeDataGridHierarchicalExpanderColumn（用 ChildrenBinding 找子项），也可用 WithTextColumn、WithHierarchicalExpanderTextColumn 在代码里搭。SelectionMode 默认 Row，可选 Cell 或 Row,Multiple；CanUserSortColumns 默认开，CanUserResizeColumns 默认关。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 或更高授权，项目文件里要写 AvaloniaUILicenseKey。注意它是独立的付费控件，与免费主包里的 TableView 不是一回事。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/TreeDataGridSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TreeDataGridSignpostPage : UserControl
    {
        public TreeDataGridSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: RichTextEditor、MediaPlayer 与 VirtualKeyboard**

#### `Avalonia.ControlsDemo/Views/Pages/Premium/RichTextEditorSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.RichTextEditorSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="RichTextEditor：所见即所得的富文本编辑"
                               DocPath="controls/input/text-input/richtexteditor" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/input/text-input/richtexteditor"
                                License="Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）"
                                SubPages="官方子页 11 页：控件总览、文档查看器、工具条与选择浮出、分页、页眉页脚、脚注、PDF 导出、Markdown 序列化、扩展模式、性能调优、线程安全"
                                Summary="一个基于 FlowDocument 的富文本编辑器与查看器。编辑器属性有 Document、IsReadOnly、AcceptsReturn、ViewMode（Continuous 或 PageLayout）、Toolbar 与 ShowToolbar、PageSize / PageMargins / PageGap，方法有 Load / LoadAsync、Save / SaveAsync，事件是 ContentChanged。文档内容由块（Paragraph、List、Table、Section）与内联（RichRun、RichBold、RichImage、RichHyperlink）组成。格式方面 RTF、DOCX、XAML、Markdown、纯文本可读可写，HTML 只读，PDF 只写。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 或更高授权，项目文件里要写 AvaloniaUILicenseKey。官方子页里有工具条、分页、页眉页脚与 PDF 导出的写法。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/RichTextEditorSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RichTextEditorSignpostPage : UserControl
    {
        public RichTextEditorSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/MediaPlayerSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MediaPlayerSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="MediaPlayer：音频与视频播放"
                               DocPath="controls/media/mediaplayer" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/media/mediaplayer"
                                License="Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）"
                                SubPages="官方子页 4 页：控件总览、媒体源（UriSource 与 StreamSource）、播放控制与命令、错误处理"
                                Summary="播放音频与视频的控件 MediaPlayerControl，内部包着一个 MediaPlayer。播放相关有 Source（UriSource 或 StreamSource）、LoadedBehavior（AutoPlay 或 Manual）、Position、Duration、SkipTime；状态有 IsBuffering、BufferProgress、IsPaused、IsMediaActive、HasVideo、IsSeekable；音量有 Volume 与 IsMuted。播放控制既可直接调也要以命令绑定：PlayPauseCommand、StopCommand、MuteCommand、SkipForwardCommand、SkipBackwardCommand。出错时触发 ErrorOccurred，把 args.Handled 设为 true 可以拦下异常。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 或更高授权，项目文件里要写 AvaloniaUILicenseKey，并在 App.axaml 里引用 MediaFluentTheme。注意它要在加载之后再设 Source（不是构造函数里）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/MediaPlayerSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MediaPlayerSignpostPage : UserControl
    {
        public MediaPlayerSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/VirtualKeyboardSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.VirtualKeyboardSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="VirtualKeyboard：屏幕软键盘"
                               DocPath="controls/input/text-input/virtualkeyboard" />
            <TextBlock Classes="caption" Text="路标：本仓库暂时没有这一项的演示" />
            <local:SignpostView DocPath="controls/input/text-input/virtualkeyboard"
                                License="Avalonia Pro 或更高（官方同时标 Pro 与 Enterprise）"
                                SubPages="官方子页 3 页：控件总览、VirtualKeyboard 控件参考、VirtualKeyboardScope 容器"
                                Summary="为触摸设备提供屏幕软键盘，有三个类型：VirtualKeyboardScope 是容器，随其中的文本输入控件获得或失去焦点自动显示与隐藏（推荐用法）；VirtualKeyboard 是可以手动放进布局的键盘控件，用 Target 指向接收输入的元素；VirtualKeyboardInputMethod 代表一种输入法或键盘布局，本身不是可视控件。InputMethods 可以同时启用多个布局（如“en-US:kbd:standard, de:kbd:standard, ja:ime:kana”）。附着的 TextInputOptions.ContentType 与 ReturnKeyType 可以按文本框调整输入行为。"
                                Target="暂无可运行演示：这一组需要 Avalonia Pro 或更高授权，项目文件里要写 AvaloniaUILicenseKey，并引用 Avalonia.Controls.VirtualKeyboard 包（还要在 App.axaml 里带 Fluent 主题）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Premium/VirtualKeyboardSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class VirtualKeyboardSignpostPage : UserControl
    {
        public VirtualKeyboardSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 4: 抽取并构建**

Run: 同 Task 1 Step 4。
Expected: 0 个错误。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the seven premium control signpost pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 3: 横切 How-to 的 9 个路标页

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/ResponsiveLayoutSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/StylingControlsSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/ThemeSwitchingSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/CustomFontSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/MvvmSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/CodedUiSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/DragAndDropSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/ClipboardSignpostPage.axaml` 与 `.axaml.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/HowTo/DebuggingSignpostPage.axaml` 与 `.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 `SignpostView`、第 00 份的 `DemoHeader` 与 `SharedStyles`
- Produces: 9 个横切路标页类（Task 4 登记）

- [ ] **Step 1: 响应式布局、样式与主题（4 页）**

这 9 页的特征是**有可运行演示**（就在现有项目里），所以 `Target` 写清"项目 →（Tab 名）"，`RunLine` 写 `dotnet run --project <项目>`。`License` 留空（自动隐藏），`SubPages` 也留空。上面的 `Header` 已经逐个在本仓库的 `MainWindow.axaml` 里核对过。

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/ResponsiveLayoutSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ResponsiveLayoutSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="响应式布局"
                               DocPath="docs/how-to/responsive-layout-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.LayoutDemo" />
            <local:SignpostView DocPath="docs/how-to/responsive-layout-how-to"
                                Summary="让界面随窗口大小改变排布。常用手段是 AdaptiveGrid、WrapPanel 与相对尺寸，以及用属性样式按控件实际宽度切换布局。这里不重复实现，指向已有演示。"
                                Target="Avalonia.LayoutDemo →「响应式布局」"
                                RunLine="dotnet run --project Avalonia.LayoutDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/ResponsiveLayoutSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ResponsiveLayoutSignpostPage : UserControl
    {
        public ResponsiveLayoutSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/StylingControlsSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.StylingControlsSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="给控件写样式"
                               DocPath="docs/how-to/styling-controls-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.StylingDemo" />
            <local:SignpostView DocPath="docs/how-to/styling-controls-how-to"
                                Summary="用样式选择器给控件设定外观，用样式类给同类控件分组，用 ControlTheme 替换整个控件模板。这三件事互相配合：选择器决定“命中谁”，样式类决定“哪一组”，ControlTheme 决定“长什么样”。"
                                Target="Avalonia.StylingDemo →「选择器」「样式类」「ControlTheme」"
                                RunLine="dotnet run --project Avalonia.StylingDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/StylingControlsSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class StylingControlsSignpostPage : UserControl
    {
        public StylingControlsSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/ThemeSwitchingSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ThemeSwitchingSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="切换主题"
                               DocPath="docs/how-to/theme-switching-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.StylingDemo" />
            <local:SignpostView DocPath="docs/how-to/theme-switching-how-to"
                                Summary="在 Light 与 Dark 之间切换，或在局部区域强制某个变体（本项目 Primitives 分类的 ThemeVariantScope 页演示了局部作用域的用法）。切换时用 DynamicResource 引用主题资源的控件会立即重取颜色，所以写资源时要选对 Static 与 Dynamic。"
                                Target="Avalonia.StylingDemo →「主题变体」"
                                RunLine="dotnet run --project Avalonia.StylingDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/ThemeSwitchingSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ThemeSwitchingSignpostPage : UserControl
    {
        public ThemeSwitchingSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/CustomFontSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CustomFontSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="使用自定义字体"
                               DocPath="docs/how-to/custom-font-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.StylingDemo" />
            <local:SignpostView DocPath="docs/how-to/custom-font-how-to"
                                Summary="把字体文件作为资源嵌入，再用 FontFamily 的 avares 路径引用它，或用一个 FontFamily 的默认值给整棵树换字体。字体资源可以只在需要的地方声明，也可以放 Application 资源里全局生效。"
                                Target="Avalonia.StylingDemo →「字体」"
                                RunLine="dotnet run --project Avalonia.StylingDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/CustomFontSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CustomFontSignpostPage : UserControl
    {
        public CustomFontSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 2: MVVM、纯代码 UI、拖放、剪贴板、调试（5 页）**

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/MvvmSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MvvmSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="用 MVVM 组织代码"
                               DocPath="docs/how-to/mvvm-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.FundamentalsDemo" />
            <local:SignpostView DocPath="docs/how-to/mvvm-how-to"
                                Summary="把界面拆成 View 与 ViewModel，中间用绑定连接：ViewModel 暴露可观察属性与命令，View 只负责呈现。本项目自己也全部用 CommunityToolkit.Mvvm 的 [ObservableProperty] 与 [RelayCommand] 写。"
                                Target="Avalonia.FundamentalsDemo →「MVVM」"
                                RunLine="dotnet run --project Avalonia.FundamentalsDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/MvvmSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MvvmSignpostPage : UserControl
    {
        public MvvmSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/CodedUiSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CodedUiSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="用纯代码搭界面"
                               DocPath="docs/how-to/coded-ui-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.FundamentalsDemo" />
            <local:SignpostView DocPath="docs/how-to/coded-ui-how-to"
                                Summary="不用 XAML，直接在 C# 里 new 出控件树、设属性、挂事件、填 Children。适合动态生成的内容，或者把界面代码当普通代码来调试、重构与单元测试。"
                                Target="Avalonia.FundamentalsDemo →「纯代码 UI」"
                                RunLine="dotnet run --project Avalonia.FundamentalsDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/CodedUiSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CodedUiSignpostPage : UserControl
    {
        public CodedUiSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/DragAndDropSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DragAndDropSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="拖放"
                               DocPath="docs/how-to/drag-and-drop-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.InputDemo" />
            <local:SignpostView DocPath="docs/how-to/drag-and-drop-how-to"
                                Summary="从拖动的源头调 DragDrop.DoDragDrop 起手，落到目标的 DragOver 与 Drop 事件里处理数据；同一个应用内拖放用自定义数据格式最省事，跨应用则要挂靠系统的剪贴板格式。"
                                Target="Avalonia.InputDemo →「拖放」"
                                RunLine="dotnet run --project Avalonia.InputDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/DragAndDropSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DragAndDropSignpostPage : UserControl
    {
        public DragAndDropSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/ClipboardSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ClipboardSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="剪贴板"
                               DocPath="docs/how-to/clipboard-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.ServicesDemo" />
            <local:SignpostView DocPath="docs/how-to/clipboard-how-to"
                                Summary="从 TopLevel.Clipboard 读写数据：SetTextAsync / GetTextAsync 处理纯文本，需要富格式时用 DataObject 装多个格式一起放进剪贴板，粘出的一端挑它认识的格式。"
                                Target="Avalonia.ServicesDemo →「剪贴板」"
                                RunLine="dotnet run --project Avalonia.ServicesDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/ClipboardSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ClipboardSignpostPage : UserControl
    {
        public ClipboardSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/DebuggingSignpostPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:local="using:Avalonia.ControlsDemo.Views.Pages"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DebuggingSignpostPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="调试应用"
                               DocPath="docs/how-to/debugging-how-to" />
            <TextBlock Classes="caption" Text="路标：完整演示在 Avalonia.AppDevelopmentDemo" />
            <local:SignpostView DocPath="docs/how-to/debugging-how-to"
                                Summary="统一接住日志与未处理异常：用日志抽象（Logger）把诊断信息写出去，在 App 上接 DispatcherUnhandledException 与 UnhandledException，把崩溃信息记下来再决定是吞掉还是退出，而不是让它静默消失。"
                                Target="Avalonia.AppDevelopmentDemo →「日志」「未处理异常」"
                                RunLine="dotnet run --project Avalonia.AppDevelopmentDemo" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/HowTo/DebuggingSignpostPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DebuggingSignpostPage : UserControl
    {
        public DebuggingSignpostPage()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: 抽取并构建**

Run: 同 Task 1 Step 4。
Expected: 0 个错误。

- [ ] **Step 4: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the nine cross-cutting how-to signpost pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: 登记 20 个路标页、探针验证与整套目录收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Web.cs`、`PageCatalog.Premium.cs`、`PageCatalog.HowToSignposts.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Sign.cs`、`Probe.Sign2.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `README.md`、`docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）、`CLAUDE.md`

**Interfaces:**
- Consumes: Task 1–3 的 20 个路标页类、`SignpostView`、第 00 份的 `SignpostPage<T>`、`Harness`
- Produces: Web 4 个路标、付费控件 7 个路标、How-to 路标 9 个，共 20 个 `PageKind.Signpost` 页

- [ ] **Step 1: 登记**

路标页的标题写纯主题名（如 `NativeWebView`、`响应式布局`），导航树自动加 `↗ ` 前缀（`NavNode.DisplayTitle`）。How-to 路标的 `DocPath` 一律 `docs/how-to/<slug>-how-to`；Web 与付费控件用官方 `controls/...` 原路径。

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Web.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddWeb(List<PageEntry> list)
        {
            const string c = Categories.Web;

            // No runnable demo lives in this project: Avalonia.WebViewDemo covers two of the four.
            list.Add(SignpostPage<NativeWebViewSignpostPage>("NativeWebView", c, "controls/web/nativewebview"));
            list.Add(SignpostPage<NativeWebDialogSignpostPage>("NativeWebDialog", c, "controls/web/nativewebdialog"));
            list.Add(SignpostPage<WebAuthenticationBrokerSignpostPage>("WebAuthenticationBroker", c, "controls/web/webauthenticationbroker"));
            list.Add(SignpostPage<WebViewEnvironmentSignpostPage>("WebView 环境配置", c, "controls/web/webview-environment"));
        }
    }
}
```

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Premium.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddPremium(List<PageEntry> list)
        {
            const string c = Categories.Premium;

            // Official docs paths, kept verbatim even though the pages sit under one category here.
            list.Add(SignpostPage<ChartsSignpostPage>("Charts", c, "controls/data-display/charts"));
            list.Add(SignpostPage<PdfViewerSignpostPage>("PdfViewer", c, "controls/data-display/pdfviewer"));
            list.Add(SignpostPage<MarkdownSignpostPage>("Markdown", c, "controls/data-display/text-display/markdown"));
            list.Add(SignpostPage<TreeDataGridSignpostPage>("TreeDataGrid", c, "controls/data-display/structured-data/treedatagrid"));
            list.Add(SignpostPage<RichTextEditorSignpostPage>("RichTextEditor", c, "controls/input/text-input/richtexteditor"));
            list.Add(SignpostPage<VirtualKeyboardSignpostPage>("VirtualKeyboard", c, "controls/input/text-input/virtualkeyboard"));
            list.Add(SignpostPage<MediaPlayerSignpostPage>("MediaPlayer", c, "controls/media/mediaplayer"));
        }
    }
}
```

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.HowToSignposts.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddHowToSignposts(List<PageEntry> list)
        {
            const string c = Categories.HowToSignposts;

            // The nine cross-cutting how-tos whose subject is not a single control; each points at an existing demo.
            list.Add(SignpostPage<ResponsiveLayoutSignpostPage>("响应式布局", c, "docs/how-to/responsive-layout-how-to"));
            list.Add(SignpostPage<StylingControlsSignpostPage>("给控件写样式", c, "docs/how-to/styling-controls-how-to"));
            list.Add(SignpostPage<ThemeSwitchingSignpostPage>("切换主题", c, "docs/how-to/theme-switching-how-to"));
            list.Add(SignpostPage<CustomFontSignpostPage>("使用自定义字体", c, "docs/how-to/custom-font-how-to"));
            list.Add(SignpostPage<MvvmSignpostPage>("用 MVVM 组织代码", c, "docs/how-to/mvvm-how-to"));
            list.Add(SignpostPage<CodedUiSignpostPage>("用纯代码搭界面", c, "docs/how-to/coded-ui-how-to"));
            list.Add(SignpostPage<DragAndDropSignpostPage>("拖放", c, "docs/how-to/drag-and-drop-how-to"));
            list.Add(SignpostPage<ClipboardSignpostPage>("剪贴板", c, "docs/how-to/clipboard-how-to"));
            list.Add(SignpostPage<DebuggingSignpostPage>("调试应用", c, "docs/how-to/debugging-how-to"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

两个文件，都是 `static partial class ProbeSign`：`Probe.Sign.cs` 做整套目录的收尾断言（总数、分类、顺序、可导航性）并逐页核对三处 `DocPath` 一致；`Probe.Sign2.cs` 专测 `SignpostView` 的行为与点「在浏览器中打开」在 headless 下的结果。

**探针要守住的两条底线**：① 20 个路标页都要能实例化、渲染并显示正确的 `DemoHeader.DocPath`；② 每个路标页里的 `SignpostView.DocPath` 与 `DemoHeader.DocPath`、`PageCatalog` 登记的 `DocPath` 三处**逐字相等**——这是"不允许指向死链接"的机械保证。

**`C:\Temp\probe-controls\Probe.Sign.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;

static partial class ProbeSign
{
    static readonly string[] SignpostSlugs =
    {
        "responsive-layout", "styling-controls", "theme-switching", "custom-font",
        "mvvm", "coded-ui", "drag-and-drop", "clipboard", "debugging"
    };

    public static void Run()
    {
        WholeCatalog();
        Signposts();
        SignpostViewCases();
        PathEdges();
        PremiumAndWebLabels();
    }

    static IReadOnlyList<PageEntry> Of(string category) =>
        PageCatalog.All.Where(e => e.Category == category).ToList();

    static void WholeCatalog()
    {
        var all = PageCatalog.All;
        Harness.Check("Sign: the whole catalog has 119 pages", all.Count == 119, all.Count);

        int controls = all.Count(e => e.Kind == PageKind.Control);
        int howtos = all.Count(e => e.Kind == PageKind.HowTo);
        int signposts = all.Count(e => e.Kind == PageKind.Signpost);
        Harness.Check("Sign: 80 runnable control pages", controls == 80, controls);
        Harness.Check("Sign: 19 how-to practice pages", howtos == 19, howtos);
        Harness.Check("Sign: 20 signpost pages", signposts == 20, signposts);
        Harness.Check("Sign: 80 + 19 + 20 = 119", all.Count == controls + howtos + signposts, $"{controls + howtos + signposts}");

        Harness.Check("Sign: Web has 4 signposts", Of(Categories.Web).Count == 4, Of(Categories.Web).Count);
        Harness.Check("Sign: Premium has 7 signposts", Of(Categories.Premium).Count == 7, Of(Categories.Premium).Count);
        Harness.Check("Sign: How-to signposts has 9", Of(Categories.HowToSignposts).Count == 9, Of(Categories.HowToSignposts).Count);

        var everySignpost = all.Where(e => e.Kind == PageKind.Signpost).ToList();
        Harness.Check("Sign: every signpost is in Web, Premium or How-to signposts",
            everySignpost.All(e => e.Category == Categories.Web || e.Category == Categories.Premium || e.Category == Categories.HowToSignposts),
            string.Join(",", everySignpost.Select(e => e.Category).Distinct()));

        Harness.Check("Sign: the nine how-to signposts use docs/how-to/<slug>-how-to",
            Of(Categories.HowToSignposts).All(e => e.DocPath.StartsWith("docs/how-to/") && e.DocPath.EndsWith("-how-to")),
            string.Join(",", Of(Categories.HowToSignposts).Select(e => e.DocPath)));

        var slugs = Of(Categories.HowToSignposts)
            .Select(e => e.DocPath.Replace("docs/how-to/", "").Replace("-how-to", "")).OrderBy(s => s).ToList();
        Harness.Check("Sign: those nine slugs are exactly the expected set",
            string.Join(",", slugs) == string.Join(",", SignpostSlugs.OrderBy(s => s)),
            string.Join(",", slugs));

        Harness.Check("Sign: every title in the whole catalog is unique across a category",
            all.GroupBy(e => (e.Category, e.Title, e.Kind)).All(g => g.Count() == 1),
            string.Join(",", all.GroupBy(e => (e.Category, e.Title, e.Kind)).Where(g => g.Count() > 1).Select(g => g.Key.ToString())));

        Harness.Check("Sign: every DocPath in the whole catalog is non-empty",
            all.All(e => !string.IsNullOrWhiteSpace(e.DocPath)));
    }

    static void Signposts()
    {
        var signposts = PageCatalog.All.Where(e => e.Kind == PageKind.Signpost).ToList();

        foreach (var entry in signposts)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);

            Harness.Check($"Sign/{entry.Title}: renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);

            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Sign/{entry.Title}: header shows the catalog DocPath", header?.DocPath == entry.DocPath, header?.DocPath);

            var body = page.GetVisualDescendants().OfType<SignpostView>().FirstOrDefault();
            Harness.Check($"Sign/{entry.Title}: has a SignpostView", body is not null);
            Harness.Check($"Sign/{entry.Title}: SignpostView DocPath matches the header and the catalog",
                body?.DocPath == entry.DocPath && body?.DocPath == header?.DocPath, body?.DocPath);
            Harness.Check($"Sign/{entry.Title}: has a summary", !string.IsNullOrWhiteSpace(body?.Summary));

            w.Close();
        }

        // The two Web pages that point at Avalonia.WebViewDemo carry a run line; the other two do not.
        foreach (var title in new[] { "NativeWebView", "NativeWebDialog" })
        {
            var entry = signposts.Single(e => e.Title == title);
            var w = Harness.Show(entry.CreatePage());
            var body = w.Content is Control c ? c.GetVisualDescendants().OfType<SignpostView>().First() : null;
            Harness.Check($"Sign/{title}: names the WebViewDemo and gives a run command",
                body?.Target?.Contains("Avalonia.WebViewDemo") == true && body.RunLine == "dotnet run --project Avalonia.WebViewDemo",
                $"{body?.Target} | {body?.RunLine}");
            w.Close();
        }

        // The two unfinished Web topics say so honestly, with no run line.
        foreach (var title in new[] { "WebAuthenticationBroker", "WebView 环境配置" })
        {
            var entry = signposts.Single(e => e.Title == title);
            var w = Harness.Show(entry.CreatePage());
            var body = w.Content is Control c ? c.GetVisualDescendants().OfType<SignpostView>().First() : null;
            Harness.Check($"Sign/{title}: says there is no runnable demo here and offers no run command",
                body?.Target?.StartsWith("暂无可运行演示") == true && string.IsNullOrEmpty(body.RunLine),
                $"{body?.Target} | {body?.RunLine}");
            w.Close();
        }
    }

    static void SignpostViewCases()
    {
        var view = new SignpostView();
        var w = Harness.Show(view);

        Harness.Check("SignpostView: with only a summary and a path, all three optional rows are hidden",
            Harness.Find<StackPanel>(view, "LicenseRow").IsVisible == false
            && Harness.Find<StackPanel>(view, "SubPagesRow").IsVisible == false
            && Harness.Find<StackPanel>(view, "TargetRow").IsVisible == false
            && Harness.Find<StackPanel>(view, "RunRow").IsVisible == false,
            "rows");

        view.Summary = "这是一段说明。";
        view.DocPath = "controls/web/nativewebview";
        Harness.Check("SignpostView: the summary shows and the URL is the doc base plus the path",
            Harness.Find<TextBlock>(view, "SummaryText").Text == "这是一段说明。"
            && Harness.Find<SelectableTextBlock>(view, "UrlText").Text == "https://docs.avaloniaui.net/controls/web/nativewebview",
            Harness.Find<SelectableTextBlock>(view, "UrlText").Text);
        Harness.Check("SignpostView: a path enables the open button", Harness.Find<Button>(view, "OpenButton").IsEnabled);

        view.License = "Avalonia Pro";
        view.SubPages = "7 页";
        view.Target = "某个项目 →「某个 Tab」";
        view.RunLine = "dotnet run --project Something";
        Harness.Check("SignpostView: setting a value shows its row and fills its text",
            Harness.Find<StackPanel>(view, "LicenseRow").IsVisible && Harness.Find<TextBlock>(view, "LicenseText").Text == "Avalonia Pro"
            && Harness.Find<StackPanel>(view, "SubPagesRow").IsVisible && Harness.Find<TextBlock>(view, "SubPagesText").Text == "7 页"
            && Harness.Find<StackPanel>(view, "TargetRow").IsVisible && Harness.Find<TextBlock>(view, "TargetText").Text == "某个项目 →「某个 Tab」"
            && Harness.Find<StackPanel>(view, "RunRow").IsVisible && Harness.Find<SelectableTextBlock>(view, "RunText").Text == "dotnet run --project Something",
            "rows");

        view.Target = "";
        Harness.Check("SignpostView: clearing a value hides its row again", Harness.Find<StackPanel>(view, "TargetRow").IsVisible == false
            && Harness.Find<StackPanel>(view, "LicenseRow").IsVisible, "rows");

        Harness.Find<Button>(view, "OpenButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Harness.Check("SignpostView: headless has a Launcher but it declines, and the page says so",
            Harness.Find<TextBlock>(view, "LaunchText").Text == "系统没有接受这个请求（没有可用的浏览器，或当前平台不支持 Launcher）",
            Harness.Find<TextBlock>(view, "LaunchText").Text);

        Harness.Check("SignpostView: DocBase is the official docs root", SignpostView.DocBase == "https://docs.avaloniaui.net/");
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Sign2.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System.Linq;

static partial class ProbeSign
{
    // A signpost with an unknown doc path still builds a URL, and clearing the path disables the button.
    static void PathEdges()
    {
        var view = new SignpostView();
        var w = Harness.Show(view);

        Harness.Check("SignpostView: with no path the URL is empty and the button is disabled",
            string.IsNullOrEmpty(Harness.Find<SelectableTextBlock>(view, "UrlText").Text) && !Harness.Find<Button>(view, "OpenButton").IsEnabled);

        view.DocPath = "controls/media/image";
        Harness.Check("SignpostView: the whole doc URL is the base plus the path, with no doubling",
            Harness.Find<SelectableTextBlock>(view, "UrlText").Text == "https://docs.avaloniaui.net/controls/media/image",
            Harness.Find<SelectableTextBlock>(view, "UrlText").Text);
        w.Close();
    }

    // Each of the twenty pages carries the summary the catalog needs, and the Web/Premium pages are labeled by licence.
    static void PremiumAndWebLabels()
    {
        var premium = PageCatalog.All.Where(e => e.Category == Categories.Premium).ToList();
        foreach (var entry in premium)
        {
            var w = Harness.Show(entry.CreatePage());
            var body = w.Content is Control c ? c.GetVisualDescendants().OfType<SignpostView>().First() : null;
            Harness.Check($"Sign/Premium/{entry.Title}: names a licence and a sub-page count",
                !string.IsNullOrWhiteSpace(body?.License) && !string.IsNullOrWhiteSpace(body?.SubPages),
                $"{body?.License} | {body?.SubPages}");
            Harness.Check($"Sign/Premium/{entry.Title}: says there is no runnable demo and gives no run command",
                body?.Target?.StartsWith("暂无可运行演示") == true && string.IsNullOrEmpty(body.RunLine), $"{body?.Target}");
            w.Close();
        }

        var charts = premium.Single(e => e.Title == "Charts");
        var w2 = Harness.Show(charts.CreatePage());
        var chartsBody = w2.Content is Control c2 ? c2.GetVisualDescendants().OfType<SignpostView>().First() : null;
        Harness.Check("Sign/Premium/Charts: only Pro, unlike the other six which are also Enterprise",
            chartsBody?.License == "Avalonia Pro（官方只标 Pro，不含 Enterprise）", chartsBody?.License);
        w2.Close();
    }
}
```

（本文件与 `Probe.Sign.cs` 是同一个 `static partial class ProbeSign`，`Run()` 在那边定义了调用顺序。）

- [ ] **Step 3: 接入探针入口并运行**

在 `C:\Temp\probe-controls\Program.cs` 里，`ProbePrim.Run();` 之后加一行 `ProbeSign.Run();`。

Run: `cd C:\Temp\probe-controls && dotnet run 2>&1 | grep -E "FAIL|Unhandled|   at |warning log|failed|passed"`
Expected: 末行 `1014 passed, 0 failed, 0 warning log(s)`（累计数，含第 00–06 份；本份新增 141 条）。出现 `FAIL` 时先判断是页面缺陷还是探针写错，改对应的代码块再重新抽取，不要放宽断言。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|个错误|Build succeeded"`
Expected: 0 个错误，且相比构建前没有新增警告。

- [ ] **Step 5: README 收尾**

`README.md` 的项目表里，`Avalonia.ControlsDemo` 那一行现在写的是"10 个分类"；把那行改成同时说明路标与来源。

#### `README.md`（只改这一行）

把

```markdown
| [Avalonia.ControlsDemo](Avalonia.ControlsDemo) | 按官方 Controls 目录组织的控件演示（10 个分类，侧边导航）；Pro / Enterprise 控件只有路标页 |
```

改为

```markdown
| [Avalonia.ControlsDemo](Avalonia.ControlsDemo) | 按官方 Controls 目录组织的控件演示（80 个可运行控件页 + 19 个实战页 + 20 个路标页，侧边导航） |
```

再在同一节里追加一张对照表（放在项目表之后、下一节之前）：

#### `README.md`（在项目表之后追加）

```markdown
### 控件目录对照表（Avalonia.ControlsDemo）

| 分类 | 页面数 | 说明 |
|---|---|---|
| Input | 26 | 22 个控件页 + 4 个实战页 |
| Layout | 23 | 20 个控件页 + 3 个实战页 |
| Data display | 16 | 12 个控件页 + 4 个实战页 |
| Feedback | 5 | 4 个控件页 + 1 个实战页 |
| Media | 4 | 3 个控件页 + 1 个实战页 |
| Menus | 6 | 5 个控件页 + 1 个实战页 |
| Navigation | 8 | 8 个控件页 |
| Primitives | 7 | 5 个控件页 + 2 个实战页 |
| System | 2 | 1 个控件页 + 1 个实战页 |
| Web | 4 | 4 个路标页（指向 `Avalonia.WebViewDemo`） |
| 付费控件 | 7 | 7 个路标页（Pro / Enterprise，只在浏览器里打开官方文档） |
| How-to 路标 | 9 | 9 个路标页（指向本仓库其他演示项目） |
| **合计** | **119** | 80 个可运行控件页 + 19 个实战页 + 20 个路标页 |

导航树里，实战页标题带「实战：」前缀，路标页带「↗」前缀；路标页只说明用途与指向，不重复实现演示。
```

- [ ] **Step 6: 回填 spec 与 CLAUDE.md**

在 spec 末尾"批 5 实测结论"之后、"验证标准"之前追加下面这一节：

```markdown
### 批 6 实测结论（2026-10-11）

- **路标页不需要任何新包**：20 个路标页只是 `DemoHeader` 加一个共用的 `SignpostView`，付费控件页只写说明与官方链接，不引用任何商业授权包。
- **`TopLevel.Launcher` 在 headless 里是 `NoopLauncher`**：`LaunchUriAsync` 同步完成并返回 `False`，**不会真的启动浏览器**，所以"在浏览器中打开"按钮可以在探针里放心点。
- **三处 `DocPath` 必须逐字一致**：`SignpostView.DocPath`、同页 `DemoHeader.DocPath`、`PageCatalog` 登记的 `DocPath`。探针逐页断言，避免以后改名后路标悬空。
- **路标里每条"指向"都指向真实存在的 Tab**：探针直接读对应项目 `MainWindow.axaml` 里的 `Header="…"` 核对（见下）。
- **付费控件的官方位置都在 `controls/...` 下**：Charts 在 `controls/data-display/charts`、PdfViewer 在 `controls/data-display/pdfviewer`、TreeDataGrid 在 `controls/data-display/structured-data/treedatagrid`、Markdown 在 `controls/data-display/text-display/markdown`、RichTextEditor 在 `controls/input/text-input/richtexteditor`、VirtualKeyboard 在 `controls/input/text-input/virtualkeyboard`、MediaPlayer 在 `controls/media/mediaplayer`；`DocPath` 保留官方原路径，只在导航树上统一收进"付费控件"分类。**这就是前几批记录的"范围缺口"的落点。**
- **授权标签**：Charts 只标 Pro；其余六组同时标 Pro 与 Enterprise。
- **整套目录**：80 个可运行控件页 + 19 个实战页 + 20 个路标页 = 119 页。
- **官方 How-to 与 12.1.2 的出入**：无。
- **探针断言实际条数**：本批累计 1014 条（含第 00–06 份），全部通过且零警告日志。
```

**核对 Tab 名的探针断言**（把"指向"钉死；写进 spec 与探针里各一份）。下面每一条都已在编写计划时读过对应文件确认，探针重读一遍防止以后改名：

| 路标 | 项目 | Tab（`Header`） |
|---|---|---|
| 响应式布局 | `Avalonia.LayoutDemo` | 响应式布局 |
| 给控件写样式 | `Avalonia.StylingDemo` | 选择器、样式类、ControlTheme |
| 切换主题 | `Avalonia.StylingDemo` | 主题变体 |
| 使用自定义字体 | `Avalonia.StylingDemo` | 字体 |
| 用 MVVM 组织代码 | `Avalonia.FundamentalsDemo` | MVVM |
| 用纯代码搭界面 | `Avalonia.FundamentalsDemo` | 纯代码 UI |
| 拖放 | `Avalonia.InputDemo` | 拖放 |
| 剪贴板 | `Avalonia.ServicesDemo` | 剪贴板 |
| 调试应用 | `Avalonia.AppDevelopmentDemo` | 日志、未处理异常 |

然后把 `CLAUDE.md`「进行中的工作」一节的现状描述更新为：第 00–07 份计划已写，第 00–06 份已执行并提交，第 07 份待执行（以执行时仓库的实际状态为准）。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo README.md docs/superpowers/specs CLAUDE.md
git commit -m "feat: complete the catalog with Web, premium and how-to signposts

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec 覆盖：**

| spec 要求 | 落点 |
|---|---|
| Web 4 页（NativeWebView、NativeWebDialog、WebAuthenticationBroker、WebView 环境配置），全部为路标，指向 `Avalonia.WebViewDemo` | Task 1、Task 4 Step 1 |
| 付费控件 7 页（Charts、PdfViewer、Markdown、TreeDataGrid、RichTextEditor、MediaPlayer、VirtualKeyboard），只做路标，四项内容：用途 / 授权 / 子页 / 链接 | Task 2、Task 4 Step 2 的 `License`/`SubPages`/`Target` 断言 |
| 横切 How-to 9 篇做成路标，指向已有项目与 Tab | Task 3、Task 4 Step 6 的 Tab 核对表 |
| 路标页不写演示代码（去重规则 4） | 每页只有 `DemoHeader` + `SignpostView`，探针不点任何"运行" |
| 付费控件不引用任何商业授权包（非目标） | 无新增 `PackageReference`，无 `AvaloniaUILicenseKey` |
| 页面总数 = 80 + 19 + 20 = 119 | Task 4 Step 2 的 `WholeCatalog()` |
| README 补"控件目录对照表"并改项目表一行 | Task 4 Step 5 |
| 探针读回属性值、零警告日志、探针在仓库外不提交 | Task 4 Steps 2–3 |

**2. 占位符扫描：** 除 Step 3 与 Step 6 里**有意留空、由执行期实测回填**的 `<N>`（探针累计条数）外，无 TBD / TODO；每个代码步骤都有完整代码块。

**3. 类型一致性：** `SignpostView` 的六个属性名（`Summary`/`License`/`SubPages`/`Target`/`RunLine`/`DocPath`）与 20 个页面里的 XAML 用法、`Probe.Sign*.cs` 里的读写、`ShowRow` 的调用一致；20 个页面类名与 `PageCatalog.*.cs` 登记的 `TPage` 一致；`NavNode.DisplayTitle` 的 `↗ ` 前缀断言只依赖 `PageKind.Signpost`，与标题文本无关。

**4. 已知遗留（不在本份处理）：**
- `Charts` 的官方子页数（128）随官方文档增长会变，路标里按"约"写；探针只断言"非空"，不钉死数字。
- 官方 Window 指南的"透明窗口"等缺口、Web 的两项无演示主题，均如实标注"暂无可运行演示"，不假装有。

<!-- PLAN-END -->
