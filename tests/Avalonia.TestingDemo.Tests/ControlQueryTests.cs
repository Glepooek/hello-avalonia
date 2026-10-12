using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestingDemo.Views.Pages;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class ControlQueryTests
    {
        [AvaloniaFact]
        public void Finds_a_control_by_Name()
        {
            var window = UiHelpers.Show(new CounterPage());

            var button = window.ByName<Button>("IncrementButton");

            Assert.Equal("加", button.Content);
        }

        [AvaloniaFact]
        public void Finds_a_control_by_AutomationId()
        {
            var window = UiHelpers.Show(new CounterPage());

            var button = window.ByAutomationId<Button>("increment-btn");

            Assert.Equal("IncrementButton", button.Name);
        }

        [AvaloniaFact]
        public void Every_form_control_is_reachable_by_its_AutomationId()
        {
            var window = UiHelpers.Show(new FormPage());

            Assert.NotNull(window.ByAutomationId<TextBox>("name-box"));
            Assert.NotNull(window.ByAutomationId<TextBox>("age-box"));
            Assert.NotNull(window.ByAutomationId<TextBlock>("name-error"));
            Assert.NotNull(window.ByAutomationId<TextBlock>("age-error"));
            Assert.NotNull(window.ByAutomationId<Button>("submit-btn"));
            Assert.NotNull(window.ByAutomationId<TextBlock>("status-text"));
        }

        [AvaloniaFact]
        public void Initial_state_is_readable_from_the_controls()
        {
            var window = UiHelpers.Show(new CounterPage());

            Assert.Equal("0", window.ByName<TextBlock>("CountText").Text);
            // Assert IsEffectivelyEnabled: a command-disabled button keeps IsEnabled = true on some paths.
            Assert.True(window.ByName<Button>("IncrementButton").IsEffectivelyEnabled);
            Assert.False(window.ByName<Button>("DecrementButton").IsEffectivelyEnabled);
            Assert.False(window.ByName<Button>("ResetButton").IsEffectivelyEnabled);
        }
    }
}
