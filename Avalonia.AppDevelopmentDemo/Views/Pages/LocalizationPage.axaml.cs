using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class LocalizationPage : UserControl
    {
        // The manifest name is the assembly's root namespace + folder path + file name.
        private static readonly ResourceManager Strings =
            new("Avalonia.AppDevelopmentDemo.Assets.Langs.Strings", Assembly.GetExecutingAssembly());

        public LocalizationPage()
        {
            InitializeComponent();
            Apply(new CultureInfo("zh-CN"));
        }

        private void OnLangChanged(object? sender, RoutedEventArgs e)
        {
            // Decide from the sender, not from the sibling's state: the sibling is unchecked only after this event.
            if (sender is RadioButton { IsChecked: true } radio)
                Apply(new CultureInfo(ReferenceEquals(radio, EnglishRadio) ? "en-US" : "zh-CN"));
        }

        private void Apply(CultureInfo culture)
        {
            GreetingText.Text = Strings.GetString("Greeting", culture);
            FarewellText.Text = Strings.GetString("Farewell", culture);
            CultureText.Text = $"culture = {culture.Name}";
        }
    }
}
