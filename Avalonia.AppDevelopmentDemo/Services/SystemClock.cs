using System;

namespace Avalonia.AppDevelopmentDemo.Services
{
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
