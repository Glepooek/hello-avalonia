using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using System;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class UiHelpersTests
    {
        [AvaloniaFact]
        public void Click_reaches_a_control_that_starts_below_the_scroll_viewport()
        {
            var clicks = 0;
            var button = new Button { Content = "far away", Height = 40 };
            button.Click += (_, _) => clicks++;
            // The test window is 600x480, so a 2000px spacer pushes the button far out of view.
            var panel = new StackPanel { Children = { new Border { Height = 2000 }, button } };
            var window = UiHelpers.Show(new ScrollViewer { Content = panel });

            window.Click(button);

            Assert.Equal(1, clicks);
        }

        // Button.Content is not always a string (DemoHeader has an icon button), so the match must not cast it.
        [AvaloniaFact]
        public void ButtonWithText_finds_the_button_by_its_text_and_skips_buttons_with_other_content()
        {
            var target = new Button { Content = "go" };
            var window = UiHelpers.Show(new StackPanel
            {
                Children = { new Button { Content = new Border() }, new Button { Content = "stop" }, target },
            });

            Assert.Same(target, window.ButtonWithText("go"));
            Assert.Throws<InvalidOperationException>(() => window.ButtonWithText("missing"));
        }
    }
}
