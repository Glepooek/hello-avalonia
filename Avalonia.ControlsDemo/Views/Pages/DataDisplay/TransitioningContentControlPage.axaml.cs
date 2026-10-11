using Avalonia.Animation;
using Avalonia.Controls;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class TransitioningContentControlPage : UserControl
    {
        private static readonly string[] TransitionNames = ["PageSlide", "CrossFade", "Rotate3D"];
        private int _page = 1;
        private int _done;
        private bool _initialSeen;

        public TransitioningContentControlPage()
        {
            InitializeComponent();

            Host.TransitionCompleted += (_, _) =>
            {
                // The control also reports once for the content it was created with; skip that one so only real changes count.
                if (!_initialSeen)
                {
                    _initialSeen = true;
                    return;
                }

                DoneText.Text = $"已完成 {++_done} 次过渡";
            };
            NextButton.Click += (_, _) => Host.Content = $"第 {++_page} 页";

            TransitionBox.ItemsSource = TransitionNames;
            TransitionBox.SelectedIndex = 0;
            Apply();
            TransitionBox.SelectionChanged += (_, _) => Apply();
            ReverseCheck.IsCheckedChanged += (_, _) => Host.IsTransitionReversed = ReverseCheck.IsChecked == true;
        }

        private void Apply()
        {
            var duration = TimeSpan.FromMilliseconds(400);
            Host.PageTransition = TransitionBox.SelectedIndex switch
            {
                1 => new CrossFade(duration),
                2 => new Rotate3DTransition(duration),
                _ => new PageSlide(duration),
            };
        }
    }
}
