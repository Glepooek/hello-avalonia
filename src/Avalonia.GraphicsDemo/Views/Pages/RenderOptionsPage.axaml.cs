using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class RenderOptionsPage : UserControl
    {
        public RenderOptionsPage()
        {
            InitializeComponent();

            var checker = CreateChecker();
            Pixels.Source = checker;
            BlendBottom.Source = checker;
            BlendTop.Source = checker;

            // The drop-downs list the enum values themselves, so they cannot drift from the framework.
            InterpolationBox.ItemsSource = Enum.GetValues<BitmapInterpolationMode>();
            InterpolationBox.SelectedItem = BitmapInterpolationMode.None;
            InterpolationBox.SelectionChanged += (_, _) =>
            {
                if (InterpolationBox.SelectedItem is BitmapInterpolationMode mode)
                {
                    RenderOptions.SetBitmapInterpolationMode(Pixels, mode);
                }
            };
            RenderOptions.SetBitmapInterpolationMode(Pixels, BitmapInterpolationMode.None);

            BlendBox.ItemsSource = Enum.GetValues<BitmapBlendingMode>();
            BlendBox.SelectedItem = BitmapBlendingMode.SourceOver;
            BlendBox.SelectionChanged += (_, _) =>
            {
                if (BlendBox.SelectedItem is BitmapBlendingMode mode)
                {
                    RenderOptions.SetBitmapBlendingMode(BlendTop, mode);
                }
            };
        }

        // An 8x8 two-colour checkerboard, built in memory so the page needs no image file.
        private static WriteableBitmap CreateChecker()
        {
            const int size = 8;
            var bitmap = new WriteableBitmap(new PixelSize(size, size), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);

            var pixels = new int[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = (x + y) % 2 == 0 ? unchecked((int)0xFFE8974A) : unchecked((int)0xFF3A6FB0);
                }
            }

            using var frame = bitmap.Lock();
            Marshal.Copy(pixels, 0, frame.Address, pixels.Length);
            return bitmap;
        }
    }
}
