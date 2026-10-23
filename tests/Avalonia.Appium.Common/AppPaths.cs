using System;
using System.IO;
using System.Linq;

namespace Avalonia.Appium.Common
{
    // Finds the built exe of a demo app: walks up to the folder holding hello-avalonia.slnx, then picks the newer of
    // src\<app>\bin or samples\<app>\bin, Debug and Release (Directory.Build.props drops the target framework folder).
    public static class AppPaths
    {
        // Null when no hello-avalonia.slnx sits above the test binaries. A throwing static initializer would surface as
        // a TypeInitializationException during test discovery; null lets FindExe report "not built" and the test skip.
        public static string? RepoRoot { get; } = FindRepoRoot();

        public static string? FindExe(string app)
        {
            if (RepoRoot is null)
                return null;
            return new[] { "src", "samples" }
                .SelectMany(root => new[] { "Debug", "Release" }.Select(config => Path.Combine(RepoRoot, root, app, "bin", config, app + ".exe")))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static string? FindRepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "hello-avalonia.slnx")))
                    return dir.FullName;
            }
            return null;
        }
    }
}
