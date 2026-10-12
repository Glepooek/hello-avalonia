using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class KeyboardPage : UserControl
    {
        private int _saves;
        private int _bindFires;

        public KeyboardPage()
        {
            InitializeComponent();

            // Tunnel so the readout also updates for keys the TextBox itself consumes (arrows, Backspace).
            Entry.AddHandler(InputElement.KeyDownEvent, OnEntryKeyDown, RoutingStrategies.Tunnel);

            // A command bound to a gesture. Built in code because the page has no DataContext to bind a
            // command from; in an MVVM page this would be <KeyBinding Gesture="Ctrl+Shift+K" Command="{Binding ...}" />.
            KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.K, KeyModifiers.Control | KeyModifiers.Shift),
                Command = new RelayCommand(() =>
                {
                    _bindFires++;
                    BindReadout.Text = $"Ctrl+Shift+K 触发了 {_bindFires} 次";
                }),
            });
        }

        private void OnEntryKeyDown(object? sender, KeyEventArgs e)
            => KeyReadout.Text = $"Key={e.Key}  PhysicalKey={e.PhysicalKey}  Modifiers={e.KeyModifiers}  KeySymbol='{e.KeySymbol}'";

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            _saves++;
            SaveReadout.Text = $"保存了 {_saves} 次";
        }
    }
}
