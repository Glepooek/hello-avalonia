using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ProgressBarPage : UserControl
    {
        public ProgressBarPage()
        {
            InitializeComponent();

            ValueSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Bar.Value = ValueSlider.Value;
                    RangeBar.Value = ValueSlider.Value - 50;
                }
            };
            IndeterminateCheck.IsCheckedChanged += (_, _) => Bar.IsIndeterminate = IndeterminateCheck.IsChecked == true;
            TextCheck.IsCheckedChanged += (_, _) => Bar.ShowProgressText = TextCheck.IsChecked == true;
            VerticalCheck.IsCheckedChanged += (_, _) =>
            {
                var vertical = VerticalCheck.IsChecked == true;
                Bar.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
                Bar.Width = vertical ? 20 : 280;
                Bar.Height = vertical ? 100 : double.NaN;
            };

            // Percentage is only recomputed once the bar has been laid out, so the readout listens to it
            // instead of reading it right after Value is set (that would show the stale 0%).
            Bar.PropertyChanged += (_, e) =>
            {
                if (e.Property == ProgressBar.PercentageProperty)
                {
                    Show();
                }
            };
            RangeBar.PropertyChanged += (_, e) =>
            {
                if (e.Property == ProgressBar.PercentageProperty)
                {
                    Show();
                }
            };

            Bar.Value = ValueSlider.Value;
            RangeBar.Value = ValueSlider.Value - 50;
            Show();
        }

        private void Show()
        {
            PercentText.Text = $"Value = {Bar.Value:0}，Percentage = {Bar.Percentage:0.#}%";
            RangeText.Text = $"Value = {RangeBar.Value:0}，Percentage = {RangeBar.Percentage:0.#}%";
        }
    }
}
