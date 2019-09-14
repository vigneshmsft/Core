namespace Core.Messaging.AzureServiceBus
{
    using Microsoft.Azure.ServiceBus;
    using System.Threading.Tasks;

    public interface IMessageReceiver
    {
        Task OnMessageReceived(Message message);

        Task OnExceptionReceived(ExceptionReceivedEventArgs exceptionReceivedEventArgs);
    }
}