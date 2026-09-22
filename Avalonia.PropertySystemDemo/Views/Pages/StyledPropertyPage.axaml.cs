using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.PropertySystemDemo.Views.Pages
{
    public partial class StyledPropertyPage : UserControl
    {
        public StyledPropertyPage()
        {
            InitializeComponent();
        }

        private void OnWarningToggled(object? sender, RoutedEventArgs e)
        {
            if (WarningToggle.IsChecked == true)
            {
                StyledGauge.Classes.Add("warning");
            }
            else
            {
                StyledGauge.Classes.Remove("warning");
            }
        }
    }
}
