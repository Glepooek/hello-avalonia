using Avalonia.Controls.Primitives;

namespace Avalonia.Shared.Controls
{
    /// <summary>
    /// A header bar shown at the top of every demo page. It states which
    /// documentation topic the page demonstrates and where to find it upstream.
    /// </summary>
    public class DemoHeader : TemplatedControl
    {
        public static readonly StyledProperty<string?> TitleProperty =
            AvaloniaProperty.Register<DemoHeader, string?>(nameof(Title));

        public static readonly StyledProperty<string?> DocPathProperty =
            AvaloniaProperty.Register<DemoHeader, string?>(nameof(DocPath));

        /// <summary>The feature being demonstrated, written in Chinese.</summary>
        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>Path of the matching page on docs.avaloniaui.net.</summary>
        public string? DocPath
        {
            get => GetValue(DocPathProperty);
            set => SetValue(DocPathProperty, value);
        }
    }
}
