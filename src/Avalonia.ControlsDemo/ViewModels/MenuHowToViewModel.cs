using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Common base of a real entry and the empty-list placeholder, so one style can bind to either.</summary>
    public abstract class RecentEntry
    {
    }

    public sealed class RecentFile : RecentEntry
    {
        public RecentFile(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public sealed partial class MenuHowToViewModel : ObservableObject
    {
        public ObservableCollection<RecentFile> RecentFiles { get; } = [new("报告.docx"), new("预算.xlsx"), new("方案.pptx")];

        public ObservableCollection<string> Items { get; } = ["苹果", "面包", "樱桃"];

        /// <summary>The recent list, or one disabled placeholder entry when it is empty.</summary>
        public IEnumerable<RecentEntry> RecentMenuItems => RecentFiles.Count > 0 ? RecentFiles : [new EmptyRecent()];

        [ObservableProperty]
        private bool _isBold;

        [ObservableProperty]
        private string _lastAction = "（无）";

        public MenuHowToViewModel()
        {
            RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(RecentMenuItems));
        }

        // The placeholder goes through the same command, but CanOpen is false for it, so its menu item is greyed out.
        [RelayCommand(CanExecute = nameof(CanOpen))]
        private void OpenRecent(RecentEntry? entry)
        {
            if (entry is RecentFile file)
            {
                LastAction = $"打开 {file.Name}";
            }
        }

        private static bool CanOpen(RecentEntry? entry) => entry is RecentFile;

        [RelayCommand]
        private void Save() => LastAction = "保存（Ctrl+S 或菜单都会走到这里）";

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete(string? item)
        {
            if (item is not null)
            {
                Items.Remove(item);
                LastAction = $"删除 {item}";
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanDelete(string? item) => item is not null && Items.Count > 1;

        [RelayCommand]
        private void ClearRecent() => RecentFiles.Clear();
    }

    /// <summary>Stands in for the list when it is empty; its template draws one disabled line.</summary>
    public sealed class EmptyRecent : RecentEntry
    {
    }
}
