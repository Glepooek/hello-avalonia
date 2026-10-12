using System.Linq;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class NetworkPage : UserControl
    {
        public NetworkPage()
        {
            InitializeComponent();
            var vm = new NetworkViewModel();
            DataContext = vm;

            // Same reason as the file watcher page: a plain Trigger gives its actions no DataContext, so assign the command directly.
            var trigger = Interaction.GetBehaviors(NetBox).OfType<Trigger>().First();
            ((InvokeCommandAction)trigger.Actions[0]).Command = vm.NetworkChangedCommand;
        }
    }
}
