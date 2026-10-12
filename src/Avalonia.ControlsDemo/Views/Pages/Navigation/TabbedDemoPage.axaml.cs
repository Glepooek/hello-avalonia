using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TabbedDemoPage : UserControl
    {
        public TabbedDemoPage()
        {
            InitializeComponent();

            Demo.Pages = new Page[]
            {
                DemoPages.Make("收件箱", "收件箱的页面内容。"),
                DemoPages.Make("已发送", "已发送的页面内容。"),
                DemoPages.Make("草稿", "草稿的页面内容。")
            };

            PrevButton.Click += (_, _) => Demo.SelectedIndex = Math.Max(0, Demo.SelectedIndex - 1);
            NextButton.Click += (_, _) => Demo.SelectedIndex = Math.Min(2, Demo.SelectedIndex + 1);

            PlacementBox.ItemsSource = Enum.GetValues<TabPlacement>();
            PlacementBox.SelectedItem = Demo.TabPlacement;
            PlacementBox.SelectionChanged += (_, _) =>
            {
                if (PlacementBox.SelectedItem is TabPlacement value) Demo.TabPlacement = value;
            };

            GestureCheck.IsChecked = Demo.IsGestureEnabled;
            GestureCheck.IsCheckedChanged += (_, _) => Demo.IsGestureEnabled = GestureCheck.IsChecked == true;

            Demo.PropertyChanged += (_, e) =>
            {
                // SelectedPage is updated after SelectedIndex, so listen for it to read the right header.
                if (e.Property == SelectingMultiPage.SelectedIndexProperty || e.Property == SelectingMultiPage.SelectedPageProperty) Refresh();
            };
            Refresh();
        }

        private void Refresh()
        {
            string header = (Demo.SelectedPage as ContentPage)?.Header?.ToString() ?? "（无）";
            StateText.Text = $"SelectedIndex = {Demo.SelectedIndex}，SelectedPage.Header = {header}";
        }
    }
}
