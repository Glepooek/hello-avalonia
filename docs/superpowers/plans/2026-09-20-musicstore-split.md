# Avalonia.MusicStore 迁移与拆分 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 `Avalonia.MusicStore` 从 hello-dotnet 迁到 hello-avalonia，并按演示主题拆成 4 个独立项目 + 1 个共享类库。

**Architecture:** 单一 `.slnx` 解决方案 + `Directory.Packages.props` 集中管理包版本。`Avalonia.Shared` 类库承载 Helpers / MessageParam / ViewModelBase，四个演示项目单向引用它，彼此无依赖，各自可独立启动。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、CommunityToolkit.Mvvm 8.4.2、Avalonia.Controls.WebView 12.1.0、Avalonia.HtmlRenderer 12.0.0、Xaml.Behaviors.Avalonia 12.0.7、iTunesSearch 1.0.44

**Spec:** `docs/superpowers/specs/2026-09-20-musicstore-split-design.md`

## Global Constraints

- 目标框架一律 `net10.0`；`Nullable` 启用；`AvaloniaUseCompiledBindingsByDefault` 为 true
- 包版本只在 `Directory.Packages.props` 声明，各项目 `<PackageReference>` **不带** `Version` 属性
- 新增代码注释一律用英文；不修改从原项目搬来的既有中文注释
- 命名空间：共享库 `Avalonia.Shared.*`，演示项目 `Avalonia.<Name>Demo.*`（MusicStore 保持 `Avalonia.MusicStore.*`）
- `MessageParam.Reult` 拼写错误保持原样，不修正
- 源路径统一记为 `$SRC` = `E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\Avalonia.MusicStore`
- 目标仓库根目录 `$DST` = `E:\ProjectxPlex\WPFCodePlex\hello-avalonia`
- 无自动化测试（GUI 演示仓库约定）。每个任务的验证 = `dotnet build` 零错误 + 启动 exe 无异常 + 截图确认渲染
- 每个任务结束提交一次，提交信息末尾附：`Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`

---

## 文件结构总览

```
hello-avalonia/
├── hello-avalonia.slnx
├── Directory.Packages.props
├── Avalonia.Shared/
│   ├── Avalonia.Shared.csproj          # 类库，无 OutputType
│   ├── Helpers/AvaloniaHelper.cs       # GetMainWindow / GetTopLevel
│   ├── Helpers/NativeMethodHelper.cs   # Win32 MouseDownDrag
│   ├── Messages/MessageParam.cs        # { bool Reult; object Data; }
│   └── ViewModels/ViewModelBase.cs     # : ObservableObject
├── Avalonia.MusicStore/                # Task 3
├── Avalonia.WebViewDemo/               # Task 4
├── Avalonia.HtmlRendererDemo/          # Task 5
└── Avalonia.DataTemplateDemo/          # Task 6
```

四个演示项目共有的骨架文件（每个项目一份，内容按项目名替换）：
`App.axaml`、`App.axaml.cs`、`Program.cs`、`app.manifest`、`Views/MainWindow.axaml(.cs)`、`Assets/avalonia-logo.ico`。

## 任务依赖

```
Task 1 (脚手架) → Task 2 (Shared) → Task 3/4/5/6 (四个演示，彼此独立可并行)
                                  → Task 7 (README) → Task 8 (删除源目录)
```

---

### Task 1: 解决方案脚手架与集中式包管理

**Files:**
- Create: `$DST/Directory.Packages.props`
- Create: `$DST/hello-avalonia.slnx`

**Interfaces:**
- Consumes: 无
- Produces: `Directory.Packages.props` 中的 `PackageVersion` 条目，Task 2-6 的 csproj 依赖这些版本号；`hello-avalonia.slnx` 供后续任务追加项目条目

沿用仓库现有 `.gitignore`（345 行标准 VS 模板，已覆盖 bin/obj），不新建。

- [ ] **Step 1: 创建 `Directory.Packages.props`**

```xml
<Project>
    <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    </PropertyGroup>

    <ItemGroup>
        <PackageVersion Include="Avalonia" Version="12.1.2" />
        <PackageVersion Include="Avalonia.Desktop" Version="12.1.2" />
        <PackageVersion Include="Avalonia.Themes.Fluent" Version="12.1.2" />
        <PackageVersion Include="Avalonia.Fonts.Inter" Version="12.1.2" />
        <!--NOTE: Avalonia.Diagnostics has no 12.x release yet; DevTools are built into the main Avalonia package in v12.-->
        <PackageVersion Include="Avalonia.Diagnostics" Version="11.3.22" />
        <PackageVersion Include="Avalonia.Controls.WebView" Version="12.1.0" />
        <PackageVersion Include="Avalonia.HtmlRenderer" Version="12.0.0" />
        <!--NOTE: Avalonia.Xaml.Behaviors stopped at 11.3.x and is binary-incompatible with Avalonia 12;
            the same author continues it as Xaml.Behaviors.Avalonia for the 12.x line.-->
        <PackageVersion Include="Xaml.Behaviors.Avalonia" Version="12.0.7" />
        <PackageVersion Include="AsyncImageLoader.Avalonia" Version="3.8.0" />
        <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />
        <PackageVersion Include="MessageBox.Avalonia" Version="12.0.0" />
        <PackageVersion Include="iTunesSearch" Version="1.0.44" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 创建空的 `hello-avalonia.slnx`**

```xml
<Solution>
</Solution>
```

- [ ] **Step 3: 验证 slnx 可被 dotnet 识别**

Run: `dotnet sln "$DST/hello-avalonia.slnx" list`
Expected: 输出 "在解决方案中找不到项目。"（或英文等价）——表示格式合法但暂无项目。若报解析错误则 XML 有误。

- [ ] **Step 4: 提交**

```bash
cd "$DST"
git add Directory.Packages.props hello-avalonia.slnx
git commit -m "chore: add solution scaffold and central package management

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Avalonia.Shared 共享类库

**Files:**
- Create: `$DST/Avalonia.Shared/Avalonia.Shared.csproj`
- Create: `$DST/Avalonia.Shared/Helpers/AvaloniaHelper.cs`
- Create: `$DST/Avalonia.Shared/Helpers/NativeMethodHelper.cs`
- Create: `$DST/Avalonia.Shared/Messages/MessageParam.cs`
- Create: `$DST/Avalonia.Shared/ViewModels/ViewModelBase.cs`
- Modify: `$DST/hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 1 的 `Directory.Packages.props`（需要 `Avalonia`、`CommunityToolkit.Mvvm` 版本）
- Produces: 供 Task 3-6 使用的公开 API：
  - `Avalonia.Shared.Helpers.AvaloniaHelper.GetMainWindow() -> Window?`
  - `Avalonia.Shared.Helpers.AvaloniaHelper.GetTopLevel(Visual?) -> Window?`
  - `Avalonia.Shared.Helpers.NativeMethodHelper.MouseDownDrag(IntPtr) -> void`
  - `Avalonia.Shared.Messages.MessageParam` — 属性 `bool Reult`、`object Data`
  - `Avalonia.Shared.ViewModels.ViewModelBase : ObservableObject`

- [ ] **Step 1: 创建类库 csproj**

注意 `OutputType` 不设（默认 Library），引用 `Avalonia` 而非 `Avalonia.Desktop`——类库不需要桌面启动器。

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 创建 `Helpers/AvaloniaHelper.cs`**

从 `$SRC/Helpers/AvaloniaHelper.cs` 搬运，仅改命名空间，并删掉原文件里未使用的 using（`System.Collections.Generic`/`Linq`/`Text`/`Threading.Tasks`）——这些是本次移动产生的孤立引用。

```csharp
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Shared.Helpers
{
    public class AvaloniaHelper
    {
        public static Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desk)
            {
                return desk.MainWindow;
            }

            return null;
        }

        public static Window? GetTopLevel(Visual? visual)
        {
            return TopLevel.GetTopLevel(visual) as Window;
        }
    }
}
```

- [ ] **Step 3: 创建 `Helpers/NativeMethodHelper.cs`**

原样搬运（保留既有中文注释），只改命名空间。

```csharp
using System;
using System.Runtime.InteropServices;

namespace Avalonia.Shared.Helpers
{
    public class NativeMethodHelper
    {
        /// <summary>
        /// 拖动窗体
        /// </summary>
        public static void MouseDownDrag(IntPtr hWnd)
        {
            ReleaseCapture();
            SendMessage(hWnd, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        #region Win32API

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        #endregion
    }
}
```

- [ ] **Step 4: 创建 `Messages/MessageParam.cs`**

`Reult` 拼写保持原样（见 spec「保留 Reult 拼写」）。删掉原文件未使用的 using。

```csharp
namespace Avalonia.Shared.Messages
{
    /// <summary>
    /// Payload for WeakReferenceMessenger. Routed by type, so both the music store
    /// (which uses Data) and the WebView demo (which uses Reult) share this one class.
    /// </summary>
    public class MessageParam
    {
        public bool Reult { get; set; }
        public object Data { get; set; }
    }
}
```

- [ ] **Step 5: 创建 `ViewModels/ViewModelBase.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Shared.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
    }
}
```

- [ ] **Step 6: 把项目加入解决方案**

```bash
cd "$DST"
dotnet sln hello-avalonia.slnx add Avalonia.Shared/Avalonia.Shared.csproj
```

- [ ] **Step 7: 构建验证**

Run: `dotnet build "$DST/Avalonia.Shared/Avalonia.Shared.csproj" -v q --nologo`
Expected: `已成功生成。` / 0 个错误。
若报 `NU1008`（项目指定了 Version 但启用了集中管理），说明 Step 1 的 csproj 误加了 `Version` 属性。

- [ ] **Step 8: 提交**

```bash
cd "$DST"
git add Avalonia.Shared hello-avalonia.slnx
git commit -m "feat: add Avalonia.Shared library

Helpers, MessageParam and ViewModelBase shared by the demo projects.
MessageParam is shared deliberately: WeakReferenceMessenger routes by type,
so one class lets the demos illustrate a cross-window message bus.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Avalonia.MusicStore

搬运专辑业务（搜索 / 购买 / 缓存 / 多语言），移除 WebView、富文本、IconFont、模版选择器相关内容。

**Files:**
- Create: `$DST/Avalonia.MusicStore/Avalonia.MusicStore.csproj`
- Create: `$DST/Avalonia.MusicStore/App.axaml`、`App.axaml.cs`、`Program.cs`、`app.manifest`
- Copy from `$SRC`（保持相对路径，仅改命名空间与 using）:
  - `Models/Album.cs`
  - `ViewModels/AlbumViewModel.cs`、`ViewModels/MusicStoreViewModel.cs`
  - `Views/AlbumView.axaml(.cs)`、`Views/MusicStoreView.axaml(.cs)`、`Views/MusicStoreWindow.axaml(.cs)`
  - `Assets/avalonia-logo.ico`、`Assets/Langs/*`（3 个文件）
  - `Resources/Icons.axaml`（含 `store_microsoft_regular`、`music_regular` 两个键，
    均为音乐商店主题；HtmlRendererDemo 会各自持有一份副本用于演示 PathIcon）
- Create: `$DST/Avalonia.MusicStore/ViewModels/MainWindowViewModel.cs`（精简重写）
- Create: `$DST/Avalonia.MusicStore/Views/MainWindow.axaml(.cs)`（精简重写）
- Modify: `$DST/hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 2 的 `AvaloniaHelper.GetMainWindow()`、`AvaloniaHelper.GetTopLevel(Visual?)`、`MessageParam`（`Data` 字段）、`ViewModelBase`
- Produces: 无（终端项目，不被其他项目引用）

**不搬运的内容**（属于其他任务）：`Views/WebViewWindow.*`、`TestWeb/`、`DataTemplates/`、`Models/Person.cs`、`Resources/ButtonStyles.axaml`、`Resources/Icons.axaml`、`Assets/iconfont.ttf`。

- [ ] **Step 1: 创建 csproj**

相比原 csproj 移除了：`Avalonia.Controls.WebView`、`Avalonia.HtmlRenderer`、`Xaml.Behaviors.Avalonia`、`TestWeb` 拷贝项、`System.Xaml` 引用（那是 HtmlRenderer 的依赖，会产生 MSB3245 警告）、`README.md` 的 AvaloniaXaml 项（原项目里这是误配）。

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
        <AvaloniaResource Remove="Assets\Langs\Resources.en-US.resx" />
        <AvaloniaResource Remove="Assets\Langs\Resources.resx" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="Avalonia.Fonts.Inter" />
        <PackageReference Condition="'$(Configuration)' == 'Debug'" Include="Avalonia.Diagnostics" />
        <PackageReference Include="AsyncImageLoader.Avalonia" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
        <PackageReference Include="MessageBox.Avalonia" />
        <PackageReference Include="iTunesSearch" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>

    <ItemGroup>
        <EmbeddedResource Update="Assets\Langs\Resources.en-US.resx">
            <Generator>PublicResXFileCodeGenerator</Generator>
        </EmbeddedResource>
        <EmbeddedResource Update="Assets\Langs\Resources.resx">
            <Generator>PublicResXFileCodeGenerator</Generator>
            <LastGenOutput>Resources.Designer.cs</LastGenOutput>
        </EmbeddedResource>
        <Compile Update="Assets\Langs\Resources.Designer.cs">
            <DesignTime>True</DesignTime>
            <AutoGen>True</AutoGen>
            <DependentUpon>Resources.resx</DependentUpon>
        </Compile>
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 拷贝资源与业务文件**

```powershell
$SRC = "E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\Avalonia.MusicStore"
$DST = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.MusicStore"
New-Item -ItemType Directory -Force "$DST\Models","$DST\ViewModels","$DST\Views","$DST\Assets\Langs" | Out-Null
Copy-Item "$SRC\app.manifest" $DST
Copy-Item "$SRC\Assets\avalonia-logo.ico" "$DST\Assets"
Copy-Item "$SRC\Assets\Langs\*" "$DST\Assets\Langs"
Copy-Item "$SRC\Models\Album.cs" "$DST\Models"
Copy-Item "$SRC\ViewModels\AlbumViewModel.cs","$SRC\ViewModels\MusicStoreViewModel.cs" "$DST\ViewModels"
Copy-Item "$SRC\Views\AlbumView.axaml","$SRC\Views\AlbumView.axaml.cs","$SRC\Views\MusicStoreView.axaml","$SRC\Views\MusicStoreView.axaml.cs","$SRC\Views\MusicStoreWindow.axaml","$SRC\Views\MusicStoreWindow.axaml.cs" "$DST\Views"
New-Item -ItemType Directory -Force "$DST\Resources" | Out-Null
Copy-Item "$SRC\Resources\Icons.axaml" "$DST\Resources"
```

- [ ] **Step 3: 改写拷贝文件中的引用**

命名空间保持 `Avalonia.MusicStore.*` 不变，只改指向 Shared 的 using：

| 文件 | 改动 |
|---|---|
| `ViewModels/AlbumViewModel.cs` | 加 `using Avalonia.Shared.ViewModels;`（`ViewModelBase` 现在来自 Shared） |
| `ViewModels/MusicStoreViewModel.cs` | `using Avalonia.MusicStore.Helpers;` → `using Avalonia.Shared.Helpers;`<br>`using Avalonia.MusicStore.Messages;` → `using Avalonia.Shared.Messages;`<br>加 `using Avalonia.Shared.ViewModels;` |
| `Views/AlbumView.axaml.cs`、`MusicStoreView.axaml.cs`、`MusicStoreWindow.axaml.cs` | 无需改 |
| `Views/MusicStoreView.axaml` | `Watermark` → `PlaceholderText`（消除 AVLN5001 过时警告） |
| `Models/Album.cs` | 无需改 |

- [ ] **Step 4: 创建 `App.axaml`**

保留 `Icons.axaml`（商店图标在主窗要用），移除 `IconFont` 字体资源（那属于
HtmlRendererDemo）。

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.MusicStore.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Avalonia.MusicStore/Resources/Icons.axaml" />
    </Application.Styles>
</Application>
```

- [ ] **Step 5: 创建 `App.axaml.cs`**

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.MusicStore.ViewModels;
using Avalonia.MusicStore.Views;

namespace Avalonia.MusicStore
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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

- [ ] **Step 6: 创建 `Program.cs`**

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.MusicStore
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

- [ ] **Step 7: 创建 `ViewModels/MainWindowViewModel.cs`**

相比原版移除：`ShowWebCommand`、`ShowWebDialogCommand`、`CallJSMethodCommand`、`ShowNativeWebDialog`、`Text` 富文本属性、`People` 集合。保留专辑加载与购买回执。

```csharp
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.Views;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.Messages;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.MusicStore.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IRecipient<MessageParam>
    {
        #region Properties

        public ObservableCollection<AlbumViewModel> Albums { get; } = new();

        #endregion

        #region Commands

        public RelayCommand LoadedCommand { get; private set; }
        public RelayCommand ShowAlbumsCommand { get; private set; }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            LoadedCommand = new RelayCommand(LoadAlbums);

            ShowAlbumsCommand = new RelayCommand(() =>
            {
                MusicStoreWindow dialog = new MusicStoreWindow();
                dialog.ShowDialog(AvaloniaHelper.GetMainWindow());
            });

            WeakReferenceMessenger.Default.Register<MessageParam>(this);
        }

        #endregion

        #region Methods

        public async void Receive(MessageParam message)
        {
            if (message?.Data is AlbumViewModel albumVM
                && !Albums.Contains(albumVM))
            {
                Albums.Add(albumVM);
                await albumVM.SaveToDiskAsync();
            }
        }

        public async void LoadAlbums()
        {
            var albums = (await Album.LoadCachedAsync()).Select(x => new AlbumViewModel(x));

            foreach (var album in albums)
            {
                Albums.Add(album);
            }

            foreach (var album in Albums.ToList())
            {
                await album.LoadCover();
            }
        }

        #endregion
    }
}
```

- [ ] **Step 8: 创建 `Views/MainWindow.axaml`**

Loaded 触发改用 `Window.Loaded` 事件直连，省掉 Behaviors 包依赖。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="using:Avalonia.MusicStore.ViewModels"
        xmlns:views="using:Avalonia.MusicStore.Views"
        mc:Ignorable="d"
        d:DesignWidth="800"
        d:DesignHeight="450"
        x:Class="Avalonia.MusicStore.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia.MusicStore"
        Width="1000"
        Height="700"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        Loaded="OnLoaded">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <Panel>
        <ExperimentalAcrylicBorder IsHitTestVisible="False">
            <ExperimentalAcrylicBorder.Material>
                <ExperimentalAcrylicMaterial BackgroundSource="Digger"
                                             TintColor="Black"
                                             TintOpacity="1"
                                             MaterialOpacity="0.65" />
            </ExperimentalAcrylicBorder.Material>
        </ExperimentalAcrylicBorder>

        <Grid Margin="40" RowDefinitions="auto,*">
            <Button Grid.Row="0"
                    HorizontalAlignment="Right"
                    Command="{Binding ShowAlbumsCommand}"
                    ToolTip.Tip="Music Store">
                <PathIcon Data="{StaticResource store_microsoft_regular}" />
            </Button>

            <ItemsControl Grid.Row="1"
                          Margin="0,40,0,0"
                          ItemsSource="{Binding Albums}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <views:AlbumView Margin="0,0,20,20" />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </Grid>
    </Panel>
</Window>
```

多语言资源（`Assets/Langs`）仍随项目保留，由 `MusicStoreView.axaml` 中已有的
`{x:Static lang:Resources.*}` 绑定继续演示。主窗按钮改用商店图标 + ToolTip，不复用
语义不符的 `btn_ShowFlyout` 文案键。

- [ ] **Step 9: 创建 `Views/MainWindow.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.MusicStore.ViewModels;

namespace Avalonia.MusicStore.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            (DataContext as MainWindowViewModel)?.LoadedCommand.Execute(null);
        }
    }
}
```

- [ ] **Step 10: 加入解决方案并构建**

```bash
cd "$DST"
dotnet sln hello-avalonia.slnx add Avalonia.MusicStore/Avalonia.MusicStore.csproj
dotnet build Avalonia.MusicStore/Avalonia.MusicStore.csproj -v q --nologo
```
Expected: 0 个错误。常见失败：`CS0246 未找到 AvaloniaHelper` → Step 3 的 using 未改全。

- [ ] **Step 11: 运行验证**

```powershell
$b = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.MusicStore\bin\Debug\net10.0"
# 从旧项目拷入已有缓存，使专辑区有数据可显示
$old = "E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\Avalonia.MusicStore\bin\Debug\net10.0\Cache"
if (Test-Path $old) { New-Item -ItemType Directory -Force "$b\Cache" | Out-Null; Copy-Item "$old\*" "$b\Cache" }
Start-Process "$b\Avalonia.MusicStore.exe" -WorkingDirectory $b -RedirectStandardError "$env:TEMP\ms_err.txt"
Start-Sleep 10
Get-Content "$env:TEMP\ms_err.txt"
```
Expected: stderr 为空；截图可见专辑封面网格与 Music Store 按钮。点击按钮能打开搜索窗并搜到结果。

- [ ] **Step 12: 提交**

```bash
cd "$DST"
git add Avalonia.MusicStore hello-avalonia.slnx
git commit -m "feat: add Avalonia.MusicStore demo

Album search, purchase and local cache, plus the RESX-based i18n sample.
WebView, rich text, icon font and data template demos move to their own
projects.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Avalonia.WebViewDemo

拆分的核心产物。主窗从"音乐商店的一个按钮"升级为演示菜单，三个入口分别覆盖
`NativeWebView` 嵌入控件、`NativeWebDialog` 原生窗口、C# 调用 JS。

**Files:**
- Create: `$DST/Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj`
- Create: `App.axaml`、`App.axaml.cs`、`Program.cs`、`app.manifest`
- Create: `ViewModels/MainWindowViewModel.cs`
- Create: `Views/MainWindow.axaml(.cs)`
- Copy from `$SRC`: `Views/WebViewWindow.axaml(.cs)`、`TestWeb/index.html`、`TestWeb/vue.js`、`Assets/avalonia-logo.ico`
- Modify: `$DST/hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 2 的 `AvaloniaHelper.GetMainWindow()`、`MessageParam`（`Reult` 字段）、`ViewModelBase`
- Produces: 无（终端项目）

- [ ] **Step 1: 创建 csproj**

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
        <PackageReference Condition="'$(Configuration)' == 'Debug'" Include="Avalonia.Diagnostics" />
        <PackageReference Include="Avalonia.Controls.WebView" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>

    <ItemGroup>
        <None Update="TestWeb\index.html">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </None>
        <None Update="TestWeb\vue.js">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </None>
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 拷贝文件**

```powershell
$SRC = "E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\Avalonia.MusicStore"
$DST = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.WebViewDemo"
New-Item -ItemType Directory -Force "$DST\ViewModels","$DST\Views","$DST\Assets","$DST\TestWeb" | Out-Null
Copy-Item "$SRC\app.manifest" $DST
Copy-Item "$SRC\Assets\avalonia-logo.ico" "$DST\Assets"
Copy-Item "$SRC\TestWeb\index.html","$SRC\TestWeb\vue.js" "$DST\TestWeb"
Copy-Item "$SRC\Views\WebViewWindow.axaml","$SRC\Views\WebViewWindow.axaml.cs" "$DST\Views"
```

- [ ] **Step 3: 改写 `WebViewWindow` 的命名空间**

在 `Views/WebViewWindow.axaml.cs` 中：
- `namespace Avalonia.MusicStore.Views;` → `namespace Avalonia.WebViewDemo.Views;`
- `using Avalonia.MusicStore.Messages;` → `using Avalonia.Shared.Messages;`

在 `Views/WebViewWindow.axaml` 中：
- `x:Class="Avalonia.MusicStore.Views.WebViewWindow"` → `x:Class="Avalonia.WebViewDemo.Views.WebViewWindow"`

其余内容（加载动画样式、NativeWebView 控件、JSON 信封桥）原样保留。

- [ ] **Step 4: 创建 `App.axaml`**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.WebViewDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

- [ ] **Step 5: 创建 `App.axaml.cs`**

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.WebViewDemo.ViewModels;
using Avalonia.WebViewDemo.Views;

namespace Avalonia.WebViewDemo
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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

- [ ] **Step 6: 创建 `Program.cs`**

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.WebViewDemo
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

- [ ] **Step 7: 创建 `ViewModels/MainWindowViewModel.cs`**

三个命令分别对应三种演示。`ShowNativeWebDialog` 从原 `MainWindowViewModel` 搬来，逻辑不变。

```csharp
using Avalonia.Controls;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.Messages;
using Avalonia.Shared.ViewModels;
using Avalonia.WebViewDemo.Views;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;

namespace Avalonia.WebViewDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Commands

        public RelayCommand ShowWebViewCommand { get; private set; }
        public RelayCommand ShowWebDialogCommand { get; private set; }
        public RelayCommand CallJSMethodCommand { get; private set; }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            // NativeWebView hosted inside an Avalonia Window.
            ShowWebViewCommand = new RelayCommand(() =>
            {
                WebViewWindow dialog = new WebViewWindow();
                dialog.Show(AvaloniaHelper.GetMainWindow());
            });

            // NativeWebDialog hosts web content in its own native window, with no Avalonia Window wrapper.
            ShowWebDialogCommand = new RelayCommand(ShowNativeWebDialog);

            // C# -> JS, received by the open WebViewWindow.
            CallJSMethodCommand = new RelayCommand(() =>
            {
                WeakReferenceMessenger.Default.Send<MessageParam>(new MessageParam { Reult = true });
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Demonstrates NativeWebDialog: a native window hosting web content, shown owned by the main window.
        /// </summary>
        private void ShowNativeWebDialog()
        {
            var dialog = new NativeWebDialog
            {
                Title = "NativeWebDialog Demo",
                CanUserResize = true,
                Source = new Uri("https://avaloniaui.net/"),
            };

            dialog.NavigationCompleted += async (s, e) =>
            {
                if (!e.IsSuccess)
                {
                    return;
                }

                var title = await dialog.InvokeScript("document.title");
                Debug.WriteLine($"NativeWebDialog loaded, document.title = {title}");
            };

            dialog.Closing += (s, e) => Debug.WriteLine("NativeWebDialog closing");

            var owner = AvaloniaHelper.GetMainWindow();
            if (owner is not null)
            {
                dialog.Show(owner);
            }
            else
            {
                dialog.Show();
            }

            dialog.Resize(1000, 700);
        }

        #endregion
    }
}
```

- [ ] **Step 8: 创建 `Views/MainWindow.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="using:Avalonia.WebViewDemo.ViewModels"
        mc:Ignorable="d"
        d:DesignWidth="600"
        d:DesignHeight="400"
        x:Class="Avalonia.WebViewDemo.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia WebView Demo"
        Width="600"
        Height="400"
        WindowStartupLocation="CenterScreen">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <StackPanel Margin="40"
                Spacing="16"
                VerticalAlignment="Center">
        <TextBlock FontSize="20"
                   FontWeight="Bold"
                   Text="Avalonia 12 WebView" />
        <TextBlock Opacity="0.7"
                   TextWrapping="Wrap"
                   Text="WebView2 on Windows, WKWebView on macOS, WPE/WebKitGTK on Linux." />

        <Button HorizontalAlignment="Stretch"
                Command="{Binding ShowWebViewCommand}"
                Content="1. NativeWebView (embedded in a Window)" />
        <Button HorizontalAlignment="Stretch"
                Command="{Binding ShowWebDialogCommand}"
                Content="2. NativeWebDialog (standalone native window)" />
        <Button HorizontalAlignment="Stretch"
                Command="{Binding CallJSMethodCommand}"
                Content="3. Call JS from C# (open #1 first)" />
    </StackPanel>
</Window>
```

- [ ] **Step 9: 创建 `Views/MainWindow.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.WebViewDemo.Views
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

- [ ] **Step 10: 加入解决方案并构建**

```bash
cd "$DST"
dotnet sln hello-avalonia.slnx add Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj
dotnet build Avalonia.WebViewDemo/Avalonia.WebViewDemo.csproj -v q --nologo
```
Expected: 0 个错误。

- [ ] **Step 11: 运行验证**

```powershell
$b = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.WebViewDemo\bin\Debug\net10.0"
Start-Process "$b\Avalonia.WebViewDemo.exe" -WorkingDirectory $b -RedirectStandardError "$env:TEMP\wv_err.txt"
Start-Sleep 10
Get-Content "$env:TEMP\wv_err.txt"
```
Expected: stderr 为空。三项人工确认：
1. 点按钮 1 → 弹窗加载 `TestWeb/index.html`，加载动画消失后显示网页
2. 点按钮 2 → 打开原生窗口加载 avaloniaui.net
3. 按钮 1 的窗口打开时点按钮 3 → 调试输出出现 `JS returned: 25`

- [ ] **Step 12: 提交**

```bash
cd "$DST"
git add Avalonia.WebViewDemo hello-avalonia.slnx
git commit -m "feat: add Avalonia.WebViewDemo

Split out of MusicStore. Covers the three WebView surfaces in Avalonia 12:
NativeWebView embedded in a Window, NativeWebDialog as a standalone native
window, and the JSON-envelope bridge for JS <-> C# calls in both directions.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Avalonia.HtmlRendererDemo

演示 `HtmlPanel` 富文本渲染与 IconFont 字体图标。

**Files:**
- Create: `$DST/Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj`
- Create: `App.axaml`、`App.axaml.cs`、`Program.cs`、`app.manifest`
- Create: `ViewModels/MainWindowViewModel.cs`
- Create: `Views/MainWindow.axaml(.cs)`
- Copy from `$SRC`: `Assets/iconfont.ttf`、`Assets/avalonia-logo.ico`、`Resources/Icons.axaml`
- Modify: `$DST/hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 2 的 `ViewModelBase`
- Produces: 无（终端项目）

`System.Xaml` 引用必须保留——`Avalonia.HtmlRenderer` 依赖它，否则构建报 MSB3245。

- [ ] **Step 1: 创建 csproj**

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
        <None Remove="Assets\iconfont.ttf" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="Avalonia.Fonts.Inter" />
        <PackageReference Condition="'$(Configuration)' == 'Debug'" Include="Avalonia.Diagnostics" />
        <PackageReference Include="Avalonia.HtmlRenderer" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>

    <ItemGroup>
        <Reference Include="System.Xaml" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 拷贝资源**

```powershell
$SRC = "E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\Avalonia.MusicStore"
$DST = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.HtmlRendererDemo"
New-Item -ItemType Directory -Force "$DST\ViewModels","$DST\Views","$DST\Assets","$DST\Resources" | Out-Null
Copy-Item "$SRC\app.manifest" $DST
Copy-Item "$SRC\Assets\avalonia-logo.ico","$SRC\Assets\iconfont.ttf" "$DST\Assets"
Copy-Item "$SRC\Resources\Icons.axaml" "$DST\Resources"
```

- [ ] **Step 3: 创建 `App.axaml`**

`IconFont` 资源与 `Icons.axaml` 从原 MusicStore 的 App.axaml 搬到这里，avares URI 改为本项目程序集名。

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.HtmlRendererDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Avalonia.HtmlRendererDemo/Resources/Icons.axaml" />
    </Application.Styles>

    <Application.Resources>
        <FontFamily x:Key="IconFont">avares://Avalonia.HtmlRendererDemo/Assets/#iconfont</FontFamily>
    </Application.Resources>
</Application>
```

- [ ] **Step 4: 创建 `App.axaml.cs`**

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.HtmlRendererDemo.ViewModels;
using Avalonia.HtmlRendererDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.HtmlRendererDemo
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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

- [ ] **Step 5: 创建 `Program.cs`**

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.HtmlRendererDemo
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

- [ ] **Step 6: 创建 `ViewModels/MainWindowViewModel.cs`**

`Text` 内容沿用原 MusicStore 主窗的富文本示例。

```csharp
using Avalonia.Shared.ViewModels;

namespace Avalonia.HtmlRendererDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Text { get; }

        public MainWindowViewModel()
        {
            Text = " <h1>欢迎来到我的网页</h1>\r\n    <p>这是一个段落。你可以在这里添加内容。</p>\r\n    <p>学习HTML是创建网页的第一步。</p>\r\n";
        }
    }
}
```

- [ ] **Step 7: 创建 `Views/MainWindow.axaml`**

左侧 HtmlPanel 富文本，右侧两种图标用法（IconFont 字符 + Icons.axaml 里的 PathIcon）。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:html="using:TheArtOfDev.HtmlRenderer.Avalonia"
        xmlns:vm="using:Avalonia.HtmlRendererDemo.ViewModels"
        mc:Ignorable="d"
        d:DesignWidth="800"
        d:DesignHeight="450"
        x:Class="Avalonia.HtmlRendererDemo.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia HtmlRenderer &amp; IconFont Demo"
        Width="800"
        Height="450"
        WindowStartupLocation="CenterScreen">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <Grid Margin="20" ColumnDefinitions="*,auto">
        <Border Grid.Column="0"
                Padding="12"
                BorderBrush="Gray"
                BorderThickness="1">
            <StackPanel Spacing="8">
                <TextBlock FontWeight="Bold" Text="HtmlPanel (rich text)" />
                <html:HtmlPanel Text="{Binding Text}" />
            </StackPanel>
        </Border>

        <StackPanel Grid.Column="1"
                    Margin="20,0,0,0"
                    Spacing="16">
            <TextBlock FontWeight="Bold" Text="IconFont" />
            <TextBlock FontFamily="{StaticResource IconFont}"
                       FontSize="32"
                       Foreground="OrangeRed"
                       Text="&#xe71a;" />
            <TextBlock FontWeight="Bold" Text="PathIcon" />
            <PathIcon Width="32"
                      Height="32"
                      Data="{StaticResource store_microsoft_regular}" />
        </StackPanel>
    </Grid>
</Window>
```

`store_microsoft_regular` 是 `Resources/Icons.axaml` 中已有的键（该文件另有一个
`music_regular` 键，本项目不用）。

- [ ] **Step 8: 创建 `Views/MainWindow.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.HtmlRendererDemo.Views
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

- [ ] **Step 9: 加入解决方案并构建**

```bash
cd "$DST"
dotnet sln hello-avalonia.slnx add Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj
dotnet build Avalonia.HtmlRendererDemo/Avalonia.HtmlRendererDemo.csproj -v q --nologo
```
Expected: 0 个错误。允许 MSB3245（System.Xaml）警告——它是 HtmlRenderer 的已知情况。

- [ ] **Step 10: 运行验证**

```powershell
$b = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.HtmlRendererDemo\bin\Debug\net10.0"
Start-Process "$b\Avalonia.HtmlRendererDemo.exe" -WorkingDirectory $b -RedirectStandardError "$env:TEMP\hr_err.txt"
Start-Sleep 8
Get-Content "$env:TEMP\hr_err.txt"
```
Expected: stderr 为空；截图可见左侧 HTML 标题与段落按富文本样式渲染，右侧橙红色字体图标与 PathIcon 均可见。若图标显示为方框，说明 `AvaloniaResource` 未包含 ttf。

- [ ] **Step 11: 提交**

```bash
cd "$DST"
git add Avalonia.HtmlRendererDemo hello-avalonia.slnx
git commit -m "feat: add Avalonia.HtmlRendererDemo

Split out of MusicStore: HtmlPanel rich-text rendering plus the two icon
approaches (icon font glyph and PathIcon from a resource dictionary).

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Avalonia.DataTemplateDemo

演示 `IDataTemplate` 模版选择器、Flyout 与 ControlTheme 样式系统。

**Files:**
- Create: `$DST/Avalonia.DataTemplateDemo/Avalonia.DataTemplateDemo.csproj`
- Create: `App.axaml`、`App.axaml.cs`、`Program.cs`、`app.manifest`
- Create: `Models/Person.cs`
- Create: `DataTemplates/PersonDataTemplateSelector.cs`
- Create: `ViewModels/MainWindowViewModel.cs`
- Create: `Views/MainWindow.axaml(.cs)`
- Copy from `$SRC`: `Resources/ButtonStyles.axaml`、`Assets/avalonia-logo.ico`
- Modify: `$DST/hello-avalonia.slnx`

**Interfaces:**
- Consumes: Task 2 的 `ViewModelBase`
- Produces: 无（终端项目）

- [ ] **Step 1: 创建 csproj**

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
        <PackageReference Condition="'$(Configuration)' == 'Debug'" Include="Avalonia.Diagnostics" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: 拷贝资源**

```powershell
$SRC = "E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\Avalonia.MusicStore"
$DST = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.DataTemplateDemo"
New-Item -ItemType Directory -Force "$DST\Models","$DST\DataTemplates","$DST\ViewModels","$DST\Views","$DST\Assets","$DST\Resources" | Out-Null
Copy-Item "$SRC\app.manifest" $DST
Copy-Item "$SRC\Assets\avalonia-logo.ico" "$DST\Assets"
Copy-Item "$SRC\Resources\ButtonStyles.axaml" "$DST\Resources"
```

- [ ] **Step 3: 创建 `Models/Person.cs`**

删掉原文件未使用的 using。

```csharp
namespace Avalonia.DataTemplateDemo.Models
{
    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Sex Sex { get; set; }
    }

    public enum Sex
    {
        Male,
        Female
    }
}
```

- [ ] **Step 4: 创建 `DataTemplates/PersonDataTemplateSelector.cs`**

原样搬运，仅改命名空间。

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.DataTemplateDemo.Models;

namespace Avalonia.DataTemplateDemo.DataTemplates
{
    public class PersonDataTemplateSelector : IDataTemplate
    {
        public IDataTemplate MaleDataTemplate { get; set; }
        public IDataTemplate FemaleDataTemplate { get; set; }

        public Control? Build(object? param)
        {
            if (param is Person person)
            {
                if (person.Sex == Sex.Male)
                {
                    return MaleDataTemplate.Build(param);
                }
                else
                {
                    return FemaleDataTemplate.Build(param);
                }
            };

            return new Control();
        }

        public bool Match(object? data)
        {
            return data is Person;
        }
    }
}
```

- [ ] **Step 5: 创建 `App.axaml`**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Avalonia.DataTemplateDemo.App"
             RequestedThemeVariant="Dark">

    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

- [ ] **Step 6: 创建 `App.axaml.cs`**

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.DataTemplateDemo.ViewModels;
using Avalonia.DataTemplateDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.DataTemplateDemo
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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
```

- [ ] **Step 7: 创建 `Program.cs`**

```csharp
using Avalonia;
using Avalonia.Logging;
using System;

namespace Avalonia.DataTemplateDemo
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

- [ ] **Step 8: 创建 `ViewModels/MainWindowViewModel.cs`**

```csharp
using Avalonia.DataTemplateDemo.Models;
using Avalonia.Shared.ViewModels;
using System.Collections.ObjectModel;

namespace Avalonia.DataTemplateDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<Person> People { get; } = new();

        public MainWindowViewModel()
        {
            People.Add(new Person() { Id = "10", Name = "anyu", Address = "Beijing", Sex = Sex.Male });
            People.Add(new Person() { Id = "20", Name = "lff", Address = "Beijing", Sex = Sex.Female });
        }
    }
}
```

- [ ] **Step 9: 创建 `Views/MainWindow.axaml`**

三块演示：模版选择器（男红女黄）、Flyout、ControlTheme 自定义按钮。均从原 MusicStore
主窗搬来。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:datatemplates="using:Avalonia.DataTemplateDemo.DataTemplates"
        xmlns:model="using:Avalonia.DataTemplateDemo.Models"
        xmlns:vm="using:Avalonia.DataTemplateDemo.ViewModels"
        mc:Ignorable="d"
        d:DesignWidth="800"
        d:DesignHeight="450"
        x:Class="Avalonia.DataTemplateDemo.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Avalonia DataTemplate &amp; Style Demo"
        Width="800"
        Height="450"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
        <Flyout x:Key="SharedFlyout"
                ShowMode="Standard"
                Placement="BottomEdgeAlignedLeft">
            <TextBlock Text="这是弹出层" />
        </Flyout>

        <!--  ControlTheme类似于WPF中的Style  -->
        <ControlTheme x:Key="ButtonStyle" TargetType="Button">
            <Setter Property="Width" Value="200" />
            <Setter Property="Height" Value="40" />
            <Setter Property="Background" Value="Yellow" />
            <Setter Property="BorderBrush" Value="Blue" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="4">
                            <Border.Styles>
                                <Style Selector="Border:pointerover">
                                    <Setter Property="Background" Value="Blue" />
                                    <Setter Property="TextBlock.Foreground" Value="Black" />
                                </Style>
                            </Border.Styles>
                            <ContentPresenter HorizontalAlignment="Center"
                                              VerticalAlignment="Center"
                                              Content="{TemplateBinding Content}" />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </ControlTheme>
    </Window.Resources>

    <Window.Styles>
        <StyleInclude Source="avares://Avalonia.DataTemplateDemo/Resources/ButtonStyles.axaml" />
        <Style Selector="FlyoutPresenter">
            <Setter Property="Background" Value="Red" />
            <Setter Property="Margin" Value="0,5,0,0" />

            <!--  ^是嵌套样式选择器  -->
            <Style Selector="^:pointerover">
                <Setter Property="Background" Value="Blue" />
            </Style>
        </Style>
    </Window.Styles>

    <Window.DataTemplates>
        <datatemplates:PersonDataTemplateSelector>
            <datatemplates:PersonDataTemplateSelector.MaleDataTemplate>
                <DataTemplate x:DataType="{x:Type model:Person}">
                    <StackPanel Orientation="Horizontal" Background="Red">
                        <TextBlock Text="{Binding Id}" FontSize="40" />
                        <StackPanel>
                            <TextBlock Text="{Binding Name}" FontSize="20" />
                            <TextBlock Text="{Binding Address}" FontSize="20" />
                        </StackPanel>
                    </StackPanel>
                </DataTemplate>
            </datatemplates:PersonDataTemplateSelector.MaleDataTemplate>
            <datatemplates:PersonDataTemplateSelector.FemaleDataTemplate>
                <DataTemplate x:DataType="{x:Type model:Person}">
                    <StackPanel Orientation="Horizontal" Background="Yellow">
                        <TextBlock Text="{Binding Id}" FontSize="40" />
                        <StackPanel>
                            <TextBlock Text="{Binding Name}" FontSize="20" />
                            <TextBlock Text="{Binding Address}" FontSize="20" />
                        </StackPanel>
                    </StackPanel>
                </DataTemplate>
            </datatemplates:PersonDataTemplateSelector.FemaleDataTemplate>
        </datatemplates:PersonDataTemplateSelector>
    </Window.DataTemplates>

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <StackPanel Margin="30" Spacing="24">
        <TextBlock FontWeight="Bold" Text="1. IDataTemplate selector (male = red, female = yellow)" />
        <ListBox ItemsSource="{Binding People}">
            <ListBox.Styles>
                <Style Selector="ListBoxItem">
                    <Setter Property="Height" Value="80" />
                </Style>
            </ListBox.Styles>
            <ListBox.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal" />
                </ItemsPanelTemplate>
            </ListBox.ItemsPanel>
        </ListBox>

        <TextBlock FontWeight="Bold" Text="2. Flyout + styled FlyoutPresenter" />
        <Button HorizontalAlignment="Left"
                Content="Show Flyout"
                Flyout="{StaticResource SharedFlyout}" />

        <TextBlock FontWeight="Bold" Text="3. ControlTheme (custom template)" />
        <Button HorizontalAlignment="Left"
                Theme="{StaticResource ButtonStyle}"
                Content="Themed Button" />
    </StackPanel>
</Window>
```

- [ ] **Step 10: 创建 `Views/MainWindow.axaml.cs`**

```csharp
using Avalonia.Controls;

namespace Avalonia.DataTemplateDemo.Views
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

- [ ] **Step 11: 加入解决方案并构建**

```bash
cd "$DST"
dotnet sln hello-avalonia.slnx add Avalonia.DataTemplateDemo/Avalonia.DataTemplateDemo.csproj
dotnet build Avalonia.DataTemplateDemo/Avalonia.DataTemplateDemo.csproj -v q --nologo
```
Expected: 0 个错误。（`ButtonStyles.axaml` 只含 `Button.Call` / `Button.Icon` 两个
Selector 样式，无外部资源依赖，可直接搬用。）

- [ ] **Step 12: 运行验证**

```powershell
$b = "E:\ProjectxPlex\WPFCodePlex\hello-avalonia\Avalonia.DataTemplateDemo\bin\Debug\net10.0"
Start-Process "$b\Avalonia.DataTemplateDemo.exe" -WorkingDirectory $b -RedirectStandardError "$env:TEMP\dt_err.txt"
Start-Sleep 8
Get-Content "$env:TEMP\dt_err.txt"
```
Expected: stderr 为空；截图可见 anyu 条目红底、lff 条目黄底；点 Show Flyout 弹出红色浮层；Themed Button 为黄底蓝边、悬停变蓝。

- [ ] **Step 13: 提交**

```bash
cd "$DST"
git add Avalonia.DataTemplateDemo hello-avalonia.slnx
git commit -m "feat: add Avalonia.DataTemplateDemo

Split out of MusicStore: IDataTemplate selector picking a template per item
type, plus the Flyout and ControlTheme styling samples.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: 仓库 README

**Files:**
- Modify: `$DST/README.md`

**Interfaces:**
- Consumes: Task 3-6 的项目名与演示主题
- Produces: 无

- [ ] **Step 1: 全量构建验证**

先确认整个解决方案可一次性构建，再写 README。

Run: `dotnet build "$DST/hello-avalonia.slnx" -v q --nologo`
Expected: 0 个错误，5 个项目全部生成。

- [ ] **Step 2: 改写 README.md**

```markdown
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
```

- [ ] **Step 3: 提交**

```bash
cd "$DST"
git add README.md
git commit -m "docs: describe the demo projects in README

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: 删除 hello-dotnet 中的源目录

仅在 Task 3-7 全部验证通过后执行。

**Files:**
- Delete: `E:\ProjectxPlex\WPFCodePlex\hello-dotnet\AvaloniaSamples\`（整个目录）

**Interfaces:**
- Consumes: Task 3-6 的验证结论（四个演示均已在新仓库跑通）
- Produces: 无

- [ ] **Step 1: 确认新仓库四个演示都已验证通过**

复查 Task 3/4/5/6 的 Step 11~13 是否都已勾选。任一未通过则停止，不要删除源目录。

- [ ] **Step 2: 确认无其他项目引用 AvaloniaSamples**

```powershell
Select-String -Path "E:\ProjectxPlex\WPFCodePlex\hello-dotnet\**\*.csproj","E:\ProjectxPlex\WPFCodePlex\hello-dotnet\**\*.slnx" -Pattern "AvaloniaSamples|Avalonia\.MusicStore" -ErrorAction SilentlyContinue
```
Expected: 无输出。若有命中，先处理该引用再继续。

- [ ] **Step 3: 删除并提交**

```bash
cd E:/ProjectxPlex/WPFCodePlex/hello-dotnet
git rm -r AvaloniaSamples
git commit -m "chore: remove AvaloniaSamples, moved to hello-avalonia

Avalonia.MusicStore now lives in the hello-avalonia repository, split into
four focused demo projects plus a shared library.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 4: 确认源仓库状态干净**

Run: `git -C E:/ProjectxPlex/WPFCodePlex/hello-dotnet status --short`
Expected: 无 `AvaloniaSamples` 相关条目（bin/obj 残留目录若存在可手动删除，它们本就不在版本控制内）。
