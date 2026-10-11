using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class ResponsiveViewModel : ViewModelBase
    {
        // The slider drives the width of both boxes; the behaviors only ever see the resulting Bounds.
        [ObservableProperty]
        private double _panelWidth = 360;
    }
}
