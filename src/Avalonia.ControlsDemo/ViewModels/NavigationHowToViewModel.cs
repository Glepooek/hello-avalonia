using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class NavigationHowToViewModel : ObservableObject
    {
        private readonly Stack<NavPageViewModel> _back = new();
        private readonly Stack<NavPageViewModel> _forward = new();

        public NavigationHowToViewModel()
        {
            _current = new HomeNavViewModel();
            _historyText = Describe();
        }

        [ObservableProperty]
        private NavPageViewModel _current;

        [ObservableProperty]
        private string _historyText;

        [RelayCommand]
        private void GoHome() => NavigateTo(new HomeNavViewModel());

        [RelayCommand]
        private void GoSettings() => NavigateTo(new SettingsNavViewModel());

        [RelayCommand]
        private void GoAbout() => NavigateTo(new AboutNavViewModel());

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void GoBack()
        {
            _forward.Push(Current);
            Current = _back.Pop();
            Refresh();
        }

        private bool CanGoBack() => _back.Count > 0;

        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void GoForward()
        {
            _back.Push(Current);
            Current = _forward.Pop();
            Refresh();
        }

        private bool CanGoForward() => _forward.Count > 0;

        private void NavigateTo(NavPageViewModel page)
        {
            // Going to the page you are already on would only fill the back stack with duplicates.
            if (page.GetType() == Current.GetType()) return;

            _back.Push(Current);
            _forward.Clear();
            Current = page;
            Refresh();
        }

        private void Refresh()
        {
            HistoryText = Describe();
            GoBackCommand.NotifyCanExecuteChanged();
            GoForwardCommand.NotifyCanExecuteChanged();
        }

        private string Describe() => $"后退栈 {_back.Count} 项，前进栈 {_forward.Count} 项";
    }
}
