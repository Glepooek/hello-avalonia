using Avalonia.MusicStore.Models;
using Avalonia.Media.Imaging;
using Avalonia.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.MusicStore.ViewModels
{
    public class AlbumViewModel : ViewModelBase
    {
        #region Fields
        private readonly Album _album;

        #endregion

        #region Properties
        public string Artist => _album.Artist;
        public string Title => _album.Title;

        private Bitmap? _cover;
        public Bitmap? Cover
        {
            get => _cover;
            private set => SetProperty(ref _cover, value);
        }
        #endregion

        #region Costructor
        public AlbumViewModel(Album album)
        {
            _album = album;
        }
        #endregion

        #region Methods
        public async Task LoadCover()
        {
            await using var imageStream = await _album.LoadCoverBitmapAsync();
            Cover = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }

        public async Task SaveToDiskAsync()
        {
            await _album.SaveAsync();

            if (Cover != null)
            {
                var bitmap = Cover;

                await Task.Run(() =>
                {
                    using var fs = _album.SaveCoverBitmapStream();
                    bitmap.Save(fs);
                });
            }
        }
        #endregion
    }
}
