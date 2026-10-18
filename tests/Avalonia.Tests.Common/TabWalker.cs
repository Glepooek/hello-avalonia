using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Tests.Common
{
    /// <summary>One tab selected by <see cref="TabWalker.Walk" />: its position, header text, and the exception it raised, if any.</summary>
    public sealed record TabVisit(int Index, string Header, Exception? Error);

    /// <summary>Selects every tab of a window's first TabControl in turn and forces a layout pass after each one.</summary>
    public static class TabWalker
    {
        public static IReadOnlyList<TabVisit> Walk(Window window)
        {
            var tabs = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault()
                ?? throw new InvalidOperationException("The window has no TabControl.");

            var visits = new List<TabVisit>();
            for (int i = 0; i < tabs.ItemCount; i++)
            {
                string header = (tabs.Items[i] as TabItem)?.Header?.ToString() ?? $"#{i}";
                try
                {
                    tabs.SelectedIndex = i;
                    if (tabs.SelectedIndex != i)
                        throw new InvalidOperationException($"Setting SelectedIndex to {i} left it at {tabs.SelectedIndex}.");
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    visits.Add(new TabVisit(i, header, null));
                }
                catch (Exception ex)
                {
                    // One broken page must not hide the pages after it.
                    visits.Add(new TabVisit(i, header, ex));
                }
            }

            return visits;
        }
    }
}
