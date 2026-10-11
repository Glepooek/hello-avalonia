

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
