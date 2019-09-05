namespace Core.Messaging.AzureServiceBus
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using Authentication;
    using Serializers;
    using Microsoft.Azure.ServiceBus;
    using Newtonsoft.Json;

    internal class AzureEventPublisher : IEventPublisher, IDisposable
    {
        private static readonly object SwapConnectionLock = new object();
        private string _activeConnectionString;
        private string _passiveConnectionString;
        private readonly bool _hasFallback;
        private readonly ManualResetEventSlim _swapInProgress = new ManualResetEventSlim(true, 400);
        private DateTime _lastConnectionSwap = DateTime.MinValue;

        private static readonly RetryPolicy RetryPolicy = new RetryExponential(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), 10);

        private ITopicClient _activeTopicClient;
        private ITopicClient _passiveTopicClient;

        private readonly ILog _log;
        private readonly IUserProvider _userProvider;

        public string Namespace { get; private set; }

        public AzureEventPublisher(ILog log, IUserProvider userProvider, string activeConnectionString, string topicName)
        {
            _log = log;
            _userProvider = userProvider;
            _activeConnectionString = activeConnectionString;
            TopicName = topicName;

            _activeTopicClient = CreateTopicClient(activeConnectionString, topicName);

            Namespace = _activeTopicClient.ServiceBusConnection.Endpoint.AbsoluteUri;
        }

        public AzureEventPublisher(ILog log, IUserProvider userProvider, string activeConnectionString, string passiveConnectionString, string topicName)
        : this(log, userProvider, activeConnectionString, topicName)
        {
            if (string.IsNullOrWhiteSpace(passiveConnectionString) || activeConnectionString == passiveConnectionString)
            {
                _log.Error("Use a valid passive connection string");
                _hasFallback = false;
            }
            else
            {
                _hasFallback = true;
                _passiveConnectionString = passiveConnectionString;
                _passiveTopicClient = CreateTopicClient(passiveConnectionString, topicName);
            }
        }

        private static ITopicClient CreateTopicClient(string connectionString, string topicName)
        {
            return new TopicClient(connectionString, topicName, RetryPolicy)
            {
                OperationTimeout = TimeSpan.FromSeconds(45)
            };
        }

        public async Task Publish<TEvent>(TEvent @event) where TEvent : Event
        {
            var eventName = typeof(TEvent).FullName;
            string eventDataJson = string.Empty;
            try
            {
                eventDataJson = JsonConvert.SerializeObject(@event, SerializerSettings.Default);
                var userName = _userProvider.GetUser()?.LoginName;

                var message = new Message(Encoding.UTF8.GetBytes(eventDataJson))
                {
                    Label = eventName,
                    MessageId = @event.EventId.ToString("N"),
                    UserProperties = { { "AuthenticationToken", _userProvider.GetToken() }, { "User", userName } }
                };

                if (@event is SessionEvent sessionEvent)
                {
                    message.SessionId = sessionEvent.SessionId;
                }

                _log.Event($"Publishing event {eventName} {message.MessageId} ...", new Dictionary<string, string>
                {
                    { "EventName", eventName },
                    { "MessageId", message.MessageId },
                    { "Message", eventDataJson },
                    { "User", userName},
                    { "SessionId", message.SessionId},
                    {"Namespace", Namespace}
                });

                _swapInProgress.Wait(TimeSpan.FromSeconds(10));

                //Not locking Send as swap should be a rare occurance
                if (!(await SendMessage(_activeTopicClient, message, eventName)))
                {
                    if (_hasFallback)
                    {
                        SwapActiveAndPassiveNamespace();
                        await SendMessage(_activeTopicClient, message.Clone(), eventName);
                    }
                }
            }
            catch (Exception exception)
            {
                _swapInProgress.Set();

                _log.Warn($"Error publishing {eventName} with Data {eventDataJson}");
                _log.Error(exception, new Dictionary<string, string>
                {
                    { "EventName", eventName },
                    { "Message", eventDataJson},
                    { "EventId", @event.EventId.ToString("N")}
                });
                throw;
            }
        }

        private async Task<bool> SendMessage(ITopicClient topicClient, Message message, string eventName)
        {
            int attempt = 0;
            bool sentSuccessfully = false;
            do
            {
                try
                {
                    sentSuccessfully = false;
                    await topicClient.SendAsync(message);
                    sentSuccessfully = true;
                }
                catch (Exception exception)
                {
                    sentSuccessfully = false;
                    _log.Warn($"Error publishing {eventName}");
                    _log.Error(exception, new Dictionary<string, string>
                    {
                        { "Event Name", eventName },
                        { "Inner Exception", exception.InnerException?.ToString()}
                    });

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                finally
                {
                    ++attempt;
                }
            } while (!sentSuccessfully && attempt < 5);

            return sentSuccessfully;
        }

        private void SwapActiveAndPassiveNamespace()
        {
            if (!_hasFallback)
            {
                _log.Warn("No Secondary Service Bus connection to fallback.");
                return;
            }

            if (DateTime.Now.Subtract(_lastConnectionSwap) < TimeSpan.FromMinutes(30))
            {
                _log.Info($"Not swapping as last swap was at {_lastConnectionSwap} which was less than 30 mins ago");
                return;
            }

            lock (SwapConnectionLock)
            {
                _swapInProgress.Reset();
                // Only swap connections if last swap was more than 30 mins ago, 
                // so as to avoid reswapping back to a failed connection in case another thread requests swap because SendMessage is not locked.
                if (DateTime.Now.Subtract(_lastConnectionSwap) > TimeSpan.FromMinutes(30))
                {
                    _log.Event($"Swapping Primary & Secondary Service Bus Connections",
                        new Dictionary<string, string>
                        {
                            {"Existing Primary", _activeTopicClient.ServiceBusConnection.Endpoint.AbsoluteUri},
                            {"Existing Secondary", _passiveTopicClient.ServiceBusConnection.Endpoint.AbsoluteUri}
                        });

                    Close();
                    _activeTopicClient = null;
                    _passiveTopicClient = null;

                    _activeTopicClient = CreateTopicClient(_passiveConnectionString, TopicName);
                    _passiveTopicClient = CreateTopicClient(_activeConnectionString, TopicName);

                    _lastConnectionSwap = DateTime.Now;

                    var swap = _activeConnectionString;
                    _activeConnectionString = _passiveConnectionString;
                    _passiveConnectionString = swap;
                    _swapInProgress.Set();
                    _log.Event($"Done Swapping Primary & Secondary Service Bus Connections",
                        new Dictionary<string, string>
                        {
                            {
                                "Current Primary",
                                _activeConnectionString.Substring(0, _activeConnectionString.IndexOf(';'))
                            },
                            {
                                "Current Secondary",
                                _passiveConnectionString.Substring(0, _passiveConnectionString.IndexOf(';'))
                            }
                        });

                    Namespace = _activeTopicClient.ServiceBusConnection.Endpoint.AbsoluteUri;
                }
            }

            _swapInProgress.Set();
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            _activeTopicClient?.CloseAsync();
            _passiveTopicClient?.CloseAsync();
        }

        internal string TopicName { get; }
    }
}