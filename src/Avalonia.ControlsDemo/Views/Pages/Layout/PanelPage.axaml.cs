using Avalonia.Controls;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PanelPage : UserControl
    {
        public PanelPage()
        {
            InitializeComponent();

            // ZIndex is a plain property on every Visual; the larger one is drawn on top.
            SwapButton.Click += (_, _) =>
            {
                var overOnTop = Over.ZIndex >= Under.ZIndex;
                Over.ZIndex = overOnTop ? -1 : 1;
                Under.ZIndex = overOnTop ? 1 : -1;
            };
        }
    }
}
