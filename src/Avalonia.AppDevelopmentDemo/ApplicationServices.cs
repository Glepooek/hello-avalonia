using Avalonia.AppDevelopmentDemo.Logging;
using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.AppDevelopmentDemo.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.AppDevelopmentDemo
{
    public static class ApplicationServices
    {
        public static ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IClock, SystemClock>();
            services.AddTransient<Greeter>();
            services.AddKeyedTransient<Counter>("transient");
            services.AddKeyedScoped<Counter>("scoped");
            services.AddKeyedSingleton<Counter>("singleton");

            services.AddSingleton<ListLoggerProvider>();
            services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<ListLoggerProvider>());
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

            services.AddSingleton<ApplicationExceptionReporter>();
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }
    }
}
