using System;
using System.IO;
using System.Linq;
using Avalonia.Shared.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class FileSystemViewModel : ViewModelBase
    {
        public FileSystemViewModel()
        {
            // Start from a clean, existing sandbox: FileSystemWatcherTrigger never fires for a folder that
            // does not exist when it attaches, and a leftover file from a previous run would skew the page.
            if (Directory.Exists(SandboxDir))
            {
                Directory.Delete(SandboxDir, recursive: true);
            }

            Directory.CreateDirectory(SandboxDir);
            Refresh();
        }

        // Every file action on this page is confined to this folder under the system temp directory.
        public string SandboxDir { get; } = Path.Combine(Path.GetTempPath(), "avalonia-behaviors-demo", "files");

        public string SubDir => Path.Combine(SandboxDir, "notes");

        public string FilePath => Path.Combine(SubDir, "hello.txt");

        [ObservableProperty]
        private string _noteText = "你好，Behaviors\n";

        [ObservableProperty]
        private int _watchHits;

        [ObservableProperty]
        private string _listing = "";

        [RelayCommand]
        private void Refresh()
        {
            var entries = Directory.EnumerateFileSystemEntries(SandboxDir, "*", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(SandboxDir, p))
                .OrderBy(p => p)
                .ToList();
            Listing = entries.Count == 0 ? "（沙箱是空的）" : string.Join("\n", entries);
        }

        // The watcher raises its event on a thread-pool thread, so hop back to the UI thread before touching bindings.
        [RelayCommand]
        private void WatcherHit() => Dispatcher.UIThread.Post(() =>
        {
            WatchHits++;
            Refresh();
        });
    }
}
