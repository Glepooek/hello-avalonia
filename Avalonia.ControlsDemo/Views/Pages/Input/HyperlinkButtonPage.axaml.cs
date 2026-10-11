using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class HyperlinkButtonPage : UserControl
    {
        private int _count;

        public HyperlinkButtonPage()
        {
            InitializeComponent();
        }

        private void OnPlainLinkClick(object? sender, RoutedEventArgs e) => PlainCount.Text = (++_count).ToString();
    }
}
