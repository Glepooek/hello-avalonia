using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.LayoutDemo.ViewModels
{
    /// <summary>
    /// Turns a raw width into named breakpoints. Use this approach when the
    /// decision depends on more than size, or drives logic rather than styling.
    /// </summary>
    public partial class ResponsiveViewModel : ViewModelBase
    {
        private const double CompactThreshold = 500d;

        [ObservableProperty]
        private bool _isCompact = true;

        [ObservableProperty]
        private bool _isWide;

        [ObservableProperty]
        private double _currentWidth;

        public void UpdateLayout(double width)
        {
            CurrentWidth = width;
            IsCompact = width < CompactThreshold;
            IsWide = !IsCompact;
        }
    }
}
