using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddInput(List<PageEntry> list)
        {
            const string c = Categories.Input;

            // Buttons, in the order of the official sidebar. Task 5 inserts the other four.
            list.Add(ControlPage<ButtonPage>("Button", c, "controls/input/buttons/button"));
            list.Add(ControlPage<HyperlinkButtonPage>("HyperlinkButton", c, "controls/input/buttons/hyperlinkbutton"));
            list.Add(ControlPage<RepeatButtonPage>("RepeatButton", c, "controls/input/buttons/repeatbutton"));
            list.Add(ControlPage<ToggleButtonPage>("ToggleButton", c, "controls/input/buttons/togglebutton"));
        }
    }
}
