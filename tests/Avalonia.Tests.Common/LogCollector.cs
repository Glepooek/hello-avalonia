using Avalonia.Logging;
using System;
using System.Collections.Generic;

namespace Avalonia.Tests.Common
{
    /// <summary>Collects Avalonia log events at Warning level or above until it is disposed, then restores the previous sink.</summary>
    public sealed class LogCollector : ILogSink, IDisposable
    {
        private readonly ILogSink? _previous = Logger.Sink;

        public LogCollector() => Logger.Sink = this;

        public List<string> Messages { get; } = new();

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
            => Messages.Add($"{area}: {messageTemplate}");

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
            => Messages.Add($"{area}: {messageTemplate} [{string.Join(", ", propertyValues)}]");

        public void Dispose() => Logger.Sink = _previous;
    }
}
