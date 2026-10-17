using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.HtmlRendererDemo.AppiumTests
{
    // A1. HtmlPanel draws its own text and has no UIA children, so the labels around it are what can be checked.
    public class SmokeTests
    {
        private const string App = "Avalonia.HtmlRendererDemo";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_labels()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia HtmlRenderer & IconFont Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            var texts = app.Driver.FindElementsByTagName("Text").Select(t => t.Text).ToList();
            Assert.Contains("HtmlPanel (rich text)", texts);
            Assert.Contains("IconFont", texts);
            Assert.Contains("PathIcon", texts);
        }
    }
}
