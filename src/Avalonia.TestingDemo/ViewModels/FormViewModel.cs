using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.TestingDemo.ViewModels
{
    public partial class FormViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NameError), nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private string _name = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AgeError), nameof(CanSubmit))]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        private string _age = "";

        [ObservableProperty]
        private string _status = "未提交";

        // Empty strings mean "no error"; a field that was never typed into starts invalid via CanSubmit.
        public string NameError => string.IsNullOrWhiteSpace(Name) ? "" : Name.Trim().Length < 2 ? "姓名至少 2 个字符" : "";

        public string AgeError
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Age))
                    return "";
                if (!int.TryParse(Age, out var age))
                    return "年龄必须是整数";
                return age is < 0 or > 150 ? "年龄必须在 0 到 150 之间" : "";
            }
        }

        public bool CanSubmit =>
            !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Age) && NameError == "" && AgeError == "";

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private void Submit() => Status = $"已提交：{Name.Trim()}，{Age} 岁";
    }
}
