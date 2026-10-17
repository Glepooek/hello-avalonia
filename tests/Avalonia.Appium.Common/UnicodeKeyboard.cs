namespace Avalonia.Appium.Common
{
    // Types text into whatever has keyboard focus as Unicode packets (KEYEVENTF_UNICODE). WinAppDriver's SendKeys
    // sends virtual keys, which a Chinese IME (Sogou or Microsoft Pinyin) turns into pinyin candidates: "abc3"
    // arrives as "安保处". Unicode packets reach the window as WM_CHAR and skip the IME's composition.
    public static class UnicodeKeyboard
    {
        public static void Type(string text)
        {
            var inputs = new NativeInput.Input[text.Length * 2];
            for (var i = 0; i < text.Length; i++)
            {
                inputs[2 * i] = NativeInput.Key(0, text[i], NativeInput.KeyEventUnicode);
                inputs[2 * i + 1] = NativeInput.Key(0, text[i], NativeInput.KeyEventUnicode | NativeInput.KeyEventKeyUp);
            }
            NativeInput.Send(inputs);
        }
    }
}
