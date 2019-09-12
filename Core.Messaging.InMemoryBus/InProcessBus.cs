namespace Core.Messaging.InProcess
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    public class InProcessBus : IDisposable
    {
        private readonly List<WeakReference<InProcessEventPublisher>> _createdPublishers;
        private readonly Dictionary<string, WeakReference<InProcessEventSubscription>> _subscriptionForTopics;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _serviceProviderScope;

        public InProcessBus(IServiceProvider serviceProvider)
        {
            _createdPublishers = new List<WeakReference<InProcessEventPublisher>>();
            _subscriptionForTopics = new Dictionary<string, WeakReference<InProcessEventSubscription>>();
            _serviceProviderScope = serviceProvider.CreateScope();
            _serviceProvider = _serviceProviderScope.ServiceProvider;
        }

        public IEventPublisher CreatePublisher(string topicName)
        {
            var eventPublisher = new InProcessEventPublisher(topicName);
            eventPublisher.OnEventPublished += OnEventPublished;
            _createdPublishers.Add(new WeakReference<InProcessEventPublisher>(eventPublisher));
            return eventPublisher;
        }

        private Task OnEventPublished<TEvent>(string topicName, Type typeOfEvent, TEvent @event) where TEvent : Event
        {
            return Task.Factory.StartNew(async () =>
            {
                var subscriptionReference = _subscriptionForTopics[topicName];
                if (subscriptionReference.TryGetTarget(out InProcessEventSubscription subscription))
                {
                    var onEventReceivedMethod = typeof(EventSubscription)
                        .GetMethod("OnEventReceived", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .MakeGenericMethod(typeOfEvent);

                    await (Task)onEventReceivedMethod.Invoke(subscription, new object[] { @event });
                }
            });
        }

        public IEventSubscription CreateSubscription(string topicName)
        {
            var subscription = new InProcessEventSubscription(topicName, _serviceProvider);
            _subscriptionForTopics.Add(topicName, new WeakReference<InProcessEventSubscription>(subscription));
            return subscription;
        }

        public void Dispose()
        {
            foreach (var publisherReference in _createdPublishers)
            {
                if (publisherReference.TryGetTarget(out var publisher))
                {
                    publisher.OnEventPublished -= OnEventPublished;
                }
            }
            _createdPublishers.Clear();
            _subscriptionForTopics.Clear();
            _serviceProviderScope.Dispose();
        }
    }
}
