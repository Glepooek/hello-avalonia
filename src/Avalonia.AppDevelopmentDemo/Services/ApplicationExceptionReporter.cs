using Microsoft.Extensions.Logging;
using System;

namespace Avalonia.AppDevelopmentDemo.Services
{
    public sealed class ApplicationExceptionReporter
    {
        private readonly ILogger<ApplicationExceptionReporter> _logger;

        public ApplicationExceptionReporter(ILogger<ApplicationExceptionReporter> logger)
        {
            _logger = logger;
        }

        public void ReportUiThreadException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled UI-thread exception");
        }

        public void ReportUiThreadExceptionFilter(Exception exception, bool requestCatch)
        {
            _logger.LogDebug(exception, "UI-thread exception filter (RequestCatch: {RequestCatch})", requestCatch);
        }
    }
}
