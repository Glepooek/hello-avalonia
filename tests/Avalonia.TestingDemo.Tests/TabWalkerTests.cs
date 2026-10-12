using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestingDemo.Views;
using Avalonia.Tests.Common;
using System;
using System.Linq;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class TabWalkerTests
    {
        // Stands in for a demo page that breaks as soon as it is laid out.
        private sealed class BrokenControl : Control
        {
            protected override Size MeasureOverride(Size availableSize) => throw new InvalidOperationException("broken page");
        }

        [AvaloniaFact]
        public void Walk_visits_every_tab_of_the_main_window()
        {
            var window = new MainWindow();
            window.Show();

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "计数器", "表单", "列表" }, visits.Select(v => v.Header));
            Assert.Equal(new[] { 0, 1, 2 }, visits.Select(v => v.Index));
            Assert.All(visits, v => Assert.Null(v.Error));
        }

        [AvaloniaFact]
        public void Walk_reports_a_tab_that_throws_and_keeps_going()
        {
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Header = "ok", Content = new TextBlock { Text = "fine" } });
            tabs.Items.Add(new TabItem { Header = "bad", Content = new BrokenControl() });
            tabs.Items.Add(new TabItem { Header = "after", Content = new TextBlock { Text = "still fine" } });
            var window = UiHelpers.Show(tabs);

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "ok", "bad", "after" }, visits.Select(v => v.Header));
            Assert.Null(visits[0].Error);
            Assert.IsType<InvalidOperationException>(visits[1].Error);
            Assert.Null(visits[2].Error);
        }

        [AvaloniaFact]
        public void Walk_throws_a_clear_error_when_there_is_no_tab_control()
        {
            var window = UiHelpers.Show(new TextBlock { Text = "no tabs here" });

            var ex = Assert.Throws<InvalidOperationException>(() => TabWalker.Walk(window));

            Assert.Contains("TabControl", ex.Message);
        }

        [AvaloniaFact]
        public void Walk_names_a_tab_without_a_header_by_its_position()
        {
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Content = new TextBlock { Text = "no header" } });
            var window = UiHelpers.Show(tabs);

            var visits = TabWalker.Walk(window);

            Assert.Equal(new[] { "#0" }, visits.Select(v => v.Header));
        }
    }
}
