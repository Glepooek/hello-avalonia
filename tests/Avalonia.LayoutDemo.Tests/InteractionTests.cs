using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LayoutDemo.ViewModels;
using Avalonia.LayoutDemo.Views.Pages;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.LayoutDemo.Tests
{
    public class InteractionTests
    {
        // Both bindings go through a converter into a Thickness. A binding to a struct property fails
        // silently, so this has to be asserted on the live control rather than trusted from the build.
        [AvaloniaFact]
        public void Positioning_sliders_drive_the_margin_and_padding_of_the_sample_borders()
        {
            var window = UiHelpers.Show(new PositioningPage());
            var marginBorder = (Border)window.GetVisualDescendants().OfType<TextBlock>()
                .First(t => t.Text == "我有 Margin").Parent!;
            var paddingBorder = (Border)window.GetVisualDescendants().OfType<TextBlock>()
                .First(t => t.Text == "我有 Padding").Parent!;

            window.ByName<Slider>("MarginSlider").Value = 20;
            window.ByName<Slider>("PaddingSlider").Value = 30;

            Assert.Equal(new Thickness(20), marginBorder.Margin);
            Assert.Equal(new Thickness(30), paddingBorder.Padding);
        }

        [AvaloniaFact]
        public void Responsive_page_breakpoint_follows_the_window_width()
        {
            var page = new ResponsivePage();
            var window = UiHelpers.Show(page);
            var vm = (ResponsiveViewModel)page.DataContext!;

            // SizeChanged reaches the view model through the dispatcher, so drain it after every resize.
            // Asserting CurrentWidth too keeps the first step from passing on the initial 600 px layout.
            window.Width = 800;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(800, vm.CurrentWidth);
            Assert.True(vm.IsWide);

            window.Width = 400;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(400, vm.CurrentWidth);
            Assert.True(vm.IsCompact);
        }

        // Columns is deliberately not set on the element: a local value would suppress the container queries.
        [AvaloniaFact]
        public void Responsive_page_container_query_changes_the_tile_columns_with_the_width()
        {
            var window = UiHelpers.Show(new ResponsivePage());
            var grid = window.ByName<UniformGrid>("TileGrid");

            window.Width = 300;
            window.UpdateLayout();
            Assert.Equal(1, grid.Columns);

            window.Width = 600;
            window.UpdateLayout();
            Assert.Equal(2, grid.Columns);

            window.Width = 900;
            window.UpdateLayout();
            Assert.Equal(4, grid.Columns);
        }
    }
}
