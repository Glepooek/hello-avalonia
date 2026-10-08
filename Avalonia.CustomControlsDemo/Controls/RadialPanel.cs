using System;
using Avalonia.Controls;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Places children evenly on a circle, clockwise from <see cref="StartAngle"/> (0 = twelve o'clock).
    /// </summary>
    public class RadialPanel : Panel
    {
        public static readonly StyledProperty<double> RadiusProperty =
            AvaloniaProperty.Register<RadialPanel, double>(nameof(Radius), 100);

        public static readonly StyledProperty<double> StartAngleProperty =
            AvaloniaProperty.Register<RadialPanel, double>(nameof(StartAngle), 0);

        static RadialPanel()
        {
            // Radius changes the desired size; the angle only moves children inside the same size.
            AffectsMeasure<RadialPanel>(RadiusProperty);
            AffectsArrange<RadialPanel>(StartAngleProperty);
        }

        public double Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

        public double StartAngle { get => GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

        protected override Size MeasureOverride(Size availableSize)
        {
            var largest = 0.0;
            foreach (var child in Children)
            {
                // Children get unlimited room; the panel's own size comes from the radius.
                child.Measure(Size.Infinity);
                largest = Math.Max(largest, Math.Max(child.DesiredSize.Width, child.DesiredSize.Height));
            }

            var side = 2 * Radius + largest;
            return new Size(side, side);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var centre = new Point(finalSize.Width / 2, finalSize.Height / 2);
            var count = Children.Count;
            for (var i = 0; i < count; i++)
            {
                var child = Children[i];
                var angle = (StartAngle + i * 360.0 / count) * Math.PI / 180;
                var x = centre.X + Radius * Math.Sin(angle) - child.DesiredSize.Width / 2;
                var y = centre.Y - Radius * Math.Cos(angle) - child.DesiredSize.Height / 2;
                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }

            return finalSize;
        }
    }
}
