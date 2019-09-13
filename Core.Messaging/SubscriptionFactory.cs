namespace Core.Messaging
{
    using System;
    using System.Collections.Generic;

    public class SubscriptionFactory : IDisposable
    {
        private readonly Dictionary<string, Subscription> _subscribers = new Dictionary<string, Subscription>();

        public Subscription GetSubscriberForEvent<TEvent>() where TEvent : Event
        {
            return GetSubscriberForEvent(typeof(TEvent));
        }

        public Subscription GetSubscriberForEvent(Type typeOfEvent)
        {
            return GetSubscriberForEvent(typeOfEvent.FullName);
        }

        public Subscription GetSubscriberForEvent(string typeOfEvent)
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
            _subscribers[typeof(TEvent).FullName] = _subscriber;
            return _subscriber;
        }

        public void Dispose()
        {
            _subscribers.Clear();
        }
    }
}
