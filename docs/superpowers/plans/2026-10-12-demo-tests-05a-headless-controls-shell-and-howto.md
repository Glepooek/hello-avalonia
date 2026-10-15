# 演示测试 05a：ControlsDemo 的外壳、ViewModel 与实战页 Headless 测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `ControlsDemo` 建 `Avalonia.ControlsDemo.Tests`，交付 spec 批 5 的前半（05a）：H1 走完导航树全部 119 个节点（含目录计数、标题前缀、日志），H2 覆盖全部带逻辑的 ViewModel 与两个存储类，H3 覆盖 19 个实战页；修复探针发现的 `NavigationHowToPage` 过渡下拉框显示错误。80 个控件页的 H3 留给 05b。

**Architecture:** 沿用批 1–4：一个 xUnit v3 + `Avalonia.Headless.XUnit` 测试项目，引用演示项目与 `tests/Avalonia.Tests.Common`，`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。`ControlsDemo` 不是 `TabControl` 应用，`TabWalker` 用不上：H1 通过 `MainViewModel.SelectedNode` 逐个选中导航节点（与点击 `NavTree` 走同一条路），断言 `PageHost` 显示的页面类型。项目内新增一个 `PageHelpers`（`Open<TPage>`、`Text`、`Press`、`PressText`），05b 继续用它。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、Avalonia.Skia、Avalonia.Controls.DataGrid、xUnit v3（3.2.2）、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 5；用户 2026-10-15 决定拆成 05a/05b）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文。
- 版本只写在根目录 `Directory.Packages.props`；csproj 的 `PackageReference` **不带 `Version`**。本批用到的包都已登记，不改 `Directory.Packages.props`。
- 代码风格沿用前几批的测试项目：块作用域命名空间、测试方法名用英文下划线句式。
- `TestAppBuilder` 只写 `HeadlessApp.Build<App>()`，不复制 `Avalonia.Tests.Common` 里的辅助代码；`PageHelpers` 只放本项目专用的薄封装。
- 目录约定：测试项目放 `tests/`，并登记到 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">`（放在 `Avalonia.BehaviorsDemo.Tests` 之后、`Avalonia.TestingDemo.Tests` 之前）。
- `src/` 只允许一处改动：Task 4 修 `src/Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs` 的过渡映射（用户 2026-10-15 确认"修，改映射"）。其余"破坏检查"临时改 `src/` 的行必须 `git checkout --` 还原；Task 1–3 提交前 `git status --short src` 必须为空。
- **测试不得读写真实的 `%AppData%\Avalonia.ControlsDemo` 与 `%LocalAppData%\Avalonia.ControlsDemo`**：凡是会改设置或最近文件的测试，都给 `DataPersistenceHowToViewModel` / `SettingsStore` / `RecentFiles` 传自建的临时目录，并在 `finally`/`Dispose` 里删除。只构造 `DataPersistenceHowToPage`（H1 走树）只读不写，探针确认不会创建这两个目录。
- 打开过子窗口的测试（`WindowHowToPage`、`DialogsHowToPage`、走完全部节点的 H1）在 `finally` 里 `Close()` 宿主窗口。
- 异步结果一律 `await UiHelpers.WaitUntil(...)`，不要 `Thread.Sleep` 或固定 `Task.Delay` 后断言。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`；提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（`C:\Temp\probe-*`），**永不提交**。
- 此前各批的 270 个测试始终保持通过。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. **测试写坏使用者真实的设置文件**：`DataPersistenceHowToPage` 构造时用 `SettingsStore.DefaultDirectory`（`%AppData%`）与 `RecentFiles.DefaultDirectory`（`%LocalAppData%`）。H3 若直接在页面上打字，会改写本机正在用的 `settings.json`。Task 2 的 `Opening_the_page_reads_without_writing` 钉住"只读不写"（H1 走树依赖它），Task 4 的页面测试先把 `DataContext` 换成临时目录上的 ViewModel 再操作。
2. **实战页开的窗口泄漏到后面的测试**：`WindowHowToPage` 会开独立窗口（没有所有者，关宿主也不会跟着关）和带"未保存"拦截的编辑窗口。页面靠 `DetachedFromVisualTree` 统一关掉；若这条坏了，脏编辑窗口会弹确认框挡住关闭，独立窗口留在进程里。Task 4 的 `Leaving_the_page_closes_every_window_it_opened` 把页面移出视觉树后断言全部关闭、且没有留下确认框（破坏检查：去掉 `_confirmedClose = true` → 恰好这一条失败）。
3. **下拉框显示的过渡与实际不符**（真实缺陷）：Avalonia 12 的 `TransitioningContentControl` 默认过渡是私有嵌套类 `ImmutableCrossFade`，不是 `CrossFade`，页面的 `CrossFade => 1` 匹配不到，下拉框显示「无」而切页仍在淡入淡出。Task 4 先写会红的测试（Expected `CrossFade`，Actual `无`），再改映射。
4. **同名页面在导航树里分不清**：17 个标题同时是控件页和实战页（`ListBox`、`TextBox`、`Window`…），`Title` 本身有重复，靠 `DisplayTitle` 的「实战：」前缀区分。Task 1 断言 119 个 `DisplayTitle` 互不相同、前缀与 `Kind` 一一对应。
5. **`UserControl.KeyBindings` 只在焦点进入页面后生效**：`MenuHowToPage` 的 Ctrl+S 写在 `UserControl.KeyBindings` 上，刚 `Show` 时没有焦点，按了没反应；点一下列表项之后才生效。Task 4 把"之前无效、之后有效"两半都断言，防止把"没反应"当成通过，也防止以后有人以为 Ctrl+S 坏了。

---

## 本份已核实的事实（编写计划时在仓库外的探针里实测）

探针在 `C:\Temp\probe-b5-controls`（`Facts1`–`Facts4` 是取事实用的，`final\` 是本计划的全部测试代码）。**与批 4 不同：本计划 Task 1–4 的测试代码就是 `final\` 里实跑过的成品**——81 个测试在探针里全部通过（Task 4 的缺陷测试在修复前红、修复后绿），执行时照抄即可；与预期不符时先查是否抄错。

| 事项 | 实测结果 | 对计划的影响 |
|---|---|---|
| `PageCatalog.All` | 119 项：`Control` 80、`HowTo` 19、`Signpost` 20；无重复的 `PageType`；`DocPath` 无空值 | spec 写的 121 是错的，Task 5 改正 |
| 每个目录的页数（注册顺序） | `Data display` 16、`Feedback` 5、`Input` 26、`Layout` 23、`Media` 4、`Menus` 6、`Navigation` 10、`Primitives` 7、`System` 2、`Web` 4、`付费控件` 7、`How-to 路标` 9 | 与 README 对照表一致；Task 1 |
| 导航树 | 12 个目录节点（`Entry` 为 null）、119 个页面节点；只有第一个目录展开；`Title` 有 17 个重复，`DisplayTitle` 无重复；实战页前缀「实战：」、路标页前缀「↗ 」 | Review Focus 第 4 条；Task 1 |
| 启动状态 | 选中 `Carousel`，`PageHost` 显示 `CarouselPage`，`NavTree.SelectedItem` 与之相同 | Task 1 |
| `CarouselPage` 命名 | Avalonia 12 自带 `Avalonia.Controls.CarouselPage`，与演示页同名，`using Avalonia.Controls;` 时报 CS0104 | 测试文件用 `using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;` |
| 逐个选中 119 个节点 | 0 个异常、0 条 Warning 日志；`PageHost` 显示的类型都等于 `Entry.PageType` | Task 1 |
| 选中目录节点 / 重选同一页 | `CurrentPage` 变 null；重选时新建实例（不复用） | Task 1 |
| 点击树项 | 展开 `Input` 后点第一项 → 选中 `Button`，显示 `ButtonPage` | Task 1 |
| `DataPersistenceHowToPage` 只构造 | 走完全部节点后 `%AppData%\Avalonia.ControlsDemo`、`%LocalAppData%\Avalonia.ControlsDemo` 都**不存在** | Review Focus 第 1 条 |
| `DataPersistenceHowToViewModel`（临时目录） | 构造：`还没有设置文件，使用默认值`、`（文件不存在）`、不建目录；打字 `小明` → `已自动保存（第 1 次）`，文件含 `"Username": "小明"`（不转义中文）；写坏 → `{ this is not json`；重新读取 → `设置文件损坏，已回退到默认值`、名字清空、`SaveCount` 不变；恢复默认 → `已恢复默认值并保存`；删除 → `（文件不存在）`；最近文件 12 次 → 10 条，首项 `文档 12.txt`，末项 `文档 3.txt`；重新构造读回 10 条 | Task 2、Task 4 |
| `TextBoxHowToViewModel` | 初始 `校验通过（输入至少 3 个字符）`；`ab` → `至少 3 个字符`；空 → `用户名必填` | Task 2、Task 4 |
| `DatePickerHowToViewModel` | 初始 `请同时选择日期和时间`、错误为空；昨天 → `日期不能早于今天`；2099-01-02 + 09:30 → `预约时间：2099-01-02 09:30` | Task 2、Task 4 |
| `DataGridHowToViewModel` | 分组键 `Fruit,Bakery`；降序 `Croissant,Cherry,Bread,Banana,Bagel,Apple`；明细 `Apple,Bread,Cherry,Bagel,Banana,Croissant`；页面上 `GroupGrid` 2 个组头，`DetailGrid` 列头 `名称,分类,价格,有货` | Task 2、Task 4 |
| `TabControlHowToViewModel` | 构造时 `CreatedCount` = 0；页面打开后 = 1（第一步被显示）；下一步两次 → 3，再上一步仍 3；新建标签 → `文档 4` 并选中；关第一个 → `日志,设置,文档 4`，选中不变；改 `TabStripPlacement` 后选中保持 | Task 2、Task 4 |
| `NotificationsHowToPage` | 三个按钮 → `第 1 条信息`、`第 2 条：已完成`、`第 3 条：出错了，需要手动关闭`；约 2.8 s 后只剩错误那条；它的 `✕` 关掉它；状态行 `已保存（HH:mm:ss）`，约 2.5 s 后 `就绪`；横幅可显示 | Task 4 用 `WaitUntil`（默认 5 s） |
| `TreeViewHowToPage` | 初始可见 2 项，展开全部 7 项，折叠回 2；选中文件 `选中文件：Program.cs（2 KB）`，选中文件夹 `选中文件夹：项目（2 项）`；**选中置 null 时文字不变**（页面不处理 null）；懒加载展开两次只加载 1 次 | Task 4 不断言"选中置空" |
| `ComboBoxHowToPage` | `PriorityBox` 选第 3 项 → `SelectedValue = High`；`AutoCompleteBox` 输入「王」→ 下拉打开、`未选择`；选中王芳 → `已选：王芳，市场部`；置 null → `未选择` | Task 4 |
| `SliderHowToPage` | 初始 `#4080C0`；红 255 → `#FF80C0`，`Preview` 画刷同色；点轨道 90% 处：禁用的与不可命中的仍为 5，`IntSlider` → 9、`Value = 9` | Task 4 |
| `TextBoxHowToPage` | 数字框逐字：`1`、`1`+提示、`12`、`1234`；赋值 `x9` → `9`+`已滤掉非数字`；搜索「西」→ `西瓜`、「子」→ `橙子`；✕ 清空回到 7 项；撤销/重做按钮生效 | Task 4 |
| `ExpanderHowToPage` | 点第 2 节 → `OpenSection = 1`；再点第 1 节 → 0 且第 2 节收起；再点第 1 节 → -1；方向框 `Down,Up,Left,Right`；勾选禁用 → `DirectionDemo.IsEnabled` 为 false | Task 4 |
| `ScrollViewerHowToPage` | 初始 20 项；每次 `ScrollToEnd` +10，到 60 停；跳转按钮把偏移从 0 改为正数；`ChainCheck` 取消后 `IsScrollChainingEnabled` 为 false | Task 4 |
| `ImageHowToPage` | 占位文字在 `Border` 里，隐藏的是 `Border`：要看 `IsEffectivelyVisible`（`IsVisible` 一直是 true）；加载后图片宽 64；截图 → `已截图 160×60`、`RenderTargetBitmap` | Task 4 |
| `MenuHowToPage` | 刚 `Show` 时没有焦点，Ctrl+S 无效；点列表项后 Ctrl+S → `保存（…）`；右键菜单对选中项删除 → `删除 面包`；格式 → 粗体 → `IsBold` 为 true；清空最近文件 → 只剩 `EmptyRecent` | Review Focus 第 5 条；Task 4 |
| `NavigationHowToPage` | 初始 `Host.PageTransition` 是 `TransitioningContentControl+ImmutableCrossFade`（**私有**，不是 `CrossFade` 的子类），下拉框显示「无」 | Review Focus 第 3 条；Task 4 修复 |
| `WindowHowToPage` | 按钮右边缘：`RestoreButton` 567、`SaveButton` 471，其余不超过 436（600 宽能点中，`RestoreButton` 贴边，测试放宽到 900）；开 2 子 1 独立 → `独立窗口 1 个，子窗口 2 个；所有者的 OwnedWindows 共 2 个…`；示例窗口初始 `ClientSize = 280×140`，移动后 `Position = 200, 120，ClientSize = 420×260`，恢复后与记下的一致；脏编辑窗口 `Close()` 后仍可见、`OwnedWindows` 里有一个确认框；取消/确定的结果文字见 Task 4；保存后关闭不拦截；页面移出视觉树后全部窗口关闭 | Review Focus 第 2 条；Task 4 |
| `DialogsHowToPage` | Headless 下 `ShowDialog` 不阻塞，对话框是宿主的 `OwnedWindows`；确认框标题 `确认`；起名字的确定按钮在有字之前禁用，答案去空格；必答题的取消禁用、`Close()` 被拦下；选 `Swatch_Blue` → `SteelBlue`；`StorageText` = `CanOpen = False，CanSave = False，CanPickFolder = False`，打开文件按钮禁用 | Task 4 |
| 破坏检查（探针里跑过、均已还原） | 去掉 Expander 的 `_openSection == index` 守卫 → 1 个（`Opening_one_expander_section_closes_the_other`）；去掉 `if (_loading) return;` → 3 个（`PersistenceTests` 的读回、损坏、恢复默认）；`DetachedFromVisualTree` 去掉 `_confirmedClose = true` → 1 个（`Leaving_the_page_closes_every_window_it_opened`）；去掉 `_forward.Clear()` → 1 个（`Navigation_keeps_back_and_forward_stacks`）；`OrdinalIgnoreCase` 改 `Ordinal` → 1 个（`Recent_files_keep_the_newest_ten_…`）。以上计数不含当时尚未修复的过渡缺陷测试 | Task 2、Task 4 的破坏检查步骤 |

## 文件结构

**新增**

| 文件 | 职责 |
|---|---|
| `tests/Avalonia.ControlsDemo.Tests/Avalonia.ControlsDemo.Tests.csproj` | 测试项目工程 |
| `tests/Avalonia.ControlsDemo.Tests/TestAppBuilder.cs` | 一行：`HeadlessApp.Build<App>()` |
| `tests/Avalonia.ControlsDemo.Tests/PageHelpers.cs` | 本项目共用的 `Open<TPage>`、`Text`、`Press`、`PressText`（05b 继续用） |
| `tests/Avalonia.ControlsDemo.Tests/SmokeTests.cs` | H1：目录计数、标题前缀、启动状态、走完 119 个节点、日志、点击树项 |
| `tests/Avalonia.ControlsDemo.Tests/ViewModelTests.cs` | H2：实战页的 ViewModel |
| `tests/Avalonia.ControlsDemo.Tests/PersistenceTests.cs` | H2：`DataPersistenceHowToViewModel`、`SettingsStore`、`RecentFiles`（临时目录） |
| `tests/Avalonia.ControlsDemo.Tests/HowToDataInputTests.cs` | H3：Data display、Feedback、Input、Layout 的 12 个实战页 |
| `tests/Avalonia.ControlsDemo.Tests/HowToShellTests.cs` | H3：Media、Menus、Navigation、Primitives、System 的 7 个实战页 |

**修改**

| 文件 | 改动 |
|---|---|
| `src/Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs` | 过渡下拉框的初始映射（Task 4） |
| `hello-avalonia.slnx` | `/tests/` 加入新项目（Task 1） |
| `README.md` | 测试项目一行补上 ControlsDemo（Task 5） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 121 → 119（两处）、交付表拆 05a/05b、回填「批 5 实测结论」的 05a 部分（Task 5） |

**不碰**：`src/` 下其他文件、`Directory.Packages.props`、`tests/Avalonia.Tests.Common`、`CLAUDE.md`。

**代码块抽取**：`CLAUDE.md` 提到的 `C:\Temp\extract-plan.ps1` 不存在。可以按 `#### \`路径\`` 标注手动 Write，或用批 4 工作区的 `python .superpowers/sdd/2026-10-12-demo-tests-04-headless-app-services/extract.py <本计划> <仓库根>`；也可以直接从探针 `C:\Temp\probe-b5-controls\final\` 复制同名文件（内容与本计划一致）。

---

### Task 1: 测试项目骨架 + H1 冒烟（走完 119 个导航节点）

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/Avalonia.ControlsDemo.Tests.csproj`、`TestAppBuilder.cs`、`PageHelpers.cs`、`SmokeTests.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes（批 0–4 已交付，`tests/Avalonia.Tests.Common`）：`HeadlessApp.Build<TApp>()`、`LogCollector`（`Messages`、`Dispose` 还原）、`UiHelpers.Show(Control) : Window`（600×480）、`ByName<T>`、`ButtonWithText`、`Click`、`Type`、`WaitUntil(Func<bool>, int = 5000) : Task`。
- Consumes（`src/Avalonia.ControlsDemo`，均为 `public`）：`PageCatalog.All : IReadOnlyList<PageEntry>`；`PageEntry(Title, Category, DocPath, PageType, Kind)`；`PageKind { Control, HowTo, Signpost }`；`MainViewModel.Nodes : IReadOnlyList<NavNode>`、`SelectedNode`、`CurrentPage`；`NavNode.DisplayTitle`、`Entry`、`Children`、`IsExpanded`；`MainWindow` 里的 `NavTree`（`TreeView`）与 `PageHost`（`ContentControl`）。
- Produces（Task 3、4 与 05b 依赖）：`internal static class PageHelpers`——
  - `Open<TPage>() : (Window Window, TPage Page)`（`new TPage()` → `UiHelpers.Show` → `RunJobs`）
  - `Text(this Visual root, string name) : string`（`TextBlock.Text ?? ""`）
  - `Press(this Window window, string name)`（按 `Name` 点击后 `RunJobs`）
  - `PressText(this Window window, string text)`（按按钮文字点击后 `RunJobs`）

- [ ] **Step 1: 创建项目骨架并登记到解决方案**

#### `tests/Avalonia.ControlsDemo.Tests/Avalonia.ControlsDemo.Tests.csproj`

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
        <ProjectReference Include="..\..\src\Avalonia.ControlsDemo\Avalonia.ControlsDemo.csproj" />
        <ProjectReference Include="..\Avalonia.Tests.Common\Avalonia.Tests.Common.csproj" />
    </ItemGroup>
</Project>
```

#### `tests/Avalonia.ControlsDemo.Tests/TestAppBuilder.cs`

```csharp
using Avalonia;
using Avalonia.ControlsDemo;
using Avalonia.ControlsDemo.Tests;
using Avalonia.Headless;
using Avalonia.Tests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Avalonia.ControlsDemo.Tests
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => HeadlessApp.Build<App>();
    }
}
```

#### `tests/Avalonia.ControlsDemo.Tests/PageHelpers.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Tests.Common;
using Avalonia.Threading;

namespace Avalonia.ControlsDemo.Tests
{
    // Shared by every page test in this project (05a How-to pages, 05b control pages).
    internal static class PageHelpers
    {
        public static (Window Window, TPage Page) Open<TPage>() where TPage : Control, new()
        {
            var page = new TPage();
            var window = UiHelpers.Show(page);
            Dispatcher.UIThread.RunJobs();
            return (window, page);
        }

        public static string Text(this Visual root, string name) => root.ByName<TextBlock>(name).Text ?? "";

        public static void Press(this Window window, string name)
        {
            window.Click(window.ByName<Control>(name));
            Dispatcher.UIThread.RunJobs();
        }

        public static void PressText(this Window window, string text)
        {
            window.Click(window.ButtonWithText(text));
            Dispatcher.UIThread.RunJobs();
        }
    }
}
```

在 `hello-avalonia.slnx` 的 `<Folder Name="/tests/">` 里，`Avalonia.BehaviorsDemo.Tests` 那一行之后插入：

```xml
    <Project Path="tests/Avalonia.ControlsDemo.Tests/Avalonia.ControlsDemo.Tests.csproj" />
```

Run: `dotnet build tests/Avalonia.ControlsDemo.Tests`
Expected: `已成功生成` / `Build succeeded`，0 个错误。

- [ ] **Step 2: H1 冒烟测试**

`ControlsDemo` 的外壳是 `SplitView` + `TreeView`，不是 `TabControl`：逐个给 `MainViewModel.SelectedNode` 赋值（点击 `NavTree` 走的也是这条路），每次 `RunJobs` + `UpdateLayout`，记下 `PageHost` 实际显示的类型和异常。计数断言来自 README 的控件目录对照表；改了目录要同步改这里，这正是"防止漏页或误删页"的契约。

#### `tests/Avalonia.ControlsDemo.Tests/SmokeTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
// Avalonia 12 ships its own CarouselPage (and NavigationPage, TabbedPage…); the demo page names collide with them.
using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;

namespace Avalonia.ControlsDemo.Tests
{
    public class SmokeTests
    {
        private static (MainWindow Window, MainViewModel Vm) OpenShell()
        {
            var window = new MainWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, (MainViewModel)window.DataContext!);
        }

        private static IEnumerable<NavNode> PageNodes(MainViewModel vm) => vm.Nodes.SelectMany(n => n.Children);

        // The shell is not a TabControl, so TabWalker does not apply: select every page node through the view model,
        // the same path a click in NavTree takes, and record what each one did.
        private static List<(string Title, Type Expected, Type? Shown, string? Error)> WalkEveryPage(MainWindow window, MainViewModel vm)
        {
            var host = window.ByName<ContentControl>("PageHost");
            var visits = new List<(string, Type, Type?, string?)>();
            foreach (var node in PageNodes(vm).ToList())
            {
                string? error = null;
                try
                {
                    vm.SelectedNode = node;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Dispatcher.UIThread.RunJobs();
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                }
                visits.Add((node.DisplayTitle, node.Entry!.PageType, host.Content?.GetType(), error));
            }
            return visits;
        }

        [AvaloniaFact]
        public void Catalog_registers_119_pages_by_kind()
        {
            Assert.Equal(119, PageCatalog.All.Count);
            Assert.Equal(80, PageCatalog.All.Count(e => e.Kind == PageKind.Control));
            Assert.Equal(19, PageCatalog.All.Count(e => e.Kind == PageKind.HowTo));
            Assert.Equal(20, PageCatalog.All.Count(e => e.Kind == PageKind.Signpost));
        }

        // The README's table is the contract; categories appear in registration order.
        [AvaloniaFact]
        public void Catalog_counts_pages_per_category_in_registration_order()
        {
            Assert.Equal(
                new[]
                {
                    ("Data display", 16), ("Feedback", 5), ("Input", 26), ("Layout", 23), ("Media", 4), ("Menus", 6),
                    ("Navigation", 10), ("Primitives", 7), ("System", 2), ("Web", 4), ("付费控件", 7), ("How-to 路标", 9),
                },
                PageCatalog.All.GroupBy(e => e.Category).Select(g => (g.Key, g.Count())));
        }

        [AvaloniaFact]
        public void Every_page_type_is_registered_once_and_every_entry_has_a_doc_path()
        {
            Assert.Empty(PageCatalog.All.GroupBy(e => e.PageType).Where(g => g.Count() > 1).Select(g => g.Key.Name));
            Assert.Empty(PageCatalog.All.Where(e => string.IsNullOrEmpty(e.DocPath)).Select(e => e.Title));
        }

        // Seventeen titles exist twice (a control page and its How-to page); the prefix is what tells them apart.
        [AvaloniaFact]
        public void Navigation_titles_are_unique_and_prefixed_by_kind()
        {
            var (_, vm) = OpenShell();
            var nodes = PageNodes(vm).ToList();

            Assert.Equal(119, nodes.Count);
            Assert.Empty(nodes.GroupBy(n => n.DisplayTitle).Where(g => g.Count() > 1).Select(g => g.Key));
            Assert.All(nodes, n =>
            {
                Assert.Equal(n.Entry!.Kind == PageKind.HowTo, n.DisplayTitle.StartsWith("实战："));
                Assert.Equal(n.Entry.Kind == PageKind.Signpost, n.DisplayTitle.StartsWith("↗ "));
            });
            Assert.Equal(PageCatalog.All.Select(e => e.Category).Distinct(), vm.Nodes.Select(n => n.DisplayTitle));
            Assert.All(vm.Nodes, n => Assert.Null(n.Entry));
        }

        [AvaloniaFact]
        public void Main_window_opens_on_carousel_with_only_the_first_category_expanded()
        {
            var (window, vm) = OpenShell();

            Assert.Equal("Carousel", vm.SelectedNode?.DisplayTitle);
            Assert.IsType<CarouselPage>(window.ByName<ContentControl>("PageHost").Content);
            Assert.Same(vm.SelectedNode, window.ByName<TreeView>("NavTree").SelectedItem);
            Assert.Equal(new[] { true, false, false, false, false, false, false, false, false, false, false, false },
                vm.Nodes.Select(n => n.IsExpanded));
        }

        [AvaloniaFact]
        public void Every_navigation_node_shows_its_page_without_errors()
        {
            var (window, vm) = OpenShell();
            try
            {
                var visits = WalkEveryPage(window, vm);

                Assert.Equal(119, visits.Count);
                Assert.Empty(visits.Where(v => v.Error != null).Select(v => $"{v.Title}: {v.Error}"));
                Assert.Empty(visits.Where(v => v.Shown != v.Expected).Select(v => v.Title));
            }
            finally
            {
                // Window and Dialogs pages open child windows; closing the shell closes them too.
                window.Close();
            }
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_page_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var (window, vm) = OpenShell();
            try
            {
                WalkEveryPage(window, vm);

                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Selecting_a_category_clears_the_page_and_reselecting_builds_a_fresh_one()
        {
            var (_, vm) = OpenShell();
            var carousel = vm.SelectedNode!;
            var first = vm.CurrentPage;

            vm.SelectedNode = vm.Nodes[1];
            Assert.Null(vm.CurrentPage);

            vm.SelectedNode = carousel;
            Assert.IsType<CarouselPage>(vm.CurrentPage);
            Assert.NotSame(first, vm.CurrentPage);
        }

        [AvaloniaFact]
        public void Clicking_a_tree_item_selects_its_page()
        {
            var (window, vm) = OpenShell();
            var input = vm.Nodes.Single(n => n.DisplayTitle == "Input");
            input.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var item = window.ByName<TreeView>("NavTree").GetVisualDescendants().OfType<TreeViewItem>()
                .Single(i => i.DataContext == input.Children[0]);

            window.Click(item);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Button", vm.SelectedNode?.DisplayTitle);
            Assert.IsType<ButtonPage>(window.ByName<ContentControl>("PageHost").Content);
        }
    }
}
```

- [ ] **Step 3: 运行**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests`
Expected: `失败: 0，通过: 9`。走完 119 个节点约 10 秒。

- [ ] **Step 4: 破坏检查（临时改 `src/`，必须还原）**

1. 在 `src/Avalonia.ControlsDemo/Navigation/PageCatalog.Media.cs` 删掉 `list.Add(HowToPage<ImageHowToPage>(...));` 一行 → Run 同上 → Expected: 4 个失败（`Catalog_registers_119_pages_by_kind`、`Catalog_counts_pages_per_category_in_registration_order`、`Navigation_titles_are_unique_and_prefixed_by_kind`、`Every_navigation_node_shows_its_page_without_errors`）。
2. 在 `src/Avalonia.ControlsDemo/Navigation/NavNode.cs` 把 `PageKind.HowTo => $"实战：{title}",` 改成 `PageKind.HowTo => title,` → Expected: 1 个失败（`Navigation_titles_are_unique_and_prefixed_by_kind`）。

每次改完后还原：`git checkout -- src/Avalonia.ControlsDemo/Navigation`，再确认 `git status --short src` 为空。

- [ ] **Step 5: Commit**

```bash
git add tests/Avalonia.ControlsDemo.Tests hello-avalonia.slnx
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add ControlsDemo test project and walk every navigation node

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: H2——实战页的 ViewModel 与两个存储类

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/ViewModelTests.cs`、`tests/Avalonia.ControlsDemo.Tests/PersistenceTests.cs`

**Interfaces:**
- Consumes（Task 1）：项目骨架；`UiHelpers.WaitUntil`。
- Consumes（`src/Avalonia.ControlsDemo/ViewModels`，均为 `public`）：`TextBoxHowToViewModel`、`DatePickerHowToViewModel`、`ListBoxHowToViewModel`/`TodoItem`、`ExpanderHowToViewModel`、`TreeViewHowToViewModel`/`FolderNode`/`FileNode`/`LazyNode`、`DataGridHowToViewModel`/`DemoProduct`、`MenuHowToViewModel`/`RecentFile`/`EmptyRecent`、`NavigationHowToViewModel`/`HomeNavViewModel`/`SettingsNavViewModel`/`AboutNavViewModel`、`TabControlHowToViewModel`、`NotificationsHowToViewModel`、`DialogsHowToViewModel`/`IDialogService`、`ImageHowToViewModel`、`DataPersistenceHowToViewModel(string settingsFolder, string recentFolder)`、`SettingsStore(string folder)`、`RecentFiles(string folder)`（`Capacity` = 10）。
- Produces：无（Task 4 只依赖同样的 `src/` 类型）。

哪些用 `[AvaloniaFact]`：`TabControlHowToViewModel` 的图标是 `StreamGeometry.Parse`、`ImageHowToViewModel` 用 `AssetLoader` 与 `Bitmap`，都要平台；`NotificationsHowToViewModel` 的定时移除 `await` 之后要回到 UI 线程、靠 `WaitUntil` 推 dispatcher。其余用普通 `[Fact]`，不起 Avalonia。`DialogsHowToViewModel` 通过假的 `IDialogService` 测试，不开窗口；真实对话框的流程在 Task 4。

`PersistenceTests` 每个实例一个新的临时根目录，`Dispose` 删除；xUnit 每个测试新建一个实例，所以测试之间互不影响。**不要**在这里用 `SettingsStore.DefaultDirectory`，第一条测试只断言它的路径，不读写。

- [ ] **Step 1: ViewModel 测试**

#### `tests/Avalonia.ControlsDemo.Tests/ViewModelTests.cs`

```csharp
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Tests.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // Plain [Fact] where the view model only touches CommunityToolkit types; [AvaloniaFact] where it needs the
    // platform (StreamGeometry.Parse, AssetLoader, Bitmap) or awaits a continuation on the UI thread.
    public class ViewModelTests
    {
        // ---- TextBox / DatePicker validation ----

        [Fact]
        public void Username_validation_reports_required_then_min_length_then_passes()
        {
            var vm = new TextBoxHowToViewModel();
            Assert.Equal("校验通过（输入至少 3 个字符）", vm.ErrorText);

            vm.Username = "ab";
            Assert.Equal("至少 3 个字符", vm.ErrorText);
            vm.Username = "";
            Assert.Equal("用户名必填", vm.ErrorText);
            vm.Username = "abc";
            Assert.False(vm.HasErrors);
            Assert.Equal("校验通过（输入至少 3 个字符）", vm.ErrorText);
        }

        [Fact]
        public void Error_text_change_is_announced_with_the_validation_result()
        {
            var vm = new TextBoxHowToViewModel();
            var raised = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.ErrorText)) raised++; };

            vm.Username = "ab";

            Assert.True(raised >= 1);
        }

        [Fact]
        public void Date_and_time_combine_only_when_both_are_set_and_past_dates_are_rejected()
        {
            var vm = new DatePickerHowToViewModel();
            Assert.Equal("请同时选择日期和时间", vm.Combined);
            Assert.Equal("", vm.ErrorText);

            vm.Date = DateTimeOffset.Now.AddDays(-1);
            Assert.Equal("日期不能早于今天", vm.ErrorText);

            vm.Date = new DateTimeOffset(2099, 1, 2, 0, 0, 0, TimeSpan.Zero);
            Assert.Equal("", vm.ErrorText);
            Assert.Equal("请同时选择日期和时间", vm.Combined);
            vm.Time = new TimeSpan(9, 30, 0);
            Assert.Equal("预约时间：2099-01-02 09:30", vm.Combined);
        }

        [Fact]
        public void Today_is_not_in_the_past()
        {
            var vm = new DatePickerHowToViewModel { Date = DateTimeOffset.Now };

            Assert.False(vm.HasErrors);
        }

        // ---- ListBox / Expander ----

        [Fact]
        public void Done_count_follows_the_check_boxes_on_the_data()
        {
            var vm = new ListBoxHowToViewModel();
            var raised = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.DoneCount)) raised++; };
            Assert.Equal(new[] { "写计划", "跑探针", "目视检查", "提交" }, vm.Todos.Select(t => t.Title));

            vm.Todos[1].IsDone = true;
            vm.Todos[3].IsDone = true;

            Assert.Equal(2, vm.DoneCount);
            Assert.Equal(2, raised);
        }

        [Fact]
        public void Opening_one_expander_section_closes_the_other()
        {
            var vm = new ExpanderHowToViewModel();
            Assert.Equal(-1, vm.OpenSection);

            vm.IsAdvancedOpen = true;
            Assert.Equal(1, vm.OpenSection);
            vm.IsGeneralOpen = true;
            Assert.Equal(0, vm.OpenSection);
            Assert.False(vm.IsAdvancedOpen);

            // Collapsing a section that is not the open one must not close the open one.
            vm.IsAboutOpen = false;
            Assert.Equal(0, vm.OpenSection);
            vm.IsGeneralOpen = false;
            Assert.Equal(-1, vm.OpenSection);
        }

        // ---- TreeView ----

        [Fact]
        public void Expand_all_and_collapse_all_recurse_through_folders()
        {
            var vm = new TreeViewHowToViewModel();
            var project = (FolderNode)vm.Roots[0];
            var src = (FolderNode)project.Children[0];

            vm.ExpandAll(vm.Roots);
            Assert.True(project.IsExpanded);
            Assert.True(src.IsExpanded);
            Assert.True(vm.Roots[1].IsExpanded);
            // Files have nothing to expand.
            Assert.False(src.Children[0].IsExpanded);

            vm.CollapseAll(vm.Roots);
            Assert.False(project.IsExpanded);
            Assert.False(src.IsExpanded);
        }

        [Fact]
        public void Lazy_node_loads_its_children_on_the_first_expand_only()
        {
            var node = new TreeViewHowToViewModel().Lazy[0];
            Assert.Equal("远程目录", node.Name);
            Assert.Equal(new[] { "加载中…" }, node.Children.Select(c => c.Name));

            node.IsExpanded = true;
            Assert.Equal(new[] { "子项 1", "子项 2", "子项 3" }, node.Children.Select(c => c.Name));
            Assert.Equal(1, node.LoadCount);

            node.IsExpanded = false;
            node.IsExpanded = true;
            Assert.Equal(1, node.LoadCount);
            Assert.Equal(3, node.Children.Count);
        }

        // ---- DataGrid ----

        [Fact]
        public void Data_grid_views_group_by_category_and_sort_by_name_descending()
        {
            var vm = new DataGridHowToViewModel();

            Assert.Equal(new object?[] { "Fruit", "Bakery" },
                vm.Grouped.Groups!.Cast<Avalonia.Collections.DataGridCollectionViewGroup>().Select(g => g.Key));
            Assert.Equal(new[] { "Croissant", "Cherry", "Bread", "Banana", "Bagel", "Apple" },
                vm.Sorted.Cast<DemoProduct>().Select(p => p.Name));
            Assert.Equal(new[] { "Apple", "Bread", "Cherry", "Bagel", "Banana", "Croissant" }, vm.Details.Select(p => p.Name));
        }

        // ---- Menu ----

        [Fact]
        public void Recent_menu_shows_a_disabled_placeholder_once_the_list_is_empty()
        {
            var vm = new MenuHowToViewModel();
            var raised = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.RecentMenuItems)) raised++; };
            Assert.Equal(new[] { "报告.docx", "预算.xlsx", "方案.pptx" }, vm.RecentMenuItems.Cast<RecentFile>().Select(f => f.Name));

            vm.OpenRecentCommand.Execute(vm.RecentFiles[1]);
            Assert.Equal("打开 预算.xlsx", vm.LastAction);

            vm.ClearRecentCommand.Execute(null);
            var placeholder = Assert.IsType<EmptyRecent>(Assert.Single(vm.RecentMenuItems));
            Assert.False(vm.OpenRecentCommand.CanExecute(placeholder));
            Assert.True(raised >= 1);
        }

        [Fact]
        public void Delete_keeps_the_last_item_and_save_reports_the_shared_command()
        {
            var vm = new MenuHowToViewModel();
            Assert.Equal("（无）", vm.LastAction);
            Assert.False(vm.DeleteCommand.CanExecute(null));

            vm.DeleteCommand.Execute("苹果");
            vm.DeleteCommand.Execute("面包");
            Assert.Equal(new[] { "樱桃" }, vm.Items);
            Assert.Equal("删除 面包", vm.LastAction);
            Assert.False(vm.DeleteCommand.CanExecute("樱桃"));

            vm.SaveCommand.Execute(null);
            Assert.Equal("保存（Ctrl+S 或菜单都会走到这里）", vm.LastAction);
        }

        // ---- Navigation ----

        [Fact]
        public void Navigation_keeps_back_and_forward_stacks()
        {
            var vm = new NavigationHowToViewModel();
            Assert.Equal("首页", vm.Current.Title);
            Assert.Equal("后退栈 0 项，前进栈 0 项", vm.HistoryText);
            Assert.False(vm.GoBackCommand.CanExecute(null));

            vm.GoSettingsCommand.Execute(null);
            vm.GoSettingsCommand.Execute(null);
            vm.GoAboutCommand.Execute(null);
            Assert.Equal("关于", vm.Current.Title);
            // Going to the page you are on adds nothing.
            Assert.Equal("后退栈 2 项，前进栈 0 项", vm.HistoryText);

            vm.GoBackCommand.Execute(null);
            Assert.Equal("设置", vm.Current.Title);
            Assert.Equal("后退栈 1 项，前进栈 1 项", vm.HistoryText);
            Assert.True(vm.GoForwardCommand.CanExecute(null));

            // A new navigation drops the forward history.
            vm.GoHomeCommand.Execute(null);
            Assert.Equal("后退栈 2 项，前进栈 0 项", vm.HistoryText);
            Assert.False(vm.GoForwardCommand.CanExecute(null));
        }

        [Fact]
        public void Navigation_pages_carry_their_own_state()
        {
            Assert.StartsWith("欢迎回来。", new HomeNavViewModel().Welcome);
            var settings = new SettingsNavViewModel();
            Assert.Equal(50, settings.Volume);
            Assert.False(settings.NotifyEnabled);
            Assert.Equal("ControlsDemo · Avalonia 12.1.2", new AboutNavViewModel().Version);
        }

        // ---- TabControl (its icons are StreamGeometry, which needs the platform) ----

        [AvaloniaFact]
        public void Closing_tabs_never_removes_the_last_one_and_moves_the_selection_right()
        {
            var vm = new TabControlHowToViewModel();
            Assert.Equal(new[] { "说明", "日志", "设置" }, vm.Tabs.Select(t => t.Title));
            Assert.Same(vm.Tabs[0], vm.SelectedTab);

            vm.AddTabCommand.Execute(null);
            Assert.Equal("文档 4", vm.SelectedTab?.Title);

            vm.SelectedTab = vm.Tabs[1];
            vm.CloseTabCommand.Execute(vm.Tabs[1]);
            Assert.Equal("设置", vm.SelectedTab?.Title);
            // Closing the selected last tab selects the new last one.
            vm.SelectedTab = vm.Tabs[2];
            vm.CloseTabCommand.Execute(vm.Tabs[2]);
            Assert.Equal("设置", vm.SelectedTab?.Title);
            // Closing a tab that is not selected leaves the selection alone.
            vm.CloseTabCommand.Execute(vm.Tabs[0]);
            Assert.Equal(new[] { "设置" }, vm.Tabs.Select(t => t.Title));

            vm.CloseTabCommand.Execute(vm.Tabs[0]);
            Assert.Single(vm.Tabs);
        }

        [AvaloniaFact]
        public void Wizard_steps_build_their_content_once_and_stop_at_both_ends()
        {
            var vm = new TabControlHowToViewModel();
            Assert.Equal(0, vm.CreatedCount);
            Assert.False(vm.PreviousStepCommand.CanExecute(null));

            Assert.StartsWith("「资料」", vm.WizardSteps[1].Body);
            _ = vm.WizardSteps[1].Body;
            Assert.Equal(1, vm.CreatedCount);

            vm.NextStepCommand.Execute(null);
            vm.NextStepCommand.Execute(null);
            Assert.Equal(2, vm.WizardIndex);
            Assert.False(vm.NextStepCommand.CanExecute(null));
            Assert.True(vm.PreviousStepCommand.CanExecute(null));
        }

        // ---- Notifications (timed removal awaits on the UI thread) ----

        [AvaloniaFact]
        public async Task Timed_toasts_remove_themselves_and_sticky_ones_stay()
        {
            var vm = new NotificationsHowToViewModel();

            vm.Show("a", "info", 50);
            vm.Show("b", "error", 0);
            Assert.Equal(2, vm.Toasts.Count);
            Assert.True(vm.Toasts[0].IsInfo);
            Assert.True(vm.Toasts[1].IsError);

            await UiHelpers.WaitUntil(() => vm.Toasts.Count == 1);
            Assert.Equal("b", vm.Toasts[0].Message);

            vm.DismissCommand.Execute(vm.Toasts[0]);
            Assert.Empty(vm.Toasts);
        }

        [AvaloniaFact]
        public async Task Status_message_returns_to_ready_and_the_banner_toggles()
        {
            var vm = new NotificationsHowToViewModel();
            Assert.Equal("就绪", vm.StatusMessage);

            _ = vm.ShowStatusCommand.ExecuteAsync(null);
            Assert.StartsWith("已保存（", vm.StatusMessage);
            await UiHelpers.WaitUntil(() => vm.StatusMessage == "就绪");

            vm.ShowBannerNowCommand.Execute(null);
            Assert.True(vm.ShowBanner);
            vm.DismissBannerCommand.Execute(null);
            Assert.False(vm.ShowBanner);
        }

        // ---- Dialogs (through a fake service: no window opens) ----

        private sealed class FakeDialogs : IDialogService
        {
            public bool? Confirm { get; set; }
            public string? Answer { get; set; }
            public Color? Picked { get; set; }
            public (string Title, string Prompt, bool Required)? LastPrompt { get; private set; }
            public Color? LastInitial { get; private set; }

            public Task<bool?> ConfirmAsync(string title, string message) => Task.FromResult(Confirm);

            public Task<string?> PromptAsync(string title, string prompt, bool required)
            {
                LastPrompt = (title, prompt, required);
                return Task.FromResult(Answer);
            }

            public Task<Color?> PickColorAsync(Color initial)
            {
                LastInitial = initial;
                return Task.FromResult(Picked);
            }
        }

        [Fact]
        public async Task Confirm_reports_all_three_answers()
        {
            var dialogs = new FakeDialogs();
            var vm = new DialogsHowToViewModel(dialogs);
            Assert.Equal("（还没问过）", vm.ConfirmResult);

            dialogs.Confirm = true;
            await vm.AskConfirmCommand.ExecuteAsync(null);
            Assert.Equal("结果 true：用户点了「确定」", vm.ConfirmResult);
            dialogs.Confirm = false;
            await vm.AskConfirmCommand.ExecuteAsync(null);
            Assert.Equal("结果 false：用户点了「取消」", vm.ConfirmResult);
            dialogs.Confirm = null;
            await vm.AskConfirmCommand.ExecuteAsync(null);
            Assert.Equal("结果 null：用户直接关掉了窗口，既没确定也没取消", vm.ConfirmResult);
        }

        [Fact]
        public async Task Prompts_pass_the_required_flag_and_report_the_answer()
        {
            var dialogs = new FakeDialogs { Answer = "小明" };
            var vm = new DialogsHowToViewModel(dialogs);

            await vm.AskNameCommand.ExecuteAsync(null);
            Assert.Equal("名字：小明", vm.NameResult);
            Assert.False(dialogs.LastPrompt!.Value.Required);

            await vm.AskRequiredNameCommand.ExecuteAsync(null);
            Assert.Equal("名字（必答）：小明", vm.NameResult);
            Assert.True(dialogs.LastPrompt!.Value.Required);

            dialogs.Answer = null;
            await vm.AskNameCommand.ExecuteAsync(null);
            Assert.Equal("没有名字（取消或直接关闭）", vm.NameResult);
            await vm.AskRequiredNameCommand.ExecuteAsync(null);
            Assert.Equal("没有名字", vm.NameResult);
        }

        [Fact]
        public async Task Picking_a_color_starts_from_the_current_one_and_cancel_keeps_it()
        {
            var dialogs = new FakeDialogs { Picked = Colors.SteelBlue };
            var vm = new DialogsHowToViewModel(dialogs);
            Assert.Equal(Colors.Gray, vm.PickedColor);
            Assert.Equal("（还没选过）", vm.ColorResult);

            await vm.PickColorCommand.ExecuteAsync(null);
            Assert.Equal(Colors.Gray, dialogs.LastInitial);
            Assert.Equal(Colors.SteelBlue, vm.PickedColor);
            Assert.Equal(Colors.SteelBlue, ((ISolidColorBrush)vm.PickedBrush).Color);
            Assert.Equal($"结果 Color：{Colors.SteelBlue}", vm.ColorResult);

            dialogs.Picked = null;
            await vm.PickColorCommand.ExecuteAsync(null);
            Assert.Equal(Colors.SteelBlue, dialogs.LastInitial);
            Assert.Equal(Colors.SteelBlue, vm.PickedColor);
            Assert.Equal("结果 null：没选，颜色保持不变", vm.ColorResult);
        }

        // ---- Image (AssetLoader and Bitmap need the platform) ----

        [AvaloniaFact]
        public void Image_loads_the_logo_decoded_to_64_pixels_and_clears_it()
        {
            var vm = new ImageHowToViewModel();
            Assert.Null(vm.Photo);
            Assert.Equal("（未加载）", vm.Source);

            vm.LoadCommand.Execute(null);
            Assert.Equal(64, vm.Photo!.PixelSize.Width);
            Assert.Equal("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico（DecodeToWidth 64）", vm.Source);

            vm.ClearCommand.Execute(null);
            Assert.Null(vm.Photo);
            Assert.Equal("（未加载）", vm.Source);
        }
    }
}
```

- [ ] **Step 2: 存储测试（临时目录）**

#### `tests/Avalonia.ControlsDemo.Tests/PersistenceTests.cs`

```csharp
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // The app keeps these files under %AppData% and %LocalAppData%; every test here points the stores at its own
    // temp folder instead, so running the tests never touches the settings of a real copy of the app.
    public sealed class PersistenceTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "controls-demo-tests-" + Guid.NewGuid().ToString("N"));

        private string Settings => Path.Combine(_root, "settings");
        private string Recent => Path.Combine(_root, "recent");

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [Fact]
        public void Default_folders_are_the_per_user_app_data_folders()
        {
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.ControlsDemo"),
                SettingsStore.DefaultDirectory);
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Avalonia.ControlsDemo"),
                RecentFiles.DefaultDirectory);
        }

        [Fact]
        public void Opening_the_page_reads_without_writing()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);

            Assert.Equal("还没有设置文件，使用默认值", vm.StatusText);
            Assert.Equal("（文件不存在）", vm.FileText);
            Assert.Equal(("", 14d, false), (vm.Username, vm.FontSize, vm.NotifyEnabled));
            Assert.False(Directory.Exists(_root));
        }

        [Fact]
        public void Every_change_saves_and_a_new_view_model_reads_it_back()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);

            vm.Username = "小明";
            vm.FontSize = 18;
            vm.NotifyEnabled = true;

            Assert.Equal(3, vm.SaveCount);
            Assert.Equal("已自动保存（第 3 次）", vm.StatusText);
            // Relaxed escaping keeps the Chinese name readable in the file.
            Assert.Contains("\"Username\": \"小明\"", vm.FileText);
            Assert.False(File.Exists(vm.SettingsPath + ".tmp"));

            var reopened = new DataPersistenceHowToViewModel(Settings, Recent);
            Assert.Equal("已从文件读到上次的设置", reopened.StatusText);
            Assert.Equal(("小明", 18d, true), (reopened.Username, reopened.FontSize, reopened.NotifyEnabled));
            // Loading assigns all three properties but must not save them again.
            Assert.Equal(0, reopened.SaveCount);
        }

        [Fact]
        public void A_corrupt_file_falls_back_to_defaults_on_reload_and_on_open()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);
            vm.Username = "小明";

            vm.CorruptFileCommand.Execute(null);
            Assert.Equal("{ this is not json", vm.FileText);
            Assert.Equal("已把设置文件写坏，点「重新读取」看看会怎样", vm.StatusText);

            vm.ReloadCommand.Execute(null);
            Assert.Equal("设置文件损坏，已回退到默认值", vm.StatusText);
            Assert.Equal("", vm.Username);
            Assert.Equal(1, vm.SaveCount);

            Assert.Equal("设置文件损坏，已回退到默认值", new DataPersistenceHowToViewModel(Settings, Recent).StatusText);
        }

        [Fact]
        public void Reset_saves_the_defaults_and_delete_keeps_the_values_on_screen()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);
            vm.Username = "小明";

            vm.ResetCommand.Execute(null);
            Assert.Equal("已恢复默认值并保存", vm.StatusText);
            Assert.Equal("", vm.Username);
            Assert.Equal(2, vm.SaveCount);

            vm.Username = "小红";
            vm.DeleteFileCommand.Execute(null);
            Assert.Equal("设置文件已删除（界面上的值不变，下次启动会用默认值）", vm.StatusText);
            Assert.Equal("（文件不存在）", vm.FileText);
            Assert.Equal("小红", vm.Username);
            Assert.False(File.Exists(vm.SettingsPath));
        }

        [Fact]
        public void Recent_files_keep_the_newest_ten_and_move_a_reopened_path_to_the_top()
        {
            var recent = new RecentFiles(Recent);
            for (var i = 1; i <= 12; i++) recent.Add($@"C:\docs\{i}.txt");

            Assert.Equal(RecentFiles.Capacity, recent.Items.Count);
            Assert.Equal(@"C:\docs\12.txt", recent.Items[0]);
            Assert.Equal(@"C:\docs\3.txt", recent.Items[^1]);

            // Case-insensitive: the same file with different casing is not listed twice.
            recent.Add(@"C:\DOCS\5.TXT");
            Assert.Equal(@"C:\DOCS\5.TXT", recent.Items[0]);
            Assert.Single(recent.Items, p => p.EndsWith("5.txt", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(10, recent.Items.Count);

            Assert.Equal(recent.Items, new RecentFiles(Recent).Items);

            recent.Clear();
            Assert.Empty(new RecentFiles(Recent).Items);
        }

        [Fact]
        public void An_unreadable_recent_list_is_an_empty_list()
        {
            Directory.CreateDirectory(Recent);
            File.WriteAllText(Path.Combine(Recent, "recent.json"), "not json");

            Assert.Empty(new RecentFiles(Recent).Items);
        }

        [Fact]
        public void The_page_command_adds_documents_paths_to_the_recent_list()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);

            vm.AddRecentCommand.Execute(null);
            vm.AddRecentCommand.Execute(null);

            Assert.Equal(new[] { "文档 2.txt", "文档 1.txt" }, vm.RecentItems.Select(Path.GetFileName));
            Assert.True(File.Exists(vm.RecentPath));
            vm.ClearRecentCommand.Execute(null);
            Assert.Empty(vm.RecentItems);
        }
    }
}
```

- [ ] **Step 3: 运行**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ViewModelTests|FullyQualifiedName~PersistenceTests"`
Expected: `失败: 0，通过: 29`（`ViewModelTests` 21 + `PersistenceTests` 8）。

Run: `Test-Path "$env:APPDATA\Avalonia.ControlsDemo", "$env:LOCALAPPDATA\Avalonia.ControlsDemo"`（PowerShell）
Expected: 与运行前相同（本机编写计划时两者都是 `False`）。若本来就有，比较 `settings.json` 的修改时间没有变。

- [ ] **Step 4: 破坏检查（临时改 `src/`，必须还原）**

1. `src/Avalonia.ControlsDemo/ViewModels/ExpanderHowToViewModel.cs`：`else if (_openSection == index)` 改成 `else` → Expected: 1 个失败（`Opening_one_expander_section_closes_the_other`）。
2. `src/Avalonia.ControlsDemo/ViewModels/DataPersistenceHowToViewModel.cs`：删掉 `if (_loading) return;` → Expected: 3 个失败（`Every_change_saves_and_a_new_view_model_reads_it_back`、`A_corrupt_file_falls_back_to_defaults_on_reload_and_on_open`、`Reset_saves_the_defaults_and_delete_keeps_the_values_on_screen`）。
3. `src/Avalonia.ControlsDemo/ViewModels/NavigationHowToViewModel.cs`：删掉 `_forward.Clear();` → Expected: 1 个失败（`Navigation_keeps_back_and_forward_stacks`）。
4. `src/Avalonia.ControlsDemo/ViewModels/RecentFiles.cs`：`StringComparison.OrdinalIgnoreCase` 改成 `StringComparison.Ordinal` → Expected: 1 个失败（`Recent_files_keep_the_newest_ten_and_move_a_reopened_path_to_the_top`）。

每项用 Step 3 的第一条命令运行；改完一项就 `git checkout -- src/Avalonia.ControlsDemo/ViewModels`，最后确认 `git status --short src` 为空。

- [ ] **Step 5: Commit**

```bash
git add tests/Avalonia.ControlsDemo.Tests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: cover ControlsDemo how-to view models and the settings stores

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: H3——Data display、Feedback、Input、Layout 的 12 个实战页

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/HowToDataInputTests.cs`

**Interfaces:**
- Consumes（Task 1）：`PageHelpers.Open<TPage>`、`Text`、`Press`；（共享库）`UiHelpers.Click`、`Type`、`ByName`、`WaitUntil`；`Avalonia.Headless` 的 `MouseDown`/`MouseUp` 扩展。
- Consumes（`src/`）：12 个实战页的 `x:Name` 控件，名字见各测试；ViewModel 类型同 Task 2。
- Produces：无。

逐页覆盖（每页至少一条"操作 → 界面变化"）：`ItemsControlHowToPage` 2、`ListBoxHowToPage` 1、`DataGridHowToPage` 1、`TreeViewHowToPage` 2、`NotificationsHowToPage` 2、`DatePickerHowToPage` 1、`ComboBoxHowToPage` 1、`SliderHowToPage` 2、`TextBoxHowToPage` 4、`ExpanderHowToPage` 2、`ScrollViewerHowToPage` 2、`GridHowToPage` 1。

几处容易写错的地方（都在探针里踩过）：

- 鼠标点轨道用 `window.MouseDown/MouseUp`，需要 `using Avalonia.Headless;`，否则 CS1061。
- `UiHelpers.Type` 一次调用是**一个** `TextInput` 事件；数字框的过滤是"事后改写 `Text`"，所以 `"3b4"` 一次输入会留下 `34`，而不是整串被拒（与批 3 InputDemo 的隧道过滤不同）。
- `TreeViewHowToPage` 把选中置 null 时**不更新**文字，所以不要断言"清空选择后回到 `选中：（无）`"。
- `DataGrid` 只实现放得下的行，降序网格只比较前三行。
- 通知的 3 秒自动消失用 `WaitUntil` 的默认 5 秒等，不要加长超时去"保险"——超时就说明真的没消失。

- [ ] **Step 1: 交互测试**

#### `tests/Avalonia.ControlsDemo.Tests/HowToDataInputTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the How-to pages of Data display, Feedback, Input and Layout.
    public class HowToDataInputTests
    {
        // ---- Data display ----

        [AvaloniaFact]
        public void Tags_can_be_added_and_cleared_and_the_empty_hint_follows()
        {
            var (window, _) = PageHelpers.Open<ItemsControlHowToPage>();
            var tags = window.ByName<ItemsControl>("Tags");
            var empty = window.ByName<TextBlock>("EmptyText");
            Assert.Equal(new[] { "Avalonia", "XAML" }, tags.Items.Cast<string>());
            Assert.False(empty.IsVisible);

            window.Press("AddTagButton");
            Assert.Equal(new[] { "Avalonia", "XAML", "标签 1" }, tags.Items.Cast<string>());

            window.Press("ClearTagsButton");
            Assert.Equal(0, tags.ItemCount);
            Assert.True(empty.IsVisible);

            // The counter keeps going after a clear.
            window.Press("AddTagButton");
            Assert.Equal(new[] { "标签 2" }, tags.Items.Cast<string>());
            Assert.False(empty.IsVisible);
        }

        [AvaloniaFact]
        public void Striped_rows_mark_every_odd_container_and_the_big_list_is_virtualized()
        {
            var (window, _) = PageHelpers.Open<ItemsControlHowToPage>();
            var striped = window.ByName<ItemsControl>("Striped");
            var big = window.ByName<ItemsControl>("Big");

            Assert.Equal(new[] { false, true, false, true },
                Enumerable.Range(0, 4).Select(i => striped.ContainerFromIndex(i)!.Classes.Contains("odd")));
            Assert.Equal(1000, big.ItemCount);
            Assert.NotNull(big.ContainerFromIndex(0));
            Assert.Null(big.ContainerFromIndex(999));
            Assert.StartsWith("共 1000 项，已实现 ", window.Text("RealizedText"));
        }

        [AvaloniaFact]
        public void Ticking_a_row_check_box_changes_the_data_not_the_selection()
        {
            var (window, page) = PageHelpers.Open<ListBoxHowToPage>();
            var vm = (ListBoxHowToViewModel)page.DataContext!;
            var todos = window.ByName<ListBox>("Todos");
            Assert.Equal("已完成 0 项", window.Text("DoneText"));

            window.Click(todos.GetVisualDescendants().OfType<CheckBox>().ElementAt(1));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("已完成 1 项", window.Text("DoneText"));
            Assert.True(vm.Todos[1].IsDone);
            Assert.Equal(-1, todos.SelectedIndex);
        }

        [AvaloniaFact]
        public void Data_grids_show_groups_descending_names_and_all_columns()
        {
            var (window, _) = PageHelpers.Open<DataGridHowToPage>();
            string?[] Rows(string name)
            {
                var grid = window.ByName<DataGrid>(name);
                grid.BringIntoView();
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                return grid.GetVisualDescendants().OfType<DataGridRow>().Select(r => (r.DataContext as DemoProduct)?.Name).ToArray();
            }

            Assert.Equal(2, window.ByName<DataGrid>("GroupGrid").GetVisualDescendants().OfType<DataGridRowGroupHeader>().Count());
            // Only the rows that fit are realized, so compare the start of the sorted list.
            Assert.Equal(new[] { "Croissant", "Cherry", "Bread" }, Rows("SortGrid").Take(3));
            Assert.Equal(new[] { "名称", "分类", "价格", "有货" }, window.ByName<DataGrid>("DetailGrid").Columns.Select(c => c.Header));
        }

        [AvaloniaFact]
        public void Tree_buttons_expand_and_collapse_and_selection_describes_the_node()
        {
            var (window, page) = PageHelpers.Open<TreeViewHowToPage>();
            var vm = (TreeViewHowToViewModel)page.DataContext!;
            var mixed = window.ByName<TreeView>("Mixed");
            int Visible() => mixed.GetVisualDescendants().OfType<TreeViewItem>().Count(i => i.IsEffectivelyVisible);
            Assert.Equal(2, Visible());

            window.Press("ExpandAllButton");
            window.UpdateLayout();
            Assert.Equal(7, Visible());

            var src = (FolderNode)((FolderNode)vm.Roots[0]).Children[0];
            mixed.SelectedItem = src.Children[1];
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中文件：Program.cs（2 KB）", window.Text("MixedText"));
            mixed.SelectedItem = vm.Roots[0];
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中文件夹：项目（2 项）", window.Text("MixedText"));

            window.Press("CollapseAllButton");
            window.UpdateLayout();
            Assert.Equal(2, Visible());
        }

        [AvaloniaFact]
        public void Expanding_the_lazy_node_loads_once_and_the_page_counts_it()
        {
            var (window, page) = PageHelpers.Open<TreeViewHowToPage>();
            var node = ((TreeViewHowToViewModel)page.DataContext!).Lazy[0];
            Assert.Equal("远程目录已加载 0 次", window.Text("LazyText"));

            node.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            node.IsExpanded = false;
            node.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("远程目录已加载 1 次", window.Text("LazyText"));
        }

        // ---- Feedback ----

        // Info and success toasts last 3 s; WaitUntil's 5 s default covers that with room to spare.
        [AvaloniaFact]
        public async Task Timed_toasts_disappear_and_the_sticky_one_needs_its_close_button()
        {
            var (window, page) = PageHelpers.Open<NotificationsHowToPage>();
            var vm = (NotificationsHowToViewModel)page.DataContext!;

            window.Press("InfoButton");
            window.Press("SuccessButton");
            window.Press("StickyButton");
            Assert.Equal(new[] { "第 1 条信息", "第 2 条：已完成", "第 3 条：出错了，需要手动关闭" }, vm.Toasts.Select(t => t.Message));

            await UiHelpers.WaitUntil(() => vm.Toasts.Count == 1);
            Assert.True(vm.Toasts[0].IsError);

            window.Click(window.ByName<ItemsControl>("ToastList").GetVisualDescendants().OfType<Button>().Single());
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(vm.Toasts);
        }

        [AvaloniaFact]
        public async Task Status_line_says_saved_then_ready_and_the_banner_opens()
        {
            var (window, _) = PageHelpers.Open<NotificationsHowToPage>();
            Assert.False(window.ByName<Border>("Banner").IsVisible);

            window.Press("StatusButton");
            Assert.StartsWith("已保存（", window.Text("StatusText"));
            await UiHelpers.WaitUntil(() => window.Text("StatusText") == "就绪");

            window.Press("BannerButton");
            Assert.True(window.ByName<Border>("Banner").IsVisible);
        }

        // ---- Input ----

        [AvaloniaFact]
        public void Date_and_time_pickers_feed_the_combined_label_and_reject_past_dates()
        {
            var (window, _) = PageHelpers.Open<DatePickerHowToPage>();
            var date = window.ByName<DatePicker>("DateBox");
            Assert.Equal("请同时选择日期和时间", window.Text("CombinedLabel"));

            date.SelectedDate = DateTimeOffset.Now.AddDays(-1);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("日期不能早于今天", window.Text("DateErrorLabel"));

            date.SelectedDate = new DateTimeOffset(2099, 1, 2, 0, 0, 0, TimeSpan.Zero);
            window.ByName<TimePicker>("TimeBox").SelectedTime = new TimeSpan(9, 30, 0);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("预约时间：2099-01-02 09:30", window.Text("CombinedLabel"));
            Assert.Equal("", window.Text("DateErrorLabel"));
        }

        [AvaloniaFact]
        public void Combo_boxes_report_selected_value_and_search_picks_an_employee()
        {
            var (window, _) = PageHelpers.Open<ComboBoxHowToPage>();
            var priority = window.ByName<ComboBox>("PriorityBox");
            var search = window.ByName<AutoCompleteBox>("SearchEmployee");
            Assert.Equal(5, window.ByName<ComboBox>("EmployeeBox").ItemCount);
            Assert.Equal("SelectedValue = (空)", window.Text("PriorityResult"));

            priority.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedValue = High", window.Text("PriorityResult"));

            window.Type(search.GetVisualDescendants().OfType<TextBox>().First(), "王");
            Dispatcher.UIThread.RunJobs();
            Assert.True(search.IsDropDownOpen);
            Assert.Equal("未选择", window.Text("SearchResult"));

            search.SelectedItem = search.ItemsSource!.Cast<Employee>().Single(e => e.Name == "王芳");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("已选：王芳，市场部", window.Text("SearchResult"));
            search.SelectedItem = null;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("未选择", window.Text("SearchResult"));
        }

        [AvaloniaFact]
        public void Colour_sliders_drive_the_hex_text_and_the_preview()
        {
            var (window, _) = PageHelpers.Open<SliderHowToPage>();
            Assert.Equal("#4080C0", window.Text("HexText"));

            window.ByName<Slider>("RedSlider").Value = 255;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("#FF80C0", window.Text("HexText"));
            Assert.Equal(Color.Parse("#FF80C0"), ((ISolidColorBrush)window.ByName<Border>("Preview").Background!).Color);
        }

        // A click at 90% of the track jumps there; the disabled and hit-test-invisible sliders must not move.
        [AvaloniaFact]
        public void Clicking_the_track_moves_only_the_slider_that_accepts_input()
        {
            var (window, _) = PageHelpers.Open<SliderHowToPage>();
            void ClickAt90(string name)
            {
                var slider = window.ByName<Slider>(name);
                slider.BringIntoView();
                Dispatcher.UIThread.RunJobs();
                var point = slider.TranslatePoint(new Point(slider.Bounds.Width * 0.9, slider.Bounds.Height / 2), window)!.Value;
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
            }

            ClickAt90("DisabledSlider");
            ClickAt90("NoHitSlider");
            ClickAt90("IntSlider");

            Assert.Equal(5, window.ByName<Slider>("DisabledSlider").Value);
            Assert.Equal(5, window.ByName<Slider>("NoHitSlider").Value);
            Assert.Equal(9, window.ByName<Slider>("IntSlider").Value);
            Assert.Equal("Value = 9", window.Text("IntResult"));
        }

        [AvaloniaFact]
        public void Username_box_shows_each_validation_message_as_you_type()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("UsernameBox");

            window.Type(box, "ab");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("至少 3 个字符", window.Text("ErrorLabel"));
            window.Type(box, "c");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("校验通过（输入至少 3 个字符）", window.Text("ErrorLabel"));
            box.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("用户名必填", window.Text("ErrorLabel"));
        }

        // The filter rewrites the text after the fact, so a mixed paste keeps its digits and an assignment is
        // filtered too; the hint only shows when something was dropped.
        [AvaloniaFact]
        public void Digits_box_drops_everything_but_digits()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("DigitsBox");

            window.Type(box, "1");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("1", ""), (box.Text, window.Text("DigitsResult")));
            window.Type(box, "a");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("1", "已滤掉非数字"), (box.Text, window.Text("DigitsResult")));
            window.Type(box, "2");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("12", ""), (box.Text, window.Text("DigitsResult")));
            window.Type(box, "3b4");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("1234", box.Text);
            box.Text = "x9";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(("9", "已滤掉非数字"), (box.Text, window.Text("DigitsResult")));
        }

        [AvaloniaFact]
        public void Search_box_filters_the_fruit_list_and_the_clear_button_resets_it()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("SearchBox");
            var list = window.ByName<ListBox>("SearchList");
            Assert.Equal(7, list.ItemCount);

            window.Type(box, "西");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "西瓜" }, list.Items.Cast<string>());
            box.Text = "子";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "橙子" }, list.Items.Cast<string>());

            window.Press("ClearButton");
            Assert.Equal("", box.Text);
            Assert.Equal(7, list.ItemCount);
        }

        [AvaloniaFact]
        public void Undo_and_redo_buttons_drive_the_text_box_history()
        {
            var (window, _) = PageHelpers.Open<TextBoxHowToPage>();
            var box = window.ByName<TextBox>("UndoBox");
            box.CaretIndex = box.Text!.Length;

            window.Type(box, "！");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("改一改，再撤销！", box.Text);

            window.Press("UndoButton");
            Assert.Equal("改一改，再撤销", box.Text);
            window.Press("RedoButton");
            Assert.Equal("改一改，再撤销！", box.Text);
        }

        // ---- Layout ----

        [AvaloniaFact]
        public void Accordion_keeps_at_most_one_section_open()
        {
            var (window, _) = PageHelpers.Open<ExpanderHowToPage>();
            Expander Section(int n) => window.ByName<Expander>("Section" + n);
            void ClickHeader(int n)
            {
                window.Click(Section(n).GetVisualDescendants().OfType<ToggleButton>().First());
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("OpenSection = -1（-1 表示全收起）", window.Text("OpenLabel"));

            ClickHeader(2);
            Assert.Equal("OpenSection = 1（-1 表示全收起）", window.Text("OpenLabel"));
            ClickHeader(1);
            Assert.Equal("OpenSection = 0（-1 表示全收起）", window.Text("OpenLabel"));
            Assert.False(Section(2).IsExpanded);
            ClickHeader(1);
            Assert.Equal("OpenSection = -1（-1 表示全收起）", window.Text("OpenLabel"));
        }

        [AvaloniaFact]
        public void Direction_box_and_check_box_change_the_demo_expander()
        {
            var (window, _) = PageHelpers.Open<ExpanderHowToPage>();
            var box = window.ByName<ComboBox>("DirectionBox");
            var demo = window.ByName<Expander>("DirectionDemo");
            Assert.Equal(new object[] { ExpandDirection.Down, ExpandDirection.Up, ExpandDirection.Left, ExpandDirection.Right },
                box.Items.Cast<object>());

            box.SelectedItem = ExpandDirection.Up;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ExpandDirection.Up, demo.ExpandDirection);

            window.Press("DisabledCheck");
            Assert.False(demo.IsEnabled);
        }

        // Each ScrollToEnd reaches the bottom, which loads ten more, up to sixty.
        [AvaloniaFact]
        public void Scrolling_to_the_end_loads_more_items_until_sixty()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerHowToPage>();
            Assert.Equal("已加载 20 项", window.Text("FeedCount"));

            for (var i = 0; i < 5; i++)
            {
                window.Press("LoadButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal("已加载 60 项", window.Text("FeedCount"));
            Assert.Equal(60, window.ByName<StackPanel>("FeedList").Children.Count);
        }

        [AvaloniaFact]
        public void Jump_button_scrolls_the_target_into_view_and_chaining_can_be_turned_off()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerHowToPage>();
            var jump = window.ByName<ScrollViewer>("JumpScroller");
            Assert.Equal(0, jump.Offset.Y);

            window.Press("JumpButton");
            window.UpdateLayout();
            Assert.True(jump.Offset.Y > 0);

            Assert.True(window.ByName<ScrollViewer>("ChainInner").IsScrollChainingEnabled);
            window.Press("ChainCheck");
            Assert.False(window.ByName<ScrollViewer>("ChainInner").IsScrollChainingEnabled);
        }

        [AvaloniaFact]
        public void Grid_lines_toggle_and_shared_size_columns_line_up()
        {
            var (window, _) = PageHelpers.Open<GridHowToPage>();
            var formA = window.ByName<Grid>("FormA");
            var formB = window.ByName<Grid>("FormB");

            Assert.Equal("Label", formA.ColumnDefinitions[0].SharedSizeGroup);
            Assert.Equal(formA.ColumnDefinitions[0].ActualWidth, formB.ColumnDefinitions[0].ActualWidth);

            window.Press("LinesCheck");
            Assert.True(window.ByName<Grid>("SpanGrid").ShowGridLines);
        }
    }
}
```

- [ ] **Step 2: 运行**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~HowToDataInputTests"`
Expected: `失败: 0，通过: 21`。通知两条各等约 3 秒。

- [ ] **Step 3: 破坏检查（临时改 `src/`，必须还原）**

1. `src/Avalonia.ControlsDemo/Views/Pages/Layout/ScrollViewerHowToPage.axaml.cs`：`MaxItems = 60` 改成 `70` → Expected: 1 个失败（`Scrolling_to_the_end_loads_more_items_until_sixty`）。
2. `src/Avalonia.ControlsDemo/Views/Pages/DataDisplay/ItemsControlHowToPage.axaml.cs`：删掉 `e.Container.Classes.Add("odd");` → Expected: 1 个失败（`Striped_rows_mark_every_odd_container_and_the_big_list_is_virtualized`）。
3. `src/Avalonia.ControlsDemo/Views/Pages/Input/TextBoxHowToPage.axaml.cs`：`text.Where(char.IsDigit)` 改成 `text.Where(char.IsLetterOrDigit)` → Expected: 1 个失败（`Digits_box_drops_everything_but_digits`）。

每项改完 `git checkout -- src/Avalonia.ControlsDemo/Views`，最后确认 `git status --short src` 为空。

- [ ] **Step 4: Commit**

```bash
git add tests/Avalonia.ControlsDemo.Tests
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add interaction tests for the data, feedback, input and layout how-to pages

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: H3——Media、Menus、Navigation、Primitives、System 的 7 个实战页，并修复过渡下拉框

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/HowToShellTests.cs`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs:16-21`

**Interfaces:**
- Consumes（Task 1）：`PageHelpers.Open<TPage>`、`Text`、`Press`、`PressText`；（共享库）`UiHelpers.Show`、`Click`、`Type`、`ByName`、`ButtonWithText`、`WaitUntil`。
- Consumes（`src/`）：`WindowHowToPage.Editor : Window?`（`public`，编辑窗口）；`DemoDialogs` 生成的对话框控件名 `OkButton`、`CancelButton`、`AnswerBox`、`Swatch_Red|Green|Blue|Orange`；编辑窗口里的 `EditorBox`、`EditorSaveButton`、`EditorState`。
- Produces：无。

逐页覆盖：`ImageHowToPage` 2、`MenuHowToPage` 3、`NavigationHowToPage` 3（其中 1 条是缺陷测试）、`TabControlHowToPage` 3、`WindowHowToPage` 5、`DialogsHowToPage` 5、`DataPersistenceHowToPage` 1。

要点：

- **窗口一律在 `finally` 里关**。`WindowHowToPage` 靠 `DetachedFromVisualTree` 关掉它开的全部窗口（含没有所有者的独立窗口），关宿主窗口会触发它。
- `Place_buttons_…` 先 `window.Width = 900`：`RestoreButton` 的右边缘在 600 宽窗口的 x=567，贴边，放宽免得随主题字号变化而点空（批 3、批 4 的教训）。示例窗口的初始位置随宿主位置变化，测试只断言"恢复后与记下的完全一致"，不写死坐标。
- **`DataPersistenceHowToPage` 必须先换 `DataContext` 再操作**（Review Focus 第 1 条）。页面构造只读不写，所以构造后、`Show` 前替换是安全的。
- 缺陷测试 `Transition_box_shows_the_transition_the_host_really_uses` 在修复前**必须红**，红的原因必须是 `Expected: CrossFade / Actual: 无`；别的原因（找不到控件之类）说明测试抄错了。

- [ ] **Step 1: 交互测试（其中一条会先红）**

#### `tests/Avalonia.ControlsDemo.Tests/HowToShellTests.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the How-to pages of Media, Menus, Navigation, Primitives and System.
    public class HowToShellTests
    {
        // ---- Media ----

        // The placeholder's Border is what hides, so the TextBlock itself stays IsVisible; check the effective value.
        [AvaloniaFact]
        public void Loading_the_logo_hides_the_placeholder_and_clearing_brings_it_back()
        {
            var (window, _) = PageHelpers.Open<ImageHowToPage>();
            var placeholder = window.ByName<TextBlock>("PlaceholderText");
            Assert.True(placeholder.IsEffectivelyVisible);
            Assert.IsType<WriteableBitmap>(window.ByName<Image>("Avatar").Source);

            window.Press("LoadButton");
            Assert.False(placeholder.IsEffectivelyVisible);
            Assert.Equal(64, ((Bitmap)window.ByName<Image>("PhotoImage").Source!).PixelSize.Width);
            Assert.Equal("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico（DecodeToWidth 64）", window.Text("SourceText"));

            window.Press("ClearButton");
            Assert.True(placeholder.IsEffectivelyVisible);
            Assert.Equal("（未加载）", window.Text("SourceText"));
        }

        [AvaloniaFact]
        public void Screenshot_button_renders_the_card_into_a_bitmap()
        {
            var (window, _) = PageHelpers.Open<ImageHowToPage>();
            Assert.Equal("尚未截图", window.Text("ShotText"));

            window.Press("ShotButton");

            Assert.Equal("已截图 160×60", window.Text("ShotText"));
            Assert.IsType<RenderTargetBitmap>(window.ByName<Image>("ShotImage").Source);
        }

        // ---- Menus ----

        // KeyBindings on a UserControl only see keys while focus is inside it: right after Show nothing has focus.
        [AvaloniaFact]
        public void Ctrl_s_saves_only_once_focus_is_inside_the_page()
        {
            var (window, page) = PageHelpers.Open<MenuHowToPage>();
            var vm = (MenuHowToViewModel)page.DataContext!;
            void CtrlS()
            {
                window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
                window.KeyRelease(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
                Dispatcher.UIThread.RunJobs();
            }

            CtrlS();
            Assert.Equal("最近操作：（无）", window.Text("HotKeyText"));

            window.Click(window.ByName<ListBox>("Items").ContainerFromIndex(0)!);
            Dispatcher.UIThread.RunJobs();
            CtrlS();
            Assert.Equal("最近操作：保存（Ctrl+S 或菜单都会走到这里）", window.Text("HotKeyText"));
            Assert.Equal("保存（Ctrl+S 或菜单都会走到这里）", vm.LastAction);
        }

        [AvaloniaFact]
        public void Context_menu_deletes_the_selected_item()
        {
            var (window, page) = PageHelpers.Open<MenuHowToPage>();
            var vm = (MenuHowToViewModel)page.DataContext!;
            var list = window.ByName<ListBox>("Items");
            list.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();

            list.ContextMenu!.Open(list);
            Dispatcher.UIThread.RunJobs();
            window.Click(list.ContextMenu.Items.OfType<MenuItem>().Single(m => "删除".Equals(m.Header)));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new[] { "苹果", "樱桃" }, vm.Items);
            Assert.Equal("最近操作：删除 面包", window.Text("HotKeyText"));
            Assert.False(list.ContextMenu.IsOpen);
        }

        [AvaloniaFact]
        public void Check_menu_item_toggles_bold_and_clearing_recent_files_leaves_a_placeholder()
        {
            var (window, page) = PageHelpers.Open<MenuHowToPage>();
            var vm = (MenuHowToViewModel)page.DataContext!;
            var format = window.GetVisualDescendants().OfType<Menu>().First().Items.OfType<MenuItem>().ElementAt(1);

            window.Click(format);
            Dispatcher.UIThread.RunJobs();
            window.Click(format.Items.OfType<MenuItem>().Single(m => "_粗体".Equals(m.Header)));
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.IsBold);

            var recent = window.ByName<MenuItem>("RecentMenu");
            Assert.Equal(3, recent.ItemCount);
            window.PressText("清空最近的文件");
            Assert.IsType<EmptyRecent>(recent.Items.Cast<object>().Single());
        }

        // ---- Navigation ----

        [AvaloniaFact]
        public void Navigation_buttons_move_through_the_history()
        {
            var (window, _) = PageHelpers.Open<NavigationHowToPage>();
            string Line(string prefix) => window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text?.StartsWith(prefix) == true).Text!;
            Assert.False(window.ButtonWithText("◀ 后退").IsEffectivelyEnabled);

            window.PressText("设置");
            window.PressText("关于");
            Assert.Equal("当前页：关于", Line("当前页"));
            Assert.Equal("后退栈 2 项，前进栈 0 项", Line("后退栈"));

            window.PressText("◀ 后退");
            Assert.Equal("当前页：设置", Line("当前页"));
            Assert.True(window.ButtonWithText("前进 ▶").IsEffectivelyEnabled);
            window.PressText("前进 ▶");
            Assert.Equal("当前页：关于", Line("当前页"));
        }

        // Avalonia 12's TransitioningContentControl starts with a private cross-fade class, not CrossFade, so the
        // page used to show "无" while every page change still faded.
        [AvaloniaFact]
        public void Transition_box_shows_the_transition_the_host_really_uses()
        {
            var (window, _) = PageHelpers.Open<NavigationHowToPage>();

            Assert.NotNull(window.ByName<TransitioningContentControl>("Host").PageTransition);
            Assert.Equal("CrossFade", window.ByName<ComboBox>("TransitionBox").SelectedItem);
        }

        [AvaloniaFact]
        public void Transition_box_swaps_the_host_transition()
        {
            var (window, _) = PageHelpers.Open<NavigationHowToPage>();
            var box = window.ByName<ComboBox>("TransitionBox");
            var host = window.ByName<TransitioningContentControl>("Host");

            box.SelectedItem = "PageSlide";
            Assert.IsType<PageSlide>(host.PageTransition);
            box.SelectedItem = "无";
            Assert.Null(host.PageTransition);
            box.SelectedItem = "CrossFade";
            Assert.Equal(TimeSpan.FromMilliseconds(250), Assert.IsType<CrossFade>(host.PageTransition).Duration);
        }

        [AvaloniaFact]
        public void New_tab_button_adds_and_selects_and_a_close_button_removes_its_tab()
        {
            var (window, page) = PageHelpers.Open<TabControlHowToPage>();
            var vm = (TabControlHowToViewModel)page.DataContext!;
            var tabs = window.ByName<TabControl>("Tabs");
            Button[] Closers() => tabs.GetVisualDescendants().OfType<Button>().ToArray();
            Assert.Equal(3, Closers().Length);

            window.PressText("新建标签");
            Assert.Equal(4, tabs.ItemCount);
            Assert.Equal(3, tabs.SelectedIndex);
            Assert.Equal(4, Closers().Length);

            window.Click(Closers()[0]);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "日志", "设置", "文档 4" }, vm.Tabs.Select(t => t.Title));
            Assert.Equal("文档 4", vm.SelectedTab?.Title);
        }

        // Moving the strip rebuilds the headers, which drops the selection; the page puts it back afterwards.
        [AvaloniaFact]
        public void Moving_the_tab_strip_keeps_the_selected_tab()
        {
            var (window, page) = PageHelpers.Open<TabControlHowToPage>();
            var vm = (TabControlHowToViewModel)page.DataContext!;
            var tabs = window.ByName<TabControl>("Tabs");
            tabs.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();

            window.ByName<ComboBox>("PlacementBox").SelectedItem = Dock.Left;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Dock.Left, tabs.TabStripPlacement);
            Assert.Equal("设置", vm.SelectedTab?.Title);
        }

        // Only the first step is built when the page opens; each step builds once, when first shown.
        [AvaloniaFact]
        public void Wizard_builds_each_step_when_it_is_first_shown()
        {
            var (window, page) = PageHelpers.Open<TabControlHowToPage>();
            var vm = (TabControlHowToViewModel)page.DataContext!;
            Assert.Equal(1, vm.CreatedCount);
            Assert.False(window.ButtonWithText("上一步").IsEffectivelyEnabled);

            window.PressText("下一步");
            window.PressText("下一步");
            Assert.Equal(3, vm.CreatedCount);
            Assert.False(window.ButtonWithText("下一步").IsEffectivelyEnabled);

            window.PressText("上一步");
            Assert.Equal(1, vm.WizardIndex);
            Assert.Equal(3, vm.CreatedCount);
        }

        // ---- Primitives: windows ----

        // Every test that opens windows closes the host in finally: the page closes what it opened when it leaves
        // the visual tree, so nothing leaks into later tests.
        [AvaloniaFact]
        public void Free_and_owned_windows_are_counted_and_close_all_closes_both_kinds()
        {
            var (window, _) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                Assert.Equal("独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）", window.Text("WindowsText"));

                window.Press("OwnedButton");
                window.Press("FreeButton");
                window.Press("OwnedButton");
                Assert.Equal("独立窗口 1 个，子窗口 2 个；所有者的 OwnedWindows 共 2 个（含下面两块开的窗口）", window.Text("WindowsText"));
                Assert.Equal(2, window.OwnedWindows.Count);

                window.Press("CloseAllButton");
                Assert.Equal("独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）", window.Text("WindowsText"));
                Assert.Empty(window.OwnedWindows);
            }
            finally
            {
                window.Close();
            }
        }

        // RestoreButton ends at x=567 of the 600px window; widen it so the click cannot miss.
        [AvaloniaFact]
        public void Place_buttons_save_move_and_restore_the_sample_window()
        {
            var (window, _) = PageHelpers.Open<WindowHowToPage>();
            window.Width = 900;
            try
            {
                Assert.Equal("示例窗口：未打开；已记下：（还没有）", window.Text("PlaceText"));
                window.Press("PlaceOpenButton");
                window.Press("SaveButton");
                var saved = window.Text("PlaceText");
                Assert.EndsWith("ClientSize = 280×140；已记下：Position = " + saved.Split("Position = ")[1].Split('，')[0] + "，ClientSize = 280×140", saved);

                window.Press("MoveButton");
                Assert.StartsWith("示例窗口：Position = 200, 120，ClientSize = 420×260；", window.Text("PlaceText"));

                window.Press("RestoreButton");
                Assert.Equal(saved, window.Text("PlaceText"));

                window.Press("PlaceCloseButton");
                Assert.StartsWith("示例窗口：未打开；已记下：Position = ", window.Text("PlaceText"));
            }
            finally
            {
                window.Close();
            }
        }

        // ShowDialog does not block in Headless: the confirm box is simply another owned window of the editor.
        [AvaloniaFact]
        public void Closing_a_dirty_editor_asks_first_and_only_ok_lets_it_close()
        {
            var (window, page) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                window.Press("EditorButton");
                var editor = page.Editor!;
                editor.Type(editor.ByName<TextBox>("EditorBox"), "x");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("有未保存的更改", editor.Text("EditorState"));

                editor.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(editor.IsVisible);
                var confirm = Assert.Single(editor.OwnedWindows);
                confirm.Click(confirm.ByName<Button>("CancelButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("窗口有未保存的更改；Closing 被拦下 1 次；最近一次：用户选了「取消」，窗口留着", window.Text("ConfirmText"));
                Assert.True(editor.IsVisible);

                editor.Close();
                Dispatcher.UIThread.RunJobs();
                confirm = Assert.Single(editor.OwnedWindows);
                confirm.Click(confirm.ByName<Button>("OkButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("窗口未打开；Closing 被拦下 2 次；最近一次：用户选了「确定」，窗口随后关闭", window.Text("ConfirmText"));
                Assert.False(editor.IsVisible);
                Assert.Null(page.Editor);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void A_saved_editor_closes_without_asking()
        {
            var (window, page) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                window.Press("EditorButton");
                var editor = page.Editor!;
                editor.Type(editor.ByName<TextBox>("EditorBox"), "y");
                editor.Click(editor.ByName<Button>("EditorSaveButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("窗口没有未保存的更改；Closing 被拦下 0 次；最近一次：（还没触发）", window.Text("ConfirmText"));

                editor.Close();
                Dispatcher.UIThread.RunJobs();

                Assert.False(editor.IsVisible);
                Assert.Equal("窗口未打开；Closing 被拦下 0 次；最近一次：（还没触发）", window.Text("ConfirmText"));
            }
            finally
            {
                window.Close();
            }
        }

        // Review Focus: the page owns the windows it opened. Taking it out of the visual tree must close them all,
        // including the free ones that no owner would close.
        [AvaloniaFact]
        public void Leaving_the_page_closes_every_window_it_opened()
        {
            var (window, page) = PageHelpers.Open<WindowHowToPage>();
            try
            {
                window.Press("FreeButton");
                window.Press("OwnedButton");
                window.Press("PlaceOpenButton");
                window.Press("EditorButton");
                var editor = page.Editor!;
                editor.Type(editor.ByName<TextBox>("EditorBox"), "dirty");
                Dispatcher.UIThread.RunJobs();
                var opened = window.OwnedWindows.ToList();

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.All(opened, w => Assert.False(w.IsVisible));
                Assert.Equal("独立窗口 0 个，子窗口 0 个；所有者的 OwnedWindows 共 0 个（含下面两块开的窗口）", page.Text("WindowsText"));
                // A dirty editor would normally ask; leaving the page must not leave a confirm box behind.
                Assert.Empty(editor.OwnedWindows);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Primitives: dialogs ----

        private static Window Dialog(Window owner) => Assert.Single(owner.OwnedWindows);

        [AvaloniaFact]
        public async Task Confirm_dialog_reports_ok_and_closing_the_window()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("删除…");
                Assert.Equal("确认", Dialog(window).Title);
                Dialog(window).Click(Dialog(window).ByName<Button>("OkButton"));
                await UiHelpers.WaitUntil(() => vm.ConfirmResult == "结果 true：用户点了「确定」");
                Assert.Empty(window.OwnedWindows);

                window.PressText("删除…");
                Dialog(window).Close();
                await UiHelpers.WaitUntil(() => vm.ConfirmResult == "结果 null：用户直接关掉了窗口，既没确定也没取消");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Name_prompt_enables_ok_only_for_text_and_trims_the_answer()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("起名字…");
                var dialog = Dialog(window);
                var ok = dialog.ByName<Button>("OkButton");
                Assert.False(ok.IsEnabled);
                Assert.True(dialog.ByName<Button>("CancelButton").IsEnabled);

                dialog.Type(dialog.ByName<TextBox>("AnswerBox"), "  小明 ");
                Dispatcher.UIThread.RunJobs();
                Assert.True(ok.IsEnabled);
                dialog.Click(ok);

                await UiHelpers.WaitUntil(() => vm.NameResult == "名字：小明");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Required_prompt_cannot_be_cancelled_or_closed_without_an_answer()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("必答的问题…");
                var dialog = Dialog(window);
                Assert.False(dialog.ByName<Button>("CancelButton").IsEnabled);

                dialog.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(dialog.IsVisible);

                dialog.Type(dialog.ByName<TextBox>("AnswerBox"), "答");
                Dispatcher.UIThread.RunJobs();
                dialog.Click(dialog.ByName<Button>("OkButton"));
                await UiHelpers.WaitUntil(() => vm.NameResult == "名字（必答）：答");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Colour_dialog_picks_a_swatch_and_cancel_keeps_it()
        {
            var (window, page) = PageHelpers.Open<DialogsHowToPage>();
            var vm = (DialogsHowToViewModel)page.DataContext!;
            try
            {
                window.PressText("选颜色…");
                Dialog(window).Click(Dialog(window).ByName<Button>("Swatch_Blue"));
                await UiHelpers.WaitUntil(() => vm.ColorResult != "（还没选过）");
                Assert.Equal(Colors.SteelBlue, vm.PickedColor);

                window.PressText("选颜色…");
                Dialog(window).Click(Dialog(window).ByName<Button>("CancelButton"));
                await UiHelpers.WaitUntil(() => vm.ColorResult == "结果 null：没选，颜色保持不变");
                Assert.Equal(Colors.SteelBlue, vm.PickedColor);
            }
            finally
            {
                window.Close();
            }
        }

        // Headless has no picker: the open button must stay disabled instead of awaiting a dialog that never comes.
        [AvaloniaFact]
        public void Storage_line_reports_no_picker_and_keeps_the_open_button_disabled()
        {
            var (window, _) = PageHelpers.Open<DialogsHowToPage>();

            Assert.Equal("CanOpen = False，CanSave = False，CanPickFolder = False", window.Text("StorageText"));
            Assert.False(window.ByName<Button>("OpenFileButton").IsEnabled);
            Assert.Equal("（还没选过）", window.Text("FileText"));
        }

        // ---- System ----

        // The page's own view model would read and write the real %AppData%; swap in one on a temp folder before
        // anything can change a setting. Constructing the page only reads, which the smoke walk already relies on.
        [AvaloniaFact]
        public void Typing_a_name_saves_it_and_the_buttons_corrupt_reload_and_reset()
        {
            var root = Path.Combine(Path.GetTempPath(), "controls-demo-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var page = new DataPersistenceHowToPage();
                var vm = new DataPersistenceHowToViewModel(Path.Combine(root, "settings"), Path.Combine(root, "recent"));
                page.DataContext = vm;
                var window = UiHelpers.Show(page);
                Dispatcher.UIThread.RunJobs();
                bool Shows(string text) => window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == text);

                window.Type(window.GetVisualDescendants().OfType<TextBox>().First(), "小明");
                Dispatcher.UIThread.RunJobs();
                Assert.True(Shows("预览：你好，小明"));
                Assert.True(Shows("已自动保存（第 1 次）"));
                Assert.Contains("小明", vm.FileText);

                window.PressText("写坏文件");
                window.PressText("重新读取");
                Assert.True(Shows("设置文件损坏，已回退到默认值"));
                Assert.True(Shows("预览：你好，"));

                window.PressText("恢复默认");
                Assert.True(Shows("已恢复默认值并保存"));

                for (var i = 0; i < 12; i++) window.PressText("假装打开一个文件");
                Assert.True(Shows("共 10 条，上限 10"));
                window.Close();
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }
    }
}
```

- [ ] **Step 2: 运行，确认只有缺陷测试失败**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~HowToShellTests"`
Expected: `失败: 1，通过: 21`；失败的是 `Transition_box_shows_the_transition_the_host_really_uses`，消息为

```
Assert.Equal() Failure: Values differ
Expected: CrossFade
Actual:   无
```

- [ ] **Step 3: 修复 `NavigationHowToPage` 的过渡映射**

`src/Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs` 里，把

```csharp
            TransitionBox.SelectedIndex = Host.PageTransition switch
            {
                CrossFade => 1,
                PageSlide => 2,
                _ => 0
            };
```

改成

```csharp
            // The default transition is a private cross-fade type, not CrossFade, so match "none" and PageSlide
            // explicitly and treat anything else as a cross-fade.
            TransitionBox.SelectedIndex = Host.PageTransition switch
            {
                null => 0,
                PageSlide => 2,
                _ => 1
            };
```

只改这一处。`SelectionChanged` 里的处理不变：选「CrossFade」时换成 250 ms 的公开 `CrossFade`，与默认的淡入淡出效果一致。注意 `SelectedIndex` 的赋值发生在订阅 `SelectionChanged` 之前，所以初始化**不会**把默认过渡替换掉——`Host.PageTransition` 仍是默认实例，测试里的 `Assert.NotNull` 就是在钉这一点。

- [ ] **Step 4: 确认通过**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~HowToShellTests"`
Expected: `失败: 0，通过: 22`。

Run: `git status --short src`
Expected: 只有 ` M src/Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs`。

- [ ] **Step 5: 破坏检查（临时改 `src/`，必须还原）**

`src/Avalonia.ControlsDemo/Views/Pages/Primitives/WindowHowToPage.axaml.cs` 的 `DetachedFromVisualTree` 处理器里删掉 `_confirmedClose = true;` 一行 → Run Step 4 的命令 → Expected: 1 个失败（`Leaving_the_page_closes_every_window_it_opened`：脏编辑窗口弹出确认框，没有关掉）。

还原：`git checkout -- src/Avalonia.ControlsDemo/Views/Pages/Primitives/WindowHowToPage.axaml.cs`（**不要** checkout 整个 `src/`，Step 3 的修复还没提交）。再跑一次 Step 4 确认 22 通过。

- [ ] **Step 6: Commit**

```bash
git add tests/Avalonia.ControlsDemo.Tests src/Avalonia.ControlsDemo/Views/Pages/Navigation/NavigationHowToPage.axaml.cs
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add interaction tests for the remaining how-to pages and fix the transition picker

The default transition of TransitioningContentControl in Avalonia 12 is a
private cross-fade type, so the page showed \"none\" while pages still faded.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: 文档回填与全量验证

**Files:**
- Modify: `README.md:26`
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md:52,107,271,444`

**Interfaces:**
- Consumes：Task 1–4 的提交；各任务实测的测试数与破坏检查结果。
- Produces：spec 里「批 5 实测结论」的 05a 小节（05b 在其后续写）。

- [ ] **Step 1: README 的测试项目一行补上 ControlsDemo**

`README.md` 第 26 行，把

```
目前覆盖 Fundamentals、Xaml、Layout、StarterTutorial、Styling、DataBinding、DataTemplates、PropertySystem、Events、Input、Graphics、CustomControls、AppDevelopment、Services、Behaviors）
```

改成

```
目前覆盖 Fundamentals、Xaml、Layout、StarterTutorial、Styling、DataBinding、DataTemplates、PropertySystem、Events、Input、Graphics、CustomControls、AppDevelopment、Services、Behaviors、Controls）
```

只改括号里的清单，这一行其余文字不动。

- [ ] **Step 2: spec 改正页数，交付表注明拆分**

`docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`：

1. 第 52 行：`（121 个页面）` → `（119 个页面）`。
2. 第 107 行：`遍历 \`PageCatalog\` 的 121 个节点` → `遍历 \`PageCatalog\` 的 119 个节点`。
3. 第 271 行整行替换为：

```
| 5 | 05-headless-controls | ControlsDemo（119 页）。已拆分：05a 外壳 H1 + 全部 ViewModel H2 + 19 个实战页 H3；05b 80 个控件页 H3，按目录分任务 | 1 |
```

Run: `grep -n "121" docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`
Expected: 无输出。

- [ ] **Step 3: 回填「批 5 实测结论」的 05a 小节**

把 spec 里

```
## 批 5 实测结论（执行后回填）
```

替换为下面整段（标题去掉"执行后回填"，05a 写成小节，05b 执行后在其后加 `### 05b`）。`构建警告数` 与 `共 … 个测试通过` 填 Step 4 实测的数字；若与这里写的 6 和 351 不同，按实测写，并在"执行时与计划不符"里说明原因。

````markdown
## 批 5 实测结论

### 05a（计划 05a 执行后）

| 项目 | 测试数 | H1 节点数 | 说明 |
|---|---|---|---|
| `Avalonia.ControlsDemo.Tests` | 81 | 119 | `SmokeTests` 9：目录总数与分目录计数、`PageType` 不重复、`DisplayTitle` 唯一且前缀与 `Kind` 对应、启动状态、走完 119 个节点（0 异常、0 Warning）、选中目录清空页面、点击树项；`ViewModelTests` 21 + `PersistenceTests` 8（全部在临时目录）；`HowToDataInputTests` 21（12 个实战页）；`HowToShellTests` 22（7 个实战页，含过渡缺陷测试） |

执行时的实测：构建警告数 6（批开始前 6）；`dotnet test hello-avalonia.slnx` 共 351 个测试通过（批 4 结束时的 270 + 本批 81）；破坏检查：删 `ImageHowToPage` 的目录登记 → 4 个；去掉「实战：」前缀 → 1 个；Expander 去掉 `_openSection == index` 守卫 → 1 个；去掉 `if (_loading) return;` → 3 个；去掉 `_forward.Clear()` → 1 个；`OrdinalIgnoreCase` 改 `Ordinal` → 1 个；`MaxItems` 60 → 70 → 1 个；去掉 `odd` 类 → 1 个；`IsDigit` 改 `IsLetterOrDigit` → 1 个；`DetachedFromVisualTree` 去掉 `_confirmedClose = true` → 1 个，均已还原。

**`src/` 改动（用户 2026-10-15 确认）**：`NavigationHowToPage` 用 `Host.PageTransition switch { CrossFade => 1, PageSlide => 2, _ => 0 }` 初始化下拉框，但 Avalonia 12 的 `TransitioningContentControl` 默认过渡是私有嵌套类 `ImmutableCrossFade`（不是 `CrossFade` 的子类），于是下拉框显示「无」，切页却仍在淡入淡出。改为 `null => 0, PageSlide => 2, _ => 1`；先写的 `Transition_box_shows_the_transition_the_host_really_uses` 修复前失败（Expected `CrossFade`，Actual `无`）、修复后通过。

**对 05b 的决定与发现**（计划阶段在探针里实测，执行阶段原样复现）：

- **测试不碰真实的用户数据目录**：`DataPersistenceHowToPage` 构造时用 `%AppData%`/`%LocalAppData%` 下的 `Avalonia.ControlsDemo`。只构造只读不写（H1 依赖这一点，`Opening_the_page_reads_without_writing` 钉住）；凡是要操作的测试，先把 `DataContext` 换成临时目录上的 ViewModel。05b 若有页面写文件，照此处理。
- **类型名与 Avalonia 12 自带控件冲突**：`CarouselPage`（以及 `NavigationPage`、`TabbedPage` 等）在 Avalonia 12 里有同名控件，`using Avalonia.Controls;` 时报 CS0104；测试文件用 `using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;` 这类别名。05b 的控件页更容易撞名，先 build 再写断言。
- **`UserControl.KeyBindings` 要焦点在页内**：刚 `Show` 时没有焦点，快捷键无效；先点一下页内控件。断言"之前无效、之后有效"两半。
- **"隐藏"可能发生在祖先上**：`ImageHowToPage` 的占位文字本身一直 `IsVisible`，被隐藏的是外层 `Border`；判断可见性用 `IsEffectivelyVisible`。
- **`TreeViewHowToPage` 不处理选中置 null**：文字停在上一次的选中项，没有断言"清空后回到初始文字"；这是页面的已知行为，未列为缺陷。
- **窗口泄漏**：`WindowHowToPage` 在 `DetachedFromVisualTree` 里关掉它开的全部窗口（含无所有者的独立窗口与脏编辑窗口）；开窗口的测试一律在 `finally` 里关宿主。H1 走完 119 个节点后也关窗口。
- **贴边按钮**：`WindowHowToPage` 的 `RestoreButton` 右边缘在 600 宽窗口的 x=567，测试放宽到 900；05b 遇到"点了没反应"先查按钮是否超出 600 宽。
- **本批没有跳过清单**：风险表里"Headless 下构造即抛"的担忧，在 ControlsDemo 上不成立——H1 走完 119 个节点（含 `Web` 目录 4 页、`付费控件` 7 页）0 个异常、0 条 Warning。
- **H2 的 `[AvaloniaFact]` 例外**：spec 要求 H2 不起 Avalonia，但 `TabControlHowToViewModel`（`StreamGeometry.Parse`）、`ImageHowToViewModel`（`AssetLoader`、`Bitmap`）与 `NotificationsHowToViewModel`（`await` 后回 UI 线程）离开平台就跑不了，这 5 条用 `[AvaloniaFact]`；其余 24 条是普通 `[Fact]`。

执行时与计划不符、改了测试的地方：

- （执行时填写；没有就写"无"。）
````

最后那一条 `（执行时填写；没有就写"无"。）` 在执行时**必须**替换成实际情况，不要原样留下。

- [ ] **Step 4: 全量验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -3`
Expected: 0 个错误；警告数 ≤ 6（批开始前 6，新项目 0 警告）。

Run: `dotnet test hello-avalonia.slnx`
Expected: 全部通过，共 351 个（270 + 81）。

Run: `Test-Path "$env:APPDATA\Avalonia.ControlsDemo", "$env:LOCALAPPDATA\Avalonia.ControlsDemo"`（PowerShell）
Expected: 与 Task 2 Step 3 记下的相同。

Run: `git status --short`
Expected: 只有 ` M README.md` 与 ` M docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（若本计划文件尚未提交，还会有它）；没有探针文件，`src/` 下没有改动。

用实测数字回填 Step 3 那段里的警告数与测试数。

- [ ] **Step 5: Commit**

```bash
git add README.md docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 5a results and list ControlsDemo tests

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec 覆盖（批 5 前半）**

| spec 要求 | 落在哪里 |
|---|---|
| H1：`new MainWindow()` 能显示，每个导航节点能选中并布局而不抛，页数等于预期 | Task 1：`Main_window_opens_…`、`Every_navigation_node_shows_its_page_without_errors`、`Catalog_registers_119_pages_by_kind`、`Catalog_counts_pages_per_category_…` |
| H1 遍历 `PageCatalog`（spec 写 121，实测 119） | Task 1 断言 119；Task 5 改正 spec 两处 |
| H2：每个带逻辑的 ViewModel | Task 2：14 个实战页 ViewModel + `SettingsStore`、`RecentFiles`。没有实战 ViewModel 的控件页留给 05b 按页判断 |
| H2 不起 Avalonia | 24 条普通 `[Fact]`；5 条需要平台的用 `[AvaloniaFact]`，理由写在 Task 2 和回填里 |
| H3：有交互的页面至少一条"操作 → 界面变化" | Task 3（12 页）+ Task 4（7 页）= 19 个实战页全部覆盖；80 个控件页是 05b |
| 风险表："构造即抛"要建带理由的跳过清单 | H1 实测 0 异常，不需要清单；Task 5 回填写明 |
| 风险表：真实时间/动画只断言状态 | 通知用 `WaitUntil`；过渡只断言 `PageTransition` 的类型和 `Duration`，不断言中间帧 |
| 每批回填「实测结论」，交付表注明拆分 | Task 5 Step 2–3 |

**2. 占位符**：没有 TBD/TODO。Task 5 回填段里"执行时与计划不符"一条是执行时必须写的记录，Step 3 明确要求替换掉，不能原样提交。

**3. 名称一致性**：`PageHelpers` 的 4 个成员在 Task 1 定义，Task 3、4 按同样签名使用。`Open<TPage>` 返回 `(Window, TPage)`，Task 4 的 `page.Editor` 依赖 `TPage = WindowHowToPage` 的强类型。Task 1–4 的代码块就是探针 `final\` 里实跑过的文件（81/81，缺陷测试修复前红），测试数 9 / 21 + 8 / 21 / 22 与各任务的 Expected 一致。

**4. Review Focus**：5 条各有测试。
- 第 1 条：Task 2 `Opening_the_page_reads_without_writing`，Task 4 换 `DataContext`，Task 2、5 检查 AppData。
- 第 2 条：Task 4 `Leaving_the_page_closes_every_window_it_opened`，有破坏检查。
- 第 3 条：Task 4 缺陷测试，先红后绿。
- 第 4 条：Task 1 `Navigation_titles_are_unique_and_prefixed_by_kind`，有破坏检查。
- 第 5 条：Task 4 `Ctrl_s_saves_only_once_focus_is_inside_the_page`，前后两半都断言。
