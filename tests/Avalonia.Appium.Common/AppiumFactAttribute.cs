using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Avalonia.Appium.Common
{
    // Appium drives the real mouse and keyboard, so a test only runs when it is switched on explicitly and can
    // actually work; otherwise it is skipped with the first reason that applies.
    public sealed class AppiumFactAttribute : FactAttribute
    {
        public const string SwitchVariable = "HELLO_AVALONIA_APPIUM";

        public AppiumFactAttribute(
            string app,
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            Skip = SkipReason(app);
        }

        private static string? SkipReason(string app)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Appium tests run on Windows only.";
            if (Environment.GetEnvironmentVariable(SwitchVariable) != "1")
                return $"Set {SwitchVariable}=1 to run Appium tests; they take over the mouse and keyboard.";
            if (!DriverListening())
                return $"WinAppDriver is not listening on {AppSession.DriverUri}.";
            if (AppPaths.FindExe(app) is null)
                return $"{app}.exe is not built; run dotnet build first.";
            return null;
        }

        // Only the TCP connection counts: WinAppDriver answers /status with "status 13" even when healthy.
        private static bool DriverListening()
        {
            try
            {
                using var client = new TcpClient();
                return client.ConnectAsync(AppSession.DriverUri.Host, AppSession.DriverUri.Port).Wait(1000)
                       && client.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
