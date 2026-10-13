using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using Avalonia.XamlDemo.Views.Pages;
using System.Linq;
using Xunit;

namespace Avalonia.XamlDemo.Tests
{
    public class InteractionTests
    {
        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        [AvaloniaFact]
        public void Directives_page_reads_the_text_box_from_code_behind_and_through_element_binding()
        {
            var window = UiHelpers.Show(new DirectivesPage());
            var box = window.ByName<TextBox>("SourceBox");

            box.Text = "hello";
            window.Click(window.GetVisualDescendants().OfType<Button>().Single(b => "从 code-behind 读取".Equals(b.Content)));

            Assert.Equal("code-behind 读到：hello", window.ByName<TextBlock>("ReadResult").Text);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "元素名绑定读到：hello");
        }

        [AvaloniaFact]
        public void Compilation_page_reports_the_demo_assembly()
        {
            var window = UiHelpers.Show(new CompilationPage());

            Assert.Contains("Avalonia.XamlDemo", window.ByName<TextBlock>("CompiledFlagText").Text);
        }

        [AvaloniaFact]
        public void Compilation_page_element_binding_follows_the_probe_text_box()
        {
            var window = UiHelpers.Show(new CompilationPage());

            window.ByName<TextBox>("Probe").Text = "changed";

            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "读到：changed");
        }
    }
}
