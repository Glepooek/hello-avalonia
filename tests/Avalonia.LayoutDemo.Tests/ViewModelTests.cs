using Avalonia.LayoutDemo.ViewModels;
using Xunit;

namespace Avalonia.LayoutDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Responsive_starts_compact()
        {
            var vm = new ResponsiveViewModel();

            Assert.True(vm.IsCompact);
            Assert.False(vm.IsWide);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(499.9, true)]
        [InlineData(500, false)]
        [InlineData(1200, false)]
        public void Responsive_switches_at_the_500_pixel_breakpoint(double width, bool expectedCompact)
        {
            var vm = new ResponsiveViewModel();

            vm.UpdateLayout(width);

            Assert.Equal(width, vm.CurrentWidth);
            Assert.Equal(expectedCompact, vm.IsCompact);
            Assert.Equal(!expectedCompact, vm.IsWide);
        }
    }
}
