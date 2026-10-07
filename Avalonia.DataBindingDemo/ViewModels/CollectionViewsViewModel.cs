using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class CollectionViewsViewModel : ViewModelBase
    {
        private readonly Contact[] _all = Contact.Samples();

        // Holds Contact and ContactGroupHeader rows side by side; templates tell them apart.
        public ObservableCollection<object> Rows { get; } = new();

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private bool _sortByAge;

        [ObservableProperty]
        private bool _groupByCity;

        public CollectionViewsViewModel() => Rebuild();

        partial void OnFilterChanged(string value) => Rebuild();

        partial void OnSortByAgeChanged(bool value) => Rebuild();

        partial void OnGroupByCityChanged(bool value) => Rebuild();

        private void Rebuild()
        {
            var query = _all.Where(c => c.Name.Contains(Filter.Trim()));
            query = SortByAge ? query.OrderBy(c => c.Age) : query.OrderBy(c => c.Name);

            Rows.Clear();
            if (!GroupByCity)
            {
                foreach (var c in query)
                {
                    Rows.Add(c);
                }

                return;
            }

            foreach (var group in query.GroupBy(c => c.City))
            {
                Rows.Add(new ContactGroupHeader(group.Key, group.Count()));
                foreach (var c in group)
                {
                    Rows.Add(c);
                }
            }
        }
    }
}
