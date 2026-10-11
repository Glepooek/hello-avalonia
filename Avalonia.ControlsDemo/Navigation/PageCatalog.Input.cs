using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddInput(List<PageEntry> list)
        {
            const string c = Categories.Input;

            // Buttons, in the order of the official sidebar.
            list.Add(ControlPage<ButtonPage>("Button", c, "controls/input/buttons/button"));
            list.Add(ControlPage<ButtonSpinnerPage>("ButtonSpinner", c, "controls/input/buttons/buttonspinner"));
            list.Add(ControlPage<HyperlinkButtonPage>("HyperlinkButton", c, "controls/input/buttons/hyperlinkbutton"));
            list.Add(ControlPage<RadioButtonPage>("RadioButton", c, "controls/input/buttons/radiobutton"));
            list.Add(ControlPage<RepeatButtonPage>("RepeatButton", c, "controls/input/buttons/repeatbutton"));
            list.Add(ControlPage<SplitButtonPage>("SplitButton", c, "controls/input/buttons/splitbutton"));
            list.Add(ControlPage<ToggleButtonPage>("ToggleButton", c, "controls/input/buttons/togglebutton"));
            list.Add(ControlPage<ToggleSplitButtonPage>("ToggleSplitButton", c, "controls/input/buttons/togglesplitbutton"));
        }
    }
}
