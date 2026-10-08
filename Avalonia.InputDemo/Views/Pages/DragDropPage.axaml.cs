using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Shared.Helpers;

namespace Avalonia.InputDemo.Views.Pages
{
    public partial class DragDropPage : UserControl
    {
        private readonly EventLog _log = new();

        public DragDropPage()
        {
            InitializeComponent();
            LogList.ItemsSource = _log.Entries;

            // Four handlers on the drop target. AllowDrop is set in XAML.
            DragDrop.AddDragEnterHandler(Target, OnDragEnter);
            DragDrop.AddDragOverHandler(Target, OnDragOver);
            DragDrop.AddDragLeaveHandler(Target, OnDragLeave);
            DragDrop.AddDropHandler(Target, OnDrop);
        }

        // The source side: build the payload, then await the whole drag.
        private async void OnHandlePressed(object? sender, PointerPressedEventArgs e)
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(SourceBox.Text ?? ""));

            _log.Write("DoDragDropAsync 开始");
            var result = await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy | DragDropEffects.Move);
            _log.Write($"DoDragDropAsync 结束 → {result}");
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            Target.Classes.Set("over", true);
            _log.Write("DragEnter");
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            // Only text is welcome; anything else gets the "not allowed" cursor.
            e.DragEffects = e.DataTransfer.TryGetText() is not null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            Target.Classes.Set("over", false);
            _log.Write("DragLeave");
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            Target.Classes.Set("over", false);
            var text = e.DataTransfer.TryGetText();
            Dropped.Text = text ?? "（没有文本）";
            _log.Write($"Drop  text='{text}'  modifiers={e.KeyModifiers}");
            e.Handled = true;
        }
    }
}
