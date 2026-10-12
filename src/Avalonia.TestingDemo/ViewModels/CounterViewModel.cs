using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.TestingDemo.ViewModels
{
    public partial class CounterViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DecrementCommand), nameof(ResetCommand))]
        private int _count;

        [RelayCommand]
        private void Increment() => Count++;

        [RelayCommand(CanExecute = nameof(HasCount))]
        private void Decrement() => Count--;

        [RelayCommand(CanExecute = nameof(HasCount))]
        private void Reset() => Count = 0;

        private bool HasCount() => Count > 0;
    }
}
