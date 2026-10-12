using System.Collections.Generic;
using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.GraphicsDemo.Controls
{
    /// <summary>
    /// Draws one fixed scene in <see cref="Render"/>, optionally wrapped in clip / transform / opacity scopes.
    /// </summary>
    public class Sketch : Control
    {
        public static readonly StyledProperty<bool> UseClipProperty =
            AvaloniaProperty.Register<Sketch, bool>(nameof(UseClip));

        public static readonly StyledProperty<bool> UseTransformProperty =
            AvaloniaProperty.Register<Sketch, bool>(nameof(UseTransform));

        public static readonly StyledProperty<bool> UseOpacityProperty =
            AvaloniaProperty.Register<Sketch, bool>(nameof(UseOpacity));

        static Sketch()
        {
            // Without this a toggle changes the property but the control keeps showing the old drawing.
            AffectsRender<Sketch>(UseClipProperty, UseTransformProperty, UseOpacityProperty);
        }

        public bool UseClip { get => GetValue(UseClipProperty); set => SetValue(UseClipProperty, value); }

        public bool UseTransform { get => GetValue(UseTransformProperty); set => SetValue(UseTransformProperty, value); }

        public bool UseOpacity { get => GetValue(UseOpacityProperty); set => SetValue(UseOpacityProperty, value); }

        protected override Size MeasureOverride(Size availableSize) => new(260, 150);

        public override void Render(DrawingContext context)
        {
            var area = new Rect(Bounds.Size);
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#20FFFFFF")), new Pen(Brushes.Gray, 1), area.Deflate(0.5));

            // Each Push* returns a disposable scope. Anything drawn until it is disposed is affected;
            // disposing in reverse order restores the previous state.
            var scopes = new Stack<IDisposable>();
            if (UseClip)
            {
                scopes.Push(context.PushClip(new Rect(0, 0, area.Width / 2, area.Height)));
            }

            if (UseTransform)
            {
                scopes.Push(context.PushTransform(Matrix.CreateRotation(Math.PI / 18) * Matrix.CreateTranslation(20, 0)));
            }

            if (UseOpacity)
            {
                scopes.Push(context.PushOpacity(0.4));
            }

            DrawScene(context);

            while (scopes.Count > 0)
            {
                scopes.Pop().Dispose();
            }
        }

        private static void DrawScene(DrawingContext context)
        {
            context.DrawRectangle(Brushes.SteelBlue, new Pen(Brushes.White, 2), new Rect(20, 20, 90, 60), 8, 8);
            context.DrawEllipse(Brushes.Orange, null, new Point(150, 50), 30, 30);
            context.DrawLine(new Pen(Brushes.LimeGreen, 4, lineCap: PenLineCap.Round), new Point(20, 110), new Point(240, 125));

            var text = new FormattedText("DrawingContext", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 16, Brushes.White);
            context.DrawText(text, new Point(130, 85));
        }
    }
}
