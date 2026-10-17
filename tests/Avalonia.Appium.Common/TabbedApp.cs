using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.Appium.Common
{
    // The A1 and A2 checks every TabControl demo shares; each app's test project only supplies its title and tabs.
    public static class TabbedApp
    {
        // A1: the app opens one main window with the expected title and exactly these tabs, in this order.
        public static void AssertStartsWithItsTabs(string app, string title, IReadOnlyList<string> tabs)
        {
            using var session = new AppSession(app);

            Assert.Equal(title, session.Driver.Title);
            Assert.Single(session.Driver.WindowHandles);
            Assert.Equal(tabs, session.Driver.FindElementsByTagName("TabItem").Select(t => t.Text));
        }

        // A2: each tab can be selected and then shows visible text of its own. Comparing with the previous tab's
        // texts proves the page really swapped; a stale tree would still have visible text.
        public static void AssertEveryTabShowsItsPage(string app, IReadOnlyList<string> tabs)
        {
            using var session = new AppSession(app);
            var tabControl = session.Driver.FindElementByTagName("Tab");
            IReadOnlyList<string>? previous = null;

            foreach (var header in tabs)
            {
                // Picked from the tab strip, not by name, so text on a page can never be clicked instead.
                var tab = session.Driver.FindElementsByTagName("TabItem").Single(t => t.Text == header);
                tab.Click();
                AppSession.WaitFor(() => tab.Selected, $"tab {header} is selected");

                IReadOnlyList<string> texts = Array.Empty<string>();
                AppSession.WaitFor(() =>
                {
                    var found = tabControl.FindElementsByTagName("Text");
                    texts = found.Select(e => e.Text ?? "").ToList();
                    return found.Any(e => e.Displayed) && (previous is null || !texts.SequenceEqual(previous));
                }, $"tab {header} shows visible text that differs from the previous tab");
                previous = texts;
            }

            Assert.Single(session.Driver.WindowHandles);
        }
    }
}
