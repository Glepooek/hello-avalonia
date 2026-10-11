using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class EventToCommandPage : UserControl
    {
        public EventToCommandPage()
        {
            InitializeComponent();
            DataContext = new EventToCommandViewModel();
        }
    }
}
