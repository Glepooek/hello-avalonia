using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class AsyncPage : UserControl
    {
        public AsyncPage()
        {
            InitializeComponent();
            DataContext = new AsyncViewModel();
        }
    }
}
