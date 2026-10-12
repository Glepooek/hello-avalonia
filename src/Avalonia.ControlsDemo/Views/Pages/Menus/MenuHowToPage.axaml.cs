using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MenuHowToPage : UserControl
    {
        public MenuHowToPage()
        {
            InitializeComponent();
            DataContext = new MenuHowToViewModel();
        }
    }
}
