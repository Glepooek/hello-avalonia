using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;

namespace Avalonia.GraphicsDemo.Views.Pages
{
    public partial class CompositionPage : UserControl
    {
        private CompositionVisual? _visual;

        public CompositionPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Only now is Target attached to the visual tree, so only now does it have a composition visual.
            _visual = ElementComposition.GetElementVisual(Target);
            if (_visual == null)
            {
                Status.Text = "取不到合成 Visual（此平台不支持）";
                return;
            }

            // Rotate and scale around the middle of the 80x80 box rather than its top-left corner.
            _visual.CenterPoint = new Vector3D(40, 40, 0);
            Status.Text = $"已取得 {_visual.GetType().Name}";
        }

        private void OnSliderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_visual == null)
            {
                return;
            }

            _visual.Opacity = (float)OpacitySlider.Value;
            _visual.Scale = new Vector3D(ScaleSlider.Value, ScaleSlider.Value, 1);
            _visual.RotationAngle = (float)(AngleSlider.Value * Math.PI / 180);
        }

        private void OnFade(object? sender, RoutedEventArgs e)
        {
            if (_visual == null)
            {
                return;
            }

            var animation = _visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.Target = "Opacity";
            animation.InsertKeyFrame(0f, 1f);
            animation.InsertKeyFrame(0.5f, 0.15f);
            animation.InsertKeyFrame(1f, 1f);
            animation.Duration = TimeSpan.FromSeconds(1);
            _visual.StartAnimation("Opacity", animation);
            Status.Text = "已启动 Opacity 关键帧动画（1 秒）";
        }

        private void OnSpin(object? sender, RoutedEventArgs e)
        {
            if (_visual == null)
            {
                return;
            }

            var animation = _visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.Target = "RotationAngle";
            animation.InsertKeyFrame(0f, 0f);
            animation.InsertKeyFrame(1f, (float)(Math.PI * 2));
            animation.Duration = TimeSpan.FromSeconds(1);
            _visual.StartAnimation("RotationAngle", animation);
            Status.Text = "已启动 RotationAngle 关键帧动画（1 秒）";
        }
    }
}
