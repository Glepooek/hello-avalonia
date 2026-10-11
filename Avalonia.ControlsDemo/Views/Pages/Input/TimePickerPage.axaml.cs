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
