using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class ScrollBarPage : UserControl
    {
        private int _scrollCount;
        private ScrollEventType? _lastType;

        public ScrollBarPage()
        {
            InitializeComponent();

            LineUpButton.Click += (_, _) => Bar.LineUp();
            LineDownButton.Click += (_, _) => Bar.LineDown();
            PageUpButton.Click += (_, _) => Bar.PageUp();
            PageDownButton.Click += (_, _) => Bar.PageDown();
            HomeButton.Click += (_, _) => Bar.ScrollToHome();
            EndButton.Click += (_, _) => Bar.ScrollToEnd();

            Bar.Scroll += (_, e) =>
            {
                _scrollCount++;
                _lastType = e.ScrollEventType;
                Refresh();
            };
            Bar.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty) Refresh();
            };

            ValueSlider.ValueChanged += (_, e) => Bar.Value = e.NewValue;

            VisibilityBox.ItemsSource = Enum.GetValues<ScrollBarVisibility>();
            VisibilityBox.SelectedItem = Bar.Visibility;
            VisibilityBox.SelectionChanged += (_, _) =>
            {
                if (VisibilityBox.SelectedItem is ScrollBarVisibility value) Bar.Visibility = value;
            };

            AutoHideCheck.IsChecked = Bar.AllowAutoHide;
            AutoHideCheck.IsCheckedChanged += (_, _) => Bar.AllowAutoHide = AutoHideCheck.IsChecked == true;

            Refresh();
        }

        private void Refresh()
        {
            // The slider only mirrors the value; writing the same number back is a no-op.
            ValueSlider.Value = Bar.Value;
            ValueText.Text = $"Value = {Bar.Value:0.#}（Maximum = {Bar.Maximum:0.#}，ViewportSize = {Bar.ViewportSize:0.#}）";
            ScrollText.Text = $"Scroll 事件 {_scrollCount} 次，最近一次：{(_lastType?.ToString() ?? "（无）")}";
        }
    }
}
