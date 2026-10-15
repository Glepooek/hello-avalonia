using Avalonia.BehaviorsDemo.ViewModels;
using System;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.BehaviorsDemo.Tests
{
    public class ViewModelTests
    {
        private sealed class ListObserver : IObserver<object>
        {
            public List<object> Items { get; } = new();
            public void OnNext(object value) => Items.Add(value);
            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }

        // The page binds the two Valid flags OneWayToSource from the validation behaviors; the VM only combines them.
        [Fact]
        public void Submit_is_allowed_only_when_both_fields_are_valid()
        {
            var vm = new ValidationViewModel();
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.UsernameValid = true;
            Assert.False(vm.SubmitCommand.CanExecute(null));

            vm.EmailValid = true;
            Assert.True(vm.CanSubmit);
            Assert.True(vm.SubmitCommand.CanExecute(null));
            vm.SubmitCommand.Execute(null);
            Assert.Equal("已提交：两个字段都通过了校验", vm.SubmitResult);
        }

        [Fact]
        public void Reset_todos_restores_the_three_items_in_order()
        {
            var vm = new CollectionViewModel();
            vm.Todos.RemoveAt(1);
            vm.Todos.Add("别的");

            vm.ResetTodosCommand.Execute(null);

            Assert.Equal(new[] { "写计划", "跑探针", "提交" }, vm.Todos);
        }

        [Fact]
        public void Scroll_to_publishes_the_numbered_item()
        {
            var vm = new CollectionViewModel();
            var observer = new ListObserver();
            using var subscription = vm.ScrollRequests.Subscribe(observer);

            vm.ScrollToCommand.Execute("150");

            Assert.Equal(new object[] { "第 150 项" }, observer.Items);
        }

        // ScrollToItemBehavior subscribes on attach and disposes on detach; a leaked observer would keep scrolling
        // a list that is no longer on screen.
        [Fact]
        public void Scroll_request_stops_publishing_after_the_subscription_is_disposed()
        {
            var requests = new ScrollRequest();
            var observer = new ListObserver();
            var subscription = requests.Subscribe(observer);
            requests.Publish("a");

            subscription.Dispose();
            requests.Publish("b");

            Assert.Equal(new object[] { "a" }, observer.Items);
        }

        [Fact]
        public void Order_text_follows_moves_in_the_collection()
        {
            var vm = new DragViewModel();
            var changed = new List<string?>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.Order.Move(0, 2);

            Assert.Equal("B → C → A → D", vm.OrderText);
            Assert.Contains(nameof(DragViewModel.OrderText), changed);
        }

        [Fact]
        public void Start_work_hands_the_trigger_a_new_task_each_time()
        {
            var vm = new DataTriggerViewModel();

            vm.StartWorkCommand.Execute(null);
            var first = vm.Work;
            vm.StartWorkCommand.Execute(null);

            Assert.Equal("运行中", vm.WorkStatus);
            Assert.NotNull(first);
            Assert.NotSame(first, vm.Work);

            vm.WorkDoneCommand.Execute(null);
            Assert.Equal("已完成", vm.WorkStatus);
            Assert.Equal(1, vm.DoneCount);
        }

        [Fact]
        public void Finish_counts_the_run_and_logs_after_the_steps()
        {
            var vm = new MechanismViewModel();

            vm.StepCommand.Execute("x");
            vm.FinishCommand.Execute(null);

            Assert.Equal(1, vm.Runs);
            Assert.Equal(new[] { "01  x", "02  -- 第 1 次触发结束 --" }, vm.Log.Entries);
        }
    }
}
