using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CollectionsPage : UserControl
    {
        public CollectionsPage()
        {
            InitializeComponent();
            DataContext = new CollectionsViewModel();
        }
    }
}
