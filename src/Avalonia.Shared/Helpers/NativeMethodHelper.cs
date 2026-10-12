using System;
using System.Runtime.InteropServices;

namespace Avalonia.Shared.Helpers
{
    public class NativeMethodHelper
    {
        /// <summary>
        /// 拖动窗体
        /// </summary>
        public static void MouseDownDrag(IntPtr hWnd)
        {
            ReleaseCapture();
            SendMessage(hWnd, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        #region Win32API

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        #endregion
    }
}
