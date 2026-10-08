using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class RenderSnapshotTests
    {
        // Reads one pixel out of a captured frame. Needs Skia drawing (see TestAppBuilder), otherwise the bitmap is 1x1.
        private static Color PixelAt(WriteableBitmap frame, int x, int y)
        {
            using var fb = frame.Lock();
            var bytes = new byte[4];
            var offset = y * fb.RowBytes + x * 4;
            System.Runtime.InteropServices.Marshal.Copy(fb.Address + offset, bytes, 0, 4);
            // The byte order depends on the frame's own pixel format, so read it instead of assuming one.
            return fb.Format == PixelFormat.Bgra8888
                ? Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0])
                : Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }

        [AvaloniaFact]
        public void A_captured_frame_has_the_window_size()
        {
            var window = new Window { Width = 200, Height = 100, Content = new Border { Background = Brushes.Red } };
            window.Show();

            var frame = window.CaptureRenderedFrame();

            Assert.NotNull(frame);
            Assert.Equal(200, frame!.PixelSize.Width);
            Assert.Equal(100, frame.PixelSize.Height);
        }

        [AvaloniaFact]
        public void A_filled_border_renders_its_colour()
        {
            var window = new Window { Width = 200, Height = 100, Content = new Border { Background = Brushes.Red } };
            window.Show();

            var frame = window.CaptureRenderedFrame()!;

            Assert.Equal(Colors.Red, PixelAt(frame, 100, 50));
        }

        [AvaloniaFact]
        public void Two_halves_render_two_colours()
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            grid.Children.Add(new Border { Background = Brushes.Blue });
            var right = new Border { Background = Brushes.Lime };
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
            var window = new Window { Width = 200, Height = 100, Content = grid };
            window.Show();

            var frame = window.CaptureRenderedFrame()!;

            Assert.Equal(Colors.Blue, PixelAt(frame, 50, 50));
            Assert.Equal(Colors.Lime, PixelAt(frame, 150, 50));
        }
    }
}
