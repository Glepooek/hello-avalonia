using Avalonia.Controls;
using Avalonia.DataBindingDemo.Models;
using Avalonia.DataBindingDemo.ViewModels;
using Avalonia.DataBindingDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Avalonia.DataBindingDemo.Tests
{
    public class InteractionTests
    {
        // UiHelpers.Type inserts at the caret, so clear the box first to get replace semantics.
        private static void Replace(Window window, TextBox box, string text)
        {
            box.Text = "";
            window.Type(box, text);
        }

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaFact]
        public void Commands_page_counts_and_enables_decrement_only_above_zero()
        {
            var window = UiHelpers.Show(new CommandsPage());
            var decrement = window.ByName<Button>("DecrementButton");
            Assert.False(decrement.IsEffectivelyEnabled);

            window.Click(ButtonWithText(window, "+1"));
            Assert.Equal("Count = 1", window.ByName<TextBlock>("CountText").Text);
            Assert.True(decrement.IsEffectivelyEnabled);

            window.Click(decrement);
            Assert.Equal("Count = 0", window.ByName<TextBlock>("CountText").Text);
            Assert.False(decrement.IsEffectivelyEnabled);
        }

        [AvaloniaFact]
        public void Commands_page_passes_the_command_parameter_and_binds_to_a_method()
        {
            var window = UiHelpers.Show(new CommandsPage());

            window.Click(ButtonWithText(window, "+10"));
            window.Click(window.ByName<Button>("MethodButton"));

            Assert.Equal("Count = 10", window.ByName<TextBlock>("CountText").Text);
            Assert.Equal("方法绑定被调用，参数 = 小明", window.ByName<TextBlock>("LastActionText").Text);
        }

        [AvaloniaFact]
        public void Validation_page_enables_submit_only_for_a_valid_user_name()
        {
            var page = new ValidationPage();
            var window = UiHelpers.Show(page);
            var submit = window.ByName<Button>("SubmitButton");
            Assert.False(submit.IsEffectivelyEnabled);

            Replace(window, window.ByName<TextBox>("UserNameBox"), "ab");
            Assert.False(submit.IsEffectivelyEnabled);

            Replace(window, window.ByName<TextBox>("UserNameBox"), "abc");
            Assert.True(submit.IsEffectivelyEnabled);

            // The submit button sits below the fold of the 600x480 test window; Click scrolls it into view.
            window.Click(submit);
            Assert.Equal("已提交：abc / someone@example.com / 18 岁", ((ValidationViewModel)page.DataContext!).SubmitResult);
        }

        [AvaloniaFact]
        public void Validation_page_keeps_the_old_age_when_the_setter_rejects_a_value()
        {
            var page = new ValidationPage();
            var window = UiHelpers.Show(page);

            Replace(window, window.ByName<TextBox>("AgeBox"), "200");

            Assert.Equal(18, ((ValidationViewModel)page.DataContext!).Age);
            Assert.True(DataValidationErrors.GetHasErrors(window.ByName<TextBox>("AgeBox")));
        }

        [AvaloniaFact]
        public void Converters_page_converts_celsius_to_fahrenheit_and_back()
        {
            var window = UiHelpers.Show(new ConvertersPage());
            var celsius = window.ByName<Slider>("Celsius");
            var fahrenheit = window.ByName<TextBox>("Fahrenheit");

            celsius.Value = 100;
            // The binding formats with the current culture, so build the expectation the same way.
            Assert.Equal(212d.ToString("F1", CultureInfo.CurrentCulture), fahrenheit.Text);
            Assert.Equal("🔥 炎热（100 °C）", window.ByName<TextBlock>("FuncText").Text);

            Replace(window, fahrenheit, 32d.ToString("F1", CultureInfo.CurrentCulture));
            Assert.Equal(0, celsius.Value, 6);

            // Half-typed input is ignored by the converter instead of resetting the source.
            Replace(window, fahrenheit, "-");
            Assert.Equal(0, celsius.Value, 6);
        }

        [AvaloniaFact]
        public void Converters_page_toggles_hints_with_the_built_in_string_converters()
        {
            var window = UiHelpers.Show(new ConvertersPage());
            Assert.True(window.ByName<TextBlock>("EmptyWarning").IsVisible);
            Assert.False(window.ByName<TextBlock>("NotEmptyNote").IsVisible);

            Replace(window, window.ByName<TextBox>("Input"), "x");

            Assert.False(window.ByName<TextBlock>("EmptyWarning").IsVisible);
            Assert.True(window.ByName<TextBlock>("NotEmptyNote").IsVisible);
        }

        [AvaloniaFact]
        public void Collections_page_shows_the_new_row_only_in_the_observable_list()
        {
            var window = UiHelpers.Show(new CollectionsPage());
            Assert.Equal("共 3 人", window.ByName<TextBlock>("LiveCount").Text);

            window.Click(ButtonWithText(window, "两边各加一人"));

            Assert.Equal("共 4 人", window.ByName<TextBlock>("LiveCount").Text);
            Assert.Equal(4, window.ByName<ListBox>("LiveList").ItemCount);
            // The plain List raised no change notification: the bound count and the rows stay at 3.
            Assert.Equal("共 3 人", window.ByName<TextBlock>("FrozenCount").Text);
            Assert.Equal(3, window.ByName<ListBox>("FrozenList").ItemCount);
        }

        [AvaloniaFact]
        public void Master_detail_page_reveals_the_detail_panel_and_edits_write_back()
        {
            var window = UiHelpers.Show(new MasterDetailPage());
            Assert.True(window.ByName<TextBlock>("EmptyHint").IsVisible);
            Assert.False(window.ByName<StackPanel>("DetailPanel").IsVisible);

            var list = window.ByName<ListBox>("MasterList");
            list.SelectedIndex = 0;

            Assert.False(window.ByName<TextBlock>("EmptyHint").IsVisible);
            Assert.True(window.ByName<StackPanel>("DetailPanel").IsVisible);
            Assert.Equal("张三", window.ByName<TextBox>("DetailName").Text);

            Replace(window, window.ByName<TextBox>("DetailName"), "张三丰");
            Assert.Equal("张三丰", ((Contact)list.SelectedItem!).Name);
        }

        [AvaloniaFact]
        public void Collection_views_page_filters_rows_as_the_filter_text_is_typed()
        {
            var window = UiHelpers.Show(new CollectionViewsPage());
            var rows = window.ByName<ListBox>("RowList");
            Assert.Equal(6, rows.ItemCount);

            Replace(window, window.ByName<TextBox>("FilterBox"), "张");

            Assert.Equal(1, rows.ItemCount);
        }
    }
}
