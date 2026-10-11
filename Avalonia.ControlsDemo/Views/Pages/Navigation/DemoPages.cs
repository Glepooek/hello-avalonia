using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>Builds the small sample pages that the navigation demos switch between.</summary>
    internal static class DemoPages
    {
        public static ContentPage Make(string header, string body)
        {
            return new ContentPage
            {
                Header = header,
                Content = new TextBlock
                {
                    Text = body,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }
    }
}
