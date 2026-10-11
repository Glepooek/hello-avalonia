

## 批 3 实测结论（计划 03 执行后）

- **普通 `Trigger` 不给 Action 传 DataContext**：`FileSystemWatcherTrigger`、`NetworkInformationTrigger` 都继承它，里面 `InvokeCommandAction` 的 `Command="{Binding …}"` 解析不出来（触发 0 次、无日志）；在代码里直接给 `Command` 赋值就正常。自己写的 `Trigger<T>` 同样如此（在 UI 线程上也失败，排除了线程原因），改继承 `StyledElementTrigger<T>` 后绑定恢复。`EventTriggerBehavior` 等本来就是 `StyledElementTrigger`，不受影响。
- **`FileSystemWatcherTrigger` 要求挂载时目录已存在**；新建 `.txt` 触发两次（创建 + 内容变化），追加再触发一次；事件来自线程池线程，命令里要回到 UI 线程。
- **`HttpRequestAction` 的响应是只读属性**：Action 要有 `x:Name`，用 `{Binding #名字.ResponseContent}` 读回；对本进程的 `HttpListener` 回环服务 POST 得到 201 与「pong:ping」。
- **`NetworkInformationTrigger` 没有属性，headless 里无法制造事件**，只能验证挂载，真实效果需手动开关网络。
- **剪贴板三个 Action 都能用**，清空后读回空字符串；页面提示会改写真实剪贴板。
- **`RenderTargetBitmapBehavior` 挂在 `Image` 上**，`Renderer` 绑定到实现 `IRenderTargetBitmapRenderer` 的对象；`RenderRenderTargetBitmapAction` 能再触发一次渲染。**`ScreenshotAction` headless 里不产出文件**，真实效果需目视。
- **自定义三件套**：`Behavior<T>` 在 `OnAttachedToVisualTree` 订阅、`OnDetachedFromVisualTree` 取消；触发器用 `Interaction.ExecuteActions(AssociatedObject, Actions, null)`；Action 基类要写全名 `Avalonia.Xaml.Interactivity.Action`（避免与 `System.Action` 冲突）。连点计数要监听 `PointerPressed`，`Tapped` 的第二次会被 `DoubleTapped` 吞掉。
- 探针累计 161 条断言全部通过，0 条警告日志。
