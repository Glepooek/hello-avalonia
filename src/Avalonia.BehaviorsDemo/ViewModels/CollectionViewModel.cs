using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class CollectionViewModel : ViewModelBase
    {
        // The collection actions work on whatever ItemsSource is bound to, so these must be real collections.
        public ObservableCollection<string> Fruits { get; } = new() { "苹果", "香蕉", "橙子" };

        public ObservableCollection<string> Todos { get; } = new() { "写计划", "跑探针", "提交" };

        public IReadOnlyList<string> Numbers { get; } = Enumerable.Range(0, 200).Select(i => $"第 {i} 项").ToList();

        public IReadOnlyList<string> Colors { get; } = new[] { "红", "橙", "黄", "绿", "青", "蓝", "紫", "灰" };

        public ScrollRequest ScrollRequests { get; } = new();

        [RelayCommand]
        private void ScrollTo(string? index) => ScrollRequests.Publish(Numbers[int.Parse(index ?? "0")]);

        [RelayCommand]
        private void ResetTodos()
        {
            Todos.Clear();
            foreach (var todo in new[] { "写计划", "跑探针", "提交" })
            {
                Todos.Add(todo);
            }
        }
    }
}
