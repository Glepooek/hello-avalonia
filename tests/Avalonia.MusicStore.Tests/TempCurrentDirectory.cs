using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace Avalonia.MusicStore.Tests
{
    // Album reads and writes "./Cache" relative to the process working directory, so every test that touches the
    // cache runs inside a fresh temp folder; the previous directory comes back on Dispose.
    internal sealed class TempCurrentDirectory : IDisposable
    {
        private readonly string _previous = Environment.CurrentDirectory;

        public TempCurrentDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "musicstore-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Environment.CurrentDirectory = Root;
        }

        public string Root { get; }

        public string Cache(string fileName) => Path.Combine(Root, "Cache", fileName);

        // Writes a solid PNG; the app names cover files ".bmp" but decodes whatever format is inside.
        // Needs the Avalonia platform, so only [AvaloniaFact] tests call it.
        public void WriteCover(string fileName, int width, int height)
        {
            Directory.CreateDirectory(Path.Combine(Root, "Cache"));
            using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
            using (var context = bitmap.CreateDrawingContext())
            {
                context.FillRectangle(Brushes.Red, new Rect(0, 0, width, height));
            }
            bitmap.Save(Cache(fileName));
        }

        public void Dispose()
        {
            Environment.CurrentDirectory = _previous;
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
                // A cover stream still being finalised can hold a file for a moment; the temp folder is harmless.
            }
        }
    }
}
