using System.Collections.ObjectModel;

namespace Avalonia.DataTemplatesDemo.Models
{
    public class Folder
    {
        public Folder(string title, params Folder[] children)
        {
            Title = title;
            Children = new ObservableCollection<Folder>(children);
        }

        public string Title { get; }

        public ObservableCollection<Folder> Children { get; }
    }
}
