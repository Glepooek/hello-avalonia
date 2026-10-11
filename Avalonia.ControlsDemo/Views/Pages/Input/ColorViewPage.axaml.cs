using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ColorViewPage : UserControl
    {
        public ColorViewPage()
        {
            InitializeComponent();

            ShapeBox.ItemsSource = Enum.GetValues<ColorSpectrumShape>();
            ShapeBox.SelectedItem = View.ColorSpectrumShape;
            ShapeBox.SelectionChanged += (_, _) =>
            {
                if (ShapeBox.SelectedItem is ColorSpectrumShape shape)
                {
                    View.ColorSpectrumShape = shape;
                }
            };

            ModelBox.ItemsSource = Enum.GetValues<ColorModel>();
            ModelBox.SelectedItem = View.ColorModel;
            ModelBox.SelectionChanged += (_, _) =>
            {
                if (ModelBox.SelectedItem is ColorModel model)
                {
                    View.ColorModel = model;
                }
            };

            View.ColorChanged += (_, e) => Show(e.NewColor);
            Show(View.Color);
        }

        private void Show(Color color)
        {
            Swatch.Background = new SolidColorBrush(color);
            ColorText.Text = $"{color}  H={View.HsvColor.H:F0}";
        }
    }
}
