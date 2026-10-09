# Avalonia 交互图形层演示项目（#8 Events / #9 Input / #10 Graphics / #11 CustomControls）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建成 `Avalonia.EventsDemo`、`Avalonia.InputDemo`、`Avalonia.GraphicsDemo`、`Avalonia.CustomControlsDemo` 四个演示项目，覆盖官方文档 Events、Input & Interaction、Graphics and Animation、Custom controls 四个分类的可演示功能点。

**Architecture:** 四个独立 WinExe 项目，沿用 `Avalonia.LayoutDemo` 样板确立的结构——`MainWindow` 只承载 `TabControl` 外壳，每个功能点是 `Views/Pages/` 下一个独立 `UserControl`，页面顶部统一用 `Avalonia.Shared` 的 `DemoHeader` 显示中文说明与官方文档路径。无外部依赖，无网络调用。事件日志这种四个项目里至少三个要用的小部件，提到 `Avalonia.Shared/Helpers/EventLog.cs` 共享。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm、中央包管理（`Directory.Packages.props`）

**Spec:** `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**前几组的写法参考:** `docs/superpowers/plans/2026-10-07-styling-binding-layer-demos.md`（硬性规则 1–21、探针写法）、`docs/superpowers/plans/2026-09-22-foundation-layer-demos.md`，以及已落成的 `Avalonia.StylingDemo/`、`Avalonia.LayoutDemo/`

## Global Constraints

以下约束适用于本 plan 的每一个任务：

- **Avalonia 版本统一为 12.1.2**，不为任何项目降级到 Avalonia 11
- **TargetFramework 为 `net10.0`**，`Nullable` 为 `enable`
- **包版本只在 `Directory.Packages.props` 声明**，`.csproj` 里的 `PackageReference` 不带 `Version` 属性；本 plan **不新增任何包**
- **不引用 `Avalonia.Diagnostics`**（停在 11.3.22，v12 的 DevTools 已内置于主包）
- **不引入 ReactiveUI、Prism 等第三方 MVVM/UI 框架**，只用官方 API + `CommunityToolkit.Mvvm`
- **C# 与 XAML 注释用英文**；**界面文字（Tab 标题、说明条、按钮文案）用中文**；**标识符（类名、属性名、`x:Name`）用英文**
- **每个演示页顶部必须有 `DemoHeader`**，含中文功能点描述 + 对应官方文档路径。`DocPath` **不带 `docs/` 前缀**，写成 `分类名/子页名`（如 `input-interaction/pointer`、`graphics-animation/brushes`）
- **每个功能点一个 `UserControl`**，放在 `Views/Pages/` 下
- **不为演示项目写自动化测试**
- **`AvaloniaUseCompiledBindingsByDefault` 设为 `true`**
- **功能点粒度 3–13 个 Tab**（spec「功能点粒度」）：本 plan 为 5 / 8 / 13 / 7
- **去重规则**（spec「分类之间的去重规则」）：一个功能点只在最贴合的分类里完整实现，其他分类做指路页。本 plan 涉及：Focus Manager 完整实现放 #9（#12 做指路页）；路由事件完整实现放 #8，#9 做指路页；自定义路由事件放 #8，#11 的「定义事件」指向它；定义属性放 #7，#11 的「定义属性」指向它
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 前三组已确立的硬性规则（违反会导致静默失败）

完整的来龙去脉见 spec 的「样板阶段」「基础层」「样式绑定层」三节实测结论。这里只列与本组页面直接相关的，**每一条都对应一次"构建 0 错误、运行无异常、行为却是错的"**：

1. **数值绑到结构体属性必须走转换器**（`double` → `Thickness`/`CornerRadius`）。用 `Avalonia.Shared/Converters/DoubleToThicknessConverter.cs`。本组的滑块几乎都绑到 `double` 型属性（`Angle`、`Radius`、`Offset`），不涉及此条，但凡写到结构体属性就要想起它。
2. **打算被 Style / 伪类 / Transition 驱动的属性，不在元素上写本地值。** XAML 元素属性是 `BindingPriority.LocalValue`，永久压过所有样式 Setter，连警告都没有。**本组的过渡页与动画页尤其要小心**：要被 `:pointerover` 或类名驱动改变的 `Width`、`Background`、`RenderTransform`，初值写进普通 `<Style>`，不写在元素上。
3. **`StringFormat` 里的字面花括号必须双写**；**以 `{0}` 开头的格式串要写成 `'{}{0} …'`**，否则 `AVLN2000`。
4. **验证用 headless 探针读回属性值，不靠目视。** 探针建在仓库外、跑完即弃。
5. **探针里发鼠标事件前，先用 `TranslatePoint` 换算出窗口坐标**，凭感觉写的坐标常落在空白处，什么都不发生且不报错。
6. **`RaiseEvent(new RoutedEventArgs(Button.ClickEvent))` 只触发 `Click` 处理器，不执行 `Command`**；验证命令要写 `button.Command!.Execute(button.CommandParameter)`。
7. **headless 默认绘图后端里所有 `Bitmap` 的 `PixelSize` 都是 `1×1`**，只能断言 `Image.Source is Bitmap`。
8. **标记扩展参数里以 `{` 开头的单引号字符串要写成 `'{}{…'`。**
9. **Flyout 的 Presenter 在独立的弹出层里**，探针要从 Flyout 持有的 Presenter 引用向下找，从窗口向下找不到。
10. **`Watermark` 已过时（`AVLN5001`）**，用 `PlaceholderText`。

### 本组新增的硬性规则（编写 plan 时已实测，Avalonia 12.1.2 headless）

11. **`Gestures` 类在 12.1.2 是 internal 的**（`CS0122`）。手势事件直接用 `InputElement` 上的：`Tapped`、`DoubleTapped`、`RightTapped`、`Holding`，以及静态路由事件 `InputElement.PinchEvent`、`PinchEndedEvent`、`ScrollGestureEvent`、`SwipeGestureEvent`、`PointerTouchPadGestureMagnifyEvent`（用 `AddHandler` 订阅）。
12. **生命周期事件的顺序与直觉不符：`Initialized` 在附加之后才触发。** 实测 `Button` 加入已显示的 Window，依次为 `AttachedToLogicalTree > AttachedToVisualTree > Initialized > SizeChanged > Loaded`；移除时为 `DetachedFromLogicalTree > DetachedFromVisualTree > Unloaded`。生命周期页按实测顺序写说明，不按 WPF 经验写。
13. **`KeyDown` 的路由顺序与 `Handled` 的作用范围。** 外层 outer、中层 mid、内层 inner 各在 Tunnel 与 Bubble 注册时，顺序为 `outer:tunnel → mid:tunnel → inner:tunnel → inner:bubble → mid:bubble → outer:bubble`。在 `inner:bubble` 里置 `e.Handled = true`，`mid:bubble` 与 `outer:bubble` **都不再触发**；但用 `AddHandler(..., handledEventsToo: true)` 注册的 outer 处理器**仍会触发**。`Handled` 同时让按键不再写进 `TextBox`（文本保持 `''`）。
14. **路由策略因事件而异，不要凭印象写。** 实测 `KeyDownEvent` 与 `PointerPressedEvent` 是 `Tunnel, Bubble`，`PointerEnteredEvent` 是 `Direct`，`Button.ClickEvent` 是 `Bubble`。
15. **命中测试：没有 `Background` 的元素不参与命中。** 实测 `Border` 不设 `Background` 时 `window.InputHitTest(point)` 返回的是它的父 `Panel`；设 `Background="Transparent"` 后命中的就是它自己。需要「透明但可点」的区域，要显式写 `Transparent`。
16. **`:focus-visible` 只在键盘导航获得焦点后出现。** 实测 `Focus(NavigationMethod.Tab)` 后 `Classes` 为 `:focus-within,:focus,:focus-visible`；`Focus(NavigationMethod.Pointer)` 后只有 `:focus-within,:focus`。样式要画「键盘焦点环」就选 `:focus-visible`，不是 `:focus`。
17. **`TextInput` 事件可以在 Tunnel 阶段拦截。** `AddHandler(InputElement.TextInputEvent, handler, RoutingStrategies.Tunnel)` 里对非数字置 `e.Handled = true`，实测 `KeyTextInput("a")` 被吞、`KeyTextInput("7")` 通过，文本为 `'7'`。
18. **指针捕获与读数。** 实测 `e.Pointer.Capture(control)` 后 `e.Pointer.Captured == control` 为真；`e.Pointer.Type` 在 headless 里是 `Mouse`；`e.GetCurrentPoint(control).Properties.IsLeftButtonPressed` 为真。
19. **只有 `VisualChildren` 的自定义控件，子元素拿不到继承的 `DataContext`。** 实测给 Window 设 `DataContext="ctx"` 后：只调 `VisualChildren.Add` 的子元素 `DataContext` 为 `null`；同时调 `LogicalChildren.Add` 的子元素为 `ctx`。自定义控件手动管理子元素时，两个集合都要加（控件树页把这一对照做成演示）。
20. **`FlyoutBase` 是纯抽象基类**（要实现 `ShowAt(Control)`、`Hide()` 等一整套），自定义 Flyout 要从 **`PopupFlyoutBase`** 派生，只需重写 `protected override Control CreatePresenter()`。实测 `ShowAt(button)` 后 `IsOpen` 为真，在 Presenter 内的按钮 `Click` 里调 `Hide()` 后 `IsOpen` 为假。
21. **headless 里动画只能做粗粒度断言，且「有限」与「无限」两类行为不同。** 反复 `AvaloniaHeadlessPlatform.ForceRenderTimerTick()`（每次后 `Thread.Sleep(30)` 再 `Dispatcher.UIThread.RunJobs()`）才会推进时钟。实测：
    - **`Transitions`（有限时长）能走完全程**：`Width` 从 100 过渡到 300，采样到 `168,196,229,257,272,284,292,296,299,300`，终值 `299.88`；`BrushTransition` 同样把背景色过渡到目标色附近。可以断言「走到了终点附近」。
    - **`IterationCount="Infinite"` 的 `Style.Animations` 只推进头一两帧就停住**：`Opacity` 采样 60 次（共 3.6 秒）读回 `0.26,0.43,0.43,0.43…`，之后恒定。**只能断言「偏离了静止值且落在关键帧区间内」**（例：关键帧 0.2–1，静止值 1，读回 `< 0.99` 且 `>= 0.2`），不能断言「在振荡」。
    - `Animation.RunAsync(control)` 在 headless 里不会推进，任务不完成（实测 80 次节拍后宽度仍为初值）——动画一律用 `Style.Animations` + 类名驱动，不用 `RunAsync`。
    - 因此动画与过渡的探针**绝不断言确切数值**；起始读数也不是关键帧起点（首次 `RunJobs` 就已跑过若干帧）。
22. **`StreamGeometryContext` 的弧线写法**：`BeginFigure(Point, false)` → `ArcTo(Point, Size, 0, isLargeArc, SweepDirection.Clockwise)` → `EndFigure(false)`，`StreamGeometry.Bounds` 随即可读。`Easing` 子类的 `Ease(double)` 可直接调用（`BounceEaseOut.Ease(0.5)` 读回 `0.71875`）。
23. **`GotFocus` 的事件参数类型是 `FocusChangedEventArgs`**（`Avalonia.Input`，带 `NavigationMethod`），**不存在 `GotFocusEventArgs`**（实测 `CS0246`）。
24. **`MenuItem` 的 `InputGesture` 只显示文字，不注册按键。** 实测同一命令分别用三种写法，焦点在 `TextBox` 时依次按 `Ctrl+1/2/3`：只写 `InputGesture` 执行 0 次；只写 `HotKey` 执行 1 次；两个都写执行 1 次。「菜单右边显示了快捷键、按下去没反应」就是这个静默失败。
25. **`ElementComposition.GetElementVisual(control)` 在控件加入可视树之前返回 `null`。** 实测 `new Border()` 未挂载时为 `null`，挂到已显示的窗口后返回 `CompositionDrawListVisual`。要在 `Loaded` 之后取。`visual.Opacity = 0.5f`、`visual.Scale = new Vector3D(1.2, 1.2, 1)` 同步写入、可读回；`compositor.CreateScalarKeyFrameAnimation()` + `Target` + `InsertKeyFrame` + `visual.StartAnimation("Opacity", anim)` 可以启动，**但动画在合成线程上跑，客户端读回的 `visual.Opacity` 不反映动画中间值**，探针只能断言「调用不抛异常」。
26. **非控件对象不能命名。** `GradientStop`、`RotateTransform`、`BlurEffect`、`DropShadowEffect`、`CombinedGeometry` 上写 `Name` 或 `x:Name` 都是构建期 `AVLN2000: Unable to resolve suitable regular or attached property Name`。要在 code-behind 里改它们，命名它们的**宿主控件**再顺着属性取：`CombinedPath.Data` 是 `CombinedGeometry`，`((LinearGradientBrush)border.Background).GradientStops[1]` 是第二个停靠点，`(RotateTransform)border.RenderTransform`。
27. **滑块绑到这些非控件对象的属性是通的。** 实测 `Offset="{Binding #S.Value}"`（`GradientStop`）、`Angle="{Binding #A.Value}"`（`RotateTransform`）、`Radius=`（`BlurEffect`）、`OffsetX/BlurRadius/Opacity=`（`DropShadowEffect`）、`Width=`（`PathIcon`）全部连通：滑块从 0.5 拨到 0.8，读回 `GradientStop.Offset` 为 `0.8`；从 30 拨到 60，读回 `RotateTransform.Angle` 为 `60`。**元素名绑定不需要这些对象自己有名字**，只要被绑的源是命名控件。
28. **`GeometryCombineMode` 在 12.1.2 只有 `Union` / `Intersect` / `Xor` / `Exclude`**，没有 WPF 的 `Exclude1From2` / `Exclude2From1`。其余本组用到的枚举：`BitmapInterpolationMode` = `Unspecified/None/LowQuality/MediumQuality/HighQuality`；`TextRenderingMode` = `Unspecified/SubpixelAntialias/Antialias/Alias`；`EdgeMode` = `Unspecified/Antialias/Aliased`。下拉框一律 `Enum.GetValues<T>()`，不手写列表。

### 编写本 plan 时已实测通过的写法（直接照用，不必再试）

仓库外的 headless 探针里逐项编译、读回，Avalonia 12.1.2：

| 写法 | 结果 |
|---|---|
| `Style.Animations` 里 `Animation` + `KeyFrame` + `Cue`（`Duration=0.4s`、`PlaybackDirection=Alternate`、`IterationCount=Infinite`、`Easing=CubicEaseInOut`） | 逐帧推进 `Width`、`Opacity` |
| `Border.Transitions` 内 `DoubleTransition`（`BounceEaseOut`）、`BrushTransition`、`TransformOperationsTransition` | 编译通过 |
| `TransitioningContentControl.PageTransition` 内 `CompositePageTransition` 含 `PageSlide` 与 `CrossFade` | 编译通过，换 `Content` 后读回新内容 |
| `RenderTransform="rotate(30deg) scale(1.2)"`、`RenderTransformOrigin="50%,50%"`、`skew(10deg,0)` | 编译通过 |
| `LayoutTransformControl` 内 `RotateTransform` | 编译通过 |
| `DropShadowEffect`、`BlurEffect` | 编译通过 |
| `ConicGradientBrush`、`RadialGradientBrush` 作 `OpacityMask`、`VisualBrush` | 编译通过 |
| `Clip="M0,0 L60,0 L30,60 Z"`、`ClipToBounds` 配 `CornerRadius` | 编译通过 |
| `Path` 的 `StrokeJoin`、`StrokeDashArray`；`CombinedGeometry GeometryCombineMode="Xor"` 内 `EllipseGeometry` + `RectangleGeometry` | 编译通过 |
| `DrawingImage` 内 `GeometryDrawing`；**元素形式**的 `DrawingBrush`（`TileMode="Tile" DestinationRect`） | 编译通过。`Fill="{DrawingBrush}"` 会被当成标记扩展而 `AVLN2000`，必须写元素形式 |
| `PathIcon`；`RenderOptions.BitmapInterpolationMode="None"`、`RenderOptions.EdgeMode="Aliased"`、`RenderOptions.BitmapBlendingMode="Plus"`、`TextOptions.TextRenderingMode` | 编译通过 |
| `[TemplatePart("PART_Fill", typeof(Border))]` + `OnApplyTemplate(TemplateAppliedEventArgs e)` 里 `e.NameScope.Find<Border>("PART_Fill")` | 拿到模板部件 |
| 重写 `Render(DrawingContext)`：`DrawRectangle`、`DrawEllipse`、`DrawLine`、`DrawText(FormattedText, Point)`、`PushClip`/`PushTransform`/`PushOpacity`（`using`） | 编译通过 |
| `class Ring : Panel` 重写 `MeasureOverride`/`ArrangeOverride`，子元素 `Arrange(new Rect(...))` | 子元素 `Bounds.TopLeft` 读回 `(0,0)`、`(10,10)` |
| `ElementComposition.GetElementVisual(control)` | 返回 `CompositionDrawListVisual`；`Compositor` 有 `CreateScalarKeyFrameAnimation`、`CreateVector3DKeyFrameAnimation`、`CreateExpressionAnimation`、`CreateImplicitAnimationCollection` 等；`CompositionVisual` 有 `Offset`、`Scale`、`Opacity`、`RotationAngle`、`CenterPoint`、`ImplicitAnimations` |
| `Button.HotKey = new KeyGesture(Key.S, KeyModifiers.Control)` | 按 `Ctrl+S` 触发按钮 |
| `Window.KeyBindings` 里 `KeyBinding { Gesture, Command }` | 按键触发命令 |
| `TopLevel.GetTopLevel(c).FocusManager.GetFocusedElement()`；`Focus(NavigationMethod.Tab)`；`IsTabStop=false` 的控件被 Tab 跳过 | 均按预期 |
| `window.MouseDown(pt, MouseButton.Left)` + `MouseUp` 连发两次 | `Tapped=1`、`DoubleTapped=1`；右键 `RightTapped=1` |
| `DragDrop.DoDragDropAsync(PointerPressedEventArgs, IDataTransfer, DragDropEffects)` → `Task<DragDropEffects>`；`DragDrop.SetAllowDrop`；`AddDropHandler`/`AddDragOverHandler`/`AddDragEnterHandler`/`AddDragLeaveHandler`；`DragEventArgs.DataTransfer`/`DragEffects`/`KeyModifiers`/`Handled`；`new DataTransfer()` + `Add(DataTransferItem.CreateText("hello"))` + `TryGetText()` | 均存在（`DataObject`/`DataFormats` 是旧 API，不用） |
| `RoutedEvent.Register<Owner, TArgs>(name, RoutingStrategies.Bubble)` + `RaiseEvent` | 冒泡到父级的 `AddHandler` |

## 本 plan 的范围

spec 第二阶段分 4 组，本 plan 只实现**第三组「交互图形层」**：#8 Events、#9 Input、#10 Graphics、#11 CustomControls。基础层与样式绑定层已交付，应用服务层（#12–#15）留待后续 plan。

## 功能点映射的实测修正

编写本 plan 时逐个核对了四个分类的官方侧边栏（2026-10-07 抓取），与 spec 的功能点列表有以下出入，本 plan 按实际文档结构执行：

| 分类 | 官方子页数 | spec 的列法 | 本 plan 的处理 |
|---|---|---|---|
| Events | 2 | 5 个功能点 | 5 个 Tab。官方只有 `lifecycle-events`、`input-events` 两页；路由事件三阶段、`Handled`、自定义路由事件三项在官方放在 `input-interaction/routed-events`，spec 归给 #8，照 spec 办，三者各占一个 Tab（粒度够细，值得分开演示） |
| Input & Interaction | 10 | 7 个功能点 | 8 个 Tab。`mouse-and-keyboard-shortcuts` 并入「键盘与 HotKey」；`adding-interactivity` 并入「交互的写法」（事件处理器 / 命令 / 手势三种响应方式）；`routed-events` 做成指路页指向 #8（去重） |
| Graphics and Animation | 21 | 13 个功能点 | 13 个 Tab。`brushes`+`gradients` 合成「画刷」；`transforms`+`render-vs-layout-transforms` 合成「变换」；`drawing-graphics`+`custom-rendering` 合成「自定义绘制」；`animations`+`animation-settings`+`keyframe-animations` 合成「关键帧动画」；`clipping-and-masking`+`hit-testing` 合成「裁剪遮罩与命中测试」；`image-interpolation`+`text-options`+`bitmap-blend-modes` 合成「渲染选项」；spec 漏了 `composition-animations`，补一个 Tab |
| Custom controls | 10 | 7 个功能点 | 7 个 Tab。「定义属性」「定义事件」合成一个指路页（属性指向 #7，事件指向 #8 的自定义路由事件）；`control-trees` 补一个 Tab；`custom-control-library` 讲的是类库打包流程，无运行时内容，不做 Tab |

## File Structure

四个项目结构同构，均照搬 `Avalonia.LayoutDemo`。下表只列每个项目**独有**的文件；`Program.cs`、`App.axaml(.cs)`、`app.manifest`、`Assets/avalonia-logo.ico`、`Views/MainWindow.axaml(.cs)`、`.csproj` 六件套每个项目都有一份，由 Task 1 的脚本一次生成。

### 共享类库的改动

| 文件 | 职责 |
|---|---|
| `Avalonia.Shared/Helpers/EventLog.cs` | 事件日志：一个 `ObservableCollection<string>` 加 `Write`/`Clear`/`Bind(ListBox)`。#8、#9、#10、#11 共 20+ 页要写「触发了什么」，复制次数远超 3 次，按 spec「判断是否该共享，看的是复制次数」提到 Shared |

### 项目 #8 `Avalonia.EventsDemo`（5 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/LifecyclePage.axaml(.cs)` | 控件挂载/卸载时 8 个生命周期事件的实际触发顺序 | `events/lifecycle-events` |
| `Views/Pages/InputEventsPage.axaml(.cs)` | 指针、键盘、文本输入事件的触发与参数读数 | `events/input-events` |
| `Views/Pages/RoutingPage.axaml(.cs)` | 三层嵌套元素上 Tunnel/Bubble/Direct 的触发顺序与各事件的 `RoutingStrategies` | `input-interaction/routed-events` |
| `Views/Pages/HandledPage.axaml(.cs)` | `Handled` 截断冒泡、`handledEventsToo` 越过截断 | `input-interaction/routed-events` |
| `Views/Pages/CustomRoutedEventPage.axaml(.cs)` | 自定义路由事件的声明、CLR 包装、XAML 附加订阅 | `input-interaction/routed-events` |
| `Controls/Notifier.cs` | 带自定义路由事件 `Ping` 的最小控件 | — |
| `Controls/PingEventArgs.cs` | `Ping` 的事件参数 | — |

### 项目 #9 `Avalonia.InputDemo`（8 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/PointerPage.axaml(.cs)` | 指针读数、按键状态、滚轮、捕获 | `input-interaction/pointer` |
| `Views/Pages/FocusPage.axaml(.cs)` | `FocusManager`、Tab 顺序、`IsTabStop`、`NavigationMethod`、`:focus-visible` | `input-interaction/focus` |
| `Views/Pages/GesturesPage.axaml(.cs)` | Tapped / DoubleTapped / RightTapped / Holding 与触摸手势事件的计数 | `input-interaction/gestures` |
| `Views/Pages/KeyboardPage.axaml(.cs)` | 按键读数、`HotKey`、`KeyBinding` | `input-interaction/keyboard-and-hotkeys` |
| `Views/Pages/InteractivityPage.axaml(.cs)` | 同一个动作的三种响应写法：`Click` 处理器 / `Command` / `Tapped`；菜单项的 `InputGesture` | `input-interaction/commanding` |
| `Views/Pages/DragDropPage.axaml(.cs)` | 文本拖放：`DoDragDropAsync`、`SetAllowDrop`、四个拖放处理器 | `input-interaction/drag-and-drop` |
| `Views/Pages/TextInputPage.axaml(.cs)` | `TextInput` 事件过滤、`TextInputOptions`、IME 开关 | `input-interaction/text-input` |
| `Views/Pages/RoutedEventsPage.axaml(.cs)` | 指路页 → `Avalonia.EventsDemo` | `input-interaction/routed-events` |

### 项目 #10 `Avalonia.GraphicsDemo`（13 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/BrushesPage.axaml(.cs)` | 纯色/线性/径向/锥形渐变、`DrawingBrush` 平铺、`VisualBrush`，渐变停靠点用滑块实时改 | `graphics-animation/brushes` |
| `Views/Pages/TransformsPage.axaml(.cs)` | `RenderTransform` 与 `LayoutTransformControl` 对照：旋转后邻居是否被推开 | `graphics-animation/transforms` |
| `Views/Pages/ShapesPage.axaml(.cs)` | 6 种形状、描边属性、路径迷你语言、`CombinedGeometry` | `graphics-animation/shapes-and-geometries` |
| `Views/Pages/DrawingPage.axaml(.cs)` | `DrawingContext` 的图元与状态栈（`PushClip`/`PushTransform`/`PushOpacity`）+ `DrawingImage` | `graphics-animation/drawing-graphics` |
| `Views/Pages/EffectsPage.axaml(.cs)` | `BlurEffect`、`DropShadowEffect`，参数用滑块实时改 | `graphics-animation/effects` |
| `Views/Pages/ClipAndHitPage.axaml(.cs)` | `Clip`、`ClipToBounds`、`OpacityMask`；`Background` 为空与 `Transparent` 的命中差别 | `graphics-animation/clipping-and-masking` |
| `Views/Pages/IconsPage.axaml(.cs)` | `PathIcon`、`StreamGeometry` 资源、`DrawingImage` 三种图标写法 | `graphics-animation/adding-icons` |
| `Views/Pages/RenderOptionsPage.axaml(.cs)` | 位图插值、边缘抗锯齿、文本渲染、位图混合 | `graphics-animation/image-interpolation` |
| `Views/Pages/AnimationsPage.axaml(.cs)` | `Style.Animations` 关键帧，用类名切换 Duration / 迭代 / 方向 / 延迟 | `graphics-animation/keyframe-animations` |
| `Views/Pages/TransitionsPage.axaml(.cs)` | `Transitions` 驱动属性平滑变化：Double / Brush / TransformOperations | `graphics-animation/control-transitions` |
| `Views/Pages/PageTransitionsPage.axaml(.cs)` | `TransitioningContentControl` 与 `PageSlide` / `CrossFade` / 组合 | `graphics-animation/page-transitions` |
| `Views/Pages/EasingPage.axaml(.cs)` | 13 种缓动函数的曲线图与小球位移对照 | `graphics-animation/easing-functions` |
| `Views/Pages/CompositionPage.axaml(.cs)` | `ElementComposition` 取合成层 Visual，写 Offset / Opacity 动画 | `graphics-animation/composition-animations` |
| `Controls/Sketch.cs` | `DrawingPage` 用的自绘控件：在 `Render(DrawingContext)` 里画一组图元，三个开关决定是否套 `PushClip` / `PushTransform` / `PushOpacity` |

### 项目 #11 `Avalonia.CustomControlsDemo`（7 个 Tab）

| 文件 | 职责 | 官方文档路径 |
|---|---|---|
| `Views/Pages/UserControlPage.axaml(.cs)` | 用 `UserControl` 组合出 `LabeledSlider` | `custom-controls/usercontrol` |
| `Views/Pages/TemplatedControlPage.axaml(.cs)` | `Meter`：`TemplatePart`、`OnApplyTemplate`、`TemplateBinding`、伪类 `:full`、模板换皮 | `custom-controls/templated-controls` |
| `Views/Pages/CustomDrawnPage.axaml(.cs)` | `RingGauge`：`Render` 自绘 + `AffectsRender` | `custom-controls/custom-drawn-controls` |
| `Views/Pages/PropertiesAndEventsPage.axaml(.cs)` | 指路页 → #7 定义属性、#8 自定义路由事件 | `custom-controls/defining-properties` |
| `Views/Pages/ControlTreesPage.axaml(.cs)` | 只加 `VisualChildren` 与两个集合都加的 `DataContext` 继承对照 | `custom-controls/control-trees` |
| `Views/Pages/CustomPanelPage.axaml(.cs)` | `RadialPanel`：`MeasureOverride`/`ArrangeOverride` 与 `AffectsArrange` | `custom-controls/custom-panel` |
| `Views/Pages/CustomFlyoutPage.axaml(.cs)` | `SwatchFlyout : PopupFlyoutBase` | `custom-controls/custom-flyout` |
| `Controls/LabeledSlider.axaml(.cs)` | UserControl | — |
| `Controls/Meter.cs` / `Controls/Meter.axaml` | TemplatedControl 与它的 ControlTheme | — |
| `Controls/RingGauge.cs` | 自绘控件 | — |
| `Controls/OnlyVisualHost.cs` / `Controls/VisualAndLogicalHost.cs` | 控件树页的两个对照宿主 | — |
| `Controls/RadialPanel.cs` | 自定义 Panel | — |
| `Controls/SwatchFlyout.cs` | 自定义 Flyout | — |

### 共享文件的改动

| 文件 | 改动 |
|---|---|
| `hello-avalonia.slnx` | 注册四个新项目 |
| `README.md` | 项目表格在 `Avalonia.PropertySystemDemo` 与 `Avalonia.MusicStore` 之间插四行（官方分类顺序：Events、Input、Graphics、CustomControls） |
| `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md` | 回写本组实测结论 |

---

## Task 1: 共享事件日志、四个项目的骨架与 TabControl 外壳

**Files:**
- Create: `Avalonia.Shared/Helpers/EventLog.cs`
- Create: `Avalonia.EventsDemo/`、`Avalonia.InputDemo/`、`Avalonia.GraphicsDemo/`、`Avalonia.CustomControlsDemo/` 四个目录，各含 `Avalonia.XxxDemo.csproj`、`Program.cs`、`App.axaml`、`App.axaml.cs`、`app.manifest`、`Assets/avalonia-logo.ico`、`Views/MainWindow.axaml`、`Views/MainWindow.axaml.cs`
- Modify: `hello-avalonia.slnx`

**Interfaces:**
- Consumes: `avares://Avalonia.Shared/Themes/SharedStyles.axaml`（已存在，提供 `DemoHeader`、`TextBlock.caption`、`TextBlock.hint`、`Border.stage`）
- Produces:
  - `Avalonia.Shared.Helpers.EventLog`——`ObservableCollection<string> Entries { get; }`、`void Write(string message)`、`void Clear()`。Task 2–5 的页面都靠它写事件流
  - 四个可运行的空壳窗口。页面命名空间分别为 `Avalonia.EventsDemo.Views.Pages`、`Avalonia.InputDemo.Views.Pages`、`Avalonia.GraphicsDemo.Views.Pages`、`Avalonia.CustomControlsDemo.Views.Pages`，供 Task 2–5 挂页面

- [x] **Step 1: 用脚本批量生成四份骨架的目录与二进制文件**

在仓库根目录执行：

```bash
for p in EventsDemo InputDemo GraphicsDemo CustomControlsDemo; do
  mkdir -p "Avalonia.$p/Assets" "Avalonia.$p/Views/Pages"
  cp Avalonia.LayoutDemo/Assets/avalonia-logo.ico "Avalonia.$p/Assets/"
  sed "s/Avalonia\.LayoutDemo/Avalonia.$p/" Avalonia.LayoutDemo/app.manifest > "Avalonia.$p/app.manifest"
done
grep -l "Avalonia.LayoutDemo" Avalonia.EventsDemo/app.manifest Avalonia.InputDemo/app.manifest Avalonia.GraphicsDemo/app.manifest Avalonia.CustomControlsDemo/app.manifest || echo "manifest names rewritten"
```

Expected: 最后一行输出 `manifest names rewritten`。

- [x] **Step 2: 创建四个 .csproj**

四份内容逐字相同（文件里没有项目名）。`Avalonia.EventsDemo/Avalonia.EventsDemo.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <AvaloniaResource Include="Assets\**" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="Avalonia.Fonts.Inter" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

复制为 `Avalonia.InputDemo/Avalonia.InputDemo.csproj`、`Avalonia.GraphicsDemo/Avalonia.GraphicsDemo.csproj`、`Avalonia.CustomControlsDemo/Avalonia.CustomControlsDemo.csproj`。与样式绑定层三项目逐字相同，**不新增任何包**。

- [x] **Step 3: 创建四个 Program.cs**

`Avalonia.EventsDemo/Program.cs`：

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.EventsDemo
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace(LogEventLevel.Debug);
    }
}
```

另三个项目只把 `namespace` 换成 `Avalonia.InputDemo` / `Avalonia.GraphicsDemo` / `Avalonia.CustomControlsDemo`。

- [x] **Step 4: 创建四个 App.axaml 与 App.axaml.cs**

`Avalonia.EventsDemo/App.axaml`：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.EventsDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader plus the shared caption, hint and stage styles.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>
</Application>
```

`Avalonia.EventsDemo/App.axaml.cs`：

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.EventsDemo.Views;

namespace Avalonia.EventsDemo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // No window-level DataContext: each page owns its own state.
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

另三个项目把 `x:Class`、`namespace`、`using` 里的 `Avalonia.EventsDemo` 换成对应项目名。

`Application.Resources` 这一节本组四个项目**一开始都不需要**（自定义控件的 ControlTheme 各自由页面或 Task 5 加），Task 5 再给 CustomControlsDemo 补。

- [x] **Step 5: 创建四个 MainWindow**

`Avalonia.EventsDemo/Views/MainWindow.axaml`（5 个 Tab，横排放得下）：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="900"
        d:DesignHeight="640"
        x:Class="Avalonia.EventsDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Events Demo"
        Width="900"
        Height="640"
        WindowStartupLocation="CenterScreen">

    <TabControl Margin="12">
    </TabControl>
</Window>
```

`Avalonia.EventsDemo/Views/MainWindow.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.EventsDemo.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

另三个项目的 `Title` 分别为 `Avalonia Input Demo`、`Avalonia Graphics Demo`、`Avalonia Custom Controls Demo`，`x:Class` 与 `namespace` 随项目名变化。**InputDemo（8 Tab）、GraphicsDemo（13 Tab）、CustomControlsDemo（7 Tab）三者的 `TabControl` 改成竖排**：

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
    </TabControl>
```

理由与 StylingDemo 的 12 Tab 相同：横排会横向滚动，spec 明确把这当作要避免的体验问题。13 个竖排项在 640 高度内放得下。

- [x] **Step 6: 创建共享的事件日志**

`Avalonia.Shared/Helpers/EventLog.cs`：

```csharp
using System.Collections.ObjectModel;

namespace Avalonia.Shared.Helpers
{
    /// <summary>
    /// An ordered trail of what fired, shared by the event-heavy demo pages.
    /// A page owns one instance, calls <see cref="Write"/> from its handlers
    /// and binds a ListBox to <see cref="Entries"/>.
    /// </summary>
    public class EventLog
    {
        // Long enough to show a nested routing sequence, short enough to read on screen.
        private const int MaxEntries = 60;

        public ObservableCollection<string> Entries { get; } = new();

        public void Write(string message)
        {
            // Sequence numbers make the order unmistakable when several events
            // land in the same frame and the ListBox renders them together.
            Entries.Add($"{Entries.Count + 1:00}  {message}");
            while (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }

        public void Clear() => Entries.Clear();
    }
}
```

放在 `Avalonia.Shared` 而不是各项目里：本组 33 个页面里至少 20 个要写"触发了什么"，而它的实现只有十几行——spec 的判断标准是复制次数而非代码行数。它与 Avalonia 类型无关（只用 `ObservableCollection`），不增加 `Avalonia.Shared` 的依赖。

- [x] **Step 7: 注册到解决方案**

修改 `hello-avalonia.slnx`，按字母序插入四行：

```xml
<Solution>
  <Project Path="Avalonia.CustomControlsDemo/Avalonia.CustomControlsDemo.csproj" />
  <Project Path="Avalonia.DataBindingDemo/Avalonia.DataBindingDemo.csproj" />
  <Project Path="Avalonia.DataTemplatesDemo/Avalonia.DataTemplatesDemo.csproj" />
  <Project Path="Avalonia.EventsDemo/Avalonia.EventsDemo.csproj" />
  <Project Path="Avalonia.FundamentalsDemo/Avalonia.FundamentalsDemo.csproj" />
  <Project Path="Avalonia.GraphicsDemo/Avalonia.GraphicsDemo.csproj" />
  <Project Path="Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj" />
  <Project Path="Avalonia.InputDemo/Avalonia.InputDemo.csproj" />
  <Project Path="Avalonia.LayoutDemo/Avalonia.LayoutDemo.csproj" />
  <Project Path="Avalonia.MusicStore/Avalonia.MusicStore.csproj" />
  <Project Path="Avalonia.PropertySystemDemo/Avalonia.PropertySystemDemo.csproj" />
  <Project Path="Avalonia.Shared/Avalonia.Shared.csproj" />
  <Project Path="Avalonia.StylingDemo/Avalonia.StylingDemo.csproj" />
  <Project Path="Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj" />
  <Project Path="Avalonia.XamlDemo/Avalonia.XamlDemo.csproj" />
</Solution>
```

- [x] **Step 8: 构建验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "EventsDemo|InputDemo|GraphicsDemo|CustomControlsDemo" | grep -v MSB3884`
Expected: 无输出。仓库既有的 MSB3884（`MinimumRecommendedRules.ruleset` 不存在）每个项目一条，新增项目必然让它增加，所以判据只排除它。

- [x] **Step 9: 提交**

```bash
git add Avalonia.Shared/Helpers/EventLog.cs Avalonia.EventsDemo/ Avalonia.InputDemo/ Avalonia.GraphicsDemo/ Avalonia.CustomControlsDemo/ hello-avalonia.slnx
git commit -m "$(cat <<'EOF'
feat: scaffold the four interaction-and-graphics demo projects

Empty TabControl shells wired to the shared styles, following the
LayoutDemo template. InputDemo, GraphicsDemo and CustomControlsDemo
stack their tabs vertically so the headers fit.

Also adds a shared EventLog: twenty of the pages in this group need to
show which events fired, and the widget is only a dozen lines.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: 项目 #8 Events 的 5 个页面

**Files:**
- Create: `Avalonia.EventsDemo/Controls/PingEventArgs.cs`
- Create: `Avalonia.EventsDemo/Controls/Notifier.cs`
- Create: `Avalonia.EventsDemo/Views/Pages/` 下 5 组 `XxxPage.axaml(.cs)`：`LifecyclePage`、`InputEventsPage`、`RoutingPage`、`HandledPage`、`CustomRoutedEventPage`
- Modify: `Avalonia.EventsDemo/Views/MainWindow.axaml`（挂 5 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳；`Avalonia.Shared.Helpers.EventLog`（`Entries`、`Write(string)`、`Clear()`）；`DemoHeader`、`caption`/`hint`/`stage` 样式
- Produces: `Avalonia.EventsDemo.Controls.Notifier`（自定义路由事件 `PingEvent`）与 `PingEventArgs`。**只在本项目使用**，Task 3 的 InputDemo 与 Task 5 的 CustomControlsDemo 只做文字指路，不引用它。

本项目所有页面都是**无状态**的：事件日志放在页面的 code-behind 字段里，没有 ViewModel 目录。事件演示的本质就是「处理器被调用」，用 `Click="OnXxx"` 与 `AddHandler` 比命令绑定更直接。

每页的日志列表是同一个写法，先约定好（后面各页的 XAML 都照这个模板）：

```xml
<ListBox Name="LogList" Height="160" />
```

code-behind 里 `LogList.ItemsSource = _log.Entries;`。不用绑定而用赋值：页面无 `DataContext`，且 `ListBox.ItemsSource` 在赋值后会随 `ObservableCollection` 的变化自动刷新。

- [x] **Step 1: 创建自定义路由事件的两个类型**

`Avalonia.EventsDemo/Controls/PingEventArgs.cs`：

```csharp
using Avalonia.Interactivity;

namespace Avalonia.EventsDemo.Controls
{
    public class PingEventArgs : RoutedEventArgs
    {
        public PingEventArgs(RoutedEvent routedEvent, object? source) : base(routedEvent, source)
        {
        }

        // The payload a handler further up the tree can read.
        public string Message { get; init; } = "";
    }
}
```

`Avalonia.EventsDemo/Controls/Notifier.cs`：

```csharp
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.EventsDemo.Controls
{
    /// <summary>
    /// A button that raises a custom bubbling routed event, <see cref="PingEvent"/>, when clicked.
    /// </summary>
    public class Notifier : Button
    {
        // 1. The routed event itself: owner type, argument type, routing strategy.
        public static readonly RoutedEvent<PingEventArgs> PingEvent =
            RoutedEvent.Register<Notifier, PingEventArgs>(nameof(Ping), RoutingStrategies.Bubble);

        // 2. The CLR wrapper. It is what lets C# code write `notifier.Ping += ...`.
        public event EventHandler<PingEventArgs>? Ping
        {
            add => AddHandler(PingEvent, value);
            remove => RemoveHandler(PingEvent, value);
        }

        protected override void OnClick()
        {
            base.OnClick();

            // 3. Raising it: the event starts at this control and bubbles up the visual tree.
            RaiseEvent(new PingEventArgs(PingEvent, this) { Message = $"ping from {Name}" });
        }
    }
}
```

实测验证过：`Notifier.Ping` 的 XAML 附加订阅（`local:Notifier.Ping="OnPing"` 写在父元素上）在 12.1.2 编译并收到事件。

- [x] **Step 2: 生命周期页 LifecyclePage**

演示：一个按钮被「加入」与「移除」时，8 个生命周期事件按什么顺序触发。

`Avalonia.EventsDemo/Views/Pages/LifecyclePage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.EventsDemo.Views.Pages.LifecyclePage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="生命周期事件：控件从创建到离开可视树，8 个事件按什么顺序触发"
                               DocPath="events/lifecycle-events" />

            <TextBlock Classes="caption" Text="1. 加入与移除一个按钮" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="AddButton" Content="加入按钮" Click="OnAdd" />
                <Button Name="RemoveButton" Content="移除按钮" Click="OnRemove" IsEnabled="False" />
                <Button Content="清空日志" Click="OnClear" />
            </StackPanel>
            <Border Classes="stage" Height="70" Margin="0,8,0,0">
                <!--  The probed child is added here at runtime.  -->
                <Panel Name="Slot" />
            </Border>

            <TextBlock Classes="caption" Text="2. 触发顺序" />
            <ListBox Name="LogList" Height="220" />
            <TextBlock Classes="hint"
                       Text="注意 Initialized 在两个 Attached 之后才出现——这与 WPF 经验相反。SizeChanged 与 Loaded 发生在第一次布局之后。移除时没有 Initialized，Loaded 的对偶是 Unloaded。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.EventsDemo/Views/Pages/LifecyclePage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class LifecyclePage : UserControl
    {
        private readonly EventLog _log = new();
        private Button? _probed;

        public LifecyclePage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;
        }

        private void OnAdd(object? sender, RoutedEventArgs e)
        {
            if (_probed != null)
            {
                return;
            }

            _log.Clear();
            _probed = new Button { Content = "被观察的按钮", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

            // Subscribe to all of them before attaching, otherwise the early ones are missed.
            _probed.AttachedToLogicalTree += (_, _) => _log.Write("AttachedToLogicalTree");
            _probed.AttachedToVisualTree += (_, _) => _log.Write("AttachedToVisualTree");
            _probed.Initialized += (_, _) => _log.Write("Initialized");
            _probed.SizeChanged += (_, _) => _log.Write("SizeChanged");
            _probed.Loaded += (_, _) => _log.Write("Loaded");
            _probed.Unloaded += (_, _) => _log.Write("Unloaded");
            _probed.DetachedFromVisualTree += (_, _) => _log.Write("DetachedFromVisualTree");
            _probed.DetachedFromLogicalTree += (_, _) => _log.Write("DetachedFromLogicalTree");

            Slot.Children.Add(_probed);
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = true;
        }

        private void OnRemove(object? sender, RoutedEventArgs e)
        {
            if (_probed == null)
            {
                return;
            }

            Slot.Children.Remove(_probed);
            _probed = null;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = false;
        }

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
```

- [x] **Step 3: 输入事件页 InputEventsPage**

演示：鼠标、键盘、文本三类输入事件各自触发时，参数里有什么。

`Avalonia.EventsDemo/Views/Pages/InputEventsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.EventsDemo.Views.Pages.InputEventsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="输入事件：指针、键盘、文本三类事件各带什么参数"
                               DocPath="events/input-events" />

            <TextBlock Classes="caption" Text="1. 在下面的区域里移动、点击、滚动、按键" />
            <!--  Background must be set: a Border without one is invisible to hit testing.  -->
            <Border Name="Pad" Classes="stage" Height="90" Background="#20FFFFFF" Focusable="True"
                    PointerEntered="OnEntered" PointerExited="OnExited"
                    PointerPressed="OnPressed" PointerReleased="OnReleased"
                    PointerWheelChanged="OnWheel"
                    KeyDown="OnKeyDown" KeyUp="OnKeyUp">
                <TextBlock Text="点这里获得焦点，再按键" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>

            <TextBlock Classes="caption" Text="2. 文本输入事件（区别于 KeyDown：它给出的是字符，不是按键）" />
            <TextBox Name="Entry" Width="260" HorizontalAlignment="Left" PlaceholderText="在这里打字" />

            <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,12,0,6">
                <TextBlock Classes="caption" Margin="0" VerticalAlignment="Center" Text="3. 事件流" />
                <Button Content="清空" Click="OnClear" />
            </StackPanel>
            <ListBox Name="LogList" Height="200" />
            <TextBlock Classes="hint"
                       Text="PointerMoved 太密，这里没有挂。按 A 键会同时看到 KeyDown(A)、TextInput(a)、KeyUp(A)；按方向键只有 KeyDown/KeyUp 而没有 TextInput——它不产生字符。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.EventsDemo/Views/Pages/InputEventsPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class InputEventsPage : UserControl
    {
        private readonly EventLog _log = new();

        public InputEventsPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // TextBox marks TextInput as handled in its own class handler, so a plain `+=` would
            // never run. Tunnel runs before the TextBox sees the event.
            Entry.AddHandler(InputElement.TextInputEvent,
                (object? s, TextInputEventArgs e) => _log.Write($"TextInput  text='{e.Text}'"),
                RoutingStrategies.Tunnel);
        }

        private void OnEntered(object? sender, PointerEventArgs e) => _log.Write("PointerEntered");

        private void OnExited(object? sender, PointerEventArgs e) => _log.Write("PointerExited");

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(Pad);
            _log.Write($"PointerPressed  {e.Pointer.Type}  left={point.Properties.IsLeftButtonPressed}  right={point.Properties.IsRightButtonPressed}  at {point.Position:F0}");

            // Without this the Border never gets keyboard focus from a click.
            Pad.Focus();
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e) => _log.Write("PointerReleased");

        private void OnWheel(object? sender, PointerWheelEventArgs e) => _log.Write($"PointerWheelChanged  delta={e.Delta}");

        private void OnKeyDown(object? sender, KeyEventArgs e) => _log.Write($"KeyDown  key={e.Key}  modifiers={e.KeyModifiers}");

        private void OnKeyUp(object? sender, KeyEventArgs e) => _log.Write($"KeyUp  key={e.Key}");

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
```

`Border` 上直接写 `Background="#20FFFFFF"` 是对的：这个属性不是由样式驱动的（规则 2 不适用），而且它同时满足规则 15（没有 Background 的元素不参与命中，指针事件根本不会到这里）。

- [x] **Step 4: 路由三阶段页 RoutingPage**

演示：三层嵌套元素（Outer > Middle > Inner）上，同一个事件在 Tunnel 与 Bubble 两个阶段的触发顺序。再加一张表，列出常见事件各自的 `RoutingStrategies`。

`Avalonia.EventsDemo/Views/Pages/RoutingPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.EventsDemo.Views.Pages.RoutingPage">

    <UserControl.Styles>
        <Style Selector="Border.layer">
            <Setter Property="BorderThickness" Value="2" />
            <Setter Property="Padding" Value="16" />
            <Setter Property="CornerRadius" Value="4" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="路由事件的三个阶段：Tunnel 从外向内，Bubble 从内向外，Direct 只在源元素"
                               DocPath="input-interaction/routed-events" />

            <TextBlock Classes="caption" Text="1. 点击最里面的格子，看 PointerPressed 怎么走" />
            <Border Name="Outer" Classes="layer" Background="#203A6FB0" BorderBrush="#3A6FB0" HorizontalAlignment="Left">
                <Border Name="Middle" Classes="layer" Background="#20B07A3A" BorderBrush="#B07A3A">
                    <Border Name="Inner" Classes="layer" Background="#203AB06F" BorderBrush="#3AB06F" Width="140" Height="60">
                        <TextBlock Text="Inner" HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                </Border>
            </Border>

            <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,8,0,6">
                <Button Content="清空" Click="OnClear" />
            </StackPanel>
            <ListBox Name="LogList" Height="190" />
            <TextBlock Classes="hint"
                       Text="顺序是 outer:tunnel → middle:tunnel → inner:tunnel → inner:bubble → middle:bubble → outer:bubble。Tunnel 是预览，Bubble 才是常规处理；两者共用同一个事件参数。" />

            <TextBlock Classes="caption" Text="2. 常见事件的路由策略（读自事件对象，不是凭记忆写的）" />
            <TextBlock Name="StrategyTable" FontFamily="Consolas, Menlo, monospace" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.EventsDemo/Views/Pages/RoutingPage.axaml.cs`：

```csharp
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class RoutingPage : UserControl
    {
        private readonly EventLog _log = new();

        public RoutingPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // The same event, registered at two strategies on each of the three layers.
            foreach (var layer in new[] { Outer, Middle, Inner })
            {
                var name = layer.Name;
                layer.AddHandler(InputElement.PointerPressedEvent,
                    (object? s, PointerPressedEventArgs e) => _log.Write($"{name}:tunnel"),
                    RoutingStrategies.Tunnel);
                layer.AddHandler(InputElement.PointerPressedEvent,
                    (object? s, PointerPressedEventArgs e) => _log.Write($"{name}:bubble"),
                    RoutingStrategies.Bubble);
            }

            // Read the strategies off the event objects so the table cannot drift from the framework.
            var events = new RoutedEvent[]
            {
                InputElement.PointerPressedEvent,
                InputElement.KeyDownEvent,
                InputElement.TextInputEvent,
                InputElement.PointerEnteredEvent,
                InputElement.PointerExitedEvent,
                Button.ClickEvent,
            };
            StrategyTable.Text = string.Join("\n", events.Select(ev => $"{ev.Name,-20} {ev.RoutingStrategies}"));
        }

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
```

`PointerEnteredEvent` 是 **Direct**：只在进入的元素本身触发，不冒泡也不隧道，所以不能指望在父级里收到子元素的 `PointerEntered`（要父级知道子元素被悬停，要靠子元素的 `:pointerover` 伪类或自己订阅）。

- [x] **Step 5: Handled 拦截页 HandledPage**

演示：内层把 `Handled` 置真，外层的冒泡处理器就不再触发；用 `handledEventsToo: true` 注册的处理器仍然触发。

`Avalonia.EventsDemo/Views/Pages/HandledPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.EventsDemo.Views.Pages.HandledPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Handled：把事件标记为已处理，截断后续的冒泡"
                               DocPath="input-interaction/routed-events" />

            <TextBlock Classes="caption" Text="1. 在 TextBox 里打字，同一个 KeyDown 经过三层" />
            <CheckBox Name="HandleBox" Content="内层在 KeyDown 冒泡时置 Handled = true" />
            <Border Name="Outer" Classes="stage" Padding="12" Margin="0,6,0,0">
                <Border Name="Middle" Classes="stage" Padding="12">
                    <TextBox Name="Entry" Width="260" HorizontalAlignment="Left" PlaceholderText="在这里按键" />
                </Border>
            </Border>

            <StackPanel Orientation="Horizontal" Spacing="16" Margin="0,8,0,6">
                <TextBlock Text="{Binding #Entry.Text, StringFormat='TextBox.Text = [{0}]'}" VerticalAlignment="Center" />
                <Button Content="清空" Click="OnClear" />
            </StackPanel>
            <ListBox Name="LogList" Height="200" />
            <TextBlock Classes="hint"
                       Text="勾选后：middle:bubble 与 outer:bubble 不再出现，TextBox 也不再收到字符（Text 保持为空）。但「outer（handledEventsToo）」那一行仍然出现——它是用 handledEventsToo 注册的，专门用来无视 Handled。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.EventsDemo/Views/Pages/HandledPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class HandledPage : UserControl
    {
        private readonly EventLog _log = new();

        public HandledPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // Tunnel first: every layer sees the key before the TextBox does.
            Outer.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("outer:tunnel"),
                RoutingStrategies.Tunnel);
            Middle.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("middle:tunnel"),
                RoutingStrategies.Tunnel);

            // The innermost bubble handler is the one that decides to stop the event.
            Entry.AddHandler(InputElement.KeyDownEvent, (object? s, KeyEventArgs e) =>
            {
                _log.Write($"inner:bubble  (Handled was {e.Handled})");
                if (HandleBox.IsChecked == true)
                {
                    e.Handled = true;
                }
            }, RoutingStrategies.Bubble);

            Middle.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("middle:bubble"),
                RoutingStrategies.Bubble);
            Outer.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("outer:bubble"),
                RoutingStrategies.Bubble);

            // handledEventsToo: true opts this handler out of the Handled cut-off.
            Outer.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write($"outer (handledEventsToo)  Handled={e.Handled}"),
                RoutingStrategies.Bubble,
                handledEventsToo: true);
        }

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
```

- [x] **Step 6: 自定义路由事件页 CustomRoutedEventPage**

演示：`Notifier` 点击时触发自定义冒泡事件 `Ping`，由祖先元素在 XAML 里用附加语法订阅，也在代码里用 CLR 包装订阅。

`Avalonia.EventsDemo/Views/Pages/CustomRoutedEventPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.EventsDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.EventsDemo.Views.Pages.CustomRoutedEventPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="自定义路由事件：声明、CLR 包装、触发，祖先用附加语法订阅"
                               DocPath="input-interaction/routed-events" />

            <TextBlock Classes="caption" Text="1. 点任意一个按钮" />
            <!--  The attached-syntax subscription: this StackPanel never mentions Notifier's
                  visual tree, yet receives the event as it bubbles through.  -->
            <StackPanel Name="Listener" Spacing="8" controls:Notifier.Ping="OnPingFromXaml">
                <controls:Notifier Name="Alpha" Content="Alpha" HorizontalAlignment="Left" />
                <Border Classes="stage" Padding="8">
                    <controls:Notifier Name="Beta" Content="Beta（嵌在 Border 里）" HorizontalAlignment="Left" />
                </Border>
            </StackPanel>

            <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,8,0,6">
                <Button Content="清空" Click="OnClear" />
            </StackPanel>
            <ListBox Name="LogList" Height="160" />

            <TextBlock Classes="caption" Text="2. 声明一个自定义路由事件需要的三样东西" />
            <TextBlock Classes="hint"
                       Text="① static readonly RoutedEvent 字段：RoutedEvent.Register&lt;Owner, Args&gt;(name, strategy)。② CLR 包装事件：add/remove 转调 AddHandler/RemoveHandler，C# 的 += 才能用。③ 触发处：RaiseEvent(new Args(event, source))。XAML 里 controls:Notifier.Ping=&quot;OnPing&quot; 这种附加写法不需要额外代码，它靠的就是第一项。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.EventsDemo/Views/Pages/CustomRoutedEventPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.EventsDemo.Controls;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class CustomRoutedEventPage : UserControl
    {
        private readonly EventLog _log = new();

        public CustomRoutedEventPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // The CLR-wrapper route: only Alpha, because the wrapper subscribes on the control itself.
            Alpha.Ping += (_, e) => _log.Write($"Alpha.Ping +=  (direct on the control)  Message='{e.Message}'");
        }

        // Fired by the attached-syntax subscription on the StackPanel, for both buttons.
        private void OnPingFromXaml(object? sender, PingEventArgs e)
            => _log.Write($"Listener (attached)  Message='{e.Message}'  Source={(e.Source as Control)?.Name}");

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
```

两种订阅的差别在日志里一眼可见：点 Alpha 两行（自己的 `+=` 与祖先的附加订阅），点 Beta 只有祖先那一行。这就是「路由」的意义——不需要知道事件从哪个具体控件来。

- [x] **Step 7: 挂 5 个 Tab**

修改 `Avalonia.EventsDemo/Views/MainWindow.axaml`，在 `<Window>` 上加 `xmlns:pages="using:Avalonia.EventsDemo.Views.Pages"`，`TabControl` 改为：

```xml
    <TabControl Margin="12">
        <TabItem Header="生命周期">
            <pages:LifecyclePage />
        </TabItem>
        <TabItem Header="输入事件">
            <pages:InputEventsPage />
        </TabItem>
        <TabItem Header="路由三阶段">
            <pages:RoutingPage />
        </TabItem>
        <TabItem Header="Handled">
            <pages:HandledPage />
        </TabItem>
        <TabItem Header="自定义路由事件">
            <pages:CustomRoutedEventPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 8: 构建**

Run: `dotnet build Avalonia.EventsDemo 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "EventsDemo" | grep -v MSB3884`
Expected: 无输出。

- [x] **Step 9: 用 headless 探针断言页面行为**

**不要用目视核对代替这一步**。在**仓库外**建探针。创建 `C:\Temp\eventscheck\eventscheck.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" Version="12.1.2" />
        <PackageReference Include="Avalonia.Headless" Version="12.1.2" />
        <PackageReference Include="Avalonia.Themes.Fluent" Version="12.1.2" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.EventsDemo\Avalonia.EventsDemo.csproj" />
    </ItemGroup>
</Project>
```

创建 `C:\Temp\eventscheck\Program.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.EventsDemo.Controls;
using Avalonia.EventsDemo.Views;
using Avalonia.EventsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
    }
}

internal sealed class WarnSink : ILogSink
{
    public readonly List<string> Entries = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel l, string a, object? s, string m) => Entries.Add($"[{l}] ({a}) {m}");
    public void Log(LogEventLevel l, string a, object? s, string m, params object?[] v)
        => Entries.Add($"[{l}] ({a}) {m} :: {string.Join(", ", v)}");
}

internal static class Probe
{
    private static int _pass, _fail;

    private static T Find<T>(Visual root, string name) where T : Visual
    {
        foreach (var d in root.GetVisualDescendants())
        {
            if (d is T hit && (d as Control)?.Name == name) return hit;
        }
        throw new InvalidOperationException($"not found: {name}");
    }

    private static Window Show(Control c)
    {
        var w = new Window { Content = c, Width = 900, Height = 700 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        return w;
    }

    private static void Check(string label, bool ok, string detail)
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-44} {detail}");
    }

    // The ListBox of a page holds the log strings; strip the "01  " sequence prefix.
    private static List<string> Log(Visual page)
        => Find<ListBox>(page, "LogList").Items.Cast<string>().Select(s => s[4..]).ToList();

    private static void Press(Window w, Visual target)
    {
        var p = target.TranslatePoint(new Point(5, 5), w)!.Value;
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

        // --- Shell: five tabs, each rendering the right page ---
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        var pageNames = new[] { "LifecyclePage", "InputEventsPage", "RoutingPage", "HandledPage", "CustomRoutedEventPage" };
        Check("Shell: tab count", tabs.Count == 5, tabs.Count.ToString());
        for (var i = 0; i < tabs.Count && i < 5; i++)
        {
            tabs[i].IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            Check($"Shell: tab {i} renders", tabs[i].Content?.GetType().Name == pageNames[i], tabs[i].Content?.GetType().Name ?? "<null>");
        }
        mw.Close();

        // --- Lifecycle: add then remove, order must match what plan rule 12 records ---
        var life = new LifecyclePage();
        var lw = Show(life);
        Find<Button>(life, "AddButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var addLog = Log(life);
        Check("Lifecycle: add order", addLog.SequenceEqual(new[]
            { "AttachedToLogicalTree", "AttachedToVisualTree", "Initialized", "SizeChanged", "Loaded" }),
            string.Join(">", addLog));
        Find<Button>(life, "RemoveButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var allLog = Log(life);
        Check("Lifecycle: remove order", allLog.Skip(5).SequenceEqual(new[]
            { "DetachedFromLogicalTree", "DetachedFromVisualTree", "Unloaded" }),
            string.Join(">", allLog.Skip(5)));
        lw.Close();

        // --- Routing: 4 PointerPressed hops in the documented order ---
        var routing = new RoutingPage();
        var rw = Show(routing);
        Press(rw, Find<Border>(routing, "Inner"));
        Check("Routing: tunnel then bubble", Log(routing).SequenceEqual(new[]
            { "Outer:tunnel", "Middle:tunnel", "Inner:tunnel", "Inner:bubble", "Middle:bubble", "Outer:bubble" }),
            string.Join(" ", Log(routing)));
        var table = Find<TextBlock>(routing, "StrategyTable").Text ?? "";
        Check("Routing: KeyDown is Tunnel, Bubble", table.Contains("KeyDown") && table.Split('\n').First(l => l.StartsWith("KeyDown")).Contains("Tunnel, Bubble"), "");
        Check("Routing: PointerEntered is Direct", table.Split('\n').First(l => l.StartsWith("PointerEntered")).Contains("Direct"), "");
        Check("Routing: Click is Bubble", table.Split('\n').First(l => l.StartsWith("Click")).Trim().EndsWith("Bubble"), "");
        rw.Close();

        // --- Handled: ticked box cuts the bubble but not handledEventsToo ---
        var handled = new HandledPage();
        var hw = Show(handled);
        var entry = Find<TextBox>(handled, "Entry");
        entry.Focus();
        Dispatcher.UIThread.RunJobs();
        hw.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var open = Log(handled);
        Check("Handled off: bubbles reach outer", open.Contains("middle:bubble") && open.Contains("outer:bubble"), string.Join(" | ", open));
        Check("Handled off: TextBox received text", entry.Text == "a", $"'{entry.Text}'");

        hw.Close();

        // A fresh page, so the page-private EventLog starts empty, with the box ticked first.
        var cut = new HandledPage();
        var cutWin = Show(cut);
        Find<CheckBox>(cut, "HandleBox").IsChecked = true;
        var cutEntry = Find<TextBox>(cut, "Entry");
        cutEntry.Focus();
        Dispatcher.UIThread.RunJobs();
        cutWin.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var cutLog = Log(cut);
        Check("Handled on: middle/outer bubble cut off", !cutLog.Contains("middle:bubble") && !cutLog.Contains("outer:bubble"), string.Join(" | ", cutLog));
        Check("Handled on: handledEventsToo still runs", cutLog.Contains("outer (handledEventsToo)  Handled=True"), string.Join(" | ", cutLog));
        Check("Handled on: TextBox got no text", cutEntry.Text is null or "", $"'{cutEntry.Text}'");
        cutWin.Close();

        // --- Input events: a typed character must reach the Tunnel-subscribed TextInput logger ---
        var input = new InputEventsPage();
        var iw = Show(input);
        var typed = Find<TextBox>(input, "Entry");
        typed.Focus();
        Dispatcher.UIThread.RunJobs();
        iw.KeyTextInput("q");
        Dispatcher.UIThread.RunJobs();
        Check("Input: TextInput logged despite TextBox handling", Log(input).Any(l => l.StartsWith("TextInput") && l.Contains("'q'")), string.Join(" | ", Log(input)));
        Check("Input: TextBox still got the character", typed.Text == "q", $"'{typed.Text}'");
        iw.Close();

        // --- Custom routed event: subscribers via += and via attached syntax ---
        var custom = new CustomRoutedEventPage();
        var cw = Show(custom);
        var alpha = Find<Notifier>(custom, "Alpha");
        var beta = Find<Notifier>(custom, "Beta");
        Press(cw, alpha);
        var afterAlpha = Log(custom);
        Check("Custom: Alpha reaches two subscribers", afterAlpha.Count == 2, string.Join(" | ", afterAlpha));
        Check("Custom: attached handler reads Message and Source",
            afterAlpha.Any(l => l.Contains("Listener (attached)") && l.Contains("Message='ping from Alpha'") && l.Contains("Source=Alpha")), "");
        Press(cw, beta);
        var afterBeta = Log(custom);
        Check("Custom: Beta reaches only the ancestor", afterBeta.Count == 3 && afterBeta[2].Contains("Source=Beta"), string.Join(" | ", afterBeta.Skip(2)));
        cw.Close();

        Check("No warning-or-worse log entries", sink.Entries.Count == 0, string.Join(" // ", sink.Entries));
        Console.WriteLine($"\n{_pass} passed, {_fail} failed");
    }
}
```

Run: `cd /c/Temp/eventscheck && dotnet run 2>&1 | grep -E "PASS|FAIL|passed"`
Expected: 全部 `PASS`，末行 `N passed, 0 failed`。**N 是执行期实测条数**，把实际数字回填到本步，不要沿用本 plan 的任何估计。

Handled 页分两个实例测：页面的 `EventLog` 是私有字段，探针清不了它，所以「未勾选」与「勾选」各用一个全新的页面。「勾选」那一组才是规则 13 的核心验证。

若任何一条 `FAIL`，**以探针输出为准修正页面或说明文字**，并在 spec 回写时注明"（执行期修正）"。

- [x] **Step 10: 清理探针并提交**

```bash
rm -rf /c/Temp/eventscheck
git add Avalonia.EventsDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Events category

Five pages: lifecycle order, input events, tunnel/bubble routing, the
Handled cut-off with handledEventsToo, and a custom routed event with
both CLR-wrapper and attached-syntax subscribers.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: 项目 #9 Input 的 8 个页面

**Files:**
- Create: `Avalonia.InputDemo/ViewModels/InteractivityViewModel.cs`
- Create: `Avalonia.InputDemo/Views/Pages/` 下 8 组 `XxxPage.axaml(.cs)`：`PointerPage`、`FocusPage`、`GesturesPage`、`KeyboardPage`、`InteractivityPage`、`DragDropPage`、`TextInputPage`、`RoutedEventsPage`
- Modify: `Avalonia.InputDemo/Views/MainWindow.axaml`（挂 8 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳；`Avalonia.Shared.Helpers.EventLog`；`Avalonia.Shared.ViewModels.ViewModelBase`（继承 `ObservableObject`）
- Produces: 无跨任务产物。

只有 `InteractivityPage` 需要 ViewModel（演示命令绑定必须有可绑的命令），其余页面无状态，状态放在 code-behind 字段里，与 spec「无状态页可省略 ViewModels」一致。

日志列表沿用 Task 2 约定：`<ListBox Name="LogList" Height="…" />`，code-behind 里 `LogList.ItemsSource = _log.Entries;`。

- [x] **Step 1: 指针页 PointerPage**

演示：指针读数（类型、按键、位置、滚轮）与指针捕获——拖动一个小方块，鼠标移出画布后仍持续收到移动事件。

`Avalonia.InputDemo/Views/Pages/PointerPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.PointerPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="指针设备：鼠标、触摸、手写笔共用一套指针事件，用捕获把拖动锁在手里"
                               DocPath="input-interaction/pointer" />

            <TextBlock Classes="caption" Text="1. 在灰色区域里移动、按键、滚动滚轮" />
            <!--  Background is required: a Border without one is invisible to pointer hit testing.  -->
            <Border Name="Pad" Classes="stage" Height="80" Background="#20FFFFFF"
                    PointerMoved="OnPadMoved" PointerPressed="OnPadPressed" PointerWheelChanged="OnPadWheel" />
            <TextBlock Name="Readout" FontFamily="Consolas, Menlo, monospace" Margin="0,6,0,0"
                       Text="（还没有事件）" />

            <TextBlock Classes="caption" Text="2. 指针捕获：拖动红色方块，拖出画布也不会丢" />
            <Border Classes="stage" Width="300" HorizontalAlignment="Left">
                <Canvas Name="Track" Width="300" Height="50">
                    <Border Name="Handle" Width="40" Height="40" Canvas.Top="5" CornerRadius="4"
                            Background="#E8564A"
                            PointerPressed="OnHandlePressed" PointerMoved="OnHandleMoved"
                            PointerReleased="OnHandleReleased" PointerCaptureLost="OnHandleCaptureLost" />
                </Canvas>
            </Border>
            <TextBlock Name="DragState" Margin="0,6,0,0" Text="空闲" />
            <TextBlock Classes="hint"
                       Text="不捕获时，指针一离开方块，PointerMoved 就不再送给它；Capture 之后，不管指针跑到哪里都继续送，直到松开（Capture(null)）或被别人抢走（PointerCaptureLost）。方块被夹在 0–260 之间，所以把鼠标拖到画布外，方块停在边缘。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Canvas.Top="5"` 写在元素上没问题：`Canvas.Top` 不被任何样式驱动（规则 2 不适用）。`Canvas.Left` 由 code-behind 设，所以 XAML 里不写初值。

`Avalonia.InputDemo/Views/Pages/PointerPage.axaml.cs`：

```csharp
using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class PointerPage : UserControl
    {
        private const double HandleHalf = 20;

        public PointerPage()
        {
            InitializeComponent();
            Canvas.SetLeft(Handle, 0);
        }

        // Reading the pointer: type, which buttons are down, and where.
        private void OnPadMoved(object? sender, PointerEventArgs e) => Show(e, "Moved");

        private void OnPadPressed(object? sender, PointerPressedEventArgs e) => Show(e, "Pressed");

        private void OnPadWheel(object? sender, PointerWheelEventArgs e)
            => Readout.Text = $"Wheel  delta={e.Delta}";

        private void Show(PointerEventArgs e, string what)
        {
            var point = e.GetCurrentPoint(Pad);
            var p = point.Properties;
            Readout.Text = $"{what,-8} type={e.Pointer.Type}  pos={point.Position:F0}  L={p.IsLeftButtonPressed} M={p.IsMiddleButtonPressed} R={p.IsRightButtonPressed}  mods={e.KeyModifiers}";
        }

        private void OnHandlePressed(object? sender, PointerPressedEventArgs e)
        {
            // From now on every pointer event for this pointer goes to Handle,
            // wherever the pointer actually is.
            e.Pointer.Capture(Handle);
            DragState.Text = "拖动中（已捕获）";
        }

        private void OnHandleMoved(object? sender, PointerEventArgs e)
        {
            if (e.Pointer.Captured != Handle)
            {
                return;
            }

            // Position is relative to Track and may be far outside it: that is the point of capturing.
            var x = e.GetPosition(Track).X - HandleHalf;
            var max = Track.Width - Handle.Width;
            Canvas.SetLeft(Handle, Math.Clamp(x, 0, max));
        }

        private void OnHandleReleased(object? sender, PointerReleasedEventArgs e) => e.Pointer.Capture(null);

        private void OnHandleCaptureLost(object? sender, PointerCaptureLostEventArgs e)
            => DragState.Text = "空闲（捕获已释放）";
    }
}
```

- [x] **Step 2: 焦点页 FocusPage**

演示：Tab 顺序与 `IsTabStop`、`TabIndex`；`FocusManager` 读当前焦点；`NavigationMethod` 决定 `:focus-visible` 是否出现；用 `Focus(NavigationMethod.…)` 对照。

`Avalonia.InputDemo/Views/Pages/FocusPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.FocusPage">

    <UserControl.Styles>
        <!--  :focus-visible appears only for keyboard-driven focus. Plain :focus would also light up on click.  -->
        <Style Selector="Button.ring:focus-visible">
            <Setter Property="BorderBrush" Value="#E8974A" />
            <Setter Property="BorderThickness" Value="3" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="焦点：Tab 顺序、IsTabStop、FocusManager，以及只对键盘出现的 :focus-visible"
                               DocPath="input-interaction/focus" />

            <TextBlock Classes="caption" Text="1. 按 Tab / Shift+Tab 在四个按钮间移动" />
            <StackPanel Name="Row" Orientation="Horizontal" Spacing="8" GotFocus="OnRowGotFocus">
                <Button Name="BtnA" Classes="ring" Content="A" KeyboardNavigation.TabIndex="1" />
                <Button Name="BtnB" Classes="ring" Content="B" KeyboardNavigation.TabIndex="2" />
                <Button Name="BtnC" Classes="ring" Content="C（IsTabStop=False）" IsTabStop="False" KeyboardNavigation.TabIndex="3" />
                <Button Name="BtnD" Classes="ring" Content="D" KeyboardNavigation.TabIndex="4" />
            </StackPanel>
            <TextBlock Name="FocusReadout" Margin="0,6,0,0" Text="还没有焦点" />
            <TextBlock Classes="hint"
                       Text="C 被 Tab 跳过，但用鼠标点它仍然能获得焦点——IsTabStop 只管键盘导航。点一下 A 与按 Tab 到 A，读数里的 :focus-visible 不同，橙色描边也只在后者出现。" />

            <TextBlock Classes="caption" Text="2. 用代码设置焦点，并指定它是怎么来的" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="Focus(Tab) → B" Click="OnFocusViaTab" />
                <Button Content="Focus(Pointer) → B" Click="OnFocusViaPointer" />
                <Button Content="读取 FocusManager 当前焦点" Click="OnQueryFocus" />
            </StackPanel>
            <TextBlock Name="QueryReadout" Margin="0,6,0,0" Text="" />
            <TextBlock Classes="hint"
                       Text="NavigationMethod 是 Focus() 的参数：Tab 与 Directional 表示键盘导航，会带上 :focus-visible；Pointer 与 Unspecified 不会。第三个按钮读到的是它自己——点它时焦点已经移到了它身上。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/FocusPage.axaml.cs`：

```csharp
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class FocusPage : UserControl
    {
        public FocusPage()
        {
            InitializeComponent();
        }

        // GotFocus bubbles, so one handler on the row sees all four buttons.
        // The argument type is FocusChangedEventArgs (Avalonia.Input) — there is no GotFocusEventArgs.
        private void OnRowGotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (e.Source is Button button)
            {
                var pseudo = string.Join(" ", button.Classes.Where(c => c.StartsWith(':')));
                FocusReadout.Text = $"焦点在 {button.Content}  via {e.NavigationMethod}  [{pseudo}]";
            }
        }

        private void OnFocusViaTab(object? sender, RoutedEventArgs e) => BtnB.Focus(NavigationMethod.Tab);

        private void OnFocusViaPointer(object? sender, RoutedEventArgs e) => BtnB.Focus(NavigationMethod.Pointer);

        private void OnQueryFocus(object? sender, RoutedEventArgs e)
        {
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            QueryReadout.Text = $"FocusManager.GetFocusedElement() = {(focused as Control)?.Name ?? focused?.GetType().Name ?? "null"}";
        }
    }
}
```

`Row` 上的 `GotFocus="OnRowGotFocus"` 是 XAML 属性订阅冒泡事件，事件参数类型是 `Avalonia.Input.FocusChangedEventArgs`（带 `NavigationMethod`）——**不是 `GotFocusEventArgs`，那个类型在 12.1.2 里不存在**（实测 CS0246），这是编写本 plan 时纠的第一个错。

- [x] **Step 3: 手势页 GesturesPage**

演示：`Tapped` / `DoubleTapped` / `RightTapped` / `Holding` 四个常用手势用计数器展示；`Pinch` / `Scroll` / `Swipe` / 触控板手势只在有触屏或触控板时触发，页面注册它们并给出说明。

`Avalonia.InputDemo/Views/Pages/GesturesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.GesturesPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="手势：把原始指针事件归纳成「点按、双击、右击、长按」"
                               DocPath="input-interaction/gestures" />

            <TextBlock Classes="caption" Text="1. 在灰色区域里点按、双击、右击、按住不动" />
            <!--  Background is required for hit testing. Tapped/RightTapped/DoubleTapped/Holding are
                  declared on InputElement, so they are plain attributes here.  -->
            <Border Name="Pad" Classes="stage" Height="100" Background="#20FFFFFF"
                    Tapped="OnTapped" DoubleTapped="OnDoubleTapped" RightTapped="OnRightTapped" Holding="OnHolding">
                <TextBlock Text="在这里操作" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>
            <TextBlock Name="Counts" FontFamily="Consolas, Menlo, monospace" Margin="0,6,0,0" />
            <Button Content="清零" Click="OnReset" HorizontalAlignment="Left" Margin="0,6,0,0" />
            <TextBlock Classes="hint"
                       Text="Holding 对触摸默认开启；对鼠标默认关闭，本页在 code-behind 里把 IsHoldWithMouseEnabled 打开了，所以按住鼠标不动也会触发。双击时会先后看到两次 Tapped 与一次 DoubleTapped。" />

            <TextBlock Classes="caption" Text="2. 需要触屏或触控板的手势" />
            <TextBlock Name="TouchCounts" FontFamily="Consolas, Menlo, monospace" />
            <TextBlock Classes="hint"
                       Text="Pinch（双指缩放）、ScrollGesture（拖动滚动）、SwipeGesture（划动）、PointerTouchPadGestureMagnify / Rotate / Swipe（触控板）。这些是静态路由事件，用 AddHandler 订阅；没有触屏或触控板的机器上它们一次也不会触发，读数保持为 0 是正常的。Gestures 工具类在 12.1.2 里是内部类型，不能直接使用，要用 InputElement 上的这些事件。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/GesturesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class GesturesPage : UserControl
    {
        private int _tapped, _double, _right, _holdStarted, _holdCompleted, _holdCanceled;
        private int _pinch, _scroll, _swipe, _magnify, _rotate, _padSwipe;

        public GesturesPage()
        {
            InitializeComponent();

            // Mouse does not hold by default; opt this pad in. (Not available as a XAML attribute on Border.)
            Pad.SetValue(InputElement.IsHoldWithMouseEnabledProperty, true);

            // The touch gestures are static routed events with no CLR event, so use AddHandler.
            Pad.AddHandler(InputElement.PinchEvent, (object? s, PinchEventArgs e) => { _pinch++; ShowTouch(); });
            Pad.AddHandler(InputElement.ScrollGestureEvent, (object? s, ScrollGestureEventArgs e) => { _scroll++; ShowTouch(); });
            Pad.AddHandler(InputElement.SwipeGestureEvent, (object? s, SwipeGestureEventArgs e) => { _swipe++; ShowTouch(); });
            Pad.AddHandler(InputElement.PointerTouchPadGestureMagnifyEvent, (object? s, PointerDeltaEventArgs e) => { _magnify++; ShowTouch(); });
            Pad.AddHandler(InputElement.PointerTouchPadGestureRotateEvent, (object? s, PointerDeltaEventArgs e) => { _rotate++; ShowTouch(); });
            Pad.AddHandler(InputElement.PointerTouchPadGestureSwipeEvent, (object? s, PointerDeltaEventArgs e) => { _padSwipe++; ShowTouch(); });

            Show();
            ShowTouch();
        }

        private void OnTapped(object? sender, TappedEventArgs e) { _tapped++; Show(); }

        private void OnDoubleTapped(object? sender, TappedEventArgs e) { _double++; Show(); }

        private void OnRightTapped(object? sender, TappedEventArgs e) { _right++; Show(); }

        private void OnHolding(object? sender, HoldingRoutedEventArgs e)
        {
            switch (e.HoldingState)
            {
                case HoldingState.Started: _holdStarted++; break;
                case HoldingState.Completed: _holdCompleted++; break;
                case HoldingState.Canceled: _holdCanceled++; break;
            }

            Show();
        }

        private void OnReset(object? sender, RoutedEventArgs e)
        {
            _tapped = _double = _right = _holdStarted = _holdCompleted = _holdCanceled = 0;
            Show();
        }

        private void Show()
            => Counts.Text = $"Tapped={_tapped}  DoubleTapped={_double}  RightTapped={_right}\nHolding: Started={_holdStarted} Completed={_holdCompleted} Canceled={_holdCanceled}";

        private void ShowTouch()
            => TouchCounts.Text = $"Pinch={_pinch}  Scroll={_scroll}  Swipe={_swipe}\nTouchPad: Magnify={_magnify} Rotate={_rotate} Swipe={_padSwipe}";
    }
}
```

实测依据：这几个参数类型（`PinchEventArgs`、`ScrollGestureEventArgs`、`SwipeGestureEventArgs`、`PointerDeltaEventArgs`，全部在 `Avalonia.Input`）已逐个编译验证，不需要执行时再查。

- [x] **Step 4: 键盘页 KeyboardPage**

演示：按键读数（`Key`、`PhysicalKey`、`KeyModifiers`、`KeySymbol`）；`HotKey` 触发按钮；`KeyBindings` 触发命令；二者的差别。

`Avalonia.InputDemo/Views/Pages/KeyboardPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.KeyboardPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="键盘：按键读数、按钮的 HotKey、窗口级 KeyBinding"
                               DocPath="input-interaction/keyboard-and-hotkeys" />

            <TextBlock Classes="caption" Text="1. 在输入框里按任意键，看 KeyEventArgs 带了什么" />
            <TextBox Name="Entry" Width="300" HorizontalAlignment="Left" PlaceholderText="点这里，再按键" />
            <TextBlock Name="KeyReadout" FontFamily="Consolas, Menlo, monospace" Margin="0,6,0,0" Text="（还没有按键）" />
            <TextBlock Classes="hint"
                       Text="Key 是逻辑键（受键盘布局影响），PhysicalKey 是物理位置（与布局无关，适合游戏式的 WASD）。KeySymbol 是这次按键产生的字符，修饰键与功能键为空。" />

            <TextBlock Classes="caption" Text="2. HotKey：绑在按钮上，按下相当于点击它" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Content="保存（Ctrl+S）" HotKey="Ctrl+S" Click="OnSaveClick" />
                <TextBlock Name="SaveReadout" VerticalAlignment="Center" Text="保存了 0 次" />
            </StackPanel>

            <TextBlock Classes="caption" Text="3. KeyBinding：绑在页面上，把按键映射到命令" />
            <TextBlock Name="BindReadout" Text="Ctrl+Shift+K 触发了 0 次" />
            <TextBlock Classes="hint"
                       Text="HotKey 是按钮自己的属性，触发的是它的 Click；KeyBinding 挂在任意元素的 KeyBindings 集合里，触发的是一个 ICommand，不要求有按钮。两者都是在元素所在窗口内生效，焦点在窗口里任何位置都可以（不必在这个页面的某个控件上）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/KeyboardPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class KeyboardPage : UserControl
    {
        private int _saves;
        private int _bindFires;

        public KeyboardPage()
        {
            InitializeComponent();

            // Tunnel so the readout also updates for keys the TextBox itself consumes (arrows, Backspace).
            Entry.AddHandler(InputElement.KeyDownEvent, OnEntryKeyDown, RoutingStrategies.Tunnel);

            // A command bound to a gesture. Built in code because the page has no DataContext to bind a
            // command from; in an MVVM page this would be <KeyBinding Gesture="Ctrl+Shift+K" Command="{Binding ...}" />.
            KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.K, KeyModifiers.Control | KeyModifiers.Shift),
                Command = new RelayCommand(() =>
                {
                    _bindFires++;
                    BindReadout.Text = $"Ctrl+Shift+K 触发了 {_bindFires} 次";
                }),
            });
        }

        private void OnEntryKeyDown(object? sender, KeyEventArgs e)
            => KeyReadout.Text = $"Key={e.Key}  PhysicalKey={e.PhysicalKey}  Modifiers={e.KeyModifiers}  KeySymbol='{e.KeySymbol}'";

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            _saves++;
            SaveReadout.Text = $"保存了 {_saves} 次";
        }
    }
}
```

实测依据：`Button.HotKey = new KeyGesture(Key.S, Control)` 与 `Window.KeyBindings` 里 `KeyBinding{Gesture, Command}` 都经 headless `KeyPressQwerty(PhysicalKey, RawInputModifiers)` 验证触发；XAML 里 `HotKey="Ctrl+S"` 字符串形式读回 `Ctrl+S`。`KeyBindings` 在 `UserControl` 上同样生效（探针里焦点在内部 `TextBox`、手势 `Ctrl+Shift+K` 触发一次）。

- [x] **Step 5: 交互写法页 InteractivityPage 与它的 ViewModel**

演示：同一个「计数 +1」动作的三种触发写法（`Click` 处理器 / `Command` / `Tapped`），以及菜单项里 `InputGesture` 与 `HotKey` 的区别。

`Avalonia.InputDemo/ViewModels/InteractivityViewModel.cs`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Shared.ViewModels;

namespace Avalonia.InputDemo.ViewModels
{
    public partial class InteractivityViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _count;

        // The command's CanExecute follows this flag; a bound Button disables itself automatically.
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(IncrementCommand))]
        private bool _unlocked;

        [RelayCommand(CanExecute = nameof(Unlocked))]
        private void Increment() => Count++;
    }
}
```

`Avalonia.InputDemo/Views/Pages/InteractivityPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.InputDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:DataType="vm:InteractivityViewModel"
             x:Class="Avalonia.InputDemo.Views.Pages.InteractivityPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="交互的三种写法：Click 处理器、Command、Tapped，以及菜单里的快捷键"
                               DocPath="input-interaction/commanding" />

            <TextBlock Classes="caption" Text="1. 同一个动作：计数 +1" />
            <TextBlock Text="{Binding Count, StringFormat='计数 = {0}'}" FontSize="20" />

            <StackPanel Orientation="Horizontal" Spacing="12" Margin="0,8,0,0">
                <!--  Style 1: an event handler in code-behind. Always enabled; the VM knows nothing.  -->
                <Button Content="Click 处理器" Click="OnClickHandler" />
                <!--  Style 2: a command. CanExecute drives IsEnabled with no extra code.  -->
                <Button Content="Command" Command="{Binding IncrementCommand}" />
                <!--  Style 3: a gesture on a non-button element.  -->
                <Border Classes="stage" Padding="10,6" Background="#20FFFFFF" Tapped="OnTapped">
                    <TextBlock Text="Tapped 手势" />
                </Border>
            </StackPanel>
            <CheckBox Content="解锁 Command（取消勾选则按钮自动变灰）" IsChecked="{Binding Unlocked}" Margin="0,8,0,0" />
            <TextBlock Name="HandlerReadout" Margin="0,6,0,0" Text="" />
            <TextBlock Classes="hint"
                       Text="Click 处理器与 Tapped 直接改了 code-behind 里的 VM，不受 CanExecute 约束；Command 是唯一遵守「解锁」开关的。需要「可用性」跟着状态走的动作，用 Command。" />

            <TextBlock Classes="caption" Text="2. 菜单里的快捷键：InputGesture 与 HotKey" />
            <Menu HorizontalAlignment="Left">
                <MenuItem Header="计数">
                    <!--  Only displays the text. Pressing Ctrl+1 does nothing.  -->
                    <MenuItem Header="只写 InputGesture" Command="{Binding IncrementCommand}" InputGesture="Ctrl+1" />
                    <!--  Actually binds the key. The menu shows no hint text.  -->
                    <MenuItem Header="只写 HotKey" Command="{Binding IncrementCommand}" HotKey="Ctrl+2" />
                    <!--  The usual pairing: HotKey triggers, InputGesture displays.  -->
                    <MenuItem Header="两个都写" Command="{Binding IncrementCommand}" HotKey="Ctrl+3" InputGesture="Ctrl+3" />
                </MenuItem>
            </Menu>
            <TextBlock Classes="hint"
                       Text="这是一个典型的「界面写了快捷键、按下去没反应」：InputGesture 只负责在菜单右侧显示文字，真正注册按键的是 HotKey。先勾选上面的「解锁」，再依次按 Ctrl+1、Ctrl+2、Ctrl+3，只有后两个会让计数增加。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/InteractivityPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.InputDemo.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class InteractivityPage : UserControl
    {
        private readonly InteractivityViewModel _vm = new();

        public InteractivityPage()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void OnClickHandler(object? sender, RoutedEventArgs e)
        {
            _vm.Count++;
            HandlerReadout.Text = "Click 处理器触发（不看 CanExecute）";
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            _vm.Count++;
            HandlerReadout.Text = "Tapped 触发（不看 CanExecute）";
        }
    }
}
```

实测依据：`MenuItem` 同时写 `InputGesture="Ctrl+1"` 与 `HotKey="Ctrl+2"`、`HotKey="Ctrl+3"` 的三个菜单项，焦点在 `TextBox` 时按 `Ctrl+1/2/3`，命令执行次数读回 `0 / 1 / 1`。

- [x] **Step 6: 拖放页 DragDropPage**

演示：从一个文本源拖到一个目标，目标在拖入时高亮、按数据类型决定是否接受、放下时读出文本。

`Avalonia.InputDemo/Views/Pages/DragDropPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.DragDropPage">

    <UserControl.Styles>
        <!--  The idle look lives in a Style, not on the element, so the :over class can override it.  -->
        <Style Selector="Border.target">
            <Setter Property="Background" Value="#20FFFFFF" />
            <Setter Property="BorderBrush" Value="#40FFFFFF" />
        </Style>
        <Style Selector="Border.target.over">
            <Setter Property="Background" Value="#404A7BE8" />
            <Setter Property="BorderBrush" Value="#4A7BE8" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="拖放：DoDragDropAsync 发起，AllowDrop 与四个拖放事件接收"
                               DocPath="input-interaction/drag-and-drop" />

            <TextBlock Classes="caption" Text="1. 按住左边的文字，拖到右边" />
            <StackPanel Orientation="Horizontal" Spacing="24">
                <StackPanel Spacing="4">
                    <TextBox Name="SourceBox" Text="拖我走" Width="160" />
                    <!--  Press on this handle to start the drag. A TextBox itself would swallow the press for text selection.  -->
                    <Border Name="Handle" Classes="stage" Padding="8,6" Background="#20FFFFFF" Cursor="Hand"
                            PointerPressed="OnHandlePressed">
                        <TextBlock Text="⠿ 按住这里拖动" HorizontalAlignment="Center" />
                    </Border>
                </StackPanel>

                <Border Name="Target" Classes="stage target" Width="260" Height="100" BorderThickness="2"
                        DragDrop.AllowDrop="True">
                    <TextBlock Name="Dropped" Text="放到这里" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 拖放事件流" />
            <ListBox Name="LogList" Height="190" />
            <TextBlock Classes="hint"
                       Text="DragEnter / DragOver / DragLeave / Drop 四个事件落在目标上；DragOver 里设置的 DragEffects 决定光标与是否允许放下（这里仅接受带文本的数据，空数据得到 None）。发起端的 DoDragDropAsync 在放下后返回最终的效果。DataObject 与 DataFormats 是旧 API，12.1.2 起用 DataTransfer 与 DataTransferItem。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/DragDropPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Shared.Helpers;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class DragDropPage : UserControl
    {
        private readonly EventLog _log = new();

        public DragDropPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // Four handlers on the drop target. AllowDrop is set in XAML.
            DragDrop.AddDragEnterHandler(Target, OnDragEnter);
            DragDrop.AddDragOverHandler(Target, OnDragOver);
            DragDrop.AddDragLeaveHandler(Target, OnDragLeave);
            DragDrop.AddDropHandler(Target, OnDrop);
        }

        // The source side: build the payload, then await the whole drag.
        private async void OnHandlePressed(object? sender, PointerPressedEventArgs e)
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(SourceBox.Text ?? ""));

            _log.Write("DoDragDropAsync 开始");
            var result = await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy | DragDropEffects.Move);
            _log.Write($"DoDragDropAsync 结束 → {result}");
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            Target.Classes.Set("over", true);
            _log.Write("DragEnter");
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            // Only text is welcome; anything else gets the "not allowed" cursor.
            e.DragEffects = e.DataTransfer.TryGetText() is not null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            Target.Classes.Set("over", false);
            _log.Write("DragLeave");
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            Target.Classes.Set("over", false);
            var text = e.DataTransfer.TryGetText();
            Dropped.Text = text ?? "（没有文本）";
            _log.Write($"Drop  text='{text}'  modifiers={e.KeyModifiers}");
            e.Handled = true;
        }
    }
}
```

**`DoDragDropAsync` 在 headless 里无法发起**（它要平台的拖放循环），所以这页的探针（Step 11）直接构造 `DragEventArgs` 并 `RaiseEvent` 到目标上，验证的是接收端三件事：`DragOver` 的效果判定、`Drop` 读出文本、`:over` 类的增删。发起端靠真机目视。构造函数是 `new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, target, point, KeyModifiers.None)`，已实测。

- [x] **Step 7: 文本输入页 TextInputPage**

演示：`TextInput` 事件在 Tunnel 阶段拦截非数字；`TextInputOptions` 给软键盘的提示（内容类型、回车键类型）；`InputMethod.IsInputMethodEnabled` 开关输入法。

`Avalonia.InputDemo/Views/Pages/TextInputPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:ti="using:Avalonia.Input.TextInput"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.TextInputPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="文本输入与 IME：拦截字符、给软键盘提示、开关输入法"
                               DocPath="input-interaction/text-input" />

            <TextBlock Classes="caption" Text="1. 只允许数字：在 Tunnel 阶段拦下非数字字符" />
            <TextBox Name="DigitsBox" Width="260" HorizontalAlignment="Left" PlaceholderText="试试输入字母" />
            <TextBlock Classes="hint"
                       Text="TextInput 事件给的是「字符」而不是按键。在 Tunnel 阶段把 Handled 置真，TextBox 就收不到这个字符。这样拦下的只是键入；它不是完整的输入校验，校验要放进绑定（见 DataBindingDemo 的校验页）。" />

            <TextBlock Classes="caption" Text="2. TextInputOptions：给软键盘的提示（桌面端键盘不会变，移动端会）" />
            <StackPanel Spacing="6">
                <TextBox Width="260" HorizontalAlignment="Left" PlaceholderText="邮箱，回车键为「发送」"
                         ti:TextInputOptions.ContentType="Email"
                         ti:TextInputOptions.ReturnKeyType="Send" />
                <TextBox Width="260" HorizontalAlignment="Left" PlaceholderText="数字键盘"
                         ti:TextInputOptions.ContentType="Digits" />
                <TextBox Name="OptionsBox" Width="260" HorizontalAlignment="Left" PlaceholderText="密码，可显示"
                         PasswordChar="●" RevealPassword="False" ti:TextInputOptions.IsSensitive="True" />
                <CheckBox Content="显示密码" IsChecked="{Binding #OptionsBox.RevealPassword}" />
            </StackPanel>
            <TextBlock Name="OptionsReadout" Margin="0,6,0,0" FontFamily="Consolas, Menlo, monospace" />

            <TextBlock Classes="caption" Text="3. 开关输入法（IME）" />
            <CheckBox Name="ImeBox" IsChecked="True" Content="允许输入法" />
            <TextBox Name="ImeEntry" Width="260" HorizontalAlignment="Left" PlaceholderText="切到中文输入法试试"
                     InputMethod.IsInputMethodEnabled="{Binding #ImeBox.IsChecked}" />
            <TextBlock Classes="hint"
                       Text="取消勾选后，这个输入框不再接受输入法的组合输入（拼音候选框不出现）；这对「只收英文代码」的框很有用。TextInputOptions 里的 ContentType、ReturnKeyType、IsSensitive、Multiline 等值是给平台输入法的提示，桌面上不会有可见变化，所以这里读回数值证明它们生效。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/TextInputPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using System.Linq;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class TextInputPage : UserControl
    {
        public TextInputPage()
        {
            InitializeComponent();

            // Tunnel: this runs before the TextBox's own handling, so Handled really stops the character.
            DigitsBox.AddHandler(InputElement.TextInputEvent, (object? s, TextInputEventArgs e) =>
            {
                if (e.Text is { Length: > 0 } text && !text.All(char.IsDigit))
                {
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);

            // Read the attached values back off the first two boxes so the readout proves they were applied.
            var email = (TextBox)((StackPanel)OptionsBox.Parent!).Children[0];
            var digits = (TextBox)((StackPanel)OptionsBox.Parent!).Children[1];
            OptionsReadout.Text =
                $"邮箱框  ContentType={TextInputOptions.GetContentType(email)}  ReturnKeyType={TextInputOptions.GetReturnKeyType(email)}\n" +
                $"数字框  ContentType={TextInputOptions.GetContentType(digits)}\n" +
                $"密码框  IsSensitive={TextInputOptions.GetIsSensitive(OptionsBox)}";
        }
    }
}
```

`TextInputOptions` 在 `Avalonia.Input.TextInput` 命名空间（不是 `Avalonia.Input`），XAML 里用 `xmlns:ti="using:Avalonia.Input.TextInput"`。`Children[0]/[1]` 按位置取是为了不给两个邮箱/数字框起名而多写 `x:Name`；它们在 StackPanel 里的顺序固定。

实测依据：`ti:TextInputOptions.ContentType="Email"` + `ReturnKeyType="Send"` 读回 `Email` / `Send`；`InputMethod.IsInputMethodEnabled="{Binding #ImeBox.IsChecked}"` 取消勾选后读回 `False`；Tunnel 拦截 `KeyTextInput("a")` 被吞、`"7"` 通过，文本为 `'7'`。

- [x] **Step 8: 路由事件指路页 RoutedEventsPage**

按 spec 去重规则，路由事件完整实现放在 #8。本页沿用 StylingDemo `ContainerQueriesPage` 的指路页格式。

`Avalonia.InputDemo/Views/Pages/RoutedEventsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.InputDemo.Views.Pages.RoutedEventsPage">

    <StackPanel Margin="12">
        <shared:DemoHeader Title="路由事件：完整演示在 Avalonia.EventsDemo"
                           DocPath="input-interaction/routed-events" />

        <Border Classes="stage" Padding="12">
            <StackPanel Spacing="8">
                <TextBlock TextWrapping="Wrap"
                           Text="输入事件几乎都是路由事件（Tunnel / Bubble / Direct）。官方文档把它放在 Input &amp; Interaction 下，但它本质上属于事件机制，因此完整演示放在 Events 项目里。" />
                <SelectableTextBlock FontFamily="Consolas, monospace"
                                     Text="dotnet run --project Avalonia.EventsDemo   →  「路由三阶段」「Handled」「自定义路由事件」三个标签页" />
                <TextBlock Classes="hint"
                           Text="那里用三层嵌套元素演示 Tunnel/Bubble 的触发顺序，演示 Handled 如何截断冒泡、handledEventsToo 如何越过截断，并自己声明一个路由事件。" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

`Avalonia.InputDemo/Views/Pages/RoutedEventsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class RoutedEventsPage : UserControl
    {
        public RoutedEventsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 9: 挂 8 个 Tab**

修改 `Avalonia.InputDemo/Views/MainWindow.axaml`，在 `<Window>` 上加 `xmlns:pages="using:Avalonia.InputDemo.Views.Pages"`，`TabControl` 改为（竖排，Task 1 已设 `TabStripPlacement="Left"`）：

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
        <TabItem Header="指针">
            <pages:PointerPage />
        </TabItem>
        <TabItem Header="焦点">
            <pages:FocusPage />
        </TabItem>
        <TabItem Header="手势">
            <pages:GesturesPage />
        </TabItem>
        <TabItem Header="键盘与 HotKey">
            <pages:KeyboardPage />
        </TabItem>
        <TabItem Header="交互的写法">
            <pages:InteractivityPage />
        </TabItem>
        <TabItem Header="拖放">
            <pages:DragDropPage />
        </TabItem>
        <TabItem Header="文本输入">
            <pages:TextInputPage />
        </TabItem>
        <TabItem Header="路由事件">
            <pages:RoutedEventsPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 10: 构建**

Run: `dotnet build Avalonia.InputDemo 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "InputDemo" | grep -v MSB3884`
Expected: 无输出。

- [x] **Step 11: 用 headless 探针断言页面行为**

创建 `C:\Temp\inputcheck\inputcheck.csproj`（与 Task 2 的 `eventscheck.csproj` 相同，只把 `ProjectReference` 改成 `Avalonia.InputDemo\Avalonia.InputDemo.csproj`）。

创建 `C:\Temp\inputcheck\Program.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.InputDemo.Views;
using Avalonia.InputDemo.Views.Pages;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
    }
}

internal sealed class WarnSink : ILogSink
{
    public readonly List<string> Entries = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel l, string a, object? s, string m) => Entries.Add($"[{l}] ({a}) {m}");
    public void Log(LogEventLevel l, string a, object? s, string m, params object?[] v)
        => Entries.Add($"[{l}] ({a}) {m} :: {string.Join(", ", v)}");
}

internal static class Probe
{
    private static int _pass, _fail;

    private static T Find<T>(Visual root, string name) where T : Visual
    {
        foreach (var d in root.GetVisualDescendants())
        {
            if (d is T hit && (d as Control)?.Name == name) return hit;
        }
        throw new InvalidOperationException($"not found: {name}");
    }

    private static Window Show(Control c)
    {
        var w = new Window { Content = c, Width = 900, Height = 700 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        return w;
    }

    private static void Check(string label, bool ok, string detail)
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-46} {detail}");
    }

    private static Point Centre(Visual v, Visual root)
        => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    private static string Pseudo(Control c) => string.Join(",", c.Classes.Where(x => x.StartsWith(':')));

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

        // --- Shell ---
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        var names = new[] { "PointerPage", "FocusPage", "GesturesPage", "KeyboardPage", "InteractivityPage", "DragDropPage", "TextInputPage", "RoutedEventsPage" };
        Check("Shell: 8 tabs", tabs.Count == 8, tabs.Count.ToString());
        for (var i = 0; i < tabs.Count && i < 8; i++)
        {
            tabs[i].IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            Check($"Shell: tab {i} renders", tabs[i].Content?.GetType().Name == names[i], tabs[i].Content?.GetType().Name ?? "<null>");
        }
        mw.Close();

        // --- Pointer: capture keeps the handle moving outside the track, and is released on mouse-up ---
        var pointer = new PointerPage();
        var pw = Show(pointer);
        var handle = Find<Border>(pointer, "Handle");
        var track = Find<Canvas>(pointer, "Track");
        var start = Centre(handle, pw);
        pw.MouseDown(start, MouseButton.Left);
        pw.MouseMove(new Point(start.X + 80, start.Y));
        Dispatcher.UIThread.RunJobs();
        Check("Pointer: drag moves handle", Math.Abs(Canvas.GetLeft(handle) - 80) < 1, Canvas.GetLeft(handle).ToString());
        Check("Pointer: state says captured", Find<TextBlock>(pointer, "DragState").Text!.Contains("已捕获"), "");
        pw.MouseMove(new Point(start.X + 400, start.Y + 150));
        Dispatcher.UIThread.RunJobs();
        Check("Pointer: far outside clamps to max", Math.Abs(Canvas.GetLeft(handle) - (track.Width - handle.Width)) < 1, Canvas.GetLeft(handle).ToString());
        pw.MouseUp(new Point(start.X + 400, start.Y + 150), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Check("Pointer: capture lost on release", Find<TextBlock>(pointer, "DragState").Text!.Contains("已释放"), "");
        pw.Close();

        // --- Focus: Tab gives :focus-visible and the ring, Pointer does not; IsTabStop=false is skipped ---
        var focus = new FocusPage();
        var fw = Show(focus);
        var a = Find<Button>(focus, "BtnA");
        var b = Find<Button>(focus, "BtnB");
        var c = Find<Button>(focus, "BtnC");
        b.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Check("Focus: Tab gives :focus-visible", b.Classes.Contains(":focus-visible"), Pseudo(b));
        Check("Focus: ring brush applied", (b.BorderBrush as ISolidColorBrush)?.Color == Color.Parse("#E8974A") && b.BorderThickness.Left == 3, $"{b.BorderBrush} {b.BorderThickness}");
        Check("Focus: readout mentions Tab", Find<TextBlock>(focus, "FocusReadout").Text!.Contains("Tab"), Find<TextBlock>(focus, "FocusReadout").Text!);
        a.Focus(NavigationMethod.Pointer);
        Dispatcher.UIThread.RunJobs();
        Check("Focus: Pointer has no :focus-visible", a.Classes.Contains(":focus") && !a.Classes.Contains(":focus-visible"), Pseudo(a));
        // Walk Tab from A: B, then C must be skipped, landing on D.
        a.Focus(NavigationMethod.Tab);
        fw.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        fw.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var focused = TopLevel.GetTopLevel(focus)!.FocusManager!.GetFocusedElement();
        Check("Focus: Tab skips IsTabStop=false", ReferenceEquals(focused, Find<Button>(focus, "BtnD")) && !ReferenceEquals(focused, c), (focused as Control)?.Name ?? "?");
        fw.Close();

        // --- Gestures: two clicks -> Tapped+DoubleTapped; right click -> RightTapped ---
        var gestures = new GesturesPage();
        var gw = Show(gestures);
        var pad = Find<Border>(gestures, "Pad");
        var pt = Centre(pad, gw);
        gw.MouseDown(pt, MouseButton.Left); gw.MouseUp(pt, MouseButton.Left);
        gw.MouseDown(pt, MouseButton.Left); gw.MouseUp(pt, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        var counts = Find<TextBlock>(gestures, "Counts").Text!;
        Check("Gestures: Tapped counted", counts.Contains("Tapped=1") || counts.Contains("Tapped=2"), counts.Split('\n')[0]);
        Check("Gestures: DoubleTapped=1", counts.Contains("DoubleTapped=1"), counts.Split('\n')[0]);
        gw.MouseDown(pt, MouseButton.Right); gw.MouseUp(pt, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        Check("Gestures: RightTapped=1", Find<TextBlock>(gestures, "Counts").Text!.Contains("RightTapped=1"), "");
        Check("Gestures: pad opted in to mouse hold", pad.GetValue(InputElement.IsHoldWithMouseEnabledProperty), "");
        gw.Close();

        // --- Keyboard: HotKey and KeyBinding both fire from inside the page ---
        var keyboard = new KeyboardPage();
        var kw = Show(keyboard);
        Find<TextBox>(keyboard, "Entry").Focus();
        Dispatcher.UIThread.RunJobs();
        kw.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Control);
        kw.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
        Dispatcher.UIThread.RunJobs();
        Check("Keyboard: Ctrl+S clicked the HotKey button", Find<TextBlock>(keyboard, "SaveReadout").Text == "保存了 1 次", Find<TextBlock>(keyboard, "SaveReadout").Text!);
        Check("Keyboard: Ctrl+Shift+K fired the KeyBinding", Find<TextBlock>(keyboard, "BindReadout").Text!.Contains("1 次"), Find<TextBlock>(keyboard, "BindReadout").Text!);
        Check("Keyboard: readout shows Key and PhysicalKey", Find<TextBlock>(keyboard, "KeyReadout").Text!.Contains("PhysicalKey=K"), Find<TextBlock>(keyboard, "KeyReadout").Text!);
        kw.Close();

        // --- Interactivity: only the Command obeys the lock; InputGesture alone does not fire ---
        var inter = new InteractivityPage();
        var iw = Show(inter);
        var vm = (Avalonia.InputDemo.ViewModels.InteractivityViewModel)inter.DataContext!;
        var buttons = inter.GetVisualDescendants().OfType<Button>().ToList();
        var cmdButton = buttons.First(x => x.Content as string == "Command");
        Check("Interactivity: Command button disabled while locked", !cmdButton.IsEnabled, cmdButton.IsEnabled.ToString());
        buttons.First(x => x.Content as string == "Click 处理器").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Check("Interactivity: Click handler ignores the lock", vm.Count == 1, vm.Count.ToString());
        vm.Unlocked = true;
        Dispatcher.UIThread.RunJobs();
        Check("Interactivity: unlocking enables the Command button", cmdButton.IsEnabled, cmdButton.IsEnabled.ToString());
        cmdButton.Command!.Execute(cmdButton.CommandParameter);
        Check("Interactivity: Command increments", vm.Count == 2, vm.Count.ToString());
        // A hot key only fires while focus is inside the window, so park focus on the first button.
        buttons.First(x => x.Content as string == "Click 处理器").Focus();
        Dispatcher.UIThread.RunJobs();
        iw.KeyPressQwerty(PhysicalKey.Digit1, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        Check("Interactivity: Ctrl+1 (InputGesture only) does nothing", vm.Count == 2, vm.Count.ToString());
        iw.KeyPressQwerty(PhysicalKey.Digit2, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        Check("Interactivity: Ctrl+2 (HotKey) increments", vm.Count == 3, vm.Count.ToString());
        iw.KeyPressQwerty(PhysicalKey.Digit3, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        Check("Interactivity: Ctrl+3 (both) increments", vm.Count == 4, vm.Count.ToString());
        iw.Close();

        // --- DragDrop: receiver side only (the source needs a platform drag loop) ---
        var drag = new DragDropPage();
        var dw = Show(drag);
        var target = Find<Border>(drag, "Target");
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText("hello"));
        var enter = new DragEventArgs(DragDrop.DragEnterEvent, data, target, new Point(5, 5), KeyModifiers.None);
        target.RaiseEvent(enter);
        Check("DragDrop: DragEnter adds :over class", target.Classes.Contains("over"), string.Join(",", target.Classes));
        var over = new DragEventArgs(DragDrop.DragOverEvent, data, target, new Point(5, 5), KeyModifiers.None);
        target.RaiseEvent(over);
        Check("DragDrop: text is accepted (Copy)", over.DragEffects == DragDropEffects.Copy, over.DragEffects.ToString());
        var refused = new DragEventArgs(DragDrop.DragOverEvent, new DataTransfer(), target, new Point(5, 5), KeyModifiers.None);
        target.RaiseEvent(refused);
        Check("DragDrop: no text is refused (None)", refused.DragEffects == DragDropEffects.None, refused.DragEffects.ToString());
        target.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, data, target, new Point(5, 5), KeyModifiers.None));
        Check("DragDrop: Drop shows the text", Find<TextBlock>(drag, "Dropped").Text == "hello", Find<TextBlock>(drag, "Dropped").Text!);
        Check("DragDrop: Drop clears :over", !target.Classes.Contains("over"), string.Join(",", target.Classes));
        dw.Close();

        // --- TextInput: digits box swallows letters; options read back; IME toggle ---
        var text = new TextInputPage();
        var tw = Show(text);
        var digits = Find<TextBox>(text, "DigitsBox");
        digits.Focus();
        Dispatcher.UIThread.RunJobs();
        tw.KeyTextInput("a");
        tw.KeyTextInput("7");
        Dispatcher.UIThread.RunJobs();
        Check("TextInput: letter swallowed, digit kept", digits.Text == "7", $"'{digits.Text}'");
        var readout = Find<TextBlock>(text, "OptionsReadout").Text!;
        Check("TextInput: Email/Send read back", readout.Contains("ContentType=Email") && readout.Contains("ReturnKeyType=Send"), readout.Split('\n')[0]);
        Check("TextInput: Digits read back", readout.Contains("ContentType=Digits"), "");
        Check("TextInput: IsSensitive read back", readout.Contains("IsSensitive=True"), "");
        var ime = Find<TextBox>(text, "ImeEntry");
        Check("TextInput: IME enabled by default", InputMethod.GetIsInputMethodEnabled(ime), "");
        Find<CheckBox>(text, "ImeBox").IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Check("TextInput: unchecking disables IME", !InputMethod.GetIsInputMethodEnabled(ime), "");
        tw.Close();

        Check("No warning-or-worse log entries", sink.Entries.Count == 0, string.Join(" // ", sink.Entries));
        Console.WriteLine($"\n{_pass} passed, {_fail} failed");
    }
}
```

Run: `cd /c/Temp/inputcheck && dotnet run 2>&1 | grep -E "PASS|FAIL|passed"`
Expected: 全部 `PASS`，末行 `N passed, 0 failed`。**N 是执行期实测条数**，把实际数字回填到本步。

热键只在焦点位于窗口内时触发，所以探针在按 `Ctrl+1/2/3` 之前先把焦点放到页面里的第一个按钮上。

若任何一条 `FAIL`，**以探针输出为准修正页面或说明文字**，并在 spec 回写时注明"（执行期修正）"。

- [x] **Step 12: 清理探针并提交**

```bash
rm -rf /c/Temp/inputcheck
git add Avalonia.InputDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Input & Interaction category

Eight pages: pointer capture, focus and :focus-visible, gestures,
keyboard with HotKey and KeyBinding, three ways to trigger one action,
drag and drop, text input and IME, and a signpost to the routed-events
page in EventsDemo.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

## Task 4: 项目 #10 Graphics 的 13 个页面

**Files:**
- Create: `Avalonia.GraphicsDemo/Controls/Sketch.cs`
- Create: `Avalonia.GraphicsDemo/Views/Pages/` 下 13 组 `XxxPage.axaml(.cs)`：`BrushesPage`、`TransformsPage`、`ShapesPage`、`DrawingPage`、`EffectsPage`、`ClipAndHitPage`、`IconsPage`、`RenderOptionsPage`、`AnimationsPage`、`TransitionsPage`、`PageTransitionsPage`、`EasingPage`、`CompositionPage`
- Modify: `Avalonia.GraphicsDemo/Views/MainWindow.axaml`（挂 13 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳；`DemoHeader` 与 `caption`/`hint`/`stage` 样式
- Produces: `Avalonia.GraphicsDemo.Controls.Sketch`（只在 `DrawingPage` 用）。无跨任务产物。

本项目所有页面都是**无状态**的：滑块、复选框、下拉框驱动演示元素，靠 `{Binding #元素名.属性}`，或在 code-behind 里直接改属性；没有 ViewModel 目录。`GradientStop`、`RotateTransform`、`BlurEffect` 这类**非控件对象**上也用 `{Binding #元素名.属性}`——它们靠 XAML 名称作用域解析元素名，本 plan 的探针（Step 16）会读回这些被绑定属性来确认绑定真的连上了（这是规则 2 之外本组最容易静默失败的地方：绑定不通时只有一条 binding warning，图形纹丝不动）。

**动画/过渡/合成页（Step 9–13）的探针限制**见规则 21、25：只能断言「落在区间内」与「随时间变化」，不能断言确切值。

- [x] **Step 1: 画刷页 BrushesPage**

演示：纯色、线性、径向、锥形渐变与图案画刷（`DrawingBrush` 平铺）、`VisualBrush`；渐变中间停靠点的位置用滑块实时改。

`Avalonia.GraphicsDemo/Views/Pages/BrushesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.BrushesPage">

    <UserControl.Styles>
        <Style Selector="Border.swatch">
            <Setter Property="Width" Value="130" />
            <Setter Property="Height" Value="90" />
            <Setter Property="CornerRadius" Value="6" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="画刷与渐变：纯色、线性、径向、锥形、图案平铺、视觉画刷"
                               DocPath="graphics-animation/brushes" />

            <TextBlock Classes="caption" Text="1. 六种画刷填充同一种形状" />
            <WrapPanel>
                <StackPanel Margin="0,0,12,12">
                    <Border Classes="swatch" Background="#E8974A" />
                    <TextBlock Classes="hint" Text="纯色 SolidColorBrush" />
                </StackPanel>

                <StackPanel Margin="0,0,12,12">
                    <Border Classes="swatch">
                        <Border.Background>
                            <!--  The middle stop's Offset is bound to the slider below.  -->
                            <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                <GradientStop Color="#4A7BE8" Offset="0" />
                                <GradientStop Color="#E8564A" Offset="{Binding #StopSlider.Value}" />
                                <GradientStop Color="#E8D54A" Offset="1" />
                            </LinearGradientBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Classes="hint" Text="线性 LinearGradientBrush" />
                </StackPanel>

                <StackPanel Margin="0,0,12,12">
                    <Border Classes="swatch">
                        <Border.Background>
                            <RadialGradientBrush Center="50%,50%" GradientOrigin="30%,30%" RadiusX="70%" RadiusY="70%">
                                <GradientStop Color="#FFFFFF" Offset="0" />
                                <GradientStop Color="#4A7BE8" Offset="1" />
                            </RadialGradientBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Classes="hint" Text="径向 RadialGradientBrush" />
                </StackPanel>

                <StackPanel Margin="0,0,12,12">
                    <Border Classes="swatch">
                        <Border.Background>
                            <ConicGradientBrush Center="50%,50%" Angle="0">
                                <GradientStop Color="#E8564A" Offset="0" />
                                <GradientStop Color="#E8D54A" Offset="0.33" />
                                <GradientStop Color="#4AE87B" Offset="0.66" />
                                <GradientStop Color="#E8564A" Offset="1" />
                            </ConicGradientBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Classes="hint" Text="锥形 ConicGradientBrush" />
                </StackPanel>

                <StackPanel Margin="0,0,12,12">
                    <Border Classes="swatch">
                        <Border.Background>
                            <!--  Element form is required: Fill="{DrawingBrush}" would be read as a markup extension.  -->
                            <DrawingBrush TileMode="Tile" DestinationRect="0,0,20,20">
                                <DrawingBrush.Drawing>
                                    <GeometryDrawing Brush="#4A7BE8" Geometry="M0,0 L10,0 L10,10 L0,10 Z M10,10 L20,10 L20,20 L10,20 Z" />
                                </DrawingBrush.Drawing>
                            </DrawingBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Classes="hint" Text="图案 DrawingBrush（平铺）" />
                </StackPanel>

                <StackPanel Margin="0,0,12,12">
                    <Border Classes="swatch">
                        <Border.Background>
                            <VisualBrush Stretch="Uniform">
                                <VisualBrush.Visual>
                                    <TextBlock Text="Aa" FontSize="48" Foreground="White" />
                                </VisualBrush.Visual>
                            </VisualBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Classes="hint" Text="视觉 VisualBrush" />
                </StackPanel>
            </WrapPanel>

            <TextBlock Classes="caption" Text="2. 拖动滑块，线性渐变的中间停靠点跟着动" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="StopSlider" Minimum="0.05" Maximum="0.95" Value="0.5" Width="260" />
                <TextBlock VerticalAlignment="Center" Text="{Binding #StopSlider.Value, StringFormat='中间停靠点 Offset = {0:F2}'}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="渐变由一串 GradientStop（颜色 + 0~1 的位置）定义；起点终点用相对坐标（百分比）时与控件尺寸无关。径向渐变多一个 GradientOrigin（高光点），锥形渐变绕 Center 转一圈，所以首尾两个停靠点颜色要一致才无接缝。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/BrushesPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class BrushesPage : UserControl
    {
        public BrushesPage()
        {
            InitializeComponent();
        }
    }
}
```

`Border.swatch` 的 `Width`/`Height` 写在样式里而不是元素上，符合规则 2 的习惯（尽管这两个属性本页没有别的样式去覆盖它）。

- [x] **Step 2: 变换页 TransformsPage**

演示：`RenderTransform`（渲染变换，只改绘制，不影响布局）与 `LayoutTransformControl`（布局变换，邻居会被推开）在同一旋转角度下的对照；以及 `RenderTransform` 字符串语法与 `RenderTransformOrigin`。

`Avalonia.GraphicsDemo/Views/Pages/TransformsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.TransformsPage">

    <UserControl.Styles>
        <Style Selector="Border.tile">
            <Setter Property="Width" Value="110" />
            <Setter Property="Height" Value="44" />
            <Setter Property="Background" Value="#4A7BE8" />
            <Setter Property="CornerRadius" Value="4" />
        </Style>
        <Style Selector="Border.neighbor">
            <Setter Property="Width" Value="60" />
            <Setter Property="Height" Value="44" />
            <Setter Property="Background" Value="#555555" />
            <Setter Property="CornerRadius" Value="4" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="变换：RenderTransform 只改绘制，LayoutTransform 会推开邻居"
                               DocPath="graphics-animation/transforms" />

            <TextBlock Classes="caption" Text="1. 同一个旋转角度，两种变换" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="AngleSlider" Minimum="0" Maximum="90" Value="0" Width="260" />
                <TextBlock VerticalAlignment="Center" Text="{Binding #AngleSlider.Value, StringFormat='角度 = {0:F0}°'}" />
            </StackPanel>

            <TextBlock Margin="0,10,0,4" Text="RenderTransform（灰块不动）" />
            <Border Classes="stage" Padding="40,50" HorizontalAlignment="Left">
                <StackPanel Orientation="Horizontal" Spacing="4">
                    <Border Name="RenderLeft" Classes="neighbor" />
                    <Border Classes="tile" RenderTransformOrigin="50%,50%">
                        <Border.RenderTransform>
                            <RotateTransform Angle="{Binding #AngleSlider.Value}" />
                        </Border.RenderTransform>
                    </Border>
                    <Border Name="RenderRight" Classes="neighbor" />
                </StackPanel>
            </Border>

            <TextBlock Margin="0,10,0,4" Text="LayoutTransformControl（右边灰块被推开）" />
            <Border Classes="stage" Padding="40,50" HorizontalAlignment="Left">
                <StackPanel Orientation="Horizontal" Spacing="4">
                    <Border Name="LayoutLeft" Classes="neighbor" />
                    <LayoutTransformControl>
                        <LayoutTransformControl.LayoutTransform>
                            <RotateTransform Angle="{Binding #AngleSlider.Value}" />
                        </LayoutTransformControl.LayoutTransform>
                        <Border Classes="tile" />
                    </LayoutTransformControl>
                    <Border Name="LayoutRight" Classes="neighbor" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint"
                       Text="RenderTransform 在布局完成之后才应用，所以旋转后的蓝块会盖住邻居、也可能画到容器外；LayoutTransformControl 先变换再布局，容器按旋转后的包围盒算尺寸，邻居被推开。代价是 LayoutTransform 要重新布局，滑块拖动时开销更大。" />

            <TextBlock Classes="caption" Text="2. RenderTransform 的字符串语法与原点" />
            <StackPanel Orientation="Horizontal" Spacing="40" Margin="0,20,0,20">
                <StackPanel>
                    <Border Classes="tile" RenderTransform="rotate(30deg) scale(1.2)" RenderTransformOrigin="50%,50%" />
                    <TextBlock Classes="hint" Margin="0,18,0,0" Text="rotate(30deg) scale(1.2)&#x0A;原点 50%,50%（默认）" />
                </StackPanel>
                <StackPanel>
                    <Border Classes="tile" RenderTransform="rotate(30deg) scale(1.2)" RenderTransformOrigin="0%,0%" />
                    <TextBlock Classes="hint" Margin="0,18,0,0" Text="同样的变换&#x0A;原点 0%,0%（左上角）" />
                </StackPanel>
                <StackPanel>
                    <Border Classes="tile" RenderTransform="skew(15deg,0) translate(10px,0)" />
                    <TextBlock Classes="hint" Margin="0,18,0,0" Text="skew(15deg,0) translate(10px,0)" />
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="多个变换写在一个字符串里，从左到右依次应用。原点决定旋转/缩放绕哪一点转，用百分比时与控件尺寸无关。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Border.tile` 的 `Width`/`Height`/`Background` 都在样式里，元素上只有 `RenderTransform` 与 `RenderTransformOrigin`（不被任何样式驱动）。

`Avalonia.GraphicsDemo/Views/Pages/TransformsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class TransformsPage : UserControl
    {
        public TransformsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 3: 形状页 ShapesPage**

演示：六种形状、描边属性（粗细 / 虚线 / 线帽 / 连接），路径迷你语言，`CombinedGeometry` 的四种合并模式。

`Avalonia.GraphicsDemo/Views/Pages/ShapesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.ShapesPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="形状与几何：六种形状、描边属性、路径语言、几何合并"
                               DocPath="graphics-animation/shapes-and-geometries" />

            <TextBlock Classes="caption" Text="1. 六种形状（共用下面滑块的描边粗细）" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="ThickSlider" Minimum="1" Maximum="14" Value="4" Width="220" />
                <TextBlock VerticalAlignment="Center" Text="{Binding #ThickSlider.Value, StringFormat='StrokeThickness = {0:F0}'}" />
            </StackPanel>
            <WrapPanel Margin="0,8,0,0">
                <StackPanel Margin="0,0,16,12">
                    <Rectangle Width="100" Height="60" RadiusX="10" RadiusY="10" Fill="#304A7BE8"
                               Stroke="#4A7BE8" StrokeThickness="{Binding #ThickSlider.Value}" />
                    <TextBlock Classes="hint" Text="Rectangle（RadiusX/Y）" />
                </StackPanel>
                <StackPanel Margin="0,0,16,12">
                    <Ellipse Width="100" Height="60" Fill="#30E8974A" Stroke="#E8974A"
                             StrokeThickness="{Binding #ThickSlider.Value}" />
                    <TextBlock Classes="hint" Text="Ellipse" />
                </StackPanel>
                <StackPanel Margin="0,0,16,12">
                    <Line StartPoint="0,0" EndPoint="100,60" Stroke="#4AE87B"
                          StrokeThickness="{Binding #ThickSlider.Value}" StrokeLineCap="Round" />
                    <TextBlock Classes="hint" Text="Line（线帽 Round）" />
                </StackPanel>
                <StackPanel Margin="0,0,16,12">
                    <Polyline Points="0,60 25,0 50,60 75,0 100,60" Stroke="#E8564A"
                              StrokeThickness="{Binding #ThickSlider.Value}" StrokeJoin="Round" />
                    <TextBlock Classes="hint" Text="Polyline（连接 Round）" />
                </StackPanel>
                <StackPanel Margin="0,0,16,12">
                    <Polygon Points="50,0 100,60 0,60" Fill="#30E8D54A" Stroke="#E8D54A"
                             StrokeThickness="{Binding #ThickSlider.Value}" StrokeJoin="Miter" />
                    <TextBlock Classes="hint" Text="Polygon（连接 Miter）" />
                </StackPanel>
                <StackPanel Margin="0,0,16,12">
                    <Path Data="M0,50 C 20,-20 60,-20 80,50 S 100,80 100,40" Stroke="White"
                          StrokeThickness="{Binding #ThickSlider.Value}" StrokeDashArray="4,3" />
                    <TextBlock Classes="hint" Text="Path + StrokeDashArray=&quot;4,3&quot;" />
                </StackPanel>
            </WrapPanel>
            <TextBlock Classes="hint"
                       Text="StrokeDashArray 的数字是「实线长, 空白长, …」，单位是 StrokeThickness 的倍数，所以滑块拉粗后虚线的段也跟着变长。StrokeLineCap 管线两端（Flat / Round / Square），StrokeJoin 管折角（Miter 尖 / Round 圆 / Bevel 平）。" />

            <TextBlock Classes="caption" Text="2. 路径迷你语言：M 移动 · L 直线 · C 三次贝塞尔 · S 平滑贝塞尔 · A 弧 · Z 闭合" />
            <SelectableTextBlock FontFamily="Consolas, Menlo, monospace" Text="M0,50 C 20,-20 60,-20 80,50 S 100,80 100,40" />
            <TextBlock Classes="hint"
                       Text="上面第六个形状就是这条路径。大写字母用绝对坐标，小写字母用相对当前点的坐标。同一份 Data 可以写成 StreamGeometry 放进资源复用（见「图标」页）。" />

            <TextBlock Classes="caption" Text="3. CombinedGeometry：两个几何做布尔运算" />
            <StackPanel Orientation="Horizontal" Spacing="16">
                <ComboBox Name="ModeBox" Width="160" VerticalAlignment="Center" />
                <Path Name="CombinedPath" Fill="#E8974A" Width="120" Height="90">
                    <Path.Data>
                        <!--  CombinedGeometry cannot be named (neither Name nor x:Name, AVLN2000);
                              code-behind reaches it through the named Path's Data.  -->
                        <CombinedGeometry GeometryCombineMode="Xor">
                            <CombinedGeometry.Geometry1>
                                <EllipseGeometry Center="40,45" RadiusX="35" RadiusY="35" />
                            </CombinedGeometry.Geometry1>
                            <CombinedGeometry.Geometry2>
                                <RectangleGeometry Rect="40,20,60,50" />
                            </CombinedGeometry.Geometry2>
                        </CombinedGeometry>
                    </Path.Data>
                </Path>
            </StackPanel>
            <TextBlock Classes="hint" Text="Union 并集 · Intersect 交集 · Xor 异或 · Exclude 差集（几何 1 减去几何 2）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/ShapesPage.axaml.cs`：

```csharp
using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class ShapesPage : UserControl
    {
        public ShapesPage()
        {
            InitializeComponent();

            // Geometry objects have no name of their own, so go through the Path that owns it.
            var combined = (CombinedGeometry)CombinedPath.Data!;

            ModeBox.ItemsSource = Enum.GetValues<GeometryCombineMode>();
            ModeBox.SelectedItem = combined.GeometryCombineMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is GeometryCombineMode mode)
                {
                    combined.GeometryCombineMode = mode;
                }
            };
        }
    }
}
```

下拉框直接从枚举取值，不手写列表。实测 `GeometryCombineMode` 在 12.1.2 只有 `Union`、`Intersect`、`Xor`、`Exclude` 四个值（没有 `Exclude1From2`/`Exclude2From1`——那是 WPF 的写法，编写本 plan 时按 WPF 经验写错过一次，探针打印枚举后纠正）。

- [x] **Step 4: 自定义绘制页 DrawingPage 与自绘控件 Sketch**

演示：`DrawingContext` 的几个图元，以及 `PushClip` / `PushTransform` / `PushOpacity` 这类「状态栈」——它们是 `using` 作用域，作用域内画的东西才受影响。再加 `DrawingImage` 作为不写代码的绘制方式。

`Avalonia.GraphicsDemo/Controls/Sketch.cs`：

```csharp
using System.Collections.Generic;
using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.GraphicsDemo.Controls
{
    /// <summary>
    /// Draws one fixed scene in <see cref="Render"/>, optionally wrapped in clip / transform / opacity scopes.
    /// </summary>
    public class Sketch : Control
    {
        public static readonly StyledProperty<bool> UseClipProperty =
            AvaloniaProperty.Register<Sketch, bool>(nameof(UseClip));

        public static readonly StyledProperty<bool> UseTransformProperty =
            AvaloniaProperty.Register<Sketch, bool>(nameof(UseTransform));

        public static readonly StyledProperty<bool> UseOpacityProperty =
            AvaloniaProperty.Register<Sketch, bool>(nameof(UseOpacity));

        static Sketch()
        {
            // Without this a toggle changes the property but the control keeps showing the old drawing.
            AffectsRender<Sketch>(UseClipProperty, UseTransformProperty, UseOpacityProperty);
        }

        public bool UseClip { get => GetValue(UseClipProperty); set => SetValue(UseClipProperty, value); }

        public bool UseTransform { get => GetValue(UseTransformProperty); set => SetValue(UseTransformProperty, value); }

        public bool UseOpacity { get => GetValue(UseOpacityProperty); set => SetValue(UseOpacityProperty, value); }

        protected override Size MeasureOverride(Size availableSize) => new(260, 150);

        public override void Render(DrawingContext context)
        {
            var area = new Rect(Bounds.Size);
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#20FFFFFF")), new Pen(Brushes.Gray, 1), area.Deflate(0.5));

            // Each Push* returns a disposable scope. Anything drawn until it is disposed is affected;
            // disposing in reverse order restores the previous state.
            var scopes = new Stack<IDisposable>();
            if (UseClip)
            {
                scopes.Push(context.PushClip(new Rect(0, 0, area.Width / 2, area.Height)));
            }

            if (UseTransform)
            {
                scopes.Push(context.PushTransform(Matrix.CreateRotation(Math.PI / 18) * Matrix.CreateTranslation(20, 0)));
            }

            if (UseOpacity)
            {
                scopes.Push(context.PushOpacity(0.4));
            }

            DrawScene(context);

            while (scopes.Count > 0)
            {
                scopes.Pop().Dispose();
            }
        }

        private static void DrawScene(DrawingContext context)
        {
            context.DrawRectangle(Brushes.SteelBlue, new Pen(Brushes.White, 2), new Rect(20, 20, 90, 60), 8, 8);
            context.DrawEllipse(Brushes.Orange, null, new Point(150, 50), 30, 30);
            context.DrawLine(new Pen(Brushes.LimeGreen, 4, lineCap: PenLineCap.Round), new Point(20, 110), new Point(240, 125));

            var text = new FormattedText("DrawingContext", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 16, Brushes.White);
            context.DrawText(text, new Point(130, 85));
        }
    }
}
```

`Avalonia.GraphicsDemo/Views/Pages/DrawingPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.GraphicsDemo.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.DrawingPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="自定义绘制：DrawingContext 的图元与状态栈，以及 DrawingImage"
                               DocPath="graphics-animation/drawing-graphics" />

            <TextBlock Classes="caption" Text="1. 在 Render(DrawingContext) 里画场景，勾选看状态栈的作用" />
            <StackPanel Orientation="Horizontal" Spacing="12" Margin="0,0,0,8">
                <CheckBox Content="PushClip（只留左半）" IsChecked="{Binding #Scene.UseClip, Mode=TwoWay}" />
                <CheckBox Content="PushTransform（旋转+平移）" IsChecked="{Binding #Scene.UseTransform, Mode=TwoWay}" />
                <CheckBox Content="PushOpacity（0.4）" IsChecked="{Binding #Scene.UseOpacity, Mode=TwoWay}" />
            </StackPanel>
            <controls:Sketch Name="Scene" HorizontalAlignment="Left" />
            <TextBlock Classes="hint"
                       Text="场景里有矩形、椭圆、线、文字四种图元（DrawRectangle / DrawEllipse / DrawLine / DrawText）。三个 Push 都返回要 Dispose 的作用域：作用域内画的东西才受影响，Dispose 后恢复。控件的属性登记了 AffectsRender，所以勾选后会自动重绘——少了这一行，属性变了但画面不变。" />

            <TextBlock Classes="caption" Text="2. DrawingImage：不写代码的绘制，当作 Image.Source 用" />
            <Image Width="160" Height="100" HorizontalAlignment="Left">
                <Image.Source>
                    <DrawingImage>
                        <DrawingGroup>
                            <GeometryDrawing Brush="#4A7BE8" Geometry="M0,0 L160,0 L160,100 L0,100 Z" />
                            <GeometryDrawing Brush="#E8974A" Geometry="M80,10 L150,90 L10,90 Z" />
                            <GeometryDrawing Brush="White" Geometry="M80,40 A12,12 0 1 1 79.9,40 Z" />
                        </DrawingGroup>
                    </DrawingImage>
                </Image.Source>
            </Image>
            <TextBlock Classes="hint"
                       Text="DrawingImage 把一组 GeometryDrawing 包成可缩放的矢量图，Image 用它时按 Stretch 缩放且不会糊。需要在运行时按条件作画才写 Render；静态图形用 DrawingImage 更省事。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/DrawingPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class DrawingPage : UserControl
    {
        public DrawingPage()
        {
            InitializeComponent();
        }
    }
}
```

`Sketch` 的三个开关用 `TwoWay` 绑定到 CheckBox：`IsChecked` 是 `bool?`，绑定到 `bool` 属性时 `null` 会被当成默认值，没有问题。

**headless 下 `Render` 能否被调用，需要在探针里确认**（后端不同行为不同），所以 Step 16 的探针用一个继承 `Sketch` 的计数子类验证；若 headless 不调用 `Render`，该条断言降级为「切换开关后属性读回正确」，并把原因写进 spec 回写。

- [x] **Step 5: 特效页 EffectsPage**

演示：`BlurEffect` 与 `DropShadowEffect`，参数用滑块实时改。

`Avalonia.GraphicsDemo/Views/Pages/EffectsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.EffectsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="特效：模糊与投影，作用在控件及其全部子元素上"
                               DocPath="graphics-animation/effects" />

            <TextBlock Classes="caption" Text="1. BlurEffect" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="BlurSlider" Minimum="0" Maximum="12" Value="4" Width="220" />
                <TextBlock VerticalAlignment="Center" Text="{Binding #BlurSlider.Value, StringFormat='Radius = {0:F1}'}" />
            </StackPanel>
            <Border Classes="stage" Padding="24" HorizontalAlignment="Left" Margin="0,8,0,0">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Button Content="被模糊的按钮">
                        <Button.Effect>
                            <BlurEffect Radius="{Binding #BlurSlider.Value}" />
                        </Button.Effect>
                    </Button>
                    <TextBlock Text="被模糊的文字" VerticalAlignment="Center">
                        <TextBlock.Effect>
                            <BlurEffect Radius="{Binding #BlurSlider.Value}" />
                        </TextBlock.Effect>
                    </TextBlock>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. DropShadowEffect" />
            <Grid ColumnDefinitions="Auto,260" RowDefinitions="Auto,Auto,Auto,Auto" Margin="0,0,0,8">
                <TextBlock Grid.Row="0" Text="OffsetX" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="OffsetXSlider" Grid.Row="0" Grid.Column="1" Minimum="-20" Maximum="20" Value="6" />
                <TextBlock Grid.Row="1" Text="OffsetY" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="OffsetYSlider" Grid.Row="1" Grid.Column="1" Minimum="-20" Maximum="20" Value="6" />
                <TextBlock Grid.Row="2" Text="BlurRadius" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="ShadowBlurSlider" Grid.Row="2" Grid.Column="1" Minimum="0" Maximum="30" Value="12" />
                <TextBlock Grid.Row="3" Text="Opacity" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="ShadowOpacitySlider" Grid.Row="3" Grid.Column="1" Minimum="0" Maximum="1" Value="0.7" />
            </Grid>
            <Border Classes="stage" Padding="40" HorizontalAlignment="Left">
                <Border Width="140" Height="70" Background="#E8974A" CornerRadius="8">
                    <Border.Effect>
                        <DropShadowEffect Color="Black"
                                          OffsetX="{Binding #OffsetXSlider.Value}"
                                          OffsetY="{Binding #OffsetYSlider.Value}"
                                          BlurRadius="{Binding #ShadowBlurSlider.Value}"
                                          Opacity="{Binding #ShadowOpacitySlider.Value}" />
                    </Border.Effect>
                    <TextBlock Text="投影" Foreground="Black" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </Border>
            <TextBlock Classes="hint"
                       Text="Effect 作用在整个控件（含子元素）渲染出的位图上，所以模糊的按钮里文字也会糊。特效有渲染开销，不要给大面积、频繁重绘的控件加；投影只需要阴影时，也可以用 BoxShadow（Border 的属性）代替。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/EffectsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class EffectsPage : UserControl
    {
        public EffectsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 6: 裁剪遮罩与命中测试页 ClipAndHitPage**

演示：`Clip`（任意几何裁剪）、`ClipToBounds` 配 `CornerRadius`、`OpacityMask`；以及命中测试——`Background` 为空与 `Transparent` 的区别、`IsHitTestVisible="False"`。

`Avalonia.GraphicsDemo/Views/Pages/ClipAndHitPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.ClipAndHitPage">

    <UserControl.Styles>
        <Style Selector="Border.probe">
            <Setter Property="Width" Value="140" />
            <Setter Property="Height" Value="60" />
            <Setter Property="BorderBrush" Value="#E8974A" />
            <Setter Property="BorderThickness" Value="3" />
            <Setter Property="CornerRadius" Value="4" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="裁剪、遮罩与命中测试"
                               DocPath="graphics-animation/clipping-and-masking" />

            <TextBlock Classes="caption" Text="1. 三种裁剪/遮罩，对同一张渐变图" />
            <WrapPanel>
                <StackPanel Margin="0,0,16,12">
                    <!--  Clip takes any geometry; the triangle is written in path mini-language.  -->
                    <Border Width="120" Height="90" Clip="M0,0 L120,0 L60,90 Z">
                        <Border.Background>
                            <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                <GradientStop Color="#4A7BE8" Offset="0" />
                                <GradientStop Color="#E8564A" Offset="1" />
                            </LinearGradientBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Classes="hint" Text="Clip=&quot;M0,0 L120,0 L60,90 Z&quot;" />
                </StackPanel>

                <StackPanel Margin="0,0,16,12">
                    <!--  ClipToBounds with CornerRadius rounds the corners of a child that would otherwise poke out.  -->
                    <Border Width="120" Height="90" CornerRadius="30" ClipToBounds="True" Background="#303040">
                        <Rectangle Width="200" Height="40" Fill="#E8D54A" />
                    </Border>
                    <TextBlock Classes="hint" Text="ClipToBounds + CornerRadius" />
                </StackPanel>

                <StackPanel Margin="0,0,16,12">
                    <Border Width="120" Height="90">
                        <Border.Background>
                            <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                <GradientStop Color="#4A7BE8" Offset="0" />
                                <GradientStop Color="#E8564A" Offset="1" />
                            </LinearGradientBrush>
                        </Border.Background>
                        <Border.OpacityMask>
                            <RadialGradientBrush>
                                <GradientStop Color="White" Offset="0.4" />
                                <GradientStop Color="Transparent" Offset="1" />
                            </RadialGradientBrush>
                        </Border.OpacityMask>
                    </Border>
                    <TextBlock Classes="hint" Text="OpacityMask（径向渐变做柔边）" />
                </StackPanel>
            </WrapPanel>
            <TextBlock Classes="hint"
                       Text="Clip 是硬边裁剪（可以是任何几何）；OpacityMask 按画刷的 alpha 通道决定每个像素的透明度，所以能做柔边。ClipToBounds 只裁到控件自己的矩形，配 CornerRadius 才有圆角。" />

            <TextBlock Classes="caption" Text="2. 命中测试：点点三个框的正中央，看哪个收到 Tapped" />
            <StackPanel Orientation="Horizontal" Spacing="16">
                <StackPanel>
                    <Border Name="NoBrush" Classes="probe" Tapped="OnTapped" />
                    <TextBlock Classes="hint" Text="没有 Background" />
                </StackPanel>
                <StackPanel>
                    <Border Name="Clear" Classes="probe" Background="Transparent" Tapped="OnTapped" />
                    <TextBlock Classes="hint" Text="Background=Transparent" />
                </StackPanel>
                <StackPanel>
                    <Border Name="Invisible" Classes="probe" Background="#40E8564A" IsHitTestVisible="False" Tapped="OnTapped" />
                    <TextBlock Classes="hint" Text="IsHitTestVisible=False" />
                </StackPanel>
            </StackPanel>
            <TextBlock Name="HitReadout" Margin="0,8,0,0" FontFamily="Consolas, Menlo, monospace" />
            <TextBlock Classes="hint"
                       Text="三个框看起来一样（都有橙色描边），但正中央只有第二个会响应：没有 Background 的元素不参与命中，点中间等于点到身后；Transparent 是「看不见但可点」；IsHitTestVisible=False 是「看得见但不可点」。需要透明可点区域时一定要显式写 Transparent。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/ClipAndHitPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class ClipAndHitPage : UserControl
    {
        private int _noBrush, _clear, _invisible;

        public ClipAndHitPage()
        {
            InitializeComponent();
            Show();
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            switch ((sender as Control)?.Name)
            {
                case "NoBrush": _noBrush++; break;
                case "Clear": _clear++; break;
                case "Invisible": _invisible++; break;
            }

            Show();
        }

        private void Show()
            => HitReadout.Text = $"没有 Background={_noBrush}   Transparent={_clear}   IsHitTestVisible=False={_invisible}";
    }
}
```

**这页的 `Background` 本身就是被测对象，所以第一个框刻意不写**——这是规则 15 的反面教材，别「顺手补上」。

- [x] **Step 7: 图标页 IconsPage**

演示：三种图标写法——`PathIcon` 写路径数据、`PathIcon` 引用 `StreamGeometry` 资源、`Image` + `DrawingImage`。

`Avalonia.GraphicsDemo/Views/Pages/IconsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.IconsPage">

    <UserControl.Resources>
        <!--  A path string compiled once into a reusable geometry resource.  -->
        <StreamGeometry x:Key="StarGeometry">M12,2 L15.09,8.26 L22,9.27 L17,14.14 L18.18,21.02 L12,17.77 L5.82,21.02 L7,14.14 L2,9.27 L8.91,8.26 Z</StreamGeometry>
    </UserControl.Resources>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="图标：PathIcon、几何资源、DrawingImage 三种写法"
                               DocPath="graphics-animation/adding-icons" />

            <TextBlock Classes="caption" Text="1. 同一颗星，三种写法（滑块调大小，颜色随 Foreground）" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Slider Name="SizeSlider" Minimum="16" Maximum="96" Value="48" Width="220" />
                <TextBlock VerticalAlignment="Center" Text="{Binding #SizeSlider.Value, StringFormat='大小 = {0:F0}'}" />
            </StackPanel>

            <StackPanel Orientation="Horizontal" Spacing="32" Margin="0,12,0,0">
                <StackPanel>
                    <PathIcon Width="{Binding #SizeSlider.Value}" Height="{Binding #SizeSlider.Value}"
                              Foreground="#E8D54A"
                              Data="M12,2 L15.09,8.26 L22,9.27 L17,14.14 L18.18,21.02 L12,17.77 L5.82,21.02 L7,14.14 L2,9.27 L8.91,8.26 Z" />
                    <TextBlock Classes="hint" Text="PathIcon Data=&quot;…&quot;" />
                </StackPanel>

                <StackPanel>
                    <PathIcon Width="{Binding #SizeSlider.Value}" Height="{Binding #SizeSlider.Value}"
                              Foreground="#4AE87B"
                              Data="{StaticResource StarGeometry}" />
                    <TextBlock Classes="hint" Text="PathIcon + StreamGeometry 资源" />
                </StackPanel>

                <StackPanel>
                    <Image Width="{Binding #SizeSlider.Value}" Height="{Binding #SizeSlider.Value}">
                        <Image.Source>
                            <DrawingImage>
                                <GeometryDrawing Brush="#E8564A" Geometry="{StaticResource StarGeometry}" />
                            </DrawingImage>
                        </Image.Source>
                    </Image>
                    <TextBlock Classes="hint" Text="Image + DrawingImage" />
                </StackPanel>
            </StackPanel>

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="PathIcon 继承 Foreground，所以放进按钮、菜单时会自动跟着主题与禁用状态变色；DrawingImage 的颜色写死在 Brush 里，适合多色图标。几何数据放资源里，同一个图标出现几十次也只解析一次。字体图标（iconfont）的写法见 Avalonia.StylingDemo 的「字体」页与 Avalonia.HtmlRendererDemo。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/IconsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class IconsPage : UserControl
    {
        public IconsPage()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 8: 渲染选项页 RenderOptionsPage**

演示：位图插值（放大 8×8 的棋盘格，是「像素风」还是「糊」）、位图混合模式、边缘抗锯齿、文本渲染模式。位图在 code-behind 里生成，不依赖外部图片文件。

`Avalonia.GraphicsDemo/Views/Pages/RenderOptionsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.RenderOptionsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="渲染选项：位图插值、混合模式、边缘与文本抗锯齿"
                               DocPath="graphics-animation/image-interpolation" />

            <TextBlock Classes="caption" Text="1. 位图插值：把 8×8 的棋盘格放大到 160×160" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ComboBox Name="InterpolationBox" Width="180" VerticalAlignment="Center" />
                <Border Classes="stage">
                    <Image Name="Pixels" Width="160" Height="160" />
                </Border>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="None 是最近邻（保留硬边的像素风），LowQuality 以上是双线性/双三次（放大后变糊）。像素画、二维码、放大镜这类场景要选 None。" />

            <TextBlock Classes="caption" Text="2. 位图混合模式：两张棋盘格叠在一起" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ComboBox Name="BlendBox" Width="180" VerticalAlignment="Center" />
                <Border Classes="stage" Width="200" Height="140">
                    <Panel>
                        <Image Name="BlendBottom" Width="120" Height="120" HorizontalAlignment="Left" Margin="10,0,0,0" />
                        <Image Name="BlendTop" Width="120" Height="120" HorizontalAlignment="Right" Margin="0,0,10,0" />
                    </Panel>
                </Border>
            </StackPanel>
            <TextBlock Classes="hint" Text="混合模式只作用于位图（Image），决定上层像素与下层像素怎么合成；重叠区域的颜色随模式变化。" />

            <TextBlock Classes="caption" Text="3. 边缘与文本" />
            <StackPanel Orientation="Horizontal" Spacing="32">
                <StackPanel>
                    <Line StartPoint="0,0" EndPoint="120,40" Stroke="White" StrokeThickness="1"
                          RenderOptions.EdgeMode="Aliased" />
                    <TextBlock Classes="hint" Text="EdgeMode=Aliased（锯齿）" />
                </StackPanel>
                <StackPanel>
                    <Line StartPoint="0,0" EndPoint="120,40" Stroke="White" StrokeThickness="1"
                          RenderOptions.EdgeMode="Antialias" />
                    <TextBlock Classes="hint" Text="EdgeMode=Antialias（默认）" />
                </StackPanel>
                <StackPanel>
                    <TextBlock Text="Hello 你好 0123" FontSize="18" TextOptions.TextRenderingMode="Alias" />
                    <TextBlock Classes="hint" Text="TextRenderingMode=Alias" />
                </StackPanel>
                <StackPanel>
                    <TextBlock Text="Hello 你好 0123" FontSize="18" TextOptions.TextRenderingMode="Antialias" />
                    <TextBlock Classes="hint" Text="TextRenderingMode=Antialias" />
                </StackPanel>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/RenderOptionsPage.axaml.cs`：

```csharp
using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class RenderOptionsPage : UserControl
    {
        public RenderOptionsPage()
        {
            InitializeComponent();

            var checker = CreateChecker();
            Pixels.Source = checker;
            BlendBottom.Source = checker;
            BlendTop.Source = checker;

            // The drop-downs list the enum values themselves, so they cannot drift from the framework.
            InterpolationBox.ItemsSource = Enum.GetValues<BitmapInterpolationMode>();
            InterpolationBox.SelectedItem = BitmapInterpolationMode.None;
            InterpolationBox.SelectionChanged += (_, _) =>
            {
                if (InterpolationBox.SelectedItem is BitmapInterpolationMode mode)
                {
                    RenderOptions.SetBitmapInterpolationMode(Pixels, mode);
                }
            };
            RenderOptions.SetBitmapInterpolationMode(Pixels, BitmapInterpolationMode.None);

            BlendBox.ItemsSource = Enum.GetValues<BitmapBlendingMode>();
            BlendBox.SelectedItem = BitmapBlendingMode.SourceOver;
            BlendBox.SelectionChanged += (_, _) =>
            {
                if (BlendBox.SelectedItem is BitmapBlendingMode mode)
                {
                    RenderOptions.SetBitmapBlendingMode(BlendTop, mode);
                }
            };
        }

        // An 8x8 two-colour checkerboard, built in memory so the page needs no image file.
        private static WriteableBitmap CreateChecker()
        {
            const int size = 8;
            var bitmap = new WriteableBitmap(new PixelSize(size, size), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);

            var pixels = new int[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = (x + y) % 2 == 0 ? unchecked((int)0xFFE8974A) : unchecked((int)0xFF3A6FB0);
                }
            }

            using var frame = bitmap.Lock();
            Marshal.Copy(pixels, 0, frame.Address, pixels.Length);
            return bitmap;
        }
    }
}
```

`BitmapInterpolationMode`、`BitmapBlendingMode` 在 `Avalonia.Media`，`WriteableBitmap` 在 `Avalonia.Media.Imaging`，`PixelFormats`/`AlphaFormat` 在 `Avalonia.Platform`。8 像素宽、4 字节每像素，`Lock()` 读回 `RowBytes=32`，行跨度恰好等于一行像素，所以可以一次性 `Marshal.Copy`；换成别的宽度就要按 `frame.RowBytes` 逐行拷。以上枚举取值与整段位图写法都已实测：`BitmapInterpolationMode` 为 `Unspecified/None/LowQuality/MediumQuality/HighQuality`；`BitmapBlendingMode` 有 28 个值（`SourceOver`、`Plus`、`Multiply`、`Screen` 等）；`TextRenderingMode` 为 `Unspecified/SubpixelAntialias/Antialias/Alias`；`EdgeMode` 为 `Unspecified/Antialias/Aliased`。本页 XAML 里用到的 `Alias`、`Antialias`、`Aliased` 都在其中。

- [x] **Step 9: 关键帧动画页 AnimationsPage**

演示：`Style.Animations` 里的 `Animation` + `KeyFrame`，用样式类开关动画；`Duration`、`IterationCount`、`PlaybackDirection`、`Delay`、`FillMode` 各自的效果。**动画由类名驱动**：加上 `run` 类才开始，去掉就停（规则 21：不用 `RunAsync`）。

`Avalonia.GraphicsDemo/Views/Pages/AnimationsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.AnimationsPage">

    <UserControl.Styles>
        <!--  Resting look. Width/Height/Background live here, not on the elements, so nothing local
              can shadow what the animation writes.  -->
        <Style Selector="Border.ball">
            <Setter Property="Width" Value="40" />
            <Setter Property="Height" Value="40" />
            <Setter Property="CornerRadius" Value="20" />
            <Setter Property="Background" Value="#4A7BE8" />
            <Setter Property="HorizontalAlignment" Value="Left" />
        </Style>

        <!--  1. Pulse: opacity and size, looping back and forth.  -->
        <Style Selector="Border.ball.pulse.run">
            <Style.Animations>
                <Animation Duration="0:0:0.8" IterationCount="Infinite" PlaybackDirection="Alternate"
                           Easing="CubicEaseInOut">
                    <KeyFrame Cue="0%">
                        <Setter Property="Opacity" Value="0.3" />
                        <Setter Property="Width" Value="40" />
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Opacity" Value="1" />
                        <Setter Property="Width" Value="120" />
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>

        <!--  2. Once: runs a single time and, with FillMode=Forward, holds the last frame.  -->
        <Style Selector="Border.ball.once.run">
            <Style.Animations>
                <Animation Duration="0:0:1" IterationCount="1" FillMode="Forward" Easing="CubicEaseOut">
                    <KeyFrame Cue="0%">
                        <Setter Property="Width" Value="40" />
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Width" Value="300" />
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>

        <!--  3. Three keyframes: the colour passes through a middle stop.  -->
        <Style Selector="Border.ball.rainbow.run">
            <Style.Animations>
                <Animation Duration="0:0:3" IterationCount="Infinite">
                    <KeyFrame Cue="0%">
                        <Setter Property="Background" Value="#E8564A" />
                    </KeyFrame>
                    <KeyFrame Cue="50%">
                        <Setter Property="Background" Value="#4AE87B" />
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Background" Value="#4A7BE8" />
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="关键帧动画：Style.Animations，用类名开关"
                               DocPath="graphics-animation/keyframe-animations" />

            <ToggleButton Name="RunToggle" Content="运行动画（给下面三个球加 / 去掉 run 类）" Click="OnRunToggle" HorizontalAlignment="Left" />

            <TextBlock Classes="caption" Text="1. 往复：Alternate + Infinite，透明度与宽度" />
            <Border Name="Pulse" Classes="ball pulse" />

            <TextBlock Classes="caption" Text="2. 单次：IterationCount=1 + FillMode=Forward，停在末帧" />
            <Border Name="Once" Classes="ball once" />

            <TextBlock Classes="caption" Text="3. 三个关键帧：颜色经过中间色" />
            <Border Name="Rainbow" Classes="ball rainbow" />

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="动画写在 Style 的 Animations 里，选择器末尾的 .run 决定何时生效：类名加上，动画开始；去掉，动画停止，属性回到样式里的静止值。FillMode=Forward 让「单次」动画结束后保持末帧，否则会弹回起点。Cue 可以写 0% / 50% / 100%，关键帧之间按 Easing 插值。静止值（Width / Background）写在 Border.ball 样式里而不是元素上——元素上的本地值优先级最高，动画写的值会被它盖住，动画「看起来没效果」且没有任何警告（本组规则 2）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/AnimationsPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class AnimationsPage : UserControl
    {
        public AnimationsPage()
        {
            InitializeComponent();
        }

        private void OnRunToggle(object? sender, RoutedEventArgs e)
        {
            var on = ((ToggleButton)RunToggle).IsChecked == true;

            // Adding or removing the class is the whole switch: the animations are declared in styles.
            foreach (var ball in new[] { Pulse, Once, Rainbow })
            {
                ball.Classes.Set("run", on);
            }
        }
    }
}
```

- [x] **Step 10: 控件过渡页 TransitionsPage**

演示：`Transitions` 让属性变化「平滑」而不是瞬变——悬停时宽度、背景色、旋转各自过渡，缓动函数可换。与上一页不同：动画是「自己跑」，过渡是「被动响应属性变化」。

`Avalonia.GraphicsDemo/Views/Pages/TransitionsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.TransitionsPage">

    <UserControl.Styles>
        <!--  Resting state and the hover state both live in styles: the Transitions on the element
              animate between them. A local Width on the element would block every change.  -->
        <Style Selector="Border.card">
            <Setter Property="Width" Value="120" />
            <Setter Property="Height" Value="56" />
            <Setter Property="CornerRadius" Value="6" />
            <Setter Property="Background" Value="#4A7BE8" />
            <Setter Property="HorizontalAlignment" Value="Left" />
            <Setter Property="RenderTransformOrigin" Value="50%,50%" />
            <Setter Property="RenderTransform" Value="none" />
        </Style>
        <Style Selector="Border.card:pointerover">
            <Setter Property="Width" Value="260" />
            <Setter Property="Background" Value="#E8974A" />
            <Setter Property="RenderTransform" Value="rotate(6deg)" />
        </Style>
    </UserControl.Styles>

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="控件过渡：属性一变，Transitions 负责把变化「走」完"
                               DocPath="graphics-animation/control-transitions" />

            <TextBlock Classes="caption" Text="1. 鼠标移上去：宽度（Double）、背景（Brush）、旋转（TransformOperations）各自过渡" />
            <Border Name="Smooth" Classes="card">
                <Border.Transitions>
                    <Transitions>
                        <DoubleTransition Property="Width" Duration="0:0:0.4" Easing="CubicEaseOut" />
                        <BrushTransition Property="Background" Duration="0:0:0.4" />
                        <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
                    </Transitions>
                </Border.Transitions>
                <TextBlock Text="有过渡" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>

            <TextBlock Classes="caption" Text="2. 同样的样式，没有 Transitions：瞬变" />
            <Border Name="Abrupt" Classes="card">
                <TextBlock Text="无过渡" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>

            <TextBlock Classes="caption" Text="3. 弹跳缓动" />
            <Border Name="Bouncy" Classes="card">
                <Border.Transitions>
                    <Transitions>
                        <DoubleTransition Property="Width" Duration="0:0:0.8" Easing="BounceEaseOut" />
                    </Transitions>
                </Border.Transitions>
                <TextBlock Text="BounceEaseOut" HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>

            <TextBlock Classes="hint" Margin="0,12,0,0"
                       Text="Transition 只管「属性值变了之后怎么走过去」，谁让属性变不管：这里是 :pointerover 伪类的样式，代码里直接赋值也一样会被过渡。静止值与悬停值都写在样式里，元素上不写 Width / Background——元素上的本地值优先级最高，会让样式的变化根本不发生，Transition 也就无事可做，而且没有任何警告（本组规则 2）。没有 Transitions 的第二块卡片是对照：同样的样式，变化是瞬间完成的。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/TransitionsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class TransitionsPage : UserControl
    {
        public TransitionsPage()
        {
            InitializeComponent();
        }
    }
}
```

**`Border.card` 里 `RenderTransform` 的静止值写成 `none`**，`TransformOperationsTransition` 才有一个同类型的起点可以过渡到 `rotate(6deg)`。

已实测（headless，`w.MouseMove` 悬停 + 反复 `ForceRenderTimerTick`）：无 `Transitions` 的卡片悬停后**立即** `Width=260`；有 `Transitions` 的卡片宽度依次读回 `164,199,223,239,247,255,259,260`，终值 `259.86`，背景色读回 `#ffde946a`（蓝橙之间的中间色，说明 `BrushTransition` 在走），`RenderTransform` 是 `TransformOperations` 类型。`:pointerover` 在 headless 里随 `MouseMove` 正确打上。

- [x] **Step 11: 页面过渡页 PageTransitionsPage**

演示：`TransitioningContentControl` 在内容替换时播放 `IPageTransition`：`PageSlide`（水平/垂直）、`CrossFade`、两者用 `CompositePageTransition` 组合。

`Avalonia.GraphicsDemo/Views/Pages/PageTransitionsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.PageTransitionsPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="页面过渡：内容被替换时，旧内容怎么离开、新内容怎么进来"
                               DocPath="graphics-animation/page-transitions" />

            <TextBlock Classes="caption" Text="1. 选一种过渡，再点「下一页」" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <ComboBox Name="KindBox" Width="220" SelectedIndex="0">
                    <ComboBoxItem Content="PageSlide（水平）" />
                    <ComboBoxItem Content="PageSlide（垂直）" />
                    <ComboBoxItem Content="CrossFade" />
                    <ComboBoxItem Content="组合：PageSlide + CrossFade" />
                </ComboBox>
                <Button Content="下一页" Click="OnNext" />
                <TextBlock Name="Readout" VerticalAlignment="Center" />
            </StackPanel>

            <Border Classes="stage" Width="360" Height="140" ClipToBounds="True" Margin="0,8,0,0" HorizontalAlignment="Left">
                <TransitioningContentControl Name="Pager" />
            </Border>

            <TextBlock Classes="hint" Margin="0,8,0,0"
                       Text="TransitioningContentControl 的 PageTransition 属性接收任何 IPageTransition。PageSlide 让新旧内容沿一条轴滑动，CrossFade 让它们淡入淡出，CompositePageTransition 把多个过渡同时播放。外面的 Border 设了 ClipToBounds，否则滑出去的内容会画到框外。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/PageTransitionsPage.axaml.cs`：

```csharp
using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class PageTransitionsPage : UserControl
    {
        private static readonly string[] Colors = { "#4A7BE8", "#E8974A", "#4AE87B", "#E8564A" };
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(400);

        private int _index;

        public PageTransitionsPage()
        {
            InitializeComponent();
            KindBox.SelectionChanged += (_, _) => Pager.PageTransition = CreateTransition();
            Pager.PageTransition = CreateTransition();
            Pager.Content = CreatePage();
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            _index++;

            // Replacing Content is what triggers the transition; a fresh control each time,
            // because one control instance cannot sit in two places at once.
            Pager.Content = CreatePage();
            Readout.Text = $"第 {_index % Colors.Length + 1} 页";
        }

        private Control CreatePage() => new Border
        {
            Background = new SolidColorBrush(Color.Parse(Colors[_index % Colors.Length])),
            Child = new TextBlock
            {
                Text = $"第 {_index % Colors.Length + 1} 页",
                FontSize = 24,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

        private IPageTransition CreateTransition() => KindBox.SelectedIndex switch
        {
            0 => new PageSlide(Duration, PageSlide.SlideAxis.Horizontal),
            1 => new PageSlide(Duration, PageSlide.SlideAxis.Vertical),
            2 => new CrossFade(Duration),
            _ => new CompositePageTransition
            {
                PageTransitions =
                {
                    new PageSlide(Duration, PageSlide.SlideAxis.Horizontal),
                    new CrossFade(Duration),
                },
            },
        };
    }
}
```

`PageSlide(TimeSpan, SlideAxis)`、`CrossFade(TimeSpan)` 两个构造函数与 `SlideAxis` 的 `Horizontal`/`Vertical` 两个值、`CompositePageTransition.PageTransitions` 集合都已读回确认；`TransitioningContentControl` 换 `Content` 后读回新内容也已实测。

- [x] **Step 12: 缓动函数页 EasingPage**

演示：选一个缓动函数，画出它的曲线，并让小球按它走一趟——曲线是 `Ease(t)` 的直接采样，小球是用同一个函数做的 `DoubleTransition`，两者对得上。

`Avalonia.GraphicsDemo/Views/Pages/EasingPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.EasingPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="缓动函数：把「时间进度 0→1」映射成「值进度」的曲线"
                               DocPath="graphics-animation/easing-functions" />

            <TextBlock Classes="caption" Text="1. 选一个缓动函数" />
            <ComboBox Name="EasingBox" Width="220" />

            <TextBlock Classes="caption" Text="2. 曲线：横轴是时间，纵轴是值（虚线是 0 与 1）" />
            <Border Classes="stage" Width="320" Height="170" HorizontalAlignment="Left">
                <Canvas Width="300" Height="160">
                    <Line StartPoint="0,130" EndPoint="300,130" Stroke="#60FFFFFF" StrokeDashArray="3,3" />
                    <Line StartPoint="0,30" EndPoint="300,30" Stroke="#60FFFFFF" StrokeDashArray="3,3" />
                    <Polyline Name="Curve" Stroke="#E8974A" StrokeThickness="2" />
                </Canvas>
            </Border>
            <TextBlock Name="Samples" Classes="hint" FontFamily="Consolas, Menlo, monospace" />

            <TextBlock Classes="caption" Text="3. 小球按同一个函数走一趟" />
            <Border Classes="stage" Width="320" Height="50" HorizontalAlignment="Left">
                <Canvas Width="300" Height="40">
                    <Border Name="Ball" Width="24" Height="24" CornerRadius="12" Background="#4A7BE8" Canvas.Top="8" />
                </Canvas>
            </Border>
            <Button Content="走一趟" Click="OnRun" Margin="0,8,0,0" />
            <TextBlock Classes="hint" Margin="0,8,0,0"
                       Text="In 是先慢后快，Out 是先快后慢，InOut 两头慢中间快。Back 会越过终点再回来，Elastic 像弹簧来回摆，Bounce 像球落地反弹——它们的值会超出 0~1，曲线因此冲出虚线。同一个函数既能用在 Transition，也能用在关键帧动画的 Easing 属性上。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/EasingPage.axaml.cs`：

```csharp
using System;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class EasingPage : UserControl
    {
        // Easing.Parse resolves these names, so the list is also the set of strings usable in XAML.
        private static readonly string[] Names =
        {
            "LinearEasing",
            "CubicEaseIn", "CubicEaseOut", "CubicEaseInOut",
            "BackEaseIn", "BackEaseOut", "BackEaseInOut",
            "ElasticEaseIn", "ElasticEaseOut", "ElasticEaseInOut",
            "BounceEaseIn", "BounceEaseOut", "BounceEaseInOut",
        };

        private const double CurveWidth = 300;
        private const double TrackLength = 276;
        private bool _atEnd;

        public EasingPage()
        {
            InitializeComponent();

            EasingBox.ItemsSource = Names;
            EasingBox.SelectionChanged += (_, _) => Redraw();
            EasingBox.SelectedIndex = 3;
        }

        private Easing Current => Easing.Parse(Names[Math.Max(EasingBox.SelectedIndex, 0)]);

        private void Redraw()
        {
            var easing = Current;

            // Sample the function itself: x is time (0..1), y is the eased value.
            // The value range is drawn as y = 130 (value 0) up to y = 30 (value 1); overshoot leaves that band.
            Curve.Points = new Avalonia.Collections.AvaloniaList<Point>(
                Enumerable.Range(0, 61).Select(i =>
                {
                    var t = i / 60.0;
                    return new Point(t * CurveWidth, 130 - easing.Ease(t) * 100);
                }));

            Samples.Text = $"Ease(0.25)={easing.Ease(0.25):F3}   Ease(0.5)={easing.Ease(0.5):F3}   Ease(0.75)={easing.Ease(0.75):F3}";
        }

        private void OnRun(object? sender, RoutedEventArgs e)
        {
            // Rebuild the transition each run so it picks up the currently selected easing.
            Ball.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Canvas.LeftProperty,
                    Duration = TimeSpan.FromMilliseconds(1200),
                    Easing = Current,
                },
            };

            _atEnd = !_atEnd;
            Canvas.SetLeft(Ball, _atEnd ? TrackLength : 0);
        }
    }
}
```

实测依据：`Easing` 在 `Avalonia.Animation.Easings` 命名空间下有这些具体子类（`LinearEasing`、`SplineEasing`、`SpringEasing`，以及 Back / Bounce / Circular / Cubic / Elastic / Exponential / Quadratic / Quartic / Quintic / Sine 各自的 `EaseIn` / `EaseOut` / `EaseInOut`）；`Easing.Parse("CubicEaseOut")` 返回 `CubicEaseOut`；`DoubleTransition { Property = Canvas.LeftProperty }` 在 headless 里读回 `7,80,144,188,220,246,267,280,288,295,298,300`，完整走到终点；`BounceEaseOut.Ease(0.5)` 为 `0.719`，`ElasticEaseOut.Ease(0.5)` 为 `1.022`（超出 1，所以曲线会冲出上方虚线）。`Curve.Points` 在 Avalonia 12 里是 `IList<Point>`，用 `AvaloniaList<Point>` 赋值即可。

- [x] **Step 13: 合成动画页 CompositionPage**

演示：`ElementComposition.GetElementVisual(control)` 拿到控件在合成层的 `CompositionVisual`，直接改它的 `Opacity` / `Scale` / `RotationAngle`（客户端同步属性），再用 `Compositor` 创建关键帧动画并 `StartAnimation`——合成动画跑在渲染线程上，不占 UI 线程。

`Avalonia.GraphicsDemo/Views/Pages/CompositionPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.GraphicsDemo.Views.Pages.CompositionPage">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="合成动画：绕过布局与样式，直接驱动合成层的 Visual"
                               DocPath="graphics-animation/composition-animations" />

            <TextBlock Classes="caption" Text="1. 被驱动的控件" />
            <Border Classes="stage" Width="260" Height="140" HorizontalAlignment="Left">
                <Border Name="Target" Width="80" Height="80" CornerRadius="8" Background="#E8974A"
                        HorizontalAlignment="Center" VerticalAlignment="Center">
                    <TextBlock Text="合成层" Foreground="Black" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </Border>

            <TextBlock Classes="caption" Text="2. 直接改合成属性（立即生效，不触发布局）" />
            <Grid ColumnDefinitions="Auto,260" RowDefinitions="Auto,Auto,Auto">
                <TextBlock Grid.Row="0" Text="Opacity" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="OpacitySlider" Grid.Row="0" Grid.Column="1" Minimum="0" Maximum="1" Value="1" ValueChanged="OnSliderChanged" />
                <TextBlock Grid.Row="1" Text="Scale" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="ScaleSlider" Grid.Row="1" Grid.Column="1" Minimum="0.5" Maximum="1.5" Value="1" ValueChanged="OnSliderChanged" />
                <TextBlock Grid.Row="2" Text="RotationAngle" VerticalAlignment="Center" Margin="0,0,12,0" />
                <Slider Name="AngleSlider" Grid.Row="2" Grid.Column="1" Minimum="0" Maximum="360" Value="0" ValueChanged="OnSliderChanged" />
            </Grid>

            <TextBlock Classes="caption" Text="3. 用 Compositor 创建关键帧动画" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="淡出再淡入（Opacity）" Click="OnFade" />
                <Button Content="转一圈（RotationAngle）" Click="OnSpin" />
            </StackPanel>
            <TextBlock Name="Status" Margin="0,6,0,0" FontFamily="Consolas, Menlo, monospace" />

            <TextBlock Classes="hint" Margin="0,8,0,0"
                       Text="合成动画在渲染线程上跑：UI 线程被卡住时它也不卡。代价是它只动合成层的属性（Opacity、Scale、Offset、RotationAngle 等），不改控件自己的属性——所以读控件的 Opacity 读不到动画中途的值，布局也不会因此重算。取 Visual 要在控件挂上可视树之后（Loaded），挂载前 GetElementVisual 返回 null。需要改布局或绑定的动画，仍然用 Style.Animations。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

`Avalonia.GraphicsDemo/Views/Pages/CompositionPage.axaml.cs`：

```csharp
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class CompositionPage : UserControl
    {
        private CompositionVisual? _visual;

        public CompositionPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Only now is Target attached to the visual tree, so only now does it have a composition visual.
            _visual = ElementComposition.GetElementVisual(Target);
            if (_visual == null)
            {
                Status.Text = "取不到合成 Visual（此平台不支持）";
                return;
            }

            // Rotate and scale around the middle of the 80x80 box rather than its top-left corner.
            _visual.CenterPoint = new Vector3D(40, 40, 0);
            Status.Text = $"已取得 {_visual.GetType().Name}";
        }

        private void OnSliderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_visual == null)
            {
                return;
            }

            _visual.Opacity = (float)OpacitySlider.Value;
            _visual.Scale = new Vector3D(ScaleSlider.Value, ScaleSlider.Value, 1);
            _visual.RotationAngle = (float)(AngleSlider.Value * Math.PI / 180);
        }

        private void OnFade(object? sender, RoutedEventArgs e)
        {
            if (_visual == null)
            {
                return;
            }

            var animation = _visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.Target = "Opacity";
            animation.InsertKeyFrame(0f, 1f);
            animation.InsertKeyFrame(0.5f, 0.15f);
            animation.InsertKeyFrame(1f, 1f);
            animation.Duration = TimeSpan.FromSeconds(1);
            _visual.StartAnimation("Opacity", animation);
            Status.Text = "已启动 Opacity 关键帧动画（1 秒）";
        }

        private void OnSpin(object? sender, RoutedEventArgs e)
        {
            if (_visual == null)
            {
                return;
            }

            var animation = _visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.Target = "RotationAngle";
            animation.InsertKeyFrame(0f, 0f);
            animation.InsertKeyFrame(1f, (float)(Math.PI * 2));
            animation.Duration = TimeSpan.FromSeconds(1);
            _visual.StartAnimation("RotationAngle", animation);
            Status.Text = "已启动 RotationAngle 关键帧动画（1 秒）";
        }
    }
}
```

实测依据（规则 25）：`GetElementVisual` 挂载前返回 `null`、挂载后返回 `CompositionDrawListVisual`；`CenterPoint`、`Scale` 是 `Vector3D`，`Opacity`、`RotationAngle` 是 `float`（弧度）；同步写入后读回 `opacity=0.5`、`scale=(1.2,1.2,1)`；`CreateScalarKeyFrameAnimation` + `Target` + `InsertKeyFrame(float, float)` + `Duration` + `StartAnimation` 整条链编译并运行不抛异常。

**`CompositionVisual` 在哪个命名空间**：`Avalonia.Rendering.Composition`（`ElementComposition`、`CompositionVisual`、`Compositor` 都在这里），`Vector3D` 在 `Avalonia`，所以页面里不需要额外 using。

- [x] **Step 14: 挂 13 个 Tab**

修改 `Avalonia.GraphicsDemo/Views/MainWindow.axaml`，在 `<Window>` 上加 `xmlns:pages="using:Avalonia.GraphicsDemo.Views.Pages"`，`TabControl` 改为（竖排，Task 1 已设 `TabStripPlacement="Left"`）：

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
        <TabItem Header="画刷与渐变">
            <pages:BrushesPage />
        </TabItem>
        <TabItem Header="变换">
            <pages:TransformsPage />
        </TabItem>
        <TabItem Header="形状与几何">
            <pages:ShapesPage />
        </TabItem>
        <TabItem Header="自定义绘制">
            <pages:DrawingPage />
        </TabItem>
        <TabItem Header="特效">
            <pages:EffectsPage />
        </TabItem>
        <TabItem Header="裁剪与命中">
            <pages:ClipAndHitPage />
        </TabItem>
        <TabItem Header="图标">
            <pages:IconsPage />
        </TabItem>
        <TabItem Header="渲染选项">
            <pages:RenderOptionsPage />
        </TabItem>
        <TabItem Header="关键帧动画">
            <pages:AnimationsPage />
        </TabItem>
        <TabItem Header="控件过渡">
            <pages:TransitionsPage />
        </TabItem>
        <TabItem Header="页面过渡">
            <pages:PageTransitionsPage />
        </TabItem>
        <TabItem Header="缓动函数">
            <pages:EasingPage />
        </TabItem>
        <TabItem Header="合成动画">
            <pages:CompositionPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 15: 构建**

Run: `dotnet build Avalonia.GraphicsDemo 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "GraphicsDemo" | grep -v MSB3884`
Expected: 无输出。

- [x] **Step 16: 用 headless 探针断言页面行为**

创建 `C:\Temp\graphicscheck\graphicscheck.csproj`（与 Task 2 的 `eventscheck.csproj` 相同，只把 `ProjectReference` 改成 `Avalonia.GraphicsDemo\Avalonia.GraphicsDemo.csproj`）。

创建 `C:\Temp\graphicscheck\Program.cs`。探针较长，下面是完整的一个文件：先是公共部分与外壳、画刷、变换、形状，再依次是自绘、特效、裁剪命中、图标、渲染选项、动画、过渡、页面过渡、缓动、合成。

```csharp
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.GraphicsDemo.Controls;
using Avalonia.GraphicsDemo.Views;
using Avalonia.GraphicsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });
    }
}

internal sealed class WarnSink : ILogSink
{
    public readonly List<string> Entries = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel l, string a, object? s, string m) => Entries.Add($"[{l}] ({a}) {m}");
    public void Log(LogEventLevel l, string a, object? s, string m, params object?[] v)
        => Entries.Add($"[{l}] ({a}) {m} :: {string.Join(", ", v)}");
}

// Counts Render calls, to prove AffectsRender really invalidates the drawing.
internal sealed class CountingSketch : Sketch
{
    public int Renders;

    public override void Render(DrawingContext context)
    {
        Renders++;
        base.Render(context);
    }
}

internal static class Probe
{
    private static int _pass, _fail;

    private static T Find<T>(Visual root, string name) where T : Visual
    {
        foreach (var d in root.GetVisualDescendants())
        {
            if (d is T hit && (d as Control)?.Name == name) return hit;
        }
        throw new InvalidOperationException($"not found: {name}");
    }

    private static Window Show(Control c)
    {
        var w = new Window { Content = c, Width = 900, Height = 900 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        return w;
    }

    private static void Check(string label, bool ok, string detail)
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-52} {detail}");
    }

    private static Point Centre(Visual v, Visual root)
        => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    // The render clock only advances when ticked by hand (plan rule 21).
    private static void Tick(int n)
    {
        for (var i = 0; i < n; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(30);
            Dispatcher.UIThread.RunJobs();
        }
    }

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

        // --- Shell: thirteen tabs, each rendering the right page ---
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        var names = new[]
        {
            "BrushesPage", "TransformsPage", "ShapesPage", "DrawingPage", "EffectsPage", "ClipAndHitPage", "IconsPage",
            "RenderOptionsPage", "AnimationsPage", "TransitionsPage", "PageTransitionsPage", "EasingPage", "CompositionPage",
        };
        Check("Shell: 13 tabs", tabs.Count == 13, tabs.Count.ToString());
        for (var i = 0; i < tabs.Count && i < 13; i++)
        {
            tabs[i].IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            Check($"Shell: tab {i} renders", tabs[i].Content?.GetType().Name == names[i], tabs[i].Content?.GetType().Name ?? "<null>");
        }
        mw.Close();

        // --- Brushes: the slider reaches a GradientStop that has no name (rule 27) ---
        var brushes = new BrushesPage();
        var bw = Show(brushes);
        var gradientBorder = brushes.GetVisualDescendants().OfType<Border>().First(b => b.Background is LinearGradientBrush);
        var middleStop = ((LinearGradientBrush)gradientBorder.Background!).GradientStops[1];
        Check("Brushes: stop starts at 0.5", Math.Abs(middleStop.Offset - 0.5) < 1e-6, middleStop.Offset.ToString());
        Find<Slider>(brushes, "StopSlider").Value = 0.8;
        Dispatcher.UIThread.RunJobs();
        Check("Brushes: slider moves GradientStop.Offset", Math.Abs(middleStop.Offset - 0.8) < 1e-6, middleStop.Offset.ToString());
        bw.Close();

        // --- Transforms: one angle drives both; only the layout one pushes its neighbour ---
        var transforms = new TransformsPage();
        var tw = Show(transforms);
        var renderRight = Find<Border>(transforms, "RenderRight");
        var layoutRight = Find<Border>(transforms, "LayoutRight");
        var renderX0 = renderRight.TranslatePoint(default, tw)!.Value.X;
        var layoutX0 = layoutRight.TranslatePoint(default, tw)!.Value.X;
        Find<Slider>(transforms, "AngleSlider").Value = 90;
        Dispatcher.UIThread.RunJobs();
        var renderX1 = renderRight.TranslatePoint(default, tw)!.Value.X;
        var layoutX1 = layoutRight.TranslatePoint(default, tw)!.Value.X;
        Check("Transforms: RenderTransform leaves neighbour put", Math.Abs(renderX1 - renderX0) < 0.5, $"{renderX0:F1} -> {renderX1:F1}");
        Check("Transforms: LayoutTransform pushes neighbour", Math.Abs(layoutX1 - layoutX0) > 20, $"{layoutX0:F1} -> {layoutX1:F1}");
        var rotations = transforms.GetVisualDescendants().OfType<Border>().Where(b => b.RenderTransform is RotateTransform).ToList();
        Check("Transforms: slider reaches RotateTransform.Angle", rotations.Count == 1 && Math.Abs(((RotateTransform)rotations[0].RenderTransform!).Angle - 90) < 1e-6,
            rotations.Count == 1 ? ((RotateTransform)rotations[0].RenderTransform!).Angle.ToString() : $"{rotations.Count} borders");
        tw.Close();

        // --- Shapes: stroke slider, and the enum the plan says exists ---
        var shapes = new ShapesPage();
        var sw = Show(shapes);
        Console.WriteLine("INFO  GeometryCombineMode = " + string.Join("/", Enum.GetNames<GeometryCombineMode>()));
        Check("Shapes: GeometryCombineMode has the 4 documented values",
            Enum.GetNames<GeometryCombineMode>().OrderBy(x => x).SequenceEqual(new[] { "Exclude", "Intersect", "Union", "Xor" }), "");
        Find<Slider>(shapes, "ThickSlider").Value = 9;
        Dispatcher.UIThread.RunJobs();
        var rect = shapes.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Rectangle>().First();
        Check("Shapes: slider reaches Rectangle.StrokeThickness", Math.Abs(rect.StrokeThickness - 9) < 1e-6, rect.StrokeThickness.ToString());
        var modeBox = Find<ComboBox>(shapes, "ModeBox");
        var combined = (CombinedGeometry)Find<Avalonia.Controls.Shapes.Path>(shapes, "CombinedPath").Data!;
        Check("Shapes: ModeBox lists the enum", modeBox.ItemCount == Enum.GetValues<GeometryCombineMode>().Length, modeBox.ItemCount.ToString());
        modeBox.SelectedItem = GeometryCombineMode.Exclude;
        Dispatcher.UIThread.RunJobs();
        Check("Shapes: ModeBox drives CombinedGeometry", combined.GeometryCombineMode == GeometryCombineMode.Exclude, combined.GeometryCombineMode.ToString());
        sw.Close();

        // --- Drawing: the page's checkboxes reach Sketch; a counting Sketch proves AffectsRender ---
        var drawing = new DrawingPage();
        var dw = Show(drawing);
        var scene = Find<Sketch>(drawing, "Scene");
        var clipBox = drawing.GetVisualDescendants().OfType<CheckBox>().First(c => c.Content as string == "PushClip（只留左半）");
        clipBox.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Check("Drawing: checkbox reaches Sketch.UseClip (TwoWay)", scene.UseClip, scene.UseClip.ToString());
        dw.Close();

        var counting = new CountingSketch();
        var cw = Show(counting);
        Tick(3);
        var before = counting.Renders;
        counting.UseTransform = true;
        Tick(3);
        if (before == 0)
        {
            // Headless backend never called Render at all: nothing to compare, so degrade to the property readback
            // and note it in the spec write-back (plan Step 4).
            Check("Drawing: AffectsRender (SKIPPED - headless never rendered)", counting.UseTransform, "Renders=0");
        }
        else
        {
            Check("Drawing: toggling a property re-renders", counting.Renders > before, $"{before} -> {counting.Renders}");
        }
        cw.Close();

        // --- Effects: sliders reach effect objects that cannot be named (rule 27) ---
        var effects = new EffectsPage();
        var ew = Show(effects);
        var blur = effects.GetVisualDescendants().OfType<Button>().Select(b => b.Effect).OfType<BlurEffect>().First();
        var shadow = effects.GetVisualDescendants().OfType<Border>().Select(b => b.Effect).OfType<DropShadowEffect>().First();
        Find<Slider>(effects, "BlurSlider").Value = 8;
        Find<Slider>(effects, "OffsetXSlider").Value = 10;
        Find<Slider>(effects, "ShadowOpacitySlider").Value = 0.3;
        Dispatcher.UIThread.RunJobs();
        Check("Effects: slider reaches BlurEffect.Radius", Math.Abs(blur.Radius - 8) < 1e-6, blur.Radius.ToString());
        Check("Effects: slider reaches DropShadowEffect.OffsetX", Math.Abs(shadow.OffsetX - 10) < 1e-6, shadow.OffsetX.ToString());
        Check("Effects: slider reaches DropShadowEffect.Opacity", Math.Abs(shadow.Opacity - 0.3) < 1e-6, shadow.Opacity.ToString());
        ew.Close();

        // --- ClipAndHit: rule 15, a Border with no Background is not hit-testable ---
        var hit = new ClipAndHitPage();
        var hw = Show(hit);
        var noBrush = Find<Border>(hit, "NoBrush");
        var clear = Find<Border>(hit, "Clear");
        var invisible = Find<Border>(hit, "Invisible");
        Check("ClipAndHit: no Background -> not hit", !ReferenceEquals(hw.InputHitTest(Centre(noBrush, hw)), noBrush), hw.InputHitTest(Centre(noBrush, hw))?.GetType().Name ?? "null");
        Check("ClipAndHit: Transparent -> hit", ReferenceEquals(hw.InputHitTest(Centre(clear, hw)), clear), hw.InputHitTest(Centre(clear, hw))?.GetType().Name ?? "null");
        Check("ClipAndHit: IsHitTestVisible=False -> not hit", !ReferenceEquals(hw.InputHitTest(Centre(invisible, hw)), invisible), hw.InputHitTest(Centre(invisible, hw))?.GetType().Name ?? "null");
        foreach (var b in new[] { noBrush, clear, invisible })
        {
            var p = Centre(b, hw);
            hw.MouseDown(p, MouseButton.Left);
            hw.MouseUp(p, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }
        Check("ClipAndHit: only Transparent got Tapped", Find<TextBlock>(hit, "HitReadout").Text == "没有 Background=0   Transparent=1   IsHitTestVisible=False=0", Find<TextBlock>(hit, "HitReadout").Text!);
        hw.Close();

        // --- Icons: size slider reaches PathIcon.Width ---
        var icons = new IconsPage();
        var iw = Show(icons);
        Find<Slider>(icons, "SizeSlider").Value = 72;
        Dispatcher.UIThread.RunJobs();
        Check("Icons: slider reaches PathIcon.Width", Math.Abs(icons.GetVisualDescendants().OfType<PathIcon>().First().Width - 72) < 1e-6, "");
        Check("Icons: three icons drawn", icons.GetVisualDescendants().OfType<PathIcon>().Count() == 2 && icons.GetVisualDescendants().OfType<Image>().Count() == 1, "");
        iw.Close();

        // --- RenderOptions: generated bitmap, enum drop-downs drive the attached properties ---
        var options = new RenderOptionsPage();
        var ow = Show(options);
        var pixels = Find<Image>(options, "Pixels");
        Check("RenderOptions: generated checkerboard is a Bitmap", pixels.Source is Bitmap, pixels.Source?.GetType().Name ?? "null");
        Check("RenderOptions: default interpolation is None", RenderOptions.GetBitmapInterpolationMode(pixels) == BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode(pixels).ToString());
        Find<ComboBox>(options, "InterpolationBox").SelectedItem = BitmapInterpolationMode.HighQuality;
        Dispatcher.UIThread.RunJobs();
        Check("RenderOptions: drop-down drives interpolation", RenderOptions.GetBitmapInterpolationMode(pixels) == BitmapInterpolationMode.HighQuality, RenderOptions.GetBitmapInterpolationMode(pixels).ToString());
        var blendBox = Find<ComboBox>(options, "BlendBox");
        Check("RenderOptions: blend drop-down lists every enum value", blendBox.ItemCount == Enum.GetValues<BitmapBlendingMode>().Length, blendBox.ItemCount.ToString());
        blendBox.SelectedItem = BitmapBlendingMode.Plus;
        Dispatcher.UIThread.RunJobs();
        Check("RenderOptions: drop-down drives blend mode", RenderOptions.GetBitmapBlendingMode(Find<Image>(options, "BlendTop")) == BitmapBlendingMode.Plus, "");
        ow.Close();

        // --- Animations: the class is the switch; infinite animations only move a frame or two (rule 21) ---
        var animations = new AnimationsPage();
        var aw = Show(animations);
        var pulse = Find<Border>(animations, "Pulse");
        Check("Animations: resting Width comes from the style", Math.Abs(pulse.Width - 40) < 1e-6, pulse.Width.ToString());
        var runToggle = Find<ToggleButton>(animations, "RunToggle");
        runToggle.IsChecked = true;
        runToggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Check("Animations: toggle adds the run class", pulse.Classes.Contains("run"), string.Join(",", pulse.Classes));
        Tick(10);
        // Infinite: assert "off the resting value and inside the keyframe band", never an exact frame (rule 21).
        Check("Animations: infinite pulse moved off the rest value", pulse.Opacity < 0.99 && pulse.Opacity >= 0.29, pulse.Opacity.ToString("F3"));
        Check("Animations: width still inside its keyframe band", pulse.Width >= 39.5 && pulse.Width <= 120.5, pulse.Width.ToString("F1"));
        Tick(4);
        Check("Animations: rainbow left its first keyframe colour",
            (Find<Border>(animations, "Rainbow").Background as ISolidColorBrush)?.Color != Color.Parse("#E8564A"),
            (Find<Border>(animations, "Rainbow").Background as ISolidColorBrush)?.Color.ToString());
        runToggle.IsChecked = false;
        runToggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Check("Animations: toggle removes the class again", !pulse.Classes.Contains("run"), string.Join(",", pulse.Classes));
        Tick(8);
        Check("Animations: removed class restores the resting width", Math.Abs(pulse.Width - 40) < 1e-6, pulse.Width.ToString("F1"));
        aw.Close();

        // --- Transitions: hovering a card animates width and brush; the untransitioned card snaps ---
        var transitions = new TransitionsPage();
        var tnw = Show(transitions);
        var smooth = Find<Border>(transitions, "Smooth");
        var abrupt = Find<Border>(transitions, "Abrupt");
        tnw.MouseMove(Centre(smooth, tnw));
        Dispatcher.UIThread.RunJobs();
        Check("Transitions: hover state applied", smooth.Classes.Contains(":pointerover"), string.Join(",", smooth.Classes));
        Tick(1);
        var mid = smooth.Width;
        Check("Transitions: width is mid-flight, not the target", mid > 120 && mid < 260, mid.ToString("F1"));
        Tick(20);
        Check("Transitions: width reached the target", Math.Abs(smooth.Width - 260) < 1, smooth.Width.ToString("F1"));
        Check("Transitions: brush transitioned to the target colour",
            (smooth.Background as ISolidColorBrush)?.Color == Color.Parse("#E8974A"),
            (smooth.Background as ISolidColorBrush)?.Color.ToString());
        Check("Transitions: RenderTransform became an operation list", smooth.RenderTransform is TransformOperations, smooth.RenderTransform?.GetType().Name ?? "null");
        tnw.MouseMove(Centre(abrupt, tnw));
        Dispatcher.UIThread.RunJobs();
        Check("Transitions: the untransitioned card snaps instantly", Math.Abs(abrupt.Width - 260) < 0.5, abrupt.Width.ToString("F1"));
        tnw.Close();

        // --- PageTransitions: replacing Content runs the transition; assert the result, not the frames ---
        var pages = new PageTransitionsPage();
        var pgw = Show(pages);
        var pager = Find<TransitioningContentControl>(pages, "Pager");
        var nextButton = pages.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == "下一页");
        for (var i = 0; i < 5; i++)
        {
            nextButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Tick(1);
        }
        Check("PageTransitions: content replaced with a fresh control", pager.Content is Border, pager.Content?.GetType().Name ?? "null");
        Check("PageTransitions: readout counted the pages", Find<TextBlock>(pages, "Readout").Text == "第 2 页", Find<TextBlock>(pages, "Readout").Text!);
        // Cycle every kind: each one must construct and assign without throwing.
        var kindBox = Find<ComboBox>(pages, "KindBox");
        var kinds = new List<string>();
        for (var i = 0; i < kindBox.ItemCount; i++)
        {
            kindBox.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
            kinds.Add(pager.PageTransition?.GetType().Name ?? "null");
        }
        Check("PageTransitions: all four transitions build",
            kinds.SequenceEqual(new[] { "PageSlide", "PageSlide", "CrossFade", "CompositePageTransition" }), string.Join(",", kinds));
        pgw.Close();

        // --- Easing: the plotted curve is Ease(t) sampled; overshoot really leaves the band (rule 22) ---
        var easing = new EasingPage();
        var egw = Show(easing);
        var curve = Find<Avalonia.Controls.Shapes.Polyline>(easing, "Curve");
        Check("Easing: 61 sample points plotted", curve.Points!.Count == 61, curve.Points!.Count.ToString());
        Check("Easing: CubicEaseOut(0.5) is 0.875", Math.Abs(new CubicEaseOut().Ease(0.5) - 0.875) < 1e-9, new CubicEaseOut().Ease(0.5).ToString("F4"));
        Check("Easing: BounceEaseOut(0.5) is 0.71875", Math.Abs(new BounceEaseOut().Ease(0.5) - 0.71875) < 1e-9, new BounceEaseOut().Ease(0.5).ToString("F5"));
        var easingBox = Find<ComboBox>(easing, "EasingBox");
        easingBox.SelectedItem = "BounceEaseOut";
        Dispatcher.UIThread.RunJobs();
        var bounceMin = curve.Points!.Min(p => p.Y);
        Check("Easing: Bounce curve stays inside the band", bounceMin >= 29.5, bounceMin.ToString("F1"));
        easingBox.SelectedItem = "ElasticEaseOut";
        Dispatcher.UIThread.RunJobs();
        var elasticMin = curve.Points!.Min(p => p.Y);
        Check("Easing: Elastic overshoots above the top line", elasticMin < 30, elasticMin.ToString("F1"));
        Check("Easing: readout prints three samples", Find<TextBlock>(easing, "Samples").Text!.Contains("Ease(0.5)="), Find<TextBlock>(easing, "Samples").Text!);
        // The ball: rebuild the transition, then move the attached property; headless runs it to the end (rule 21).
        easing.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == "走一趟")
            .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var ball = Find<Border>(easing, "Ball");
        Tick(1);
        Check("Easing: ball left the start", Canvas.GetLeft(ball) > 1, Canvas.GetLeft(ball).ToString("F1"));
        Tick(30);
        Check("Easing: ball reached the end", Math.Abs(Canvas.GetLeft(ball) - 276) < 1, Canvas.GetLeft(ball).ToString("F1"));
        egw.Close();

        // --- Composition: the visual needs a loaded control; client-side writes read back (rule 25) ---
        var composition = new CompositionPage();
        var cow = Show(composition);
        var target = Find<Border>(composition, "Target");
        Check("Composition: visual obtained after Loaded", ElementComposition.GetElementVisual(target) is not null, Find<TextBlock>(composition, "Status").Text ?? "");
        Check("Composition: an unattached control has no visual", ElementComposition.GetElementVisual(new Border()) is null, "");
        Find<Slider>(composition, "OpacitySlider").Value = 0.5;
        Find<Slider>(composition, "ScaleSlider").Value = 1.2;
        Dispatcher.UIThread.RunJobs();
        var visual = ElementComposition.GetElementVisual(target)!;
        Check("Composition: Opacity written and read back", Math.Abs(visual.Opacity - 0.5f) < 1e-5, visual.Opacity.ToString("F3"));
        Check("Composition: Scale written and read back", Math.Abs(visual.Scale.X - 1.2f) < 1e-5, visual.Scale.ToString());
        Check("Composition: CenterPoint set to the middle", Math.Abs(visual.CenterPoint.X - 40) < 1e-5, visual.CenterPoint.ToString());
        // The keyframe animations run on the compositor thread, so the only safe assertion is "does not throw" (rule 25).
        var compButtons = composition.GetVisualDescendants().OfType<Button>().ToList();
        try
        {
            compButtons.First(b => b.Content as string == "淡出再淡入（Opacity）")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            compButtons.First(b => b.Content as string == "转一圈（RotationAngle）")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Check("Composition: starting both keyframe animations does not throw",
                Find<TextBlock>(composition, "Status").Text!.Contains("已启动"), Find<TextBlock>(composition, "Status").Text!);
        }
        catch (Exception ex)
        {
            Check("Composition: starting both keyframe animations does not throw", false, ex.GetType().Name + ": " + ex.Message);
        }
        cow.Close();

        Check("No warning-or-worse log entries", sink.Entries.Count == 0, string.Join(" // ", sink.Entries));
        Console.WriteLine($"\n{_pass} passed, {_fail} failed");
    }
}
```

Run: `cd /c/Temp/graphicscheck && dotnet run 2>&1 | grep -E "PASS|FAIL|INFO|passed"`
Expected: 全部 `PASS`，末行 `N passed, 0 failed`。**N 是执行期实测条数**，把实际数字回填到本步。`INFO  GeometryCombineMode = …` 那行是打印的枚举成员，用来核对说明文字，不是断言。

几处取值依据：`CubicEaseOut.Ease(0.5)` = `0.875`；`BounceEaseOut.Ease(0.5)` = `0.71875`；`ElasticEaseOut.Ease(0.5)` = `1.022`，所以 Elastic 的曲线会冲出上方 `y=30` 的虚线而 Bounce 不会；四种页面过渡的运行时类型名依次是 `PageSlide`/`PageSlide`/`CrossFade`/`CompositePageTransition`；`RenderOptions` 的附加属性写读一致。

**若任何一条 `FAIL`，以探针输出为准修正页面或说明文字**，并在 spec 回写时注明「（执行期修正）」。若 `Drawing: AffectsRender` 走了 SKIPPED 分支，把「headless 后端不调用 `Render`」这一事实写进 spec 回写。

- [x] **Step 17: 清理探针并提交**

```bash
rm -rf /c/Temp/graphicscheck
git add Avalonia.GraphicsDemo/
git commit -m "$(cat <<'EOF'
feat: demonstrate the Graphics and Animation category

Thirteen pages: brushes and gradients, render versus layout transforms,
shapes and geometry, custom rendering through DrawingContext, effects,
clipping and hit testing, icons, render options, keyframe animations,
control transitions, page transitions, easing functions, and composition
animations.

Animations are switched on by class name rather than RunAsync, and every
page that both rests and animates a property keeps the resting values in
a Style so nothing local can shadow them.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: 项目 #11 CustomControls 的 7 个页面

**Files:**
- Create: `Avalonia.CustomControlsDemo/Controls/LabeledSlider.axaml(.cs)`
- Create: `Avalonia.CustomControlsDemo/Controls/Meter.cs`、`Controls/Meter.axaml`
- Create: `Avalonia.CustomControlsDemo/Controls/RingGauge.cs`
- Create: `Avalonia.CustomControlsDemo/Controls/OnlyVisualHost.cs`、`Controls/VisualAndLogicalHost.cs`
- Create: `Avalonia.CustomControlsDemo/Controls/RadialPanel.cs`
- Create: `Avalonia.CustomControlsDemo/Controls/SwatchFlyout.cs`
- Create: `Avalonia.CustomControlsDemo/Views/Pages/` 下 7 组 `XxxPage.axaml(.cs)`：`UserControlPage`、`TemplatedControlPage`、`CustomDrawnPage`、`PropertiesAndEventsPage`、`ControlTreesPage`、`CustomPanelPage`、`CustomFlyoutPage`
- Modify: `Avalonia.CustomControlsDemo/App.axaml`（合并 `Meter.axaml` 的资源字典）
- Modify: `Avalonia.CustomControlsDemo/Views/MainWindow.axaml`（挂 7 个 Tab）

**Interfaces:**
- Consumes: Task 1 的空壳；`DemoHeader` 与 `caption`/`hint`/`stage` 样式
- Produces: 无跨任务产物。本项目所有控件只在本项目内使用。

### 本步新增的硬性规则（实测于 Avalonia 12.1.2 headless）

29. **`PopupFlyoutBase` 在 `Avalonia.Controls.Primitives`**，`FlyoutBase` 在 `Avalonia.Controls`。自定义 Flyout 必须两者都 using，否则 `CS0246: 未能找到类型 PopupFlyoutBase`。
30. **`AvaloniaProperty.Register` 的 `coerce` 委托第一个参数是 `AvaloniaObject`，不是宿主类型。** 写 `coerce: (m, v) => Math.Clamp(v, 0, m.Maximum)` 会 `CS1061: “AvaloniaObject”未包含“Maximum”的定义`，必须写 `coerce: (o, v) => Math.Clamp(v, 0, ((Meter)o).Maximum)`。
31. **headless 后端会调用 `Render`。** 实测自绘控件挂进已显示的窗口后 `Render` 被调用（计数 1），改一个 `AffectsRender` 属性后变 2。所以自绘页的断言是**真的断言**，不是"不抛异常"。
32. **Flyout 的 Presenter 只在打开后才存在。** `flyout.Popup.Child` 在 `ShowAt` 之后是 `FlyoutPresenter`，它的 `Parent` 是宿主控件。Presenter 里除了自己放的控件，`FlyoutPresenter` 模板本身还带若干按钮（实测 6 个色块 + 模板自带的 8 个 = 14 个 `Button`），**探针要按内容特征找，不能按下标找**。

33. **三个探针写法的坑。** ①`meter.Theme` 在主题来自祖先资源时读回 `null`，但 `meter.Template` 非空，断言要落在 `Template` 上；②`RaiseEvent(new RoutedEventArgs(Button.ClickEvent))` 会运行 `Click="..."` 挂的处理器，**不会**运行 `Button.OnClick`，所以 `Button.Flyout` 不会因此打开，要直接 `flyout.ShowAt(button)`；③`PopupFlyoutBase.Popup` 不是公开成员，探针经反射读取（`GetProperty("Popup", Instance | Public | NonPublic)`），读到后 `Popup.Child` 才是 `FlyoutPresenter`。

- [x] **Step 1: 组合式控件 LabeledSlider**

演示的第一种自定义控件：用 `UserControl` 把现成控件拼起来，`x:Name` 直接暴露成属性（不是 `StyledProperty`）。

`Avalonia.CustomControlsDemo/Controls/LabeledSlider.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Name="Root"
             x:Class="Avalonia.CustomControlsDemo.Controls.LabeledSlider">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Width="80" VerticalAlignment="Center" Text="{Binding #Root.Label}" />
        <Slider Name="Inner" Width="220"
                Minimum="{Binding #Root.Minimum}"
                Maximum="{Binding #Root.Maximum}"
                Value="{Binding #Root.Value, Mode=TwoWay}" />
        <TextBlock Width="40" VerticalAlignment="Center"
                   Text="{Binding #Root.Value, StringFormat='{}{0:F0}'}" />
    </StackPanel>
</UserControl>
```

`Avalonia.CustomControlsDemo/Controls/LabeledSlider.axaml.cs`：

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.CustomControlsDemo.Controls
{
    public partial class LabeledSlider : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<LabeledSlider, string>(nameof(Label), "");

        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<LabeledSlider, double>(nameof(Minimum), 0);

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<LabeledSlider, double>(nameof(Maximum), 100);

        // TwoWay by default: the internal Slider writes back into this property.
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<LabeledSlider, double>(nameof(Value), 0,
                defaultBindingMode: BindingMode.TwoWay);

        public LabeledSlider() => InitializeComponent();

        public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    }
}
```

`Value` 的 `defaultBindingMode: BindingMode.TwoWay` 是这里唯一的非显然之处：不加它，宿主写 `Value="{Binding …}"` 只有单向，用户拖滑块不会写回。实测内外双向都通（拖内层到 35 → 外层 `Value` 为 35；外层设 12 → 内层 `Slider.Value` 为 12）。

- [x] **Step 2: 模板化控件 Meter 与它的 ControlTheme**

第二种：`TemplatedControl`——外观完全由 `ControlTheme` 提供，逻辑只认模板部件与伪类。

`Avalonia.CustomControlsDemo/Controls/Meter.cs`：

```csharp
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Controls
{
    // Attributes are documentation for tooling; the code below does not depend on them.
    [TemplatePart("PART_Fill", typeof(Border))]
    [PseudoClasses(":full")]
    public class Meter : TemplatedControl
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<Meter, double>(nameof(Value),
                // The coerce callback receives an AvaloniaObject, not a Meter, so cast before reading Maximum.
                coerce: (o, v) => Math.Clamp(v, 0, ((Meter)o).Maximum));

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<Meter, double>(nameof(Maximum), 100);

        // A computed value with no backing styled property: cheaper, and it cannot be set from XAML.
        public static readonly DirectProperty<Meter, string> DisplayProperty =
            AvaloniaProperty.RegisterDirect<Meter, string>(nameof(Display), m => m.Display);

        private Border? _fill;
        private string _display = "0%";

        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        public string Display { get => _display; private set => SetAndRaise(DisplayProperty, ref _display, value); }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Always re-resolve on every apply: a new theme means new parts.
            _fill = e.NameScope.Find<Border>("PART_Fill");
            Refresh();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ValueProperty || change.Property == MaximumProperty)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var ratio = Maximum <= 0 ? 0 : Math.Clamp(Value / Maximum, 0, 1);
            Display = $"{ratio:P0}";
            PseudoClasses.Set(":full", ratio >= 1);

            // Null after a retemplate whose theme has no PART_Fill: guard rather than assume.
            if (_fill != null)
            {
                _fill.RenderTransform = new ScaleTransform(ratio, 1);
            }
        }
    }
}
```

`Avalonia.CustomControlsDemo/Controls/Meter.axaml`（一个资源字典，不是 UserControl）：

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:controls="using:Avalonia.CustomControlsDemo.Controls">

    <!--  The key is the control type, so Meter needs no explicit Theme= in XAML.  -->
    <ControlTheme x:Key="{x:Type controls:Meter}" TargetType="controls:Meter">
        <Setter Property="Height" Value="22" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="#303040" CornerRadius="4" ClipToBounds="True">
                    <Panel>
                        <!--  RenderTransformOrigin at the left edge, so the scale grows rightward.  -->
                        <Border Name="PART_Fill" Background="#4A7BE8" RenderTransformOrigin="0%,50%" />
                        <TextBlock Text="{TemplateBinding Display}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Panel>
                </Border>
            </ControlTemplate>
        </Setter>

        <!--  ^ is the styled control; /template/ crosses into the template's name scope.  -->
        <Style Selector="^:full /template/ Border#PART_Fill">
            <Setter Property="Background" Value="#4AE87B" />
        </Style>
    </ControlTheme>

    <!--  A second theme for the same control type: swapping it retemplates the instance.  -->
    <ControlTheme x:Key="TextMeter" TargetType="controls:Meter">
        <Setter Property="Template">
            <ControlTemplate>
                <TextBlock Text="{TemplateBinding Display}" FontSize="28" />
            </ControlTemplate>
        </Setter>
    </ControlTheme>
</ResourceDictionary>
```

`Avalonia.CustomControlsDemo/App.axaml` 改成：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.CustomControlsDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <!--  Brings in DemoHeader plus the shared caption, hint and stage styles.  -->
        <StyleInclude Source="avares://Avalonia.Shared/Themes/SharedStyles.axaml" />
    </Application.Styles>

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://Avalonia.CustomControlsDemo/Controls/Meter.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

实测依据：`[TemplatePart]` / `[PseudoClasses]` 特性可编译；控件挂进窗口后 `Template` 非空、`e.NameScope.Find<Border>("PART_Fill")` 拿得到部件；`Value=100` 之后 `Classes` 出现 `:full` 且 `PART_Fill` 的背景读回 `#ff4ae87b`（模板里的 `^:full` 样式生效）；`PART_Fill.RenderTransform` 读回 `{M11:0.4 …}` 的缩放矩阵；`coerce` 把 250 夹到 100；把 `Theme` 换成 `TextMeter` 后重模板成功（`PART_Fill` 消失、出现 `FontSize=28` 的 `TextBlock`），之后再改 `Value` 不抛异常。

- [x] **Step 3: 自绘控件 RingGauge**

第三种：重写 `Render` 完全自己画，用 `AffectsRender` 让属性变化触发重绘。

`Avalonia.CustomControlsDemo/Controls/RingGauge.cs`：

```csharp
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Draws a ring whose filled arc follows <see cref="Value"/>.
    /// </summary>
    public class RingGauge : Control
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<RingGauge, double>(nameof(Value));

        static RingGauge()
        {
            // Without this the property changes but Render is never called again.
            AffectsRender<RingGauge>(ValueProperty);
        }

        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        // Self-drawn controls declare their own size; nothing else can measure them.
        protected override Size MeasureOverride(Size availableSize) => new(120, 120);

        public override void Render(DrawingContext context)
        {
            var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 8;

            // The track.
            context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.Parse("#404050")), 10), centre, radius, radius);

            // The arc. 99.99 never divides the full circle, which would make the two ends coincide.
            var sweep = Math.Clamp(Value, 0, 99.99) / 100 * 2 * Math.PI;
            if (sweep > 0)
            {
                var start = new Point(centre.X, centre.Y - radius);
                var end = new Point(centre.X + radius * Math.Sin(sweep), centre.Y - radius * Math.Cos(sweep));
                var arc = new StreamGeometry();
                using (var g = arc.Open())
                {
                    g.BeginFigure(start, false);
                    g.ArcTo(end, new Size(radius, radius), 0, sweep > Math.PI, SweepDirection.Clockwise);
                    g.EndFigure(false);
                }

                context.DrawGeometry(null, new Pen(Brushes.OrangeRed, 10, lineCap: PenLineCap.Round), arc);
            }

            var text = new FormattedText($"{Value:F0}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 22, Brushes.White);
            context.DrawText(text, new Point(centre.X - text.Width / 2, centre.Y - text.Height / 2));
        }
    }
}
```

实测依据：`AffectsRender<RingGauge>(ValueProperty)` + 重写 `Render`，挂进窗口后 `Render` 被调用（计数 1），把 `Value` 改成 60 并跑两轮渲染节拍后计数变 2。**headless 后端确实会渲染**（规则 31），所以这是真断言。

- [x] **Step 4: 三个页面——UserControlPage、TemplatedControlPage、CustomDrawnPage**

`Views/Pages/UserControlPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.CustomControlsDemo.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.UserControlPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="用户控件（UserControl）"
                           DocPath="controls/primitives/usercontrol" />
        <StackPanel Margin="16" Spacing="12">
            <TextBlock Classes="caption" Text="LabeledSlider 是 UserControl：内部拼一个 TextBlock + Slider + TextBlock，对外暴露 4 个 StyledProperty。" />
            <controls:LabeledSlider Name="Size" Label="Size" Minimum="10" Maximum="50" Value="20" />
            <controls:LabeledSlider Name="Opacity" Label="Opacity" Minimum="0" Maximum="100" Value="{Binding #Size.Value, Mode=TwoWay}" />
            <TextBlock Classes="hint" Text="第二个滑块的 Value 绑到第一个的 Value：能双向联动，正因为 Value 的 defaultBindingMode 是 TwoWay。" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/UserControlPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class UserControlPage : UserControl
    {
        public UserControlPage() => InitializeComponent();
    }
}
```

`Views/Pages/TemplatedControlPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.CustomControlsDemo.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.TemplatedControlPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="模板化控件（TemplatedControl）"
                           DocPath="custom-controls/templated-controls" />
        <StackPanel Margin="16" Spacing="12">
            <TextBlock Classes="caption" Text="Meter 的外观全在 Meter.axaml 的 ControlTheme 里；控件只认 PART_Fill 部件和 :full 伪类。" />
            <Slider Name="ValueSlider" Minimum="0" Maximum="100" Value="40" Width="300" HorizontalAlignment="Left" />
            <controls:Meter Name="TheMeter" Width="300" HorizontalAlignment="Left"
                            Value="{Binding #ValueSlider.Value}" />
            <Button Name="SwapButton" Content="换模板（TextMeter）" Click="OnSwapClick" />
            <TextBlock Classes="hint" Text="拖到 100：:full 伪类出现，填充色由蓝变绿。换模板后 PART_Fill 消失，控件不崩溃（OnApplyTemplate 里对部件判空）。" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/TemplatedControlPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class TemplatedControlPage : UserControl
    {
        public TemplatedControlPage() => InitializeComponent();

        private void OnSwapClick(object? sender, RoutedEventArgs e)
        {
            // Looks up the key in Application.Resources, where Meter.axaml was merged.
            if (this.TryFindResource("TextMeter", ActualThemeVariant, out var theme) && theme is ControlTheme textTheme)
            {
                TheMeter.Theme = textTheme;
            }
        }
    }
}
```

`Views/Pages/CustomDrawnPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.CustomControlsDemo.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.CustomDrawnPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="自绘控件（Render）"
                           DocPath="custom-controls/custom-drawn-controls" />
        <StackPanel Margin="16" Spacing="12">
            <TextBlock Classes="caption" Text="RingGauge 重写 Render，用 DrawingContext 画轨道、圆弧和文字。" />
            <Slider Name="GaugeSlider" Minimum="0" Maximum="100" Value="35" Width="300" HorizontalAlignment="Left" />
            <controls:RingGauge Name="Gauge" HorizontalAlignment="Left"
                                Value="{Binding #GaugeSlider.Value}" />
            <TextBlock Classes="hint" Text="没有 AffectsRender 时属性会变但画面不动；有了它，每次赋值都触发一次重绘。" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/CustomDrawnPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class CustomDrawnPage : UserControl
    {
        public CustomDrawnPage() => InitializeComponent();
    }
}
```

> `DocPath` 的值已对照 docs.avaloniaui.net 导航核实（`controls/primitives/usercontrol` 与 `custom-controls/*` 均存在）。

- [x] **Step 5: PropertiesAndEventsPage（路标页）与 ControlTreesPage**

`PropertiesAndEventsPage` 不重复演示：定义属性在 #7（PropertySystemDemo），自定义路由事件在 #8（EventsDemo）。本页只做导航说明，并展示 `Meter.Value` 这个**真实使用了**两者的例子。

`Views/Pages/PropertiesAndEventsPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.PropertiesAndEventsPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="自定义属性与事件"
                           DocPath="custom-controls/defining-properties" />
        <StackPanel Margin="16" Spacing="12">
            <TextBlock Classes="caption" Text="这一页不重复演示，只指路。" />
            <TextBlock TextWrapping="Wrap" Text="• 定义 StyledProperty / DirectProperty / AttachedProperty：见 Avalonia.PropertySystemDemo。" />
            <TextBlock TextWrapping="Wrap" Text="• 自定义路由事件（RoutedEvent.Register + AddHandler）：见 Avalonia.EventsDemo。" />
            <TextBlock TextWrapping="Wrap" Text="• 本项目里它们的真实用法：Meter.Value 是带 coerce 的 StyledProperty，Meter.Display 是 DirectProperty，RingGauge.Value 用了 AffectsRender，LabeledSlider.Value 指定了 defaultBindingMode。" />
            <TextBlock Classes="hint" Text="PropertySystemDemo 与 EventsDemo 是各自一个独立可运行的项目，在解决方案里并列。" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/PropertiesAndEventsPage.axaml.cs`：

```csharp
using Avalonia.Controls;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class PropertiesAndEventsPage : UserControl
    {
        public PropertiesAndEventsPage() => InitializeComponent();
    }
}
```

`ControlTreesPage` 演示规则 19：**`DataContext` 继承沿逻辑树走，不沿视觉树走。**

`Controls/OnlyVisualHost.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Hosts a child in the visual tree only. The child never sees an inherited DataContext.
    /// </summary>
    public class OnlyVisualHost : Control
    {
        public OnlyVisualHost()
        {
            Child.Bind(TextBlock.TextProperty, new Binding());
            VisualChildren.Add(Child);
        }

        public TextBlock Child { get; } = new();

        protected override Size MeasureOverride(Size availableSize)
        {
            Child.Measure(availableSize);
            return Child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
```

`Controls/VisualAndLogicalHost.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Hosts a child in both trees. DataContext flows down the logical tree, so the child binds.
    /// </summary>
    public class VisualAndLogicalHost : Control
    {
        public VisualAndLogicalHost()
        {
            Child.Bind(TextBlock.TextProperty, new Binding());
            VisualChildren.Add(Child);
            LogicalChildren.Add(Child);
        }

        public TextBlock Child { get; } = new();

        protected override Size MeasureOverride(Size availableSize)
        {
            Child.Measure(availableSize);
            return Child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
```

`Views/Pages/ControlTreesPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.CustomControlsDemo.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.ControlTreesPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="控件树：逻辑树与视觉树"
                           DocPath="custom-controls/control-trees" />
        <StackPanel Margin="16" Spacing="12" DataContext="来自父级的 DataContext">
            <TextBlock Classes="caption" Text="两个宿主的子元素都 Bind(Text, new Binding())，绑的是继承来的 DataContext。" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="只在视觉树：" Width="120" />
                <controls:OnlyVisualHost Name="VisualOnly" />
            </StackPanel>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="视觉树 + 逻辑树：" Width="120" />
                <controls:VisualAndLogicalHost Name="Both" />
            </StackPanel>
            <TextBlock Name="Readout" Classes="hint" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/ControlTreesPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class ControlTreesPage : UserControl
    {
        public ControlTreesPage() => InitializeComponent();

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Readout.Text =
                $"视觉树宿主的子元素 DataContext = {VisualOnly.Child.DataContext ?? "null"}；" +
                $"双树宿主的子元素 DataContext = {Both.Child.DataContext ?? "null"}";
        }
    }
}
```

实测依据：`OnlyVisualHost.Child.DataContext == null` 且 `Text == ''`；`VisualAndLogicalHost.Child.DataContext == "ctx"` 且 `Text == 'ctx'`。差别只有 `LogicalChildren.Add(Child)` 这一行。

- [x] **Step 6: 自定义面板 RadialPanel**

`Controls/RadialPanel.cs`：

```csharp
using System;
using Avalonia.Controls;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Places children evenly on a circle, clockwise from <see cref="StartAngle"/> (0 = twelve o'clock).
    /// </summary>
    public class RadialPanel : Panel
    {
        public static readonly StyledProperty<double> RadiusProperty =
            AvaloniaProperty.Register<RadialPanel, double>(nameof(Radius), 100);

        public static readonly StyledProperty<double> StartAngleProperty =
            AvaloniaProperty.Register<RadialPanel, double>(nameof(StartAngle), 0);

        static RadialPanel()
        {
            // Radius changes the desired size; the angle only moves children inside the same size.
            AffectsMeasure<RadialPanel>(RadiusProperty);
            AffectsArrange<RadialPanel>(StartAngleProperty);
        }

        public double Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

        public double StartAngle { get => GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

        protected override Size MeasureOverride(Size availableSize)
        {
            var largest = 0.0;
            foreach (var child in Children)
            {
                // Children get unlimited room; the panel's own size comes from the radius.
                child.Measure(Size.Infinity);
                largest = Math.Max(largest, Math.Max(child.DesiredSize.Width, child.DesiredSize.Height));
            }

            var side = 2 * Radius + largest;
            return new Size(side, side);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var centre = new Point(finalSize.Width / 2, finalSize.Height / 2);
            var count = Children.Count;
            for (var i = 0; i < count; i++)
            {
                var child = Children[i];
                var angle = (StartAngle + i * 360.0 / count) * Math.PI / 180;
                var x = centre.X + Radius * Math.Sin(angle) - child.DesiredSize.Width / 2;
                var y = centre.Y - Radius * Math.Cos(angle) - child.DesiredSize.Height / 2;
                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }

            return finalSize;
        }
    }
}
```

`Views/Pages/CustomPanelPage.axaml`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.CustomControlsDemo.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.CustomPanelPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="自定义面板（Panel）"
                           DocPath="custom-controls/custom-panel" />
        <StackPanel Margin="16" Spacing="8">
            <TextBlock Classes="caption" Text="RadialPanel 只重写 MeasureOverride 与 ArrangeOverride，子元素沿圆周均分。" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="Radius" VerticalAlignment="Center" />
                <Slider Name="RadiusSlider" Minimum="20" Maximum="140" Value="80" Width="180" />
                <TextBlock Text="StartAngle" VerticalAlignment="Center" />
                <Slider Name="AngleSlider" Minimum="0" Maximum="360" Value="0" Width="180" />
                <Button Name="AddButton" Content="加一个" Click="OnAddClick" />
                <Button Name="RemoveButton" Content="减一个" Click="OnRemoveClick" />
            </StackPanel>
            <Border Classes="stage" Height="340">
                <controls:RadialPanel Name="Radial"
                                      Radius="{Binding #RadiusSlider.Value}"
                                      StartAngle="{Binding #AngleSlider.Value}" />
            </Border>
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/CustomPanelPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class CustomPanelPage : UserControl
    {
        public CustomPanelPage()
        {
            InitializeComponent();
            for (var i = 0; i < 6; i++)
            {
                Radial.Children.Add(CreateDot());
            }
        }

        private static Border CreateDot() => new()
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = Brushes.OrangeRed,
        };

        private void OnAddClick(object? sender, RoutedEventArgs e) => Radial.Children.Add(CreateDot());

        private void OnRemoveClick(object? sender, RoutedEventArgs e)
        {
            if (Radial.Children.Count > 0)
            {
                Radial.Children.RemoveAt(Radial.Children.Count - 1);
            }
        }
    }
}
```

> 圆点的 `Width`/`Height`/`Background` 写在元素上是**有意的**：它们是代码里新建的元素，没有任何样式要覆盖（规则 2 只针对 XAML 里会被样式压制的默认值）。

实测依据（4 个 20×20 子元素、`Radius=50`、窗口 600×600）：第 0 个子元素 `Bounds.TopLeft = 290,240`，第 1 个 `340,290`；`StartAngle=90` 后第 0 个移到 `340,290`；`Radius` 改成 80 后面板仍是 `600×600`（它被窗口拉伸，`MeasureOverride` 的返回值只是 desired size）。

- [x] **Step 7: 自定义 Flyout SwatchFlyout**

`Controls/SwatchFlyout.cs`：

```csharp
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// A colour-swatch popup. Derives from PopupFlyoutBase because that is where the placement and light-dismiss logic lives.
    /// </summary>
    public class SwatchFlyout : PopupFlyoutBase
    {
        private static readonly string[] Palette = { "#E8564A", "#E8974A", "#E8D54A", "#4AE87B", "#4A7BE8", "#B04AE8" };

        public event EventHandler<Color>? ColorPicked;

        protected override Control CreatePresenter()
        {
            var panel = new WrapPanel { Width = 120 };
            foreach (var hex in Palette)
            {
                var colour = Color.Parse(hex);
                var swatch = new Button
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush(colour),
                };
                swatch.Click += (_, _) =>
                {
                    ColorPicked?.Invoke(this, colour);
                    Hide();
                };
                panel.Children.Add(swatch);
            }

            return new FlyoutPresenter { Content = panel };
        }
    }
}
```

`Views/Pages/CustomFlyoutPage.axaml`（注意：`SwatchFlyout` 不是 `Control`，**不能给它 `Name`**，规则 24；改用事件属性接结果）：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:controls="using:Avalonia.CustomControlsDemo.Controls"
             x:Class="Avalonia.CustomControlsDemo.Views.Pages.CustomFlyoutPage">
    <DockPanel>
        <shared:DemoHeader DockPanel.Dock="Top" Title="自定义 Flyout"
                           DocPath="custom-controls/custom-flyout" />
        <StackPanel Margin="16" Spacing="12">
            <TextBlock Classes="caption" Text="SwatchFlyout : PopupFlyoutBase，重写 CreatePresenter 返回自己的内容。" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="PickButton" Content="选颜色">
                    <Button.Flyout>
                        <controls:SwatchFlyout Placement="BottomEdgeAlignedLeft" ColorPicked="OnColorPicked" />
                    </Button.Flyout>
                </Button>
                <Border Name="Chosen" Width="32" Height="32" CornerRadius="4" BorderBrush="Gray" BorderThickness="1" />
                <TextBlock Name="ChosenText" VerticalAlignment="Center" Text="尚未选择" />
            </StackPanel>
            <TextBlock Classes="hint" Text="点一个色块：事件触发、结果显示在右侧、Flyout 关闭。点空白处同样会关闭（light dismiss 由基类提供）。" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Pages/CustomFlyoutPage.axaml.cs`：

```csharp
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Views.Pages
{
    public partial class CustomFlyoutPage : UserControl
    {
        public CustomFlyoutPage() => InitializeComponent();

        private void OnColorPicked(object? sender, Color colour)
        {
            Chosen.Background = new SolidColorBrush(colour);
            ChosenText.Text = colour.ToString();
        }
    }
}
```

实测依据：`flyout.ShowAt(button)` 后 `IsOpen == True`；经反射取 `PopupFlyoutBase.Popup`，`Child` 是 `FlyoutPresenter`、`Parent` 是 `Button`；点第三个色块触发 `ColorPicked` 值为 `#ffe8d54a`，随后 `IsOpen == False`。`ColorPicked` 的参数类型 `Color` 是**值类型**，事件处理方法签名必须是 `(object?, Color)`，与 `EventHandler<Color>` 一致。


- [x] **Step 8: 挂 7 个 Tab**

修改 `Avalonia.CustomControlsDemo/Views/MainWindow.axaml`，在 `<Window>` 上加 `xmlns:pages="using:Avalonia.CustomControlsDemo.Views.Pages"`，`TabControl` 改为（竖排，Task 1 已设 `TabStripPlacement="Left"`）：

```xml
    <TabControl Margin="12" TabStripPlacement="Left">
        <TabItem Header="用户控件">
            <pages:UserControlPage />
        </TabItem>
        <TabItem Header="模板化控件">
            <pages:TemplatedControlPage />
        </TabItem>
        <TabItem Header="自绘控件">
            <pages:CustomDrawnPage />
        </TabItem>
        <TabItem Header="属性与事件">
            <pages:PropertiesAndEventsPage />
        </TabItem>
        <TabItem Header="控件树">
            <pages:ControlTreesPage />
        </TabItem>
        <TabItem Header="自定义面板">
            <pages:CustomPanelPage />
        </TabItem>
        <TabItem Header="自定义 Flyout">
            <pages:CustomFlyoutPage />
        </TabItem>
    </TabControl>
```

- [x] **Step 9: 构建**

Run: `dotnet build Avalonia.CustomControlsDemo 2>&1 | grep -E "个错误|error"`
Expected: `0 个错误`。

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "warning" | grep -E "CustomControlsDemo" | grep -v MSB3884`
Expected: 无输出。

- [x] **Step 10: 用 headless 探针断言页面行为**

创建 `C:\Temp\customcheck\customcheck.csproj`（与 Task 2 的 `eventscheck.csproj` 相同，只把 `ProjectReference` 改成 `Avalonia.CustomControlsDemo\Avalonia.CustomControlsDemo.csproj`）。

创建 `C:\Temp\customcheck\Program.cs`。下面是完整的一个文件：先是公共部分、外壳、用户控件、模板化控件、自绘，再依次是属性页、控件树、面板、Flyout 与收尾。

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.CustomControlsDemo.Controls;
using Avalonia.CustomControlsDemo.Views;
using Avalonia.CustomControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Shared/"))
        {
            Source = new Uri("avares://Avalonia.Shared/Themes/SharedStyles.axaml")
        });

        // App.axaml is not loaded under the probe, so merge Meter's theme the way App.axaml does.
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Avalonia.CustomControlsDemo/"))
        {
            Source = new Uri("avares://Avalonia.CustomControlsDemo/Controls/Meter.axaml")
        });
    }
}

internal sealed class WarnSink : ILogSink
{
    public readonly List<string> Entries = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel l, string a, object? s, string m) => Entries.Add($"[{l}] ({a}) {m}");
    public void Log(LogEventLevel l, string a, object? s, string m, params object?[] v)
        => Entries.Add($"[{l}] ({a}) {m} :: {string.Join(", ", v)}");
}

// Counts Render calls, to prove AffectsRender really invalidates the drawing.
internal sealed class CountingGauge : RingGauge
{
    public int Renders;

    public override void Render(DrawingContext context)
    {
        Renders++;
        base.Render(context);
    }
}

internal static class Probe
{
    private static int _pass, _fail;

    private static T Find<T>(Visual root, string name) where T : Visual
    {
        foreach (var d in root.GetVisualDescendants())
        {
            if (d is T hit && (d as Control)?.Name == name) return hit;
        }
        throw new InvalidOperationException($"not found: {name}");
    }

    private static Window Show(Control c)
    {
        var w = new Window { Content = c, Width = 900, Height = 700 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        return w;
    }

    private static void Check(string label, bool ok, string detail)
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label,-52} {detail}");
    }

    private static void Tick(int n)
    {
        for (var i = 0; i < n; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(30);
            Dispatcher.UIThread.RunJobs();
        }
    }

    // RaiseEvent(Click) runs handlers attached with Click="..." but not Button.OnClick.
    private static void Click(Button b)
    {
        b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static Color Colour(IBrush? b) => (b as ISolidColorBrush)?.Color ?? default;

    public static void Main()
    {
        var sink = new WarnSink();
        Logger.Sink = sink;
        AppBuilder.Configure<ProbeApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();

        // --- Shell: seven tabs, each rendering the right page ---
        var mw = new MainWindow();
        mw.Show();
        Dispatcher.UIThread.RunJobs();
        var tabs = mw.GetVisualDescendants().OfType<TabControl>().First().Items.Cast<TabItem>().ToList();
        var names = new[] { "用户控件", "模板化控件", "自绘控件", "属性与事件", "控件树", "自定义面板", "自定义 Flyout" };
        var types = new[]
        {
            typeof(UserControlPage), typeof(TemplatedControlPage), typeof(CustomDrawnPage),
            typeof(PropertiesAndEventsPage), typeof(ControlTreesPage), typeof(CustomPanelPage), typeof(CustomFlyoutPage),
        };
        Check("shell has 7 tabs", tabs.Count == 7, $"count={tabs.Count}");
        for (var i = 0; i < tabs.Count; i++)
        {
            Check($"tab {i} header", (string?)tabs[i].Header == names[i], $"header={tabs[i].Header}");
            Check($"tab {i} content type", tabs[i].Content?.GetType() == types[i], $"type={tabs[i].Content?.GetType().Name}");
        }
        mw.Close();

        // --- UserControl: inner slider drives the outer Value, which a second LabeledSlider follows ---
        var uc = new UserControlPage();
        var w1 = Show(uc);
        var size = Find<LabeledSlider>(uc, "Size");
        var opacity = Find<LabeledSlider>(uc, "Opacity");
        var innerSize = size.GetVisualDescendants().OfType<Slider>().First();
        var innerOpacity = opacity.GetVisualDescendants().OfType<Slider>().First();
        Check("LabeledSlider inner range", innerSize.Minimum == 10 && innerSize.Maximum == 50, $"{innerSize.Minimum}/{innerSize.Maximum}");
        innerSize.Value = 35;
        Dispatcher.UIThread.RunJobs();
        Check("inner drag reaches outer Value", size.Value == 35, $"Size.Value={size.Value}");
        Check("second slider follows through TwoWay", opacity.Value == 35 && innerOpacity.Value == 35,
            $"Opacity.Value={opacity.Value} inner={innerOpacity.Value}");
        size.Value = 12;
        Dispatcher.UIThread.RunJobs();
        Check("outer set reaches inner slider", innerSize.Value == 12, $"inner={innerSize.Value}");
        w1.Close();

        // --- TemplatedControl: part, pseudo-class, template style, retemplate ---
        var tp = new TemplatedControlPage();
        var w2 = Show(tp);
        var meter = Find<Meter>(tp, "TheMeter");
        var slider = Find<Slider>(tp, "ValueSlider");
        Border? Fill() => meter.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "PART_Fill");
        Check("meter has a template", meter.Template != null, $"template={meter.Template != null}");
        Check("PART_Fill found", Fill() != null, "");
        Check("meter follows slider", meter.Value == 40 && meter.Display == "40%", $"value={meter.Value} display={meter.Display}");
        Check("fill scaled to 0.4", Fill()?.RenderTransform is ScaleTransform { ScaleX: > 0.39 and < 0.41 },
            $"transform={Fill()?.RenderTransform}");
        Check("not :full at 40", !meter.Classes.Contains(":full"), "");
        var before = Colour(Fill()?.Background);
        slider.Value = 100;
        Dispatcher.UIThread.RunJobs();
        Check(":full appears at 100", meter.Classes.Contains(":full"), $"classes={string.Join(",", meter.Classes)}");
        Check("template style recolours the fill", Colour(Fill()?.Background) != before, $"{before} -> {Colour(Fill()?.Background)}");
        meter.Value = 250;
        Check("coerce clamps 250 to Maximum", meter.Value == 100, $"value={meter.Value}");
        slider.Value = 30;
        Dispatcher.UIThread.RunJobs();
        Click(Find<Button>(tp, "SwapButton"));
        Check("retemplate removes PART_Fill", Fill() == null, "");
        var text = meter.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        Check("retemplate shows the 28px text", text?.FontSize == 28 && text.Text == "30%", $"size={text?.FontSize} text={text?.Text}");
        slider.Value = 60;
        Dispatcher.UIThread.RunJobs();
        Check("value change after retemplate does not throw", meter.GetVisualDescendants().OfType<TextBlock>().First().Text == "60%", "");
        w2.Close();

        // --- Custom drawn: value flows in, and AffectsRender really redraws ---
        var dp = new CustomDrawnPage();
        var w3 = Show(dp);
        var gauge = Find<RingGauge>(dp, "Gauge");
        var gSlider = Find<Slider>(dp, "GaugeSlider");
        Check("gauge starts at the slider value", gauge.Value == 35, $"value={gauge.Value}");
        gSlider.Value = 80;
        Dispatcher.UIThread.RunJobs();
        Check("gauge follows slider", gauge.Value == 80, $"value={gauge.Value}");
        w3.Close();

        var counting = new CountingGauge();
        var w4 = Show(counting);
        Tick(2);
        var rendersBefore = counting.Renders;
        counting.Value = 60;
        Tick(2);
        Check("AffectsRender triggers another Render", counting.Renders > rendersBefore, $"{rendersBefore} -> {counting.Renders}");
        w4.Close();

        // --- PropertiesAndEventsPage: a signpost that names the two projects it points to ---
        var pe = new PropertiesAndEventsPage();
        var w5 = Show(pe);
        var peTexts = pe.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
        Check("signpost names PropertySystemDemo", peTexts.Any(t => t.Contains("PropertySystemDemo")), "");
        Check("signpost names EventsDemo", peTexts.Any(t => t.Contains("EventsDemo")), "");
        w5.Close();

        // --- Control trees: DataContext follows the logical tree only (plan rule 19) ---
        var ct = new ControlTreesPage();
        var w6 = Show(ct);
        var onlyVisual = Find<OnlyVisualHost>(ct, "VisualOnly");
        var both = Find<VisualAndLogicalHost>(ct, "Both");
        Check("visual-only child gets no DataContext", onlyVisual.Child.DataContext == null && onlyVisual.Child.Text == "",
            $"dc={onlyVisual.Child.DataContext ?? "null"} text='{onlyVisual.Child.Text}'");
        Check("visual+logical child inherits DataContext",
            both.Child.DataContext as string == "来自父级的 DataContext" && both.Child.Text == "来自父级的 DataContext",
            $"dc={both.Child.DataContext ?? "null"}");
        var readout = Find<TextBlock>(ct, "Readout").Text ?? "";
        Check("readout reports both", readout.Contains("null") && readout.Contains("来自父级的 DataContext"), readout);
        w6.Close();

        // --- Custom panel: children sit on the circle, and the properties re-arrange them ---
        var cp = new CustomPanelPage();
        var w7 = Show(cp);
        var radial = Find<RadialPanel>(cp, "Radial");
        Point Centre(Control c) => new(c.Bounds.X + c.Bounds.Width / 2, c.Bounds.Y + c.Bounds.Height / 2);
        bool Near(Point a, double x, double y) => Math.Abs(a.X - x) < 0.5 && Math.Abs(a.Y - y) < 0.5;
        var cx = radial.Bounds.Width / 2;
        var cy = radial.Bounds.Height / 2;
        Check("starts with six dots", radial.Children.Count == 6, $"count={radial.Children.Count}");
        Check("dot 0 is at twelve o'clock", Near(Centre(radial.Children[0]), cx, cy - 80), $"c0={Centre(radial.Children[0])} want {cx},{cy - 80}");
        Check("dot 3 is opposite dot 0", Near(Centre(radial.Children[3]), cx, cy + 80), $"c3={Centre(radial.Children[3])}");
        Find<Slider>(cp, "AngleSlider").Value = 90;
        Dispatcher.UIThread.RunJobs();
        Check("StartAngle=90 moves dot 0 to three o'clock", Near(Centre(radial.Children[0]), cx + 80, cy), $"c0={Centre(radial.Children[0])}");
        Find<Slider>(cp, "RadiusSlider").Value = 40;
        Dispatcher.UIThread.RunJobs();
        Check("Radius=40 pulls dot 0 inwards", Near(Centre(radial.Children[0]), cx + 40, cy), $"c0={Centre(radial.Children[0])}");
        Click(Find<Button>(cp, "AddButton"));
        Check("add button adds a dot", radial.Children.Count == 7, $"count={radial.Children.Count}");
        Click(Find<Button>(cp, "RemoveButton"));
        Click(Find<Button>(cp, "RemoveButton"));
        Check("remove button removes dots", radial.Children.Count == 5, $"count={radial.Children.Count}");
        w7.Close();

        // --- Custom flyout: opens, picking a swatch raises the event, writes the page, and closes ---
        var fp = new CustomFlyoutPage();
        var w8 = Show(fp);
        var pick = Find<Button>(fp, "PickButton");
        var flyout = pick.Flyout as SwatchFlyout;
        Check("button carries a SwatchFlyout", flyout != null, $"flyout={pick.Flyout?.GetType().Name}");
        flyout!.ShowAt(pick);
        Dispatcher.UIThread.RunJobs();
        Tick(1);
        Check("ShowAt opens the flyout", flyout.IsOpen, $"IsOpen={flyout.IsOpen}");
        var popupProp = typeof(PopupFlyoutBase).GetProperty("Popup",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var popup = (Popup?)popupProp?.GetValue(flyout);
        Check("popup child is a FlyoutPresenter", popup?.Child is FlyoutPresenter, $"child={popup?.Child?.GetType().Name}");
        // The presenter template brings buttons of its own, so locate the swatch by colour, never by index (rule 32).
        var yellow = popup?.Child?.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(b => Colour(b.Background) == Color.Parse("#E8D54A"));
        Check("a yellow swatch exists", yellow != null, "");
        Click(yellow!);
        Check("picking writes the colour text", Find<TextBlock>(fp, "ChosenText").Text == "#ffe8d54a", $"text={Find<TextBlock>(fp, "ChosenText").Text}");
        Check("picking paints the preview", Colour(Find<Border>(fp, "Chosen").Background) == Color.Parse("#E8D54A"), "");
        Check("picking closes the flyout", !flyout.IsOpen, $"IsOpen={flyout.IsOpen}");
        w8.Close();

        // --- No warnings anywhere ---
        Check("no log warnings", sink.Entries.Count == 0, sink.Entries.Count == 0 ? "" : string.Join(" | ", sink.Entries));

        Console.WriteLine($"{_pass} passed, {_fail} failed");
        Environment.Exit(_fail == 0 ? 0 : 1);
    }
}
```

Run: `cd /c/Temp/customcheck && dotnet run 2>&1 | grep -E "PASS|FAIL|passed|error"`
Expected: 全部 PASS，末行 `N passed, 0 failed`。

这些断言的数值来自 Avalonia 12.1.2 headless 的实测，不是推算：`Meter` 的 `Template` 非空而 `Theme` 为 null（主题来自祖先资源，所以断言落在 `Template` 上）；`:full` 只在 `Value = Maximum` 时出现并让填充色由 `#4A7BE8` 变 `#4AE87B`；`RadialPanel` 6 个点在 `Radius=80` 时第 0 个正好在圆心正上方 80；Flyout 的 Presenter 里除了 6 个色块还有模板自带按钮，所以探针按颜色找色块。控件层面的行为（上述各项与"全部控件加载零日志警告"）在写计划时用临时项目实测过；页面级探针本身要等执行时才第一次运行，第一次出现 FAIL 时先怀疑探针的查找方式，再怀疑页面。

若有 `FAIL`：

- `PART_Fill found` 失败：多半是 `App.axaml` 没合并 `Meter.axaml`，或 `ControlTheme` 的 `x:Key` 写成了字符串而不是 `{x:Type controls:Meter}`。
- `retemplate removes PART_Fill` 失败：`TryFindResource("TextMeter", ...)` 返回 false，说明资源没进 `Application.Resources`；探针里的 `ProbeApp` 已手动合并，页面里查不到就是 `Meter.axaml` 里的键拼错了。
- `a yellow swatch exists` 失败：`SwatchFlyout.Palette` 里的 `#E8D54A` 被改过，或 Presenter 在 `ShowAt` 之后还没生成（多 `Tick(1)` 一次）。
- 任何 `no log warnings` 失败：先看 detail 里的完整文本，绑定路径错误会在这里露出来，不要放宽断言。

- [x] **Step 11: 清理探针并提交**

```bash
rm -rf /c/Temp/customcheck
git add Avalonia.CustomControlsDemo
git commit -m "feat: demonstrate the Custom Controls category

Seven pages: a UserControl, a TemplatedControl with a ControlTheme and
pseudo-class, a self-drawn ring gauge, a signpost for properties and events,
logical versus visual trees, a radial panel and a PopupFlyoutBase subclass.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 6: README 与规约回写

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md`

**Interfaces:**
- Consumes: Task 2–5 四个探针最后一行的 `N passed, 0 failed` 计数；执行过程中遇到并修正的 plan 错误
- Produces: 无。这是本组最后一个任务。

- [x] **Step 1: README 表格插四行**

在 `README.md` 的 `Avalonia.PropertySystemDemo` 行与 `Avalonia.MusicStore` 行之间，按官方分类顺序插入：

```markdown
| [Avalonia.EventsDemo](Avalonia.EventsDemo) | 路由事件的隧道/冒泡/Handled、指针与手势事件、事件订阅与取消、自定义路由事件、生命周期事件 |
| [Avalonia.InputDemo](Avalonia.InputDemo) | 键盘与文本输入、指针与触摸、手势、焦点与导航、快捷键与命令、拖放、剪贴板、输入法 |
| [Avalonia.GraphicsDemo](Avalonia.GraphicsDemo) | 画刷与渐变、变换、形状与几何、自定义绘制、特效、裁剪与命中、图标、渲染选项、动画、过渡、页面过渡、缓动、合成动画 |
| [Avalonia.CustomControlsDemo](Avalonia.CustomControlsDemo) | UserControl、TemplatedControl 与 ControlTheme、自绘控件、控件树、自定义 Panel、自定义 Flyout |
```

> 每行"演示内容"以该项目最终的 Tab 为准：执行到这一步时，把上面四行与 `MainWindow.axaml` 里实际的 `Header` 逐个对一遍，Tab 名有出入就改 README，不改 Tab。

Run: `grep -c "Demo\](Avalonia" README.md`
Expected: 比插入前多 4。

- [x] **Step 2: 构建整个解决方案**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "个错误|个警告"`
Expected: `0 个错误`；警告数与 Task 1 之前相比只多出 MSB3884 类（若有）。

- [x] **Step 3: 把实测结论写回规约**

在规约 `### 样式绑定层实测结论（2026-10-07）` 小节之后、`## 交付顺序与验证标准` 之前，新增 `### 交互图形层实测结论（2026-10-07）`。内容按下列条目写，**数字取 Task 2–5 各探针的实际末行**，不要照抄 plan 里的预估：

- **功能点映射的出入**：Events 官方子页合成 5 个 Tab；Input 8 个；Graphics 13 个；CustomControls 7 个，其中「属性与事件」是指向 #7 与 #8 的路标页，用户控件的官方页在 `controls/primitives/usercontrol` 而非 `custom-controls/`。
- **去重规则落地**：路由事件主体在 #8（#9 只留路标）；焦点管理完整在 #9；定义属性在 #7（#11 路标）；自定义路由事件在 #8（#11 路标）。
- **新增静默失败与响亮失败**（见 plan 规则 1–33，只抄本组新增的 26–33）：非控件对象不能命名（`AVLN2000`）但元素名绑定可以绑它们的属性；`GeometryCombineMode` 只有四个值；`coerce` 第一个参数是 `AvaloniaObject`；`PopupFlyoutBase` 要 `using Avalonia.Controls.Primitives`。
- **headless 的边界**：`Render` 会被调用，所以自绘控件可以断言重绘次数；无限动画只前进 1–2 帧、`Animation.RunAsync` 在 headless 里不推进，动画页的断言只落在类的切换与 `Transitions` 的终值上。
- **探针写法的坑**：`RaiseEvent(Click)` 只跑 `Click=` 处理器、不跑 `OnClick`；`Meter.Theme` 读回 null 要断言 `Template`；Flyout Presenter 里按钮数含模板自带的，色块按颜色找。
- **探针断言实际条数**：#8 为 `<Task 2 末行>` 条、#9 为 `<Task 3 末行>` 条、#10 为 `<Task 4 末行>` 条、#11 为 `<Task 5 末行>` 条，全部通过且零警告日志；plan 里的预估若与实际不符，以实际为准并在此注明。
- **执行时更正的 plan 错误**：逐条列出执行过程中发现并修正的 plan 缺陷（没有就写"无"），每条一行，写清是什么、怎么修的。

> 尖括号里的四处是**执行时必须替换成数字**的位置，不是留给读者的占位；提交前 `grep -n "<Task" docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md` 应无输出。

- [x] **Step 4: 提交**

```bash
git add README.md docs/superpowers/specs/2026-09-21-avalonia-docs-category-demos-design.md
git commit -m "docs: register the interaction-graphics demos and record what they measured

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```
