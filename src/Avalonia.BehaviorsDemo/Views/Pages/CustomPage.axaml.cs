using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class CustomPage : UserControl
    {
        public CustomPage()
        {
            InitializeComponent();
            DataContext = new CustomViewModel();
        }
    }
}
