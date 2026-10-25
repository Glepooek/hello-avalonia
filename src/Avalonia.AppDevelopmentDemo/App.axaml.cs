using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.AppDevelopmentDemo.Views;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.AppDevelopmentDemo
{
    public partial class App : Application
    {
        private ServiceProvider? _services;
        private ApplicationExceptionReporter? _exceptionReporter;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            _services = ApplicationServices.CreateServiceProvider();
            _exceptionReporter = _services.GetRequiredService<ApplicationExceptionReporter>();
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = _services.GetRequiredService<MainWindow>();
                desktop.Exit += OnDesktopExit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _exceptionReporter?.ReportUiThreadException(e.Exception);
            // Do not mark unknown exceptions as handled. Logging them does not make continuing safe.
        }

        private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;

            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Exit -= OnDesktopExit;

            _services?.Dispose();
            _services = null;
            _exceptionReporter = null;
        }
    }
}
