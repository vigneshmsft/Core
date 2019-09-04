namespace Core.Messaging
{
    using System.Threading.Tasks;

    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event to the bus.
        /// </summary>
        /// <typeparam name="TEvent">The event to be published.</typeparam>
        Task Publish<TEvent>(TEvent @event) where TEvent : Event;

        /// <summary>
        /// The namespace of the bus where this instance publishes the events.
        /// </summary>
        string Namespace { get; }
    }
}