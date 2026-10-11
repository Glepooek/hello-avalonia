using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>A small reusable UserControl: a colour block with a caption.</summary>
    public partial class SwatchView : UserControl
    {
        public static readonly StyledProperty<string?> TitleProperty =
            AvaloniaProperty.Register<SwatchView, string?>(nameof(Title));

        public static readonly StyledProperty<IBrush?> FillProperty =
            AvaloniaProperty.Register<SwatchView, IBrush?>(nameof(Fill));

        public SwatchView()
        {
            InitializeComponent();
        }

        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }
    }
}
