using System;
using System.Runtime.InteropServices;

namespace Avalonia.Demos.AppiumTests
{
    // Types text into whatever has keyboard focus as Unicode packets (KEYEVENTF_UNICODE). WinAppDriver's SendKeys
    // sends virtual keys, which a Chinese IME (Sogou or Microsoft Pinyin) turns into pinyin candidates: "abc3"
    // arrives as "安保处". Unicode packets reach the window as WM_CHAR and skip the IME's composition.
    public static class UnicodeKeyboard
    {
        private const uint InputKeyboard = 1;
        private const uint KeyEventUnicode = 0x0004;
        private const uint KeyEventKeyUp = 0x0002;

        public static void Type(string text)
        {
            var inputs = new Input[text.Length * 2];
            for (var i = 0; i < text.Length; i++)
            {
                inputs[2 * i] = Key(text[i], KeyEventUnicode);
                inputs[2 * i + 1] = Key(text[i], KeyEventUnicode | KeyEventKeyUp);
            }
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
                throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Length} events (error {Marshal.GetLastWin32Error()}).");
        }

        private static Input Key(char c, uint flags) => new()
        {
            Type = InputKeyboard,
            Union = new InputUnion { Keyboard = new KeyboardInput { Scan = c, Flags = flags } },
        };

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public InputUnion Union;
        }

        // Sized by MouseInput, the largest member, so Marshal.SizeOf<Input>() matches what SendInput expects.
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MouseInput Mouse;
            [FieldOffset(0)] public KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }
    }
}
