namespace Avalonia.AppDevelopmentDemo.Services
{
    // Takes IClock in the constructor: the container supplies it, nothing here calls new.
    public class Greeter
    {
        private readonly IClock _clock;

        public Greeter(IClock clock) => _clock = clock;

        public string Greet(string name) => $"你好，{name}。现在是 {_clock.Now:HH:mm:ss}";
    }
}
