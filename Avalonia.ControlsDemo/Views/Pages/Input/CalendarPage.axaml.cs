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
