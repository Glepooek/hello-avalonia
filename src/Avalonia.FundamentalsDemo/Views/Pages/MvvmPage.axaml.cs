using Avalonia.Controls;
using Avalonia.FundamentalsDemo.ViewModels;

namespace Avalonia.FundamentalsDemo.Views.Pages
{
    public partial class MvvmPage : UserControl
    {
        public MvvmPage()
        {
            InitializeComponent();
            DataContext = new MvvmViewModel();
        }
    }
}
