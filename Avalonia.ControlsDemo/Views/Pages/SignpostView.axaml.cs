using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Avalonia.ControlsDemo.Views.Pages
{
    /// <summary>
    /// The body shared by every signpost page: a summary, optional fact rows, and a link to the
    /// official docs. A row whose value is empty is hidden, so each page only shows what it has.
    /// </summary>
    public partial class SignpostView : UserControl
    {
        public const string DocBase = "https://docs.avaloniaui.net/";

        public static readonly StyledProperty<string?> SummaryProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(Summary));

        public static readonly StyledProperty<string?> LicenseProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(License));

        public static readonly StyledProperty<string?> SubPagesProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(SubPages));

        public static readonly StyledProperty<string?> TargetProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(Target));

        public static readonly StyledProperty<string?> RunLineProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(RunLine));

        public static readonly StyledProperty<string?> DocPathProperty =
            AvaloniaProperty.Register<SignpostView, string?>(nameof(DocPath));

        // Property changes can arrive before InitializeComponent has created the named elements.
        private readonly bool _ready;

        public SignpostView()
        {
            InitializeComponent();
            OpenButton.Click += OnOpenClick;
            _ready = true;
            Refresh();
        }

        public string? Summary
        {
            get => GetValue(SummaryProperty);
            set => SetValue(SummaryProperty, value);
        }

        public string? License
        {
            get => GetValue(LicenseProperty);
            set => SetValue(LicenseProperty, value);
        }

        public string? SubPages
        {
            get => GetValue(SubPagesProperty);
            set => SetValue(SubPagesProperty, value);
        }

        public string? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public string? RunLine
        {
            get => GetValue(RunLineProperty);
            set => SetValue(RunLineProperty, value);
        }

        public string? DocPath
        {
            get => GetValue(DocPathProperty);
            set => SetValue(DocPathProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SummaryProperty || change.Property == LicenseProperty || change.Property == SubPagesProperty
                || change.Property == TargetProperty || change.Property == RunLineProperty || change.Property == DocPathProperty)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!_ready) return;

            SummaryText.Text = Summary;
            ShowRow(LicenseRow, LicenseText, License);
            ShowRow(SubPagesRow, SubPagesText, SubPages);
            ShowRow(TargetRow, TargetText, Target);
            ShowRow(RunRow, RunText, RunLine);

            bool hasPath = !string.IsNullOrEmpty(DocPath);
            UrlText.Text = hasPath ? DocBase + DocPath : "";
            OpenButton.IsEnabled = hasPath;
        }

        private static void ShowRow(Control row, TextBlock text, string? value)
        {
            text.Text = value;
            row.IsVisible = !string.IsNullOrEmpty(value);
        }

        // async void is fine here: this is an event handler, and nothing awaits it.
        private async void OnOpenClick(object? sender, RoutedEventArgs e)
        {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            if (launcher is null || string.IsNullOrEmpty(DocPath))
            {
                LaunchText.Text = "没有可用的 Launcher";
                return;
            }

            bool accepted = await launcher.LaunchUriAsync(new Uri(DocBase + DocPath));
            LaunchText.Text = accepted
                ? "已请求系统用默认浏览器打开"
                : "系统没有接受这个请求（没有可用的浏览器，或当前平台不支持 Launcher）";
        }
    }
}
