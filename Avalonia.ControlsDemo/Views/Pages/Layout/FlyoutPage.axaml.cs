using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class FlyoutPage : UserControl
    {
        public FlyoutPage()
        {
            InitializeComponent();

            if (OpenButton.Flyout is Flyout flyout)
            {
                flyout.Opened += (_, _) => FlyoutResult.Text = "Flyout 已打开";
                flyout.Closed += (_, _) => FlyoutResult.Text = "Flyout 已关闭";
            }
        }
    }
}
