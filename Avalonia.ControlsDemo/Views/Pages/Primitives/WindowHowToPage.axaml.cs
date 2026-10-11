using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.ControlsDemo.Views.Pages
{
    public partial class WindowHowToPage : UserControl
    {
        private readonly List<Window> _free = new();
        private readonly List<Window> _owned = new();
        private Window? _placeWindow;
        private Window? _editor;
        private (PixelPoint Position, Size Size)? _saved;
        private bool _dirty;
        private bool _confirmedClose;
        private int _blocked;
        private string _lastChoice = "（还没触发）";
        private int _counter;

        public WindowHowToPage()
        {
            InitializeComponent();

            FreeButton.Click += (_, _) => OpenFree();
            OwnedButton.Click += (_, _) => OpenOwned();
            CloseAllButton.Click += (_, _) => CloseAll();

            PlaceOpenButton.Click += (_, _) => OpenPlace();
            MoveButton.Click += (_, _) => MovePlace();
            SaveButton.Click += (_, _) => SavePlace();
            RestoreButton.Click += (_, _) => RestorePlace();
            PlaceCloseButton.Click += (_, _) => _placeWindow?.Close();

            EditorButton.Click += (_, _) => OpenEditor();

            // Never leave orphan windows on the desktop when the user leaves this page.
            DetachedFromVisualTree += (_, _) =>
            {
                _confirmedClose = true;
                CloseAll();
                _placeWindow?.Close();
                _editor?.Close();
            };

            RefreshWindows();
            RefreshPlace();
            RefreshConfirm();
        }

        /// <summary>The position-and-size sample window while it is open, otherwise null.</summary>
        public Window? PlaceWindow => _placeWindow;

        /// <summary>The editable window with the close confirmation while it is open, otherwise null.</summary>
        public Window? Editor => _editor;

        private Window? Owner => TopLevel.GetTopLevel(this) as Window;

        private static Window NewWindow(string title, string text) => new Window
        {
            Title = title,
            Width = 280,
            Height = 140,
            Content = new TextBlock { Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap, Text = text }
        };

        private void ShowOwned(Window window)
        {
            if (Owner is { } owner)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
        }

        // ---- multi-window ----

        private void OpenFree()
        {
            var window = NewWindow($"独立窗口 {++_counter}", "用 Show() 打开，没有所有者。");
            _free.Add(window);
            window.Closed += (_, _) =>
            {
                _free.Remove(window);
                RefreshWindows();
            };
            window.Show();
            RefreshWindows();
        }

        private void OpenOwned()
        {
            var window = NewWindow($"子窗口 {++_counter}", "用 Show(owner) 打开，随所有者一起关闭。");
            _owned.Add(window);
            window.Closed += (_, _) =>
            {
                _owned.Remove(window);
                RefreshWindows();
            };
            ShowOwned(window);
            RefreshWindows();
        }

        private void CloseAll()
        {
            foreach (var window in _free.ToArray()) window.Close();
            foreach (var window in _owned.ToArray()) window.Close();
        }

        private void RefreshWindows()
        {
            // Closed fires while the window is still in OwnedWindows, so count only the ones still showing.
            int owned = Owner?.OwnedWindows.Count(w => w.IsVisible) ?? 0;
            WindowsText.Text = $"独立窗口 {_free.Count} 个，子窗口 {_owned.Count} 个；所有者的 OwnedWindows 共 {owned} 个（含下面两块开的窗口）";
        }

        // ---- position and size ----

        private void OpenPlace()
        {
            if (_placeWindow is not null)
            {
                _placeWindow.Activate();
                return;
            }

            var window = NewWindow("位置与大小示例", "拖动或缩放它，再用页面上的按钮记下、移动、恢复。");
            _placeWindow = window;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_placeWindow, window)) _placeWindow = null;
                RefreshPlace();
            };
            window.PositionChanged += (_, _) => RefreshPlace();
            window.Resized += (_, _) => RefreshPlace();
            ShowOwned(window);
            RefreshPlace();
        }

        private void MovePlace()
        {
            if (_placeWindow is null) return;

            _placeWindow.Position = new PixelPoint(200, 120);
            _placeWindow.Width = 420;
            _placeWindow.Height = 260;
            RefreshPlace();
        }

        private void SavePlace()
        {
            if (_placeWindow is null) return;

            _saved = (_placeWindow.Position, _placeWindow.ClientSize);
            RefreshPlace();
        }

        private void RestorePlace()
        {
            if (_placeWindow is null || _saved is not { } saved) return;

            _placeWindow.Position = saved.Position;
            _placeWindow.Width = saved.Size.Width;
            _placeWindow.Height = saved.Size.Height;
            RefreshPlace();
        }

        private void RefreshPlace()
        {
            string current = _placeWindow is null
                ? "示例窗口：未打开"
                : $"示例窗口：Position = {_placeWindow.Position}，ClientSize = {_placeWindow.ClientSize.Width:0}×{_placeWindow.ClientSize.Height:0}";
            string saved = _saved is { } s
                ? $"已记下：Position = {s.Position}，ClientSize = {s.Size.Width:0}×{s.Size.Height:0}"
                : "已记下：（还没有）";
            PlaceText.Text = $"{current}；{saved}";
        }

        // ---- confirm before closing ----

        private void OpenEditor()
        {
            if (_editor is not null)
            {
                _editor.Activate();
                return;
            }

            _dirty = false;
            _confirmedClose = false;
            _blocked = 0;
            _lastChoice = "（还没触发）";

            var box = new TextBox { Name = "EditorBox", AcceptsReturn = true, Height = 70, PlaceholderText = "改点什么" };
            var save = new Button { Name = "EditorSaveButton", Content = "保存" };
            var state = new TextBlock { Name = "EditorState", Text = "没有未保存的更改" };
            box.TextChanged += (_, _) =>
            {
                _dirty = true;
                state.Text = "有未保存的更改";
                RefreshConfirm();
            };
            save.Click += (_, _) =>
            {
                _dirty = false;
                state.Text = "没有未保存的更改";
                RefreshConfirm();
            };

            var window = new Window
            {
                Title = "可编辑的窗口",
                Width = 320,
                Height = 220,
                Content = new StackPanel { Margin = new Thickness(16), Spacing = 8, Children = { box, save, state } }
            };
            _editor = window;
            window.Closing += OnEditorClosing;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_editor, window)) _editor = null;
                RefreshConfirm();
            };
            ShowOwned(window);
            RefreshConfirm();
        }

        // async void is fine here: this is an event handler, and Cancel is set before the first await.
        private async void OnEditorClosing(object? sender, WindowClosingEventArgs e)
        {
            if (!_dirty || _confirmedClose || e.CloseReason != WindowCloseReason.WindowClosing) return;

            e.Cancel = true;
            _blocked++;
            RefreshConfirm();

            var window = (Window)sender!;
            bool? discard = await DemoDialogs.ConfirmAsync(window, "放弃更改？", "窗口里的内容还没保存，确定要关闭吗？");
            _lastChoice = discard switch
            {
                true => "用户选了「确定」，窗口随后关闭",
                false => "用户选了「取消」，窗口留着",
                _ => "用户直接关掉了确认框，窗口留着"
            };
            RefreshConfirm();

            if (discard == true)
            {
                _confirmedClose = true;
                window.Close();
            }
        }

        private void RefreshConfirm()
        {
            string state = _editor is null ? "窗口未打开" : _dirty ? "窗口有未保存的更改" : "窗口没有未保存的更改";
            ConfirmText.Text = $"{state}；Closing 被拦下 {_blocked} 次；最近一次：{_lastChoice}";
        }
    }
}
