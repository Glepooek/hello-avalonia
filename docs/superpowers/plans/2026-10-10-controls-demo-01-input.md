# ControlsDemo 01：Input 其余控件与 4 篇实战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Avalonia.ControlsDemo` 里补全 Input 分类：Date and time 4 页、Selectors 7 页、Text input 3 页（共 14 个 Control 页），加 TextBox、ComboBox、Slider、DatePicker 4 篇 How-to 实战页；同时引入 `Avalonia.Controls.ColorPicker` 包并启用它的样式。

**Architecture:** 沿用第 00 份的壳与 `PageCatalog`。本份只新增页面文件、`PageCatalog.Input.cs` 的登记行、`App.axaml` 的一条 `StyleInclude`、csproj 的一个包引用，并新增探针 `Probe.Input.cs`。`DataGrid` 包不在本份引入（第 03 份）。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Fluent 主题、CommunityToolkit.Mvvm 8.4.2（How-to 页用 `ObservableValidator`）、`Avalonia.Controls.ColorPicker` 12.1.2

**Spec:** `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`
**前置：** 第 00 份已执行（`PageCatalog`、`Categories`、`Harness`、`NumberConverters` 已存在）。

## Global Constraints

沿用第 00 份「Global Constraints」「命名与结构约定」「硬性规则 1–14」「探针写法」，**不重复贴**，执行者必须先读第 00 份。本份额外强调：

- 页面类名 `<控件>Page`，How-to 页 `<主题>HowToPage`；命名空间一律 `Avalonia.ControlsDemo.Views.Pages`，ViewModel 一律 `Avalonia.ControlsDemo.ViewModels`
- 页面 XAML 省略 `d:` / `mc:` 设计时属性（第 00 份骨架里有，可有可无，不影响运行）
- `DocPath` 已用官方 `sitemap.xml` 核对：控件页 `controls/input/<子类>/<控件>`，How-to 页 `docs/how-to/<主题>-how-to`
- **事件接线放在构造函数里、`InitializeComponent()` 之后**：XAML 里写的 `IsChecked="True"`、`SelectedIndex="0"` 会在加载期触发变更事件，而此时后声明的元素还不存在（第 00 份 RadioButton 的教训，规则 12 的同类）。需要响应用户操作的事件一律在 code-behind 里订阅
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`

### 本份新增的已核实事实（编写计划时在 Avalonia 12.1.2 headless 实测）

| 控件 | 实测结果 | 对页面写法的影响 |
|---|---|---|
| `NumericUpDown` | `Minimum=0 Maximum=10` 时，**代码赋值 `Value = 99` 不被夹取**，`ClipValueToMinMax=true` 也一样（99、-5 原样保留）；但**微调遵守上限**：`Value=9`、`Increment=3`、`Maximum=10` 时 `Increase` 得 10，再 `Decrease` 得 7 | 页面里做一个"代码写入 99"按钮，把这个静默行为摆出来 |
| `Slider` | `IsSnapToTickEnabled=true` 时代码赋值 `Value=3.3` **不吸附**（读回 3.3）；`TickFrequency=2`、`SmallChange=1` 时键盘右方向键从 0 得 **2**（步长取刻度而非 SmallChange） | 页面说明：吸附只作用于用户操作 |
| `Calendar` | 把 `SelectedDate` 设为黑名单日期**抛 `ArgumentOutOfRangeException`**（消息 `SelectedDate value is not valid. (Parameter 'e')`）；**抛出之后 `SelectedDate` 仍保留被拒绝的值，`SelectedDates` 却是空的**，两者不一致 | 页面做一个按钮，捕获并显示这个异常；说明文字提醒"别依赖 SelectedDate 判断有没有选中" |
| `TextBox` 撤销 | 直接给 `Text` 赋值**不进撤销栈**（`CanUndo=False`）；键入才进 | 撤销演示的说明要写明"请在框里键入后再点撤销" |
| `AutoCompleteBox` | 用 `KeyTextInput` 输入 `"rr"`（`Contains` 模式，列表含 `Cherry`）后 `IsDropDownOpen=true`；**直接给 `Text` 属性赋值不会展开下拉**；下拉的 `ListBox` 不在页面可视树里 | 探针用键入驱动，断言 `IsDropDownOpen` |
| `MaskedTextBox` | `Mask="(000) 000-0000"`、`PromptChar='_'`，键入 `5551234567` 得 `Text='(555) 123-4567'`、`MaskCompleted=true`、`MaskFull=true` | 可直接断言 |
| `ColorPicker` | `Color=Red` → `HsvColor` 为 H0 S1 V1；改成 `Lime` → H120 | 摆色块靠 `ColorChanged` 事件，不靠绑定 |
| `TextBox` | `TextChanging` 的事件参数 `TextChangingEventArgs` **没有 `Cancel`**（官方 How-to 里的 `e.Cancel = true` 在 12.1.2 编译不过）；在处理器里**改写 `Text`** 可以过滤（`"12ab3"` → `"123"`） | How-to 页的"仅数字"用改写 `Text` 的写法 |

**成员核实（反射，Avalonia 12.1.2）**：`CalendarDatePicker` 有 `SelectedDateFormat`（`Long`/`Short`/`Custom`）、`CustomDateFormatString`、`PlaceholderText`、`DisplayDateStart/End`、`BlackoutDates`、事件 `SelectedDateChanged`、`DateValidationError`；`DatePicker` 有 `DayFormat`/`MonthFormat`/`YearFormat`、`DayVisible`/`MonthVisible`/`YearVisible`、`MinYear`/`MaxYear`（`DateTimeOffset`）；`TimePicker` 有 `ClockIdentifier`（字符串 `"12HourClock"`/`"24HourClock"`）、`MinuteIncrement`、`UseSeconds`、`SelectedTime`（`TimeSpan?`）；`ColorView` 有 `Color`、`ColorModel`（`Hsva`/`Rgba`）、`ColorSpectrumShape`（`Box`/`Ring`）、`IsAlphaEnabled`、`IsAlphaVisible`，`ColorPicker` 继承 `ColorView`。

---

## File Structure（本份创建 / 修改）

```
Avalonia.ControlsDemo/
├── Avalonia.ControlsDemo.csproj          改：+ColorPicker 包引用
├── App.axaml                             改：+ColorPicker 的 StyleInclude
├── Navigation/PageCatalog.Input.cs       改：+14 控件页 +4 How-to 页，按官方顺序重排
├── ViewModels/
│   ├── TextBoxHowToViewModel.cs          新
│   └── DatePickerHowToViewModel.cs       新
└── Views/Pages/Input/                    新增 18 个页面（各含 .axaml 与 .axaml.cs）
    Calendar / CalendarDatePicker / DatePicker / TimePicker
    CheckBox / ToggleSwitch / Slider / NumericUpDown / ComboBox / ColorPicker / ColorView
    AutoCompleteBox / MaskedTextBox / TextBox
    TextBoxHowTo / ComboBoxHowTo / SliderHowTo / DatePickerHowTo
```

探针（仓库外）：`C:\Temp\probe-controls\Probe.Input.cs`。

---

### Task 1: 引入 ColorPicker 包并启用样式

**Files:**
- Modify: `Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj`
- Modify: `Avalonia.ControlsDemo/App.axaml`

**Interfaces:**
- Consumes: 第 00 份 Task 1 已在 `Directory.Packages.props` 声明 `Avalonia.Controls.ColorPicker` 12.1.2
- Produces: `ColorPicker`、`ColorView` 在本项目里能渲染

**为什么必须两处都改**：只加包引用，`ColorPicker` / `ColorView` 构造成功、能布局、但**什么都不画，且不报错不记日志**（第 00 份 Task 7 已实测：不加样式时后代数为 0）。所以包引用和 `StyleInclude` 是一个原子改动，探针里要断言后代数大于零。

- [ ] **Step 1: csproj 加包引用**

在 `Avalonia.ControlsDemo/Avalonia.ControlsDemo.csproj` 的 `CommunityToolkit.Mvvm` 那行之后加一行：

```xml
        <PackageReference Include="Avalonia.Controls.ColorPicker" />
```

- [ ] **Step 2: App.axaml 加样式**

`App.axaml` 的 `Application.Styles` 里，`SharedStyles` 那行之后加（路径多一层 `Fluent/`，少了会抛 `XamlLoadException`）：

```xml
        <!--  ColorPicker and ColorView draw nothing without this; there is no error and no log.  -->
        <StyleInclude Source="avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml" />
```

- [ ] **Step 3: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|个错误|Build succeeded"`
Expected: 0 个错误。

- [ ] **Step 4: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the ColorPicker package and its theme to ControlsDemo

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### 本份页面的写法约定

- 下文每个代码块前的 `#### \`路径\`` 标题就是目标文件路径（相对仓库根），整块内容即文件全文。
- 页面骨架（`UserControl` + `ScrollViewer` + `StackPanel Margin="12"` + `DemoHeader`）与第 00 份相同；演示块用 `TextBlock.caption` + `Border.stage` + `TextBlock.hint`。
- 需要枚举下拉的地方一律在 code-behind 构造函数里 `ItemsSource = Enum.GetValues<T>()`，先设 `SelectedItem` 再订阅 `SelectionChanged`（避免加载期误触发）。

---

### Task 2: Date and time（Calendar、CalendarDatePicker、DatePicker、TimePicker）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/CalendarPage.axaml`、`.axaml.cs`
- Create: `.../CalendarDatePickerPage.axaml`、`.axaml.cs`
- Create: `.../DatePickerPage.axaml`、`.axaml.cs`
- Create: `.../TimePickerPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: 第 00 份 `DemoHeader`、`NumberConverters.DoubleToInt`
- Produces（探针依赖的元素名）：
  - `CalendarPage`：`Cal`（`Calendar`）、`ModeBox`、`DisplayModeBox`、`FirstDayBox`（`ComboBox`）、`SelectionText`、`BlackoutCal`（`Calendar`）、`BlackoutButton`（`Button`）、`BlackoutResult`
  - `CalendarDatePickerPage`：`Picker`、`FormatBox`（`ComboBox`）、`CustomFormat`（`TextBox`）、`PickerResult`
  - `DatePickerPage`：`Picker`、`DayCheck`/`MonthCheck`/`YearCheck`（`CheckBox`）、`MonthFormatBox`（`ComboBox`）、`PickerResult`
  - `TimePickerPage`：`Picker`、`ClockBox`（`ComboBox`）、`StepSlider`、`SecondsCheck`、`PickerResult`

#### `Avalonia.ControlsDemo/Views/Pages/Input/CalendarPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CalendarPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Calendar：月历"
                               DocPath="controls/input/date-and-time/calendar" />

            <TextBlock Classes="caption" Text="1. SelectionMode：单日、范围、多范围、不可选" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="24">
                    <Calendar Name="Cal" />
                    <StackPanel Spacing="8" VerticalAlignment="Top">
                        <ComboBox Name="ModeBox" Width="180" />
                        <TextBlock Name="SelectionText" TextWrapping="Wrap" MaxWidth="260" Text="已选 0 天" />
                    </StackPanel>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. DisplayMode 与 FirstDayOfWeek" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBlock VerticalAlignment="Center" Text="DisplayMode" />
                    <ComboBox Name="DisplayModeBox" Width="120" />
                    <TextBlock VerticalAlignment="Center" Text="FirstDayOfWeek" />
                    <ComboBox Name="FirstDayBox" Width="140" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="DisplayMode 切换后，上面第一个日历随之变成月、年或十年视图。" />

            <TextBlock Classes="caption" Text="3. BlackoutDates：黑名单日期不可选，代码强行选中会抛异常" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="24">
                    <Calendar Name="BlackoutCal" />
                    <StackPanel Spacing="8" VerticalAlignment="Top">
                        <Button Name="BlackoutButton" Content="用代码选中 1 月 6 日（黑名单）" />
                        <TextBlock Name="BlackoutResult" TextWrapping="Wrap" MaxWidth="260" Text="尚未尝试" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="抛出异常后 SelectedDate 仍保留被拒绝的值，SelectedDates 却是空的，所以不要用 SelectedDate 判断「有没有选中」。DisplayDateStart / DisplayDateEnd 可限制可浏览范围。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/CalendarPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CalendarPage : UserControl
    {
        public CalendarPage()
        {
            InitializeComponent();

            ModeBox.ItemsSource = Enum.GetValues<CalendarSelectionMode>();
            ModeBox.SelectedItem = Cal.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is CalendarSelectionMode mode)
                {
                    Cal.SelectionMode = mode;
                }
            };
            Cal.SelectedDatesChanged += (_, _) => SelectionText.Text = $"已选 {Cal.SelectedDates.Count} 天";

            DisplayModeBox.ItemsSource = Enum.GetValues<CalendarMode>();
            DisplayModeBox.SelectedItem = Cal.DisplayMode;
            DisplayModeBox.SelectionChanged += (_, _) =>
            {
                if (DisplayModeBox.SelectedItem is CalendarMode mode)
                {
                    Cal.DisplayMode = mode;
                }
            };

            FirstDayBox.ItemsSource = Enum.GetValues<DayOfWeek>();
            FirstDayBox.SelectedItem = Cal.FirstDayOfWeek;
            FirstDayBox.SelectionChanged += (_, _) =>
            {
                if (FirstDayBox.SelectedItem is DayOfWeek day)
                {
                    Cal.FirstDayOfWeek = day;
                    BlackoutCal.FirstDayOfWeek = day;
                }
            };

            BlackoutCal.DisplayDate = new DateTime(2026, 1, 1);
            BlackoutCal.BlackoutDates.Add(new CalendarDateRange(new DateTime(2026, 1, 5), new DateTime(2026, 1, 9)));
            BlackoutButton.Click += (_, _) =>
            {
                // Selecting a blacked-out date from code throws; only the user's clicks are silently refused.
                try
                {
                    BlackoutCal.SelectedDate = new DateTime(2026, 1, 6);
                    BlackoutResult.Text = "没有异常";
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    BlackoutResult.Text = $"抛出 {ex.GetType().Name}：{ex.Message}";
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/CalendarDatePickerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CalendarDatePickerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="CalendarDatePicker：带下拉月历的日期框"
                               DocPath="controls/input/date-and-time/calendardatepicker" />

            <TextBlock Classes="caption" Text="1. SelectedDateFormat：长、短、自定义" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CalendarDatePicker Name="Picker" Width="220" PlaceholderText="选择日期" />
                        <TextBlock Name="PickerResult" VerticalAlignment="Center" Text="未选择" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <ComboBox Name="FormatBox" Width="120" />
                        <TextBox Name="CustomFormat" Width="160" Text="yyyy年M月d日" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="自定义格式串只在 SelectedDateFormat 为 Custom 时生效；格式串写错不会抛异常，日期框显示原样文本。" />

            <TextBlock Classes="caption" Text="2. DisplayDateStart / DisplayDateEnd：限制可选范围" />
            <Border Classes="stage" Padding="12">
                <CalendarDatePicker Name="RangedPicker" Width="220" PlaceholderText="仅 2026 年可选" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/CalendarDatePickerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CalendarDatePickerPage : UserControl
    {
        public CalendarDatePickerPage()
        {
            InitializeComponent();

            FormatBox.ItemsSource = Enum.GetValues<CalendarDatePickerFormat>();
            FormatBox.SelectedItem = Picker.SelectedDateFormat;
            FormatBox.SelectionChanged += (_, _) =>
            {
                if (FormatBox.SelectedItem is CalendarDatePickerFormat format)
                {
                    Picker.SelectedDateFormat = format;
                }
            };
            CustomFormat.TextChanged += (_, _) => Picker.CustomDateFormatString = CustomFormat.Text ?? string.Empty;
            Picker.CustomDateFormatString = CustomFormat.Text ?? string.Empty;

            Picker.SelectedDateChanged += (_, e) =>
                PickerResult.Text = e.AddedItems.Count > 0 ? $"已选：{Picker.SelectedDate:yyyy-MM-dd}" : "未选择";

            RangedPicker.DisplayDateStart = new DateTime(2026, 1, 1);
            RangedPicker.DisplayDateEnd = new DateTime(2026, 12, 31);
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/DatePickerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DatePickerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DatePicker：三列滚动选择的日期框"
                               DocPath="controls/input/date-and-time/datepicker" />

            <TextBlock Classes="caption" Text="1. 基本用法与 SelectedDate" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <DatePicker Name="Picker" />
                    <TextBlock Name="PickerResult" VerticalAlignment="Center" Text="未选择" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 隐藏某一列，改格式" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="16">
                        <CheckBox Name="DayCheck" Content="DayVisible" IsChecked="True" />
                        <CheckBox Name="MonthCheck" Content="MonthVisible" IsChecked="True" />
                        <CheckBox Name="YearCheck" Content="YearVisible" IsChecked="True" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="MonthFormat" />
                        <ComboBox Name="MonthFormatBox" Width="120" />
                    </StackPanel>
                    <DatePicker Name="FormatPicker" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="MonthFormat 取 M、MM、MMM、MMMM；DayFormat 与 YearFormat 同理。只留年和月，就得到「选择月份」的控件。" />

            <TextBlock Classes="caption" Text="3. MinYear / MaxYear" />
            <Border Classes="stage" Padding="12">
                <DatePicker Name="LimitedPicker" />
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/DatePickerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DatePickerPage : UserControl
    {
        public DatePickerPage()
        {
            InitializeComponent();

            Picker.SelectedDateChanged += (_, _) =>
                PickerResult.Text = Picker.SelectedDate is { } date ? $"已选：{date:yyyy-MM-dd}" : "未选择";

            DayCheck.IsCheckedChanged += (_, _) => FormatPicker.DayVisible = DayCheck.IsChecked == true;
            MonthCheck.IsCheckedChanged += (_, _) => FormatPicker.MonthVisible = MonthCheck.IsChecked == true;
            YearCheck.IsCheckedChanged += (_, _) => FormatPicker.YearVisible = YearCheck.IsChecked == true;

            MonthFormatBox.ItemsSource = new[] { "M", "MM", "MMM", "MMMM" };
            MonthFormatBox.SelectedItem = FormatPicker.MonthFormat;
            MonthFormatBox.SelectionChanged += (_, _) =>
            {
                if (MonthFormatBox.SelectedItem is string format)
                {
                    FormatPicker.MonthFormat = format;
                }
            };

            LimitedPicker.MinYear = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            LimitedPicker.MaxYear = new DateTimeOffset(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/TimePickerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TimePickerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TimePicker：时间选择"
                               DocPath="controls/input/date-and-time/timepicker" />

            <TextBlock Classes="caption" Text="ClockIdentifier、MinuteIncrement、UseSeconds" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TimePicker Name="Picker" />
                        <TextBlock Name="PickerResult" VerticalAlignment="Center" Text="未选择" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="ClockIdentifier" />
                        <ComboBox Name="ClockBox" Width="140" />
                        <CheckBox Name="SecondsCheck" Content="UseSeconds" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock Width="110" VerticalAlignment="Center" Text="MinuteIncrement" />
                        <Slider Name="StepSlider" Width="200" Minimum="1" Maximum="30" Value="1" />
                        <TextBlock VerticalAlignment="Center"
                                   Text="{Binding #StepSlider.Value, StringFormat='{}{0:F0}'}" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ClockIdentifier 是字符串，只认 12HourClock 与 24HourClock；MinuteIncrement 要能整除 60 才有意义。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/TimePickerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TimePickerPage : UserControl
    {
        public TimePickerPage()
        {
            InitializeComponent();

            Picker.SelectedTimeChanged += (_, _) =>
                PickerResult.Text = Picker.SelectedTime is { } time ? $"已选：{time:hh\\:mm\\:ss}" : "未选择";

            ClockBox.ItemsSource = new[] { "12HourClock", "24HourClock" };
            ClockBox.SelectedItem = Picker.ClockIdentifier;
            ClockBox.SelectionChanged += (_, _) =>
            {
                if (ClockBox.SelectedItem is string clock)
                {
                    Picker.ClockIdentifier = clock;
                }
            };

            SecondsCheck.IsCheckedChanged += (_, _) => Picker.UseSeconds = SecondsCheck.IsChecked == true;

            // MinuteIncrement is an int and Slider.Value a double; the handler does the conversion.
            StepSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == Slider.ValueProperty)
                {
                    Picker.MinuteIncrement = (int)StepSlider.Value;
                }
            };
        }
    }
}
```

- [ ] **Step 1: 登记四个页面**

登记统一放到 Task 7 一次完成（那里给出 `PageCatalog.Input.cs` 的最终全文，按官方顺序）。本任务只建页面文件。

- [ ] **Step 2: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|个错误|Build succeeded"`
Expected: 0 个错误。

- [ ] **Step 3: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate Calendar, CalendarDatePicker, DatePicker and TimePicker

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Selectors（CheckBox、ToggleSwitch、Slider、NumericUpDown）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml`、`.axaml.cs`
- Create: `.../ToggleSwitchPage.axaml`、`.axaml.cs`
- Create: `.../SliderPage.axaml`、`.axaml.cs`
- Create: `.../NumericUpDownPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖的元素名）：
  - `CheckBoxPage`：`Single`、`Three`（`CheckBox`）、`ParentBox`、`ChildA`、`ChildB`、`ChildC`、`ThreeResult`
  - `ToggleSwitchPage`：`Plain`、`Custom`（`ToggleSwitch`）、`PlainResult`
  - `SliderPage`：`Basic`、`Ticked`、`Vertical`（`Slider`）、`SnapCheck`、`WriteButton`、`TickedResult`
  - `NumericUpDownPage`：`Basic`、`Money`（`NumericUpDown`）、`WriteButton`、`BasicResult`、`ClipCheck`

#### `Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.CheckBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="CheckBox：复选框"
                               DocPath="controls/input/selectors/checkbox" />

            <TextBlock Classes="caption" Text="1. 双态" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <CheckBox Name="Single" Content="我同意" />
                    <TextBlock VerticalAlignment="Center"
                               Text="{Binding #Single.IsChecked, StringFormat='IsChecked = {0}'}" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 三态：父项汇总子项" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="4">
                    <CheckBox Name="ParentBox" IsThreeState="True" Content="全选" />
                    <CheckBox Name="ChildA" Margin="24,0,0,0" Content="选项 A" />
                    <CheckBox Name="ChildB" Margin="24,0,0,0" Content="选项 B" />
                    <CheckBox Name="ChildC" Margin="24,0,0,0" Content="选项 C" />
                    <TextBlock Name="ThreeResult" Classes="hint" Text="父项：未选" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="父项的 null 表示「部分选中」，由子项变化时在代码里汇总；CheckBox 自己不会替你做这件事。" />

            <TextBlock Classes="caption" Text="3. 不可用" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <CheckBox IsEnabled="False" IsChecked="True" Content="已选且不可用" />
                    <CheckBox IsEnabled="False" Content="未选且不可用" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CheckBoxPage : UserControl
    {
        private bool _updating;

        public CheckBoxPage()
        {
            InitializeComponent();

            foreach (var child in new[] { ChildA, ChildB, ChildC })
            {
                child.IsCheckedChanged += (_, _) => SummariseChildren();
            }

            ParentBox.IsCheckedChanged += (_, _) =>
            {
                // The guard stops the summary below from fighting the user's click on the parent.
                if (_updating || ParentBox.IsChecked is null)
                {
                    return;
                }

                _updating = true;
                foreach (var child in new[] { ChildA, ChildB, ChildC })
                {
                    child.IsChecked = ParentBox.IsChecked;
                }
                _updating = false;
                ThreeResult.Text = $"父项：{(ParentBox.IsChecked == true ? "全选" : "未选")}";
            };
        }

        private void SummariseChildren()
        {
            if (_updating)
            {
                return;
            }

            var checkedCount = new[] { ChildA, ChildB, ChildC }.Count(c => c.IsChecked == true);
            _updating = true;
            ParentBox.IsChecked = checkedCount switch
            {
                0 => false,
                3 => true,
                _ => null,
            };
            _updating = false;
            ThreeResult.Text = $"父项：{(ParentBox.IsChecked is null ? "部分选中" : ParentBox.IsChecked == true ? "全选" : "未选")}";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ToggleSwitchPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ToggleSwitchPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ToggleSwitch：开关"
                               DocPath="controls/input/selectors/toggleswitch" />

            <TextBlock Classes="caption" Text="1. 默认外观与 IsChecked" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <ToggleSwitch Name="Plain" />
                    <TextBlock Name="PlainResult" VerticalAlignment="Center" Text="关" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. OnContent / OffContent 自定义文字" />
            <Border Classes="stage" Padding="12">
                <ToggleSwitch Name="Custom" OnContent="已开启" OffContent="已关闭" />
            </Border>
            <TextBlock Classes="hint" Text="ToggleSwitch 适合「立即生效」的设置；需要点提交才生效的选项用 CheckBox。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ToggleSwitchPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ToggleSwitchPage : UserControl
    {
        public ToggleSwitchPage()
        {
            InitializeComponent();
            Plain.IsCheckedChanged += (_, _) => PlainResult.Text = Plain.IsChecked == true ? "开" : "关";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/SliderPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SliderPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Slider：滑块"
                               DocPath="controls/input/selectors/slider" />

            <TextBlock Classes="caption" Text="1. Minimum、Maximum、Value" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Slider Name="Basic" Width="240" Minimum="0" Maximum="100" Value="30" />
                    <TextBlock VerticalAlignment="Center"
                               Text="{Binding #Basic.Value, StringFormat='Value = {0:F1}'}" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 刻度与吸附：TickFrequency、TickPlacement、IsSnapToTickEnabled" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Slider Name="Ticked" Width="300" Minimum="0" Maximum="10"
                            TickFrequency="2" TickPlacement="BottomRight" IsSnapToTickEnabled="True" />
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="SnapCheck" Content="IsSnapToTickEnabled" IsChecked="True" />
                        <Button Name="WriteButton" Content="代码写入 3.3" />
                        <TextBlock Name="TickedResult" VerticalAlignment="Center" Text="Value = 0" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="吸附只作用于用户的拖动与按键；代码直接写 Value 不吸附（点按钮看读数）。" />

            <TextBlock Classes="caption" Text="3. 竖向：Orientation、IsDirectionReversed" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="24">
                    <Slider Name="Vertical" Orientation="Vertical" Height="140" Minimum="0" Maximum="10" Value="4" />
                    <Slider Orientation="Vertical" Height="140" Minimum="0" Maximum="10" Value="4" IsDirectionReversed="True" />
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/SliderPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SliderPage : UserControl
    {
        public SliderPage()
        {
            InitializeComponent();

            Ticked.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    TickedResult.Text = $"Value = {Ticked.Value:F1}";
                }
            };
            SnapCheck.IsCheckedChanged += (_, _) => Ticked.IsSnapToTickEnabled = SnapCheck.IsChecked == true;
            WriteButton.Click += (_, _) => Ticked.Value = 3.3;
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/NumericUpDownPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.NumericUpDownPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="NumericUpDown：数值微调框"
                               DocPath="controls/input/selectors/numericupdown" />

            <TextBlock Classes="caption" Text="1. Minimum、Maximum、Increment" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <NumericUpDown Name="Basic" Width="160" Minimum="0" Maximum="10" Increment="3" Value="9" />
                        <TextBlock Name="BasicResult" VerticalAlignment="Center" Text="Value = 9" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <CheckBox Name="ClipCheck" Content="ClipValueToMinMax" />
                        <Button Name="WriteButton" Content="代码写入 99" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="从 9 点向上微调得 10（遵守 Maximum），但代码直接写 99 会原样保留，勾选 ClipValueToMinMax 也一样。" />

            <TextBlock Classes="caption" Text="2. FormatString：把数值显示成金额" />
            <Border Classes="stage" Padding="12">
                <NumericUpDown Name="Money" Width="200" FormatString="C2" Minimum="0" Increment="0.5" Value="12.5" />
            </Border>

            <TextBlock Classes="caption" Text="3. ShowButtonSpinner、ButtonSpinnerLocation、AllowSpin、IsReadOnly" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <NumericUpDown Width="140" ButtonSpinnerLocation="Left" Value="1" />
                    <NumericUpDown Width="140" ShowButtonSpinner="False" Value="2" />
                    <NumericUpDown Width="140" AllowSpin="False" Value="3" />
                    <NumericUpDown Width="140" IsReadOnly="True" Value="4" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="NumericUpDown 内部是 ButtonSpinner 加 TextBox；更深入的演示：本页的 ButtonSpinner 页。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/NumericUpDownPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class NumericUpDownPage : UserControl
    {
        public NumericUpDownPage()
        {
            InitializeComponent();

            Basic.ValueChanged += (_, e) => BasicResult.Text = $"Value = {e.NewValue}";
            ClipCheck.IsCheckedChanged += (_, _) => Basic.ClipValueToMinMax = ClipCheck.IsChecked == true;
            WriteButton.Click += (_, _) => Basic.Value = 99;
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|个错误|Build succeeded"`
Expected: 0 个错误。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate CheckBox, ToggleSwitch, Slider and NumericUpDown

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 4: ComboBox、ColorPicker、ColorView

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/ComboBoxPage.axaml`、`.axaml.cs`
- Create: `.../ColorPickerPage.axaml`、`.axaml.cs`
- Create: `.../ColorViewPage.axaml`、`.axaml.cs`

**Interfaces:**
- Consumes: Task 1 的 ColorPicker 样式
- Produces（探针依赖的元素名）：
  - `ComboBoxPage`：`Basic`、`Editable`、`ByValue`（`ComboBox`）、`BasicResult`、`EditableResult`、`ValueResult`
  - `ColorPickerPage`：`Picker`（`ColorPicker`）、`Swatch`（`Border`）、`ColorText`、`AlphaCheck`
  - `ColorViewPage`：`View`（`ColorView`）、`ShapeBox`、`ModelBox`（`ComboBox`）、`Swatch`、`ColorText`

#### `Avalonia.ControlsDemo/Views/Pages/Input/ComboBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ComboBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ComboBox：下拉选择框"
                               DocPath="controls/input/selectors/combobox" />

            <TextBlock Classes="caption" Text="1. 静态项、PlaceholderText、SelectedItem" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <ComboBox Name="Basic" Width="180" PlaceholderText="请选择水果">
                        <ComboBoxItem Content="苹果" />
                        <ComboBoxItem Content="香蕉" />
                        <ComboBoxItem Content="樱桃" />
                    </ComboBox>
                    <TextBlock Name="BasicResult" VerticalAlignment="Center" Text="尚未选择" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. IsEditable：允许输入自己的文字" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <ComboBox Name="Editable" Width="180" IsEditable="True" PlaceholderText="选择或输入">
                        <ComboBoxItem Content="北京" />
                        <ComboBoxItem Content="上海" />
                        <ComboBoxItem Content="深圳" />
                    </ComboBox>
                    <TextBlock Name="EditableResult" VerticalAlignment="Center" Text="Text = (空)" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. SelectedValueBinding：选中的是对象，取到的是它的某个属性" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <ComboBox Name="ByValue" Width="180" SelectedValueBinding="{ReflectionBinding Code}" DisplayMemberBinding="{ReflectionBinding Name}" />
                    <TextBlock Name="ValueResult" VerticalAlignment="Center" Text="SelectedValue = (空)" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="IsTextSearchEnabled 让你在下拉列表聚焦时敲字母跳到匹配项；MaxDropDownHeight 限制下拉的高度。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

> 第 3 块必须写 `{ReflectionBinding …}`：项类型是 code-behind 里的私有 record，XAML 看不到它，没有 `x:DataType` 时编译绑定直接 `AVLN2100`（已实测）。项类型公开且在 XAML 里用 `x:DataType` 声明后可改回编译绑定。

#### `Avalonia.ControlsDemo/Views/Pages/Input/ComboBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ComboBoxPage : UserControl
    {
        private sealed record Country(string Code, string Name);

        public ComboBoxPage()
        {
            InitializeComponent();

            Basic.SelectionChanged += (_, _) =>
                BasicResult.Text = Basic.SelectedItem is ComboBoxItem item ? $"已选：{item.Content}" : "尚未选择";

            Editable.PropertyChanged += (_, e) =>
            {
                if (e.Property == ComboBox.TextProperty)
                {
                    EditableResult.Text = $"Text = {(string.IsNullOrEmpty(Editable.Text) ? "(空)" : Editable.Text)}";
                }
            };

            ByValue.ItemsSource = new[]
            {
                new Country("CN", "中国"),
                new Country("JP", "日本"),
                new Country("DE", "德国"),
            };
            ByValue.SelectionChanged += (_, _) =>
                ValueResult.Text = $"SelectedValue = {ByValue.SelectedValue ?? "(空)"}";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ColorPickerPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ColorPickerPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ColorPicker：带弹出面板的取色按钮"
                               DocPath="controls/input/selectors/colorpicker" />

            <TextBlock Classes="caption" Text="1. 点击色块弹出面板，ColorChanged 事件报告新颜色" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="16">
                    <ColorPicker Name="Picker" Color="#FF3B82F6" />
                    <Border Name="Swatch" Width="80" Height="40" CornerRadius="4" />
                    <TextBlock Name="ColorText" VerticalAlignment="Center" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. IsAlphaEnabled：是否允许透明度" />
            <Border Classes="stage" Padding="12">
                <CheckBox Name="AlphaCheck" Content="IsAlphaEnabled" IsChecked="True" />
            </Border>
            <TextBlock Classes="hint" Text="ColorPicker 继承自 ColorView，面板里的内容就是一个 ColorView；需要常驻显示时直接用 ColorView（见下一页）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ColorPickerPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ColorPickerPage : UserControl
    {
        public ColorPickerPage()
        {
            InitializeComponent();

            Picker.ColorChanged += (_, e) => Show(e.NewColor);
            AlphaCheck.IsCheckedChanged += (_, _) => Picker.IsAlphaEnabled = AlphaCheck.IsChecked == true;
            Show(Picker.Color);
        }

        // The swatch brush is set from code on purpose: nothing styles Background here, so it is not a local-value trap.
        private void Show(Color color)
        {
            Swatch.Background = new SolidColorBrush(color);
            ColorText.Text = color.ToString();
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ColorViewPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ColorViewPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ColorView：常驻显示的取色面板"
                               DocPath="controls/input/selectors/colorview" />

            <TextBlock Classes="caption" Text="ColorSpectrumShape、ColorModel 与读数" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="24">
                    <ColorView Name="View" Color="#FF10B981" />
                    <StackPanel Spacing="8" VerticalAlignment="Top">
                        <ComboBox Name="ShapeBox" Width="140" />
                        <ComboBox Name="ModelBox" Width="140" />
                        <Border Name="Swatch" Width="140" Height="40" CornerRadius="4" />
                        <TextBlock Name="ColorText" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="ColorView 与 ColorPicker 共用同一套属性：Color、HsvColor、IsAlphaVisible、ColorSpectrumShape、ColorModel、Palette 等。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ColorViewPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ColorViewPage : UserControl
    {
        public ColorViewPage()
        {
            InitializeComponent();

            ShapeBox.ItemsSource = Enum.GetValues<ColorSpectrumShape>();
            ShapeBox.SelectedItem = View.ColorSpectrumShape;
            ShapeBox.SelectionChanged += (_, _) =>
            {
                if (ShapeBox.SelectedItem is ColorSpectrumShape shape)
                {
                    View.ColorSpectrumShape = shape;
                }
            };

            ModelBox.ItemsSource = Enum.GetValues<ColorModel>();
            ModelBox.SelectedItem = View.ColorModel;
            ModelBox.SelectionChanged += (_, _) =>
            {
                if (ModelBox.SelectedItem is ColorModel model)
                {
                    View.ColorModel = model;
                }
            };

            View.ColorChanged += (_, e) => Show(e.NewColor);
            Show(View.Color);
        }

        private void Show(Color color)
        {
            Swatch.Background = new SolidColorBrush(color);
            ColorText.Text = $"{color}  H={View.HsvColor.H:F0}";
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|个错误|Build succeeded"`
Expected: 0 个错误。若 `ColorSpectrumShape` / `ColorModel` 找不到，加 `using Avalonia.Controls.Primitives;`（枚举所在命名空间以构建为准）。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate ComboBox, ColorPicker and ColorView

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 5: Text input（AutoCompleteBox、MaskedTextBox、TextBox）

**Files:**
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/AutoCompleteBoxPage.axaml`、`.axaml.cs`
- Create: `.../MaskedTextBoxPage.axaml`、`.axaml.cs`
- Create: `.../TextBoxPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖的元素名）：
  - `AutoCompleteBoxPage`：`Box`（`AutoCompleteBox`）、`ModeBox`（`ComboBox`）、`BoxResult`
  - `MaskedTextBoxPage`：`Phone`（`MaskedTextBox`）、`PhoneResult`
  - `TextBoxPage`：`Basic`、`Password`、`Multi`、`Limited`、`Selectable`（`TextBox`）、`BasicResult`、`RevealCheck`、`SelectButton`、`SelectionResult`

#### `Avalonia.ControlsDemo/Views/Pages/Input/AutoCompleteBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.AutoCompleteBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="AutoCompleteBox：边输入边提示"
                               DocPath="controls/input/text-input/autocompletebox" />

            <TextBlock Classes="caption" Text="FilterMode 与 MinimumPrefixLength" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <AutoCompleteBox Name="Box" Width="220" MinimumPrefixLength="1" PlaceholderText="试试输入 rr 或 ap" />
                        <TextBlock Name="BoxResult" VerticalAlignment="Center" Text="未选择" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock VerticalAlignment="Center" Text="FilterMode" />
                        <ComboBox Name="ModeBox" Width="220" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="候选词：Apple、Apricot、Banana、Blueberry、Cherry。StartsWith 下 rr 无匹配，Contains 下命中 Cherry。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/AutoCompleteBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class AutoCompleteBoxPage : UserControl
    {
        public AutoCompleteBoxPage()
        {
            InitializeComponent();

            Box.ItemsSource = new[] { "Apple", "Apricot", "Banana", "Blueberry", "Cherry" };
            Box.FilterMode = AutoCompleteFilterMode.Contains;

            ModeBox.ItemsSource = Enum.GetValues<AutoCompleteFilterMode>();
            ModeBox.SelectedItem = Box.FilterMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is AutoCompleteFilterMode mode)
                {
                    Box.FilterMode = mode;
                }
            };

            Box.SelectionChanged += (_, _) =>
                BoxResult.Text = Box.SelectedItem is string item ? $"已选：{item}" : "未选择";
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/MaskedTextBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.MaskedTextBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="MaskedTextBox：按掩码限制输入格式"
                               DocPath="controls/input/text-input/maskedtextbox" />

            <TextBlock Classes="caption" Text="1. 电话号码：Mask 与 PromptChar" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <MaskedTextBox Name="Phone" Width="200" Mask="(000) 000-0000" PromptChar="_" />
                    <TextBlock Name="PhoneResult" VerticalAlignment="Center" Text="未填完" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 日期与大写字母" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <MaskedTextBox Width="140" Mask="0000-00-00" PromptChar="_" />
                    <MaskedTextBox Width="140" Mask="&gt;LLL-000" PromptChar="_" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="掩码字符：0 = 必填数字，9 = 可选数字，L = 必填字母，? = 可选字母，> 之后转大写，其余原样显示。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/MaskedTextBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MaskedTextBoxPage : UserControl
    {
        public MaskedTextBoxPage()
        {
            InitializeComponent();

            Phone.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                {
                    PhoneResult.Text = Phone.MaskCompleted == true ? $"已填完：{Phone.Text}" : "未填完";
                }
            };
        }
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/TextBoxPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TextBoxPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TextBox：文本输入框"
                               DocPath="controls/input/text-input/textbox" />

            <TextBlock Classes="caption" Text="1. Text、PlaceholderText 与 TextChanged" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBox Name="Basic" Width="220" PlaceholderText="请输入姓名" />
                    <TextBlock Name="BasicResult" VerticalAlignment="Center" Text="长度 0" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. PasswordChar 与 RevealPassword" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBox Name="Password" Width="220" PasswordChar="●" Text="secret" />
                    <CheckBox Name="RevealCheck" Content="显示密码" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. 多行：AcceptsReturn、TextWrapping、MinLines / MaxLines" />
            <Border Classes="stage" Padding="12">
                <TextBox Name="Multi" Width="320" AcceptsReturn="True" TextWrapping="Wrap"
                         MinLines="2" MaxLines="4"
                         Text="第一行&#10;第二行" />
            </Border>

            <TextBlock Classes="caption" Text="4. MaxLength 与 IsReadOnly" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBox Name="Limited" Width="160" MaxLength="5" PlaceholderText="最多 5 个字" />
                    <TextBox Width="160" IsReadOnly="True" Text="只读，可选中复制" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="5. 选区：SelectionStart、SelectionEnd、SelectedText" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TextBox Name="Selectable" Width="320" Text="Avalonia 的 TextBox 支持程序化选区" />
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Button Name="SelectButton" Content="选中前 7 个字符" />
                        <TextBlock Name="SelectionResult" VerticalAlignment="Center" Text="选区：无" />
                    </StackPanel>
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="InnerLeftContent / InnerRightContent、撤销重做、校验等见紧随其后的「实战：TextBox」。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/TextBoxPage.axaml.cs`

```csharp
using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextBoxPage : UserControl
    {
        public TextBoxPage()
        {
            InitializeComponent();

            Basic.TextChanged += (_, _) => BasicResult.Text = $"长度 {Basic.Text?.Length ?? 0}";
            RevealCheck.IsCheckedChanged += (_, _) => Password.RevealPassword = RevealCheck.IsChecked == true;

            SelectButton.Click += (_, _) =>
            {
                Selectable.Focus();
                Selectable.SelectionStart = 0;
                Selectable.SelectionEnd = 7;
                SelectionResult.Text = $"选区：「{Selectable.SelectedText}」";
            };
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: demonstrate AutoCompleteBox, MaskedTextBox and TextBox

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 6: 四篇 How-to 实战页

每篇是对应官方 How-to 里**能在一个窗口内演示的场景**，紧跟在控件页之后；`PageKind.HowTo`，树里显示为「实战：…」。

**Files:**
- Create: `Avalonia.ControlsDemo/ViewModels/TextBoxHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/ComboBoxHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/ViewModels/DatePickerHowToViewModel.cs`
- Create: `Avalonia.ControlsDemo/Views/Pages/Input/TextBoxHowToPage.axaml`、`.axaml.cs`
- Create: `.../ComboBoxHowToPage.axaml`、`.axaml.cs`
- Create: `.../SliderHowToPage.axaml`、`.axaml.cs`
- Create: `.../DatePickerHowToPage.axaml`、`.axaml.cs`

**Interfaces:**
- Produces（探针依赖）：
  - `TextBoxHowToViewModel : ObservableValidator`：`string Username`（必填、至少 3 字符，`[NotifyDataErrorInfo]`）、`string ErrorText`
  - `Employee(string Name, string Department)`、`enum Priority { Low, Normal, High }`、`PriorityOption(Priority Value, string Display)`（均 `public`，命名空间 `Avalonia.ControlsDemo.ViewModels`）
  - `DatePickerHowToViewModel : ObservableValidator`：`DateTimeOffset? Date`（不得早于今天）、`TimeSpan? Time`、`string Combined`、`string ErrorText`
  - 元素名：`TextBoxHowToPage`：`UsernameBox`、`ErrorLabel`、`DigitsBox`、`DigitsResult`、`SearchBox`、`SearchList`、`ClearButton`、`UndoBox`、`UndoButton`、`RedoButton`；`ComboBoxHowToPage`：`EmployeeBox`、`PriorityBox`、`PriorityResult`、`SearchEmployee`（`AutoCompleteBox`）、`SearchResult`；`SliderHowToPage`：`RedSlider`、`GreenSlider`、`BlueSlider`、`Preview`、`HexText`、`IntSlider`、`IntResult`、`DisabledSlider`、`NoHitSlider`；`DatePickerHowToPage`：`DateBox`（`DatePicker`）、`TimeBox`（`TimePicker`）、`CombinedLabel`、`DateErrorLabel`

#### `Avalonia.ControlsDemo/ViewModels/TextBoxHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Backs the validation block of the TextBox how-to: attributes on the field become rules on the property.</summary>
    public sealed partial class TextBoxHowToViewModel : ObservableValidator
    {
        public TextBoxHowToViewModel()
        {
            // Validation runs on every change; the label is refreshed whenever the error set changes.
            ErrorsChanged += (_, _) => OnPropertyChanged(nameof(ErrorText));
        }

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "用户名必填")]
        [MinLength(3, ErrorMessage = "至少 3 个字符")]
        private string _username = string.Empty;

        public string ErrorText => HasErrors
            ? string.Join("；", GetErrors(nameof(Username)).Select(e => e.ErrorMessage))
            : "校验通过（输入至少 3 个字符）";
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/TextBoxHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.TextBoxHowToPage"
             x:DataType="vm:TextBoxHowToViewModel">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="TextBox 实战"
                               DocPath="docs/how-to/textbox-how-to" />

            <TextBlock Classes="caption" Text="1. 校验：ObservableValidator + [NotifyDataErrorInfo]" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="6">
                    <TextBox Name="UsernameBox" Width="260" PlaceholderText="用户名"
                             Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" />
                    <TextBlock Name="ErrorLabel" Classes="hint" Text="{Binding ErrorText}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="绑定到 INotifyDataErrorInfo 的属性，出错时 TextBox 自动进入 :error 状态并显示红框。" />

            <TextBlock Classes="caption" Text="2. 只允许数字：在 TextChanging 里改写 Text" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBox Name="DigitsBox" Width="200" PlaceholderText="只能输入数字" />
                    <TextBlock Name="DigitsResult" VerticalAlignment="Center" Text="" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="官方文章里的 e.Cancel 在 12.1.2 不存在（TextChangingEventArgs 没有任何成员），这里改成把 Text 重写为过滤后的结果。" />

            <TextBlock Classes="caption" Text="3. 搜索框：InnerLeftContent、InnerRightContent，TextChanged 实时过滤" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TextBox Name="SearchBox" Width="260" PlaceholderText="搜索水果">
                        <TextBox.InnerLeftContent>
                            <TextBlock Margin="8,0,0,0" VerticalAlignment="Center" Text="🔍" />
                        </TextBox.InnerLeftContent>
                        <TextBox.InnerRightContent>
                            <Button Name="ClearButton" Content="✕" Padding="6,2" Background="Transparent" />
                        </TextBox.InnerRightContent>
                    </TextBox>
                    <ListBox Name="SearchList" Width="260" Height="110" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="4. 撤销与重做：CanUndo、CanRedo、Undo()、Redo()" />
            <TextBlock Classes="hint" Text="请在下面的框里键入几个字再点撤销：代码给 Text 赋值不会进入撤销栈。" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <TextBox Name="UndoBox" Width="320" Text="改一改，再撤销" />
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Name="UndoButton" Content="撤销" />
                        <Button Name="RedoButton" Content="重做" />
                    </StackPanel>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="5. 自定义右键菜单" />
            <Border Classes="stage" Padding="12">
                <TextBox Name="MenuBox" Width="320" Text="右键点我：菜单只有剪切、复制、粘贴">
                    <TextBox.ContextMenu>
                        <ContextMenu>
                            <MenuItem Name="CutItem" Header="剪切" />
                            <MenuItem Name="CopyItem" Header="复制" />
                            <MenuItem Name="PasteItem" Header="粘贴" />
                        </ContextMenu>
                    </TextBox.ContextMenu>
                </TextBox>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/TextBoxHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextBoxHowToPage : UserControl
    {
        private static readonly string[] Fruits = { "苹果", "香蕉", "樱桃", "葡萄", "橙子", "西瓜", "草莓" };

        public TextBoxHowToPage()
        {
            InitializeComponent();
            DataContext = new TextBoxHowToViewModel();

            // Digits only: rewriting Text is the only filter available in 12.1.2.
            // Setting Text raises TextChanging again, so only write when something was removed.
            DigitsBox.TextChanging += (_, _) =>
            {
                var text = DigitsBox.Text ?? string.Empty;
                var digits = new string(text.Where(char.IsDigit).ToArray());
                if (digits != text)
                {
                    DigitsBox.Text = digits;
                    DigitsBox.CaretIndex = digits.Length;
                    DigitsResult.Text = "已滤掉非数字";
                }
                else
                {
                    DigitsResult.Text = string.Empty;
                }
            };

            SearchList.ItemsSource = Fruits;
            SearchBox.TextChanged += (_, _) =>
            {
                var key = SearchBox.Text ?? string.Empty;
                SearchList.ItemsSource = Fruits.Where(f => f.Contains(key, StringComparison.OrdinalIgnoreCase)).ToList();
            };
            ClearButton.Click += (_, _) => SearchBox.Clear();

            UndoButton.Click += (_, _) => UndoBox.Undo();
            RedoButton.Click += (_, _) => UndoBox.Redo();

            CutItem.Click += (_, _) => MenuBox.Cut();
            CopyItem.Click += (_, _) => MenuBox.Copy();
            PasteItem.Click += (_, _) => MenuBox.Paste();
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/ComboBoxHowToViewModel.cs`

```csharp
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>An item type for templated ComboBox content; public so XAML can name it in x:DataType.</summary>
    public sealed record Employee(string Name, string Department);

    public enum Priority
    {
        Low,
        Normal,
        High,
    }

    /// <summary>An enum value paired with the text shown for it; enum members themselves cannot carry display text.</summary>
    public sealed record PriorityOption(Priority Value, string Display)
    {
        public static PriorityOption[] All { get; } = Enum.GetValues<Priority>()
            .Select(p => new PriorityOption(p, p switch
            {
                Priority.Low => "低",
                Priority.Normal => "普通",
                _ => "高",
            }))
            .ToArray();
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ComboBoxHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.ComboBoxHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="ComboBox 实战"
                               DocPath="docs/how-to/combobox-how-to" />

            <TextBlock Classes="caption" Text="1. ItemTemplate：每一项自定义外观" />
            <Border Classes="stage" Padding="12">
                <ComboBox Name="EmployeeBox" Width="260" PlaceholderText="选择员工">
                    <ComboBox.ItemTemplate>
                        <DataTemplate x:DataType="vm:Employee">
                            <StackPanel Orientation="Horizontal" Spacing="8">
                                <TextBlock FontWeight="SemiBold" Text="{Binding Name}" />
                                <TextBlock Opacity="0.6" Text="{Binding Department}" />
                            </StackPanel>
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
            </Border>

            <TextBlock Classes="caption" Text="2. 绑定枚举：用带显示文字的 record 包一层" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <ComboBox Name="PriorityBox" Width="140"
                              SelectedValueBinding="{ReflectionBinding Value}"
                              DisplayMemberBinding="{ReflectionBinding Display}" />
                    <TextBlock Name="PriorityResult" VerticalAlignment="Center" Text="SelectedValue = (空)" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="枚举成员没有地方放中文显示名，所以给每个枚举值配一个 PriorityOption(Value, Display)，用 SelectedValue 取回枚举。" />

            <TextBlock Classes="caption" Text="3. 数据很多时：换成 AutoCompleteBox 边输入边搜" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <AutoCompleteBox Name="SearchEmployee" Width="260" MinimumPrefixLength="1"
                                     FilterMode="Contains" PlaceholderText="输入姓名片段" />
                    <TextBlock Name="SearchResult" VerticalAlignment="Center" Text="未选择" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="4. 样式：放宽下拉面板、改占位符颜色" />
            <Border Classes="stage" Padding="12">
                <ComboBox Name="StyledBox" Width="120" PlaceholderText="窄框宽下拉">
                    <ComboBox.Styles>
                        <Style Selector="ComboBox /template/ Popup">
                            <Setter Property="MinWidth" Value="260" />
                        </Style>
                        <Style Selector="ComboBox /template/ TextBlock#PlaceholderTextBlock">
                            <Setter Property="Foreground" Value="Orange" />
                        </Style>
                    </ComboBox.Styles>
                    <ComboBoxItem Content="这一项的文字很长很长很长很长" />
                    <ComboBoxItem Content="短项" />
                </ComboBox>
            </Border>
            <TextBlock Classes="hint" Text="Popup 的 MinWidth 与占位符前景都写在 ComboBox 自己的 Styles 里，不在元素上写本地值（规则 1）。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/ComboBoxHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ComboBoxHowToPage : UserControl
    {
        private static readonly Employee[] Staff =
        {
            new("张伟", "研发部"),
            new("王芳", "市场部"),
            new("李娜", "财务部"),
            new("刘洋", "研发部"),
            new("陈静", "人事部"),
        };

        public ComboBoxHowToPage()
        {
            InitializeComponent();

            EmployeeBox.ItemsSource = Staff;

            PriorityBox.ItemsSource = PriorityOption.All;
            PriorityBox.SelectionChanged += (_, _) =>
                PriorityResult.Text = $"SelectedValue = {PriorityBox.SelectedValue ?? "(空)"}";

            // AutoCompleteBox filters on the string produced by ItemSelector, so give it the name.
            SearchEmployee.ItemsSource = Staff;
            SearchEmployee.ItemSelector = (_, item) => (item as Employee)?.Name ?? string.Empty;
            SearchEmployee.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Employee>(
                (e, _) => new TextBlock { Text = $"{e.Name}（{e.Department}）" });
            SearchEmployee.SelectionChanged += (_, _) =>
                SearchResult.Text = SearchEmployee.SelectedItem is Employee e ? $"已选：{e.Name}，{e.Department}" : "未选择";
        }
    }
}
```

> 第 3 块里 `ItemSelector` 的签名是 `Func<string?, object?, string?>`（搜索文本、项 → 过滤与回填用的字符串），已按此构建通过。

#### `Avalonia.ControlsDemo/Views/Pages/Input/SliderHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             x:Class="Avalonia.ControlsDemo.Views.Pages.SliderHowToPage">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="Slider 实战"
                               DocPath="docs/how-to/slider-how-to" />

            <TextBlock Classes="caption" Text="1. 三个滑块合成一个 RGB 颜色" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="20" VerticalAlignment="Center" Text="R" />
                        <Slider Name="RedSlider" Width="260" Minimum="0" Maximum="255" Value="64" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="20" VerticalAlignment="Center" Text="G" />
                        <Slider Name="GreenSlider" Width="260" Minimum="0" Maximum="255" Value="128" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Width="20" VerticalAlignment="Center" Text="B" />
                        <Slider Name="BlueSlider" Width="260" Minimum="0" Maximum="255" Value="192" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <Border Name="Preview" Width="120" Height="40" CornerRadius="4" />
                        <TextBlock Name="HexText" VerticalAlignment="Center" />
                    </StackPanel>
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="2. 只取整数：IsSnapToTickEnabled + TickFrequency=1" />
            <Border Classes="stage" Padding="12">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Slider Name="IntSlider" Width="260" Minimum="0" Maximum="10"
                            TickFrequency="1" TickPlacement="BottomRight" IsSnapToTickEnabled="True" />
                    <TextBlock Name="IntResult" VerticalAlignment="Center" Text="Value = 0" />
                </StackPanel>
            </Border>

            <TextBlock Classes="caption" Text="3. IsEnabled 与 IsHitTestVisible 的区别" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <Slider Name="DisabledSlider" Width="260" Minimum="0" Maximum="10" Value="5" IsEnabled="False" />
                    <Slider Name="NoHitSlider" Width="260" Minimum="0" Maximum="10" Value="5" IsHitTestVisible="False" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="IsEnabled=False 会变灰；IsHitTestVisible=False 外观不变，只是点不动。想「看起来正常但不能操作」用后者。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/SliderHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SliderHowToPage : UserControl
    {
        public SliderHowToPage()
        {
            InitializeComponent();

            foreach (var slider in new[] { RedSlider, GreenSlider, BlueSlider })
            {
                slider.PropertyChanged += (_, e) =>
                {
                    if (e.Property == RangeBase.ValueProperty)
                    {
                        UpdatePreview();
                    }
                };
            }

            IntSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    IntResult.Text = $"Value = {IntSlider.Value:F0}";
                }
            };

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var color = Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            Preview.Background = new SolidColorBrush(color);
            HexText.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
```

#### `Avalonia.ControlsDemo/ViewModels/DatePickerHowToViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Backs the DatePicker how-to: a date that may not be in the past, plus a time, combined into one value.</summary>
    public sealed partial class DatePickerHowToViewModel : ObservableValidator
    {
        public DatePickerHowToViewModel()
        {
            ErrorsChanged += (_, _) => OnPropertyChanged(nameof(ErrorText));
        }

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyPropertyChangedFor(nameof(Combined))]
        [CustomValidation(typeof(DatePickerHowToViewModel), nameof(ValidateNotPast))]
        private DateTimeOffset? _date;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Combined))]
        private TimeSpan? _time;

        public string Combined => Date is { } date && Time is { } time
            ? $"预约时间：{date.Date + time:yyyy-MM-dd HH:mm}"
            : "请同时选择日期和时间";

        public string ErrorText => HasErrors
            ? string.Join("；", GetErrors(nameof(Date)).Select(e => e.ErrorMessage))
            : string.Empty;

        public static ValidationResult? ValidateNotPast(DateTimeOffset? value, ValidationContext context)
            => value is { } date && date.Date < DateTime.Today
                ? new ValidationResult("日期不能早于今天")
                : ValidationResult.Success;
    }
}
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/DatePickerHowToPage.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:shared="using:Avalonia.Shared.Controls"
             xmlns:vm="using:Avalonia.ControlsDemo.ViewModels"
             x:Class="Avalonia.ControlsDemo.Views.Pages.DatePickerHowToPage"
             x:DataType="vm:DatePickerHowToViewModel">
    <ScrollViewer>
        <StackPanel Margin="12">
            <shared:DemoHeader Title="DatePicker 实战"
                               DocPath="docs/how-to/datepicker-how-to" />

            <TextBlock Classes="caption" Text="日期加时间合成一个预约时间，并校验日期不早于今天" />
            <Border Classes="stage" Padding="12">
                <StackPanel Spacing="8">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <DatePicker Name="DateBox" SelectedDate="{Binding Date, Mode=TwoWay}" />
                        <TimePicker Name="TimeBox" ClockIdentifier="24HourClock" MinuteIncrement="15"
                                    SelectedTime="{Binding Time, Mode=TwoWay}" />
                    </StackPanel>
                    <TextBlock Name="CombinedLabel" Text="{Binding Combined}" />
                    <TextBlock Name="DateErrorLabel" Foreground="OrangeRed" Text="{Binding ErrorText}" />
                </StackPanel>
            </Border>
            <TextBlock Classes="hint" Text="校验规则写在 ViewModel 的 [CustomValidation] 上，页面只绑定属性；出错文字来自 INotifyDataErrorInfo。" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### `Avalonia.ControlsDemo/Views/Pages/Input/DatePickerHowToPage.axaml.cs`

```csharp
using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DatePickerHowToPage : UserControl
    {
        public DatePickerHowToPage()
        {
            InitializeComponent();
            DataContext = new DatePickerHowToViewModel();
        }
    }
}
```

- [ ] **Step 1: 构建**

Run: `dotnet build Avalonia.ControlsDemo 2>&1 | grep -E "error|warn|个错误|Build succeeded"`
Expected: 0 个错误、无警告。常见问题：`[ObservableProperty]` 与 `[NotifyDataErrorInfo]` 叠加的属性要求类继承 `ObservableValidator`（已满足）；`ItemSelector` 签名见 ComboBoxHowToPage 后的说明。

- [ ] **Step 2: 提交**

```bash
git add Avalonia.ControlsDemo
git commit -m "feat: add the TextBox, ComboBox, Slider and DatePicker how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

### Task 7: 登记全部页面、探针验证、收尾

**Files:**
- Modify: `Avalonia.ControlsDemo/Navigation/PageCatalog.Input.cs`（整体替换）
- Create（仓库外）: `C:\Temp\probe-controls\Probe.Input.cs`
- Modify（仓库外）: `C:\Temp\probe-controls\Program.cs`
- Modify: `docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md`（回填）

**Interfaces:**
- Consumes: Task 2–6 的全部页面类；第 00 份的 `ControlPage<T>`、`HowToPage<T>`、`Harness`
- Produces: Input 分类共 22 个 `Control` 页 + 4 个 `HowTo` 页，登记顺序为官方侧边栏顺序，HowTo 紧跟其控件

- [ ] **Step 1: 登记**

#### `Avalonia.ControlsDemo/Navigation/PageCatalog.Input.cs`

```csharp
using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddInput(List<PageEntry> list)
        {
            const string c = Categories.Input;

            // Buttons, in the order of the official sidebar.
            list.Add(ControlPage<ButtonPage>("Button", c, "controls/input/buttons/button"));
            list.Add(ControlPage<ButtonSpinnerPage>("ButtonSpinner", c, "controls/input/buttons/buttonspinner"));
            list.Add(ControlPage<HyperlinkButtonPage>("HyperlinkButton", c, "controls/input/buttons/hyperlinkbutton"));
            list.Add(ControlPage<RadioButtonPage>("RadioButton", c, "controls/input/buttons/radiobutton"));
            list.Add(ControlPage<RepeatButtonPage>("RepeatButton", c, "controls/input/buttons/repeatbutton"));
            list.Add(ControlPage<SplitButtonPage>("SplitButton", c, "controls/input/buttons/splitbutton"));
            list.Add(ControlPage<ToggleButtonPage>("ToggleButton", c, "controls/input/buttons/togglebutton"));
            list.Add(ControlPage<ToggleSplitButtonPage>("ToggleSplitButton", c, "controls/input/buttons/togglesplitbutton"));

            // Date and time
            list.Add(ControlPage<CalendarPage>("Calendar", c, "controls/input/date-and-time/calendar"));
            list.Add(ControlPage<CalendarDatePickerPage>("CalendarDatePicker", c, "controls/input/date-and-time/calendardatepicker"));
            list.Add(ControlPage<DatePickerPage>("DatePicker", c, "controls/input/date-and-time/datepicker"));
            list.Add(HowToPage<DatePickerHowToPage>("DatePicker", c, "docs/how-to/datepicker-how-to"));
            list.Add(ControlPage<TimePickerPage>("TimePicker", c, "controls/input/date-and-time/timepicker"));

            // Selectors
            list.Add(ControlPage<CheckBoxPage>("CheckBox", c, "controls/input/selectors/checkbox"));
            list.Add(ControlPage<ColorPickerPage>("ColorPicker", c, "controls/input/selectors/colorpicker"));
            list.Add(ControlPage<ColorViewPage>("ColorView", c, "controls/input/selectors/colorview"));
            list.Add(ControlPage<ComboBoxPage>("ComboBox", c, "controls/input/selectors/combobox"));
            list.Add(HowToPage<ComboBoxHowToPage>("ComboBox", c, "docs/how-to/combobox-how-to"));
            list.Add(ControlPage<NumericUpDownPage>("NumericUpDown", c, "controls/input/selectors/numericupdown"));
            list.Add(ControlPage<SliderPage>("Slider", c, "controls/input/selectors/slider"));
            list.Add(HowToPage<SliderHowToPage>("Slider", c, "docs/how-to/slider-how-to"));
            list.Add(ControlPage<ToggleSwitchPage>("ToggleSwitch", c, "controls/input/selectors/toggleswitch"));

            // Text input
            list.Add(ControlPage<AutoCompleteBoxPage>("AutoCompleteBox", c, "controls/input/text-input/autocompletebox"));
            list.Add(ControlPage<MaskedTextBoxPage>("MaskedTextBox", c, "controls/input/text-input/maskedtextbox"));
            list.Add(ControlPage<TextBoxPage>("TextBox", c, "controls/input/text-input/textbox"));
            list.Add(HowToPage<TextBoxHowToPage>("TextBox", c, "docs/how-to/textbox-how-to"));
        }
    }
}
```

- [ ] **Step 2: 写探针（仓库外，不提交）**

探针分三个文件（每个文件都是 `partial class ProbeInput` 的一部分），原文如下。要点：弹出层不走可视树；`AutoCompleteBox` 与 `MaskedTextBox` 必须用 `KeyTextInput` 键入；撤销要先键入再断言；日期用 `DateTimeKind.Unspecified` 构造 `DateTimeOffset`，否则本地时区会抛 `ArgumentException`。

**`C:\Temp\probe-controls\Probe.Input.cs`**（目录完整性、Date and time）

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Controls;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeInput
{
    public static void Run()
    {
        Catalog();
        DateAndTime();
        Selectors();
        TextInput();
        HowTos();
    }

    static void Catalog()
    {
        var input = PageCatalog.All.Where(e => e.Category == Categories.Input).ToList();
        Harness.Check("Input: 22 Control pages", input.Count(e => e.Kind == PageKind.Control) == 22, input.Count(e => e.Kind == PageKind.Control));
        Harness.Check("Input: 4 HowTo pages", input.Count(e => e.Kind == PageKind.HowTo) == 4, input.Count(e => e.Kind == PageKind.HowTo));

        // Each HowTo page sits directly after the control page with the same title.
        for (int i = 0; i < input.Count; i++)
        {
            if (input[i].Kind != PageKind.HowTo) continue;
            Harness.Check($"Input: HowTo {input[i].Title} follows its control",
                i > 0 && input[i - 1].Kind == PageKind.Control && input[i - 1].Title == input[i].Title);
            Harness.Check($"Input: HowTo {input[i].Title} DocPath starts with docs/how-to/", input[i].DocPath.StartsWith("docs/how-to/"));
        }

        foreach (var entry in input)
        {
            var page = entry.CreatePage();
            var w = Harness.Show(page);
            var header = page.GetVisualDescendants().OfType<DemoHeader>().FirstOrDefault();
            Harness.Check($"Input/{entry.Title}({entry.Kind}): renders", page.Bounds.Width > 0 && page.Bounds.Height > 0, page.Bounds);
            Harness.Check($"Input/{entry.Title}({entry.Kind}): header matches catalog", header?.DocPath == entry.DocPath, header?.DocPath);
            w.Close();
        }
    }

    static void DateAndTime()
    {
        // ---- Calendar
        var cal = new CalendarPage();
        var w = Harness.Show(cal);
        var c = Harness.Find<Calendar>(cal, "Cal");
        Harness.Check("Calendar: SelectionMode box starts on the control's mode", Harness.Find<ComboBox>(cal, "ModeBox").SelectedItem is CalendarSelectionMode.SingleDate);
        Harness.Find<ComboBox>(cal, "ModeBox").SelectedItem = CalendarSelectionMode.MultipleRange;
        Harness.Pump();
        Harness.Check("Calendar: choosing MultipleRange changes the control", c.SelectionMode == CalendarSelectionMode.MultipleRange, c.SelectionMode);
        Harness.Find<ComboBox>(cal, "DisplayModeBox").SelectedItem = CalendarMode.Year;
        Harness.Pump();
        Harness.Check("Calendar: DisplayMode box drives the control", c.DisplayMode == CalendarMode.Year, c.DisplayMode);
        Harness.Find<ComboBox>(cal, "FirstDayBox").SelectedItem = DayOfWeek.Monday;
        Harness.Pump();
        Harness.Check("Calendar: FirstDayOfWeek box drives both calendars",
            c.FirstDayOfWeek == DayOfWeek.Monday && Harness.Find<Calendar>(cal, "BlackoutCal").FirstDayOfWeek == DayOfWeek.Monday);
        Harness.Find<Button>(cal, "BlackoutButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var blackout = Harness.Find<TextBlock>(cal, "BlackoutResult").Text;
        Harness.Check("Calendar: selecting a blacked-out date reports the exception", blackout?.Contains("ArgumentOutOfRangeException") == true, blackout);
        var bc = Harness.Find<Calendar>(cal, "BlackoutCal");
        // Measured quirk: the rejected value stays in SelectedDate while SelectedDates stays empty.
        Harness.Check("Calendar: after the throw SelectedDate keeps the rejected value but SelectedDates is empty",
            bc.SelectedDate == new DateTime(2026, 1, 6) && bc.SelectedDates.Count == 0, $"{bc.SelectedDate} / {bc.SelectedDates.Count}");
        w.Close();

        // ---- CalendarDatePicker
        var cdp = new CalendarDatePickerPage();
        w = Harness.Show(cdp);
        var picker = Harness.Find<CalendarDatePicker>(cdp, "Picker");
        picker.SelectedDate = new DateTime(2026, 3, 9);
        Harness.Pump();
        Harness.Check("CalendarDatePicker: result follows SelectedDate",
            Harness.Find<TextBlock>(cdp, "PickerResult").Text == "已选：2026-03-09", Harness.Find<TextBlock>(cdp, "PickerResult").Text);
        Harness.Find<ComboBox>(cdp, "FormatBox").SelectedItem = CalendarDatePickerFormat.Custom;
        Harness.Pump();
        Harness.Check("CalendarDatePicker: Custom format applies CustomDateFormatString",
            picker.SelectedDateFormat == CalendarDatePickerFormat.Custom && picker.CustomDateFormatString == "yyyy年M月d日"
            && picker.Text == "2026年3月9日", $"{picker.SelectedDateFormat} '{picker.CustomDateFormatString}' '{picker.Text}'");
        var ranged = Harness.Find<CalendarDatePicker>(cdp, "RangedPicker");
        Harness.Check("CalendarDatePicker: DisplayDateStart/End are 2026",
            ranged.DisplayDateStart == new DateTime(2026, 1, 1) && ranged.DisplayDateEnd == new DateTime(2026, 12, 31));
        w.Close();

        // ---- DatePicker
        var dp = new DatePickerPage();
        w = Harness.Show(dp);
        Harness.Find<DatePicker>(dp, "Picker").SelectedDate = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);
        Harness.Pump();
        Harness.Check("DatePicker: result follows SelectedDate", Harness.Find<TextBlock>(dp, "PickerResult").Text == "已选：2026-05-20",
            Harness.Find<TextBlock>(dp, "PickerResult").Text);
        var fp = Harness.Find<DatePicker>(dp, "FormatPicker");
        Harness.Find<CheckBox>(dp, "DayCheck").IsChecked = false;
        Harness.Pump();
        Harness.Check("DatePicker: unticking DayVisible hides the day column", fp.DayVisible == false);
        Harness.Find<ComboBox>(dp, "MonthFormatBox").SelectedItem = "MMMM";
        Harness.Pump();
        Harness.Check("DatePicker: MonthFormat follows the box", fp.MonthFormat == "MMMM", fp.MonthFormat);
        var lim = Harness.Find<DatePicker>(dp, "LimitedPicker");
        Harness.Check("DatePicker: MinYear/MaxYear are 2020/2030", lim.MinYear.Year == 2020 && lim.MaxYear.Year == 2030);
        w.Close();

        // ---- TimePicker
        var tp = new TimePickerPage();
        w = Harness.Show(tp);
        var t = Harness.Find<TimePicker>(tp, "Picker");
        t.SelectedTime = new TimeSpan(14, 5, 0);
        Harness.Pump();
        Harness.Check("TimePicker: result follows SelectedTime", Harness.Find<TextBlock>(tp, "PickerResult").Text == "已选：14:05:00",
            Harness.Find<TextBlock>(tp, "PickerResult").Text);
        Harness.Find<ComboBox>(tp, "ClockBox").SelectedItem = "24HourClock";
        Harness.Find<CheckBox>(tp, "SecondsCheck").IsChecked = true;
        Harness.Find<Slider>(tp, "StepSlider").Value = 15;
        Harness.Pump();
        Harness.Check("TimePicker: ClockIdentifier / UseSeconds / MinuteIncrement follow the controls",
            t.ClockIdentifier == "24HourClock" && t.UseSeconds && t.MinuteIncrement == 15, $"{t.ClockIdentifier} {t.UseSeconds} {t.MinuteIncrement}");
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Input2.cs`**（Selectors、Text input）

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeInput
{
    static void Selectors()
    {
        // ---- CheckBox: three-state parent summarises its children
        var cb = new CheckBoxPage();
        var w = Harness.Show(cb);
        var parent = Harness.Find<CheckBox>(cb, "ParentBox");
        Harness.Find<CheckBox>(cb, "ChildA").IsChecked = true;
        Harness.Pump();
        Harness.Check("CheckBox: one child checked makes the parent indeterminate", parent.IsChecked is null, parent.IsChecked);
        Harness.Find<CheckBox>(cb, "ChildB").IsChecked = true;
        Harness.Find<CheckBox>(cb, "ChildC").IsChecked = true;
        Harness.Pump();
        Harness.Check("CheckBox: all children checked makes the parent checked", parent.IsChecked == true, parent.IsChecked);
        parent.IsChecked = false;
        Harness.Pump();
        Harness.Check("CheckBox: unchecking the parent clears every child",
            new[] { "ChildA", "ChildB", "ChildC" }.All(n => Harness.Find<CheckBox>(cb, n).IsChecked == false));
        w.Close();

        // ---- ToggleSwitch
        var ts = new ToggleSwitchPage();
        w = Harness.Show(ts);
        Harness.Find<ToggleSwitch>(ts, "Plain").IsChecked = true;
        Harness.Pump();
        Harness.Check("ToggleSwitch: result follows IsChecked", Harness.Find<TextBlock>(ts, "PlainResult").Text == "开");
        Harness.Check("ToggleSwitch: custom On/Off content",
            Harness.Find<ToggleSwitch>(ts, "Custom").OnContent?.ToString() == "已开启" && Harness.Find<ToggleSwitch>(ts, "Custom").OffContent?.ToString() == "已关闭");
        w.Close();

        // ---- Slider: programmatic writes are not snapped
        var sl = new SliderPage();
        w = Harness.Show(sl);
        Harness.Find<Button>(sl, "WriteButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        var ticked = Harness.Find<Slider>(sl, "Ticked");
        Harness.Check("Slider: a code write is not snapped to the tick", Math.Abs(ticked.Value - 3.3) < 1e-9, ticked.Value);
        Harness.Check("Slider: the readout shows the unsnapped value", Harness.Find<TextBlock>(sl, "TickedResult").Text == "Value = 3.3",
            Harness.Find<TextBlock>(sl, "TickedResult").Text);
        Harness.Check("Slider: vertical slider is vertical", Harness.Find<Slider>(sl, "Vertical").Orientation == Avalonia.Layout.Orientation.Vertical);
        w.Close();

        // ---- NumericUpDown
        var nu = new NumericUpDownPage();
        w = Harness.Show(nu);
        var basic = Harness.Find<NumericUpDown>(nu, "Basic");
        var spinner = basic.GetVisualDescendants().OfType<ButtonSpinner>().First();
        spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, SpinDirection.Increase));
        Harness.Pump();
        Harness.Check("NumericUpDown: spinning up from 9 stops at Maximum 10", basic.Value == 10m, basic.Value);
        Harness.Find<Button>(nu, "WriteButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("NumericUpDown: a code write of 99 is kept", basic.Value == 99m, basic.Value);
        Harness.Check("NumericUpDown: the readout follows ValueChanged", Harness.Find<TextBlock>(nu, "BasicResult").Text == "Value = 99",
            Harness.Find<TextBlock>(nu, "BasicResult").Text);
        Harness.Check("NumericUpDown: Money uses a currency format", Harness.Find<NumericUpDown>(nu, "Money").FormatString == "C2");
        w.Close();

        // ---- ComboBox
        var co = new ComboBoxPage();
        w = Harness.Show(co);
        Harness.Find<ComboBox>(co, "Basic").SelectedIndex = 1;
        Harness.Pump();
        Harness.Check("ComboBox: static item selection reported", Harness.Find<TextBlock>(co, "BasicResult").Text == "已选：香蕉",
            Harness.Find<TextBlock>(co, "BasicResult").Text);
        Harness.Find<ComboBox>(co, "ByValue").SelectedIndex = 1;
        Harness.Pump();
        Harness.Check("ComboBox: SelectedValueBinding yields the Code, not the object",
            Harness.Find<TextBlock>(co, "ValueResult").Text == "SelectedValue = JP", Harness.Find<TextBlock>(co, "ValueResult").Text);
        Harness.Check("ComboBox: Editable flag is set", Harness.Find<ComboBox>(co, "Editable").IsEditable);
        w.Close();

        // ---- ColorPicker and ColorView: styles are included, so they have visuals
        var cp = new ColorPickerPage();
        w = Harness.Show(cp);
        var picker = Harness.Find<ColorPicker>(cp, "Picker");
        Harness.Check("ColorPicker: renders visuals (StyleInclude present)", picker.GetVisualDescendants().Count() > 5, picker.GetVisualDescendants().Count());
        picker.Color = Colors.Lime;
        Harness.Pump();
        Harness.Check("ColorPicker: ColorChanged updates the swatch",
            Harness.Find<Border>(cp, "Swatch").Background is SolidColorBrush { Color: var swatch } && swatch == Colors.Lime);
        Harness.Find<CheckBox>(cp, "AlphaCheck").IsChecked = false;
        Harness.Pump();
        Harness.Check("ColorPicker: IsAlphaEnabled follows the box", picker.IsAlphaEnabled == false);
        w.Close();

        var cv = new ColorViewPage();
        w = Harness.Show(cv);
        var view = Harness.Find<ColorView>(cv, "View");
        Harness.Check("ColorView: renders visuals (StyleInclude present)", view.GetVisualDescendants().Count() > 50, view.GetVisualDescendants().Count());
        Harness.Find<ComboBox>(cv, "ShapeBox").SelectedItem = ColorSpectrumShape.Ring;
        Harness.Find<ComboBox>(cv, "ModelBox").SelectedItem = ColorModel.Rgba;
        Harness.Pump();
        Harness.Check("ColorView: shape and model follow the boxes",
            view.ColorSpectrumShape == ColorSpectrumShape.Ring && view.ColorModel == ColorModel.Rgba, $"{view.ColorSpectrumShape} {view.ColorModel}");
        view.Color = Colors.Red;
        Harness.Pump();
        Harness.Check("ColorView: readout shows hue 0 for red", Harness.Find<TextBlock>(cv, "ColorText").Text?.Contains("H=0") == true,
            Harness.Find<TextBlock>(cv, "ColorText").Text);
        w.Close();
    }

    static void TextInput()
    {
        // ---- AutoCompleteBox: typing opens the drop-down; assigning Text does not
        var ac = new AutoCompleteBoxPage();
        var w = Harness.Show(ac);
        var box = Harness.Find<AutoCompleteBox>(ac, "Box");
        var inner = box.GetVisualDescendants().OfType<TextBox>().First();
        inner.Focus();
        Harness.Pump();
        w.KeyTextInput("rr");
        Harness.Pump(); Harness.Pump();
        Harness.Check("AutoCompleteBox: typing 'rr' opens the drop-down under Contains", box.IsDropDownOpen, box.Text);
        box.FilterMode = AutoCompleteFilterMode.StartsWith;
        inner.Text = string.Empty;
        Harness.Pump();
        w.KeyTextInput("rr");
        Harness.Pump(); Harness.Pump();
        Harness.Check("AutoCompleteBox: under StartsWith 'rr' matches nothing", !box.IsDropDownOpen, box.IsDropDownOpen);
        w.Close();

        // ---- MaskedTextBox
        var mk = new MaskedTextBoxPage();
        w = Harness.Show(mk);
        var phone = Harness.Find<MaskedTextBox>(mk, "Phone");
        phone.Focus();
        Harness.Pump();
        w.KeyTextInput("5551234567");
        Harness.Pump();
        Harness.Check("MaskedTextBox: digits fill the mask", phone.Text == "(555) 123-4567", phone.Text);
        Harness.Check("MaskedTextBox: MaskCompleted is true", phone.MaskCompleted == true);
        Harness.Check("MaskedTextBox: the page reports completion",
            Harness.Find<TextBlock>(mk, "PhoneResult").Text == "已填完：(555) 123-4567", Harness.Find<TextBlock>(mk, "PhoneResult").Text);
        w.Close();

        // ---- TextBox
        var tb = new TextBoxPage();
        w = Harness.Show(tb);
        Harness.Find<TextBox>(tb, "Basic").Text = "abcd";
        Harness.Pump();
        Harness.Check("TextBox: TextChanged updates the length", Harness.Find<TextBlock>(tb, "BasicResult").Text == "长度 4");
        Harness.Find<CheckBox>(tb, "RevealCheck").IsChecked = true;
        Harness.Pump();
        Harness.Check("TextBox: RevealPassword follows the box", Harness.Find<TextBox>(tb, "Password").RevealPassword);
        Harness.Check("TextBox: MaxLength is 5", Harness.Find<TextBox>(tb, "Limited").MaxLength == 5);
        Harness.Find<Button>(tb, "SelectButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        var sel = Harness.Find<TextBox>(tb, "Selectable");
        Harness.Check("TextBox: programmatic selection covers 7 characters", sel.SelectionEnd - sel.SelectionStart == 7 && sel.SelectedText == "Avaloni",
            $"[{sel.SelectionStart},{sel.SelectionEnd}) '{sel.SelectedText}'");
        w.Close();
    }
}
```

**`C:\Temp\probe-controls\Probe.Input3.cs`**（四篇 How-to）

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;

static partial class ProbeInput
{
    static void HowTos()
    {
        // ---- TextBox how-to
        var page = new TextBoxHowToPage();
        var w = Harness.Show(page);
        var vm = (TextBoxHowToViewModel)page.DataContext!;
        var user = Harness.Find<TextBox>(page, "UsernameBox");
        user.Text = "ab";
        Harness.Pump();
        Harness.Check("TextBox how-to: 2 characters fail MinLength", vm.HasErrors && Harness.Find<TextBlock>(page, "ErrorLabel").Text?.Contains("至少 3") == true,
            Harness.Find<TextBlock>(page, "ErrorLabel").Text);
        user.Text = "abc";
        Harness.Pump();
        Harness.Check("TextBox how-to: 3 characters pass", !vm.HasErrors && Harness.Find<TextBlock>(page, "ErrorLabel").Text?.StartsWith("校验通过") == true,
            Harness.Find<TextBlock>(page, "ErrorLabel").Text);

        var digits = Harness.Find<TextBox>(page, "DigitsBox");
        digits.Text = "12ab3";
        Harness.Pump();
        Harness.Check("TextBox how-to: rewriting Text in TextChanging keeps only digits", digits.Text == "123", digits.Text);
        Harness.Check("TextBox how-to: the filter reports it removed something", Harness.Find<TextBlock>(page, "DigitsResult").Text == "已滤掉非数字");

        var search = Harness.Find<TextBox>(page, "SearchBox");
        var list = Harness.Find<ListBox>(page, "SearchList");
        search.Text = "果";
        Harness.Pump();
        Harness.Check("TextBox how-to: search filters the list", list.ItemCount == 1 && list.Items.Cast<string>().First() == "苹果", list.ItemCount);
        Harness.Find<Button>(page, "ClearButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("TextBox how-to: the inner clear button empties the box and restores the list", string.IsNullOrEmpty(search.Text) && list.ItemCount == 7,
            $"'{search.Text}' {list.ItemCount}");

        var undo = Harness.Find<TextBox>(page, "UndoBox");
        undo.Focus();
        Harness.Pump();
        undo.CaretIndex = undo.Text!.Length;
        w.KeyTextInput("!");
        Harness.Pump();
        Harness.Check("TextBox how-to: an edit makes CanUndo true", undo.CanUndo, undo.CanUndo);
        Harness.Find<Button>(page, "UndoButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Harness.Pump();
        Harness.Check("TextBox how-to: Undo restores the earlier text", undo.Text == "改一改，再撤销", undo.Text);
        Harness.Check("TextBox how-to: custom context menu has 3 items", Harness.Find<TextBox>(page, "MenuBox").ContextMenu?.Items.Count == 3);
        w.Close();

        // ---- ComboBox how-to
        var cb = new ComboBoxHowToPage();
        w = Harness.Show(cb);
        var emp = Harness.Find<ComboBox>(cb, "EmployeeBox");
        Harness.Check("ComboBox how-to: 5 employees", emp.ItemCount == 5, emp.ItemCount);
        emp.SelectedIndex = 0;
        Harness.Pump();
        Harness.Check("ComboBox how-to: selected item is an Employee", emp.SelectedItem is Employee { Name: "张伟" });
        var pri = Harness.Find<ComboBox>(cb, "PriorityBox");
        pri.SelectedIndex = 2;
        Harness.Pump();
        Harness.Check("ComboBox how-to: SelectedValue is the enum, not the record", pri.SelectedValue is Priority.High, pri.SelectedValue);
        Harness.Check("ComboBox how-to: PriorityBox shows 3 options", pri.ItemCount == 3);
        var auto = Harness.Find<AutoCompleteBox>(cb, "SearchEmployee");
        auto.SelectedItem = auto.ItemsSource!.Cast<Employee>().First(e => e.Name == "李娜");
        Harness.Pump();
        Harness.Check("ComboBox how-to: AutoCompleteBox selection reported",
            Harness.Find<TextBlock>(cb, "SearchResult").Text == "已选：李娜，财务部", Harness.Find<TextBlock>(cb, "SearchResult").Text);
        w.Close();

        // ---- Slider how-to
        var sl = new SliderHowToPage();
        w = Harness.Show(sl);
        Harness.Check("Slider how-to: initial hex is #4080C0", Harness.Find<TextBlock>(sl, "HexText").Text == "#4080C0", Harness.Find<TextBlock>(sl, "HexText").Text);
        Harness.Find<Slider>(sl, "RedSlider").Value = 255;
        Harness.Pump();
        Harness.Check("Slider how-to: moving red updates the preview and hex",
            Harness.Find<TextBlock>(sl, "HexText").Text == "#FF80C0"
            && Harness.Find<Border>(sl, "Preview").Background is SolidColorBrush { Color: var c } && c.R == 255,
            Harness.Find<TextBlock>(sl, "HexText").Text);
        Harness.Find<Slider>(sl, "IntSlider").Value = 4;
        Harness.Pump();
        Harness.Check("Slider how-to: integer slider reads back", Harness.Find<TextBlock>(sl, "IntResult").Text == "Value = 4");
        Harness.Check("Slider how-to: IsEnabled=False vs IsHitTestVisible=False",
            !Harness.Find<Slider>(sl, "DisabledSlider").IsEffectivelyEnabled
            && Harness.Find<Slider>(sl, "NoHitSlider").IsEffectivelyEnabled
            && !Harness.Find<Slider>(sl, "NoHitSlider").IsHitTestVisible);
        w.Close();

        // ---- DatePicker how-to
        var dp = new DatePickerHowToPage();
        w = Harness.Show(dp);
        var dvm = (DatePickerHowToViewModel)dp.DataContext!;
        Harness.Check("DatePicker how-to: starts asking for both values",
            Harness.Find<TextBlock>(dp, "CombinedLabel").Text == "请同时选择日期和时间", Harness.Find<TextBlock>(dp, "CombinedLabel").Text);
        var future = DateTimeOffset.Now.Date.AddDays(10);
        Harness.Find<DatePicker>(dp, "DateBox").SelectedDate = new DateTimeOffset(future, TimeSpan.Zero);
        Harness.Find<TimePicker>(dp, "TimeBox").SelectedTime = new TimeSpan(9, 30, 0);
        Harness.Pump();
        Harness.Check("DatePicker how-to: date and time combine",
            Harness.Find<TextBlock>(dp, "CombinedLabel").Text == $"预约时间：{future:yyyy-MM-dd} 09:30", Harness.Find<TextBlock>(dp, "CombinedLabel").Text);
        Harness.Check("DatePicker how-to: a future date has no error", !dvm.HasErrors, dvm.ErrorText);
        Harness.Find<DatePicker>(dp, "DateBox").SelectedDate = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Today.AddDays(-3), DateTimeKind.Unspecified), TimeSpan.Zero);
        Harness.Pump();
        Harness.Check("DatePicker how-to: a past date fails validation",
            dvm.HasErrors && Harness.Find<TextBlock>(dp, "DateErrorLabel").Text == "日期不能早于今天", Harness.Find<TextBlock>(dp, "DateErrorLabel").Text);
        w.Close();
    }
}
```

同时把 `C:\Temp\probe-controls\Program.cs` 的 `Main` 改为依次调用 `ProbeShell.Run(); ProbeButtons.Run(); ProbeInput.Run(); ProbePackages.Run(); ProbeNewPages.Run();`。

第 00 份留下的 `Probe.Packages.cs` 要做一处调整，因为 `App.axaml` 现在已经包含 ColorPicker 样式，"不加样式时为空"的那条断言不再成立：把

```csharp
Harness.Check("ColorPicker without a StyleInclude has no visuals", PickerVisuals() == 0);
```

换成

```csharp
Harness.Check("ColorPicker + ColorView render under the app styles", PickerVisuals() > 50);
```

并删掉文件末尾对 ColorPicker 的 `Include(...)` 与 `Styles.Remove(cp)` 两处。DataGrid 的两条断言保持不变（DataGrid 在第 03 份才引入）。

- [ ] **Step 3: 跑探针**

Run: `cd /c/Temp/probe-controls && dotnet run 2>&1 | grep -E "FAIL|error|Unhandled|passed"`
Expected: `0 failed`、`0 warning log(s)`（编写计划时实测为 **245 passed**，其中第 00 份的 65 条在内）。

**失败时的排查顺序**：
1. `ColorPicker/ColorView: renders visuals` 失败：`App.axaml` 的 `StyleInclude` 路径少了 `Fluent/` 一层，或没加（Task 1）；
2. `AutoCompleteBox` 两条失败：探针里改用了给 `Text` 赋值而不是键入（赋值不展开下拉）；
3. `TextBox how-to` 的撤销两条失败：探针用 `Text = ...` 赋值而不是键入（赋值不进撤销栈）；
4. `CheckBox: …parent…` 失败：`_updating` 守卫缺失，父项与子项互相触发；
5. 任何页面 `renders` 失败且无异常：对照第 00 份「Buttons 页面的共同写法」检查骨架。

- [ ] **Step 4: 全量构建**

Run: `dotnet build hello-avalonia.slnx 2>&1 | grep -E "error|warn|个错误|Build succeeded" | tail -5`
Expected: 0 个错误、无新增警告。

- [ ] **Step 5: 真实窗口目视（人工，一次）**

```bash
dotnet run --project Avalonia.ControlsDemo
```

确认：Input 分类下 26 个条目都能切换；`ColorPicker` 页点色块弹出面板、拖动取色后右侧色块变色；`ColorView` 页切换 `Ring` 后光谱变成圆环；`AutoCompleteBox` 页键入 `rr` 弹出 Cherry；`TextBox` 实战页的右键菜单只有三项；`Calendar` 页点"用代码选中 1 月 6 日"显示异常文字。这些弹出层与真实键入是 headless 验证不了的部分。

- [ ] **Step 6: 回填 spec**

在 spec「待验证的技术风险」末尾的「批 0 实测结论」之后追加：

```markdown
### 批 1 实测结论（填入执行日期）

- **控件的静默行为**：`NumericUpDown` 与 `Slider` 的代码赋值既不夹取也不吸附（`ClipValueToMinMax` 与 `IsSnapToTickEnabled` 只管用户操作与微调）；`Calendar` 把黑名单日期赋给 `SelectedDate` 会抛 `ArgumentOutOfRangeException`，且抛出后 `SelectedDate` 保留被拒绝的值而 `SelectedDates` 为空；`AutoCompleteBox` 要靠键入才展开下拉，给 `Text` 赋值不会；`TextBox` 给 `Text` 赋值不进撤销栈。
- **API 与官方 How-to 的出入**：官方 TextBox 指南里的 `TextChanging` + `e.Cancel` 在 12.1.2 不存在，`TextChangingEventArgs` 没有成员；改成在处理器里重写 `Text`。
- **绑定**：`ComboBox.SelectedValueBinding` / `DisplayMemberBinding` 的项类型若是页面内的私有类型，编译绑定报 `AVLN2100`，要写 `{ReflectionBinding …}`，或把项类型公开并写 `x:DataType`。
- **探针断言实际条数**：本批累计 245 条（含第 00 份），全部通过且零警告日志。
```

尖括号与"填入执行日期"必须替换为实际日期；数字以 Step 3 的真实输出为准。

- [ ] **Step 7: 提交**

```bash
git add Avalonia.ControlsDemo docs/superpowers/specs/2026-10-10-avalonia-controls-demo-design.md
git commit -m "feat: complete the Input category with 14 control pages and 4 how-to pages

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review（编写者自查记录）

**Spec 覆盖**：Input 的 Date and time 4、Selectors 7、Text input 3 共 14 页 → Task 2–5；4 篇 How-to（TextBox、ComboBox、Slider、DatePicker）→ Task 6；官方侧边栏顺序与 HowTo 紧跟 → Task 7 Step 1 与探针的"紧跟"断言；风险 4（ColorPicker 样式）→ Task 1 与探针后代数断言。

**已实测，不是推断**：本份全部页面代码已从计划原文抽取到 `C:\Temp\plan-verify`，构建 0 错误 0 警告，探针 245 条通过。`ComboBox` 的 `AVLN2100`、`MaskCompleted` 的 `bool?`、`CustomFormat.Text` 的可空警告三处都是构建时发现并已回写到上面的代码里。

**类型一致性**：`Employee`、`Priority`、`PriorityOption`、`TextBoxHowToViewModel`、`DatePickerHowToViewModel` 的成员名在 ViewModel、页面 XAML、code-behind 与探针里一致；元素名与 Task 2–6 的 Interfaces 列表一致。

**未覆盖，已知**：真实键入下"只允许数字"在逐字符输入时的体验（探针只测了一次性赋值 `"12ab3"`）；`Calendar` 在用户点击黑名单日期时的表现；这些在 Step 5 目视。

