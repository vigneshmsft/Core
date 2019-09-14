namespace Core.Messaging.AzureServiceBus
{
    using Logging;
    using Authentication.Tokens;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.DependencyInjection;
    using static Core.Serializers.JsonSerializer;

    public class ServiceBusMessageReceiver : IMessageReceiver
    {
        private readonly SubscriptionFactory _subscriptionFactory;
        private readonly ILog _log;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceBusMessageReceiver(SubscriptionFactory subscriptionFactory, ILog log, IServiceScopeFactory serviceScopeFactory)
        {
            _log = log;
            _subscriptionFactory = subscriptionFactory;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task OnExceptionReceived(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
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

            await Task.CompletedTask;
        }

        public async Task OnMessageReceived(Message message)
        {
            var eventName = message.Label;

            var typeOfEvent = Type.GetType(eventName);

            try
            {
                var provider = _serviceScopeFactory.CreateScope().ServiceProvider;
                var messageBody = Encoding.UTF8.GetString(message.Body);
                var authenticationToken = (string)message.UserProperties["AuthenticationToken"];
                var userName = (string)message.UserProperties["User"];

                _log.Event($"Received Message {eventName}", new Dictionary<string, string>
                {
                    { "MessageId", message.MessageId },
                    { "User", userName},
                    { "MessageBody", messageBody}
                });

                SetAuthenticationToken();

                var subscriberInfos = _subscriptionFactory.GetSubscriberForEvent(eventName)?.EventSubscribers;

                if (!subscriberInfos?.Any() ?? false)
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
                _log.Error(ex);

                _log.Warn(ex.Message);
                _log.Error(ex, new Dictionary<string, string>
                {
                    { "EventName", eventName },
                    { "MessageId", message.MessageId}
                });

                throw;
            }
        }
    }
}