using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SliderHowToPage : UserControl
    {
        public SliderHowToPage()
        {
            InitializeComponent();

            foreach (var slider in new[] { RedSlider, GreenSlider, BlueSlider })
            {
                slider.PropertyChanged += (_, e) =>
                {
                    if (e.Property == RangeBase.ValueProperty)
                    {
                        UpdatePreview();
                    }
                };
            }

            IntSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    IntResult.Text = $"Value = {IntSlider.Value:F0}";
                }
            };

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var color = Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            Preview.Background = new SolidColorBrush(color);
            HexText.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
