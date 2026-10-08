using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Avalonia.AppDevelopmentDemo.Views.Pages
{
    public partial class DependencyInjectionPage : UserControl
    {
        private readonly ServiceProvider _root;

        public DependencyInjectionPage()
        {
            InitializeComponent();

            var services = new ServiceCollection();
            services.AddSingleton<IClock, SystemClock>();
            services.AddTransient<Greeter>();
            services.AddKeyedTransient<Counter>("transient");
            services.AddKeyedScoped<Counter>("scoped");
            services.AddKeyedSingleton<Counter>("singleton");

            // ValidateScopes makes a scoped service resolved from the root fail loudly.
            _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        private void OnResolve(object? sender, RoutedEventArgs e)
        {
            using var scope = _root.CreateScope();
            var sp = scope.ServiceProvider;

            TransientLine.Text = Describe("Transient", sp.GetRequiredKeyedService<Counter>("transient"), sp.GetRequiredKeyedService<Counter>("transient"));
            ScopedLine.Text = Describe("Scoped", sp.GetRequiredKeyedService<Counter>("scoped"), sp.GetRequiredKeyedService<Counter>("scoped"));
            SingletonLine.Text = Describe("Singleton", sp.GetRequiredKeyedService<Counter>("singleton"), sp.GetRequiredKeyedService<Counter>("singleton"));
        }

        private static string Describe(string kind, Counter a, Counter b)
            => $"{kind}：第一次 Id = {a.Id}，第二次 Id = {b.Id}，{(ReferenceEquals(a, b) ? "同一个实例" : "不同实例")}";

        private void OnGreet(object? sender, RoutedEventArgs e)
        {
            GreetLine.Text = _root.GetRequiredService<Greeter>().Greet("Avalonia");
        }

        private void OnRootScoped(object? sender, RoutedEventArgs e)
        {
            try
            {
                var counter = _root.GetRequiredKeyedService<Counter>("scoped");
                ScopeErrorLine.Text = $"没有抛异常（Id = {counter.Id}）";
            }
            catch (InvalidOperationException ex)
            {
                ScopeErrorLine.Text = $"InvalidOperationException：{ex.Message}";
            }
        }
    }
}
