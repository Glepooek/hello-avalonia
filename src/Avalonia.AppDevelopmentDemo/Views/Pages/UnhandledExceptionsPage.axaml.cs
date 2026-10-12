using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class UnhandledExceptionsPage : UserControl
    {
        private int _count;

        public UnhandledExceptionsPage()
        {
            InitializeComponent();
        }

        // The event is static-ish (one per dispatcher), so subscribe only while the page is on screen.
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Dispatcher.UIThread.UnhandledException += OnUnhandled;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            Dispatcher.UIThread.UnhandledException -= OnUnhandled;
        }

        private void OnUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _count++;
            CountLine.Text = $"处理器被调用：{_count} 次";
            MessageLine.Text = $"最近一次异常：{e.Exception.GetType().Name}：{e.Exception.Message}";
            e.Handled = true;
        }

        private void OnThrow(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => throw new InvalidOperationException("thrown from a posted callback"));
        }
    }
}
