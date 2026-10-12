using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class AutoCompleteBoxPage : UserControl
    {
        public AutoCompleteBoxPage()
        {
            InitializeComponent();

            Box.ItemsSource = new[] { "Apple", "Apricot", "Banana", "Blueberry", "Cherry" };
            Box.FilterMode = AutoCompleteFilterMode.Contains;

            ModeBox.ItemsSource = Enum.GetValues<AutoCompleteFilterMode>();
            ModeBox.SelectedItem = Box.FilterMode;
            ModeBox.SelectionChanged += (_, _) =>
            {
                if (ModeBox.SelectedItem is AutoCompleteFilterMode mode)
                {
                    Box.FilterMode = mode;
                }
            };

            Box.SelectionChanged += (_, _) =>
                BoxResult.Text = Box.SelectedItem is string item ? $"已选：{item}" : "未选择";
        }
    }
}
