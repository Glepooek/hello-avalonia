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
