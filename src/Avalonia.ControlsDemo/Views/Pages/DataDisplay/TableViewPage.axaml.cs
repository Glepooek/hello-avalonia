using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TableViewPage : UserControl
    {
        public TableViewPage()
        {
            InitializeComponent();

            Table.ItemsSource = DemoProduct.Sample();
            Table.SelectionChanged += (_, _) =>
                SelectedText.Text = Table.SelectedItem is DemoProduct p ? $"选中：{p.Name}（{p.Category}，{p.Price}）" : "选中：（无）";

            ModeBox.ItemsSource = Enum.GetValues<SelectionMode>();
            ModeBox.SelectedItem = Table.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SelectionMode mode)
                {
                    Table.SelectionMode = mode;
                }
            };

            ResizeCheck.IsChecked = Table.CanUserResizeColumns;
            ResizeCheck.IsCheckedChanged += (_, _) => Table.CanUserResizeColumns = ResizeCheck.IsChecked == true;
        }
    }
}
