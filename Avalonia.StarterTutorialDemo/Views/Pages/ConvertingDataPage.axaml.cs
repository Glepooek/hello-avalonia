using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.StarterTutorialDemo.Views.Pages
{
    public partial class ConvertingDataPage : UserControl
    {
        public ConvertingDataPage()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            if (double.TryParse(Celsius.Text, out double c))
            {
                var f = c * (9d / 5d) + 32;
                Fahrenheit.Text = f.ToString("0.0");
            }
            else
            {
                Celsius.Text = "0";
                Fahrenheit.Text = "0";
            }
        }
    }
}
