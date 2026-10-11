using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace Avalonia.ControlsDemo.Navigation
{
    /// <summary>
    /// The single registry of demo pages. Adding a page is two steps: create the page file, then
    /// add one line to the matching PageCatalog.&lt;Category&gt;.cs. The navigation tree and the
    /// probe's completeness counts are both derived from <see cref="All"/>.
    /// </summary>
    public static partial class PageCatalog
    {
        public static IReadOnlyList<PageEntry> All { get; } = Build();

        // Registration order is the tree order, so these calls follow the official sidebar.
        private static IReadOnlyList<PageEntry> Build()
        {
            var list = new List<PageEntry>();
            AddDataDisplay(list);
            AddFeedback(list);
            AddInput(list);
            AddLayout(list);
            AddMedia(list);
            AddMenus(list);
            AddNavigation(list);
            AddPrimitives(list);
            AddSystem(list);
            AddWeb(list);
            AddPremium(list);
            AddHowToSignposts(list);
            return list;
        }

        private static PageEntry ControlPage<TPage>(string title, string category, string docPath)
            where TPage : Control
            => new(title, category, docPath, typeof(TPage), PageKind.Control);

        private static PageEntry HowToPage<TPage>(string title, string category, string docPath)
            where TPage : Control
            => new(title, category, docPath, typeof(TPage), PageKind.HowTo);

        private static PageEntry SignpostPage<TPage>(string title, string category, string docPath)
            where TPage : Control
            => new(title, category, docPath, typeof(TPage), PageKind.Signpost);
    }
}
