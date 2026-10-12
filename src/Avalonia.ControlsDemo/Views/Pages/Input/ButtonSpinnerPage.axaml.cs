using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ButtonSpinnerPage : UserControl
    {
        private int _value;

        public ButtonSpinnerPage()
        {
            InitializeComponent();
        }

        // ButtonSpinner only reports a direction; keeping the number is the caller's job.
        private void OnCounterSpin(object? sender, SpinEventArgs e)
        {
            _value += e.Direction == SpinDirection.Increase ? 1 : -1;
            CounterText.Text = _value.ToString();
            SpinLog.Text = $"最近一次：{e.Direction}";
        }
    }
}
