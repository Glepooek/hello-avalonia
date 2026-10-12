using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ListBoxHowToPage : UserControl
    {
        public ListBoxHowToPage()
        {
            InitializeComponent();
            DataContext = new ListBoxHowToViewModel();
        }
    }
}
