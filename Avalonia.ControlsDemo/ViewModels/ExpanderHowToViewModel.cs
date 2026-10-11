using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Backs the accordion block: one int says which section is open, -1 means all collapsed.</summary>
    public sealed class ExpanderHowToViewModel : ObservableObject
    {
        private int _openSection = -1;

        public int OpenSection
        {
            get => _openSection;
            set
            {
                if (SetProperty(ref _openSection, value))
                {
                    OnPropertyChanged(nameof(IsGeneralOpen));
                    OnPropertyChanged(nameof(IsAdvancedOpen));
                    OnPropertyChanged(nameof(IsAboutOpen));
                }
            }
        }

        // Only a "false" for the section that is currently open collapses it. Without that guard the
        // TwoWay binding of the section being closed by someone else would close the new one as well.
        public bool IsGeneralOpen
        {
            get => _openSection == 0;
            set => Apply(0, value);
        }

        public bool IsAdvancedOpen
        {
            get => _openSection == 1;
            set => Apply(1, value);
        }

        public bool IsAboutOpen
        {
            get => _openSection == 2;
            set => Apply(2, value);
        }

        private void Apply(int index, bool isOpen)
        {
            if (isOpen)
            {
                OpenSection = index;
            }
            else if (_openSection == index)
            {
                OpenSection = -1;
            }
        }
    }
}
