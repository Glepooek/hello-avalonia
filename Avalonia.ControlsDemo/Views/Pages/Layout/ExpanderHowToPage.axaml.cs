using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ExpanderHowToPage : UserControl
    {
        public ExpanderHowToPage()
        {
            InitializeComponent();
            DataContext = new ExpanderHowToViewModel();

            DirectionBox.ItemsSource = Enum.GetValues<ExpandDirection>();
            DirectionBox.SelectedItem = DirectionDemo.ExpandDirection;
            DirectionBox.SelectionChanged += (_, _) =>
            {
                if (DirectionBox.SelectedItem is ExpandDirection direction)
                {
                    DirectionDemo.ExpandDirection = direction;
                }
            };
            DisabledCheck.IsCheckedChanged += (_, _) => DirectionDemo.IsEnabled = DisabledCheck.IsChecked != true;
        }
    }
}
