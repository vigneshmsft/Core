using System;
using System.Threading.Tasks;

namespace Core.Messaging.InProcess
{
    public delegate Task EventPublished<TEvent>(string topicName, Type type, TEvent @event) where TEvent : Event;
    public class InProcessEventPublisher : IEventPublisher
    {
        private readonly string _topicName;

        public InProcessEventPublisher(string topicName)
        {
            _topicName = topicName;
        }

        private event EventPublished<Event> publishEvent;
        public string Namespace => $"{_topicName} {System.Reflection.Assembly.GetExecutingAssembly().FullName}";

        public event EventPublished<Event> OnEventPublished
        {
            add { publishEvent += value; }
            remove { publishEvent -= value; }
        }

        public async Task Publish<TEvent>(TEvent @event) where TEvent : Event
        {
            await publishEvent?.Invoke(_topicName, typeof(TEvent), @event);
        }
    }
}
