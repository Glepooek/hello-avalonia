using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Behaviors
{
    /// <summary>
    /// The smallest useful Behavior: subscribe in OnAttachedToVisualTree, unsubscribe in OnDetachedFromVisualTree.
    /// It keeps a TextBox upper-case, no matter how the text got there.
    /// </summary>
    public class UpperCaseBehavior : Behavior<TextBox>
    {
        protected override void OnAttachedToVisualTree()
        {
            base.OnAttachedToVisualTree();
            if (AssociatedObject is { } box)
            {
                box.TextChanged += OnTextChanged;
            }
        }

        protected override void OnDetachedFromVisualTree()
        {
            if (AssociatedObject is { } box)
            {
                box.TextChanged -= OnTextChanged;
            }

            base.OnDetachedFromVisualTree();
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (AssociatedObject is not { } box)
            {
                return;
            }

            var upper = box.Text?.ToUpperInvariant();
            if (box.Text != upper)
            {
                box.Text = upper;
            }
        }
    }
}
