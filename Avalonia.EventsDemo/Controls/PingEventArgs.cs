using Avalonia.Interactivity;

namespace Avalonia.EventsDemo.Controls
{
    public class PingEventArgs : RoutedEventArgs
    {
        public PingEventArgs(RoutedEvent routedEvent, object? source) : base(routedEvent, source)
        {
        }

        // The payload a handler further up the tree can read.
        public string Message { get; init; } = "";
    }
}
