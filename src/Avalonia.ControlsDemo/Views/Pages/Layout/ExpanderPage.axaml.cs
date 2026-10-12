using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ExpanderPage : UserControl
    {
        private int _expanded, _collapsed;

        public ExpanderPage()
        {
            InitializeComponent();

            Basic.Expanded += (_, _) => { _expanded++; ShowCounts(); };
            Basic.Collapsed += (_, _) => { _collapsed++; ShowCounts(); };
            EnabledCheck.IsCheckedChanged += (_, _) => Basic.IsEnabled = EnabledCheck.IsChecked == true;

            DirectionBox.ItemsSource = Enum.GetValues<ExpandDirection>();
            DirectionBox.SelectedItem = Sideways.ExpandDirection;
            DirectionBox.SelectionChanged += (_, _) =>
            {
                if (DirectionBox.SelectedItem is ExpandDirection direction)
                {
                    Sideways.ExpandDirection = direction;
                }
            };
        }

        private void ShowCounts() => BasicResult.Text = $"已展开 {_expanded} 次，已折叠 {_collapsed} 次";
    }
}
