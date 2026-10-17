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
            Picker.CustomDateFormatString = CustomFormat.Text ?? string.Empty;

            Picker.SelectedDateChanged += (_, e) =>
                PickerResult.Text = e.AddedItems.Count > 0 ? $"已选：{Picker.SelectedDate:yyyy-MM-dd}" : "未选择";

            RangedPicker.DisplayDateStart = new DateTime(2026, 1, 1);
            RangedPicker.DisplayDateEnd = new DateTime(2026, 12, 31);
        }
    }
}
