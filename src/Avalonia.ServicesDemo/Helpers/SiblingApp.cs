using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Avalonia.ServicesDemo.Helpers
{
    // Builds the ProcessStartInfo for an app published into the same folder as this one. Every platform difference
    // in starting it lives here, so the page only calls Process.Start.
    public static class SiblingApp
    {
        // The apphost is Name.exe on Windows and an extensionless Name on Linux and macOS. Inside a macOS .app bundle
        // both apphosts sit together in Contents/MacOS, so the same path works there too.
        public static string ExecutablePath(string directory, string appName) =>
            Path.Combine(directory, OperatingSystem.IsWindows() ? appName + ".exe" : appName);

        public static ProcessStartInfo CreateStartInfo(string directory, string appName, IEnumerable<string> args)
        {
            var psi = new ProcessStartInfo(ExecutablePath(directory, appName));

            // ArgumentList quotes each item for the target platform; joining a string by hand breaks on spaces and quotes.
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);
            psi.WorkingDirectory = directory;
            psi.UseShellExecute = false;
            return psi;
        }
    }
}
