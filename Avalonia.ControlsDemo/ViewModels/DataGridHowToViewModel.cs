using Avalonia.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>Two DataGridCollectionViews over the same rows: one grouped by Category, one sorted by Name.</summary>
    public sealed class DataGridHowToViewModel
    {
        public DataGridHowToViewModel()
        {
            var rows = DemoProduct.Sample();

            Grouped = new DataGridCollectionView(rows);
            Grouped.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(DemoProduct.Category)));

            Sorted = new DataGridCollectionView(DemoProduct.Sample());
            Sorted.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(DemoProduct.Name), ListSortDirection.Descending));

            Details = DemoProduct.Sample();
        }

        public DataGridCollectionView Grouped { get; }

        public DataGridCollectionView Sorted { get; }

        public List<DemoProduct> Details { get; }
    }
}
