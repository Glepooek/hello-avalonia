using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Builds sample bitmaps in code, so the Image pages need no binary asset and nothing to decode.</summary>
    public static class DemoImages
    {
        /// <summary>A horizontal blue-to-orange gradient, so Stretch and interpolation changes are easy to see.</summary>
        public static WriteableBitmap Gradient(int width, int height)
        {
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            var pixels = new int[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var t = width <= 1 ? 0 : x / (double)(width - 1);
                    var r = (int)(0x33 + t * (0xE8 - 0x33));
                    var g = (int)(0x66 + t * (0x97 - 0x66));
                    var b = (int)(0xCC + t * (0x4A - 0xCC));
                    // A dark diagonal makes the aspect ratio visible when the image is stretched.
                    if (x * height / width == y)
                    {
                        r = g = b = 0x20;
                    }

                    pixels[y * width + x] = unchecked((int)(0xFF000000 | (uint)(r << 16) | (uint)(g << 8) | (uint)b));
                }
            }

            using var frame = bitmap.Lock();
            Marshal.Copy(pixels, 0, frame.Address, pixels.Length);
            return bitmap;
        }
    }
}
