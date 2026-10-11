using Avalonia.Controls;
using Avalonia.ControlsDemo.ViewModels;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class DialogsHowToPage : UserControl
    {
        public DialogsHowToPage()
        {
            InitializeComponent();
            DataContext = new DialogsHowToViewModel(new WindowDialogService(() => TopLevel.GetTopLevel(this) as Window));

            // The storage provider belongs to the TopLevel, which is only known once the page is on screen.
            OpenFileButton.IsEnabled = false;
            AttachedToVisualTree += (_, _) => RefreshStorage();
            OpenFileButton.Click += async (_, _) => await OpenFileAsync();
        }

        private void RefreshStorage()
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            StorageText.Text = storage is null
                ? "StorageProvider：不可用"
                : $"CanOpen = {storage.CanOpen}，CanSave = {storage.CanSave}，CanPickFolder = {storage.CanPickFolder}";
            OpenFileButton.IsEnabled = storage?.CanOpen == true;
        }

        private async Task OpenFileAsync()
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage is null || !storage.CanOpen) return;

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选一个文件", AllowMultiple = false });
            FileText.Text = files.Count == 0 ? "没选文件" : $"选了：{files[0].Name}";
        }
    }
}
