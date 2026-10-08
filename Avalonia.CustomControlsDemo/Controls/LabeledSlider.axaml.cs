using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.CustomControlsDemo.Controls
{
    public partial class LabeledSlider : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<LabeledSlider, string>(nameof(Label), "");

        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<LabeledSlider, double>(nameof(Minimum), 0);

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<LabeledSlider, double>(nameof(Maximum), 100);

        // TwoWay by default: the internal Slider writes back into this property.
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<LabeledSlider, double>(nameof(Value), 0,
                defaultBindingMode: BindingMode.TwoWay);

        public LabeledSlider() => InitializeComponent();

        public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    }
}
