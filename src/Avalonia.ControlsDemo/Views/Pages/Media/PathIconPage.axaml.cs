using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PathIconPage : UserControl
    {
        private static readonly (string Name, IBrush Brush)[] Colors =
        [
            ("OrangeRed", Brushes.OrangeRed),
            ("SeaGreen", Brushes.SeaGreen),
            ("SteelBlue", Brushes.SteelBlue),
            ("Gray", Brushes.Gray),
        ];

        public PathIconPage()
        {
            InitializeComponent();

            GeometryBox.ItemsSource = new[] { "Heart", "Check" };
            GeometryBox.SelectedIndex = 0;
            GeometryBox.SelectionChanged += (_, _) =>
                Icon.Data = (Geometry)this.FindResource(GeometryBox.SelectedIndex == 1 ? "CheckGeometry" : "HeartGeometry")!;

            ColorBox.ItemsSource = new[] { Colors[0].Name, Colors[1].Name, Colors[2].Name, Colors[3].Name };
            ColorBox.SelectionChanged += (_, _) =>
            {
                if (ColorBox.SelectedIndex >= 0)
                {
                    Icon.Foreground = Colors[ColorBox.SelectedIndex].Brush;
                }
            };

            SizeSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Icon.Width = Icon.Height = SizeSlider.Value;
                    SizeText.Text = $"{SizeSlider.Value:0}";
                }
            };
            Icon.Width = Icon.Height = SizeSlider.Value;
            SizeText.Text = $"{SizeSlider.Value:0}";
        }
    }
}
