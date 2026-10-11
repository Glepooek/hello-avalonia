using System.Threading.Tasks;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class DataTriggerViewModel : ViewModelBase
    {
        [ObservableProperty] private bool _isOnline = true;
        [ObservableProperty] private double _score = 40;
        [ObservableProperty] private bool _agreed;
        [ObservableProperty] private bool _flag;
        [ObservableProperty] private int _timerTicks;
        [ObservableProperty] private int _doneCount;
        [ObservableProperty] private string _workStatus = "未开始";

        // TaskCompletedTrigger watches this property: every new Task assigned here is observed again.
        [ObservableProperty] private Task? _work;

        [RelayCommand]
        private void Tick() => TimerTicks++;

        [RelayCommand]
        private void StartWork()
        {
            WorkStatus = "运行中";
            Work = Task.Delay(600);
        }

        [RelayCommand]
        private void WorkDone()
        {
            DoneCount++;
            WorkStatus = "已完成";
        }
    }
}
