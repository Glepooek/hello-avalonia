using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ServicesDemo.LaunchTarget
{
    public partial class MainWindow : Window
    {
        // The designer needs a parameterless constructor.
        public MainWindow() : this([])
        {
        }

        public MainWindow(string[] args)
        {
            InitializeComponent();
            PidText.Text = $"本进程 PID = {Environment.ProcessId}";
            ArgsList.ItemsSource = args.Length == 0 ? new[] { "（没有参数）" } : args;
        }

        // Shutdown(code) becomes the process exit code, which program 1 reads from Process.ExitCode.
        private void OnExit(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown((int)(ExitCodeBox.Value ?? 0));
        }
    }
}
