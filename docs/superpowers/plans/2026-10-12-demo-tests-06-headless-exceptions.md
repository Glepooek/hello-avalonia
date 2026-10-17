# 演示测试 06：MusicStore、WebViewDemo、HtmlRendererDemo 的 Headless 测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 spec 批 6：给三个"较完整的应用"补 H1 冒烟、H2 ViewModel、H3 交互测试，每个应用一个新测试项目；同时修复探针发现的 3 个缺陷（用户 2026-10-17 确认全部修复）。

**Architecture:** 照 `tests/Avalonia.StarterTutorialDemo.Tests` 的样子新建 `Avalonia.MusicStore.Tests`、`Avalonia.WebViewDemo.Tests`、`Avalonia.HtmlRendererDemo.Tests`，登记到 `hello-avalonia.slnx` 的 `/tests/`。按 spec 的风险表，不往 `src/` 加接缝：
- MusicStore 的搜索走 iTunes 网络，所以只测窗口与离线部分（缓存、购买消息流）。缓存目录是相对 CWD 的 `./Cache`，测试临时切换 CWD，并关闭本程序集的测试并行。
- WebView2 在 headless 的 MTA 线程上起不来，所以 `WebViewWindow` 只构造、不 Show。
- HtmlRenderer 用像素和字形断言渲染。

每个任务按同一顺序推进：放入测试（缺陷测试此时红）、修 `src/`、全绿、提交、做破坏检查。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、Avalonia.Skia、CommunityToolkit.Mvvm、xUnit v3（3.2.2）

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 6；批 5 已完成，见「批 5 实测结论」）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**。断言里的界面文字取资源属性（`Resources.btn_BuyAlbum`），因为 MusicStore 的文案随 UI 区域设置变化（zh-CN 是「购买专辑」）；WebView、HtmlRenderer 的英文文案照原文断言。
- 版本只写在根目录 `Directory.Packages.props`。本批不新增包，新 csproj 的 `PackageReference` 不带 `Version`。
- 新项目放 `tests/`，登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`（放在最后一项 `Avalonia.TestingDemo.Tests` 之后）。`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。
- 代码风格沿用已有测试项目：块作用域命名空间 `Avalonia.<应用>.Tests`；测试方法名用英文下划线句式；LF 行尾。
- 不改 `tests/Avalonia.Tests.Common`。本批的小辅助放在各测试项目内：MusicStore 的 `TempCurrentDirectory`；HtmlRenderer 的 `private static` 像素计数。
- `src/` 只允许下面 3 处修复（用户 2026-10-17 确认）：
  - `src/Avalonia.MusicStore/Models/Album.cs`（缺陷 #2、#3）
  - `src/Avalonia.WebViewDemo/Views/WebViewWindow.axaml.cs`（缺陷 #1）

  破坏检查一律在**提交之后**做，临时改的行用 `git checkout -- <文件>` 还原。
- 测试不读写真实的 `%AppData%`/`%LocalAppData%`，也不碰测试输出目录下的 `./Cache`。
  - 凡是会触发 `Album` 读写的测试，都包在 `using var cwd = new TempCurrentDirectory();` 里。
  - 打开窗口的测试在 `finally` 里 `Close()`。
  - 构造了 `MainWindowViewModel`（MusicStore）的测试在 `finally` 里 `WeakReferenceMessenger.Default.UnregisterAll(vm)`。
- 异步结果一律 `await UiHelpers.WaitUntil(...)`（默认 5 秒），不要 `Thread.Sleep` 或固定 `Task.Delay`。
- 不断言网页内容，不断言 iTunes 网络结果（spec 非目标）。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`。提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（本批是 `C:\Temp\probe-b6\`），**永不提交**。
- 此前的 501 个测试始终保持通过。

## Review Focus

按"最可能让使用者踩到"的顺序排列；每条在所属任务里都有对应测试。

1. **页面没加载完就点「3. Call JS」会让应用崩溃**（缺陷 #1）
   - 原因：`WebViewWindow.Receive` 是 `async void`，`InvokeScript` 在首个页面加载完成之前抛 `InvalidOperationException`（`Unable to invoke script before any page was loaded`）。异常经 Dispatcher 漏出，桌面应用直接退出。窗口刚打开、转圈还在时点 #3 就会触发。
   - 测试：Task 2 的 `Call_js_before_the_page_has_loaded_does_not_throw` 先红后绿。
2. **名字里带「.」的专辑重启后丢失**（缺陷 #2）
   - 原因：`LoadCachedAsync` 把"有扩展名"的文件都当封面跳过。`R.E.M. - Monster`、`Mr. Big - Lean Into It` 的扩展名是 `. - Monster`、`. Big - …`，于是购买时写进去了，下次启动却读不出来，而且没有任何提示。
   - 测试：Task 1 的 `An_album_with_a_dot_in_its_name_loads_back` 先红后绿。
3. **覆写缓存不截断，较短的新内容后面残留旧尾巴**（缺陷 #3）
   - 原因：`File.OpenWrite` 不截断。同名专辑再存一次、新 JSON 更短时，文件变成"新 JSON + 旧尾巴"，下次启动 `LoadCachedAsync` 抛 `JsonException`，主窗口的 `async void` 加载直接崩溃。封面同理，会留下旧图的尾部字节。
   - 测试：Task 1 的 `Saving_shorter_data_over_an_album_leaves_a_readable_file` 与 `A_smaller_cover_overwrite_leaves_only_the_new_image` 先红后绿。
4. **窗口关了还在收消息**
   - 原因：`WebViewWindow` 与 MusicStore 的 `MainWindowViewModel` 都注册在进程级的 `WeakReferenceMessenger.Default` 上。漏掉注销，关掉的窗口仍会响应 #3，同一专辑也会被加两次。
   - 测试：Task 2 的 `Window_listens_for_js_calls_until_it_is_closed`；Task 1 的 `Receiving_an_album_adds_it_once_and_saves_it_to_the_cache`。两条都有破坏检查。
5. **图标字体静默回退**
   - 原因：`IconFont` 资源的 URI 写错或字体没打包时，不报错、不出日志，TextBlock 改用系统字体，U+E71A 显示成方框或空白。
   - 测试：Task 3 的 `Icon_text_is_shaped_with_the_bundled_icon_font` 断言实际排版用的字体族名，有破坏检查。

另有两处**已知但本批不修**的问题，只写进回填，不写测试：
- 搜索竞态：快速连打时，旧搜索的结果可能追加在新搜索之后。只能经网络复现，spec 不许加接缝。
- 重复购买：两次搜索得到的同一张专辑是两个 `AlbumViewModel`，`Albums.Contains` 按引用比较，会被重复加入。

---

## 本份已核实的事实（编写计划时在仓库外的探针里实测）

探针在 `C:\Temp\probe-b6\`，分成 `ProbeMusic`、`ProbeWeb`、`ProbeHtml` 三个项目：
- 每个项目都用绝对路径引用仓库的 `src/<应用>` 与 `tests/Avalonia.Tests.Common`。
- 根目录复制了一份 `Directory.Packages.props` 与 `Directory.Build.props`。

**本计划 Task 1–3 的测试代码就是探针里实跑过的成品**，与探针里的同名文件内容一致，`TestAppBuilder.cs` 除外（探针里的命名空间是 `ProbeX`，还多一个 `Notes` 辅助）。

实跑结果：
- **红**：修复前，Music 13 个里恰好 3 个缺陷测试红，Web 6 个里恰好 1 个红，Html 3 个全绿。
- **绿**：3 处修复一起套上后 22 个全绿。修复随后已 `git checkout` 还原，执行时在各任务里重新套用。

### 覆盖清单

| 应用 | H1 冒烟 | H2 ViewModel | H3 交互 | 用例数 |
|---|---|---|---|---|
| MusicStore | 主窗口（无 VM）、商店窗口 | 搜索框清空、购买发消息（有/无选中）、收到专辑去重并写缓存；`Album` 缓存读写 5 条 | 主窗口从缓存加载专辑与封面、商店里点购买 → 关窗 → 主窗口多一张 | 13 |
| WebViewDemo | 主窗口三个按钮、`WebViewWindow` 指向随包页面且显示转圈 | Call JS 命令广播 `Reult = true` | 窗口关前收消息/关后不收、页面未加载时收到 Call JS 不抛、非 JS 消息忽略 | 6 |
| HtmlRendererDemo | 主窗口、标题、无日志 | —（`MainWindowViewModel` 只有一个常量 `Text`，无逻辑可测） | HtmlPanel 真的画出了绑定的 HTML（像素）、图标用随包 iconfont 排版（字形） | 3 |
| **合计** | | | | **22** |

**有意不测的部分**：
- 网络：iTunes 搜索结果、网络封面下载、网页内容与 JS ↔ C# 往返。WebView2 起不来，headless 下没有页面。
- 真实窗口：`ShowWebViewCommand`/`ShowWebDialogCommand`。在 headless 下前者因没有桌面生命周期、`GetMainWindow()` 为 null 而抛 `ArgumentNullException`，后者抛 COM 异常，都是测试环境限制，不是应用缺陷。
- `ShowAlbumsCommand` 的 `ShowDialog`：同样依赖 `GetMainWindow()`。H3 改为直接 `new MusicStoreWindow().Show(main)`。

### 3 个缺陷（用户 2026-10-17 确认全部修复）

| # | 位置 | 复现 | 修复前（实测） | 修复 | 所在任务 |
|---|---|---|---|---|---|
| 1 | `WebViewDemo/Views/WebViewWindow.axaml.cs` `Receive` | 打开 #1 后、页面加载完之前点 #3（headless 下页面永远不加载） | `InvalidOperationException: Unable to invoke script before any page was loaded. Listen for NavigationCompleted event.` 从 `Dispatcher.RunJobs` 漏出 | `InvokeScript` 包进 `try/catch (InvalidOperationException)`，`Debug.WriteLine` 记一行 | Task 2 |
| 2 | `MusicStore/Models/Album.cs` `LoadCachedAsync` | 缓存里有 `R.E.M. - Monster`、`Mr. Big - Lean Into It` | `LoadCachedAsync` 返回空集合 | 跳过条件从"有扩展名"改为"以 `.bmp` 结尾"（`StringComparison.OrdinalIgnoreCase`） | Task 1 |
| 3 | `MusicStore/Models/Album.cs` `SaveAsync`、`SaveCoverBitmapStream` | 同名专辑先存长 JSON 再存短 JSON；封面同理 | JSON：`JsonException: 'e' is invalid after a single JSON value`；封面：新图 806 字节，文件仍是 1048576 字节 | 两处 `File.OpenWrite` → `File.Create` | Task 1 |

### 写测试时踩过的坑（对后续批同样适用）

- **headless 没有桌面生命周期**：`AvaloniaHelper.GetMainWindow()` 返回 null，`App.OnFrameworkInitializationCompleted` 也不会给 `MainWindow` 设 `DataContext`。测试自己 `new MainWindow { DataContext = vm }`；需要 owner 的地方自己传。
- **headless 的 UI 线程是 MTA**：WebView2 的 COM 对象拒绝在这里启动，`NativeWebView` 一 Show 就抛 `COMException`（`RPC_E_CHANGED_MODE`），`NativeWebDialog.Show` 同理。**只构造**是安全的：构造函数里的注册、`Source`、XAML 状态都能断言。不 Show 的窗口照样能 `Close()`，也会触发 `Closed`。
- **`async void` 里同步抛出的异常**会被 post 到 Avalonia 的 SynchronizationContext，在下一次 `Dispatcher.UIThread.RunJobs()` 时重抛。所以 `Record.Exception` 要同时包住 `Send` 和 `RunJobs`。
- **`WeakReferenceMessenger.Default` 是进程级的**：每个 MusicStore `MainWindowViewModel` 一构造就注册。测试结束不注销的话，后面测试发的 `MessageParam` 会被已经没用的 VM 接走，往**另一个测试的**临时目录写缓存。
- **测试用的 `IRecipient<T>` 不能是 `private` 嵌套类**：CommunityToolkit.Mvvm 的 `IMessengerRegisterAllGenerator` 会为它生成代码，`private` 时报 `CS0122`。用 `internal`。
- **`Album` 的缓存路径相对进程 CWD**（默认是测试的 `bin\Debug`）。`Environment.CurrentDirectory` 是进程级的，所以 MusicStore 测试程序集要 `[assembly: CollectionBehavior(DisableTestParallelization = true)]`。别的测试程序集不受影响，它们各是一个进程。
- **MusicStore 的界面文字随 UI 区域设置变**：本机 zh-CN 显示「购买专辑」，en-US 显示 "Buy Album"。断言用 `Assets.Langs.Resources.btn_BuyAlbum`、`Resources.txt_SearchAlbums`。
- **封面文件叫 `.bmp`，里面可以是任何格式**：`Bitmap.DecodeToWidth` 按内容解码，测试用 `RenderTargetBitmap.Save` 写 PNG 就行。`DecodeToWidth(…, 400)` 总是按 400 宽缩放，20×10 的图解出来是 400×200。
- **HtmlPanel 的像素**：帧格式是 `Rgba8888`（按 `px & 0xff` 取 R 也对，"暗"判据对通道顺序不敏感）。带 VM 时面板区域有约 3569 个暗像素（rgb 都 < 100），不带 VM 时为 0，因为面板背景是白的。阈值取 500。
- **字体回退是静默的**：图标 TextBlock 的 `TextLayout.TextLines[*].TextRuns.OfType<ShapedTextRun>()` 的 `GlyphRun.GlyphTypeface.FamilyName` 实测是 `iconfont`，字形号 125。URI 写错时会变成系统字体名。

## 文件结构

**新增**

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.MusicStore.Tests/Avalonia.MusicStore.Tests.csproj` | 测试项目（照 StarterTutorialDemo.Tests） |
| `tests/Avalonia.MusicStore.Tests/TestAppBuilder.cs` | Headless 入口 + 关闭本程序集测试并行 |
| `tests/Avalonia.MusicStore.Tests/TempCurrentDirectory.cs` | 临时 CWD、缓存路径、写测试封面 |
| `tests/Avalonia.MusicStore.Tests/SmokeTests.cs` | H1：主窗口、商店窗口 |
| `tests/Avalonia.MusicStore.Tests/ViewModelTests.cs` | H2：`MusicStoreViewModel`、`MainWindowViewModel` |
| `tests/Avalonia.MusicStore.Tests/CacheTests.cs` | H2：`Album`/`AlbumViewModel` 缓存读写（缺陷 #2、#3） |
| `tests/Avalonia.MusicStore.Tests/InteractionTests.cs` | H3：缓存加载、购买流程 |
| `tests/Avalonia.WebViewDemo.Tests/Avalonia.WebViewDemo.Tests.csproj` | 测试项目 |
| `tests/Avalonia.WebViewDemo.Tests/TestAppBuilder.cs` | Headless 入口 |
| `tests/Avalonia.WebViewDemo.Tests/SmokeTests.cs` | H1：主窗口、`WebViewWindow` 构造 |
| `tests/Avalonia.WebViewDemo.Tests/ViewModelTests.cs` | H2：Call JS 命令 |
| `tests/Avalonia.WebViewDemo.Tests/WebViewWindowTests.cs` | H3：消息注册与缺陷 #1 |
| `tests/Avalonia.HtmlRendererDemo.Tests/Avalonia.HtmlRendererDemo.Tests.csproj` | 测试项目 |
| `tests/Avalonia.HtmlRendererDemo.Tests/TestAppBuilder.cs` | Headless 入口 |
| `tests/Avalonia.HtmlRendererDemo.Tests/SmokeTests.cs` | H1：主窗口 |
| `tests/Avalonia.HtmlRendererDemo.Tests/RenderTests.cs` | H3：HTML 像素、图标字形 |

**修改**

| 文件 | 改动 |
|---|---|
| `src/Avalonia.MusicStore/Models/Album.cs` | 缺陷 #2、#3（Task 1） |
| `src/Avalonia.WebViewDemo/Views/WebViewWindow.axaml.cs` | 缺陷 #1（Task 2） |
| `hello-avalonia.slnx` | `/tests/` 下登记三个新项目（Task 1–3 各一行） |
| `README.md` | 测试项目一行的"目前覆盖"列表补上 MusicStore、WebView、HtmlRenderer（Task 4） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 6 实测结论」（Task 4） |

**不碰**：`tests/Avalonia.Tests.Common`、`Directory.Packages.props`、`Directory.Build.props`、`CLAUDE.md`、其他测试项目。

**代码块抽取**：`CLAUDE.md` 提到的 `C:\Temp\extract-plan.ps1` 已不存在。可以按 `#### \`路径\`` 标注手动 Write，也可以直接从 `C:\Temp\probe-b6\Probe*\` 复制同名文件；`TestAppBuilder.cs` 必须用本计划里的版本。

---

### Task 1: MusicStore 测试项目，并修复带「.」的专辑丢失与缓存覆写不截断（缺陷 #2、#3）

**Files:**
- Create: `tests/Avalonia.MusicStore.Tests/` 下 7 个文件：
  - `Avalonia.MusicStore.Tests.csproj`、`TestAppBuilder.cs`、`TempCurrentDirectory.cs`
  - `SmokeTests.cs`、`ViewModelTests.cs`、`CacheTests.cs`、`InteractionTests.cs`
- Modify: `hello-avalonia.slnx`（`/tests/` 末尾加一行）
- Modify: `src/Avalonia.MusicStore/Models/Album.cs`（`using`、`SaveAsync`、`SaveCoverBitmapStream`、`LoadCachedAsync`）

**Interfaces:**
- Consumes（共享库 `tests/Avalonia.Tests.Common`）：
  - `HeadlessApp.Build<TApp>() : AppBuilder`
  - `LogCollector`（`IDisposable`，有 `Messages`）
  - `UiHelpers.ButtonWithText(this Visual, string) : Button`
  - `UiHelpers.Click(this Window, Control)`
  - `UiHelpers.WaitUntil(Func<bool>, int = 5000) : Task`
- Consumes（`src/Avalonia.MusicStore`，均为 `public`）：
  - `Models.Album(string artist, string title, string coverUrl)`：属性 `Artist`、`Title`、`CoverUrl`；静态 `LoadCachedAsync() : Task<IEnumerable<Album>>`。
  - `ViewModels.AlbumViewModel(Album)`：`Cover : Bitmap?`、`LoadCover() : Task`、`SaveToDiskAsync() : Task`。
  - `ViewModels.MusicStoreViewModel`：`SearchText`、`IsBusy`、`SearchResults`、`SelectedAlbum`、`BuyMusicCommand : RelayCommand<UserControl>`。
  - `ViewModels.MainWindowViewModel`：`Albums`。
  - `Views.MainWindow`、`MusicStoreWindow`、`MusicStoreView`、`AlbumView`。
  - `Assets.Langs.Resources.btn_BuyAlbum`、`txt_SearchAlbums`。
- Consumes（`src/Avalonia.Shared`）：`Messages.MessageParam`（`Reult : bool`、`Data : object?`）
- Produces：`internal sealed class TempCurrentDirectory : IDisposable`，只在本项目内用：
  - `Root : string`
  - `Cache(string fileName) : string`
  - `WriteCover(string fileName, int width, int height)`

逐类覆盖，共 13 个：

| 类 | 用例数 | 内容 |
|---|---|---|
| `SmokeTests` | 2 | 主窗口（无 VM）；商店窗口 |
| `ViewModelTests` | 4 | 清空搜索；无选中购买；有选中购买；收到专辑去重并写缓存 |
| `CacheTests` | 5 | 非法字符往返；**带点的专辑**（#2）；**短 JSON 覆写**（#3）；**小封面覆写**（#3）；本地封面不走网络 |
| `InteractionTests` | 2 | 缓存加载；购买流程 |

几处容易写错的地方（都在探针里踩过）：

- 主窗口的 `OnLoaded` 才执行 `LoadedCommand`。所以：
  - 无 VM 的冒烟测试不会碰缓存，可以断言 `Cache` 目录不存在。
  - 带 VM 的测试要先等 `Cache` 目录出现，或者等专辑出现。
- 点商店里的按钮要用**商店窗口**的 `store.Click(buy)`。用主窗口点，坐标会按主窗口计算，抛 `Nullable object must have a value`。
- `BuyMusicCommand` 的参数是 XAML 传的 UserControl。ViewModel 测试直接 `Execute(null)`：`GetTopLevel(null)?.Close()` 什么都不做，消息照样发出。
- `Receive` 里的 `SaveToDiskAsync` 没有 await，是 fire-and-forget。所以要 `WaitUntil` 文件出现**且非空**，再读回来断言。

- [ ] **Step 1: 建项目并登记到解决方案**

#### `tests/Avalonia.MusicStore.Tests/Avalonia.MusicStore.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <!-- xunit v3 test projects are executables, and Avalonia.Headless.XUnit 12.x is built on v3. -->
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia.Headless.XUnit" />
        <PackageReference Include="xunit.v3" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Avalonia.MusicStore\Avalonia.MusicStore.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.MusicStore.Tests/TestAppBuilder.cs`

```csharp
using Avalonia.Headless;
using Avalonia.Tests.Common;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Avalonia.MusicStore.Tests.TestAppBuilder))]
// Album keeps its cache in "./Cache" relative to the process working directory and the tests point that at a
// temp folder; the working directory is process-wide, so the tests in this assembly run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Avalonia.MusicStore.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 里，`<Project Path="tests/Avalonia.TestingDemo.Tests/Avalonia.TestingDemo.Tests.csproj" />` 一行之后加：

```xml
    <Project Path="tests/Avalonia.MusicStore.Tests/Avalonia.MusicStore.Tests.csproj" />
```

Run: `dotnet build tests/Avalonia.MusicStore.Tests 2>&1 | tail -3`
Expected: 0 个错误。

- [ ] **Step 2: 放入辅助与测试文件**

#### `tests/Avalonia.MusicStore.Tests/TempCurrentDirectory.cs`

```csharp
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace Avalonia.MusicStore.Tests
{
    // Album reads and writes "./Cache" relative to the process working directory, so every test that touches the
    // cache runs inside a fresh temp folder; the previous directory comes back on Dispose.
    internal sealed class TempCurrentDirectory : IDisposable
    {
        private readonly string _previous = Environment.CurrentDirectory;

        public TempCurrentDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "musicstore-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Environment.CurrentDirectory = Root;
        }

        public string Root { get; }

        public string Cache(string fileName) => Path.Combine(Root, "Cache", fileName);

        // Writes a solid PNG; the app names cover files ".bmp" but decodes whatever format is inside.
        // Needs the Avalonia platform, so only [AvaloniaFact] tests call it.
        public void WriteCover(string fileName, int width, int height)
        {
            Directory.CreateDirectory(Path.Combine(Root, "Cache"));
            using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
            using (var context = bitmap.CreateDrawingContext())
            {
                context.FillRectangle(Brushes.Red, new Rect(0, 0, width, height));
            }
            bitmap.Save(Cache(fileName));
        }

        public void Dispose()
        {
            Environment.CurrentDirectory = _previous;
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
                // A cover stream still being finalised can hold a file for a moment; the temp folder is harmless.
            }
        }
    }
}
```

#### `tests/Avalonia.MusicStore.Tests/SmokeTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Assets.Langs;
using Avalonia.MusicStore.Views;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.IO;
using System.Linq;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_without_a_view_model_shows_the_store_button_and_logs_nothing()
        {
            using var cwd = new TempCurrentDirectory();
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var button = Assert.Single(window.GetVisualDescendants().OfType<Button>());
                Assert.Equal("Music Store", ToolTip.GetTip(button));
                Assert.Empty(logs.Messages);
                // Without a view model the Loaded command never runs, so nothing touches the cache.
                Assert.False(Directory.Exists(Path.Combine(cwd.Root, "Cache")));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Store_window_shows_the_search_box_and_buy_button_with_the_progress_bar_hidden()
        {
            using var logs = new LogCollector();
            var window = new MusicStoreWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var search = Assert.Single(window.GetVisualDescendants().OfType<TextBox>());
                Assert.Equal(Resources.txt_SearchAlbums, search.PlaceholderText);
                Assert.NotNull(window.ButtonWithText(Resources.btn_BuyAlbum));
                Assert.False(Assert.Single(window.GetVisualDescendants().OfType<ProgressBar>()).IsVisible);
                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

#### `tests/Avalonia.MusicStore.Tests/ViewModelTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.ViewModels;
using Avalonia.Shared.Messages;
using Avalonia.Tests.Common;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class ViewModelTests
    {
        // Collects every MessageParam broadcast on the process-wide messenger while a test runs.
        // Not private: the toolkit's RegisterAll source generator emits code that must see the type.
        internal sealed class Recipient : IRecipient<MessageParam>
        {
            public List<MessageParam> Received { get; } = new();
            public void Receive(MessageParam message) => Received.Add(message);
        }

        [AvaloniaFact]
        public void Clearing_the_search_text_clears_results_without_a_search()
        {
            var vm = new MusicStoreViewModel();
            vm.SearchResults.Add(new AlbumViewModel(new Album("Queen", "Jazz", "u")));

            vm.SearchText = "";

            Assert.Empty(vm.SearchResults);
            Assert.False(vm.IsBusy);
        }

        [AvaloniaFact]
        public void Buy_without_a_selection_sends_nothing()
        {
            var vm = new MusicStoreViewModel();
            var recipient = new Recipient();
            WeakReferenceMessenger.Default.Register(recipient);
            try
            {
                vm.BuyMusicCommand.Execute(null);
                Assert.Empty(recipient.Received);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }

        [AvaloniaFact]
        public void Buy_with_a_selection_sends_the_selected_album()
        {
            var album = new AlbumViewModel(new Album("AC/DC", "Back in Black", "u"));
            var vm = new MusicStoreViewModel();
            vm.SearchResults.Add(album);
            vm.SelectedAlbum = album;
            var recipient = new Recipient();
            WeakReferenceMessenger.Default.Register(recipient);
            try
            {
                vm.BuyMusicCommand.Execute(null);
                Assert.Same(album, Assert.Single(recipient.Received).Data);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }

        [AvaloniaFact]
        public async Task Receiving_an_album_adds_it_once_and_saves_it_to_the_cache()
        {
            using var cwd = new TempCurrentDirectory();
            var vm = new MainWindowViewModel();
            try
            {
                var album = new AlbumViewModel(new Album("AC/DC", "Back in Black", "u"));
                WeakReferenceMessenger.Default.Send(new MessageParam { Data = album });
                WeakReferenceMessenger.Default.Send(new MessageParam { Data = album });

                Assert.Same(album, Assert.Single(vm.Albums));
                var file = cwd.Cache("AC_DC - Back in Black");
                await UiHelpers.WaitUntil(() => File.Exists(file) && new FileInfo(file).Length > 0);
                var saved = Assert.Single(await Album.LoadCachedAsync());
                Assert.Equal("AC/DC", saved.Artist);
                Assert.Equal("Back in Black", saved.Title);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(vm);
            }
        }
    }
}
```

#### `tests/Avalonia.MusicStore.Tests/CacheTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.ViewModels;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class CacheTests
    {
        [AvaloniaFact]
        public async Task Names_with_invalid_file_characters_round_trip()
        {
            using var cwd = new TempCurrentDirectory();
            await new AlbumViewModel(new Album("A/B", "C:D?", "u")).SaveToDiskAsync();

            Assert.True(File.Exists(cwd.Cache("A_B - C_D_")));
            var loaded = Assert.Single(await Album.LoadCachedAsync());
            Assert.Equal(("A/B", "C:D?", "u"), (loaded.Artist, loaded.Title, loaded.CoverUrl));
        }

        [AvaloniaFact]
        public async Task An_album_with_a_dot_in_its_name_loads_back()
        {
            using var cwd = new TempCurrentDirectory();
            await new AlbumViewModel(new Album("R.E.M.", "Monster", "u")).SaveToDiskAsync();
            await new AlbumViewModel(new Album("Mr. Big", "Lean Into It", "u")).SaveToDiskAsync();

            var artists = (await Album.LoadCachedAsync()).Select(a => a.Artist).OrderBy(a => a).ToArray();

            Assert.Equal(new[] { "Mr. Big", "R.E.M." }, artists);
        }

        [AvaloniaFact]
        public async Task Saving_shorter_data_over_an_album_leaves_a_readable_file()
        {
            using var cwd = new TempCurrentDirectory();
            await new AlbumViewModel(new Album("X", "Y", "a-very-long-cover-url-that-makes-the-json-longer")).SaveToDiskAsync();
            await new AlbumViewModel(new Album("X", "Y", "s")).SaveToDiskAsync();

            var loaded = Assert.Single(await Album.LoadCachedAsync());
            Assert.Equal("s", loaded.CoverUrl);
        }

        [AvaloniaFact]
        public async Task A_smaller_cover_overwrite_leaves_only_the_new_image()
        {
            using var cwd = new TempCurrentDirectory();
            cwd.WriteCover("X - Y.bmp", 20, 10);
            var album = new AlbumViewModel(new Album("X", "Y", "u"));
            await album.LoadCover();
            // An older, larger cover file is still on disk when the album is saved again.
            File.WriteAllBytes(cwd.Cache("X - Y.bmp"), new byte[1024 * 1024]);

            await album.SaveToDiskAsync();

            using var fresh = new MemoryStream();
            album.Cover!.Save(fresh);
            Assert.Equal(fresh.Length, new FileInfo(cwd.Cache("X - Y.bmp")).Length);
        }

        [AvaloniaFact]
        public async Task A_cover_saved_beside_the_album_loads_without_the_network()
        {
            using var cwd = new TempCurrentDirectory();
            cwd.WriteCover("Queen - Jazz.bmp", 20, 10);
            // The cover URL is unreachable on purpose: only the cached file can satisfy LoadCover.
            var album = new AlbumViewModel(new Album("Queen", "Jazz", "http://invalid.example/jazz.jpg"));

            await album.LoadCover();

            Assert.Equal(new PixelSize(400, 200), album.Cover!.PixelSize);
        }
    }
}
```

#### `tests/Avalonia.MusicStore.Tests/InteractionTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Assets.Langs;
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.ViewModels;
using Avalonia.MusicStore.Views;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class InteractionTests
    {
        [AvaloniaFact]
        public async Task Main_window_loads_cached_albums_with_their_covers()
        {
            using var cwd = new TempCurrentDirectory();
            cwd.WriteCover("Queen - Jazz.bmp", 20, 10);
            File.WriteAllText(cwd.Cache("Queen - Jazz"),
                JsonSerializer.Serialize(new Album("Queen", "Jazz", "http://invalid.example/jazz.jpg")));
            var vm = new MainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            try
            {
                window.Show();
                await UiHelpers.WaitUntil(() => vm.Albums.Count == 1 && vm.Albums[0].Cover is not null);
                Dispatcher.UIThread.RunJobs();

                var view = Assert.Single(window.GetVisualDescendants().OfType<AlbumView>());
                var texts = view.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToArray();
                Assert.Contains("Queen", texts);
                Assert.Contains("Jazz", texts);
                Assert.Equal(new PixelSize(400, 200), vm.Albums[0].Cover!.PixelSize);
            }
            finally
            {
                window.Close();
                WeakReferenceMessenger.Default.UnregisterAll(vm);
            }
        }

        [AvaloniaFact]
        public async Task Buying_the_selected_album_closes_the_store_and_adds_it_to_the_main_window()
        {
            using var cwd = new TempCurrentDirectory();
            var vm = new MainWindowViewModel();
            var main = new MainWindow { DataContext = vm };
            try
            {
                main.Show();
                await UiHelpers.WaitUntil(() => Directory.Exists(Path.Combine(cwd.Root, "Cache")));
                var store = new MusicStoreWindow();
                store.Show(main);
                Dispatcher.UIThread.RunJobs();
                var storeVm = (MusicStoreViewModel)store.GetVisualDescendants().OfType<MusicStoreView>().Single().DataContext!;
                var buy = store.ButtonWithText(Resources.btn_BuyAlbum);

                store.Click(buy);
                Dispatcher.UIThread.RunJobs();
                Assert.True(store.IsVisible);
                Assert.Empty(vm.Albums);

                // Search needs the iTunes network, so the result is placed in the list directly.
                var album = new AlbumViewModel(new Album("AC/DC", "Back in Black", "http://invalid.example/bib.jpg"));
                storeVm.SearchResults.Add(album);
                storeVm.SelectedAlbum = album;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(0, store.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex);

                store.Click(buy);
                Dispatcher.UIThread.RunJobs();

                Assert.False(store.IsVisible);
                Assert.Same(album, Assert.Single(vm.Albums));
                Dispatcher.UIThread.RunJobs();
                Assert.Single(main.GetVisualDescendants().OfType<AlbumView>());
                await UiHelpers.WaitUntil(() => File.Exists(cwd.Cache("AC_DC - Back in Black")));
            }
            finally
            {
                main.Close();
                WeakReferenceMessenger.Default.UnregisterAll(vm);
            }
        }
    }
}
```

Run: `dotnet test tests/Avalonia.MusicStore.Tests`
Expected: 13 个里**恰好 3 个失败**，都在 `CacheTests`：

| 测试 | 失败信息 |
|---|---|
| `An_album_with_a_dot_in_its_name_loads_back` | `Assert.Equal() Failure: Collections differ`，Expected `["Mr. Big", "R.E.M."]`，Actual `[]` |
| `Saving_shorter_data_over_an_album_leaves_a_readable_file` | `System.Text.Json.JsonException : 'e' is invalid after a single JSON value.` |
| `A_smaller_cover_overwrite_leaves_only_the_new_image` | `Assert.Equal() Failure: Values differ`，Expected `806`，Actual `1048576`（806 是本机 Skia 编码 400×200 纯色 PNG 的长度，以实测为准） |

失败的不是这 3 个，或者数量不对，都先停下来查，不要改测试去迁就。

- [ ] **Step 3: 修复 `src/Avalonia.MusicStore/Models/Album.cs`**

文件开头带 BOM，用 Edit 改，保留 BOM。

`using` 区补 `System`（`StringComparison` 要用）：

```csharp
using iTunesSearch.Library;
using System;
using System.Collections.Generic;
```

`SaveAsync` 里（缺陷 #3）：

```csharp
            using (var fs = File.Create(CachePath))
```

原来是 `File.OpenWrite(CachePath)`。

`SaveCoverBitmapStream`（缺陷 #3）：

```csharp
        public Stream SaveCoverBitmapStream()
        {
            return File.Create(CachePath + ".bmp");
        }
```

`LoadCachedAsync` 的循环里（缺陷 #2），把 `if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension)) continue;` 换成：

```csharp
                // Album names can contain dots ("R.E.M."), so only cover files are skipped, not every file with an extension.
                if (file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)) continue;
```

- [ ] **Step 4: 全绿**

Run: `dotnet test tests/Avalonia.MusicStore.Tests`
Expected: 13 个全部通过。

Run: `dotnet test tests/Avalonia.MusicStore.Tests`（再跑一次，确认没有残留状态）
Expected: 13 个全部通过。

Run: `git status --short`
Expected: 只有 `hello-avalonia.slnx`、`src/Avalonia.MusicStore/Models/Album.cs`、`tests/Avalonia.MusicStore.Tests/`（以及尚未提交时的本计划文件）。测试输出目录下没有新的 `Cache/`（它被 `bin/` 忽略规则挡住，用 `ls tests/Avalonia.MusicStore.Tests/bin/Debug/Cache` 确认不存在）。

- [ ] **Step 5: 提交**

```bash
git add hello-avalonia.slnx tests/Avalonia.MusicStore.Tests src/Avalonia.MusicStore/Models/Album.cs
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add MusicStore tests and keep dotted album names and overwritten cache files readable

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

各改一行，跑 `dotnet test tests/Avalonia.MusicStore.Tests`，预期恰好 1 个失败：

| 临时改动 | 预期失败的测试 |
|---|---|
| `ViewModels/MainWindowViewModel.cs` 的 `Receive` 去掉 `!Albums.Contains(...)` 判断（无条件 `Add`） | `Receiving_an_album_adds_it_once_and_saves_it_to_the_cache` |
| `Models/Album.cs` 的 `SanitizeFileName` 改成 `return name;` | `Names_with_invalid_file_characters_round_trip` |
| `ViewModels/MusicStoreViewModel.cs` 去掉 `AvaloniaHelper.GetTopLevel(control)?.Close();` | `Buying_the_selected_album_closes_the_store_and_adds_it_to_the_main_window` |
| `Models/Album.cs` 的 `LoadCoverBitmapAsync` 里两处 `CachePath + ".bmp"` 改成 `CachePath + ".png"` | `A_cover_saved_beside_the_album_loads_without_the_network` 与 `Main_window_loads_cached_albums_with_their_covers`（两条都依赖本地封面，**预期 2 个失败**） |

第 2 行的 `SanitizeFileName` 改动会让 `AC/DC` 的路径变成子目录。这时 `Receiving_an_album_…` 和购买流程也可能一起失败；只要 `Names_with_invalid_file_characters_round_trip` 在失败列表里就算通过，并在回填里记下实际失败数。

Run: `git status --short src`
Expected: 无输出。

### Task 2: WebViewDemo 测试项目，并修复页面未加载时调用 JS 崩溃（缺陷 #1）

**Files:**
- Create: `tests/Avalonia.WebViewDemo.Tests/` 下 5 个文件：
  - `Avalonia.WebViewDemo.Tests.csproj`、`TestAppBuilder.cs`
  - `SmokeTests.cs`、`ViewModelTests.cs`、`WebViewWindowTests.cs`
- Modify: `hello-avalonia.slnx`（`/tests/` 末尾加一行）
- Modify: `src/Avalonia.WebViewDemo/Views/WebViewWindow.axaml.cs`（`Receive`）

**Interfaces:**
- Consumes（共享库）：`HeadlessApp.Build<TApp>()`、`LogCollector`、`UiHelpers.ButtonWithText`
- Consumes（`src/Avalonia.WebViewDemo`，均为 `public`）：
  - `ViewModels.MainWindowViewModel`：`CallJSMethodCommand`
  - `Views.MainWindow`
  - `Views.WebViewWindow`：XAML 里有 `x:Name="webView"`（`NativeWebView`）与 `x:Name="canvas"`（`Canvas`），用 `FindControl` 取。
- Consumes（`src/Avalonia.Shared`）：`Messages.MessageParam`
- Produces：无

逐类覆盖，共 6 个：

| 类 | 用例数 | 内容 |
|---|---|---|
| `SmokeTests` | 2 | 主窗口三个按钮；`WebViewWindow` 指向随包页面并显示转圈 |
| `ViewModelTests` | 1 | Call JS 命令广播 `Reult = true` |
| `WebViewWindowTests` | 3 | 关窗前收消息、关窗后不收；**页面未加载时 Call JS 不抛**（#1）；非 JS 消息忽略 |

几处容易写错的地方（都在探针里踩过）：

- **不要 Show `WebViewWindow`**：headless 的 UI 线程是 MTA，WebView2 抛 `COMException`（`RPC_E_CHANGED_MODE`）。只构造，`finally` 里 `Close()`，不 Show 也会触发 `Closed`。
- `TestWeb/index.html` 与 `vue.js` 由 `src` 项目的 `CopyToOutputDirectory` 带进测试输出目录，`AppContext.BaseDirectory` 就是测试的 `bin\Debug`。
- 缺陷测试的 `Record.Exception` 要同时包住 `Send` 和 `RunJobs`：`async void` 里同步抛出的异常要等到 `RunJobs` 才重抛。

- [ ] **Step 1: 建项目并登记到解决方案**

#### `tests/Avalonia.WebViewDemo.Tests/Avalonia.WebViewDemo.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <!-- xunit v3 test projects are executables, and Avalonia.Headless.XUnit 12.x is built on v3. -->
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia.Headless.XUnit" />
        <PackageReference Include="xunit.v3" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Avalonia.WebViewDemo\Avalonia.WebViewDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.WebViewDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(Avalonia.WebViewDemo.Tests.TestAppBuilder))]

namespace Avalonia.WebViewDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 里，`<Project Path="tests/Avalonia.MusicStore.Tests/Avalonia.MusicStore.Tests.csproj" />` 一行之后加：

```xml
    <Project Path="tests/Avalonia.WebViewDemo.Tests/Avalonia.WebViewDemo.Tests.csproj" />
```

Run: `dotnet build tests/Avalonia.WebViewDemo.Tests 2>&1 | tail -3`
Expected: 0 个错误。

- [ ] **Step 2: 放入测试文件**

#### `tests/Avalonia.WebViewDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.WebViewDemo.ViewModels;
using Avalonia.WebViewDemo.Views;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Avalonia.WebViewDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_shows_the_three_demo_buttons_and_logs_nothing()
        {
            using var logs = new LogCollector();
            var window = new MainWindow { DataContext = new MainWindowViewModel() };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var buttons = window.GetVisualDescendants().OfType<Button>().ToArray();
                Assert.Equal(3, buttons.Length);
                Assert.All(buttons, b => Assert.NotNull(b.Command));
                Assert.NotNull(window.ButtonWithText("1. NativeWebView (embedded in a Window)"));
                Assert.NotNull(window.ButtonWithText("2. NativeWebDialog (standalone native window)"));
                Assert.NotNull(window.ButtonWithText("3. Call JS from C# (open #1 first)"));
                Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Avalonia 12 WebView");
                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }

        // The headless UI thread is MTA, and WebView2 refuses to start there (RPC_E_CHANGED_MODE), so the
        // window is only constructed, never shown; that still runs everything the constructor sets up.
        [AvaloniaFact]
        public void Web_view_window_points_at_the_bundled_page_and_shows_the_spinner()
        {
            var window = new WebViewWindow();
            try
            {
                var source = window.FindControl<NativeWebView>("webView")!.Source!;
                Assert.True(source.IsFile);
                Assert.Equal(Path.Combine(AppContext.BaseDirectory, "TestWeb", "index.html"), source.LocalPath);
                Assert.True(File.Exists(source.LocalPath));
                Assert.True(window.FindControl<Canvas>("canvas")!.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

#### `tests/Avalonia.WebViewDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Shared.Messages;
using Avalonia.WebViewDemo.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.WebViewDemo.Tests
{
    public class ViewModelTests
    {
        // Not private: the toolkit's RegisterAll source generator emits code that must see the type.
        internal sealed class Recipient : IRecipient<MessageParam>
        {
            public List<MessageParam> Received { get; } = new();
            public void Receive(MessageParam message) => Received.Add(message);
        }

        [AvaloniaFact]
        public void Call_js_command_broadcasts_a_request_to_open_web_view_windows()
        {
            var recipient = new Recipient();
            WeakReferenceMessenger.Default.Register(recipient);
            try
            {
                new MainWindowViewModel().CallJSMethodCommand.Execute(null);

                Assert.True(Assert.Single(recipient.Received).Reult);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }
    }
}
```

#### `tests/Avalonia.WebViewDemo.Tests/WebViewWindowTests.cs`

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Shared.Messages;
using Avalonia.Threading;
using Avalonia.WebViewDemo.Views;
using CommunityToolkit.Mvvm.Messaging;
using Xunit;

namespace Avalonia.WebViewDemo.Tests
{
    // Constructed only, never shown: see SmokeTests for why WebView2 cannot start under headless.
    public class WebViewWindowTests
    {
        [AvaloniaFact]
        public void Window_listens_for_js_calls_until_it_is_closed()
        {
            var window = new WebViewWindow();
            Assert.True(WeakReferenceMessenger.Default.IsRegistered<MessageParam>(window));

            window.Close();
            Dispatcher.UIThread.RunJobs();

            Assert.False(WeakReferenceMessenger.Default.IsRegistered<MessageParam>(window));
        }

        // Button #3 can be clicked while the page is still loading; the async void handler must not crash the app.
        [AvaloniaFact]
        public void Call_js_before_the_page_has_loaded_does_not_throw()
        {
            var window = new WebViewWindow();
            try
            {
                var error = Record.Exception(() =>
                {
                    WeakReferenceMessenger.Default.Send(new MessageParam { Reult = true });
                    Dispatcher.UIThread.RunJobs();
                });

                Assert.Null(error);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Messages_that_are_not_js_calls_are_ignored()
        {
            var window = new WebViewWindow();
            try
            {
                var error = Record.Exception(() =>
                {
                    WeakReferenceMessenger.Default.Send(new MessageParam { Reult = false });
                    Dispatcher.UIThread.RunJobs();
                });

                Assert.Null(error);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

Run: `dotnet test tests/Avalonia.WebViewDemo.Tests`
Expected: 6 个里**恰好 1 个失败**：`Call_js_before_the_page_has_loaded_does_not_throw`，信息是 `Assert.Null() Failure: Value is not null`，Actual 为 `System.InvalidOperationException: Unable to invoke script before any page was loaded. Listen for NavigationCompleted event.`。

- [ ] **Step 3: 修复 `src/Avalonia.WebViewDemo/Views/WebViewWindow.axaml.cs`**

这个文件在工作区里是 LF 行尾，Edit 时保持不变。`Receive` 的最后两行换成：

```csharp
        // InvokeScript throws until the first page has loaded; an exception escaping this async void handler
        // would take the whole app down, so a click that comes too early is just logged.
        try
        {
            var result = await webView.InvokeScript("window.calculateAdd(12, 13)");
            Debug.WriteLine($"JS returned: {result}");
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"JS call skipped: {ex.Message}");
        }
```

`System` 与 `System.Diagnostics` 文件里已经引用了，不用加 `using`。只捕获 `InvalidOperationException`：JS 自己抛的错误（`window.calculateAdd` 不存在之类）属于别的问题，不在本次修复范围里。

- [ ] **Step 4: 全绿**

Run: `dotnet test tests/Avalonia.WebViewDemo.Tests`
Expected: 6 个全部通过。

- [ ] **Step 5: 提交**

```bash
git add hello-avalonia.slnx tests/Avalonia.WebViewDemo.Tests src/Avalonia.WebViewDemo/Views/WebViewWindow.axaml.cs
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add WebViewDemo tests and stop an early JS call from crashing the app

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

各改一行，跑 `dotnet test tests/Avalonia.WebViewDemo.Tests`，预期恰好 1 个失败：

| 临时改动 | 预期失败的测试 |
|---|---|
| `Views/WebViewWindow.axaml.cs` 的 `OnClosed` 里去掉 `Unregister<MessageParam>(this);` | `Window_listens_for_js_calls_until_it_is_closed` |
| `Views/WebViewWindow.axaml.cs` 构造函数里的 `"index.html"` 改成 `"main.html"` | `Web_view_window_points_at_the_bundled_page_and_shows_the_spinner` |
| `ViewModels/MainWindowViewModel.cs` 的 `CallJSMethodCommand` lambda 里 `Reult = true` 改成 `Reult = false` | `Call_js_command_broadcasts_a_request_to_open_web_view_windows` |

Run: `git status --short src`
Expected: 无输出。

### Task 3: HtmlRendererDemo 测试项目

**Files:**
- Create: `tests/Avalonia.HtmlRendererDemo.Tests/` 下 4 个文件：
  - `Avalonia.HtmlRendererDemo.Tests.csproj`、`TestAppBuilder.cs`
  - `SmokeTests.cs`、`RenderTests.cs`
- Modify: `hello-avalonia.slnx`（`/tests/` 末尾加一行）

**Interfaces:**
- Consumes（共享库）：`HeadlessApp.Build<TApp>()`、`LogCollector`
- Consumes（`src/Avalonia.HtmlRendererDemo`）：`ViewModels.MainWindowViewModel`（`Text`）、`Views.MainWindow`
- Consumes（第三方，`src` 项目已引用）：`TheArtOfDev.HtmlRenderer.Avalonia.HtmlPanel`
- Consumes（Avalonia）：`AvaloniaHeadlessPlatform.ForceRenderTimerTick()`、`Window.CaptureRenderedFrame()`、`Avalonia.Media.TextFormatting.ShapedTextRun`
- Produces：无（`RenderTests` 里的私有辅助 `Open(bool)`、`DarkPixelsInPanel(Window)`）

逐类覆盖，共 3 个：

| 类 | 用例数 | 内容 |
|---|---|---|
| `SmokeTests` | 1 | 主窗口、标题、HtmlPanel 与 PathIcon 都在、无日志 |
| `RenderTests` | 2 | HtmlPanel 带 VM 有字、不带 VM 全白；图标由随包 `iconfont` 排版 |

本任务没有缺陷，测试一放进去就应全绿，所以**破坏检查是唯一能证明测试有效的步骤**，不能省。

几处容易写错的地方：

- 图标 TextBlock 的 `Text` 是单个私用区字符 U+E71A。测试源码里直接写这个字符（UTF-8 字节 `EE 9C 9A`），编辑器里看起来像空字符串 `""`，**不要**当成空串删掉或替换。复制时用 `C:\Temp\probe-b6\ProbeHtml\RenderTests.cs`，最稳妥。
- 读像素前先 `ForceRenderTimerTick()`，否则拿到的是上一帧。

- [ ] **Step 1: 建项目并登记到解决方案**

#### `tests/Avalonia.HtmlRendererDemo.Tests/Avalonia.HtmlRendererDemo.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <!-- xunit v3 test projects are executables, and Avalonia.Headless.XUnit 12.x is built on v3. -->
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia.Headless.XUnit" />
        <PackageReference Include="xunit.v3" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Avalonia.HtmlRendererDemo\Avalonia.HtmlRendererDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.HtmlRendererDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(Avalonia.HtmlRendererDemo.Tests.TestAppBuilder))]

namespace Avalonia.HtmlRendererDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

在 `hello-avalonia.slnx` 里，`<Project Path="tests/Avalonia.WebViewDemo.Tests/Avalonia.WebViewDemo.Tests.csproj" />` 一行之后加：

```xml
    <Project Path="tests/Avalonia.HtmlRendererDemo.Tests/Avalonia.HtmlRendererDemo.Tests.csproj" />
```

Run: `dotnet build tests/Avalonia.HtmlRendererDemo.Tests 2>&1 | tail -3`
Expected: 0 个错误。会有一条 `src/Avalonia.HtmlRendererDemo` 既有的 `MSB3245`（找不到 `System.Xaml`），与本批无关。

- [ ] **Step 2: 放入测试文件**

#### `tests/Avalonia.HtmlRendererDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.HtmlRendererDemo.ViewModels;
using Avalonia.HtmlRendererDemo.Views;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using TheArtOfDev.HtmlRenderer.Avalonia;
using Xunit;

namespace Avalonia.HtmlRendererDemo.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_shows_the_html_panel_and_logs_nothing()
        {
            using var logs = new LogCollector();
            var window = new MainWindow { DataContext = new MainWindowViewModel() };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                Assert.Equal("Avalonia HtmlRenderer & IconFont Demo", window.Title);
                var panel = Assert.Single(window.GetVisualDescendants().OfType<HtmlPanel>());
                Assert.False(string.IsNullOrEmpty(panel.Text));
                Assert.Single(window.GetVisualDescendants().OfType<PathIcon>());
                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

#### `tests/Avalonia.HtmlRendererDemo.Tests/RenderTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.HtmlRendererDemo.ViewModels;
using Avalonia.HtmlRendererDemo.Views;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using System.Runtime.InteropServices;
using TheArtOfDev.HtmlRenderer.Avalonia;
using Xunit;

namespace Avalonia.HtmlRendererDemo.Tests
{
    public class RenderTests
    {
        private static MainWindow Open(bool withViewModel)
        {
            var window = new MainWindow();
            if (withViewModel)
                window.DataContext = new MainWindowViewModel();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return window;
        }

        // Counts near-black pixels inside the HtmlPanel; the panel paints a white background, so only glyphs are dark.
        private static int DarkPixelsInPanel(Window window)
        {
            var panel = window.GetVisualDescendants().OfType<HtmlPanel>().Single();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var frame = window.CaptureRenderedFrame()!;
            var origin = panel.TranslatePoint(default, window)!.Value;
            using var buffer = frame.Lock();
            var dark = 0;
            for (var y = (int)origin.Y; y < (int)(origin.Y + panel.Bounds.Height); y++)
            {
                for (var x = (int)origin.X; x < (int)(origin.X + panel.Bounds.Width); x++)
                {
                    var pixel = Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
                    if ((pixel & 0xff) < 100 && ((pixel >> 8) & 0xff) < 100 && ((pixel >> 16) & 0xff) < 100)
                        dark++;
                }
            }
            return dark;
        }

        [AvaloniaFact]
        public void Html_panel_draws_the_bound_markup()
        {
            var empty = Open(withViewModel: false);
            var withText = Open(withViewModel: true);
            try
            {
                Assert.Equal(0, DarkPixelsInPanel(empty));
                Assert.True(DarkPixelsInPanel(withText) > 500);
            }
            finally
            {
                empty.Close();
                withText.Close();
            }
        }

        // A missing or misnamed font resource does not fail: text silently falls back to a system font, which has
        // no glyph at U+E71A. So the test checks which typeface actually shaped the icon.
        [AvaloniaFact]
        public void Icon_text_is_shaped_with_the_bundled_icon_font()
        {
            var window = Open(withViewModel: true);
            try
            {
                var icon = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "");
                var run = icon.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>().Single();

                Assert.Equal("iconfont", run.GlyphRun.GlyphTypeface.FamilyName);
                Assert.All(run.GlyphRun.GlyphInfos, g => Assert.NotEqual(0, (int)g.GlyphIndex));
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

Run: `dotnet test tests/Avalonia.HtmlRendererDemo.Tests`
Expected: 3 个全部通过。

- [ ] **Step 3: 提交**

```bash
git add hello-avalonia.slnx tests/Avalonia.HtmlRendererDemo.Tests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add HtmlRendererDemo smoke and render tests

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 4: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

各改一行，跑 `dotnet test tests/Avalonia.HtmlRendererDemo.Tests`，预期恰好 1 个失败：

| 临时改动 | 预期失败的测试 |
|---|---|
| `Views/MainWindow.axaml` 的 HtmlPanel 去掉 `Background="White"` | `Html_panel_draws_the_bound_markup`（无 VM 的一半：深色主题背景让暗像素不再是 0） |
| `App.axaml` 的 `IconFont` 资源 URI 里 `#iconfont` 改成 `#iconfont2` | `Icon_text_is_shaped_with_the_bundled_icon_font`（字体族名变成系统回退字体） |

Run: `git status --short src`
Expected: 无输出。

### Task 4: README、回填 spec 与全量验证

**Files:**
- Modify: `README.md`（测试项目那一行的"目前覆盖"列表）
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（`## 批 6 实测结论（执行后回填）` 一节）

**Interfaces:**
- Consumes：Task 1–3 的提交、各自实测的测试数与破坏检查结果。
- Produces：README 一行、spec「批 6 实测结论」。

- [ ] **Step 1: 全量验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -3`
Expected: 0 个错误；警告数 ≤ 6（批开始前 6，全部来自 `src/` 里既有的警告）。

Run: `dotnet test hello-avalonia.slnx`
Expected: 全部通过，共 523 个（501 + 13 + 6 + 3），0 个跳过。

Run: `git status --short`
Expected: 无输出（三个任务都已提交），没有探针文件。

- [ ] **Step 2: README**

`README.md` 里 `[Avalonia.\*Demo.Tests](tests)` 那一行的括号内，把 `…、Behaviors、Controls）` 改成 `…、Behaviors、Controls、MusicStore、WebView、HtmlRenderer）`。同一行"走完全部标签页"之后的描述不动。

- [ ] **Step 3: 回填「批 6 实测结论」**

spec 里 `## 批 6 实测结论（执行后回填）` 目前只有标题，紧跟着就是 `## 批 7 …`。把这个标题行换成下面整段，整段里已含同一个标题。测试数、警告数、破坏检查的计数按 Step 1 与 Task 1–3 的实测填写；与这里写的不同就按实测写，并在"执行时与计划不符"里说明。

```markdown
## 批 6 实测结论（执行后回填）

计划：`docs/superpowers/plans/2026-10-12-demo-tests-06-headless-exceptions.md`；探针：`C:\Temp\probe-b6\`（未提交）。

**交付**：3 个新测试项目，共 22 个用例。全量 523 个通过，0 跳过；构建 0 错误，警告 <实测> 个（不增）。

| 项目 | 用例 | 内容 |
|---|---|---|
| `Avalonia.MusicStore.Tests` | 13 | H1 2、H2 9（含缓存 5）、H3 2 |
| `Avalonia.WebViewDemo.Tests` | 6 | H1 2、H2 1、H3 3 |
| `Avalonia.HtmlRendererDemo.Tests` | 3 | H1 1、H3 2（像素 + 字形） |

**修复的缺陷**（用户 2026-10-17 确认，均先红后绿）：
1. `WebViewWindow.Receive`：页面加载完之前调用 `InvokeScript` 抛 `InvalidOperationException`，从 `async void` 漏出会让应用退出 → 捕获并记日志。
2. `Album.LoadCachedAsync`：名字带「.」的专辑被当成封面文件跳过，重启后丢失 → 只跳过 `.bmp`。
3. `Album.SaveAsync`/`SaveCoverBitmapStream`：`File.OpenWrite` 不截断，短内容覆写后残留旧尾巴，下次启动 `JsonException` → `File.Create`。

**已知未修**（只记录，无测试）：
- 搜索竞态：快速连打时旧搜索的结果可能追加在新搜索之后；只能经网络复现，spec 不许加接缝。
- 重复购买：两次搜索得到同一张专辑会被加两次，`Albums.Contains` 按引用比较。

**headless 的硬边界**（后续批同样适用）：
- 没有桌面生命周期，`GetMainWindow()` 为 null，依赖它的命令（`ShowWebViewCommand`、`ShowAlbumsCommand`）不能直接执行。
- UI 线程是 MTA，WebView2 无法启动；`NativeWebView`/`NativeWebDialog` 只能构造。
- `async void` 里的同步异常在下一次 `RunJobs` 时重抛。

**破坏检查**：MusicStore <实测>/4、WebView <实测>/3、HtmlRenderer <实测>/2 条按预期失败。

**执行时与计划不符**：<无，或逐条写>
```

`<实测>` 与 `<无，或逐条写>` 是执行时**必须替换**的记录位，不能原样提交。

- [ ] **Step 4: 提交**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 6 results

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

## Self-Review

**1. Spec 覆盖（批 6）**

| spec 要求 | 落在哪里 |
|---|---|
| 批 6 范围：MusicStore、WebViewDemo、HtmlRendererDemo（TestingDemo 的 H1 已在批 0 完成） | Task 1、2、3 各建一个测试项目 |
| H1：窗口能显示、无绑定/布局警告 | 三个 `SmokeTests` 都用 `LogCollector` 断言 0 条 |
| H2：ViewModel 单测 | MusicStore `ViewModelTests` 4 + `CacheTests` 5；WebView `ViewModelTests` 1；HtmlRenderer 的 VM 只有一个常量，按无逻辑跳过，理由写在覆盖清单 |
| H3：关键交互"操作 → 界面变化" | MusicStore 缓存加载与购买；WebView 消息注册；HtmlRenderer 像素与字形 |
| 非目标：不断言网页内容、不断言 iTunes 网络结果 | 网络封面 URL 一律是 `http://invalid.example/…`，搜索结果手工放进列表；WebView 不 Show、不断言 JS 返回 |
| 风险表：MusicStore 搜索无接缝，不往 `src/` 加接缝 | `src/` 只改 3 处缺陷；两处已知问题（竞态、重复购买）只记录 |
| 测试不读写真实用户目录 | `TempCurrentDirectory` + 关闭并行；Task 1 Step 4 检查 `bin/Debug/Cache` 不存在 |
| 真实缺陷经用户确认后修复，先写会失败的测试 | 3 处，用户 2026-10-17 确认；Task 1、2 的 Step 2（红）→ Step 3（修）→ Step 4（绿） |
| 新项目登记 slnx，README 列出覆盖的应用 | Task 1–3 Step 1；Task 4 Step 2 |
| 每批回填「实测结论」 | Task 4 Step 3 |
| 验证判据：构建 0 错误且警告不增、全部测试通过、`src/` 只有确认过的修复 | Task 4 Step 1；各任务破坏检查末尾 `git status --short src` |

**2. 占位符**：测试代码块由脚本从 `C:\Temp\probe-b6\Probe*\` 的成品文件原样嵌入，没有手抄。Task 4 回填段里的 `<实测>` 与 `<无，或逐条写>` 是执行时必须替换的记录位，Step 3 已写明。

**3. 名称一致性**：
- `TempCurrentDirectory` 的 `Root`、`Cache`、`WriteCover` 在 4 个 MusicStore 测试文件里用法一致。
- 两个 `Recipient` 都是 `internal`，分属不同程序集，互不冲突。
- 各缺陷测试名与 Step 2 的 Expected 表一致。
- 523 = 501 + 13 + 6 + 3。

**4. Review Focus**：5 条都有测试。
- 第 1 条：Task 2 `Call_js_before_the_page_has_loaded_does_not_throw`，先红后绿。
- 第 2 条：Task 1 `An_album_with_a_dot_in_its_name_loads_back`，先红后绿。
- 第 3 条：Task 1 `Saving_shorter_data_over_an_album_leaves_a_readable_file` 与 `A_smaller_cover_overwrite_leaves_only_the_new_image`，先红后绿。
- 第 4 条：Task 2 `Window_listens_for_js_calls_until_it_is_closed`；Task 1 `Receiving_an_album_adds_it_once_and_saves_it_to_the_cache`。两条都有破坏检查。
- 第 5 条：Task 3 `Icon_text_is_shaped_with_the_bundled_icon_font`，有破坏检查。
