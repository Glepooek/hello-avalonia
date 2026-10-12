using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TextTrimmingPage : UserControl
    {
        // TextTrimming is a class with static instances rather than an enum, so the box lists names and maps them back.
        private static readonly (string Name, TextTrimming Value)[] Options =
        [
            ("None", TextTrimming.None),
            ("CharacterEllipsis", TextTrimming.CharacterEllipsis),
            ("WordEllipsis", TextTrimming.WordEllipsis),
            ("PrefixCharacterEllipsis", TextTrimming.PrefixCharacterEllipsis),
            ("LeadingCharacterEllipsis", TextTrimming.LeadingCharacterEllipsis),
            ("PathSegmentEllipsis", TextTrimming.PathSegmentEllipsis),
        ];

        public TextTrimmingPage()
        {
            InitializeComponent();

            var names = new List<string>();
            foreach (var option in Options)
            {
                names.Add(option.Name);
            }

            TrimBox.ItemsSource = names;
            TrimBox.SelectedIndex = 1;
            Trimmed.TextTrimming = Options[1].Value;
            TrimBox.SelectionChanged += (_, _) =>
            {
                if (TrimBox.SelectedIndex >= 0)
                {
                    Trimmed.TextTrimming = Options[TrimBox.SelectedIndex].Value;
                }
            };

            WidthSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty)
                {
                    Trimmed.Width = WidthSlider.Value;
                }
            };
            LengthText.Text = $"Text 长度 = {Trimmed.Text!.Length}（无论怎么截都不变）";
        }
    }
}
