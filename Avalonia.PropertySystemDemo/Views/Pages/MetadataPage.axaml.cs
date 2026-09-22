using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class MetadataPage : UserControl
    {
        public MetadataPage()
        {
            InitializeComponent();

            // FontSize inherits, so setting it on the host reaches both labels.
            InheritHost.Bind(FontSizeProperty, FontSlider.GetObservable(Slider.ValueProperty));
        }

        private void OnWriteHighClick(object? sender, RoutedEventArgs e) => Write(150);

        private void OnWriteLowClick(object? sender, RoutedEventArgs e) => Write(-20);

        private void OnWriteMidClick(object? sender, RoutedEventArgs e) => Write(55);

        private void Write(double requested)
        {
            Target.Value = requested;
            CoerceLog.Text = $"写入 {requested:F0}，实际存下 {Target.Value:F0}"
                + (System.Math.Abs(requested - Target.Value) > 0.01 ? "（被 coerce 夹取了）" : "（在范围内，原样保留）");
        }
    }
}
