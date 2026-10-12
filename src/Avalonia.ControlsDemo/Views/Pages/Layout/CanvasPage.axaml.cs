using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class CanvasPage : UserControl
    {
        public CanvasPage()
        {
            InitializeComponent();

            // Attached properties cannot take an element-name binding as a source here, so move the dot from code.
            LeftSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Canvas.SetLeft(Dot, LeftSlider.Value);
                }
            };
            TopSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Canvas.SetTop(Dot, TopSlider.Value);
                }
            };
        }
    }
}
