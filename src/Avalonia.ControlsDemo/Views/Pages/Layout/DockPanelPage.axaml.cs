using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DockPanelPage : UserControl
    {
        public DockPanelPage()
        {
            InitializeComponent();
            FillCheck.IsCheckedChanged += (_, _) => Dock.LastChildFill = FillCheck.IsChecked == true;
        }
    }
}
