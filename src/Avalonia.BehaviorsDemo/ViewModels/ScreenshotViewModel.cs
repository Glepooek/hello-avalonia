using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Shared.ViewModels;
using Avalonia.Xaml.Interactions.Custom;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public class ScreenshotViewModel : ViewModelBase
    {
        public CounterRenderer Renderer { get; } = new();

        // The screenshot file name points into the temp sandbox, never into the user's folders.
        public string ShotPath { get; } = Path.Combine(Path.GetTempPath(), "avalonia-behaviors-demo", "screenshot.png");
    }

    /// <summary>
    /// Draws a square whose colour changes on every call, so each render is visible in the preview.
    /// </summary>
    public sealed class CounterRenderer : IRenderTargetBitmapRenderer
    {
        private static readonly IBrush[] Palette =
        {
            Brushes.DodgerBlue, Brushes.OrangeRed, Brushes.SeaGreen, Brushes.MediumPurple, Brushes.Goldenrod,
        };

        public int Calls { get; private set; }

        public void Render(DrawingContext context, TimeSpan elapsed)
        {
            var brush = Palette[Calls % Palette.Length];
            Calls++;
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 64, 64));
            context.DrawRectangle(brush, null, new Rect(8, 8, 48, 48));
        }
    }
}
