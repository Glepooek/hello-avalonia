using Avalonia.Controls;
using Avalonia.FundamentalsDemo.ViewModels;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.FundamentalsDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Greet_is_disabled_until_the_input_has_text()
        {
            var vm = new MvvmViewModel();

            Assert.False(vm.GreetCommand.CanExecute(null));

            vm.Input = "Al";

            Assert.True(vm.GreetCommand.CanExecute(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Greet_stays_disabled_for_blank_input(string input)
        {
            var vm = new MvvmViewModel { Input = input };

            Assert.False(vm.GreetCommand.CanExecute(null));
        }

        [Fact]
        public void Greet_trims_the_name_into_the_greeting()
        {
            var vm = new MvvmViewModel { Input = "  Al  " };

            vm.GreetCommand.Execute(null);

            Assert.Equal("你好，Al！", vm.Greeting);
        }

        // TreesViewModel walks real controls, so it needs the Avalonia platform.
        [AvaloniaFact]
        public void Trees_refresh_replaces_both_trees_instead_of_appending()
        {
            var vm = new TreesViewModel();
            var button = new Button();

            vm.Refresh(button);
            vm.Refresh(button);

            Assert.Single(vm.VisualTree);
            Assert.Single(vm.LogicalTree);
            Assert.StartsWith("Button", vm.VisualTree[0].Label);
        }
    }
}
