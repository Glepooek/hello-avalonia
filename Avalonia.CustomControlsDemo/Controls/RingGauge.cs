using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Draws a ring whose filled arc follows <see cref="Value"/>.
    /// </summary>
    public class RingGauge : Control
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<RingGauge, double>(nameof(Value));

        static RingGauge()
        {
            // Without this the property changes but Render is never called again.
            AffectsRender<RingGauge>(ValueProperty);
        }

        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        // Self-drawn controls declare their own size; nothing else can measure them.
        protected override Size MeasureOverride(Size availableSize) => new(120, 120);

        public override void Render(DrawingContext context)
        {
            var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 8;

            // The track.
            context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.Parse("#404050")), 10), centre, radius, radius);

            // The arc. 99.99 never divides the full circle, which would make the two ends coincide.
            var sweep = Math.Clamp(Value, 0, 99.99) / 100 * 2 * Math.PI;
            if (sweep > 0)
            {
                var start = new Point(centre.X, centre.Y - radius);
                var end = new Point(centre.X + radius * Math.Sin(sweep), centre.Y - radius * Math.Cos(sweep));
                var arc = new StreamGeometry();
                using (var g = arc.Open())
                {
                    g.BeginFigure(start, false);
                    g.ArcTo(end, new Size(radius, radius), 0, sweep > Math.PI, SweepDirection.Clockwise);
                    g.EndFigure(false);
                }

                context.DrawGeometry(null, new Pen(Brushes.OrangeRed, 10, lineCap: PenLineCap.Round), arc);
            }

            var text = new FormattedText($"{Value:F0}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 22, Brushes.White);
            context.DrawText(text, new Point(centre.X - text.Width / 2, centre.Y - text.Height / 2));
        }
    }
}
