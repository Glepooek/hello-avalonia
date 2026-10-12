using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddNavigation(List<PageEntry> list)
        {
            const string c = Categories.Navigation;

            list.Add(ControlPage<CarouselDemoPage>("CarouselPage", c, "controls/navigation/carouselpage"));
            list.Add(ControlPage<CommandBarPage>("CommandBar", c, "controls/navigation/commandbar"));
            list.Add(ControlPage<ContentDemoPage>("ContentPage", c, "controls/navigation/contentpage"));
            list.Add(ControlPage<DrawerDemoPage>("DrawerPage", c, "controls/navigation/drawerpage"));
            list.Add(ControlPage<NavigationDemoPage>("NavigationPage", c, "controls/navigation/navigationpage"));
            list.Add(HowToPage<NavigationHowToPage>("NavigationPage", c, "docs/how-to/navigation-how-to"));
            list.Add(ControlPage<TabbedDemoPage>("TabbedPage", c, "controls/navigation/tabbedpage"));
            list.Add(ControlPage<TabControlPage>("TabControl", c, "controls/navigation/tabcontrol"));
            list.Add(HowToPage<TabControlHowToPage>("TabControl", c, "docs/how-to/tabcontrol-how-to"));
            list.Add(ControlPage<TabStripPage>("TabStrip", c, "controls/navigation/tabstrip"));
        }
    }
}
