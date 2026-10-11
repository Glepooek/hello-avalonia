# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 仓库是什么

Avalonia 12.1.2 / .NET 10 的学习型演示集合：每个 `Avalonia.*Demo` 目录是一个**独立可运行的 WinExe 项目**，按 Avalonia 官方文档的分类组织（Fundamentals、Layout、Styling、DataBinding 等）。全部项目收在 `hello-avalonia.slnx`。

## 常用命令

```bash
dotnet build hello-avalonia.slnx                      # 构建全部
dotnet run --project Avalonia.LayoutDemo              # 运行单个演示（换成任意 Avalonia.*Demo）
dotnet test Avalonia.TestingDemo.Tests                # 唯一的测试项目（xUnit v3 + Avalonia.Headless）
dotnet test Avalonia.TestingDemo.Tests --filter "FullyQualifiedName~ViewModelTests"   # 跑单个类/用例
```

没有 lint 配置；`TreatWarningsAsErrors=false`。

## 架构要点（需要跨文件才能看出来的部分）

- **集中管理包版本**：版本只写在根目录 `Directory.Packages.props`（central package management），csproj 里的 `PackageReference` **不带 `Version`**。新增包时先在那里登记。
- **`Directory.Build.props`** 统一输出路径为 `bin\Debug|Release`，并设 `AppendTargetFrameworkToOutputPath=false`（输出目录里没有 `net10.0` 这一层）。
- **`Avalonia.Shared`** 是所有演示共用的库：`DemoHeader` 控件、`SharedStyles.axaml`、`ViewModelBase`、`EventLog`、`DoubleToThicknessConverter` 等。演示项目通过 `ProjectReference` 引用它；通用的东西放这里，不要在各项目里复制。
- 各演示项目结构一致：`App.axaml` + `Program.cs` + `Views/MainWindow` + `Views/Pages/*`（每个知识点一页）+ `ViewModels/`。默认开启编译绑定（`AvaloniaUseCompiledBindingsByDefault=true`）。
- MVVM 一律用 CommunityToolkit.Mvvm（`[ObservableProperty]`、`[RelayCommand]`）。
- `Avalonia.MusicStore`、`Avalonia.WebViewDemo`、`Avalonia.HtmlRendererDemo` 是较完整的示例应用；`Avalonia.TestingDemo` 与 `.Tests` 演示 Headless 测试（控件查询、交互、渲染快照）。

## 包版本的特殊约定（见 `Directory.Packages.props` 注释）

- `Avalonia.Diagnostics` 固定在 11.3.22：v12 的 DevTools 已内置在主包里。
- 行为库用 `Xaml.Behaviors.Avalonia`（12.0.7）；旧的 `Avalonia.Xaml.Behaviors` 与 v12 二进制不兼容，不要换回去。

## 代码约定（来自 README）

- 新写的代码 / XAML 注释用**英文**；界面文字（标签页标题、说明栏、按钮文案）用**中文**；之前迁移过来的中文注释保持原样，不要顺手改。
- 设计文档放 `docs/superpowers/specs/`，实现计划放 `docs/superpowers/plans/`。

## Avalonia 坑（已在本仓库反复踩过）

- 在元素上写属性的**默认值**（本地值）会永久压制该属性的所有样式，且没有任何日志。
- 绑定到结构体属性失败时**静默**，构建与冒烟测试都检不出，需要运行时断言。
- 带 `{0}` 开头的 `StringFormat` 要加 `{}` 前缀转义。

## ControlsDemo（已完成，待目视验证与合并）

`docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md` 与 `docs/superpowers/plans/2026-10-10-controls-demo-0*.md` 描述了一个按官方 Controls 分类整理的单项目 `Avalonia.ControlsDemo`（SplitView + TreeView 导航）。第 00–07 份计划已全部执行并提交（分支 `feat/controls-demo`），目录共 119 页：80 个可运行控件页 + 19 个实战页 + 20 个路标页（Web、付费控件、How-to 路标）。计划里的代码块用 `#### \`路径\`` 标题标注目标文件，可整块抽取。

- 验证靠仓库外的 headless 探针（`C:\Temp\probe-controls`，不入库）；每批实测结论回填到 spec 的"批 N 实测结论"小节。
- `DataGrid`、`ColorPicker` 是独立包，必须在 `App.axaml` 里 `StyleInclude` 才会渲染，否则静默空白。

## BehaviorsDemo（Xaml.Behaviors.Avalonia）

`docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md` 与 `docs/superpowers/plans/2026-10-11-behaviors-demo-0*.md` 描述了单项目 `Avalonia.BehaviorsDemo`（左侧 TabControl 导航，共 15 页，按概念层次组织）。计划里的代码块用 `#### \`路径\`` 标题标注目标文件，可整块抽取；各页的"实测结论"回填在 spec 的「批 N 实测结论」小节。

- 验证靠仓库外的 headless 探针（`C:\Temp\probe-bdemo`，不入库）。计时器类行为（`DebounceAction`、`TimerTrigger`）在 headless 里要跑 `Dispatcher.UIThread.MainLoop` 才会走。
- 副作用动作都限制在安全范围：文件只动 `Path.GetTempPath()` 下的 `avalonia-behaviors-demo`，网络只连本进程的 127.0.0.1 回环服务，剪贴板与截图页有醒目提示。
- 行为库的坑：继承普通 `Trigger`（如 `FileSystemWatcherTrigger`、`NetworkInformationTrigger`，以及自己写的 `Trigger<T>`）的触发器不会把 DataContext 传给里面的 Action，`InvokeCommandAction` 的 `{Binding}` 命令不执行且无日志，自写触发器应继承 `StyledElementTrigger<T>`；`Animation` 不能直接写在行为里（放进 `Style.Resources` 再 `DynamicResource` 引用）；`ListReorderDragBehavior` 必须经 `ListBoxItem` 样式挂载。
