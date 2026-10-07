using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CollectionViewsPage : UserControl
    {
        public CollectionViewsPage()
        {
            InitializeComponent();
            DataContext = new CollectionViewsViewModel();
        }
    }
}
