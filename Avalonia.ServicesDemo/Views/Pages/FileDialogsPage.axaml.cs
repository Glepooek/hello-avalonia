using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class FileDialogsPage : UserControl
    {
        private static readonly FilePickerFileType TextType = new("文本文件") { Patterns = new[] { "*.txt" } };
        private static readonly FilePickerFileType MarkdownType = new("Markdown") { Patterns = new[] { "*.md" } };

        public FileDialogsPage()
        {
            InitializeComponent();
        }

        private IStorageProvider? Storage => TopLevel.GetTopLevel(this)?.StorageProvider;

        private async void OnOpen(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { CanOpen: true } storage)
            {
                Result.Text = "当前平台的 StorageProvider.CanOpen 为 False，无法弹出打开对话框";
                return;
            }

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开文本文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { TextType, FilePickerFileTypes.All },
            });

            // Cancelling yields an empty list, not an exception.
            if (files.Count == 0)
            {
                Result.Text = "已取消（OpenFilePickerAsync 返回空列表）";
                return;
            }

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            FileText.Text = await reader.ReadToEndAsync();
            Result.Text = $"已打开：{files[0].Name}";
        }

        private async void OnSave(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { CanSave: true } storage)
            {
                Result.Text = "当前平台的 StorageProvider.CanSave 为 False，无法弹出保存对话框";
                return;
            }

            var result = await storage.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
            {
                Title = "保存文件",
                SuggestedFileName = "demo",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { TextType, MarkdownType },
            });

            if (result.File is not { } file)
            {
                Result.Text = "已取消（StorageFile 为 null）";
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("Hello from Avalonia.ServicesDemo");
            Result.Text = $"已保存：{file.Name}，用户选的类型：{result.SelectedFileType?.Name}";
        }

        private async void OnFolder(object? sender, RoutedEventArgs e)
        {
            if (Storage is not { CanPickFolder: true } storage)
            {
                Result.Text = "当前平台的 StorageProvider.CanPickFolder 为 False，无法弹出文件夹对话框";
                return;
            }

            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择文件夹" });
            Result.Text = folders.Count == 0 ? "已取消" : $"已选择：{folders.First().Name}";
        }
    }
}
