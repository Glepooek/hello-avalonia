using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class GridPage : UserControl
    {
        public GridPage()
        {
            InitializeComponent();

            LinesCheck.IsCheckedChanged += (_, _) => Demo.ShowGridLines = LinesCheck.IsChecked == true;
            SpacingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Demo.RowSpacing = SpacingSlider.Value;
                    Demo.ColumnSpacing = SpacingSlider.Value;
                }
            };
        }
    }
}
