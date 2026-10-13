using Avalonia.Data;
using Avalonia.DataBindingDemo.Converters;
using Avalonia.DataBindingDemo.Models;
using Avalonia.DataBindingDemo.ViewModels;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Avalonia.DataBindingDemo.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void Decrement_is_disabled_at_zero_and_notified_when_count_changes()
        {
            var vm = new CommandsViewModel();
            var raised = 0;
            vm.DecrementCommand.CanExecuteChanged += (_, _) => raised++;
            Assert.False(vm.DecrementCommand.CanExecute(null));

            vm.IncrementCommand.Execute(null);

            Assert.True(vm.DecrementCommand.CanExecute(null));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Add_command_parses_its_parameter()
        {
            var vm = new CommandsViewModel();

            vm.AddCommand.Execute("5");
            vm.AddCommand.Execute("10");

            Assert.Equal(15, vm.Count);
        }

        [Fact]
        public void User_name_needs_three_characters_to_pass_validation()
        {
            var vm = new ValidationViewModel();

            vm.UserName = "ab";
            Assert.True(vm.HasErrors);
            Assert.Contains("用户名至少 3 个字符", vm.GetErrors(nameof(vm.UserName)).Select(e => e.ErrorMessage));
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.UserName = "abc";
            Assert.False(vm.HasErrors);
            Assert.True(vm.SubmitCommand.CanExecute(null));
        }

        [Fact]
        public void Bad_email_is_reported_and_blocks_submit()
        {
            var vm = new ValidationViewModel { UserName = "abc" };

            vm.Email = "not-an-email";

            Assert.Contains("邮箱格式不对", vm.GetErrors(nameof(vm.Email)).Select(e => e.ErrorMessage));
            Assert.False(vm.SubmitCommand.CanExecute(null));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(151)]
        public void Age_outside_the_range_throws_and_keeps_the_old_value(int age)
        {
            var vm = new ValidationViewModel();

            var ex = Assert.Throws<DataValidationException>(() => vm.Age = age);

            Assert.Equal("年龄必须在 0 到 150 之间", ex.Message);
            Assert.Equal(18, vm.Age);
        }

        [Fact]
        public void Add_grows_both_lists_but_remove_last_only_touches_the_observable_one()
        {
            var vm = new CollectionsViewModel();

            vm.AddCommand.Execute(null);
            vm.RemoveLastCommand.Execute(null);
            vm.RemoveLastCommand.Execute(null);

            Assert.Equal(2, vm.Contacts.Count);
            Assert.Equal(4, vm.FrozenContacts.Count);
        }

        [Fact]
        public void Filter_sort_and_grouping_rebuild_the_rows()
        {
            var vm = new CollectionViewsViewModel();
            Assert.Equal(6, vm.Rows.Count);

            vm.Filter = "张";
            Assert.Equal(new[] { "张三" }, vm.Rows.Cast<Contact>().Select(c => c.Name));

            vm.Filter = "";
            vm.SortByAge = true;
            Assert.Equal(new[] { 22, 26, 28, 30, 35, 41 }, vm.Rows.Cast<Contact>().Select(c => c.Age));

            // Groups come out in order of first appearance in the age-sorted list, not alphabetically.
            vm.GroupByCity = true;
            var headers = vm.Rows.OfType<ContactGroupHeader>().ToList();
            Assert.Equal(new[] { "北京", "深圳", "上海", "广州" }, headers.Select(h => h.Key));
            Assert.Equal(2, headers[0].Count);
            Assert.Equal(10, vm.Rows.Count);
        }

        [Fact]
        public void Toggle_flips_selected_between_null_and_a_contact()
        {
            var vm = new DebuggingViewModel();

            vm.ToggleCommand.Execute(null);
            Assert.Equal("张三", vm.Selected?.Name);

            vm.ToggleCommand.Execute(null);
            Assert.Null(vm.Selected);
        }

        [Fact]
        public void Contact_to_string_shows_name_and_city()
        {
            Assert.Equal("张三（北京）", new Contact("张三", "北京", 28).ToString());
        }

        [Theory]
        [InlineData(-5.0, "❄ 结冰（-5 °C）")]
        [InlineData(20.0, "🙂 舒适（20 °C）")]
        [InlineData(30.0, "🔥 炎热（30 °C）")]
        public void Thermometer_picks_the_band_for_the_temperature(double celsius, string expected)
        {
            Assert.Equal(expected, InlineConverters.Thermometer.Convert(celsius, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Celsius_converter_round_trips_and_ignores_unparseable_input()
        {
            var converter = new CelsiusToFahrenheitConverter();
            var inv = CultureInfo.InvariantCulture;

            Assert.Equal("212.0", converter.Convert(100.0, typeof(string), null, inv));
            Assert.Equal(100.0, (double)converter.ConvertBack("212", typeof(double), null, inv)!, 6);
            Assert.Same(BindingOperations.DoNothing, converter.ConvertBack("-", typeof(double), null, inv));
            Assert.Same(BindingOperations.DoNothing, converter.Convert("text", typeof(string), null, inv));
        }
    }
}
