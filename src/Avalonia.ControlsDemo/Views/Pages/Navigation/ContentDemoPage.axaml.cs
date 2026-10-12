using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ContentDemoPage : UserControl
    {
        // The slot is cleared by setting it to null, so keep the original bar to put it back.
        private readonly object? _bottomBar;

        public ContentDemoPage()
        {
            InitializeComponent();
            _bottomBar = Demo.BottomCommandBar;

            HAlignBox.ItemsSource = Enum.GetValues<HorizontalAlignment>();
            HAlignBox.SelectedItem = Demo.HorizontalContentAlignment;
            HAlignBox.SelectionChanged += (_, _) =>
            {
                if (HAlignBox.SelectedItem is HorizontalAlignment value) Demo.HorizontalContentAlignment = value;
            };

            VAlignBox.ItemsSource = Enum.GetValues<VerticalAlignment>();
            VAlignBox.SelectedItem = Demo.VerticalContentAlignment;
            VAlignBox.SelectionChanged += (_, _) =>
            {
                if (VAlignBox.SelectedItem is VerticalAlignment value) Demo.VerticalContentAlignment = value;
            };

            BottomCheck.IsChecked = true;
            BottomCheck.IsCheckedChanged += (_, _) =>
            {
                Demo.BottomCommandBar = BottomCheck.IsChecked == true ? _bottomBar : null;
            };

            Demo.AddHandler(Button.ClickEvent, OnDemoClick);
        }

        private void OnDemoClick(object? sender, RoutedEventArgs e)
        {
            if (e.Source is CommandBarButton button)
            {
                ClickText.Text = $"最近点击：{button.Label}";
            }
        }
    }
}
