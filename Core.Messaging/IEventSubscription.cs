namespace Core.Messaging
{
    using System;

    public interface IEventSubscription : IDisposable
    {
        string Namespace { get; }

        void AddSubscriber<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>;

        void RemoveSubscriber<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>;
    }
}