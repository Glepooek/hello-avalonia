using Avalonia.XamlDemo.MarkupExtensions;
using Xunit;

namespace Avalonia.XamlDemo.Tests
{
    // No [AvaloniaFact]: the extension is a plain class, ProvideValue never touches the service provider.
    public class MarkupExtensionTests
    {
        [Fact]
        public void RepeatText_repeats_the_text_count_times()
        {
            var extension = new RepeatTextExtension("ab") { Count = 3 };

            Assert.Equal("ababab", extension.ProvideValue(null!));
        }

        [Fact]
        public void RepeatText_defaults_to_two_repeats()
        {
            Assert.Equal("abab", new RepeatTextExtension("ab").ProvideValue(null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void RepeatText_never_drops_below_one_repeat(int count)
        {
            var extension = new RepeatTextExtension("ab") { Count = count };

            Assert.Equal("ab", extension.ProvideValue(null!));
        }
    }
}
