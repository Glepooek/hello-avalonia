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
