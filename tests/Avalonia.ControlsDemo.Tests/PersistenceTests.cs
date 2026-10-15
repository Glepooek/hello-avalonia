using Avalonia.ControlsDemo.ViewModels;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Avalonia.ControlsDemo.Tests
{
    // The app keeps these files under %AppData% and %LocalAppData%; every test here points the stores at its own
    // temp folder instead, so running the tests never touches the settings of a real copy of the app.
    public sealed class PersistenceTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "controls-demo-tests-" + Guid.NewGuid().ToString("N"));

        private string Settings => Path.Combine(_root, "settings");
        private string Recent => Path.Combine(_root, "recent");

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [Fact]
        public void Default_folders_are_the_per_user_app_data_folders()
        {
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.ControlsDemo"),
                SettingsStore.DefaultDirectory);
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Avalonia.ControlsDemo"),
                RecentFiles.DefaultDirectory);
        }

        [Fact]
        public void Opening_the_page_reads_without_writing()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);

            Assert.Equal("还没有设置文件，使用默认值", vm.StatusText);
            Assert.Equal("（文件不存在）", vm.FileText);
            Assert.Equal(("", 14d, false), (vm.Username, vm.FontSize, vm.NotifyEnabled));
            Assert.False(Directory.Exists(_root));
        }

        [Fact]
        public void Every_change_saves_and_a_new_view_model_reads_it_back()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);

            vm.Username = "小明";
            vm.FontSize = 18;
            vm.NotifyEnabled = true;

            Assert.Equal(3, vm.SaveCount);
            Assert.Equal("已自动保存（第 3 次）", vm.StatusText);
            // Relaxed escaping keeps the Chinese name readable in the file.
            Assert.Contains("\"Username\": \"小明\"", vm.FileText);
            Assert.False(File.Exists(vm.SettingsPath + ".tmp"));

            var reopened = new DataPersistenceHowToViewModel(Settings, Recent);
            Assert.Equal("已从文件读到上次的设置", reopened.StatusText);
            Assert.Equal(("小明", 18d, true), (reopened.Username, reopened.FontSize, reopened.NotifyEnabled));
            // Loading assigns all three properties but must not save them again.
            Assert.Equal(0, reopened.SaveCount);
        }

        [Fact]
        public void A_corrupt_file_falls_back_to_defaults_on_reload_and_on_open()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);
            vm.Username = "小明";

            vm.CorruptFileCommand.Execute(null);
            Assert.Equal("{ this is not json", vm.FileText);
            Assert.Equal("已把设置文件写坏，点「重新读取」看看会怎样", vm.StatusText);

            vm.ReloadCommand.Execute(null);
            Assert.Equal("设置文件损坏，已回退到默认值", vm.StatusText);
            Assert.Equal("", vm.Username);
            Assert.Equal(1, vm.SaveCount);

            Assert.Equal("设置文件损坏，已回退到默认值", new DataPersistenceHowToViewModel(Settings, Recent).StatusText);
        }

        [Fact]
        public void Reset_saves_the_defaults_and_delete_keeps_the_values_on_screen()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);
            vm.Username = "小明";

            vm.ResetCommand.Execute(null);
            Assert.Equal("已恢复默认值并保存", vm.StatusText);
            Assert.Equal("", vm.Username);
            Assert.Equal(2, vm.SaveCount);

            vm.Username = "小红";
            vm.DeleteFileCommand.Execute(null);
            Assert.Equal("设置文件已删除（界面上的值不变，下次启动会用默认值）", vm.StatusText);
            Assert.Equal("（文件不存在）", vm.FileText);
            Assert.Equal("小红", vm.Username);
            Assert.False(File.Exists(vm.SettingsPath));
        }

        [Fact]
        public void Recent_files_keep_the_newest_ten_and_move_a_reopened_path_to_the_top()
        {
            var recent = new RecentFiles(Recent);
            for (var i = 1; i <= 12; i++) recent.Add($@"C:\docs\{i}.txt");

            Assert.Equal(RecentFiles.Capacity, recent.Items.Count);
            Assert.Equal(@"C:\docs\12.txt", recent.Items[0]);
            Assert.Equal(@"C:\docs\3.txt", recent.Items[^1]);

            // Case-insensitive: the same file with different casing is not listed twice.
            recent.Add(@"C:\DOCS\5.TXT");
            Assert.Equal(@"C:\DOCS\5.TXT", recent.Items[0]);
            Assert.Single(recent.Items, p => p.EndsWith("5.txt", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(10, recent.Items.Count);

            Assert.Equal(recent.Items, new RecentFiles(Recent).Items);

            recent.Clear();
            Assert.Empty(new RecentFiles(Recent).Items);
        }

        [Fact]
        public void An_unreadable_recent_list_is_an_empty_list()
        {
            Directory.CreateDirectory(Recent);
            File.WriteAllText(Path.Combine(Recent, "recent.json"), "not json");

            Assert.Empty(new RecentFiles(Recent).Items);
        }

        [Fact]
        public void The_page_command_adds_documents_paths_to_the_recent_list()
        {
            var vm = new DataPersistenceHowToViewModel(Settings, Recent);

            vm.AddRecentCommand.Execute(null);
            vm.AddRecentCommand.Execute(null);

            Assert.Equal(new[] { "文档 2.txt", "文档 1.txt" }, vm.RecentItems.Select(Path.GetFileName));
            Assert.True(File.Exists(vm.RecentPath));
            vm.ClearRecentCommand.Execute(null);
            Assert.Empty(vm.RecentItems);
        }
    }
}
