namespace Core.Messaging.AzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Logging;
    using Authentication.Tokens;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.DependencyInjection;
    using static Core.Serializers.JsonSerializer;

    internal class AzureEventSubscription : IEventSubscription
    {
        private readonly ILog _log;
        private readonly IServiceProvider _scopeProvider;

        private SubscriptionClient _activeSubscriptionClient;
        private SubscriptionClient _passiveSubscriptionClient;

        private static readonly RetryPolicy RetryPolicy = new RetryExponential(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60), 20);

        private readonly Dictionary<string, List<SubscriberInfo>> _subscribers = new Dictionary<string, List<SubscriberInfo>>();

        public string Namespace { get; }

        public AzureEventSubscription(IServiceProvider scopeProvider, string activeConnectionString, string passiveConnectionString, string topicName, string subscriptionName, bool sessionEnabled = false)
        {
            _scopeProvider = scopeProvider;

            _log = scopeProvider.GetService<ILog>();

            Namespace = activeConnectionString.Substring(0, activeConnectionString.IndexOf(';'));

            CreateSubscriptionClient(activeConnectionString, passiveConnectionString, topicName, subscriptionName, sessionEnabled);
        }

        private void CreateSubscriptionClient(string activeConnectionString, string passiveConnectionString, string topicName, string subscriptionName, bool sessionEnabled)
        {
            try
            {
                _activeSubscriptionClient = new SubscriptionClient(activeConnectionString, topicName, subscriptionName,
                    retryPolicy: RetryPolicy);

                if (sessionEnabled)
                {
                    RegisterSubscriptionClientSessionMessageHandler(_activeSubscriptionClient);
                }
                else
                {
                    RegisterSubscriptionClientMessageHandler(_activeSubscriptionClient);
                }
            }
            catch (Exception exception)
            {
                var activeNamespace = activeConnectionString.Substring(0, activeConnectionString.IndexOf(';'));
                _log.Error($"Error subscribing to {activeNamespace}");
                _log.Error(exception);
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(passiveConnectionString))
                {
                    _passiveSubscriptionClient = new SubscriptionClient(passiveConnectionString, topicName,
                        subscriptionName, retryPolicy: RetryPolicy);
                    if (sessionEnabled)
                    {
                        RegisterSubscriptionClientSessionMessageHandler(_passiveSubscriptionClient);
                    }
                    else
                    {
                        RegisterSubscriptionClientMessageHandler(_passiveSubscriptionClient);
                    }
                }
            }
            catch (Exception exception)
            {
                var passiveNamespace = passiveConnectionString?.Substring(0, passiveConnectionString.IndexOf(';'));
                _log.Error($"Error subscribing to {passiveNamespace}");
                _log.Error(exception);
            }
        }

        private void RegisterSubscriptionClientMessageHandler(SubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    _log.Trace($"Received Message {message.MessageId} {message.Label} for {subscriptionClient.ServiceBusConnection.Endpoint}");

                    if (!token.IsCancellationRequested && !subscriptionClient.IsClosedOrClosing)
                    {
                        await OnMessageReceived(message);
                        // Complete the message so that it is not received again.
                        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                    else
                    {
                        _log.Warn($"SubscriptionClient MessageReceived CancellationRequested or IsClosedOrClosing");
                        await subscriptionClient.AbandonAsync(message.SystemProperties.LockToken);
                    }
                },
                new MessageHandlerOptions(ExceptionReceivedHandler)
                {
                    MaxConcurrentCalls = 20,
                    AutoComplete = false,
                    MaxAutoRenewDuration = TimeSpan.FromSeconds(60)
                });
        }

        private void RegisterSubscriptionClientSessionMessageHandler(SubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterSessionHandler(async (session, message, token) =>
            {
                _log.Trace($"Received Message {message.MessageId} session {session.SessionId} {message.Label} for {subscriptionClient.ServiceBusConnection.Endpoint}");
                if (!token.IsCancellationRequested && !subscriptionClient.IsClosedOrClosing && !session.IsClosedOrClosing)
                {
                    await OnMessageReceived(message);
                    await session.CompleteAsync(message.SystemProperties.LockToken);
                    //TODO: Session to be closed based on the message.Not for all messages.
                    await session.CloseAsync();
                }
                else
                {
                    _log.Warn($"SubscriptionClient MessageReceived CancellationRequested or IsClosedOrClosing");
                    await session.AbandonAsync(message.SystemProperties.LockToken);
                }

            }, new SessionHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false,
                MaxConcurrentSessions = 1,
                MaxAutoRenewDuration = TimeSpan.FromSeconds(60)
            });
        }

        public void AddSubscription<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>
        {
            var eventName = GetTypeName<TEvent>();

            _log.Trace($"AddSubscription {typeof(TEventSubscriber).FullName} for {eventName}");

            List<SubscriberInfo> currentSubscribers = GetCurrentSubscribers(eventName);

            var subscriptionInfo = SubscriberInfo.From<TEvent, TEventSubscriber>();

            if (!currentSubscribers.Contains(subscriptionInfo))
            {
                _log.Trace($"Subscription already exists for {typeof(TEventSubscriber).FullName} for event {eventName}");

                //_activeSubscriptionClient.AddRuleAsync(new RuleDescription(subscriptionInfo.RuleName, new CorrelationFilter
                //{
                //    Label = eventName

                //})).GetAwaiter().GetResult();

                currentSubscribers.Add(subscriptionInfo);
            }

            _log.Trace($"Subscription added for {typeof(TEventSubscriber).FullName} for event {eventName}");

            _subscribers[eventName] = currentSubscribers;
        }

        public void RemoveSubscription<TEvent, TEventSubscriber>() where TEvent : Event where TEventSubscriber : IEventSubscriber<TEvent>
        {
            var eventName = GetTypeName<TEvent>();

            List<SubscriberInfo> currentSubscribers = GetCurrentSubscribers(eventName);

            var subscriptionInfo = SubscriberInfo.From<TEvent, TEventSubscriber>();

            _log.Trace($"RemoveSubscription {subscriptionInfo.EventType} for {eventName}");

            if (!currentSubscribers.Contains(subscriptionInfo))
            {
                return;
            }

            //_activeSubscriptionClient.RemoveRuleAsync(subscriptionInfo.RuleName)
            //                   .GetAwaiter().GetResult();

            currentSubscribers.Remove(subscriptionInfo);

            _subscribers[eventName] = currentSubscribers;
        }

        internal IEnumerable<SubscriberInfo> GetSubscribersForEvent(string eventName)
        {
            var currentSubscribers = GetCurrentSubscribers(eventName);

            return (IEnumerable<SubscriberInfo>)currentSubscribers ?? new SubscriberInfo[0];
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            _log.Warn($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}");

            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            _log.Error(exceptionReceivedEventArgs.Exception, new Dictionary<string, string>
            {
                { "Endpoint", context.Endpoint },
                { "Entity Path", context.EntityPath},
                { "Executing Action", context.Action},
                { "ClientId", context.ClientId}
            });

            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(Message message)
        {
            var eventName = message.Label;

            try
            {
                var provider = _scopeProvider.CreateScope().ServiceProvider;
                var messageBody = Encoding.UTF8.GetString(message.Body);
                var authenticationToken = (string)message.UserProperties["AuthenticationToken"];
                var userName = (string)message.UserProperties["User"];
                _log.Trace($"Received Message {eventName}");
                _log.Event($"Received Message {eventName}", new Dictionary<string, string>
                {
                    { "MessageId", message.MessageId },
                    { "User", userName},
                    { "MessageBody", messageBody}
                });

                SetAuthenticationToken();

                var subscriberInfos = GetSubscribersForEvent(eventName).ToArray();

                if (!subscriberInfos.Any())
                {
                    _log.Warn($"No subscribers found for {eventName}");
                    return;
                }

                var handleTasks = new List<Task>();

                foreach (var subscriberInfo in subscriberInfos)
                {
                    var eventSubscribers = provider.GetServices(subscriberInfo.Type).ToArray();

                    if (!eventSubscribers.Any())
                    {
                        _log.Warn($"No types registered as {subscriberInfo.Type}");
                        continue;
                    }

                    var eventData = FromJson<object>(messageBody);

                    var handleMethod = subscriberInfo.Type.GetMethod("Handle", new[] { subscriberInfo.EventType });

                    foreach (var eventSubscriber in eventSubscribers)
                    {
                        var handleTask = (Task)handleMethod.Invoke(eventSubscriber, new[] { eventData });
                        handleTasks.Add(handleTask);

#pragma warning disable 4014
                        handleTask.ContinueWith(task =>
#pragma warning restore 4014
                        {
                            if (handleTask.IsFaulted)
                            {
                                var aggException = handleTask.Exception;
                                _log.Error(new Exception("Handle Task Exception -> " + aggException.Message));
                                aggException.Handle(hndExp =>
                                {
                                    _log.Error(hndExp, new Dictionary<string, string> {
                                        { "Handler", eventSubscriber.ToString() },
                                        { "SubscriberType", subscriberInfo.Type.FullName },
                                        { "EventType", subscriberInfo.EventType.FullName}});

                                    return true;
                                });
                            }
                        }, TaskContinuationOptions.OnlyOnFaulted);

                    }
                }

                await Task.WhenAll(handleTasks);

                void SetAuthenticationToken()
                {
                    var tokenValidators = provider.GetServices<UserFromAuthenticationToken>();

                    foreach (var tokenValidator in tokenValidators)
                    {
                        var validUser = tokenValidator.ReadUserFromToken(authenticationToken);

                        _log.Info($"Authentication token validation result is {validUser} " +
                                  $"for User {tokenValidator.GetUser()?.LoginName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogExceptions(ex);

                _log.Warn(ex.Message);
                _log.Error(ex, new Dictionary<string, string>
                {
                    { "EventName", eventName },
                    { "MessageId", message.MessageId}
                });

                throw;
            }
        }


        private string GetTypeName<TEvent>()
        {
            return typeof(TEvent).FullName;
        }

        private List<SubscriberInfo> GetCurrentSubscribers(string eventName)
        {
            _log.Trace($"GetCurrentSubscribers for {eventName}");

            List<SubscriberInfo> currentSubscribers = null;
            if (_subscribers.ContainsKey(eventName))
            {
                currentSubscribers = _subscribers[eventName];
            }
            else
            {
                currentSubscribers = new List<SubscriberInfo>();
                _subscribers.Add(eventName, currentSubscribers);
            }

            _log.Trace($"GetCurrentSubscribers for {eventName} returned {currentSubscribers.Count} subscribers.");

            return currentSubscribers;
        }

        public void Dispose()
        {
            _activeSubscriptionClient?.CloseAsync()
                               .GetAwaiter().GetResult();

            _passiveSubscriptionClient?.CloseAsync()
                               .GetAwaiter().GetResult();

            _subscribers.Clear();
        }

        private void LogExceptions(Exception ex)
        {
            var innerException = ex.InnerException;

            if (innerException != null)
            {
                _log.Trace("Logging Inner Exception !!!");
                LogExceptions(innerException);
            }

            _log.Error(ex);
        }
    }
}