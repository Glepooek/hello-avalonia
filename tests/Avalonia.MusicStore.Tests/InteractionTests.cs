using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Assets.Langs;
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.ViewModels;
using Avalonia.MusicStore.Views;
using Avalonia.Tests.Common;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class InteractionTests
    {
        [AvaloniaFact]
        public async Task Main_window_loads_cached_albums_with_their_covers()
        {
            using var cwd = new TempCurrentDirectory();
            cwd.WriteCover("Queen - Jazz.bmp", 20, 10);
            File.WriteAllText(cwd.Cache("Queen - Jazz"),
                JsonSerializer.Serialize(new Album("Queen", "Jazz", "http://invalid.example/jazz.jpg")));
            var vm = new MainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            try
            {
                window.Show();
                await UiHelpers.WaitUntil(() => vm.Albums.Count == 1 && vm.Albums[0].Cover is not null);
                Dispatcher.UIThread.RunJobs();

                var view = Assert.Single(window.GetVisualDescendants().OfType<AlbumView>());
                var texts = view.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToArray();
                Assert.Contains("Queen", texts);
                Assert.Contains("Jazz", texts);
                Assert.Equal(new PixelSize(400, 200), vm.Albums[0].Cover!.PixelSize);
            }
            finally
            {
                window.Close();
                WeakReferenceMessenger.Default.UnregisterAll(vm);
            }
        }

        [AvaloniaFact]
        public async Task Buying_the_selected_album_closes_the_store_and_adds_it_to_the_main_window()
        {
            using var cwd = new TempCurrentDirectory();
            var vm = new MainWindowViewModel();
            var main = new MainWindow { DataContext = vm };
            try
            {
                main.Show();
                await UiHelpers.WaitUntil(() => Directory.Exists(Path.Combine(cwd.Root, "Cache")));
                var store = new MusicStoreWindow();
                store.Show(main);
                Dispatcher.UIThread.RunJobs();
                var storeVm = (MusicStoreViewModel)store.GetVisualDescendants().OfType<MusicStoreView>().Single().DataContext!;
                var buy = store.ButtonWithText(Resources.btn_BuyAlbum);

                store.Click(buy);
                Dispatcher.UIThread.RunJobs();
                Assert.True(store.IsVisible);
                Assert.Empty(vm.Albums);

                // Search needs the iTunes network, so the result is placed in the list directly.
                var album = new AlbumViewModel(new Album("AC/DC", "Back in Black", "http://invalid.example/bib.jpg"));
                storeVm.SearchResults.Add(album);
                storeVm.SelectedAlbum = album;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(0, store.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex);

                store.Click(buy);
                Dispatcher.UIThread.RunJobs();

                Assert.False(store.IsVisible);
                Assert.Same(album, Assert.Single(vm.Albums));
                Dispatcher.UIThread.RunJobs();
                Assert.Single(main.GetVisualDescendants().OfType<AlbumView>());
                await UiHelpers.WaitUntil(() => File.Exists(cwd.Cache("AC_DC - Back in Black")));
            }
            finally
            {
                main.Close();
                WeakReferenceMessenger.Default.UnregisterAll(vm);
            }
        }
    }
}
