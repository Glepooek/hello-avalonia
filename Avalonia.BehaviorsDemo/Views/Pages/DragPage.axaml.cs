using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class DragPage : UserControl
    {
        public DragPage()
        {
            InitializeComponent();
            DataContext = new DragViewModel();
        }
    }
}
