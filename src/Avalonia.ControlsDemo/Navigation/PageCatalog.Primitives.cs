using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddPrimitives(List<PageEntry> list)
        {
            const string c = Categories.Primitives;

            list.Add(ControlPage<ScrollBarPage>("ScrollBar", c, "controls/primitives/scrollbar"));
            list.Add(ControlPage<ThemeVariantScopePage>("ThemeVariantScope", c, "controls/primitives/themevariantscope"));
            list.Add(ControlPage<UserControlPage>("UserControl", c, "controls/primitives/usercontrol"));
            list.Add(ControlPage<WindowPage>("Window", c, "controls/primitives/window"));
            list.Add(HowToPage<WindowHowToPage>("Window", c, "docs/how-to/window-how-to"));
            list.Add(HowToPage<DialogsHowToPage>("Dialogs", c, "docs/how-to/dialogs-how-to"));
            list.Add(ControlPage<WindowDrawnDecorationsPage>("WindowDrawnDecorations", c, "controls/primitives/windowdrawndecorations"));
        }
    }
}
