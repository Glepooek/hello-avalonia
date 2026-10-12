using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Tests.Common;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class LogCollectorTests
    {
        [AvaloniaFact]
        public void Collects_the_warning_for_a_binding_to_a_missing_property()
        {
            using var logs = new LogCollector();
            var text = new TextBlock { DataContext = new object() };
            text.Bind(TextBlock.TextProperty, new Binding("Missing"));

            UiHelpers.Show(text);

            Assert.Contains(logs.Messages, m => m.StartsWith("Binding:") && m.Contains("Missing"));
        }

        [AvaloniaFact]
        public void Restores_the_previous_sink_on_dispose()
        {
            var before = Logger.Sink;

            using (new LogCollector())
            {
            }

            Assert.Same(before, Logger.Sink);
        }
    }
}
