using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class SyntaxPage : UserControl
    {
        public SyntaxPage()
        {
            InitializeComponent();
            DataContext = new SyntaxViewModel();
        }
    }
}
