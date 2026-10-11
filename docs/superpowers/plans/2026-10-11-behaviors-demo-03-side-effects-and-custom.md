# BehaviorsDemo 03：第 11–15 页（文件系统、网络、剪贴板、截图、自定义）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Avalonia.BehaviorsDemo` 里加入第 11–15 页：文件系统、网络、剪贴板、截图这四类有副作用的动作（全部限制在安全范围内），以及自己动手写 `Behavior`、`Trigger`、`Action`；把 `MainWindow` 扩成 15 个标签，并完成 README 与 CLAUDE.md 收尾。

**Architecture:** 延续计划 01、02：每页一个 `UserControl`（有状态的页面带一个 ViewModel），行为全部写在 XAML 的 `Interaction.Behaviors` 里。副作用一律关进沙箱：文件只动系统临时目录下的专用文件夹，网络只访问本进程自己起的 127.0.0.1 回环服务，剪贴板与截图在页面上明确提示会覆盖用户的剪贴板或弹出保存对话框。验证用仓库外的 headless 探针（`C:\Temp\probe-bdemo`）。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Xaml.Behaviors.Avalonia 12.0.7、CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md`

**前置：** 计划 01、02 已执行（项目、10 个页面、`C:\Temp\probe-bdemo` 里的 `Harness.cs` 与 `Probe.*.cs` 都在，探针累计 118 条）。

## Global Constraints

- 新写的代码与 XAML 注释用**英文**；界面文字用**中文**。
- 中文属性值里不要用 ASCII 双引号（用「」），不用 `&` 与 `<`（AVLN1001）。
- 版本只写在根目录 `Directory.Packages.props`，csproj 里的 `PackageReference` 不带 `Version`；本份不新增任何包。
- MVVM 用 CommunityToolkit.Mvvm；默认开启编译绑定，页面根元素写 `x:DataType`。
- 页面类名 `<主题>Page`，命名空间 `Avalonia.BehaviorsDemo.Views.Pages`；ViewModel 命名空间 `Avalonia.BehaviorsDemo.ViewModels`；自定义行为放 `Avalonia.BehaviorsDemo.Behaviors`。
- `DemoHeader.DocPath` 写被演示类型的完整名。
- 副作用安全边界：文件只写 `Path.GetTempPath()` 下的 `avalonia-behaviors-demo` 文件夹；网络只连 `127.0.0.1`；不访问任何外部地址。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`；探针放仓库外，不提交。

### 本份已核实的事实（编写计划时在仓库外的临时项目里 headless 实测）

| 事项 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `CreateDirectoryAction Path`、`WriteTextToFileAction Path/Text/Append`、`DeleteFileAction Path`、`DeleteDirectoryAction Path/Recursive` | 绑定到沙箱路径后全部真实执行 | 页面直接演示真实读写，范围限定在沙箱里 |
| `FileSystemWatcherTrigger Path/Filter/IncludeSubdirectories` | **监视的目录在挂载时必须已经存在**，否则永远不触发；目录存在时，创建一个 `.txt` 文件触发 2 次（Created + Changed），再追加内容触发 1 次；事件来自线程池线程；**它继承普通的 `Trigger`，不会把 DataContext 传给里面的 Action，所以 `InvokeCommandAction` 的 `Command` 若是 `{Binding …}` 就解析不出来：触发 0 次、没有日志**；直接赋值的命令则正常（用 UI 线程上的自定义 `Trigger<T>` 做对照，同样失败，说明与线程无关）。继承 `StyledElementTrigger` 的触发器（如 `EventTriggerBehavior`）没有这个问题 | ViewModel 构造函数里先建好沙箱；Command 在 code-behind 里直接赋值，命令里用 `Dispatcher.UIThread.Post` 回到 UI 线程再改属性；探针用 `WaitUntil` 轮询，断言「至少一次」 |
| `HttpRequestAction Url/Method/Content/ContentType`，只读 `ResponseContent`/`ResponseStatusCode` | 对本进程的 `HttpListener`（127.0.0.1，端口由 `TcpListener(0)` 选出）请求成功：POST「ping」得到「pong:ping」与 201 | 回环服务自己写在项目里；动作要 `x:Name`，用 `{Binding #Req.ResponseContent}` 读回 |
| `NetworkInformationTrigger` | 只有 `OnAttached`/`OnDetaching`，没有属性；靠系统网络变化事件触发，无法在 headless 里制造 | 探针只验证已挂载，页面注明需要拔网线或开关飞行模式才能看到 |
| `SetClipboardTextAction Text`、`GetClipboardTextAction`（`Command` 收到文字）、`ClearClipboardAction` | 全部可用；读取用 `using Avalonia.Input.Platform;` 与 `TopLevel.Clipboard.TryGetTextAsync()`；清空后读到空字符串 | 页面提示：它会覆盖你真实的剪贴板 |
| `RenderTargetBitmapBehavior PixelWidth/PixelHeight/Dpi/Renderer` | 挂在 `Image` 上，`Renderer` 绑定到实现 `IRenderTargetBitmapRenderer.Render(DrawingContext, TimeSpan)` 的对象；`Image.Source` 变成 64×64 的 `RenderTargetBitmap`，渲染器的调用次数增加；`RenderRenderTargetBitmapAction Target="{Binding #Rtb}"` 能再触发一次渲染 | 渲染器写在 ViewModel 里，页面画一个计数器方块 |
| `ScreenshotAction TargetControl/FileName` | headless 里执行不抛异常，但没有写出文件、也观察不到对话框 | 探针只验证挂载；真实行为写进目视清单 |
| 自定义 `Behavior<T>` / `StyledElementTrigger<T>` / `Action` | **触发器要继承 `StyledElementTrigger<T>`**：继承普通 `Trigger<T>` 时里面的 `InvokeCommandAction` 拿不到 DataContext，`{Binding}` 命令不执行（自定义 Action 仍会执行）；重写 `OnAttachedToVisualTree`/`OnDetachedFromVisualTree` 挂接事件；`Trigger` 里用 `Interaction.ExecuteActions(AssociatedObject, Actions, null)` 触发；`Action` 重写 `Execute(object? sender, object? parameter)`；`StyledProperty` 在 XAML 里照常可绑定 | 页面 15 写三个最小自定义类型，触发器用 `StyledElementTrigger<Control>` |
| 自定义计数触发器的事件选择 | 用 `Tapped` 连点两次，第二次被识别为 `DoubleTapped`，**第二次不会再发 `Tapped`**，计数永远凑不够；改用 `PointerPressed` 才正常 | 自定义触发器监听 `PointerPressed`，并在页面上注明原因 |

---

## File Structure（本份创建 / 修改）

```
Avalonia.BehaviorsDemo/
├── Behaviors/
│   ├── UpperCaseBehavior.cs                新：自定义 Behavior<TextBox>
│   ├── ClickCountTrigger.cs                新：自定义 StyledElementTrigger<Control>
│   └── AppendTextAction.cs                 新：自定义 Action
├── Services/
│   └── LoopbackServer.cs                   新：本进程内的 127.0.0.1 回环 HTTP 服务
├── Views/
│   ├── MainWindow.axaml                    改：10 个标签 → 15 个标签
│   └── Pages/
│       ├── FileSystemPage                  新：页面 11
│       ├── NetworkPage                     新：页面 12
│       ├── ClipboardPage                   新：页面 13
│       ├── ScreenshotPage                  新：页面 14
│       └── CustomPage                      新：页面 15
└── ViewModels/
    ├── FileSystemViewModel.cs              新
    ├── NetworkViewModel.cs                 新
    ├── ClipboardViewModel.cs               新
    ├── ScreenshotViewModel.cs              新（含 IRenderTargetBitmapRenderer 实现）
    └── CustomViewModel.cs                  新
README.md                                   改：项目行页数 10 → 15
CLAUDE.md                                   改：加 BehaviorsDemo 一节
docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md   改：追加「批 3 实测结论」
```

### 抽取与验证的约定

- 同计划 01、02：代码块前的 `#### \`相对路径\`` 标题是目标文件（相对仓库根），`**\`C:\绝对路径\`**` 标题是仓库外的探针文件；同一路径出现多次时后者覆盖前者。抽取脚本见计划 01 附录 A。
- 验证：`bash /c/Temp/verify.sh <计划 02> <本计划>`（先抽计划 01，再按顺序抽后续计划，然后跑探针）。
- 探针累计：计划 02 结束时是 118 条；本份每个任务末尾给出新增条数与累计数。

## Task 1: 页面 11 文件系统

**Files:**
- Create: `ViewModels/FileSystemViewModel.cs`、`Views/Pages/FileSystemPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.FileSystem.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `FileSystemViewModel`：`SandboxDir`、`SubDir`、`FilePath`（只读字符串）、`NoteText`、`WatchHits`、`Listing`；命令 `RefreshCommand`、`WatcherHitCommand`。控件名：`MkdirButton`、`WriteButton`、`AppendButton`、`DeleteFileButton`、`DeleteDirButton`、`WatchBox`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.FileSystem.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeFileSystem
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 11: file system ==");
        var page = new FileSystemPage();
        Show(page, 900, 900);
        var vm = (FileSystemViewModel)page.DataContext!;

        Check("the sandbox is under the system temp folder", vm.SandboxDir.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Check("the sandbox exists before any click (the watcher needs it)", Directory.Exists(vm.SandboxDir));
        Check("the sub folder does not exist yet", !Directory.Exists(vm.SubDir));

        Click(Find<Button>(page, "MkdirButton")); Pump();
        Check("CreateDirectoryAction created the sub folder", Directory.Exists(vm.SubDir));

        Click(Find<Button>(page, "WriteButton")); Pump();
        Check("WriteTextToFileAction wrote the bound text", File.Exists(vm.FilePath) && File.ReadAllText(vm.FilePath) == vm.NoteText);
        Check("FileSystemWatcherTrigger noticed the new .txt file", WaitUntil(() => vm.WatchHits >= 1, 5000));
        Check("the listing shows the file", vm.Listing.Contains("hello.txt"));

        var hits = vm.WatchHits;
        Click(Find<Button>(page, "AppendButton")); Pump();
        Check("Append=True added the text after the old content", File.ReadAllText(vm.FilePath) == vm.NoteText + vm.NoteText);
        Check("the watcher noticed the change too", WaitUntil(() => vm.WatchHits > hits, 5000));

        Click(Find<Button>(page, "DeleteFileButton")); Pump();
        Check("DeleteFileAction removed the file", !File.Exists(vm.FilePath));
        Check("the listing no longer shows it", !vm.Listing.Contains("hello.txt"));

        Click(Find<Button>(page, "DeleteDirButton")); Pump();
        Check("DeleteDirectoryAction removed the sub folder", !Directory.Exists(vm.SubDir));
        Check("the sandbox root is still there", Directory.Exists(vm.SandboxDir));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeShell.Run();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
ProbeAnimation.Run();
ProbeFileSystem.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/FileSystemViewModel.cs`

```csharp
using System;
using System.IO;
using System.Linq;
using Avalonia.Shared.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class FileSystemViewModel : ViewModelBase
    {
        public FileSystemViewModel()
        {
            // Start from a clean, existing sandbox: FileSystemWatcherTrigger never fires for a folder that
            // does not exist when it attaches, and a leftover file from a previous run would skew the page.
            if (Directory.Exists(SandboxDir))
            {
                Directory.Delete(SandboxDir, recursive: true);
            }

            Directory.CreateDirectory(SandboxDir);
            Refresh();
        }

        // Every file action on this page is confined to this folder under the system temp directory.
        public string SandboxDir { get; } = Path.Combine(Path.GetTempPath(), "avalonia-behaviors-demo", "files");

        public string SubDir => Path.Combine(SandboxDir, "notes");

        public string FilePath => Path.Combine(SubDir, "hello.txt");

        [ObservableProperty]
        private string _noteText = "你好，Behaviors\n";

        [ObservableProperty]
        private int _watchHits;

        [ObservableProperty]
        private string _listing = "";

        [RelayCommand]
        private void Refresh()
        {
            var entries = Directory.EnumerateFileSystemEntries(SandboxDir, "*", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(SandboxDir, p))
                .OrderBy(p => p)
                .ToList();
            Listing = entries.Count == 0 ? "（沙箱是空的）" : string.Join("\n", entries);
        }

        // The watcher raises its event on a thread-pool thread, so hop back to the UI thread before touching bindings.
        [RelayCommand]
        private void WatcherHit() => Dispatcher.UIThread.Post(() =>
        {
            WatchHits++;
            Refresh();
        });
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/FileSystemPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="900"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.FileSystemPage"
             x:DataType="vm:FileSystemViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="文件系统：CreateDirectoryAction、WriteTextToFileAction、DeleteFileAction、DeleteDirectoryAction、FileSystemWatcherTrigger"
                               DocPath="Avalonia.Xaml.Interactions.FileSystem.WriteTextToFileAction" />

            <TextBlock Classes="caption" Text="沙箱（所有操作只发生在这里）" />
            <Border Classes="stage" Padding="8">
                <StackPanel Spacing="4">
                    <TextBlock Text="{Binding SandboxDir}" TextWrapping="Wrap" />
                    <TextBlock Classes="hint" Text="位于系统临时目录。页面每次打开都会清空它，真实文件夹与文件不受影响。" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="1. 一排按钮，每个按钮一个 Action；每个 Action 后面再跟一个刷新命令" />
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Name="MkdirButton" Content="创建 notes 文件夹">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <CreateDirectoryAction Path="{Binding SubDir}" />
                            <InvokeCommandAction Command="{Binding RefreshCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="WriteButton" Content="写入 hello.txt（覆盖）">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <WriteTextToFileAction Path="{Binding FilePath}" Text="{Binding NoteText}" Append="False" />
                            <InvokeCommandAction Command="{Binding RefreshCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="AppendButton" Content="追加一行">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <WriteTextToFileAction Path="{Binding FilePath}" Text="{Binding NoteText}" Append="True" />
                            <InvokeCommandAction Command="{Binding RefreshCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="DeleteFileButton" Content="删除 hello.txt">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <DeleteFileAction Path="{Binding FilePath}" />
                            <InvokeCommandAction Command="{Binding RefreshCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <Button Name="DeleteDirButton" Content="删除 notes 文件夹">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <DeleteDirectoryAction Path="{Binding SubDir}" Recursive="True" />
                            <InvokeCommandAction Command="{Binding RefreshCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
            </StackPanel>
            <TextBox Margin="0,8,0,0" Width="320" HorizontalAlignment="Left" Text="{Binding NoteText}" Watermark="要写入的文字" />
            <TextBlock Classes="hint"
                       Text="路径与文字都是绑定出来的，所以改上面的输入框再点写入，文件内容就跟着变。写入前要先创建 notes 文件夹，否则写不进去。「追加」用的是同一个 Action，只是 Append 为 True。" />

            <TextBlock Classes="caption" Text="2. 沙箱当前内容" />
            <Border Classes="stage" Padding="8" MinHeight="70">
                <TextBlock Text="{Binding Listing}" />
            </Border>

            <TextBlock Classes="caption" Text="3. FileSystemWatcherTrigger：监视沙箱里的 .txt 文件" />
            <Border Name="WatchBox" Classes="stage" Padding="12">
                <Interaction.Behaviors>
                    <!-- The action's Command is assigned in code-behind, not bound: see the hint below. -->
                    <FileSystemWatcherTrigger Path="{Binding SandboxDir}" Filter="*.txt" IncludeSubdirectories="True">
                        <InvokeCommandAction />
                    </FileSystemWatcherTrigger>
                </Interaction.Behaviors>
                <TextBlock Text="{Binding WatchHits, StringFormat={}监视器已触发 {0} 次}" />
            </Border>
            <TextBlock Classes="hint"
                       Text="新建一个 .txt 会触发两次（创建与内容变化各一次），再追加内容又触发一次。你也可以在资源管理器里打开上面的沙箱路径手动改文件，这里同样会计数。监视的文件夹在行为挂载时必须已经存在，所以 ViewModel 在构造函数里先把沙箱建好。另外，这个触发器继承的是普通 Trigger，不会把 DataContext 传给里面的 Action，写成 {Binding} 的 Command 解析不出来（触发了但什么也不执行，也没有日志），所以这个 InvokeCommandAction 的 Command 在 code-behind 里直接赋值。事件本身来自线程池线程，命令里再回到 UI 线程。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/FileSystemPage.axaml.cs`

```csharp
using System.Linq;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class FileSystemPage : UserControl
    {
        public FileSystemPage()
        {
            InitializeComponent();
            var vm = new FileSystemViewModel();
            DataContext = vm;

            // FileSystemWatcherTrigger derives from the plain Trigger, which does not hand its DataContext to the actions,
            // so a bound Command never resolves. A command assigned directly is already a value; it hops to the UI thread itself.
            var trigger = Interaction.GetBehaviors(WatchBox).OfType<Trigger>().First();
            ((InvokeCommandAction)trigger.Actions[0]).Command = vm.WatcherHitCommand;
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `bash /c/Temp/verify.sh docs/superpowers/plans/2026-10-11-behaviors-demo-02-collections-windows-drag-responsive-animation.md docs/superpowers/plans/2026-10-11-behaviors-demo-03-side-effects-and-custom.md`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 13 条断言，累计 131 条（此时 Shell 探针还是计划 02 的 10 标签版本，仍然通过）。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo file system page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 2: 页面 12 网络

**Files:**
- Create: `Services/LoopbackServer.cs`、`ViewModels/NetworkViewModel.cs`、`Views/Pages/NetworkPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Network.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `LoopbackServer`（`BaseUrl`，形如 `http://127.0.0.1:端口/`；POST 返回 201 与 `pong:请求体`，其余方法返回 200 与 `pong`）；`NetworkViewModel`：`PingUrl`、`NetworkEvents`、命令 `NetworkChangedCommand`。控件名：`PostButton`、`GetButton`、`PostBody`、`PostStatus`、`GetBody`、`GetStatus`、`NetBox`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Network.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.Network;
using Avalonia.Xaml.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeNetwork
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 12: network ==");
        var page = new NetworkPage();
        Show(page, 900, 900);
        var vm = (NetworkViewModel)page.DataContext!;

        Check("the demo only talks to the loopback address", vm.PingUrl.StartsWith("http://127.0.0.1:"));

        Click(Find<Button>(page, "PostButton"));
        Check("HttpRequestAction POST: the response body arrives", WaitUntil(() => Find<TextBlock>(page, "PostBody").Text == "pong:ping", 5000));
        Check("the status code is readable from the action", Find<TextBlock>(page, "PostStatus").Text == "状态码 201");

        Click(Find<Button>(page, "GetButton"));
        Check("HttpRequestAction GET: the response body arrives", WaitUntil(() => Find<TextBlock>(page, "GetBody").Text == "pong", 5000));
        Check("GET returns 200", Find<TextBlock>(page, "GetStatus").Text == "状态码 200");

        var trigger = Interaction.GetBehaviors(Find<Border>(page, "NetBox")).OfType<NetworkInformationTrigger>().ToList();
        Check("NetworkInformationTrigger is attached", trigger.Count == 1);
        Check("its action runs the view model command", ((Avalonia.Xaml.Interactions.Core.InvokeCommandAction)trigger[0].Actions[0]).Command == vm.NetworkChangedCommand);
        vm.NetworkChangedCommand.Execute(null);
        Check("the command counts a network change on the UI thread", WaitUntil(() => vm.NetworkEvents == 1, 2000));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeShell.Run();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
ProbeAnimation.Run();
ProbeFileSystem.Run();
ProbeNetwork.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写回环服务与 ViewModel**

#### `Avalonia.BehaviorsDemo/Services/LoopbackServer.cs`

```csharp
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.BehaviorsDemo.Services
{
    /// <summary>
    /// A tiny HTTP server bound to 127.0.0.1 only, so HttpRequestAction has something safe to talk to.
    /// POST answers 201 with "pong:" plus the request body; anything else answers 200 with "pong".
    /// </summary>
    public sealed class LoopbackServer : IDisposable
    {
        private readonly HttpListener _listener = new();

        public LoopbackServer()
        {
            // Let the OS pick a free port, release it, then ask HttpListener to use it.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            BaseUrl = $"http://127.0.0.1:{port}/";
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _ = Task.Run(Serve);
        }

        public string BaseUrl { get; }

        private async Task Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return; // listener stopped
                }

                string reply;
                if (context.Request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    reply = "pong:" + await reader.ReadToEndAsync();
                    context.Response.StatusCode = 201;
                }
                else
                {
                    reply = "pong";
                    context.Response.StatusCode = 200;
                }

                var bytes = Encoding.UTF8.GetBytes(reply);
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        public void Dispose() => _listener.Close();
    }
}
```

#### `Avalonia.BehaviorsDemo/ViewModels/NetworkViewModel.cs`

```csharp
using Avalonia.BehaviorsDemo.Services;
using Avalonia.Shared.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class NetworkViewModel : ViewModelBase
    {
        // Lives for the whole process: it is a background pool task on a loopback port, and the page can be revisited.
        private static readonly LoopbackServer Server = new();

        public string PingUrl { get; } = Server.BaseUrl + "ping";

        [ObservableProperty]
        private int _networkEvents;

        // NetworkInformationTrigger fires from a system thread, so hop to the UI thread before touching bindings.
        [RelayCommand]
        private void NetworkChanged() => Dispatcher.UIThread.Post(() => NetworkEvents++);
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/NetworkPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="700"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.NetworkPage"
             x:DataType="vm:NetworkViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="网络：HttpRequestAction 与 NetworkInformationTrigger"
                               DocPath="Avalonia.Xaml.Interactions.Network.HttpRequestAction" />

            <TextBlock Classes="caption" Text="请求的目标（本进程自己起的回环服务，只监听 127.0.0.1）" />
            <Border Classes="stage" Padding="8">
                <TextBlock Text="{Binding PingUrl}" />
            </Border>

            <TextBlock Classes="caption" Text="1. HttpRequestAction：POST 一段文字，读回响应内容与状态码" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="PostButton" Content="POST「ping」">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <!-- Named so the response can be bound from the page. -->
                            <HttpRequestAction x:Name="PostReq" Url="{Binding PingUrl}" Method="POST"
                                               Content="ping" ContentType="text/plain" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock Name="PostBody" VerticalAlignment="Center" Text="{Binding #PostReq.ResponseContent}" />
                <TextBlock Name="PostStatus" VerticalAlignment="Center"
                           Text="{Binding #PostReq.ResponseStatusCode, StringFormat={}状态码 {0}}" />
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 同一个 Action，Method 换成 GET" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="GetButton" Content="GET">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <HttpRequestAction x:Name="GetReq" Url="{Binding PingUrl}" Method="GET" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock Name="GetBody" VerticalAlignment="Center" Text="{Binding #GetReq.ResponseContent}" />
                <TextBlock Name="GetStatus" VerticalAlignment="Center"
                           Text="{Binding #GetReq.ResponseStatusCode, StringFormat={}状态码 {0}}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="ResponseContent 与 ResponseStatusCode 是 Action 上的只读属性，请求完成后才有值，所以 Action 要起个 x:Name，页面用 {Binding #名字.属性} 读回。请求是异步的，点完按钮稍等一下才会出现。这里不访问任何外部网址。" />

            <TextBlock Classes="caption" Text="3. NetworkInformationTrigger：网络可用性变化时触发（需手动制造变化）" />
            <Border Name="NetBox" Classes="stage" Padding="12">
                <Interaction.Behaviors>
                    <!-- The action's Command is assigned in code-behind, not bound: see the hint below. -->
                    <NetworkInformationTrigger>
                        <InvokeCommandAction />
                    </NetworkInformationTrigger>
                </Interaction.Behaviors>
                <TextBlock Text="{Binding NetworkEvents, StringFormat={}网络变化事件：{0} 次}" />
            </Border>
            <TextBlock Classes="hint"
                       Text="这个触发器没有任何属性，只是挂上去，等系统报告网络变化。在自动化测试里无法制造这种变化，所以要手动试：关闭再打开 Wi-Fi 或飞行模式，上面的计数会增加。和文件监视器一样，它继承的是普通 Trigger，Action 拿不到 DataContext，所以 Command 在 code-behind 里直接赋值；事件来自系统线程，命令里再回到 UI 线程。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/NetworkPage.axaml.cs`

```csharp
using System.Linq;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class NetworkPage : UserControl
    {
        public NetworkPage()
        {
            InitializeComponent();
            var vm = new NetworkViewModel();
            DataContext = vm;

            // Same reason as the file watcher page: a plain Trigger gives its actions no DataContext, so assign the command directly.
            var trigger = Interaction.GetBehaviors(NetBox).OfType<Trigger>().First();
            ((InvokeCommandAction)trigger.Actions[0]).Command = vm.NetworkChangedCommand;
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `bash /c/Temp/verify.sh docs/superpowers/plans/2026-10-11-behaviors-demo-02-collections-windows-drag-responsive-animation.md docs/superpowers/plans/2026-10-11-behaviors-demo-03-side-effects-and-custom.md`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 8 条断言，累计 139 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo network page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 3: 页面 13 剪贴板

**Files:**
- Create: `ViewModels/ClipboardViewModel.cs`、`Views/Pages/ClipboardPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Clipboard.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `ClipboardViewModel`：`Input`（要复制的文字）、`Pasted`（最近一次读回的文字）、命令 `PasteCommand(string?)`。控件名：`CopyButton`、`PasteButton`、`ClearButton`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Clipboard.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeClipboard
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    // Reads the system clipboard the way application code would, and waits for the async result.
    static string? ReadClipboard(Window w)
    {
        var task = w.Clipboard!.TryGetTextAsync();
        WaitUntil(() => task.IsCompleted, 3000);
        return task.IsCompletedSuccessfully ? task.Result : null;
    }

    public static void Run()
    {
        Console.WriteLine("== page 13: clipboard ==");
        var page = new ClipboardPage();
        var w = Show(page, 900, 700);
        var vm = (ClipboardViewModel)page.DataContext!;

        // Work on a known clipboard state, and put nothing of the user's back: the page itself overwrites it.
        vm.Input = "复制我：behaviors";
        Click(Find<Button>(page, "CopyButton")); Pump();
        Check("SetClipboardTextAction put the bound text on the clipboard", WaitUntil(() => ReadClipboard(w) == "复制我：behaviors", 3000));

        Click(Find<Button>(page, "PasteButton"));
        Check("GetClipboardTextAction handed the text to the command", WaitUntil(() => vm.Pasted == "复制我：behaviors", 3000));

        Click(Find<Button>(page, "ClearButton")); Pump();
        Check("ClearClipboardAction emptied the clipboard", WaitUntil(() => string.IsNullOrEmpty(ReadClipboard(w)), 3000));

        Click(Find<Button>(page, "PasteButton"));
        Check("pasting after a clear gives an empty string", WaitUntil(() => vm.Pasted == "", 3000));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeShell.Run();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
ProbeAnimation.Run();
ProbeFileSystem.Run();
ProbeNetwork.Run();
ProbeClipboard.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel**

#### `Avalonia.BehaviorsDemo/ViewModels/ClipboardViewModel.cs`

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class ClipboardViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _input = "来自 SetClipboardTextAction 的文字";

        [ObservableProperty]
        private string _pasted = "（还没读过）";

        // GetClipboardTextAction passes the clipboard text as the command parameter.
        [RelayCommand]
        private void Paste(string? text) => Pasted = text ?? "";
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/ClipboardPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="560"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.ClipboardPage"
             x:DataType="vm:ClipboardViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="剪贴板：SetClipboardTextAction、GetClipboardTextAction、ClearClipboardAction"
                               DocPath="Avalonia.Xaml.Interactions.Core.SetClipboardTextAction" />

            <Border Classes="stage" Padding="8" Margin="0,0,0,8">
                <TextBlock Foreground="OrangeRed" TextWrapping="Wrap"
                           Text="注意：这一页会真的改写系统剪贴板，包括「清空」。点之前如果剪贴板里有重要内容，请先另存。" />
            </Border>

            <TextBlock Classes="caption" Text="1. 复制：把绑定的文字放进剪贴板" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <TextBox Width="320" Text="{Binding Input}" />
                <Button Name="CopyButton" Content="复制">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <SetClipboardTextAction Text="{Binding Input}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
            </StackPanel>

            <TextBlock Classes="caption" Text="2. 粘贴：读剪贴板，文字作为参数交给命令" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Name="PasteButton" Content="读取剪贴板">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <GetClipboardTextAction Command="{Binding PasteCommand}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock VerticalAlignment="Center" Text="{Binding Pasted, StringFormat={}读到：{0}}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="先复制，再读取，上面就会显示同样的文字；也可以在别的程序里复制一段文字，回来点读取。读取是异步的，点完稍等一下。" />

            <TextBlock Classes="caption" Text="3. 清空" />
            <Button Name="ClearButton" Content="清空剪贴板">
                <Interaction.Behaviors>
                    <EventTriggerBehavior EventName="Click">
                        <ClearClipboardAction />
                    </EventTriggerBehavior>
                </Interaction.Behaviors>
            </Button>
            <TextBlock Classes="hint" Text="清空之后再点「读取剪贴板」，读到的是空字符串。三个 Action 都不用写任何 code-behind，剪贴板对象由行为自己从所在窗口取得。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/ClipboardPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ClipboardPage : UserControl
    {
        public ClipboardPage()
        {
            InitializeComponent();
            DataContext = new ClipboardViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `bash /c/Temp/verify.sh docs/superpowers/plans/2026-10-11-behaviors-demo-02-collections-windows-drag-responsive-animation.md docs/superpowers/plans/2026-10-11-behaviors-demo-03-side-effects-and-custom.md`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 4 条断言，累计 143 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo clipboard page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 4: 页面 14 截图与离屏渲染

**Files:**
- Create: `ViewModels/ScreenshotViewModel.cs`、`Views/Pages/ScreenshotPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Screenshot.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `ScreenshotViewModel`：`Renderer`（`CounterRenderer : IRenderTargetBitmapRenderer`，有 `Calls` 计数）、`ShotPath`（沙箱里的 png 路径）。控件名：`PreviewImage`、`RerenderButton`、`ShotTarget`、`ShotButton`。`RenderTargetBitmapBehavior` 的 `x:Name` 是 `Rtb`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Screenshot.cs`**

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeScreenshot
{
    static void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static void Run()
    {
        Console.WriteLine("== page 14: screenshot and off-screen rendering ==");
        var page = new ScreenshotPage();
        Show(page, 900, 800);
        var vm = (ScreenshotViewModel)page.DataContext!;
        var image = Find<Image>(page, "PreviewImage");

        Check("RenderTargetBitmapBehavior replaced Image.Source with a bitmap", WaitUntil(() => image.Source is RenderTargetBitmap, 3000));
        var bitmap = (RenderTargetBitmap)image.Source!;
        Check("the bitmap has the requested pixel size (64 x 64)", bitmap.PixelSize.Width == 64 && bitmap.PixelSize.Height == 64);
        Check("our renderer was asked to draw at least once", vm.Renderer.Calls >= 1);

        var before = vm.Renderer.Calls;
        Click(Find<Button>(page, "RerenderButton")); Pump();
        Check("RenderRenderTargetBitmapAction asked for one more render", vm.Renderer.Calls > before);

        Check("ScreenshotAction is attached to the capture button",
            Interaction.GetBehaviors(Find<Button>(page, "ShotButton")).Count == 1);
        Check("the screenshot path stays inside the temp sandbox", vm.ShotPath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeShell.Run();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
ProbeAnimation.Run();
ProbeFileSystem.Run();
ProbeNetwork.Run();
ProbeClipboard.Run();
ProbeScreenshot.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写 ViewModel 与渲染器**

#### `Avalonia.BehaviorsDemo/ViewModels/ScreenshotViewModel.cs`

```csharp
using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Shared.ViewModels;
using Avalonia.Xaml.Interactions.Custom;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public class ScreenshotViewModel : ViewModelBase
    {
        public CounterRenderer Renderer { get; } = new();

        // The screenshot file name points into the temp sandbox, never into the user's folders.
        public string ShotPath { get; } = Path.Combine(Path.GetTempPath(), "avalonia-behaviors-demo", "screenshot.png");
    }

    /// <summary>
    /// Draws a square whose colour changes on every call, so each render is visible in the preview.
    /// </summary>
    public sealed class CounterRenderer : IRenderTargetBitmapRenderer
    {
        private static readonly IBrush[] Palette =
        {
            Brushes.DodgerBlue, Brushes.OrangeRed, Brushes.SeaGreen, Brushes.MediumPurple, Brushes.Goldenrod,
        };

        public int Calls { get; private set; }

        public void Render(DrawingContext context, TimeSpan elapsed)
        {
            var brush = Palette[Calls % Palette.Length];
            Calls++;
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 64, 64));
            context.DrawRectangle(brush, null, new Rect(8, 8, 48, 48));
        }
    }
}
```

- [ ] **Step 3: 写页面**

#### `Avalonia.BehaviorsDemo/Views/Pages/ScreenshotPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="640"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.ScreenshotPage"
             x:DataType="vm:ScreenshotViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="截图与离屏渲染：RenderTargetBitmapBehavior、RenderRenderTargetBitmapAction、ScreenshotAction"
                               DocPath="Avalonia.Xaml.Interactions.Custom.RenderTargetBitmapBehavior" />

            <TextBlock Classes="caption" Text="1. RenderTargetBitmapBehavior：挂在 Image 上，由我们的渲染器画进一张离屏位图" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Image Name="PreviewImage" Width="128" Height="128" Stretch="Uniform">
                    <Interaction.Behaviors>
                        <!-- Named so the action below can point at it. -->
                        <RenderTargetBitmapBehavior x:Name="Rtb" PixelWidth="64" PixelHeight="64"
                                                    Dpi="96,96" Renderer="{Binding Renderer}" />
                    </Interaction.Behaviors>
                </Image>
                <Button Name="RerenderButton" VerticalAlignment="Top" Content="再画一次">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <RenderRenderTargetBitmapAction Target="{Binding #Rtb}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
                <TextBlock VerticalAlignment="Top" Text="{Binding Renderer.Calls, StringFormat={}渲染器已被调用 {0} 次}" />
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="行为给 Image.Source 换成一张 64 x 64 的位图，画什么由 Renderer（实现 IRenderTargetBitmapRenderer 的对象）决定。这里的渲染器每次换一种颜色，点「再画一次」预览就会变色。" />

            <TextBlock Classes="caption" Text="2. ScreenshotAction：把一个控件截成图片（需目视）" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Name="ShotTarget" Classes="stage" Width="200" Height="80" Background="#304060">
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="截我" />
                </Border>
                <Button Name="ShotButton" VerticalAlignment="Top" Content="截图">
                    <Interaction.Behaviors>
                        <EventTriggerBehavior EventName="Click">
                            <ScreenshotAction TargetControl="{Binding #ShotTarget}" FileName="{Binding ShotPath}" />
                        </EventTriggerBehavior>
                    </Interaction.Behaviors>
                </Button>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="FileName 指向系统临时目录里的沙箱，不会写进你的文档。自动化测试里这个 Action 能执行但不会产出文件，也看不到对话框，所以真实效果要手动试：点截图，然后去上面的路径或弹出的保存对话框里确认图片。" />
            <Border Classes="stage" Padding="8">
                <TextBlock Text="{Binding ShotPath}" TextWrapping="Wrap" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/ScreenshotPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ScreenshotPage : UserControl
    {
        public ScreenshotPage()
        {
            InitializeComponent();
            DataContext = new ScreenshotViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `bash /c/Temp/verify.sh docs/superpowers/plans/2026-10-11-behaviors-demo-02-collections-windows-drag-responsive-animation.md docs/superpowers/plans/2026-10-11-behaviors-demo-03-side-effects-and-custom.md`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 6 条断言，累计 149 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo screenshot and off-screen rendering page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 5: 页面 15 自定义 Behavior、Trigger、Action

**Files:**
- Create: `Behaviors/UpperCaseBehavior.cs`、`Behaviors/ClickCountTrigger.cs`、`Behaviors/AppendTextAction.cs`、`ViewModels/CustomViewModel.cs`、`Views/Pages/CustomPage.axaml(.cs)`
- Create（仓库外）: `C:\Temp\probe-bdemo\Probe.Custom.cs`；覆盖 `Program.cs`

**Interfaces:**
- Produces: `UpperCaseBehavior : Behavior<TextBox>`；`ClickCountTrigger : StyledElementTrigger<Control>`（`Count`，默认 3）；`AppendTextAction : Action`（`Target`、`Suffix`）；`CustomViewModel.Fires` 与 `FireCommand`。控件名：`UpperBox`、`ClickPad`、`Output`。

- [ ] **Step 1: 写探针**

**`C:\Temp\probe-bdemo\Probe.Custom.cs`**

```csharp
using Avalonia;
using Avalonia.BehaviorsDemo.Behaviors;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.BehaviorsDemo.Views.Pages;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeCustom
{
    static void Press(Window w, Control c)
    {
        var pt = c.TranslatePoint(new Point(5, 5), w)!.Value;
        w.MouseDown(pt, MouseButton.Left);
        w.MouseUp(pt, MouseButton.Left);
        Pump();
    }

    public static void Run()
    {
        Console.WriteLine("== page 15: custom behavior, trigger, action ==");
        var page = new CustomPage();
        var w = Show(page, 900, 700);
        var vm = (CustomViewModel)page.DataContext!;

        var box = Find<TextBox>(page, "UpperBox");
        box.Text = "abc"; Pump();
        Check("custom Behavior: typed text is upper-cased", box.Text == "ABC");
        box.Text = "x1y"; Pump();
        Check("custom Behavior keeps reacting to later edits", box.Text == "X1Y");

        var pad = Find<Border>(page, "ClickPad");
        var output = Find<TextBlock>(page, "Output");
        var triggers = Interaction.GetBehaviors(pad);
        Check("the pad carries exactly one ClickCountTrigger", triggers.Count == 1 && triggers[0] is ClickCountTrigger { Count: 2 });

        var text0 = output.Text;
        Press(w, pad);
        Check("custom Trigger: one press is not enough", vm.Fires == 0 && output.Text == text0);
        Press(w, pad);
        Check("custom Trigger: the second press fires the stock command", vm.Fires == 1);
        Check("custom Action: the target text got the suffix appended", output.Text == text0 + "★");

        // OnDetached must unhook the event, or a removed trigger keeps firing.
        triggers.Clear();
        Press(w, pad); Press(w, pad);
        Check("after the trigger is removed, presses fire nothing", vm.Fires == 1);
    }
}
```

**`C:\Temp\probe-bdemo\Program.cs`**

```csharp
using ProbeBdemo;

Harness.Setup();
ProbeShell.Run();
ProbeMech.Run();
ProbeEvent.Run();
ProbeData.Run();
ProbeFocus.Run();
ProbeValid.Run();
ProbeCollection.Run();
ProbePopup.Run();
ProbeDrag.Run();
ProbeResponsive.Run();
ProbeAnimation.Run();
ProbeFileSystem.Run();
ProbeNetwork.Run();
ProbeClipboard.Run();
ProbeScreenshot.Run();
ProbeCustom.Run();
return Harness.Exit();
```

- [ ] **Step 2: 写三个自定义类型**

#### `Avalonia.BehaviorsDemo/Behaviors/UpperCaseBehavior.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Behaviors
{
    /// <summary>
    /// The smallest useful Behavior: subscribe in OnAttachedToVisualTree, unsubscribe in OnDetachedFromVisualTree.
    /// It keeps a TextBox upper-case, no matter how the text got there.
    /// </summary>
    public class UpperCaseBehavior : Behavior<TextBox>
    {
        protected override void OnAttachedToVisualTree()
        {
            base.OnAttachedToVisualTree();
            if (AssociatedObject is { } box)
            {
                box.TextChanged += OnTextChanged;
            }
        }

        protected override void OnDetachedFromVisualTree()
        {
            if (AssociatedObject is { } box)
            {
                box.TextChanged -= OnTextChanged;
            }

            base.OnDetachedFromVisualTree();
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (AssociatedObject is not { } box)
            {
                return;
            }

            var upper = box.Text?.ToUpperInvariant();
            if (box.Text != upper)
            {
                box.Text = upper;
            }
        }
    }
}
```

#### `Avalonia.BehaviorsDemo/Behaviors/ClickCountTrigger.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Behaviors
{
    /// <summary>
    /// A custom Trigger (a StyledElementTrigger, so bindings inside its actions resolve): runs its Actions once every <see cref="Count"/> presses.
    /// It listens to PointerPressed rather than Tapped, because the second quick Tapped is swallowed by DoubleTapped.
    /// </summary>
    public class ClickCountTrigger : StyledElementTrigger<Control>
    {
        // A StyledProperty is what makes the value settable and bindable from XAML.
        public static readonly StyledProperty<int> CountProperty =
            AvaloniaProperty.Register<ClickCountTrigger, int>(nameof(Count), 3);

        private int _presses;

        public int Count
        {
            get => GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        protected override void OnAttachedToVisualTree()
        {
            base.OnAttachedToVisualTree();
            AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble);
        }

        protected override void OnDetachedFromVisualTree()
        {
            AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            base.OnDetachedFromVisualTree();
        }

        private void OnPressed(object? sender, RoutedEventArgs e)
        {
            if (++_presses < Count)
            {
                return;
            }

            _presses = 0;
            // This is the whole job of a Trigger: decide when, then hand the Actions to Interaction.
            Interaction.ExecuteActions(AssociatedObject, Actions, null);
        }
    }
}
```

#### `Avalonia.BehaviorsDemo/Behaviors/AppendTextAction.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Behaviors
{
    /// <summary>
    /// A custom Action: appends a suffix to another TextBlock. Note the full base name:
    /// plain "Action" would collide with System.Action in files that import System.
    /// </summary>
    public class AppendTextAction : Avalonia.Xaml.Interactivity.Action
    {
        public static readonly StyledProperty<TextBlock?> TargetProperty =
            AvaloniaProperty.Register<AppendTextAction, TextBlock?>(nameof(Target));

        public static readonly StyledProperty<string> SuffixProperty =
            AvaloniaProperty.Register<AppendTextAction, string>(nameof(Suffix), "!");

        public TextBlock? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public string Suffix
        {
            get => GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        // The return value is passed to later actions that care; nothing here needs it.
        public override object? Execute(object? sender, object? parameter)
        {
            if (Target is { } target)
            {
                target.Text += Suffix;
            }

            return true;
        }
    }
}
```

- [ ] **Step 3: 写 ViewModel 与页面**

#### `Avalonia.BehaviorsDemo/ViewModels/CustomViewModel.cs`

```csharp
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class CustomViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _fires;

        [RelayCommand]
        private void Fire() => Fires++;
    }
}
```

#### `Avalonia.BehaviorsDemo/Views/Pages/CustomPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:b="using:Avalonia.BehaviorsDemo.Behaviors"
             xmlns:vm="using:Avalonia.BehaviorsDemo.ViewModels"
             mc:Ignorable="d"
             d:DesignWidth="860"
             d:DesignHeight="640"
             x:Class="Avalonia.BehaviorsDemo.Views.Pages.CustomPage"
             x:DataType="vm:CustomViewModel">

    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="自定义：自己写 Behavior、Trigger、Action"
                               DocPath="Avalonia.Xaml.Interactivity.Behavior" />

            <TextBlock Classes="caption" Text="1. 自定义 Behavior：UpperCaseBehavior，让 TextBox 永远是大写" />
            <TextBox Name="UpperBox" Width="320" HorizontalAlignment="Left" Watermark="随便输入小写字母试试">
                <Interaction.Behaviors>
                    <b:UpperCaseBehavior />
                </Interaction.Behaviors>
            </TextBox>
            <TextBlock Classes="hint"
                       Text="整个类只有几十行：继承 Behavior&lt;TextBox&gt;，在 OnAttachedToVisualTree 里订阅事件，在 OnDetachedFromVisualTree 里取消订阅。粘贴、程序赋值也会被转成大写，因为它盯的是 TextChanged。" />

            <TextBlock Classes="caption" Text="2. 自定义 Trigger 与 Action：按两下面板，才执行一次；执行的是现成的命令和我们自己写的 Action" />
            <StackPanel Orientation="Horizontal" Spacing="12">
                <Border Name="ClickPad" Classes="stage" Width="200" Height="80" Background="#304060">
                    <Interaction.Behaviors>
                        <b:ClickCountTrigger Count="2">
                            <InvokeCommandAction Command="{Binding FireCommand}" />
                            <b:AppendTextAction Target="{Binding #Output}" Suffix="★" />
                        </b:ClickCountTrigger>
                    </Interaction.Behaviors>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="在这里按两下鼠标" />
                </Border>
                <StackPanel VerticalAlignment="Center" Spacing="4">
                    <TextBlock Text="{Binding Fires, StringFormat={}触发了 {0} 次}" />
                    <TextBlock Name="Output" Text="星星：" />
                </StackPanel>
            </StackPanel>
            <TextBlock Classes="hint"
                       Text="Trigger 只负责「什么时候」（要继承 StyledElementTrigger：继承普通 Trigger 的话，里面 InvokeCommandAction 的 {Binding} 命令解析不出来）：数够了就调用 Interaction.ExecuteActions，把自己的 Actions 交出去。Action 只负责「做什么」：重写 Execute。两者互不知道对方，所以 InvokeCommandAction 和自定义的 AppendTextAction 可以混着放在同一个触发器里。Count、Target、Suffix 都是 StyledProperty，所以能直接在 XAML 里写、也能绑定。触发器监听 PointerPressed 而不是 Tapped：连点时第二次 Tapped 会被 DoubleTapped 吞掉，永远数不够。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.BehaviorsDemo/Views/Pages/CustomPage.axaml.cs`

```csharp
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class CustomPage : UserControl
    {
        public CustomPage()
        {
            InitializeComponent();
            DataContext = new CustomViewModel();
        }
    }
}
```

- [ ] **Step 4: 抽取、构建并运行探针**

Run: `bash /c/Temp/verify.sh docs/superpowers/plans/2026-10-11-behaviors-demo-02-collections-windows-drag-responsive-animation.md docs/superpowers/plans/2026-10-11-behaviors-demo-03-side-effects-and-custom.md`
Expected: 无 `FAIL`，`0 warning log(s)`，本页新增 7 条断言，累计 156 条。

- [ ] **Step 5: 提交**

```bash
git add Avalonia.BehaviorsDemo
git commit -m "feat: add the BehaviorsDemo custom behavior, trigger and action page

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

## Task 6: 登记全部页面、整体探针、README、CLAUDE.md 与 spec 回填

**Files:**
- 覆盖: `Avalonia.BehaviorsDemo/Views/MainWindow.axaml`（10 个标签 → 15 个标签）
- Modify: `README.md`（项目行改成 15 页）、`CLAUDE.md`（加 BehaviorsDemo 一节）、`docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md`（追加「批 3 实测结论」）
- Create（仓库外）: 覆盖 `C:\Temp\probe-bdemo\Probe.Shell.cs`

- [ ] **Step 1: 改写 Shell 探针（先于 MainWindow）**

**`C:\Temp\probe-bdemo\Probe.Shell.cs`**

```csharp
using Avalonia.BehaviorsDemo.Views;
using Avalonia.Controls;
using static ProbeBdemo.Harness;

namespace ProbeBdemo;

static class ProbeShell
{
    public static void Run()
    {
        Console.WriteLine("== shell: MainWindow ==");
        var window = new MainWindow();
        window.Show();
        Pump();
        var tabs = Find<TabControl>(window, "MainTabs");
        Check("MainWindow lists 15 tabs", tabs.Items.Count == 15);
        var headers = tabs.Items.OfType<TabItem>().Select(t => t.Header?.ToString()).ToArray();
        Check("tab headers match the page order",
            string.Join("|", headers) == "机制|事件转命令|数据驱动|焦点键盘指针|表单验证|集合|窗口弹出通知|拖拽|响应式|动画|文件系统|网络|剪贴板|截图|自定义");
        for (var i = 0; i < tabs.Items.Count; i++)
        {
            tabs.SelectedIndex = i;
            Pump();
            var item = (TabItem)tabs.Items[i]!;
            Check($"tab {i + 1} ({headers[i]}) renders a page", item.Content is UserControl);
        }
        window.Close();
    }
}
```

- [ ] **Step 2: 写完整的 MainWindow（覆盖计划 02 的 10 标签版本）**

#### `Avalonia.BehaviorsDemo/Views/MainWindow.axaml`

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:pages="using:Avalonia.BehaviorsDemo.Views.Pages"
        mc:Ignorable="d"
        d:DesignWidth="1000"
        d:DesignHeight="720"
        x:Class="Avalonia.BehaviorsDemo.Views.MainWindow"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia Behaviors Demo"
        Width="1000"
        Height="720"
        WindowStartupLocation="CenterScreen">

    <TabControl Name="MainTabs" Margin="12" TabStripPlacement="Left">
        <TabItem Header="机制">
            <pages:MechanismPage />
        </TabItem>
        <TabItem Header="事件转命令">
            <pages:EventToCommandPage />
        </TabItem>
        <TabItem Header="数据驱动">
            <pages:DataTriggerPage />
        </TabItem>
        <TabItem Header="焦点键盘指针">
            <pages:FocusKeyboardPage />
        </TabItem>
        <TabItem Header="表单验证">
            <pages:ValidationPage />
        </TabItem>
        <TabItem Header="集合">
            <pages:CollectionPage />
        </TabItem>
        <TabItem Header="窗口弹出通知">
            <pages:WindowPopupPage />
        </TabItem>
        <TabItem Header="拖拽">
            <pages:DragPage />
        </TabItem>
        <TabItem Header="响应式">
            <pages:ResponsivePage />
        </TabItem>
        <TabItem Header="动画">
            <pages:AnimationPage />
        </TabItem>
        <TabItem Header="文件系统">
            <pages:FileSystemPage />
        </TabItem>
        <TabItem Header="网络">
            <pages:NetworkPage />
        </TabItem>
        <TabItem Header="剪贴板">
            <pages:ClipboardPage />
        </TabItem>
        <TabItem Header="截图">
            <pages:ScreenshotPage />
        </TabItem>
        <TabItem Header="自定义">
            <pages:CustomPage />
        </TabItem>
    </TabControl>
</Window>
```

- [ ] **Step 3: 抽取、构建、运行全部探针**

Run: `dotnet build Avalonia.BehaviorsDemo 2>&1 | grep -E " error |个错误"` 然后 `bash /c/Temp/verify.sh <计划 02> <本计划>`
Expected: 构建 0 个错误；探针无 `FAIL`，末行 `161 passed, 0 failed, 0 warning log(s)`（计划 02 的 118 条里 Shell 的 12 条换成 17 条，再加页面 11–15 的 13 + 8 + 4 + 6 + 7 = 38 条）。

- [ ] **Step 4: 更新 README 的项目行**

把计划 02 写入的那一行（`| [Avalonia.BehaviorsDemo]…`）整行替换为：

```markdown
| [Avalonia.BehaviorsDemo](Avalonia.BehaviorsDemo) | Xaml.Behaviors.Avalonia：Behavior / Trigger / Action 机制、事件转命令、数据触发、焦点键盘指针、表单验证、集合、窗口弹出通知、拖拽、响应式、动画，以及文件系统、网络、剪贴板、截图（均限制在沙箱内）和自定义 Behavior（侧边导航，共 15 页） |
```

- [ ] **Step 5: 在 CLAUDE.md 里加一节**

用 Edit 在 `CLAUDE.md` 末尾（`ControlsDemo` 一节之后）追加：

```markdown

## BehaviorsDemo（Xaml.Behaviors.Avalonia）

`docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md` 与 `docs/superpowers/plans/2026-10-11-behaviors-demo-0*.md` 描述了单项目 `Avalonia.BehaviorsDemo`（左侧 TabControl 导航，共 15 页，按概念层次组织）。计划里的代码块用 `#### \`路径\`` 标题标注目标文件，可整块抽取；各页的"实测结论"回填在 spec 的「批 N 实测结论」小节。

- 验证靠仓库外的 headless 探针（`C:\Temp\probe-bdemo`，不入库）。计时器类行为（`DebounceAction`、`TimerTrigger`）在 headless 里要跑 `Dispatcher.UIThread.MainLoop` 才会走。
- 副作用动作都限制在安全范围：文件只动 `Path.GetTempPath()` 下的 `avalonia-behaviors-demo`，网络只连本进程的 127.0.0.1 回环服务，剪贴板与截图页有醒目提示。
- 行为库的坑：继承普通 `Trigger`（如 `FileSystemWatcherTrigger`、`NetworkInformationTrigger`，以及自己写的 `Trigger<T>`）的触发器不会把 DataContext 传给里面的 Action，`InvokeCommandAction` 的 `{Binding}` 命令不执行且无日志，自写触发器应继承 `StyledElementTrigger<T>`；`Animation` 不能直接写在行为里（放进 `Style.Resources` 再 `DynamicResource` 引用）；`ListReorderDragBehavior` 必须经 `ListBoxItem` 样式挂载。
```

- [ ] **Step 6: 回填 spec 的「批 3 实测结论」**

在 spec 末尾（「批 2 实测结论」小节之后）追加：

```markdown

## 批 3 实测结论（计划 03 执行后）

- **普通 `Trigger` 不给 Action 传 DataContext**：`FileSystemWatcherTrigger`、`NetworkInformationTrigger` 都继承它，里面 `InvokeCommandAction` 的 `Command="{Binding …}"` 解析不出来（触发 0 次、无日志）；在代码里直接给 `Command` 赋值就正常。自己写的 `Trigger<T>` 同样如此（在 UI 线程上也失败，排除了线程原因），改继承 `StyledElementTrigger<T>` 后绑定恢复。`EventTriggerBehavior` 等本来就是 `StyledElementTrigger`，不受影响。
- **`FileSystemWatcherTrigger` 要求挂载时目录已存在**；新建 `.txt` 触发两次（创建 + 内容变化），追加再触发一次；事件来自线程池线程，命令里要回到 UI 线程。
- **`HttpRequestAction` 的响应是只读属性**：Action 要有 `x:Name`，用 `{Binding #名字.ResponseContent}` 读回；对本进程的 `HttpListener` 回环服务 POST 得到 201 与「pong:ping」。
- **`NetworkInformationTrigger` 没有属性，headless 里无法制造事件**，只能验证挂载，真实效果需手动开关网络。
- **剪贴板三个 Action 都能用**，清空后读回空字符串；页面提示会改写真实剪贴板。
- **`RenderTargetBitmapBehavior` 挂在 `Image` 上**，`Renderer` 绑定到实现 `IRenderTargetBitmapRenderer` 的对象；`RenderRenderTargetBitmapAction` 能再触发一次渲染。**`ScreenshotAction` headless 里不产出文件**，真实效果需目视。
- **自定义三件套**：`Behavior<T>` 在 `OnAttachedToVisualTree` 订阅、`OnDetachedFromVisualTree` 取消；触发器用 `Interaction.ExecuteActions(AssociatedObject, Actions, null)`；Action 基类要写全名 `Avalonia.Xaml.Interactivity.Action`（避免与 `System.Action` 冲突）。连点计数要监听 `PointerPressed`，`Tapped` 的第二次会被 `DoubleTapped` 吞掉。
- 探针累计 161 条断言全部通过，0 条警告日志。
```

- [ ] **Step 7: 全解决方案构建，提交**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E " error |个错误"`
Expected: 0 个错误。

```bash
git add Avalonia.BehaviorsDemo README.md CLAUDE.md docs/superpowers/specs/2026-10-11-avalonia-behaviors-demo-design.md
git commit -m "feat: register all fifteen BehaviorsDemo pages and record batch 3 findings

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

- **Spec 覆盖**：页面 11–15（Task 1–5）、15 标签导航、README 与 CLAUDE.md 收尾、spec 回填（Task 6）。副作用动作（文件、网络、剪贴板、截图）全部演示，且都限制在沙箱或回环内。
- **占位扫描**：无 TBD/TODO；每个代码步骤都有完整代码，每个探针断言都有明确期望。
- **类型一致**：命令名由 `[RelayCommand]` 方法名加 `Command` 生成（`WatcherHit`→`WatcherHitCommand`、`NetworkChanged`→`NetworkChangedCommand`、`Paste`→`PasteCommand`、`Fire`→`FireCommand`）；控件名在探针与 XAML 中逐一对照；`Program.cs` 在每个任务里都是累积版本。
- **已知的需目视项**：`ScreenshotAction` 的真实产出、`NetworkInformationTrigger`、`TiltEffectBehavior`/`ParallaxBehavior`、文字与文件投放；README 与页面上都已注明。
- **实测过程**：本计划的所有代码块都用计划 01 附录 A 的脚本抽取、构建并跑通了探针（见 Task 6 Step 3 的期望数）。
