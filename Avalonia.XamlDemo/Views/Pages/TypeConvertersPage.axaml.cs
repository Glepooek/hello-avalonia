using Avalonia.Controls;

namespace Avalonia.XamlDemo.Views.Pages
{
    public partial class TypeConvertersPage : UserControl
    {
        public TypeConvertersPage()
        {
            InitializeComponent();

            // Read back what the string literals actually became.
            foreach (var line in new[]
                     {
                         $"Background \"#FF3366\" → {Sample.Background}",
                         $"BorderBrush \"White\" → {Sample.BorderBrush}",
                         $"BorderThickness \"1,2,3,4\" → {Sample.BorderThickness}",
                         $"CornerRadius \"8\" → {Sample.CornerRadius}",
                         $"Width \"140\" → {Sample.Width} ({Sample.Width.GetType().Name})",
                     })
            {
                ReadoutHost.Children.Add(new TextBlock { Text = line });
            }
        }
    }
}
