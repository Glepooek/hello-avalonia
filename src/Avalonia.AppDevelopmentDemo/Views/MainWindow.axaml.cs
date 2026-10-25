using Avalonia.AppDevelopmentDemo.Services;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.AppDevelopmentDemo.Views
{
    public partial class MainWindow : Window
    {
        // Required by the XAML loader and designer. The running application resolves the marked constructor below.
        public MainWindow()
        {
            InitializeComponent();
        }

        [ActivatorUtilitiesConstructor]
        public MainWindow(Greeter greeter, ILogger<MainWindow> logger)
            : this()
        {
            logger.LogInformation("Main window created for {Greeting}", greeter.Greet("Avalonia"));
        }
    }
}
