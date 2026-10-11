using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class ValidationPage : UserControl
    {
        public ValidationPage()
        {
            InitializeComponent();
            DataContext = new ValidationViewModel();
        }
    }
}
