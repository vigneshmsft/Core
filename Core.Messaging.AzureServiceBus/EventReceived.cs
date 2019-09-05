namespace Core.Messaging.AzureServiceBus
{
    using Microsoft.Azure.ServiceBus;

    internal delegate void MessageReceived(Message message);
}
