using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ToolTipPage : UserControl
    {
        public ToolTipPage()
        {
            InitializeComponent();

            DelaySlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    var delay = (int)DelaySlider.Value;
                    DelayText.Text = delay.ToString();
                    ToolTip.SetShowDelay(Target, delay);
                }
            };

            PlacementBox.ItemsSource = new[] { PlacementMode.Bottom, PlacementMode.Top, PlacementMode.Left, PlacementMode.Right, PlacementMode.Pointer };
            PlacementBox.SelectedItem = ToolTip.GetPlacement(Target);
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is PlacementMode mode)
                {
                    ToolTip.SetPlacement(Target, mode);
                }
            };

            OpenButton.Click += (_, _) =>
            {
                ToolTip.SetIsOpen(Target, !ToolTip.GetIsOpen(Target));
                StateText.Text = $"IsOpen = {ToolTip.GetIsOpen(Target)}";
            };
        }
    }
}
