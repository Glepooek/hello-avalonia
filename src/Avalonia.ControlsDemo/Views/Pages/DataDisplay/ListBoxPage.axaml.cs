using Avalonia.Controls;
using System;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ListBoxPage : UserControl
    {
        public ListBoxPage()
        {
            InitializeComponent();

            List.ItemsSource = Enumerable.Range(1, 20).Select(i => $"第 {i} 项").ToList();
            List.SelectionChanged += (_, _) => SelectionText.Text = $"已选 {List.SelectedItems?.Count ?? 0} 项";

            ModeBox.ItemsSource = Enum.GetValues<SelectionMode>();
            ModeBox.SelectedItem = List.SelectionMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is SelectionMode mode)
                {
                    List.SelectionMode = mode;
                }
            };

            SelectAllButton.Click += (_, _) => List.SelectAll();
            ClearButton.Click += (_, _) => List.UnselectAll();
            JumpButton.Click += (_, _) => List.ScrollIntoView(19);
        }
    }
}
