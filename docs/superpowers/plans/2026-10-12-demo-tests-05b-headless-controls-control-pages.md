# 演示测试 05b：ControlsDemo 的 80 个控件页 Headless 交互测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 spec 批 5 的后半（05b）：给 `ControlsDemo` 的 80 个控件页补 H3（每个有交互的页面至少一条"操作 → 界面变化"），按目录分 4 个测试文件；同时修复探针发现的 6 个页面缺陷（用户 2026-10-17 确认全部修复）。

**Architecture:** 只往 05a 建好的 `tests/Avalonia.ControlsDemo.Tests` 里加 4 个文件，沿用 `PageHelpers.Open<TPage>`/`Text`/`Press`/`PressText` 与共享库的 `UiHelpers`。每个任务先放入测试文件（缺陷测试此时会红），再改 `src/` 修缺陷，然后全绿、做破坏检查、提交。不新增项目、不改共享库。

**Tech Stack:** .NET 10.0、Avalonia 12.1.2、Avalonia.Headless.XUnit、Avalonia.Skia、Avalonia.Controls.DataGrid、Avalonia.Controls.ColorPicker、xUnit v3（3.2.2）

**Spec:** `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（批 5；05a 已完成，见「批 5 实测结论」的 05a 小节）

## Global Constraints

- 新写的注释（代码与 XAML）用**英文**；断言里的界面文字用界面上的**中文**原文；改动的提示文字（`Classes="hint"`）用中文。
- 版本只写在根目录 `Directory.Packages.props`；本批不改它，也不改 csproj。
- 代码风格沿用 05a 的测试文件：块作用域命名空间 `Avalonia.ControlsDemo.Tests`、测试方法名用英文下划线句式、LF 行尾。
- 不改 `tests/Avalonia.Tests.Common` 与 `PageHelpers.cs`；本批需要的小辅助（`Center`、`PressKey`、`RightClick`、`TypeEach` 等）是各测试文件里的 `private static` 方法。
- `src/` 只允许下面 6 处修复（用户 2026-10-17 确认）：`ProgressBarPage.axaml.cs`、`ToggleButtonPage.axaml`、`CheckBoxPage.axaml`、`CalendarDatePickerPage.axaml.cs`、`PipsPagerPage.axaml`、`NativeMenuPage.axaml.cs`。破坏检查临时改的行必须 `git checkout -- <文件>` 还原（注意不要把同一文件里的修复一起还原掉——破坏检查一律在**提交之后**做）。
- 测试不读写真实的 `%AppData%`/`%LocalAppData%`；打开窗口的测试在 `finally` 里 `Close()`；改进程级状态（`TrayIcon.SetIcons(Application.Current, …)`）的测试在 `finally` 里还原。
- 异步结果一律 `await UiHelpers.WaitUntil(...)`（默认 5 秒），不要 `Thread.Sleep` 或固定 `Task.Delay`。
- 格式化数字、日期的断言包在 `using var culture = CultureScope.Invariant();` 里。
- 提交信息用英文，结尾附 `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`；提交用 `git -c user.name=anyu -c user.email=glepooek@outlook.com commit ...`，不写 git 配置。
- 探针放在仓库外（本批是 `C:\Temp\probe-b5b\` 与 `.claude/worktrees/agent-*`），**永不提交**。
- 此前的 352 个测试始终保持通过。

## Review Focus

按"最可能让使用者踩到"的顺序；每条在所属任务里都有对应测试。

1. **改自定义日期格式会让应用崩溃**（缺陷 #1）：`CalendarDatePickerPage` 把格式框的每次编辑直接写进 `CustomDateFormatString`。全选删除（空串 → `ArgumentException`）或只剩一个非标准字母（`Q` → `FormatException`）都从 `TextChanged` 里漏出来，桌面应用没有未处理异常兜底，进程直接退出；页面提示却写着"格式串写错不会抛异常"。Task 2 的 `Editing_the_custom_format_box_never_throws` 先红后绿。
2. **离开页面后原生菜单还挂在窗口上**（缺陷 #2）：Avalonia 12 的 `DetachedFromVisualTree` 里 `e.RootVisual` 是 `TopLevelHost`，`is TopLevel` 永远不成立，在有全局菜单栏的平台上演示菜单会一直留着。同类写法静默失效、无日志。Task 4 的 `Leaving_the_page_takes_the_native_menu_off_the_window` 先红后绿。
3. **复选框与控件实际状态相反**（缺陷 #3、#5）：`PipsPagerPage` 的两个复选框初始不勾、提示写"默认隐藏"，但 v12 默认显示；`CheckBoxPage` 点击已全选的父项进入"部分选中"、子项不清空、文字仍是「全选」。Task 3、Task 2 各有先红后绿的测试。
4. **页面打开的窗口与托盘图标泄漏到后面的测试**：`WindowPage`、`WindowDrawnDecorationsPage` 开子窗口，`TrayIconPage` 往 `Application` 上挂托盘图标。Task 4 断言"离开页面即关闭/摘下"，并在 `finally` 里关宿主、还原图标；破坏检查覆盖窗口强制关闭与图标还原两条。
5. **点击静默落空与"永远通过"的可见性断言**：`ContentDemoPage` 的 `BottomCheck` 在 x = 650–841，600 宽窗口里点不中；`ToggleSwitch` 的开/关内容靠 `Opacity` 切换，两者一直 `IsEffectivelyVisible`。对应测试分别先放宽窗口、改断言 `Opacity`，并都有"之前/之后"两半。

---
## 本份已核实的事实（编写计划时在仓库外的探针里实测）

探针是 4 个独立的 git worktree（每组一个，互不争用 `obj/`，破坏检查可以安全地改 `src/`），成品测试文件汇总在 `C:\Temp\probe-b5b\`。**本计划 Task 1–4 的测试代码就是探针里实跑过的成品**：缺陷测试当时标 `Skip`，其余 143 个用例各连跑 3 次全绿，整个测试项目跑过一次无串扰；6 个缺陷各自"去掉 Skip → 红、套用修复 → 绿"。汇总时只做了三件事：去掉 6 个 `Skip`、改写缺陷测试的注释、把 CRLF 统一成 LF；以及 Layout 的 `Next_check_box_hides_and_shows_the_next_button` 去掉了"先用代码勾上 `NextCheck`"那一步（修复后复选框本来就勾着），所以它在修复前也会红。PipsPager 的修法由探针的"构造时同步"改为更简单的 XAML `IsChecked="True"`，执行时以 Task 3 的红绿为准。

### 覆盖清单

| 目录 | 控件页 | 有 H3 的页 | 用例数 | 只靠 H1 的页（理由） |
|---|---|---|---|---|
| Data display | 12 | 12 | 31（含 Feedback） | — |
| Feedback | 4 | 4 | （同上） | — |
| Input | 22 | 22 | 42 | — |
| Layout | 20 | 19 | 33（29 个 Fact + 1 个 4 组 Theory） | `GroupBoxPage`：纯 XAML、无处理器 |
| Media、Menus、Navigation、Primitives、System | 22 | 21 | 43 | `SeparatorPage`：纯 XAML、code-behind 为空 |
| **合计** | **80** | **78** | **149** | 2 |

页面里**有意不测**的部分（按 spec 只断言"能构造"，或者是真实操作系统行为）：悬停计时弹出的 ToolTip、Carousel/过渡/SplitView 动画的中间帧、触摸滑动、DataGrid 单元格编辑与列拖拽、`HyperlinkButton` 的 `NavigateUri`（只走 `Launcher`）、`RepeatButton` 的真实重复计时、真实托盘与原生菜单导出、`LabelPage` 的失焦路径（页上只有一个可聚焦控件，`window.Focus()` 拿不走焦点）。

### 6 个缺陷（用户 2026-10-17 确认全部修复）

| # | 页面 | 复现 | 修复前（实测） | 修复 | 所在任务 |
|---|---|---|---|---|---|
| 1 | `Input/CalendarDatePickerPage` | 格式框全选后 Backspace；或 Custom 模式下把格式换成 `Q` | `ArgumentException`（`'' is not a valid value for 'CustomDateFormatString'`）/ `FormatException`，从 `Type`/`KeyPress` 里抛出 | `TextChanged` 里空串直接返回，`DateTime.Today.ToString(format)` 抛 `FormatException` 也返回，否则才赋值 | Task 2 |
| 2 | `Menus/NativeMenuPage` | 勾选「挂到当前窗口」后离开页面 | `NativeMenu.GetMenu(window)` 仍是 `Built` | 挂上时记下 `TopLevel.GetTopLevel(this)`，离开时用它 `SetMenu(_top, null)` | Task 4 |
| 3 | `Layout/PipsPagerPage` | 打开页面 | 两个复选框不勾，`IsPreviousButtonVisible`/`IsNextButtonVisible` 却都是 `True`；提示写"默认隐藏" | XAML 两个复选框加 `IsChecked="True"`，提示改为默认显示 | Task 3 |
| 4 | `Input/ToggleButtonPage` | 打开页面看三态按钮的读数 | `IsChecked = `（后面是空的） | `FallbackValue='IsChecked = null'` → `TargetNullValue='IsChecked = null'` | Task 2 |
| 5 | `Input/CheckBoxPage` | 点「全选」使三项全勾，再点一次 | 父项变 null、子项全勾、文字仍 `父项：全选` | 去掉 `ParentBox` 的 `IsThreeState="True"`（代码仍可设 null 表示部分选中） | Task 2 |
| 6 | `Feedback/ProgressBarPage` | 勾「竖向」 | `Bounds` 为 20×200（Fluent 竖向样式 `MinHeight = 200` 压过 `Height = 100`），溢出 120 高的外框 | 竖向时 `Bar.MinHeight = 0`，横向时 `Bar.ClearValue(MinHeightProperty)`（不留本地值，免得永久压住主题样式） | Task 1 |

### 写测试时踩过的坑（对后续批同样适用）

- **弹出层都在窗口的叠加层上**：ComboBox 下拉项、AutoCompleteBox 建议、SplitButton/MenuFlyout/ContextMenu 菜单、ColorPicker 弹窗、Flyout、ToolTip、Popup、通知卡片，只能用 `window.GetVisualDescendants()` 找；断言写"打开前 0 个、打开后恰好 N 个"。`ComboBoxItem` 派生自 `ListBoxItem`，窗口范围的 `OfType<ListBoxItem>()` 会把打开着的下拉项也算进去。
- **关上的子菜单项不在视觉树里**：`ByName<MenuItem>` 找不到，用 `page.FindControl<MenuItem>(name)`；折叠的 Expander 内容同理，展开后再找。
- **`ToggleSwitch` 的开/关内容靠 `Opacity` 切换**，两个 `ContentPresenter` 一直 `IsEffectivelyVisible`，断言可见性永远通过。
- **`MaskedTextBox` 要逐字符 `Type`**：一次输入多个字符时光标落点不对（`"1234567"` 会得到 `(512) 345-6755`）。
- **点了复选框或按钮焦点就跑了**：键盘测试每次按键前重新 `Focus()` 目标（`PressKey` 辅助就是干这个），否则方向键静默无效。
- **`TextChanged` 里的异常从输入调用本身抛出**（`Type`/`KeyPress`），不是从 `RunJobs`；`Record.Exception` 要把输入调用包进去。
- **`Color.ToString()` 对已知颜色返回名字**（`Gray`、`Orange`、`Red`），不是十六进制；`ImmutableSolidColorBrush.ToString()` 同理（`OrangeRed`）。
- **`CalendarDayButton` 的停用状态是伪类**：`Classes` 里是 `":blackout"`，`Contains("blackout")` 不成立。
- **运行时异常消息的尾巴跟随 UI 语言**：`(Parameter 'e')` 不受 `CultureScope` 控制，比较用 `StartsWith`。
- **布局值**：`Grid` 设了 `ColumnSpacing` 后 `ColumnDefinition.ActualWidth` 把间距也算进每一列（含最后一列），改断言子元素 `Bounds`；`UniformGrid` 的小数格子被布局取整（46.67 → 47）；`ScrollToEnd()` 去的是左下角（X 回 0）；`HorizontalScrollBarVisibility=Disabled` 时代码仍能把 `Offset.X` 设到 280。
- **事件早于动画结束**：`SplitView.PaneOpened` 触发时窗格宽度还在动画里（167、171），用 `WaitUntil` 等最终宽度；`NavigationPage` 的 `PageSlide` 在 Headless 下不会报告完成，导航前先把过渡选成「无」。
- **派发优先级**：`CommandBar` 的溢出计数以 `Loaded` 优先级投递，断言前要 `RunJobs` → `UpdateLayout` → `RunJobs`；`ImagePage` 的读数跟 `LayoutUpdated` 走，选完要 `RunJobs` + `UpdateLayout`。
- **`Image` 的 `StretchDirection.DownOnly`** 只影响测量（期望 40×20），排列仍占满槽位（258×129）。
- **`ScrollBar.ScrollToEnd()` 报的是 `LargeIncrement`**，不是 `EndScroll`；`PopToRootAsync` 每移除一页触发一次 `Popped`。
- **`NativeMenuItemSeparator` 也是 `NativeMenuItem`**（Header 为 `"-"`），取菜单项要排除它。
- **贴边控件**：`ContentDemoPage.BottomCheck` 在 x = 650–841，先 `window.Width = 900`。
- **命名冲突**：Navigation 组要找 Avalonia 自带的 `CarouselPage` 时写全名 `Avalonia.Controls.CarouselPage`（演示的 `CarouselPage` 是 Data display 的控件页）；`NavigationPage`、`TabbedPage`、`ContentPage`、`CommandBar` 不冲突。Data display 组的文件仍用 05a 的别名 `using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;`。
- **局部辅助方法不要叫 `Key`**：参数 `Key key` 会解析成方法本身，编译失败。
- **xUnit2013**：`Assert.Equal(1, x.Count())` 报警告，用 `Assert.Single`。

## 文件结构

**新增**（都在 `tests/Avalonia.ControlsDemo.Tests/`）

| 文件 | 职责 |
|---|---|
| `ControlDataFeedbackTests.cs` | H3：Data display 12 页 + Feedback 4 页 |
| `ControlInputTests.cs` | H3：Input 22 页 |
| `ControlLayoutTests.cs` | H3：Layout 19 页 |
| `ControlShellTests.cs` | H3：Media 3、Menus 4、Navigation 8、Primitives 5、System 1 页 |

**修改**

| 文件 | 改动 |
|---|---|
| `src/Avalonia.ControlsDemo/Views/Pages/Feedback/ProgressBarPage.axaml.cs` | 缺陷 #6（Task 1） |
| `src/Avalonia.ControlsDemo/Views/Pages/Input/ToggleButtonPage.axaml` | 缺陷 #4（Task 2） |
| `src/Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml` | 缺陷 #5（Task 2） |
| `src/Avalonia.ControlsDemo/Views/Pages/Input/CalendarDatePickerPage.axaml.cs` | 缺陷 #1（Task 2） |
| `src/Avalonia.ControlsDemo/Views/Pages/Layout/PipsPagerPage.axaml` | 缺陷 #3（Task 3） |
| `src/Avalonia.ControlsDemo/Views/Pages/Menus/NativeMenuPage.axaml.cs` | 缺陷 #2（Task 4） |
| `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md` | 回填「批 5 实测结论」的 05b 小节（Task 5） |

**不碰**：`tests/Avalonia.Tests.Common`、`PageHelpers.cs`、`Directory.Packages.props`、`hello-avalonia.slnx`、`README.md`（测试项目一行 05a 已列上 Controls）、`CLAUDE.md`。

**代码块抽取**：`CLAUDE.md` 提到的 `C:\Temp\extract-plan.ps1` 不存在。按 `#### \`路径\`` 标注手动 Write，或直接从 `C:\Temp\probe-b5b\` 复制同名文件（内容与本计划一致）。

---

### Task 1: Data display 与 Feedback 的 16 个控件页，并修复竖向进度条（缺陷 #6）

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/ControlDataFeedbackTests.cs`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Feedback/ProgressBarPage.axaml.cs:23-29`

**Interfaces:**
- Consumes（05a，`tests/Avalonia.ControlsDemo.Tests/PageHelpers.cs`）：`PageHelpers.Open<TPage>() : (Window Window, TPage Page)`、`Text(this Visual, string) : string`、`Press(this Window, string name)`、`PressText(this Window, string text)`。
- Consumes（共享库 `tests/Avalonia.Tests.Common`）：`UiHelpers.ByName<T>`、`Click`、`WaitUntil(Func<bool>, int = 5000) : Task`、`CultureScope.Invariant() : IDisposable`；`Avalonia.Headless` 的 `MouseDown`/`MouseMove`/`MouseUp`/`KeyPress`/`KeyRelease`。
- Consumes（`src/`，均为 `public`）：16 个控件页的 `x:Name` 控件（名字见各测试）；`NotificationPage.Manager : WindowNotificationManager?`；`DemoProduct.Name`、`DemoTreeNode.Label`。
- Produces：无（文件内私有辅助 `Texts(Visual) : string[]`、`Cards(Window) : NotificationCard[]`）。

逐页覆盖：`CarouselPage` 2、`ItemsControlPage` 2、`ListBoxPage` 2、`ContentControlPage` 1、`DataGridPage` 2、`TableViewPage` 1、`TreeViewPage` 2、`LabelPage` 1、`SelectableTextBlockPage` 2、`TextBlockPage` 1、`TextTrimmingPage` 1、`TransitioningContentControlPage` 2、`NotificationPage` 4、`PopupPage` 3、`ProgressBarPage` 3（含缺陷测试）、`ToolTipPage` 2，共 31。

几处容易写错的地方（都在探针里踩过）：

- 通知卡片在叠加层里按**发送顺序**排列（旧的在前）；关闭全部要 `WaitUntil`。
- `TextBlock.TextAlignment` 的默认值是 `Start`，不是 `Left`。
- 访问键 Alt+N：先单独按下 Alt，再带 `RawInputModifiers.Alt` 按 N。
- `TransitioningContentControl` 的 `TransitionCompleted` 走真实时间（400 ms），用 `WaitUntil` 等；页面跳过"初始内容那一次"报告是对的。
- `{0:C}` 在不变区域设置下是 `¤1.20`；`decimal` 的 `3.5`、`5.0` 原样。

- [ ] **Step 1: 放入测试文件**

#### `tests/Avalonia.ControlsDemo.Tests/ControlDataFeedbackTests.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Data display and Feedback.
    public class ControlDataFeedbackTests
    {
        private static string[] Texts(Visual root) => root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();

        private static NotificationCard[] Cards(Window window) => window.GetVisualDescendants().OfType<NotificationCard>().ToArray();

        // ---- Data display ----

        [AvaloniaFact]
        public void Carousel_buttons_step_through_the_slides_and_stop_at_both_ends()
        {
            var (window, _) = PageHelpers.Open<CarouselPage>();
            var slides = window.ByName<Carousel>("Slides");
            Assert.Equal("第 1 / 3 张", window.Text("IndexText"));

            window.Press("PreviousButton");
            Assert.Equal(0, slides.SelectedIndex);
            Assert.Equal("第 1 / 3 张", window.Text("IndexText"));

            window.Press("NextButton");
            Assert.Equal("第 2 / 3 张", window.Text("IndexText"));
            window.Press("NextButton");
            window.Press("NextButton");
            Assert.Equal(2, slides.SelectedIndex);
            Assert.Equal("第 3 / 3 张", window.Text("IndexText"));

            window.Press("PreviousButton");
            Assert.Equal("第 2 / 3 张", window.Text("IndexText"));
        }

        [AvaloniaFact]
        public void Carousel_transition_box_and_swipe_check_set_the_carousel()
        {
            var (window, _) = PageHelpers.Open<CarouselPage>();
            var slides = window.ByName<Carousel>("Slides");
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.Equal("无", box.SelectedItem);
            Assert.Null(slides.PageTransition);
            Assert.False(slides.IsSwipeEnabled);

            box.SelectedIndex = 1;
            Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.IsType<PageSlide>(slides.PageTransition).Duration);
            box.SelectedIndex = 2;
            Assert.IsType<CrossFade>(slides.PageTransition);
            box.SelectedIndex = 0;
            Assert.Null(slides.PageTransition);

            window.Click(window.ByName<CheckBox>("SwipeCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(slides.IsSwipeEnabled);
        }

        [AvaloniaFact]
        public void Items_control_buttons_add_and_remove_items_and_the_count_follows()
        {
            var (window, _) = PageHelpers.Open<ItemsControlPage>();
            var items = window.ByName<ItemsControl>("Items");
            Assert.Equal("ItemCount = 3", window.Text("CountText"));

            window.Press("AddButton");
            window.Press("AddButton");
            Assert.Equal(new[] { "Apple", "Bread", "Cherry", "新项 1", "新项 2" }, items.Items.Cast<string>());
            Assert.Equal("ItemCount = 5", window.Text("CountText"));

            for (int i = 0; i < 6; i++)
                window.Press("RemoveButton");
            Assert.Equal(0, items.ItemCount);
            Assert.Equal("ItemCount = 0", window.Text("CountText"));

            // The counter keeps going after the list was emptied.
            window.Press("AddButton");
            Assert.Equal(new[] { "新项 3" }, items.Items.Cast<string>());
        }

        [AvaloniaFact]
        public void Items_panel_box_swaps_the_panel_the_items_are_laid_out_in()
        {
            var (window, _) = PageHelpers.Open<ItemsControlPage>();
            var items = window.ByName<ItemsControl>("Items");
            Assert.IsType<StackPanel>(items.ItemsPanelRoot);

            window.ByName<ComboBox>("PanelBox").SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.IsType<WrapPanel>(items.ItemsPanelRoot);
            // In a WrapPanel the three short items share the first row.
            Assert.Single(Enumerable.Range(0, 3).Select(i => items.ContainerFromIndex(i)!.Bounds.Y).Distinct());
        }

        [AvaloniaFact]
        public void List_box_select_all_needs_multiple_mode_and_single_mode_keeps_one()
        {
            var (window, _) = PageHelpers.Open<ListBoxPage>();
            var list = window.ByName<ListBox>("List");
            var mode = window.ByName<ComboBox>("ModeBox");
            Assert.Equal(SelectionMode.Single, mode.SelectedItem);
            Assert.Equal("已选 0 项", window.Text("SelectionText"));

            window.Click(list.ContainerFromIndex(1)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("已选 1 项", window.Text("SelectionText"));
            Assert.Equal("第 2 项", list.SelectedItem);

            mode.SelectedItem = SelectionMode.Multiple;
            window.Press("SelectAllButton");
            Assert.Equal("已选 20 项", window.Text("SelectionText"));

            mode.SelectedItem = SelectionMode.Single;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("已选 1 项", window.Text("SelectionText"));

            window.Press("ClearButton");
            Assert.Equal("已选 0 项", window.Text("SelectionText"));
        }

        [AvaloniaFact]
        public void List_box_jump_button_brings_the_last_item_into_view()
        {
            var (window, _) = PageHelpers.Open<ListBoxPage>();
            var list = window.ByName<ListBox>("List");
            Assert.Null(list.ContainerFromIndex(19));

            window.Press("JumpButton");
            window.UpdateLayout();

            Assert.NotNull(list.ContainerFromIndex(19));
            Assert.Null(list.ContainerFromIndex(0));
        }

        [AvaloniaFact]
        public void Content_control_draws_a_string_a_control_and_a_templated_object_differently()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ContentControlPage>();
            var host = window.ByName<ContentControl>("Host");
            Assert.Equal("Content 的类型：（未设置）", window.Text("TypeText"));
            Assert.Empty(Texts(host));

            window.PressText("Content = 字符串");
            Assert.Equal("Content 的类型：String", window.Text("TypeText"));
            Assert.Equal(new[] { "你好，ContentControl" }, Texts(host));

            window.PressText("Content = 控件");
            Assert.Equal("Content 的类型：Border", window.Text("TypeText"));
            Assert.Same(host.Content, host.GetVisualDescendants().OfType<Border>().First());
            Assert.Equal(new[] { "我是一个 Border" }, Texts(host));

            window.PressText("Content = 数据对象");
            Assert.Equal("Content 的类型：DemoProduct", window.Text("TypeText"));
            Assert.Equal(new[] { "Apple", "¤1.20" }, Texts(host));
        }

        [AvaloniaFact]
        public void Data_grid_selection_shows_the_row_and_the_option_boxes_set_the_grid()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<DataGridPage>();
            var grid = window.ByName<DataGrid>("Grid");
            Assert.Equal("选中：（无）", window.Text("SelectedText"));

            var row = grid.GetVisualDescendants().OfType<DataGridRow>().First(r => ((DemoProduct)r.DataContext!).Name == "Bread");
            window.Click(row);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中：Bread（Bakery，3.5）", window.Text("SelectedText"));

            window.ByName<ComboBox>("LinesBox").SelectedItem = DataGridGridLinesVisibility.All;
            window.ByName<ComboBox>("SelectionBox").SelectedItem = DataGridSelectionMode.Single;
            Assert.Equal(DataGridGridLinesVisibility.All, grid.GridLinesVisibility);
            Assert.Equal(DataGridSelectionMode.Single, grid.SelectionMode);

            Assert.False(grid.IsReadOnly);
            window.Click(window.ByName<CheckBox>("ReadOnlyCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(grid.IsReadOnly);
            Assert.False(grid.CanUserReorderColumns);
            window.Click(window.ByName<CheckBox>("ReorderCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(grid.CanUserReorderColumns);
        }

        [AvaloniaFact]
        public void Clicking_a_data_grid_header_sorts_the_rows()
        {
            var (window, _) = PageHelpers.Open<DataGridPage>();
            var grid = window.ByName<DataGrid>("Grid");
            string?[] Names()
            {
                window.UpdateLayout();
                return grid.GetVisualDescendants().OfType<DataGridRow>().OrderBy(r => r.Index)
                    .Select(r => (r.DataContext as DemoProduct)?.Name).ToArray();
            }
            Assert.Equal("Apple", Names().First());

            window.Click(grid.GetVisualDescendants().OfType<DataGridColumnHeader>().First(h => "价格".Equals(h.Content)));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new[] { "Banana", "Apple", "Bagel" }, Names().Take(3));
        }

        [AvaloniaFact]
        public void Table_view_selection_shows_the_row_and_the_options_set_the_table()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<TableViewPage>();
            var table = window.ByName<TableView>("Table");
            var resize = window.ByName<CheckBox>("ResizeCheck");
            Assert.Equal("选中：（无）", window.Text("SelectedText"));
            Assert.Equal(table.CanUserResizeColumns, resize.IsChecked);

            window.Click(table.ContainerFromIndex(2)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中：Cherry（Fruit，5.0）", window.Text("SelectedText"));

            window.ByName<ComboBox>("ModeBox").SelectedItem = SelectionMode.Multiple;
            Assert.Equal(SelectionMode.Multiple, table.SelectionMode);

            bool before = table.CanUserResizeColumns;
            window.Click(resize);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(!before, table.CanUserResizeColumns);
        }

        [AvaloniaFact]
        public void Tree_buttons_expand_and_collapse_every_level_and_selection_names_the_node()
        {
            var (window, _) = PageHelpers.Open<TreeViewPage>();
            var tree = window.ByName<TreeView>("Tree");
            string[] Visible()
            {
                window.UpdateLayout();
                return tree.GetVisualDescendants().OfType<TreeViewItem>().Where(i => i.IsEffectivelyVisible)
                    .Select(i => ((DemoTreeNode)i.DataContext!).Label).ToArray();
            }
            Assert.Equal(new[] { "文档", "图片" }, Visible());
            Assert.Equal("选中：（无）", window.Text("SelectedText"));

            window.Press("ExpandAllButton");
            Assert.Equal(new[] { "文档", "报告", "2025 年报", "2026 季报", "合同", "图片", "旅行" }, Visible());

            window.Click(tree.GetVisualDescendants().OfType<TreeViewItem>().First(i => ((DemoTreeNode)i.DataContext!).Label == "2026 季报"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("选中：2026 季报", window.Text("SelectedText"));

            window.Press("CollapseAllButton");
            Assert.Equal(new[] { "文档", "图片" }, Visible());
        }

        [AvaloniaFact]
        public void Tree_mode_box_sets_the_selection_mode()
        {
            var (window, _) = PageHelpers.Open<TreeViewPage>();
            var box = window.ByName<ComboBox>("ModeBox");
            Assert.Equal(SelectionMode.Single, box.SelectedItem);

            box.SelectedItem = SelectionMode.Multiple;
            Assert.Equal(SelectionMode.Multiple, window.ByName<TreeView>("Tree").SelectionMode);
        }

        [AvaloniaFact]
        public void Alt_n_on_the_label_moves_focus_to_its_target_box()
        {
            var (window, _) = PageHelpers.Open<LabelPage>();
            var box = window.ByName<TextBox>("NameBox");
            Assert.Equal("输入框：未聚焦", window.Text("FocusText"));

            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            window.KeyPress(Key.N, RawInputModifiers.Alt, PhysicalKey.N, "n");
            window.KeyRelease(Key.N, RawInputModifiers.Alt, PhysicalKey.N, "n");
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(box.IsFocused);
            Assert.Equal("输入框：已聚焦", window.Text("FocusText"));
        }

        [AvaloniaFact]
        public void Selection_buttons_select_and_clear_text_and_the_readout_follows()
        {
            var (window, _) = PageHelpers.Open<SelectableTextBlockPage>();
            var selectable = window.ByName<SelectableTextBlock>("Selectable");
            Assert.Equal("SelectedText：（空）", window.Text("SelectionText"));

            window.Press("FirstWordButton");
            Assert.Equal("SelectedText：Hello", window.Text("SelectionText"));

            window.Press("SelectAllButton");
            Assert.Equal("SelectedText：Hello Avalonia：用鼠标拖选这段文字，按 Ctrl+C 复制。", window.Text("SelectionText"));
            Assert.Equal(selectable.Text, selectable.SelectedText);

            window.Press("ClearButton");
            Assert.Equal("SelectedText：（空）", window.Text("SelectionText"));
            Assert.Equal("", selectable.SelectedText);
        }

        [AvaloniaFact]
        public void Dragging_across_the_text_selects_it_and_the_release_updates_the_readout()
        {
            var (window, _) = PageHelpers.Open<SelectableTextBlockPage>();
            var selectable = window.ByName<SelectableTextBlock>("Selectable");
            var start = selectable.TranslatePoint(new Point(1, 8), window)!.Value;
            var end = selectable.TranslatePoint(new Point(60, 8), window)!.Value;

            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            window.MouseUp(end, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.False(string.IsNullOrEmpty(selectable.SelectedText));
            Assert.StartsWith("H", selectable.SelectedText);
            Assert.Equal($"SelectedText：{selectable.SelectedText}", window.Text("SelectionText"));
        }

        [AvaloniaFact]
        public void Text_block_sliders_and_boxes_set_the_sample()
        {
            var (window, _) = PageHelpers.Open<TextBlockPage>();
            var sample = window.ByName<TextBlock>("Sample");
            Assert.Equal(TextWrapping.NoWrap, window.ByName<ComboBox>("WrapBox").SelectedItem);
            Assert.Equal(TextAlignment.Start, window.ByName<ComboBox>("AlignBox").SelectedItem);
            double oneLine = sample.Bounds.Height;

            window.ByName<Slider>("SizeSlider").Value = 20;
            Assert.Equal(20, sample.FontSize);

            window.ByName<ComboBox>("WrapBox").SelectedItem = TextWrapping.Wrap;
            window.UpdateLayout();
            Assert.Equal(TextWrapping.Wrap, sample.TextWrapping);
            double wrapped = sample.Bounds.Height;
            Assert.True(wrapped > oneLine * 2, $"wrapped {wrapped}, one line {oneLine}");

            window.ByName<Slider>("MaxLinesSlider").Value = 1;
            window.UpdateLayout();
            Assert.Equal(1, sample.MaxLines);
            Assert.True(sample.Bounds.Height < wrapped, $"MaxLines 1 {sample.Bounds.Height}, wrapped {wrapped}");

            window.ByName<ComboBox>("AlignBox").SelectedItem = TextAlignment.Center;
            Assert.Equal(TextAlignment.Center, sample.TextAlignment);
        }

        [AvaloniaFact]
        public void Trimming_controls_set_width_and_mode_while_the_text_stays_whole()
        {
            var (window, _) = PageHelpers.Open<TextTrimmingPage>();
            var trimmed = window.ByName<TextBlock>("Trimmed");
            var box = window.ByName<ComboBox>("TrimBox");
            const string path = @"C:\Users\Administrator\Documents\Reports\2026\summary-final.docx";
            Assert.Equal("CharacterEllipsis", box.SelectedItem);
            Assert.Same(TextTrimming.CharacterEllipsis, trimmed.TextTrimming);
            Assert.Equal($"Text 长度 = {path.Length}（无论怎么截都不变）", window.Text("LengthText"));

            window.ByName<Slider>("WidthSlider").Value = 200;
            Assert.Equal(200, trimmed.Width);

            box.SelectedItem = "PathSegmentEllipsis";
            Assert.Same(TextTrimming.PathSegmentEllipsis, trimmed.TextTrimming);
            box.SelectedItem = "None";
            Assert.Same(TextTrimming.None, trimmed.TextTrimming);
            Assert.Equal(path, trimmed.Text);
        }

        [AvaloniaFact]
        public async Task Next_button_changes_the_page_and_counts_only_real_transitions()
        {
            var (window, _) = PageHelpers.Open<TransitioningContentControlPage>();
            var host = window.ByName<TransitioningContentControl>("Host");
            Assert.Equal("已完成 0 次过渡", window.Text("DoneText"));

            window.Press("NextButton");
            Assert.Equal("第 2 页", host.Content);
            await UiHelpers.WaitUntil(() => window.Text("DoneText") == "已完成 1 次过渡");

            window.Press("NextButton");
            Assert.Equal("第 3 页", host.Content);
            await UiHelpers.WaitUntil(() => window.Text("DoneText") == "已完成 2 次过渡");
        }

        [AvaloniaFact]
        public void Transition_box_and_reverse_check_set_the_host()
        {
            var (window, _) = PageHelpers.Open<TransitioningContentControlPage>();
            var host = window.ByName<TransitioningContentControl>("Host");
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.Equal("PageSlide", box.SelectedItem);
            Assert.Equal(TimeSpan.FromMilliseconds(400), Assert.IsType<PageSlide>(host.PageTransition).Duration);

            box.SelectedIndex = 1;
            Assert.IsType<CrossFade>(host.PageTransition);
            box.SelectedIndex = 2;
            Assert.IsType<Rotate3DTransition>(host.PageTransition);

            Assert.False(host.IsTransitionReversed);
            window.Click(window.ByName<CheckBox>("ReverseCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(host.IsTransitionReversed);
        }

        // ---- Feedback ----

        [AvaloniaFact]
        public async Task Send_button_shows_a_card_of_the_chosen_type_and_close_all_removes_them()
        {
            var (window, _) = PageHelpers.Open<NotificationPage>();
            try
            {
                Assert.Empty(Cards(window));
                Assert.Equal("已发送 0 条，点击 0 次", window.Text("SentText"));

                window.Press("ShowButton");
                window.ByName<ComboBox>("TypeBox").SelectedItem = NotificationType.Warning;
                window.Press("ShowButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 2);

                Assert.Equal("已发送 2 条，点击 0 次", window.Text("SentText"));
                var cards = Cards(window);
                Assert.Equal(new[] { NotificationType.Information, NotificationType.Warning }, cards.Select(c => c.NotificationType));
                Assert.Equal(new[] { "第 2 条", "这是一条 Warning 通知" }, Texts(cards[1]));

                window.Press("CloseAllButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 0);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Clicking_a_clickable_card_counts_the_click()
        {
            var (window, _) = PageHelpers.Open<NotificationPage>();
            try
            {
                window.Press("ClickableButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 1);
                Assert.Equal("已发送 1 条，点击 0 次", window.Text("SentText"));

                window.Click(Cards(window)[0]);
                Dispatcher.UIThread.RunJobs();

                Assert.Equal("已发送 1 条，点击 1 次", window.Text("SentText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Max_items_slider_caps_the_cards_and_the_oldest_one_goes()
        {
            var (window, page) = PageHelpers.Open<NotificationPage>();
            try
            {
                Assert.Equal(3, page.Manager!.MaxItems);
                window.ByName<Slider>("MaxItemsSlider").Value = 1;
                Assert.Equal("1", window.Text("MaxItemsText"));
                Assert.Equal(1, page.Manager.MaxItems);

                window.Press("ShowButton");
                window.Press("ShowButton");
                await UiHelpers.WaitUntil(() => Cards(window).Length == 1);

                Assert.Equal("第 2 条", Texts(Cards(window)[0])[0]);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Position_box_and_seconds_slider_reach_the_manager_and_the_readout()
        {
            var (window, page) = PageHelpers.Open<NotificationPage>();
            try
            {
                Assert.Equal(NotificationPosition.TopRight, page.Manager!.Position);
                window.ByName<ComboBox>("PositionBox").SelectedItem = NotificationPosition.BottomLeft;
                Assert.Equal(NotificationPosition.BottomLeft, page.Manager.Position);

                window.ByName<Slider>("SecondsSlider").Value = 7;
                Assert.Equal("7", window.Text("SecondsText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Popup_toggle_opens_and_closes_and_counts_the_openings()
        {
            var (window, _) = PageHelpers.Open<PopupPage>();
            var pop = window.ByName<Popup>("Pop");
            int Shown() => window.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "我是 Popup 里的内容");
            Assert.Equal("IsOpen = False，打开 0 次", window.Text("StateText"));
            Assert.Equal(0, Shown());

            window.Press("ToggleButton");
            Assert.True(pop.IsOpen);
            Assert.Equal("IsOpen = True，打开 1 次", window.Text("StateText"));
            Assert.Equal(1, Shown());

            window.Press("ToggleButton");
            Assert.False(pop.IsOpen);
            Assert.Equal("IsOpen = False，打开 1 次", window.Text("StateText"));
            Assert.Equal(0, Shown());

            window.Press("ToggleButton");
            Assert.Equal("IsOpen = True，打开 2 次", window.Text("StateText"));
            window.Press("ToggleButton");
        }

        [AvaloniaFact]
        public void Clicking_outside_closes_the_popup_only_with_light_dismiss()
        {
            var (window, _) = PageHelpers.Open<PopupPage>();
            var pop = window.ByName<Popup>("Pop");
            var outside = window.ByName<TextBlock>("StateText");

            window.Press("ToggleButton");
            window.Click(outside);
            Dispatcher.UIThread.RunJobs();
            Assert.True(pop.IsOpen);
            window.Press("ToggleButton");

            window.Click(window.ByName<CheckBox>("LightDismissCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.True(pop.IsLightDismissEnabled);
            window.Press("ToggleButton");
            Assert.True(pop.IsOpen);
            window.Click(outside);
            Dispatcher.UIThread.RunJobs();

            Assert.False(pop.IsOpen);
            Assert.Equal("IsOpen = False，打开 2 次", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Popup_placement_box_sets_the_placement()
        {
            var (window, _) = PageHelpers.Open<PopupPage>();
            var box = window.ByName<ComboBox>("PlacementBox");
            Assert.Equal(PlacementMode.Bottom, box.SelectedItem);

            box.SelectedItem = PlacementMode.Right;
            Assert.Equal(PlacementMode.Right, window.ByName<Popup>("Pop").Placement);
        }

        [AvaloniaFact]
        public void Value_slider_drives_both_bars_and_their_percentages()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ProgressBarPage>();
            Assert.Equal("Value = 40，Percentage = 40%", window.Text("PercentText"));
            Assert.Equal("Value = -10，Percentage = 40%", window.Text("RangeText"));

            window.ByName<Slider>("ValueSlider").Value = 75;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Equal(75, window.ByName<ProgressBar>("Bar").Value);
            Assert.Equal("Value = 75，Percentage = 75%", window.Text("PercentText"));
            Assert.Equal("Value = 25，Percentage = 75%", window.Text("RangeText"));
        }

        [AvaloniaFact]
        public void Progress_check_boxes_set_indeterminate_text_and_orientation()
        {
            var (window, _) = PageHelpers.Open<ProgressBarPage>();
            var bar = window.ByName<ProgressBar>("Bar");
            void Check(string name)
            {
                window.Click(window.ByName<CheckBox>(name));
                Dispatcher.UIThread.RunJobs();
            }

            Check("IndeterminateCheck");
            Assert.True(bar.IsIndeterminate);
            Check("TextCheck");
            Assert.True(bar.ShowProgressText);

            Check("VerticalCheck");
            window.UpdateLayout();
            Assert.Equal(Orientation.Vertical, bar.Orientation);
            Assert.Equal(20, bar.Bounds.Width);

            Check("VerticalCheck");
            window.UpdateLayout();
            Assert.Equal(Orientation.Horizontal, bar.Orientation);
            Assert.Equal(280, bar.Bounds.Width);
        }

        // The Fluent theme gives a vertical ProgressBar MinHeight = 200, which beats a plain Height = 100: the page
        // has to lift MinHeight too, or the bar comes out 200 tall and overflows its 120-high frame.
        [AvaloniaFact]
        public void Vertical_bar_takes_the_height_the_page_sets()
        {
            var (window, _) = PageHelpers.Open<ProgressBarPage>();
            var bar = window.ByName<ProgressBar>("Bar");

            window.Click(window.ByName<CheckBox>("VerticalCheck"));
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Equal(new Size(20, 100), bar.Bounds.Size);
        }

        [AvaloniaFact]
        public void Tooltip_open_button_toggles_the_tip_and_the_readout()
        {
            var (window, _) = PageHelpers.Open<ToolTipPage>();
            var target = window.ByName<Button>("Target");
            int Shown() => window.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "我是简单的文字提示");
            Assert.Equal("IsOpen = False", window.Text("StateText"));
            Assert.Equal(0, Shown());

            window.Press("OpenButton");
            Assert.True(ToolTip.GetIsOpen(target));
            Assert.Equal("IsOpen = True", window.Text("StateText"));
            Assert.Equal(1, Shown());

            window.Press("OpenButton");
            Assert.False(ToolTip.GetIsOpen(target));
            Assert.Equal("IsOpen = False", window.Text("StateText"));
            Assert.Equal(0, Shown());
        }

        [AvaloniaFact]
        public void Delay_slider_and_placement_box_set_the_target_tip()
        {
            var (window, _) = PageHelpers.Open<ToolTipPage>();
            var target = window.ByName<Button>("Target");
            Assert.Equal(PlacementMode.Pointer, window.ByName<ComboBox>("PlacementBox").SelectedItem);

            window.ByName<Slider>("DelaySlider").Value = 1000;
            Assert.Equal("1000", window.Text("DelayText"));
            Assert.Equal(1000, ToolTip.GetShowDelay(target));

            window.ByName<ComboBox>("PlacementBox").SelectedItem = PlacementMode.Top;
            Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(target));
        }
    }
}
```

- [ ] **Step 2: 运行，确认只有缺陷测试失败**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlDataFeedbackTests"`
Expected: 31 个里 30 过、1 个失败——`Vertical_bar_takes_the_height_the_page_sets`，`Assert.Equal() Failure`，Expected `20, 100`，Actual `20, 200`。若有别的失败，先对照 `C:\Temp\probe-b5b\ControlDataFeedbackTests.cs` 查是否抄错。

- [ ] **Step 3: 修复竖向进度条**

`src/Avalonia.ControlsDemo/Views/Pages/Feedback/ProgressBarPage.axaml.cs`，把

```csharp
            VerticalCheck.IsCheckedChanged += (_, _) =>
            {
                var vertical = VerticalCheck.IsChecked == true;
                Bar.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
                Bar.Width = vertical ? 20 : 280;
                Bar.Height = vertical ? 100 : double.NaN;
            };
```

改成

```csharp
            VerticalCheck.IsCheckedChanged += (_, _) =>
            {
                var vertical = VerticalCheck.IsChecked == true;
                Bar.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
                Bar.Width = vertical ? 20 : 280;
                Bar.Height = vertical ? 100 : double.NaN;
                // The Fluent :vertical style sets MinHeight = 200, which beats Height. Clear it again when going back
                // so no local value is left to override the theme.
                if (vertical)
                {
                    Bar.MinHeight = 0;
                }
                else
                {
                    Bar.ClearValue(MinHeightProperty);
                }
            };
```

- [ ] **Step 4: 运行，全绿**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlDataFeedbackTests"`
Expected: 31 passed，0 failed。

Run: `dotnet build tests/Avalonia.ControlsDemo.Tests 2>&1 | grep -E "ControlDataFeedbackTests|ProgressBarPage" | grep -c warning`
Expected: `0`。

- [ ] **Step 5: 提交**

```bash
git add tests/Avalonia.ControlsDemo.Tests/ControlDataFeedbackTests.cs src/Avalonia.ControlsDemo/Views/Pages/Feedback/ProgressBarPage.axaml.cs
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add interaction tests for the data display and feedback control pages and fix the vertical progress bar

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

各改一行，跑 `--filter "FullyQualifiedName~ControlDataFeedbackTests"`，预期恰好 1 个失败：

| 临时改动 | 预期失败的测试 |
|---|---|
| `LabelPage.axaml` 删掉 `Target="{Binding #NameBox}"` | `Alt_n_on_the_label_moves_focus_to_its_target_box`（Expected True，Actual False） |
| `ListBoxPage.axaml.cs` 的 `ScrollIntoView(19)` → `(10)` | `List_box_jump_button_brings_the_last_item_into_view` |
| `TransitioningContentControlPage.axaml.cs` 的 `if (!_initialSeen)` → `if (false)` | `Next_button_changes_the_page_and_counts_only_real_transitions`（Actual `已完成 1 次过渡`） |
| `PopupPage.axaml.cs:28` 的 `LightDismissCheck.IsChecked == true` → `== false` | `Clicking_outside_closes_the_popup_only_with_light_dismiss` |
| `ProgressBarPage.axaml.cs` 去掉 `Bar.MinHeight = 0;` | `Vertical_bar_takes_the_height_the_page_sets` |

Run: `git status --short src`
Expected: 无输出。

### Task 2: Input 的 22 个控件页，并修复日期格式崩溃、三态读数与父复选框（缺陷 #1、#4、#5）

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/ControlInputTests.cs`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Input/CalendarDatePickerPage.axaml.cs:21`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Input/ToggleButtonPage.axaml:29`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml:22`

**Interfaces:**
- Consumes：同 Task 1（`PageHelpers`、`UiHelpers.Click`/`Type`/`ByName`、`CultureScope`、Headless 键鼠扩展）。
- Consumes（`src/`）：22 个控件页的 `x:Name` 控件；模板部件名 `PART_IncreaseButton`/`PART_DecreaseButton`（ButtonSpinner、NumericUpDown）、`PART_PrimaryButton`/`PART_SecondaryButton`（SplitButton、ToggleSplitButton）、`PART_DayTextBlock`/`PART_MonthTextBlock`/`PART_YearTextBlock`（DatePicker）、`PART_HourTextBlock`/`PART_SecondTextBlock`/`PART_PeriodTextBlock`（TimePicker）、`PART_OnContentPresenter`/`PART_OffContentPresenter`（ToggleSwitch）。
- Produces：无（文件内私有辅助 `Center`、`PressKey`、`ClickPaletteSwatch`、`TypeEach`）。

逐页覆盖：`ButtonPage` 2、`ButtonSpinnerPage` 2、`HyperlinkButtonPage` 1、`RadioButtonPage` 1、`RepeatButtonPage` 1、`SplitButtonPage` 1、`ToggleButtonPage` 2（含缺陷 #4）、`ToggleSplitButtonPage` 1、`CalendarPage` 4、`CalendarDatePickerPage` 3（含缺陷 #1）、`DatePickerPage` 2、`TimePickerPage` 1、`CheckBoxPage` 3（含缺陷 #5）、`ColorPickerPage` 2、`ColorViewPage` 1、`ComboBoxPage` 2、`NumericUpDownPage` 2、`SliderPage` 2、`ToggleSwitchPage` 1、`AutoCompleteBoxPage` 1、`MaskedTextBoxPage` 2、`TextBoxPage` 5，共 42。

几处容易写错的地方（都在探针里踩过）：

- 第一个 `Calendar` 显示的是**今天**所在的月，只通过 `SelectedDates` 驱动；真实点击日期只用固定在 2026 年 1 月的 `BlackoutCal`。
- `RepeatButton.Interval` 由 `DoubleToInt` 截断（50.6 → 50），旁边的 `F0` 读数却四舍五入成 `51`。
- `Ticked` 滑块的读数一开始是 XAML 里的字面量 `Value = 0`，第一次变化后才是 `F1` 格式；避开 `F1` 的舍入平局值（42.25 → `42.2`）。
- `ColorView` 的调色板是模板里的第二个 `TabItem`（没有名字、表头是图标），按序号选。
- 多行框里按 Enter 插入的是 `Environment.NewLine`，XAML 里的初始文字用的是 `\n`，结果两种换行并存。
- **`NumericUpDown.NumberFormat` 的默认值在该类型第一次被使用时取一次当时的区域设置**，之后不变，`CultureScope` 管不到它（执行时发现：探针里碰巧先跑了带 `CultureScope` 的测试才得到 `¤12.50`，换个顺序就是本机的 `¥12.50`）。期望文字用控件自己的 `NumberFormat` 格式化。
- `AutoCompleteBoxPage` 提示说 Contains 模式下 `rr` 只匹配 Cherry，实际是 `Blueberry`、`Cherry`；只是提示措辞，不算缺陷，不改。

- [ ] **Step 1: 放入测试文件**

#### `tests/Avalonia.ControlsDemo.Tests/ControlInputTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Input.
    public class ControlInputTests
    {
        private static Point Center(Window window, Control control)
        {
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            return control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        }

        // Keys go to the focused element, so focus the target first.
        private static void PressKey(Window window, Control target, Key key, PhysicalKey physical)
        {
            target.Focus();
            window.KeyPress(key, RawInputModifiers.None, physical, null);
            window.KeyRelease(key, RawInputModifiers.None, physical, null);
            Dispatcher.UIThread.RunJobs();
        }

        // ---- Buttons ----

        [AvaloniaFact]
        public void Click_button_counts_every_click()
        {
            var (window, _) = PageHelpers.Open<ButtonPage>();
            try
            {
                Assert.Equal("点击次数：0", window.Text("ClickCount"));
                window.Press("ClickButton");
                window.Press("ClickButton");
                Assert.Equal("点击次数：2", window.Text("ClickCount"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Press_mode_clicks_on_mouse_down_and_release_mode_on_mouse_up()
        {
            var (window, _) = PageHelpers.Open<ButtonPage>();
            try
            {
                var press = Center(window, window.ByName<Button>("PressButton"));
                window.MouseDown(press, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("1", window.Text("PressCount"));
                window.MouseUp(press, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("1", window.Text("PressCount"));

                var release = Center(window, window.ByName<Button>("ReleaseButton"));
                window.MouseDown(release, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("0", window.Text("ReleaseCount"));
                window.MouseUp(release, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("1", window.Text("ReleaseCount"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Spinner_buttons_report_a_direction_and_the_page_keeps_the_number()
        {
            var (window, _) = PageHelpers.Open<ButtonSpinnerPage>();
            try
            {
                var spinner = window.ByName<ButtonSpinner>("CounterSpinner");
                Assert.Equal("尚未微调", window.Text("SpinLog"));

                window.Click(spinner.ByName<RepeatButton>("PART_IncreaseButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("1", window.Text("CounterText"));
                Assert.Equal("最近一次：Increase", window.Text("SpinLog"));

                window.Click(spinner.ByName<RepeatButton>("PART_DecreaseButton"));
                window.Click(spinner.ByName<RepeatButton>("PART_DecreaseButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("-1", window.Text("CounterText"));
                Assert.Equal("最近一次：Decrease", window.Text("SpinLog"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Allow_spin_off_disables_the_buttons_and_show_button_spinner_off_hides_them()
        {
            var (window, _) = PageHelpers.Open<ButtonSpinnerPage>();
            try
            {
                var noSpin = window.ByName<ButtonSpinner>("NoSpinSpinner").ByName<RepeatButton>("PART_IncreaseButton");
                var hidden = window.ByName<ButtonSpinner>("HiddenSpinner").ByName<RepeatButton>("PART_IncreaseButton");
                Assert.True(noSpin.IsEffectivelyVisible);
                Assert.False(noSpin.IsEffectivelyEnabled);
                Assert.False(hidden.IsEffectivelyVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // The NavigateUri link only reaches the platform Launcher, so only the plain link is driven here.
        [AvaloniaFact]
        public void Hyperlink_without_uri_works_as_a_plain_button()
        {
            var (window, _) = PageHelpers.Open<HyperlinkButtonPage>();
            try
            {
                window.Press("PlainLink");
                window.Press("PlainLink");
                Assert.Equal("2", window.Text("PlainCount"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Radio_buttons_are_exclusive_within_a_group_only()
        {
            var (window, _) = PageHelpers.Open<RadioButtonPage>();
            try
            {
                Assert.Equal("当前选择：中", window.Text("SizeResult"));
                window.Press("SizeLarge");
                Assert.Equal("当前选择：大", window.Text("SizeResult"));
                Assert.False(window.ByName<RadioButton>("SizeMedium").IsChecked);

                window.Press("A2");
                Assert.False(window.ByName<RadioButton>("A1").IsChecked);
                Assert.True(window.ByName<RadioButton>("B1").IsChecked);
            }
            finally
            {
                window.Close();
            }
        }

        // Real-time repetition is not asserted; only the slider bindings and a single press are deterministic.
        [AvaloniaFact]
        public void Repeat_button_takes_delay_and_interval_from_the_sliders_and_clicks_once_on_a_short_press()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<RepeatButtonPage>();
            try
            {
                var repeater = window.ByName<RepeatButton>("Repeater");
                Assert.Equal((300, 100), (repeater.Delay, repeater.Interval));

                window.ByName<Slider>("DelaySlider").Value = 500;
                window.ByName<Slider>("IntervalSlider").Value = 50.6;
                Dispatcher.UIThread.RunJobs();
                // DoubleToInt truncates while the F0 readout rounds.
                Assert.Equal((500, 50), (repeater.Delay, repeater.Interval));
                Assert.Contains("51", window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));

                var center = Center(window, repeater);
                window.MouseDown(center, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("触发次数：0", window.Text("RepeatCount"));
                window.MouseUp(center, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("触发次数：1", window.Text("RepeatCount"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Split_button_primary_part_clicks_and_the_arrow_opens_the_menu()
        {
            var (window, _) = PageHelpers.Open<SplitButtonPage>();
            try
            {
                var split = window.ByName<SplitButton>("SaveSplit");
                window.Click(split.ByName<Button>("PART_PrimaryButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("执行：保存", window.Text("SplitResult"));
                Assert.Empty(window.GetVisualDescendants().OfType<MenuItem>());

                window.Click(split.ByName<Button>("PART_SecondaryButton"));
                Dispatcher.UIThread.RunJobs();
                var items = window.GetVisualDescendants().OfType<MenuItem>().ToList();
                Assert.Equal(new object?[] { "另存为…", "保存全部" }, items.Select(m => m.Header));
                Assert.Equal("执行：保存", window.Text("SplitResult"));

                window.Click(items[1]);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("执行：保存全部", window.Text("SplitResult"));
                Assert.False(split.Flyout!.IsOpen);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Toggle_buttons_cycle_two_and_three_states()
        {
            var (window, _) = PageHelpers.Open<ToggleButtonPage>();
            try
            {
                var two = window.ByName<ToggleButton>("TwoState");
                var three = window.ByName<ToggleButton>("ThreeState");
                string Readout(ToggleButton button) => ((StackPanel)button.Parent!).Children.OfType<TextBlock>().Single().Text ?? "";
                Assert.Equal("IsChecked = False", Readout(two));

                window.Press("TwoState");
                Assert.Equal("IsChecked = True", Readout(two));
                window.Press("TwoState");
                Assert.Equal("IsChecked = False", Readout(two));

                Assert.Null(three.IsChecked);
                window.Press("ThreeState");
                Assert.Equal("IsChecked = False", Readout(three));
                window.Press("ThreeState");
                Assert.Equal("IsChecked = True", Readout(three));
                window.Press("ThreeState");
                Assert.Null(three.IsChecked);
            }
            finally
            {
                window.Close();
            }
        }

        // FallbackValue only applies when the binding fails; a null IsChecked needs TargetNullValue, or the readout
        // shows "IsChecked = " with nothing after it.
        [AvaloniaFact]
        public void Three_state_toggle_shows_null_when_indeterminate()
        {
            var (window, _) = PageHelpers.Open<ToggleButtonPage>();
            try
            {
                var three = window.ByName<ToggleButton>("ThreeState");
                Assert.Equal("IsChecked = null", ((StackPanel)three.Parent!).Children.OfType<TextBlock>().Single().Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Toggle_split_button_primary_part_toggles_and_the_arrow_only_opens_the_menu()
        {
            var (window, _) = PageHelpers.Open<ToggleSplitButtonPage>();
            try
            {
                var split = window.ByName<ToggleSplitButton>("BoldSplit");
                string Readout() => ((StackPanel)split.Parent!).Children.OfType<TextBlock>().Single().Text ?? "";
                Assert.Equal("IsChecked = False", Readout());

                window.Click(split.ByName<Button>("PART_PrimaryButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("IsChecked = True", Readout());

                window.Click(split.ByName<Button>("PART_SecondaryButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.True(split.Flyout!.IsOpen);
                Assert.Equal(2, window.GetVisualDescendants().OfType<MenuItem>().Count());
                Assert.Equal("IsChecked = True", Readout());
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Date and time ----

        // The first calendar shows today's month, so it is driven through SelectedDates rather than day clicks.
        [AvaloniaFact]
        public void Selection_mode_box_switches_the_calendar_and_clears_its_selection()
        {
            var (window, _) = PageHelpers.Open<CalendarPage>();
            try
            {
                var cal = window.ByName<Calendar>("Cal");
                var modeBox = window.ByName<ComboBox>("ModeBox");
                Assert.Equal(CalendarSelectionMode.SingleDate, modeBox.SelectedItem);
                cal.SelectedDates.Add(new DateTime(2026, 3, 3));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选 1 天", window.Text("SelectionText"));

                modeBox.SelectedItem = CalendarSelectionMode.MultipleRange;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(CalendarSelectionMode.MultipleRange, cal.SelectionMode);
                Assert.Equal("已选 0 天", window.Text("SelectionText"));
                cal.SelectedDates.AddRange(new DateTime(2026, 3, 10), new DateTime(2026, 3, 12));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选 3 天", window.Text("SelectionText"));

                modeBox.SelectedItem = CalendarSelectionMode.None;
                Dispatcher.UIThread.RunJobs();
                Assert.Empty(cal.SelectedDates);
                Assert.Equal("已选 0 天", window.Text("SelectionText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Display_mode_and_first_day_boxes_reach_the_calendars()
        {
            var (window, _) = PageHelpers.Open<CalendarPage>();
            try
            {
                window.ByName<ComboBox>("DisplayModeBox").SelectedItem = CalendarMode.Year;
                window.ByName<ComboBox>("FirstDayBox").SelectedItem = DayOfWeek.Monday;
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(CalendarMode.Year, window.ByName<Calendar>("Cal").DisplayMode);
                Assert.Equal(DayOfWeek.Monday, window.ByName<Calendar>("Cal").FirstDayOfWeek);
                Assert.Equal(DayOfWeek.Monday, window.ByName<Calendar>("BlackoutCal").FirstDayOfWeek);
            }
            finally
            {
                window.Close();
            }
        }

        // The exception text ends with the runtime's "(Parameter 'e')", which is localised by the UI culture,
        // so only the part the page and Avalonia control is compared.
        [AvaloniaFact]
        public void Selecting_a_blackout_date_from_code_throws_and_leaves_selected_dates_empty()
        {
            var (window, _) = PageHelpers.Open<CalendarPage>();
            try
            {
                Assert.Equal("尚未尝试", window.Text("BlackoutResult"));
                window.Press("BlackoutButton");

                var cal = window.ByName<Calendar>("BlackoutCal");
                Assert.StartsWith("抛出 ArgumentOutOfRangeException：SelectedDate value is not valid.", window.Text("BlackoutResult"));
                Assert.Equal(new DateTime(2026, 1, 6), cal.SelectedDate);
                Assert.Empty(cal.SelectedDates);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Clicking_a_blackout_day_is_refused_and_a_normal_day_is_selected()
        {
            var (window, _) = PageHelpers.Open<CalendarPage>();
            try
            {
                var cal = window.ByName<Calendar>("BlackoutCal");
                CalendarDayButton Day(int day) => cal.GetVisualDescendants().OfType<CalendarDayButton>()
                    .Single(d => d.DataContext is DateTime date && date == new DateTime(2026, 1, day));
                Assert.Contains(":blackout", Day(6).Classes);

                window.Click(Day(6));
                Dispatcher.UIThread.RunJobs();
                Assert.Null(cal.SelectedDate);

                window.Click(Day(12));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(new DateTime(2026, 1, 12), cal.SelectedDate);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Calendar_date_picker_reports_the_date_and_formats_it_short_long_or_custom()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<CalendarDatePickerPage>();
            try
            {
                var picker = window.ByName<CalendarDatePicker>("Picker");
                var shown = picker.GetVisualDescendants().OfType<TextBox>().First();
                var formatBox = window.ByName<ComboBox>("FormatBox");
                Assert.Equal(CalendarDatePickerFormat.Short, formatBox.SelectedItem);
                Assert.Equal("未选择", window.Text("PickerResult"));

                picker.SelectedDate = new DateTime(2026, 3, 14);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选：2026-03-14", window.Text("PickerResult"));
                Assert.Equal("03/14/2026", shown.Text);

                formatBox.SelectedItem = CalendarDatePickerFormat.Long;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Saturday, 14 March 2026", shown.Text);

                formatBox.SelectedItem = CalendarDatePickerFormat.Custom;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("2026年3月14日", shown.Text);

                picker.SelectedDate = null;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("未选择", window.Text("PickerResult"));
            }
            finally
            {
                window.Close();
            }
        }

        // Typing replaces the selection, so one Type call swaps the whole format for "QQ" in one TextChanged.
        [AvaloniaFact]
        public void Custom_format_box_drives_the_picker_and_unknown_letters_show_as_is()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<CalendarDatePickerPage>();
            try
            {
                var picker = window.ByName<CalendarDatePicker>("Picker");
                var shown = picker.GetVisualDescendants().OfType<TextBox>().First();
                picker.SelectedDate = new DateTime(2026, 3, 14);
                window.ByName<ComboBox>("FormatBox").SelectedItem = CalendarDatePickerFormat.Custom;
                Dispatcher.UIThread.RunJobs();

                var format = window.ByName<TextBox>("CustomFormat");
                format.Focus();
                format.CaretIndex = format.Text!.Length;
                PressKey(window, format, Key.Back, PhysicalKey.Backspace);
                Assert.Equal("yyyy年M月d", picker.CustomDateFormatString);
                Assert.Equal("2026年3月14", shown.Text);

                format.SelectAll();
                window.Type(format, "QQ");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("QQ", shown.Text);
            }
            finally
            {
                window.Close();
            }
        }

        // CustomDateFormatString rejects "" (ArgumentException) and a lone non-standard letter ("Q") fails while the
        // date is re-formatted (FormatException). Either would escape the TextChanged handler, so the page has to
        // keep the last good format. Exceptions surface inside the input call, so Record.Exception wraps it.
        [AvaloniaFact]
        public void Editing_the_custom_format_box_never_throws()
        {
            var (window, _) = PageHelpers.Open<CalendarDatePickerPage>();
            try
            {
                var picker = window.ByName<CalendarDatePicker>("Picker");
                picker.SelectedDate = new DateTime(2026, 3, 14);
                window.ByName<ComboBox>("FormatBox").SelectedItem = CalendarDatePickerFormat.Custom;
                Dispatcher.UIThread.RunJobs();
                var format = window.ByName<TextBox>("CustomFormat");

                format.Focus();
                format.SelectAll();
                Assert.Null(Record.Exception(() =>
                {
                    window.Type(format, "Q");
                    Dispatcher.UIThread.RunJobs();
                }));

                format.SelectAll();
                Assert.Null(Record.Exception(() =>
                {
                    window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null);
                    Dispatcher.UIThread.RunJobs();
                }));
                Assert.Equal("", format.Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Date_picker_reports_the_selected_date()
        {
            var (window, _) = PageHelpers.Open<DatePickerPage>();
            try
            {
                var picker = window.ByName<DatePicker>("Picker");
                Assert.Equal("未选择", window.Text("PickerResult"));

                picker.SelectedDate = new DateTimeOffset(2026, 3, 14, 0, 0, 0, TimeSpan.Zero);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选：2026-03-14", window.Text("PickerResult"));

                picker.SelectedDate = null;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("未选择", window.Text("PickerResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Column_check_boxes_hide_date_parts_and_the_month_format_box_reformats()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<DatePickerPage>();
            try
            {
                var picker = window.ByName<DatePicker>("FormatPicker");
                picker.SelectedDate = new DateTimeOffset(2026, 3, 14, 0, 0, 0, TimeSpan.Zero);
                Dispatcher.UIThread.RunJobs();
                TextBlock Part(string name) => picker.ByName<TextBlock>(name);
                Assert.Equal("MMMM", window.ByName<ComboBox>("MonthFormatBox").SelectedItem);
                Assert.Equal("March", Part("PART_MonthTextBlock").Text);

                window.Press("DayCheck");
                Assert.False(Part("PART_DayTextBlock").IsEffectivelyVisible);
                Assert.True(Part("PART_YearTextBlock").IsEffectivelyVisible);
                window.Press("YearCheck");
                Assert.False(Part("PART_YearTextBlock").IsEffectivelyVisible);

                window.ByName<ComboBox>("MonthFormatBox").SelectedItem = "MM";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("03", Part("PART_MonthTextBlock").Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Time_picker_follows_clock_seconds_and_minute_step_controls()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<TimePickerPage>();
            try
            {
                var picker = window.ByName<TimePicker>("Picker");
                TextBlock Part(string name) => picker.ByName<TextBlock>(name);
                Assert.Equal("24HourClock", window.ByName<ComboBox>("ClockBox").SelectedItem);

                picker.SelectedTime = new TimeSpan(14, 5, 9);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选：14:05:09", window.Text("PickerResult"));
                Assert.Equal("14", Part("PART_HourTextBlock").Text);
                Assert.False(Part("PART_SecondTextBlock").IsEffectivelyVisible);
                Assert.False(Part("PART_PeriodTextBlock").IsEffectivelyVisible);

                window.ByName<ComboBox>("ClockBox").SelectedItem = "12HourClock";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("2", Part("PART_HourTextBlock").Text);
                Assert.True(Part("PART_PeriodTextBlock").IsEffectivelyVisible);
                Assert.Equal("PM", Part("PART_PeriodTextBlock").Text);

                window.Press("SecondsCheck");
                Assert.True(Part("PART_SecondTextBlock").IsEffectivelyVisible);
                Assert.Equal("09", Part("PART_SecondTextBlock").Text);

                window.ByName<Slider>("StepSlider").Value = 15.7;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(15, picker.MinuteIncrement);

                picker.SelectedTime = null;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("未选择", window.Text("PickerResult"));
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Selectors ----

        [AvaloniaFact]
        public void Two_state_check_box_readout_follows_clicks()
        {
            var (window, _) = PageHelpers.Open<CheckBoxPage>();
            try
            {
                var single = window.ByName<CheckBox>("Single");
                string Readout() => ((StackPanel)single.Parent!).Children.OfType<TextBlock>().Single().Text ?? "";
                Assert.Equal("IsChecked = False", Readout());
                window.Press("Single");
                Assert.Equal("IsChecked = True", Readout());
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Parent_check_box_summarises_children_and_sets_them_all()
        {
            var (window, _) = PageHelpers.Open<CheckBoxPage>();
            try
            {
                var parent = window.ByName<CheckBox>("ParentBox");
                bool?[] Children() => new[] { "ChildA", "ChildB", "ChildC" }.Select(n => window.ByName<CheckBox>(n).IsChecked).ToArray();
                Assert.Equal("父项：未选", window.Text("ThreeResult"));

                window.Press("ChildA");
                Assert.Null(parent.IsChecked);
                Assert.Equal("父项：部分选中", window.Text("ThreeResult"));
                window.Press("ChildB");
                window.Press("ChildC");
                Assert.True(parent.IsChecked);
                Assert.Equal("父项：全选", window.Text("ThreeResult"));

                // From partial, a click on the parent goes to unchecked and clears every child.
                window.Press("ChildB");
                window.Press("ParentBox");
                Assert.Equal(new bool?[] { false, false, false }, Children());
                Assert.Equal("父项：未选", window.Text("ThreeResult"));

                window.Press("ParentBox");
                Assert.Equal(new bool?[] { true, true, true }, Children());
                Assert.Equal("父项：全选", window.Text("ThreeResult"));
            }
            finally
            {
                window.Close();
            }
        }

        // With IsThreeState a user click would move a fully checked parent to null, which the handler ignores, so the
        // children would stay checked. Code still sets null for "partial"; a click only goes true -> false.
        [AvaloniaFact]
        public void Clicking_a_fully_checked_parent_clears_every_child()
        {
            var (window, _) = PageHelpers.Open<CheckBoxPage>();
            try
            {
                window.Press("ParentBox");
                Assert.True(window.ByName<CheckBox>("ParentBox").IsChecked);

                window.Press("ParentBox");
                Assert.False(window.ByName<CheckBox>("ParentBox").IsChecked);
                Assert.False(window.ByName<CheckBox>("ChildA").IsChecked);
                Assert.Equal("父项：未选", window.Text("ThreeResult"));
            }
            finally
            {
                window.Close();
            }
        }

        // The palette is the second tab of the ColorView template; its swatches are ListBoxItems whose DataContext is the Color.
        private static Color ClickPaletteSwatch(Window window, Visual scope, int index)
        {
            window.Click(scope.GetVisualDescendants().OfType<TabItem>().ElementAt(1));
            Dispatcher.UIThread.RunJobs();
            var swatch = scope.GetVisualDescendants().OfType<ListBox>().Single(l => l.IsEffectivelyVisible)
                .GetVisualDescendants().OfType<ListBoxItem>().ElementAt(index);
            window.Click(swatch);
            Dispatcher.UIThread.RunJobs();
            return (Color)swatch.DataContext!;
        }

        [AvaloniaFact]
        public void Color_picker_flyout_palette_updates_the_swatch_and_text()
        {
            var (window, _) = PageHelpers.Open<ColorPickerPage>();
            try
            {
                var picker = window.ByName<ColorPicker>("Picker");
                Assert.Equal("#ff3b82f6", window.Text("ColorText"));
                Assert.Empty(window.GetVisualDescendants().OfType<ColorSpectrum>());

                window.Click(picker.GetVisualDescendants().OfType<DropDownButton>().Single());
                Dispatcher.UIThread.RunJobs();
                Assert.Single(window.GetVisualDescendants().OfType<ColorSpectrum>());

                var chosen = ClickPaletteSwatch(window, window, 3);
                Assert.Equal(Color.Parse("#ff0099bc"), chosen);
                Assert.Equal("#ff0099bc", window.Text("ColorText"));
                Assert.Equal(chosen, ((ISolidColorBrush)window.ByName<Border>("Swatch").Background!).Color);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Turning_alpha_off_makes_the_picked_color_opaque()
        {
            var (window, _) = PageHelpers.Open<ColorPickerPage>();
            try
            {
                var picker = window.ByName<ColorPicker>("Picker");
                picker.Color = Color.Parse("#80ff0000");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("#80ff0000", window.Text("ColorText"));

                window.Press("AlphaCheck");
                Assert.False(picker.IsAlphaEnabled);
                Assert.Equal(Colors.Red, picker.Color);
                Assert.Equal("Red", window.Text("ColorText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Color_view_palette_updates_the_readout_with_hue_and_the_boxes_set_shape_and_model()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ColorViewPage>();
            try
            {
                var view = window.ByName<ColorView>("View");
                Assert.Equal("#ff10b981  H=160", window.Text("ColorText"));

                ClickPaletteSwatch(window, view, 3);
                Assert.Equal("#ff0099bc  H=191", window.Text("ColorText"));
                Assert.Equal(Color.Parse("#ff0099bc"), ((ISolidColorBrush)window.ByName<Border>("Swatch").Background!).Color);

                window.ByName<ComboBox>("ShapeBox").SelectedItem = ColorSpectrumShape.Ring;
                window.ByName<ComboBox>("ModelBox").SelectedItem = ColorModel.Hsva;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal((ColorSpectrumShape.Ring, ColorModel.Hsva), (view.ColorSpectrumShape, view.ColorModel));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Basic_combo_box_drop_down_click_selects_an_item()
        {
            var (window, _) = PageHelpers.Open<ComboBoxPage>();
            try
            {
                var basic = window.ByName<ComboBox>("Basic");
                Assert.Empty(window.GetVisualDescendants().OfType<ComboBoxItem>());

                window.Click(basic);
                Dispatcher.UIThread.RunJobs();
                var items = window.GetVisualDescendants().OfType<ComboBoxItem>().ToList();
                Assert.Equal(3, items.Count);

                window.Click(items.Single(i => "香蕉".Equals(i.Content)));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选：香蕉", window.Text("BasicResult"));
                Assert.False(basic.IsDropDownOpen);

                basic.SelectedIndex = -1;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("尚未选择", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Editable_combo_box_accepts_free_text_and_selected_value_reads_the_code()
        {
            var (window, _) = PageHelpers.Open<ComboBoxPage>();
            try
            {
                var editable = window.ByName<ComboBox>("Editable");
                Assert.Equal("Text = (空)", window.Text("EditableResult"));

                window.Type(editable.GetVisualDescendants().OfType<TextBox>().Single(), "广州");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Text = 广州", window.Text("EditableResult"));
                Assert.Equal(-1, editable.SelectedIndex);

                editable.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Text = 上海", window.Text("EditableResult"));

                Assert.Equal("SelectedValue = (空)", window.Text("ValueResult"));
                window.ByName<ComboBox>("ByValue").SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("SelectedValue = JP", window.Text("ValueResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Numeric_spin_respects_maximum_but_code_writes_are_kept_as_is()
        {
            var (window, _) = PageHelpers.Open<NumericUpDownPage>();
            try
            {
                var box = window.ByName<NumericUpDown>("Basic");
                var up = box.ByName<RepeatButton>("PART_IncreaseButton");
                Assert.Equal("Value = 9", window.Text("BasicResult"));

                window.Click(up);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Value = 10", window.Text("BasicResult"));
                window.Click(up);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(10, box.Value);
                window.Click(box.ByName<RepeatButton>("PART_DecreaseButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Value = 7", window.Text("BasicResult"));

                window.Press("WriteButton");
                Assert.Equal("Value = 99", window.Text("BasicResult"));

                box.Value = 9;
                window.Press("ClipCheck");
                Assert.True(box.ClipValueToMinMax);
                window.Press("WriteButton");
                Assert.Equal(99, box.Value);
                Assert.Equal("Value = 99", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        // NumberFormat's default is the culture current when NumericUpDown is first used in the process, so
        // CultureScope cannot pin it; format the expected text with the box's own NumberFormat instead.
        [AvaloniaFact]
        public void Money_box_formats_with_c2_and_spins_by_half()
        {
            var (window, _) = PageHelpers.Open<NumericUpDownPage>();
            try
            {
                var money = window.ByName<NumericUpDown>("Money");
                var text = money.GetVisualDescendants().OfType<TextBox>().First();
                Assert.Equal(12.5m.ToString("C2", money.NumberFormat), text.Text);

                window.Click(money.ByName<RepeatButton>("PART_IncreaseButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(13m, money.Value);
                Assert.Equal(13m.ToString("C2", money.NumberFormat), text.Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Arrow_key_moves_the_basic_slider_and_the_readout_follows()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<SliderPage>();
            try
            {
                var basic = window.ByName<Slider>("Basic");
                string Readout() => ((StackPanel)basic.Parent!).Children.OfType<TextBlock>().Single().Text ?? "";
                Assert.Equal("Value = 30.0", Readout());

                PressKey(window, basic, Key.Right, PhysicalKey.ArrowRight);
                Assert.Equal("Value = 31.0", Readout());
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Snapping_applies_to_keys_but_not_to_code_writes()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<SliderPage>();
            try
            {
                var ticked = window.ByName<Slider>("Ticked");
                PressKey(window, ticked, Key.Right, PhysicalKey.ArrowRight);
                Assert.Equal("Value = 2.0", window.Text("TickedResult"));

                window.Press("WriteButton");
                Assert.Equal("Value = 3.3", window.Text("TickedResult"));

                window.Press("SnapCheck");
                Assert.False(ticked.IsSnapToTickEnabled);
                PressKey(window, ticked, Key.Right, PhysicalKey.ArrowRight);
                Assert.Equal("Value = 4.3", window.Text("TickedResult"));
            }
            finally
            {
                window.Close();
            }
        }

        // On and off contents both stay in the tree; the template switches them by opacity.
        [AvaloniaFact]
        public void Toggle_switches_report_their_state_and_swap_on_and_off_content()
        {
            var (window, _) = PageHelpers.Open<ToggleSwitchPage>();
            try
            {
                Assert.Equal("关", window.Text("PlainResult"));
                window.Press("Plain");
                Assert.Equal("开", window.Text("PlainResult"));
                window.Press("Plain");
                Assert.Equal("关", window.Text("PlainResult"));

                var custom = window.ByName<ToggleSwitch>("Custom");
                (double On, double Off) Opacity() => (custom.ByName<ContentPresenter>("PART_OnContentPresenter").Opacity,
                    custom.ByName<ContentPresenter>("PART_OffContentPresenter").Opacity);
                Assert.Equal("已关闭", custom.ByName<ContentPresenter>("PART_OffContentPresenter").Content);
                Assert.Equal((0d, 1d), Opacity());
                window.Press("Custom");
                Assert.Equal((1d, 0d), Opacity());
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Text input ----

        // The drop-down lives in the overlay layer, so its items are found from the window, not from the box.
        [AvaloniaFact]
        public void Filter_mode_decides_the_suggestions_and_a_click_selects_one()
        {
            var (window, _) = PageHelpers.Open<AutoCompleteBoxPage>();
            try
            {
                var box = window.ByName<AutoCompleteBox>("Box");
                var input = box.GetVisualDescendants().OfType<TextBox>().First();
                string?[] Suggestions() => window.GetVisualDescendants().OfType<ListBoxItem>().Select(i => i.Content as string).ToArray();
                Assert.Equal(AutoCompleteFilterMode.Contains, window.ByName<ComboBox>("ModeBox").SelectedItem);
                Assert.Empty(Suggestions());

                window.Type(input, "rr");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(new[] { "Blueberry", "Cherry" }, Suggestions());

                input.Text = "";
                window.ByName<ComboBox>("ModeBox").SelectedItem = AutoCompleteFilterMode.StartsWith;
                Dispatcher.UIThread.RunJobs();
                window.Type(input, "rr");
                Dispatcher.UIThread.RunJobs();
                Assert.False(box.IsDropDownOpen);
                Assert.Empty(Suggestions());

                input.Text = "";
                Dispatcher.UIThread.RunJobs();
                window.Type(input, "ap");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(new[] { "Apple", "Apricot" }, Suggestions());

                window.Click(window.GetVisualDescendants().OfType<ListBoxItem>().Single(i => "Apricot".Equals(i.Content)));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已选：Apricot", window.Text("BoxResult"));
                Assert.False(box.IsDropDownOpen);
            }
            finally
            {
                window.Close();
            }
        }

        // Type one character per call: a multi-character TextInput lands at an odd caret position in MaskedTextBox.
        private static void TypeEach(Window window, MaskedTextBox box, string text)
        {
            box.Focus();
            foreach (var ch in text)
            {
                window.Type(box, ch.ToString());
            }
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaFact]
        public void Phone_mask_reports_completion_only_after_the_last_digit()
        {
            var (window, _) = PageHelpers.Open<MaskedTextBoxPage>();
            try
            {
                var phone = window.ByName<MaskedTextBox>("Phone");
                Assert.Equal("(___) ___-____", phone.Text);

                TypeEach(window, phone, "555123456");
                Assert.Equal("(555) 123-456_", phone.Text);
                Assert.Equal("未填完", window.Text("PhoneResult"));

                TypeEach(window, phone, "78");
                Assert.Equal("(555) 123-4567", phone.Text);
                Assert.Equal("已填完：(555) 123-4567", window.Text("PhoneResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Date_and_letter_masks_skip_wrong_characters_and_upper_case_letters()
        {
            var (window, _) = PageHelpers.Open<MaskedTextBoxPage>();
            try
            {
                var boxes = window.GetVisualDescendants().OfType<MaskedTextBox>().Where(b => b.Name != "Phone").ToList();
                TypeEach(window, boxes[0], "2026a1017");
                Assert.Equal("2026-10-17", boxes[0].Text);
                Assert.True(boxes[0].MaskCompleted);

                TypeEach(window, boxes[1], "ab1c23x4");
                Assert.Equal("ABC-234", boxes[1].Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Text_changed_reports_the_length()
        {
            var (window, _) = PageHelpers.Open<TextBoxPage>();
            try
            {
                var basic = window.ByName<TextBox>("Basic");
                window.Type(basic, "张三丰");
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("长度 3", window.Text("BasicResult"));

                PressKey(window, basic, Key.Back, PhysicalKey.Backspace);
                Assert.Equal("长度 2", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Reveal_check_box_toggles_the_password_presenter()
        {
            var (window, _) = PageHelpers.Open<TextBoxPage>();
            try
            {
                var presenter = window.ByName<TextBox>("Password").GetVisualDescendants().OfType<TextPresenter>().Single();
                Assert.Equal('●', presenter.PasswordChar);
                Assert.False(presenter.RevealPassword);

                window.Press("RevealCheck");
                Assert.True(presenter.RevealPassword);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Enter_breaks_lines_only_in_accepts_return_box()
        {
            var (window, _) = PageHelpers.Open<TextBoxPage>();
            try
            {
                var basic = window.ByName<TextBox>("Basic");
                PressKey(window, basic, Key.Enter, PhysicalKey.Enter);
                Assert.Equal("", basic.Text ?? "");

                var multi = window.ByName<TextBox>("Multi");
                multi.Focus();
                multi.CaretIndex = 3;
                PressKey(window, multi, Key.Enter, PhysicalKey.Enter);
                Assert.Equal("第一行" + Environment.NewLine + "\n第二行", multi.Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Max_length_cuts_input_and_read_only_box_ignores_it()
        {
            var (window, _) = PageHelpers.Open<TextBoxPage>();
            try
            {
                var limited = window.ByName<TextBox>("Limited");
                window.Type(limited, "abcdefgh");
                window.Type(limited, "x");
                Assert.Equal("abcde", limited.Text);

                var readOnly = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.IsReadOnly);
                window.Type(readOnly, "x");
                Assert.Equal("只读，可选中复制", readOnly.Text);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Select_button_selects_the_first_seven_characters()
        {
            var (window, _) = PageHelpers.Open<TextBoxPage>();
            try
            {
                Assert.Equal("选区：无", window.Text("SelectionResult"));
                window.Press("SelectButton");
                Assert.Equal("选区：「Avaloni」", window.Text("SelectionResult"));
                Assert.Equal("Avaloni", window.ByName<TextBox>("Selectable").SelectedText);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

- [ ] **Step 2: 运行，确认只有 3 个缺陷测试失败**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlInputTests"`
Expected: 42 个里 39 过、3 个失败：
- `Three_state_toggle_shows_null_when_indeterminate`：Expected `IsChecked = null`，Actual `IsChecked = `。
- `Editing_the_custom_format_box_never_throws`：`Assert.Null() Failure`，值是 `FormatException`（输入 `Q` 那一步）。
- `Clicking_a_fully_checked_parent_clears_every_child`：Expected False，Actual null（第二次点父项后）。

- [ ] **Step 3: 修复三处**

(a) `src/Avalonia.ControlsDemo/Views/Pages/Input/CalendarDatePickerPage.axaml.cs`，把

```csharp
            CustomFormat.TextChanged += (_, _) => Picker.CustomDateFormatString = CustomFormat.Text ?? string.Empty;
```

改成

```csharp
            // CustomDateFormatString rejects "" and a lone non-standard letter ("Q") throws while the date is
            // re-formatted, so keep the last good format instead of letting an edit crash the app.
            CustomFormat.TextChanged += (_, _) =>
            {
                var format = CustomFormat.Text;
                if (string.IsNullOrEmpty(format))
                {
                    return;
                }
                try
                {
                    _ = DateTime.Today.ToString(format);
                }
                catch (FormatException)
                {
                    return;
                }
                Picker.CustomDateFormatString = format;
            };
```

下一行 `Picker.CustomDateFormatString = CustomFormat.Text ?? string.Empty;`（构造时的初值，XAML 里是合法格式）不动；文件已有 `using System;`。页面提示"格式串写错不会抛异常，日期框显示原样文本"修复后成立（`QQ` 原样显示，`Q` 与空串保留上一个格式），不改。

(b) `src/Avalonia.ControlsDemo/Views/Pages/Input/ToggleButtonPage.axaml` 第 29 行，把 `FallbackValue='IsChecked = null'` 改成 `TargetNullValue='IsChecked = null'`，其余不动。

(c) `src/Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml` 第 22 行，把

```xml
                    <CheckBox Name="ParentBox" IsThreeState="True" Content="全选" />
```

改成

```xml
                    <CheckBox Name="ParentBox" Content="全选" />
```

（代码仍可把 `IsChecked` 设成 null 表示"部分选中"；`IsThreeState` 只决定用户点击时会不会进入 null。）

- [ ] **Step 4: 运行，全绿**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlInputTests"`
Expected: 42 passed，0 failed。

Run: `dotnet build tests/Avalonia.ControlsDemo.Tests 2>&1 | grep -E "ControlInputTests|CalendarDatePickerPage|ToggleButtonPage|CheckBoxPage" | grep -c warning`
Expected: `0`。

- [ ] **Step 5: 提交**

```bash
git add tests/Avalonia.ControlsDemo.Tests/ControlInputTests.cs src/Avalonia.ControlsDemo/Views/Pages/Input/CalendarDatePickerPage.axaml.cs src/Avalonia.ControlsDemo/Views/Pages/Input/ToggleButtonPage.axaml src/Avalonia.ControlsDemo/Views/Pages/Input/CheckBoxPage.axaml
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add interaction tests for the input control pages and fix three input page defects

Keep the last valid custom date format instead of throwing, show null for
the three-state toggle, and stop a click on the full parent check box from
going indeterminate.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

| 临时改动 | 预期失败的测试 |
|---|---|
| `ButtonPage.axaml:31` 的 `ClickMode="Press"` → `"Release"` | `Press_mode_clicks_on_mouse_down_and_release_mode_on_mouse_up`（Expected `1`，Actual `0`） |
| `CheckBoxPage.axaml.cs:50` 的 `3 => true` → `3 => false` | `Parent_check_box_summarises_children_and_sets_them_all` |
| `NumericUpDownPage.axaml.cs:13` 的 `99` → `98` | `Numeric_spin_respects_maximum_but_code_writes_are_kept_as_is` |
| `ColorPickerPage.axaml.cs:12` 的 `e.NewColor` → `e.OldColor` | 2 个：`Turning_alpha_off_makes_the_picked_color_opaque`、`Color_picker_flyout_palette_updates_the_swatch_and_text` |
| `CalendarDatePickerPage.axaml.cs` 删掉 `catch (FormatException)` 那一段里的 `return;`（让它落到赋值） | `Editing_the_custom_format_box_never_throws` |

Run: `git status --short src`
Expected: 无输出。

### Task 3: Layout 的 19 个控件页，并修复 PipsPager 复选框（缺陷 #3）

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/ControlLayoutTests.cs`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Layout/PipsPagerPage.axaml:20-21,25`

**Interfaces:**
- Consumes：同 Task 1；另用 `UiHelpers.ButtonWithText`（Flyout 的「上/右/下/左」按钮）。
- Consumes（`src/`）：19 个控件页的 `x:Name` 控件；模板部件名 `PART_NextButton`（PipsPager）、`PART_HorizontalScrollBar`/`PART_VerticalScrollBar`（ScrollViewer）、`PART_PaneRoot`/`ContentRoot`/`Container`（SplitView）。
- Produces：无（文件内私有辅助 `ExpanderHeader`、`PagerButton`、`Pips`、`ScrollerBar`、`SplitPart`、`Center`）。

逐页覆盖：`BorderPage` 1、`ExpanderPage` 3、`FlyoutPage` 2（1 个 Fact + 4 组 Theory）、`PipsPagerPage` 4（含缺陷 #3）、`RefreshContainerPage` 1、`ScrollViewerPage` 3、`SplitViewPage` 2、`ViewboxPage` 1、`DecoratorPage` 1、`LayoutTransformControlPage` 1、`CanvasPage` 1、`DockPanelPage` 1、`GridPage` 1、`GridSplitterPage` 3、`PanelPage` 1、`RelativePanelPage` 1、`StackPanelPage` 1、`UniformGridPage` 1、`WrapPanelPage` 1，共 30 个方法、33 个用例。`GroupBoxPage` 只靠 H1。

几处容易写错的地方（都在探针里踩过）：

- 布局断言读 `Bounds`，并且先 `window.UpdateLayout()`；结构体属性（`Thickness`、`CornerRadius`）断言控件上的真实值（CLAUDE.md 的"绑定到结构体静默失败"）。
- Expander 自己的 `CheckBox` 也是 `ToggleButton`，找表头要用 `TemplatedParent == expander`。
- 第一页时 PipsPager 的「上一页」按钮 `IsEffectivelyVisible` 为真，只是 `Opacity` 0、不可命中、禁用；所以只测「下一页」的显隐。
- `GridSplitter`：一次带 `RawInputModifiers.LeftMouseButton` 的 `MouseMove` 就够；`Click` 能给它焦点，方向键每次 10 px。
- `RefreshContainer.RequestRefresh()` 与下拉手势走同一条延迟路径，处理器 300 ms 后完成，用 `WaitUntil`。
- 探针里出现过一次 `Catastrophic failure: Test process did not return valid JSON`，原样重跑即过，之后未再出现（推测是 4 个探针并行造成的机器负载）；执行时若遇到，先重跑一次再排查。

- [ ] **Step 1: 放入测试文件**

#### `tests/Avalonia.ControlsDemo.Tests/ControlLayoutTests.cs`

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Layout.
    public class ControlLayoutTests
    {
        // ---- Containers ----

        // Thickness and CornerRadius are structs set from code, so assert the real values and where the child lands.
        [AvaloniaFact]
        public void Border_sliders_set_thickness_and_radius_and_push_the_child_in()
        {
            var (window, _) = PageHelpers.Open<BorderPage>();
            try
            {
                var target = window.ByName<Border>("Target");
                var child = (Control)target.Child!;
                Assert.Equal(new Thickness(2), target.BorderThickness);
                Assert.Equal(new CornerRadius(8), target.CornerRadius);
                Assert.Equal(14, child.Bounds.X);

                window.ByName<Slider>("ThicknessSlider").Value = 6;
                window.ByName<Slider>("RadiusSlider").Value = 20;
                window.UpdateLayout();

                Assert.Equal(new Thickness(6), target.BorderThickness);
                Assert.Equal(new CornerRadius(20), target.CornerRadius);
                Assert.Equal(18, child.Bounds.X);
            }
            finally
            {
                window.Close();
            }
        }

        private static ToggleButton ExpanderHeader(Expander expander)
            => expander.GetVisualDescendants().OfType<ToggleButton>().First(t => t.TemplatedParent == expander);

        [AvaloniaFact]
        public void Clicking_the_expander_header_counts_expanded_and_collapsed()
        {
            var (window, _) = PageHelpers.Open<ExpanderPage>();
            try
            {
                var basic = window.ByName<Expander>("Basic");
                Assert.Equal("已展开 0 次，已折叠 0 次", window.Text("BasicResult"));

                window.Click(ExpanderHeader(basic));
                Dispatcher.UIThread.RunJobs();
                Assert.True(basic.IsExpanded);
                Assert.Equal("已展开 1 次，已折叠 0 次", window.Text("BasicResult"));

                window.Click(ExpanderHeader(basic));
                Dispatcher.UIThread.RunJobs();
                Assert.False(basic.IsExpanded);
                Assert.Equal("已展开 1 次，已折叠 1 次", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Disabled_expander_ignores_the_mouse_but_not_code()
        {
            var (window, _) = PageHelpers.Open<ExpanderPage>();
            try
            {
                var basic = window.ByName<Expander>("Basic");
                window.Press("EnabledCheck");
                Assert.False(basic.IsEnabled);

                window.Click(ExpanderHeader(basic));
                Dispatcher.UIThread.RunJobs();
                Assert.False(basic.IsExpanded);
                Assert.Equal("已展开 0 次，已折叠 0 次", window.Text("BasicResult"));

                basic.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("已展开 1 次，已折叠 0 次", window.Text("BasicResult"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Direction_box_moves_the_expander_content_to_that_side()
        {
            var (window, _) = PageHelpers.Open<ExpanderPage>();
            try
            {
                var box = window.ByName<ComboBox>("DirectionBox");
                var sideways = window.ByName<Expander>("Sideways");
                // Collapsed content is not in the visual tree yet, so look it up after expanding.
                TextBlock Content() => sideways.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "展开的内容");
                Assert.Equal(ExpandDirection.Down, box.SelectedItem);
                sideways.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                var content = Content();
                var header = ExpanderHeader(sideways);
                Assert.True(content.TranslatePoint(default, header)!.Value.Y >= header.Bounds.Height);

                box.SelectedItem = ExpandDirection.Right;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(ExpandDirection.Right, sideways.ExpandDirection);
                content = Content();
                header = ExpanderHeader(sideways);
                Assert.True(content.TranslatePoint(default, header)!.Value.X >= header.Bounds.Width);
            }
            finally
            {
                window.Close();
            }
        }

        // The flyout content lives in the overlay layer: absent before opening, present exactly once after.
        [AvaloniaFact]
        public void Opening_and_dismissing_the_flyout_updates_the_status()
        {
            var (window, _) = PageHelpers.Open<FlyoutPage>();
            try
            {
                int Confirms() => window.GetVisualDescendants().OfType<Button>().Count(b => b.Name == "ConfirmButton");
                Assert.Equal("尚未操作", window.Text("FlyoutResult"));
                Assert.Equal(0, Confirms());

                window.Press("OpenButton");
                Assert.Equal("Flyout 已打开", window.Text("FlyoutResult"));
                Assert.Equal(1, Confirms());

                window.Click(window.ByName<TextBlock>("FlyoutResult"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Flyout 已关闭", window.Text("FlyoutResult"));
                Assert.Equal(0, Confirms());
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTheory]
        [InlineData("上", "在上方")]
        [InlineData("右", "在右侧")]
        [InlineData("下", "在下方")]
        [InlineData("左", "在左侧")]
        public void Placement_buttons_open_their_flyout_on_that_side(string button, string text)
        {
            var (window, _) = PageHelpers.Open<FlyoutPage>();
            try
            {
                TextBlock[] Shown() => window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Text == text).ToArray();
                Assert.Empty(Shown());

                var target = window.ButtonWithText(button);
                window.PressText(button);
                window.UpdateLayout();

                var shown = Assert.Single(Shown());
                var topLeft = shown.TranslatePoint(default, target)!.Value;
                var bottomRight = shown.TranslatePoint(new Point(shown.Bounds.Width, shown.Bounds.Height), target)!.Value;
                switch (button)
                {
                    case "上": Assert.True(bottomRight.Y <= 0, $"bottom {bottomRight.Y}"); break;
                    case "右": Assert.True(topLeft.X >= target.Bounds.Width, $"left {topLeft.X}"); break;
                    case "下": Assert.True(topLeft.Y >= target.Bounds.Height, $"top {topLeft.Y}"); break;
                    case "左": Assert.True(bottomRight.X <= 0, $"right {bottomRight.X}"); break;
                }
            }
            finally
            {
                window.Close();
            }
        }

        private static Button PagerButton(PipsPager pager, string part)
            => pager.GetVisualDescendants().OfType<Button>().Single(b => b.Name == part);

        private static ListBoxItem[] Pips(PipsPager pager)
            => pager.GetVisualDescendants().OfType<ListBoxItem>().ToArray();

        [AvaloniaFact]
        public void Next_button_and_pips_change_the_page_text()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var pager = window.ByName<PipsPager>("Pager");
                Assert.Equal("第 1 页", window.Text("PageText"));

                window.Click(PagerButton(pager, "PART_NextButton"));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("第 2 页", window.Text("PageText"));

                window.Click(Pips(pager)[3]);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(3, pager.SelectedPageIndex);
                Assert.Equal("第 4 页", window.Text("PageText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Pages_slider_sets_the_number_of_pips()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var pager = window.ByName<PipsPager>("Pager");
                Assert.Equal(5, Pips(pager).Length);

                window.ByName<Slider>("PagesSlider").Value = 8;
                window.UpdateLayout();

                Assert.Equal(8, pager.NumberOfPages);
                Assert.Equal(8, Pips(pager).Length);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Next_check_box_hides_and_shows_the_next_button()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var next = PagerButton(window.ByName<PipsPager>("Pager"), "PART_NextButton");
                Assert.True(next.IsEffectivelyVisible);

                window.Press("NextCheck");
                Assert.False(next.IsEffectivelyVisible);
                window.Press("NextCheck");
                Assert.True(next.IsEffectivelyVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // Avalonia 12's PipsPager shows both buttons by default, so the check boxes have to start ticked.
        [AvaloniaFact]
        public void Pager_button_check_boxes_start_in_sync_with_the_pager()
        {
            var (window, _) = PageHelpers.Open<PipsPagerPage>();
            try
            {
                var pager = window.ByName<PipsPager>("Pager");
                Assert.Equal(pager.IsPreviousButtonVisible, window.ByName<CheckBox>("PreviousCheck").IsChecked);
                Assert.Equal(pager.IsNextButtonVisible, window.ByName<CheckBox>("NextCheck").IsChecked);
            }
            finally
            {
                window.Close();
            }
        }

        // RequestRefresh goes through the same deferral path as the pull gesture; the handler finishes 300 ms later.
        [AvaloniaFact]
        public async Task Refresh_button_runs_the_refresh_handler_once_per_press()
        {
            var (window, _) = PageHelpers.Open<RefreshContainerPage>();
            try
            {
                Assert.Equal("刷新 0 次", window.Text("RefreshResult"));
                Assert.Equal("内容：尚未刷新", window.Text("Body"));

                window.Press("RefreshButton");
                await UiHelpers.WaitUntil(() => window.Text("RefreshResult") == "刷新 1 次");
                Assert.Matches(@"^内容：\d\d:\d\d:\d\d 刷新$", window.Text("Body"));

                window.Press("RefreshButton");
                await UiHelpers.WaitUntil(() => window.Text("RefreshResult") == "刷新 2 次");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Scroll_buttons_move_the_offset_and_the_readout_follows()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerPage>();
            try
            {
                var scroller = window.ByName<ScrollViewer>("Scroller");
                Assert.Equal(ScrollBarVisibility.Disabled, window.ByName<ComboBox>("HBox").SelectedItem);
                Assert.Equal(ScrollBarVisibility.Auto, window.ByName<ComboBox>("VBox").SelectedItem);
                Assert.Equal("Offset = 0, 0", window.Text("OffsetText"));

                window.Press("EndButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                // ScrollToEnd goes to the bottom-left corner: the vertical maximum, horizontal 0.
                Assert.Equal(new Vector(0, 360), scroller.Offset);
                Assert.Equal("Offset = 0, 360", window.Text("OffsetText"));

                window.Press("HomeButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Offset = 0, 0", window.Text("OffsetText"));

                window.Press("LineButton");
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("Offset = 0, 16", window.Text("OffsetText"));
            }
            finally
            {
                window.Close();
            }
        }

        private static ScrollBar ScrollerBar(ScrollViewer scroller, string part)
            => scroller.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Name == part);

        [AvaloniaFact]
        public void Visibility_boxes_show_and_hide_the_scroll_bars()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerPage>();
            try
            {
                var scroller = window.ByName<ScrollViewer>("Scroller");
                Assert.False(ScrollerBar(scroller, "PART_HorizontalScrollBar").IsEffectivelyVisible);
                Assert.True(ScrollerBar(scroller, "PART_VerticalScrollBar").IsEffectivelyVisible);

                window.ByName<ComboBox>("HBox").SelectedItem = ScrollBarVisibility.Visible;
                window.ByName<ComboBox>("VBox").SelectedItem = ScrollBarVisibility.Hidden;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                Assert.Equal(ScrollBarVisibility.Visible, scroller.HorizontalScrollBarVisibility);
                Assert.True(ScrollerBar(scroller, "PART_HorizontalScrollBar").IsEffectivelyVisible);
                Assert.False(ScrollerBar(scroller, "PART_VerticalScrollBar").IsEffectivelyVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // Writing an Offset past the end is clamped to extent minus viewport (600x500 content in a 320x140 viewer).
        // Code can scroll sideways even while the horizontal bar is Disabled.
        [AvaloniaFact]
        public void Out_of_range_offset_is_clamped_to_the_maximum()
        {
            var (window, _) = PageHelpers.Open<ScrollViewerPage>();
            try
            {
                var scroller = window.ByName<ScrollViewer>("Scroller");
                scroller.Offset = new Vector(1000, 1000);
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(new Vector(280, 360), scroller.Offset);
                Assert.Equal("Offset = 280, 360", window.Text("OffsetText"));
            }
            finally
            {
                window.Close();
            }
        }

        private static Control SplitPart(SplitView split, string name)
            => split.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name);

        [AvaloniaFact]
        public async Task Toggle_button_opens_the_compact_inline_pane_and_pushes_the_content()
        {
            var (window, _) = PageHelpers.Open<SplitViewPage>();
            try
            {
                var split = window.ByName<SplitView>("Split");
                Assert.Equal(SplitViewDisplayMode.CompactInline, window.ByName<ComboBox>("ModeBox").SelectedItem);
                Assert.Equal("窗格：关闭", window.Text("PaneText"));
                Assert.Equal(48, SplitPart(split, "PART_PaneRoot").Bounds.Width);
                Assert.Equal(48, SplitPart(split, "ContentRoot").Bounds.X);

                // PaneOpened fires when the pane starts opening; its width animates, so wait for the final layout.
                window.Press("ToggleButton");
                await UiHelpers.WaitUntil(() => window.Text("PaneText") == "窗格：打开");
                await UiHelpers.WaitUntil(() => { window.UpdateLayout(); return SplitPart(split, "PART_PaneRoot").Bounds.Width == 180; });
                Assert.True(split.IsPaneOpen);
                Assert.Equal(180, SplitPart(split, "ContentRoot").Bounds.X);

                window.Press("ToggleButton");
                await UiHelpers.WaitUntil(() => window.Text("PaneText") == "窗格：关闭");
                await UiHelpers.WaitUntil(() => { window.UpdateLayout(); return SplitPart(split, "PART_PaneRoot").Bounds.Width == 48; });
                Assert.Equal(48, SplitPart(split, "ContentRoot").Bounds.X);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public async Task Overlay_mode_opens_the_pane_over_the_content_and_right_placement_moves_it()
        {
            var (window, _) = PageHelpers.Open<SplitViewPage>();
            try
            {
                var split = window.ByName<SplitView>("Split");
                window.ByName<ComboBox>("ModeBox").SelectedItem = SplitViewDisplayMode.Overlay;
                window.ByName<ComboBox>("PlacementBox").SelectedItem = SplitViewPanePlacement.Right;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(SplitViewDisplayMode.Overlay, split.DisplayMode);
                Assert.Equal(SplitViewPanePlacement.Right, split.PanePlacement);

                window.Press("ToggleButton");
                await UiHelpers.WaitUntil(() => window.Text("PaneText") == "窗格：打开");
                var pane = SplitPart(split, "PART_PaneRoot");
                var content = SplitPart(split, "ContentRoot");
                await UiHelpers.WaitUntil(() => { window.UpdateLayout(); return pane.Bounds.Width == 180; });
                // Overlay leaves the content full width under the open pane, which now hugs the right edge.
                Assert.Equal(0, content.Bounds.X);
                Assert.Equal(SplitPart(split, "Container").Bounds.Width, content.Bounds.Width);
                Assert.Equal(content.Bounds.Right, pane.Bounds.Right);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Stretch_box_rescales_the_viewbox_child()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<ViewboxPage>();
            try
            {
                Assert.Equal(Stretch.Uniform, window.ByName<ComboBox>("StretchBox").SelectedItem);
                Assert.Equal("水平缩放约 2.76 倍", window.Text("ScaleText"));

                void Pick(Stretch stretch)
                {
                    window.ByName<ComboBox>("StretchBox").SelectedItem = stretch;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }

                Pick(Stretch.Fill);
                Assert.Equal("水平缩放约 3.18 倍", window.Text("ScaleText"));
                Pick(Stretch.None);
                Assert.Equal("水平缩放约 1.00 倍", window.Text("ScaleText"));
                // The child itself is never resized.
                Assert.Equal(new Size(100, 50), window.ByName<Border>("Inner").Bounds.Size);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Decorator ----

        [AvaloniaFact]
        public void Padding_slider_grows_the_decorator_around_its_child()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<DecoratorPage>();
            try
            {
                var decorator = window.ByName<Decorator>("PaddedDecorator");
                Assert.Equal(new Thickness(10), decorator.Padding);
                Assert.Equal("占 140 × 60", window.Text("SizeText"));

                window.ByName<Slider>("PaddingSlider").Value = 25;
                window.UpdateLayout();

                Assert.Equal(new Thickness(25), decorator.Padding);
                Assert.Equal(new Size(170, 90), decorator.Bounds.Size);
                Assert.Equal("占 170 × 90", window.Text("SizeText"));
            }
            finally
            {
                window.Close();
            }
        }

        // ---- LayoutTransformControl ----

        [AvaloniaFact]
        public void Angle_slider_resizes_the_layout_host_but_not_the_render_host()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<LayoutTransformControlPage>();
            try
            {
                Assert.Equal("宿主 40 × 100", window.Text("LayoutSize"));
                Assert.Equal("宿主 100 × 40", window.Text("RenderSize"));

                window.ByName<Slider>("AngleSlider").Value = 45;
                window.UpdateLayout();
                Assert.Equal("宿主 99 × 99", window.Text("LayoutSize"));
                Assert.Equal("宿主 100 × 40", window.Text("RenderSize"));

                window.ByName<Slider>("AngleSlider").Value = 0;
                window.UpdateLayout();
                Assert.Equal("宿主 100 × 40", window.Text("LayoutSize"));
                Assert.Equal(new Size(100, 40), window.ByName<LayoutTransformControl>("LayoutHost").Bounds.Size);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Panels ----

        [AvaloniaFact]
        public void Left_and_top_sliders_move_the_dot_on_the_canvas()
        {
            var (window, _) = PageHelpers.Open<CanvasPage>();
            try
            {
                var dot = window.ByName<Border>("Dot");
                Assert.Equal(new Rect(200, 30, 40, 40), dot.Bounds);

                window.ByName<Slider>("LeftSlider").Value = 100;
                window.ByName<Slider>("TopSlider").Value = 80;
                window.UpdateLayout();

                Assert.Equal(100, Canvas.GetLeft(dot));
                Assert.Equal(new Rect(100, 80, 40, 40), dot.Bounds);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Clearing_last_child_fill_docks_the_last_child_left_at_its_own_width()
        {
            var (window, _) = PageHelpers.Open<DockPanelPage>();
            try
            {
                var last = window.ByName<Border>("LastChild");
                Assert.Equal(new Rect(60, 30, 200, 100), last.Bounds);

                window.Press("FillCheck");
                window.UpdateLayout();

                Assert.False(window.ByName<DockPanel>("Dock").LastChildFill);
                Assert.Equal(60, last.Bounds.X);
                Assert.Equal(last.Child!.DesiredSize.Width, last.Bounds.Width);
                Assert.True(last.Bounds.Width < 200);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Grid_columns_split_by_their_definitions_and_spacing_opens_gaps()
        {
            var (window, _) = PageHelpers.Open<GridPage>();
            try
            {
                var grid = window.ByName<Grid>("Demo");
                // ColumnDefinition.ActualWidth includes the spacing once ColumnSpacing is set, so read the cells instead.
                Rect Cell(int i) => grid.Children[i].Bounds;
                Assert.Equal(80, Cell(0).Width);
                Assert.Equal(2 * Cell(2).Width, Cell(3).Width);
                Assert.Equal(360, Cell(3).Right);
                Assert.Equal(40, Cell(4).Y);

                window.ByName<Slider>("SpacingSlider").Value = 10;
                window.UpdateLayout();

                Assert.Equal(10, grid.ColumnSpacing);
                Assert.Equal(10, grid.RowSpacing);
                Assert.Equal(new double[] { 0, 90, 146, 224 }, Enumerable.Range(0, 4).Select(i => Cell(i).X));
                Assert.Equal(2 * Cell(2).Width, Cell(3).Width);
                Assert.Equal(360, Cell(3).Right);
                Assert.Equal(50, Cell(4).Y);

                window.Press("LinesCheck");
                Assert.True(grid.ShowGridLines);
            }
            finally
            {
                window.Close();
            }
        }

        private static Point Center(Window window, Control control)
            => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        [AvaloniaFact]
        public void Dragging_the_splitter_resizes_the_left_column()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<GridSplitterPage>();
            try
            {
                var split = window.ByName<Grid>("Split");
                Assert.Equal("左栏宽度 200", window.Text("WidthText"));

                var from = Center(window, window.ByName<GridSplitter>("Splitter"));
                window.MouseDown(from, MouseButton.Left);
                window.MouseMove(from + new Point(30, 0), RawInputModifiers.LeftMouseButton);
                window.MouseUp(from + new Point(30, 0), MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                Assert.Equal(230, split.ColumnDefinitions[0].ActualWidth);
                Assert.Equal("左栏宽度 230", window.Text("WidthText"));
            }
            finally
            {
                window.Close();
            }
        }

        // With ShowsPreview only a preview line moves during the drag; the column changes on release.
        [AvaloniaFact]
        public void Shows_preview_holds_the_column_until_the_drag_ends()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<GridSplitterPage>();
            try
            {
                var split = window.ByName<Grid>("Split");
                var splitter = window.ByName<GridSplitter>("Splitter");
                window.Press("PreviewCheck");
                Assert.True(splitter.ShowsPreview);

                var from = Center(window, splitter);
                window.MouseDown(from, MouseButton.Left);
                window.MouseMove(from + new Point(30, 0), RawInputModifiers.LeftMouseButton);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(200, split.ColumnDefinitions[0].ActualWidth);

                window.MouseUp(from + new Point(30, 0), MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(230, split.ColumnDefinitions[0].ActualWidth);
                Assert.Equal("左栏宽度 230", window.Text("WidthText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Arrow_keys_on_the_focused_splitter_move_it_by_the_keyboard_increment()
        {
            using var culture = CultureScope.Invariant();
            var (window, _) = PageHelpers.Open<GridSplitterPage>();
            try
            {
                var splitter = window.ByName<GridSplitter>("Splitter");
                window.Click(splitter);
                Dispatcher.UIThread.RunJobs();
                Assert.True(splitter.IsFocused);

                window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal("左栏宽度 210", window.Text("WidthText"));

                window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                window.KeyRelease(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                window.KeyRelease(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal("左栏宽度 190", window.Text("WidthText"));
            }
            finally
            {
                window.Close();
            }
        }

        // Hit testing tells which child is really on top, not just which ZIndex is larger.
        [AvaloniaFact]
        public void Swap_button_puts_the_lower_child_on_top()
        {
            var (window, _) = PageHelpers.Open<PanelPage>();
            try
            {
                var under = window.ByName<Border>("Under");
                var over = window.ByName<Border>("Over");
                var probe = window.ByName<Panel>("Stack").TranslatePoint(new Point(150, 60), window)!.Value;
                Border? Top() => (window.InputHitTest(probe) as Visual)?.GetSelfAndVisualAncestors()
                    .OfType<Border>().FirstOrDefault(b => b == under || b == over);
                Assert.Same(over, Top());

                window.Press("SwapButton");
                Assert.Equal(-1, over.ZIndex);
                Assert.Equal(1, under.ZIndex);
                Assert.Same(under, Top());

                window.Press("SwapButton");
                Assert.Same(over, Top());
            }
            finally
            {
                window.Close();
            }
        }

        // With RightOf and AlignRightWithPanel both set, the fixed-width follower centres between the two constraints.
        [AvaloniaFact]
        public void Align_right_with_panel_centres_the_follower_between_its_constraints()
        {
            var (window, _) = PageHelpers.Open<RelativePanelPage>();
            try
            {
                var follower = window.ByName<Border>("Follower");
                Assert.Equal(new Rect(90, 0, 90, 40), follower.Bounds);
                Assert.Equal(new Rect(250, 110, 90, 30), window.ByName<Border>("Edge").Bounds);

                window.Press("PanelRightCheck");
                window.UpdateLayout();

                Assert.True(RelativePanel.GetAlignRightWithPanel(follower));
                Assert.Equal(new Rect(170, 0, 90, 40), follower.Bounds);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Orientation_and_spacing_rearrange_the_stack()
        {
            var (window, _) = PageHelpers.Open<StackPanelPage>();
            try
            {
                var stack = window.ByName<StackPanel>("Stack");
                double[] Ys() => stack.Children.Select(c => c.Bounds.Y).ToArray();
                double[] Xs() => stack.Children.Select(c => c.Bounds.X).ToArray();
                Assert.Equal(Orientation.Vertical, window.ByName<ComboBox>("OrientationBox").SelectedItem);
                Assert.Equal(new double[] { 0, 36, 72 }, Ys());

                window.ByName<ComboBox>("OrientationBox").SelectedItem = Orientation.Horizontal;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(new double[] { 0, 66, 132 }, Xs());

                window.ByName<Slider>("SpacingSlider").Value = 20;
                window.UpdateLayout();
                Assert.Equal(new double[] { 0, 80, 160 }, Xs());
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Columns_and_first_column_sliders_reflow_the_uniform_grid()
        {
            var (window, _) = PageHelpers.Open<UniformGridPage>();
            try
            {
                var grid = window.ByName<UniformGrid>("Uniform");
                // Each cell's Border has Margin 2, so Bounds sit 2 px inside the cell.
                Assert.Equal(new Rect(2, 72, 96, 66), grid.Children[3].Bounds);

                window.ByName<Slider>("ColumnsSlider").Value = 2;
                window.UpdateLayout();
                Assert.Equal(2, grid.Columns);
                // Three rows of 46.67 px; layout rounding puts the third item at y = 2 + 47.
                Assert.Equal(new Point(2, 49), grid.Children[2].Bounds.Position);

                window.ByName<Slider>("ColumnsSlider").Value = 3;
                window.ByName<Slider>("FirstSlider").Value = 1;
                window.UpdateLayout();
                Assert.Equal(1, grid.FirstColumn);
                Assert.Equal(102, grid.Children[0].Bounds.X);
                Assert.Equal(new Point(2, 72), grid.Children[2].Bounds.Position);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Wrap_panel_wraps_by_width_and_follows_spacing_and_orientation()
        {
            var (window, _) = PageHelpers.Open<WrapPanelPage>();
            try
            {
                var wrap = window.ByName<WrapPanel>("Wrap");
                var fourth = wrap.Children[3];
                // 70 + 50 + 80 + 60 plus three 4 px gaps is 272 > 260, so the fourth item starts the second line.
                Assert.Equal(new Point(0, 44), fourth.Bounds.Position);

                window.ByName<Slider>("ItemGapSlider").Value = 0;
                window.UpdateLayout();
                Assert.Equal(0, wrap.ItemSpacing);
                Assert.Equal(new Point(200, 0), fourth.Bounds.Position);

                window.ByName<Slider>("ItemGapSlider").Value = 4;
                window.ByName<ComboBox>("OrientationBox").SelectedItem = Orientation.Vertical;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(Orientation.Vertical, wrap.Orientation);
                // 30 + 40 + 30 + 50 plus gaps is 162 > 150, so it starts the second column (after the 80-wide first
                // column and a 4 px line gap), centred in that column's 90 px width.
                Assert.Equal(new Point(99, 0), fourth.Bounds.Position);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
```

- [ ] **Step 2: 运行，确认只有 PipsPager 的 2 个测试失败**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlLayoutTests"`
Expected: 33 个用例里 31 过、2 个失败：
- `Pager_button_check_boxes_start_in_sync_with_the_pager`：Expected True，Actual False。
- `Next_check_box_hides_and_shows_the_next_button`：第一次 `Press("NextCheck")` 把复选框从"不勾"点成"勾"，按钮仍可见，`Assert.False` 失败。

- [ ] **Step 3: 修复 PipsPager 复选框与提示文字**

`src/Avalonia.ControlsDemo/Views/Pages/Layout/PipsPagerPage.axaml`，把

```xml
                        <CheckBox Name="PreviousCheck" Content="IsPreviousButtonVisible" />
                        <CheckBox Name="NextCheck" Content="IsNextButtonVisible" />
```

改成

```xml
                        <!-- Avalonia 12's PipsPager shows both buttons by default. -->
                        <CheckBox Name="PreviousCheck" IsChecked="True" Content="IsPreviousButtonVisible" />
                        <CheckBox Name="NextCheck" IsChecked="True" Content="IsNextButtonVisible" />
```

并把同文件第 25 行提示里的 `上一页 / 下一页按钮默认隐藏。` 改成 `上一页 / 下一页按钮默认显示（在第一页时「上一页」透明且不可点）。`，提示的其余文字不动。

（`IsCheckedChanged` 在 `InitializeComponent()` 之后才订阅，所以 XAML 里的初值不会触发处理器，`Pager` 保持它自己的默认值 `True`，两边一致。）

- [ ] **Step 4: 运行，全绿**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlLayoutTests"`
Expected: 33 passed，0 failed。

Run: `dotnet build tests/Avalonia.ControlsDemo.Tests 2>&1 | grep -E "ControlLayoutTests|PipsPagerPage" | grep -c warning`
Expected: `0`。

- [ ] **Step 5: 提交**

```bash
git add tests/Avalonia.ControlsDemo.Tests/ControlLayoutTests.cs src/Avalonia.ControlsDemo/Views/Pages/Layout/PipsPagerPage.axaml
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add interaction tests for the layout control pages and sync the pips pager check boxes

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

| 临时改动 | 预期失败的测试 |
|---|---|
| `BorderPage.axaml.cs:17` 的 `ThicknessSlider.Value` → `ThicknessSlider.Value / 2` | `Border_sliders_set_thickness_and_radius_and_push_the_child_in`（Expected `6,6,6,6`，Actual `3,3,3,3`） |
| `GridSplitterPage.axaml.cs:13` 的 `== true` → `== false` | `Shows_preview_holds_the_column_until_the_drag_ends` |
| `WrapPanelPage.axaml.cs:27` 的 `Wrap.ItemSpacing` → `Wrap.LineSpacing` | `Wrap_panel_wraps_by_width_and_follows_spacing_and_orientation` |
| `SplitViewPage.axaml.cs:12` 的 `!Split.IsPaneOpen` → `true` | `Toggle_button_opens_the_compact_inline_pane_and_pushes_the_content`（等 `窗格：关闭` 超时） |
| `RefreshContainerPage.axaml.cs:20` 注释掉 `_count++;` | `Refresh_button_runs_the_refresh_handler_once_per_press`（超时） |

Run: `git status --short src`
Expected: 无输出。

### Task 4: Media、Menus、Navigation、Primitives、System 的 21 个控件页，并修复原生菜单残留（缺陷 #2）

**Files:**
- Create: `tests/Avalonia.ControlsDemo.Tests/ControlShellTests.cs`
- Modify: `src/Avalonia.ControlsDemo/Views/Pages/Menus/NativeMenuPage.axaml.cs:28-44`

**Interfaces:**
- Consumes：同 Task 1。
- Consumes（`src/`，均为 `public`）：`NativeMenuPage.Built : NativeMenu`；`WindowPage.Sample : Window?`、`WindowPage.ClosingCount : int`；`WindowDrawnDecorationsPage.Sample : Window?`；`TrayIconPage.Tray : TrayIcon?`；`SwatchView`；Avalonia 12 的 `Avalonia.Controls.CarouselPage`、`NavigationPage`、`TabbedPage`、`ContentPage`、`CommandBar`、`CommandBarButton`、`CommandBarToggleButton`。
- Produces：无（文件内私有辅助 `RightClick(Window, Control)`、`MenuItemsWithHeader(Window, string) : int`）。

逐页覆盖：`DrawingImagePage` 1、`ImagePage` 2、`PathIconPage` 1、`ContextMenuPage` 3、`MenuPage` 1、`MenuFlyoutPage` 2、`NativeMenuPage` 2（含缺陷 #2）、`CarouselDemoPage` 2、`CommandBarPage` 3、`ContentDemoPage` 2、`DrawerDemoPage` 2、`NavigationDemoPage` 2、`TabbedDemoPage` 2、`TabControlPage` 2、`TabStripPage` 1、`ScrollBarPage` 3、`ThemeVariantScopePage` 2、`UserControlPage` 1、`WindowPage` 4、`WindowDrawnDecorationsPage` 2、`TrayIconPage` 3，共 43。`SeparatorPage` 只靠 H1。

几处容易写错的地方（都在探针里踩过）：

- 应用跑在 Dark 主题下，`ThemeVariantScope` 的 `Default` 解析成 Dark；`RequestedThemeVariant` 一开始是 `Default` 而不是 null，页面的 `（空）` 分支不会出现。这些测试**不改** `Application.Current.RequestedThemeVariant`。
- `MenuPage` 的读数显示原始表头，`最近点击：_新建` 带着访问键下划线；这是页面现状，不算缺陷。
- `TrayIconPage` 显示期间把自己的图标挂到 `Application` 上：测试先存 `TrayIcon.GetIcons(app)`，`finally` 里关窗口再 `SetIcons(app, saved)`。Headless 没有托盘导出器，菜单命令直接 `Execute` 模拟。
- 打开示例窗口的 4 个 `WindowPage` 测试与 2 个 `WindowDrawnDecorationsPage` 测试都在 `finally` 里关宿主；示例窗口有所有者，随宿主关闭。"离开页面"用 `window.Content = null` 模拟，与在导航树里换页同一条路径（`DetachedFromVisualTree`）。

- [ ] **Step 1: 放入测试文件**

#### `tests/Avalonia.ControlsDemo.Tests/ControlShellTests.cs`

```csharp
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // H3 for the control pages of Media, Menus, Navigation, Primitives and System.
    public class ControlShellTests
    {
        // ---- Media ----

        [AvaloniaFact]
        public void Recolor_button_cycles_the_built_drawing_through_the_palette()
        {
            var (window, _) = PageHelpers.Open<DrawingImagePage>();
            var drawing = (GeometryDrawing)((DrawingImage)window.ByName<Image>("Built").Source!).Drawing!;
            Assert.Equal("Brush = OrangeRed", window.Text("ColorText"));

            window.Press("RecolorButton");
            Assert.Equal("Brush = SeaGreen", window.Text("ColorText"));
            Assert.Same(Brushes.SeaGreen, drawing.Brush);

            window.Press("RecolorButton");
            window.Press("RecolorButton");
            window.Press("RecolorButton");
            Assert.Equal("Brush = OrangeRed", window.Text("ColorText"));
            Assert.Same(Brushes.OrangeRed, drawing.Brush);
        }

        // The readout follows LayoutUpdated, so it only changes after a layout pass. The 260×140 frame has a 1px
        // border, which leaves 258×138 for the image.
        [AvaloniaFact]
        public void Stretch_and_direction_boxes_change_the_rendered_size()
        {
            var (window, _) = PageHelpers.Open<ImagePage>();
            var pic = window.ByName<Image>("Pic");
            void Pick(string box, object value)
            {
                window.ByName<ComboBox>(box).SelectedItem = value;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
            }
            Assert.Equal("源图 40×20，Image 占位 258×129，期望尺寸 258×129", window.Text("SizeText"));

            Pick("StretchBox", Stretch.None);
            Assert.Equal(Stretch.None, pic.Stretch);
            Assert.Equal("源图 40×20，Image 占位 40×20，期望尺寸 40×20", window.Text("SizeText"));

            Pick("StretchBox", Stretch.Uniform);
            // DownOnly only limits measuring: the image still gets the whole slot and just draws its pixels at 40×20.
            Pick("DirectionBox", StretchDirection.DownOnly);
            Assert.Equal("源图 40×20，Image 占位 258×129，期望尺寸 40×20", window.Text("SizeText"));
        }

        [AvaloniaFact]
        public void Interpolation_box_sets_the_render_option_on_the_image()
        {
            var (window, _) = PageHelpers.Open<ImagePage>();
            var pic = window.ByName<Image>("Pic");

            window.ByName<ComboBox>("InterpolationBox").SelectedItem = BitmapInterpolationMode.None;

            Assert.Equal(BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode(pic));
        }

        [AvaloniaFact]
        public void Path_icon_follows_the_geometry_colour_and_size_controls()
        {
            var (window, page) = PageHelpers.Open<PathIconPage>();
            var icon = window.ByName<PathIcon>("Icon");
            Assert.Equal("32", window.Text("SizeText"));
            Assert.Equal(32, icon.Width);
            Assert.Same(page.FindResource("HeartGeometry"), icon.Data);

            window.ByName<ComboBox>("GeometryBox").SelectedIndex = 1;
            window.ByName<ComboBox>("ColorBox").SelectedIndex = 2;
            window.ByName<Slider>("SizeSlider").Value = 64;

            Assert.Same(page.FindResource("CheckGeometry"), icon.Data);
            Assert.Same(Brushes.SteelBlue, icon.Foreground);
            Assert.Equal("64", window.Text("SizeText"));
            Assert.Equal(64, icon.Height);
        }

        // ---- Menus ----

        private static void RightClick(Window window, Control control)
        {
            Dispatcher.UIThread.RunJobs();
            control.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Right);
            window.MouseUp(center, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
        }

        private static int MenuItemsWithHeader(Window window, string header)
            => window.GetVisualDescendants().OfType<MenuItem>().Count(m => header.Equals(m.Header));

        [AvaloniaFact]
        public void Right_click_raises_opening_and_opened_and_shows_the_items()
        {
            var (window, _) = PageHelpers.Open<ContextMenuPage>();
            var menu = window.ByName<Border>("Area").ContextMenu!;
            Assert.Equal(0, MenuItemsWithHeader(window, "属性"));

            RightClick(window, window.ByName<Border>("Area"));

            Assert.True(menu.IsOpen);
            Assert.Equal(1, MenuItemsWithHeader(window, "属性"));
            Assert.Equal("Opening 1 次，Opened 1 次，Closing 0 次", window.Text("LogText"));

            menu.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Opening 1 次，Opened 1 次，Closing 1 次", window.Text("LogText"));
        }

        [AvaloniaFact]
        public void Cancelling_in_opening_keeps_the_menu_closed()
        {
            var (window, _) = PageHelpers.Open<ContextMenuPage>();
            var menu = window.ByName<Border>("Area").ContextMenu!;
            window.Click(window.ByName<CheckBox>("CancelCheck"));
            Dispatcher.UIThread.RunJobs();

            RightClick(window, window.ByName<Border>("Area"));

            Assert.False(menu.IsOpen);
            Assert.Equal(0, MenuItemsWithHeader(window, "属性"));
            Assert.Equal("Opening 1 次，Opened 0 次，Closing 0 次", window.Text("LogText"));
        }

        // What the page's hint claims: Open() from code skips Opening, so the cancel box has no say.
        [AvaloniaFact]
        public void Opening_from_code_skips_opening_even_when_cancel_is_checked()
        {
            var (window, _) = PageHelpers.Open<ContextMenuPage>();
            var menu = window.ByName<Border>("Area").ContextMenu!;
            window.ByName<CheckBox>("CancelCheck").IsChecked = true;

            window.Press("OpenButton");

            Assert.True(menu.IsOpen);
            Assert.Equal("Opening 0 次，Opened 1 次，Closing 0 次", window.Text("LogText"));
        }

        // Header holds the raw "_新建": the access-key underscore shows up in the readout.
        [AvaloniaFact]
        public void Clicking_a_menu_item_reports_it_and_toggles_update_the_state_line()
        {
            var (window, page) = PageHelpers.Open<MenuPage>();
            var top = window.GetVisualDescendants().OfType<Menu>().Single().Items.OfType<MenuItem>().ToArray();
            void Choose(MenuItem parent, string name)
            {
                window.Click(parent);
                Dispatcher.UIThread.RunJobs();
                window.Click(window.ByName<MenuItem>(name));
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("粗体 = False，对齐 = 左", window.Text("StateText"));

            Choose(top[0], "NewItem");
            Assert.Equal("最近点击：_新建", window.Text("LastText"));

            Choose(top[1], "BoldItem");
            Choose(top[1], "CenterItem");
            Assert.Equal("最近点击：居中", window.Text("LastText"));
            Assert.Equal("粗体 = True，对齐 = 中", window.Text("StateText"));
            // The submenu has closed again, so look the item up in the name scope, not the visual tree.
            Assert.False(page.FindControl<MenuItem>("LeftItem")!.IsChecked);
        }

        [AvaloniaFact]
        public void Clicking_the_trigger_opens_the_flyout_and_picking_an_item_closes_it()
        {
            var (window, _) = PageHelpers.Open<MenuFlyoutPage>();
            Assert.Equal(0, MenuItemsWithHeader(window, "重命名"));

            window.Press("Trigger");
            Assert.Equal(1, MenuItemsWithHeader(window, "重命名"));
            Assert.Equal("Opened 1 次，Closed 0 次", window.Text("LogText"));

            window.Click(window.GetVisualDescendants().OfType<MenuItem>().Single(m => "重命名".Equals(m.Header)));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, MenuItemsWithHeader(window, "重命名"));
            Assert.Equal("Opened 1 次，Closed 1 次", window.Text("LogText"));
        }

        [AvaloniaFact]
        public void Placement_box_and_show_at_drive_the_same_flyout()
        {
            var (window, _) = PageHelpers.Open<MenuFlyoutPage>();
            var flyout = (MenuFlyout)window.ByName<Button>("Trigger").Flyout!;

            window.ByName<ComboBox>("PlacementBox").SelectedItem = PlacementMode.Top;
            window.Press("ShowButton");

            Assert.Equal(PlacementMode.Top, flyout.Placement);
            Assert.True(flyout.IsOpen);
            Assert.Equal("Opened 1 次，Closed 0 次", window.Text("LogText"));
        }

        // Headless exports nothing, but attaching and detaching the menu is the page's own logic.
        [AvaloniaFact]
        public void Attach_box_sets_and_clears_the_window_native_menu()
        {
            var (window, page) = PageHelpers.Open<NativeMenuPage>();
            try
            {
                Assert.Equal("NativeMenu.GetIsNativeMenuExported = False", window.Text("ExportText"));
                Assert.Equal("菜单含 1 个顶层项，点击「演示：你好」0 次", window.Text("ItemText"));
                Assert.Null(NativeMenu.GetMenu(window));

                window.Click(window.ByName<CheckBox>("AttachCheck"));
                Dispatcher.UIThread.RunJobs();
                Assert.Same(page.Built, NativeMenu.GetMenu(window));

                window.Click(window.ByName<CheckBox>("AttachCheck"));
                Dispatcher.UIThread.RunJobs();
                Assert.Null(NativeMenu.GetMenu(window));
            }
            finally
            {
                window.Close();
            }
        }

        // Avalonia 12 passes the TopLevelHost as RootVisual on detach, so "e.RootVisual is TopLevel" never matches:
        // the page has to remember the window it was attached to.
        [AvaloniaFact]
        public void Leaving_the_page_takes_the_native_menu_off_the_window()
        {
            var (window, page) = PageHelpers.Open<NativeMenuPage>();
            try
            {
                window.ByName<CheckBox>("AttachCheck").IsChecked = true;
                Assert.Same(page.Built, NativeMenu.GetMenu(window));

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.Null(NativeMenu.GetMenu(window));
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Navigation ----

        [AvaloniaFact]
        public void Carousel_buttons_step_through_the_pages_and_stop_at_both_ends()
        {
            var (window, _) = PageHelpers.Open<CarouselDemoPage>();
            Assert.Equal("SelectedIndex = 0，共 3 页", window.Text("StateText"));

            window.Press("PrevButton");
            Assert.Equal("SelectedIndex = 0，共 3 页", window.Text("StateText"));
            window.Press("NextButton");
            window.Press("NextButton");
            window.Press("NextButton");
            Assert.Equal("SelectedIndex = 2，共 3 页", window.Text("StateText"));
            window.Press("PrevButton");
            Assert.Equal("SelectedIndex = 1，共 3 页", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Carousel_transition_box_swaps_the_page_transition()
        {
            var (window, _) = PageHelpers.Open<CarouselDemoPage>();
            var carousel = window.GetVisualDescendants().OfType<Avalonia.Controls.CarouselPage>().Single();
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.Null(carousel.PageTransition);

            box.SelectedItem = "CrossFade";
            Assert.IsType<CrossFade>(carousel.PageTransition);
            box.SelectedItem = "PageSlide";
            Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.IsType<PageSlide>(carousel.PageTransition).Duration);
            box.SelectedItem = "无";
            Assert.Null(carousel.PageTransition);
        }

        [AvaloniaFact]
        public void Command_bar_buttons_report_their_label_and_the_toggle_keeps_its_state()
        {
            var (window, _) = PageHelpers.Open<CommandBarPage>();
            Assert.Equal("最近点击：（无）", window.Text("ClickText"));

            window.Press("NewButton");
            Assert.Equal("最近点击：新建", window.Text("ClickText"));

            window.Press("BoldButton");
            Assert.Equal("最近点击：加粗", window.Text("ClickText"));
            Assert.True(window.ByName<CommandBarToggleButton>("BoldButton").IsChecked);
        }

        [AvaloniaFact]
        public void Label_box_moves_the_labels()
        {
            var (window, _) = PageHelpers.Open<CommandBarPage>();
            var bar = window.ByName<CommandBar>("Bar");

            window.ByName<ComboBox>("LabelBox").SelectedItem = CommandBarDefaultLabelPosition.Collapsed;

            Assert.Equal(CommandBarDefaultLabelPosition.Collapsed, bar.DefaultLabelPosition);
        }

        // The overflow count is posted at Loaded priority after layout, so the readout lags one RunJobs behind.
        [AvaloniaFact]
        public void Narrowing_the_bar_overflows_commands_only_when_dynamic_overflow_is_on()
        {
            var (window, _) = PageHelpers.Open<CommandBarPage>();
            var slider = window.ByName<Slider>("WidthSlider");
            void Settle()
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Equal("IsDynamicOverflowEnabled = False，进入溢出菜单的主命令：0 个", window.Text("OverflowText"));

            slider.Value = 80;
            Settle();
            Assert.Equal("IsDynamicOverflowEnabled = False，进入溢出菜单的主命令：0 个", window.Text("OverflowText"));

            window.ByName<CheckBox>("OverflowCheck").IsChecked = true;
            Settle();
            Assert.Equal("IsDynamicOverflowEnabled = True，进入溢出菜单的主命令：4 个", window.Text("OverflowText"));

            slider.Value = 520;
            Settle();
            Assert.Equal("IsDynamicOverflowEnabled = True，进入溢出菜单的主命令：0 个", window.Text("OverflowText"));
        }

        [AvaloniaFact]
        public void Content_page_reports_bar_clicks_and_the_bottom_bar_can_be_removed_and_restored()
        {
            var (window, _) = PageHelpers.Open<ContentDemoPage>();
            // BottomCheck spans x = 650–841, past the right edge of the 600px window.
            window.Width = 900;
            var demo = window.GetVisualDescendants().OfType<ContentPage>().Single();
            var bottom = demo.BottomCommandBar;
            Assert.NotNull(bottom);

            window.Press("RefreshButton");
            Assert.Equal("最近点击：刷新", window.Text("ClickText"));
            window.Press("ReplyButton");
            Assert.Equal("最近点击：回复", window.Text("ClickText"));

            window.Click(window.ByName<CheckBox>("BottomCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.Null(demo.BottomCommandBar);
            Assert.DoesNotContain(window.GetVisualDescendants().OfType<CommandBarButton>(), b => b.Name == "ReplyButton");

            window.Click(window.ByName<CheckBox>("BottomCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.Same(bottom, demo.BottomCommandBar);
            Assert.Single(window.GetVisualDescendants().OfType<CommandBarButton>(), b => b.Name == "ReplyButton");
        }

        [AvaloniaFact]
        public void Alignment_boxes_place_the_content_inside_the_page()
        {
            var (window, _) = PageHelpers.Open<ContentDemoPage>();
            var demo = window.GetVisualDescendants().OfType<ContentPage>().Single();

            window.ByName<ComboBox>("HAlignBox").SelectedItem = HorizontalAlignment.Right;
            window.ByName<ComboBox>("VAlignBox").SelectedItem = VerticalAlignment.Top;

            Assert.Equal(HorizontalAlignment.Right, demo.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Top, demo.VerticalContentAlignment);
        }

        [AvaloniaFact]
        public void Open_box_opens_the_drawer_and_the_button_inside_closes_it()
        {
            var (window, _) = PageHelpers.Open<DrawerDemoPage>();
            Assert.Equal("IsOpen = False，DisplayMode = Overlay", window.Text("StateText"));
            Assert.Equal("Opened 0 次，Closed 0 次", window.Text("EventText"));

            window.Click(window.ByName<CheckBox>("OpenCheck"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("IsOpen = True，DisplayMode = Overlay", window.Text("StateText"));
            Assert.Equal("Opened 1 次，Closed 0 次", window.Text("EventText"));

            window.Press("InDrawerButton");
            Assert.Equal("IsOpen = False，DisplayMode = Overlay", window.Text("StateText"));
            Assert.Equal("Opened 1 次，Closed 1 次", window.Text("EventText"));
            Assert.False(window.ByName<CheckBox>("OpenCheck").IsChecked);
        }

        // The box only mirrors the drawer: Locked opens it on its own and the page reads that back.
        [AvaloniaFact]
        public void Locked_behavior_opens_the_drawer_inline_and_ticks_the_box()
        {
            var (window, _) = PageHelpers.Open<DrawerDemoPage>();

            window.ByName<ComboBox>("BehaviorBox").SelectedItem = DrawerBehavior.Locked;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("IsOpen = True，DisplayMode = Inline", window.Text("StateText"));
            Assert.True(window.ByName<CheckBox>("OpenCheck").IsChecked);
        }

        // The theme fills in PageSlide only once the page is on screen; the page syncs the box on Loaded.
        [AvaloniaFact]
        public void Navigation_transition_box_starts_on_the_theme_default_and_can_switch_it_off()
        {
            var (window, _) = PageHelpers.Open<NavigationDemoPage>();
            var nav = window.GetVisualDescendants().OfType<NavigationPage>().Single();
            var box = window.ByName<ComboBox>("TransitionBox");
            Assert.IsType<PageSlide>(nav.PageTransition);
            Assert.Equal("PageSlide", box.SelectedItem);

            box.SelectedItem = "无";

            Assert.Null(nav.PageTransition);
            Assert.Null(nav.ModalTransition);
        }

        // Headless never reports PageSlide as finished, so switch the transitions off before navigating.
        [AvaloniaFact]
        public async Task Push_pop_and_modal_buttons_move_the_two_stacks()
        {
            var (window, _) = PageHelpers.Open<NavigationDemoPage>();
            window.ByName<ComboBox>("TransitionBox").SelectedItem = "无";
            Assert.Equal("StackDepth = 1，CanGoBack = False，ModalStack = 0", window.Text("StateText"));

            window.Press("PopButton");
            Assert.Equal("Pushed 0 次，Popped 0 次，ModalPushed 0 次，ModalPopped 0 次", window.Text("EventText"));
            window.Press("PushButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 2，CanGoBack = True，ModalStack = 0");
            window.Press("PushButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 3，CanGoBack = True，ModalStack = 0");

            window.Press("ModalButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 3，CanGoBack = True，ModalStack = 1");
            window.Press("CloseModalButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 3，CanGoBack = True，ModalStack = 0");

            window.Press("RootButton");
            await UiHelpers.WaitUntil(() => window.Text("StateText") == "StackDepth = 1，CanGoBack = False，ModalStack = 0");
            // PopToRoot raises Popped once for each page it removes.
            Assert.Equal("Pushed 2 次，Popped 2 次，ModalPushed 1 次，ModalPopped 1 次", window.Text("EventText"));
        }

        [AvaloniaFact]
        public void Tabbed_page_buttons_change_the_selected_page_and_its_header()
        {
            var (window, _) = PageHelpers.Open<TabbedDemoPage>();
            Assert.Equal("SelectedIndex = 0，SelectedPage.Header = 收件箱", window.Text("StateText"));

            window.Press("NextButton");
            Assert.Equal("SelectedIndex = 1，SelectedPage.Header = 已发送", window.Text("StateText"));
            window.Press("NextButton");
            window.Press("NextButton");
            Assert.Equal("SelectedIndex = 2，SelectedPage.Header = 草稿", window.Text("StateText"));
            window.Press("PrevButton");
            Assert.Equal("SelectedIndex = 1，SelectedPage.Header = 已发送", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Tabbed_page_placement_and_gesture_controls_reach_the_page()
        {
            var (window, _) = PageHelpers.Open<TabbedDemoPage>();
            var tabbed = window.GetVisualDescendants().OfType<TabbedPage>().Single();
            Assert.False(tabbed.IsGestureEnabled);

            window.ByName<ComboBox>("PlacementBox").SelectedItem = TabPlacement.Bottom;
            window.Click(window.ByName<CheckBox>("GestureCheck"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(TabPlacement.Bottom, tabbed.TabPlacement);
            Assert.True(tabbed.IsGestureEnabled);
        }

        [AvaloniaFact]
        public void Code_can_select_the_disabled_tab_but_a_click_cannot()
        {
            var (window, _) = PageHelpers.Open<TabControlPage>();
            var tabs = window.ByName<TabControl>("Tabs");
            Assert.Equal("SelectedIndex = 0，选中的标签 = 概览", window.Text("StateText"));

            window.Click((Control)tabs.ContainerFromIndex(1)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedIndex = 0，选中的标签 = 概览", window.Text("StateText"));

            window.Press("DisabledButton");
            Assert.Equal("SelectedIndex = 1，选中的标签 = 详情", window.Text("StateText"));
        }

        // The template swap resets the selection to 0; the page posts the old index back after layout.
        [AvaloniaFact]
        public void Moving_the_strip_keeps_the_selected_tab()
        {
            var (window, _) = PageHelpers.Open<TabControlPage>();
            var tabs = window.ByName<TabControl>("Tabs");
            window.Click((Control)tabs.ContainerFromIndex(2)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedIndex = 2，选中的标签 = 设置", window.Text("StateText"));

            window.ByName<ComboBox>("PlacementBox").SelectedItem = Dock.Left;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Dock.Left, tabs.TabStripPlacement);
            Assert.Equal("SelectedIndex = 2，选中的标签 = 设置", window.Text("StateText"));
        }

        [AvaloniaFact]
        public void Tab_strip_selection_drives_the_page_content_line()
        {
            var (window, _) = PageHelpers.Open<TabStripPage>();
            var strip = window.ByName<TabStrip>("Strip");
            Assert.Equal("SelectedIndex = 0，当前显示「全部」的内容", window.Text("ContentText"));

            window.Click((Control)strip.ContainerFromIndex(1)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("SelectedIndex = 1，当前显示「未读」的内容", window.Text("ContentText"));

            window.Press("LastButton");
            Assert.Equal("SelectedIndex = 2，当前显示「已标星」的内容", window.Text("ContentText"));
        }

        // ---- Primitives ----

        [AvaloniaFact]
        public void Scroll_methods_move_the_value_and_raise_scroll_with_their_type()
        {
            var (window, _) = PageHelpers.Open<ScrollBarPage>();
            Assert.Equal("Value = 0（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 0 次，最近一次：（无）", window.Text("ScrollText"));

            window.Press("LineDownButton");
            Assert.Equal("Value = 1（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 1 次，最近一次：SmallIncrement", window.Text("ScrollText"));

            window.Press("PageDownButton");
            Assert.Equal("Value = 11（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 2 次，最近一次：LargeIncrement", window.Text("ScrollText"));

            window.Press("EndButton");
            Assert.Equal("Value = 100（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            // Avalonia reports ScrollToEnd as LargeIncrement, not EndScroll.
            Assert.Equal("Scroll 事件 3 次，最近一次：LargeIncrement", window.Text("ScrollText"));
            Assert.Equal(100, window.ByName<Slider>("ValueSlider").Value);

            window.Press("PageUpButton");
            Assert.Equal("Value = 90（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 4 次，最近一次：LargeDecrement", window.Text("ScrollText"));
        }

        [AvaloniaFact]
        public void Writing_value_through_the_slider_moves_the_bar_without_a_scroll_event()
        {
            var (window, _) = PageHelpers.Open<ScrollBarPage>();

            window.ByName<Slider>("ValueSlider").Value = 42;

            Assert.Equal(42, window.ByName<ScrollBar>("Bar").Value);
            Assert.Equal("Value = 42（Maximum = 100，ViewportSize = 20）", window.Text("ValueText"));
            Assert.Equal("Scroll 事件 0 次，最近一次：（无）", window.Text("ScrollText"));
        }

        [AvaloniaFact]
        public void Visibility_box_and_auto_hide_box_reach_the_scroll_bar()
        {
            var (window, _) = PageHelpers.Open<ScrollBarPage>();
            var bar = window.ByName<ScrollBar>("Bar");
            Assert.True(bar.AllowAutoHide);

            window.ByName<ComboBox>("VisibilityBox").SelectedItem = ScrollBarVisibility.Hidden;
            window.Click(window.ByName<CheckBox>("AutoHideCheck"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(ScrollBarVisibility.Hidden, bar.Visibility);
            Assert.False(bar.AllowAutoHide);
        }

        // The app runs Dark, so Default resolves to Dark here. The scope starts with Requested = Default, not null.
        [AvaloniaFact]
        public void Variant_box_sets_the_live_scope_and_default_follows_the_window()
        {
            var (window, _) = PageHelpers.Open<ThemeVariantScopePage>();
            var box = window.ByName<ComboBox>("VariantBox");
            Assert.Equal("Requested = Default，Actual = Dark", window.Text("VariantText"));
            Assert.Equal(ThemeVariant.Default, box.SelectedItem);

            box.SelectedItem = ThemeVariant.Light;
            Assert.Equal("Requested = Light，Actual = Light", window.Text("VariantText"));

            box.SelectedItem = ThemeVariant.Dark;
            Assert.Equal("Requested = Dark，Actual = Dark", window.Text("VariantText"));
            box.SelectedItem = ThemeVariant.Default;
            Assert.Equal("Requested = Default，Actual = Dark", window.Text("VariantText"));
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
        }

        [AvaloniaFact]
        public void Light_and_dark_scopes_resolve_the_same_resource_to_different_brushes()
        {
            var (window, _) = PageHelpers.Open<ThemeVariantScopePage>();

            var light = ((ISolidColorBrush)window.ByName<Border>("LightBorder").Background!).Color;
            var dark = ((ISolidColorBrush)window.ByName<Border>("DarkBorder").Background!).Color;

            Assert.NotEqual(light, dark);
        }

        [AvaloniaFact]
        public void Colour_box_and_title_box_change_the_live_swatch()
        {
            var (window, _) = PageHelpers.Open<UserControlPage>();
            var live = window.ByName<SwatchView>("Live");
            var titleBox = window.ByName<TextBox>("TitleBox");
            // Color.ToString() prints the known colour name, not the hex value.
            Assert.Equal("Live.Title = 可改的一块，Live.Fill = Gray", window.Text("StateText"));

            window.ByName<ComboBox>("ColorBox").SelectedItem = "Orange";
            Assert.Equal("Live.Title = 可改的一块，Live.Fill = Orange", window.Text("StateText"));
            Assert.Same(Brushes.Orange, live.GetVisualDescendants().OfType<Border>().First().Background);

            window.Type(titleBox, "样本");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Live.Title = 样本，Live.Fill = Orange", window.Text("StateText"));
            Assert.Equal("样本", live.GetVisualDescendants().OfType<TextBlock>().Single().Text);

            titleBox.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Live.Title = 可改的一块，Live.Fill = Orange", window.Text("StateText"));
        }

        // ---- Primitives: windows ----

        // Every test that opens windows closes the host in finally; the sample is owned, so it goes with it.
        [AvaloniaFact]
        public void Sample_window_opens_owned_and_the_close_button_closes_it()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                Assert.Equal("所有者的 OwnedWindows 数：0", window.Text("OwnerText"));
                Assert.Equal("示例窗口：未打开", window.Text("StateText"));

                window.Press("OpenButton");
                var sample = page.Sample!;
                Assert.Same(sample, Assert.Single(window.OwnedWindows));
                Assert.Equal("示例窗口", sample.Title);
                Assert.Equal("所有者的 OwnedWindows 数：1", window.Text("OwnerText"));
                Assert.StartsWith("示例窗口：IsVisible = True，WindowState = Normal，Position = ", window.Text("StateText"));
                Assert.EndsWith("大小 = 320×200，Closing 触发 0 次", window.Text("StateText"));

                window.Press("OpenButton");
                Assert.Single(window.OwnedWindows);

                window.Press("CloseButton");
                Assert.False(sample.IsVisible);
                Assert.Null(page.Sample);
                Assert.Equal("所有者的 OwnedWindows 数：0", window.Text("OwnerText"));
                Assert.Equal("示例窗口：已关闭（上次共触发 Closing 1 次）", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Blocking_keeps_the_sample_open_but_leaving_the_page_still_closes_it()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                window.Click(window.ByName<CheckBox>("BlockCheck"));
                window.Press("OpenButton");
                var sample = page.Sample!;

                window.Press("CloseButton");
                sample.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.True(sample.IsVisible);
                Assert.Equal(2, page.ClosingCount);
                Assert.EndsWith("Closing 触发 2 次", window.Text("StateText"));

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.False(sample.IsVisible);
                Assert.Null(page.Sample);
                Assert.Equal("示例窗口：已关闭（上次共触发 Closing 3 次）", page.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Property_boxes_apply_to_the_open_sample()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                window.Press("OpenButton");
                var sample = page.Sample!;

                window.ByName<CheckBox>("ResizeCheck").IsChecked = false;
                window.ByName<CheckBox>("TopmostCheck").IsChecked = true;
                window.ByName<CheckBox>("TaskbarCheck").IsChecked = false;
                window.ByName<ComboBox>("SizeBox").SelectedItem = SizeToContent.WidthAndHeight;
                window.ByName<ComboBox>("StateBox").SelectedItem = WindowState.Maximized;
                Dispatcher.UIThread.RunJobs();

                Assert.False(sample.CanResize);
                Assert.True(sample.Topmost);
                Assert.False(sample.ShowInTaskbar);
                Assert.Equal(SizeToContent.WidthAndHeight, sample.SizeToContent);
                Assert.Equal(WindowState.Maximized, sample.WindowState);
                Assert.Contains("WindowState = Maximized", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        // A title-bar minimise is simulated by setting WindowState on the sample itself.
        [AvaloniaFact]
        public void A_state_change_on_the_sample_is_mirrored_into_the_state_box()
        {
            var (window, page) = PageHelpers.Open<WindowPage>();
            try
            {
                window.Press("OpenButton");
                var sample = page.Sample!;

                sample.WindowState = WindowState.Minimized;
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(WindowState.Minimized, window.ByName<ComboBox>("StateBox").SelectedItem);
                Assert.Equal(WindowState.Minimized, sample.WindowState);
                Assert.Contains("WindowState = Minimized", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Decoration_controls_apply_to_the_system_sample()
        {
            var (window, page) = PageHelpers.Open<WindowDrawnDecorationsPage>();
            try
            {
                Assert.Equal("示例窗口：未打开", window.Text("StateText"));

                window.Press("OpenSystemButton");
                var sample = page.Sample!;
                Assert.Same(sample, Assert.Single(window.OwnedWindows));
                Assert.Equal("系统装饰示例", sample.Title);
                Assert.StartsWith("WindowDecorations = Full，ExtendClientAreaToDecorationsHint = False，", window.Text("StateText"));

                window.ByName<ComboBox>("DecorationsBox").SelectedItem = WindowDecorations.BorderOnly;
                window.ByName<CheckBox>("ExtendCheck").IsChecked = true;
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(WindowDecorations.BorderOnly, sample.WindowDecorations);
                Assert.True(sample.ExtendClientAreaToDecorationsHint);
                Assert.StartsWith("WindowDecorations = BorderOnly，ExtendClientAreaToDecorationsHint = True，", window.Text("StateText"));

                window.Press("CloseButton");
                Assert.False(sample.IsVisible);
                Assert.Equal("示例窗口：未打开", window.Text("StateText"));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Custom_title_bar_window_replaces_the_system_sample_and_reads_back_its_roles()
        {
            var (window, page) = PageHelpers.Open<WindowDrawnDecorationsPage>();
            try
            {
                window.Press("OpenSystemButton");
                var system = page.Sample!;

                window.Press("OpenCustomButton");
                var custom = page.Sample!;
                Assert.False(system.IsVisible);
                Assert.Same(custom, Assert.Single(window.OwnedWindows, w => w.IsVisible));
                Assert.Equal(WindowDecorations.None, custom.WindowDecorations);
                Assert.Equal("已标记的角色（读回）：TitleBar、MinimizeButton、MaximizeButton、CloseButton", window.Text("RolesText"));

                // The decoration box only drives the system sample.
                window.ByName<ComboBox>("DecorationsBox").SelectedItem = WindowDecorations.Full;
                Assert.Equal(WindowDecorations.None, custom.WindowDecorations);

                window.Content = null;
                Dispatcher.UIThread.RunJobs();
                Assert.False(custom.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- System ----

        // The page installs its icon on Application while it is on screen. Headless has no tray, so only the
        // page's own bookkeeping is tested; the icons are put back in finally whatever happens.
        [AvaloniaFact]
        public void Tray_icon_is_installed_while_the_page_is_shown_and_removed_when_it_leaves()
        {
            var app = Application.Current!;
            var saved = TrayIcon.GetIcons(app);
            var (window, page) = PageHelpers.Open<TrayIconPage>();
            try
            {
                var tray = page.Tray!;
                Assert.Same(tray, Assert.Single(TrayIcon.GetIcons(app)!));
                Assert.Equal("托盘图标：已挂上，IsVisible = True，ToolTipText = ControlsDemo 托盘图标，菜单项 4 个，NativeMenuExporter = 无（当前平台没有原生托盘实现）", window.Text("StateText"));

                window.Content = null;
                Dispatcher.UIThread.RunJobs();

                Assert.Null(page.Tray);
                Assert.Same(saved, TrayIcon.GetIcons(app));
                Assert.Equal("托盘图标：未挂上（页面不在屏幕上）", page.Text("StateText"));
            }
            finally
            {
                window.Close();
                TrayIcon.SetIcons(app, saved);
            }
        }

        [AvaloniaFact]
        public void Visible_box_and_tooltip_box_update_the_tray_icon()
        {
            var app = Application.Current!;
            var saved = TrayIcon.GetIcons(app);
            var (window, page) = PageHelpers.Open<TrayIconPage>();
            try
            {
                var tray = page.Tray!;

                window.Click(window.ByName<CheckBox>("VisibleCheck"));
                window.ByName<TextBox>("TipBox").Text = "新提示";
                Dispatcher.UIThread.RunJobs();

                Assert.False(tray.IsVisible);
                Assert.Equal("新提示", tray.ToolTipText);
                Assert.StartsWith("托盘图标：已挂上，IsVisible = False，ToolTipText = 新提示，", window.Text("StateText"));
            }
            finally
            {
                window.Close();
                TrayIcon.SetIcons(app, saved);
            }
        }

        // No platform exporter runs the menu here, so invoke the items' commands the way an exporter would.
        [AvaloniaFact]
        public void Tray_menu_commands_are_counted()
        {
            var app = Application.Current!;
            var saved = TrayIcon.GetIcons(app);
            var (window, page) = PageHelpers.Open<TrayIconPage>();
            try
            {
                // NativeMenuItemSeparator is a NativeMenuItem too (Header "-"), so filter it out.
                var items = page.Tray!.Menu!.Items.OfType<NativeMenuItem>().Where(i => i is not NativeMenuItemSeparator).ToArray();
                Assert.Equal(new[] { "显示", "开机提示", "退出（演示用，不会真的退出）" }, items.Select(i => i.Header));
                Assert.Equal("菜单点击：显示 0 次，开机提示 0 次（IsChecked = True），退出 0 次；图标本身被点击 0 次（Clicked）", window.Text("MenuText"));

                items[0].Command!.Execute(null);
                items[2].Command!.Execute(null);
                items[2].Command!.Execute(null);

                Assert.Equal("菜单点击：显示 1 次，开机提示 0 次（IsChecked = True），退出 2 次；图标本身被点击 0 次（Clicked）", window.Text("MenuText"));
            }
            finally
            {
                window.Close();
                TrayIcon.SetIcons(app, saved);
            }
        }
    }
}
```

- [ ] **Step 2: 运行，确认只有缺陷测试失败**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlShellTests"`
Expected: 43 个里 42 过、1 个失败——`Leaving_the_page_takes_the_native_menu_off_the_window`，`Assert.Null() Failure`（值是页面的 `NativeMenu`）。

- [ ] **Step 3: 修复原生菜单残留**

`src/Avalonia.ControlsDemo/Views/Pages/Menus/NativeMenuPage.axaml.cs`：

(a) 在 `private int _clicks;` 下面加一个字段：

```csharp
        private TopLevel? _top;
```

(b) 把

```csharp
            DetachedFromVisualTree += (_, e) =>
            {
                // Leave the window as it was, whatever the box says.
                if (AttachCheck.IsChecked == true && e.RootVisual is TopLevel top)
                {
                    NativeMenu.SetMenu(top, null);
                }
            };
            AttachedToVisualTree += (_, _) => ShowExport();
```

改成

```csharp
            DetachedFromVisualTree += (_, _) =>
            {
                // Leave the window as it was, whatever the box says. e.RootVisual is the TopLevelHost in Avalonia 12,
                // not the window, so use the top level remembered on attach.
                if (AttachCheck.IsChecked == true && _top is not null)
                {
                    NativeMenu.SetMenu(_top, null);
                }
                _top = null;
            };
            AttachedToVisualTree += (_, _) =>
            {
                _top = TopLevel.GetTopLevel(this);
                ShowExport();
            };
```

`AttachCheck.IsCheckedChanged` 处理器与 `ShowExport()` 不动。

- [ ] **Step 4: 运行，全绿；再跑整个测试项目确认没有串扰**

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests --filter "FullyQualifiedName~ControlShellTests"`
Expected: 43 passed，0 failed。

Run: `dotnet test tests/Avalonia.ControlsDemo.Tests`
Expected: 231 passed（05a 的 82 + 本批 149），0 failed，0 skipped。

Run: `dotnet build tests/Avalonia.ControlsDemo.Tests 2>&1 | grep -E "ControlShellTests|NativeMenuPage" | grep -c warning`
Expected: `0`。

- [ ] **Step 5: 提交**

```bash
git add tests/Avalonia.ControlsDemo.Tests/ControlShellTests.cs src/Avalonia.ControlsDemo/Views/Pages/Menus/NativeMenuPage.axaml.cs
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "test: add interaction tests for the media, menu, navigation, primitive and system control pages

Also take the native menu off the window when its page is left: in
Avalonia 12 the detach event's RootVisual is the TopLevelHost, so the old
check never matched.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: 破坏检查（提交之后做，每次一行，跑完 `git checkout -- <文件>`）**

| 临时改动 | 预期失败的测试 |
|---|---|
| `DrawingImagePage.axaml.cs:27` 删掉 `_circle.Brush = Palette[_next];` | `Recolor_button_cycles_the_built_drawing_through_the_palette`（Expected SeaGreen） |
| `WindowPage.axaml.cs:42` 的 `CloseSample()` → `_sample?.Close()` | `Blocking_keeps_the_sample_open_but_leaving_the_page_still_closes_it` |
| `TabControlPage.axaml.cs:22` 删掉投递的 `Tabs.SelectedIndex = selected` 那一行 | `Moving_the_strip_keeps_the_selected_tab`（Actual `SelectedIndex = 0，选中的标签 = 概览`） |
| `TrayIconPage.axaml.cs:80` 删掉 `TrayIcon.SetIcons(app, _previous)` | `Tray_icon_is_installed_while_the_page_is_shown_and_removed_when_it_leaves` |
| `ContextMenuPage.axaml.cs:18` 的 `e.Cancel = CancelCheck.IsChecked == true;` → `e.Cancel = false;` | `Cancelling_in_opening_keeps_the_menu_closed` |
| `NativeMenuPage.axaml.cs` 删掉 `_top = TopLevel.GetTopLevel(this);` | `Leaving_the_page_takes_the_native_menu_off_the_window` |

Run: `git status --short src`
Expected: 无输出。

### Task 5: 回填 spec 与全量验证

**Files:**
- Modify: `docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md`（「批 5 实测结论」末尾、`## 批 6 实测结论（执行后回填）` 之前）

**Interfaces:**
- Consumes：Task 1–4 的提交与各自实测的测试数、破坏检查结果。
- Produces：spec「批 5 实测结论」的 05b 小节。

- [ ] **Step 1: 全量验证**

Run: `dotnet build hello-avalonia.slnx 2>&1 | tail -3`
Expected: 0 个错误；警告数 ≤ 6（批开始前 6，全部来自 `src/` 里既有的警告）。

Run: `dotnet test hello-avalonia.slnx`
Expected: 全部通过，共 501 个（352 + 149），0 个跳过。

Run: `git status --short`
Expected: 只有本计划文件（若尚未提交）；`src/` 下没有改动，没有探针文件。

- [ ] **Step 2: 回填「批 5 实测结论」的 05b 小节**

在 spec 的 `## 批 6 实测结论（执行后回填）` 一行**之前**插入下面整段。测试数、警告数、破坏检查的计数按 Step 1 与 Task 1–4 的实测填写；与这里写的不同就按实测写，并在"执行时与计划不符"里说明。

````markdown
### 05b（计划 05b 执行后）

| 测试文件 | 用例数 | 覆盖的控件页 | 说明 |
|---|---|---|---|
| `ControlDataFeedbackTests` | 31 | Data display 12、Feedback 4 | 选择与读数、`ItemsPanel` 切换、`ScrollIntoView`、`ContentControl` 三种内容、DataGrid 排序、树全部展开/折叠、访问键、真实拖选文字、过渡完成计数、通知卡片（类型、点击、`MaxItems`）、Popup 轻触关闭、进度条百分比与竖向尺寸 |
| `ControlInputTests` | 42 | Input 22 | `ClickMode` 按下/松开、ButtonSpinner、单选分组、SplitButton 主次部件、三态循环、日历选择模式与停用日期、日期/时间格式与部件显隐、父子复选框、调色板选色、可编辑 ComboBox、NumericUpDown 夹取、滑块吸附、AutoCompleteBox 过滤、掩码输入、TextBox 的 `MaxLength`/只读/回车/密码 |
| `ControlLayoutTests` | 33 | Layout 19（`GroupBoxPage` 只靠 H1） | 结构体属性的真实值与子元素位置、Expander 方向、Flyout 四个方位、PipsPager、刷新、滚动偏移与夹取、SplitView 两种模式、各面板的真实排列、GridSplitter 拖拽/预览/键盘 |
| `ControlShellTests` | 43 | Media 3、Menus 4、Navigation 8、Primitives 5、System 1（`SeparatorPage` 只靠 H1） | 右键菜单与取消、菜单与 MenuFlyout、原生菜单挂上/摘下、CommandBar 溢出、NavigationPage 栈与模态栈、TabControl 禁用页与换位保选中、ScrollBar 的 `Scroll` 事件类型、主题作用域、示例窗口（拦截关闭、离开页面强制关闭、属性下发）、托盘图标挂上与还原 |

执行时的实测：构建警告数 6（批开始前 6）；`dotnet test hello-avalonia.slnx` 共 501 个测试通过（05a 结束时的 352 + 本批 149）；破坏检查：Task 1 五项、Task 2 五项、Task 3 五项、Task 4 六项，每项恰好让目标测试失败（`ColorPickerPage` 的 `NewColor` → `OldColor` 是 2 个），均已还原。

**`src/` 改动（用户 2026-10-17 确认，6 处，均先写会失败的测试再修）**：

| 页面 | 缺陷 | 修复前（实测） | 修复 |
|---|---|---|---|
| `CalendarDatePickerPage` | 清空格式框或只剩 `Q` 时，`CustomDateFormatString` 抛出的异常从 `TextChanged` 漏出，应用崩溃 | `FormatException` / `ArgumentException` | 空串与无效格式保留上一个有效格式 |
| `NativeMenuPage` | 离开页面后菜单仍挂在窗口上：v12 的 `e.RootVisual` 是 `TopLevelHost` | `GetMenu(window)` 仍是 `Built` | 挂上时记下 `TopLevel`，离开时用它摘下 |
| `PipsPagerPage` | 复选框初始不勾、提示写"默认隐藏"，v12 默认显示 | 复选框 False，控件 True | XAML `IsChecked="True"`，改提示 |
| `ToggleButtonPage` | 三态为 null 时读数 `IsChecked = ` | 读数缺 `null` | `FallbackValue` → `TargetNullValue` |
| `CheckBoxPage` | 点已全选的父项进入"部分选中"、子项不清空 | 父项 null，文字 `父项：全选` | 去掉父项的 `IsThreeState` |
| `ProgressBarPage` | 竖向时主题的 `MinHeight = 200` 压过 `Height = 100` | 20×200 | 竖向 `MinHeight = 0`，横向 `ClearValue` |

**对后续批的决定与发现**（计划阶段在探针里实测，执行阶段原样复现）：

- **Avalonia 12 的默认值与 API 行为变了，旧写法静默失效**：本批 6 个缺陷里 3 个属于这一类（`RootVisual` 的类型、`PipsPager` 按钮默认显示、竖向 `ProgressBar` 的 `MinHeight`），加上 05a 的 `ImmutableCrossFade`。都不抛异常、不写日志，只有断言真实状态才看得见。批 6（MusicStore、WebViewDemo、HtmlRendererDemo）里凡是依赖默认值或事件参数类型的代码，读代码时先查一遍。
- **弹出层一律在叠加层里找**，并写"打开前 0 个、打开后恰好 N 个"；关上的子菜单项、折叠的 Expander 内容不在视觉树里，用 `FindControl` 或先展开。
- **可见性断言要防"永远通过"**：`ToggleSwitch` 靠 `Opacity`、PipsPager 第一页的「上一页」靠 `Opacity` 与禁用，`IsEffectivelyVisible` 都一直为真。
- **键盘测试每次按键前重新聚焦目标**：点过复选框或按钮后焦点就不在原控件上了，方向键静默无效。
- **`TextChanged` 里的异常从输入调用本身抛出**，`Record.Exception` 要包住 `Type`/`KeyPress`。
- **`Color.ToString()` 对已知颜色返回名字**；运行时异常消息的尾巴随 UI 语言变化，`CultureScope` 管不到，用 `StartsWith`。
- **并行探针**：本批第一次用 4 个 git worktree 并行做探针（每组一个），互不争用 `obj/`、破坏检查可以安全地改 `src/`；首次构建每个 worktree 约 5 分钟（还原包），之后约 20 秒。
- **只靠 H1 的页面**：`GroupBoxPage`、`SeparatorPage`（纯 XAML）。页面里有意不测的部分：悬停计时的 ToolTip、动画中间帧、触摸滑动、DataGrid 编辑与列拖拽、`NavigateUri`、`RepeatButton` 的真实重复、真实托盘与原生菜单导出。

执行时与计划不符、改了测试的地方：

- （执行时填写；没有就写"无"。）
````

最后那一条 `（执行时填写；没有就写"无"。）` 在执行时**必须**替换成实际情况，不要原样留下。

- [ ] **Step 3: 提交**

```bash
git add docs/superpowers/specs/2026-10-12-demo-test-coverage-design.md
git -c user.name=anyu -c user.email=glepooek@outlook.com commit -m "docs: record batch 5b results

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec 覆盖（批 5 后半）**

| spec 要求 | 落在哪里 |
|---|---|
| H3：有交互的页面至少一条"操作 → 界面变化" | 80 个控件页里 78 页有 H3（Task 1 16 页、Task 2 22 页、Task 3 19 页、Task 4 21 页）；剩下 2 页是纯 XAML，理由写在覆盖清单与回填里 |
| H3 的"至少一条"是下限，由 plan 读过页面代码后列出 | 每个任务开头的"逐页覆盖"列出每页的用例数 |
| 真实时间/动画只断言状态，不断言中间态 | 过渡、SplitView、刷新、通知一律 `WaitUntil` 最终状态；NavigationPage 先关掉过渡 |
| 测试发现的真实缺陷经用户确认后单列任务修复，先写会失败的测试 | 6 处，用户 2026-10-17 确认；Task 1–4 的 Step 2（红）→ Step 3（修）→ Step 4（绿） |
| 改进程级状态的页面要有"还原"测试 | `TrayIconPage`（`Tray_icon_is_installed_…_removed_when_it_leaves`，有破坏检查）；主题作用域页不改应用主题 |
| 每批回填「实测结论」 | Task 5 |
| 验证判据：构建 0 错误且警告不增、全部测试通过、`src/` 只有确认过的修复 | Task 5 Step 1；各任务 Step 6 末尾 `git status --short src` |

**2. 占位符**：测试代码块是 `C:\Temp\probe-b5b\` 里的成品文件原样嵌入；Task 5 回填段里"执行时与计划不符"一条是执行时必须写的记录，Step 2 明确要求替换。

**3. 名称一致性**：4 个文件只用 05a 的 `PageHelpers`（`Open`、`Text`、`Press`、`PressText`）与共享库已有成员；各文件的私有辅助互不引用。Task 2 的缺陷测试名与 Step 2 的 Expected 一致；Task 4 Step 4 的 231 = 82 + 31 + 42 + 33 + 43。

**4. Review Focus**：5 条各有测试。
- 第 1 条：Task 2 `Editing_the_custom_format_box_never_throws`，先红后绿，有破坏检查。
- 第 2 条：Task 4 `Leaving_the_page_takes_the_native_menu_off_the_window`，先红后绿，有破坏检查。
- 第 3 条：Task 3 `Pager_button_check_boxes_start_in_sync_with_the_pager` 与 `Next_check_box_hides_and_shows_the_next_button`；Task 2 `Clicking_a_fully_checked_parent_clears_every_child`。
- 第 4 条：Task 4 的 `Blocking_keeps_the_sample_open_but_leaving_the_page_still_closes_it`、`Tray_icon_is_installed_…`，两条都有破坏检查。
- 第 5 条：Task 4 `Content_page_reports_bar_clicks_…`（先 `Width = 900`）；Task 2 `Toggle_switches_report_their_state_and_swap_on_and_off_content`（断言 `Opacity` 前后两半）。
