using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class SplitViewPage : UserControl
    {
        public SplitViewPage()
        {
            InitializeComponent();

            ToggleButton.Click += (_, _) => Split.IsPaneOpen = !Split.IsPaneOpen;
            Split.PaneOpened += (_, _) => PaneText.Text = "窗格：打开";
            Split.PaneClosed += (_, _) => PaneText.Text = "窗格：关闭";

            ModeBox.ItemsSource = Enum.GetValues<SplitViewDisplayMode>();
            ModeBox.SelectedItem = Split.DisplayMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SplitViewDisplayMode mode)
                {
                    Split.DisplayMode = mode;
                }
            };

            PlacementBox.ItemsSource = Enum.GetValues<SplitViewPanePlacement>();
            PlacementBox.SelectedItem = Split.PanePlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is SplitViewPanePlacement placement)
                {
                    Split.PanePlacement = placement;
                }
            };
        }
    }
}
