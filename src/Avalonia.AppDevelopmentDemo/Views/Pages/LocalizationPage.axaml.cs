using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
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
            {
                var cultureName = ReferenceEquals(radio, EnglishRadio)
                    ? "en-US"
                    : ReferenceEquals(radio, ArabicRadio) ? "ar-SA" : "zh-CN";
                Apply(new CultureInfo(cultureName));
            }
        }

        private void Apply(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            GreetingText.Text = Strings.GetString("Greeting", culture);
            FarewellText.Text = Strings.GetString("Farewell", culture);
            CultureText.Text = $"culture = {culture.Name}";
            FormatText.Text = string.Format(culture, "{0:N2} · {1:D}", 12345.67, new DateTime(2026, 10, 25));
            var direction = culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            LocalizedPreview.FlowDirection = direction;
            DirectionText.Text = $"اتجاه المعاينة: {direction}";
        }
    }
}
