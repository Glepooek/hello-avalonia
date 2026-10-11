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
