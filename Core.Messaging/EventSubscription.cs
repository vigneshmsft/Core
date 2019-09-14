namespace Core.Messaging
{
    using System;
    using System.Threading.Tasks;
    using Core.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class EventSubscription : IEventSubscription
    {
        protected readonly string _topicName;
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected readonly SubscriptionFactory _subscriptionFactory;
        protected readonly ILog _log;
        protected readonly EventHandler _eventHandler;

        protected EventSubscription(string topicName, IServiceScopeFactory serviceScopeFactory)
        {
            _topicName = topicName;
            _serviceScopeFactory = serviceScopeFactory;
            _subscriptionFactory = new SubscriptionFactory();
            _log = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILog>();
            _eventHandler = new EventHandler(serviceScopeFactory, _subscriptionFactory);
        }

        public abstract string Namespace { get; }

        public void AddSubscriber<TEvent, TEventSubscriber>()
            where TEvent : Event
            where TEventSubscriber : IEventSubscriber<TEvent>
        {
            _log.Trace($"Adding {nameof(TEventSubscriber)} as subscriber for {nameof(TEvent)}");

            var subscriber = _subscriptionFactory.GetSubscriberForEvent<TEvent>();
            if (subscriber == null)
            {
                _log.Trace($"Creating subscription for {nameof(TEvent)}");
                subscriber = _subscriptionFactory.CreateSubscriberForEvent<TEvent>();
            }
            subscriber.Add<TEvent, TEventSubscriber>();
        }

        public void RemoveSubscriber<TEvent, TEventSubscriber>()
            where TEvent : Event
            where TEventSubscriber : IEventSubscriber<TEvent>
        {
            _log.Trace($"Removing {nameof(TEventSubscriber)} as subscriber for {nameof(TEvent)}");

            var subscriber = _subscriptionFactory.GetSubscriberForEvent<TEvent>();
            subscriber?.Remove<TEvent, TEventSubscriber>();
        }

        protected async Task OnEventReceived<TEvent>(TEvent @event) where TEvent : Event
        {
            _log.Trace($"OnEventReceived {nameof(TEvent)}");
            await _eventHandler.Handle(@event);
        }

        protected void LogExceptions(Exception exception)
        {
            if (exception == null) return;

            var innerException = exception.InnerException;

            if (innerException != null)
            {
                _log.Trace("Logging Inner Exception !!!");
                LogExceptions(innerException);
            }

            _log.Error(exception);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _subscriptionFactory.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
