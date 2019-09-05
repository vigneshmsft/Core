namespace Core.Messaging.AzureServiceBus
{
    using System;
    using System.Collections.Generic;

    internal class SubscriberInfo : IEqualityComparer<SubscriberInfo>
    {
        public Type EventType { get; set; }
        public Type Type { get; private set; }
        public string Name { get; private set; }

        private SubscriberInfo()
        {
        }

        public static SubscriberInfo From<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>
        {
            return new SubscriberInfo
            {
                EventType = typeof(TEvent),
                Type = typeof(TEventSubscriber),
                Name = typeof(TEventSubscriber).FullName
            };
        }

        public bool Equals(SubscriberInfo x, SubscriberInfo y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;

            return x.Name == y.Name && x.Type == y.Type;
        }

        public int GetHashCode(SubscriberInfo obj)
        {
            return obj.Name.GetHashCode();
        }

        public string RuleName => string.Format("{0}::{1}", EventType.Name, Type.Name);
    }
}