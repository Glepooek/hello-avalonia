using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.DataTemplatesDemo.ViewModels
{
    /// <summary>
    /// Navigation without the view model knowing any view type: it only swaps
    /// which view model is current, and the ViewLocator picks the view.
    /// </summary>
    public partial class ViewLocatorViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboard = new();
        private readonly SettingsViewModel _settings = new();

        [ObservableProperty]
        private object _current;

        public ViewLocatorViewModel() => _current = _dashboard;

        [RelayCommand]
        private void ShowDashboard() => Current = _dashboard;

        [RelayCommand]
        private void ShowSettings() => Current = _settings;

        // No matching view exists for this one: shows the locator's fallback text.
        [RelayCommand]
        private void ShowMissing() => Current = new MissingViewModel();
    }

    public class MissingViewModel
    {
    }
}
