using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DrawerDemoPage : UserControl
    {
        private int _opened, _closed;

        public DrawerDemoPage()
        {
            InitializeComponent();

            OpenCheck.IsChecked = Demo.IsOpen;
            OpenCheck.IsCheckedChanged += (_, _) =>
            {
                Demo.IsOpen = OpenCheck.IsChecked == true;
                Refresh();
            };
            InDrawerButton.Click += (_, _) =>
            {
                Demo.IsOpen = false;
                Refresh();
            };

            BehaviorBox.ItemsSource = Enum.GetValues<DrawerBehavior>();
            BehaviorBox.SelectedItem = Demo.DrawerBehavior;
            BehaviorBox.SelectionChanged += (_, _) =>
            {
                if (BehaviorBox.SelectedItem is DrawerBehavior value) Demo.DrawerBehavior = value;
                Refresh();
            };

            PlacementBox.ItemsSource = Enum.GetValues<DrawerPlacement>();
            PlacementBox.SelectedItem = Demo.DrawerPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is DrawerPlacement value) Demo.DrawerPlacement = value;
            };

            LayoutBox.ItemsSource = Enum.GetValues<DrawerLayoutBehavior>();
            LayoutBox.SelectedItem = Demo.DrawerLayoutBehavior;
            LayoutBox.SelectionChanged += (_, _) =>
            {
                if (LayoutBox.SelectedItem is DrawerLayoutBehavior value) Demo.DrawerLayoutBehavior = value;
                Refresh();
            };

            LengthSlider.Value = Demo.DrawerLength;
            LengthSlider.ValueChanged += (_, e) => Demo.DrawerLength = e.NewValue;

            Demo.Opened += (_, _) => { _opened++; Refresh(); };
            Demo.Closed += (_, _) => { _closed++; Refresh(); };

            Refresh();
        }

        private void Refresh()
        {
            // Locked opens the drawer on its own, so read the real value back instead of trusting the box.
            OpenCheck.IsChecked = Demo.IsOpen;
            StateText.Text = $"IsOpen = {Demo.IsOpen}，DisplayMode = {Demo.DisplayMode}";
            EventText.Text = $"Opened {_opened} 次，Closed {_closed} 次";
        }
    }
}
