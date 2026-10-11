namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>The shape of settings.json. Missing fields in an older file simply keep these defaults.</summary>
    public class AppSettings
    {
        public string Username { get; set; } = "";

        public double FontSize { get; set; } = 14;

        public bool NotifyEnabled { get; set; }
    }
}
