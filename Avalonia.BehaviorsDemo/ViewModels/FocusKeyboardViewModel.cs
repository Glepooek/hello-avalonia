using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class FocusKeyboardViewModel : ViewModelBase
    {
        // Two-way target of FocusBehavior.IsFocused.
        [ObservableProperty] private bool _wantFocus;

        [ObservableProperty] private int _enterDirectCount;
        [ObservableProperty] private int _enterTunnelCount;
        [ObservableProperty] private int _saveCount;
        [ObservableProperty] private int _f5Count;
        [ObservableProperty] private int _pressDefaultCount;
        [ObservableProperty] private int _pressBubbleCount;
        [ObservableProperty] private int _tapCount;

        [RelayCommand] private void EnterDirect() => EnterDirectCount++;
        [RelayCommand] private void EnterTunnel() => EnterTunnelCount++;
        [RelayCommand] private void Save() => SaveCount++;
        [RelayCommand] private void F5() => F5Count++;
        [RelayCommand] private void PressDefault() => PressDefaultCount++;
        [RelayCommand] private void PressBubble() => PressBubbleCount++;
        [RelayCommand] private void Tap() => TapCount++;
    }
}
