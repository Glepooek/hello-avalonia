# .NET 桌面项目性能基准规范（BenchmarkDotNet）

> 适用范围：任意 .NET 桌面项目（Avalonia / WPF 同理）。
> 文中出现的 `Avalonia.*Demo`、`src/`、`tests/` 只是**本仓库的举例**，规则本身不依赖它们。
>
> 与测试规范（`2026-10-18-avalonia-wpf-testing-standards.md`）并列：那份负责「对不对」，本文负责「快不快」。

---

## 0. 一页结论

| 问题 | 结论 |
| --- | --- |
| 基准算不算测试？ | **算性能测试**，但不算功能回归测试。xUnit 断言确定性结果，基准断言相对基线的比值 |
| 基准哪类代码？ | 纯函数式的热路径：转换器、格式化、排序过滤、VM 构造、序列化。**不测**网络、磁盘、需要真实窗口的代码 |
| 项目放哪？ | 独立的 `benchmarks/<App>.Benchmarks/` 控制台项目，引用被测项目；`dotnet test` 不会执行它 |
| 要引哪些包？ | 只有 **`BenchmarkDotNet`** 一个必需（`[MemoryDiagnoser]`、JSON 导出器都在里面）。测控件才加 `Avalonia.Headless` + `Avalonia.Skia` |
| 怎么跑？ | `dotnet run -c Release --project benchmarks/<App>.Benchmarks -- --filter *你的类名*` |
| 结果可信吗？ | 只对「当前机器 + 运行时 + 架构」成立；托管 CI 噪声 ±10~20%，因此默认只告警不阻断 |

---

## 1. 定位：为什么基准不在测试套件里

三个层面上的结论不同，混为一谈会得出错误的门禁策略：

| 层面 | 结论 | 说明 |
| --- | --- | --- |
| 测试分类 | **是**测试——性能测试（非功能测试）的一种 | 把性能与功能并列是通行分类 |
| 分层模型（L0–L3 之类的功能测试分层） | **不占一层**，是并列的独立执行阶段 | 分层解决「在哪里复现」，基准解决「快不快」 |
| 合并门禁 | 默认**不阻断** | 测量非确定性 + 工具默认阈值不失败 [2] |

不能进 `dotnet test` 的具体原因：

1. **结果非确定性**：同一提交换机器、换负载可差 10–20% [2]；
2. **结果环境绑定**：只对「该运行时 + 该架构 + 该 OS + 该 JIT」成立，不能外推 [1]；
3. **工具形态不同**：BenchmarkDotNet 项目是控制台 `Exe`，测试运行器不会把它当测试项目；
4. **判定语义不同**：测试断言「相等/抛出/包含」，基准断言「相对基线的比值」。

断言**业务正确性**永远属于 xUnit。基准里可以 `if` 校验结果，但那是防 JIT 死代码消除的手段，不是替代单测——结果不被使用，整段代码可能被整个优化掉。

---

## 2. 该基准哪些文件与哪些方法

这是本文最需要具体回答的部分。分四步走，最后给出本仓库的候选清单。

### 2.1 四问筛子

一个方法要同时过四关才值得基准化：

| # | 问题 | 不过关的例子 |
| --- | --- | --- |
| 1 | **它在热路径上吗？** 每次输入、每次列表刷新、每个绑定更新都要跑 | 只在启动时跑一次的代码 |
| 2 | **它是纯的吗？** 输入确定、不碰网络/磁盘、不需要 UI 线程 | `File.OpenRead`、`HttpClient.GetAsync`、`Dispatcher.UIThread` |
| 3 | **它会随数据规模增长吗？** 或已经有人抱怨它慢 | 固定 3 个元素的常量运算 |
| 4 | **你想回答什么？** 「这个实现比上一个快多少」或「这么写会不会分配」 | 没有基线、没有疑问时为跑而跑 |

**不过第 2 关的一律不测**，哪怕它在热路径上。网络和磁盘的抖动远大于代码本身的差异，测出来的是机房不是代码。

### 2.2 只测应用代码，不测第三方库

`ViewModelBase`、`ObservableObject`、`[ObservableProperty]` 生成的 setter 属于 CommunityToolkit.Mvvm。基准化它们得到的是「库的作者写得怎么样」，不是「我们的代码有没有退化」。同理不要基准化 `string.Format`、LINQ 本身。

判断法：**这个方法被改的那一天，改的是我们的提交吗？** 不是就跳过。

### 2.3 私有方法怎么测

三种做法，优先级从上到下：

1. **从公开入口进入**（首选）。私有方法由公开属性/命令触发时，测那个公开入口——那才是应用真实付出的代价。缺点是测量里混入了 `ObservableCollection` 更新等副作用，但这恰恰是运行时真实成本。
2. **改成 `internal` + `InternalsVisibleTo`**。只在该私有方法本身是被反复调用的热点、而公开入口有大量无关开销时才做。
3. **复制一份逻辑到基准项目**——**禁止**。复制的那份迟早与生产代码分叉，测的是一个不存在的实现。

### 2.4 数据规模测不了怎么办

如果数据源是硬编码常量（`Contact.Samples()` 之类），你只能在那个规模上测「单次调用开销」，测不出「随规模增长」。

要测规模就必须让数据可注入——那是**生产代码的设计改动**，不是基准的改动。两个选择：接受现状只测固定规模；或者先做一次可注入性重构（用测试兜住正确性），再回来测规模。不要为了基准去改生产签名而不写测试。

### 2.5 本仓库的候选清单（举例）

按「值得测程度」排序。路径均相对 `src/`。

| 被测方法 | 为什么值得测 | 预期能回答什么 |
| --- | --- | --- |
| `Avalonia.Shared/Converters/DoubleToThicknessConverter.Convert` | 绑定每次刷新都会调用；`AvaloniaProperty.UnsetValue` 分支容易被改成抛异常 | 单次转换的耗时与分配 |
| `Avalonia.Shared/Converters/NumberConverters.DoubleToInt`（`FuncValueConverter`） | 滑块拖动时每个 tick 都调用 | 委托包装本身的开销 |
| `Avalonia.DataBindingDemo/Converters/CelsiusToFahrenheitConverter.Convert` / `ConvertBack` | `ConvertBack` 里做 `double.TryParse` + 格式化，是输入法逐字输入时的热路径 | 字符串生成的分配 |
| `Avalonia.DataBindingDemo/Converters/RgbToBrushConverter.Convert` | 每次调用 `new SolidColorBrush(...)`，分配敏感 | 每次多分配多少字节（`[MemoryDiagnoser]`） |
| `Avalonia.Shared/Helpers/EventLog.Write` | 事件密集页面上高频调用；内部有字符串插值与 `While` 裁剪 | 60 条上限附近的写入门槛 |
| `Avalonia.DataBindingDemo/ViewModels/CollectionViewsViewModel` 的 `Filter` 属性 setter | 私有 `Rebuild()` 的公开入口：`Where` + `OrderBy` + `GroupBy` 三次遍历 | 过滤器每次按键重建列表的代价（私有方法从公开 setter 测，见 2.3） |
| `Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.UpdateLayout` | 每次窗口 resize 都调用；`[ObservableProperty]` 的相等检查使「同宽度」与「跨断点」代价差一个量级，必须两种都测 | 跨断点时三次属性通知的开销，以及同宽度调用的下限 |
| `Avalonia.DataBindingDemo/Models/Contact.Samples` | 固定 6 条，只能测固定规模 | 仅作基线，别指望规模结论 |

**明确不测**（全部卡在第 2 关）：

- `Avalonia.MusicStore/Models/Album.SearchAsync`、`LoadCoverBitmapAsync`——网络/磁盘；
- `Album.SaveAsync`、`LoadCachedAsync`——文件 IO；
- `AlbumViewModel.LoadCover`——`Bitmap.DecodeToWidth` 需要图形栈初始化；
- 任何 `*Page.axaml.cs` 的事件处理器——需要真实控件树，且多半是琐碎操作。

控件相关的基准（`Measure`/`Arrange`/`Render`）见 3.6：需要 headless 初始化，值得单独立项，不要和纯函数基准混在一个项目里。

---

## 3. 基准项目长什么样

### 3.1 目录结构

```
benchmarks/<App>.Benchmarks/            ← 一个应用一个（也可整个解决方案共用一个）
  <App>.Benchmarks.csproj
  Program.cs                            ← 入口：BenchmarkSwitcher
  Converters/                           ← 按被测代码的目录分区
    DoubleToThicknessConverterBenchmarks.cs
  ViewModels/
    CollectionViewsFilterBenchmarks.cs
```

约定：

- 一个基准类测**一个类型**，类名 `<被测类型>Benchmarks`；
- 一个 `[Benchmark]` 方法对应**一个方法的一次调用**，方法名与生产方法同名；
- `benchmarks/` 与 `src/`、`tests/` 平级，不改它们的结构。

### 3.2 csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>   <!-- 必须与被测项目一致 -->
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="BenchmarkDotNet" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Avalonia.DataBindingDemo\Avalonia.DataBindingDemo.csproj" />
        <ProjectReference Include="..\..\src\Avalonia.Shared\Avalonia.Shared.csproj" />
    </ItemGroup>
</Project>
```

两个容易踩的点：

- **不要在这里硬写 `<Optimize>true</Optimize>`**。本仓库的 `Directory.Build.props` 已按配置设置：Debug 为 `false`、Release 为 `true`。硬写 true 会让 `-c Debug` 也变成优化编译，于是「必须用 Release」这条规则被悄悄绕过，数字看着能用实际不可比。
- 基准项目可以 `ProjectReference` 一个 `WinExe`（如演示应用），编译与运行都正常；它只是引用了 UI 程序集，不会启动窗口。

### 3.3 要引哪些包

| 包 | 是否必需 | 用途 |
| --- | --- | --- |
| `BenchmarkDotNet` | **必需** | 框架本体。`[MemoryDiagnoser]`、`[JsonExporterAttribute.FullCompressed]`、`[Params]` 全在包内，无需额外引用 |
| `BenchmarkDotNet.Diagnostics.Windows` | 可选（仅 Windows） | ETW 诊断器（`[EtwProfiler]` 等），定位 CPU 热点时用；跨平台项目不要引 |
| `Avalonia.Headless` + `Avalonia.Skia` | 可选 | 只在基准化控件/布局时需要（见 3.6） |
| `xunit.*` / `Microsoft.NET.Test.Sdk` | **不要引** | 基准项目不是测试项目，引了会让 `dotnet test` 误收，且引入无用的测试宿主 |

**版本只写在 `Directory.Packages.props`**（本仓库已启用中央包管理），csproj 的 `PackageReference` 不写 `Version`。截至 2026-10-18，NuGet 上最新稳定版是 `0.15.8`（另有 `0.16.0-preview.2` 预览版，不要用）：

```xml
<PackageVersion Include="BenchmarkDotNet" Version="0.15.8" />
```

### 3.4 Program.cs

```csharp
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

配合 `--filter` 可以只跑某一类或某个方法，开发时不必等全套跑完。

### 3.5 两个完整示例

**示例一：无状态转换器（最简形态）**

```csharp
using System.Globalization;
using Avalonia.DataBindingDemo.Converters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]   // CI 要用 -report-full-compressed.json
public class CelsiusConverterBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private readonly CelsiusToFahrenheitConverter _converter = new();

    [Benchmark(Baseline = true)]
    public object? Convert() => _converter.Convert(21.5, typeof(string), null, Culture);

    // ConvertBack 里有 double.TryParse + 字符串格式化，分配主要发生在这里。
    [Benchmark]
    public object? ConvertBack() => _converter.ConvertBack("70.7", typeof(double), null, Culture);
}
```

**示例二：有状态 VM + 参数化 + 非纯副作用**

```csharp
using Avalonia.DataBindingDemo.ViewModels;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.ViewModels;

[MemoryDiagnoser]
public class CollectionViewsFilterBenchmarks
{
    private CollectionViewsViewModel _vm = null!;

    // 覆盖「空过滤（全量）/ 命中一条」两类输入。Rebuild() 按 Name 过滤，
    // 所以这里给的是姓名片段，不是城市。
    [Params("", "张", "孙")]
    public string Term = "";

    [IterationSetup]
    public void NewViewModel() => _vm = new CollectionViewsViewModel();

    // 设置 Filter 会触发私有 Rebuild()：Where + OrderBy + GroupBy。
    // 这里同时计入 ObservableCollection 的 Clear/Add——那正是真实成本。
    [Benchmark]
    public void ApplyFilter()
    {
        _vm.Filter = Term;

        // 防 JIT 死代码消除，也顺带守住「结果非空」这条业务语义。
        if (_vm.Rows.Count == 0) throw new InvalidOperationException("filter dropped every row");
    }
}
```

注意示例二用 `[IterationSetup]` 而不是 `[GlobalSetup]`：`Filter` 是状态，跑第二次时列表已建好，必须每轮重建 VM 才能测到同一条路径。

### 3.6 控件级基准（可选）

要测自定义控件的 `Measure`/`Arrange`/`Render`，必须在基准进程里初始化 Avalonia。加到 `[GlobalSetup]`：

```csharp
[GlobalSetup]
public void Init()
{
    AppBuilder.Configure<Application>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .SetupWithoutStarting();

    // 再构造控件树、ApplyTemplate()、Measure/Arrange。
}
```

这类基准噪声更大（涉及 Skia 绘制），单独放一个项目或单独一个类，不要与第 2.5 节的纯函数基准混跑比较。

### 3.7 注册到解决方案与忽略产物

```xml
<Folder Name="/benchmarks/">
    <Project Path="benchmarks/Avalonia.Benchmarks/Avalonia.Benchmarks.csproj" />
</Folder>
```

`dotnet build hello-avalonia.slnx` 会编译它（保证不退化为无法编译），`dotnet test` 不会执行它。`BenchmarkDotNet.Artifacts/` 已在本仓库 `.gitignore` 中。

---

## 4. 写法规则

1. **只在 Release 下跑**。官方原话「Never use the Debug build for benchmarking. Never.」，Debug 版本可能慢 10–100 倍，数字无意义 [1]。
2. **不在调试器下运行**。从命令行 `dotnet run -c Release` 启动 [1]。
3. **必须使用或返回计算结果**，否则 JIT 会消除整段代码，测到的是空 [1]。返回 `object?` 也可以；在方法里 `if (...) throw` 校验业务结果同样有效。
4. **一个 `[Benchmark]` 只做一件事**。把「准备 → 调用 → 断言」都塞进去，测到的是三段之和，改了一处就说不清是哪段变了。
5. **把参数与循环移到基准方法外**。`Convert()` 每次 `new byte[]` 之类的准备动作会被计入耗时，用字段或 `[GlobalSetup]` 提前建好。
6. **状态要能重置**。`[GlobalSetup]` 每轮跑一次，`[IterationSetup]` 每次迭代跑一次。会改变内部状态的方法（示例二的 `Filter`）用后者。
7. **`[Params]` 至少覆盖空/小/大**。单一输入规模的结论往往反直觉。
8. **`Baseline = true` 标在「上一个实现」上**，让报告直接给出比例，不用心算。
9. **环境绑定**：结果只对当前运行时 + 架构 + OS + JIT 成立，不要外推到其他机器 [1]；写进 README 时必须注明环境。
10. **安静机器**：关闭其他应用、插电、最高性能模式 [1]。
11. **断言业务正确性 ∈ xUnit；断言相对性能 ∈ 基准门禁**。两者不要互相替代。

---

## 5. 怎么跑、怎么看结果

### 5.1 常用命令

```bash
# 只跑某个类（开发期）
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter *Celsius*

# 只跑某个方法
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter *CelsiusConverterBenchmarks.ConvertBack*

# 冒烟：用 dry job 快速确认代码能跑通（数字无效，不要读）
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --job dry --filter *Celsius*

# 导出 CI 用的 JSON
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --exporters json
```

### 5.2 读报告时看什么

| 列 | 含义 | 怎么用 |
| --- | --- | --- |
| `Mean` | 平均耗时 | 主要比较对象 |
| `Error` / `StdDev` | 置信区间与标准差 | 标准差占比高（>10%）说明机器不安静，结论不可信 |
| `Ratio` | 相对 `Baseline` 的倍数 | 回答「新实现快了多少」 |
| `Allocated` | 每次调用分配字节数（`[MemoryDiagnoser]`） | 只需知道「有没有分配」，不要拿它对性能做门禁 |
| `Gen0/1/2` | GC 次数 | 热路径上出现 Gen2 基本等于设计问题 |

**判断稳定的经验值**：`Error` 小于 `Mean` 的 5% 才算一次可信运行；否则先关掉后台程序重跑。

### 5.3 本地对比两份结果

改完实现要回答「真的快了还是噪声」时，不要用单次 `Mean` 的差值下结论。官方推荐用 `ResultsComparer`：它随 BenchmarkDotNet 文档提供（示例项目在官方仓库里，不是内置命令），思路是把两份 `BenchmarkDotNet.Artifacts/results` 目录交给它，并显式给一个噪声阈值（例如 5%），小于阈值的差异按噪声处理。

---

## 6. CI 接入（github-action-benchmark）

```yaml
- name: Run benchmarks
  run: dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --exporters json

- name: Store benchmark result
  uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: benchmarks/Avalonia.Benchmarks/BenchmarkDotNet.Artifacts/results/*-report-full-compressed.json
    alert-threshold: '120%'
    fail-on-alert: false
    comment-on-alert: true
```

关键事实（来自官方 README [2]）：

- `tool: 'benchmarkdotnet'` 必填且取值固定；
- 输入文件是 `-report-full-compressed.json`：需要基准类上启用 `[JsonExporterAttribute.FullCompressed]`，或用命令行 `--exporters json`；默认 CSV 输出不能用；
- 默认 `alert-threshold` 是 **200%**、`fail-on-alert` 默认 **false**——要做门禁必须显式收紧，否则永远不会失败；
- 历史数据存于 `gh-pages` 分支的 `dev/bench/data.js`；
- 托管 runner 噪声约 **±10~20%**，网络/磁盘基准更差；
- **不要**在 `pull_request` 事件上运行并推送 gh-pages：外部 PR 可以改你的 pages 分支。

### 6.1 门禁策略

| 场景 | 阈值 | 行为 |
| --- | --- | --- |
| 托管 CI、机器不稳定 | 150–200% | 只告警、不失败 |
| 自托管固定机器 | 110–120% | `fail-on-alert: true` |
| 发布前基线对比 | 手动 `ResultsComparer` | 人工确认 |

先让基准稳定，再谈严格阈值。阈值小于噪声等于随机红。

---

## 7. 与测试套件的整体顺序

```
build → 单元/headless 测试（每次提交，阻断）→ Appium（可选，门控）→ benchmarks（独立阶段，告警或门禁）
```

排序即优先级：先把「对不对」卡住，再谈「快不快」。基准失败默认只产出趋势和告警；只有仓库明确把它定义为性能契约时才允许阻断。

---

## 8. 证据与置信度

### 8.1 一手来源

| # | 来源 | 支撑的规则 | 置信度 |
| --- | --- | --- | --- |
| [1] | https://benchmarkdotnet.org/articles/guides/good-practices.html | Release、无调试器、防死代码消除、环境绑定、安静机器 | 高（官方原文） |
| [2] | https://github.com/benchmark-action/github-action-benchmark | CLI 参数、JSON 导出、默认阈值 200%、fail-on-alert 默认 false、gh-pages、±10~20% 噪声、PR 安全提示 | 高（官方 README） |

### 8.2 未充分核实的部分（使用时需自行验证）

- **「基准是性能测试的一种」**：通行测试分类的表述，不来自上述来源；只影响第 1 节的措辞，不影响任何操作规则。
- **`Error/Mean < 5%`、阈值数字（150–200% / 110–120%）**：经验值，不是官方结论；官方只给出噪声范围与默认值。
- **控件级基准的 headless 初始化**：写法参照本仓库 `tests/Avalonia.Tests.Common/HeadlessApp.cs`，未在基准进程中实测。

已实测的两条（2026-10-18，本仓库 `benchmarks/Avalonia.Benchmarks`）：

- **`dotnet test` 不会执行控制台基准项目**：对基准 csproj 直接 `dotnet test -c Release --no-build` 退出码 0、无任何输出，没有启动 BenchmarkDotNet。
- **`ProjectReference` 一个 `WinExe` 演示应用可行**：基准项目引用 `Avalonia.DataBindingDemo`（`WinExe`）编译与 dry 运行均正常，未启动窗口。

### 8.3 版本与依赖

- `BenchmarkDotNet` 稳定版 **0.15.8**（2026-10-18 查自 NuGet）；`0.16.0-preview.2` 为预览版。
- 版本以 `Directory.Packages.props` 为准；引入前用 `dotnet package search BenchmarkDotNet` 复核。

### 8.4 与本仓库的对照

| 本文规则 | 仓库现状 | 行动 |
| --- | --- | --- |
| 独立 `benchmarks/` 项目 | 已有 `benchmarks/Avalonia.Benchmarks`（7 个基准类，覆盖 2.5 节候选清单全部条目） | 保持；新增应用按第 3 节扩展 |
| `PackageVersion` 登记在中央包管理 | `Directory.Packages.props` 已有 `BenchmarkDotNet` 0.15.8 | 保持 |
| `BenchmarkDotNet.Artifacts/` 不入库 | `.gitignore` 已含 | 保持 |
| 基准不进 `dotnet test` 门禁 | 已实测：`dotnet test` 对基准 csproj 退出 0 且无输出 | 接入 CI 时按第 5–7 节 |
