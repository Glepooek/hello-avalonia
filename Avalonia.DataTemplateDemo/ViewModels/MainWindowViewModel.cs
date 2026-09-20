using Avalonia.DataTemplateDemo.Models;
using Avalonia.Shared.ViewModels;
using System.Collections.ObjectModel;

namespace Avalonia.DataTemplateDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<Person> People { get; } = new();

        public MainWindowViewModel()
        {
            People.Add(new Person() { Id = "10", Name = "anyu", Address = "Beijing", Sex = Sex.Male });
            People.Add(new Person() { Id = "20", Name = "lff", Address = "Beijing", Sex = Sex.Female });
        }
    }
}
