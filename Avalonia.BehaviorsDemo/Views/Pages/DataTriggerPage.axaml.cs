using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class DataTriggerPage : UserControl
    {
        public DataTriggerPage()
        {
            InitializeComponent();
            DataContext = new DataTriggerViewModel();
        }
    }
}
