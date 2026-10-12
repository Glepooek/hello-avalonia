using System.Collections.ObjectModel;

namespace Avalonia.Shared.Helpers
{
    /// <summary>
    /// An ordered trail of what fired, shared by the event-heavy demo pages.
    /// A page owns one instance, calls <see cref="Write"/> from its handlers
    /// and binds a ListBox to <see cref="Entries"/>.
    /// </summary>
    public class EventLog
    {
        // Long enough to show a nested routing sequence, short enough to read on screen.
        private const int MaxEntries = 60;

        public ObservableCollection<string> Entries { get; } = new();

        public void Write(string message)
        {
            // Sequence numbers make the order unmistakable when several events
            // land in the same frame and the ListBox renders them together.
            Entries.Add($"{Entries.Count + 1:00}  {message}");
            while (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }

        public void Clear() => Entries.Clear();
    }
}
