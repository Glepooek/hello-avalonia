using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Assets.Langs;
using Avalonia.MusicStore.Views;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.IO;
using System.Linq;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class SmokeTests
    {
        [AvaloniaFact]
        public void Main_window_without_a_view_model_shows_the_store_button_and_logs_nothing()
        {
            using var cwd = new TempCurrentDirectory();
            using var logs = new LogCollector();
            var window = new MainWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var button = Assert.Single(window.GetVisualDescendants().OfType<Button>());
                Assert.Equal("Music Store", ToolTip.GetTip(button));
                Assert.Empty(logs.Messages);
                // Without a view model the Loaded command never runs, so nothing touches the cache.
                Assert.False(Directory.Exists(Path.Combine(cwd.Root, "Cache")));
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaFact]
        public void Store_window_shows_the_search_box_and_buy_button_with_the_progress_bar_hidden()
        {
            using var logs = new LogCollector();
            var window = new MusicStoreWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var search = Assert.Single(window.GetVisualDescendants().OfType<TextBox>());
                Assert.Equal(Resources.txt_SearchAlbums, search.PlaceholderText);
                Assert.NotNull(window.ButtonWithText(Resources.btn_BuyAlbum));
                Assert.False(Assert.Single(window.GetVisualDescendants().OfType<ProgressBar>()).IsVisible);
                Assert.Empty(logs.Messages);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
