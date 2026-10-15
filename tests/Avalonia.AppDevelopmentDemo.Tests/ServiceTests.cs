using Avalonia.AppDevelopmentDemo.Logging;
using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.Tests.Common;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace Avalonia.AppDevelopmentDemo.Tests
{
    public class ServiceTests
    {
        private sealed class FixedClock : IClock
        {
            public DateTime Now { get; } = new(2026, 10, 15, 9, 5, 7);
        }

        // HH:mm:ss uses the culture's time separator, so pin the culture before asserting on ':'.
        [Fact]
        public void Greeter_formats_the_name_and_the_time_from_the_injected_clock()
        {
            using var culture = CultureScope.Invariant();

            Assert.Equal("你好，Avalonia。现在是 09:05:07", new Greeter(new FixedClock()).Greet("Avalonia"));
        }

        // The id comes from a static counter shared by every test in the process, so only the order is stable.
        [Fact]
        public void Each_counter_takes_a_later_id_than_the_one_before()
        {
            var first = new Counter();
            var second = new Counter();

            Assert.True(second.Id > first.Id, $"{second.Id} should be greater than {first.Id}");
        }

        [Fact]
        public void List_logger_writes_level_and_message_and_ignores_level_none()
        {
            using var provider = new ListLoggerProvider();
            var logger = provider.CreateLogger("Demo");

            logger.LogWarning("w {Index}", 1);
            logger.LogError(new InvalidOperationException("boom"), "e");

            Assert.Equal(new[] { "Warning:w 1", "Error:e" }, provider.Entries);
            Assert.False(logger.IsEnabled(LogLevel.None));
        }
    }
}
