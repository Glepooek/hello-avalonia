using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class CommandsViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DecrementCommand))]
        private int _count;

        [ObservableProperty]
        private string _lastAction = "（还没点过）";

        [RelayCommand]
        private void Increment() => Count++;

        // CanExecute is re-queried whenever Count changes, via the attribute on _count.
        private bool CanDecrement() => Count > 0;

        [RelayCommand(CanExecute = nameof(CanDecrement))]
        private void Decrement() => Count--;

        // The generated command reads the parameter from CommandParameter.
        [RelayCommand]
        private void Add(string step) => Count += int.Parse(step);

        // An async command disables its button while running and exposes IsRunning.
        [RelayCommand]
        private async Task SlowReset()
        {
            await Task.Delay(1500);
            Count = 0;
        }

        // Not a command at all: Avalonia can bind Command straight to a public method.
        public void SayHello(object? who) => LastAction = $"方法绑定被调用，参数 = {who}";
    }
}
