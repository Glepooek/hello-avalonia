using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class ShapesPage : UserControl
    {
        public ShapesPage()
        {
            InitializeComponent();

            // Geometry objects have no name of their own, so go through the Path that owns it.
            var combined = (CombinedGeometry)CombinedPath.Data!;

            ModeBox.ItemsSource = Enum.GetValues<GeometryCombineMode>();
            ModeBox.SelectedItem = combined.GeometryCombineMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is GeometryCombineMode mode)
                {
                    combined.GeometryCombineMode = mode;
                }
            };
        }
    }
}
