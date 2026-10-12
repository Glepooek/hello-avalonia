using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class PopupPage : UserControl
    {
        private int _opened;

        public PopupPage()
        {
            InitializeComponent();

            PlacementBox.ItemsSource = new[]
            {
                PlacementMode.Bottom, PlacementMode.Top, PlacementMode.Left, PlacementMode.Right, PlacementMode.Pointer,
            };
            PlacementBox.SelectedItem = PlacementMode.Bottom;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is PlacementMode mode)
                {
                    Pop.Placement = mode;
                }
            };

            LightDismissCheck.IsCheckedChanged += (_, _) => Pop.IsLightDismissEnabled = LightDismissCheck.IsChecked == true;
            ToggleButton.Click += (_, _) => Pop.IsOpen = !Pop.IsOpen;
            Pop.Opened += (_, _) =>
            {
                _opened++;
                Show();
            };
            Pop.Closed += (_, _) => Show();
        }

        private void Show() => StateText.Text = $"IsOpen = {Pop.IsOpen}，打开 {_opened} 次";
    }
}
