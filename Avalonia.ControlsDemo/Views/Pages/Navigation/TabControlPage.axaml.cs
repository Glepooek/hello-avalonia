using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabControlPage : UserControl
    {
        public TabControlPage()
        {
            InitializeComponent();

            PlacementBox.ItemsSource = Enum.GetValues<Dock>();
            PlacementBox.SelectedItem = Tabs.TabStripPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is not Dock dock) return;

                // Changing the placement swaps the template and resets the selection to 0.
                int selected = Tabs.SelectedIndex;
                Tabs.TabStripPlacement = dock;
                Dispatcher.UIThread.Post(() => Tabs.SelectedIndex = selected, DispatcherPriority.Loaded);
            };

            DisabledButton.Click += (_, _) => Tabs.SelectedIndex = 1;
            Tabs.SelectionChanged += (_, _) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            string header = (Tabs.SelectedItem as TabItem)?.Header?.ToString() ?? "（无）";
            StateText.Text = $"SelectedIndex = {Tabs.SelectedIndex}，选中的标签 = {header}";
        }
    }
}
