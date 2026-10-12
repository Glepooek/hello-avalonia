using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RadioButtonPage : UserControl
    {
        public RadioButtonPage()
        {
            InitializeComponent();
        }

        // IsCheckedChanged fires for the button being unchecked too, and at that moment its
        // sibling is not yet checked, so act only on the sender that became checked.
        private void OnSizeChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { IsChecked: true } button)
            {
                SizeResult.Text = $"当前选择：{button.Content}";
            }
        }
    }
}
