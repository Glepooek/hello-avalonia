using Avalonia.InputDemo.ViewModels;
using Xunit;

namespace Avalonia.InputDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Increment_is_blocked_while_the_view_model_is_locked()
        {
            var vm = new InteractivityViewModel();

            Assert.False(vm.IncrementCommand.CanExecute(null));
        }

        [Fact]
        public void Unlocking_raises_CanExecuteChanged_and_enables_the_command()
        {
            var vm = new InteractivityViewModel();
            var raised = 0;
            vm.IncrementCommand.CanExecuteChanged += (_, _) => raised++;

            vm.Unlocked = true;

            Assert.Equal(1, raised);
            Assert.True(vm.IncrementCommand.CanExecute(null));
        }

        [Fact]
        public void Locking_again_disables_the_command_again()
        {
            var vm = new InteractivityViewModel { Unlocked = true };

            vm.Unlocked = false;

            Assert.False(vm.IncrementCommand.CanExecute(null));
        }

        [Fact]
        public void Increment_adds_one_each_time_it_runs()
        {
            var vm = new InteractivityViewModel { Unlocked = true };

            vm.IncrementCommand.Execute(null);
            vm.IncrementCommand.Execute(null);

            Assert.Equal(2, vm.Count);
        }
    }
}
