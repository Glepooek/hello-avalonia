using Avalonia;
using Avalonia.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Avalonia.AppDevelopmentDemo
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception exception)
            {
                Trace.TraceError("Application crashed: {0}", exception);
                Environment.ExitCode = 1;
            }
            finally
            {
                AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                Trace.Flush();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace(LogEventLevel.Debug);

        private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError(
                "Unhandled domain exception (terminating: {0}): {1}",
                e.IsTerminating,
                e.ExceptionObject);
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Trace.TraceError("Unobserved task exception: {0}", e.Exception);
            e.SetObserved();
        }
    }
}
