using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.InputDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.InputDemo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // No window-level DataContext: each page owns its own state.
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
