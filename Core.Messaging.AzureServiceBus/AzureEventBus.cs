namespace Core.Messaging.AzureServiceBus
{
    using System;
    using Logging;
    using Authentication;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Represents an event bus for a given topic.
    /// </summary>
    public class AzureEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _activeConnectionString;
        private readonly string _passiveConnectionString;

        /// <summary>
        /// Creates an <see cref="IEventPublisher"/> instance of the service bus.
        /// </summary>
        public AzureEventBus(IServiceProvider serviceProvider, string activeConnectionString, string passiveConnectionString)
        : this(serviceProvider, activeConnectionString)
        {
            _passiveConnectionString = passiveConnectionString;
        }

        public AzureEventBus(IServiceProvider serviceProvider, string activeConnectionString)
        {
            _serviceProvider = serviceProvider;
            _activeConnectionString = activeConnectionString;
        }

        public IEventPublisher CreatePublisher(string topicName)
        {
            return new AzureEventPublisher(_serviceProvider.GetService<ILog>(),
                                                      _serviceProvider.GetService<IUserProvider>(),
                                                      _activeConnectionString,
                                                      _passiveConnectionString, topicName);
        }

        public IEventSubscription AddSubscription(string topicName, string subscriptionName, bool sessionEnabled = false)
        {
            return new AzureEventSubscription(_serviceProvider, _activeConnectionString, _passiveConnectionString,
                                                               topicName, subscriptionName, sessionEnabled);
        }
    }
}