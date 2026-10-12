using Avalonia.AppDevelopmentDemo.Logging;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class LoggingPage : UserControl
    {
        private readonly ListLoggerProvider _provider = new();
        private readonly ILoggerFactory _factory;
        private readonly ILogger _logger;
        private readonly ObservableCollection<string> _avaloniaEntries = new();
        private ILogSink? _previousSink;
        private PageSink? _sink;

        public LoggingPage()
        {
            InitializeComponent();

            _factory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug).AddProvider(_provider));
            _logger = _factory.CreateLogger("Demo");

            AppEntries.ItemsSource = _provider.Entries;
            AvaloniaEntries.ItemsSource = _avaloniaEntries;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            // Logger.Sink is process-wide, so remember the old sink and put it back on unload.
            _previousSink = Logger.Sink;
            _sink = new PageSink(line => Dispatcher.UIThread.Post(() => _avaloniaEntries.Add(line)));
            Logger.Sink = _sink;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            if (ReferenceEquals(Logger.Sink, _sink))
                Logger.Sink = _previousSink;
        }

        private void OnAppLog(object? sender, RoutedEventArgs e)
        {
            _logger.LogDebug("d {Index}", 1);
            _logger.LogWarning("w");
            _logger.LogError(new InvalidOperationException("boom"), "e");
        }

        private void OnAppClear(object? sender, RoutedEventArgs e) => _provider.Entries.Clear();

        private void OnAvaloniaLog(object? sender, RoutedEventArgs e)
        {
            // A reflection binding to a property that does not exist: the framework reports it
            // through Logger rather than throwing.
            var probe = new TextBlock { DataContext = new object() };
            probe.Bind(TextBlock.TextProperty, new Binding("Missing"));
            ((Panel)Probe).Children.Add(probe);
            ((Panel)Probe).Children.Clear();
        }

        private void OnAvaloniaClear(object? sender, RoutedEventArgs e) => _avaloniaEntries.Clear();

        private sealed class PageSink : ILogSink
        {
            private readonly Action<string> _write;

            public PageSink(Action<string> write) => _write = write;

            public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

            public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
                => _write($"{level}/{area}: {messageTemplate}");

            public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
                => _write($"{level}/{area}: {messageTemplate} [{string.Join(", ", propertyValues.Select(v => v?.ToString()))}]");
        }
    }
}
