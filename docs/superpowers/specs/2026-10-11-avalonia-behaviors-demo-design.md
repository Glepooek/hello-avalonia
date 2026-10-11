# Avalonia.BehaviorsDemo 设计：Xaml.Behaviors.Avalonia 演示项目

> 状态：设计稿（2026-10-11），待用户审阅。实现计划见后续 `docs/superpowers/plans/2026-10-11-behaviors-demo-*.md`。

## 目标

新增一个独立可运行的演示项目 `Avalonia.BehaviorsDemo`，用 15 个页面讲清 `Xaml.Behaviors.Avalonia`（12.0.7）：先讲 `Behavior` / `Trigger` / `Action` 三件套的机制，再按概念层次演示常用内置类型，最后教读者自己写。

## 范围与非目标

**范围**：机制、事件与数据触发、焦点键盘指针、表单验证、集合、窗口与通知、拖拽、响应式、动画、文件系统、网络、剪贴板、截图，以及自定义 Behavior / Trigger / Action。

**非目标**：
- 不穷举全部内置类型。包是元包，依赖 8 个子包，公开类型约 530 个（`Interactions.Custom` 一个子包就有 335 个），每页只挑代表。
- 不引入新包：版本已在 `Directory.Packages.props` 登记（`Xaml.Behaviors.Avalonia` 12.0.7），项目只加一行不带版本的 `PackageReference`。
- 不修改任何现有项目；`Avalonia.Shared` 只在确有通用价值时才加东西，本项目自己的行为类放 `Avalonia.BehaviorsDemo/Behaviors/`。

## 已实测的事实（编写 spec 时在仓库外的临时项目里核实）

| 事项 | 结果 | 对设计的影响 |
|---|---|---|
| 元包依赖 | 8 个子包：Interactivity、Interactions、Interactions.Events、Interactions.Custom、Interactions.DragAndDrop、Interactions.Draggable、Interactions.Responsive、Animations；元包依赖 `Avalonia 12.0.5`，仓库用 12.1.2，可正常还原 | 只引用元包即可 |
| XAML 命名空间 | 8 个子包的全部 CLR 命名空间都用 `XmlnsDefinition` 映射进默认的 `https://github.com/avaloniaui` | 页面里**不需要任何额外 `xmlns` 前缀**，直接写 `<Interaction.Behaviors>`、`<EventTriggerBehavior>` |
| 编译绑定 | `EventTriggerBehavior` + `InvokeCommandAction`（`Command="{Binding BumpCommand}" CommandParameter="7"`）与 `DataTriggerBehavior` + `ChangePropertyAction`（`Binding`、`ComparisonCondition`、`Value`）在 `x:DataType` 下都能工作：点击后命令执行、条件满足后前景色变红 | 页面照常开启编译绑定，探针读回结果 |
| 文档来源 | 每个子包都带 XML 文档，类型与属性可直接查 | 写每页之前先查文档，查不到的类型不用 |

**类型清单（供各页取用，均已在 XML 文档里确认存在）**

- 机制（Interactivity）：`Interaction`、`Behavior`、`Behavior<T>`、`Trigger`、`Trigger<T>`、`Action`、`IAction`、`BehaviorCollection`、`ActionCollection`、`Condition`、`ConditionCollection`
- 事件与数据（Interactions.Core）：`EventTriggerBehavior`、`InvokeCommandAction`、`CallMethodAction`、`ChangePropertyAction`、`DataTriggerBehavior`、`MultiDataTriggerBehavior`、`TimerTrigger`、`TaskCompletedTrigger`、`DelayAction`、`DebounceAction`、`ThrottleAction`、`ObservableStreamBehavior`
- 剪贴板（Interactions.Core）：`SetClipboardTextAction`、`GetClipboardTextAction`、`ClearClipboardAction` 等
- 文件系统（Interactions.FileSystem）：`CreateDirectoryAction`、`WriteTextToFileAction`（`Path`、`Text`、`Append`）、`DeleteFileAction`、`DeleteDirectoryAction`（`Path`、`Recursive`）、`FileSystemWatcherTrigger`（`Path`、`Filter`、`IncludeSubdirectories`）
- 网络（Interactions.Network）：`HttpRequestAction`（`Url`、`Method`、`Content`、`ContentType`、`ResponseContent`、`ResponseStatusCode`）、`NetworkInformationTrigger`
- 焦点键盘指针（Interactions.Custom / Events）：`FocusBehavior`、`AutoFocusBehavior`、`FocusTrapBehavior`、`KeyTrigger`、`KeyGestureTrigger`、`SelectAllOnFocusBehavior`、`LoseFocusOnEnterBehavior`、`ExecuteCommandOn*` 系列、`PointerPressedEventTrigger` 等
- 验证（Interactions.Custom）：`TextBoxValidationBehavior`、`RequiredTextValidationRule`、`MinLengthValidationRule`、`RegexValidationRule`、`RangeValidationRule<T>`
- 集合（Interactions.Custom）：`AddItemToItemsControlAction`、`RemoveItemInItemsControlAction`、`ClearItemsControlAction`、`ScrollToItemBehavior`、`ListBoxSelectAllBehavior`
- 窗口、弹出、通知（Interactions.Custom）：`CloseWindowAction`、`ShowFlyoutAction`、`ShowPopupAction`、`ShowToolTipAction`、`NotificationManagerBehavior`、`ShowSuccessNotificationAction` 等
- 拖拽：`Draggable.CanvasDragBehavior`、`ListReorderDragBehavior`、`MouseDragElementBehavior`；`DragAndDrop.ContextDragBehavior`、`ContextDropBehavior`、`FilesDropBehavior`、`TextDropBehavior`
- 响应式：`AdaptiveBehavior`、`AspectRatioBehavior`
- 动画：`FadeInBehavior`（`InitialDelay`、`Duration`）、`PlayAnimationBehavior`、`BeginAnimationAction`、`StartBuiltAnimationAction`、`AnimateOnAttachedBehavior`、`ParallaxBehavior`、`TiltEffectBehavior`；Animations 子包的 `AnimationFactory`、`EntranceAnimations`、`AttentionAnimations`、`ExitAnimations`、`FramerMotionAnimations`、`SpecialAnimations`
- 截图（Interactions.Custom）：`ScreenshotAction`（`TargetControl`、`FileName`，保存到文件）、`RenderRenderTargetBitmapAction`、`RenderTargetBitmapBehavior`

## 架构

沿用 `Avalonia.InputDemo` 的结构：`App.axaml` + `Program.cs` + `Views/MainWindow`（左侧导航）+ `Views/Pages/*`（每页一个知识点）+ `ViewModels/`。依赖 `Avalonia.Shared`（`DemoHeader`、`EventLog`、`ViewModelBase`、`SharedStyles`）。MVVM 用 CommunityToolkit.Mvvm，默认开启编译绑定。

本项目特有的目录：`Behaviors/`，放第 15 页自己写的 `Behavior<T>` / `Trigger` / `Action`，以及各页共用的小工具（例如沙箱目录）。登记到 `hello-avalonia.slnx` 与 README 项目表。

## 页面清单（15 页，按概念层次排序）

| # | 页面 | 演示的类型 | 验证要点 |
|---|---|---|---|
| 1 | 机制：Behavior / Trigger / Action | `Interaction.Behaviors`、三个基类、`ConditionCollection` | 行为挂到控件上、触发后动作被执行 |
| 2 | 事件转命令 | `EventTriggerBehavior`、`InvokeCommandAction`、`CallMethodAction`、`DelayAction`、`DebounceAction`、`ThrottleAction` | 事件→命令执行，防抖与节流的计数 |
| 3 | 数据驱动与条件 | `DataTriggerBehavior`、`MultiDataTriggerBehavior`、`ChangePropertyAction`、`TimerTrigger`、`TaskCompletedTrigger` | 条件满足时属性改变，条件不满足时不改 |
| 4 | 焦点、键盘、指针 | `FocusBehavior`、`FocusTrapBehavior`、`KeyTrigger`、`SelectAllOnFocusBehavior`、`ExecuteCommandOn*`、`PointerPressedEventTrigger` | 焦点落点、按键触发 |
| 5 | 表单验证 | `TextBoxValidationBehavior` 与各 `*ValidationRule` | 非法输入出现错误、合法输入清除错误 |
| 6 | 集合与 ItemsControl | `AddItemToItemsControlAction`、`RemoveItemInItemsControlAction`、`ClearItemsControlAction`、`ScrollToItemBehavior`、`ListBoxSelectAllBehavior` | 集合项数变化、选中项数变化 |
| 7 | 窗口、弹出与通知 | `CloseWindowAction`、`ShowFlyoutAction`、`ShowPopupAction`、`ShowToolTipAction`、`NotificationManagerBehavior` + `Show*NotificationAction` | 弹出的开关状态、通知卡片出现 |
| 8 | 拖拽 | `Draggable.*`、`DragAndDrop.*` | headless 只验证行为已挂载与属性读回，真实拖动需目视 |
| 9 | 响应式 | `AdaptiveBehavior`、`AspectRatioBehavior` | 宽度变化后样式类切换 |
| 10 | 动画 | `FadeInBehavior`、`PlayAnimationBehavior`、`BeginAnimationAction`、`StartBuiltAnimationAction`、`ParallaxBehavior`、`TiltEffectBehavior`，以及 `AnimationFactory` 与五类动画辅助类 | 动画对象构造成功、触发后控件属性或状态发生变化；动画观感需目视 |
| 11 | 文件系统（沙箱） | `CreateDirectoryAction`、`WriteTextToFileAction`、`DeleteFileAction`、`DeleteDirectoryAction`、`FileSystemWatcherTrigger` | 沙箱里文件真实创建、写入、删除；监视器收到事件 |
| 12 | 网络（本机回环） | `HttpRequestAction`、`NetworkInformationTrigger` | 对本机回环服务发请求，读回状态码与响应内容 |
| 13 | 剪贴板 | `SetClipboardTextAction`、`GetClipboardTextAction`、`ClearClipboardAction` | 写入后读回、清空后为空 |
| 14 | 截图 | `RenderRenderTargetBitmapAction`、`RenderTargetBitmapBehavior`、`ScreenshotAction` | 内存渲染得到非空位图；`ScreenshotAction` 推断会弹保存对话框（待实测），需目视 |
| 15 | 自定义 | 自己写的 `Behavior<T>`、`Trigger`、`Action`（放在 `Behaviors/`） | 自定义行为随控件挂载与卸载，触发与动作按预期执行 |

## 安全约定（有副作用的页面）

"有副作用"的第 11、12、13、14 页都演示**真实执行**，但把影响范围圈在应用自己创建的资源里，并如实显示：

- **文件系统（第 11 页）**：页面载入时在系统临时目录下建一个沙箱文件夹（`<TEMP>\BehaviorsDemo-<随机后缀>`），所有文件操作的 `Path` 都绑定到沙箱内的路径，页面卸载时把整个沙箱删掉。页面上显示沙箱的完整路径，且明确提示"只操作这个文件夹"。页面代码不接受用户输入的任意路径，从源头杜绝误删真实文件。
- **网络（第 12 页）**：请求的目标是页面自己在 `127.0.0.1` 上起的 `HttpListener` 回环服务，不访问任何外部地址，不依赖网络环境，结果确定。
- **剪贴板（第 13 页）**：会真的改系统剪贴板，页面上用提示条说明；页面载入时不动剪贴板，只在用户点按钮时才写。
- **截图（第 14 页）**：优先用内存渲染（`RenderRenderTargetBitmapAction`）；`ScreenshotAction` 的文档只说"保存到文件、`FileName` 是建议文件名"，据此推断会弹保存对话框（**推断，待实测**），由用户选择是否保存。

## 验证

- **headless 探针**（仓库外，不入库），沿用 ControlsDemo 的 `Harness`：每页都做"触发 → 读回"断言，不只检查渲染。原因：行为库靠反射与编译绑定，配错时通常静默失败。
- **实测结论回填**：每批执行完，把实测发现写回本 spec 的"批 N 实测结论"小节（沿用 ControlsDemo 的做法）。
- **构建**：`dotnet build hello-avalonia.slnx` 0 错误，且不新增警告。
- **目视（由用户做）**：拖拽、动画观感、真实窗口里的弹出与通知、`ScreenshotAction` 的保存对话框，headless 验证不了。

## 风险

| 风险 | 处理 |
|---|---|
| 类型名与属性凭记忆会错 | 写每页前先查对应子包的 XML 文档，探针读回结果；文档里没有的不用 |
| 绑定失败是静默的 | 探针对核心行为做触发后读回，而非只看渲染 |
| 所有类型都进了默认命名空间，可能与 Avalonia 自带类型同名 | 遇到歧义时在该页改用显式命名空间，记入"实测结论" |
| 第 11–14 页的 headless 行为未知（临时目录、`HttpListener`、`Clipboard`、`RenderTargetBitmap`） | 各份计划开头先做一次最小探测，结果写进"已实测的事实" |
| `FileSystemWatcherTrigger`、`NetworkInformationTrigger` 依赖系统事件，时序不稳 | 探针里带超时轮询，不用固定等待；仍不稳定的改为只验证"已挂载" |
| 行为库对 Avalonia 版本敏感（见 CLAUDE.md：旧的 `Avalonia.Xaml.Behaviors` 与 v12 二进制不兼容） | 只用 `Xaml.Behaviors.Avalonia`，不引用旧包 |

## 交付拆分

15 页分三份实现计划，每份可独立构建与验证，探针累计计数：

1. **计划 01**：项目骨架（csproj、`App`、`MainWindow` 导航、slnx 与 README 登记）+ 第 1–5 页（机制、事件转命令、数据驱动、焦点键盘指针、表单验证）
2. **计划 02**：第 6–10 页（集合、窗口弹出通知、拖拽、响应式、动画）
3. **计划 03**：第 11–15 页（文件系统、网络、剪贴板、截图、自定义）+ README 与 CLAUDE.md 收尾

## 待确认

无阻塞项。若审阅时对页面增删、沙箱方案、或拆分方式有意见，修改后再进入计划编写。

## 批 1 实测结论（计划 01 执行后）

- **计时器类行为在 headless 里需要跑主循环**：`DebounceAction`、`TimerTrigger` 内部都是 `DispatcherTimer`，`Dispatcher.UIThread.RunJobs()` 不会让它们走一格，必须用 `Dispatcher.UIThread.MainLoop(token)` 切片。探针的 `WaitUntil` / `RunLoop` 据此实现；此前 spike 里"Sleep 之后再泵就能读到结果"的印象是错的。
- **`Button.Command` 不会被手工 `RaiseEvent(Click)` 触发**：命令在 `OnClick` 里执行，探针对绑了 `Command` 的按钮直接调用 `vm.XxxCommand.Execute`，并另外断言 `button.Command == vm.XxxCommand`。
- **`DelayAction` 单独放着不会让后面的 Action 等**，只有放进 `AsyncActionGroup Mode="Sequence"` 才排队（页面 2 并排演示）。
- **`KeyTrigger` 默认 Direct 路由收不到 `TextBox` 里的 Enter**，要写 `EventRoutingStrategy="Tunnel"`；`PointerPressedTrigger` 默认 `Tunnel, Bubble` 会触发两次，写 `Bubble` 才是一次（页面 4 并排演示）。
- **`ConditionalBehavior` 不对初始值触发**，`DataTriggerBehavior` 与 `MultiDataTriggerBehavior` 会在挂载时按当前值执行一次。
- **`TextBoxValidationBehavior` 在挂载时就校验一次**，多条规则失败时 `Error` 用换行拼接，`IsValid` / `Error` 可用 `OneWayToSource` 写进 ViewModel。
- 全部 8 个子包的类型都在默认 XAML 命名空间里，5 个页面没有加任何 `xmlns` 前缀，也没有遇到与 Avalonia 自带类型的同名歧义。
- 探针累计 74 条断言全部通过，0 条警告日志（金丝雀已确认日志汇有效）。

## 批 2 实测结论（计划 02 执行后）

- **集合 Action 作用在 `ItemsSource` 绑定的集合上**：`AddItemToItemsControlAction`、`ClearItemsControlAction` 对手写子项的列表什么都不做，也没有日志；`RemoveItemInItemsControlAction` 没有属性，删的是所在列表项的 DataContext，只能放在 `ItemTemplate` 里。
- **`ScrollToItemBehavior.Item` 是 `IObservable<object>`**：项目没有引入 System.Reactive，用一个只有 `Publish` 的 `ScrollRequest` 即可。
- **通知必须显式绑定管理器**：`NotificationManagerBehavior` 要有 `x:Name`，每个 `Show*NotificationAction.NotificationManager` 绑 `{Binding #NM.NotificationManager}`；不绑就不显示，也没有日志。
- **`CloseWindowAction` 不写 `TargetWindow` 时关掉所在窗口**，所以只能放在子窗口里。
- **`ListReorderDragBehavior` 必须通过 `ListBoxItem` 样式里的 `BehaviorCollectionTemplate` 挂载**；直接写进 `DataTemplate` 的子元素里拖不动。`CanvasDragBehavior` 改 `Canvas.Left/Top`，`MouseDragElementBehavior` 加 `TranslateTransform`。headless 里拖动的 `MouseMove` 必须带 `RawInputModifiers.LeftMouseButton`。
- **文字与文件投放只能验证行为已挂载**：真实投放要操作系统的拖放循环，页面上注明需手动试。
- **`AdaptiveBehavior` / `AspectRatioBehavior` 只加减样式类**，颜色与文字由样式表按类名决定；标签不能在本地写 `Text`，否则会压制样式里的 Setter。
- **`Animation` 不能直接写在行为或 Action 里**（AVLN2200）：放进某个 `Style` 的 `Style.Resources`，用 `DynamicResource` 引用；`PlayAnimationBehavior` 与 `BeginAnimationAction` 都能让不透明度落到 0.4。`FadeInBehavior` 只在挂载时播一次。`AttentionAnimations.SetShakeX` 等是 `SetXxx(Control, double)` 形态，没有 XAML 行为。`TiltEffectBehavior` 与 `ParallaxBehavior` 只能验证已挂载，真实效果需目视。
- 探针累计 118 条断言全部通过，0 条警告日志。

## 批 3 实测结论（计划 03 执行后）

- **普通 `Trigger` 不给 Action 传 DataContext**：`FileSystemWatcherTrigger`、`NetworkInformationTrigger` 都继承它，里面 `InvokeCommandAction` 的 `Command="{Binding …}"` 解析不出来（触发 0 次、无日志）；在代码里直接给 `Command` 赋值就正常。自己写的 `Trigger<T>` 同样如此（在 UI 线程上也失败，排除了线程原因），改继承 `StyledElementTrigger<T>` 后绑定恢复。`EventTriggerBehavior` 等本来就是 `StyledElementTrigger`，不受影响。
- **`FileSystemWatcherTrigger` 要求挂载时目录已存在**；新建 `.txt` 触发两次（创建 + 内容变化），追加再触发一次；事件来自线程池线程，命令里要回到 UI 线程。
- **`HttpRequestAction` 的响应是只读属性**：Action 要有 `x:Name`，用 `{Binding #名字.ResponseContent}` 读回；对本进程的 `HttpListener` 回环服务 POST 得到 201 与「pong:ping」。
- **`NetworkInformationTrigger` 没有属性，headless 里无法制造事件**，只能验证挂载，真实效果需手动开关网络。
- **剪贴板三个 Action 都能用**，清空后读回空字符串；页面提示会改写真实剪贴板。
- **`RenderTargetBitmapBehavior` 挂在 `Image` 上**，`Renderer` 绑定到实现 `IRenderTargetBitmapRenderer` 的对象；`RenderRenderTargetBitmapAction` 能再触发一次渲染。**`ScreenshotAction` headless 里不产出文件**，真实效果需目视。
- **自定义三件套**：`Behavior<T>` 在 `OnAttachedToVisualTree` 订阅、`OnDetachedFromVisualTree` 取消；触发器用 `Interaction.ExecuteActions(AssociatedObject, Actions, null)`；Action 基类要写全名 `Avalonia.Xaml.Interactivity.Action`（避免与 `System.Action` 冲突）。连点计数要监听 `PointerPressed`，`Tapped` 的第二次会被 `DoubleTapped` 吞掉。
- 探针累计 161 条断言全部通过，0 条警告日志。
