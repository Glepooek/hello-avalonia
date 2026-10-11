using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddMedia(List<PageEntry> list)
        {
            const string c = Categories.Media;

            list.Add(ControlPage<DrawingImagePage>("DrawingImage", c, "controls/media/drawingimage"));
            list.Add(ControlPage<ImagePage>("Image", c, "controls/media/image"));
            list.Add(HowToPage<ImageHowToPage>("Image", c, "docs/how-to/image-how-to"));
            list.Add(ControlPage<PathIconPage>("PathIcon", c, "controls/media/pathicon"));
        }
    }
}
