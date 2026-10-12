using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddSystem(List<PageEntry> list)
        {
            const string c = Categories.System;

            // Official URL is under controls/navigation/, but the official sidebar files TrayIcon under System.
            list.Add(ControlPage<TrayIconPage>("TrayIcon", c, "controls/navigation/trayicon"));
            list.Add(HowToPage<DataPersistenceHowToPage>("数据持久化", c, "docs/how-to/data-persistence-how-to"));
        }
    }
}
