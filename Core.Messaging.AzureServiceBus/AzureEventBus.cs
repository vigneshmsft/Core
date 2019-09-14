namespace Core.Messaging.AzureServiceBus
{
    using Logging;
    using Authentication;
    using Microsoft.Extensions.DependencyInjection;

    //TODO: Check and Fix Service Provider Scopes
    /// <summary>
    /// Represents an event bus for a given topic.
    /// </summary>
    public class AzureEventBus
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _activeConnectionString;
        private readonly string _passiveConnectionString;

        /// <summary>
        /// Creates an <see cref="IEventPublisher"/> instance of the service bus.
        /// </summary>
        public AzureEventBus(IServiceScopeFactory scopeFactory, string activeConnectionString, string passiveConnectionString)
        : this(scopeFactory, activeConnectionString)
        {
            _passiveConnectionString = passiveConnectionString;
        }

        public AzureEventBus(IServiceScopeFactory scopeFactory, string activeConnectionString)
        {
            _serviceScopeFactory = scopeFactory;
            _activeConnectionString = activeConnectionString;
        }

        public IEventPublisher CreatePublisher(string topicName)
        {
            var serviceProvider = _serviceScopeFactory.CreateScope().ServiceProvider;
            return new AzureEventPublisher(serviceProvider.GetService<ILog>(),
                                                      serviceProvider.GetService<IUserProvider>(),
                                                      _activeConnectionString,
                                                      _passiveConnectionString, topicName);
        }

        public IEventSubscription AddSubscription(string topicName, string subscriptionName, bool sessionEnabled = false)
        {
            return new AzureEventSubscription(_serviceScopeFactory, _activeConnectionString, _passiveConnectionString,
                                                               topicName, subscriptionName, sessionEnabled);
        }
    }
}