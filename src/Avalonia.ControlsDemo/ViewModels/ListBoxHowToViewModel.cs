using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>One row of the checkbox list; IsDone is the state, so no ListBox selection is involved.</summary>
    public sealed class TodoItem : ObservableObject
    {
        private bool _isDone;

        public TodoItem(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public bool IsDone
        {
            get => _isDone;
            set => SetProperty(ref _isDone, value);
        }
    }

    public sealed class ListBoxHowToViewModel : ObservableObject
    {
        public ListBoxHowToViewModel()
        {
            Todos = [new TodoItem("写计划"), new TodoItem("跑探针"), new TodoItem("目视检查"), new TodoItem("提交")];
            foreach (var todo in Todos)
            {
                todo.PropertyChanged += (_, _) => OnPropertyChanged(nameof(DoneCount));
            }
        }

        public IReadOnlyList<TodoItem> Todos { get; }

        public int DoneCount => Todos.Count(t => t.IsDone);
    }
}
