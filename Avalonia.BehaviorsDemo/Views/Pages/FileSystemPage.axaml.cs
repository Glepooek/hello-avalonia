using System.Linq;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class FileSystemPage : UserControl
    {
        public FileSystemPage()
        {
            InitializeComponent();
            var vm = new FileSystemViewModel();
            DataContext = vm;

            // FileSystemWatcherTrigger derives from the plain Trigger, which does not hand its DataContext to the actions,
            // so a bound Command never resolves. A command assigned directly is already a value; it hops to the UI thread itself.
            var trigger = Interaction.GetBehaviors(WatchBox).OfType<Trigger>().First();
            ((InvokeCommandAction)trigger.Actions[0]).Command = vm.WatcherHitCommand;
        }
    }
}
