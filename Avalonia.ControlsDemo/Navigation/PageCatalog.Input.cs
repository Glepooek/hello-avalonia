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

            // Date and time
            list.Add(ControlPage<CalendarPage>("Calendar", c, "controls/input/date-and-time/calendar"));
            list.Add(ControlPage<CalendarDatePickerPage>("CalendarDatePicker", c, "controls/input/date-and-time/calendardatepicker"));
            list.Add(ControlPage<DatePickerPage>("DatePicker", c, "controls/input/date-and-time/datepicker"));
            list.Add(HowToPage<DatePickerHowToPage>("DatePicker", c, "docs/how-to/datepicker-how-to"));
            list.Add(ControlPage<TimePickerPage>("TimePicker", c, "controls/input/date-and-time/timepicker"));

            // Selectors
            list.Add(ControlPage<CheckBoxPage>("CheckBox", c, "controls/input/selectors/checkbox"));
            list.Add(ControlPage<ColorPickerPage>("ColorPicker", c, "controls/input/selectors/colorpicker"));
            list.Add(ControlPage<ColorViewPage>("ColorView", c, "controls/input/selectors/colorview"));
            list.Add(ControlPage<ComboBoxPage>("ComboBox", c, "controls/input/selectors/combobox"));
            list.Add(HowToPage<ComboBoxHowToPage>("ComboBox", c, "docs/how-to/combobox-how-to"));
            list.Add(ControlPage<NumericUpDownPage>("NumericUpDown", c, "controls/input/selectors/numericupdown"));
            list.Add(ControlPage<SliderPage>("Slider", c, "controls/input/selectors/slider"));
            list.Add(HowToPage<SliderHowToPage>("Slider", c, "docs/how-to/slider-how-to"));
            list.Add(ControlPage<ToggleSwitchPage>("ToggleSwitch", c, "controls/input/selectors/toggleswitch"));

            // Text input
            list.Add(ControlPage<AutoCompleteBoxPage>("AutoCompleteBox", c, "controls/input/text-input/autocompletebox"));
            list.Add(ControlPage<MaskedTextBoxPage>("MaskedTextBox", c, "controls/input/text-input/maskedtextbox"));
            list.Add(ControlPage<TextBoxPage>("TextBox", c, "controls/input/text-input/textbox"));
            list.Add(HowToPage<TextBoxHowToPage>("TextBox", c, "docs/how-to/textbox-how-to"));
        }
    }
}
