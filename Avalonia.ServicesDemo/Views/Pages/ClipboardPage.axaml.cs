using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using System.Linq;

namespace Avalonia.ServicesDemo.Views.Pages
{
    public partial class ClipboardPage : UserControl
    {
        public ClipboardPage()
        {
            InitializeComponent();
        }

        // The clipboard hangs off the TopLevel, and is null until the control is attached.
        private IClipboard? Clipboard => TopLevel.GetTopLevel(this)?.Clipboard;

        private async void OnSetText(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                Result.Text = "当前平台没有剪贴板服务";
                return;
            }

            await clipboard.SetTextAsync(Source.Text ?? "");
            Result.Text = "已用 SetTextAsync 写入";
        }

        private async void OnSetData(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                return;
            }

            // Ownership passes to Avalonia on SetDataAsync, so the DataTransfer is not disposed here.
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(Source.Text ?? ""));
            await clipboard.SetDataAsync(data);
            Result.Text = "已用 SetDataAsync(DataTransfer) 写入";
        }

        private async void OnClear(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is { } clipboard)
            {
                await clipboard.ClearAsync();
                Result.Text = "已清空";
            }
        }

        private async void OnRead(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                return;
            }

            var text = await clipboard.TryGetTextAsync();
            Result.Text = text is null ? "TryGetTextAsync 返回 null（剪贴板里没有文本）" : $"TryGetTextAsync = \"{text}\"";
        }

        private async void OnFormats(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is not { } clipboard)
            {
                return;
            }

            // The returned transfer is disposable; call TryGetDataAsync once rather than chaining extension methods.
            using var data = await clipboard.TryGetDataAsync();
            Result.Text = data is null
                ? "TryGetDataAsync 返回 null（剪贴板为空）"
                : "格式：" + string.Join("、", data.Formats.Select(f => f.Identifier));
        }
    }
}
