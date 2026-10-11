using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            InitializeComponent();

            Grid.ItemsSource = DemoProduct.Sample();
            Grid.SelectionChanged += (_, _) =>
                SelectedText.Text = Grid.SelectedItem is DemoProduct p ? $"选中：{p.Name}（{p.Category}，{p.Price}）" : "选中：（无）";

            LinesBox.ItemsSource = Enum.GetValues<DataGridGridLinesVisibility>();
            LinesBox.SelectedItem = Grid.GridLinesVisibility;
            LinesBox.SelectionChanged += (_, _) =>
            {
                if (LinesBox.SelectedItem is DataGridGridLinesVisibility lines)
                {
                    Grid.GridLinesVisibility = lines;
                }
            };

            SelectionBox.ItemsSource = Enum.GetValues<DataGridSelectionMode>();
            SelectionBox.SelectedItem = Grid.SelectionMode;
            SelectionBox.SelectionChanged += (_, _) =>
            {
                if (SelectionBox.SelectedItem is DataGridSelectionMode mode)
                {
                    Grid.SelectionMode = mode;
                }
            };

            ReadOnlyCheck.IsCheckedChanged += (_, _) => Grid.IsReadOnly = ReadOnlyCheck.IsChecked == true;
            ReorderCheck.IsCheckedChanged += (_, _) => Grid.CanUserReorderColumns = ReorderCheck.IsChecked == true;
        }
    }
}
