using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.HtmlRendererDemo.ViewModels;
using Avalonia.HtmlRendererDemo.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.HtmlRendererDemo
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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
