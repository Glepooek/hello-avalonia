using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.StarterTutorialDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.StarterTutorialDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        [AvaloniaFact]
        public void Events_page_counts_clicks()
        {
            var window = UiHelpers.Show(new EventsPage());
            var button = window.GetVisualDescendants().OfType<Button>().Single(b => "Calculate".Equals(b.Content));

            window.Click(button);
            window.Click(button);

            Assert.Equal("已点击 2 次", window.ByName<TextBlock>("ClickInfo").Text);
        }

        [AvaloniaTheory]
        [InlineData("100", "212.0")]
        [InlineData("-40", "-40.0")]
        [InlineData("37", "98.6")]
        public void Converting_page_calculate_button_converts_celsius_to_fahrenheit(string celsius, string expected)
        {
            var window = UiHelpers.Show(new ConvertingDataPage());
            window.ByName<TextBox>("Celsius").Text = celsius;

            window.Click(window.GetVisualDescendants().OfType<Button>().Single(b => "Calculate".Equals(b.Content)));

            Assert.Equal(expected, window.ByName<TextBox>("Fahrenheit").Text);
        }

        [AvaloniaFact]
        public void Converting_page_resets_both_boxes_on_input_that_is_not_a_number()
        {
            var window = UiHelpers.Show(new ConvertingDataPage());
            window.ByName<TextBox>("Celsius").Text = "abc";

            window.Click(window.GetVisualDescendants().OfType<Button>().Single(b => "Calculate".Equals(b.Content)));

            Assert.Equal("0", window.ByName<TextBox>("Celsius").Text);
            Assert.Equal("0", window.ByName<TextBox>("Fahrenheit").Text);
        }

        [AvaloniaFact]
        public void Exercises_page_converts_live_as_the_celsius_text_changes()
        {
            var window = UiHelpers.Show(new ExercisesPage());
            var celsius = window.ByName<TextBox>("Celsius");
            var fahrenheit = window.ByName<TextBox>("Fahrenheit");

            // TextChanged is raised through the dispatcher, so drain it after every edit.
            celsius.Text = "100";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("212.0", fahrenheit.Text);

            celsius.Text = "-";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", fahrenheit.Text);

            celsius.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", fahrenheit.Text);
        }
    }
}
