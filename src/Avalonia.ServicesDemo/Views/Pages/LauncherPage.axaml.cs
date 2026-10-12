using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class LauncherPage : UserControl
    {
        public LauncherPage()
        {
            InitializeComponent();
            DirBox.Text = Path.GetTempPath();
        }

        private ILauncher? Launcher => TopLevel.GetTopLevel(this)?.Launcher;

        private async void OnUri(object? sender, RoutedEventArgs e)
        {
            if (Launcher is not { } launcher)
            {
                Result.Text = "Launcher 为 null";
                return;
            }

            if (!Uri.TryCreate(UriBox.Text, UriKind.Absolute, out var uri))
            {
                Result.Text = "不是合法的绝对 URI";
                return;
            }

            var accepted = await launcher.LaunchUriAsync(uri);
            Result.Text = $"LaunchUriAsync 返回 {accepted}";
        }

        private async void OnDirectory(object? sender, RoutedEventArgs e)
        {
            if (Launcher is not { } launcher)
            {
                Result.Text = "Launcher 为 null";
                return;
            }

            var accepted = await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(DirBox.Text ?? ""));
            Result.Text = $"LaunchDirectoryInfoAsync 返回 {accepted}";
        }
    }
}
