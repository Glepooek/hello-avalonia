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
            // Nest two collectors: with a null sink before the test, restoring to null would also look correct.
            using var outer = new LogCollector();

            using (new LogCollector())
            {
            }

            Assert.Same(outer, Logger.Sink);
        }
    }
}
