using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ColorPickerPage : UserControl
    {
        public ColorPickerPage()
        {
            InitializeComponent();

            Picker.ColorChanged += (_, e) => Show(e.NewColor);
            AlphaCheck.IsCheckedChanged += (_, _) => Picker.IsAlphaEnabled = AlphaCheck.IsChecked == true;
            Show(Picker.Color);
        }

        // The swatch brush is set from code on purpose: nothing styles Background here, so it is not a local-value trap.
        private void Show(Color color)
        {
            Swatch.Background = new SolidColorBrush(color);
            ColorText.Text = color.ToString();
        }
    }
}
