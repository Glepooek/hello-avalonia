using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class ClipboardViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _input = "来自 SetClipboardTextAction 的文字";

        [ObservableProperty]
        private string _pasted = "（还没读过）";

        // GetClipboardTextAction passes the clipboard text as the command parameter.
        [RelayCommand]
        private void Paste(string? text) => Pasted = text ?? "";
    }
}
