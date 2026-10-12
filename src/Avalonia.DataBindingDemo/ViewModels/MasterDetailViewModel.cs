using Avalonia.DataBindingDemo.Models;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Avalonia.DataBindingDemo.ViewModels
{
    public partial class MasterDetailViewModel : ViewModelBase
    {
        public ObservableCollection<Contact> Contacts { get; } = new(Contact.Samples());

        [ObservableProperty]
        private Contact? _selected;
    }
}
