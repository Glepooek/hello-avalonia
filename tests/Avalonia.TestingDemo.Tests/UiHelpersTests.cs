using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
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

        // BehaviorsDemo's CollectionPage does this: a ListBox scrolls its first item into view on the first RunJobs
        // after Show, which used to carry the target off screen between BringIntoView and the click.
        [AvaloniaFact]
        public void Click_reaches_a_control_when_the_page_scrolls_itself_after_show()
        {
            var clicks = 0;
            var top = new Button { Content = "top", Height = 40 };
            top.Click += (_, _) => clicks++;
            var bottom = new Border { Height = 40 };
            var panel = new StackPanel { Children = { top, new Border { Height = 2000 }, bottom } };
            var window = UiHelpers.Show(new ScrollViewer { Content = panel });
            Dispatcher.UIThread.Post(() => bottom.BringIntoView());

            window.Click(top);

            Assert.Equal(1, clicks);
        }

        [AvaloniaFact]
        public async Task WaitUntil_returns_once_background_work_lands_on_the_ui_thread()
        {
            var done = false;
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                Dispatcher.UIThread.Post(() => done = true);
            });

            Assert.False(done);
            await UiHelpers.WaitUntil(() => done);
            Assert.True(done);
        }

        [AvaloniaFact]
        public async Task WaitUntil_throws_when_the_condition_never_holds()
        {
            await Assert.ThrowsAsync<TimeoutException>(() => UiHelpers.WaitUntil(() => false, 100));
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

        // Self-drawn controls that forget AffectsRender change a property without changing the picture, so the only
        // way to see it is to compare pixels.
        [AvaloniaFact]
        public void CaptureFrame_is_stable_for_the_same_scene_and_changes_when_the_scene_changes()
        {
            var border = new Border { Background = Brushes.Red };
            var window = UiHelpers.Show(border);

            var first = window.CaptureFrame();
            var again = window.CaptureFrame();
            border.Background = Brushes.Blue;
            var changed = window.CaptureFrame();

            Assert.Equal(first, again);
            Assert.NotEqual(first, changed);
        }
    }
}
