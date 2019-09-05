namespace Core.Messaging.AzureServiceBus
{
    using System;

    public interface IEventSubscription : IDisposable
    {
        string Namespace { get; }

        void AddSubscription<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>;

        void RemoveSubscription<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>;
    }
}