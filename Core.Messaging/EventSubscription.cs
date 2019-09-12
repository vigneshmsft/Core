namespace Core.Messaging.InProcess
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class EventSubscription : IEventSubscription
    {
        protected readonly string _topicName;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly SubscriptionFactory _subscriptionFactory;
        protected readonly ILog _log;

        protected EventSubscription(string topicName, IServiceProvider serviceProvider)
        {
            _topicName = topicName;
            _serviceProvider = serviceProvider;
            _subscriptionFactory = new SubscriptionFactory();
            _log = serviceProvider.GetRequiredService<ILog>();
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

        internal async Task OnEventReceived<TEvent>(TEvent @event) where TEvent : Event
        {
            var typeOfEvent = @event.GetType();
            var eventSubscriber = _subscriptionFactory.GetSubscriberForEvent(typeOfEvent);
            var subscribers = eventSubscriber?.EventSubscribers ?? new SubscriberInfo[0];

            var handleTasks = new List<Task>();

            foreach (var subscriberInfo in subscribers)
            {
                handleTasks.Add(Task.Run(async () =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var subscriber = scope.ServiceProvider.GetService(subscriberInfo.Type) as IEventSubscriber<TEvent>;
                        await subscriber?.Handle(@event);
                    }
                }));
            }

            await Task.WhenAll(handleTasks).ContinueWith(task =>
            {
                task.Exception.Handle(e =>
                {
                    return false;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
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
