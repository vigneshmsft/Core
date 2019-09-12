namespace Core.Messaging
{
    using System.Collections.Generic;

    public sealed class Subscription
    {
        private readonly List<SubscriberInfo> _subscriberInfos = new List<SubscriberInfo>(3);

        internal Subscription() { }

        public ICollection<SubscriberInfo> EventSubscribers => _subscriberInfos;

        public int Count => _subscriberInfos.Count;

        public void Add<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>
        {
            var subscriberInfo = SubscriberInfo.From<TEvent, TEventSubscriber>();
            if (_subscriberInfos.Contains(subscriberInfo))
            {
                return;
            }
            _subscriberInfos.Add(subscriberInfo);
        }

        public void Remove<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>
        {
            var subscriberInfo = SubscriberInfo.From<TEvent, TEventSubscriber>();
            if (_subscriberInfos.Contains(subscriberInfo))
            {
                return;
            }
            _subscriberInfos.Remove(subscriberInfo);
        }
    }
}