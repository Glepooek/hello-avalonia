# Avalonia AppDevelopmentDemo 文档覆盖补充设计

日期：2026-10-25

状态：已实施并验证

## 背景

`Avalonia.AppDevelopmentDemo` 用于对应 Avalonia 官方文档的 `app-development` 节点。现有项目已覆盖依赖注入、本地化、Web 内容、日志、未处理异常、资源、数据校验、线程、窗口管理和无障碍，但主窗口只有 10 个标签页，尚未覆盖当前官方目录中的全部 14 个顶层主题。

此前已经把依赖注入、日志和未处理异常等跨页面能力移到应用组合根、启动入口与生命周期中。此次补充继续遵守同一原则：Page 负责说明或触发局部演示，不在 Page 中创建 DI 容器、替换全局日志接收器或订阅应用级异常事件。

## 目标

1. 让主窗口按官方 `app-development` 目录顺序覆盖全部 14 个顶层主题。
2. 补充“跨平台解决方案结构、XAML 预览与设计时设置、性能优化、原生互操作”四个缺失主题。
3. 对已有页面中最有教学价值、可以稳定验证的缺口做定向补充。
4. 保持跨平台构建，不引入只在 Windows 上可编译或运行的实现。
5. 使用 L0 单元测试和 L1 Avalonia Headless 测试验证新增行为。

## 非目标

- 不把官方文档的每个小节逐字搬进 UI。
- 不为桌面、Android、iOS 和浏览器新增平台启动项目；跨平台结构页只说明推荐结构。
- 不在本项目重复实现已有独立项目承载的完整 WebView 和数据校验示例。
- 不增加真实 P/Invoke、`NativeControlHost` 或第三方原生库依赖。
- 不把性能页面做成微基准；性能数据应由 BenchmarkDotNet 或专业分析器测量。
- 不新增 Appium 测试。

## 官方主题覆盖矩阵

| 官方主题 | 当前状态 | 本次处理 |
|---|---|---|
| Cross-platform solution setup | 缺失 | 新增说明页，展示 Core 与各平台启动项目的职责边界 |
| Dependency injection | 已覆盖 | 保留组合根、构造函数注入与容器释放说明 |
| Localizing | 部分覆盖 | 增加当前文化、文化格式化和 RTL 预览 |
| Embedding web content | 指路页 | 保留对独立 WebView 项目的引用 |
| Logging errors and warnings | 已覆盖 | 保留应用日志与 Avalonia 框架日志的边界说明 |
| Handling unhandled exceptions | 部分覆盖 | 在 `App` 增加 `UnhandledExceptionFilter` 生命周期接入并更新说明 |
| Resources | 部分覆盖 | 增加合并字典和主题变体资源示例 |
| XAML preview/design settings | 缺失 | 新增设计时尺寸、`Design.DataContext` 和设计数据示例 |
| Data validation | 指路页 | 保留对数据绑定项目“校验”标签页的引用 |
| Threading | 部分覆盖 | 增加 `InvokeAsync` 和调度优先级示例 |
| Window management | 已覆盖 | 保留窗口、对话框、关闭拦截和屏幕信息示例 |
| Performance | 缺失 | 新增虚拟化、编译绑定和优化检查清单 |
| Accessibility | 已覆盖 | 保留自动化属性与 AutomationPeer 示例 |
| Native interop | 缺失 | 新增原生窗口句柄读取与平台边界说明 |

## 总体结构

`MainWindow.axaml` 继续仅承担标签页导航。新增页面位于 `Views/Pages`，新增页面专用数据放在 `ViewModels`。应用级能力仍由 `Program`、`App`、`ApplicationServices` 和 `Services` 承担。

新增标签页按官方顺序插入：

1. 跨平台结构
2. 依赖注入
3. 本地化
4. Web 内容
5. 日志
6. 未处理异常
7. 资源
8. XAML 设计时
9. 数据校验
10. 线程模型
11. 窗口管理
12. 性能
13. 无障碍
14. 原生互操作

## 新增主题设计

### 跨平台解决方案结构

新增 `CrossPlatformSetupPage`。页面以只读结构图说明共享 Core 项目与 Desktop、Android、iOS、Browser 启动项目的关系，并明确平台项目只负责启动、权限、清单、原生资产和平台服务注册。

该页不创建空壳平台项目，避免给现有桌面演示增加不必要的工作负载与构建依赖。页面同时指出当前仓库的演示项目是桌面入口，而共享 UI 和服务应保持可被其他平台入口复用。

### XAML 预览与设计时设置

新增 `XamlDesignTimePage` 和一个小型设计数据模型。页面本身使用 `d:DesignWidth`、`d:DesignHeight`、`x:DataType` 与 `Design.DataContext`，使源文件既是说明，也是可被 IDE 预览器读取的真实示例。

运行时使用独立的正常数据，避免把设计数据泄漏到产品逻辑。页面说明 `DataContext` 与 `Design.DataContext` 的差别，以及代码中使用 `Design.IsDesignMode` 的适用边界。

### 性能优化

新增 `PerformancePage` 和 `PerformancePageViewModel`。页面使用固定数量的大列表展示 `ListBox` 虚拟化，并对列表项使用编译绑定。页面显示总数据量和当前已实现的容器数量，用可观察事实说明“数据项数量不等于同时创建的控件数量”。

页面还提供只读检查清单，覆盖浅层布局、减少无效绑定、批量集合更新、避免阻塞 UI 线程和使用 DevTools/分析器。它不显示毫秒级比较，不把单次运行结果包装成基准结论。

### 原生平台互操作

新增 `NativeInteropPage`。页面加载到真实 `TopLevel` 后，通过 `TryGetPlatformHandle` 读取句柄描述和值并展示；Headless 环境允许返回“不可用”，这是受支持的结果。

页面只读取句柄，不调用平台 API。P/Invoke、`NativeControlHost`、原生库加载和平台服务 DI 以代码结构说明呈现，并强调平台判断与原生调用应封装在服务层，而不是散落在 View 中。

## 现有主题的定向增强

### 本地化

保留 RESX 资源读取，增加阿拉伯语资源用于 RTL 演示。切换语言时同步更新当前文化和当前 UI 文化，展示本地化字符串、数字/日期格式以及由 `TextInfo.IsRightToLeft` 决定的预览区域方向。

文化修改仅发生在显式语言切换流程中。测试必须保存并恢复原文化，避免跨测试污染。

### 未处理异常

`App.OnFrameworkInitializationCompleted` 同时订阅 `Dispatcher.UIThread.UnhandledExceptionFilter` 与 `UnhandledException`，退出时对称解除订阅。Filter 只记录进入过滤阶段的信息，不把未知异常强制标记为可继续；最终处理器仍遵循“记录不等于安全恢复”的原则。

`ApplicationExceptionReporter` 增加对应的过滤阶段记录方法，便于 L0 测试验证，而页面仍为只读说明。

### 资源

新增独立资源字典并通过 `MergeResourceInclude` 合并，增加 Light/Dark 主题变体资源。现有一次性查找与 `DynamicResource` 示例保留，并增加主题资源色块及主题切换操作。

主题切换只修改页面或所属 TopLevel 的请求主题，不改变应用启动配置文件。

### 线程模型

在现有错误访问、`Post` 和 `await` 示例之外增加 `InvokeAsync`。示例从后台线程请求 UI 线程计算结果并等待返回，同时展示 `DispatcherPriority.Background` 的用途。

不使用 `Thread.Sleep` 阻塞 UI 线程；异步事件处理器捕获并把预期错误写入页面结果区。

## 错误处理与跨平台约束

- 原生句柄在 Headless、尚未附加平台窗口或不支持的平台上可以为空，页面显示状态而不是抛出异常。
- RTL 和文化格式化使用 BCL `CultureInfo`，不依赖操作系统语言包才能构建。
- 性能页只断言结构和虚拟化特征，不断言机器相关耗时。
- 异常过滤器不改变未知异常的安全策略。
- 新增代码与 AXAML 注释使用英文，用户可见文字使用中文。

## 测试设计

### L0

- 设计/性能 ViewModel 提供稳定的示例数据。
- `ApplicationExceptionReporter` 能记录异常过滤阶段。
- 文化格式化辅助逻辑对 LTR/RTL 文化返回正确结果，并恢复测试文化。

### L1 Avalonia Headless

- 主窗口可以依次加载 14 个标签页且没有绑定警告。
- XAML 设计时页在运行时使用正常数据。
- 性能页数据量正确，已实现容器数量小于总数据量。
- 原生互操作页在无句柄时显示受支持状态。
- 本地化切换更新字符串、格式化文本和方向。
- 合并字典、主题资源与动态资源按预期更新。
- `InvokeAsync` 示例返回 UI 线程结果。

### 验证命令

```powershell
dotnet test tests/Avalonia.AppDevelopmentDemo.Tests
dotnet build hello-avalonia.slnx
```

## 预期文件范围

新增：

- `Views/Pages/CrossPlatformSetupPage.axaml(.cs)`
- `Views/Pages/XamlDesignTimePage.axaml(.cs)`
- `Views/Pages/PerformancePage.axaml(.cs)`
- `Views/Pages/NativeInteropPage.axaml(.cs)`
- `ViewModels/DesignTimeSampleViewModel.cs`
- `ViewModels/PerformancePageViewModel.cs`
- `Assets/Langs/Strings.ar-SA.resx`
- `Themes/AppDevelopmentResources.axaml`

修改：

- `Views/MainWindow.axaml`
- `Views/Pages/LocalizationPage.axaml(.cs)`
- `Views/Pages/UnhandledExceptionsPage.axaml`
- `Views/Pages/ResourcesPage.axaml(.cs)`
- `Views/Pages/ThreadingPage.axaml(.cs)`
- `App.axaml.cs`
- `Services/ApplicationExceptionReporter.cs`
- `tests/Avalonia.AppDevelopmentDemo.Tests/SmokeTests.cs`
- `tests/Avalonia.AppDevelopmentDemo.Tests/InteractionTests.cs`
- `tests/Avalonia.AppDevelopmentDemo.Tests/ServiceTests.cs`

## 完成标准

- 14 个官方顶层主题均有对应标签页。
- 应用级功能位于启动、生命周期或服务层，Page 不承担全局基础设施职责。
- 新增示例在 Windows 与 Avalonia Headless 下可加载；实现不包含 Windows 专用编译依赖。
- AppDevelopmentDemo 测试全部通过，完整解决方案构建通过。

## 实施验证

2026-10-25 完成实现并执行以下验证：

- `dotnet test tests/Avalonia.AppDevelopmentDemo.Tests --no-restore --nologo --verbosity minimal`：25 个测试通过，0 失败，0 跳过。
- `dotnet build hello-avalonia.slnx --no-restore --nologo --verbosity minimal`：构建成功，0 错误；保留 1 个既有的 `Avalonia.MusicStore` `NU1701` 警告。
- 主窗口包含 14 个官方顶层主题标签；四个新增页面均使用对应的 `app-development` 文档路径。

验证期间曾有两个重叠的解决方案构建进程争用 `Avalonia.TestingDemo.Tests.dll`，导致一次 `CS2012` 文件占用错误。终止本次会话遗留的构建进程后，单独重跑构建成功；该错误不是源代码或项目配置失败。
