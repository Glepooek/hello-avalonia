using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.EventsDemo.Controls
{
    /// <summary>
    /// A button that raises a custom bubbling routed event, <see cref="PingEvent"/>, when clicked.
    /// </summary>
    public class Notifier : Button
    {
        // 1. The routed event itself: owner type, argument type, routing strategy.
        public static readonly RoutedEvent<PingEventArgs> PingEvent =
            RoutedEvent.Register<Notifier, PingEventArgs>(nameof(Ping), RoutingStrategies.Bubble);

        // 2. The CLR wrapper. It is what lets C# code write `notifier.Ping += ...`.
        public event EventHandler<PingEventArgs>? Ping
        {
            add => AddHandler(PingEvent, value);
            remove => RemoveHandler(PingEvent, value);
        }

        // A subclass looks up its theme by its own type; without this it gets no template and renders as nothing.
        protected override Type StyleKeyOverride => typeof(Button);

        protected override void OnClick()
        {
            base.OnClick();

            // 3. Raising it: the event starts at this control and bubbles up the visual tree.
            RaiseEvent(new PingEventArgs(PingEvent, this) { Message = $"ping from {Name}" });
        }
    }
}
