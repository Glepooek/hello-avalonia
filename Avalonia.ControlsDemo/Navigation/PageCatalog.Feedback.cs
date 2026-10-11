using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddFeedback(List<PageEntry> list)
        {
            const string c = Categories.Feedback;

            list.Add(ControlPage<NotificationPage>("Notification", c, "controls/feedback/notification"));
            list.Add(HowToPage<NotificationsHowToPage>("Notification", c, "docs/how-to/notifications-how-to"));
            list.Add(ControlPage<PopupPage>("Popup", c, "controls/feedback/popup"));
            list.Add(ControlPage<ProgressBarPage>("ProgressBar", c, "controls/feedback/progressbar"));
            list.Add(ControlPage<ToolTipPage>("ToolTip", c, "controls/feedback/tooltip"));
        }
    }
}
