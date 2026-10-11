using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WrapPanelPage : UserControl
    {
        public WrapPanelPage()
        {
            InitializeComponent();

            OrientationBox.ItemsSource = Enum.GetValues<Orientation>();
            OrientationBox.SelectedItem = Wrap.Orientation;
            OrientationBox.SelectionChanged += (_, _) =>
            {
                if (OrientationBox.SelectedItem is Orientation orientation)
                {
                    Wrap.Orientation = orientation;
                }
            };
            ItemGapSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Wrap.ItemSpacing = ItemGapSlider.Value;
                }
            };
            LineGapSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Wrap.LineSpacing = LineGapSlider.Value;
                }
            };
            Wrap.ItemSpacing = ItemGapSlider.Value;
            Wrap.LineSpacing = LineGapSlider.Value;
        }
    }
}
