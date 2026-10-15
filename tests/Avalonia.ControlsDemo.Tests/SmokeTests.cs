using Avalonia.Controls;
using Avalonia.ControlsDemo.Navigation;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.ControlsDemo.Views;
using Avalonia.ControlsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
// Avalonia 12 ships its own CarouselPage (and NavigationPage, TabbedPage…); the demo page names collide with them.
using CarouselPage = Avalonia.ControlsDemo.Views.Pages.CarouselPage;

namespace Avalonia.ControlsDemo.Tests
{
    public class SmokeTests
    {
        private static (MainWindow Window, MainViewModel Vm) OpenShell()
        {
            var window = new MainWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, (MainViewModel)window.DataContext!);
        }

        private static IEnumerable<NavNode> PageNodes(MainViewModel vm) => vm.Nodes.SelectMany(n => n.Children);

        // The shell is not a TabControl, so TabWalker does not apply: select every page node through the view model,
        // the same path a click in NavTree takes, and record what each one did.
        private static List<(string Title, Type Expected, Type? Shown, string? Error)> WalkEveryPage(MainWindow window, MainViewModel vm)
        {
            var host = window.ByName<ContentControl>("PageHost");
            var visits = new List<(string, Type, Type?, string?)>();
            foreach (var node in PageNodes(vm).ToList())
            {
                string? error = null;
                try
                {
                    vm.SelectedNode = node;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Dispatcher.UIThread.RunJobs();
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                }
                visits.Add((node.DisplayTitle, node.Entry!.PageType, host.Content?.GetType(), error));
            }
            return visits;
        }

        [AvaloniaFact]
        public void Catalog_registers_119_pages_by_kind()
        {
            Assert.Equal(119, PageCatalog.All.Count);
            Assert.Equal(80, PageCatalog.All.Count(e => e.Kind == PageKind.Control));
            Assert.Equal(19, PageCatalog.All.Count(e => e.Kind == PageKind.HowTo));
            Assert.Equal(20, PageCatalog.All.Count(e => e.Kind == PageKind.Signpost));
        }

        // The README's table is the contract; categories appear in registration order.
        [AvaloniaFact]
        public void Catalog_counts_pages_per_category_in_registration_order()
        {
            Assert.Equal(
                new[]
                {
                    ("Data display", 16), ("Feedback", 5), ("Input", 26), ("Layout", 23), ("Media", 4), ("Menus", 6),
                    ("Navigation", 10), ("Primitives", 7), ("System", 2), ("Web", 4), ("付费控件", 7), ("How-to 路标", 9),
                },
                PageCatalog.All.GroupBy(e => e.Category).Select(g => (g.Key, g.Count())));
        }

        [AvaloniaFact]
        public void Every_page_type_is_registered_once_and_every_entry_has_a_doc_path()
        {
            Assert.Empty(PageCatalog.All.GroupBy(e => e.PageType).Where(g => g.Count() > 1).Select(g => g.Key.Name));
            Assert.Empty(PageCatalog.All.Where(e => string.IsNullOrEmpty(e.DocPath)).Select(e => e.Title));
        }

        // Seventeen titles exist twice (a control page and its How-to page); the prefix is what tells them apart.
        [AvaloniaFact]
        public void Navigation_titles_are_unique_and_prefixed_by_kind()
        {
            var (_, vm) = OpenShell();
            var nodes = PageNodes(vm).ToList();

            Assert.Equal(119, nodes.Count);
            Assert.Empty(nodes.GroupBy(n => n.DisplayTitle).Where(g => g.Count() > 1).Select(g => g.Key));
            Assert.All(nodes, n =>
            {
                Assert.Equal(n.Entry!.Kind == PageKind.HowTo, n.DisplayTitle.StartsWith("实战："));
                Assert.Equal(n.Entry.Kind == PageKind.Signpost, n.DisplayTitle.StartsWith("↗ "));
            });
            Assert.Equal(PageCatalog.All.Select(e => e.Category).Distinct(), vm.Nodes.Select(n => n.DisplayTitle));
            Assert.All(vm.Nodes, n => Assert.Null(n.Entry));
        }

        [AvaloniaFact]
        public void Main_window_opens_on_carousel_with_only_the_first_category_expanded()
        {
            var (window, vm) = OpenShell();

            Assert.Equal("Carousel", vm.SelectedNode?.DisplayTitle);
            Assert.IsType<CarouselPage>(window.ByName<ContentControl>("PageHost").Content);
            Assert.Same(vm.SelectedNode, window.ByName<TreeView>("NavTree").SelectedItem);
            Assert.Equal(new[] { true, false, false, false, false, false, false, false, false, false, false, false },
                vm.Nodes.Select(n => n.IsExpanded));
        }

        [AvaloniaFact]
        public void Every_navigation_node_shows_its_page_without_errors()
        {
            var (window, vm) = OpenShell();
            try
            {
                var visits = WalkEveryPage(window, vm);

                Assert.Equal(119, visits.Count);
                Assert.Empty(visits.Where(v => v.Error != null).Select(v => $"{v.Title}: {v.Error}"));
                Assert.Empty(visits.Where(v => v.Shown != v.Expected).Select(v => v.Title));
            }
            finally
            {
                // Window and Dialogs pages open child windows; closing the shell closes them too.
                window.Close();
            }
        }

        // Catches runtime binding errors on any page; compiled bindings to struct properties are not visible here.
        [AvaloniaFact]
        public void Walking_every_page_logs_no_warnings()
        {
            using var logs = new LogCollector();
            var (window, vm) = OpenShell();
            try
            {
                WalkEveryPage(window, vm);

                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Selecting_a_category_clears_the_page_and_reselecting_builds_a_fresh_one()
        {
            var (_, vm) = OpenShell();
            var carousel = vm.SelectedNode!;
            var first = vm.CurrentPage;

            vm.SelectedNode = vm.Nodes[1];
            Assert.Null(vm.CurrentPage);

            vm.SelectedNode = carousel;
            Assert.IsType<CarouselPage>(vm.CurrentPage);
            Assert.NotSame(first, vm.CurrentPage);
        }

        [AvaloniaFact]
        public void Clicking_a_tree_item_selects_its_page()
        {
            var (window, vm) = OpenShell();
            var input = vm.Nodes.Single(n => n.DisplayTitle == "Input");
            input.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var item = window.ByName<TreeView>("NavTree").GetVisualDescendants().OfType<TreeViewItem>()
                .Single(i => i.DataContext == input.Children[0]);

            window.Click(item);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Button", vm.SelectedNode?.DisplayTitle);
            Assert.IsType<ButtonPage>(window.ByName<ContentControl>("PageHost").Content);
        }
    }
}
