using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class StorageProviderPage : UserControl
    {
        public StorageProviderPage()
        {
            InitializeComponent();

            // Enum.GetValues keeps the list honest if a folder is added upstream.
            FolderBox.ItemsSource = Enum.GetValues<WellKnownFolder>();
            FolderBox.SelectedIndex = 1;
            PathBox.Text = Path.GetTempPath();
        }

        private IStorageProvider? Storage => TopLevel.GetTopLevel(this)?.StorageProvider;

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Capabilities.Text = Storage is { } s
                ? $"CanOpen = {s.CanOpen}，CanSave = {s.CanSave}，CanPickFolder = {s.CanPickFolder}"
                : "StorageProvider 为 null";
        }

        private async void OnList(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { } storage || FolderBox.SelectedItem is not WellKnownFolder kind)
            {
                return;
            }

            var folder = await storage.TryGetWellKnownFolderAsync(kind);
            await Show(folder, $"WellKnownFolder.{kind}");
        }

        private async void OnPath(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { } storage)
            {
                return;
            }

            // The string overload is an extension method meant for desktop; the core API takes a Uri.
            var folder = await storage.TryGetFolderFromPathAsync(PathBox.Text ?? "");
            await Show(folder, "TryGetFolderFromPathAsync");
        }

        private async System.Threading.Tasks.Task Show(IStorageFolder? folder, string source)
        {
            Items.ItemsSource = null;
            if (folder is null)
            {
                Result.Text = $"{source} 返回 null（路径不存在，或平台不提供）";
                return;
            }

            var names = new List<string>();
            await foreach (var item in folder.GetItemsAsync())
            {
                names.Add((item is IStorageFolder ? "[夹] " : "[文件] ") + item.Name);
                if (names.Count >= 50)
                {
                    break;
                }
            }

            Items.ItemsSource = names;
            Result.Text = $"{source} → {folder.Name}，前 {names.Count} 项";
        }
    }
}
