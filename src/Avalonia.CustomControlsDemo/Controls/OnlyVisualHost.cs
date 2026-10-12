using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Hosts a child in the visual tree only. The child never sees an inherited DataContext.
    /// </summary>
    public class OnlyVisualHost : Control
    {
        public OnlyVisualHost()
        {
            Child.Bind(TextBlock.TextProperty, new Binding());
            VisualChildren.Add(Child);
        }

        public TextBlock Child { get; } = new();

        protected override Size MeasureOverride(Size availableSize)
        {
            Child.Measure(availableSize);
            return Child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
