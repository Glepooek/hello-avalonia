using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddPremium(List<PageEntry> list)
        {
            const string c = Categories.Premium;

            // Official docs paths, kept verbatim even though the pages sit under one category here.
            list.Add(SignpostPage<ChartsSignpostPage>("Charts", c, "controls/data-display/charts"));
            list.Add(SignpostPage<PdfViewerSignpostPage>("PdfViewer", c, "controls/data-display/pdfviewer"));
            list.Add(SignpostPage<MarkdownSignpostPage>("Markdown", c, "controls/data-display/text-display/markdown"));
            list.Add(SignpostPage<TreeDataGridSignpostPage>("TreeDataGrid", c, "controls/data-display/structured-data/treedatagrid"));
            list.Add(SignpostPage<RichTextEditorSignpostPage>("RichTextEditor", c, "controls/input/text-input/richtexteditor"));
            list.Add(SignpostPage<VirtualKeyboardSignpostPage>("VirtualKeyboard", c, "controls/input/text-input/virtualkeyboard"));
            list.Add(SignpostPage<MediaPlayerSignpostPage>("MediaPlayer", c, "controls/media/mediaplayer"));
        }
    }
}
