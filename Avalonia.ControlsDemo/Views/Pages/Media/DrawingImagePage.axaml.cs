using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DrawingImagePage : UserControl
    {
        private static readonly IBrush[] Palette = [Brushes.OrangeRed, Brushes.SeaGreen, Brushes.SteelBlue, Brushes.Goldenrod];
        private readonly GeometryDrawing _circle;
        private int _next;

        public DrawingImagePage()
        {
            InitializeComponent();

            _circle = new GeometryDrawing
            {
                Brush = Palette[0],
                Geometry = new EllipseGeometry(new Rect(5, 5, 90, 90)),
            };
            Built.Source = new DrawingImage(_circle);
            ColorText.Text = $"Brush = {Palette[0]}";

            RecolorButton.Click += (_, _) =>
            {
                _next = (_next + 1) % Palette.Length;
                _circle.Brush = Palette[_next];
                ColorText.Text = $"Brush = {Palette[_next]}";
            };
        }
    }
}
