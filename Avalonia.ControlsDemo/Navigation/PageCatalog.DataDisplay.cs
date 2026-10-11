using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddDataDisplay(List<PageEntry> list)
        {
            const string c = Categories.DataDisplay;

            // Collections, in the order of the official sidebar.
            list.Add(ControlPage<CarouselPage>("Carousel", c, "controls/data-display/collections/carousel"));
            list.Add(ControlPage<ItemsControlPage>("ItemsControl", c, "controls/data-display/collections/itemscontrol"));
            list.Add(HowToPage<ItemsControlHowToPage>("ItemsControl", c, "docs/how-to/itemscontrol-how-to"));
            list.Add(ControlPage<ListBoxPage>("ListBox", c, "controls/data-display/collections/listbox"));
            list.Add(HowToPage<ListBoxHowToPage>("ListBox", c, "docs/how-to/listbox-how-to"));

            // Stand-alone pages directly under the category.
            list.Add(ControlPage<ContentControlPage>("ContentControl", c, "controls/data-display/contentcontrol"));

            // Structured data
            list.Add(ControlPage<DataGridPage>("DataGrid", c, "controls/data-display/structured-data/datagrid"));
            list.Add(HowToPage<DataGridHowToPage>("DataGrid", c, "docs/how-to/datagrid-how-to"));
            list.Add(ControlPage<TableViewPage>("TableView", c, "controls/data-display/structured-data/tableview"));
            list.Add(ControlPage<TreeViewPage>("TreeView", c, "controls/data-display/structured-data/treeview"));
            list.Add(HowToPage<TreeViewHowToPage>("TreeView", c, "docs/how-to/treeview-how-to"));

            // Text display
            list.Add(ControlPage<LabelPage>("Label", c, "controls/data-display/text-display/label"));
            list.Add(ControlPage<SelectableTextBlockPage>("SelectableTextBlock", c, "controls/data-display/text-display/selectabletextblock"));
            list.Add(ControlPage<TextBlockPage>("TextBlock", c, "controls/data-display/text-display/textblock"));
            list.Add(ControlPage<TextTrimmingPage>("TextTrimming", c, "controls/data-display/text-display/texttrimming"));

            list.Add(ControlPage<TransitioningContentControlPage>("TransitioningContentControl", c, "controls/data-display/transitioningcontentcontrol"));
        }
    }
}
