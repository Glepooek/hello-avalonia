using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class RelativePanelPage : UserControl
    {
        public RelativePanelPage()
        {
            InitializeComponent();

            PanelRightCheck.IsCheckedChanged += (_, _) =>
                RelativePanel.SetAlignRightWithPanel(Follower, PanelRightCheck.IsChecked == true);
        }
    }
}
