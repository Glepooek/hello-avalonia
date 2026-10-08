using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.CustomControlsDemo.Controls
{
    /// <summary>
    /// A colour-swatch popup. Derives from PopupFlyoutBase because that is where the placement and light-dismiss logic lives.
    /// </summary>
    public class SwatchFlyout : PopupFlyoutBase
    {
        private static readonly string[] Palette = { "#E8564A", "#E8974A", "#E8D54A", "#4AE87B", "#4A7BE8", "#B04AE8" };

        public event EventHandler<Color>? ColorPicked;

        protected override Control CreatePresenter()
        {
            var panel = new WrapPanel { Width = 120 };
            foreach (var hex in Palette)
            {
                var colour = Color.Parse(hex);
                var swatch = new Button
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush(colour),
                };
                swatch.Click += (_, _) =>
                {
                    ColorPicked?.Invoke(this, colour);
                    Hide();
                };
                panel.Children.Add(swatch);
            }

            return new FlyoutPresenter { Content = panel };
        }
    }
}
