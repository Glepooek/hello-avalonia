using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class RoutingPage : UserControl
    {
        private readonly EventLog _log = new();

        public RoutingPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // The same event, registered at two strategies on each of the three layers.
            foreach (var layer in new[] { Outer, Middle, Inner })
            {
                var name = layer.Name;
                layer.AddHandler(InputElement.PointerPressedEvent,
                    (object? s, PointerPressedEventArgs e) => _log.Write($"{name}:tunnel"),
                    RoutingStrategies.Tunnel);
                layer.AddHandler(InputElement.PointerPressedEvent,
                    (object? s, PointerPressedEventArgs e) => _log.Write($"{name}:bubble"),
                    RoutingStrategies.Bubble);
            }

            // Read the strategies off the event objects so the table cannot drift from the framework.
            var events = new RoutedEvent[]
            {
                InputElement.PointerPressedEvent,
                InputElement.KeyDownEvent,
                InputElement.TextInputEvent,
                InputElement.PointerEnteredEvent,
                InputElement.PointerExitedEvent,
                Button.ClickEvent,
            };
            StrategyTable.Text = string.Join("\n", events.Select(ev => $"{ev.Name,-20} {ev.RoutingStrategies}"));
        }

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
