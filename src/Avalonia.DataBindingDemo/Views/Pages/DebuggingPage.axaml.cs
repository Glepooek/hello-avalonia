using Avalonia.Controls;
using Avalonia.DataBindingDemo.Diagnostics;
using Avalonia.DataBindingDemo.ViewModels;

namespace Avalonia.DataBindingDemo.Views.Pages
{
    public partial class DebuggingPage : UserControl
    {
        public DebuggingPage()
        {
            // Install before InitializeComponent so the page's own broken bindings are caught.
            var sink = BindingLogSink.Install();
            InitializeComponent();
            LogList.ItemsSource = sink.Entries;
            DataContext = new DebuggingViewModel();
        }
    }
}
