using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.Views;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.Messages;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.MusicStore.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IRecipient<MessageParam>
    {
        #region Properties

        public ObservableCollection<AlbumViewModel> Albums { get; } = new();

        #endregion

        #region Commands

        public RelayCommand LoadedCommand { get; private set; }
        public RelayCommand ShowAlbumsCommand { get; private set; }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            LoadedCommand = new RelayCommand(LoadAlbums);

            ShowAlbumsCommand = new RelayCommand(() =>
            {
                MusicStoreWindow dialog = new MusicStoreWindow();
                dialog.ShowDialog(AvaloniaHelper.GetMainWindow());
            });

            WeakReferenceMessenger.Default.Register<MessageParam>(this);
        }

        #endregion

        #region Methods

        public async void Receive(MessageParam message)
        {
            if (message?.Data is AlbumViewModel albumVM
                && !Albums.Contains(albumVM))
            {
                Albums.Add(albumVM);
                await albumVM.SaveToDiskAsync();
            }
        }

        public async void LoadAlbums()
        {
            var albums = (await Album.LoadCachedAsync()).Select(x => new AlbumViewModel(x));

            foreach (var album in albums)
            {
                Albums.Add(album);
            }

            foreach (var album in Albums.ToList())
            {
                await album.LoadCover();
            }
        }

        #endregion
    }
}
