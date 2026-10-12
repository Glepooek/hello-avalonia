using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.FundamentalsDemo.ViewModels
{
    /// <summary>
    /// The view model knows nothing about the view: no control types, no
    /// event handlers. That is what makes the same state testable and
    /// reusable across views.
    /// </summary>
    public partial class MvvmViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GreetCommand))]
        private string _input = string.Empty;

        [ObservableProperty]
        private string _greeting = "（还没有问候）";

        private bool CanGreet() => !string.IsNullOrWhiteSpace(Input);

        [RelayCommand(CanExecute = nameof(CanGreet))]
        private void Greet()
        {
            Greeting = $"你好，{Input.Trim()}！";
        }
    }
}
