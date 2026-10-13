using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.PropertySystemDemo.Controls;
using Avalonia.PropertySystemDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.PropertySystemDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaTheory]
        [InlineData(150, 100, "100%")]
        [InlineData(-20, 0, "0%")]
        [InlineData(55, 55, "55%")]
        public void Gauge_coerces_value_into_range_and_mirrors_it_in_readout(double written, double stored, string readout)
        {
            var gauge = new GaugeControl();
            Assert.Equal("0%", gauge.Readout);

            gauge.Value = written;

            Assert.Equal(stored, gauge.Value);
            Assert.Equal(readout, gauge.Readout);
        }

        [AvaloniaFact]
        public void Highlight_is_a_plain_attached_property_but_tag_line_inherits_down_the_tree()
        {
            var parent = new StackPanel();
            var child = new Button();
            parent.Children.Add(child);
            UiHelpers.Show(parent);

            Assert.False(HighlightBehavior.GetIsHighlighted(child));
            HighlightBehavior.SetIsHighlighted(parent, true);
            Assert.False(HighlightBehavior.GetIsHighlighted(child));

            Assert.Null(HighlightBehavior.GetTagLine(child));
            HighlightBehavior.SetTagLine(parent, "hello");
            Assert.Equal("hello", HighlightBehavior.GetTagLine(child));
        }

        [AvaloniaFact]
        public void Local_slider_value_is_clamped_by_the_gauge_it_drives()
        {
            var window = UiHelpers.Show(new StyledPropertyPage());
            var gauge = window.GetVisualDescendants().OfType<GaugeControl>().First();

            window.ByName<Slider>("ValueSlider").Value = 150;

            Assert.Equal(100, gauge.Value);
        }

        // StyledGauge deliberately has no local Value: a local value would outrank the style setter for good.
        [AvaloniaFact]
        public void Warning_class_sets_the_value_through_a_style_and_removing_it_restores_the_default()
        {
            var window = UiHelpers.Show(new StyledPropertyPage());
            var gauge = window.ByName<GaugeControl>("StyledGauge");
            Assert.Equal(0, gauge.Value);

            window.ByName<CheckBox>("WarningToggle").IsChecked = true;
            Assert.Equal(90, gauge.Value);
            Assert.Equal("90%", gauge.Readout);

            window.ByName<CheckBox>("WarningToggle").IsChecked = false;
            Assert.Equal(0, gauge.Value);
        }

        [AvaloniaFact]
        public void Direct_property_readout_follows_the_driving_slider()
        {
            var window = UiHelpers.Show(new DirectPropertyPage());

            window.ByName<Slider>("Driver").Value = 60;

            Assert.Equal("60%", window.ByName<GaugeControl>("Gauge").Readout);
        }

        [AvaloniaFact]
        public void Attached_page_highlights_target_a_only_and_tag_line_reaches_both_levels()
        {
            var window = UiHelpers.Show(new AttachedPropertyPage());
            var a = window.ByName<Button>("TargetA");
            var b = window.ByName<Button>("TargetB");

            window.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked = true;
            Assert.True(HighlightBehavior.GetIsHighlighted(a));
            Assert.False(HighlightBehavior.GetIsHighlighted(b));

            Assert.Equal("第 1 层读到：（尚未设置）", window.ByName<TextBlock>("Level1Text").Text);
            window.Click(ButtonWithText(window, "在最外层设置 TagLine"));
            Assert.Equal("第 1 层读到：我在最外层被设置", window.ByName<TextBlock>("Level1Text").Text);
            Assert.Equal("第 2 层读到：我在最外层被设置", window.ByName<TextBlock>("Level2Text").Text);
        }

        // The readouts are first filled on attach, before layout, so they say 0; the button re-reads after layout.
        [AvaloniaFact]
        public void Precedence_page_local_value_beats_style_and_binding_follows_the_view_model()
        {
            var window = UiHelpers.Show(new PrecedencePage());

            window.Click(ButtonWithText(window, "重新读取"));

            Assert.Equal("A 实际宽度：300（样式给的 300）", window.ByName<TextBlock>("ReadoutA").Text);
            Assert.Equal("B 实际宽度：150（本地值 150 压过样式 300）", window.ByName<TextBlock>("ReadoutB").Text);
            Assert.Equal("C 实际宽度：220（跟随滑块）", window.ByName<TextBlock>("ReadoutC").Text);
        }

        [AvaloniaTheory]
        [InlineData("写入 150", "写入 150，实际存下 100（被 coerce 夹取了）")]
        [InlineData("写入 -20", "写入 -20，实际存下 0（被 coerce 夹取了）")]
        [InlineData("写入 55", "写入 55，实际存下 55（在范围内，原样保留）")]
        public void Metadata_page_reports_what_the_coerce_callback_did(string button, string expected)
        {
            var window = UiHelpers.Show(new MetadataPage());

            window.Click(ButtonWithText(window, button));

            Assert.Equal(expected, window.ByName<TextBlock>("CoerceLog").Text);
        }
    }
}
