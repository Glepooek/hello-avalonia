using Avalonia.BehaviorsDemo.Services;
using Avalonia.Shared.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class NetworkViewModel : ViewModelBase
    {
        // Lives for the whole process: it is a background pool task on a loopback port, and the page can be revisited.
        private static readonly LoopbackServer Server = new();

        public string PingUrl { get; } = Server.BaseUrl + "ping";

        [ObservableProperty]
        private int _networkEvents;

        // NetworkInformationTrigger fires from a system thread, so hop to the UI thread before touching bindings.
        [RelayCommand]
        private void NetworkChanged() => Dispatcher.UIThread.Post(() => NetworkEvents++);
    }
}
