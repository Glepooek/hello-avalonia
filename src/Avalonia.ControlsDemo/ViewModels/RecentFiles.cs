using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A most-recently-used list of paths, newest first, capped at <see cref="Capacity"/> and kept in recent.json.</summary>
    public class RecentFiles
    {
        public const int Capacity = 10;

        private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        // Linux file systems are case-sensitive; Windows and (by default) macOS are not.
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        public RecentFiles(string folder)
        {
            FolderPath = folder;
            FilePath = Path.Combine(folder, "recent.json");
            Load();
        }

        /// <summary>Per-machine, not roaming: %LocalAppData%\Avalonia.ControlsDemo on Windows.</summary>
        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Avalonia.ControlsDemo");

        public string FolderPath { get; }

        public string FilePath { get; }

        public ObservableCollection<string> Items { get; } = new();

        public void Add(string path)
        {
            // Opening a file that is already listed moves it to the top instead of listing it twice.
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Items[i], path, PathComparison)) Items.RemoveAt(i);
            }

            Items.Insert(0, path);
            while (Items.Count > Capacity) Items.RemoveAt(Items.Count - 1);
            Save();
        }

        public void Clear()
        {
            Items.Clear();
            Save();
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;

            try
            {
                var saved = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(FilePath), Options);
                if (saved is null) return;
                foreach (string path in saved)
                {
                    if (Items.Count < Capacity) Items.Add(path);
                }
            }
            catch (JsonException)
            {
                // An unreadable list is just an empty list.
            }
        }

        private void Save()
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Items, Options));
        }
    }
}
