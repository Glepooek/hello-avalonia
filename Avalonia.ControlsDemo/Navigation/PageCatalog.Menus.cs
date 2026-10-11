using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddMenus(List<PageEntry> list)
        {
            const string c = Categories.Menus;

            list.Add(ControlPage<ContextMenuPage>("ContextMenu", c, "controls/menus/contextmenu"));
            list.Add(ControlPage<MenuPage>("Menu", c, "controls/menus/menu"));
            list.Add(HowToPage<MenuHowToPage>("Menu", c, "docs/how-to/menu-how-to"));
            list.Add(ControlPage<MenuFlyoutPage>("MenuFlyout", c, "controls/menus/menuflyout"));
            list.Add(ControlPage<NativeMenuPage>("NativeMenu", c, "controls/menus/nativemenu"));
            list.Add(ControlPage<SeparatorPage>("Separator", c, "controls/menus/separator"));
        }
    }
}
