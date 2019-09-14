namespace Core.Messaging
{
    using Logging;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class EventHandler
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly SubscriptionFactory _subscriptionFactory;

        public EventHandler(IServiceScopeFactory serviceScopeFactory, SubscriptionFactory subscriptionFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _subscriptionFactory = subscriptionFactory;
        }

        public async Task Handle<TEvent>(TEvent @event) where TEvent : Event
        {
            var typeOfEvent = @event.GetType();
            var eventSubscriber = _subscriptionFactory.GetSubscriberForEvent(typeOfEvent);
            var subscribers = eventSubscriber?.EventSubscribers ?? new SubscriberInfo[0];

            var handleTasks = new List<Task>();

            foreach (var subscriberInfo in subscribers)
            {
                handleTasks.Add(Task.Run(async () =>
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
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
    }
}
