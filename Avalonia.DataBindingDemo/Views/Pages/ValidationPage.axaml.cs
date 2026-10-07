using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class ValidationPage : UserControl
    {
        public ValidationPage()
        {
            InitializeComponent();
            DataContext = new ValidationViewModel();
        }
    }
}
