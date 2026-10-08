using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Avalonia.TestingDemo.ViewModels
{
    public partial class ListViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCommand))]
        private string _newItem = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        private string? _selected;

        public ListViewModel()
        {
            Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        }

        public string Summary => $"共 {Items.Count} 项";

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private void Add()
        {
            Items.Add(NewItem.Trim());
            NewItem = "";
        }

        private bool CanAdd() => !string.IsNullOrWhiteSpace(NewItem);

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove()
        {
            if (Selected is { } item)
                Items.Remove(item);
        }

        private bool CanRemove() => Selected is not null;
    }
}
