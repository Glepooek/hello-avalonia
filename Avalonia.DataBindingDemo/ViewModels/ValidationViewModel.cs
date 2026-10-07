using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;

namespace Avalonia.DataBindingDemo.ViewModels
{
    /// <summary>
    /// Derives from ObservableValidator rather than ViewModelBase: the validator
    /// base class is what implements INotifyDataErrorInfo, which is the channel
    /// Avalonia reads errors from.
    /// </summary>
    public partial class ValidationViewModel : ObservableValidator
    {
        // 1. DataAnnotations, surfaced through INotifyDataErrorInfo.
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        [Required(ErrorMessage = "用户名必填")]
        [MinLength(3, ErrorMessage = "用户名至少 3 个字符")]
        private string _userName = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        [EmailAddress(ErrorMessage = "邮箱格式不对")]
        private string _email = "someone@example.com";

        [ObservableProperty]
        private string _submitResult = string.Empty;

        // 2. Exception-based: the setter refuses the value and the binding shows the message.
        private int _age = 18;

        public int Age
        {
            get => _age;
            set
            {
                if (value is < 0 or > 150)
                {
                    // DataValidationException shows its bare message; any other exception
                    // type is shown with its type name in front (see rule 15).
                    throw new DataValidationException("年龄必须在 0 到 150 之间");
                }

                SetProperty(ref _age, value);
            }
        }

        private bool CanSubmit() => !HasErrors && !string.IsNullOrEmpty(UserName);

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private void Submit() => SubmitResult = $"已提交：{UserName} / {Email} / {Age} 岁";
    }
}
