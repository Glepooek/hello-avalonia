using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.StarterTutorialDemo.Views.Pages
{
    public partial class EventsPage : UserControl
    {
        private int _clicks;

        public EventsPage()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            _clicks++;
            Debug.WriteLine($"Click! ({_clicks})");
            ClickInfo.Text = $"已点击 {_clicks} 次";
        }
    }
}
