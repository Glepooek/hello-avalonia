using Avalonia.ControlsDemo.Views.Pages;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    public static partial class PageCatalog
    {
        private static void AddHowToSignposts(List<PageEntry> list)
        {
            const string c = Categories.HowToSignposts;

            // The nine cross-cutting how-tos whose subject is not a single control; each points at an existing demo.
            list.Add(SignpostPage<ResponsiveLayoutSignpostPage>("响应式布局", c, "docs/how-to/responsive-layout-how-to"));
            list.Add(SignpostPage<StylingControlsSignpostPage>("给控件写样式", c, "docs/how-to/styling-controls-how-to"));
            list.Add(SignpostPage<ThemeSwitchingSignpostPage>("切换主题", c, "docs/how-to/theme-switching-how-to"));
            list.Add(SignpostPage<CustomFontSignpostPage>("使用自定义字体", c, "docs/how-to/custom-font-how-to"));
            list.Add(SignpostPage<MvvmSignpostPage>("用 MVVM 组织代码", c, "docs/how-to/mvvm-how-to"));
            list.Add(SignpostPage<CodedUiSignpostPage>("用纯代码搭界面", c, "docs/how-to/coded-ui-how-to"));
            list.Add(SignpostPage<DragAndDropSignpostPage>("拖放", c, "docs/how-to/drag-and-drop-how-to"));
            list.Add(SignpostPage<ClipboardSignpostPage>("剪贴板", c, "docs/how-to/clipboard-how-to"));
            list.Add(SignpostPage<DebuggingSignpostPage>("调试应用", c, "docs/how-to/debugging-how-to"));
        }
    }
}
