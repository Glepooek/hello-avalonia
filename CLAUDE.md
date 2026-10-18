# CLAUDE.md

每个 `Avalonia.*Demo` 是独立可运行的 WinExe 项目，统一收在 `hello-avalonia.slnx`；项目清单见 `README.md`。

## 命令

```bash
dotnet build hello-avalonia.slnx
dotnet run --project src/Avalonia.LayoutDemo     # 换成任意 Avalonia.*Demo
dotnet test hello-avalonia.slnx                  # 全部测试项目（xUnit v3 + Headless），每个应用一个 Avalonia.<应用>.Tests
dotnet test tests/Avalonia.TestingDemo.Tests     # 只跑单个测试项目，换成任意 Avalonia.<应用>.Tests
dotnet test tests/Avalonia.TestingDemo.Tests --filter "FullyQualifiedName~ViewModelTests"
$env:HELLO_AVALONIA_APPIUM=1; dotnet test tests/Avalonia.TestingDemo.AppiumTests   # PowerShell；每个应用一个 Avalonia.<应用>.AppiumTests（MusicStore 无 Demo 后缀）；需先运行 WinAppDriver，会接管鼠标键盘
```

无 lint 配置；`TreatWarningsAsErrors=false`。

## 项目结构

- 目录约定：演示项目与 `Avalonia.Shared` 放 `src/`，测试项目放 `tests/`；`Directory.*.props` 与 `.slnx` 留在仓库根。新项目按此归位，同时登记到 `hello-avalonia.slnx` 的对应 `<Folder>`。
- 典型演示：`App.axaml` + `Program.cs` + `Views/MainWindow` + `Views/Pages/*`（每个知识点一页）+ `ViewModels/`；默认开启编译绑定。
- 例外：`MusicStore`、`WebViewDemo`、`HtmlRendererDemo` 是较完整的应用；`StarterTutorialDemo` 照官方教程用 code-behind，无 `ViewModels/`，别补 ViewModel。
- `Avalonia.Shared` 是共用库（`DemoHeader`、`SharedStyles.axaml`、`ViewModelBase`、`EventLog` 等），通用代码放这里，不要在各项目复制。
- `tests/Avalonia.Tests.Common` 是测试共用库（`HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`）：各应用的测试项目引用它，不要在测试项目里复制辅助代码；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。
- `tests/Avalonia.Appium.Common` 是 Appium 共用库（`AppSession`、`AppiumFactAttribute`、`AppPaths`、`UnicodeKeyboard`、`KeyChord`）；键入、按键、子窗口、并行的用法规则见 `.claude/rules/testing.md` §3。
- 写或改测试前先读 `.claude/rules/testing.md`，写或改基准前先读 `.claude/rules/benchmarks.md`：两者按路径加载，只在碰到 `tests/**`、`benchmarks/**` 下的文件时才自动进上下文。
- MVVM 一律用 CommunityToolkit.Mvvm（`[ObservableProperty]`、`[RelayCommand]`）。
- `Directory.Build.props` 统一输出到 `bin\Debug|Release`，且 `AppendTargetFrameworkToOutputPath=false`（路径里没有 `net10.0`）。

## 包版本

- 版本只写在 `Directory.Packages.props`（中央包管理）；csproj 的 `PackageReference` 不带 `Version`，新增包先在那里登记。
- `Avalonia.Diagnostics` 固定 11.3.22：v12 的 DevTools 已内置于主包。
- 行为库用 `Xaml.Behaviors.Avalonia`（12.0.7）；旧的 `Avalonia.Xaml.Behaviors` 与 v12 二进制不兼容，不要换回去。

## 编码约定

- 新写的注释（代码与 XAML）用英文；界面文字（标签页标题、说明栏、按钮文案）用中文；从 hello-dotnet 迁移来的中文注释保持原样，不要顺手改。
- 提交信息用英文。

## 计划驱动的实现

大改动按「spec → 分批 plan → 执行」推进：设计文档在 `docs/superpowers/specs/`，计划在 `docs/superpowers/plans/`。计划里的代码块以 `#### \`相对路径\`` 标注目标文件（相对仓库根，新计划须带 `src/` 或 `tests/` 前缀；`**\`.claude/temp/…\`**` 是探针文件），可整块抽取。

- 抽取：按 `#### \`路径\`` 标题把代码块写到对应路径的脚本，放在 `.claude/temp/` 下。
- 验证：用 headless 探针，放在 `.claude/temp/<批次>/`（如 `.claude/temp/probe-b11/`）；每批实测结论回填到 spec 的「批 N 实测结论」。
- 探针文件永不提交：`.claude/temp/` 已在 `.gitignore` 里，提交前仍用 `git status` 确认。
- 探针在仓库目录树内，会继承根目录的 `Directory.Build.props` 与 `Directory.Packages.props`。默认保持继承：探针与正式测试用同一套版本，结论搬过去最可靠；探针 csproj 的 `PackageReference` 不写 `Version`。
- 只有要试未登记的包或别的版本时才隔离，优先只在那个探针的 csproj 里设 `ManagePackageVersionsCentrally=false`（影响最小）。若整个 `.claude/temp/` 都要隔离，可在其中放空的 `Directory.Build.props`（`<Project />`，截断向上查找）与只设 `ManagePackageVersionsCentrally=false` 的 `Directory.Packages.props`，并注意：
  - 只隔离探针自己，被引用的 `src` 项目仍按仓库配置构建；共用的包照抄 `Directory.Packages.props` 的版本，写低了按 NuGet 规则会报 NU1605（本仓库未实测）。
  - 删掉这两个文件后探针重新继承仓库配置，写了 `Version` 的探针会报 NU1008（同样未实测）；只在该批探针不再用时删。
  - 它们在 gitignore 里不随仓库走，需要时由探针脚本在批次开始时创建。
- 引用演示项目直接写相对路径的 `ProjectReference`（csproj 在 `.claude/temp/probe-b11/` 时是 `../../../src/Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj`），不要建指向 `src` 的目录联接。
- 2026-10-17 之前的探针在 `C:\Temp\probe-*`，spec 与计划里的这类路径是历史记录，不要改。

## Avalonia 坑（本仓库反复踩过）

- 元素上写属性的**默认值**（本地值）会永久压制该属性的所有样式，且无任何日志。
- 绑定到结构体属性失败时**静默**，构建与冒烟测试都检不出，需运行时断言。
- 以 `{0}` 开头的 `StringFormat` 要加 `{}` 前缀转义。
