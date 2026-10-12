using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class BorderPage : UserControl
    {
        public BorderPage()
        {
            InitializeComponent();

            // Thickness and CornerRadius are structs, so a double slider cannot bind to them directly (rule 3).
            ThicknessSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Target.BorderThickness = new Thickness(ThicknessSlider.Value);
                }
            };
            RadiusSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Target.CornerRadius = new CornerRadius(RadiusSlider.Value);
                }
            };

            // Applied after wiring so the starting slider values are honoured once, from code.
            Target.BorderThickness = new Thickness(ThicknessSlider.Value);
            Target.CornerRadius = new CornerRadius(RadiusSlider.Value);
        }
    }
}
