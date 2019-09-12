namespace Core.Messaging.InProcess
{
    using System;

    public class InProcessEventSubscription : EventSubscription
    {
        public InProcessEventSubscription(string topicName, IServiceProvider serviceProvider)
            :base(topicName, serviceProvider)
        {

        }

        public override string Namespace => $"{_topicName} {System.Reflection.Assembly.GetExecutingAssembly().FullName}";
    }
}
