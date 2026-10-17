using System.Linq;

namespace Avalonia.Appium.Common
{
    // The keys the tests press. Values are Win32 virtual-key codes.
    public enum VirtualKey : ushort
    {
        Tab = 0x09,
        Shift = 0x10,
        Control = 0x11,
        Escape = 0x1B,
        Right = 0x27,
        S = 0x53,
    }

    // Presses a key or chord as virtual keys through SendInput: the keys go down in order and come up in reverse.
    // In batch 10 WinAppDriver's Keyboard.SendKeys pressed Tab and Ctrl+S, but its Right arrow left no KeyDown in
    // the event log, while SendInput delivered it. Avoid Ctrl+Shift chords: Windows also uses Ctrl+Shift to switch
    // between installed input methods, and a chord with it worked in one run and not the next.
    public static class KeyChord
    {
        public static void Press(params VirtualKey[] keys)
        {
            var down = keys.Select(k => NativeInput.Key((ushort)k, 0, 0));
            var up = keys.Reverse().Select(k => NativeInput.Key((ushort)k, 0, NativeInput.KeyEventKeyUp));
            NativeInput.Send(down.Concat(up).ToArray());
        }
    }
}
