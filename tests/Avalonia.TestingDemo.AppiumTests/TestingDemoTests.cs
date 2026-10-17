using System.Diagnostics;
using System.Linq;
using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.TestingDemo.AppiumTests
{
    // Batch 7 baseline: TestingDemo has an AutomationId on every control it tests, so these tests show the shape
    // every later Appium test follows. Each test starts its own app, so no state carries from one test to the next.
    public class TestingDemoTests
    {
        private const string App = "Avalonia.TestingDemo";

        [AppiumFact(App)]
        public void App_starts_with_its_main_window_and_three_tabs()
        {
            using var app = new AppSession(App);

            Assert.Equal("Avalonia Testing Demo", app.Driver.Title);
            Assert.Single(app.Driver.WindowHandles);
            Assert.Equal(new[] { "计数器", "表单", "列表" }, app.Driver.FindElementsByTagName("TabItem").Select(t => t.Text));
        }

        [AppiumFact(App)]
        public void Every_tab_can_be_selected_and_shows_its_panel()
        {
            using var app = new AppSession(App);

            foreach (var (header, control) in new[] { ("表单", "name-box"), ("列表", "items-list"), ("计数器", "count-text") })
            {
                var tab = app.ByName(header);
                tab.Click();
                AppSession.WaitFor(() => tab.Selected, $"tab {header} is selected");
                AppSession.WaitFor(() => app.ById(control).Displayed, $"{control} is shown on tab {header}");
            }
        }

        [AppiumFact(App)]
        public void Closing_the_session_ends_the_app()
        {
            var before = Process.GetProcessesByName(App).Select(p => p.Id).ToHashSet();
            var app = new AppSession(App);
            _ = app.Driver.Title;
            var started = Assert.Single(Process.GetProcessesByName(App), p => !before.Contains(p.Id));

            app.Dispose();

            // Dispose kills an app that outlives Quit; this test is about Quit, so the kill must not have been needed.
            Assert.False(app.KilledAfterQuit);
            AppSession.WaitFor(() => started.HasExited, $"process {started.Id} has exited");
        }

        [AppiumFact(App)]
        public void Counter_buttons_change_the_count_and_which_buttons_are_enabled()
        {
            using var app = new AppSession(App);
            Assert.Equal("0", app.ById("count-text").Text);
            Assert.False(app.ById("decrement-btn").Enabled);
            Assert.False(app.ById("reset-btn").Enabled);

            app.ById("increment-btn").Click();
            app.ById("increment-btn").Click();
            app.WaitForText("count-text", "2");
            AppSession.WaitFor(() => app.ById("decrement-btn").Enabled, "decrement is enabled above zero");

            app.ById("decrement-btn").Click();
            app.WaitForText("count-text", "1");

            app.ById("reset-btn").Click();
            app.WaitForText("count-text", "0");
            AppSession.WaitFor(() => !app.ById("reset-btn").Enabled, "reset is disabled at zero");
        }

        [AppiumFact(App)]
        public void Form_checks_typed_input_and_submits_once_both_fields_are_valid()
        {
            using var app = new AppSession(App);
            app.ByName("表单").Click();
            Assert.False(app.ById("submit-btn").Enabled);

            app.TypeInto(app.ById("name-box"), "张");
            app.WaitForText("name-error", "姓名至少 2 个字符");
            app.TypeInto(app.ById("name-box"), "三");
            app.WaitForText("name-error", "");

            var age = app.ById("age-box");
            app.TypeInto(age, "abc");
            app.WaitForText("age-error", "年龄必须是整数");
            age.Clear();
            app.TypeInto(age, "30");
            app.WaitForText("age-error", "");
            AppSession.WaitFor(() => app.ById("submit-btn").Enabled, "submit is enabled with valid input");

            app.ById("submit-btn").Click();
            app.WaitForText("status-text", "已提交：张三，30 岁");
        }

        [AppiumFact(App)]
        public void List_adds_typed_items_and_removes_the_selected_one()
        {
            using var app = new AppSession(App);
            app.ByName("列表").Click();
            app.WaitForText("summary-text", "共 0 项");
            Assert.False(app.ById("add-btn").Enabled);

            foreach (var item in new[] { "apple", "pear" })
            {
                app.TypeInto(app.ById("new-item-box"), item);
                AppSession.WaitFor(() => app.ById("add-btn").Enabled, $"add is enabled after typing {item}");
                app.ById("add-btn").Click();
                app.WaitForText("new-item-box", "");
            }
            app.WaitForText("summary-text", "共 2 项");
            var list = app.ById("items-list");
            Assert.Equal(new[] { "apple", "pear" }, list.FindElementsByTagName("ListItem").Select(i => i.Text));

            list.FindElementByName("apple").Click();
            AppSession.WaitFor(() => app.ById("remove-btn").Enabled, "remove is enabled with a selection");
            app.ById("remove-btn").Click();

            app.WaitForText("summary-text", "共 1 项");
            AppSession.WaitFor(() => list.FindElementsByName("apple").Count == 0, "apple is gone from the list");
            AppSession.WaitFor(() => !app.ById("remove-btn").Enabled, "remove is disabled without a selection");
        }
    }
}
