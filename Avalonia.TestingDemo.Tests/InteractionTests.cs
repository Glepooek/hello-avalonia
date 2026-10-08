using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestingDemo.Views.Pages;
using System.Linq;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class InteractionTests
    {
        [AvaloniaFact]
        public void Clicking_plus_then_minus_walks_the_counter_and_toggles_enabled_state()
        {
            var window = UiHelpers.Show(new CounterPage());
            var count = window.ByName<TextBlock>("CountText");
            var plus = window.ByName<Button>("IncrementButton");
            var minus = window.ByName<Button>("DecrementButton");

            window.Click(plus);
            Assert.Equal("1", count.Text);
            Assert.True(minus.IsEffectivelyEnabled);

            window.Click(minus);
            Assert.Equal("0", count.Text);
            Assert.False(minus.IsEffectivelyEnabled);
        }

        [AvaloniaFact]
        public void Reset_returns_the_counter_to_zero()
        {
            var window = UiHelpers.Show(new CounterPage());
            var plus = window.ByName<Button>("IncrementButton");

            window.Click(plus);
            window.Click(plus);
            window.Click(plus);
            window.Click(window.ByName<Button>("ResetButton"));

            Assert.Equal("0", window.ByName<TextBlock>("CountText").Text);
        }

        [AvaloniaFact]
        public void Typing_into_the_form_shows_and_clears_validation_errors()
        {
            var window = UiHelpers.Show(new FormPage());

            window.Type(window.ByName<TextBox>("NameBox"), "A");
            Assert.Equal("姓名至少 2 个字符", window.ByName<TextBlock>("NameError").Text);

            window.Type(window.ByName<TextBox>("NameBox"), "l");
            Assert.Equal("", window.ByName<TextBlock>("NameError").Text);

            window.Type(window.ByName<TextBox>("AgeBox"), "x");
            Assert.Equal("年龄必须是整数", window.ByName<TextBlock>("AgeError").Text);
        }

        [AvaloniaFact]
        public void Submit_is_enabled_only_when_the_whole_form_is_valid()
        {
            var window = UiHelpers.Show(new FormPage());
            var submit = window.ByName<Button>("SubmitButton");
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(window.ByName<TextBox>("NameBox"), "Alice");
            Assert.False(submit.IsEffectivelyEnabled);

            window.Type(window.ByName<TextBox>("AgeBox"), "30");
            Assert.True(submit.IsEffectivelyEnabled);

            window.Click(submit);
            Assert.Equal("已提交：Alice，30 岁", window.ByName<TextBlock>("StatusText").Text);
        }

        [AvaloniaFact]
        public void List_add_select_remove_round_trip()
        {
            var window = UiHelpers.Show(new ListPage());
            var summary = window.ByName<TextBlock>("SummaryText");
            var list = window.ByName<ListBox>("ItemsList");
            Assert.Equal("共 0 项", summary.Text);

            window.Type(window.ByName<TextBox>("NewItemBox"), "apple");
            window.Click(window.ByName<Button>("AddButton"));
            window.Type(window.ByName<TextBox>("NewItemBox"), "pear");
            window.Click(window.ByName<Button>("AddButton"));

            Assert.Equal("共 2 项", summary.Text);
            Assert.Equal("", window.ByName<TextBox>("NewItemBox").Text);

            list.SelectedIndex = 0;
            window.Click(window.ByName<Button>("RemoveButton"));

            Assert.Equal("共 1 项", summary.Text);
            Assert.Equal(new[] { "pear" }, list.Items.Cast<string>());
        }
    }
}
