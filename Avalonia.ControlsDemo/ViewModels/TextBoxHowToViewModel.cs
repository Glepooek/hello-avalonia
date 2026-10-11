using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Backs the validation block of the TextBox how-to: attributes on the field become rules on the property.</summary>
    public sealed partial class TextBoxHowToViewModel : ObservableValidator
    {
        public TextBoxHowToViewModel()
        {
            // Validation runs on every change; the label is refreshed whenever the error set changes.
            ErrorsChanged += (_, _) => OnPropertyChanged(nameof(ErrorText));
        }

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "用户名必填")]
        [MinLength(3, ErrorMessage = "至少 3 个字符")]
        private string _username = string.Empty;

        public string ErrorText => HasErrors
            ? string.Join("；", GetErrors(nameof(Username)).Select(e => e.ErrorMessage))
            : "校验通过（输入至少 3 个字符）";
    }
}
