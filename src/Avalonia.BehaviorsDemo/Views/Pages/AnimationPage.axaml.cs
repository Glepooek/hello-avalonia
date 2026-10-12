using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.Custom;

namespace Avalonia.BehaviorsDemo.Views.Pages
{
    public partial class AnimationPage : UserControl
    {
        public AnimationPage()
        {
            InitializeComponent();
        }

        private void OnShake(object? sender, RoutedEventArgs e) => AttentionAnimations.SetShakeX(ShakeBox, 500);
    }
}
