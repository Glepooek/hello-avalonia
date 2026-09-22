using Avalonia.Controls;

namespace Avalonia.PropertySystemDemo.Controls
{
    /// <summary>
    /// Attached properties let one type add state to controls it does not own.
    /// The owner here is a static class that is never instantiated.
    /// </summary>
    public static class HighlightBehavior
    {
        /// <summary>Non-inheriting: each control carries its own value.</summary>
        public static readonly AttachedProperty<bool> IsHighlightedProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                "IsHighlighted", typeof(HighlightBehavior));

        /// <summary>
        /// Inheriting: setting it on an ancestor makes every descendant read
        /// the same value, without any of them declaring it.
        /// </summary>
        public static readonly AttachedProperty<string?> TagLineProperty =
            AvaloniaProperty.RegisterAttached<Control, string?>(
                "TagLine", typeof(HighlightBehavior), inherits: true);

        public static bool GetIsHighlighted(Control target)
            => target.GetValue(IsHighlightedProperty);

        public static void SetIsHighlighted(Control target, bool value)
            => target.SetValue(IsHighlightedProperty, value);

        public static string? GetTagLine(Control target)
            => target.GetValue(TagLineProperty);

        public static void SetTagLine(Control target, string? value)
            => target.SetValue(TagLineProperty, value);
    }
}
