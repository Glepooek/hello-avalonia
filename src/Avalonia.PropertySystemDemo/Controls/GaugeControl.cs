using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.PropertySystemDemo.Controls
{
    /// <summary>
    /// One control carrying all three property kinds, so the pages can point
    /// at the same object and talk about different registration styles.
    /// </summary>
    public class GaugeControl : TemplatedControl
    {
        /// <summary>
        /// A styled property: reachable from styles, and able to coerce.
        /// The coerce callback runs on every write, whatever the source.
        /// </summary>
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<GaugeControl, double>(
                nameof(Value),
                defaultValue: 0d,
                coerce: static (_, v) => Math.Clamp(v, 0d, 100d));

        public static readonly StyledProperty<string?> CaptionProperty =
            AvaloniaProperty.Register<GaugeControl, string?>(nameof(Caption));

        private string _readout = "0%";

        /// <summary>
        /// A direct property: a plain CLR field with change notification bolted
        /// on. Cheaper to read than a styled property, but styles cannot set it.
        /// </summary>
        public static readonly DirectProperty<GaugeControl, string> ReadoutProperty =
            AvaloniaProperty.RegisterDirect<GaugeControl, string>(
                nameof(Readout),
                o => o._readout);

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string? Caption
        {
            get => GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public string Readout
        {
            get => _readout;
            private set => SetAndRaise(ReadoutProperty, ref _readout, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // The change callback is where a derived value gets recomputed.
            if (change.Property == ValueProperty)
            {
                Readout = $"{Value:F0}%";
            }
        }
    }
}
