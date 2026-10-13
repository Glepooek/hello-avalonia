using Avalonia.Controls;
using Avalonia.DataTemplatesDemo.ViewModels;
using Avalonia.DataTemplatesDemo.Views.Located;
using Avalonia.DataTemplatesDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.DataTemplatesDemo.Tests
{
    public class InteractionTests
    {
        private static List<string?> Texts(Visual root)
            => root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        private static Button ButtonWithText(Window window, string text)
            => window.GetVisualDescendants().OfType<Button>().Single(b => text.Equals(b.Content));

        [AvaloniaFact]
        public void Controls_render_themselves_but_arbitrary_objects_fall_back_to_to_string()
        {
            var window = UiHelpers.Show(new ControlContentPage());

            Assert.Equal(new[] { "我本身就是控件" }, Texts(window.ByName<ContentControl>("ControlContent")));
            Assert.Equal(new[] { "一段普通字符串" }, Texts(window.ByName<ContentControl>("StringContent")));
            Assert.Equal(new[] { "Avalonia.DataTemplatesDemo.Models.Circle" }, Texts(window.ByName<ContentControl>("ObjectContent")));
            Assert.Equal(new[] { "Avalonia.DataTemplatesDemo.Models.Circle" }, Texts(window.ByName<Button>("ObjectButton")));
            Assert.Equal(new[] { "现在由 ContentTemplate 决定怎么画。" }, Texts(window.ByName<ContentControl>("TemplatedContent")));
        }

        [AvaloniaFact]
        public void Inline_templates_draw_a_single_content_and_every_list_item()
        {
            var window = UiHelpers.Show(new ContentTemplatesPage());

            Assert.Equal(new[] { "半径 40" }, Texts(window.ByName<ContentControl>("SingleCircle")));
            Assert.Equal(new[] { "小", "中", "大" }, Texts(window.ByName<ListBox>("CircleList")));
        }

        [AvaloniaFact]
        public void Template_collection_picks_the_first_template_that_matches_so_order_matters()
        {
            var window = UiHelpers.Show(new TemplateCollectionPage());

            Assert.Equal(
                new[] { "● 圆，半径 3", "■ 方，边长 4", "（通用 Shape 模板）三角形" },
                Texts(window.ByName<ItemsControl>("SpecificFirst")));
            // The base-class template is listed first, so it swallows every subclass.
            Assert.Equal(
                new[] { "（通用 Shape 模板）圆", "（通用 Shape 模板）方", "（通用 Shape 模板）三角形" },
                Texts(window.ByName<ItemsControl>("BaseFirst")));
        }

        [AvaloniaFact]
        public void Selector_template_gives_each_person_the_template_for_their_sex()
        {
            var window = UiHelpers.Show(new SelectorPage());
            var list = window.ByName<ListBox>("PeopleList");

            var rows = list.GetVisualDescendants().OfType<StackPanel>()
                .Where(p => p.Classes.Contains("male") || p.Classes.Contains("female"))
                .ToList();

            Assert.Equal(new[] { "male", "female", "male" }, rows.Select(p => p.Classes.Contains("male") ? "male" : "female"));
            Assert.Equal(new[] { "1", "anyu", "Beijing" }, Texts(rows[0]));
        }

        [AvaloniaFact]
        public void Code_built_templates_render_and_a_match_predicate_filters_which_items_they_take()
        {
            var window = UiHelpers.Show(new CodeTemplatesPage());

            Assert.Equal(new[] { "代码模板：小", "代码模板：中", "代码模板：大" }, Texts(window.ByName<ListBox>("CodeList")));
            Assert.Equal(new[] { "兜底模板：小", "大圆模板：中", "大圆模板：大" }, Texts(window.ByName<ItemsControl>("FilteredList")));
        }

        [AvaloniaFact]
        public void One_template_resource_serves_content_list_and_combo_and_implicit_templates_work_anywhere_below()
        {
            var window = UiHelpers.Show(new ReusePage());

            Assert.Equal(new[] { "共享卡片：甲" }, Texts(window.ByName<ContentControl>("ReuseContent")));
            Assert.Equal(new[] { "共享卡片：甲", "共享卡片：乙" }, Texts(window.ByName<ListBox>("ReuseList")));
            Assert.Equal(new[] { "页面级隐式模板：边长 4" }, Texts(window.ByName<ContentControl>("ImplicitA")));
            Assert.Equal(new[] { "页面级隐式模板：边长 6" }, Texts(window.ByName<ContentControl>("ImplicitB")));
        }

        [AvaloniaFact]
        public void Control_template_and_data_template_are_independent_axes()
        {
            var window = UiHelpers.Show(new VersusControlTemplatePage());
            const string fallback = "Avalonia.DataTemplatesDemo.Models.Circle";

            Assert.Equal(new[] { fallback }, Texts(window.ByName<Button>("Neither")));
            Assert.Equal(new[] { "同一个圆" }, Texts(window.ByName<Button>("DataOnly")));
            Assert.Equal(new[] { fallback }, Texts(window.ByName<Button>("ControlOnly")));
            Assert.Equal(new[] { "同一个圆" }, Texts(window.ByName<Button>("Both")));
            // Only the buttons given the pill theme have the custom chrome.
            Assert.DoesNotContain(window.ByName<Button>("Neither").GetVisualDescendants().OfType<Border>(), b => b.Name == "Chrome");
            Assert.Contains(window.ByName<Button>("Both").GetVisualDescendants().OfType<Border>(), b => b.Name == "Chrome");
        }

        [AvaloniaFact]
        public void Default_panel_virtualizes_while_the_wrap_panel_realizes_every_item()
        {
            var window = UiHelpers.Show(new PanelsAndTreesPage());

            Assert.Equal(12, Texts(window.ByName<ListBox>("WrapPanelList")).Count);
            // Not pinned to an exact number: how many rows fit depends on the window height.
            Assert.True(Texts(window.ByName<ListBox>("DefaultPanelList")).Count < 12);
        }

        [AvaloniaFact]
        public void Tree_template_shows_children_after_a_node_is_expanded()
        {
            var window = UiHelpers.Show(new PanelsAndTreesPage());
            var tree = window.ByName<TreeView>("FolderTree");
            Assert.DoesNotContain("📁 工作", Texts(tree));

            ((TreeViewItem)tree.ContainerFromIndex(0)!).IsExpanded = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("📁 工作", Texts(tree));
            Assert.Contains("📁 个人", Texts(tree));
        }

        [AvaloniaFact]
        public void View_locator_page_swaps_the_view_with_the_view_model_and_shows_a_visible_miss()
        {
            var window = UiHelpers.Show(new ViewLocatorPage());
            var host = window.ByName<ContentControl>("Host");
            Assert.Single(host.GetVisualDescendants().OfType<DashboardView>());

            window.Click(ButtonWithText(window, "SettingsViewModel"));
            Assert.Single(host.GetVisualDescendants().OfType<SettingsView>());
            Assert.Empty(host.GetVisualDescendants().OfType<DashboardView>());

            window.Click(ButtonWithText(window, "MissingViewModel（没有对应视图）"));
            Assert.Equal(
                new[] { "找不到视图：Avalonia.DataTemplatesDemo.Views.Located.MissingView" },
                Texts(host));
        }

        [AvaloniaFact]
        public void View_locator_matches_only_view_models_and_builds_the_conventional_view()
        {
            var locator = new ViewLocator();

            Assert.True(locator.Match(new DashboardViewModel()));
            Assert.False(locator.Match("not a view model"));
            Assert.False(locator.Match(null));
            Assert.IsType<DashboardView>(locator.Build(new DashboardViewModel()));
            Assert.IsType<SettingsView>(locator.Build(new SettingsViewModel()));
        }

        [Fact]
        public void Dashboard_summary_and_settings_default_are_stable_strings()
        {
            Assert.Equal("今日待办 3 项，已完成 1 项", new DashboardViewModel().Summary);
            Assert.True(new SettingsViewModel().DarkMode);
        }
    }
}
