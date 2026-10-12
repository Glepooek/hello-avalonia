using Avalonia.Controls;

namespace Avalonia.StarterTutorialDemo.Views.Pages
{
    public partial class ExercisesPage : UserControl
    {
        public ExercisesPage()
        {
            InitializeComponent();
        }

        private void Celsius_TextChanged(object? sender, TextChangedEventArgs e)
        {
            // Allow an empty box or a lone minus sign so negative numbers can be typed.
            if (string.IsNullOrEmpty(Celsius.Text) || Celsius.Text == "-")
            {
                Fahrenheit.Text = "";
            }
            else if (double.TryParse(Celsius.Text, out double c))
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
