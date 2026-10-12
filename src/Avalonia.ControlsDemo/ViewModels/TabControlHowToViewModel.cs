using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace Avalonia.ControlsDemo.ViewModels
{
    public partial class TabControlHowToViewModel : ObservableObject
    {
        private static readonly StreamGeometry DocIcon = StreamGeometry.Parse("M6,2 H14 L18,6 V22 H6 Z");
        private static readonly StreamGeometry LogIcon = StreamGeometry.Parse("M4,5 H20 V7 H4 Z M4,11 H20 V13 H4 Z M4,17 H20 V19 H4 Z");
        private static readonly StreamGeometry GearIcon = StreamGeometry.Parse("M12,2 L14,6 L19,6 L17,10 L19,14 L14,14 L12,18 L10,14 L5,14 L7,10 L5,6 L10,6 Z");

        private int _nextNumber = 4;

        public TabControlHowToViewModel()
        {
            Tabs.Add(new DocumentTab("说明", DocIcon, "这是第一个标签。"));
            Tabs.Add(new DocumentTab("日志", LogIcon, "这是日志标签。"));
            Tabs.Add(new DocumentTab("设置", GearIcon, "这是设置标签。"));
            _selectedTab = Tabs[0];

            WizardSteps = new[]
            {
                new WizardStep("账号", () => Build("账号")),
                new WizardStep("资料", () => Build("资料")),
                new WizardStep("确认", () => Build("确认"))
            };
        }

        public ObservableCollection<DocumentTab> Tabs { get; } = new();

        [ObservableProperty]
        private DocumentTab? _selectedTab;

        [RelayCommand]
        private void AddTab()
        {
            var tab = new DocumentTab($"文档 {_nextNumber++}", DocIcon, "新建的文档，可以直接编辑。");
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        [RelayCommand]
        private void CloseTab(DocumentTab? tab)
        {
            // Never close the last tab: the TabControl would have nothing left to show.
            if (tab is null || Tabs.Count <= 1) return;

            int index = Tabs.IndexOf(tab);
            bool wasSelected = ReferenceEquals(tab, SelectedTab);
            Tabs.Remove(tab);
            if (wasSelected)
            {
                SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
            }
        }

        public WizardStep[] WizardSteps { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
        private int _wizardIndex;

        [ObservableProperty]
        private int _createdCount;

        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private void PreviousStep() => WizardIndex--;

        private bool CanGoPrevious() => WizardIndex > 0;

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void NextStep() => WizardIndex++;

        private bool CanGoNext() => WizardIndex < WizardSteps.Length - 1;

        private string Build(string name)
        {
            CreatedCount++;
            return $"「{name}」步骤的内容（第一次显示时才创建）。";
        }
    }
}
