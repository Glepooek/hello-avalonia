# Copilot 仓库指令

本仓库是 Avalonia 演示集合（.NET 10，Avalonia 12.1.2）。完整的仓库规范见根目录 `AGENTS.md` 与 `CLAUDE.md`，以下为必须遵守的要点。

## 项目结构

- `src/`：按主题拆分的演示应用（`Avalonia.<X>Demo`）与共享库 `Avalonia.Shared`。
- `samples/`：较完整的应用（目前为 `MusicStore`）。
- `tests/`：`Avalonia.<X>Demo.Tests`（Headless）、`Avalonia.<X>Demo.AppiumTests`（Appium），共用辅助放在 `Avalonia.Tests.Common` / `Avalonia.Appium.Common`，不要复制到单个测试项目。
- `benchmarks/Avalonia.Benchmarks`：BenchmarkDotNet 基准。
- 新项目须登记到 `hello-avalonia.slnx`；包版本只写在 `Directory.Packages.props`，csproj 的 `PackageReference` 不写 `Version`。

## 常用命令

- `dotnet build hello-avalonia.slnx`
- `dotnet run --project src/Avalonia.<X>Demo`
- `dotnet test hello-avalonia.slnx`（Appium 测试默认跳过）
- `dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*<Name>*'`

## 编码约定

- C# 与 AXAML 使用四个空格缩进；类型与公共成员用 PascalCase，局部变量与参数用 camelCase，私有字段用 `_camelCase`。
- MVVM 使用 CommunityToolkit.Mvvm（`[ObservableProperty]`、`[RelayCommand]`）。
- 每个页面只演示一个主题；演示项目一般为 `App.axaml` + `Program.cs` + `Views/` + `ViewModels/`。`StarterTutorialDemo` 使用 code-behind，不要为它补 ViewModel。
- 新写的代码注释与 AXAML 注释用英文；界面文字（标签页标题、说明、按钮）用中文。从 hello-dotnet 迁移来的中文注释保持原样。
- 仓库未配置 linter，修改文件时保持其现有格式。

## Avalonia 注意事项

- 元素上写属性的默认值会永久压制该属性的样式，且没有任何日志。
- 绑定到结构体属性（如 `Thickness`、`CornerRadius`）失败时是静默的，需在运行时断言。
- 以 `{0}` 开头的 `StringFormat` 要加 `{}` 前缀转义。
- 包 `Avalonia.Diagnostics` 固定在 11.3.22（v12 的 DevTools 已内置于主包）；行为库使用 `Xaml.Behaviors.Avalonia`，不要换回旧的 `Avalonia.Xaml.Behaviors`。

## 测试

- 框架为 xUnit v3。优先使用纯单元测试或 Avalonia Headless（`[AvaloniaFact]`）；只有需要真实进程、原生输入、对话框或无障碍时才用 Appium。
- 修改测试前阅读 `docs/2026-10-18-testing-common-standards.md` 与 Avalonia 测试规范；路径级规则见 `.github/instructions/`。

## 提交与 PR

- 提交信息使用英文，遵循 Conventional Commits（`feat:`、`fix:`、`test:`、`docs:`）。
- 每个提交聚焦单一改动。PR 需说明改动内容、列出验证命令，并注明仅在 Windows 上运行的 Appium 验证以及未运行的测试。
