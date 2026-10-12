using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Media.Imaging;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ImageHowToPage : UserControl
    {
        public ImageHowToPage()
        {
            InitializeComponent();
            DataContext = new ImageHowToViewModel();

            Avatar.Source = DemoImages.Gradient(60, 40);

            ShotButton.Click += (_, _) =>
            {
                var size = new PixelSize((int)Capture.Bounds.Width, (int)Capture.Bounds.Height);
                var shot = new RenderTargetBitmap(size);
                shot.Render(Capture);
                ShotImage.Source = shot;
                ShotText.Text = $"已截图 {shot.PixelSize.Width}×{shot.PixelSize.Height}";
            };
        }
    }
}
