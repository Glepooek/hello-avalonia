using Avalonia.Controls;
using Avalonia.EventsDemo.Controls;
using Avalonia.EventsDemo.Views.Pages;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System.Collections;
using System.Linq;
using Xunit;

namespace Avalonia.EventsDemo.Tests
{
    public class InteractionTests
    {
        // Each page keeps its EventLog private; the ListBox next to it shows the same entries.
        // Entries look like "03  Inner:tunnel", so drop the two-digit sequence number and the gap.
        private static string[] Messages(Control page)
            => ((IEnumerable)page.ByName<ListBox>("LogList").ItemsSource!)
                .Cast<string>()
                .Select(line => line.Substring(4))
                .ToArray();

        [AvaloniaFact]
        public void Adding_the_probed_button_logs_the_attach_events_in_order()
        {
            var page = new LifecyclePage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Button>("AddButton"));
            // SizeChanged and Loaded are dispatched, not raised synchronously.
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[] { "AttachedToLogicalTree", "AttachedToVisualTree", "Initialized", "SizeChanged", "Loaded" },
                Messages(page));
            Assert.False(page.ByName<Button>("AddButton").IsEnabled);
            Assert.True(page.ByName<Button>("RemoveButton").IsEnabled);
        }

        [AvaloniaFact]
        public void Removing_the_probed_button_logs_the_detach_events_after_the_attach_ones()
        {
            var page = new LifecyclePage();
            var window = UiHelpers.Show(page);
            window.Click(page.ByName<Button>("AddButton"));
            Dispatcher.UIThread.RunJobs();

            window.Click(page.ByName<Button>("RemoveButton"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[] { "DetachedFromLogicalTree", "DetachedFromVisualTree", "Unloaded" },
                Messages(page).Skip(5));
            Assert.True(page.ByName<Button>("AddButton").IsEnabled);
            Assert.False(page.ByName<Button>("RemoveButton").IsEnabled);
        }

        [AvaloniaFact]
        public void Pointer_press_tunnels_through_the_three_layers_and_bubbles_back_out()
        {
            var page = new RoutingPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Border>("Inner"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[] { "Outer:tunnel", "Middle:tunnel", "Inner:tunnel", "Inner:bubble", "Middle:bubble", "Outer:bubble" },
                Messages(page));
        }

        [AvaloniaFact]
        public void Key_press_reaches_every_layer_while_Handled_is_off()
        {
            var page = new HandledPage();
            var window = UiHelpers.Show(page);
            page.ByName<TextBox>("Entry").Focus();

            window.KeyPress(Key.A, RawInputModifiers.None, PhysicalKey.A, "a");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                new[]
                {
                    "outer:tunnel", "middle:tunnel", "inner:bubble  (Handled was False)",
                    "middle:bubble", "outer:bubble", "outer (handledEventsToo)  Handled=False",
                },
                Messages(page));
        }

        [AvaloniaFact]
        public void Handled_stops_the_bubble_but_a_handledEventsToo_handler_still_runs()
        {
            var page = new HandledPage();
            var window = UiHelpers.Show(page);
            page.ByName<CheckBox>("HandleBox").IsChecked = true;
            page.ByName<TextBox>("Entry").Focus();

            window.KeyPress(Key.B, RawInputModifiers.None, PhysicalKey.B, "b");
            Dispatcher.UIThread.RunJobs();

            // No middle:bubble and no outer:bubble: the inner handler cut the route short.
            Assert.Equal(
                new[]
                {
                    "outer:tunnel", "middle:tunnel", "inner:bubble  (Handled was False)",
                    "outer (handledEventsToo)  Handled=True",
                },
                Messages(page));
        }

        [AvaloniaFact]
        public void Custom_routed_event_bubbles_from_its_source_up_to_the_listener()
        {
            var page = new CustomRoutedEventPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Notifier>("Alpha"));
            window.Click(page.ByName<Notifier>("Beta"));
            Dispatcher.UIThread.RunJobs();

            // Only Alpha has the CLR-wrapper handler; both reach the attached listener on the StackPanel.
            Assert.Equal(
                new[]
                {
                    "Alpha.Ping +=  (direct on the control)  Message='ping from Alpha'",
                    "Listener (attached)  Message='ping from Alpha'  Source=Alpha",
                    "Listener (attached)  Message='ping from Beta'  Source=Beta",
                },
                Messages(page));
        }

        [AvaloniaFact]
        public void Notifier_raises_Ping_with_its_own_name_and_itself_as_the_source()
        {
            var notifier = new Notifier { Name = "Solo" };
            PingEventArgs? received = null;
            notifier.Ping += (_, e) => received = e;
            var window = UiHelpers.Show(notifier);

            window.Click(notifier);

            Assert.NotNull(received);
            Assert.Equal("ping from Solo", received.Message);
            Assert.Same(notifier, received.Source);
        }

        [AvaloniaFact]
        public void Input_events_page_logs_the_pointer_the_key_and_the_text()
        {
            var page = new InputEventsPage();
            var window = UiHelpers.Show(page);

            window.Click(page.ByName<Border>("Pad"));
            window.KeyPress(Key.Q, RawInputModifiers.None, PhysicalKey.Q, "q");
            window.Type(page.ByName<TextBox>("Entry"), "z");
            Dispatcher.UIThread.RunJobs();

            var lines = Messages(page);
            Assert.Equal("PointerEntered", lines[0]);
            // The position depends on the window layout, so only the prefix is pinned.
            Assert.StartsWith("PointerPressed  Mouse  left=True  right=False", lines[1]);
            Assert.Equal(
                new[] { "PointerReleased", "KeyDown  key=Q  modifiers=None", "TextInput  text='z'" },
                lines.Skip(2));
        }
    }
}
