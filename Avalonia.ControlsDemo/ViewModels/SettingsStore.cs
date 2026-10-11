using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Reads and writes settings.json in a folder chosen by the caller.</summary>
    public class SettingsStore
    {
        // Relaxed escaping keeps Chinese text readable in the file instead of turning it into \uXXXX.
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public SettingsStore(string folder)
        {
            FolderPath = folder;
            FilePath = Path.Combine(folder, "settings.json");
        }

        /// <summary>Per-user, roaming: %AppData%\Avalonia.ControlsDemo on Windows.</summary>
        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.ControlsDemo");

        public string FolderPath { get; }

        public string FilePath { get; }

        /// <summary>True when the last Load found a file it could not parse and fell back to the defaults.</summary>
        public bool LastLoadFailed { get; private set; }

        public AppSettings Load()
        {
            LastLoadFailed = false;
            if (!File.Exists(FilePath)) return new AppSettings();

            try
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
            }
            catch (JsonException)
            {
                // A broken file must never stop the app from starting.
                LastLoadFailed = true;
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(FolderPath);

            // Write beside the target and swap, so a crash mid-write cannot leave half a file behind.
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
            File.Move(temp, FilePath, overwrite: true);
        }

        public void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        /// <summary>Writes text as-is, bypassing the serializer. Only the "corrupt file" demo uses it.</summary>
        public void WriteRaw(string text)
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, text);
        }
    }
}
