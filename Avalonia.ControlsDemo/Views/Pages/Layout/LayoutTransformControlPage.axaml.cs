using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class LayoutTransformControlPage : UserControl
    {
        public LayoutTransformControlPage()
        {
            InitializeComponent();

            AngleSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Apply();
                }
            };
            LayoutHost.LayoutUpdated += (_, _) => LayoutSize.Text = $"宿主 {LayoutHost.Bounds.Width:F0} × {LayoutHost.Bounds.Height:F0}";
            RenderHost.LayoutUpdated += (_, _) => RenderSize.Text = $"宿主 {RenderHost.Bounds.Width:F0} × {RenderHost.Bounds.Height:F0}";
            Apply();
        }

        private void Apply()
        {
            LayoutHost.LayoutTransform = new RotateTransform(AngleSlider.Value);
            RenderChild.RenderTransform = new RotateTransform(AngleSlider.Value);
        }
    }
}
