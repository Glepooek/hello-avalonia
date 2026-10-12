using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.ViewModels;

namespace Avalonia.DataTemplatesDemo.Views.Pages
{
    public partial class ViewLocatorPage : UserControl
    {
        public ViewLocatorPage()
        {
            InitializeComponent();
            DataContext = new ViewLocatorViewModel();
        }
    }
}
