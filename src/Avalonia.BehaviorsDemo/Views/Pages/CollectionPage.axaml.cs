using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class CollectionPage : UserControl
    {
        public CollectionPage()
        {
            InitializeComponent();
            DataContext = new CollectionViewModel();
        }
    }
}
