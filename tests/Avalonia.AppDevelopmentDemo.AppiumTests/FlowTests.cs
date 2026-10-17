using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.AppiumTests
{
    // A3: the AutomationProperties set in XAML reach the real Windows UIA tree, which is what a screen reader or a
    // UI test sees, and they agree with what the page reads back from Avalonia's own AutomationPeer.
    public class FlowTests
    {
        private const string App = "Avalonia.AppDevelopmentDemo";

        [AppiumFact(App)]
        public void Automation_properties_reach_the_UIA_tree_and_match_the_peer()
        {
            using var app = new AppSession(App);
            app.SelectTab("无障碍");

            var ok = app.ById("ok-btn");
            Assert.Equal("ControlType.Button", ok.TagName);
            // AutomationProperties.Name replaces the button's content "确定" as its UIA name.
            Assert.Equal("Confirm", ok.Text);
            Assert.Empty(app.Driver.FindElementsByName("确定"));
            Assert.Equal("Confirms the form", ok.GetAttribute("HelpText"));

            app.ByName("读取 Peer").Click();
            app.WaitForText("PeerName", "Name：Confirm");
            Assert.Equal("AutomationId：ok-btn", app.ById("PeerId").Text);
            Assert.Equal("HelpText：Confirms the form", app.ById("PeerHelp").Text);
            Assert.Equal("ControlType：Button", app.ById("PeerControl").Text);
        }
    }
}
