using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ViewboxPage : UserControl
    {
        public ViewboxPage()
        {
            InitializeComponent();

            StretchBox.ItemsSource = Enum.GetValues<Stretch>();
            StretchBox.SelectedItem = Box.Stretch;
            StretchBox.SelectionChanged += (_, _) =>
            {
                if (StretchBox.SelectedItem is Stretch stretch)
                {
                    Box.Stretch = stretch;
                }
            };

            Box.LayoutUpdated += (_, _) =>
            {
                // The child's Bounds never change; the scale shows in where its right edge lands inside the Viewbox.
                var right = Inner.TransformToVisual(Box)?.Transform(new Point(Inner.Bounds.Width, 0)).X;
                ScaleText.Text = right is { } x ? $"水平缩放约 {x / Inner.Bounds.Width:F2} 倍" : string.Empty;
            };
        }
    }
}
