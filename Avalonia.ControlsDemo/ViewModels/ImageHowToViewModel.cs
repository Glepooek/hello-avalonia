using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Avalonia.ControlsDemo.ViewModels
{
    public sealed partial class ImageHowToViewModel : ObservableObject
    {
        [ObservableProperty]
        private Bitmap? _photo;

        [ObservableProperty]
        private string _source = "（未加载）";

        /// <summary>Loads an embedded resource through its avares:// URI, decoding it down to 64 px wide.</summary>
        [RelayCommand]
        private void Load()
        {
            using var stream = AssetLoader.Open(new Uri("avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico"));
            // DecodeToWidth keeps only the decoded pixels needed for the target size, which saves memory for big photos.
            Photo = Bitmap.DecodeToWidth(stream, 64);
            Source = "avares://Avalonia.ControlsDemo/Assets/avalonia-logo.ico（DecodeToWidth 64）";
        }

        [RelayCommand]
        private void Clear()
        {
            Photo = null;
            Source = "（未加载）";
        }
    }
}
