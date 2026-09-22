using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.PropertySystemDemo.ViewModels
{
    public partial class PrecedenceViewModel : ViewModelBase
    {
        [ObservableProperty]
        private double _boundWidth = 220d;
    }
}
