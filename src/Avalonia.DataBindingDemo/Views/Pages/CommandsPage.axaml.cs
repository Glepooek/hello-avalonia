using Avalonia.Controls;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class CommandsPage : UserControl
    {
        public CommandsPage()
        {
            InitializeComponent();
            DataContext = new CommandsViewModel();
        }
    }
}
