using System;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class EasingPage : UserControl
    {
        // Easing.Parse resolves these names, so the list is also the set of strings usable in XAML.
        private static readonly string[] Names =
        {
            "LinearEasing",
            "CubicEaseIn", "CubicEaseOut", "CubicEaseInOut",
            "BackEaseIn", "BackEaseOut", "BackEaseInOut",
            "ElasticEaseIn", "ElasticEaseOut", "ElasticEaseInOut",
            "BounceEaseIn", "BounceEaseOut", "BounceEaseInOut",
        };

        private const double CurveWidth = 300;
        private const double TrackLength = 276;
        private bool _atEnd;

        public EasingPage()
        {
            InitializeComponent();

            EasingBox.ItemsSource = Names;
            EasingBox.SelectionChanged += (_, _) => Redraw();
            EasingBox.SelectedIndex = 3;
        }

        private Easing Current => Easing.Parse(Names[Math.Max(EasingBox.SelectedIndex, 0)]);

        private void Redraw()
        {
            var easing = Current;

            // Sample the function itself: x is time (0..1), y is the eased value.
            // The value range is drawn as y = 130 (value 0) up to y = 30 (value 1); overshoot leaves that band.
            Curve.Points = new Avalonia.Collections.AvaloniaList<Point>(
                Enumerable.Range(0, 61).Select(i =>
                {
                    var t = i / 60.0;
                    return new Point(t * CurveWidth, 130 - easing.Ease(t) * 100);
                }));

            Samples.Text = $"Ease(0.25)={easing.Ease(0.25):F3}   Ease(0.5)={easing.Ease(0.5):F3}   Ease(0.75)={easing.Ease(0.75):F3}";
        }

        private void OnRun(object? sender, RoutedEventArgs e)
        {
            // Rebuild the transition each run so it picks up the currently selected easing.
            Ball.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Canvas.LeftProperty,
                    Duration = TimeSpan.FromMilliseconds(1200),
                    Easing = Current,
                },
            };

            _atEnd = !_atEnd;
            Canvas.SetLeft(Ball, _atEnd ? TrackLength : 0);
        }
    }
}
