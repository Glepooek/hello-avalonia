using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class DataPersistenceHowToViewModel : ObservableObject
    {
        private readonly SettingsStore _store;
        private readonly RecentFiles _recent;
        private bool _loading;
        private int _recentCounter;

        public DataPersistenceHowToViewModel(string settingsFolder, string recentFolder)
        {
            _store = new SettingsStore(settingsFolder);
            _recent = new RecentFiles(recentFolder);

            // Reading never writes: the first file appears only when the user changes something.
            Apply(_store.Load());
            _statusText = _store.LastLoadFailed ? "设置文件损坏，已回退到默认值"
                : File.Exists(_store.FilePath) ? "已从文件读到上次的设置"
                : "还没有设置文件，使用默认值";
            RefreshFileText();
        }

        public string SettingsPath => _store.FilePath;

        public string RecentPath => _recent.FilePath;

        public ObservableCollection<string> RecentItems => _recent.Items;

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private double _fontSize = 14;

        [ObservableProperty]
        private bool _notifyEnabled;

        [ObservableProperty]
        private int _saveCount;

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private string _fileText = "";

        partial void OnUsernameChanged(string value) => Save();

        partial void OnFontSizeChanged(double value) => Save();

        partial void OnNotifyEnabledChanged(bool value) => Save();

        [RelayCommand]
        private void Reload()
        {
            Apply(_store.Load());
            StatusText = _store.LastLoadFailed ? "设置文件损坏，已回退到默认值" : "已从文件重新读取";
            RefreshFileText();
        }

        [RelayCommand]
        private void Reset()
        {
            Apply(new AppSettings());
            Save();
            StatusText = "已恢复默认值并保存";
        }

        [RelayCommand]
        private void DeleteFile()
        {
            _store.Delete();
            StatusText = "设置文件已删除（界面上的值不变，下次启动会用默认值）";
            RefreshFileText();
        }

        [RelayCommand]
        private void CorruptFile()
        {
            _store.WriteRaw("{ this is not json");
            StatusText = "已把设置文件写坏，点「重新读取」看看会怎样";
            RefreshFileText();
        }

        [RelayCommand]
        private void AddRecent()
        {
            _recentCounter++;
            _recent.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"文档 {_recentCounter}.txt"));
        }

        [RelayCommand]
        private void ClearRecent() => _recent.Clear();

        private void Apply(AppSettings settings)
        {
            _loading = true;
            Username = settings.Username;
            FontSize = settings.FontSize;
            NotifyEnabled = settings.NotifyEnabled;
            _loading = false;
        }

        private void Save()
        {
            if (_loading) return;

            _store.Save(new AppSettings { Username = Username, FontSize = FontSize, NotifyEnabled = NotifyEnabled });
            SaveCount++;
            StatusText = $"已自动保存（第 {SaveCount} 次）";
            RefreshFileText();
        }

        private void RefreshFileText()
        {
            FileText = File.Exists(_store.FilePath) ? File.ReadAllText(_store.FilePath) : "（文件不存在）";
        }
    }
}
