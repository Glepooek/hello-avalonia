using System;
using System.Collections.Generic;

namespace Avalonia.BehaviorsDemo.ViewModels
{
    /// <summary>
    /// A minimal push source for ScrollToItemBehavior.Item, which wants an IObservable.
    /// The project has no System.Reactive, and one Publish method is all the page needs.
    /// </summary>
    public sealed class ScrollRequest : IObservable<object>
    {
        private readonly List<IObserver<object>> _observers = new();

        public IDisposable Subscribe(IObserver<object> observer)
        {
            _observers.Add(observer);
            return new Subscription(this, observer);
        }

        public void Publish(object item)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(item);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ScrollRequest _owner;
            private readonly IObserver<object> _observer;

            public Subscription(ScrollRequest owner, IObserver<object> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Dispose() => _owner._observers.Remove(_observer);
        }
    }
}
