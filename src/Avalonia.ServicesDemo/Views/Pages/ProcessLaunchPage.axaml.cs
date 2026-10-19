using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ServicesDemo.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class ProcessLaunchPage : UserControl
    {
        public const string TargetApp = "Avalonia.ServicesDemo.LaunchTarget";

        public ProcessLaunchPage()
        {
            InitializeComponent();
        }

        private async void OnLaunch(object? sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser())
            {
                Result.Text = "当前平台在沙箱里运行，不能启动其他进程";
                return;
            }

            var args = (ArgsBox.Text ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var psi = SiblingApp.CreateStartInfo(AppContext.BaseDirectory, TargetApp, args);

            Process? process;
            try
            {
                process = Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                // Missing file, or on Linux/macOS a file that lost its execute bit when copied or unzipped.
                Result.Text = $"启动失败：{ex.Message}（{psi.FileName}）";
                return;
            }

            if (process is null)
            {
                Result.Text = "Process.Start 返回 null";
                return;
            }

            using (process)
            {
                LaunchButton.IsEnabled = false;
                Result.Text = $"已启动程序 2，PID = {process.Id}，等待它退出……";
                // Awaiting keeps the UI thread free; WaitForExit() here would freeze this window until program 2 closes.
                await process.WaitForExitAsync();
                Result.Text = $"程序 2（PID = {process.Id}）已退出，退出码 = {process.ExitCode}";
                LaunchButton.IsEnabled = true;
            }
        }
    }
}
