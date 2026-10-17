using System;
using System.Runtime.InteropServices;

namespace Avalonia.Appium.Common
{
    // The Win32 SendInput call that UnicodeKeyboard and KeyChord share. Input goes to whatever window has keyboard
    // focus, which during a test is the app WinAppDriver launched and clicked.
    internal static class NativeInput
    {
        public const uint KeyEventKeyUp = 0x0002;
        public const uint KeyEventUnicode = 0x0004;

        public static Input Key(ushort virtualKey, ushort scan, uint flags) => new()
        {
            Type = 1, // INPUT_KEYBOARD
            Union = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = virtualKey, Scan = scan, Flags = flags } },
        };

        public static void Send(Input[] inputs)
        {
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
                throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Length} events (error {Marshal.GetLastWin32Error()}).");
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            public uint Type;
            public InputUnion Union;
        }

        // Sized by MouseInput, the largest member, so Marshal.SizeOf<Input>() matches what SendInput expects.
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput Mouse;
            [FieldOffset(0)] public KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }
    }
}
