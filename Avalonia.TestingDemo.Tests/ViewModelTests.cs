using Avalonia.TestingDemo.ViewModels;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    // No [AvaloniaFact] here: a ViewModel has no controls, so plain [Fact] is enough and much faster.
    public class ViewModelTests
    {
        [Fact]
        public void Counter_starts_at_zero_with_decrement_and_reset_disabled()
        {
            var vm = new CounterViewModel();

            Assert.Equal(0, vm.Count);
            Assert.False(vm.DecrementCommand.CanExecute(null));
            Assert.False(vm.ResetCommand.CanExecute(null));
        }

        [Fact]
        public void Counter_increment_enables_decrement_and_reset()
        {
            var vm = new CounterViewModel();

            vm.IncrementCommand.Execute(null);

            Assert.Equal(1, vm.Count);
            Assert.True(vm.DecrementCommand.CanExecute(null));
            Assert.True(vm.ResetCommand.CanExecute(null));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("a", "姓名至少 2 个字符")]
        [InlineData("Al", "")]
        public void Form_name_error_follows_the_name(string name, string expected)
        {
            var vm = new FormViewModel { Name = name };

            Assert.Equal(expected, vm.NameError);
        }

        [Theory]
        [InlineData("abc", "年龄必须是整数")]
        [InlineData("-1", "年龄必须在 0 到 150 之间")]
        [InlineData("151", "年龄必须在 0 到 150 之间")]
        [InlineData("30", "")]
        public void Form_age_error_follows_the_age(string age, string expected)
        {
            var vm = new FormViewModel { Age = age };

            Assert.Equal(expected, vm.AgeError);
        }

        [Fact]
        public void Form_submit_needs_both_fields_valid()
        {
            var vm = new FormViewModel();
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.Name = "Alice";
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.Age = "30";
            Assert.True(vm.SubmitCommand.CanExecute(null));

            vm.SubmitCommand.Execute(null);
            Assert.Equal("已提交：Alice，30 岁", vm.Status);
        }

        [Fact]
        public void List_add_trims_clears_the_input_and_updates_the_summary()
        {
            var vm = new ListViewModel { NewItem = "  apple  " };

            vm.AddCommand.Execute(null);

            Assert.Equal(new[] { "apple" }, vm.Items);
            Assert.Equal("", vm.NewItem);
            Assert.Equal("共 1 项", vm.Summary);
        }

        [Fact]
        public void List_remove_needs_a_selection()
        {
            var vm = new ListViewModel();
            vm.Items.Add("a");
            Assert.False(vm.RemoveCommand.CanExecute(null));

            vm.Selected = "a";
            Assert.True(vm.RemoveCommand.CanExecute(null));

            vm.RemoveCommand.Execute(null);
            Assert.Empty(vm.Items);
            Assert.Equal("共 0 项", vm.Summary);
        }
    }
}
