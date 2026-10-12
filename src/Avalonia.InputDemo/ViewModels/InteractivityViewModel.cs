using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Shared.ViewModels;

namespace Avalonia.InputDemo.ViewModels
{
    public partial class InteractivityViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _count;

        // The command's CanExecute follows this flag; a bound Button disables itself automatically.
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(IncrementCommand))]
        private bool _unlocked;

        [RelayCommand(CanExecute = nameof(Unlocked))]
        private void Increment() => Count++;
    }
}
