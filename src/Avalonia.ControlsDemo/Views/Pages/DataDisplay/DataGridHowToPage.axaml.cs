using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DataGridHowToPage : UserControl
    {
        public DataGridHowToPage()
        {
            InitializeComponent();
            DataContext = new DataGridHowToViewModel();
        }
    }
}
