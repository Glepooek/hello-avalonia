using Avalonia.Controls;
using Avalonia.EventsDemo.Controls;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class CustomRoutedEventPage : UserControl
    {
        private readonly EventLog _log = new();

        public CustomRoutedEventPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // The CLR-wrapper route: only Alpha, because the wrapper subscribes on the control itself.
            Alpha.Ping += (_, e) => _log.Write($"Alpha.Ping +=  (direct on the control)  Message='{e.Message}'");
        }

        // Fired by the attached-syntax subscription on the StackPanel, for both buttons.
        private void OnPingFromXaml(object? sender, PingEventArgs e)
            => _log.Write($"Listener (attached)  Message='{e.Message}'  Source={(e.Source as Control)?.Name}");

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
