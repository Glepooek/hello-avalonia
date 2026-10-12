using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ImagePage : UserControl
    {
        public ImagePage()
        {
            InitializeComponent();

            Pic.Source = DemoImages.Gradient(40, 20);

            StretchBox.ItemsSource = Enum.GetValues<Stretch>();
            StretchBox.SelectedItem = Pic.Stretch;
            StretchBox.SelectionChanged += (_, _) =>
            {
                if (StretchBox.SelectedItem is Stretch stretch)
                {
                    Pic.Stretch = stretch;
                }
            };

            DirectionBox.ItemsSource = Enum.GetValues<StretchDirection>();
            DirectionBox.SelectedItem = Pic.StretchDirection;
            DirectionBox.SelectionChanged += (_, _) =>
            {
                if (DirectionBox.SelectedItem is StretchDirection direction)
                {
                    Pic.StretchDirection = direction;
                }
            };

            InterpolationBox.ItemsSource = Enum.GetValues<BitmapInterpolationMode>();
            InterpolationBox.SelectedItem = RenderOptions.GetBitmapInterpolationMode(Pic);
            InterpolationBox.SelectionChanged += (_, _) =>
            {
                if (InterpolationBox.SelectedItem is BitmapInterpolationMode mode)
                {
                    RenderOptions.SetBitmapInterpolationMode(Pic, mode);
                }
            };

            // The rendered size only changes through layout, so the readout follows LayoutUpdated.
            Pic.LayoutUpdated += (_, _) =>
            {
                var text = $"源图 {Pic.Source!.Size.Width:0}×{Pic.Source.Size.Height:0}，Image 占位 {Pic.Bounds.Width:0}×{Pic.Bounds.Height:0}，期望尺寸 {Pic.DesiredSize.Width:0}×{Pic.DesiredSize.Height:0}";
                if (SizeText.Text != text)
                {
                    SizeText.Text = text;
                }
            };
        }
    }
}
