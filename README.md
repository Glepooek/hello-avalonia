# hello-avalonia

Avalonia UI 学习示例集合。每个项目演示一个独立主题，可单独打开运行。

## 项目

| 项目 | 演示内容 |
|---|---|
| [Avalonia.MusicStore](Avalonia.MusicStore) | 专辑搜索（iTunes API）、购买、本地缓存、RESX 多语言 |
| [Avalonia.WebViewDemo](Avalonia.WebViewDemo) | NativeWebView 嵌入控件、NativeWebDialog 原生窗口、JS ↔ C# 双向调用 |
| [Avalonia.HtmlRendererDemo](Avalonia.HtmlRendererDemo) | HtmlPanel 富文本渲染、IconFont 与 PathIcon 图标 |
| [Avalonia.DataTemplateDemo](Avalonia.DataTemplateDemo) | IDataTemplate 模版选择器、Flyout、ControlTheme 样式 |
| [Avalonia.Shared](Avalonia.Shared) | 共享类库：窗口 Helper、Win32 互操作、消息载体、ViewModel 基类 |

## 环境

- .NET 10.0 SDK
- Avalonia 12.1.2
- Windows 上运行 WebViewDemo 需要 WebView2 Runtime

## 构建与运行

```bash
dotnet build hello-avalonia.slnx
dotnet run --project Avalonia.WebViewDemo
```

## 约定

- 包版本统一在 `Directory.Packages.props` 声明，项目引用不带 `Version`
- 新增代码注释用英文；从 hello-dotnet 迁移来的既有中文注释保持原样
- 设计文档位于 `docs/superpowers/specs/`
