using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.MusicStore.AppiumTests
{
    // A3: the store window opens over the main window and closes again. Nothing is typed into the search box,
    // so the store never queries the iTunes service and the test needs no network.
    public class FlowTests
    {
        private const string App = "Avalonia.MusicStore";
        private const string StoreWindow = "MusicStoreWindow";

        [AppiumFact(App)]
        public void Store_button_opens_the_store_window_and_its_close_button_closes_it()
        {
            using var app = new AppSession(App);

            // The button shows only an icon; its AutomationProperties.Name gives it a name in the UIA tree.
            app.ByName("Music Store").Click();
            AppSession.WaitFor(() => app.FindWindow(StoreWindow) is not null, "the store window is open");
            var store = app.FindWindow(StoreWindow)!;
            // The search box and the result list; the buy button's text comes from the localized resources.
            Assert.Single(store.FindElementsByTagName("Edit"));
            Assert.Single(store.FindElementsByTagName("List"));

            // The window extends its client area over the title bar, so Avalonia draws its own caption buttons.
            store.FindElementByName("Close").Click();
            AppSession.WaitFor(() => app.FindWindow(StoreWindow) is null, "the store window is closed");
            Assert.Equal("Avalonia.MusicStore", app.Driver.Title);
        }
    }
}
