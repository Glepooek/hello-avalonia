using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// Hosts a child in both trees. DataContext flows down the logical tree, so the child binds.
    /// </summary>
    public class VisualAndLogicalHost : Control
    {
        public VisualAndLogicalHost()
        {
            Child.Bind(TextBlock.TextProperty, new Binding());
            VisualChildren.Add(Child);
            LogicalChildren.Add(Child);
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
