using System.Linq;
using Avalonia.BehaviorsDemo.ViewModels;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class MechanismPage : UserControl
    {
        public MechanismPage()
        {
            InitializeComponent();
            DataContext = new MechanismViewModel();

            // The XAML above is just sugar over this collection: read it back to see what is attached.
            var behaviors = Interaction.GetBehaviors(RunButton);
            var names = behaviors.Select(b =>
                b is Trigger trigger ? $"{b.GetType().Name}（{trigger.Actions.Count} 个 Action）" : b.GetType().Name);
            CodeReadout.Text = $"Interaction.GetBehaviors(RunButton) 读到 {behaviors.Count} 个：{string.Join("、", names)}";
        }
    }
}
