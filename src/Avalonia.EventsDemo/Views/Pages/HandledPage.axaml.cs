using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class HandledPage : UserControl
    {
        private readonly EventLog _log = new();

        public HandledPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // Tunnel first: every layer sees the key before the TextBox does.
            Outer.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("outer:tunnel"),
                RoutingStrategies.Tunnel);
            Middle.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("middle:tunnel"),
                RoutingStrategies.Tunnel);

            // The innermost bubble handler is the one that decides to stop the event.
            Entry.AddHandler(InputElement.KeyDownEvent, (object? s, KeyEventArgs e) =>
            {
                _log.Write($"inner:bubble  (Handled was {e.Handled})");
                if (HandleBox.IsChecked == true)
                {
                    e.Handled = true;
                }
            }, RoutingStrategies.Bubble);

            Middle.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("middle:bubble"),
                RoutingStrategies.Bubble);
            Outer.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write("outer:bubble"),
                RoutingStrategies.Bubble);

            // handledEventsToo: true opts this handler out of the Handled cut-off.
            Outer.AddHandler(InputElement.KeyDownEvent,
                (object? s, KeyEventArgs e) => _log.Write($"outer (handledEventsToo)  Handled={e.Handled}"),
                RoutingStrategies.Bubble,
                handledEventsToo: true);
        }

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
