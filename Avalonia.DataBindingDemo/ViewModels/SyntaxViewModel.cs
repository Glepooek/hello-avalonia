using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class SyntaxViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _message = "改我试试";

        // Only ever written by the view (OneWayToSource); the page echoes it back for proof.
        [ObservableProperty]
        private string _lastTyped = string.Empty;
    }
}
