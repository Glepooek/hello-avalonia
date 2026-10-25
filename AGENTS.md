# 仓库指南

## 项目结构与模块组织

`src/` 存放按主题拆分的 Avalonia 演示应用和共享库 `Avalonia.Shared`。较完整的应用（目前为 `Avalonia.MusicStore`）放在 `samples/`。自动化测试位于 `tests/`：`Avalonia.<App>.Tests` 使用 Avalonia Headless，`Avalonia.<App>.AppiumTests` 驱动打包后的桌面界面。共享测试辅助代码应放入 `Avalonia.Tests.Common` 或 `Avalonia.Appium.Common`，不要复制到单个测试项目。性能基准位于 `benchmarks/Avalonia.Benchmarks`，设计记录和测试规范位于 `docs/`。新增项目时须登记到 `hello-avalonia.slnx`，包版本统一维护在 `Directory.Packages.props`。

## 构建、测试与开发命令

- `dotnet build hello-avalonia.slnx`：构建完整的 .NET 10 解决方案。
- `dotnet run --project src/Avalonia.LayoutDemo`：运行一个演示；可按需替换项目路径。
- `dotnet test hello-avalonia.slnx`：运行全部 xUnit v3 测试；未显式启用时会跳过 Appium 测试。
- `dotnet test tests/Avalonia.LayoutDemo.Tests`：运行单个 Headless 测试项目。
- `dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*Celsius*'`：运行选定的 BenchmarkDotNet 基准。

Windows Appium 测试需要 WinAppDriver，并须设置 `$env:HELLO_AVALONIA_APPIUM = "1"`；测试期间会接管鼠标和键盘。

## 编码风格与命名约定

C# 和 AXAML 均使用四个空格缩进。遵循标准 C# 命名方式：类型、公共成员和测试方法使用 PascalCase，局部变量和参数使用 camelCase，私有字段使用 `_camelCase`。每个页面只演示一个主题。MVVM 代码使用 CommunityToolkit.Mvvm 的 `[ObservableProperty]`、`[RelayCommand]` 等特性。新增的代码与 AXAML 注释使用英文，面向用户的界面文字使用中文。仓库未配置 linter；修改文件时应保持其现有格式。

## 测试指南

测试框架使用 xUnit v3。优先编写快速的 ViewModel 或 Avalonia Headless 测试；只有需要真实进程、原生输入、对话框或无障碍功能时才使用 Appium。测试类按被测主题命名，例如 `ViewModelTests`、`InteractionTests`；测试方法名应清晰描述行为。修改测试前，先阅读 `docs/2026-10-18-testing-common-standards.md` 和 Avalonia 专用测试规范。

## 提交与拉取请求指南

提交主题使用英文，并遵循 Conventional Commits 风格，例如 `feat: add ...`、`fix: ...`、`test: ...` 或 `docs: ...`。每个提交应聚焦单一改动。拉取请求应说明改动内容、列出验证命令、关联相关 Issue 或设计文档；涉及可见界面变化时还应附上截图。请明确注明仅在 Windows 上执行的 Appium 验证，以及任何有意未运行的测试。
