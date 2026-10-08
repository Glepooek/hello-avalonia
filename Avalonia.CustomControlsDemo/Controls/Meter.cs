using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Controls
{
    // Attributes are documentation for tooling; the code below does not depend on them.
    [TemplatePart("PART_Fill", typeof(Border))]
    [PseudoClasses(":full")]
    public class Meter : TemplatedControl
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<Meter, double>(nameof(Value),
                // The coerce callback receives an AvaloniaObject, not a Meter, so cast before reading Maximum.
                coerce: (o, v) => Math.Clamp(v, 0, ((Meter)o).Maximum));

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<Meter, double>(nameof(Maximum), 100);

        // A computed value with no backing styled property: cheaper, and it cannot be set from XAML.
        public static readonly DirectProperty<Meter, string> DisplayProperty =
            AvaloniaProperty.RegisterDirect<Meter, string>(nameof(Display), m => m.Display);

        private Border? _fill;
        private string _display = "0%";

        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        public string Display { get => _display; private set => SetAndRaise(DisplayProperty, ref _display, value); }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Always re-resolve on every apply: a new theme means new parts.
            _fill = e.NameScope.Find<Border>("PART_Fill");
            Refresh();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ValueProperty || change.Property == MaximumProperty)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var ratio = Maximum <= 0 ? 0 : Math.Clamp(Value / Maximum, 0, 1);
            Display = $"{ratio:P0}";
            PseudoClasses.Set(":full", ratio >= 1);

            // Null after a retemplate whose theme has no PART_Fill: guard rather than assume.
            if (_fill != null)
            {
                _fill.RenderTransform = new ScaleTransform(ratio, 1);
            }
        }
    }
}
