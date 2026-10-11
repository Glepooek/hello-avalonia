using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    public abstract class NavPageViewModel : ObservableObject
    {
        protected NavPageViewModel(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }

    public class HomeNavViewModel : NavPageViewModel
    {
        public HomeNavViewModel() : base("首页")
        {
        }

        public string Welcome => "欢迎回来。每个页面是一个 ViewModel，DataTemplate 决定它长什么样。";
    }

    public partial class SettingsNavViewModel : NavPageViewModel
    {
        public SettingsNavViewModel() : base("设置")
        {
        }

        [ObservableProperty]
        private bool _notifyEnabled;

        [ObservableProperty]
        private double _volume = 50;
    }

    public class AboutNavViewModel : NavPageViewModel
    {
        public AboutNavViewModel() : base("关于")
        {
        }

        public string Version => "ControlsDemo · Avalonia 12.1.2";
    }
}
