using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class MobileServicesPage : UserControl
    {
        public MobileServicesPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Both services hang off the TopLevel and are null where the platform has no equivalent.
            var topLevel = TopLevel.GetTopLevel(this);

            InputPaneLine.Text = topLevel?.InputPane is { } pane
                ? $"InputPane：可用，State = {pane.State}，OccludedRect = {pane.OccludedRect}"
                : "InputPane：null（当前平台没有软键盘服务）";

            InsetsLine.Text = topLevel?.InsetsManager is { } insets
                ? $"InsetsManager：可用，SafeAreaPadding = {insets.SafeAreaPadding}"
                : "InsetsManager：null（当前平台没有安全区服务）";
        }
    }
}
