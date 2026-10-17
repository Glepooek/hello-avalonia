using Avalonia.Appium.Common;
using Xunit;

namespace Avalonia.ServicesDemo.AppiumTests
{
    // A3: the real Windows file dialogs open and can be cancelled without a crash. Nothing is picked, so the
    // tests read and write no files. Escape cancels both dialogs; their Cancel button is named in the language
    // of Windows, so it is not clicked.
    public class FlowTests
    {
        private const string App = "Avalonia.ServicesDemo";

        // The batch 10 probe saw the first open dialog after 4.6 s and the save dialog after 2.7 s.
        private const int DialogTimeoutMs = 15000;

        [AppiumFact(App)]
        public void Open_and_save_dialogs_can_be_cancelled()
        {
            using var app = new AppSession(App);
            app.SelectTab("文件对话框");

            app.ById("OpenButton").Click();
            WaitForDialog(app, "打开文本文件");
            KeyChord.Press(VirtualKey.Escape);
            app.WaitForText("Result", "已取消（OpenFilePickerAsync 返回空列表）");
            AppSession.WaitFor(() => app.FindWindow("打开文本文件") is null, "the open dialog is closed");

            app.ById("SaveButton").Click();
            WaitForDialog(app, "保存文件");
            KeyChord.Press(VirtualKey.Escape);
            app.WaitForText("Result", "已取消（StorageFile 为 null）");
            AppSession.WaitFor(() => app.FindWindow("保存文件") is null, "the save dialog is closed");
        }

        private static void WaitForDialog(AppSession app, string title) =>
            AppSession.WaitFor(() => app.FindWindow(title) is not null, $"the {title} dialog is open", DialogTimeoutMs);
    }
}
