using Avalonia.Controls;

namespace Avalonia.BehaviorsDemo.Behaviors
{
    /// <summary>
    /// A custom Action: appends a suffix to another TextBlock. Note the full base name:
    /// plain "Action" would collide with System.Action in files that import System.
    /// </summary>
    public class AppendTextAction : Avalonia.Xaml.Interactivity.Action
    {
        public static readonly StyledProperty<TextBlock?> TargetProperty =
            AvaloniaProperty.Register<AppendTextAction, TextBlock?>(nameof(Target));

        public static readonly StyledProperty<string> SuffixProperty =
            AvaloniaProperty.Register<AppendTextAction, string>(nameof(Suffix), "!");

        public TextBlock? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public string Suffix
        {
            get => GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        // The return value is passed to later actions that care; nothing here needs it.
        public override object? Execute(object? sender, object? parameter)
        {
            if (Target is { } target)
            {
                target.Text += Suffix;
            }

            return true;
        }
    }
}
