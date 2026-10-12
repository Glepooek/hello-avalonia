using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DataPersistenceHowToPage : UserControl
    {
        public DataPersistenceHowToPage()
        {
            InitializeComponent();

            // The view model reads these folders but only writes when the user changes something.
            DataContext = new DataPersistenceHowToViewModel(SettingsStore.DefaultDirectory, RecentFiles.DefaultDirectory);
        }
    }
}
