using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class StackPanelPage : UserControl
    {
        public StackPanelPage()
        {
            InitializeComponent();

            OrientationBox.ItemsSource = Enum.GetValues<Orientation>();
            OrientationBox.SelectedItem = Stack.Orientation;
            OrientationBox.SelectionChanged += (_, _) =>
            {
                if (OrientationBox.SelectedItem is Orientation orientation)
                {
                    Stack.Orientation = orientation;
                }
            };
            SpacingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Stack.Spacing = SpacingSlider.Value;
                }
            };
        }
    }
}
