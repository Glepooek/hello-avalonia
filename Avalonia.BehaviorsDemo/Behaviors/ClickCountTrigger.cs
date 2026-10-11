using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Behaviors
{
    /// <summary>
    /// A custom Trigger (a StyledElementTrigger, so bindings inside its actions resolve): runs its Actions once every <see cref="Count"/> presses.
    /// It listens to PointerPressed rather than Tapped, because the second quick Tapped is swallowed by DoubleTapped.
    /// </summary>
    public class ClickCountTrigger : StyledElementTrigger<Control>
    {
        // A StyledProperty is what makes the value settable and bindable from XAML.
        public static readonly StyledProperty<int> CountProperty =
            AvaloniaProperty.Register<ClickCountTrigger, int>(nameof(Count), 3);

        private int _presses;

        public int Count
        {
            get => GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        protected override void OnAttachedToVisualTree()
        {
            base.OnAttachedToVisualTree();
            AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble);
        }

        protected override void OnDetachedFromVisualTree()
        {
            AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            base.OnDetachedFromVisualTree();
        }

        private void OnPressed(object? sender, RoutedEventArgs e)
        {
            if (++_presses < Count)
            {
                return;
            }

            _presses = 0;
            // This is the whole job of a Trigger: decide when, then hand the Actions to Interaction.
            Interaction.ExecuteActions(AssociatedObject, Actions, null);
        }
    }
}
