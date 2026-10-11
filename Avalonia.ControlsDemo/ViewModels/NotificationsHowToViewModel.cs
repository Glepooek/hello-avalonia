using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>One toast; the three Is* flags exist so XAML can switch a style class without a converter.</summary>
    public sealed class ToastItem
    {
        public ToastItem(string message, string kind)
        {
            Message = message;
            Kind = kind;
        }

        public string Message { get; }

        public string Kind { get; }

        public bool IsInfo => Kind == "info";

        public bool IsSuccess => Kind == "success";

        public bool IsError => Kind == "error";
    }

    public sealed partial class NotificationsHowToViewModel : ObservableObject
    {
        private CancellationTokenSource? _statusCts;

        public ObservableCollection<ToastItem> Toasts { get; } = [];

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private bool _showBanner;

        /// <summary>Adds a toast; a duration of 0 keeps it until the user dismisses it.</summary>
        public void Show(string message, string kind, int durationMs)
        {
            var toast = new ToastItem(message, kind);
            Toasts.Add(toast);
            if (durationMs > 0)
            {
                _ = RemoveLater(toast, durationMs);
            }
        }

        // await resumes on the UI thread here, so the collection is never touched from another thread.
        private async Task RemoveLater(ToastItem toast, int durationMs)
        {
            await Task.Delay(durationMs);
            Toasts.Remove(toast);
        }

        [RelayCommand]
        private void Dismiss(ToastItem toast) => Toasts.Remove(toast);

        [RelayCommand]
        private async Task ShowStatus()
        {
            // A newer message must not be wiped by the older one's timer, so each call cancels the previous wait.
            _statusCts?.Cancel();
            var cts = _statusCts = new CancellationTokenSource();
            StatusMessage = $"已保存（{DateTime.Now:HH:mm:ss}）";
            try
            {
                await Task.Delay(2500, cts.Token);
                StatusMessage = "就绪";
            }
            catch (TaskCanceledException)
            {
                // Replaced by a newer message; that call owns the reset now.
            }
        }

        [RelayCommand]
        private void ShowBannerNow() => ShowBanner = true;

        [RelayCommand]
        private void DismissBanner() => ShowBanner = false;
    }
}
