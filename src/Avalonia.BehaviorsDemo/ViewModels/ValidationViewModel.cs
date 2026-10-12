using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class ValidationViewModel : ViewModelBase
    {
        // The four properties below are written by TextBoxValidationBehavior (Mode=OneWayToSource).
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private bool _usernameValid;

        [ObservableProperty] private string? _usernameError;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private bool _emailValid;

        [ObservableProperty] private string? _emailError;

        [ObservableProperty] private string _submitResult = "";

        public bool CanSubmit => UsernameValid && EmailValid;

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private void Submit() => SubmitResult = "已提交：两个字段都通过了校验";
    }
}
