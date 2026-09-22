using Avalonia.Metadata;
using System;
using System.Linq;

namespace Avalonia.XamlDemo.MarkupExtensions
{
    /// <summary>
    /// Repeats a string. The point is the shape, not the feature: any class
    /// with a ProvideValue method can be used as {local:RepeatText ...}.
    /// The "Extension" suffix is optional in the XAML usage.
    /// </summary>
    public sealed class RepeatTextExtension
    {
        public RepeatTextExtension()
        {
        }

        public RepeatTextExtension(string text) => Text = text;

        // Marks which property the positional argument fills.
        [ConstructorArgument("text")]
        public string Text { get; set; } = string.Empty;

        public int Count { get; set; } = 2;

        public object ProvideValue(IServiceProvider serviceProvider)
            => string.Concat(Enumerable.Repeat(Text, Math.Max(1, Count)));
    }
}
