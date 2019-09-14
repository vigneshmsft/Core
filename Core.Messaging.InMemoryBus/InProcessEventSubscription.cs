namespace Core.Messaging.InProcess
{
    using Microsoft.Extensions.DependencyInjection;

    public class InProcessEventSubscription : EventSubscription
    {
        public InProcessEventSubscription(string topicName, IServiceScopeFactory serviceScopeFactory)
            :base(topicName, serviceScopeFactory)
        {

        }

        public override string Namespace => $"{_topicName} {System.Reflection.Assembly.GetExecutingAssembly().FullName}";
    }
}
