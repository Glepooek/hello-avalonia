using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.ControlsDemo.AppiumTests
{
    // A1 for the one demo that navigates with a TreeView instead of a TabControl.
    public class SmokeTests
    {
        private const string App = "Avalonia.ControlsDemo";

        // The order of the official Controls sidebar, as Navigation/Categories.cs lists it.
        private static readonly string[] Categories =
        {
            "Data display", "Feedback", "Input", "Layout", "Media", "Menus", "Navigation", "Primitives", "System", "Web",
            "付费控件", "How-to 路标",
        };

        [AppiumFact(App)]
        public void App_starts_with_every_category_in_the_tree_and_the_first_one_open()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia Controls Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            // Tree items carry no text of their own; their headers show up as Text elements under the tree.
            var texts = app.Driver.FindElementByTagName("Tree").FindElementsByTagName("Text").Select(t => t.Text).ToList();
            Assert.Equal(Categories, texts.Where(Categories.Contains));
            Assert.Equal(new[] { "Data display", "Carousel" }, texts.Take(2));
        }
    }
}
