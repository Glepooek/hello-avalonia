# hello-avalonia

Avalonia UI 学习示例集合。每个项目演示一个独立主题，可单独打开运行。

## 项目

| 项目 | 演示内容 |
|---|---|
| [Avalonia.FundamentalsDemo](Avalonia.FundamentalsDemo) | 纯代码 UI、code-behind 与 MVVM 对照、TopLevel、视觉树与逻辑树、应用生命周期 |
| [Avalonia.XamlDemo](Avalonia.XamlDemo) | 命名空间、x: 指令、标记扩展、类型转换器、泛型、XAML 编译 |
| [Avalonia.LayoutDemo](Avalonia.LayoutDemo) | 8 种布局面板对照、对齐与 Margin/Padding、四种响应式手段 |
| [Avalonia.StylingDemo](Avalonia.StylingDemo) | 选择器语法、样式类、伪类、ControlTheme、主题变体、嵌入字体、样式共享 |
| [Avalonia.DataBindingDemo](Avalonia.DataBindingDemo) | 绑定语法与模式、编译绑定、集合与主从、多值绑定、命令、转换器、校验、集合视图、异步绑定、绑定调试 |
| [Avalonia.DataTemplatesDemo](Avalonia.DataTemplatesDemo) | 内联模板、按类型匹配、模板选择器、代码建模板、复用、ViewLocator、面板与树模板 |
| [Avalonia.PropertySystemDemo](Avalonia.PropertySystemDemo) | StyledProperty / DirectProperty / 附加属性、值优先级、元数据与回调 |
| [Avalonia.EventsDemo](Avalonia.EventsDemo) | 生命周期事件、输入事件、路由事件的隧道/冒泡/直接、Handled 与 handledEventsToo、自定义路由事件 |
| [Avalonia.InputDemo](Avalonia.InputDemo) | 指针、焦点与导航、手势、键盘与 HotKey、事件/命令/手势三种交互写法、拖放、文本输入过滤 |
| [Avalonia.GraphicsDemo](Avalonia.GraphicsDemo) | 画刷与渐变、变换、形状与几何、自定义绘制、特效、裁剪与命中、图标、渲染选项、关键帧动画、控件过渡、页面过渡、缓动函数、合成动画 |
| [Avalonia.CustomControlsDemo](Avalonia.CustomControlsDemo) | UserControl、TemplatedControl 与 ControlTheme、自绘控件、控件树、自定义 Panel、自定义 Flyout |
| [Avalonia.MusicStore](Avalonia.MusicStore) | 专辑搜索（iTunes API）、购买、本地缓存、RESX 多语言 |
| [Avalonia.WebViewDemo](Avalonia.WebViewDemo) | NativeWebView 嵌入控件、NativeWebDialog 原生窗口、JS ↔ C# 双向调用 |
| [Avalonia.HtmlRendererDemo](Avalonia.HtmlRendererDemo) | HtmlPanel 富文本渲染、IconFont 与 PathIcon 图标 |
| [Avalonia.Shared](Avalonia.Shared) | 共享类库：演示页说明条控件、窗口 Helper、Win32 互操作、消息载体、ViewModel 基类 |

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
- 新增代码与 XAML 注释用英文，界面文字（Tab 标题、说明条、按钮文案）用中文；从 hello-dotnet 迁移来的既有中文注释保持原样
- 设计文档位于 `docs/superpowers/specs/`
