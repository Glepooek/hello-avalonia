using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Threading;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabControlHowToPage : UserControl
    {
        public TabControlHowToPage()
        {
            InitializeComponent();
            var viewModel = new TabControlHowToViewModel();
            DataContext = viewModel;

            PlacementBox.ItemsSource = Enum.GetValues<Dock>();
            PlacementBox.SelectedItem = Tabs.TabStripPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is not Dock dock) return;

                // Changing the placement swaps the template, which resets (and writes back) the selection.
                var keep = viewModel.SelectedTab;
                Tabs.TabStripPlacement = dock;
                Dispatcher.UIThread.Post(() => viewModel.SelectedTab = keep, DispatcherPriority.Loaded);
            };
        }
    }
}
