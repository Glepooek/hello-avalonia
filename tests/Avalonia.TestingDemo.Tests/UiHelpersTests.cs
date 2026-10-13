using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
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
    }
}
