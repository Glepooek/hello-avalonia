using Avalonia.Controls;
using Avalonia.MusicStore.Models;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.Messages;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Avalonia.MusicStore.ViewModels
{
    public class MusicStoreViewModel : ViewModelBase
    {
        #region Fields
        private CancellationTokenSource? _cancellationTokenSource;
        #endregion

        #region Properties

        private string? _searchText;
        private bool _isBusy;

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DoSearch(value);
                }
                else
                {
                    SearchResults.Clear();
                }
                SetProperty(ref _searchText, value);
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ObservableCollection<AlbumViewModel> SearchResults { get; } = new();

        private AlbumViewModel? _selectedAlbum;
        public AlbumViewModel? SelectedAlbum
        {
            get => _selectedAlbum;
            set => SetProperty(ref _selectedAlbum, value);
        }

        #endregion

        #region Commands

        public RelayCommand<UserControl> BuyMusicCommand { get; }

        #endregion

        #region Constructor

        public MusicStoreViewModel()
        {
            BuyMusicCommand = new RelayCommand<UserControl>((control) =>
            {
                if (_selectedAlbum == null)
                {
                    return;
                }
                AvaloniaHelper.GetTopLevel(control)?.Close();
                WeakReferenceMessenger.Default.Send<MessageParam>(new MessageParam { Data = _selectedAlbum });
            });
        }

        #endregion

        #region Methods

        private async void DoSearch(string s)
        {
            IsBusy = true;
            SearchResults.Clear();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            if (!string.IsNullOrWhiteSpace(s))
            {
                var albums = await Album.SearchAsync(s);

                foreach (var album in albums)
                {
                    var vm = new AlbumViewModel(album);
                    SearchResults.Add(vm);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    LoadCovers(cancellationToken);
                }
            }

            IsBusy = false;
        }

        private async void LoadCovers(CancellationToken cancellationToken)
        {
            foreach (var album in SearchResults.ToList())
            {
                await album.LoadCover();

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        #endregion
    }
}
