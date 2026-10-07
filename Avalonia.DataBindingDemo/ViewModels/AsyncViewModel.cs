using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Shared.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class AsyncViewModel : ViewModelBase
    {
        // Replaced (not mutated) on reload, so the ^ binding re-subscribes to the new task.
        // Until the new task finishes, the target keeps the previous result.
        [ObservableProperty]
        private Task<string> _greeting = LoadGreetingAsync();

        public IObservable<string> Clock { get; } = new ClockObservable();

        // A Bitmap is just a property value: load it once, bind Image.Source to it.
        public Bitmap Logo { get; } = new(AssetLoader.Open(new Uri("avares://Avalonia.DataBindingDemo/Assets/avalonia-logo.ico")));

        [RelayCommand]
        private void Reload() => Greeting = LoadGreetingAsync();

        private static async Task<string> LoadGreetingAsync()
        {
            await Task.Delay(2000);
            return $"加载完成于 {DateTime.Now:HH:mm:ss}";
        }

        /// <summary>
        /// A minimal IObservable without pulling in System.Reactive: it pushes the
        /// current time every second for as long as someone is subscribed.
        /// </summary>
        private sealed class ClockObservable : IObservable<string>
        {
            public IDisposable Subscribe(IObserver<string> observer)
            {
                observer.OnNext(DateTime.Now.ToString("HH:mm:ss"));
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (_, _) => observer.OnNext(DateTime.Now.ToString("HH:mm:ss"));
                timer.Start();
                return new Stopper(timer);
            }

            private sealed class Stopper(DispatcherTimer timer) : IDisposable
            {
                // Called when the binding drops its subscription, which happens when the
                // DataContext changes — not merely when the page leaves the visual tree.
                public void Dispose() => timer.Stop();
            }
        }
    }
}
