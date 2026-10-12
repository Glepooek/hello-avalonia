using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class ThreadingPage : UserControl
    {
        public ThreadingPage()
        {
            InitializeComponent();
        }

        private async void OnDirect(object? sender, RoutedEventArgs e)
        {
            // The write happens inside Task.Run, i.e. on a thread-pool thread.
            var message = await Task.Run(() =>
            {
                var onUi = Dispatcher.UIThread.CheckAccess();
                try
                {
                    Target.Text = "written from the background";
                    return $"CheckAccess = {onUi}，写入成功（不应该发生）";
                }
                catch (InvalidOperationException ex)
                {
                    return $"CheckAccess = {onUi}，InvalidOperationException：{ex.Message}";
                }
            });
            Result.Text = message;
        }

        private async void OnPost(object? sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Target.Text = "written via Post";
                    Result.Text = $"Post 的回调里 CheckAccess = {Dispatcher.UIThread.CheckAccess()}，写入成功";
                });
            });
        }

        private async void OnAwait(object? sender, RoutedEventArgs e)
        {
            await Task.Run(() => System.Threading.Thread.Sleep(10));
            Target.Text = "written after await";
            Result.Text = $"await 之后 CheckAccess = {Dispatcher.UIThread.CheckAccess()}，写入成功";
        }
    }
}
