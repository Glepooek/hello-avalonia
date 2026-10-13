using Avalonia.Controls;
using Avalonia.FundamentalsDemo.Views.Pages;
using Avalonia.Headless.XUnit;
using Avalonia.Tests.Common;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace Avalonia.FundamentalsDemo.Tests
{
    public class InteractionTests
    {
        [AvaloniaFact]
        public void Code_behind_page_asks_for_a_name_when_empty_and_greets_once_one_is_typed()
        {
            var window = UiHelpers.Show(new CodeBehindPage());
            var result = window.ByName<TextBlock>("ResultText");

            window.Click(window.ByName<Button>("GreetButton"));
            Assert.Equal("请先输入名字", result.Text);

            window.Type(window.ByName<TextBox>("NameBox"), "Al");
            window.Click(window.ByName<Button>("GreetButton"));
            Assert.Equal("你好，Al！", result.Text);
        }

        // Button.Content is not always a string (DemoHeader has an icon button), so compare with Equals instead of casting.
        [AvaloniaFact]
        public void Mvvm_page_enables_the_button_once_a_name_is_typed_and_greets()
        {
            var window = UiHelpers.Show(new MvvmPage());
            var greet = window.GetVisualDescendants().OfType<Button>().Single(b => "打招呼".Equals(b.Content));
            var box = window.GetVisualDescendants().OfType<TextBox>().Single();

            Assert.False(greet.IsEffectivelyEnabled);

            window.Type(box, "Al");
            Assert.True(greet.IsEffectivelyEnabled);

            window.Click(greet);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "你好，Al！");
        }

        [AvaloniaFact]
        public void Trees_page_snapshot_fills_both_tree_views()
        {
            var window = UiHelpers.Show(new TreesPage());
            var snapshot = window.GetVisualDescendants().OfType<Button>().Single(b => "生成快照".Equals(b.Content));

            window.Click(snapshot);

            Assert.Contains("快照已生成", window.ByName<TextBlock>("HintText").Text);
            var trees = window.GetVisualDescendants().OfType<TreeView>().ToList();
            Assert.Equal(2, trees.Count);
            Assert.All(trees, t => Assert.Single(t.ItemsSource!.Cast<object>()));
        }
    }
}
