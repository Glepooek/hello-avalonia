using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class DebuggingViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Contact? _selected;

        [RelayCommand]
        private void Toggle() => Selected = Selected is null ? new Contact("张三", "北京", 28) : null;
    }
}
