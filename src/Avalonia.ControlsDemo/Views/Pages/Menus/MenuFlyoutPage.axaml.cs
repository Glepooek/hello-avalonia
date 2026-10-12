using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class MenuFlyoutPage : UserControl
    {
        private int _opened;
        private int _closed;

        public MenuFlyoutPage()
        {
            InitializeComponent();

            PlacementBox.ItemsSource = new[]
            {
                PlacementMode.Bottom, PlacementMode.Top, PlacementMode.Right, PlacementMode.Left, PlacementMode.BottomEdgeAlignedLeft,
            };
            PlacementBox.SelectedItem = ItemsFlyout.Placement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is PlacementMode mode)
                {
                    ItemsFlyout.Placement = mode;
                }
            };

            ItemsFlyout.Opened += (_, _) => { _opened++; Show(); };
            ItemsFlyout.Closed += (_, _) => { _closed++; Show(); };
            ShowButton.Click += (_, _) => ItemsFlyout.ShowAt(Trigger);
        }

        // An element set as Button.Flyout is not in the XAML name scope, so no field is generated for it.
        private MenuFlyout ItemsFlyout => (MenuFlyout)Trigger.Flyout!;

        private void Show() => LogText.Text = $"Opened {_opened} 次，Closed {_closed} 次";
    }
}
