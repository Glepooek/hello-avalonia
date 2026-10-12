# CLAUDE.md

每个 `Avalonia.*Demo` 是独立可运行的 WinExe 项目，统一收在 `hello-avalonia.slnx`；项目清单见 `README.md`。

## 命令

```bash
dotnet build hello-avalonia.slnx
dotnet run --project src/Avalonia.LayoutDemo     # 换成任意 Avalonia.*Demo
dotnet test tests/Avalonia.TestingDemo.Tests     # 唯一的测试项目（xUnit v3 + Headless）
dotnet test tests/Avalonia.TestingDemo.Tests --filter "FullyQualifiedName~ViewModelTests"
```

无 lint 配置；`TreatWarningsAsErrors=false`。

## 项目结构

- 目录约定：演示项目与 `Avalonia.Shared` 放 `src/`，测试项目放 `tests/`；`Directory.*.props` 与 `.slnx` 留在仓库根。新项目按此归位，同时登记到 `hello-avalonia.slnx` 的对应 `<Folder>`。
- 典型演示：`App.axaml` + `Program.cs` + `Views/MainWindow` + `Views/Pages/*`（每个知识点一页）+ `ViewModels/`；默认开启编译绑定。
- 例外：`MusicStore`、`WebViewDemo`、`HtmlRendererDemo` 是较完整的应用；`StarterTutorialDemo` 照官方教程用 code-behind，无 `ViewModels/`，别补 ViewModel。
- `Avalonia.Shared` 是共用库（`DemoHeader`、`SharedStyles.axaml`、`ViewModelBase`、`EventLog` 等），通用代码放这里，不要在各项目复制。
- `tests/Avalonia.Tests.Common` 是测试共用库（`HeadlessApp`、`UiHelpers`、`TabWalker`、`LogCollector`）：各应用的测试项目引用它，不要在测试项目里复制辅助代码；`TestAppBuilder` 只写 `HeadlessApp.Build<App>()`。
- MVVM 一律用 CommunityToolkit.Mvvm（`[ObservableProperty]`、`[RelayCommand]`）。
- `Directory.Build.props` 统一输出到 `bin\Debug|Release`，且 `AppendTargetFrameworkToOutputPath=false`（路径里没有 `net10.0`）。

## 包版本

- 版本只写在 `Directory.Packages.props`（中央包管理）；csproj 的 `PackageReference` 不带 `Version`，新增包先在那里登记。
- `Avalonia.Diagnostics` 固定 11.3.22：v12 的 DevTools 已内置于主包。
- 行为库用 `Xaml.Behaviors.Avalonia`（12.0.7）；旧的 `Avalonia.Xaml.Behaviors` 与 v12 二进制不兼容，不要换回去。

## 编码约定

- 新写的注释（代码与 XAML）用英文；界面文字（标签页标题、说明栏、按钮文案）用中文；迁移来的中文注释保持原样，不要顺手改。
- 提交信息用英文。

## 计划驱动的实现

大改动按「spec → 分批 plan → 执行」推进：设计文档在 `docs/superpowers/specs/`，计划在 `docs/superpowers/plans/`。计划里的代码块以 `#### \`相对路径\`` 标注目标文件（相对仓库根，新计划须带 `src/` 或 `tests/` 前缀；`**\`C:\绝对路径\`**` 是仓库外的探针文件），可整块抽取。

- 抽取：`powershell -File C:\Temp\extract-plan.ps1 -Plan <计划> -RepoRoot <仓库根>`
- 验证：用仓库外的 headless 探针（`C:\Temp\probe-controls`、`C:\Temp\probe-bdemo`）；每批实测结论回填到 spec 的「批 N 实测结论」。
- 探针文件永不提交。

## Avalonia 坑（本仓库反复踩过）

- 元素上写属性的**默认值**（本地值）会永久压制该属性的所有样式，且无任何日志。
- 绑定到结构体属性失败时**静默**，构建与冒烟测试都检不出，需运行时断言。
- 以 `{0}` 开头的 `StringFormat` 要加 `{}` 前缀转义。
