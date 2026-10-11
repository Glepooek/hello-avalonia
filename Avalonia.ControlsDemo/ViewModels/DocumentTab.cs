using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>One closable tab in the dynamic TabControl.</summary>
    public partial class DocumentTab : ObservableObject
    {
        public DocumentTab(string title, StreamGeometry icon, string body)
        {
            Title = title;
            Icon = icon;
            _body = body;
        }

        public string Title { get; }

        public StreamGeometry Icon { get; }

        [ObservableProperty]
        private string _body;
    }
}
