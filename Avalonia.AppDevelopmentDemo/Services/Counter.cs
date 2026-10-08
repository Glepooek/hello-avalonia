using System.Threading;

namespace Avalonia.AppDevelopmentDemo.Services
{
    // Every instance takes the next number, so the id shows which instance a resolve returned.
    public class Counter
    {
        private static int _next;

        public int Id { get; } = Interlocked.Increment(ref _next);
    }
}
