using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class AccessibilityPage : UserControl
    {
        public AccessibilityPage()
        {
            InitializeComponent();
            ViewLine.Text = $"Decoration 的 AccessibilityView = {AutomationProperties.GetAccessibilityView(Decoration)}";
        }

        private void OnRead(object? sender, RoutedEventArgs e)
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(Target);
            PeerType.Text = $"Peer 类型：{peer.GetType().Name}";
            PeerName.Text = $"Name：{peer.GetName()}";
            PeerId.Text = $"AutomationId：{peer.GetAutomationId()}";
            PeerHelp.Text = $"HelpText：{peer.GetHelpText()}";
            PeerControl.Text = $"ControlType：{peer.GetAutomationControlType()}";
        }
    }
}
