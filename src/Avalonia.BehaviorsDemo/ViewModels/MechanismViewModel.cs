using Avalonia.Shared.Helpers;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class MechanismViewModel : ViewModelBase
    {
        public EventLog Log { get; } = new();

        // Bound to the trigger's own IsEnabled: a behavior is an object, so it can be switched like one.
        [ObservableProperty]
        private bool _behaviorEnabled = true;

        [ObservableProperty]
        private int _runs;

        [RelayCommand]
        private void Step(string? text) => Log.Write(text ?? "?");

        [RelayCommand]
        private void Finish()
        {
            Runs++;
            Log.Write($"-- 第 {Runs} 次触发结束 --");
        }

        [RelayCommand]
        private void ClearLog() => Log.Clear();
    }
}
