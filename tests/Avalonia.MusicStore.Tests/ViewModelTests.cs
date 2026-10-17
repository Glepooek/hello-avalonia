using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.ViewModels;
using Avalonia.Shared.Messages;
using Avalonia.Tests.Common;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class ViewModelTests
    {
        // Collects every MessageParam broadcast on the process-wide messenger while a test runs.
        // Not private: the toolkit's RegisterAll source generator emits code that must see the type.
        internal sealed class Recipient : IRecipient<MessageParam>
        {
            public List<MessageParam> Received { get; } = new();
            public void Receive(MessageParam message) => Received.Add(message);
        }

        [AvaloniaFact]
        public void Clearing_the_search_text_clears_results_without_a_search()
        {
            var vm = new MusicStoreViewModel();
            vm.SearchResults.Add(new AlbumViewModel(new Album("Queen", "Jazz", "u")));

            vm.SearchText = "";

            Assert.Empty(vm.SearchResults);
            Assert.False(vm.IsBusy);
        }

        [AvaloniaFact]
        public void Buy_without_a_selection_sends_nothing()
        {
            var vm = new MusicStoreViewModel();
            var recipient = new Recipient();
            WeakReferenceMessenger.Default.Register(recipient);
            try
            {
                vm.BuyMusicCommand.Execute(null);
                Assert.Empty(recipient.Received);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }

        [AvaloniaFact]
        public void Buy_with_a_selection_sends_the_selected_album()
        {
            var album = new AlbumViewModel(new Album("AC/DC", "Back in Black", "u"));
            var vm = new MusicStoreViewModel();
            vm.SearchResults.Add(album);
            vm.SelectedAlbum = album;
            var recipient = new Recipient();
            WeakReferenceMessenger.Default.Register(recipient);
            try
            {
                vm.BuyMusicCommand.Execute(null);
                Assert.Same(album, Assert.Single(recipient.Received).Data);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }

        [AvaloniaFact]
        public async Task Receiving_an_album_adds_it_once_and_saves_it_to_the_cache()
        {
            using var cwd = new TempCurrentDirectory();
            var vm = new MainWindowViewModel();
            try
            {
                var album = new AlbumViewModel(new Album("AC/DC", "Back in Black", "u"));
                WeakReferenceMessenger.Default.Send(new MessageParam { Data = album });
                WeakReferenceMessenger.Default.Send(new MessageParam { Data = album });

                Assert.Same(album, Assert.Single(vm.Albums));
                var file = cwd.Cache("AC_DC - Back in Black");
                await UiHelpers.WaitUntil(() => File.Exists(file) && new FileInfo(file).Length > 0);
                var saved = Assert.Single(await Album.LoadCachedAsync());
                Assert.Equal("AC/DC", saved.Artist);
                Assert.Equal("Back in Black", saved.Title);
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(vm);
            }
        }
    }
}
