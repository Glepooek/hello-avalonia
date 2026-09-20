using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.MusicStore.ViewModels;

namespace Avalonia.MusicStore.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            (DataContext as MainWindowViewModel)?.LoadedCommand.Execute(null);
        }
    }
}
