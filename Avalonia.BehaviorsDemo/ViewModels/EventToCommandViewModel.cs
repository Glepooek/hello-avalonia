using System;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class EventToCommandViewModel : ViewModelBase
    {
        public EventLog Log { get; } = new();

        [ObservableProperty] private int _total;
        [ObservableProperty] private string _lastArgs = "（还没点过）";
        [ObservableProperty] private int _helloCount;
        [ObservableProperty] private int _sourceHits;
        [ObservableProperty] private int _throttleAttempts;
        [ObservableProperty] private int _throttleRuns;
        [ObservableProperty] private int _debounceAttempts;
        [ObservableProperty] private int _debounceRuns;

        // CommandParameter arrives as the literal string from XAML.
        [RelayCommand]
        private void Add(string? amount) => Total += int.Parse(amount ?? "0");

        [RelayCommand]
        private void Inspect(object? args) => LastArgs = args?.GetType().Name ?? "null";

        // CallMethodAction needs a public, parameterless method on the target object.
        public void Hello() => HelloCount++;

        [RelayCommand] private void SourceHit() => SourceHits++;
        [RelayCommand] private void ThrottleAttempt() => ThrottleAttempts++;
        [RelayCommand] private void ThrottleRun() => ThrottleRuns++;
        [RelayCommand] private void DebounceAttempt() => DebounceAttempts++;
        [RelayCommand] private void DebounceRun() => DebounceRuns++;

        [RelayCommand]
        private void Mark(string? text) => Log.Write($"{DateTime.Now:HH:mm:ss.fff}  {text}");

        [RelayCommand]
        private void ClearLog() => Log.Clear();
    }
}
