using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextBlockPage : UserControl
    {
        public TextBlockPage()
        {
            InitializeComponent();

            SizeSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Sample.FontSize = SizeSlider.Value;
                }
            };
            MaxLinesSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Sample.MaxLines = (int)MaxLinesSlider.Value;
                }
            };

            WrapBox.ItemsSource = Enum.GetValues<TextWrapping>();
            WrapBox.SelectedItem = Sample.TextWrapping;
            WrapBox.SelectionChanged += (_, _) =>
            {
                if (WrapBox.SelectedItem is TextWrapping wrapping)
                {
                    Sample.TextWrapping = wrapping;
                }
            };

            AlignBox.ItemsSource = Enum.GetValues<TextAlignment>();
            AlignBox.SelectedItem = Sample.TextAlignment;
            AlignBox.SelectionChanged += (_, _) =>
            {
                if (AlignBox.SelectedItem is TextAlignment alignment)
                {
                    Sample.TextAlignment = alignment;
                }
            };
        }
    }
}
