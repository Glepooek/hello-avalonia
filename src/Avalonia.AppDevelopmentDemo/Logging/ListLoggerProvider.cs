using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;

namespace Avalonia.AppDevelopmentDemo.Logging
{
    // Keeps application log entries in one process-wide buffer for diagnostics and tests.
    public sealed class ListLoggerProvider : ILoggerProvider
    {
        public ObservableCollection<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

        public void Dispose() { }

        private sealed class ListLogger : ILogger
        {
            private readonly ObservableCollection<string> _entries;

            public ListLogger(ObservableCollection<string> entries) => _entries = entries;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var entry = $"{logLevel}:{formatter(state, exception)}";
                if (exception is not null)
                    entry += $" | {exception.GetType().Name}: {exception.Message}";

                _entries.Add(entry);
            }
        }
    }
}
