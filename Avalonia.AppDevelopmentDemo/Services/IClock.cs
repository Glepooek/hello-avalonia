using System;

namespace Avalonia.AppDevelopmentDemo.Services
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
