# .NET 桌面项目性能基准规范（BenchmarkDotNet）

> 适用范围：任意 .NET 桌面项目（Avalonia / WPF 同理）。文中的 `Avalonia.*Demo`、`src/`、`benchmarks/` 是**本仓库的举例**，规则本身不依赖它们。
>
> 与测试规范（`2026-10-18-testing-common-standards.md`、`2026-10-18-avalonia-testing-standards.md`、`2026-10-18-wpf-testing-standards.md`）并列：那几份管「对不对」，本文管「快不快」。

---

## 0. 一页结论

| 问题 | 结论 |
| --- | --- |
| 测什么？ | 我们自己写的、纯的热路径：转换器、格式化、过滤排序、VM 属性更新。**不测**网络、磁盘、要真实窗口的代码，也不测第三方库 |
| 放哪？ | 独立控制台项目 `benchmarks/<App>.Benchmarks/`，只引 `BenchmarkDotNet` 一个包；`dotnet test` 不会运行它 |
| 怎么跑？ | `dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*类名*'` |
| 结果怎么用？ | 只和同一台机器上的上一次结果比；CI 上默认只告警不阻断，噪声 ±10~20% |

---

## 1. 快速上手：给现有项目加一个基准

本仓库已有 `benchmarks/Avalonia.Benchmarks`。从零建项目见附录 B。

**① 建文件。** 照被测代码所在的目录建 `<被测类型>Benchmarks.cs`，比如被测的是 `Converters/FooConverter`，就建 `Converters/FooConverterBenchmarks.cs`。从这个模板开始：

```csharp
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;

namespace Avalonia.Benchmarks.Converters;

[MemoryDiagnoser]                        // 报告里加上 Allocated 列
[JsonExporterAttribute.FullCompressed]   // CI 要用的 *-report-full-compressed.json
public class FooConverterBenchmarks
{
    // 准备工作放字段或 [GlobalSetup]，不要写进基准方法
    private readonly FooConverter _converter = new();

    [Benchmark]
    public object? Convert() => _converter.Convert(12.5, typeof(string), null, CultureInfo.InvariantCulture);
    //     ^^^^^^^ 返回结果，防止 JIT 把调用当死代码删掉
}
```

**② 冒烟。** 先用 dry job 确认能跑通，大约十几秒。dry 跑出来的数字**不能看**：

```bash
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --job dry --filter '*FooConverter*'
```

**③ 正式跑。** 先关掉其他程序、插上电源，再跑：

```bash
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*FooConverter*'
```

**④ 看结果。** 控制台末尾会打印汇总表，同时写到 `BenchmarkDotNet.Artifacts/results/`（在**当前目录**下，不在项目目录里）。

| 列 | 看什么 |
| --- | --- |
| `Mean` | 平均耗时，主要比较对象 |
| `Error` | 99.9% 置信区间的半宽。**大于 `Mean` 的 5% 就别信这次结果**，关掉后台程序重跑 |
| `Ratio` | 相对 `Baseline` 的倍数，只在标了 `Baseline = true` 时出现 |
| `Allocated` | 每次调用分配的托管内存。基本不受机器噪声影响，比 `Mean` 更适合拿来卡回归 |
| `Gen0/1/2` | **每 1000 次调用**触发的 GC 次数；热路径上出现 Gen2 基本说明设计有问题 |

只想跑某一个方法时，把 filter 写全：`--filter '*FooConverterBenchmarks.Convert*'`。

> filter 一律加单引号：在 bash 里，不加引号的 `*Foo*` 会先被当成文件通配符展开。

---

## 2. 测什么

### 2.1 四问筛子

一个方法四关都过了才值得写基准：

| # | 问题 | 不过关的例子 |
| --- | --- | --- |
| 1 | **在热路径上吗？** 每次按键、每次列表刷新、每次绑定更新都会跑 | 只在启动时跑一次 |
| 2 | **是纯的吗？** 输入确定，不碰网络和磁盘，不需要 UI 线程 | `File.OpenRead`、`HttpClient.GetAsync`、`Dispatcher.UIThread` |
| 3 | **是我们的代码吗？** 它下次被改，改动会是我们自己的提交 | `ObservableObject`、`[ObservableProperty]` 生成的 setter、LINQ、`string.Format` |
| 4 | **想回答什么问题？** 比如「新实现快了多少」「这里会不会分配」 | 没有疑问、没有基线，只是为了跑而跑 |

**第 2 关不过的一律不测**，哪怕它就在热路径上：网络和磁盘的抖动比代码本身的差异大得多，测出来的是机房，不是代码。

### 2.2 私有方法：从公开入口测

1. **首选：测触发它的公开属性或命令。** 测量里会混进 `ObservableCollection` 刷新之类的副作用，但这本来就是运行时要付的成本。
2. **退路：改成 `internal`，再加 `InternalsVisibleTo`。** 只在私有方法本身是热点、而公开入口的无关开销又太大时才这么做。
3. **禁止：把逻辑复制一份到基准项目。** 复制品迟早和生产代码分叉，最后测的是一个不存在的实现。

### 2.3 数据规模固定怎么办

数据源如果是硬编码的（比如 `Contact.Samples()` 固定返回 6 条），就只能测单次调用的开销，测不出随规模增长的趋势。想测规模，得先把数据改成可注入的。这是**对生产代码的改动**，要先用单元测试兜住正确性，不能为了基准顺手改签名。

### 2.4 本仓库的清单

路径都相对 `src/`，基准类在 `benchmarks/Avalonia.Benchmarks/` 的同名目录下。

| 被测成员 | 为什么值得测 | 基准类 · 场景（`[Params]`） |
| --- | --- | --- |
| `Avalonia.Shared/Converters/DoubleToThicknessConverter.Convert` | 每次绑定刷新都会调用；`UnsetValue` 分支容易被人改成抛异常 | `DoubleToThicknessConverterBenchmarks` · `Input` = double / string |
| `Avalonia.Shared/Converters/NumberConverters.DoubleToInt` | 拖动滑块时每个 tick 都调用，测的是 `FuncValueConverter` 委托包装本身的开销 | `NumberConvertersBenchmarks` |
| `Avalonia.DataBindingDemo/Converters/CelsiusToFahrenheitConverter` | `Convert` 每次 `ToString("F1")` 都会分配字符串；`ConvertBack` 在逐字输入时跑 `TryParse`，结果还要装箱 | `CelsiusToFahrenheitConverterBenchmarks` |
| `Avalonia.DataBindingDemo/Converters/RgbToBrushConverter.Convert` | 每次调用都 `new SolidColorBrush`，对分配敏感 | `RgbToBrushConverterBenchmarks` · `Channels` = 3 / 2 |
| `Avalonia.Shared/Helpers/EventLog.Write` | 事件密集的页面上高频调用；写满 60 条以后，每次写入都要 `RemoveAt(0)`，这一步是 O(n) | `EventLogBenchmarks` · `Prefill` = 0 / 59 / 60 |
| `Avalonia.DataBindingDemo/ViewModels/CollectionViewsViewModel.Filter` | 私有 `Rebuild()` 的公开入口，每次按键都要 `Where` + `OrderBy` 再重填列表（`GroupBy` 只在按城市分组时才跑） | `CollectionViewsViewModelBenchmarks` · `Term` = 全部 / 一条 / 零条 |
| `Avalonia.LayoutDemo/ViewModels/ResponsiveViewModel.UpdateLayout` | 每次窗口 resize 都调用；setter 有相等检查，宽度不变和跨断点的代价差一个量级 | `ResponsiveViewModelBenchmarks` · `CrossesBreakpoint` = false / true |

**明确不测：**

- `MusicStore` 的 `Album.SearchAsync`、`LoadCoverBitmapAsync`、`SaveAsync`、`LoadCachedAsync`：网络或磁盘，第 2 关不过；
- `AlbumViewModel.LoadCover`：`Bitmap.DecodeToWidth` 要先初始化图形栈，第 2 关不过；
- `Contact.Samples`：固定 6 条，也没有想回答的问题，第 4 关不过；
- 各个 `*Page.axaml.cs` 里的事件处理器：要真实控件树，而且多半是琐碎操作。

要测控件的 `Measure`/`Arrange`/`Render`，见附录 C。

---

## 3. 怎么写

### 3.1 规则

1. **测量环境：Release、命令行启动、不挂调试器、机器要安静。** Debug 版本可能慢 10 到 100 倍，官方原话是「Never use the Debug build for benchmarking. Never.」[1]。测出的结论只在当前运行时、架构、OS、JIT 下成立，不要外推到别的机器；写进 README 时要注明环境。
2. **返回结果。** 结果不被使用的话，JIT 可能把整段调用当死代码删掉，测到的是空 [1]。有返回值就直接返回；没有的话就返回一个能反映结果的值，比如 `Rows.Count`。
3. **守卫只确认路径，不负责业务。** 可以加一句很便宜的 `if (...) throw`，确认确实走到了想测的分支，比如 `ResponsiveViewModelBenchmarks` 确认跨过了断点。但业务正确性归 xUnit 管，守卫也不能把想测的输入挡在门外。
4. **一个 `[Benchmark]` 只调用被测成员一次，准备工作放外面。** 输入数组、转换器实例放字段或 `[GlobalSetup]`，比如 `RgbToBrushConverterBenchmarks` 在 Setup 里就把 `_values` 建好了。
5. **命名对齐生产代码。** 类名用 `<被测类型>Benchmarks`；方法名和被测成员同名：属性用属性名（`Filter`），构造函数用 `Constructor`。**同一成员的多个场景用 `[Params]` 表达，不要给方法名加后缀**，这样报告里的场景是单独一列，方法名始终能和源码对上。
6. **状态怎么重置，按优先级选：**
   - 不改状态：用 `[GlobalSetup]` 建一次；
   - 会改状态但对象便宜：每次调用都新建对象，再用一个只做构造的基准当 `Baseline` 把构造成本扣掉（示例二）；
   - 前两种都不行，才用 `[IterationSetup]`。它会让每轮迭代只调用一次（`UnrollFactor=1`），微秒以下的操作会被计时误差淹没。`EventLogBenchmarks` 就属于这种情况：它的 `Prefill` 状态没法在调用之间复原，所以它的 `Mean` 只能看个量级，结论以 `Allocated` 为准。
7. **`Baseline = true` 只标在「同一件事的两种做法」上**，比如新旧实现，或者「构造」对比「构造加过滤」。两个不相关的方法之间算 `Ratio` 没有意义。
8. **`[Params]` 覆盖边界，不要重复同一类输入。** 比如过滤词要覆盖全部命中、命中一条、零条；两个都只命中一条的词，测出来是同一个场景。

### 3.2 示例一：无状态转换器（最简形态）

```csharp
[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class CelsiusToFahrenheitConverterBenchmarks
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly CelsiusToFahrenheitConverter _converter = new();

    // Runs on every slider tick; ToString("F1") allocates a new string each time.
    [Benchmark]
    public object? Convert() => _converter.Convert(21.5, typeof(string), null, Culture);

    // Runs on every keystroke: double.TryParse, then the result is boxed into object.
    [Benchmark]
    public object? ConvertBack() => _converter.ConvertBack("70.7", typeof(double), null, Culture);
}
```

这里没有标 `Baseline`：`Convert` 和 `ConvertBack` 是两件不同的事，按规则 7 不互相比较。

### 3.3 示例二：有状态 VM，用 Baseline 扣掉构造成本

```csharp
[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class CollectionViewsViewModelBenchmarks
{
    // Rebuild() filters on Name: "" keeps all six rows, "张" keeps one, "周" keeps none.
    [Params("", "张", "周")]
    public string Term = "";

    // The constructor already runs one full Rebuild(); Ratio on Filter is the extra cost of filtering.
    [Benchmark(Baseline = true)]
    public int Constructor() => new CollectionViewsViewModel().Rows.Count;

    // Setting Filter runs the private Rebuild(): Where + OrderBy + ObservableCollection refill.
    // A fresh VM per call keeps every call on the same path without [IterationSetup].
    // "" equals the initial value, so the setter skips Rebuild() and Ratio stays near 1.
    [Benchmark]
    public int Filter() => new CollectionViewsViewModel { Filter = Term }.Rows.Count;
}
```

这个示例用到了以下几条规则：

- `Filter` 是私有 `Rebuild()` 的公开入口（2.2）；
- 返回 `Rows.Count`，不用 `throw`，所以零命中的 `"周"` 也能测（规则 2、3）；
- 每次调用都新建 VM，不用 `[IterationSetup]`（规则 6）；
- `Filter` 的 `Ratio` 减 1，就是过滤本身相对一次构造的额外代价（规则 7）。

---

## 4. 比较结果与接入 CI

### 4.1 本地对比：改完到底快没快

不要拿两次单独的 `Mean` 相减就下结论。用 dotnet/performance 仓库里的 `ResultsComparer`（`src/tools/ResultsComparer`）[3]，它做的是统计检验：

```bash
# 1. 改动前后各跑一次，fulljson 会生成 ResultsComparer 要读的 *-report-full.json
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*FooConverter*' --exporters fulljson --artifacts BenchmarkDotNet.Artifacts/before
#    ……改代码……
dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*FooConverter*' --exporters fulljson --artifacts BenchmarkDotNet.Artifacts/after

# 2. 在 dotnet/performance 的 src/tools/ResultsComparer 目录下运行
dotnet run -- --base <仓库>/BenchmarkDotNet.Artifacts/before --diff <仓库>/BenchmarkDotNet.Artifacts/after --threshold 5%
```

`--threshold` 是噪声线：差异小于它的一律当作噪声。两份结果都放在 `BenchmarkDotNet.Artifacts/` 下，这个目录已经在 `.gitignore` 里。

### 4.2 CI 接入（github-action-benchmark）

```yaml
- name: Run benchmarks
  shell: bash
  run: |
    dotnet run -c Release --project benchmarks/Avalonia.Benchmarks -- --filter '*' --join --artifacts bench-out
    mv bench-out/results/*-report-full-compressed.json bench-out/benchmarks.json

- name: Store benchmark result
  uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: bench-out/benchmarks.json
    alert-threshold: '150%'
    fail-on-alert: false
    comment-on-alert: true
    github-token: ${{ secrets.GITHUB_TOKEN }}
```

每一行都有原因：

| 写法 | 为什么 |
| --- | --- |
| `--filter '*'` | 不带 filter 时，`BenchmarkSwitcher` 会弹出交互菜单等人输入，CI 会一直卡住 |
| `--join` | 不加的话每个类各出一个 json；而 `output-file-path` 只接受**单个文件** [2]。加了以后合并成一个 |
| `--artifacts bench-out` + `mv` | 产物默认写在**当前目录**，合并后的文件名还带时间戳（`BenchmarkRun-joined-<时间>-report-full-compressed.json`），所以先固定目录，再改成固定的文件名 |
| `tool: 'benchmarkdotnet'` | 必填，取值固定 [2] |
| `alert-threshold` / `fail-on-alert` | 默认值分别是 **200%** 和 **false**，不改的话永远不会失败 [2] |

另外两条：

- 历史数据存在 `gh-pages` 分支的 `dev/bench/data.js` 里 [2]；
- **不要**在 `pull_request` 事件上运行并推送 gh-pages，否则外部 PR 能改你的 pages 分支 [2]。

### 4.3 门禁策略

| 场景 | 阈值 | 行为 |
| --- | --- | --- |
| 托管 runner（噪声 ±10~20% [2]） | 150–200% | 只告警，不失败 |
| 自托管的固定机器 | 110–120% | `fail-on-alert: true` |
| 发布前对比基线 | `ResultsComparer`（4.1） | 人工确认 |

阈值定得比噪声还小，结果就是随机变红。先让基准稳定下来，再谈收紧阈值。

在整条流水线里的位置：

```
build → 单元/headless 测试（每次提交，阻断）→ Appium（可选，门控）→ benchmarks（独立阶段，默认只告警）
```

先卡住「对不对」，再谈「快不快」。只有仓库明确把某个基准定义成性能契约时，才允许它阻断合并。

---

## 附录 A：为什么基准不进 `dotnet test`

基准也是性能测试，但它和功能测试的判定方式根本不同。功能测试断言「相等、抛出、包含」，结果是确定的；基准给出的是「相对基线的比值」，换一台机器、换一种负载就能差 10% 到 20% [2]，而且只对当时的运行时和架构成立 [1]。再加上 BenchmarkDotNet 项目本身是控制台 `Exe`，测试运行器根本不会把它当成测试项目。所以基准单独作为一个阶段运行，默认只告警（见 4.3）。

---

## 附录 B：从零建基准项目

**目录：** `benchmarks/` 和 `src/`、`tests/` 平级，可以一个应用一个项目，也可以整个解决方案共用一个（本仓库是共用一个）。

```
benchmarks/Avalonia.Benchmarks/
  Avalonia.Benchmarks.csproj
  Program.cs
  Converters/   Helpers/   ViewModels/    ← 照被测代码的目录分区
```

**csproj：**

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

- **不要硬写 `<Optimize>true</Optimize>`。** `Directory.Build.props` 已经按配置设好了：Debug 为 false，Release 为 true。硬写成 true 会让 `-c Debug` 也开优化，「必须用 Release」这条规则就被悄悄绕过了。
- 可以 `ProjectReference` 一个 `WinExe` 演示应用：只是引用它的程序集，不会启动窗口（已实测，见附录 D）。

**包：**

| 包 | 是否需要 | 用途 |
| --- | --- | --- |
| `BenchmarkDotNet` | **必需** | 框架本体，`[MemoryDiagnoser]`、JSON 导出器、`[Params]` 都在里面 |
| `BenchmarkDotNet.Diagnostics.Windows` | 可选，仅 Windows | ETW 诊断器（如 `[EtwProfiler]`），定位 CPU 热点时用 |
| `Avalonia.Headless` + `Avalonia.Skia` | 可选 | 只有做控件级基准时才需要（附录 C） |
| `xunit.*` / `Microsoft.NET.Test.Sdk` | **不要引** | 引了以后 `dotnet test` 会把它当测试项目收进去 |

版本只写在 `Directory.Packages.props` 里，csproj 不带 `Version`：

```xml
<PackageVersion Include="BenchmarkDotNet" Version="0.15.8" />
```

**Program.cs：**

```csharp
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

**登记到解决方案：** 加进 `hello-avalonia.slnx` 的 `<Folder Name="/benchmarks/">`。这样 `dotnet build hello-avalonia.slnx` 会编译它，保证它不会烂到编译不过；`dotnet test` 则不会运行它。

---

## 附录 C：控件级基准（可选）

测自定义控件的 `Measure`/`Arrange`/`Render`，要先在基准进程里初始化 Avalonia：

```csharp
[GlobalSetup]
public void Init()
{
    AppBuilder.Configure<Application>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .SetupWithoutStarting();

    // Then build the control tree, ApplyTemplate(), Measure/Arrange.
}
```

这类基准牵涉 Skia 绘制，噪声更大。要单独放一个项目或一个类，不要和 2.4 的纯函数基准放在一起比较。

---

## 附录 D：证据与置信度

### D.1 一手来源

| # | 来源 | 支撑的规则 |
| --- | --- | --- |
| [1] | https://benchmarkdotnet.org/articles/guides/good-practices.html | Release、不挂调试器、防死代码消除、结论绑定环境、安静机器 |
| [2] | https://github.com/benchmark-action/github-action-benchmark （README 与 `examples/benchmarkdotnet/README.md`） | `tool` 取值、`output-file-path` 为单个文件、产物默认在当前目录、默认 200% / false、gh-pages、±10~20% 噪声、PR 安全提示 |
| [3] | https://github.com/dotnet/performance/blob/main/src/tools/ResultsComparer/README.md | `--base` / `--diff` / `--threshold`，只读 `*full.json` |

### D.2 本仓库实测（2026-10-18，Windows 10、.NET 10.0.12、BenchmarkDotNet 0.15.8）

- 从仓库根执行 `--job dry --filter '*' --join --artifacts <目录>`：7 个类共 18 个基准全部跑通，守卫没有抛异常；`results/` 下只生成**一个** `BenchmarkRun-joined-<时间戳>-report-full-compressed.json`。
- `--exporters fulljson` 会在类上已有的 full-compressed 之外，**再**生成一份 `-report-full.json`。
- `dotnet test` 不会执行这个基准项目：对它的 csproj 跑 `dotnet test -c Release --no-build`，退出码 0，没有任何输出。
- 基准项目 `ProjectReference` 了 `Avalonia.DataBindingDemo`（`WinExe`），编译和 dry 运行都正常，没有启动窗口。

### D.3 未核实（用到时自己验证）

- github-action-benchmark 能否正确解析 `--join` 生成的合并 JSON：本地只确认了文件能生成，没有在 Action 里跑过。
- `comment-on-alert` 需要配 `github-token`：凭印象写的，没有对照 README 原文核对。
- `Error < Mean 的 5%`、阈值 150–200% / 110–120% 这几个数字：都是经验值，官方只给出了噪声范围和默认值。
- 附录 C 的 headless 初始化写法：参照的是 `tests/Avalonia.Tests.Common/HeadlessApp.cs`，没有在基准进程里实测过。

### D.4 版本

`BenchmarkDotNet` 稳定版是 **0.15.8**（2026-10-18 查自 NuGet；`0.16.0-preview.2` 是预览版，不要用）。以 `Directory.Packages.props` 为准，升级前先用 `dotnet package search BenchmarkDotNet` 复核。
