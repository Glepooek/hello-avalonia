using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class FocusKeyboardPage : UserControl
    {
        public FocusKeyboardPage()
        {
            InitializeComponent();
            DataContext = new FocusKeyboardViewModel();
        }
    }
}
