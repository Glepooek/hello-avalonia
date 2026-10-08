using Avalonia.Controls;
using Avalonia.InputDemo.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class InteractivityPage : UserControl
    {
        private readonly InteractivityViewModel _vm = new();

        public InteractivityPage()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void OnClickHandler(object? sender, RoutedEventArgs e)
        {
            _vm.Count++;
            HandlerReadout.Text = "Click 处理器触发（不看 CanExecute）";
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            _vm.Count++;
            HandlerReadout.Text = "Tapped 触发（不看 CanExecute）";
        }
    }
}
