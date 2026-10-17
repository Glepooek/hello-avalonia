using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.HtmlRendererDemo.ViewModels;
using Avalonia.HtmlRendererDemo.Views;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using System.Runtime.InteropServices;
using TheArtOfDev.HtmlRenderer.Avalonia;
using Xunit;

namespace Avalonia.HtmlRendererDemo.Tests
{
    public class RenderTests
    {
        private static MainWindow Open(bool withViewModel)
        {
            var window = new MainWindow();
            if (withViewModel)
                window.DataContext = new MainWindowViewModel();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return window;
        }

        // Counts near-black pixels inside the HtmlPanel; the panel paints a white background, so only glyphs are dark.
        private static int DarkPixelsInPanel(Window window)
        {
            var panel = window.GetVisualDescendants().OfType<HtmlPanel>().Single();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var frame = window.CaptureRenderedFrame()!;
            var origin = panel.TranslatePoint(default, window)!.Value;
            using var buffer = frame.Lock();
            var dark = 0;
            for (var y = (int)origin.Y; y < (int)(origin.Y + panel.Bounds.Height); y++)
            {
                for (var x = (int)origin.X; x < (int)(origin.X + panel.Bounds.Width); x++)
                {
                    var pixel = Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
                    if ((pixel & 0xff) < 100 && ((pixel >> 8) & 0xff) < 100 && ((pixel >> 16) & 0xff) < 100)
                        dark++;
                }
            }
            return dark;
        }

        [AvaloniaFact]
        public void Html_panel_draws_the_bound_markup()
        {
            var empty = Open(withViewModel: false);
            var withText = Open(withViewModel: true);
            try
            {
                Assert.Equal(0, DarkPixelsInPanel(empty));
                Assert.True(DarkPixelsInPanel(withText) > 500);
            }
            finally
            {
                empty.Close();
                withText.Close();
            }
        }

        // A wrong family inside the avares:// collection throws, but a family that never goes through the collection
        // (for example the bare name "iconfont") silently falls back to a system font, which has no glyph at U+E71A.
        // So the test checks which typeface actually shaped the icon.
        [AvaloniaFact]
        public void Icon_text_is_shaped_with_the_bundled_icon_font()
        {
            var window = Open(withViewModel: true);
            try
            {
                var icon = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "");
                var run = icon.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>().Single();

                Assert.Equal("iconfont", run.GlyphRun.GlyphTypeface.FamilyName);
                Assert.All(run.GlyphRun.GlyphInfos, g => Assert.NotEqual(0, (int)g.GlyphIndex));
            }
            finally
            {
                window.Close();
            }
        }
    }
}
