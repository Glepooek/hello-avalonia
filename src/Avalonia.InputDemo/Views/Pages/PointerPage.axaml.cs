using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class PointerPage : UserControl
    {
        private const double HandleHalf = 20;

        public PointerPage()
        {
            InitializeComponent();
            Canvas.SetLeft(Handle, 0);
        }

        // Reading the pointer: type, which buttons are down, and where.
        private void OnPadMoved(object? sender, PointerEventArgs e) => Show(e, "Moved");

        private void OnPadPressed(object? sender, PointerPressedEventArgs e) => Show(e, "Pressed");

        private void OnPadWheel(object? sender, PointerWheelEventArgs e)
            => Readout.Text = $"Wheel  delta={e.Delta}";

        private void Show(PointerEventArgs e, string what)
        {
            var point = e.GetCurrentPoint(Pad);
            var p = point.Properties;
            Readout.Text = $"{what,-8} type={e.Pointer.Type}  pos={point.Position:F0}  L={p.IsLeftButtonPressed} M={p.IsMiddleButtonPressed} R={p.IsRightButtonPressed}  mods={e.KeyModifiers}";
        }

        private void OnHandlePressed(object? sender, PointerPressedEventArgs e)
        {
            // From now on every pointer event for this pointer goes to Handle,
            // wherever the pointer actually is.
            e.Pointer.Capture(Handle);
            DragState.Text = "拖动中（已捕获）";
        }

        private void OnHandleMoved(object? sender, PointerEventArgs e)
        {
            if (e.Pointer.Captured != Handle)
            {
                return;
            }

            // Position is relative to Track and may be far outside it: that is the point of capturing.
            var x = e.GetPosition(Track).X - HandleHalf;
            var max = Track.Width - Handle.Width;
            Canvas.SetLeft(Handle, Math.Clamp(x, 0, max));
        }

        private void OnHandleReleased(object? sender, PointerReleasedEventArgs e) => e.Pointer.Capture(null);

        private void OnHandleCaptureLost(object? sender, PointerCaptureLostEventArgs e)
            => DragState.Text = "空闲（捕获已释放）";
    }
}
