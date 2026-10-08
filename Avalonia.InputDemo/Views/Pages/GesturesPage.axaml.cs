using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class GesturesPage : UserControl
    {
        private int _tapped, _double, _right, _holdStarted, _holdCompleted, _holdCanceled;
        private int _pinch, _scroll, _swipe, _magnify, _rotate, _padSwipe;

        public GesturesPage()
        {
            InitializeComponent();

            // Mouse does not hold by default; opt this pad in. (Not available as a XAML attribute on Border.)
            Pad.SetValue(InputElement.IsHoldWithMouseEnabledProperty, true);

            // The touch gestures are static routed events with no CLR event, so use AddHandler.
            Pad.AddHandler(InputElement.PinchEvent, (object? s, PinchEventArgs e) => { _pinch++; ShowTouch(); });
            Pad.AddHandler(InputElement.ScrollGestureEvent, (object? s, ScrollGestureEventArgs e) => { _scroll++; ShowTouch(); });
            Pad.AddHandler(InputElement.SwipeGestureEvent, (object? s, SwipeGestureEventArgs e) => { _swipe++; ShowTouch(); });
            Pad.AddHandler(InputElement.PointerTouchPadGestureMagnifyEvent, (object? s, PointerDeltaEventArgs e) => { _magnify++; ShowTouch(); });
            Pad.AddHandler(InputElement.PointerTouchPadGestureRotateEvent, (object? s, PointerDeltaEventArgs e) => { _rotate++; ShowTouch(); });
            Pad.AddHandler(InputElement.PointerTouchPadGestureSwipeEvent, (object? s, PointerDeltaEventArgs e) => { _padSwipe++; ShowTouch(); });

            Show();
            ShowTouch();
        }

        private void OnTapped(object? sender, TappedEventArgs e) { _tapped++; Show(); }

        private void OnDoubleTapped(object? sender, TappedEventArgs e) { _double++; Show(); }

        private void OnRightTapped(object? sender, TappedEventArgs e) { _right++; Show(); }

        private void OnHolding(object? sender, HoldingRoutedEventArgs e)
        {
            switch (e.HoldingState)
            {
                case HoldingState.Started: _holdStarted++; break;
                case HoldingState.Completed: _holdCompleted++; break;
                case HoldingState.Canceled: _holdCanceled++; break;
            }

            Show();
        }

        private void OnReset(object? sender, RoutedEventArgs e)
        {
            _tapped = _double = _right = _holdStarted = _holdCompleted = _holdCanceled = 0;
            Show();
        }

        private void Show()
            => Counts.Text = $"Tapped={_tapped}  DoubleTapped={_double}  RightTapped={_right}\nHolding: Started={_holdStarted} Completed={_holdCompleted} Canceled={_holdCanceled}";

        private void ShowTouch()
            => TouchCounts.Text = $"Pinch={_pinch}  Scroll={_scroll}  Swipe={_swipe}\nTouchPad: Magnify={_magnify} Rotate={_rotate} Swipe={_padSwipe}";
    }
}
