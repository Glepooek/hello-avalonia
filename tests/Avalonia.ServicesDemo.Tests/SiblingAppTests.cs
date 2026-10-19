using Avalonia.ServicesDemo.Helpers;
using Avalonia.ServicesDemo.Views.Pages;
using System;
using System.IO;
using Xunit;

namespace Avalonia.ServicesDemo.Tests
{
    // SiblingApp only builds a ProcessStartInfo, so these are plain [Fact]s; nothing is started.
    public class SiblingAppTests
    {
        [Fact]
        public void Executable_path_has_exe_only_on_windows()
        {
            var path = SiblingApp.ExecutablePath("dir", "App2");

            Assert.Equal(Path.Combine("dir", OperatingSystem.IsWindows() ? "App2.exe" : "App2"), path);
        }

        [Fact]
        public void Start_info_passes_each_argument_unchanged_and_runs_in_the_app_folder()
        {
            var dir = AppContext.BaseDirectory;

            var psi = SiblingApp.CreateStartInfo(dir, "App2", new[] { "你好 世界", "--mode=\"viewer\"" });

            Assert.Equal(SiblingApp.ExecutablePath(dir, "App2"), psi.FileName);
            Assert.Equal(new[] { "你好 世界", "--mode=\"viewer\"" }, psi.ArgumentList);
            Assert.Equal(dir, psi.WorkingDirectory);
            Assert.False(psi.UseShellExecute);
        }

        // The page finds program 2 next to itself only because the ProjectReference copies it there; the
        // reference flows on to this test project, so a removed reference fails here rather than at run time.
        [Fact]
        public void Program_2_is_copied_next_to_the_app()
        {
            var exe = SiblingApp.ExecutablePath(AppContext.BaseDirectory, ProcessLaunchPage.TargetApp);

            Assert.True(File.Exists(exe), $"{exe} is missing");
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, ProcessLaunchPage.TargetApp + ".runtimeconfig.json")));
        }
    }
}
