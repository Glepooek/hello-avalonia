using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avalonia.StylingDemo.Controls
{
    /// <summary>
    /// A button that latches. Its only purpose is to own a custom pseudo-class,
    /// <c>:on</c>, so the styling page can show that pseudo-classes are not a
    /// fixed list but something any control can publish.
    /// </summary>
    [PseudoClasses(":on")]
    public class ToggleChip : Button
    {
        public static readonly StyledProperty<bool> IsOnProperty =
            AvaloniaProperty.Register<ToggleChip, bool>(nameof(IsOn));

        public bool IsOn
        {
            get => GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        protected override void OnClick()
        {
            base.OnClick();
            IsOn = !IsOn;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // The attribute above only documents the pseudo-class for tooling;
            // this call is what actually turns it on and off.
            if (change.Property == IsOnProperty)
            {
                PseudoClasses.Set(":on", IsOn);
            }
        }
    }
}
