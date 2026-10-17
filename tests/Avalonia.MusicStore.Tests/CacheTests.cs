using Avalonia.Headless.XUnit;
using Avalonia.MusicStore.Models;
using Avalonia.MusicStore.ViewModels;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.MusicStore.Tests
{
    public class CacheTests
    {
        [AvaloniaFact]
        public async Task Names_with_invalid_file_characters_round_trip()
        {
            using var cwd = new TempCurrentDirectory();
            await new AlbumViewModel(new Album("A/B", "C:D?", "u")).SaveToDiskAsync();

            Assert.True(File.Exists(cwd.Cache("A_B - C_D_")));
            var loaded = Assert.Single(await Album.LoadCachedAsync());
            Assert.Equal(("A/B", "C:D?", "u"), (loaded.Artist, loaded.Title, loaded.CoverUrl));
        }

        [AvaloniaFact]
        public async Task An_album_with_a_dot_in_its_name_loads_back()
        {
            using var cwd = new TempCurrentDirectory();
            await new AlbumViewModel(new Album("R.E.M.", "Monster", "u")).SaveToDiskAsync();
            await new AlbumViewModel(new Album("Mr. Big", "Lean Into It", "u")).SaveToDiskAsync();

            var artists = (await Album.LoadCachedAsync()).Select(a => a.Artist).OrderBy(a => a).ToArray();

            Assert.Equal(new[] { "Mr. Big", "R.E.M." }, artists);
        }

        [AvaloniaFact]
        public async Task Saving_shorter_data_over_an_album_leaves_a_readable_file()
        {
            using var cwd = new TempCurrentDirectory();
            await new AlbumViewModel(new Album("X", "Y", "a-very-long-cover-url-that-makes-the-json-longer")).SaveToDiskAsync();
            await new AlbumViewModel(new Album("X", "Y", "s")).SaveToDiskAsync();

            var loaded = Assert.Single(await Album.LoadCachedAsync());
            Assert.Equal("s", loaded.CoverUrl);
        }

        [AvaloniaFact]
        public async Task A_smaller_cover_overwrite_leaves_only_the_new_image()
        {
            using var cwd = new TempCurrentDirectory();
            cwd.WriteCover("X - Y.bmp", 20, 10);
            var album = new AlbumViewModel(new Album("X", "Y", "u"));
            await album.LoadCover();
            // An older, larger cover file is still on disk when the album is saved again.
            File.WriteAllBytes(cwd.Cache("X - Y.bmp"), new byte[1024 * 1024]);

            await album.SaveToDiskAsync();

            using var fresh = new MemoryStream();
            album.Cover!.Save(fresh);
            Assert.Equal(fresh.Length, new FileInfo(cwd.Cache("X - Y.bmp")).Length);
        }

        [AvaloniaFact]
        public async Task A_cover_saved_beside_the_album_loads_without_the_network()
        {
            using var cwd = new TempCurrentDirectory();
            cwd.WriteCover("Queen - Jazz.bmp", 20, 10);
            // The cover URL is unreachable on purpose: only the cached file can satisfy LoadCover.
            var album = new AlbumViewModel(new Album("Queen", "Jazz", "http://invalid.example/jazz.jpg"));

            await album.LoadCover();

            Assert.Equal(new PixelSize(400, 200), album.Cover!.PixelSize);
        }
    }
}
