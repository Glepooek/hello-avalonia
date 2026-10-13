using Avalonia.Controls;
using Avalonia.CustomControlsDemo.Controls;
using Avalonia.CustomControlsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.CustomControlsDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Control page, string name) => page.ByName<TextBlock>(name).Text!;

        private static Border FillOf(Meter meter)
            => meter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Fill");

        private static double ScaleOf(Meter meter) => ((ScaleTransform)FillOf(meter).RenderTransform!).ScaleX;

        private static (RadialPanel Panel, Window Window) ShowRadial(int children, double radius)
        {
            var panel = new RadialPanel { Radius = radius };
            for (var i = 0; i < children; i++)
            {
                panel.Children.Add(new Border { Width = 20, Height = 20 });
            }

            var window = new Window { Content = panel, Width = 400, Height = 400 };
            window.Show();
            return (panel, window);
        }

        // The swatches live on the window's overlay layer, and only while the flyout is open.
        private static List<Button> Swatches(Window window)
            => window.GetVisualDescendants().OfType<Button>().Where(b => b.Width == 32).ToList();

        [AvaloniaFact]
        public void Meter_follows_the_slider_and_its_template_part_scales_with_it()
        {
            using var culture = CultureScope.Invariant();
            var page = new TemplatedControlPage();
            UiHelpers.Show(page);
            var meter = page.ByName<Meter>("TheMeter");
            var slider = page.ByName<Slider>("ValueSlider");
            Assert.Equal(40, meter.Value);
            // The invariant culture writes P0 with a space ("40 %"); en-US and zh-CN would give "40%".
            Assert.Equal("40 %", meter.Display);
            Assert.Equal(0.4, ScaleOf(meter), 3);
            Assert.DoesNotContain(":full", meter.Classes);

            slider.Value = 100;
            Assert.Equal("100 %", meter.Display);
            Assert.Equal(1, ScaleOf(meter), 3);
            Assert.Contains(":full", meter.Classes);

            // Back down: the :full pseudo-class has to clear again.
            slider.Value = 40;
            Assert.Equal("40 %", meter.Display);
            Assert.DoesNotContain(":full", meter.Classes);
        }

        [AvaloniaFact]
        public void Meter_value_is_clamped_to_zero_and_the_maximum_and_follows_maximum_changes()
        {
            using var culture = CultureScope.Invariant();
            var page = new TemplatedControlPage();
            UiHelpers.Show(page);
            var meter = page.ByName<Meter>("TheMeter");

            meter.Value = 500;
            Assert.Equal(100, meter.Value);
            meter.Value = -5;
            Assert.Equal(0, meter.Value);

            meter.Maximum = 50;
            meter.Value = 25;
            Assert.Equal("50 %", meter.Display);
            Assert.DoesNotContain(":full", meter.Classes);
            meter.Value = 50;
            Assert.Contains(":full", meter.Classes);

            // A maximum of zero must not divide by zero.
            meter.Maximum = 0;
            Assert.Equal("0 %", meter.Display);
        }

        [AvaloniaFact]
        public void Swapping_to_the_text_theme_drops_the_fill_part_and_the_display_keeps_working()
        {
            using var culture = CultureScope.Invariant();
            var page = new TemplatedControlPage();
            var window = UiHelpers.Show(page);
            var meter = page.ByName<Meter>("TheMeter");
            Assert.Null(meter.Theme);
            Assert.NotNull(FillOf(meter));

            window.Click(page.ByName<Button>("SwapButton"));
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(meter.Theme);
            Assert.DoesNotContain(meter.GetVisualDescendants().OfType<Border>(), b => b.Name == "PART_Fill");
            // The part is gone, so the guard in Refresh has to cope with it.
            meter.Value = 70;
            Assert.Equal("70 %", meter.Display);
        }

        [AvaloniaFact]
        public void Ring_gauge_measures_itself_and_follows_the_slider()
        {
            var page = new CustomDrawnPage();
            UiHelpers.Show(page);
            var gauge = page.ByName<RingGauge>("Gauge");
            Assert.Equal(new Size(120, 120), gauge.Bounds.Size);
            Assert.Equal(35, gauge.Value);

            page.ByName<Slider>("GaugeSlider").Value = 90;

            Assert.Equal(90, gauge.Value);
        }

        // RingGauge declares AffectsRender for Value; without it the property changes and the ring does not. The gauge
        // stands alone here: on the demo page a readout next to it changes too, which would repaint the frame anyway.
        [AvaloniaFact]
        public void Ring_gauge_repaints_when_the_value_changes()
        {
            var gauge = new RingGauge { Value = 35 };
            var window = new Window
            {
                Content = new Border { Background = Brushes.Black, Child = gauge },
                Width = 200,
                Height = 200,
            };
            window.Show();
            var before = window.CaptureFrame();

            gauge.Value = 90;

            Assert.NotEqual(before, window.CaptureFrame());
        }

        [AvaloniaFact]
        public void Radial_panel_places_children_evenly_clockwise_from_twelve_o_clock()
        {
            var (panel, _) = ShowRadial(children: 4, radius: 100);

            // The 400x400 panel is centred on (200, 200); each 20x20 child sits on a circle of radius 100 around it.
            Assert.Equal(
                new[] { new Rect(190, 90, 20, 20), new Rect(290, 190, 20, 20), new Rect(190, 290, 20, 20), new Rect(90, 190, 20, 20) },
                panel.Children.Select(c => c.Bounds));
        }

        [AvaloniaFact]
        public void Radial_panel_start_angle_rotates_every_child_to_the_next_position()
        {
            var (panel, window) = ShowRadial(children: 4, radius: 100);

            panel.StartAngle = 90;
            window.UpdateLayout();

            Assert.Equal(
                new[] { new Rect(290, 190, 20, 20), new Rect(190, 290, 20, 20), new Rect(90, 190, 20, 20), new Rect(190, 90, 20, 20) },
                panel.Children.Select(c => c.Bounds));
        }

        [AvaloniaFact]
        public void Radial_panel_size_comes_from_the_radius_and_the_largest_child()
        {
            var (panel, window) = ShowRadial(children: 4, radius: 100);
            Assert.Equal(new Size(220, 220), panel.DesiredSize);

            panel.Radius = 40;
            window.UpdateLayout();

            Assert.Equal(new Size(100, 100), panel.DesiredSize);
        }

        [AvaloniaFact]
        public void Radial_panel_without_children_measures_to_the_bare_circle()
        {
            var (panel, _) = ShowRadial(children: 0, radius: 100);

            Assert.Equal(new Size(200, 200), panel.DesiredSize);
        }

        [AvaloniaFact]
        public void Add_and_remove_buttons_change_the_dot_count_and_it_never_goes_below_zero()
        {
            var page = new CustomPanelPage();
            var window = UiHelpers.Show(page);
            // The control row is wider than the 600px test window and is not inside a ScrollViewer, so a click on
            // the right-hand button would miss silently.
            window.Width = 1000;
            window.UpdateLayout();
            var radial = page.ByName<RadialPanel>("Radial");
            Assert.Equal(6, radial.Children.Count);

            window.Click(page.ByName<Button>("AddButton"));
            Assert.Equal(7, radial.Children.Count);

            window.Click(page.ByName<Button>("RemoveButton"));
            window.Click(page.ByName<Button>("RemoveButton"));
            Assert.Equal(5, radial.Children.Count);

            for (var i = 0; i < 10; i++)
            {
                window.Click(page.ByName<Button>("RemoveButton"));
            }

            Assert.Empty(radial.Children);
            window.Click(page.ByName<Button>("AddButton"));
            Assert.Single(radial.Children);
        }

        [AvaloniaFact]
        public void Radius_and_angle_sliders_drive_the_panel()
        {
            var page = new CustomPanelPage();
            UiHelpers.Show(page);
            var radial = page.ByName<RadialPanel>("Radial");
            Assert.Equal(80, radial.Radius);
            Assert.Equal(0, radial.StartAngle);

            page.ByName<Slider>("RadiusSlider").Value = 40;
            page.ByName<Slider>("AngleSlider").Value = 90;

            Assert.Equal(40, radial.Radius);
            Assert.Equal(90, radial.StartAngle);
        }

        [AvaloniaFact]
        public void Picking_a_swatch_fills_the_chosen_box_and_closes_the_flyout()
        {
            var page = new CustomFlyoutPage();
            var window = UiHelpers.Show(page);
            var pick = page.ByName<Button>("PickButton");
            var flyout = (SwatchFlyout)pick.Flyout!;
            Assert.Empty(Swatches(window));
            Assert.Equal("尚未选择", Text(page, "ChosenText"));

            window.Click(pick);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);
            var swatches = Swatches(window);
            Assert.Equal(6, swatches.Count);

            window.Click(swatches[2]);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("#ffe8d54a", Text(page, "ChosenText"));
            Assert.Equal(Color.Parse("#E8D54A"), ((SolidColorBrush)page.ByName<Border>("Chosen").Background!).Color);
            Assert.False(flyout.IsOpen);
        }

        [AvaloniaFact]
        public void Clicking_elsewhere_dismisses_the_open_flyout_without_choosing_a_colour()
        {
            var page = new CustomFlyoutPage();
            var window = UiHelpers.Show(page);
            var pick = page.ByName<Button>("PickButton");
            var flyout = (SwatchFlyout)pick.Flyout!;
            window.Click(pick);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);

            window.Click(page.ByName<TextBlock>("ChosenText"));
            Dispatcher.UIThread.RunJobs();

            Assert.False(flyout.IsOpen);
            Assert.Equal("尚未选择", Text(page, "ChosenText"));
        }

        [AvaloniaFact]
        public void A_visual_only_host_hides_the_data_context_from_its_child_but_a_dual_tree_host_does_not()
        {
            var visualOnly = new OnlyVisualHost();
            var both = new VisualAndLogicalHost();
            var window = new Window
            {
                Content = new StackPanel { DataContext = "ctx", Children = { visualOnly, both } },
                Width = 300,
                Height = 200,
            };
            window.Show();

            Assert.Null(visualOnly.Child.DataContext);
            Assert.Equal("ctx", both.Child.DataContext);
            Assert.Equal("ctx", both.Child.Text);
        }

        [AvaloniaFact]
        public void Control_trees_page_reports_both_data_contexts_once_it_has_loaded()
        {
            var page = new ControlTreesPage();
            UiHelpers.Show(page);
            // OnLoaded runs from the dispatcher.
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                "视觉树宿主的子元素 DataContext = null；双树宿主的子元素 DataContext = 来自父级的 DataContext",
                Text(page, "Readout"));
        }

        [AvaloniaFact]
        public void Labeled_sliders_stay_in_sync_in_both_directions()
        {
            var page = new UserControlPage();
            UiHelpers.Show(page);
            var size = page.ByName<LabeledSlider>("Size");
            var fade = page.ByName<LabeledSlider>("Fade");
            Assert.Equal(20, size.Value);
            Assert.Equal(20, fade.Value);

            size.Value = 33;
            Assert.Equal(33, fade.Value);

            // The inner Slider writes back through the TwoWay Value property, and the binding carries it on to Size.
            fade.GetVisualDescendants().OfType<Slider>().First().Value = 12;
            Assert.Equal(12, fade.Value);
            Assert.Equal(12, size.Value);
        }
    }
}
