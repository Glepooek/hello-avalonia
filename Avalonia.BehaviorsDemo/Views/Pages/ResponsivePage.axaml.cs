using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ResponsivePage : UserControl
    {
        public ResponsivePage()
        {
            InitializeComponent();
            DataContext = new ResponsiveViewModel();
        }
    }
}
