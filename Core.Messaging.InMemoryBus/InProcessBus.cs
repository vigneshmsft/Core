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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public InProcessBus(IServiceScopeFactory serviceScopeFactory)
        {
            _createdPublishers = new List<WeakReference<InProcessEventPublisher>>();
            _subscriptionForTopics = new Dictionary<string, WeakReference<InProcessEventSubscription>>();
            _serviceScopeFactory = serviceScopeFactory;
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
            var subscription = new InProcessEventSubscription(topicName, _serviceScopeFactory);
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
        }
    }
}
