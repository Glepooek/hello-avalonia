using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class CollectionsViewModel : ViewModelBase
    {
        private int _added;

        // Raises CollectionChanged, so bound lists insert and remove rows themselves.
        public ObservableCollection<Contact> Contacts { get; } = new(Contact.Samples()[..3]);

        // A plain List never tells anyone it changed: the bound list stays frozen.
        public List<Contact> FrozenContacts { get; } = new(Contact.Samples()[..3]);

        [RelayCommand]
        private void Add()
        {
            _added++;
            Contacts.Add(new Contact($"新人{_added}", "杭州", 20 + _added));
            FrozenContacts.Add(new Contact($"新人{_added}", "杭州", 20 + _added));
        }

        [RelayCommand]
        private void RemoveLast()
        {
            if (Contacts.Count > 0)
            {
                Contacts.RemoveAt(Contacts.Count - 1);
            }
        }
    }
}
