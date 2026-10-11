using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class CustomViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _fires;

        [RelayCommand]
        private void Fire() => Fires++;
    }
}
