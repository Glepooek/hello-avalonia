using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class MasterDetailPage : UserControl
    {
        public MasterDetailPage()
        {
            InitializeComponent();
            DataContext = new MasterDetailViewModel();
        }
    }
}
