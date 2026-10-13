using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.StylingDemo.Controls;
using Avalonia.StylingDemo.Views.Pages;
using Avalonia.Styling;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.StylingDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaFact]
        public void Toggle_chip_owns_the_on_pseudo_class_while_it_is_latched()
        {
            var chip = new ToggleChip();
            Assert.False(chip.IsOn);
            Assert.DoesNotContain(":on", chip.Classes);

            chip.IsOn = true;
            Assert.Contains(":on", chip.Classes);

            chip.IsOn = false;
            Assert.DoesNotContain(":on", chip.Classes);
        }

        [AvaloniaFact]
        public void Clicking_the_chip_latches_it_and_the_page_text_follows()
        {
            var window = UiHelpers.Show(new PseudoClassesPage());
            var chip = window.ByName<ToggleChip>("Chip");

            window.Click(chip);

            Assert.True(chip.IsOn);
            Assert.Contains(":on", chip.Classes);
            Assert.Contains("IsOn = True", window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));

            window.Click(chip);
            Assert.False(chip.IsOn);
            Assert.DoesNotContain(":on", chip.Classes);
        }

        [AvaloniaFact]
        public void Unchecking_the_box_disables_the_controlled_button_and_marks_it_with_the_disabled_pseudo_class()
        {
            var window = UiHelpers.Show(new PseudoClassesPage());
            var controlled = ButtonWithText(window, "被控制的按钮");
            Assert.DoesNotContain(":disabled", controlled.Classes);

            window.ByName<CheckBox>("EnableBox").IsChecked = false;

            Assert.False(controlled.IsEnabled);
            Assert.Contains(":disabled", controlled.Classes);
        }

        [AvaloniaFact]
        public void Warn_switch_adds_and_removes_the_warn_class_through_a_binding()
        {
            var window = UiHelpers.Show(new StyleClassesPage());
            var swatch = window.ByName<Border>("BoundSwatch");
            Assert.DoesNotContain("warn", swatch.Classes);

            window.ByName<ToggleSwitch>("WarnSwitch").IsChecked = true;
            Assert.Contains("warn", swatch.Classes);

            window.ByName<ToggleSwitch>("WarnSwitch").IsChecked = false;
            Assert.DoesNotContain("warn", swatch.Classes);
        }

        [AvaloniaFact]
        public void Toggle_green_button_edits_the_classes_of_the_code_swatch()
        {
            var window = UiHelpers.Show(new StyleClassesPage());
            var text = window.ByName<TextBlock>("CodeClassesText");
            Assert.Equal("当前 Classes：swatch", text.Text);

            window.Click(ButtonWithText(window, "切换 green"));
            Assert.Equal("当前 Classes：swatch green", text.Text);

            window.Click(ButtonWithText(window, "切换 green"));
            Assert.Equal("当前 Classes：swatch", text.Text);
        }

        [AvaloniaFact]
        public void Control_themes_set_size_and_derived_themes_inherit_the_base_setters()
        {
            var window = UiHelpers.Show(new ControlThemesPage());

            Assert.Equal(200, window.ByName<Button>("YellowButton").Width);
            Assert.Equal(40, window.ByName<Button>("YellowButton").Height);
            Assert.Equal(200, window.ByName<Button>("WideButton").Width);
            Assert.Equal(200, window.ByName<Button>("TallWideButton").Width);
            Assert.Equal(56, window.ByName<Button>("TallWideButton").Height);
        }

        // The page writes Application.RequestedThemeVariant, which is process-wide: always put it back.
        [AvaloniaFact]
        public void Theme_buttons_change_the_requested_and_actual_variant_and_the_app_is_restored()
        {
            var app = Avalonia.Application.Current!;
            var original = app.RequestedThemeVariant;
            try
            {
                var window = UiHelpers.Show(new ThemesPage());
                var text = window.ByName<TextBlock>("ActualText");

                window.Click(ButtonWithText(window, "Dark"));
                Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
                Assert.Equal("请求：Dark　实际：Dark", text.Text);

                window.Click(ButtonWithText(window, "Light"));
                Assert.Equal("请求：Light　实际：Light", text.Text);
            }
            finally
            {
                app.RequestedThemeVariant = original;
            }

            Assert.Equal(original, app.RequestedThemeVariant);
        }

        [AvaloniaFact]
        public void Letter_spacing_and_line_height_follow_their_sliders()
        {
            var window = UiHelpers.Show(new FontsPage());

            window.ByName<Slider>("SpacingSlider").Value = 4;
            window.ByName<Slider>("LineSlider").Value = 40;

            // The sample text is the only wrapping 16pt TextBlock on the page.
            var sample = window.GetVisualDescendants().OfType<TextBlock>()
                .Single(t => t.TextWrapping == Avalonia.Media.TextWrapping.Wrap && t.FontSize == 16);
            Assert.Equal(4, sample.LetterSpacing);
            Assert.Equal(40, sample.LineHeight);
        }
    }
}
