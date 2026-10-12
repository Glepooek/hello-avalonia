using System.Collections.ObjectModel;
using Avalonia.Shared.Helpers;
using Avalonia.Shared.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    public partial class DragViewModel : ViewModelBase
    {
        public DragViewModel()
        {
            // ListReorderDragBehavior reorders the ItemsSource in place; mirror it as text so the result is readable.
            Order.CollectionChanged += (_, _) => OnPropertyChanged(nameof(OrderText));
        }

        public ObservableCollection<string> Order { get; } = new() { "A", "B", "C", "D" };

        public string OrderText => string.Join(" → ", Order);

        public EventLog DropLog { get; } = new();

        [RelayCommand]
        private void TextDropped(object? payload) => DropLog.Write($"文字投放：参数类型 {payload?.GetType().Name ?? "null"}，内容 {payload}");

        [RelayCommand]
        private void FilesDropped(object? payload) => DropLog.Write($"文件投放：参数类型 {payload?.GetType().Name ?? "null"}");
    }
}
