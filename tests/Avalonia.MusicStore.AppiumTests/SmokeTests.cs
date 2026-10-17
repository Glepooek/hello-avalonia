using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.MusicStore.AppiumTests
{
    // A1 only: searching the store goes to the iTunes service, so nothing here depends on the network. The album
    // list is filled from the app's local cache, so its length depends on earlier runs and is not checked.
    public class SmokeTests
    {
        private const string App = "Avalonia.MusicStore";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_album_list()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia.MusicStore", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            Assert.Single(app.Driver.FindElementsByTagName("List"));
        }
    }
}
