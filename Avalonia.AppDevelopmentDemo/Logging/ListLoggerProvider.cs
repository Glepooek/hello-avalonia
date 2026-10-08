using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;

namespace Avalonia.AppDevelopmentDemo.Logging
{
    // Collects ILogger output into a list the UI can show; WinExe has no console to read.
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
                => _entries.Add($"{logLevel}:{formatter(state, exception)}");
        }
    }
}
