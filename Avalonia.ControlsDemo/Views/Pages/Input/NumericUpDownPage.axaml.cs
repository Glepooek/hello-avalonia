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
