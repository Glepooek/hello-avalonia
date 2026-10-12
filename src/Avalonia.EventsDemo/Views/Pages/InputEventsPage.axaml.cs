using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Shared.Helpers;

namespace Avalonia.EventsDemo.Views.Pages
{
    public partial class InputEventsPage : UserControl
    {
        private readonly EventLog _log = new();

        public InputEventsPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // TextBox marks TextInput as handled in its own class handler, so a plain `+=` would
            // never run. Tunnel runs before the TextBox sees the event.
            Entry.AddHandler(InputElement.TextInputEvent,
                (object? s, TextInputEventArgs e) => _log.Write($"TextInput  text='{e.Text}'"),
                RoutingStrategies.Tunnel);
        }

        private void OnEntered(object? sender, PointerEventArgs e) => _log.Write("PointerEntered");

        private void OnExited(object? sender, PointerEventArgs e) => _log.Write("PointerExited");

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(Pad);
            _log.Write($"PointerPressed  {e.Pointer.Type}  left={point.Properties.IsLeftButtonPressed}  right={point.Properties.IsRightButtonPressed}  at {point.Position:F0}");

            // Without this the Border never gets keyboard focus from a click.
            Pad.Focus();
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e) => _log.Write("PointerReleased");

        private void OnWheel(object? sender, PointerWheelEventArgs e) => _log.Write($"PointerWheelChanged  delta={e.Delta}");

        private void OnKeyDown(object? sender, KeyEventArgs e) => _log.Write($"KeyDown  key={e.Key}  modifiers={e.KeyModifiers}");

        private void OnKeyUp(object? sender, KeyEventArgs e) => _log.Write($"KeyUp  key={e.Key}");

        private void OnClear(object? sender, RoutedEventArgs e) => _log.Clear();
    }
}
