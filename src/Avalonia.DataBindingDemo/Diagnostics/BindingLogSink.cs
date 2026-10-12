using Avalonia.Logging;
using Avalonia.Threading;
using System.Collections.ObjectModel;

namespace Avalonia.DataBindingDemo.Diagnostics
{
    /// <summary>
    /// Captures binding warnings into a collection the debugging page can show,
    /// and forwards everything to whichever sink was installed before it.
    /// </summary>
    public sealed class BindingLogSink : ILogSink
    {
        private readonly ILogSink? _inner;

        private BindingLogSink(ILogSink? inner) => _inner = inner;

        public static BindingLogSink? Current { get; private set; }

        public ObservableCollection<string> Entries { get; } = new();

        // Idempotent: the page may be constructed more than once (designer, re-navigation).
        public static BindingLogSink Install()
        {
            if (Current is null)
            {
                Current = new BindingLogSink(Logger.Sink);
                Logger.Sink = Current;
            }

            return Current;
        }

        public bool IsEnabled(LogEventLevel level, string area)
            => IsBindingWarning(level, area) || (_inner?.IsEnabled(level, area) ?? false);

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            Capture(level, area, messageTemplate);
            _inner?.Log(level, area, source, messageTemplate);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            // The template is "{Property} to {Expression} ... {Message}"; the values alone read better.
            Capture(level, area, propertyValues.Length > 0 ? string.Join(" | ", propertyValues) : messageTemplate);
            _inner?.Log(level, area, source, messageTemplate, propertyValues);
        }

        private static bool IsBindingWarning(LogEventLevel level, string area)
            => area == LogArea.Binding && level >= LogEventLevel.Warning;

        private void Capture(LogEventLevel level, string area, string text)
        {
            if (IsBindingWarning(level, area))
            {
                // Bindings may log from inside layout; never mutate a bound collection re-entrantly.
                Dispatcher.UIThread.Post(() => Entries.Add(text));
            }
        }
    }
}
