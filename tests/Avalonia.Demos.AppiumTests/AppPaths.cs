using System;
using System.IO;
using System.Linq;

namespace Avalonia.Demos.AppiumTests
{
    // Finds the built exe of a demo app: walks up to the folder holding hello-avalonia.slnx, then picks the newer of
    // src\<app>\bin\Debug and bin\Release (Directory.Build.props drops the target framework folder).
    public static class AppPaths
    {
        public static string RepoRoot { get; } = FindRepoRoot();

        public static string? FindExe(string app)
        {
            return new[] { "Debug", "Release" }
                .Select(config => Path.Combine(RepoRoot, "src", app, "bin", config, app + ".exe"))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static string FindRepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "hello-avalonia.slnx")))
                    return dir.FullName;
            }
            throw new InvalidOperationException($"No hello-avalonia.slnx above {AppContext.BaseDirectory}.");
        }
    }
}
