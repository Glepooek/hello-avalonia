using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DatePickerHowToPage : UserControl
    {
        public DatePickerHowToPage()
        {
            InitializeComponent();
            DataContext = new DatePickerHowToViewModel();
        }
    }
}
