namespace Avalonia.Shared.Messages
{
    /// <summary>
    /// Payload for WeakReferenceMessenger. Routed by type, so both the music store
    /// (which uses Data) and the WebView demo (which uses Reult) share this one class.
    /// </summary>
    public class MessageParam
    {
        public bool Reult { get; set; }
        public object Data { get; set; }
    }
}
