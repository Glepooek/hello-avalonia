using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.GraphicsDemo.Controls;
using Avalonia.GraphicsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using System;
using Xunit;

namespace Avalonia.GraphicsDemo.Tests
{
    public class InteractionTests
    {
        private static string Text(Control page, string name) => page.ByName<TextBlock>(name).Text!;

        private static (Sketch Sketch, Window Window) ShowSketch()
        {
            var sketch = new Sketch();
            var window = new Window
            {
                Content = new Border { Background = Brushes.Black, Child = sketch },
                Width = 300,
                Height = 200,
            };
            window.Show();
            return (sketch, window);
        }

        // Sketch declares AffectsRender for its three switches. Without it the property changes and the picture does not,
        // with no exception and no log, so the only way to see it is to compare rendered frames.
        [AvaloniaTheory]
        [InlineData("UseClip")]
        [InlineData("UseTransform")]
        [InlineData("UseOpacity")]
        public void Each_sketch_switch_repaints_and_switching_it_off_restores_the_picture(string toggle)
        {
            var (sketch, window) = ShowSketch();
            var property = toggle switch
            {
                "UseClip" => Sketch.UseClipProperty,
                "UseTransform" => Sketch.UseTransformProperty,
                _ => Sketch.UseOpacityProperty,
            };
            var plain = window.CaptureFrame();

            sketch.SetValue(property, true);
            var scoped = window.CaptureFrame();
            sketch.SetValue(property, false);
            var restored = window.CaptureFrame();

            Assert.NotEqual(plain, scoped);
            Assert.Equal(plain, restored);
        }

        [AvaloniaFact]
        public void Easing_page_starts_on_CubicEaseInOut_and_samples_61_curve_points()
        {
            using var culture = CultureScope.Invariant();
            var page = new EasingPage();
            UiHelpers.Show(page);

            Assert.Equal("Ease(0.25)=0.062   Ease(0.5)=0.500   Ease(0.75)=0.938", Text(page, "Samples"));
            Assert.Equal(61, page.ByName<Polyline>("Curve").Points.Count);
        }

        [AvaloniaFact]
        public void Choosing_another_easing_resamples_the_curve()
        {
            using var culture = CultureScope.Invariant();
            var page = new EasingPage();
            UiHelpers.Show(page);
            var box = page.ByName<ComboBox>("EasingBox");

            // Index 0 is LinearEasing; index 6 is BackEaseInOut, which overshoots on both sides.
            box.SelectedIndex = 0;
            Assert.Equal("Ease(0.25)=0.250   Ease(0.5)=0.500   Ease(0.75)=0.750", Text(page, "Samples"));
            box.SelectedIndex = 6;
            Assert.Equal("Ease(0.25)=-0.188   Ease(0.5)=0.500   Ease(0.75)=1.188", Text(page, "Samples"));
        }

        [AvaloniaFact]
        public void Choosing_a_combine_mode_changes_the_combined_geometry()
        {
            var page = new ShapesPage();
            UiHelpers.Show(page);
            var combined = (CombinedGeometry)page.ByName<Path>("CombinedPath").Data!;
            Assert.Equal(GeometryCombineMode.Xor, combined.GeometryCombineMode);
            var xorBounds = combined.Bounds;

            page.ByName<ComboBox>("ModeBox").SelectedItem = GeometryCombineMode.Intersect;

            Assert.Equal(GeometryCombineMode.Intersect, combined.GeometryCombineMode);
            Assert.NotEqual(xorBounds, combined.Bounds);
        }

        [AvaloniaFact]
        public void Layout_transform_pushes_the_neighbour_away_but_render_transform_does_not()
        {
            var page = new TransformsPage();
            var window = UiHelpers.Show(page);
            var renderBefore = page.ByName<Border>("RenderRight").Bounds.X;
            var layoutBefore = page.ByName<Border>("LayoutRight").Bounds.X;

            page.ByName<Slider>("AngleSlider").Value = 90;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            // RenderTransform is applied after layout, so its neighbour stays put. Rotated by 90 degrees, the layout-
            // transformed 110-wide tile occupies 44 instead, which pulls its neighbour 66 to the left.
            Assert.Equal(renderBefore, page.ByName<Border>("RenderRight").Bounds.X);
            Assert.Equal(layoutBefore - 66, page.ByName<Border>("LayoutRight").Bounds.X, 0.5);
        }

        [AvaloniaFact]
        public void Only_the_transparent_probe_receives_taps_among_the_three_hit_test_cases()
        {
            var page = new ClipAndHitPage();
            var window = UiHelpers.Show(page);
            Assert.Equal("没有 Background=0   Transparent=0   IsHitTestVisible=False=0", Text(page, "HitReadout"));

            window.Click(page.ByName<Border>("NoBrush"));
            window.Click(page.ByName<Border>("Clear"));
            window.Click(page.ByName<Border>("Invisible"));

            // No Background means no hit; Transparent is a real brush and does hit; IsHitTestVisible=False never does.
            Assert.Equal("没有 Background=0   Transparent=1   IsHitTestVisible=False=0", Text(page, "HitReadout"));
        }

        [AvaloniaFact]
        public void Composition_sliders_write_through_to_the_element_visual()
        {
            var page = new CompositionPage();
            UiHelpers.Show(page);
            // Loaded is dispatched, and only then does the Target have a composition visual.
            Dispatcher.UIThread.RunJobs();
            Assert.StartsWith("已取得", Text(page, "Status"));
            var visual = ElementComposition.GetElementVisual(page.ByName<Border>("Target"))!;

            page.ByName<Slider>("OpacitySlider").Value = 0.5;
            page.ByName<Slider>("ScaleSlider").Value = 1.5;
            page.ByName<Slider>("AngleSlider").Value = 90;

            Assert.Equal(0.5f, visual.Opacity);
            Assert.Equal(1.5, visual.Scale.X);
            Assert.Equal((float)(Math.PI / 2), visual.RotationAngle, 0.001f);
        }

        [AvaloniaFact]
        public void Render_option_boxes_write_the_attached_render_options()
        {
            var page = new RenderOptionsPage();
            UiHelpers.Show(page);
            var pixels = page.ByName<Image>("Pixels");
            var top = page.ByName<Image>("BlendTop");
            Assert.Equal(BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode(pixels));

            page.ByName<ComboBox>("InterpolationBox").SelectedItem = BitmapInterpolationMode.HighQuality;
            page.ByName<ComboBox>("BlendBox").SelectedItem = BitmapBlendingMode.Plus;

            Assert.Equal(BitmapInterpolationMode.HighQuality, RenderOptions.GetBitmapInterpolationMode(pixels));
            Assert.Equal(BitmapBlendingMode.Plus, RenderOptions.GetBitmapBlendingMode(top));
        }

        [AvaloniaFact]
        public void Next_replaces_the_pager_content_and_the_kind_box_swaps_the_transition()
        {
            var page = new PageTransitionsPage();
            var window = UiHelpers.Show(page);
            var pager = page.ByName<TransitioningContentControl>("Pager");
            string PageText() => ((TextBlock)((Border)pager.Content!).Child!).Text!;
            Assert.Equal("第 1 页", PageText());
            Assert.IsType<PageSlide>(pager.PageTransition);

            window.Click(page.ButtonWithText("下一页"));

            Assert.Equal("第 2 页", PageText());
            Assert.Equal("第 2 页", Text(page, "Readout"));

            // Index 2 is the cross-fade.
            page.ByName<ComboBox>("KindBox").SelectedIndex = 2;
            Assert.IsType<CrossFade>(pager.PageTransition);
        }
    }
}
