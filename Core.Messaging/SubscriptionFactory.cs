namespace Core.Messaging
{
    using System;
    using System.Collections.Generic;

    public class SubscriptionFactory : IDisposable
    {
        private readonly Dictionary<Type, Subscription> _subscribers = new Dictionary<Type, Subscription>();

        public Subscription GetSubscriberForEvent<TEvent>() where TEvent : Event
        {
            return GetSubscriberForEvent(typeof(TEvent));
        }

        public Subscription GetSubscriberForEvent(Type typeOfEvent)
        {
            Subscription _subscriber = null;

            if (_subscribers.ContainsKey(typeOfEvent))
            {
                _subscriber = _subscribers[typeOfEvent];
            }

            return _subscriber;
        }

        public Subscription CreateSubscriberForEvent<TEvent>() where TEvent : Event
        {
            Subscription _subscriber = new Subscription();
            _subscribers[typeof(TEvent)] = _subscriber;
            return _subscriber;
        }

        public void Dispose()
        {
            _subscribers.Clear();
        }
    }
}
