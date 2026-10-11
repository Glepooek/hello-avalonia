using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ToggleSwitchPage : UserControl
    {
        public ToggleSwitchPage()
        {
            InitializeComponent();
            Plain.IsCheckedChanged += (_, _) => PlainResult.Text = Plain.IsChecked == true ? "开" : "关";
        }
    }
}
