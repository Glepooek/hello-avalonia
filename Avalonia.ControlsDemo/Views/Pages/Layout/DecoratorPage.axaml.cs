using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DecoratorPage : UserControl
    {
        public DecoratorPage()
        {
            InitializeComponent();

            PaddingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Apply();
                }
            };
            PaddedDecorator.LayoutUpdated += (_, _) => SizeText.Text = $"占 {PaddedDecorator.Bounds.Width:F0} × {PaddedDecorator.Bounds.Height:F0}";
            Apply();
        }

        private void Apply() => PaddedDecorator.Padding = new Thickness(PaddingSlider.Value);
    }
}
